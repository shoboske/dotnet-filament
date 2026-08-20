using System.Linq.Expressions;
using Fila.Schemas;
using Fila.Support;

namespace Fila.Infolists;

/// <summary>Non-generic view of an entry, used by rendering code that doesn't know TEntity —
/// the infolist counterpart to Fila.Tables.ITableColumn. Built on Fila.Schemas.IComponent
/// rather than mirroring ITableColumn's own base: an entry is a node of a schema the same way a
/// form field is, where a table column deliberately is not (see IComponent's doc comment).</summary>
public interface IEntry : IComponent
{
    /// <summary>The property this entry reads. Same value as <see cref="IComponent.Name"/>.</summary>
    string Path { get; }

    /// <summary>Names the markup this entry renders as — resolved to a partial by
    /// Fila.Panels.Rendering.ComponentViewRegistry the same way IFormField.View/
    /// ITableColumn.View are.</summary>
    string View { get; }

    object? GetValue(object entity);
    string FormatDisplay(object entity);
    string? GetBadgeColor(object entity);
}

/// <summary>Base class for every infolist entry, and the type an entry collection is declared
/// in terms of. Subclass <see cref="Entry{TEntity, TSelf}"/> rather than this one.</summary>
public abstract class Entry<TEntity> : Component, IEntry
{
    private readonly Func<TEntity, object?> _accessor;

    private protected Entry(Expression<Func<TEntity, object?>> selector)
        : base(ExpressionPath.ToPath(selector))
    {
        _accessor = selector.Compile();
    }

    public string Path => Name;

    public virtual string View => "text";

    private protected Func<object?, string>? ColorSelector { get; set; }

    /// <summary>Turns this entry's raw value into the text it displays — Filament's
    /// Entry::formatState(). The one method an entry type usually needs to override.</summary>
    public virtual string FormatState(object? value) => value?.ToString() ?? string.Empty;

    object? IEntry.GetValue(object entity) => _accessor((TEntity)entity);

    string IEntry.FormatDisplay(object entity) => FormatState(_accessor((TEntity)entity));

    string? IEntry.GetBadgeColor(object entity) => ColorSelector?.Invoke(_accessor((TEntity)entity));
}

/// <summary>The fluent surface every entry shares, typed so each setter hands back the
/// subclass — same reason Fila.Tables.TableColumn&lt;TEntity, TSelf&gt; exists.
///
/// An entry type declared outside Fila extends this:
/// <c>class RatingEntry&lt;T&gt; : Entry&lt;T, RatingEntry&lt;T&gt;&gt;</c>.</summary>
public abstract class Entry<TEntity, TSelf> : Entry<TEntity>
    where TSelf : Entry<TEntity, TSelf>
{
    protected Entry(Expression<Func<TEntity, object?>> selector)
        : base(selector)
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

    /// <summary>Maps the raw value to a tone name (neutral | info | success | warning |
    /// danger). Only shows up on an entry rendering as a badge.</summary>
    public TSelf Colors<TValue>(Func<TValue, string> selector)
    {
        ColorSelector = raw => raw is TValue typed ? selector(typed) : "neutral";
        return Self;
    }
}
