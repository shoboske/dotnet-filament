using AngleSharp.Html.Parser;
using Fila.Testing;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for TrashedFilter on CustomerResource — the "Deleted records"
/// dropdown behind the table toolbar's funnel-icon trigger, the UI RestoreActionTests/
/// ForceDeleteActionTests used to have to route around by hitting delete/restore/force-delete
/// directly by id.</summary>
public sealed class CustomersTrashedFilterTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task DefaultList_ExcludesADeletedCustomer()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: customer 3 is Initech.
        await client.PostAsync("/admin/customers/3/actions/delete", null);

        var html = await client.GetStringAsync("/admin/customers");
        Assert.DoesNotContain("Initech", html);
    }

    [Fact]
    public async Task WithDeletedRecords_ShowsBothDeletedAndNotDeleted()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        await client.PostAsync("/admin/customers/3/actions/delete", null);

        var html = await client.GetStringAsync("/admin/customers?filter_trashed=1");
        Assert.Contains("Initech", html);
        Assert.Contains("Acme Corp", html);
    }

    [Fact]
    public async Task OnlyDeletedRecords_ShowsOnlyTheDeletedCustomer()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        await client.PostAsync("/admin/customers/3/actions/delete", null);

        var html = await client.GetStringAsync("/admin/customers?filter_trashed=0");
        Assert.Contains("Initech", html);
        Assert.DoesNotContain("Acme Corp", html);
    }

    [Fact]
    public async Task FilterTrigger_BadgesTheActiveFilterCount()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var defaultHtml = await client.GetStringAsync("/admin/customers");
        var defaultDocument = await new HtmlParser().ParseDocumentAsync(defaultHtml);
        Assert.Equal("0", defaultDocument.QuerySelector(".fi-icon-btn-badge-ctn .fi-badge")?.TextContent.Trim());

        var filteredHtml = await client.GetStringAsync("/admin/customers?filter_trashed=0");
        var filteredDocument = await new HtmlParser().ParseDocumentAsync(filteredHtml);
        Assert.Equal("1", filteredDocument.QuerySelector(".fi-icon-btn-badge-ctn .fi-badge")?.TextContent.Trim());
    }
}
