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

        var html = await client.GetStringAsync("/admin/orders/create");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        // f.Select(o => o.CustomerId).Options(...) -- a select populated from a DbContext query,
        // not an enum. Seeded by samples/Demo/Data/DemoSeeder.cs.
        var customerOptions = document.QuerySelector("select[name='CustomerId']")?
            .QuerySelectorAll("option").Select(o => o.TextContent).ToList();
        Assert.Equal(["Acme Corp", "Globex", "Initech"], customerOptions);

        // f.Select(o => o.Status).Required() -- an enum-backed select, auto-populated from the
        // enum's members.
        var statusOptions = document.QuerySelector("select[name='Status']")?
            .QuerySelectorAll("option").Select(o => o.TextContent).ToList();
        Assert.Equal(["Pending", "Processing", "Shipped", "Delivered", "Cancelled"], statusOptions);
    }
}
