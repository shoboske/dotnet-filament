namespace Fila.Panels.Rendering;

/// <summary>Owns the response's `HX-Trigger` header. htmx reads exactly one such header per
/// response but dispatches every key in it as its own CustomEvent, so the header is a shared
/// channel that several unrelated concerns write to — a modal closing and a toast popping ride
/// the same response. Left to raw header assignment, whichever wrote last silently erased the
/// others.
///
/// Sealing that behind this interface means callers queue an event and never think about the
/// header: no reading it back, no merging, no JSON. It is also the only thing in the
/// notification path that touches HttpContext, which is what lets Fila.Notifications stay
/// transport-free.</summary>
public interface IHxTriggerDataReader
{
    /// <summary>Events already queued on this response, in the order they were added.</summary>
    IReadOnlyDictionary<string, object> Read();

    /// <summary>Queues an event, preserving everything already queued. A null detail means the
    /// event carries no payload — htmx still dispatches it, and Fila's listeners for those
    /// (e.g. List.cshtml's @fila-modal-open.window) read only the event name.</summary>
    void Add(string eventName, object? detail = null);
}
