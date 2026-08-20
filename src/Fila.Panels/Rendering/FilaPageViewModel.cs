using Fila.Panels.Pages;
using Fila.Support;

namespace Fila.Panels.Rendering;

/// <summary>What a custom page's own view renders against — the Pages counterpart to
/// FilaListViewModel/FilaDashboardViewModel. Wraps the activated Page instance rather than
/// letting its view take the page directly as @model, so every page's view still has Panel and
/// Evaluation available the same way every other Fila view does.</summary>
public sealed class FilaPageViewModel
{
    public required Panel Panel { get; init; }
    public required Page Page { get; init; }
    public required EvaluationContext Evaluation { get; init; }
}
