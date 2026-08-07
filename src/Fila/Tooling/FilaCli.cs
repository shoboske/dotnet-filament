using System.Reflection;
using Fila.Panels;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
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

            default:
                Fail($"Unknown fila command: '{rest[0]}'.");
                PrintUsage();
                break;
        }

        return true;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""

            Usage:
              dotnet run -- fila make:panel <Name>
              dotnet run -- fila make:resource <Entity> [--context <Name>] [--force]

            """);
    }

    // ---- make:panel ----------------------------------------------------

    private static void MakePanel(WebApplication app, string[] args)
    {
        var (positional, flags) = ParseArgs(args);
        if (positional.Count == 0)
        {
            Fail("Usage: dotnet run -- fila make:panel <Name>");
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
            using Fila;
            using Fila.Panels;
            {{usingDbContextNamespace}}
            namespace {{rootNamespace}}.Fila;

            public static class {{name}}Panel
            {
                public static void Configure(PanelBuilder panel) => panel
                    .AtPath("{{name.ToLowerInvariant()}}")
                    .Brand("{{name}}")
                    {{dbContextClause}}
                    .DiscoverResources(typeof(Program).Assembly);
            }

            """;

        File.WriteAllText(path, source);

        Console.WriteLine();
        Console.WriteLine($"  Created  {relativePath}");
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
            Fail("Usage: dotnet run -- fila make:resource <Entity> [--context <Name>] [--force]");
            return;
        }

        var entityName = positional[0];
        var force = flags.Contains("force");
        var contextName = GetFlagValue(args, "--context");
        var rootNamespace = app.Environment.ApplicationName;

        using var scope = app.Services.CreateScope();

        var contextType = contextName is not null
            ? FindTypeByName(app, contextName)
            : GetPanelDbContextType(scope) ?? FindDbContextType(app);

        if (contextType is null)
        {
            Fail("No DbContext found. Pass --context <Name> or register one via .UseDbContext<T>() in AddFilaPanel.");
            return;
        }

        if (scope.ServiceProvider.GetService(contextType) is not DbContext db)
        {
            Fail($"Could not resolve '{contextType.Name}' from the service provider. Is it registered in Program.cs?");
            return;
        }

        var entityType = db.Model.GetEntityTypes()
            .FirstOrDefault(t =>
                string.Equals(t.ClrType.Name, entityName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.ClrType.FullName, entityName, StringComparison.OrdinalIgnoreCase));

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

        var panel = scope.ServiceProvider.GetService<Panel>();
        var panelPath = panel?.Path ?? "admin";

        var result = Scaffolder.Scaffold(entityType, rootNamespace, panelPath);

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

    // ---- helpers ----------------------------------------------------

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

    private static Type? GetPanelDbContextType(IServiceScope scope) =>
        scope.ServiceProvider.GetService<Panel>()?.DbContextType;

    private static void Fail(string message)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine($"  {message}");
        Console.Error.WriteLine();
        Environment.ExitCode = 1;
    }
}
