using Fila.Infolists;
using Fila.Panels.Resources;
using Fila.Support;
using Action = Fila.Actions.Action;

namespace Fila.Panels.Rendering;

/// <summary>Renders an infolist-backed action's mount — today only the built-in View action,
/// but named generically the way FilaActionFormViewModel is, in case a resource author's own
/// custom action grows an infolist mount later. Sibling to FilaActionFormViewModel rather than
/// a variant of it: an infolist never validates or binds submitted values, so there is no
/// Errors/ActionUrl surface here for a POST to target.</summary>
public sealed class FilaActionInfolistViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required Action Action { get; init; }
    public required IInfolist Infolist { get; init; }
    public required object Entity { get; init; }

    /// <summary>Context for resolving the entries' evaluated settings — labels and visibility.
    /// Carries the record being viewed.</summary>
    public required EvaluationContext Evaluation { get; init; }
}
