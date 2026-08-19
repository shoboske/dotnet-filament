namespace Fila.Widgets;

/// <summary>A row of label/value/description/icon cards — Filament's
/// Widgets\StatsOverviewWidget. The archetypal dashboard widget: "3 customers, 42 orders,
/// $7,120.50 of revenue".
///
/// Subclass it and implement <see cref="GetStatsAsync"/>; the cards, their grid and their
/// colours are the framework's business. See samples/Demo's DemoStatsWidget.</summary>
public abstract class StatsOverviewWidget : Widget
{
    public sealed override string View => "stats-overview";

    /// <summary>The cards to draw, left to right. Query <paramref name="context"/>.Db here —
    /// this runs once per dashboard request.</summary>
    protected abstract Task<IReadOnlyList<Stat>> GetStatsAsync(WidgetContext context);

    public sealed override async Task<object> LoadAsync(WidgetContext context) =>
        await GetStatsAsync(context);
}
