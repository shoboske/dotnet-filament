using Fila.Forms;
using Fila.Infolists;
using Fila.Panels.RelationManagers;
using Fila.Panels.Resources;
using Fila.Tables;
using Fila.Tables.Filters;
using Demo.Data;
using Demo.Fila.RelationManagers;

namespace Demo.Fila.Resources;

public sealed class CustomerResource : Resource<Customer>
{
    public override string? NavigationIcon => "users";

    /// <summary>Shows this customer's Orders inline on their Edit page — see
    /// OrdersRelationManager for the CustomerId scope.</summary>
    public override IReadOnlyList<RelationManagerRegistration> GetRelations() => [RelationManagerRegistration.Of<OrdersRelationManager>()];

    protected override Table<Customer> Table(Table<Customer> t) => t
        .Columns(
            t.Text(c => c.Email).Searchable().Sortable(),
            t.Text(c => c.Name).Searchable().Sortable())
        .PaginateBy(25)
        // Customer implements ISoftDeletable (see Demo/Data/Customer.cs) — Delete becomes a
        // soft delete, and Restore/ForceDelete become available alongside it. Both only render
        // for an already-deleted row, which the default list query excludes; TrashedFilter is
        // what reaches one through the table itself — matches FilamentReference's
        // CustomersTable::configure(), the only filter it registers.
        .Filters(new TrashedFilter<Customer>())
        .Actions(BuildEditAction(), BuildDeleteAction(), BuildRestoreAction(), BuildForceDeleteAction())
        .BulkActions(BuildRestoreBulkAction(), BuildForceDeleteBulkAction());

    protected override Form<Customer> Form(Form<Customer> f) => f
        .Fields(
            f.Text(c => c.Name).Required(),
            f.Text(c => c.Email).Required());

    // Declaring this is what gets the table's row actions a View action, prepended
    // automatically by Resource.BuildTable() — nothing above lists it explicitly.
    protected override Infolist<Customer> Infolist(Infolist<Customer> i) => i
        .Entries(
            i.Text(c => c.Name),
            i.Text(c => c.Email));
}
