using Fila.Panels.Rendering;
using Xunit;

namespace Fila.Tests;

public sealed class ComponentViewRegistryTests
{
    [Theory]
    [InlineData("input", "Fila/Fields/_Input")]
    [InlineData("textarea", "Fila/Fields/_Textarea")]
    [InlineData("select", "Fila/Fields/_Select")]
    [InlineData("checkbox", "Fila/Fields/_Checkbox")]
    public void PartialForField_ResolvesEachBuiltInView(string view, string expectedPartial) =>
        Assert.Equal(expectedPartial, new ComponentViewRegistry().PartialForField(view));

    [Theory]
    [InlineData("text", "Fila/Columns/_Text")]
    [InlineData("badge", "Fila/Columns/_Badge")]
    public void PartialForColumn_ResolvesEachBuiltInView(string view, string expectedPartial) =>
        Assert.Equal(expectedPartial, new ComponentViewRegistry().PartialForColumn(view));

    [Fact]
    public void PartialForField_FallsBackToInput_ForAnUnrecognisedView()
    {
        // A consumer-authored field that never overrides View lands here — same fallback the
        // old switch statement's `default:` case gave it.
        Assert.Equal("Fila/Fields/_Input", new ComponentViewRegistry().PartialForField("color-picker"));
    }

    [Fact]
    public void PartialForColumn_FallsBackToText_ForAnUnrecognisedView()
    {
        Assert.Equal("Fila/Columns/_Text", new ComponentViewRegistry().PartialForColumn("rating"));
    }

    [Fact]
    public void RegisteringANewView_IsOneLine_NotANewBranchInARazorView()
    {
        // Sketches the acceptance bar from #5: a hypothetical new component type's renderer is
        // a one-line registration against the registry, not an added `if`/`case` in the views.
        var registry = new ComponentViewRegistry()
            .RegisterField("rich-text", "Fila/Fields/_RichText")
            .RegisterColumn("rating", "Fila/Columns/_Rating");

        Assert.Equal("Fila/Fields/_RichText", registry.PartialForField("rich-text"));
        Assert.Equal("Fila/Columns/_Rating", registry.PartialForColumn("rating"));
    }
}
