using System.Linq.Expressions;

namespace Fila.Forms;

/// <summary>A boolean toggle. Renders its own label inline, so the renderer skips the shared
/// label above the control.</summary>
public sealed class Checkbox<TEntity>(Expression<Func<TEntity, object?>> selector)
    : FormField<TEntity>(selector)
{
    public override string View => "checkbox";
}
