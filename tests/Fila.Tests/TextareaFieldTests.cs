using AngleSharp.Html.Parser;
using Demo.Data;
using Fila.Forms;
using Fila.Panels;
using Fila.Panels.Resources;
using Fila.Tables;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fila.Tests;

/// <summary>Textarea is a built-in view nothing in samples/Demo or CustomComponentTests
/// exercises otherwise — this locks in that the registry still resolves it to the same markup
/// the old switch statement produced.</summary>
public sealed class TextareaFieldResource : Resource<Customer>
{
    public override string Slug => "textarea-customers";

    protected override Table<Customer> Table(Table<Customer> t) => t
        .Columns(t.Text(c => c.Name));

    protected override Form<Customer> Form(Form<Customer> f) => f
        .Fields(f.Textarea(c => c.Name).Required());
}

public sealed class TextareaFieldAppFactory : DemoAppFactory
{
    protected override void ConfigureTestServices(IServiceCollection services) =>
        services.AddFilaPanel(panel => panel
            .AtPath("textarea-check")
            .UseDbContext<AppDb>()
            .DiscoverResources(typeof(TextareaFieldResource).Assembly));
}

public sealed class TextareaFieldTests(TextareaFieldAppFactory factory) : IClassFixture<TextareaFieldAppFactory>
{
    [Fact]
    public async Task CreateForm_RendersATextareaElement()
    {
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/textarea-check/textarea-customers/create");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var textarea = document.QuerySelector("textarea[name='Name']");
        Assert.NotNull(textarea);
        Assert.Equal("3", textarea!.GetAttribute("rows"));
    }
}
