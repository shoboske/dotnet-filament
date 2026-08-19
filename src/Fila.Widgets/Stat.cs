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

    /// <summary>An icon drawn before the description text — Filament's descriptionIcon(). Its
    /// colour follows DescriptionColor, unlike the label icon, which is always muted.</summary>
    public Stat DescriptionIcon(string icon)
    {
        DescriptionIconName = icon;
        return this;
    }

    public Stat DescriptionColor(string color)
    {
        DescriptionColorValue = color;
        return this;
    }
}
