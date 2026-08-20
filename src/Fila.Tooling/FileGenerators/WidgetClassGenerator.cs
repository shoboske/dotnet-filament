namespace Fila.Tooling.FileGenerators;

/// <summary>Emits a StatsOverviewWidget subclass — backs `make:widget`. StatsOverviewWidget
/// rather than ChartWidget/TableWidget: it needs no target entity/column to be told about (a
/// non-interactive, flag-based generator has nowhere to ask), and it's the widget every Filament
/// admin panel reaches for first. A comment in the generated file points at the other two for
/// when a stats row isn't what's wanted.</summary>
public static class WidgetClassGenerator
{
    public sealed record GeneratedFile(string Source);

    public static GeneratedFile Generate(string widgetName, string rootNamespace)
    {
        var source = $$"""
            using Fila.Widgets;

            namespace {{rootNamespace}}.Fila.Widgets;

            // A row of stat cards — the most common dashboard widget. For a line chart or an
            // embedded table instead, subclass Fila.Widgets.ChartWidget or TableWidget: see
            // samples/Demo's RevenueChartWidget/RecentOrdersWidget.
            public sealed class {{widgetName}}Widget : StatsOverviewWidget
            {
                protected override Task<IReadOnlyList<Stat>> GetStatsAsync(WidgetContext context) =>
                    Task.FromResult<IReadOnlyList<Stat>>([
                        Stat.Make("Label", "Value"),
                    ]);
            }

            """;

        return new GeneratedFile(source);
    }
}
