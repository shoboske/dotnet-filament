using System.Linq.Expressions;

namespace Fila.Tables;

/// <summary>Formats the value as currency.</summary>
public sealed class MoneyColumn<TEntity>(Expression<Func<TEntity, object?>> selector)
    : TableColumn<TEntity>(selector)
{
    public override string FormatDisplay(object? value) => value switch
    {
        null => string.Empty,
        decimal d => d.ToString("C", DisplayCulture),
        double d => d.ToString("C", DisplayCulture),
        float f => f.ToString("C", DisplayCulture),
        _ => Convert.ToDecimal(value).ToString("C", DisplayCulture),
    };
}
