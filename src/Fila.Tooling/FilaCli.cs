using System.Reflection;
using Fila.Panels;
using Fila.Tooling.FileGenerators;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Fila.Tooling;

/// <summary>Filament uses Artisan; .NET has no equivalent host. Instead this intercepts `args`
/// inside the user's own app, where the DbContext is already registered and the model already
/// built. See spec §7.</summary>
public static class FilaCli
{
    public static async Task<bool> RunFilaCommandsAsync(this WebApplication app, string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "fila", StringComparison.OrdinalIgnoreCase))
            return false;

        var rest = args[1..];
        if (rest.Length == 0)
        {
            PrintUsage();
            return true;
        }

        switch (rest[0])
        {
            case "make:panel":
                MakePanel(app, rest[1..]);
                break;

            case "make:resource":
                await MakeResourceAsync(app, rest[1..]);
                break;

            case "make:page":
                MakePage(app, rest[1..]);
                break;

            case "make:widget":
                MakeWidget(app, rest[1..]);
                break;

            case "make:relation-manager":
                await MakeRelationManagerAsync(app, rest[1..]);
                break;

            case "make:action":
                MakeAction(app, rest[1..]);
                break;

            case "--help" or "-h" or "help":
                PrintUsage();
                break;

            case "--version":
                Console.WriteLine(Version);
                break;

            default:
                Fail($"Unknown fila command: '{rest[0]}'.");
                PrintUsage();
                break;
        }

        return true;
    }

    /// <summary>How the user actually reached this code, for the usage text to echo back.
    /// Running in-app it really is `dotnet run -- fila`, but the `dotnet fila` tool reaches the
    /// same switch by shelling out to exactly that, and printing the inner form there would tell
    /// the user to type a command they did not use — the same reason `dotnet ef` prints
    /// "dotnet ef" rather than the ef.dll invocation it expands to. The dispatcher sets this
    /// variable; anything else falls back to the in-app form.</summary>
    private static string Invocation =>
        Environment.GetEnvironmentVariable("FILA_CLI_INVOCATION") is { Length: > 0 } invocation
            ? invocation
            : "dotnet run -- fila";

    private static string Version =>
        typeof(FilaCli).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    private static void PrintUsage()
    {
        Console.WriteLine($"""

            Usage:
              {Invocation} make:panel <Name>
              {Invocation} make:resource <Entity> [--context <Name>] [--force]
              {Invocation} make:page <Name> [--force]
              {Invocation} make:widget <Name> [--force]
              {Invocation} make:relation-manager <Parent> <Related> [--context <Name>] [--force]
              {Invocation} make:action <Name> [--force]

            """);
    }

    // ---- make:panel ----------------------------------------------------

    private static void MakePanel(WebApplication app, string[] args)
    {
        var (positional, flags) = ParseArgs(args);
        if (positional.Count == 0)
        {
            Fail($"Usage: {Invocation} make:panel <Name>");
            return;
        }

        var name = positional[0];
        var force = flags.Contains("force");
        var rootNamespace = app.Environment.ApplicationName;

        var dir = Path.Combine(app.Environment.ContentRootPath, "Fila");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{name}Panel.cs");
        var relativePath = Path.GetRelativePath(app.Environment.ContentRootPath, path);

        if (File.Exists(path) && !force)
        {
            Fail($"'{relativePath}' already exists. Pass --force to overwrite.");
            return;
        }

        var dbContextType = FindDbContextType(app);
        var dbContextClause = dbContextType is not null
            ? $".UseDbContext<{dbContextType.Name}>()"
            : ".UseDbContext<AppDb>() // TODO: replace with your DbContext";

        var dbContextNamespace = dbContextType?.Namespace;
        var usingDbContextNamespace = dbContextNamespace is null || dbContextNamespace == $"{rootNamespace}.Fila"
            ? string.Empty
            : $"using {dbContextNamespace};{Environment.NewLine}";

        var source = $$"""
            using Fila.Panels;
            using System.Security.Claims;
            {{usingDbContextNamespace}}
            namespace {{rootNamespace}}.Fila;

            public static class {{name}}Panel
            {
                public static void Configure(PanelBuilder panel) => panel
                    .AtPath("{{name.ToLowerInvariant()}}")
                    .Brand("{{name}}")
                    {{dbContextClause}}
                    // TODO: replace with your own credential check — this is a placeholder so
                    // the panel is usable immediately. Fila owns the login page and routing,
                    // and (by default) a dedicated auth scheme just for this panel; you only
                    // say whether a username/password pair is valid and who they are.
                    .WithLogin((username, password, ct) =>
                    {
                        var principal = username == "admin" && password == "admin"
                            ? new ClaimsPrincipal(new ClaimsIdentity(
                                [new Claim(ClaimTypes.Name, username)], "{{name}}"))
                            : null;

                        return Task.FromResult(principal);
                    })
                    .DiscoverResources(typeof(Program).Assembly);
            }

            """;

        File.WriteAllText(path, source);

        Console.WriteLine();
        Console.WriteLine($"  Created  {relativePath}");
        Console.WriteLine($"           Login enabled at /{name.ToLowerInvariant()}/login (default admin/admin — replace the credential check)");
        Console.WriteLine();
        Console.WriteLine("  Add to Program.cs:");
        Console.WriteLine();
        Console.WriteLine($"      builder.Services.AddFilaPanel({name}Panel.Configure);");
        Console.WriteLine("      app.MapFilaPanel();");
        Console.WriteLine();
    }

    // ---- make:resource ---------------------------------------------------

    private static async Task MakeResourceAsync(WebApplication app, string[] args)
    {
        var (positional, flags) = ParseArgs(args);
        if (positional.Count == 0)
        {
            Fail($"Usage: {Invocation} make:resource <Entity> [--context <Name>] [--force]");
            return;
        }

        var entityName = positional[0];
        var force = flags.Contains("force");
        var contextName = GetFlagValue(args, "--context");
        var rootNamespace = app.Environment.ApplicationName;

        using var scope = app.Services.CreateScope();

        if (ResolveDbContext(app, scope, contextName) is not { } resolved) return;
        var (contextType, db) = resolved;

        var entityType = FindEntityType(db, entityName);

        if (entityType is null)
        {
            var available = db.Model.GetEntityTypes().Select(t => t.ClrType.Name).OrderBy(n => n);
            Console.Error.WriteLine();
            Console.Error.WriteLine($"  Entity '{entityName}' not found in {contextType.Name}.");
            Console.Error.WriteLine();
            Console.Error.WriteLine($"  Available: {string.Join(", ", available)}");
            Console.Error.WriteLine();
            Environment.ExitCode = 1;
            return;
        }

        var owningPanel = scope.ServiceProvider.GetServices<Panel>()
            .FirstOrDefault(p => p.DbContextType == contextType);
        var panelPath = owningPanel?.Path ?? "admin";

        var result = ResourceClassGenerator.Generate(entityType, rootNamespace, panelPath);

        var dir = Path.Combine(app.Environment.ContentRootPath, "Fila", "Resources");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{entityType.ClrType.Name}Resource.cs");
        var relativePath = Path.GetRelativePath(app.Environment.ContentRootPath, path);

        if (File.Exists(path) && !force)
        {
            Fail($"'{relativePath}' already exists. Pass --force to overwrite.");
            return;
        }

        await File.WriteAllTextAsync(path, result.Source);

        Console.WriteLine();
        Console.WriteLine($"  Created  {relativePath}");
        Console.WriteLine($"           {result.ColumnCount} columns from {contextType.Name} → {entityType.ClrType.Name}");
        Console.WriteLine($"           Route: {result.RouteSlug}");
        Console.WriteLine();
        Console.WriteLine("  Restart the app to see it.");
        Console.WriteLine();
    }

    // ---- make:page ---------------------------------------------------

    private static void MakePage(WebApplication app, string[] args)
    {
        var (positional, flags) = ParseArgs(args);
        if (positional.Count == 0)
        {
            Fail($"Usage: {Invocation} make:page <Name>");
            return;
        }

        var name = positional[0];
        var force = flags.Contains("force");
        var rootNamespace = app.Environment.ApplicationName;

        var result = PageClassGenerator.Generate(name, rootNamespace);

        var classDir = Path.Combine(app.Environment.ContentRootPath, "Fila", "Pages");
        Directory.CreateDirectory(classDir);
        var classPath = Path.Combine(classDir, $"{name}Page.cs");
        var classRelativePath = Path.GetRelativePath(app.Environment.ContentRootPath, classPath);

        var viewPath = Path.Combine(app.Environment.ContentRootPath, result.ViewRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var viewRelativePath = Path.GetRelativePath(app.Environment.ContentRootPath, viewPath);

        if (!force && (File.Exists(classPath) || File.Exists(viewPath)))
        {
            Fail($"'{classRelativePath}' or '{viewRelativePath}' already exists. Pass --force to overwrite.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(viewPath)!);
        File.WriteAllText(classPath, result.ClassSource);
        File.WriteAllText(viewPath, result.ViewSource);

        Console.WriteLine();
        Console.WriteLine($"  Created  {classRelativePath}");
        Console.WriteLine($"           {viewRelativePath}");
        Console.WriteLine();
        Console.WriteLine("  Register it on a panel:");
        Console.WriteLine();
        Console.WriteLine($"      .Pages(PageRegistration.Of<{name}Page>())");
        Console.WriteLine();
    }

    // ---- make:widget ---------------------------------------------------

    private static void MakeWidget(WebApplication app, string[] args)
    {
        var (positional, flags) = ParseArgs(args);
        if (positional.Count == 0)
        {
            Fail($"Usage: {Invocation} make:widget <Name>");
            return;
        }

        var name = positional[0];
        var force = flags.Contains("force");
        var rootNamespace = app.Environment.ApplicationName;

        var result = WidgetClassGenerator.Generate(name, rootNamespace);

        var dir = Path.Combine(app.Environment.ContentRootPath, "Fila", "Widgets");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{name}Widget.cs");
        var relativePath = Path.GetRelativePath(app.Environment.ContentRootPath, path);

        if (File.Exists(path) && !force)
        {
            Fail($"'{relativePath}' already exists. Pass --force to overwrite.");
            return;
        }

        File.WriteAllText(path, result.Source);

        Console.WriteLine();
        Console.WriteLine($"  Created  {relativePath}");
        Console.WriteLine();
        Console.WriteLine("  Register it — on a panel:");
        Console.WriteLine();
        Console.WriteLine($"      .Widgets(WidgetRegistration.Of<{name}Widget>())");
        Console.WriteLine();
        Console.WriteLine("  or on a resource:");
        Console.WriteLine();
        Console.WriteLine($"      GetWidgets() => [WidgetRegistration.Of<{name}Widget>()]");
        Console.WriteLine();
    }

    // ---- make:relation-manager ---------------------------------------------------

    private static async Task MakeRelationManagerAsync(WebApplication app, string[] args)
    {
        var (positional, flags) = ParseArgs(args);
        if (positional.Count < 2)
        {
            Fail($"Usage: {Invocation} make:relation-manager <Parent> <Related> [--context <Name>] [--force]");
            return;
        }

        var parentName = positional[0];
        var relatedName = positional[1];
        var force = flags.Contains("force");
        var contextName = GetFlagValue(args, "--context");
        var rootNamespace = app.Environment.ApplicationName;

        using var scope = app.Services.CreateScope();

        if (ResolveDbContext(app, scope, contextName) is not { } resolved) return;
        var (contextType, db) = resolved;

        var parentType = FindEntityType(db, parentName);
        var relatedType = FindEntityType(db, relatedName);

        if (parentType is null || relatedType is null)
        {
            var missing = parentType is null ? parentName : relatedName;
            var available = db.Model.GetEntityTypes().Select(t => t.ClrType.Name).OrderBy(n => n);
            Console.Error.WriteLine();
            Console.Error.WriteLine($"  Entity '{missing}' not found in {contextType.Name}.");
            Console.Error.WriteLine();
            Console.Error.WriteLine($"  Available: {string.Join(", ", available)}");
            Console.Error.WriteLine();
            Environment.ExitCode = 1;
            return;
        }

        var result = RelationManagerClassGenerator.Generate(parentType, relatedType, rootNamespace);
        if (result is null)
        {
            Fail($"{relatedType.ClrType.Name} has no foreign key back to {parentType.ClrType.Name} — " +
                 "a relation manager needs a one-to-many relationship.");
            return;
        }

        var dir = Path.Combine(app.Environment.ContentRootPath, "Fila", "RelationManagers");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{relatedType.ClrType.Name}RelationManager.cs");
        var relativePath = Path.GetRelativePath(app.Environment.ContentRootPath, path);

        if (File.Exists(path) && !force)
        {
            Fail($"'{relativePath}' already exists. Pass --force to overwrite.");
            return;
        }

        await File.WriteAllTextAsync(path, result.Source);

        Console.WriteLine();
        Console.WriteLine($"  Created  {relativePath}");
        Console.WriteLine($"           {result.ColumnCount} columns from {contextType.Name} → {relatedType.ClrType.Name}");
        Console.WriteLine();
        Console.WriteLine($"  Add to {parentType.ClrType.Name}Resource:");
        Console.WriteLine();
        Console.WriteLine($"      GetRelations() => [RelationManagerRegistration.Of<{relatedType.ClrType.Name}RelationManager>()]");
        Console.WriteLine();
    }

    // ---- make:action ---------------------------------------------------

    private static void MakeAction(WebApplication app, string[] args)
    {
        var (positional, flags) = ParseArgs(args);
        if (positional.Count == 0)
        {
            Fail($"Usage: {Invocation} make:action <Name>");
            return;
        }

        var name = positional[0];
        var force = flags.Contains("force");
        var rootNamespace = app.Environment.ApplicationName;

        var result = ActionClassGenerator.Generate(name, rootNamespace);

        var dir = Path.Combine(app.Environment.ContentRootPath, "Fila", "Actions");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{name}Action.cs");
        var relativePath = Path.GetRelativePath(app.Environment.ContentRootPath, path);

        if (File.Exists(path) && !force)
        {
            Fail($"'{relativePath}' already exists. Pass --force to overwrite.");
            return;
        }

        File.WriteAllText(path, result.Source);

        Console.WriteLine();
        Console.WriteLine($"  Created  {relativePath}");
        Console.WriteLine();
        Console.WriteLine("  Add to a resource's .Actions(...) or .BulkActions(...):");
        Console.WriteLine();
        Console.WriteLine($"      {name}Action.Instance");
        Console.WriteLine();
    }

    // ---- helpers ----------------------------------------------------

    /// <summary>Shared by every make:* command that needs a DbContext + its EF model —
    /// make:resource and make:relation-manager. Prints its own failure message and returns null
    /// so a caller can just early-return on a null result.</summary>
    private static (Type ContextType, DbContext Db)? ResolveDbContext(WebApplication app, IServiceScope scope, string? contextName)
    {
        var contextType = contextName is not null
            ? FindTypeByName(app, contextName)
            : GetPanelDbContextType(scope) ?? FindDbContextType(app);

        if (contextType is null)
        {
            Fail("No DbContext found. Pass --context <Name> or register one via .UseDbContext<T>() in AddFilaPanel.");
            return null;
        }

        if (scope.ServiceProvider.GetService(contextType) is not DbContext db)
        {
            Fail($"Could not resolve '{contextType.Name}' from the service provider. Is it registered in Program.cs?");
            return null;
        }

        return (contextType, db);
    }

    private static IEntityType? FindEntityType(DbContext db, string name) =>
        db.Model.GetEntityTypes().FirstOrDefault(t =>
            string.Equals(t.ClrType.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.ClrType.FullName, name, StringComparison.OrdinalIgnoreCase));

    private static (List<string> Positional, HashSet<string> Flags) ParseArgs(string[] args)
    {
        var positional = new List<string>();
        var flags = new HashSet<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var flagName = arg[2..];
                flags.Add(flagName);
                // Skip an attached value for flags that take one (handled separately via GetFlagValue).
                if (flagName == "context" && i + 1 < args.Length) i++;
            }
            else
            {
                positional.Add(arg);
            }
        }

        return (positional, flags);
    }

    private static string? GetFlagValue(string[] args, string flag)
    {
        var index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static Type? FindDbContextType(WebApplication app) =>
        Assembly.GetEntryAssembly()?.GetTypes()
            .FirstOrDefault(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t));

    private static Type? FindTypeByName(WebApplication app, string name) =>
        Assembly.GetEntryAssembly()?.GetTypes()
            .FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Only useful when every registered panel agrees on one DbContext — with
    /// multiple panels on different DbContexts this is ambiguous without --context, so it
    /// returns null and callers fall back to scanning the assembly instead of guessing wrong.</summary>
    private static Type? GetPanelDbContextType(IServiceScope scope)
    {
        var distinctContextTypes = scope.ServiceProvider.GetServices<Panel>()
            .Select(p => p.DbContextType)
            .Distinct()
            .ToList();

        return distinctContextTypes.Count == 1 ? distinctContextTypes[0] : null;
    }

    private static void Fail(string message)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine($"  {message}");
        Console.Error.WriteLine();
        Environment.ExitCode = 1;
    }
}
