namespace Fila.Support;

/// <summary>Shared text helpers for component defaults. Schema components and table columns
/// each keep their own label/name settings — as Filament's do, its tables package carrying
/// its own HasLabel and HasName concerns rather than reusing the schemas ones — but the rule
/// for turning a path into a default label is one rule, not two.</summary>
public static class ComponentText
{
    /// <summary>Default label for a path: <c>CreatedAt</c> becomes <c>Created At</c>, and a
    /// dotted path is labelled from its last segment only.</summary>
    public static string Humanize(string name)
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
