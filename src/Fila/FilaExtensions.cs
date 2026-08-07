using Fila.Panels;
using Fila.Rendering;
using Fila.Resources;
using Fila.Tables;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fila;

public static class FilaExtensions
{
    public static IServiceCollection AddFilaPanel(this IServiceCollection services, Action<PanelBuilder> configure)
    {
        var builder = new PanelBuilder();
        configure(builder);
        var panel = builder.Build();

        services.AddSingleton(panel);
        services.AddScoped<ViewRenderer>();

        // Views need the MVC view engine + tempdata plumbing even though the host app
        // never registers MVC itself.
        services.AddControllersWithViews();

        if (panel.LoginEnabled)
        {
            // AddAuthentication()/AddAuthorization() register core services via TryAdd, so
            // calling them again here is safe even if the host app (or another panel) already
            // did. Only the cookie scheme itself is skipped when the host owns it via
            // .WithAuthenticationScheme(...) — Fila never touches a scheme it doesn't manage.
            services.AddAuthorization();

            var authBuilder = services.AddAuthentication();
            if (panel.ManagesOwnAuthenticationScheme)
            {
                authBuilder.AddCookie(panel.AuthenticationScheme!, options =>
                {
                    options.LoginPath = $"/{panel.Path}/login";
                    options.AccessDeniedPath = $"/{panel.Path}/login";
                    options.Cookie.Name = $".Fila.{panel.Path}";
                });
            }
        }

        foreach (var resourceType in panel.ResourceTypes)
        {
            // Registered under both its concrete type (routing resolves resources by exact
            // type) and IResource (per spec §6.3), sharing one instance per scope.
            services.AddScoped(resourceType);
            services.AddScoped(typeof(IResource), sp => sp.GetRequiredService(resourceType));
        }

        return services;
    }

    /// <summary>Maps every panel registered via AddFilaPanel — call this once regardless of how
    /// many panels you registered. (Panel is deliberately NOT resolved with
    /// GetRequiredService&lt;Panel&gt;() here: with more than one AddFilaPanel call, that always
    /// returns just the last-registered one, silently dropping every earlier panel's routes.)</summary>
    public static IEndpointRouteBuilder MapFilaPanel(this IEndpointRouteBuilder endpoints)
    {
        var panels = endpoints.ServiceProvider.GetServices<Panel>().ToList();

        var pathCollisions = panels
            .GroupBy(p => p.Path)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (pathCollisions.Count > 0)
        {
            throw new InvalidOperationException(
                $"Multiple panels registered at the same path: {string.Join(", ", pathCollisions)}. " +
                "Give each AddFilaPanel(...) call a distinct .AtPath(...).");
        }

        foreach (var panel in panels)
            MapPanelRoutes(endpoints, panel);

        return endpoints;
    }

    private static void MapPanelRoutes(IEndpointRouteBuilder endpoints, Panel panel)
    {
        // Login/logout are mapped directly on `endpoints`, never on the authorized `group`
        // below — putting them behind the same RequireAuthorization policy would send an
        // unauthenticated visit to /login straight back into a redirect loop.
        if (panel.LoginEnabled)
        {
            panel.LogoutPath ??= $"/{panel.Path}/logout";
            MapLoginRoutes(endpoints, panel);
        }

        var group = endpoints.MapGroup($"/{panel.Path}");

        if (panel.AuthorizationPolicy is not null)
            group.RequireAuthorization(panel.AuthorizationPolicy);
        else if (panel.LoginEnabled)
            group.RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = panel.AuthenticationScheme });

        var entries = ResolveNavigationAtStartup(endpoints.ServiceProvider, panel);
        panel.Navigation = entries.Select(e => new ResourceNavItem(e.Slug, e.Label, e.NavigationIcon)).ToList();

        group.MapGet("/", () =>
        {
            var first = entries.Count > 0 ? entries[0].Slug : null;
            return first is null
                ? Results.NotFound("No resources registered on this panel.")
                : Results.Redirect($"/{panel.Path}/{first}");
        });

        foreach (var entry in entries)
        {
            group.MapGet($"/{entry.Slug}", (HttpContext ctx, CancellationToken ct) =>
                HandleListAsync(ctx, panel, entry.ResourceType, ct));
        }
    }

    private static void MapLoginRoutes(IEndpointRouteBuilder endpoints, Panel panel)
    {
        endpoints.MapGet($"/{panel.Path}/login", async (HttpContext ctx) =>
        {
            var model = new LoginViewModel { Panel = panel, Error = ctx.Request.Query.ContainsKey("error") };
            var renderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
            var html = await renderer.RenderAsync(ctx, "~/Views/Fila/Login.cshtml", model);
            return Results.Content(html, "text/html");
        });

        endpoints.MapPost($"/{panel.Path}/login", async (HttpContext ctx, CancellationToken ct) =>
        {
            var form = await ctx.Request.ReadFormAsync(ct);
            var username = form["username"].ToString();
            var password = form["password"].ToString();

            var principal = await panel.Authenticate!(username, password, ct);
            if (principal is null)
                return Results.Redirect($"/{panel.Path}/login?error=true");

            await ctx.SignInAsync(panel.AuthenticationScheme!, principal);
            return Results.Redirect($"/{panel.Path}");
        });

        endpoints.MapPost($"/{panel.Path}/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(panel.AuthenticationScheme!);
            return Results.Redirect($"/{panel.Path}/login");
        });
    }

    private static List<(Type ResourceType, string Slug, string Label, string? NavigationIcon)> ResolveNavigationAtStartup(
        IServiceProvider services, Panel panel)
    {
        var entries = new List<(Type, string, string, string?)>();

        using var scope = services.CreateScope();
        foreach (var resourceType in panel.ResourceTypes)
        {
            var resource = (IResource)scope.ServiceProvider.GetRequiredService(resourceType);
            entries.Add((resourceType, resource.Slug, Humanize(resource.Slug), resource.NavigationIcon));
        }

        var collisions = entries
            .GroupBy(e => e.Item2)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (collisions.Count > 0)
        {
            throw new InvalidOperationException(
                $"Multiple resources on panel '{panel.Path}' resolve to the same slug: " +
                $"{string.Join(", ", collisions)}. Override the Slug property on one of them.");
        }

        return entries;
    }

    private static string Humanize(string slug) =>
        string.Join(' ', slug.Split('-').Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));

    private static async Task<IResult> HandleListAsync(HttpContext ctx, Panel panel, Type resourceType, CancellationToken ct)
    {
        var resource = (IResource)ctx.RequestServices.GetRequiredService(resourceType);
        var db = (DbContext)ctx.RequestServices.GetRequiredService(panel.DbContextType!);

        var table = resource.BuildTable();
        var query = TableQuery.FromRequest(ctx.Request.Query);
        var paged = await resource.ListAsync(db, table, query, ct);

        var model = new FilaListViewModel
        {
            Panel = panel,
            Resource = resource,
            Table = table,
            Query = query,
            Paged = paged,
        };

        var renderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
        var isHtmxRequest = ctx.Request.Headers.ContainsKey("HX-Request");
        var viewPath = isHtmxRequest ? "~/Views/Fila/_Table.cshtml" : "~/Views/Fila/List.cshtml";

        var html = await renderer.RenderAsync(ctx, viewPath, model);
        return Results.Content(html, "text/html");
    }
}
