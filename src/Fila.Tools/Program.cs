using System.Diagnostics;

// `dotnet fila <command> [args...]` — a thin dispatcher, the same shape as `dotnet ef`.
// It does not load the target project's assembly itself (that would mean re-implementing
// dotnet-ef's AssemblyDependencyResolver/deps.json machinery just to get a DbContext that's
// already fully configured by the target app's own Program.cs). Instead it shells out to
// `dotnet run --project <csproj> -- fila <args>`, which reuses the in-process interception
// in Fila.Tooling.FilaCli — the target app builds its own DbContext exactly as it would to
// serve a request, so nothing about DI configuration needs to be guessed or duplicated here.

// Help and version are answered here rather than forwarded, so they work from anywhere the
// way `dotnet ef --help` does. Forwarding them would make `dotnet fila --help` fail outright
// in any directory without a single .csproj, and would spend a full project build just to
// print two lines.
if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
{
    PrintUsage();
    return 0;
}

if (args[0] is "--version")
{
    Console.WriteLine(typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0");
    return 0;
}

var project = ResolveProjectPath(args, out var remainingArgs);
if (project is null)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("  Could not find a single .csproj in the current directory.");
    Console.Error.WriteLine("  Pass --project <path-to-csproj> to disambiguate.");
    Console.Error.WriteLine();
    return 1;
}

var forwardedArgs = new List<string> { "run", "--project", project, "--no-launch-profile", "--", "fila" };
forwardedArgs.AddRange(remainingArgs);

var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };
foreach (var arg in forwardedArgs)
    startInfo.ArgumentList.Add(arg);

// Every usage line FilaCli prints on the other side of this call — an unknown command, a
// make:panel with no name — would otherwise read `dotnet run -- fila ...`, telling the user to
// type something they did not type. This is the only channel that survives the process
// boundary without adding an argument the in-app path would have to ignore.
startInfo.Environment["FILA_CLI_INVOCATION"] = "dotnet fila";

using var process = Process.Start(startInfo)
    ?? throw new InvalidOperationException("Failed to start 'dotnet'.");

await process.WaitForExitAsync();
return process.ExitCode;

// Deliberately a copy of the command list FilaCli prints, not a shared constant: the two
// differ in the invocation they tell the user to type, and reaching the real one costs a
// project build. Keep them in step when a command is added.
static void PrintUsage() => Console.WriteLine("""

      Usage:
        dotnet fila make:panel <Name>
        dotnet fila make:resource <Entity> [--context <Name>] [--force]

      Options:
        --project <path>   Target .csproj, when the current directory holds more than one.
        --help             Show this help.
        --version          Show the fila tool version.

      """);

static string? ResolveProjectPath(string[] args, out string[] remaining)
{
    var list = new List<string>(args);
    var flagIndex = list.FindIndex(a => a is "--project" or "-p");

    if (flagIndex >= 0 && flagIndex + 1 < list.Count)
    {
        var explicitPath = list[flagIndex + 1];
        list.RemoveRange(flagIndex, 2);
        remaining = list.ToArray();
        return Path.GetFullPath(explicitPath);
    }

    remaining = args;

    var candidates = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
    return candidates.Length == 1 ? candidates[0] : null;
}
