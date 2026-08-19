namespace Fila.Widgets;

/// <summary>One card in a <see cref="StatsOverviewWidget"/> — Filament's
/// Widgets\StatsOverviewWidget\Stat. Built fluently off <see cref="Make"/>:
/// <c>Stat.Make("Customers", "3").Description("2 added this week").Icon("users").Color("success")</c>.
///
/// The property/setter split (DescriptionText/Description, IconName/Icon, ColorValue/Color)
/// follows Fila.Actions' Action: C# will not let a property and a method share a name, and the
/// fluent setter is the one call sites read, so it keeps the plain name.</summary>
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

    public string? DescriptionText { get; private set; }
    public string? IconName { get; private set; }

    /// <summary>"neutral" (default), "info", "success", "warning" or "danger" — the same
    /// vocabulary a table badge column uses, resolved against the same CSS tokens. Tints the
    /// description line and icon, which is how a stat shows a trend: a description of
    /// "12% increase" coloured "success".</summary>
    public string ColorValue { get; private set; } = "neutral";

    public Stat Description(string description)
    {
        DescriptionText = description;
        return this;
    }

    /// <summary>Names an icon from Fila.Support's IconRegistry, drawn beside the description.</summary>
    public Stat Icon(string icon)
    {
        IconName = icon;
        return this;
    }

    public Stat Color(string color)
    {
        ColorValue = color;
        return this;
    }
}
