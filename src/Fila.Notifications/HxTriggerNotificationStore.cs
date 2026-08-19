using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Fila.Notifications;

/// <summary>The default store: fire-and-forget, nothing persisted. Puts the notification on the
/// response's `HX-Trigger` header, which htmx dispatches as a bubbling CustomEvent that
/// fila.js's `fila-notify` listener turns into a toast.
///
/// Merges rather than overwrites, because that header is a shared channel — an action response
/// has usually already set `fila-modal-close` on it by the time a notification is sent, and
/// both signals have to ride the same header (htmx reads exactly one). A bare event name is a
/// legal HX-Trigger value on its own, so an existing bare name is folded in as `{name: true}` —
/// which is how the combined payload keeps the exact shape the frontend already expects:
/// {"fila-modal-close":true,"fila-notify":{"title":...,"color":...}}.</summary>
public sealed class HxTriggerNotificationStore : IFilaNotificationStore
{
    /// <summary>Used when no store was registered in DI. Stateless, so one instance is fine.</summary>
    public static readonly HxTriggerNotificationStore Instance = new();

    public void Send(HttpContext context, Notification notification)
    {
        var events = ReadPendingEvents(context);
        events["fila-notify"] = new { title = notification.TitleValue, color = notification.ColorValue };

        context.Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(events);
    }

    /// <summary>Whatever the response has already queued onto HX-Trigger, as a mutable map.
    /// Insertion-ordered, and the caller appends — so an already-set `fila-modal-close` stays
    /// the first key in the serialized object.</summary>
    private static Dictionary<string, object> ReadPendingEvents(HttpContext context)
    {
        var events = new Dictionary<string, object>();
        var existing = context.Response.Headers["HX-Trigger"].ToString();

        if (string.IsNullOrEmpty(existing)) return events;

        // The JSON-object form: {"event": detail, ...}. Clone each value — the JsonElements
        // would otherwise dangle once the document is disposed.
        if (existing.StartsWith('{'))
        {
            using var document = JsonDocument.Parse(existing);

            foreach (var property in document.RootElement.EnumerateObject())
                events[property.Name] = property.Value.Clone();

            return events;
        }

        // The bare form: one event name, or a comma-separated list of them, with no detail.
        foreach (var name in existing.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            events[name] = true;

        return events;
    }
}
