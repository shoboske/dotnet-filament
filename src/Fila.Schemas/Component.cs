using Fila.Support;

namespace Fila.Schemas;

/// <summary>Base class for one node of a schema — Filament's
/// Schemas\Components\Component. Holds the settings every schema node has, each as an
/// <see cref="Evaluated{TValue}"/> so a subclass's fluent setters can accept either a plain
/// value or a closure over the <see cref="EvaluationContext"/>. Filament spreads these across
/// the HasName, HasLabel and CanBeHidden concerns; C# has no traits, so they sit on the class.
///
/// The fluent setters themselves live on the subclasses rather than here: a setter has to
/// return the type the caller is chaining on, and returning <c>Component</c> from
/// <c>.Label(...)</c> would end the chain.</summary>
public abstract class Component : IComponent
{
    protected Component(string name)
    {
        Name = name;
        LabelValue = ComponentText.Humanize(name);
    }

    public string Name { get; }

    protected Evaluated<string> LabelValue { get; set; }

    protected Evaluated<bool> VisibleValue { get; set; } = true;

    public string ResolveLabel(EvaluationContext context) => LabelValue.Resolve(context) ?? Name;

    public bool ResolveVisible(EvaluationContext context) => VisibleValue.Resolve(context);
}
