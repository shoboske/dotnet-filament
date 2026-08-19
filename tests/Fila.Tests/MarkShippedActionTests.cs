using AngleSharp.Html.Parser;
using Fila.Testing;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for the custom "Mark shipped" row action on
/// samples/Demo/Fila/Resources/OrderResource.cs — the acceptance test that the general Action
/// mechanism actually works for a resource-authored action, not just Fila's own built-ins.
/// Zero changes to Fila.Actions/Fila.Panels were needed to add it.</summary>
public sealed class MarkShippedActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Mount_RendersAConfirmationStepNamedAfterTheAction()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: order 1 (i=1) is OrderStatus.Processing —
        // MarkShippedAction.Visible(...) only offers this action for Pending/Processing orders.
        var html = await client.GetStringAsync("/admin/orders/1/actions/mark-shipped");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("Mark shipped", document.QuerySelector(".fi-modal-heading")?.TextContent);
    }

    [Fact]
    public async Task Execute_SetsTheOrderStatusToShipped_AndFiresItsOwnNotification()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var response = await client.PostAsync("/admin/orders/1/actions/mark-shipped", null);
        response.EnsureSuccessStatusCode();
        response.AssertNotificationTriggered(title: "Marked as shipped", color: "success");
        response.AssertModalClosed();

        var html = await client.GetStringAsync("/admin/orders");
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var firstRow = document.QuerySelector("table.fi-ta-table tbody tr");

        Assert.Equal("Shipped", firstRow?.QuerySelector(".fi-badge")?.TextContent.Trim());
    }
}
