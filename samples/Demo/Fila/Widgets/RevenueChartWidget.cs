using Demo.Data;
using Fila.Widgets;
using Microsoft.EntityFrameworkCore;

namespace Demo.Fila.Widgets;

/// <summary>Revenue per day over the last two weeks — the phase's one chart, driven by a real
/// query rather than sample points.</summary>
public sealed class RevenueChartWidget(AppDb db) : ChartWidget
{
    private const int Days = 14;

    public override string? Heading => "Revenue";

    public override string? Description => $"Last {Days} days";

    protected override async Task<ChartData> GetDataAsync(WidgetContext context)
    {
        // Inclusive of today, so the series always ends on "now" rather than on the last day
        // that happened to have an order.
        var firstDay = DateTime.UtcNow.Date.AddDays(-(Days - 1));

        // The date filter runs in the database; the grouping and the sum do not. Order.Total is
        // decimal, which EF's SQLite provider stores as TEXT — GROUP BY with SUM() over that is
        // a string aggregate, not an arithmetic one. Bounded by the 14-day window either way.
        var recent = await db.Orders
            .Where(o => o.CreatedAt >= firstDay)
            .Select(o => new { o.CreatedAt, o.Total })
            .ToListAsync(context.Ct);

        var byDay = recent
            .GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Total));

        // Every day in the window gets a point, including the ones with no orders. Plotting only
        // the days that have data would silently compress the gaps and misstate the trend.
        var points = Enumerable.Range(0, Days)
            .Select(offset =>
            {
                var day = firstDay.AddDays(offset);
                return new ChartPoint(day.ToString("MMM d"), byDay.GetValueOrDefault(day));
            })
            .ToList();

        return new ChartData(points, ValuePrefix: "$");
    }
}
