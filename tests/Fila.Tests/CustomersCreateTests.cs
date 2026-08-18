using AngleSharp.Html.Parser;
using Fila.Testing;
using Xunit;

namespace Fila.Tests;

public sealed class CustomersCreateTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Create_ThenAppearsInASubsequentListRequest()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var createResponse = await client.PostAsync("/admin/customers/actions/create", new FormUrlEncodedContent(
        [
            new("Name", "Stark Industries"),
            new("Email", "tony@stark.test"),
        ]));
        createResponse.EnsureSuccessStatusCode();
        createResponse.AssertNotificationTriggered(title: "Created", color: "success");
        createResponse.AssertModalClosed();

        var html = await client.GetStringAsync("/admin/customers");
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var rowText = string.Join(' ', document.QuerySelectorAll("table.fi-ta-table tbody tr").Select(r => r.TextContent));

        Assert.Contains("Stark Industries", rowText);
    }
}
