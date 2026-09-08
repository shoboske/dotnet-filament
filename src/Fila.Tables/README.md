# Fila.Tables

Fila's table builder: columns, sorting, searching, pagination and filters over an `IQueryable<T>`.

Columns are declared against the entity with an expression, so the column knows its property
path and can sort and search in the database rather than in memory. Ported from Filament's
tables package.

## Install

```bash
dotnet add package Fila.Tables --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

## Declare a table

```csharp
protected override Table<Order> Table(Table<Order> t) => t
    .Columns(
        t.Date(o => o.CreatedAt).Sortable(),
        t.Text(o => o.Reference).Searchable().Sortable(),
        t.Badge(o => o.Status),
        t.Money(o => o.Total).Sortable().Alignment(ColumnAlign.End),
        t.Text(o => o.Customer.Name).Label("Customer"))
    .DefaultSort(o => o.CreatedAt, descending: true)
    .PaginateBy(25)
    .Actions(BuildEditAction(), BuildDeleteAction())
    .BulkActions(BuildDeleteBulkAction());
```

## Filters

```csharp
.Filters(new TrashedFilter<Customer>())
```

`TernaryFilter` and `TrashedFilter` ship with the package; `TableFilter` is the base to build
your own. `TrashedFilter` works against any entity implementing `ISoftDeletable`, and gives the
listing Filament's "with / without / only trashed" options.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
