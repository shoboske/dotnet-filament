using AngleSharp.Html.Parser;
using Fila.Testing;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for DeleteAction — a built-in Fila.Actions.Action instance now
/// (RequiresConfirmation, no schema), not a bespoke HandleDeleteConfirmAsync/HandleDeleteAsync
/// pair. Exercises both halves of the generic action route: GET .../{id}/actions/delete to
/// mount the confirmation step, then POST the same path to actually delete.</summary>
public sealed class DeleteActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Mount_RendersAConfirmationStep()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/orders/2/actions/delete");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.NotNull(document.QuerySelector(".fi-modal-heading"));
        Assert.NotNull(document.QuerySelector("button.fi-btn-danger"));
    }

    [Fact]
    public async Task Execute_ThenTheRecordIsGoneFromASubsequentListRequest()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: order 2 (i=2) is reference ORD-1002.
        var deleteResponse = await client.PostAsync("/admin/orders/2/actions/delete", null);
        deleteResponse.EnsureSuccessStatusCode();
        deleteResponse.AssertNotificationTriggered(title: "Deleted", color: "danger");
        deleteResponse.AssertModalClosed();

        var html = await client.GetStringAsync("/admin/orders");
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var rowText = string.Join(' ', document.QuerySelectorAll("table.fi-ta-table tbody tr").Select(r => r.TextContent));

        Assert.DoesNotContain("ORD-1002", rowText);
    }
}
