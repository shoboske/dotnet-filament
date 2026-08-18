using Fila.Panels.Rendering;
using Fila.Panels.Resources;
using Fila.Tables;
using Fila.Forms;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Fila.Panels;

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

            // Mapped unconditionally even for list-only resources (BuildForm() is null) —
            // the handlers 404 in that case. Checking per-resource here would mean resolving
            // every resource from a scope just to decide route shape, for no real benefit.
            group.MapGet($"/{entry.Slug}/create", (HttpContext ctx, CancellationToken ct) =>
                HandleFormAsync(ctx, panel, entry.ResourceType, null, ct));

            group.MapPost($"/{entry.Slug}", (HttpContext ctx, CancellationToken ct) =>
                HandleSaveAsync(ctx, panel, entry.ResourceType, null, ct));

            group.MapGet($"/{entry.Slug}/{{id}}/edit", (HttpContext ctx, string id, CancellationToken ct) =>
                HandleFormAsync(ctx, panel, entry.ResourceType, id, ct));

            group.MapPost($"/{entry.Slug}/{{id}}", (HttpContext ctx, string id, CancellationToken ct) =>
                HandleSaveAsync(ctx, panel, entry.ResourceType, id, ct));

            group.MapGet($"/{entry.Slug}/{{id}}/confirm-delete", (HttpContext ctx, string id, CancellationToken ct) =>
                HandleDeleteConfirmAsync(ctx, panel, entry.ResourceType, id, ct));

            group.MapPost($"/{entry.Slug}/{{id}}/delete", (HttpContext ctx, string id, CancellationToken ct) =>
                HandleDeleteAsync(ctx, panel, entry.ResourceType, id, ct));
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
            Db = db,
        };

        var renderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
        var isHtmxRequest = ctx.Request.Headers.ContainsKey("HX-Request");
        var viewPath = isHtmxRequest ? "~/Views/Fila/_Table.cshtml" : "~/Views/Fila/List.cshtml";

        var html = await renderer.RenderAsync(ctx, viewPath, model);
        return Results.Content(html, "text/html");
    }

    // ---- create/edit/delete ----------------------------------------------------

    private static async Task<IResult> HandleFormAsync(HttpContext ctx, Panel panel, Type resourceType, string? id, CancellationToken ct)
    {
        var resource = (IResource)ctx.RequestServices.GetRequiredService(resourceType);
        var form = resource.BuildForm();
        if (form is null) return Results.NotFound();

        var db = (DbContext)ctx.RequestServices.GetRequiredService(panel.DbContextType!);
        var entity = id is null ? resource.CreateBlank() : await resource.FindAsync(db, id, ct);
        if (entity is null) return Results.NotFound();

        var model = new FilaFormViewModel
        {
            Panel = panel,
            Resource = resource,
            Form = form,
            Entity = entity,
            Db = db,
            Id = id,
            Errors = [],
        };

        var renderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
        var html = await renderer.RenderAsync(ctx, "~/Views/Fila/_Form.cshtml", model);

        ctx.Response.Headers["HX-Trigger"] = "fila-modal-open";
        return Results.Content(html, "text/html");
    }

    private static async Task<IResult> HandleSaveAsync(HttpContext ctx, Panel panel, Type resourceType, string? id, CancellationToken ct)
    {
        var resource = (IResource)ctx.RequestServices.GetRequiredService(resourceType);
        var form = resource.BuildForm();
        if (form is null) return Results.NotFound();

        var db = (DbContext)ctx.RequestServices.GetRequiredService(panel.DbContextType!);
        var isNew = id is null;
        var entity = isNew ? resource.CreateBlank() : await resource.FindAsync(db, id!, ct);
        if (entity is null) return Results.NotFound();

        var submitted = await ctx.Request.ReadFormAsync(ct);
        var errors = new List<(string Path, string Message)>();

        foreach (var field in form.Fields)
        {
            var raw = submitted[field.Path].ToString();

            if (field.IsRequired && string.IsNullOrWhiteSpace(raw))
            {
                errors.Add((field.Path, $"{field.Label} is required."));
                continue;
            }

            try
            {
                field.SetValue(entity, FieldBinding.Parse(field.ValueType, raw));
            }
            catch
            {
                errors.Add((field.Path, $"{field.Label} is invalid."));
            }
        }

        if (errors.Count > 0)
        {
            var formModel = new FilaFormViewModel
            {
                Panel = panel,
                Resource = resource,
                Form = form,
                Entity = entity,
                Db = db,
                Id = id,
                Errors = errors,
            };

            var formRenderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
            var formHtml = await formRenderer.RenderAsync(ctx, "~/Views/Fila/_Form.cshtml", formModel);

            // Redirect this response into the modal instead of the table — the form's own
            // hx-target/hx-swap point at #fila-table for the success path, so a validation
            // failure has to override that per-response rather than change the form's markup.
            ctx.Response.Headers["HX-Retarget"] = "#fila-modal-body";
            ctx.Response.Headers["HX-Reswap"] = "innerHTML";
            return Results.Content(formHtml, "text/html");
        }

        await resource.SaveAsync(db, entity, isNew, ct);
        // Matches Filament's own copy exactly: packages/actions/resources/lang/en/{create,edit}.php
        // — CreateAction's notification title is "Created", EditAction's is "Saved".
        SetCloseAndNotifyTrigger(ctx, isNew ? "Created" : "Saved", "success");

        return await RenderTableAsync(ctx, panel, resource, db, ct);
    }

    private static async Task<IResult> HandleDeleteConfirmAsync(HttpContext ctx, Panel panel, Type resourceType, string id, CancellationToken ct)
    {
        var resource = (IResource)ctx.RequestServices.GetRequiredService(resourceType);
        var db = (DbContext)ctx.RequestServices.GetRequiredService(panel.DbContextType!);

        var entity = await resource.FindAsync(db, id, ct);
        if (entity is null) return Results.NotFound();

        var model = new FilaDeleteConfirmViewModel
        {
            Panel = panel,
            Resource = resource,
            Id = id,
            Label = Singularize(Humanize(resource.Slug)),
        };

        var renderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
        var html = await renderer.RenderAsync(ctx, "~/Views/Fila/_DeleteConfirm.cshtml", model);

        ctx.Response.Headers["HX-Trigger"] = "fila-modal-open";
        return Results.Content(html, "text/html");
    }

    private static async Task<IResult> HandleDeleteAsync(HttpContext ctx, Panel panel, Type resourceType, string id, CancellationToken ct)
    {
        var resource = (IResource)ctx.RequestServices.GetRequiredService(resourceType);
        var db = (DbContext)ctx.RequestServices.GetRequiredService(panel.DbContextType!);

        var entity = await resource.FindAsync(db, id, ct);
        if (entity is not null)
            await resource.DeleteAsync(db, entity, ct);

        // Matches Filament's DeleteAction: packages/actions/resources/lang/en/delete.php,
        // notifications.deleted.title = "Deleted".
        SetCloseAndNotifyTrigger(ctx, "Deleted", "danger");

        return await RenderTableAsync(ctx, panel, resource, db, ct);
    }

    /// <summary>Tells the client to close the CRUD modal and pop a toast, in one response —
    /// htmx parses a JSON-object HX-Trigger header as {eventName: detail} and dispatches a
    /// CustomEvent per key, so both signals ride the same header instead of needing two.</summary>
    private static void SetCloseAndNotifyTrigger(HttpContext ctx, string title, string color)
    {
        ctx.Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["fila-modal-close"] = true,
            ["fila-notify"] = new { title, color },
        });
    }

    private static string Singularize(string label) =>
        label.EndsWith('s') && !label.EndsWith("ss") ? label[..^1] : label;

    private static async Task<IResult> RenderTableAsync(HttpContext ctx, Panel panel, IResource resource, DbContext db, CancellationToken ct)
    {
        // hx-include on the Delete button normally carries #fila-table-state along as a form
        // body, but delete doesn't strictly need it — falling back to defaults here rather
        // than throwing keeps a bare POST (no body at all) working instead of 500ing.
        var table = resource.BuildTable();
        var query = ctx.Request.HasFormContentType
            ? TableQuery.FromForm(await ctx.Request.ReadFormAsync(ct))
            : new TableQuery(null, null, "asc", 1);
        var paged = await resource.ListAsync(db, table, query, ct);

        var model = new FilaListViewModel
        {
            Panel = panel,
            Resource = resource,
            Table = table,
            Query = query,
            Paged = paged,
            Db = db,
        };

        var renderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
        var html = await renderer.RenderAsync(ctx, "~/Views/Fila/_Table.cshtml", model);
        return Results.Content(html, "text/html");
    }
}
