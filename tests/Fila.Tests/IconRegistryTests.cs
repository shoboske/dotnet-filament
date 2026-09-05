using Fila.Support;
using Xunit;

namespace Fila.Tests;

/// <summary>Pins the icon set to Heroicons 24/outline — the set Filament itself ships via
/// blade-ui-kit/blade-heroicons. These bodies used to be hand-drawn Lucide-style
/// approximations at stroke-width 1.75, which is close enough to compile and render but
/// visibly wrong beside a real Filament screenshot.</summary>
public sealed class IconRegistryTests
{
    [Fact]
    public void Wrapper_UsesHeroiconsOwnAttributes()
    {
        var svg = IconRegistry.Render("home");

        // Confirmed against a live rendered Filament sidebar icon, which carries exactly these.
        Assert.Contains(@"viewBox=""0 0 24 24""", svg);
        Assert.Contains(@"fill=""none""", svg);
        Assert.Contains(@"stroke=""currentColor""", svg);
        Assert.Contains(@"stroke-width=""1.5""", svg);
        Assert.Contains(@"data-slot=""icon""", svg);
        Assert.Contains(@"class=""fi-icon""", svg);
        Assert.DoesNotContain(@"stroke-width=""1.75""", svg);
    }

    [Theory]
    // The exact opening of each Heroicons 24/outline path, so a body swapped back to a
    // hand-drawn approximation fails here rather than silently shipping.
    [InlineData("home", "m2.25 12 8.954-8.955c")]
    [InlineData("shopping-cart", "M2.25 3h1.386c")]
    [InlineData("users", "M15 19.128a9.38 9.38 0 0 0 2.625.372")]
    [InlineData("pencil", "m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652")]
    [InlineData("trash", "m14.74 9-.346 9m-4.788 0L9.26 9")]
    [InlineData("eye", "M2.036 12.322a1.012 1.012 0 0 1 0-.639")]
    [InlineData("filter", "M12 3c2.755 0 5.455.232 8.083.678")]
    [InlineData("plus", "M12 4.5v15m7.5-7.5h-15")]
    public void Body_IsTheVerbatimHeroicon(string name, string pathPrefix)
    {
        Assert.Contains($@"d=""{pathPrefix}", IconRegistry.Render(name));
    }

    [Fact]
    public void UnknownName_FallsBackToTheDocumentTextHeroicon()
    {
        var svg = IconRegistry.Render("no-such-icon");

        Assert.Contains("M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375", svg);
    }
}
