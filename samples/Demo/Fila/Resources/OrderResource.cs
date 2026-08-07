using Fila.Forms;
using Fila.Resources;
using Fila.Tables;
using Microsoft.EntityFrameworkCore;
using Demo.Data;

namespace Demo.Fila.Resources;

public sealed class OrderResource : Resource<Order>
{
    public override string? NavigationIcon => "shopping-cart";

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

    protected override Form<Order> Form(Form<Order> f) => f
        .Fields(
            f.Text(o => o.Reference).Required(),
            f.Select(o => o.CustomerId)
                .Label("Customer")
                .Required()
                .Options(db => ((AppDb)db).Customers
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectOption(c.Id.ToString(), c.Name))),
            f.Select(o => o.Status).Required(),
            f.Number(o => o.Total).Required(),
            f.Date(o => o.CreatedAt).Required());
}
