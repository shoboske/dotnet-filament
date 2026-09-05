using Fila.Tables;
using Microsoft.EntityFrameworkCore;

namespace Fila.Widgets;

/// <summary>Embeds a Fila.Tables table on the dashboard — Filament's Widgets\TableWidget. The
/// "5 most recent orders" panel: the same column definitions a resource's list page uses, over
/// a shaped query paginated Filament's own way.
///
/// Declared exactly like a resource's table, because it is one:
/// <c>t.Columns(...).DefaultSort(o =&gt; o.CreatedAt, descending: true).PaginateBy(5)</c>. There
/// is no sortable-header or search UI on a dashboard table, but there is pagination —
/// TableWidget::makeTable() sets <c>PaginationMode::Simple</c>, which Laravel's own
/// simplePaginate() renders as a bare Previous/Next pair (no page numbers, no "Showing X of Y"
/// overview: both need a total-row COUNT query the simple paginator deliberately skips).
/// PaginateBy still reads as the page size; it is the *only* size, this widget never renders
/// pagination's page-size selector (Filament hides it once there is one option).</summary>
public abstract class TableWidget<TEntity> : Widget
    where TEntity : class
{
    public sealed override string View => "table";

    /// <summary>Define the columns and the page size, same DSL as Resource&lt;TEntity&gt;.Table.</summary>
    protected abstract Table<TEntity> Table(Table<TEntity> t);

    /// <summary>Override to add `.Include(...)`, a `.Where(...)` narrowing the widget to a
    /// slice of the table, and any other query shaping.</summary>
    protected virtual IQueryable<TEntity> Query(IQueryable<TEntity> q) => q;

    public sealed override async Task<object> LoadAsync(WidgetContext context)
    {
        var table = Table(new Table<TEntity>());
        var built = (ITable)table;
        var page = Math.Max(context.Page, 1);

        // ApplySort with an all-defaults query means "no request asked for a sort", which is
        // exactly right here — there is no sortable header for the visitor to have clicked —
        // so it falls through to the table's own DefaultSort. Laravel's simplePaginate() takes
        // one extra row past the page size and uses its presence as "hasMorePages", rather than
        // running a separate COUNT query just to render a Next button — ported literally here.
        var rows = await Query(context.Db.Set<TEntity>())
            .ApplySort(built, new TableQuery(null, null, "asc", 1))
            .Skip((page - 1) * built.PerPage)
            .Take(built.PerPage + 1)
            .ToListAsync(context.Ct);

        var hasNextPage = rows.Count > built.PerPage;
        if (hasNextPage) rows.RemoveAt(rows.Count - 1);

        return new TableWidgetData(built, rows.Cast<object>().ToList(), page, hasNextPage);
    }
}

/// <summary>What a table widget's partial draws: the column definitions, the rows to run them
/// over, and enough of Laravel's simple-paginator state (current page, whether another page
/// exists) to draw the Previous/Next pair — never a total, which simple pagination never
/// queries for. Non-generic so the partial never needs to know TEntity, the same way
/// ITable/PagedRows keep the resource list views generic-free.</summary>
public sealed record TableWidgetData(ITable Table, IReadOnlyList<object> Rows, int Page, bool HasNextPage)
{
    public bool HasPreviousPage => Page > 1;
}
