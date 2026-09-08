# Fila.Support

Fila's shared primitives, used by every other Fila package.

- `Evaluated<T>` and `EvaluationContext` — the "a value, or a closure over the current record"
  pattern that lets `.Label(...)`, `.Visible(...)` and friends take either.
- `ExpressionPath` — turns `o => o.Customer.Name` into the property path a column sorts and
  searches by.
- `IconRegistry` — the icon set Fila renders, by name.
- `ISoftDeletable` — implement it and a resource gains trashed filters, restore and force-delete.
- `ComponentText` — the humanizing that turns a property name into a default label.

This is a foundation package; it comes in transitively with any other Fila package.

## Install

```bash
dotnet add package Fila.Support --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

It takes a dependency on `Microsoft.EntityFrameworkCore` because `EvaluationContext` hands a
component's closures the current request's `DbContext` — as Filament's support package sits on
Eloquent.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
