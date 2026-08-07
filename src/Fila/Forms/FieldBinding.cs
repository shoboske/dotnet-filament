using System.Globalization;

namespace Fila.Forms;

/// <summary>Converts between a submitted form value (always a string) and a field's real CLR
/// type, and back again for pre-filling an edit form. Kept deliberately simple — no
/// TypeConverter/DataAnnotations machinery, just the handful of types Fila's field kinds
/// actually produce.</summary>
public static class FieldBinding
{
    public static object? Parse(Type targetType, string? raw)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = type != targetType || !targetType.IsValueType;

        if (string.IsNullOrEmpty(raw))
        {
            if (isNullable) return null;
            if (type == typeof(bool)) return false;
            return Activator.CreateInstance(type);
        }

        if (type == typeof(string)) return raw;
        if (type == typeof(bool)) return raw is "true" or "on" or "1";
        if (type.IsEnum) return Enum.Parse(type, raw);
        if (type == typeof(Guid)) return Guid.Parse(raw);
        if (type == typeof(DateTime)) return DateTime.Parse(raw, CultureInfo.InvariantCulture);
        if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture);

        return Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
    }

    public static string Format(object? value) => value switch
    {
        null => "",
        // A blank new entity's non-nullable DateTime is CLR default (0001-01-01), not a real
        // date the user set — render it as an empty field rather than a confusing ancient one.
        DateTime dt when dt == default => "",
        DateTimeOffset dto when dto == default => "",
        bool b => b ? "true" : "false",
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeOffset dto => dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Enum e => e.ToString(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
