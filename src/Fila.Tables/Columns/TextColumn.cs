using System.Linq.Expressions;

namespace Fila.Tables;

/// <summary>Shows the value as-is — the default column type.</summary>
public sealed class TextColumn<TEntity>(Expression<Func<TEntity, object?>> selector)
    : TableColumn<TEntity>(selector);
