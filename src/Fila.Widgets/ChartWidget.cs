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

/// <summary>A chart over a series a subclass computes — Filament's Widgets\ChartWidget.
/// Keeps that class's shape: a heading and description on the widget, the series from
/// GetData, and the chart kind behind <see cref="Type"/> (Filament's abstract getType(), which
/// its LineChartWidget/BarChartWidget subclasses exist only to override).
///
/// Painted onto a &lt;canvas&gt; by Chart.js, same as Filament — vendored locally rather than
/// pulled from a CDN, the same way Fila's own frontend (fila.css, IconRegistry's inlined icons)
/// has no CDN dependency either. See wwwroot/fila/vendor and fila.charts.js. The trade is that
/// the built-in partial draws "line" and nothing else — the other types Filament ships (bar,
/// pie, doughnut, radar, ...) are backlog, per the issue's non-goals. Type is still the seam
/// they arrive through: a consumer can register their own partial for a view today and a future
/// built-in can branch on it.</summary>
public abstract class ChartWidget : Widget
{
    public sealed override string View => "chart";

    /// <summary>Which chart to draw — Filament's getType(). Only "line" is implemented by the
    /// built-in partial.</summary>
    public virtual string Type => "line";

    /// <summary>Compute the series. Runs once per dashboard request.</summary>
    protected abstract Task<ChartData> GetDataAsync(WidgetContext context);

    public sealed override async Task<object> LoadAsync(WidgetContext context) =>
        await GetDataAsync(context);
}
