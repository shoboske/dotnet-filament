using System.Linq.Expressions;
using Fila.Tables;

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

    public Form<T> Fields(params FormField<T>[] fields)
    {
        _fields = fields;
        return this;
    }

    public FormField<T> Text(Expression<Func<T, object?>> selector) => Field(FieldKind.Text, selector);

    public FormField<T> Textarea(Expression<Func<T, object?>> selector) => Field(FieldKind.Textarea, selector);

    public FormField<T> Number(Expression<Func<T, object?>> selector) => Field(FieldKind.Number, selector);

    public FormField<T> Select(Expression<Func<T, object?>> selector) => Field(FieldKind.Select, selector);

    public FormField<T> Date(Expression<Func<T, object?>> selector) => Field(FieldKind.Date, selector);

    public FormField<T> Boolean(Expression<Func<T, object?>> selector) => Field(FieldKind.Boolean, selector);

    private static FormField<T> Field(FieldKind kind, Expression<Func<T, object?>> selector)
    {
        var path = ExpressionPath.ToPath(selector);
        var property = typeof(T).GetProperty(path)
            ?? throw new NotSupportedException(
                $"Form fields must be direct properties of {typeof(T).Name}; '{path}' was not found.");

        return new FormField<T>(kind, property);
    }
}
