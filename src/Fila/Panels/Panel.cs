using System.Reflection;
using System.Security.Claims;
using Fila.Resources;
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

    /// <summary>True once .WithLogin(...) has been called — MapFilaPanel then owns the
    /// {path}/login and {path}/logout routes itself, the way a Filament panel owns its login
    /// page rather than making the host app build one.</summary>
    public bool LoginEnabled { get; internal set; }

    /// <summary>Host-supplied credential check. Fila owns the login page and routing; the host
    /// app only says whether a username/password pair is valid and, if so, who they are.
    /// Returning null means invalid credentials.</summary>
    public Func<string, string, CancellationToken, Task<ClaimsPrincipal?>>? Authenticate { get; internal set; }

    /// <summary>Populated once by MapFilaPanel; empty before routes are mapped.</summary>
    public IReadOnlyList<ResourceNavItem> Navigation { get; internal set; } = [];
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

    /// <summary>Renders a sign-out control in the sidebar for authenticated users, posting to
    /// this path. Only needed for a hand-rolled auth flow the host app builds itself — prefer
    /// .WithLogin(...) below, which wires this up automatically.</summary>
    public PanelBuilder WithLogoutPath(string path)
    {
        _panel.LogoutPath = path;
        return this;
    }

    /// <summary>Enables Fila's own login page at {path}/login, the way a Filament panel owns
    /// its login page rather than the host app building one. The host still owns what counts
    /// as valid credentials and who the signed-in principal is — Fila only owns the page, the
    /// routing, and calling SignInAsync/SignOutAsync against whatever default authentication
    /// scheme the host has configured (e.g. via .AddAuthentication(...).AddCookie(...)).</summary>
    public PanelBuilder WithLogin(Func<string, string, CancellationToken, Task<ClaimsPrincipal?>> authenticate)
    {
        _panel.LoginEnabled = true;
        _panel.Authenticate = authenticate;
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

        return _panel;
    }
}
