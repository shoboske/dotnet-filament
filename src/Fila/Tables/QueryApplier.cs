using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Fila.Tables;

public static class QueryApplier
{
    /// <summary>ORs together `Contains` across every searchable column. Returns the source
    /// unchanged when the term is blank or no column is searchable.</summary>
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> source, ITable table, string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return source;

        var searchable = table.Columns.Where(c => c.IsSearchable).ToList();
        if (searchable.Count == 0) return source;

        var parameter = Expression.Parameter(typeof(T), "e");
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        // Contains() lowers to different SQL per provider — SQLite's instr() is case-sensitive,
        // SQL Server's LIKE usually isn't. Lowering both sides makes search behave the same way
        // regardless of provider.
        var termConstant = Expression.Constant(term.ToLowerInvariant());

        Expression? predicate = null;

        foreach (var column in searchable)
        {
            Expression member = BuildMemberPath(parameter, column.Path);

            var toStringExpr = member.Type == typeof(string)
                ? member
                : Expression.Call(member, member.Type.GetMethod(nameof(ToString), Type.EmptyTypes)!);

            var loweredExpr = Expression.Call(toStringExpr, toLowerMethod);
            Expression contains = Expression.Call(loweredExpr, containsMethod, termConstant);

            var isNullable = !member.Type.IsValueType || Nullable.GetUnderlyingType(member.Type) is not null;
            Expression guarded = isNullable
                ? Expression.AndAlso(Expression.NotEqual(member, Expression.Constant(null, member.Type)), contains)
                : contains;

            predicate = predicate is null ? guarded : Expression.OrElse(predicate, guarded);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(predicate!, parameter);
        return source.Where(lambda);
    }

    /// <summary>Builds ORDER BY from the validated query, falling back to the table's default
    /// sort, then to no ordering. The requested sort path is always checked against
    /// <see cref="ITableColumn.IsSortable"/> columns before being turned into an expression —
    /// never build a member expression from a raw client string.</summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, ITable table, TableQuery query)
    {
        var requested = query.Sort is null
            ? null
            : table.Columns.FirstOrDefault(c => c.IsSortable && c.Path == query.Sort);

        if (requested is not null)
            return ApplyOrderBy(source, requested.Path, query.Dir == "desc");

        if (table.DefaultSortPath is { } defaultPath)
            return ApplyOrderBy(source, defaultPath, table.DefaultSortDescending);

        return source;
    }

    public static async Task<PagedRows> PaginateAsync<T>(
        this IQueryable<T> source, int page, int perPage, CancellationToken ct = default)
    {
        var total = await source.CountAsync(ct);
        var lastPage = total == 0 ? 1 : (int)Math.Ceiling(total / (double)perPage);
        var clampedPage = Math.Clamp(page, 1, lastPage);

        var rows = await source
            .Skip((clampedPage - 1) * perPage)
            .Take(perPage)
            .ToListAsync(ct);

        return new PagedRows
        {
            Rows = rows.Cast<object>().ToList(),
            Page = clampedPage,
            PerPage = perPage,
            Total = total,
        };
    }

    private static IQueryable<T> ApplyOrderBy<T>(IQueryable<T> source, string path, bool descending)
    {
        var parameter = Expression.Parameter(typeof(T), "e");
        var member = BuildMemberPath(parameter, path);
        var lambda = Expression.Lambda(member, parameter);

        var methodName = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), member.Type);

        var call = Expression.Call(method, source.Expression, lambda);
        return source.Provider.CreateQuery<T>(call);
    }

    private static Expression BuildMemberPath(Expression parameter, string path)
    {
        Expression member = parameter;
        foreach (var segment in path.Split('.'))
            member = Expression.PropertyOrField(member, segment);
        return member;
    }
}
