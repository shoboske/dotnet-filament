using Fila.Forms;
using Fila.Panels.Resources;
using Microsoft.EntityFrameworkCore;

namespace Fila.Panels.Rendering;

public sealed class FilaFormViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required IForm Form { get; init; }
    public required object Entity { get; init; }
    public required DbContext Db { get; init; }

    /// <summary>Null means this is the create form; non-null is the id being edited.</summary>
    public string? Id { get; init; }

    public required IReadOnlyList<(string Path, string Message)> Errors { get; init; }

    public bool IsCreate => Id is null;

    public string? ErrorFor(string path) => Errors.FirstOrDefault(e => e.Path == path).Message;
}
