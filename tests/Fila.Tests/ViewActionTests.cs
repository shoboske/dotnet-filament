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
    public async Task ViewAction_IsNotDeclaredExplicitlyOnCustomerResource_ButAppearsBecauseItHasAnInfolist()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/customers");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.NotNull(document.QuerySelector("[hx-get$='/1/actions/view']"));
    }
}
