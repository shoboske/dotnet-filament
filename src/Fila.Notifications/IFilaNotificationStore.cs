using Microsoft.AspNetCore.Http;

namespace Fila.Notifications;

/// <summary>Where a sent <see cref="Notification"/> goes. The seam exists because Filament's
/// own Notification::send() writes to a store (the session) rather than straight to a
/// transport, and its persisted variant (sendToDatabase(), backing the notification bell) is
/// just a different store behind the same builder.
///
/// Fila ships one implementation today — <see cref="HxTriggerNotificationStore"/>, which is
/// fire-and-forget and persists nothing, matching the behavior this replaced. An EF Core-backed
/// store plus a topbar bell is the deferred half of this phase.</summary>
public interface IFilaNotificationStore
{
    void Send(HttpContext context, Notification notification);
}
