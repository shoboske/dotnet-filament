namespace Fila.Widgets;

/// <summary>One plotted value and the label under it.</summary>
/// <param name="Label">Drawn on the x axis. Keep it short — a dashboard chart has room for a
/// handful of characters per point.</param>
/// <param name="Value">Plotted on the y axis. decimal rather than double because the thing
/// being charted is usually money.</param>
public sealed record ChartPoint(string Label, decimal Value);

/// <summary>A chart's plotted series plus how to write its numbers out.</summary>
/// <param name="Points">Left to right. An empty list renders the chart's empty state.</param>
/// <param name="ValuePrefix">Prepended to axis/tooltip numbers — "$" for money.</param>
/// <param name="ValueSuffix">Appended to axis/tooltip numbers — "%" , " orders".</param>
public sealed record ChartData(
    IReadOnlyList<ChartPoint> Points,
    string? ValuePrefix = null,
    string? ValueSuffix = null);

/// <summary>A line chart over a series a subclass computes — Filament's Widgets\ChartWidget.
///
/// Drawn as inline SVG by the panel's chart partial, with no charting library and no CDN
/// script: Fila has vendored or hand-rolled its whole frontend so far (fila.css, IconRegistry's
/// inlined icons), and a dashboard that silently loses its charts when a CDN is blocked would
/// break that. The trade is that this plots one series as a line and nothing more — bar,
/// stacked and multi-series charts are backlog, per the issue's non-goals.</summary>
public abstract class ChartWidget : Widget
{
    public sealed override string View => "chart";

    /// <summary>Half the dashboard grid, so two charts sit side by side — the one place a
    /// widget usually does not want the full width.</summary>
    public override int ColumnSpan => 6;

    /// <summary>Compute the series. Runs once per dashboard request.</summary>
    protected abstract Task<ChartData> GetDataAsync(WidgetContext context);

    public sealed override async Task<object> LoadAsync(WidgetContext context) =>
        await GetDataAsync(context);
}
