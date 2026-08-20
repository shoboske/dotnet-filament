using System.Globalization;
using System.Linq.Expressions;

namespace Fila.Infolists;

/// <summary>The workhorse entry, mirroring Fila.Tables.TextColumn exactly: money, dates and
/// badges are configuration on this class rather than classes of their own — the same
/// modelling choice, for the same reason (see TextColumn's own doc comment).</summary>
public sealed class TextEntry<TEntity> : Entry<TEntity, TextEntry<TEntity>>
{
    // Fixed to en-US rather than the server's current culture, matching TextColumn.Money()/Date().
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");

    private Func<object?, string>? _formatter;
    private bool _isBadge;

    public TextEntry(Expression<Func<TEntity, object?>> selector)
        : base(selector)
    {
    }

    public override string View => _isBadge ? "badge" : "text";

    /// <summary>Renders the value in a pill. Pair with .Colors(...) for a tone.</summary>
    public TextEntry<TEntity> Badge(bool value = true)
    {
        _isBadge = value;
        return this;
    }

    /// <summary>Formats the value as currency.</summary>
    public TextEntry<TEntity> Money() => FormatStateUsing(value => value switch
    {
        null => string.Empty,
        decimal d => d.ToString("C", DisplayCulture),
        double d => d.ToString("C", DisplayCulture),
        float f => f.ToString("C", DisplayCulture),
        _ => Convert.ToDecimal(value).ToString("C", DisplayCulture),
    });

    /// <summary>Formats the value as a date.</summary>
    public TextEntry<TEntity> Date() => FormatStateUsing(value => value switch
    {
        DateTime dt => dt.ToString("MMM d, yyyy", DisplayCulture),
        DateTimeOffset dto => dto.ToString("MMM d, yyyy", DisplayCulture),
        null => string.Empty,
        _ => value.ToString() ?? string.Empty,
    });

    /// <summary>Shows a boolean as Yes or No.</summary>
    public TextEntry<TEntity> Boolean() => FormatStateUsing(value => value is true ? "Yes" : "No");

    /// <summary>Replaces how the value is turned into display text — Filament's
    /// formatStateUsing(). Money(), Date() and Boolean() are themselves nothing more than
    /// calls to it.</summary>
    public TextEntry<TEntity> FormatStateUsing(Func<object?, string> formatter)
    {
        _formatter = formatter;
        return this;
    }

    public override string FormatState(object? value) =>
        _formatter is null ? base.FormatState(value) : _formatter(value);
}
