using System.Linq.Expressions;

namespace Fila.Tables.Filters;

/// <summary>Filters a soft-deletable resource's list by trashed state — Filament's
/// Filters\TrashedFilter, the concrete filter behind the "Deleted records" dropdown on
/// Customers. Its three TernaryFilter states become "with trashed" (no restriction at all),
/// "only trashed" (DeletedAt set), and blank/"without trashed" (DeletedAt unset — the same
/// predicate Resource&lt;TEntity&gt; already applies by default when no TrashedFilter is
/// present; see its ListAsync, which skips that default exactly when a table carries one of
/// these instead).
///
/// TEntity is deliberately unconstrained rather than `where TEntity : ISoftDeletable`: a
/// constraint here would force Resource&lt;TEntity&gt; — itself generic and unconstrained over
/// TEntity — to prove TEntity satisfies it just to type-check `table.Filters.OfType&lt;
/// TrashedFilter&lt;TEntity&gt;&gt;()`, which it can't do for a TEntity that doesn't
/// soft-delete. Building the DeletedAt member expression by reflection (same trick
/// Resource&lt;TEntity&gt;'s own BuildNotDeletedPredicate uses) sidesteps that; ITrashedFilter
/// is what lets ListAsync recognise this filter without touching the closed generic type at
/// all.</summary>
public sealed class TrashedFilter<TEntity> : TernaryFilter<TEntity>, ITrashedFilter
{
    public TrashedFilter() : base("trashed")
    {
        Label("Deleted records");
        Placeholder("Without deleted records");
        TrueLabel("With deleted records");
        FalseLabel("Only deleted records");

        var notDeleted = BuildPredicate(isDeleted: false);
        var onlyDeleted = BuildPredicate(isDeleted: true);

        Queries(
            whenTrue: source => source, // "with trashed": no restriction
            whenFalse: source => source.Where(onlyDeleted),
            whenBlank: source => source.Where(notDeleted));
    }

    private static Expression<Func<TEntity, bool>> BuildPredicate(bool isDeleted)
    {
        var property = typeof(TEntity).GetProperty("DeletedAt")
            ?? throw new InvalidOperationException(
                $"TrashedFilter<{typeof(TEntity).Name}> requires a DeletedAt property " +
                $"(implement ISoftDeletable) — {typeof(TEntity).Name} has none.");

        var entity = Expression.Parameter(typeof(TEntity), "e");
        var deletedAt = Expression.Property(entity, property);
        var comparison = isDeleted
            ? Expression.NotEqual(deletedAt, Expression.Constant(null, property.PropertyType))
            : Expression.Equal(deletedAt, Expression.Constant(null, property.PropertyType));
        return Expression.Lambda<Func<TEntity, bool>>(comparison, entity);
    }
}
