using AngleSharp.Html.Parser;
using Xunit;

namespace Fila.Tests;

public sealed class OrdersCreateTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task CreateForm_RendersBothSelectFields()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/orders/actions/create");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        // f.Select(o => o.CustomerId).Options(...) -- a select populated from a DbContext query,
        // not an enum. Seeded by samples/Demo/Data/DemoSeeder.cs. Every Select gets a blank
        // "Select an option" placeholder first (matching Filament's own default), selected here
        // since a freshly-created Order has no customer chosen yet.
        var customerSelect = document.QuerySelector("select[name='CustomerId']");
        var customerOptions = customerSelect?.QuerySelectorAll("option").Select(o => o.TextContent).ToList();
        Assert.Equal(["Select an option", "Acme Corp", "Globex", "Initech"], customerOptions);
        Assert.Equal("", customerSelect?.QuerySelector("option[selected]")?.GetAttribute("value"));

        // f.Select(o => o.Status).Required() -- an enum-backed select, auto-populated from the
        // enum's members. A blank new Order's Status is the enum's CLR-default (zero) member,
        // not a real choice someone made -- FieldBinding.IsUnsetClrDefault blanks it so the
        // placeholder is what's selected, not "Pending" by accident.
        var statusSelect = document.QuerySelector("select[name='Status']");
        var statusOptions = statusSelect?.QuerySelectorAll("option").Select(o => o.TextContent).ToList();
        Assert.Equal(["Select an option", "Pending", "Processing", "Shipped", "Delivered", "Cancelled"], statusOptions);
        Assert.Equal("", statusSelect?.QuerySelector("option[selected]")?.GetAttribute("value"));
    }

    [Fact]
    public async Task CreateForm_RendersTheNumericTotalFieldBlank_NotTheClrDefaultZero()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/orders/actions/create");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var total = document.QuerySelector("input[name='Total']");
        Assert.Equal("", total?.GetAttribute("value") ?? "");
    }

    [Fact]
    public async Task CreateForm_LaysOutFieldsInATwoColumnGrid()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/orders/actions/create");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var grid = document.QuerySelector(".fi-modal-content > .fi-grid");
        Assert.NotNull(grid);
        Assert.Contains("--cols-lg: repeat(2, minmax(0, 1fr))", grid!.GetAttribute("style"));
        // Reference, Customer, Status, Total, Created At.
        Assert.Equal(5, grid.QuerySelectorAll(":scope > .fi-grid-col").Length);
    }
}
