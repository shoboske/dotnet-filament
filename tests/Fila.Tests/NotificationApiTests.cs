using Fila.Notifications;
using Fila.Panels.Rendering;
using Fila.Testing;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fila.Tests;

/// <summary>Covers the notification path that replaced FilaExtensions' private
/// SetCloseAndNotifyTrigger helper: the transport-free Notification/IFilaNotificationStore pair
/// in Fila.Notifications, and the htmx-specific half (IHxTriggerDataReader plus the store that
/// rides it) in Fila.Panels.
///
/// The wire format is the contract — fila.js listens for a `fila-notify` event and renders the
/// toast from its title/color — so these assert the serialized header exactly rather than just
/// its parsed shape. A reordered or renamed key would still parse, and would still break the
/// frontend.</summary>
public sealed class NotificationApiTests
{
    private static (IHxTriggerDataReader Triggers, HttpContext Context) NewReader(string? pending = null)
    {
        var context = new DefaultHttpContext();

        if (pending is not null) context.Response.Headers["HX-Trigger"] = pending;

        return (new HxTriggerDataReader(new HttpContextAccessor { HttpContext = context }), context);
    }

    private static string TriggerOf(HttpContext context) => context.Response.Headers["HX-Trigger"].ToString();

    /// <summary>The payload every built-in action produces: the modal-close event the response
    /// already queued, with the notification appended after it.</summary>
    [Theory]
    [InlineData("Created", "success")]
    [InlineData("Saved", "success")]
    [InlineData("Deleted", "danger")]
    public void Store_AppendsTheToastToAPendingModalClose(string title, string color)
    {
        var (triggers, context) = NewReader();
        triggers.Add("fila-modal-close");

        new HxTriggerNotificationStore(triggers).Send(Notification.Make().Title(title).Color(color));

        Assert.Equal(
            $$$"""{"fila-modal-close":true,"fila-notify":{"title":"{{{title}}}","color":"{{{color}}}"}}""",
            TriggerOf(context));
    }

    [Fact]
    public void Store_WithNothingPending_EmitsTheToastAlone()
    {
        var (triggers, context) = NewReader();

        new HxTriggerNotificationStore(triggers).Send(Notification.Make().Title("Marked as shipped").Success());

        Assert.Equal("""{"fila-notify":{"title":"Marked as shipped","color":"success"}}""", TriggerOf(context));
    }

    /// <summary>The whole point of the reader: two concerns writing the one header without
    /// either erasing the other, whatever detail payloads they carry.</summary>
    [Fact]
    public void Reader_PreservesEventsAlreadyQueued()
    {
        var (triggers, context) = NewReader("""{"fila-modal-close":true,"some-event":{"n":1}}""");

        triggers.Add("fila-notify", new { title = "Saved", color = "success" });

        Assert.Equal(
            """{"fila-modal-close":true,"some-event":{"n":1},"fila-notify":{"title":"Saved","color":"success"}}""",
            TriggerOf(context));
    }

    /// <summary>A detail-less event still serializes as JSON rather than htmx's bare
    /// event-name form — one shape to append to, so nothing has to be rewritten when a second
    /// event joins it.</summary>
    [Fact]
    public void Reader_WritesDetaillessEventsAsJsonToo()
    {
        var (triggers, context) = NewReader();

        triggers.Add("fila-modal-open");

        Assert.Equal("""{"fila-modal-open":true}""", TriggerOf(context));
    }

    [Fact]
    public void Reader_OutsideARequest_SaysSoRatherThanFailingQuietly()
    {
        var triggers = new HxTriggerDataReader(new HttpContextAccessor { HttpContext = null });

        Assert.Throws<InvalidOperationException>(() => triggers.Add("fila-notify"));
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

/// <summary>The same guarantee end to end through the real endpoints — proving the built-in
/// actions and the demo's own custom action both reach the frontend with the toast payload
/// unchanged from before Phase 5, not just that the pieces compose in isolation.</summary>
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

    /// <summary>samples/Demo's "Mark shipped" declares its notification through the public
    /// builder — this is the acceptance test that a resource author's own notification travels
    /// the same channel Fila's do.</summary>
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

    /// <summary>A mount carries no notification, so it queues only the open event. This is the
    /// one payload the reader changes: it used to be htmx's bare `fila-modal-open` string.
    /// Alpine dispatches the event identically either way, and List.cshtml's
    /// @fila-modal-open.window listener reads only the event name.</summary>
    [Fact]
    public async Task Mount_QueuesTheOpenEventAlone()
    {
        using var client = factory.CreateClient();
        await TestAuth.LoginAsync(client);

        var response = await client.GetAsync("/admin/orders/12/actions/edit");
        response.EnsureSuccessStatusCode();

        Assert.Equal("""{"fila-modal-open":true}""", TriggerOf(response));
    }
}
