using AngleSharp.Html.Parser;
using Fila.Testing;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for EditAction — a built-in Fila.Actions.Action instance now,
/// not a bespoke HandleFormAsync/HandleSaveAsync pair. Exercises both halves of the generic
/// action route: GET .../{id}/actions/edit to mount the pre-filled form, then POST the same
/// path to save it.</summary>
public sealed class EditActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Mount_RendersTheFormPrefilledWithTheRecordsCurrentValues()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: order 1 (i=1) is reference ORD-1001.
        var html = await client.GetStringAsync("/admin/orders/1/actions/edit");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("ORD-1001", document.QuerySelector("input[name='Reference']")?.GetAttribute("value"));
    }

    [Fact]
    public async Task Execute_ThenTheChangeAppearsInASubsequentListRequest()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // A different order than the Mount test above uses — tests in this class share one
        // seeded database (DemoAppFactory), so each test needs its own row to stay independent
        // of xUnit's test execution order.
        var editResponse = await client.PostAsync("/admin/orders/6/actions/edit", new FormUrlEncodedContent(
        [
            new("Reference", "ORD-EDITED"),
            new("CustomerId", "1"),
            new("Status", "Processing"),
            new("Total", "42.00"),
            // Table.DefaultSort is CreatedAt descending — keep this comfortably in the future
            // so the edited row still lands on page 1 of the unpaginated list request below,
            // regardless of when the seeded rows' "now - i days" dates fall relative to it.
            new("CreatedAt", "2030-01-01"),
        ]));
        editResponse.EnsureSuccessStatusCode();
        editResponse.AssertNotificationTriggered(title: "Saved", color: "success");
        editResponse.AssertModalClosed();

        var html = await client.GetStringAsync("/admin/orders");
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var rowText = string.Join(' ', document.QuerySelectorAll("table.fi-ta-table tbody tr").Select(r => r.TextContent));

        Assert.Contains("ORD-EDITED", rowText);
    }
}
