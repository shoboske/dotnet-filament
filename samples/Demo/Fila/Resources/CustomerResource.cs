using Fila.Forms;
using Fila.Panels.Resources;
using Fila.Tables;
using Demo.Data;

namespace Demo.Fila.Resources;

public sealed class CustomerResource : Resource<Customer>
{
    public override string? NavigationIcon => "users";

    protected override Table<Customer> Table(Table<Customer> t) => t
        .Columns(
            t.Text(c => c.Email).Searchable().Sortable(),
            t.Text(c => c.Name).Searchable().Sortable())
        .PaginateBy(25);

    protected override Form<Customer> Form(Form<Customer> f) => f
        .Fields(
            f.Text(c => c.Name).Required(),
            f.Text(c => c.Email).Required());
}
