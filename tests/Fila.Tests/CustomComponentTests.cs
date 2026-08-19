using System.Linq.Expressions;
using AngleSharp.Html.Parser;
using Demo.Data;
using Fila.Forms;
using Fila.Panels;
using Fila.Panels.Resources;
using Fila.Tables;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fila.Tests;

/// <summary>A column type declared outside Fila — the thing the open component model exists to
/// make possible. It picks up the badge markup by naming that view, and formats its own value,
/// neither of which needs an edit to Fila.Tables or to Fila's views.</summary>
public sealed class InitialsColumn<TEntity>(Expression<Func<TEntity, object?>> selector)
    : TableColumn<TEntity, InitialsColumn<TEntity>>(selector)
{
    public override string View => "badge";

    public override string FormatState(object? value) => value is string text
        ? string.Concat(text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0])))
        : string.Empty;
}

/// <summary>The same idea on the forms side: a field type Fila has never heard of, rendering as
/// the input type it asks for.</summary>
public sealed class EmailInput<TEntity>(Expression<Func<TEntity, object?>> selector)
    : FormField<TEntity, EmailInput<TEntity>>(selector)
{
    public override string InputType => "email";
}

/// <summary>Lives in the test assembly, not in samples/Demo — so it is genuinely a consumer of
/// Fila rather than part of it.</summary>
public sealed class CustomComponentResource : Resource<Customer>
{
    public override string Slug => "custom-customers";

    protected override Table<Customer> Table(Table<Customer> t) => t
        .Columns(
            t.Text(c => c.Name).Sortable(),
            new InitialsColumn<Customer>(c => c.Name).Label("Initials"))
        .DefaultSort(c => c.Name);

    protected override Form<Customer> Form(Form<Customer> f) => f
        .Fields(
            f.Text(c => c.Name).Required(),
            new EmailInput<Customer>(c => c.Email).Required());
}

/// <summary>Adds a second, login-free panel carrying the resource above, so the custom
/// components are exercised through the real routing and rendering pipeline.</summary>
public sealed class CustomComponentAppFactory : DemoAppFactory
{
    protected override void ConfigureTestServices(IServiceCollection services) =>
        services.AddFilaPanel(panel => panel
            .AtPath("custom")
            .UseDbContext<AppDb>()
            .DiscoverResources(typeof(CustomComponentResource).Assembly));
}

public sealed class CustomComponentTests(CustomComponentAppFactory factory)
    : IClassFixture<CustomComponentAppFactory>
{
    [Fact]
    public async Task ConsumerAuthoredColumn_RendersThroughTheViewItNames()
    {
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/custom/custom-customers");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var headers = document.QuerySelectorAll("table.fi-ta-table thead th")
            .Select(th => th.TextContent.Trim())
            .ToList();
        Assert.Equal("Initials", headers[1]);

        // Badge markup from View => "badge", initials from the FormatDisplay override — the
        // seeded customers are Acme Corp, Globex and Initech (samples/Demo/Data/DemoSeeder.cs).
        var badges = document.QuerySelectorAll("table.fi-ta-table tbody .fi-badge")
            .Select(span => span.TextContent)
            .ToList();
        Assert.Equal(["AC", "G", "I"], badges);
    }

    [Fact]
    public void SharedSettersChainInEitherOrder()
    {
        // A compile-time guarantee more than a runtime one: the self type on the fluent base is
        // what keeps a type-specific setter reachable after a shared one. Returning the base
        // from Sortable() would make the second line here fail to compile while the first still
        // succeeded — the "compiles for the demo, breaks for other call shapes" trap.
        var table = new Table<Customer>();

        var formatThenSort = table.Text(c => c.Name).Money().Sortable();
        var sortThenFormat = table.Text(c => c.Name).Sortable().Money();

        Assert.Equal("$12.50", formatThenSort.FormatState(12.5m));
        Assert.Equal("$12.50", sortThenFormat.FormatState(12.5m));
        Assert.True(formatThenSort.IsSortable);
        Assert.True(sortThenFormat.IsSortable);
    }

    [Fact]
    public async Task ConsumerAuthoredField_RendersWithItsOwnInputType()
    {
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/custom/custom-customers/actions/create");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var email = document.QuerySelector("input[name='Email']");
        Assert.NotNull(email);
        Assert.Equal("email", email.GetAttribute("type"));

        // The built-in field alongside it is unaffected.
        Assert.Equal("text", document.QuerySelector("input[name='Name']")?.GetAttribute("type"));
    }
}
