using Fila.Panels;
using Fila.Resources;
using Fila.Tables;

namespace Fila.Rendering;

public sealed class FilaListViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required ITable Table { get; init; }
    public required TableQuery Query { get; init; }
    public required PagedRows Paged { get; init; }
}
