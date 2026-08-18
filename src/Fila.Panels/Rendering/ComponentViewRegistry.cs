using Fila.Forms;
using Fila.Support;
using Fila.Tables;

namespace Fila.Panels.Rendering;

/// <summary>The seam between "what a field/column component is" and "how it becomes HTML" —
/// a lookup from a component's <see cref="IFormField.View"/>/<see cref="ITableColumn.View"/>
/// name to the partial that renders it, replacing the switch/if chain that used to live
/// directly in <c>_Form.cshtml</c>/<c>_Table.cshtml</c>. A name with no registration falls back
/// to the default partial, which is what lets a consumer-authored component render correctly
/// (as plain text, or as an &lt;input&gt; of its own InputType) without an entry here — see
/// CustomComponentTests for that in action.
///
/// Registered as a singleton (populated once at startup); adding a new view is one call to
/// RegisterField/RegisterColumn, not a new branch in a Razor view.</summary>
public sealed class ComponentViewRegistry
{
    private const string DefaultFieldView = "input";
    private const string DefaultColumnView = "text";

    private readonly Dictionary<string, string> _fieldPartials = new();
    private readonly Dictionary<string, string> _columnPartials = new();

    public ComponentViewRegistry()
    {
        RegisterField(DefaultFieldView, "Fila/Fields/_Input");
        RegisterField("textarea", "Fila/Fields/_Textarea");
        RegisterField("select", "Fila/Fields/_Select");
        RegisterField("checkbox", "Fila/Fields/_Checkbox");

        RegisterColumn(DefaultColumnView, "Fila/Columns/_Text");
        RegisterColumn("badge", "Fila/Columns/_Badge");
    }

    public ComponentViewRegistry RegisterField(string view, string partialPath)
    {
        _fieldPartials[view] = partialPath;
        return this;
    }

    public ComponentViewRegistry RegisterColumn(string view, string partialPath)
    {
        _columnPartials[view] = partialPath;
        return this;
    }

    public string PartialForField(string view) =>
        _fieldPartials.TryGetValue(view, out var partial) ? partial : _fieldPartials[DefaultFieldView];

    public string PartialForColumn(string view) =>
        _columnPartials.TryGetValue(view, out var partial) ? partial : _columnPartials[DefaultColumnView];
}

/// <summary>What a field partial needs to render its control — everything the form-rendering
/// view already computed per field before the dispatch existed. Disabled is true only for
/// ViewAction's read-only mount.</summary>
public sealed record FieldRenderModel(IFormField Field, EvaluationContext Evaluation, string FieldId, string? RawValue, bool Disabled = false);

/// <summary>What a column partial needs to render one cell.</summary>
public sealed record ColumnRenderModel(ITableColumn Column, object Row);
