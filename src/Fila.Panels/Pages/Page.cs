using Fila.Panels.Resources;
using Fila.Support;

namespace Fila.Panels.Pages;

/// <summary>A free-standing admin screen not bound to any resource — Filament's Pages\Page,
/// trimmed to what routing needs: a slug, a title, an icon and a view. Filament's own Page is
/// enormous (clusters, sub-navigation, header actions, its own route registration); this is
/// deliberately just the "a page is a route plus a view" core of it — a resource-bound screen
/// (List/Edit/View) stays a framework-owned thing a resource author never subclasses, the same
/// as before this existed.
///
/// No Livewire-style lifecycle and no form/actions machinery of its own. A page reaching for
/// interactivity uses Fila.Actions/htmx directly inside its own view, the same tools any other
/// Fila view already uses — see samples/Demo's SettingsPage.</summary>
public abstract class Page
{
    /// <summary>The route segment this page mounts at — "/{panel}/{Slug}". Defaults to the
    /// class name's kebab-case, minus a trailing "Page" — "SettingsPage" becomes "settings",
    /// not "settings-page" — the same way Resource&lt;TEntity&gt;.Slug derives from the entity
    /// name rather than "{Entity}Resource".</summary>
    public virtual string Slug => ResourceNaming.ToKebabCase(TrimPageSuffix(GetType().Name));

    private static string TrimPageSuffix(string typeName) =>
        typeName.EndsWith("Page", StringComparison.Ordinal) && typeName.Length > "Page".Length
            ? typeName[..^"Page".Length]
            : typeName;

    /// <summary>The sidebar/heading label. Defaults to the humanized Slug.</summary>
    public virtual string Title => ResourceNaming.Humanize(Slug);

    public virtual string? NavigationIcon => null;

    /// <summary>The Razor view this page renders, e.g. "~/Views/Fila/Pages/_Settings.cshtml" —
    /// resolved the same way every other Fila view is, via ViewRenderer. Rendered with this
    /// page instance as its model (wrapped in FilaPageViewModel alongside the panel and the
    /// usual evaluation context), so the view reads whatever the concrete page class exposes.</summary>
    public abstract string ViewPath { get; }
}
