namespace Fila.Notifications;

/// <summary>A toast Fila can pop after an action runs — the reusable generalization of what
/// used to be a private SetCloseAndNotifyTrigger helper in FilaExtensions, called with
/// hardcoded strings from exactly three places. Mirrors Filament's
/// packages/notifications/src/Notification.php: a static Make() factory plus fluent setters.
///
/// Deliberately inert: a notification is a title and a tone, and knows nothing about how it
/// reaches anyone. Delivery belongs to <see cref="IFilaNotificationStore"/>, which is what
/// keeps this package free of any transport (and so of any ASP.NET Core dependency).
///
/// Trimmed to what Fila's toast actually renders. Filament keeps status (success/danger) and
/// color as two separate concerns (Concerns/HasStatus.php vs Support/Concerns/HasColor.php);
/// Fila's toast has only a single color, which fila.js maps to both the CSS modifier class and
/// the icon — so <see cref="Success"/> and friends set that one field. Filament's
/// body/icon/duration/actions have no counterpart in what the frontend reads today, and adding
/// them would mean changing the toast UI — explicitly a non-goal here.</summary>
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
}
