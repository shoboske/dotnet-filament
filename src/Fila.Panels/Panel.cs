using System.Reflection;
using System.Security.Claims;
using Fila.Panels.Resources;
using Fila.Support;
using Fila.Widgets;
using Microsoft.EntityFrameworkCore;

namespace Fila.Panels;

/// <summary>One entry in the panel's navigation, resolved once at startup alongside route
/// registration — see FilaExtensions.MapFilaPanel.</summary>
public sealed record ResourceNavItem(string Slug, string Label, string? NavigationIcon);

public sealed class Panel
{
    internal Panel()
    {
    }

    public string Path { get; internal set; } = "admin";
    public string Brand { get; internal set; } = "Admin";
    public Type? DbContextType { get; internal set; }
    public string? AuthorizationPolicy { get; internal set; }
    public string? LogoutPath { get; internal set; }
    public List<Type> ResourceTypes { get; } = [];

    /// <summary>Widget types registered directly on this panel via .Widgets(...) — the
    /// dashboard's own widgets, as opposed to the ones each resource contributes.</summary>
    public List<Type> WidgetTypes { get; } = [];

    /// <summary>Overrides the default indigo accent for this panel — set via .PrimaryColor(...).
    /// A 7-character hex string ("#rrggbb"), or null to use Fila's default.</summary>
    public string? PrimaryColorHex { get; internal set; }

    /// <summary>True once .WithLogin(...) has been called — MapFilaPanel then owns the
    /// {path}/login and {path}/logout routes itself, the way a Filament panel owns its login
    /// page rather than making the host app build one.</summary>
    public bool LoginEnabled { get; internal set; }

    /// <summary>Host-supplied credential check. Fila owns the login page and routing; the host
    /// app only says whether a username/password pair is valid and, if so, who they are.
    /// Returning null means invalid credentials.</summary>
    public Func<string, string, CancellationToken, Task<ClaimsPrincipal?>>? Authenticate { get; internal set; }

    /// <summary>The auth scheme SignInAsync/SignOutAsync/RequireAuthorization target for this
    /// panel. Defaults to an auto-registered per-panel scheme ("Fila.{path}") so multiple
    /// panels never share a session — override with .WithAuthenticationScheme(...) to point a
    /// panel at a scheme the host already owns (ASP.NET Core Identity, JWT, SSO, ...).</summary>
    public string? AuthenticationScheme { get; internal set; }

    /// <summary>False once .WithAuthenticationScheme(...) overrides AuthenticationScheme —
    /// Fila then assumes the host has already registered that scheme itself and skips its own
    /// AddCookie(...) registration for this panel.</summary>
    internal bool ManagesOwnAuthenticationScheme { get; set; } = true;

    /// <summary>Populated once by MapFilaPanel; empty before routes are mapped.</summary>
    public IReadOnlyList<ResourceNavItem> Navigation { get; internal set; } = [];

    /// <summary>Everything the dashboard draws: this panel's own WidgetTypes followed by each
    /// resource's GetWidgets(), in navigation order. Resolved once by MapFilaPanel alongside
    /// Navigation — a resource's contribution only exists on an instance, so it cannot be known
    /// at AddFilaPanel time. Empty before routes are mapped.</summary>
    public IReadOnlyList<Type> DashboardWidgetTypes { get; internal set; } = [];
}

public sealed class PanelBuilder
{
    private readonly Panel _panel = new();

    public PanelBuilder AtPath(string path)
    {
        _panel.Path = path.Trim('/');
        return this;
    }

    public PanelBuilder Brand(string brand)
    {
        _panel.Brand = brand;
        return this;
    }

    public PanelBuilder UseDbContext<TContext>() where TContext : DbContext
    {
        _panel.DbContextType = typeof(TContext);
        return this;
    }

    public PanelBuilder RequireAuthorization(string policy)
    {
        _panel.AuthorizationPolicy = policy;
        return this;
    }

    /// <summary>Overrides the default indigo accent with a per-panel color, the way a Filament
    /// panel's ->colors(['primary' => ...]) works. Accepts a 7-character hex string
    /// ("#4f46e5") — the hover shade and light/dark tint behind the active sidebar link are
    /// derived from it automatically.</summary>
    public PanelBuilder PrimaryColor(string hex)
    {
        if (!ColorMath.IsValidHex(hex))
            throw new ArgumentException(
                $"PrimaryColor expects a 7-character hex string like \"#4f46e5\", got \"{hex}\".", nameof(hex));

        _panel.PrimaryColorHex = hex;
        return this;
    }

    /// <summary>Renders a sign-out control in the sidebar for authenticated users, posting to
    /// this path. Only needed for a hand-rolled auth flow the host app builds itself — prefer
    /// .WithLogin(...) below, which wires this up automatically.</summary>
    public PanelBuilder WithLogoutPath(string path)
    {
        _panel.LogoutPath = path;
        return this;
    }

    /// <summary>Enables Fila's own login page at {path}/login, the way a Filament panel owns
    /// its login page rather than the host app building one. Fila auto-registers a cookie
    /// authentication scheme just for this panel — no .AddAuthentication(...).AddCookie(...)
    /// boilerplate needed in Program.cs, and no risk of two panels sharing one session. The
    /// host still owns what counts as valid credentials and who the signed-in principal is.
    /// Override the scheme with .WithAuthenticationScheme(...) if this panel should reuse
    /// auth the host already has configured instead.</summary>
    public PanelBuilder WithLogin(Func<string, string, CancellationToken, Task<ClaimsPrincipal?>> authenticate)
    {
        _panel.LoginEnabled = true;
        _panel.Authenticate = authenticate;
        return this;
    }

    /// <summary>Points this panel at an authentication scheme the host has already registered
    /// (ASP.NET Core Identity, JWT, SSO, an app-wide cookie scheme, ...) instead of Fila's
    /// auto-registered per-panel cookie scheme. Fila still owns the login page/routing if
    /// combined with .WithLogin(...); it just signs in/out against this scheme by name rather
    /// than registering its own.</summary>
    public PanelBuilder WithAuthenticationScheme(string schemeName)
    {
        _panel.AuthenticationScheme = schemeName;
        _panel.ManagesOwnAuthenticationScheme = false;
        return this;
    }

    /// <summary>Registers this panel's dashboard widgets, the way a Filament panel's
    /// ->widgets([...]) does. Passed as types rather than instances so a widget can take its
    /// own constructor dependencies — they are activated per request out of the request scope,
    /// so a widget may inject the app's DbContext, an IOptions&lt;T&gt;, anything else scoped.
    ///
    /// A resource contributes its own widgets separately, via Resource.GetWidgets(); those are
    /// appended after these. Widget.Sort orders the merged list.</summary>
    public PanelBuilder Widgets(params Type[] widgetTypes)
    {
        foreach (var type in widgetTypes)
        {
            if (!typeof(Widget).IsAssignableFrom(type) || type.IsAbstract)
                throw new ArgumentException(
                    $"'{type.Name}' is not a widget. .Widgets(...) takes non-abstract types deriving from Fila.Widgets.Widget.",
                    nameof(widgetTypes));
        }

        _panel.WidgetTypes.AddRange(widgetTypes);
        return this;
    }

    /// <summary>Scans the assembly's exported types for non-abstract IResource implementations.</summary>
    public PanelBuilder DiscoverResources(Assembly assembly)
    {
        var resourceTypes = assembly.GetExportedTypes()
            .Where(t => !t.IsAbstract && typeof(IResource).IsAssignableFrom(t));

        _panel.ResourceTypes.AddRange(resourceTypes);
        return this;
    }

    internal Panel Build()
    {
        if (_panel.DbContextType is null)
            throw new InvalidOperationException(
                "Panel is missing a DbContext. Call .UseDbContext<TContext>() in your AddFilaPanel configuration.");

        if (_panel.LoginEnabled && _panel.AuthenticationScheme is null)
            _panel.AuthenticationScheme = $"Fila.{_panel.Path}";

        return _panel;
    }
}
