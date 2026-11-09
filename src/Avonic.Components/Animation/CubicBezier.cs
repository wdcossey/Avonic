using Avalonia;

namespace Avonic.Components.Animation;

/// <summary>Ionic's <c>utils/animation/cubic-bezier.js</c>, ported line for line.</summary>
public static class CubicBezier
{
    /// <summary>
    /// Ionic <c>getTimeGivenProgression</c>: the times (x) at which a cubic Bézier through <paramref name="p0"/> to
    /// <paramref name="p3"/> reaches <paramref name="progression"/> (y). Empty if it never does. The menu's and the sheet modal's
    /// gestures use it to resume an eased animation from where a linear drag left it.
    /// </summary>
    public static IReadOnlyList<double> GetTimeGivenProgression(Point p0, Point p1, Point p2, Point p3, double progression) =>
        SolveCubicBezier(p0.Y, p1.Y, p2.Y, p3.Y, progression)
            .Select(t => SolveCubicParametricEquation(p0.X, p1.X, p2.X, p3.X, t))
            .ToArray();

    private static double SolveCubicParametricEquation(double p0, double p1, double p2, double p3, double t)
    {
        var partA = 3 * p1 * Math.Pow(t - 1, 2);
        var partB = -3 * p2 * t + 3 * p2 + p3 * t;
        var partC = p0 * Math.Pow(t - 1, 3);
        return t * (partA + t * partB) - partC;
    }

    private static IEnumerable<double> SolveCubicBezier(double p0, double p1, double p2, double p3, double refPoint)
    {
        p0 -= refPoint;
        p1 -= refPoint;
        p2 -= refPoint;
        p3 -= refPoint;
        var roots = SolveCubicEquation(p3 - 3 * p2 + 3 * p1 - p0, 3 * p2 - 6 * p1 + 3 * p0, 3 * p1 - 3 * p0, p0);
        return roots.Where(root => root >= 0 && root <= 1);
    }

    private static double[] SolveQuadraticEquation(double a, double b, double c)
    {
        var discriminant = b * b - 4 * a * c;
        if (discriminant < 0)
        {
            return [];
        }

        return [(-b + Math.Sqrt(discriminant)) / (2 * a), (-b - Math.Sqrt(discriminant)) / (2 * a)];
    }

    private static double[] SolveCubicEquation(double a, double b, double c, double d)
    {
        if (a == 0)
        {
            return SolveQuadraticEquation(b, c, d);
        }

        b /= a;
        c /= a;
        d /= a;
        var p = (3 * c - b * b) / 3;
        var q = (2 * b * b * b - 9 * b * c + 27 * d) / 27;
        if (p == 0)
        {
            // JavaScript's Math.pow of a negative number to 1/3 is NaN, which the caller's filter drops. So is .NET's.
            return [Math.Pow(-q, 1.0 / 3)];
        }

        if (q == 0)
        {
            return [Math.Sqrt(-p), -Math.Sqrt(-p)];
        }

        var discriminant = Math.Pow(q / 2, 2) + Math.Pow(p / 3, 3);
        if (discriminant == 0)
        {
            return [Math.Pow(q / 2, 1.0 / 2) - b / 3];
        }

        if (discriminant > 0)
        {
            return
            [
                Math.Pow(-(q / 2) + Math.Sqrt(discriminant), 1.0 / 3) - Math.Pow(q / 2 + Math.Sqrt(discriminant), 1.0 / 3) - b / 3,
            ];
        }

        var r = Math.Sqrt(Math.Pow(-(p / 3), 3));
        var phi = Math.Acos(-(q / (2 * Math.Sqrt(Math.Pow(-(p / 3), 3)))));
        var s = 2 * Math.Pow(r, 1.0 / 3);
        return
        [
            s * Math.Cos(phi / 3) - b / 3,
            s * Math.Cos((phi + 2 * Math.PI) / 3) - b / 3,
            s * Math.Cos((phi + 4 * Math.PI) / 3) - b / 3,
        ];
    }
}
