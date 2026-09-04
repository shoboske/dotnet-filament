using AngleSharp.Html.Parser;
using Xunit;

namespace Fila.Tests;

/// <summary>OrderResource is the only place in samples/Demo exercising the built-in Badge and
/// Select views end to end — nothing else touched them before this phase's dispatch registry
/// existed, so this locks in that the registry resolves them to the same markup the old
/// switch/if chain produced.</summary>
public sealed class OrdersListTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task List_RendersTheStatusColumnAsABadge()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/orders");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: order 1 (i=1) is OrderStatus.Processing.
        var firstRow = document.QuerySelector("table.fi-ta-table tbody tr");
        var badge = firstRow?.QuerySelector(".fi-badge");

        Assert.NotNull(badge);
        Assert.Equal("fi-badge fi-badge-neutral", badge!.ClassName);
        Assert.Equal("Processing", badge.TextContent.Trim());
    }

    [Fact]
    public async Task List_ShowsANumberedPageList_WithTheOverviewText()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: 42 orders, 25 per page -- exactly 2 pages.
        var html = await client.GetStringAsync("/admin/orders");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("Showing 1 to 25 of 42 results", document.QuerySelector(".fi-pagination-overview")?.TextContent.Trim());

        var items = document.QuerySelectorAll(".fi-pagination-items .fi-pagination-item");
        Assert.Equal(3, items.Length); // page 1 (active), page 2, next chevron -- no previous on page 1.
        Assert.Contains("fi-active", items[0].ClassName);
        Assert.Equal("1", items[0].QuerySelector(".fi-pagination-item-label")?.TextContent.Trim());
        Assert.Equal("2", items[1].QuerySelector(".fi-pagination-item-label")?.TextContent.Trim());
        Assert.Equal("Next", items[2].QuerySelector("button")?.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task List_SecondPage_ShowsAPreviousChevronAndTheRightOverviewRange()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/orders?page=2");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("Showing 26 to 42 of 42 results", document.QuerySelector(".fi-pagination-overview")?.TextContent.Trim());

        var items = document.QuerySelectorAll(".fi-pagination-items .fi-pagination-item");
        Assert.Equal(3, items.Length); // previous chevron, page 1, page 2 (active) -- no next on the last page.
        Assert.Equal("Previous", items[0].QuerySelector("button")?.GetAttribute("aria-label"));
        Assert.Equal("2", items[2].QuerySelector(".fi-pagination-item-label")?.TextContent.Trim());
        Assert.Contains("fi-active", items[2].ClassName);
    }

    [Fact]
    public async Task List_OnASinglePage_ShowsOnlyTheOverviewText_NoNumberedList()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: 3 customers, well under one page.
        var html = await client.GetStringAsync("/admin/customers");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("Showing 1 to 3 of 3 results", document.QuerySelector(".fi-pagination-overview")?.TextContent.Trim());
        Assert.Null(document.QuerySelector(".fi-pagination-items"));
    }
}
