using Fila.Panels.Resources;
using Fila.Tables;
using Microsoft.EntityFrameworkCore;

namespace Fila.Panels.RelationManagers;

/// <summary>Non-generic view of a relation manager, used by panel routing/rendering the same
/// way <see cref="IResource"/> is — routing never sees the concrete
/// RelationManager&lt;TParent, TRelated&gt;, only this and the parent record.</summary>
public interface IRelationManager
{
    /// <summary>The route segment and htmx-target suffix a relation manager's tab/table use.
    /// Defaults to the same pluralised-kebab-case rule Resource&lt;TEntity&gt;.Slug uses, off
    /// TRelated rather than the parent.</summary>
    string Slug { get; }

    /// <summary>The tab label. Defaults to the humanized Slug.</summary>
    string Title { get; }

    ITable BuildTable();

    /// <summary><paramref name="parent"/> is the already-resolved parent record being edited —
    /// routing hands it over as object since it never knows TParent either.</summary>
    Task<PagedRows> ListAsync(DbContext db, object parent, ITable table, TableQuery query, CancellationToken ct);
}

/// <summary>Non-generic base every RelationManager&lt;TParent, TRelated&gt; derives from — what
/// <see cref="RelationManagerRegistration.Of{TRelationManager}"/> constrains against, the same
/// way Fila.Widgets.Widget lets WidgetRegistration.Of&lt;TWidget&gt; do a compile-time check
/// instead of a runtime one.</summary>
public abstract class RelationManager : IRelationManager
{
    public abstract string Slug { get; }
    public abstract string Title { get; }
    public abstract ITable BuildTable();
    public abstract Task<PagedRows> ListAsync(DbContext db, object parent, ITable table, TableQuery query, CancellationToken ct);
}

/// <summary>Wraps a Fila.Tables.Table&lt;TRelated&gt; scoped to whatever navigation relates it
/// to TParent — Filament's Resources\RelationManagers\RelationManager, trimmed to "a relation
/// manager is a scoped Table shown as a tab on Edit": no Livewire component, no
/// attach/associate actions, no relationship-type detection. A straightforward one-to-many
/// (TParent has many TRelated via a foreign key on TRelated) is the only shape this supports —
/// <see cref="Scope"/> is where a resource author expresses that foreign key, the same way
/// Resource&lt;TEntity&gt;.Query lets a resource author add .Include(...).</summary>
public abstract class RelationManager<TParent, TRelated> : RelationManager
    where TParent : class
    where TRelated : class
{
    public override string Slug => ResourceNaming.ToSlug(typeof(TRelated).Name);

    public override string Title => ResourceNaming.Humanize(Slug);

    /// <summary>Build the nested table definition — same DSL a Resource's own Table() uses.</summary>
    protected abstract Table<TRelated> Table(Table<TRelated> t);

    /// <summary>Narrows TRelated's query down to just the rows belonging to
    /// <paramref name="parent"/> — e.g. <c>q.Where(child =&gt; child.ParentId == parent.Id)</c>.
    /// The one thing every relation manager must supply: there is no generic way to discover the FK
    /// from TParent/TRelated alone without reflecting into EF's navigation metadata, which is
    /// more machinery than a one-to-many relation manager needs.</summary>
    protected abstract IQueryable<TRelated> Scope(TParent parent, IQueryable<TRelated> q);

    /// <summary>Override to add .Include(...) the same way Resource&lt;TEntity&gt;.Query does.</summary>
    protected virtual IQueryable<TRelated> Query(IQueryable<TRelated> q) => q;

    public override ITable BuildTable() => Table(new Table<TRelated>());

    public override async Task<PagedRows> ListAsync(DbContext db, object parent, ITable table, TableQuery query, CancellationToken ct)
    {
        var source = Scope((TParent)parent, Query(db.Set<TRelated>()));
        source = source.ApplySearch(table, query.Search);
        source = source.ApplySort(table, query);
        return await source.PaginateAsync(query.Page, table.PerPage, ct);
    }
}
