using System.Globalization;
using System.Linq.Expressions;

namespace Fila.Tables;

/// <summary>The workhorse column. Money, dates, booleans and badges are configuration on this
/// class rather than classes of their own, which is how Filament models them: money() and
/// date() live in Tables\Columns\Concerns\CanFormatState, badge() on TextColumn itself, and
/// the standalone MoneyColumn/DateColumn never existed while BadgeColumn is deprecated in
/// favour of TextColumn->badge().
///
/// Boolean is the one liberty taken. Filament routes it through IconColumn->boolean(), which
/// needs an icon column Fila does not have yet; until it does, "Yes"/"No" is text and belongs
/// here.</summary>
public sealed class TextColumn<TEntity> : TableColumn<TEntity, TextColumn<TEntity>>
{
    // Fixed to en-US rather than the server's current culture — money and date columns should
    // look the same regardless of what locale the container happens to boot with.
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");

    private Func<object?, string>? _formatter;
    private bool _isBadge;

    public TextColumn(Expression<Func<TEntity, object?>> selector)
        : base(selector)
    {
    }

    public override string View => _isBadge ? "badge" : "text";

    /// <summary>Renders the value in a pill. Pair with .Colors(...) for a tone.</summary>
    public TextColumn<TEntity> Badge(bool value = true)
    {
        _isBadge = value;
        return this;
    }

    /// <summary>Formats the value as currency.</summary>
    public TextColumn<TEntity> Money() => FormatStateUsing(value => value switch
    {
        null => string.Empty,
        decimal d => d.ToString("C", DisplayCulture),
        double d => d.ToString("C", DisplayCulture),
        float f => f.ToString("C", DisplayCulture),
        _ => Convert.ToDecimal(value).ToString("C", DisplayCulture),
    });

    /// <summary>Formats the value as a date.</summary>
    public TextColumn<TEntity> Date() => FormatStateUsing(value => value switch
    {
        DateTime dt => dt.ToString("MMM d, yyyy", DisplayCulture),
        DateTimeOffset dto => dto.ToString("MMM d, yyyy", DisplayCulture),
        null => string.Empty,
        _ => value.ToString() ?? string.Empty,
    });

    /// <summary>Shows a boolean as Yes or No.</summary>
    public TextColumn<TEntity> Boolean() => FormatStateUsing(value => value is true ? "Yes" : "No");

    /// <summary>Replaces how the value is turned into cell text — Filament's
    /// formatStateUsing(). The escape hatch that means a one-off format needs no subclass at
    /// all; Money(), Date() and Boolean() are themselves nothing more than calls to it.</summary>
    public TextColumn<TEntity> FormatStateUsing(Func<object?, string> formatter)
    {
        _formatter = formatter;
        return this;
    }

    public override string FormatState(object? value) =>
        _formatter is null ? base.FormatState(value) : _formatter(value);
}
