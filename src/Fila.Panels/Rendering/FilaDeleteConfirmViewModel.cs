using Fila.Panels.Resources;

namespace Fila.Panels.Rendering;

public sealed class FilaDeleteConfirmViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required string Id { get; init; }
    public required string Label { get; init; }
}
