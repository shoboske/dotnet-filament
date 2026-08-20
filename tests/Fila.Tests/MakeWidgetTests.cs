using Fila.Tooling.FileGenerators;
using Xunit;

namespace Fila.Tests;

/// <summary>Phase 9 acceptance: `make:widget`'s generated source actually compiles, not just
/// looks plausible.</summary>
public sealed class MakeWidgetTests
{
    [Fact]
    public void Generate_ProducesAStatsOverviewWidgetSubclassThatCompiles()
    {
        var result = WidgetClassGenerator.Generate("RecentSignups", "Demo");

        Assert.Contains("class RecentSignupsWidget : StatsOverviewWidget", result.Source);
        GeneratedSourceCompiler.AssertCompiles(result.Source);
    }
}
