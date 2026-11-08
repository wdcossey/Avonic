using Avalonia;

namespace Avonic.Components.Controls;

/// <summary>A stretch of a circle's outline, as distances along it from 3 o'clock, clockwise.</summary>
internal readonly record struct SpinnerArc(double Start, double End);

/// <summary>
/// One <c>svg</c> of a spinner at an instant: what its keyframes have made of it.
/// </summary>
/// <param name="Left">The <c>svg</c>'s <c>left</c>, as a percentage of the host's width.</param>
/// <param name="Top">The <c>svg</c>'s <c>top</c>, as a percentage of the host's height.</param>
/// <param name="Opacity">Its <c>opacity</c>.</param>
/// <param name="Scale">Its <c>transform</c>'s scale.</param>
/// <param name="Rotation">Its <c>transform</c>'s rotation, in degrees, clockwise.</param>
/// <param name="Arcs">For a dashed circle, the stretches of it that are drawn. Null when it isn't dashed.</param>
internal readonly record struct SpinnerElementFrame(
    double Left, double Top, double Opacity, double Scale, double Rotation, IReadOnlyList<SpinnerArc>? Arcs)
{
    /// <summary>For <c>circular</c>, its <c>stroke-dasharray</c>'s dash, as the browser computes it.</summary>
    public double DashArray { get; init; }

    /// <summary>For <c>circular</c>, its <c>stroke-dashoffset</c>, as the browser computes it.</summary>
    public double DashOffset { get; init; }
}

/// <summary>
/// The maths of <c>spinner.css</c>'s keyframes, as a pure function of time, so that tests can pin it to what Chromium
/// computes and <see cref="AvonicSpinner"/> can draw it.
/// </summary>
/// <remarks>
/// <para>
/// Each <c>svg</c> runs its keyframes over the duration, forever, starting <c>animation-delay</c> in: so at
/// <c>elapsed</c> milliseconds its progress is <c>frac((elapsed − delay) / duration)</c>.
/// </para>
/// <para>
/// <c>circular</c>'s <c>svg</c> doesn't turn. <c>@keyframes spinner-circular { 100% { transform: rotate(360deg) } }</c>
/// has no 0 % frame, so it starts from the <c>svg</c>'s own <c>transform: translateZ(0)</c>. The two aren't the same
/// function, so the browser interpolates their matrices, and <c>rotate(360deg)</c>'s is the identity: Chromium computes
/// <c>matrix(1, 0, 0, 1, 0, 0)</c> at every instant. Its arc moves only through <c>spinner-circular-inner</c>.
/// </para>
/// </remarks>
internal static class SpinnerFrame
{
    /// <summary><c>spinner-circular-inner</c>'s gap, which is longer than the circle, so there's never a second dash.</summary>
    internal const double CircularGap = 200;

    /// <summary>
    /// <c>typeof duration === 'number' &amp;&amp; duration &gt; 10 ? duration : spinner.dur</c>. A non-finite number is
    /// the default too, where Ionic writes <c>Infinityms</c>, which the browser drops for the stylesheet's own: a
    /// deliberate difference, in COMPARISON.md.
    /// </summary>
    public static double EffectiveDuration(SpinnerConfig config, double? duration) =>
        duration is { } value && double.IsFinite(value) && value > SpinnerConfigs.MinimumDuration ? value : config.Dur;

    /// <summary>
    /// Element <paramref name="index"/> of <paramref name="config"/>, <paramref name="elapsed"/> milliseconds after its
    /// animation began.
    /// </summary>
    public static SpinnerElementFrame Compute(SpinnerConfig config, double duration, int index, double elapsed)
    {
        var element = config.Element(duration, index, config.Count);
        var progress = Frac((elapsed - element.Delay) / duration);

        var opacity = 1.0;
        var scale = 1.0;
        var rotation = element.Rotation;
        IReadOnlyList<SpinnerArc>? arcs = null;
        double dashArray = 0, dashOffset = 0;

        switch (config.Animation)
        {
            case SpinnerAnimation.FadeOut:
                opacity = 1 - progress;
                break;
            case SpinnerAnimation.ScaleOut:
                scale = 1 - progress;
                break;
            case SpinnerAnimation.Rotate:
                // The keyframes' transform replaces the svg's own, which is translateZ(0) for the crescent.
                rotation = 360 * progress;
                break;
            case SpinnerAnimation.Dots:
                // 0% scale(1) opacity 0.9, 50% scale(0.4) opacity 0.3, 100% as 0%. Linear.
                var half = progress < 0.5 ? progress / 0.5 : 1 - (progress - 0.5) / 0.5;
                scale = 1 - 0.6 * half;
                opacity = 0.9 - 0.6 * half;
                break;
            case SpinnerAnimation.Circular:
                // 0% 1px, 200px / 0px; 50% 100px, 200px / −15px; 100% 100px, 200px / −125px. ease-in-out in each half.
                if (progress < 0.5)
                {
                    var eased = EaseInOut(progress / 0.5);
                    dashArray = 1 + 99 * eased;
                    dashOffset = -15 * eased;
                }
                else
                {
                    var eased = EaseInOut((progress - 0.5) / 0.5);
                    dashArray = 100;
                    dashOffset = -15 - 110 * eased;
                }

                arcs = Dashes(2 * Math.PI * config.Radius, dashArray, CircularGap, dashOffset);
                break;
        }

        if (config.FixedArcs is { } fixedArcs)
        {
            arcs = fixedArcs;
        }

        return new SpinnerElementFrame(element.Left, element.Top, opacity, scale, rotation, arcs)
        {
            DashArray = dashArray,
            DashOffset = dashOffset,
        };
    }

    /// <summary>
    /// Where element <paramref name="frame"/> of <paramref name="config"/> draws in a host of <paramref name="host"/>:
    /// the <c>svg</c> is as big as the host, at its <c>left</c> and <c>top</c>, and its <c>viewBox</c> is scaled by the
    /// smaller side and centred (<c>xMidYMid meet</c>). Its <c>transform</c> turns about its centre
    /// (<c>transform-origin: center</c>).
    /// </summary>
    public static Matrix Transform(SpinnerConfig config, SpinnerElementFrame frame, Size host)
    {
        var x0 = frame.Left / 100 * host.Width;
        var y0 = frame.Top / 100 * host.Height;
        var viewBox = config.ViewBox;
        var fit = Math.Min(host.Width / viewBox.Width, host.Height / viewBox.Height);

        var viewBoxTransform = Matrix.CreateTranslation(-viewBox.X, -viewBox.Y)
                             * Matrix.CreateScale(fit, fit)
                             * Matrix.CreateTranslation(x0 + (host.Width - viewBox.Width * fit) / 2, y0 + (host.Height - viewBox.Height * fit) / 2);

        var originX = x0 + host.Width / 2;
        var originY = y0 + host.Height / 2;
        var svgTransform = Matrix.CreateTranslation(-originX, -originY)
                         * Matrix.CreateScale(frame.Scale, frame.Scale)
                         * Matrix.CreateRotation(frame.Rotation * Math.PI / 180)
                         * Matrix.CreateTranslation(originX, originY);

        return viewBoxTransform * svgTransform;
    }

    /// <summary>
    /// The stretches of a circle of <paramref name="circumference"/> that <c>stroke-dasharray: dash, gap</c> and
    /// <c>stroke-dashoffset: offset</c> draw. The pattern starts <paramref name="offset"/> into itself at the path's start
    /// and stops at its end: a circle's path doesn't wrap round.
    /// </summary>
    public static IReadOnlyList<SpinnerArc> Dashes(double circumference, double dash, double gap, double offset)
    {
        var period = dash + gap;
        var arcs = new List<SpinnerArc>();
        if (dash <= 0 || period <= 0)
        {
            return arcs;
        }

        // Dash k covers [k·period − offset, k·period − offset + dash] along the path.
        var first = (int)Math.Floor(offset / period) - 1;
        var last = (int)Math.Ceiling((circumference + offset) / period) + 1;
        for (var k = first; k <= last; k++)
        {
            var start = Math.Max(0, k * period - offset);
            var end = Math.Min(circumference, k * period - offset + dash);
            if (end > start)
            {
                arcs.Add(new SpinnerArc(start, end));
            }
        }

        return arcs;
    }

    /// <summary><c>ease-in-out</c>, which is <c>cubic-bezier(0.42, 0, 0.58, 1)</c>.</summary>
    public static double EaseInOut(double x) => CubicBezier(0.42, 0, 0.58, 1, x);

    /// <summary>
    /// A CSS <c>cubic-bezier()</c>: the <c>y</c> at which the curve reaches <paramref name="x"/>. Not Avalonia's
    /// <c>SplineEasing</c>, which is about 2e-3 out: it gives a dash of 5.0477 at 100 ms, where Chromium computes 5.04952.
    /// </summary>
    internal static double CubicBezier(double x1, double y1, double x2, double y2, double x)
    {
        if (x <= 0) return 0;
        if (x >= 1) return 1;

        static double Sample(double a, double b, double t) => ((1 - 3 * b + 3 * a) * t + (3 * b - 6 * a)) * t * t + 3 * a * t;
        static double Slope(double a, double b, double t) => 3 * (1 - 3 * b + 3 * a) * t * t + 2 * (3 * b - 6 * a) * t + 3 * a;

        // Newton's method, then bisection if it doesn't settle, as browsers do.
        var t = x;
        for (var i = 0; i < 8; i++)
        {
            var error = Sample(x1, x2, t) - x;
            if (Math.Abs(error) < 1e-9) return Sample(y1, y2, t);
            var slope = Slope(x1, x2, t);
            if (Math.Abs(slope) < 1e-9) break;
            t -= error / slope;
        }

        double low = 0, high = 1;
        t = x;
        for (var i = 0; i < 64; i++)
        {
            var value = Sample(x1, x2, t);
            if (Math.Abs(value - x) < 1e-9) break;
            if (value < x) low = t; else high = t;
            t = (low + high) / 2;
        }

        return Sample(y1, y2, t);
    }

    private static double Frac(double value) => value - Math.Floor(value);
}
