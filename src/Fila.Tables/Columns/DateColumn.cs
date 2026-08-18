using System.Linq.Expressions;

namespace Fila.Tables;

/// <summary>Formats the value as a date.</summary>
public sealed class DateColumn<TEntity>(Expression<Func<TEntity, object?>> selector)
    : TableColumn<TEntity>(selector)
{
    public override string FormatDisplay(object? value) => value switch
    {
        DateTime dt => dt.ToString("MMM d, yyyy", DisplayCulture),
        DateTimeOffset dto => dto.ToString("MMM d, yyyy", DisplayCulture),
        null => string.Empty,
        _ => value.ToString() ?? string.Empty,
    };
}
