using System.Linq.Expressions;

namespace Fila.Tables;

/// <summary>Shows a boolean as Yes or No.</summary>
public sealed class BooleanColumn<TEntity>(Expression<Func<TEntity, object?>> selector)
    : TableColumn<TEntity>(selector)
{
    public override string FormatDisplay(object? value) => value is true ? "Yes" : "No";
}
