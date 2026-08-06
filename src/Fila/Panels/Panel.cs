using System.Reflection;
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
    /// this path. Optional — omit it and the sidebar renders no auth UI at all, since Fila
    /// doesn't assume any particular auth scheme is configured.</summary>
    public PanelBuilder WithLogoutPath(string path)
    {
        _panel.LogoutPath = path;
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
