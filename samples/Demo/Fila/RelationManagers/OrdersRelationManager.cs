using Fila.Panels.RelationManagers;
using Fila.Tables;
using Demo.Data;

namespace Demo.Fila.RelationManagers;

/// <summary>Shows a Customer's Orders inline on their Edit page — scoped by the existing
/// Order.CustomerId FK, the same navigation OrderResource.Table() already joins through (see
/// its .Include(o => o.Customer)).</summary>
public sealed class OrdersRelationManager : RelationManager<Customer, Order>
{
    protected override Table<Order> Table(Table<Order> t) => t
        .Columns(
            t.Date(o => o.CreatedAt).Sortable(),
            t.Text(o => o.Reference).Searchable().Sortable(),
            t.Badge(o => o.Status),
            t.Money(o => o.Total).Sortable().Alignment(ColumnAlign.End))
        .DefaultSort(o => o.CreatedAt, descending: true)
        .PaginateBy(10);

    protected override IQueryable<Order> Scope(Customer parent, IQueryable<Order> q) =>
        q.Where(o => o.CustomerId == parent.Id);
}
