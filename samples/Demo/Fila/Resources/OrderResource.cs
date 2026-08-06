using Fila.Resources;
using Fila.Tables;
using Microsoft.EntityFrameworkCore;
using Demo.Data;

namespace Demo.Fila.Resources;

public sealed class OrderResource : Resource<Order>
{
    protected override Table<Order> Table(Table<Order> t) => t
        .Columns(
            t.Date(o => o.CreatedAt).Sortable(),
            t.Text(o => o.Reference).Searchable().Sortable(),
            t.Badge(o => o.Status),
            t.Money(o => o.Total).Sortable().Alignment(ColumnAlign.End),
            t.Text(o => o.Customer.Name).Label("Customer"))
        .DefaultSort(o => o.CreatedAt, descending: true)
        .PaginateBy(25);

    protected override IQueryable<Order> Query(IQueryable<Order> q) => q.Include(o => o.Customer);
}
