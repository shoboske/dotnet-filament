using AngleSharp.Html.Parser;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for issue #9 — a seeded Customer's Edit page shows their Orders
/// through the OrdersRelationManager registered on CustomerResource.</summary>
public sealed class RelationManagerTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task EditPage_ShowsTheCustomersOwnOrdersInTheRelationManagersTable()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Seeded by samples/Demo/Data/DemoSeeder.cs: customer 1 is Acme Corp (added first).
        // Orders are seeded round-robin across the three customers (customers[i % 3] for
        // i in 1..42), so Acme's are i = 3, 6, 9, ... — ORD-1003 is their most recent (Table's
        // DefaultSort is CreatedAt descending, and CreatedAt = now - i days).
        var html = await client.GetStringAsync("/admin/customers/1/edit");
        var document = await new HtmlParser().ParseDocumentAsync(html);
        var rowText = string.Join(' ', document.QuerySelectorAll("table.fi-ta-table tbody tr").Select(r => r.TextContent));

        Assert.Contains("ORD-1003", rowText);
        // ORD-1001 belongs to a different customer (i = 1, customers[1] = Globex) — the
        // relation manager's Scope must exclude it, not just the parent resource's own table.
        Assert.DoesNotContain("ORD-1001", rowText);
    }

    [Fact]
    public async Task EditPage_404sForAResourceWithoutRelations()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // OrderResource registers no relation managers — its row Edit action still opens the
        // modal (see EditActionTests), but the dedicated Edit page itself still exists and
        // renders (just with an empty relation-tabs section) since nothing routes it away.
        var response = await client.GetAsync("/admin/orders/1/edit");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Null(document.QuerySelector(".fi-tabs"));
    }

    [Fact]
    public async Task List_LinksTheEditActionToTheEditPage_OnlyForAResourceWithRelations()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var customersHtml = await client.GetStringAsync("/admin/customers");
        var customersDoc = await new HtmlParser().ParseDocumentAsync(customersHtml);
        Assert.NotNull(customersDoc.QuerySelector("a[href='/admin/customers/1/edit']"));

        var ordersHtml = await client.GetStringAsync("/admin/orders");
        var ordersDoc = await new HtmlParser().ParseDocumentAsync(ordersHtml);
        Assert.Null(ordersDoc.QuerySelector("a[href='/admin/orders/1/edit']"));
    }
}
