using Fila.Testing;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for ForceDeleteAction on CustomerResource — permanently removes
/// an already soft-deleted record. See RestoreActionTests' doc comment for why this hits routes
/// directly by id rather than through a "show trashed" list (#20).</summary>
public sealed class ForceDeleteActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Execute_PermanentlyRemovesAnAlreadySoftDeletedRecord()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: customer 3 is Initech.
        await client.PostAsync("/admin/customers/3/actions/delete", null);

        var forceDeleteResponse = await client.PostAsync("/admin/customers/3/actions/force-delete", null);
        forceDeleteResponse.EnsureSuccessStatusCode();
        forceDeleteResponse.AssertNotificationTriggered(title: "Deleted permanently", color: "danger");

        // Gone for good — FindAsync can no longer resolve it at all, so even mounting Restore
        // against the same id 404s now.
        var restoreAttempt = await client.GetAsync("/admin/customers/3/actions/restore");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, restoreAttempt.StatusCode);
    }
}
