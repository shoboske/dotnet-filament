using AngleSharp.Html.Parser;
using Fila.Testing;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for ReplicateAction on OrderResource — confirm, then a copy of
/// the record exists with a fresh id (Resource.ReplicateAsync).</summary>
public sealed class ReplicateActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Execute_InsertsACopyWithTheSameReference()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var response = await client.PostAsync("/admin/orders/3/actions/replicate", null);
        response.EnsureSuccessStatusCode();
        response.AssertNotificationTriggered(title: "Replicated", color: "success");

        var html = await client.GetStringAsync("/admin/orders");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: order 3 (i=3) is reference ORD-1003, the
        // 3rd most recent row by the table's default CreatedAt-descending sort — comfortably
        // inside page 1's first 25 rows both before and after replication. The replica shares
        // its reference (and CreatedAt), so ORD-1003 now shows up twice.
        var occurrences = document.QuerySelectorAll("table.fi-ta-table tbody tr")
            .Count(r => r.TextContent.Contains("ORD-1003"));

        Assert.Equal(2, occurrences);
    }
}
