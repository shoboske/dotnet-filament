using System.Linq.Expressions;
using System.Reflection;
using Fila.Schemas;
using Fila.Support;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>Names the markup this field renders as. A value the renderer doesn't recognise
    /// falls back to an <c>&lt;input&gt;</c> of type <see cref="InputType"/>, which is what
    /// makes a consumer-authored field type renderable without touching Fila. Phase 3 (#5)
    /// turns this into a lookup in a real partial registry.</summary>
    string View { get; }

    /// <summary>The <c>type</c> attribute for fields rendered as a plain input.</summary>
    string InputType { get; }

    Type ValueType { get; }

    bool ResolveRequired(EvaluationContext context);

    object? GetValue(object entity);
    void SetValue(object entity, object? value);

    /// <summary>Only meaningful on <see cref="Select{TEntity}"/> fields. Enum-backed selects
    /// auto-populate from the enum's members; anything else (e.g. a foreign key) needs an
    /// explicit .Options(...).</summary>
    IReadOnlyList<SelectOption>? ResolveOptions(EvaluationContext context);
}

/// <summary>Base class for every form field. Subclass it — in Fila or in a consuming app — to
/// add a field type; there is no enum to extend and no switch to edit.
///
/// Every fluent setter returns <c>FormField&lt;TEntity&gt;</c> rather than the subclass, so the
/// shared settings can be applied in any order without each subclass re-declaring them.</summary>
public abstract class FormField<TEntity> : Component, IFormField
{
    private readonly PropertyInfo _property;
    private Evaluated<bool> _required;
    private Evaluated<IEnumerable<SelectOption>?> _options;

    protected FormField(Expression<Func<TEntity, object?>> selector)
        : this(ResolveProperty(selector))
    {
    }

    protected FormField(PropertyInfo property)
        : base(property.Name)
    {
        _property = property;
    }

    public string Path => Name;

    public virtual string View => "input";

    public virtual string InputType => "text";

    public Type ValueType => _property.PropertyType;

    /// <summary>The property this field is bound to — subclasses read it to configure
    /// themselves from the CLR type (Select auto-populates enum options this way).</summary>
    protected PropertyInfo Property => _property;

    public FormField<TEntity> Label(string label)
    {
        LabelValue = label;
        return this;
    }

    public FormField<TEntity> Label(Func<EvaluationContext, string> label)
    {
        LabelValue = Evaluated<string>.From(label);
        return this;
    }

    public FormField<TEntity> Required(bool value = true)
    {
        _required = value;
        return this;
    }

    public FormField<TEntity> Required(Func<EvaluationContext, bool> value)
    {
        _required = Evaluated<bool>.From(value);
        return this;
    }

    public FormField<TEntity> Visible(bool value = true)
    {
        VisibleValue = value;
        return this;
    }

    public FormField<TEntity> Visible(Func<EvaluationContext, bool> value)
    {
        VisibleValue = Evaluated<bool>.From(value);
        return this;
    }

    public FormField<TEntity> Hidden(bool value = true) => Visible(!value);

    public FormField<TEntity> Hidden(Func<EvaluationContext, bool> value) =>
        Visible(context => !value(context));

    /// <summary>Supplies the option list for a Select field — required for anything that isn't
    /// enum-backed (e.g. a foreign key select), since Fila has no way to guess what a raw int
    /// column should display.</summary>
    public FormField<TEntity> Options(Func<DbContext, IEnumerable<SelectOption>> factory)
    {
        _options = Evaluated<IEnumerable<SelectOption>?>.From(context => factory(context.RequireDb()));
        return this;
    }

    /// <summary>Options computed from the full evaluation context rather than the DbContext
    /// alone — for an option list that depends on the record being edited, the signed-in user,
    /// or another field's value.
    ///
    /// Deliberately not an overload of Options: two overloads differing only in the delegate's
    /// parameter type make every implicitly typed `db => ...` call site ambiguous (CS0121),
    /// which would break the existing DSL.</summary>
    public FormField<TEntity> OptionsFrom(Func<EvaluationContext, IEnumerable<SelectOption>> factory)
    {
        _options = Evaluated<IEnumerable<SelectOption>?>.From(context => factory(context));
        return this;
    }

    /// <summary>For subclasses that supply their own options, like Select's enum defaulting.</summary>
    protected void SetOptions(Func<EvaluationContext, IEnumerable<SelectOption>?> factory) =>
        _options = Evaluated<IEnumerable<SelectOption>?>.From(factory);

    public bool ResolveRequired(EvaluationContext context) => _required.Resolve(context);

    object? IFormField.GetValue(object entity) => _property.GetValue(entity);

    void IFormField.SetValue(object entity, object? value) => _property.SetValue(entity, value);

    IReadOnlyList<SelectOption>? IFormField.ResolveOptions(EvaluationContext context) =>
        _options.Resolve(context)?.ToList();

    private static PropertyInfo ResolveProperty(Expression<Func<TEntity, object?>> selector)
    {
        var path = ExpressionPath.ToPath(selector);
        return typeof(TEntity).GetProperty(path)
            ?? throw new NotSupportedException(
                $"Form fields must be direct properties of {typeof(TEntity).Name}; '{path}' was not found.");
    }
}
