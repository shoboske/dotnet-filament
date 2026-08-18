namespace Fila.Support;

/// <summary>Implemented by an entity that supports soft deletes — Filament/Eloquent's
/// SoftDeletes trait. Resource&lt;TEntity&gt; checks for this via a type check
/// (typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity))) rather than a convention or
/// attribute, so support is explicit and discoverable at the entity's own declaration — the
/// same reason Filament's detection (`method_exists($model, 'trashed')`) is really asking "did
/// this model opt in to the trait," just expressed as an interface instead of a trait.
///
/// When present, Resource&lt;TEntity&gt; changes DeleteAsync to set DeletedAt instead of
/// removing the row, excludes soft-deleted rows from the default list query, and makes
/// RestoreAction/ForceDeleteAction available alongside the normal DeleteAction.</summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
}
