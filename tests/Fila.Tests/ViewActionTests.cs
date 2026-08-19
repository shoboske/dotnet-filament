using AngleSharp.Html.Parser;
using Xunit;

namespace Fila.Tests;

/// <summary>End-to-end coverage for ViewAction on OrderResource — reuses the resource's own
/// form schema in read-only mode (Fila.Infolists doesn't exist yet), so this locks in that the
/// fields render disabled and pre-filled rather than as an editable form.</summary>
public sealed class ViewActionTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task Mount_RendersThePrefilledFormWithFieldsDisabledAndNoSaveButton()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/orders/1/actions/view");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var reference = document.QuerySelector("input[name='Reference']");
        Assert.Equal("ORD-1001", reference?.GetAttribute("value"));
        Assert.True(reference?.HasAttribute("disabled"));

        Assert.Null(document.QuerySelector("button[type='submit']"));
    }
}
