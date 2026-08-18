using System.Linq.Expressions;
using Fila.Support;
using Microsoft.EntityFrameworkCore;

namespace Fila.Forms;

/// <summary>A dropdown. An enum-backed property populates itself from the enum's members;
/// anything else — a foreign key, say — needs an explicit .Options(...) or .OptionsFrom(...),
/// since there is no way to guess what a raw int should display as.
///
/// The options live here rather than on the base because only this field type has any, which
/// is where Filament puts them too.</summary>
public sealed class Select<TEntity> : FormField<TEntity, Select<TEntity>>, ISelectField
{
    private Evaluated<IEnumerable<SelectOption>?> _options;

    public Select(Expression<Func<TEntity, object?>> selector)
        : base(selector)
    {
        if (!Property.PropertyType.IsEnum) return;

        var enumType = Property.PropertyType;
        _options = Evaluated<IEnumerable<SelectOption>?>.From(
            _ => Enum.GetNames(enumType).Select(name => new SelectOption(name, ComponentText.Humanize(name))));
    }

    public override string View => "select";

    /// <summary>Supplies the option list — required for anything that isn't enum-backed (e.g. a
    /// foreign key select).</summary>
    public Select<TEntity> Options(Func<DbContext, IEnumerable<SelectOption>> factory)
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
    public Select<TEntity> OptionsFrom(Func<EvaluationContext, IEnumerable<SelectOption>> factory)
    {
        _options = Evaluated<IEnumerable<SelectOption>?>.From(context => factory(context));
        return this;
    }

    IReadOnlyList<SelectOption>? ISelectField.ResolveOptions(EvaluationContext context) =>
        _options.Resolve(context)?.ToList();
}
