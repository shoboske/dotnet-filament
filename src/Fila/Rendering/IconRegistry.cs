namespace Fila.Rendering;

/// <summary>Small built-in icon set for Resource.NavigationIcon, referenced by name (e.g.
/// "shopping-cart"). Inlined as SVG rather than pulling an icon font/CDN — keeps the
/// dependency list at zero per spec §5. Unknown names fall back to a generic document icon.</summary>
public static class IconRegistry
{
    private const string Wrapper =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" class="fi-sidebar-icon" aria-hidden="true">{0}</svg>""";

    private const string DefaultBody =
        """<rect x="5" y="3" width="14" height="18" rx="2"/><path d="M9 8h6M9 12h6M9 16h4"/>""";

    private static readonly Dictionary<string, string> Bodies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["shopping-cart"] =
            """<circle cx="9" cy="20" r="1"/><circle cx="18" cy="20" r="1"/><path d="M3 4h2l2.2 11.2a2 2 0 0 0 2 1.6h7.6a2 2 0 0 0 2-1.6L21 8H6"/>""",
        ["users"] =
            """<path d="M17 20v-1a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v1"/><circle cx="10" cy="8" r="3.5"/><path d="M20 20v-1a3.5 3.5 0 0 0-2.5-3.4"/><path d="M15.5 4.6A3.5 3.5 0 0 1 18 8a3.5 3.5 0 0 1-2.5 3.4"/>""",
        ["user"] =
            """<circle cx="12" cy="8" r="3.5"/><path d="M5 20v-1a5 5 0 0 1 5-5h4a5 5 0 0 1 5 5v1"/>""",
        ["tag"] =
            """<path d="M11 3H4v7l10 10 7-7L11 3z"/><circle cx="7.5" cy="7.5" r="1.25"/>""",
        ["clipboard"] =
            """<rect x="6" y="4" width="12" height="17" rx="2"/><path d="M9 4V3a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v1"/><path d="M9 11h6M9 15h6"/>""",
        ["cog"] =
            """<circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1"/>""",
    };

    public static string Render(string? name)
    {
        var body = name is not null && Bodies.TryGetValue(name, out var match) ? match : DefaultBody;
        return string.Format(Wrapper, body);
    }
}
