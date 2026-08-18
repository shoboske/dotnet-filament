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
    string GetId(DbContext db, object entity);
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

    public ITable BuildTable() => Table(new Table<TEntity>());

    public IForm? BuildForm() => Form(new Form<TEntity>());

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

    public string GetId(DbContext db, object entity)
    {
        var pk = GetPrimaryKeyProperty(db);
        return pk.PropertyInfo!.GetValue(entity)?.ToString() ?? "";
    }

    private static IProperty GetPrimaryKeyProperty(DbContext db) =>
        db.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties[0];
}
