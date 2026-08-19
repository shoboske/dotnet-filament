namespace Fila.Panels.RelationManagers;

/// <summary>A relation manager named for later activation — the RelationManager counterpart to
/// Fila.Widgets.WidgetRegistration, and for the same reason: Resource&lt;TEntity&gt;.GetRelations()
/// is an instance member on a generic type routing never closes over, so relation managers are
/// registered by type and activated out of the request scope once routing knows which parent
/// record they're being shown for.</summary>
public sealed class RelationManagerRegistration
{
    private RelationManagerRegistration(Type relationManagerType) => RelationManagerType = relationManagerType;

    /// <summary>Registers <typeparamref name="TRelationManager"/>. The constraint is the whole
    /// point: <c>RelationManagerRegistration.Of&lt;NotARelationManager&gt;()</c> is a compile
    /// error.</summary>
    public static RelationManagerRegistration Of<TRelationManager>() where TRelationManager : RelationManager => new(typeof(TRelationManager));

    /// <summary>The registered relation manager's type. Guaranteed to derive from
    /// <see cref="RelationManager"/>.</summary>
    public Type RelationManagerType { get; }
}
