using Fila.Notifications;

namespace Fila.Panels.Rendering;

/// <summary>Fila's default notification delivery: fire-and-forget, nothing persisted, which is
/// how notifications behaved before they had an API at all. Queues the toast as a `fila-notify`
/// event for fila.js to render.
///
/// Lives in Fila.Panels rather than Fila.Notifications because it is entirely htmx- and
/// HTTP-specific, and that is the panel's business. Registered per panel (keyed on
/// Panel.Path), so one panel can be given a different store — an EF Core-backed one behind a
/// notification bell, say — without disturbing the others.</summary>
public sealed class HxTriggerNotificationStore(IHxTriggerDataReader triggers) : IFilaNotificationStore
{
    public void Send(Notification notification) =>
        triggers.Add("fila-notify", new { title = notification.TitleValue, color = notification.ColorValue });
}
