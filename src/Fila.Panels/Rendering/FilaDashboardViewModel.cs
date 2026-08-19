using Fila.Support;

namespace Fila.Panels.Rendering;

/// <summary>The panel's landing page: a grid of already-loaded widgets. Unlike the list view
/// model this carries no DbContext — every widget did its own querying during
/// Widget.LoadAsync before the view ran, so the page renders without touching the database.</summary>
public sealed class FilaDashboardViewModel
{
    public required Panel Panel { get; init; }

    /// <summary>In display order (Widget.Sort, then registration order). Empty when the panel
    /// registered no widgets — the view draws its empty state for that.</summary>
    public required IReadOnlyList<WidgetRenderModel> Widgets { get; init; }

    /// <summary>Context for anything on the page with evaluated settings. Carries no record —
    /// a dashboard is not about one row.</summary>
    public required EvaluationContext Evaluation { get; init; }
}
