using System.Globalization;
using System.Net;
using System.Text.Json;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Demo.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fila.Tests;

/// <summary>Covers the phase-6 dashboard: that a panel root is now a page rather than a
/// redirect, and that what it draws is real data rather than placeholder text.
///
/// The stats assertions read the expected numbers back out of the seeded database instead of
/// hardcoding them, so the test cannot pass by both the widget and the assertion agreeing on a
/// stale constant. DemoSeeder's own counts are asserted once, separately, to catch the case
/// where the database is empty and every "matches" comparison is trivially 0 == 0.</summary>
public sealed class DashboardTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public async Task PanelRoot_RendersTheDashboard_RatherThanRedirectingToTheFirstResource()
    {
        // AllowAutoRedirect off is the whole point: with it on, the pre-phase-6 redirect into
        // /admin/customers would also end in a 200 and this would pass against either behaviour.
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var login = await client.PostAsync("/admin/login", new FormUrlEncodedContent(
        [
            new("username", "admin"),
            new("password", "admin"),
        ]));
        Assert.Equal(HttpStatusCode.Found, login.StatusCode);

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var document = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
        Assert.Equal("Dashboard", document.QuerySelector(".fi-header-heading")?.TextContent.Trim());

        // The sidebar's Dashboard link is the only way back here from a resource list.
        var dashboardLink = document.QuerySelectorAll(".fi-sidebar-link")
            .OfType<IHtmlAnchorElement>()
            .Single(link => link.TextContent.Trim() == "Dashboard");
        Assert.Equal("/admin", dashboardLink.GetAttribute("href"));
        Assert.Contains("fi-sidebar-link-active", dashboardLink.ClassName);
    }

    [Fact]
    public async Task Dashboard_StatsMatchTheSeededRows()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var stats = await LoadStatsAsync(client, "/admin");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var customers = await db.Customers.CountAsync(c => c.DeletedAt == null);
        var orders = await db.Orders.CountAsync();
        var revenue = (await db.Orders.Select(o => o.Total).ToListAsync()).Sum();

        // DemoSeeder seeds 3 customers and 42 orders. Asserted directly so that an empty
        // database cannot make every comparison below pass by agreeing on zero.
        Assert.Equal(3, customers);
        Assert.Equal(42, orders);

        Assert.Equal(customers.ToString(DisplayCulture), stats["Customers"]);
        Assert.Equal(orders.ToString(DisplayCulture), stats["Orders"]);
        Assert.Equal(revenue.ToString("C", DisplayCulture), stats["Revenue"]);
    }

    [Fact]
    public async Task Dashboard_ShowsTheWidgetContributedByOrderResource()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        // Nothing registers this widget on AdminPanel — it arrives purely through
        // OrderResource.GetWidgets(), which is what this asserts.
        var card = document.QuerySelectorAll(".fi-wi-table")
            .Single(widget => widget.QuerySelector(".fi-section-header-heading")?.TextContent.Trim() == "Recent orders");

        var rows = card.QuerySelectorAll("tbody tr");
        Assert.Equal(5, rows.Length); // RecentOrdersWidget.PaginateBy(5)

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var newest = await db.Orders.OrderByDescending(o => o.CreatedAt).Select(o => o.Reference).Take(5).ToListAsync();

        var rendered = rows.Select(row => row.Children[1].TextContent.Trim()).ToList();
        Assert.Equal(newest, rendered);
    }

    [Fact]
    public async Task Dashboard_ChartPlotsOnePointPerDayOfItsWindow()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var frame = document.QuerySelector(".fi-wi-chart-frame[data-fila-chart]");
        Assert.NotNull(frame);
        Assert.NotNull(frame!.QuerySelector("canvas"));

        var payload = JsonDocument.Parse(frame.GetAttribute("data-fila-chart")!).RootElement;

        // RevenueChartWidget covers 14 days and emits a point for every one of them, including
        // the days with no orders — a chart that skipped those would misstate the trend.
        Assert.Equal(14, payload.GetProperty("labels").GetArrayLength());
        Assert.Equal(14, payload.GetProperty("values").GetArrayLength());

        // Chart.js is vendored locally (wwwroot/fila/vendor/chart.min.js), never fetched from a
        // CDN — the whole point of switching off the hand-rolled SVG chart.
        var scripts = document.QuerySelectorAll("script[src]")
            .OfType<IHtmlScriptElement>()
            .Select(script => script.GetAttribute("src") ?? "")
            .ToList();
        Assert.Contains(scripts, src => src.Contains("chart.min.js", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(scripts, src => src.Contains("unpkg.com", StringComparison.OrdinalIgnoreCase) ||
                                               src.Contains("cdn.jsdelivr.net", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BothPanels_RenderTheirOwnDashboard()
    {
        using var adminClient = factory.CreateClient();
        await TestAuth.LoginAsync(adminClient);

        using var staffClient = factory.CreateClient();
        await TestAuth.LoginAsync(staffClient, "staff", "staff", "staff");

        var adminStats = await LoadStatsAsync(adminClient, "/admin");
        var staffStats = await LoadStatsAsync(staffClient, "/staff");

        // Same widgets, same data — both panels register DemoStatsWidget and both discover
        // OrderResource, so the staff dashboard is not a degraded copy.
        Assert.Equal(adminStats, staffStats);

        // Each panel still renders under its own accent (AdminPanel: #f59e0b, StaffPanel:
        // #ef4444) — the dashboard goes through the same _PrimaryColorOverride the list does.
        Assert.Contains("#f59e0b", await adminClient.GetStringAsync("/admin"));
        Assert.Contains("#ef4444", await staffClient.GetStringAsync("/staff"));
    }

    private static async Task<Dictionary<string, string>> LoadStatsAsync(HttpClient client, string path)
    {
        var document = await new HtmlParser().ParseDocumentAsync(await client.GetStringAsync(path));

        return document.QuerySelectorAll(".fi-wi-stats-overview-stat").ToDictionary(
            card => card.QuerySelector(".fi-wi-stats-overview-stat-label")!.TextContent.Trim(),
            card => card.QuerySelector(".fi-wi-stats-overview-stat-value")!.TextContent.Trim());
    }
}
