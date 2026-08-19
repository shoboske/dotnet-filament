using Fila.Actions;
using Fila.Panels.Resources;
using Fila.Support;

namespace Fila.Panels.Rendering;

/// <summary>Renders the confirmation step for a bulk action that requires one — mirrors
/// <see cref="FilaActionConfirmViewModel"/>, but a bulk action has no single record to key its
/// action URL off of; it always runs at the table's bulk-actions endpoint against whichever
/// ids the table's checkboxes submit alongside the confirm button's POST.</summary>
public sealed class FilaBulkActionConfirmViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required BulkAction Action { get; init; }
    public required EvaluationContext Evaluation { get; init; }

    public string ActionUrl => $"/{Panel.Path}/{Resource.Slug}/bulk-actions/{Action.Name}";
}
