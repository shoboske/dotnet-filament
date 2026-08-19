using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Fila.Panels.Rendering;

/// <summary>The default <see cref="IHxTriggerDataReader"/>, reading and writing the live
/// response header. Always emits htmx's JSON-object form ({"event": detail, ...}) rather than
/// the bare event-name form, so there is exactly one shape to append to — the bare form cannot
/// carry a second event's payload, so anything that started bare would have to be rewritten as
/// JSON the moment a notification joined it.</summary>
public sealed class HxTriggerDataReader(IHttpContextAccessor accessor) : IHxTriggerDataReader
{
    public IReadOnlyDictionary<string, object> Read()
    {
        var events = new Dictionary<string, object>();
        var header = Response.Headers["HX-Trigger"].ToString();

        if (string.IsNullOrEmpty(header)) return events;

        // Clone each value — the JsonElements would dangle once the document is disposed.
        using var document = JsonDocument.Parse(header);

        foreach (var property in document.RootElement.EnumerateObject())
            events[property.Name] = property.Value.Clone();

        return events;
    }

    public void Add(string eventName, object? detail = null)
    {
        var events = new Dictionary<string, object>(Read()) { [eventName] = detail ?? true };

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(events);
    }

    private HttpResponse Response =>
        accessor.HttpContext?.Response ?? throw new InvalidOperationException(
            "No HttpContext is available. IHxTriggerDataReader shapes the current response, so it " +
            "only works while a request is in flight; AddFilaPanel registers IHttpContextAccessor for it.");
}
