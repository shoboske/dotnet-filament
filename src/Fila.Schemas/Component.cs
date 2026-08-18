namespace Fila.Schemas;

/// <summary>Base class for one node of a schema. Holds the settings every component has, each
/// as an <see cref="Evaluated{TValue}"/> so a subclass's fluent setters can accept either a
/// plain value or a closure over the <see cref="EvaluationContext"/>.
///
/// The fluent setters themselves live on the subclasses rather than here: a setter has to
/// return the type the caller is chaining on, and returning <c>Component</c> from
/// <c>.Label(...)</c> would end the chain.</summary>
public abstract class Component : IComponent
{
    protected Component(string name)
    {
        Name = name;
        LabelValue = Humanize(name);
    }

    public string Name { get; }

    protected Evaluated<string> LabelValue { get; set; }

    protected Evaluated<bool> VisibleValue { get; set; } = true;

    public string ResolveLabel(EvaluationContext context) => LabelValue.Resolve(context) ?? Name;

    public bool ResolveVisible(EvaluationContext context) => VisibleValue.Resolve(context);

    /// <summary>Default label for a path: <c>CreatedAt</c> becomes <c>Created At</c>, and a
    /// dotted path is labelled from its last segment only.</summary>
    protected static string Humanize(string name)
    {
        if (name.Contains('.')) name = name[(name.LastIndexOf('.') + 1)..];

        var chars = new List<char>();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                chars.Add(' ');
            chars.Add(name[i]);
        }
        return new string(chars.ToArray());
    }
}
