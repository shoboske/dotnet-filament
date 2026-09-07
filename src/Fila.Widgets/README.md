# Fila.Widgets

Fila's dashboard widgets: stats overviews with sparklines, charts, and tables. Ported from
Filament's widgets package.

A widget is resolved from DI, so it can take the app's own `DbContext` through its constructor
and stay strongly typed. Widgets are registered on a panel, or returned by a resource, which
puts them on whichever panel that resource is registered on.

## Install

```bash
dotnet add package Fila.Widgets --prerelease
```

Most apps take the umbrella [`Fila`](https://www.nuget.org/packages/Fila) package instead —
it pulls in every Fila package at once. Fila is at `0.0.1-alpha`, so `--prerelease` is required
until a stable version ships.

## A stats overview

```csharp
public sealed class DemoStatsWidget(AppDb db) : StatsOverviewWidget
{
    protected override async Task<IReadOnlyList<Stat>> GetStatsAsync(WidgetContext context) =>
    [
        Stat.Make("Customers", customers.ToString()).Description("Active accounts"),
        Stat.Make("Orders", orders.ToString())
            .Description($"{shipped} shipped")
            .DescriptionIcon("trending-up")
            .DescriptionColor("info")
            .Chart(ordersPerDay)
            .ChartColor("info"),
    ];
}
```

## A chart

```csharp
public sealed class RevenueChartWidget(AppDb db) : ChartWidget
{
    public override string? Heading => "Revenue";

    protected override async Task<ChartData> GetDataAsync(WidgetContext context) =>
        new ChartData(points, ValuePrefix: "$");
}
```

Charts render through Chart.js, vendored locally by `Fila.Panels` — no CDN, no build step.

## Registering

```csharp
panel.Widgets(
    WidgetRegistration.Of<DemoStatsWidget>(),
    WidgetRegistration.Of<RevenueChartWidget>());
```

---

Part of **[Fila](https://github.com/shoboske/dotnet-filament)**, an admin-panel framework for
ASP.NET Core in the shape of [Laravel Filament](https://filamentphp.com): Razor views instead of
Blade, htmx instead of Livewire, the same `fi-*` class names and design tokens.

Licensed under the GNU GPL v3 — see the `LICENSE` file in this package.
