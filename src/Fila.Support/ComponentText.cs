namespace Fila.Support;

/// <summary>Shared text helpers for component defaults. Schema components and table columns
/// each keep their own label/name settings — as Filament's do, its tables package carrying
/// its own HasLabel and HasName concerns rather than reusing the schemas ones — but the rule
/// for turning a path into a default label is one rule, not two.</summary>
public static class ComponentText
{
    /// <summary>Default label for a path: <c>CreatedAt</c> becomes <c>Created at</c> — sentence
    /// case, matching Filament's own HasLabel::getLabel() default
    /// (<c>Str::kebab($name)-&gt;replace(['-', '_'], ' ')-&gt;ucfirst()</c>, which lowercases the
    /// whole name and capitalizes only the first letter, not every word). A dotted path is
    /// labelled from its last segment only.</summary>
    public static string Humanize(string name)
    {
        if (name.Contains('.')) name = name[(name.LastIndexOf('.') + 1)..];

        var chars = new List<char>();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                chars.Add(' ');
            chars.Add(char.ToLowerInvariant(name[i]));
        }
        if (chars.Count > 0) chars[0] = char.ToUpperInvariant(chars[0]);
        return new string(chars.ToArray());
    }
}
