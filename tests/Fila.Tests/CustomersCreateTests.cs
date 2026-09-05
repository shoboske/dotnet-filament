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

    [Fact]
    public async Task Mount_OffersACreateAndCreateAnotherButton_AfterTheRegularSubmit()
    {
        // CreateAction::getModalFooterActions() orders [submit, ...extraModalFooterActions,
        // cancel] -- confirmed against a live rendered create modal: Create, then
        // "Create & create another", then Cancel.
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var html = await client.GetStringAsync("/admin/customers/actions/create");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        var buttons = document.QuerySelectorAll(".fi-modal-footer-actions button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(["Create", "Create & create another", "Cancel"], buttons);

        var anotherButton = document.QuerySelector("button[name='another']");
        Assert.NotNull(anotherButton);
        Assert.Equal("true", anotherButton!.GetAttribute("value"));
    }

    [Fact]
    public async Task CreateAndCreateAnother_SavesTheRecord_KeepsTheModalOpen_AndResetsTheForm()
    {
        // Filament's CreateAction "another" branch: the record is saved and the success
        // notification still fires, but the modal doesn't close (no fila-modal-close) and the
        // form comes back blank instead of showing what was just submitted.
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var response = await client.PostAsync("/admin/customers/actions/create", new FormUrlEncodedContent(
        [
            new("Name", "Wayne Enterprises"),
            new("Email", "bruce@wayne.test"),
            new("another", "true"),
        ]));
        response.EnsureSuccessStatusCode();
        response.AssertNotificationTriggered(title: "Created", color: "success");

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("fila-modal-close", response.Headers.GetValues("HX-Trigger").Single());

        // The primary swap target (#fila-table, per the form's own hx-target) shows the new row
        // immediately -- Filament's own version stays in sync too, just via one Livewire
        // component re-rendering rather than two separate htmx swaps.
        Assert.Contains("Wayne Enterprises", body);

        // ...and the out-of-band swap resets the modal to a blank create form -- not one still
        // holding "Wayne Enterprises"/"bruce@wayne.test".
        var oobDocument = await new HtmlParser().ParseDocumentAsync(body);
        var oobInputs = oobDocument.QuerySelectorAll("[hx-swap-oob] input").Select(i => i.GetAttribute("value")).ToList();
        Assert.All(oobInputs, value => Assert.True(string.IsNullOrEmpty(value)));

        var listHtml = await client.GetStringAsync("/admin/customers");
        Assert.Contains("Wayne Enterprises", listHtml);
    }
}
