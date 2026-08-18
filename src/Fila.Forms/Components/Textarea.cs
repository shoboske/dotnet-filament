using System.Linq.Expressions;

namespace Fila.Forms;

/// <summary>A multi-line text box.</summary>
public sealed class Textarea<TEntity>(Expression<Func<TEntity, object?>> selector)
    : FormField<TEntity, Textarea<TEntity>>(selector)
{
    public override string View => "textarea";
}
