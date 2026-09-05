using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Xunit;

namespace Fila.Tests;

/// <summary>Covers the record-click resolution ported from Filament's ListRecords::table():
/// walk ['view', 'edit'], take the first action that exists and is visible for the row, and
/// make the row a link when that action carries a URL or a modal trigger when it doesn't.
///
/// The two Demo resources exercise both branches on purpose. OrderResource has no relation
/// manager, so its Edit stays a modal and "view" wins outright. CustomerResource *does* have
/// one, which points its Edit at the dedicated /edit page — a URL — and upstream prefers a
/// resolved recordUrl over a recordAction, so its rows link there instead of opening the View
/// modal they'd otherwise get.</summary>
public sealed class ClickableRowTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task OrdersRows_AreButtonsThatMountTheViewAction()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var document = await new HtmlParser().ParseDocumentAsync(await client.GetStringAsync("/admin/orders"));

        var row = document.QuerySelector("table.fi-ta-table tbody tr.fi-ta-row");
        Assert.NotNull(row);
        Assert.Contains("fi-clickable", row!.ClassName);

        // Every data cell wraps its content in the clickable .fi-ta-col; the selection
        // checkbox and the row-action cell are left unwrapped, exactly as upstream leaves them.
        var wrappers = row.QuerySelectorAll("td > .fi-ta-col");
        Assert.Equal(5, wrappers.Length); // OrderResource declares 5 columns.
        Assert.All(wrappers, w => Assert.Equal("BUTTON", w.TagName));

        // Seeded by samples/Demo/Data/DemoSeeder.cs: the newest order is id 1.
        var button = wrappers[0];
        Assert.Equal("/admin/orders/1/actions/view", button.GetAttribute("hx-get"));
        Assert.Equal("#fila-modal-body", button.GetAttribute("hx-target"));

        Assert.Null(row.QuerySelector("td.fi-ta-checkbox-cell > .fi-ta-col"));
        Assert.Null(row.QuerySelector("td:has(.fi-ta-actions) > .fi-ta-col"));
    }

    [Fact]
    public async Task CustomersRows_AreLinksToTheEditPage_BecauseThatActionCarriesAUrl()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var document = await new HtmlParser().ParseDocumentAsync(await client.GetStringAsync("/admin/customers"));

        var row = document.QuerySelector("table.fi-ta-table tbody tr.fi-ta-row");
        Assert.NotNull(row);
        Assert.Contains("fi-clickable", row!.ClassName);

        var wrappers = row.QuerySelectorAll("td > .fi-ta-col").OfType<IHtmlAnchorElement>().ToList();
        Assert.Equal(2, wrappers.Count); // CustomerResource declares 2 columns.
        Assert.All(wrappers, a => Assert.Equal("/admin/customers/1/edit", a.GetAttribute("href")));
    }

    [Fact]
    public async Task RelationManagerRows_StayUnclickable()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // RelationManager doesn't set recordAction/recordUrl at all upstream — only
        // ListRecords::table() does — so a nested table's rows are inert on both sides.
        var html = await client.GetStringAsync("/admin/customers/1/edit");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var nested = document.QuerySelector("#fila-relation-table-orders");
        Assert.NotNull(nested);
        Assert.Empty(nested!.QuerySelectorAll(".fi-ta-col"));
        Assert.Empty(nested.QuerySelectorAll("tr.fi-clickable"));
    }
}
