# Fila

An admin-panel framework for ASP.NET Core, in the shape of [Laravel
Filament](https://filamentphp.com). Declare a resource class over an EF Core entity and get a
listing table with sorting, searching, filters and pagination, create/edit forms, view
infolists, row and bulk actions, relation managers, and a dashboard of widgets.

This is the umbrella package. It carries no code of its own — it references every Fila package,
so one dependency gets the whole framework.

## Install

```bash
dotnet add package Fila --prerelease
```

Fila is at `0.0.1-alpha`, so `--prerelease` is required until a stable version ships.

## Wire up a panel

```csharp
// Program.cs
builder.Services.AddFilaPanel(AdminPanel.Configure);

var app = builder.Build();
if (await app.RunFilaCommandsAsync(args)) return;   // serves `dotnet fila make:*`
app.MapFilaPanel();
```

```csharp
public static class AdminPanel
{
    public static void Configure(PanelBuilder panel) => panel.AtPath("admin")
        .Brand("Demo Admin")
        .PrimaryColor("#f59e0b")
        .UseDbContext<AppDb>()
        .Widgets(WidgetRegistration.Of<DemoStatsWidget>())
        .WithLogin((username, password, _) => /* return a ClaimsPrincipal, or null */)
        .DiscoverResources(typeof(Program).Assembly);
}
```

## Declare a resource

```csharp
public sealed class OrderResource : Resource<Order>
{
    public override string? NavigationIcon => "shopping-cart";

    protected override Table<Order> Table(Table<Order> t) => t
        .Columns(
            t.Date(o => o.CreatedAt).Sortable(),
            t.Text(o => o.Reference).Searchable().Sortable(),
            t.Badge(o => o.Status),
            t.Money(o => o.Total).Sortable().Alignment(ColumnAlign.End))
        .DefaultSort(o => o.CreatedAt, descending: true)
        .PaginateBy(25)
        .Actions(BuildEditAction(), BuildDeleteAction())
        .BulkActions(BuildDeleteBulkAction());

    protected override Form<Order> Form(Form<Order> f) => f
        .Fields(
            f.Text(o => o.Reference).Required(),
            f.Select(o => o.Status).Required(),
            f.Number(o => o.Total).Required());
}
```

## Scaffolding

Install the [`Fila.Tools`](https://www.nuget.org/packages/Fila.Tools) CLI to generate resources,
pages, widgets, actions and relation managers:

```bash
dotnet fila make:resource Order
```

## What's in the box

| Package | |
| --- | --- |
| [`Fila.Panels`](https://www.nuget.org/packages/Fila.Panels) | Panels, resources, pages, relation managers, the Razor UI |
| [`Fila.Tables`](https://www.nuget.org/packages/Fila.Tables) | Columns, sorting, searching, pagination, filters |
| [`Fila.Forms`](https://www.nuget.org/packages/Fila.Forms) | Create/edit form fields |
| [`Fila.Infolists`](https://www.nuget.org/packages/Fila.Infolists) | Read-only record views |
| [`Fila.Actions`](https://www.nuget.org/packages/Fila.Actions) | Record actions, bulk actions, action groups |
| [`Fila.Notifications`](https://www.nuget.org/packages/Fila.Notifications) | Flash notifications over htmx |
| [`Fila.Widgets`](https://www.nuget.org/packages/Fila.Widgets) | Stats overviews, charts, table widgets |
| [`Fila.Schemas`](https://www.nuget.org/packages/Fila.Schemas) | The component tree the above are built from |
| [`Fila.Support`](https://www.nuget.org/packages/Fila.Support) | Shared primitives |
| [`Fila.Tooling`](https://www.nuget.org/packages/Fila.Tooling) | The in-app `make:*` scaffolding |
| [`Fila.Testing`](https://www.nuget.org/packages/Fila.Testing) | Assertions for testing a panel |
| [`Fila.Tools`](https://www.nuget.org/packages/Fila.Tools) | The `dotnet fila` CLI |

See the [repository](https://github.com/shoboske/dotnet-filament) for a screen-by-screen
comparison against the Filament app this is ported from, and `samples/Demo` for a complete
two-resource panel.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
