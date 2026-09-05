using AngleSharp.Html.Parser;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for the auto-registered View action, backed by Fila.Infolists —
/// on both OrderResource (already had a manually-added View action pre-Infolists) and
/// CustomerResource (gets one purely because it now declares Infolist(), with no explicit
/// .Actions(...) entry for it).</summary>
public sealed class ViewActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Mount_RendersTheOrdersFieldValuesReadOnly()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: order 1 (i=1) is reference ORD-1001,
        // customer Globex, status Processing (statuses[1 % 5]).
        var html = await client.GetStringAsync("/admin/orders/1/actions/view");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        // No form controls at all — an infolist entry is plain text/a badge, not a disabled
        // <input>, and there is nothing to submit.
        Assert.Null(document.QuerySelector("input"));
        Assert.Null(document.QuerySelector("select"));
        Assert.Null(document.QuerySelector("button[type='submit']"));

        var text = document.QuerySelector(".fi-modal-content")?.TextContent ?? "";
        Assert.Contains("ORD-1001", text);
        Assert.Contains("Globex", text);
        Assert.Contains("Processing", text);
    }

    [Fact]
    public async Task Mount_RendersTheCustomersFieldValuesReadOnly()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: customer 1 is Acme Corp.
        var html = await client.GetStringAsync("/admin/customers/1/actions/view");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var text = document.QuerySelector(".fi-modal-content")?.TextContent ?? "";
        Assert.Contains("Acme Corp", text);
        Assert.Contains("buyer@acme.test", text);
    }

    [Fact]
    public async Task Mount_RendersOrdersEntriesInFilamentsRealFieldOrder_InAWideTwoColumnModal()
    {
        // CanOpenModal::getModalWidth() falls through to Width::FourExtraLarge for any action
        // that isn't a confirmation -- View included -- the same wide modal Create/Edit forms
        // get. Confirmed against a live FilamentReference View-order modal: a real
        // fi-grid lg:fi-grid-cols (not one stacked column) holding, in this exact order,
        // created_at, reference, status, total, customer.name -- OrderInfolist::configure()'s
        // own field order, not OrderResource.cs's previous Reference-first order.
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/orders/1/actions/view");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Contains("classList.add('fi-modal-window-wide')", html);
        Assert.DoesNotContain("classList.remove('fi-modal-window-wide')", html);

        var labels = document.QuerySelectorAll(".fi-in-entry-label").Select(el => el.TextContent.Trim()).ToList();
        Assert.Equal(["Created at", "Reference", "Status", "Total", "Customer"], labels);

        Assert.Equal(labels.Count, document.QuerySelectorAll(".fi-grid-col").Count);

        // Entries/_Badge.cshtml had the same gray-default bug Columns/_Badge.cshtml had before
        // #38 -- an infolist badge with no explicit color mapping is still "primary" in real
        // Filament, not gray.
        Assert.Equal("fi-badge fi-badge-primary", document.QuerySelector(".fi-badge")!.ClassName);
    }

    [Fact]
    public async Task ViewAction_IsNotDeclaredExplicitlyOnCustomerResource_ButAppearsBecauseItHasAnInfolist()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/customers");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.NotNull(document.QuerySelector("[hx-get$='/1/actions/view']"));
    }
}
