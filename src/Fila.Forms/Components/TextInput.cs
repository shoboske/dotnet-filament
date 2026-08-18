using System.Linq.Expressions;

namespace Fila.Forms;

/// <summary>A single-line text box, and the numeric input too. Filament has no NumberInput
/// class: TextInput->numeric() flips getType() to "number" and sets step("any"), and the same
/// class covers email(), password(), tel() and url(). Only numeric() is ported here — it is
/// the one the FieldKind enum had.</summary>
public sealed class TextInput<TEntity> : FormField<TEntity, TextInput<TEntity>>
{
    private bool _isNumeric;

    public TextInput(Expression<Func<TEntity, object?>> selector)
        : base(selector)
    {
    }

    public override string InputType => _isNumeric ? "number" : "text";

    public override string? Step => _isNumeric ? "any" : null;

    public TextInput<TEntity> Numeric(bool value = true)
    {
        _isNumeric = value;
        return this;
    }
}
