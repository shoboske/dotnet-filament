using Fila.Actions;
using Fila.Panels.Resources;
using Fila.Support;

namespace Fila.Panels.Rendering;

/// <summary>Renders the confirmation step for a schema-less action that requires one — Delete,
/// Replicate, and any custom confirm-only action (e.g. samples/Demo's "Mark shipped"). Replaces
/// the Delete-specific FilaDeleteConfirmViewModel.</summary>
public sealed class FilaActionConfirmViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required Fila.Actions.Action Action { get; init; }
    public required EvaluationContext Evaluation { get; init; }

    /// <summary>Null for a header action; the id of the record being acted on otherwise. Every
    /// built-in confirm-only action (Delete, Replicate) is a row action, but a custom one in
    /// principle need not be.</summary>
    public string? Id { get; init; }

    public string ActionUrl => Id is null
        ? $"/{Panel.Path}/{Resource.Slug}/actions/{Action.Name}"
        : $"/{Panel.Path}/{Resource.Slug}/{Id}/actions/{Action.Name}";
}
