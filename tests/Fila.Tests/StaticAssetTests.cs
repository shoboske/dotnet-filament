using System.Net;
using AngleSharp.Html.Parser;
using Xunit;

namespace Fila.Tests;

/// <summary>Fila's CSS and JS ship as static web assets out of Fila.Panels, but the served URL
/// is derived from the assembly name — so the v2 package split would have moved them from
/// /_content/Fila/... to /_content/Fila.Panels/... on its own. Fila.Panels pins
/// StaticWebAssetBasePath to hold the URL still. Nothing else in the suite fetches an asset, so
/// without these the panel would render completely unstyled and every test would stay green.</summary>
public sealed class StaticAssetTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Theory]
    [InlineData("/_content/Fila/fila/fila.css")]
    [InlineData("/_content/Fila/fila/fila.actions.css")]
    [InlineData("/_content/Fila/fila/fila.js")]
    public async Task PanelAssets_AreServedAtTheContentFilaBasePath(string path)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LoginPage_LinksOnlyAssetsThatActuallyResolve()
    {
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/admin/login");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var references = document.QuerySelectorAll("link[href], script[src]")
            .Select(element => element.GetAttribute("href") ?? element.GetAttribute("src")!)
            .Where(reference => reference.StartsWith('/'))
            .ToList();

        Assert.NotEmpty(references);

        foreach (var reference in references)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(reference)).StatusCode);
    }
}
