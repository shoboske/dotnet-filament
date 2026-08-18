using System.Text;

namespace Fila.Panels.Resources;

/// <summary>Shared by Resource&lt;T&gt;'s default Slug and the CLI scaffolder's printed route,
/// so both agree on the naive pluralisation rule (handles -y/-s/-ch/-sh, nothing fancier).</summary>
internal static class ResourceNaming
{
    public static string ToSlug(string entityName) => ToKebabCase(Pluralize(entityName));

    public static string Pluralize(string word)
    {
        if (word.Length > 1 && word.EndsWith('y') && !IsVowel(word[^2]))
            return word[..^1] + "ies";

        if (word.EndsWith('s') || word.EndsWith("ch", StringComparison.Ordinal) ||
            word.EndsWith("sh", StringComparison.Ordinal) || word.EndsWith('x'))
            return word + "es";

        return word + "s";
    }

    private static bool IsVowel(char c) => "aeiouAEIOU".Contains(c);

    public static string ToKebabCase(string pascal)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < pascal.Length; i++)
        {
            if (i > 0 && char.IsUpper(pascal[i]) && !char.IsUpper(pascal[i - 1]))
                sb.Append('-');
            sb.Append(char.ToLowerInvariant(pascal[i]));
        }
        return sb.ToString();
    }
}
