namespace Fila.Widgets;

/// <summary>One card in a <see cref="StatsOverviewWidget"/> — Filament's
/// Widgets\StatsOverviewWidget\Stat. Built fluently off <see cref="Make"/>:
/// <c>Stat.Make("Customers", "3").Icon("users").Description("2 added this week").DescriptionColor("success")</c>.
///
/// Filament's stat.blade.php draws two independent icons: <see cref="IconName"/> sits in the
/// label row beside the label, and <see cref="DescriptionIconName"/> sits in the description
/// row. They are separate settings there (icon() and descriptionIcon()) and separate here.
///
/// The property/setter split (DescriptionText/Description, IconName/Icon, ...) follows
/// Fila.Actions' Action: C# will not let a property and a method share a name, and the fluent
/// setter is the one call sites read, so it keeps the plain name.</summary>
public sealed class Stat
{
    private Stat(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public static Stat Make(string label, string value) => new(label, value);

    /// <summary>What the number means — "Customers", "Revenue".</summary>
    public string Label { get; }

    /// <summary>Pre-formatted for display. A widget that wants a currency or a percentage
    /// formats it on the way in; nothing downstream reinterprets this string.</summary>
    public string Value { get; }

    public string? IconName { get; private set; }
    public string? DescriptionText { get; private set; }
    public string? DescriptionIconName { get; private set; }

    /// <summary>"after" (Filament's default — getDescriptionIconPosition() falls back to
    /// IconPosition::After) or "before".</summary>
    public string DescriptionIconPositionValue { get; private set; } = "after";

    /// <summary>The trend sparkline drawn across the bottom of the card — Filament's
    /// Stat::chart(). Null draws none. Values only: the chart has no axes, no points and no
    /// tooltip, so nothing labels them.</summary>
    public IReadOnlyList<decimal>? ChartValues { get; private set; }

    /// <summary>Colour of the sparkline, independent of DescriptionColor — Filament's
    /// chartColor(), defaulting to "gray" the same way its blade does.</summary>
    public string ChartColorValue { get; private set; } = "gray";

    /// <summary>"gray" (Filament's default when none is set), "info", "success", "warning" or
    /// "danger" — the same vocabulary a table badge column uses. Tints the description row and
    /// its icon, which is how a stat shows a trend: a description of "12% increase" coloured
    /// "success".</summary>
    public string DescriptionColorValue { get; private set; } = "gray";

    /// <summary>Names an icon from Fila.Support's IconRegistry, drawn beside the label.</summary>
    public Stat Icon(string icon)
    {
        IconName = icon;
        return this;
    }

    public Stat Description(string description)
    {
        DescriptionText = description;
        return this;
    }

    /// <summary>An icon drawn beside the description text — Filament's descriptionIcon(),
    /// which takes the same optional position argument and defaults it to "after". Its colour
    /// follows DescriptionColor, unlike the label icon, which is always muted.</summary>
    public Stat DescriptionIcon(string icon, string position = "after")
    {
        DescriptionIconName = icon;
        DescriptionIconPositionValue = position;
        return this;
    }

    public Stat DescriptionColor(string color)
    {
        DescriptionColorValue = color;
        return this;
    }

    /// <summary>Draws a trend sparkline along the bottom of the card. Fewer than two values
    /// draws nothing — a single point is not a trend.</summary>
    public Stat Chart(IReadOnlyList<decimal> values)
    {
        ChartValues = values;
        return this;
    }

    public Stat ChartColor(string color)
    {
        ChartColorValue = color;
        return this;
    }
}
