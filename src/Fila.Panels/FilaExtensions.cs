using Fila.Actions;
using Fila.Panels.Rendering;
using Fila.Panels.Resources;
using Fila.Tables;
using Fila.Forms;
using Fila.Support;
using Microsoft.AspNetCore.Authentication;
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
    private static readonly IReadOnlyDictionary<string, string?> EmptyState = new Dictionary<string, string?>();

    public static IServiceCollection AddFilaPanel(this IServiceCollection services, Action<PanelBuilder> configure)
    {
        var builder = new PanelBuilder();
        configure(builder);
        var panel = builder.Build();

        services.AddSingleton(panel);
        services.AddSingleton<ComponentViewRegistry>();
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

            // One generic mount/submit route pair per action instead of one MapGet/MapPost pair
            // per CRUD verb — Create/Edit/Delete are Action instances now (see
            // Fila.Panels.Actions), and a resource author's own custom action rides the exact
            // same two routes. The header-action pair (no {id}) is what a record-less action
            // like Create mounts at; the row-action pair is what every other built-in and any
            // per-record custom action mounts at.
            group.MapGet($"/{entry.Slug}/actions/{{name}}", (HttpContext ctx, string name, CancellationToken ct) =>
                HandleActionMountAsync(ctx, panel, entry.ResourceType, null, name, ct));

            group.MapPost($"/{entry.Slug}/actions/{{name}}", (HttpContext ctx, string name, CancellationToken ct) =>
                HandleActionExecuteAsync(ctx, panel, entry.ResourceType, null, name, ct));

            group.MapGet($"/{entry.Slug}/{{id}}/actions/{{name}}", (HttpContext ctx, string id, string name, CancellationToken ct) =>
                HandleActionMountAsync(ctx, panel, entry.ResourceType, id, name, ct));

            group.MapPost($"/{entry.Slug}/{{id}}/actions/{{name}}", (HttpContext ctx, string id, string name, CancellationToken ct) =>
                HandleActionExecuteAsync(ctx, panel, entry.ResourceType, id, name, ct));

            group.MapPost($"/{entry.Slug}/bulk-actions/{{name}}", (HttpContext ctx, string name, CancellationToken ct) =>
                HandleBulkActionExecuteAsync(ctx, panel, entry.ResourceType, name, ct));
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
            entries.Add((resourceType, resource.Slug, ResourceNaming.Humanize(resource.Slug), resource.NavigationIcon));
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
            Evaluation = new EvaluationContext { Db = db, User = ctx.User },
        };

        var renderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
        var isHtmxRequest = ctx.Request.Headers.ContainsKey("HX-Request");
        var viewPath = isHtmxRequest ? "~/Views/Fila/_Table.cshtml" : "~/Views/Fila/List.cshtml";

        var html = await renderer.RenderAsync(ctx, viewPath, model);
        return Results.Content(html, "text/html");
    }

    // ---- actions (header actions like Create, and per-record row actions) -----------------

    /// <summary>The generic GET side of one action: resolves the action and its record, then
    /// renders whichever mount UI the action declares — its form (Create/Edit/View/a custom
    /// schema-carrying action) or a confirmation step (Delete/Replicate/a custom
    /// confirmation-only action). An action with neither has nothing to mount and 404s here;
    /// it only ever runs via a direct POST.</summary>
    private static async Task<IResult> HandleActionMountAsync(
        HttpContext ctx, Panel panel, Type resourceType, string? id, string actionName, CancellationToken ct)
    {
        var resource = (IResource)ctx.RequestServices.GetRequiredService(resourceType);
        var db = (DbContext)ctx.RequestServices.GetRequiredService(panel.DbContextType!);

        var action = ResolveAction(resource, id, actionName);
        if (action is null) return Results.NotFound();

        var entity = await ResolveRecordAsync(resource, db, action, id, ct);
        if (entity is null) return Results.NotFound();

        var renderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();

        if (action.SchemaFactory is not null)
        {
            var form = action.SchemaFactory();
            var evaluation = EvaluationContextFor(ctx, db, entity, StateOf(form, entity));

            var model = new FilaActionFormViewModel
            {
                Panel = panel,
                Resource = resource,
                Action = action,
                Form = form,
                Entity = entity,
                Db = db,
                Evaluation = evaluation,
                Id = id,
                Errors = [],
            };

            var html = await renderer.RenderAsync(ctx, "~/Views/Fila/_ActionForm.cshtml", model);
            ctx.Response.Headers["HX-Trigger"] = "fila-modal-open";
            return Results.Content(html, "text/html");
        }

        if (action.RequiresConfirmationFlag)
        {
            var evaluation = EvaluationContextFor(ctx, db, entity, EmptyState);

            var model = new FilaActionConfirmViewModel
            {
                Panel = panel,
                Resource = resource,
                Action = action,
                Evaluation = evaluation,
                Id = id,
            };

            var html = await renderer.RenderAsync(ctx, "~/Views/Fila/_ActionConfirm.cshtml", model);
            ctx.Response.Headers["HX-Trigger"] = "fila-modal-open";
            return Results.Content(html, "text/html");
        }

        // No schema, no confirmation — nothing to mount; the row/header button posts directly.
        return Results.NotFound();
    }

    /// <summary>The generic POST side: resolves the action and its record, validates and binds
    /// a schema's submitted values onto the record (re-rendering the form with errors on
    /// failure, exactly like the old HandleSaveAsync), runs the action's Handle callback, then
    /// applies its notification and re-renders the table.</summary>
    private static async Task<IResult> HandleActionExecuteAsync(
        HttpContext ctx, Panel panel, Type resourceType, string? id, string actionName, CancellationToken ct)
    {
        var resource = (IResource)ctx.RequestServices.GetRequiredService(resourceType);
        var db = (DbContext)ctx.RequestServices.GetRequiredService(panel.DbContextType!);

        var action = ResolveAction(resource, id, actionName);
        if (action is null) return Results.NotFound();

        var entity = await ResolveRecordAsync(resource, db, action, id, ct);
        if (entity is null) return Results.NotFound();

        var formData = EmptyState;

        if (action.SchemaFactory is not null && !action.ReadOnlyFlag)
        {
            var form = action.SchemaFactory();
            var submitted = await ctx.Request.ReadFormAsync(ct);
            var state = form.Fields.ToDictionary(f => f.Path, f => (string?)submitted[f.Path].ToString());
            formData = state;

            var evaluation = EvaluationContextFor(ctx, db, entity, state);
            var errors = new List<(string Path, string Message)>();

            // A hidden field was never rendered, so nothing was submitted for it — validating
            // it would fail the form on a control the user cannot see.
            foreach (var field in form.Fields.Where(f => f.ResolveVisible(evaluation)))
            {
                var raw = submitted[field.Path].ToString();

                if (field.ResolveRequired(evaluation) && string.IsNullOrWhiteSpace(raw))
                {
                    errors.Add((field.Path, $"{field.ResolveLabel(evaluation)} is required."));
                    continue;
                }

                try
                {
                    field.SetValue(entity, FieldBinding.Parse(field.ValueType, raw));
                }
                catch
                {
                    errors.Add((field.Path, $"{field.ResolveLabel(evaluation)} is invalid."));
                }
            }

            if (errors.Count > 0)
            {
                var formModel = new FilaActionFormViewModel
                {
                    Panel = panel,
                    Resource = resource,
                    Action = action,
                    Form = form,
                    Entity = entity,
                    Db = db,
                    Evaluation = evaluation,
                    Id = id,
                    Errors = errors,
                };

                var formRenderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
                var formHtml = await formRenderer.RenderAsync(ctx, "~/Views/Fila/_ActionForm.cshtml", formModel);

                // Redirect this response into the modal instead of the table — the form's own
                // hx-target/hx-swap point at #fila-table for the success path, so a validation
                // failure has to override that per-response rather than change the form's markup.
                ctx.Response.Headers["HX-Retarget"] = "#fila-modal-body";
                ctx.Response.Headers["HX-Reswap"] = "innerHTML";
                return Results.Content(formHtml, "text/html");
            }
        }

        if (action.HandleCallback is not null)
        {
            var actionContext = new ActionContext { HttpContext = ctx, Db = db, Record = entity, FormData = formData, Ct = ct };
            await action.HandleCallback(actionContext);
        }

        if (action.Notification is { } notification)
            SetCloseAndNotifyTrigger(ctx, notification.Title, notification.Color);
        else
            ctx.Response.Headers["HX-Trigger"] = "fila-modal-close";

        return await RenderTableAsync(ctx, panel, resource, db, ct);
    }

    private static async Task<IResult> HandleBulkActionExecuteAsync(
        HttpContext ctx, Panel panel, Type resourceType, string actionName, CancellationToken ct)
    {
        var resource = (IResource)ctx.RequestServices.GetRequiredService(resourceType);
        var db = (DbContext)ctx.RequestServices.GetRequiredService(panel.DbContextType!);

        var action = resource.BuildTable().BulkActions.FirstOrDefault(a => a.Name == actionName);
        if (action is null) return Results.NotFound();

        var submitted = await ctx.Request.ReadFormAsync(ct);
        var records = new List<object>();

        foreach (var rawId in submitted["ids"])
        {
            if (string.IsNullOrEmpty(rawId)) continue;
            var entity = await resource.FindAsync(db, rawId, ct);
            if (entity is not null) records.Add(entity);
        }

        if (action.HandleCallback is not null)
        {
            var bulkContext = new BulkActionContext { HttpContext = ctx, Db = db, Records = records, Ct = ct };
            await action.HandleCallback(bulkContext);
        }

        if (action.Notification is { } notification)
            SetNotifyTrigger(ctx, notification.Title, notification.Color);

        return await RenderTableAsync(ctx, panel, resource, db, ct);
    }

    /// <summary>Header actions (no id in the route) resolve against the resource's implicit
    /// action list — just Create, when the resource has a form. Row actions (an id is present)
    /// resolve against Table.RowActions, which is where Edit/Delete/any custom action lives —
    /// see Resource&lt;TEntity&gt;.BuildTable() for how that list gets its built-in default.</summary>
    private static Fila.Actions.Action? ResolveAction(IResource resource, string? id, string name)
    {
        var candidates = id is null
            ? HeaderActionsFor(resource)
            : resource.BuildTable().RowActions.SelectMany(a => a.Flatten());

        return candidates.FirstOrDefault(a => a.Name == name);
    }

    private static IEnumerable<Fila.Actions.Action> HeaderActionsFor(IResource resource) =>
        resource.CreateAction() is { } action ? [action] : [];

    private static Task<object?> ResolveRecordAsync(IResource resource, DbContext db, Fila.Actions.Action action, string? id, CancellationToken ct) =>
        action.RecordSource == ActionRecordSource.New
            ? Task.FromResult<object?>(resource.CreateBlank())
            : id is null ? Task.FromResult<object?>(null) : resource.FindAsync(db, id, ct);

    /// <summary>Tells the client to close the action modal and pop a toast, in one response —
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

    /// <summary>Same as above without the modal-close signal — a bulk action never opens Fila's
    /// modal (see BulkAction's doc comment), so there's nothing to tell the client to close.</summary>
    private static void SetNotifyTrigger(HttpContext ctx, string title, string color)
    {
        ctx.Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["fila-notify"] = new { title, color },
        });
    }

    /// <summary>The context a form's evaluated settings resolve against: the entity being
    /// edited plus whatever the other fields currently hold, so one field's configuration can
    /// depend on another's value.</summary>
    private static EvaluationContext EvaluationContextFor(
        HttpContext ctx, DbContext db, object entity, IReadOnlyDictionary<string, string?> state) =>
        new()
        {
            Db = db,
            Record = entity,
            User = ctx.User,
            State = state,
        };

    private static Dictionary<string, string?> StateOf(IForm form, object entity) =>
        form.Fields.ToDictionary(f => f.Path, f => (string?)FieldBinding.Format(f.GetValue(entity)));

    private static async Task<IResult> RenderTableAsync(HttpContext ctx, Panel panel, IResource resource, DbContext db, CancellationToken ct)
    {
        // hx-include on a confirm-only action normally carries #fila-table-state along as a
        // form body, but doesn't strictly need it — falling back to defaults here rather than
        // throwing keeps a bare POST (no body at all) working instead of 500ing.
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
            Evaluation = new EvaluationContext { Db = db, User = ctx.User },
        };

        var renderer = ctx.RequestServices.GetRequiredService<ViewRenderer>();
        var html = await renderer.RenderAsync(ctx, "~/Views/Fila/_Table.cshtml", model);
        return Results.Content(html, "text/html");
    }
}
