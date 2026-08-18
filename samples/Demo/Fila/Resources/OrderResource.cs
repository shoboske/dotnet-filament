using Fila.Forms;
using Fila.Panels.Actions;
using Fila.Panels.Resources;
using Fila.Tables;
using Microsoft.EntityFrameworkCore;
using Demo.Data;

namespace Demo.Fila.Resources;

public sealed class OrderResource : Resource<Order>
{
    public override string? NavigationIcon => "shopping-cart";

    /// <summary>A custom row action added entirely from this file — no changes to
    /// Fila.Actions/Fila.Panels needed. Confirmation-only, no schema: it just flips the
    /// order's status and saves, the same shape DeleteAction uses. Hides itself once an order
    /// is already shipped or later in its lifecycle, so it isn't offered where it wouldn't
    /// make sense.</summary>
    private static readonly global::Fila.Actions.Action MarkShippedAction = new global::Fila.Actions.Action("mark-shipped")
        .Label("Mark shipped")
        .Icon("check-circle")
        .Color("success")
        .RequiresConfirmation()
        .ModalDescription("This marks the order as shipped.")
        .Visible(ctx => ((Order)ctx.Record!).Status is OrderStatus.Pending or OrderStatus.Processing)
        .Handle(async ctx =>
        {
            ((Order)ctx.Record!).Status = OrderStatus.Shipped;
            await ctx.Db.SaveChangesAsync(ctx.Ct);
        })
        .Notifies("Marked as shipped", "success");

    protected override Table<Order> Table(Table<Order> t) => t
        .Columns(
            t.Date(o => o.CreatedAt).Sortable(),
            t.Text(o => o.Reference).Searchable().Sortable(),
            t.Badge(o => o.Status),
            t.Money(o => o.Total).Sortable().Alignment(ColumnAlign.End),
            t.Text(o => o.Customer.Name).Label("Customer"))
        .DefaultSort(o => o.CreatedAt, descending: true)
        .PaginateBy(25)
        .Actions(
            ViewAction.Make(this),
            EditAction.Make(this),
            MarkShippedAction,
            ReplicateAction.Make(this),
            DeleteAction.Make(this))
        .BulkActions(DeleteBulkAction.Make(this));

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
