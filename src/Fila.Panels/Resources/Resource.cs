using Fila.Forms;
using Fila.Tables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Fila.Panels.Resources;

/// <summary>Non-generic view of a resource, used by panel routing and the CLI.</summary>
public interface IResource
{
    string Slug { get; }
    string? NavigationIcon { get; }
    Type EntityType { get; }

    ITable BuildTable();
    IForm? BuildForm();

    Task<PagedRows> ListAsync(DbContext db, ITable table, TableQuery query, CancellationToken ct);

    Task<object?> FindAsync(DbContext db, string id, CancellationToken ct);
    object CreateBlank();
    Task SaveAsync(DbContext db, object entity, bool isNew, CancellationToken ct);
    Task DeleteAsync(DbContext db, object entity, CancellationToken ct);

    /// <summary>Inserts a copy of <paramref name="entity"/> with a fresh primary key — backs
    /// ReplicateAction. Copies only the entity's own scalar/FK properties (via EF's model
    /// metadata, not reflection over every CLR property), so a navigation collection never gets
    /// its reference shared between the original and the copy.</summary>
    Task<object> ReplicateAsync(DbContext db, object entity, CancellationToken ct);

    string GetId(DbContext db, object entity);

    /// <summary>This resource's header action — the "New" button — or null when it has no
    /// form. The one thing panel routing needs from just an IResource (it never sees the
    /// concrete Resource&lt;TEntity&gt;) to mount/execute Create the same generic way it
    /// mounts/executes every row action.</summary>
    Fila.Actions.Action? CreateAction();
}

public abstract class Resource<TEntity> : IResource
    where TEntity : class
{
    public virtual string? NavigationIcon => null;

    public virtual string Slug => ResourceNaming.ToSlug(typeof(TEntity).Name);

    public Type EntityType => typeof(TEntity);

    /// <summary>Build the table definition. Called once per request (memoize per resource type
    /// later if profiling says otherwise).</summary>
    protected abstract Table<TEntity> Table(Table<TEntity> t);

    /// <summary>Override to add `.Include(...)` and other query shaping.</summary>
    protected virtual IQueryable<TEntity> Query(IQueryable<TEntity> q) => q;

    /// <summary>Override to enable Create/Edit/Delete. Returning null (the default) means this
    /// resource is list-only — no form, no New button, no row actions.</summary>
    protected virtual Form<TEntity>? Form(Form<TEntity> f) => null;

    /// <summary>Builds the table, then — only if <c>Table()</c> itself never called
    /// .Actions(...) — fills in the built-in Edit/Delete row actions (when this resource has a
    /// form) the same way Fila has always given a resource working CRUD for free. A resource
    /// that calls .Actions(...) itself (see samples/Demo's OrderResource) owns the row action
    /// list entirely, built-ins and custom actions together.</summary>
    public ITable BuildTable()
    {
        var table = Table(new Table<TEntity>());
        var built = (ITable)table;

        if (built.RowActions.Count == 0 && BuildForm() is not null)
            table.Actions(EditAction(), DeleteAction());

        return table;
    }

    public IForm? BuildForm() => Form(new Form<TEntity>());

    /// <summary>Wires Fila.Actions.CreateAction to this resource — the schema and Handle
    /// delegates it needs, supplied here the way Filament's CreateRecord page supplies
    /// handleRecordCreation() to the generic CreateAction it hosts. Null when this resource has
    /// no form.</summary>
    public Fila.Actions.Action? CreateAction() => BuildForm() is null
        ? null
        : Fila.Actions.CreateAction.Make(
            $"Create {Slug.TrimEnd('s').Replace('-', ' ')}",
            () => BuildForm()!,
            ctx => SaveAsync(ctx.Db, ctx.Record!, isNew: true, ctx.Ct));

    /// <summary>Wires Fila.Actions.EditAction to this resource. Exposed as protected so a
    /// resource that calls .Actions(...) itself (see samples/Demo's OrderResource) can still
    /// include the built-in behavior alongside its own custom actions.</summary>
    protected Fila.Actions.Action EditAction() => Fila.Actions.EditAction.Make(
        $"Edit {Slug.TrimEnd('s').Replace('-', ' ')}",
        () => BuildForm() ?? throw new InvalidOperationException("EditAction requires this resource to define a form."),
        ctx => SaveAsync(ctx.Db, ctx.Record!, isNew: false, ctx.Ct));

    protected Fila.Actions.Action DeleteAction() => Fila.Actions.DeleteAction.Make(
        $"Delete {ResourceNaming.SingularLabel(Slug)}",
        ctx => DeleteAsync(ctx.Db, ctx.Record!, ctx.Ct));

    protected Fila.Actions.Action ReplicateAction() => Fila.Actions.ReplicateAction.Make(
        $"Replicate {ResourceNaming.SingularLabel(Slug)}",
        ctx => ReplicateAsync(ctx.Db, ctx.Record!, ctx.Ct));

    protected Fila.Actions.Action ViewAction() => Fila.Actions.ViewAction.Make(
        $"View {ResourceNaming.SingularLabel(Slug)}",
        () => BuildForm() ?? throw new InvalidOperationException("ViewAction requires this resource to define a form."));

    protected Fila.Actions.BulkAction DeleteBulkAction() => Fila.Actions.DeleteBulkAction.Make(async ctx =>
    {
        foreach (var record in ctx.Records)
            await DeleteAsync(ctx.Db, record, ctx.Ct);
    });

    public async Task<PagedRows> ListAsync(DbContext db, ITable table, TableQuery query, CancellationToken ct)
    {
        var source = Query(db.Set<TEntity>());
        source = source.ApplySearch(table, query.Search);
        source = source.ApplySort(table, query);
        return await source.PaginateAsync(query.Page, table.PerPage, ct);
    }

    public async Task<object?> FindAsync(DbContext db, string id, CancellationToken ct)
    {
        var pk = GetPrimaryKeyProperty(db);
        var key = FieldBinding.Parse(pk.ClrType, id);
        return key is null ? null : await db.Set<TEntity>().FindAsync([key], ct);
    }

    public object CreateBlank() => Activator.CreateInstance<TEntity>();

    public async Task SaveAsync(DbContext db, object entity, bool isNew, CancellationToken ct)
    {
        if (isNew) db.Set<TEntity>().Add((TEntity)entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(DbContext db, object entity, CancellationToken ct)
    {
        db.Set<TEntity>().Remove((TEntity)entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<object> ReplicateAsync(DbContext db, object entity, CancellationToken ct)
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity))!;
        var pk = entityType.FindPrimaryKey()!.Properties[0];
        var clone = Activator.CreateInstance<TEntity>();

        foreach (var property in entityType.GetProperties())
        {
            if (property == pk || property.PropertyInfo is null) continue;
            property.PropertyInfo.SetValue(clone, property.PropertyInfo.GetValue(entity));
        }

        db.Set<TEntity>().Add(clone);
        await db.SaveChangesAsync(ct);
        return clone;
    }

    public string GetId(DbContext db, object entity)
    {
        var pk = GetPrimaryKeyProperty(db);
        return pk.PropertyInfo!.GetValue(entity)?.ToString() ?? "";
    }

    private static IProperty GetPrimaryKeyProperty(DbContext db) =>
        db.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties[0];
}
