namespace Fila.Support;

/// <summary>Small built-in icon set for Resource.NavigationIcon, referenced by name (e.g.
/// "shopping-cart"). Inlined as SVG rather than pulling an icon font/CDN — keeps the
/// dependency list at zero per spec §5. Unknown names fall back to a generic document icon.</summary>
public static class IconRegistry
{
    private const string Wrapper =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" class="fi-icon" aria-hidden="true">{0}</svg>""";

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
        ["sun"] =
            """<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M2 12h2M20 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4"/>""",
        ["moon"] =
            """<path d="M20 14.5A8.5 8.5 0 0 1 9.5 4a8.5 8.5 0 1 0 10.5 10.5z"/>""",
        ["system"] =
            """<rect x="3" y="4" width="18" height="12" rx="1.5"/><path d="M8 20h8M12 16v4"/>""",
        ["x"] =
            """<path d="M18 6 6 18M6 6l12 12"/>""",
        ["pencil"] =
            """<path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z"/>""",
        ["trash"] =
            """<path d="M4 7h16M9 7V5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2M6 7l1 13a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1l1-13"/><path d="M10 11v6M14 11v6"/>""",
        ["plus"] =
            """<path d="M12 5v14M5 12h14"/>""",
        ["chevron-up"] =
            """<path d="m6 15 6-6 6 6"/>""",
        ["chevron-right"] =
            """<path d="m9 6 6 6-6 6"/>""",
        ["logout"] =
            """<path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"/><path d="M10 17l5-5-5-5"/><path d="M15 12H3"/>""",
        ["inbox"] =
            """<path d="M4 12h4l1.5 3h5L16 12h4"/><path d="M4 12 5.5 5A2 2 0 0 1 7.5 3.5h9a2 2 0 0 1 2 1.5L20 12v6a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 4 18v-6z"/>""",
        ["check-circle"] =
            """<circle cx="12" cy="12" r="9"/><path d="m8.5 12.5 2.5 2.5 4.5-5"/>""",
        ["x-circle"] =
            """<circle cx="12" cy="12" r="9"/><path d="m9.5 9.5 5 5m0-5-5 5"/>""",
        ["copy"] =
            """<rect x="9" y="9" width="12" height="12" rx="2"/><path d="M5 15V5a2 2 0 0 1 2-2h10"/>""",
        ["eye"] =
            """<path d="M2.5 12S6 5 12 5s9.5 7 9.5 7-3.5 7-9.5 7-9.5-7-9.5-7z"/><circle cx="12" cy="12" r="3"/>""",
        ["dots-vertical"] =
            """<circle cx="12" cy="5" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="12" cy="19" r="1"/>""",
        ["restore"] =
            """<path d="M3 12a9 9 0 1 0 3-6.7"/><path d="M3 4v5h5"/>""",
    };

    public static string Render(string? name)
    {
        var body = name is not null && Bodies.TryGetValue(name, out var match) ? match : DefaultBody;
        return string.Format(Wrapper, body);
    }
}
