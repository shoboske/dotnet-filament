using AngleSharp.Html.Parser;
using Xunit;

namespace Fila.Tests;

public sealed class CustomersListTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task List_RendersTheSeededCustomers()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/customers");
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var rowText = string.Join(' ', document.QuerySelectorAll("table.fi-ta-table tbody tr").Select(r => r.TextContent));

        // Seeded by samples/Demo/Data/DemoSeeder.cs.
        Assert.Contains("Acme Corp", rowText);
        Assert.Contains("Globex", rowText);
        Assert.Contains("Initech", rowText);
    }
}
