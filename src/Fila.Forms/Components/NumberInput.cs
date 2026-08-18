using System.Linq.Expressions;

namespace Fila.Forms;

/// <summary>A numeric input. Its own view rather than an InputType, because it also carries
/// step="any" — without it a browser rejects decimals on an integer-stepped input.</summary>
public sealed class NumberInput<TEntity>(Expression<Func<TEntity, object?>> selector)
    : FormField<TEntity>(selector)
{
    public override string View => "number";

    public override string InputType => "number";
}
