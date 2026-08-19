using Fila.Forms;
using Fila.Panels.Resources;
using Fila.Support;
using Microsoft.EntityFrameworkCore;

namespace Fila.Panels.Rendering;

/// <summary>The dedicated Edit page a resource with relation managers gets — its own form
/// plus a tab per <see cref="Fila.Panels.RelationManagers.RelationManager"/> the
/// resource registers. A resource with no relations never routes here; its row Edit action
/// keeps opening the existing modal (see <see cref="FilaActionFormViewModel"/>) unchanged.</summary>
public sealed class FilaEditViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required IForm Form { get; init; }
    public required object Entity { get; init; }
    public required string Id { get; init; }
    public required DbContext Db { get; init; }

    /// <summary>Context for resolving the form fields' evaluated settings — carries the entity
    /// being edited and the values already entered for the form's other fields.</summary>
    public required EvaluationContext Evaluation { get; init; }

    public required IReadOnlyList<(string Path, string Message)> Errors { get; init; }

    public string? ErrorFor(string path) => Errors.FirstOrDefault(e => e.Path == path).Message;

    /// <summary>One entry per relation manager this resource registers, already loaded with
    /// its first page of rows.</summary>
    public required IReadOnlyList<FilaRelationTableViewModel> Relations { get; init; }
}
