namespace Fila.Widgets;

/// <summary>Base type for anything that can appear on a dashboard — Filament's
/// Widgets\Widget. Like a table column (and unlike a form field), a widget is a
/// self-contained view component rather than a node of a schema tree, so it deliberately does
/// not implement Fila.Schemas' IComponent: Filament's own Widget extends
/// Support\Components\ViewComponent and packages/widgets does not require filament/schemas.
///
/// A widget carries no markup of its own. <see cref="View"/> is a dispatch key the panel looks
/// up in its ComponentViewRegistry to find the partial that renders it — the same seam a
/// custom table column goes through — which is what lets a consumer define a widget type in
/// their own assembly without Fila.Widgets ever knowing how to draw it.</summary>
public abstract class Widget
{
    /// <summary>Names the partial that renders this widget. A view with no registration falls
    /// back to the default widget partial (card chrome plus the loaded data as text), so a
    /// consumer-authored widget renders something sensible without a registry entry.</summary>
    public abstract string View { get; }

    /// <summary>Title drawn above the widget's body. Null draws no heading — which is what a
    /// stats row wants, since each of its cards is labelled individually.</summary>
    public virtual string? Heading => null;

    /// <summary>Secondary line under the heading. Ignored when Heading is null.</summary>
    public virtual string? Description => null;

    /// <summary>How many of the dashboard grid's 12 columns this widget occupies. Defaults to
    /// the full width, matching Filament's widgets spanning the whole dashboard grid unless
    /// they say otherwise.</summary>
    public virtual int ColumnSpan => 12;

    /// <summary>Ascending sort position on the dashboard. Widgets with equal Sort keep their
    /// registration order — panel widgets first, then each resource's, in navigation order.</summary>
    public virtual int Sort => 0;

    /// <summary>Computes whatever this widget's partial needs to draw it. Called once per
    /// dashboard request. The returned object is passed straight through to the partial, so
    /// each widget type and its partial agree on the shape between themselves.</summary>
    public abstract Task<object> LoadAsync(WidgetContext context);
}
