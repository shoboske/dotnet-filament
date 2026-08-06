using Fila.Tables;
using Microsoft.EntityFrameworkCore;

namespace Fila.Resources;

/// <summary>Non-generic view of a resource, used by panel routing and the CLI.</summary>
public interface IResource
{
    string Slug { get; }
    string? NavigationIcon { get; }
    Type EntityType { get; }

    ITable BuildTable();

    Task<PagedRows> ListAsync(DbContext db, ITable table, TableQuery query, CancellationToken ct);
}

public abstract class Resource<TEntity> : IResource
    where TEntity : class
{
    public virtual string? NavigationIcon => null;

    public virtual string Slug => ResourceNaming.ToSlug(typeof(TEntity).Name);

    public Type EntityType => typeof(TEntity);

    /// <summary>Build the table definition. Called once per request (memoize per resource type
    /// later if profiling says otherwise).</summary>
    protected abstract Table<TEntity> Table(Table<TEntity> t);

    /// <summary>Override to add `.Include(...)` and other query shaping.</summary>
    protected virtual IQueryable<TEntity> Query(IQueryable<TEntity> q) => q;

    public ITable BuildTable() => Table(new Table<TEntity>());

    public async Task<PagedRows> ListAsync(DbContext db, ITable table, TableQuery query, CancellationToken ct)
    {
        var source = Query(db.Set<TEntity>());
        source = source.ApplySearch(table, query.Search);
        source = source.ApplySort(table, query);
        return await source.PaginateAsync(query.Page, table.PerPage, ct);
    }

}
