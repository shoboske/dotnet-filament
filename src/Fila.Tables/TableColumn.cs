using System.Linq.Expressions;
using Fila.Support;

namespace Fila.Tables;

public enum ColumnAlign
{
    Start,
    Center,
    End,
}

/// <summary>Non-generic view of a column, used by rendering and query code that doesn't know
/// TEntity. Mirrors Filament's Tables\Columns\Column, which carries its own name, label and
/// visibility rather than implementing a schema contract.</summary>
public interface ITableColumn
{
    /// <summary>The property path this column reads.</summary>
    string Path { get; }

    /// <summary>Names the markup this column's cells render as — Filament's
    /// ViewComponent::$view. Resolved to a partial by
    /// Fila.Panels.Rendering.ComponentViewRegistry; a value the registry doesn't recognise
    /// renders as plain text, which is what lets a consumer-authored column type work without
    /// touching Fila.</summary>
    string View { get; }

    ColumnAlign Alignment { get; }
    bool IsSearchable { get; }
    bool IsSortable { get; }

    string ResolveLabel(EvaluationContext context);
    bool ResolveVisible(EvaluationContext context);

    object? GetValue(object entity);
    string FormatDisplay(object entity);
    string? GetBadgeColor(object entity);
}

/// <summary>Base class for every table column, and the type a column collection is declared in
/// terms of. Subclass <see cref="TableColumn{TEntity, TSelf}"/> rather than this one — the
/// self type is what keeps the fluent setters chainable in any order.</summary>
public abstract class TableColumn<TEntity> : ITableColumn
{
    private readonly Func<TEntity, object?> _accessor;

    private protected TableColumn(Expression<Func<TEntity, object?>> selector)
        : this(ExpressionPath.ToPath(selector), selector.Compile())
    {
    }

    private protected TableColumn(string path, Func<TEntity, object?> accessor)
    {
        Path = path;
        _accessor = accessor;
        LabelValue = ComponentText.Humanize(path);
    }

    public string Path { get; }

    public virtual string View => "text";

    public bool IsSearchable { get; private protected set; }

    public bool IsSortable { get; private protected set; }

    private protected Evaluated<string> LabelValue { get; set; }

    private protected Evaluated<bool> VisibleValue { get; set; } = true;

    private protected ColumnAlign AlignmentValue { get; set; } = ColumnAlign.Start;

    private protected Func<object?, string>? ColorSelector { get; set; }

    ColumnAlign ITableColumn.Alignment => AlignmentValue;

    public string ResolveLabel(EvaluationContext context) => LabelValue.Resolve(context) ?? Path;

    public bool ResolveVisible(EvaluationContext context) => VisibleValue.Resolve(context);

    /// <summary>Turns this column's raw value into the text a cell shows — Filament's
    /// Column::formatState. The one method a column type usually needs to override.</summary>
    public virtual string FormatState(object? value) => value?.ToString() ?? string.Empty;

    object? ITableColumn.GetValue(object entity) => _accessor((TEntity)entity);

    string ITableColumn.FormatDisplay(object entity) => FormatState(_accessor((TEntity)entity));

    string? ITableColumn.GetBadgeColor(object entity) =>
        ColorSelector?.Invoke(_accessor((TEntity)entity));
}

/// <summary>The fluent surface every column shares, typed so each setter hands back the
/// subclass. That is what PHP gives Filament for free through <c>static</c> return types, and
/// what lets <c>.Sortable().Money()</c> and <c>.Money().Sortable()</c> both compile —
/// returning the base from a shared setter would have made only one of those orders work.
///
/// A column type declared outside Fila extends this:
/// <c>class RatingColumn&lt;T&gt; : TableColumn&lt;T, RatingColumn&lt;T&gt;&gt;</c>.</summary>
public abstract class TableColumn<TEntity, TSelf> : TableColumn<TEntity>
    where TSelf : TableColumn<TEntity, TSelf>
{
    protected TableColumn(Expression<Func<TEntity, object?>> selector)
        : base(selector)
    {
    }

    protected TableColumn(string path, Func<TEntity, object?> accessor)
        : base(path, accessor)
    {
    }

    private TSelf Self => (TSelf)this;

    public TSelf Label(string label)
    {
        LabelValue = label;
        return Self;
    }

    public TSelf Label(Func<EvaluationContext, string> label)
    {
        LabelValue = Evaluated<string>.From(label);
        return Self;
    }

    public TSelf Visible(bool value = true)
    {
        VisibleValue = value;
        return Self;
    }

    public TSelf Visible(Func<EvaluationContext, bool> value)
    {
        VisibleValue = Evaluated<bool>.From(value);
        return Self;
    }

    public TSelf Hidden(bool value = true) => Visible(!value);

    public TSelf Hidden(Func<EvaluationContext, bool> value) => Visible(context => !value(context));

    public TSelf Searchable(bool value = true)
    {
        IsSearchable = value;
        return Self;
    }

    public TSelf Sortable(bool value = true)
    {
        IsSortable = value;
        return Self;
    }

    public TSelf Alignment(ColumnAlign alignment)
    {
        AlignmentValue = alignment;
        return Self;
    }

    /// <summary>Maps the raw value to a tone name (neutral | info | success | warning |
    /// danger). Only shows up on a column rendering as a badge. Filament's HasColor concern is
    /// shared across column types the same way rather than living on one of them.</summary>
    public TSelf Colors<TValue>(Func<TValue, string> selector)
    {
        ColorSelector = raw => raw is TValue typed ? selector(typed) : "neutral";
        return Self;
    }
}
