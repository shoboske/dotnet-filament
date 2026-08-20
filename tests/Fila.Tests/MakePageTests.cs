using Fila.Tooling.FileGenerators;
using Xunit;

namespace Fila.Tests;

/// <summary>Phase 9 acceptance: `make:page`'s generated class source actually compiles. Its
/// generated view is Razor, not C# — Roslyn can't compile that directly — so that half is
/// covered by manually running `dotnet fila make:page` from samples/Demo and rebuilding (done
/// for this PR); here it just gets a shape check on the directives that make it work at all
/// (an app-local view has no _ViewImports.cshtml/_ViewStart.cshtml of its own to fall back on).</summary>
public sealed class MakePageTests
{
    [Fact]
    public void Generate_ProducesAPageSubclassThatCompiles()
    {
        var result = PageClassGenerator.Generate("Settings", "Demo");

        Assert.Contains("class SettingsPage : Page", result.ClassSource);
        GeneratedSourceCompiler.AssertCompiles(result.ClassSource);
    }

    [Fact]
    public void Generate_ProducesAViewWithTheDirectivesAnAppLocalViewNeeds()
    {
        var result = PageClassGenerator.Generate("Settings", "Demo");

        Assert.Equal("Views/Fila/Pages/_Settings.cshtml", result.ViewRelativePath);
        Assert.Contains("@model FilaPageViewModel", result.ViewSource);
        // Without these two, an app-local view (outside Fila.Panels' own Views folder, which
        // gets them from its _ViewImports.cshtml/_ViewStart.cshtml) fails to compile at all —
        // FilaPageViewModel unresolved, and <partial> rendering as literal unrecognised HTML.
        Assert.Contains("@using Fila.Panels.Rendering", result.ViewSource);
        Assert.Contains("@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers", result.ViewSource);
        Assert.Contains("Layout = \"_FilaLayout\"", result.ViewSource);
    }
}
