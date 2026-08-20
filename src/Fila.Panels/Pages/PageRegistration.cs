namespace Fila.Panels.Pages;

/// <summary>A page named for later activation — mirrors Fila.Widgets.WidgetRegistration for the
/// same reason: pages are declared at AddFilaPanel time via PanelBuilder.Pages(...), but only
/// activated out of the request scope per page request, so a page can take its own constructor
/// dependencies.</summary>
public sealed class PageRegistration
{
    private PageRegistration(Type pageType) => PageType = pageType;

    /// <summary>Registers <typeparamref name="TPage"/>. The constraint is the whole point:
    /// <c>PageRegistration.Of&lt;NotAPage&gt;()</c> is a compile error.</summary>
    public static PageRegistration Of<TPage>() where TPage : Page => new(typeof(TPage));

    /// <summary>The registered page's type. Guaranteed to derive from <see cref="Page"/>.</summary>
    public Type PageType { get; }
}
