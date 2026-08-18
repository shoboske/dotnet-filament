using System.Linq.Expressions;

namespace Fila.Tables;

/// <summary>Wraps the value in a pill. Tone comes from .Colors(...); without one every badge
/// is neutral.</summary>
public sealed class BadgeColumn<TEntity>(Expression<Func<TEntity, object?>> selector)
    : TableColumn<TEntity>(selector)
{
    public override string View => "badge";
}
