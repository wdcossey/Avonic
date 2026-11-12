using System.Globalization;
using Avalonia.Animation.Easings;

namespace Avonic.Components.Animation;

/// <summary>
/// CSS <c>cubic-bezier(x1, y1, x2, y2)</c>, solved for x as Chromium's <c>UnitBezier</c> does: Newton's method, then bisection.
/// An Avalonia <see cref="Easing"/>, so a transition can use it too. Avalonia's <c>SplineEasing</c> is about 2e-3 out (#92).
/// </summary>
public sealed class CubicBezierEasing : Easing
{
    private const double Epsilon = 1e-7;
    private readonly double _ax, _bx, _cx, _ay, _by, _cy;

    /// <summary>CSS's <c>ease</c>, <c>cubic-bezier(0.25, 0.1, 0.25, 1)</c>: a transition's timing function when it names none.</summary>
    internal static readonly CubicBezierEasing CssEase = new(0.25, 0.1, 0.25, 1);

    /// <exception cref="ArgumentOutOfRangeException">An x outside 0..1, which CSS rejects.</exception>
    public CubicBezierEasing(double x1, double y1, double x2, double y2)
    {
        if (x1 is < 0 or > 1 || x2 is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(x1), "A cubic-bezier's x values are from 0 to 1.");
        }

        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        _cx = 3 * x1;
        _bx = 3 * (x2 - x1) - _cx;
        _ax = 1 - _cx - _bx;
        _cy = 3 * y1;
        _by = 3 * (y2 - y1) - _cy;
        _ay = 1 - _cy - _by;
    }

    public double X1 { get; }

    public double Y1 { get; }

    public double X2 { get; }

    public double Y2 { get; }

    /// <summary>The output progress for an input progress in 0..1.</summary>
    public override double Ease(double progress)
    {
        if (progress <= 0) return 0;
        if (progress >= 1) return 1;
        return SampleY(SolveX(progress));
    }

    /// <summary>
    /// A CSS <c>&lt;easing-function&gt;</c>: <c>linear</c>, <c>ease</c>, <c>ease-in</c>, <c>ease-out</c>, <c>ease-in-out</c>
    /// or <c>cubic-bezier(x1, y1, x2, y2)</c>. Null for <c>linear</c>.
    /// </summary>
    /// <exception cref="ArgumentException">Anything else, such as <c>steps()</c>, which no Ionic animation uses.</exception>
    public static CubicBezierEasing? ParseCss(string easing)
    {
        var text = easing.Trim();
        switch (text)
        {
            case "linear":
                return null;
            case "ease":
                return new CubicBezierEasing(0.25, 0.1, 0.25, 1);
            case "ease-in":
                return new CubicBezierEasing(0.42, 0, 1, 1);
            case "ease-out":
                return new CubicBezierEasing(0, 0, 0.58, 1);
            case "ease-in-out":
                return new CubicBezierEasing(0.42, 0, 0.58, 1);
        }

        if (text.StartsWith("cubic-bezier(", StringComparison.Ordinal) && text.EndsWith(')'))
        {
            var parts = text["cubic-bezier(".Length..^1].Split(',');
            var values = new double[4];
            if (parts.Length == 4 && parts.Select((p, i) => double.TryParse(p.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i])).All(ok => ok))
            {
                return new CubicBezierEasing(values[0], values[1], values[2], values[3]);
            }
        }

        throw new ArgumentException(
            $"'{easing}' isn't a CSS easing this supports: linear, ease, ease-in, ease-out, ease-in-out or cubic-bezier().", nameof(easing));
    }

    private double SampleX(double t) => ((_ax * t + _bx) * t + _cx) * t;

    private double SampleY(double t) => ((_ay * t + _by) * t + _cy) * t;

    private double SampleDerivativeX(double t) => (3 * _ax * t + 2 * _bx) * t + _cx;

    private double SolveX(double x)
    {
        var t = x;
        for (var i = 0; i < 8; i++)
        {
            var error = SampleX(t) - x;
            if (Math.Abs(error) < Epsilon)
            {
                return t;
            }

            var derivative = SampleDerivativeX(t);
            if (Math.Abs(derivative) < 1e-6)
            {
                break;
            }

            t -= error / derivative;
        }

        double low = 0, high = 1;
        t = x;
        for (var i = 0; i < 64 && low < high; i++)
        {
            var sample = SampleX(t);
            if (Math.Abs(sample - x) < Epsilon)
            {
                return t;
            }

            if (x > sample)
            {
                low = t;
            }
            else
            {
                high = t;
            }

            t = (high - low) * 0.5 + low;
        }

        return t;
    }
}
