# Fila.Infolists

Fila's infolists: a read-only presentation of a single record, built from entries the same way a
form is built from fields. Ported from Filament's infolists package.

## Install

```bash
dotnet add package Fila.Infolists --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

## Declare an infolist

```csharp
protected override Infolist<Order> Infolist(Infolist<Order> i) => i
    .Entries(
        i.Date(o => o.CreatedAt),
        i.Text(o => o.Reference),
        i.Badge(o => o.Status),
        i.Money(o => o.Total),
        i.Text(o => o.Customer.Name).Label("Customer"));
```

Declaring an infolist on a resource is what gives its table a View action — the resource adds it
for you rather than the action having to be listed by hand.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
