# Fila.Panels

The panel package: the Razor UI, routing, resources, pages, relation managers and dashboard that
together turn an ASP.NET Core app into an admin panel.

A `Resource<T>` over an EF Core entity is the unit of work here — declare its table, form and
infolist and the package routes and renders the listing, create, edit and view screens for it.
An app can host several panels at different paths, each with its own brand, accent color and
login.

## Install

```bash
dotnet add package Fila.Panels --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

## Configure a panel

```csharp
builder.Services.AddFilaPanel(panel => panel.AtPath("admin")
    .Brand("Demo Admin")
    .PrimaryColor("#f59e0b")
    .UseDbContext<AppDb>()
    .Widgets(WidgetRegistration.Of<DemoStatsWidget>())
    .WithLogin((username, password, ct) => /* return a ClaimsPrincipal, or null */)
    .DiscoverResources(typeof(Program).Assembly));

var app = builder.Build();
app.MapFilaPanel();
```

## Relation managers

A relation manager hosts a related table on a record's Edit page:

```csharp
public sealed class OrdersRelationManager : RelationManager<Customer, Order>
{
    protected override Table<Order> Table(Table<Order> t) => t
        .Columns(t.Text(o => o.Reference).Searchable(), t.Money(o => o.Total))
        .PaginateBy(10);

    protected override IQueryable<Order> Scope(Customer parent, IQueryable<Order> q) =>
        q.Where(o => o.CustomerId == parent.Id);
}
```

Declaring one changes what the resource's Edit action does: the record gets a dedicated
`/customers/{id}/edit` page to host the manager, rather than opening a modal.

## Static assets

The package ships its CSS and JS as static web assets under `_content/Fila/fila/`, so the host
app needs `app.UseStaticFiles()` for them to resolve.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
