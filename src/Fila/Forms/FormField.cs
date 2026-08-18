using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Fila.Forms;

public enum FieldKind
{
    Text,
    Textarea,
    Number,
    Select,
    Date,
    Boolean,
}

public sealed record SelectOption(string Value, string Label);

/// <summary>Non-generic view of a field, used by rendering and binding code that doesn't know
/// TEntity.</summary>
public interface IFormField
{
    string Path { get; }
    string Label { get; }
    FieldKind Kind { get; }
    bool IsRequired { get; }
    Type ValueType { get; }

    object? GetValue(object entity);
    void SetValue(object entity, object? value);

    /// <summary>Only meaningful on Select fields. Enum-backed selects auto-populate from the
    /// enum's members; anything else (e.g. a foreign key) needs an explicit .Options(...).</summary>
    IReadOnlyList<SelectOption>? ResolveOptions(DbContext db);
}

public sealed class FormField<T> : IFormField
{
    private readonly PropertyInfo _property;
    private string _label;
    private Func<DbContext, IEnumerable<SelectOption>>? _optionsFactory;

    internal FormField(FieldKind kind, PropertyInfo property)
    {
        Kind = kind;
        Path = property.Name;
        _property = property;
        _label = Humanize(property.Name);

        if (kind == FieldKind.Select && property.PropertyType.IsEnum)
        {
            var enumType = property.PropertyType;
            _optionsFactory = _ => Enum.GetNames(enumType).Select(n => new SelectOption(n, Humanize(n)));
        }
    }

    public string Path { get; }
    public FieldKind Kind { get; }
    public bool IsRequired { get; private set; }
    public Type ValueType => _property.PropertyType;

    string IFormField.Label => _label;

    public FormField<T> Label(string label)
    {
        _label = label;
        return this;
    }

    public FormField<T> Required(bool value = true)
    {
        IsRequired = value;
        return this;
    }

    /// <summary>Supplies the option list for a Select field — required for anything that isn't
    /// enum-backed (e.g. a foreign key select), since Fila has no way to guess what a raw int
    /// column should display.</summary>
    public FormField<T> Options(Func<DbContext, IEnumerable<SelectOption>> factory)
    {
        _optionsFactory = factory;
        return this;
    }

    object? IFormField.GetValue(object entity) => _property.GetValue(entity);

    void IFormField.SetValue(object entity, object? value) => _property.SetValue(entity, value);

    IReadOnlyList<SelectOption>? IFormField.ResolveOptions(DbContext db) =>
        _optionsFactory?.Invoke(db).ToList();

    private static string Humanize(string name)
    {
        var chars = new List<char>();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                chars.Add(' ');
            chars.Add(name[i]);
        }
        return new string(chars.ToArray());
    }
}
