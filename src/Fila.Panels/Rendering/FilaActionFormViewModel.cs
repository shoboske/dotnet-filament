using Fila.Forms;
using Fila.Panels.Resources;
using Fila.Support;
using Microsoft.EntityFrameworkCore;
using Action = Fila.Actions.Action;

namespace Fila.Panels.Rendering;

/// <summary>Renders the form an action mounts — Create/Edit/View today, and any custom action a
/// resource author gives a Schema tomorrow. Replaces the Create/Edit-specific
/// FilaFormViewModel; Action.ResolveModalHeading/ReadOnlyFlag are what let one view model and
/// one partial (_ActionForm.cshtml) cover all three instead of one view model per verb.</summary>
public sealed class FilaActionFormViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required Action Action { get; init; }
    public required IForm Form { get; init; }
    public required object Entity { get; init; }
    public required DbContext Db { get; init; }

    /// <summary>Context for resolving the fields' evaluated settings — labels, required-ness,
    /// visibility, a Select's options. Carries the entity being edited and the values already
    /// entered for the form's other fields.</summary>
    public required EvaluationContext Evaluation { get; init; }

    /// <summary>Null for a header action (e.g. create, whose record doesn't exist until Handle
    /// runs); the id of the record being acted on otherwise.</summary>
    public string? Id { get; init; }

    public required IReadOnlyList<(string Path, string Message)> Errors { get; init; }

    public string? ErrorFor(string path) => Errors.FirstOrDefault(e => e.Path == path).Message;

    /// <summary>The path the mounted form posts back to — the same generic
    /// {resource}/{id?}/actions/{name} route it was mounted from.</summary>
    public string ActionUrl => Id is null
        ? $"/{Panel.Path}/{Resource.Slug}/actions/{Action.Name}"
        : $"/{Panel.Path}/{Resource.Slug}/{Id}/actions/{Action.Name}";
}
