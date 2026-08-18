using System.Linq.Expressions;

namespace Fila.Forms;

/// <summary>Non-generic view of a form, used by rendering and routing code that doesn't know
/// TEntity.</summary>
public interface IForm
{
    IReadOnlyList<IFormField> Fields { get; }
}

public sealed class Form<T> : IForm
{
    private IReadOnlyList<IFormField> _fields = [];

    IReadOnlyList<IFormField> IForm.Fields => _fields;

    /// <summary>Takes the base type, so a field type declared outside Fila drops into the same
    /// call: .Fields(new ColorPicker&lt;Product&gt;(p =&gt; p.Hex).Required()).</summary>
    public Form<T> Fields(params FormField<T>[] fields)
    {
        _fields = fields;
        return this;
    }

    public TextInput<T> Text(Expression<Func<T, object?>> selector) => new(selector);

    public Textarea<T> Textarea(Expression<Func<T, object?>> selector) => new(selector);

    public NumberInput<T> Number(Expression<Func<T, object?>> selector) => new(selector);

    public Select<T> Select(Expression<Func<T, object?>> selector) => new(selector);

    public DatePicker<T> Date(Expression<Func<T, object?>> selector) => new(selector);

    public Checkbox<T> Boolean(Expression<Func<T, object?>> selector) => new(selector);
}
