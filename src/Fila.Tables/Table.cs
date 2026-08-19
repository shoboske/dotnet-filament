using System.Linq.Expressions;
using Fila.Actions;
using Fila.Support;

namespace Fila.Tables;

/// <summary>
/// Non-generic view of a table, used by rendering and query code that doesn't know TEntity.
/// </summary>
public interface ITable
{
    Type EntityType { get; }
    IReadOnlyList<ITableColumn> Columns { get; }
    string? DefaultSortPath { get; }
    bool DefaultSortDescending { get; }
    int PerPage { get; }

    /// <summary>Row actions — rendered once per row, e.g. Edit/Delete icon buttons. Empty until
    /// .Actions(...) is called; Resource&lt;TEntity&gt;.BuildTable() fills this with the
    /// built-in Edit/Delete pair by default when a resource has a form and never called it
    /// itself, so a resource gets working CRUD actions for free the same way it already gets a
    /// working table/form for free.</summary>
    IReadOnlyList<IRowAction> RowActions { get; }

    /// <summary>Bulk actions — run once against every row a table's checkboxes select.</summary>
    IReadOnlyList<BulkAction> BulkActions { get; }
}

public sealed class Table<T> : ITable
{
    private IReadOnlyList<ITableColumn> _columns = Array.Empty<ITableColumn>();
    private string? _defaultSortPath;
    private bool _defaultSortDescending;
    private int _perPage = 10;
    private IReadOnlyList<IRowAction> _actions = Array.Empty<IRowAction>();
    private IReadOnlyList<BulkAction> _bulkActions = Array.Empty<BulkAction>();

    public Type EntityType => typeof(T);

    IReadOnlyList<ITableColumn> ITable.Columns => _columns;
    string? ITable.DefaultSortPath => _defaultSortPath;
    bool ITable.DefaultSortDescending => _defaultSortDescending;
    int ITable.PerPage => _perPage;
    IReadOnlyList<IRowAction> ITable.RowActions => _actions;
    IReadOnlyList<BulkAction> ITable.BulkActions => _bulkActions;

    /// <summary>Takes the base type, so a column type declared outside Fila drops into the
    /// same call: .Columns(new RatingColumn&lt;Product&gt;(p =&gt; p.Stars).Sortable()).</summary>
    public Table<T> Columns(params TableColumn<T>[] columns)
    {
        _columns = columns;
        return this;
    }

    /// <summary>Sets this table's row actions explicitly, replacing whatever default Fila would
    /// otherwise supply — accepts a plain Action or an ActionGroup interchangeably, including
    /// custom ones a resource author defines themselves.</summary>
    public Table<T> Actions(params IRowAction[] actions)
    {
        _actions = actions;
        return this;
    }

    public Table<T> BulkActions(params BulkAction[] actions)
    {
        _bulkActions = actions;
        return this;
    }

    public Table<T> DefaultSort<TValue>(Expression<Func<T, TValue>> selector, bool descending = false)
    {
        _defaultSortPath = ExpressionPath.ToPath(selector);
        _defaultSortDescending = descending;
        return this;
    }

    public Table<T> PaginateBy(int perPage)
    {
        _perPage = perPage;
        return this;
    }

    public TextColumn<T> Text(Expression<Func<T, object?>> selector) => new(selector);

    public TextColumn<T> Money(Expression<Func<T, object?>> selector) => new TextColumn<T>(selector).Money();

    public TextColumn<T> Badge(Expression<Func<T, object?>> selector) => new TextColumn<T>(selector).Badge();

    public TextColumn<T> Date(Expression<Func<T, object?>> selector) => new TextColumn<T>(selector).Date();

    public TextColumn<T> Bool(Expression<Func<T, object?>> selector) => new TextColumn<T>(selector).Boolean();
}
