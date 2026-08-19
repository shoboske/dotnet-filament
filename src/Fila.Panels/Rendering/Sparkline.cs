using System.Globalization;

namespace Fila.Panels.Rendering;

/// <summary>Turns a bare series of numbers into the two SVG paths a stat's trend sparkline is
/// drawn from — Fila's server-rendered stand-in for the Chart.js line
/// packages/widgets/resources/js/components/stats-overview/stat/chart.js builds.
///
/// That chart is configured with borderWidth 2, fill 'start', tension 0.5, no points, no axes
/// and no tooltip. Everything except the tension is CSS here; the tension is this class, which
/// reproduces Chart.js's own splineCurve: for each interior point, control points offset along
/// the vector between its neighbours, weighted by the relative length of the two segments.
/// Straight line segments (what tension 0 gives) look visibly different, so it is worth
/// matching rather than approximating.</summary>
internal static class Sparkline
{
    /// <summary>Chart.js's default `tension` for this chart.</summary>
    private const double Tension = 0.5;

    /// <summary>The viewBox the paths are built in. Width is arbitrary — the SVG stretches to
    /// the card with preserveAspectRatio="none" — and the height matches the canvas's h-6.</summary>
    public const double Width = 100;
    public const double Height = 24;

    /// <summary>Half the 2px stroke, so the line is not clipped when it touches an edge.</summary>
    private const double Inset = 1;

    /// <summary>The stroked line and the filled area under it, in that order. Both are empty
    /// when there is nothing to draw.</summary>
    public static (string Line, string Area) Build(IReadOnlyList<decimal> values)
    {
        if (values.Count < 2) return ("", "");

        var lo = values.Min();
        var hi = values.Max();

        var plotHeight = Height - Inset * 2;

        // A flat series has no range to scale into; Chart.js centres it, so this does too.
        double Y(decimal value) => hi == lo
            ? Height / 2
            : Inset + (1 - (double)((value - lo) / (hi - lo))) * plotHeight;

        double X(int i) => i * Width / (values.Count - 1);

        var points = values.Select((value, i) => (X: X(i), Y: Y(value))).ToArray();

        var line = new System.Text.StringBuilder();
        line.Append($"M {F(points[0].X)},{F(points[0].Y)}");

        for (var i = 0; i < points.Length - 1; i++)
        {
            var (cp2X, cp2Y) = Cap(ControlPointAfter(points, i));
            var (cp1X, cp1Y) = Cap(ControlPointBefore(points, i + 1));
            line.Append($" C {F(cp2X)},{F(cp2Y)} {F(cp1X)},{F(cp1Y)} {F(points[i + 1].X)},{F(points[i + 1].Y)}");
        }

        // fill: 'start' — closed down to the bottom of the box, not to the series' own minimum.
        var area = $"{line} L {F(Width)},{F(Height)} L 0,{F(Height)} Z";

        return (line.ToString(), area);
    }

    /// <summary>Chart.js's capBezierPoints, which defaults to true: control points are clamped
    /// into the chart area. Without it the smoothing overshoots past the top or bottom wherever
    /// a flat run meets a steep one — visible as the curve bulging outside the card.</summary>
    private static (double X, double Y) Cap((double X, double Y) point) =>
        (Math.Clamp(point.X, 0, Width), Math.Clamp(point.Y, Inset, Height - Inset));

    /// <summary>Chart.js splineCurve: the control point on the far side of <paramref name="i"/>
    /// from its predecessor. An endpoint has no neighbour to smooth against, so its control
    /// point is the point itself — the same thing Chart.js does.</summary>
    private static (double X, double Y) ControlPointAfter((double X, double Y)[] points, int i)
    {
        if (i == 0 || i == points.Length - 1) return points[i];

        var (previous, current, next) = (points[i - 1], points[i], points[i + 1]);
        var factor = Tension * SegmentWeight(previous, current, next).After;

        return (current.X + factor * (next.X - previous.X), current.Y + factor * (next.Y - previous.Y));
    }

    private static (double X, double Y) ControlPointBefore((double X, double Y)[] points, int i)
    {
        if (i == 0 || i == points.Length - 1) return points[i];

        var (previous, current, next) = (points[i - 1], points[i], points[i + 1]);
        var factor = Tension * SegmentWeight(previous, current, next).Before;

        return (current.X - factor * (next.X - previous.X), current.Y - factor * (next.Y - previous.Y));
    }

    /// <summary>How the smoothing is split between the two segments meeting at a point: the
    /// longer segment gets the larger control point, so an uneven series does not overshoot.</summary>
    private static (double Before, double After) SegmentWeight(
        (double X, double Y) previous, (double X, double Y) current, (double X, double Y) next)
    {
        var before = Distance(previous, current);
        var after = Distance(current, next);
        var total = before + after;

        return total == 0 ? (0, 0) : (before / total, after / total);
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

    /// <summary>Invariant, always: SVG path data is not localised, and a server running under a
    /// culture with a comma decimal separator would emit "12,5" and silently break the path.</summary>
    private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
