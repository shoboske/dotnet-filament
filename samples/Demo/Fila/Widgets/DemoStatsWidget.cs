using System.Globalization;
using Demo.Data;
using Fila.Widgets;
using Microsoft.EntityFrameworkCore;

namespace Demo.Fila.Widgets;

/// <summary>The dashboard's headline numbers, straight out of AppDb.
///
/// Takes AppDb through the constructor rather than casting WidgetContext.Db: a widget is
/// activated out of the request scope, so it can inject whatever it needs and stay strongly
/// typed. WidgetContext.Db exists for the other case — a framework widget like TableWidget,
/// which has to work against whatever DbContext the panel was configured with and cannot name
/// the app's type.</summary>
public sealed class DemoStatsWidget(AppDb db) : StatsOverviewWidget
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");

    private const int TrendDays = 14;

    protected override async Task<IReadOnlyList<Stat>> GetStatsAsync(WidgetContext context)
    {
        // Matches what the Customers list shows: CustomerResource soft-deletes, and a headline
        // count that keeps counting deleted rows would disagree with the table below it.
        var customers = await db.Customers.CountAsync(c => c.DeletedAt == null, context.Ct);
        var orders = await db.Orders.CountAsync(context.Ct);

        // Summed client-side on purpose. EF's SQLite provider has no native decimal type and
        // stores Order.Total as TEXT, so a server-side SUM() would be a string aggregate — it
        // warns about exactly this at model-build time. The demo's order table is small; an app
        // on a provider with real decimals should use SumAsync here.
        var totals = await db.Orders.Select(o => o.Total).ToListAsync(context.Ct);
        var revenue = totals.Sum();

        var shipped = await db.Orders.CountAsync(o => o.Status == OrderStatus.Shipped, context.Ct);

        // One point per day for the sparklines, over the same fortnight the revenue chart
        // covers. Pulled once and shaped twice rather than queried per stat.
        var since = DateTime.UtcNow.Date.AddDays(-(TrendDays - 1));
        var recent = await db.Orders
            .Where(o => o.CreatedAt >= since)
            .Select(o => new { o.CreatedAt, o.Total })
            .ToListAsync(context.Ct);

        var ordersPerDay = TrendSeries(since, recent.GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => (decimal)g.Count()));

        var revenuePerDay = TrendSeries(since, recent.GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Total)));

        return
        [
            Stat.Make("Customers", customers.ToString(DisplayCulture))
                .Description("Active accounts"),

            Stat.Make("Orders", orders.ToString(DisplayCulture))
                .Description($"{shipped} shipped")
                .DescriptionIcon("trending-up")
                .DescriptionColor("info")
                .Chart(ordersPerDay)
                .ChartColor("info"),

            Stat.Make("Revenue", revenue.ToString("C", DisplayCulture))
                .Description($"{revenuePerDay.Sum().ToString("C", DisplayCulture)} in {TrendDays} days")
                .DescriptionIcon("trending-up")
                .DescriptionColor("success")
                .Chart(revenuePerDay)
                .ChartColor("success"),
        ];
    }

    /// <summary>One value per day across the window, zero-filled — a sparkline that skipped the
    /// quiet days would compress them and overstate the trend.</summary>
    private static List<decimal> TrendSeries(DateTime since, Dictionary<DateTime, decimal> byDay) =>
        Enumerable.Range(0, TrendDays)
            .Select(offset => byDay.GetValueOrDefault(since.AddDays(offset)))
            .ToList();
}
