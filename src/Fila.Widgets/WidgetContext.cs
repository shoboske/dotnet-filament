using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Fila.Widgets;

/// <summary>Everything a widget is handed when it computes its data. Deliberately narrow: a
/// widget reads from the panel's DbContext and may branch on who is signed in, and that is all
/// it gets — the widget contract stays independent of ASP.NET Core routing, so Fila.Widgets
/// never has to reference Fila.Panels (which references it).
///
/// The panel resolves one of these per dashboard request and passes the same instance to every
/// widget on the page, so all the widgets read a consistent snapshot of one DbContext.</summary>
public sealed class WidgetContext
{
    /// <summary>The panel's DbContext for this request — the same scoped instance the resource
    /// list pages read from.</summary>
    public required DbContext Db { get; init; }

    /// <summary>The signed-in principal, for a widget whose numbers depend on who is asking.
    /// Null on a panel with no authentication configured.</summary>
    public ClaimsPrincipal? User { get; init; }

    public CancellationToken Ct { get; init; }
}
