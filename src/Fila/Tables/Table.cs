using System.Linq.Expressions;

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
}

public sealed class Table<T> : ITable
{
    private IReadOnlyList<ITableColumn> _columns = Array.Empty<ITableColumn>();
    private string? _defaultSortPath;
    private bool _defaultSortDescending;
    private int _perPage = 10;

    public Type EntityType => typeof(T);

    IReadOnlyList<ITableColumn> ITable.Columns => _columns;
    string? ITable.DefaultSortPath => _defaultSortPath;
    bool ITable.DefaultSortDescending => _defaultSortDescending;
    int ITable.PerPage => _perPage;

    public Table<T> Columns(params TableColumn<T>[] columns)
    {
        _columns = columns;
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

    public TableColumn<T> Text(Expression<Func<T, object?>> selector) => Column(ColumnKind.Text, selector);

    public TableColumn<T> Money(Expression<Func<T, object?>> selector) => Column(ColumnKind.Money, selector);

    public TableColumn<T> Badge(Expression<Func<T, object?>> selector) => Column(ColumnKind.Badge, selector);

    public TableColumn<T> Date(Expression<Func<T, object?>> selector) => Column(ColumnKind.Date, selector);

    public TableColumn<T> Bool(Expression<Func<T, object?>> selector) => Column(ColumnKind.Bool, selector);

    private static TableColumn<T> Column(ColumnKind kind, Expression<Func<T, object?>> selector) =>
        new(kind, ExpressionPath.ToPath(selector), selector.Compile());
}
