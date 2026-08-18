using System.Globalization;

namespace Fila.Rendering;

/// <summary>Tiny hex color helpers so a panel's PrimaryColor only has to be one hex string —
/// the hover shade and the light/dark "soft" tint used behind the active sidebar link are
/// derived from it rather than needing their own overrides.</summary>
internal static class ColorMath
{
    public static string Darken(string hex, double amount) => Mix(hex, "#000000", amount);

    public static string Mix(string hex, string towardHex, double amount)
    {
        var (r1, g1, b1) = Parse(hex);
        var (r2, g2, b2) = Parse(towardHex);
        return ToHex(Lerp(r1, r2, amount), Lerp(g1, g2, amount), Lerp(b1, b2, amount));
    }

    public static bool IsValidHex(string hex) =>
        hex.Length == 7 && hex[0] == '#' &&
        hex[1..].All(c => Uri.IsHexDigit(c));

    private static (byte R, byte G, byte B) Parse(string hex)
    {
        var span = hex.AsSpan().TrimStart('#');
        return (
            byte.Parse(span[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(span[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(span[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static string ToHex(byte r, byte g, byte b) => $"#{r:x2}{g:x2}{b:x2}";

    private static byte Lerp(byte from, byte to, double t) =>
        (byte)Math.Round(from + ((to - from) * t), MidpointRounding.AwayFromZero);
}
