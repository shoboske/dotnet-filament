using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fila.Tests;

/// <summary>Actually compiles a FileGenerators/*.cs output string, rather than only checking it
/// looks plausible — Phase 9's own acceptance bar. References every assembly already loaded in
/// the test process (Fila.* and Demo, since Fila.Tests already depends on Demo, which pulls in
/// the whole framework) instead of hand-picking a reference list that would drift as packages
/// are added.</summary>
internal static class GeneratedSourceCompiler
{
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
