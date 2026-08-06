using Fila.Panels;
using Fila.Rendering;
using Fila.Resources;
using Fila.Tables;
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

        foreach (var resourceType in panel.ResourceTypes)
        {
            // Registered under both its concrete type (routing resolves resources by exact
            // type) and IResource (per spec §6.3), sharing one instance per scope.
            services.AddScoped(resourceType);
            services.AddScoped(typeof(IResource), sp => sp.GetRequiredService(resourceType));
        }

        return services;
    }

    public static IEndpointRouteBuilder MapFilaPanel(this IEndpointRouteBuilder endpoints)
    {
        var panel = endpoints.ServiceProvider.GetRequiredService<Panel>();
        var group = endpoints.MapGroup($"/{panel.Path}");

        if (panel.AuthorizationPolicy is not null)
            group.RequireAuthorization(panel.AuthorizationPolicy);

        var slugs = ResolveSlugsAtStartup(endpoints.ServiceProvider, panel);

        group.MapGet("/", () =>
        {
            var first = slugs.Count > 0 ? slugs[0].Slug : null;
            return first is null
                ? Results.NotFound("No resources registered on this panel.")
                : Results.Redirect($"/{panel.Path}/{first}");
        });

        foreach (var (resourceType, slug) in slugs)
        {
            group.MapGet($"/{slug}", (HttpContext ctx, CancellationToken ct) =>
                HandleListAsync(ctx, panel, resourceType, ct));
        }

        return endpoints;
    }

    private static List<(Type ResourceType, string Slug)> ResolveSlugsAtStartup(IServiceProvider services, Panel panel)
    {
        var slugs = new List<(Type, string)>();

        using var scope = services.CreateScope();
        foreach (var resourceType in panel.ResourceTypes)
        {
            var resource = (IResource)scope.ServiceProvider.GetRequiredService(resourceType);
            slugs.Add((resourceType, resource.Slug));
        }

        var collisions = slugs
            .GroupBy(s => s.Item2)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (collisions.Count > 0)
        {
            throw new InvalidOperationException(
                $"Multiple resources on panel '{panel.Path}' resolve to the same slug: " +
                $"{string.Join(", ", collisions)}. Override the Slug property on one of them.");
        }

        return slugs;
    }

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
