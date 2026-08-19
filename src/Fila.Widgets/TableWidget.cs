using Fila.Tables;
using Microsoft.EntityFrameworkCore;

namespace Fila.Widgets;

/// <summary>Embeds a Fila.Tables table on the dashboard — Filament's Widgets\TableWidget. The
/// "5 most recent orders" panel: the same column definitions a resource's list page uses, over
/// a shaped, unpaginated top-N query.
///
/// Declared exactly like a resource's table, because it is one:
/// <c>t.Columns(...).DefaultSort(o =&gt; o.CreatedAt, descending: true).PaginateBy(5)</c>. There
/// is no pagination, search or sorting UI on a dashboard table — PaginateBy is read as "how
/// many rows to show" and the default sort decides which ones those are.</summary>
public abstract class TableWidget<TEntity> : Widget
    where TEntity : class
{
    public sealed override string View => "table";

    /// <summary>Define the columns and the row cap, same DSL as Resource&lt;TEntity&gt;.Table.</summary>
    protected abstract Table<TEntity> Table(Table<TEntity> t);

    /// <summary>Override to add `.Include(...)`, a `.Where(...)` narrowing the widget to a
    /// slice of the table, and any other query shaping.</summary>
    protected virtual IQueryable<TEntity> Query(IQueryable<TEntity> q) => q;

    public sealed override async Task<object> LoadAsync(WidgetContext context)
    {
        var table = Table(new Table<TEntity>());
        var built = (ITable)table;

        // ApplySort with an all-defaults query means "no request asked for a sort", which is
        // exactly right here — there is no sortable header for the visitor to have clicked —
        // so it falls through to the table's own DefaultSort.
        var rows = await Query(context.Db.Set<TEntity>())
            .ApplySort(built, new TableQuery(null, null, "asc", 1))
            .Take(built.PerPage)
            .ToListAsync(context.Ct);

        return new TableWidgetData(built, rows.Cast<object>().ToList());
    }
}

/// <summary>What a table widget's partial draws: the column definitions plus the rows to run
/// them over. Non-generic so the partial never needs to know TEntity, the same way
/// ITable/PagedRows keep the resource list views generic-free.</summary>
public sealed record TableWidgetData(ITable Table, IReadOnlyList<object> Rows);
