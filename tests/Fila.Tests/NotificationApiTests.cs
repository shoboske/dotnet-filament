using Fila.Notifications;
using Fila.Testing;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fila.Tests;

/// <summary>Covers the Fila.Notifications API that replaced FilaExtensions' private
/// SetCloseAndNotifyTrigger helper. The wire format is the contract here: fila.js already
/// listens for a `fila-notify` event and renders the toast from its title/color, so these
/// assert the serialized header exactly rather than just its parsed shape — a reordered or
/// renamed key would still parse, and would still break the frontend.</summary>
public sealed class NotificationApiTests
{
    private static HttpContext ContextWithPendingTrigger(string? trigger)
    {
        var context = new DefaultHttpContext();

        if (trigger is not null) context.Response.Headers["HX-Trigger"] = trigger;

        return context;
    }

    private static string TriggerOf(HttpContext context) => context.Response.Headers["HX-Trigger"].ToString();

    /// <summary>The exact payload every built-in action produced before this API existed, and
    /// still produces: the modal-close event the response already queued, folded from its bare
    /// form into `{name: true}`, with the notification appended after it.</summary>
    [Theory]
    [InlineData("Created", "success")]
    [InlineData("Saved", "success")]
    [InlineData("Deleted", "danger")]
    public void Send_MergesOntoAPendingModalClose_InTheOriginalWireFormat(string title, string color)
    {
        var context = ContextWithPendingTrigger("fila-modal-close");

        Notification.Make().Title(title).Color(color).Send(context);

        Assert.Equal(
            $$$"""{"fila-modal-close":true,"fila-notify":{"title":"{{{title}}}","color":"{{{color}}}"}}""",
            TriggerOf(context));
    }

    [Fact]
    public void Send_WithNothingPending_EmitsTheNotificationAlone()
    {
        var context = ContextWithPendingTrigger(null);

        Notification.Make().Title("Marked as shipped").Success().Send(context);

        Assert.Equal(
            """{"fila-notify":{"title":"Marked as shipped","color":"success"}}""",
            TriggerOf(context));
    }

    /// <summary>The merge also has to cope with a header already in its JSON-object form —
    /// otherwise a second Send, or any future event carrying a detail payload, would silently
    /// drop whatever was there.</summary>
    [Fact]
    public void Send_PreservesAnAlreadyStructuredTriggerHeader()
    {
        var context = ContextWithPendingTrigger("""{"fila-modal-close":true,"some-event":{"n":1}}""");

        Notification.Make().Title("Saved").Success().Send(context);

        Assert.Equal(
            """{"fila-modal-close":true,"some-event":{"n":1},"fila-notify":{"title":"Saved","color":"success"}}""",
            TriggerOf(context));
    }

    [Theory]
    [InlineData("success")]
    [InlineData("danger")]
    [InlineData("warning")]
    [InlineData("info")]
    public void StatusHelpers_SetTheColorTheToastRendersWith(string color)
    {
        var notification = Notification.Make().Title("Whatever");

        _ = color switch
        {
            "success" => notification.Success(),
            "danger" => notification.Danger(),
            "warning" => notification.Warning(),
            _ => notification.Info(),
        };

        Assert.Equal(color, notification.ColorValue);
    }

    [Fact]
    public void Make_DefaultsToASuccessToastWithNoTitle()
    {
        var notification = Notification.Make();

        Assert.Equal(string.Empty, notification.TitleValue);
        Assert.Equal("success", notification.ColorValue);
    }
}

/// <summary>The same guarantee as above, but end to end through the real endpoints — proving
/// the built-in actions and the demo's own custom action both reach the frontend with the
/// header unchanged from before Phase 5, not just that the builder can produce it.</summary>
public sealed class NotificationWireFormatTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    private static string TriggerOf(HttpResponseMessage response) =>
        response.Headers.GetValues("HX-Trigger").Single();

    [Fact]
    public async Task BuiltInCreateAction_SendsTheUnchangedCreatedPayload()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var response = await client.PostAsync("/admin/customers/actions/create", new FormUrlEncodedContent(
        [
            new("Name", "Wayne Enterprises"),
            new("Email", "bruce@wayne.test"),
        ]));
        response.EnsureSuccessStatusCode();

        Assert.Equal(
            """{"fila-modal-close":true,"fila-notify":{"title":"Created","color":"success"}}""",
            TriggerOf(response));
    }

    /// <summary>samples/Demo's "Mark shipped" builds its notification through the public
    /// builder rather than the Notifies(title, color) shorthand — this is the acceptance test
    /// that a resource author's own notification travels the same channel Fila's do.</summary>
    [Fact]
    public async Task CustomMarkShippedAction_SendsItsOwnNotificationThroughTheSameChannel()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        // Order 11 (i=11, statuses[11 % 5]) seeds as Processing, so the action is offered for
        // it — and no other test in this class touches it.
        var response = await client.PostAsync("/admin/orders/11/actions/mark-shipped", null);
        response.EnsureSuccessStatusCode();

        Assert.Equal(
            """{"fila-modal-close":true,"fila-notify":{"title":"Marked as shipped","color":"success"}}""",
            TriggerOf(response));

        response.AssertNotificationTriggered(title: "Marked as shipped", color: "success");
        response.AssertModalClosed();
    }
}
