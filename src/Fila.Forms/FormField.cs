using System.Linq.Expressions;
using System.Reflection;
using Fila.Schemas;
using Fila.Support;

namespace Fila.Forms;

public sealed record SelectOption(string Value, string Label);

/// <summary>Non-generic view of a field, used by rendering and binding code that doesn't know
/// TEntity.</summary>
public interface IFormField : IComponent
{
    /// <summary>The property this field reads and writes. Same value as
    /// <see cref="IComponent.Name"/>; kept under the older name because query and binding code
    /// talks in paths.</summary>
    string Path { get; }

    /// <summary>Names the markup this field renders as — Filament's ViewComponent::$view. A
    /// value the renderer doesn't recognise falls back to an <c>&lt;input&gt;</c> of type
    /// <see cref="InputType"/>, which is what makes a consumer-authored field type renderable
    /// without touching Fila. Phase 3 (#5) turns this into a real partial registry.</summary>
    string View { get; }

    /// <summary>The <c>type</c> attribute for fields rendered as a plain input — Filament's
    /// TextInput::getType(), which likewise reports "number" for a numeric input rather than
    /// there being a separate class for one.</summary>
    string InputType { get; }

    /// <summary>The <c>step</c> attribute, or null to omit it. Filament's HasStep concern;
    /// ->numeric() sets it to "any" there for the same reason it does here — without it a
    /// browser rejects decimals on an integer-stepped input.</summary>
    string? Step { get; }

    Type ValueType { get; }

    bool ResolveRequired(EvaluationContext context);

    object? GetValue(object entity);
    void SetValue(object entity, object? value);
}

/// <summary>A field that offers a fixed set of choices. Kept off <see cref="IFormField"/> so
/// the option machinery lives only on the field type that has options, the way Filament keeps
/// its per-type capabilities in Forms\Components\Contracts.</summary>
public interface ISelectField
{
    IReadOnlyList<SelectOption>? ResolveOptions(EvaluationContext context);
}

/// <summary>Base class for every form field, and the type a field collection is declared in
/// terms of. Subclass <see cref="FormField{TEntity, TSelf}"/> rather than this one — the self
/// type is what keeps the fluent setters chainable in any order.</summary>
public abstract class FormField<TEntity> : Component, IFormField
{
    private readonly PropertyInfo _property;

    private protected FormField(Expression<Func<TEntity, object?>> selector)
        : this(ResolveProperty(selector))
    {
    }

    private protected FormField(PropertyInfo property)
        : base(property.Name)
    {
        _property = property;
    }

    public string Path => Name;

    public virtual string View => "input";

    public virtual string InputType => "text";

    public virtual string? Step => null;

    public Type ValueType => _property.PropertyType;

    /// <summary>The property this field is bound to — subclasses read it to configure
    /// themselves from the CLR type (Select auto-populates enum options this way).</summary>
    protected PropertyInfo Property => _property;

    private protected Evaluated<bool> RequiredValue { get; set; }

    public bool ResolveRequired(EvaluationContext context) => RequiredValue.Resolve(context);

    object? IFormField.GetValue(object entity) => _property.GetValue(entity);

    void IFormField.SetValue(object entity, object? value) => _property.SetValue(entity, value);

    private static PropertyInfo ResolveProperty(Expression<Func<TEntity, object?>> selector)
    {
        var path = ExpressionPath.ToPath(selector);
        return typeof(TEntity).GetProperty(path)
            ?? throw new NotSupportedException(
                $"Form fields must be direct properties of {typeof(TEntity).Name}; '{path}' was not found.");
    }
}

/// <summary>The fluent surface every field shares, typed so each setter hands back the
/// subclass — the C# stand-in for PHP's <c>static</c> return type. It is what lets
/// <c>.Required().Options(...)</c> compile: without it, .Required() would hand back the base
/// and lose the Select the caller was chaining on.
///
/// A field type declared outside Fila extends this:
/// <c>class ColorPicker&lt;T&gt; : FormField&lt;T, ColorPicker&lt;T&gt;&gt;</c>.</summary>
public abstract class FormField<TEntity, TSelf> : FormField<TEntity>
    where TSelf : FormField<TEntity, TSelf>
{
    protected FormField(Expression<Func<TEntity, object?>> selector)
        : base(selector)
    {
    }

    protected FormField(PropertyInfo property)
        : base(property)
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

    public TSelf Required(bool value = true)
    {
        RequiredValue = value;
        return Self;
    }

    public TSelf Required(Func<EvaluationContext, bool> value)
    {
        RequiredValue = Evaluated<bool>.From(value);
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
}
