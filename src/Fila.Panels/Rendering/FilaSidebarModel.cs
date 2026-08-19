namespace Fila.Panels.Rendering;

/// <summary>What the shared sidebar partial needs — the panel it belongs to, and which nav
/// link is the current page. <paramref name="ActiveSlug"/> is a resource slug on a list page,
/// or null on the Dashboard, which is the panel root and has no slug of its own.</summary>
public sealed record FilaSidebarModel(Panel Panel, string? ActiveSlug);
