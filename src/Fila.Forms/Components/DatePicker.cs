using System.Linq.Expressions;

namespace Fila.Forms;

/// <summary>A date input. Nothing but the input type separates it from TextInput, which is the
/// point: a field type that only changes the type attribute costs one override.</summary>
public sealed class DatePicker<TEntity>(Expression<Func<TEntity, object?>> selector)
    : FormField<TEntity>(selector)
{
    public override string InputType => "date";
}
