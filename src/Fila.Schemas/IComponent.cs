using Fila.Support;

namespace Fila.Schemas;

/// <summary>The contract every node of a schema satisfies — a form field, an infolist entry, a
/// layout container. Deliberately small: it covers only what code that walks a schema without
/// knowing the concrete type needs (identity, label, whether to draw it).
///
/// Table columns deliberately do not implement this. In Filament a column extends
/// Support\Components\ViewComponent, not Schemas\Components\Component, and
/// packages/tables/composer.json does not require filament/schemas at all — a column is a
/// column, not a node of a schema tree. Fila keeps that separation; ITableColumn declares its
/// own equivalent members.</summary>
public interface IComponent
{
    /// <summary>Identifies the component within its schema. For anything bound to a property
    /// this is the property path — <c>Name</c>, or <c>Customer.Name</c> for a nested one.</summary>
    string Name { get; }

    string ResolveLabel(EvaluationContext context);

    /// <summary>False hides the component entirely — it is not drawn, and for a form field not
    /// read back on submit either.</summary>
    bool ResolveVisible(EvaluationContext context);
}
