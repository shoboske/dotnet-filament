# Fila.Forms

Fila's form builder: the fields a resource's create and edit screens are made of, bound to an
EF Core entity's properties by expression.

Ships `TextInput`, `Textarea`, `Select`, `Checkbox` and `DatePicker`. Ported from Filament's
forms package.

## Install

```bash
dotnet add package Fila.Forms --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

## Declare a form

```csharp
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
```

A `Select` over an enum infers its options from the enum's members; the `Options(db => ...)`
overload is for choices that come from a query, and is handed the panel's `DbContext` for the
current request.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
