using Fila.Panels;
using Fila.Resources;

namespace Fila.Rendering;

public sealed class FilaDeleteConfirmViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required string Id { get; init; }
    public required string Label { get; init; }
}
