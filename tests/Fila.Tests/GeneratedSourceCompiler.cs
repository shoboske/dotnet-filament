using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fila.Tests;

/// <summary>Actually compiles a FileGenerators/*.cs output string, rather than only checking it
/// looks plausible — Phase 9's own acceptance bar. References every assembly already loaded in
/// the test process (Fila.* and Demo, since Fila.Tests already depends on Demo, which pulls in
/// the whole framework) instead of hand-picking a reference list that would drift as packages
/// are added.
///
/// <see cref="Touch"/> exists because "already loaded" is order-dependent: a test that only
/// needs, say, System.Linq.Queryable (IQueryable's LINQ operators, a separate assembly from
/// System.Linq.Enumerable's IEnumerable ones) fails here — but only when run in isolation,
/// passing whenever an earlier test happened to load it first — unless something has forced
/// that assembly to load before AppDomain.CurrentDomain.GetAssemblies() runs.</summary>
internal static class GeneratedSourceCompiler
{
    /// <summary>Forces the assemblies FileGenerators output can reference to be loaded,
    /// regardless of what ran before this test in the process. One per generator's own
    /// dependencies (Fila.Tables, Fila.Widgets, Fila.Actions, Fila.Notifications,
    /// Fila.Panels.RelationManagers, Fila.Panels.Pages, EF Core, the Demo entities) plus
    /// System.Linq.Queryable specifically, since IEnumerable's Where being loaded is not enough.</summary>
    private static readonly Type[] Touch =
    [
        typeof(object), typeof(System.Linq.Enumerable), typeof(System.Linq.Queryable),
        typeof(System.Linq.Expressions.Expression), typeof(System.Threading.Tasks.Task),
        typeof(Microsoft.EntityFrameworkCore.DbContext), typeof(Fila.Tables.Table<>),
        typeof(Fila.Forms.Form<>), typeof(Fila.Widgets.Widget), typeof(Fila.Actions.Action),
        typeof(Fila.Notifications.Notification), typeof(Fila.Panels.RelationManagers.RelationManager<,>),
        typeof(Fila.Panels.Pages.Page), typeof(Demo.Data.AppDb),
    ];

    // Every project in this repo sets <ImplicitUsings>enable</ImplicitUsings> (Directory.Build.props)
    // — the same implicit usings the SDK generates into obj/.../<Project>.GlobalUsings.g.cs for
    // any app a generated file actually lands in. Without these, a generated file that leans on
    // them (as every FileGenerators/*.cs output does, matching every hand-written file in this
    // repo) fails to compile here even though it compiles fine in the real target project.
    private const string ImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    public static void AssertCompiles(string source, string assemblyName = "GeneratedSourceCheck")
    {
        var trees = new[] { CSharpSyntaxTree.ParseText(source), CSharpSyntaxTree.ParseText(ImplicitUsings) };

        foreach (var type in Touch) _ = type.Assembly;

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName,
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (errors.Count > 0)
        {
            var messages = string.Join(Environment.NewLine, errors.Select(e => e.ToString()));
            throw new Xunit.Sdk.XunitException(
                $"Generated source did not compile:{Environment.NewLine}{messages}{Environment.NewLine}---{Environment.NewLine}{source}");
        }
    }
}
