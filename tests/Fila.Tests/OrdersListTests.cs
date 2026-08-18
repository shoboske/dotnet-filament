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
}
