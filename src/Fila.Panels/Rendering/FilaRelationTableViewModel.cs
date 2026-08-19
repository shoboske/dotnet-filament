using Fila.Panels.RelationManagers;
using Fila.Panels.Resources;
using Fila.Support;
using Fila.Tables;

namespace Fila.Panels.Rendering;

/// <summary>What one relation manager's nested table needs to render — the Edit page's
/// equivalent of <see cref="FilaListViewModel"/>, scoped to a single parent record instead of a
/// whole resource list.</summary>
public sealed class FilaRelationTableViewModel
{
    public required Panel Panel { get; init; }
    public required IResource ParentResource { get; init; }
    public required string ParentId { get; init; }
    public required RelationManager RelationManager { get; init; }
    public required ITable Table { get; init; }
    public required TableQuery Query { get; init; }
    public required PagedRows Paged { get; init; }
    public required EvaluationContext Evaluation { get; init; }

    /// <summary>The path the relation manager's own table re-requests itself from for
    /// sort/search/paginate — nested under the parent record the same way the issue's routing
    /// section describes.</summary>
    public string TableUrl => $"/{Panel.Path}/{ParentResource.Slug}/{ParentId}/relations/{RelationManager.Slug}";
}
