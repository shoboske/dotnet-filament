# Fila.Schemas

Fila's schema primitives — `Component` and `IComponent`, the component tree that forms,
infolists and layouts are built from. Ported from Filament's schemas package.

This is a foundation package. You install it to *build* a Fila component, not to use one: a
form field, an infolist entry and a table column are all components, and this is what they have
in common — a name, a resolvable label, and a resolvable visibility.

## Install

```bash
dotnet add package Fila.Schemas --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

```csharp
public abstract class Component : IComponent
{
    public string Name { get; }
    public string ResolveLabel(EvaluationContext context);
    public bool ResolveVisible(EvaluationContext context);
}
```

Labels and visibility are `Evaluated<T>` (from
[`Fila.Support`](https://www.nuget.org/packages/Fila.Support)): either a fixed value or a
closure resolved against the current record and request.

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
