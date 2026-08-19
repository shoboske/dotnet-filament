namespace Fila.Notifications;

/// <summary>Delivers a <see cref="Notification"/>. The seam exists because Filament's own
/// Notification::send() writes to a store (the session) rather than straight to a transport,
/// and its persisted variant — sendToDatabase(), which backs the notification bell — is the
/// same notification behind a different store.
///
/// Takes only the notification: how the current request is reached is the implementation's
/// business, not the caller's. That is what lets this package stay transport-free — Fila's own
/// implementation (HxTriggerNotificationStore) lives in Fila.Panels, where htmx and HTTP
/// already belong.</summary>
public interface IFilaNotificationStore
{
    void Send(Notification notification);
}
