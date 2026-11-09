using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// The four <c>@keyframes</c> of <c>progress-bar.md.css</c>, each 2 s, linear, forever. A keyframe's
/// <c>animation-timing-function</c> eases the interval that starts there. Translate is a fraction of the bar's width, and
/// scale is a factor about the span's centre.
/// </summary>
internal static class IndeterminateKeyframes
{
    /// <summary><c>.indeterminate-bar-primary { left: -145.166611% }</c>.</summary>
    internal const double PrimaryLeft = -1.45166611;

    /// <summary><c>.indeterminate-bar-secondary { left: -54.888891% }</c>.</summary>
    internal const double SecondaryLeft = -0.54888891;

    private readonly record struct Segment(double Start, double End, double From, double To, CubicBezierEasing? Easing);

    private static readonly Segment[] PrimaryTranslate =
    [
        new(0, 0.2, 0, 0, null),
        new(0.2, 0.5915, 0, 0.8367142, new CubicBezierEasing(0.5, 0, 0.701732, 0.495819)),
        new(0.5915, 1, 0.8367142, 2.00611057, new CubicBezierEasing(0.302435, 0.381352, 0.55, 0.956352)),
    ];

    private static readonly Segment[] PrimaryScale =
    [
        new(0, 0.3665, 0.08, 0.08, null),
        new(0.3665, 0.6915, 0.08, 0.661479, new CubicBezierEasing(0.334731, 0.12482, 0.785844, 1)),
        new(0.6915, 1, 0.661479, 0.08, new CubicBezierEasing(0.06, 0.11, 0.6, 1)),
    ];

    private static readonly Segment[] SecondaryTranslate =
    [
        new(0, 0.25, 0, 0.37651913, new CubicBezierEasing(0.15, 0, 0.515058, 0.409685)),
        new(0.25, 0.4835, 0.37651913, 0.84386165, new CubicBezierEasing(0.31033, 0.284058, 0.8, 0.733712)),
        new(0.4835, 1, 0.84386165, 1.60277782, new CubicBezierEasing(0.4, 0.627035, 0.6, 0.902026)),
    ];

    private static readonly Segment[] SecondaryScale =
    [
        new(0, 0.1915, 0.08, 0.457104, new CubicBezierEasing(0.205028, 0.057051, 0.57661, 0.453971)),
        new(0.1915, 0.4415, 0.457104, 0.72796, new CubicBezierEasing(0.152313, 0.196432, 0.648374, 1.004315)),
        new(0.4415, 1, 0.72796, 0.08, new CubicBezierEasing(0.257759, -0.003163, 0.211762, 1.38179)),
    ];

    /// <summary>The primary bar at <paramref name="t"/>, a fraction of the 2 s loop.</summary>
    internal static (double Translate, double Scale) Primary(double t) => (Evaluate(PrimaryTranslate, t), Evaluate(PrimaryScale, t));

    /// <summary>The secondary bar at <paramref name="t"/>, a fraction of the 2 s loop.</summary>
    internal static (double Translate, double Scale) Secondary(double t) => (Evaluate(SecondaryTranslate, t), Evaluate(SecondaryScale, t));

    private static double Evaluate(Segment[] segments, double t)
    {
        var segment = segments[^1];
        foreach (var candidate in segments)
        {
            if (t <= candidate.End)
            {
                segment = candidate;
                break;
            }
        }

        var local = Math.Clamp((t - segment.Start) / (segment.End - segment.Start), 0, 1);
        var eased = segment.Easing?.Ease(local) ?? local;
        return segment.From + (segment.To - segment.From) * eased;
    }
}
