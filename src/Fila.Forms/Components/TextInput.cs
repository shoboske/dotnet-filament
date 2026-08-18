using System.Linq.Expressions;

namespace Fila.Forms;

/// <summary>A single-line text box — the default field type.</summary>
public sealed class TextInput<TEntity>(Expression<Func<TEntity, object?>> selector)
    : FormField<TEntity>(selector);
