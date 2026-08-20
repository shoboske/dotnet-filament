using Fila.Tooling.FileGenerators;
using Xunit;

namespace Fila.Tests;

/// <summary>Phase 9 acceptance: `make:action`'s generated source actually compiles, not just
/// looks plausible.</summary>
public sealed class MakeActionTests
{
    [Fact]
    public void Generate_ProducesAStaticActionFieldThatCompiles()
    {
        var result = ActionClassGenerator.Generate("Archive", "Demo");

        Assert.Contains("class ArchiveAction", result.Source);
        Assert.Contains("new Action(\"archive\")", result.Source);
        GeneratedSourceCompiler.AssertCompiles(result.Source);
    }
}
