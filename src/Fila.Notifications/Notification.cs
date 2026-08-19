using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Fila.Notifications;

/// <summary>A toast Fila can pop after an action runs — the reusable generalization of what
/// used to be a private SetCloseAndNotifyTrigger helper in FilaExtensions, called with
/// hardcoded strings from exactly three places. Mirrors Filament's
/// packages/notifications/src/Notification.php: a static Make() factory plus fluent setters,
/// terminating in Send().
///
/// Trimmed to what Fila's existing wire format carries. Filament keeps status (success/danger)
/// and color as two separate concerns (Concerns/HasStatus.php vs Support/Concerns/HasColor.php);
/// Fila's `fila-notify` payload has only a single `color` field, which fila.js maps to both the
/// toast's CSS modifier class and its icon — so <see cref="Success"/> and friends set that one
/// field. Filament's body/icon/duration/actions have no counterpart in the payload the frontend
/// reads today, and adding them would mean changing the toast UI — explicitly a non-goal here.</summary>
public sealed class Notification
{
    private Notification()
    {
    }

    public static Notification Make() => new();

    public string TitleValue { get; private set; } = string.Empty;

    /// <summary>Tone name (success | danger | warning | info). Drives the toast's
    /// `fi-no-notification-{color}` modifier class and which icon fila.js renders.</summary>
    public string ColorValue { get; private set; } = "success";

    public Notification Title(string title)
    {
        TitleValue = title;
        return this;
    }

    public Notification Color(string color)
    {
        ColorValue = color;
        return this;
    }

    public Notification Success() => Color("success");

    public Notification Danger() => Color("danger");

    public Notification Warning() => Color("warning");

    public Notification Info() => Color("info");

    /// <summary>Hands this notification to the request's <see cref="IFilaNotificationStore"/>,
    /// which decides how it reaches the user. Returns this so a caller can keep chaining, the
    /// same way Filament's send() returns static.
    ///
    /// Falls back to <see cref="HxTriggerNotificationStore"/> when nothing is registered, so a
    /// Notification still works outside a panel that called AddFilaPanel.</summary>
    public Notification Send(HttpContext context)
    {
        var store = context.RequestServices?.GetService<IFilaNotificationStore>()
            ?? HxTriggerNotificationStore.Instance;

        store.Send(context, this);
        return this;
    }
}
