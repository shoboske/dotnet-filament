using Fila.Panels.Resources;
using Fila.Schemas;
using Fila.Tables;
using Microsoft.EntityFrameworkCore;

namespace Fila.Panels.Rendering;

public sealed class FilaListViewModel
{
    public required Panel Panel { get; init; }
    public required IResource Resource { get; init; }
    public required ITable Table { get; init; }
    public required TableQuery Query { get; init; }
    public required PagedRows Paged { get; init; }

    /// <summary>Context for resolving the columns' evaluated settings — labels and
    /// visibility. Table headers are drawn once for the whole page, so it carries no record.</summary>
    public required EvaluationContext Evaluation { get; init; }

    /// <summary>Only needed so row actions can call Resource.GetId(Db, row) to build
    /// edit/delete URLs — not used for querying from the view.</summary>
    public required DbContext Db { get; init; }
}
