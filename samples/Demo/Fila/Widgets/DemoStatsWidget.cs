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

        return
        [
            Stat.Make("Customers", customers.ToString(DisplayCulture))
                .Description("Active accounts")
                .Icon("users"),

            Stat.Make("Orders", orders.ToString(DisplayCulture))
                .Description($"{shipped} shipped")
                .Icon("shopping-cart")
                .Color("info"),

            Stat.Make("Revenue", revenue.ToString("C", DisplayCulture))
                .Description("All time")
                .Icon("check-circle")
                .Color("success"),
        ];
    }
}
