using AngleSharp.Html.Parser;
using Fila.Testing;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for Table&lt;T&gt;.BulkActions(...) via OrderResource's
/// DeleteBulkAction — POST /{panel}/{resource}/bulk-actions/{name} with the checked row ids,
/// not the per-record action route.</summary>
public sealed class DeleteBulkActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Execute_DeletesEveryCheckedRecord()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: orders 4 and 5 are ORD-1004/ORD-1005.
        var response = await client.PostAsync("/admin/orders/bulk-actions/delete", new FormUrlEncodedContent(
        [
            new("ids", "4"),
            new("ids", "5"),
        ]));
        response.EnsureSuccessStatusCode();
        response.AssertNotificationTriggered(title: "Deleted", color: "danger");

        var html = await client.GetStringAsync("/admin/orders");
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var rowText = string.Join(' ', document.QuerySelectorAll("table.fi-ta-table tbody tr").Select(r => r.TextContent));

        Assert.DoesNotContain("ORD-1004", rowText);
        Assert.DoesNotContain("ORD-1005", rowText);
    }
}
