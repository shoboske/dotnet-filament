using Demo.Data;
using Fila.Tables;
using Fila.Widgets;
using Microsoft.EntityFrameworkCore;

namespace Demo.Fila.Widgets;

/// <summary>The five most recent orders, embedded on the dashboard.
///
/// Contributed by OrderResource.GetWidgets() rather than registered on the panel — the resource
/// that owns orders also owns the dashboard's summary of them, so adding OrderResource to a
/// panel brings this along with it.
///
/// The table is declared with the same DSL a resource's list page uses. There is no pagination
/// or sorting UI on a dashboard, so DefaultSort plus PaginateBy is the whole of "the five most
/// recent".</summary>
public sealed class RecentOrdersWidget : TableWidget<Order>
{
    public override string? Heading => "Recent orders";

    public override int Sort => 10;

    protected override Table<Order> Table(Table<Order> t) => t
        .Columns(
            t.Date(o => o.CreatedAt),
            t.Text(o => o.Reference),
            t.Badge(o => o.Status),
            t.Money(o => o.Total).Alignment(ColumnAlign.End),
            t.Text(o => o.Customer.Name).Label("Customer"))
        .DefaultSort(o => o.CreatedAt, descending: true)
        .PaginateBy(5);

    protected override IQueryable<Order> Query(IQueryable<Order> q) => q.Include(o => o.Customer);
}
