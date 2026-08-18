using System.Linq.Expressions;

namespace Fila.Forms;

/// <summary>A dropdown. An enum-backed property populates itself from the enum's members;
/// anything else — a foreign key, say — needs an explicit .Options(...) or .OptionsFrom(...),
/// since there is no way to guess what a raw int should display as.</summary>
public sealed class Select<TEntity> : FormField<TEntity>
{
    public Select(Expression<Func<TEntity, object?>> selector)
        : base(selector)
    {
        if (!Property.PropertyType.IsEnum) return;

        var enumType = Property.PropertyType;
        SetOptions(_ => Enum.GetNames(enumType).Select(name => new SelectOption(name, Humanize(name))));
    }

    public override string View => "select";
}
