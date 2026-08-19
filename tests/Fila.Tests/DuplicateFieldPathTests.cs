using Demo.Data;
using Fila.Forms;
using Fila.Panels;
using Fila.Panels.Resources;
using Fila.Tables;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fila.Tests;

/// <summary>Two fields bound to the same property. A legitimate thing to declare — two
/// mutually exclusive controls over one value, each gated by Visible(...) — and a natural
/// idiom now that fields can be conditionally visible. Building form state with ToDictionary
/// threw ArgumentException on the duplicate key, so both mounting and submitting the form 500'd.</summary>
public sealed class DuplicateFieldPathResource : Resource<Customer>
{
    public override string Slug => "duplicate-path-customers";

    protected override Table<Customer> Table(Table<Customer> t) => t
        .Columns(t.Text(c => c.Name));

    protected override Form<Customer> Form(Form<Customer> f) => f
        .Fields(
            f.Text(c => c.Name).Required(),
            f.Textarea(c => c.Name).Visible(false),
            f.Text(c => c.Email).Required());
}

public sealed class DuplicateFieldPathAppFactory : DemoAppFactory
{
    protected override void ConfigureTestServices(IServiceCollection services) =>
        services.AddFilaPanel(panel => panel
            .AtPath("duplicate-path-check")
            .UseDbContext<AppDb>()
            .DiscoverResources(typeof(DuplicateFieldPathResource).Assembly));
}

public sealed class DuplicateFieldPathTests(DuplicateFieldPathAppFactory factory)
    : IClassFixture<DuplicateFieldPathAppFactory>
{
    private const string Url = "/duplicate-path-check/duplicate-path-customers/actions/create";

    [Fact]
    public async Task Mount_DoesNotFailOnTheDuplicatePath()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Url);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Submit_SavesRatherThanThrowingOnTheDuplicatePath()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync(Url, new FormUrlEncodedContent(
        [
            new("Name", "Duplicate Path Co"),
            new("Email", "dup@path.test"),
        ]));

        response.EnsureSuccessStatusCode();
    }
}
