using System.Globalization;
using System.Linq.Expressions;
using Fila.Schemas;
using Fila.Support;

namespace Fila.Tables;

public enum ColumnAlign
{
    Start,
    Center,
    End,
}

/// <summary>Non-generic view of a column, used by rendering and query code that doesn't know
/// TEntity.</summary>
public interface ITableColumn : IComponent
{
    /// <summary>The property path this column reads. Same value as
    /// <see cref="IComponent.Name"/>; kept under the older name because query and sort code
    /// talks in paths.</summary>
    string Path { get; }

    /// <summary>Names the markup this column's cells render as. A value the renderer doesn't
    /// recognise renders as plain text, which is what lets a consumer-authored column type work
    /// without touching Fila. Phase 3 (#5) turns this into a lookup in a real partial
    /// registry.</summary>
    string View { get; }

    ColumnAlign Alignment { get; }
    bool IsSearchable { get; }
    bool IsSortable { get; }

    object? GetValue(object entity);
    string FormatDisplay(object entity);
    string? GetBadgeColor(object entity);
}

/// <summary>Base class for every table column. Subclass it — in Fila or in a consuming app — to
/// add a column type; there is no enum to extend and no switch to edit. Overriding
/// <see cref="FormatDisplay(object?)"/> is enough for a column that only formats differently.
///
/// Every fluent setter returns <c>TableColumn&lt;TEntity&gt;</c> rather than the subclass, so
/// the shared settings can be applied in any order without each subclass re-declaring
/// them.</summary>
public abstract class TableColumn<TEntity> : Component, ITableColumn
{
    private readonly Func<TEntity, object?> _accessor;
    private ColumnAlign _alignment = ColumnAlign.Start;
    private Func<object?, string>? _colorSelector;

    protected TableColumn(Expression<Func<TEntity, object?>> selector)
        : this(ExpressionPath.ToPath(selector), selector.Compile())
    {
    }

    protected TableColumn(string path, Func<TEntity, object?> accessor)
        : base(path)
    {
        _accessor = accessor;
    }

    public string Path => Name;

    public virtual string View => "text";

    public bool IsSearchable { get; private set; }

    public bool IsSortable { get; private set; }

    ColumnAlign ITableColumn.Alignment => _alignment;

    public TableColumn<TEntity> Label(string label)
    {
        LabelValue = label;
        return this;
    }

    public TableColumn<TEntity> Label(Func<EvaluationContext, string> label)
    {
        LabelValue = Evaluated<string>.From(label);
        return this;
    }

    public TableColumn<TEntity> Visible(bool value = true)
    {
        VisibleValue = value;
        return this;
    }

    public TableColumn<TEntity> Visible(Func<EvaluationContext, bool> value)
    {
        VisibleValue = Evaluated<bool>.From(value);
        return this;
    }

    public TableColumn<TEntity> Hidden(bool value = true) => Visible(!value);

    public TableColumn<TEntity> Hidden(Func<EvaluationContext, bool> value) =>
        Visible(context => !value(context));

    public TableColumn<TEntity> Searchable(bool value = true)
    {
        IsSearchable = value;
        return this;
    }

    public TableColumn<TEntity> Sortable(bool value = true)
    {
        IsSortable = value;
        return this;
    }

    public TableColumn<TEntity> Alignment(ColumnAlign alignment)
    {
        _alignment = alignment;
        return this;
    }

    /// <summary>Only meaningful on badge columns: maps the raw value to a tone name
    /// (neutral | info | success | warning | danger).</summary>
    public TableColumn<TEntity> Colors<TValue>(Func<TValue, string> selector)
    {
        _colorSelector = raw => raw is TValue typed ? selector(typed) : "neutral";
        return this;
    }

    /// <summary>Turns this column's raw value into the text a cell shows. The one method a
    /// column type usually needs to override.</summary>
    public virtual string FormatDisplay(object? value) => value?.ToString() ?? string.Empty;

    object? ITableColumn.GetValue(object entity) => _accessor((TEntity)entity);

    string ITableColumn.FormatDisplay(object entity) => FormatDisplay(_accessor((TEntity)entity));

    string? ITableColumn.GetBadgeColor(object entity) =>
        _colorSelector?.Invoke(_accessor((TEntity)entity));

    // Fixed to en-US rather than the server's current culture — money and date columns should
    // look the same regardless of what locale the container happens to boot with.
    protected static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");
}
