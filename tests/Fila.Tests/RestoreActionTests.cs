using AngleSharp.Html.Parser;
using Fila.Testing;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for RestoreAction on CustomerResource, which implements
/// Fila.Support.ISoftDeletable (see samples/Demo/Data/Customer.cs) — Delete becomes a soft
/// delete, and Restore undoes it. There's no UI yet for browsing already-deleted rows (#20), so
/// this hits the delete/restore routes directly by id, the same way the acceptance criteria for
/// this phase call for exercising a custom action end to end.</summary>
public sealed class RestoreActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Mount_RendersAConfirmationStepForAnAlreadyDeletedRecord()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: customer 2 is Globex.
        await client.PostAsync("/admin/customers/2/actions/delete", null);

        var html = await client.GetStringAsync("/admin/customers/2/actions/restore");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("Restore Customer", document.QuerySelector(".fi-modal-heading")?.TextContent);
    }

    [Fact]
    public async Task Execute_UndoesTheSoftDelete_AndTheRecordReappearsInTheList()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: customer 1 is Acme Corp.
        var deleteResponse = await client.PostAsync("/admin/customers/1/actions/delete", null);
        deleteResponse.AssertNotificationTriggered(title: "Deleted", color: "danger");

        var listAfterDelete = await client.GetStringAsync("/admin/customers");
        Assert.DoesNotContain("Acme Corp", listAfterDelete);

        var restoreResponse = await client.PostAsync("/admin/customers/1/actions/restore", null);
        restoreResponse.EnsureSuccessStatusCode();
        restoreResponse.AssertNotificationTriggered(title: "Restored", color: "success");

        var listAfterRestore = await client.GetStringAsync("/admin/customers");
        Assert.Contains("Acme Corp", listAfterRestore);
    }
}
