using Fila.Testing;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for Table&lt;T&gt;.BulkActions(...) via CustomerResource's
/// RestoreBulkAction/ForceDeleteBulkAction — POST /{panel}/{resource}/bulk-actions/{name}, same
/// as DeleteBulkActionTests but for the soft-delete-specific bulk actions.</summary>
public sealed class SoftDeleteBulkActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task RestoreBulkAction_UndoesTheSoftDeleteForEveryCheckedRecord()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: customers 1 and 2 are Acme Corp/Globex.
        await client.PostAsync("/admin/customers/1/actions/delete", null);
        await client.PostAsync("/admin/customers/2/actions/delete", null);

        var restoreResponse = await client.PostAsync("/admin/customers/bulk-actions/restore", new FormUrlEncodedContent(
        [
            new("ids", "1"),
            new("ids", "2"),
        ]));
        restoreResponse.EnsureSuccessStatusCode();
        restoreResponse.AssertNotificationTriggered(title: "Restored", color: "success");

        var html = await client.GetStringAsync("/admin/customers");
        Assert.Contains("Acme Corp", html);
        Assert.Contains("Globex", html);
    }

    [Fact]
    public async Task ForceDeleteBulkAction_PermanentlyRemovesEveryCheckedRecord()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: customer 3 is Initech.
        await client.PostAsync("/admin/customers/3/actions/delete", null);

        var forceDeleteResponse = await client.PostAsync("/admin/customers/bulk-actions/force-delete", new FormUrlEncodedContent(
        [
            new("ids", "3"),
        ]));
        forceDeleteResponse.EnsureSuccessStatusCode();
        forceDeleteResponse.AssertNotificationTriggered(title: "Deleted permanently", color: "danger");

        var restoreAttempt = await client.GetAsync("/admin/customers/3/actions/restore");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, restoreAttempt.StatusCode);
    }
}
