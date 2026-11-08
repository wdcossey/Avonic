using Avalonia;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>The keyframes on a spinner's <c>svg</c>, from <c>spinner.css</c>.</summary>
internal enum SpinnerAnimation
{
    /// <summary><c>spinner-fade-out</c>: opacity 1 → 0, linear.</summary>
    FadeOut,

    /// <summary><c>spinner-scale-out</c>: <c>scale(1)</c> → <c>scale(0)</c>, linear.</summary>
    ScaleOut,

    /// <summary><c>spinner-rotate</c>: 0° → 360°, linear.</summary>
    Rotate,

    /// <summary><c>spinner-dots</c>: scale 1 → 0.4 → 1 and opacity 0.9 → 0.3 → 0.9, linear.</summary>
    Dots,

    /// <summary>
    /// <c>spinner-circular</c> on the <c>svg</c>, which leaves it where it is (see <see cref="SpinnerFrame"/>), and
    /// <c>spinner-circular-inner</c> on the <c>circle</c>, <c>ease-in-out</c>.
    /// </summary>
    Circular,
}

/// <summary>Where one <c>svg</c> of a spinner sits, from its config's <c>fn</c>.</summary>
/// <param name="Left">The <c>svg</c>'s <c>left</c>, as a percentage of the host's width.</param>
/// <param name="Top">The <c>svg</c>'s <c>top</c>, as a percentage of the host's height.</param>
/// <param name="Delay">Its <c>animation-delay</c>, in milliseconds.</param>
/// <param name="Rotation">Its fixed <c>transform: rotate()</c>, in degrees, clockwise.</param>
internal readonly record struct SpinnerElement(double Left, double Top, double Delay, double Rotation);

/// <summary>
/// One entry of Ionic's <c>SPINNERS</c> (<c>spinner-configs.js</c>), together with the rules <c>spinner.css</c> gives
/// its host class. These are what each name is, and no Ionic app can reach them, as the spinner has no parts and one
/// CSS variable, so they're constants here rather than resource keys.
/// </summary>
internal sealed class SpinnerConfig
{
    /// <summary><c>dur</c>: the default duration, in milliseconds.</summary>
    public required double Dur { get; init; }

    /// <summary><c>circles</c> or <c>lines</c>: how many <c>svg</c>s.</summary>
    public required int Count { get; init; }

    /// <summary>Whether each <c>svg</c> holds a <c>line</c>, rather than a <c>circle</c>.</summary>
    public bool IsLine { get; init; }

    /// <summary>The <c>svg</c>'s keyframes.</summary>
    public required SpinnerAnimation Animation { get; init; }

    /// <summary>The <c>svg</c>'s <c>viewBox</c>: <c>0 0 64 64</c>, unless the config gives one.</summary>
    public Rect ViewBox { get; init; } = new(0, 0, 64, 64);

    /// <summary>
    /// A circle's centre, in the <c>viewBox</c>: <c>transform="translate(32,32)"</c> with no <c>cx</c>, <c>cy</c>, or
    /// <c>circular</c>'s <c>translate(0,0)</c> with <c>cx</c> and <c>cy</c> 48. A line's origin, which is also
    /// <c>translate(32,32)</c>.
    /// </summary>
    public Point Center { get; init; } = new(32, 32);

    /// <summary>A circle's <c>r</c>.</summary>
    public double Radius { get; init; }

    /// <summary>A line's <c>y1</c> and <c>y2</c>. Its <c>x1</c> and <c>x2</c> are unset, so 0.</summary>
    public double Y1 { get; init; }

    /// <inheritdoc cref="Y1"/>
    public double Y2 { get; init; }

    /// <summary>Whether the shape is filled with <c>currentColor</c>.</summary>
    public bool Fill { get; init; }

    /// <summary>The <c>stroke-width</c> of a shape stroked with <c>currentColor</c>, or 0 for none.</summary>
    public double StrokeWidth { get; init; }

    /// <summary><c>stroke-linecap</c>.</summary>
    public PenLineCap LineCap { get; init; } = PenLineCap.Flat;

    /// <summary>A fixed <c>stroke-dasharray</c> (dash, gap) and <c>stroke-dashoffset</c>, or none.</summary>
    public (double Dash, double Gap, double Offset)? Dash { get; init; }

    /// <summary>
    /// The config's <c>fn(dur, index, total)</c>: where <c>svg</c> <paramref name="index"/> sits and when it starts.
    /// </summary>
    public required Func<double, int, int, SpinnerElement> Element { get; init; }
}

/// <summary>Ionic's <c>SPINNERS</c>, from <c>spinner-configs.js</c> and <c>spinner.css</c>.</summary>
internal static class SpinnerConfigs
{
    // ── spinner.css ──────────────────────────────────────────────────────────────────────────

    /// <summary><c>:host { width: 28px; height: 28px }</c>. The theme sets it; this is for the docs and tests.</summary>
    internal const double Size = 28;

    /// <summary><c>:host(.spinner-lines) line, :host(.spinner-lines-small) line { stroke-width: 7px }</c>.</summary>
    internal const double LinesStrokeWidth = 7;

    /// <summary><c>:host(.spinner-lines-sharp) line, :host(.spinner-lines-sharp-small) line { stroke-width: 4px }</c>.</summary>
    internal const double LinesSharpStrokeWidth = 4;

    /// <summary><c>:host(.spinner-crescent) circle { stroke-width: 4px }</c>.</summary>
    internal const double CrescentStrokeWidth = 4;

    /// <summary><c>:host(.spinner-circular) circle { stroke-width: 5.6 }</c>.</summary>
    internal const double CircularStrokeWidth = 5.6;

    /// <summary>The <c>render</c> method's <c>duration &gt; 10</c>: a duration has to be longer to count.</summary>
    internal const double MinimumDuration = 10;

    // ── spinner-configs.js ───────────────────────────────────────────────────────────────────

    /// <summary>A ring of dots: <c>top: 32 sin θ %</c>, <c>left: 32 cos θ %</c>, θ = 2πi/n, delay <c>dur·i/n − dur</c>.</summary>
    private static SpinnerElement Ring(double dur, int index, int total)
    {
        var angle = 2 * Math.PI * index / total;
        return new SpinnerElement(32 * Math.Cos(angle), 32 * Math.Sin(angle), dur * index / total - dur, 0);
    }

    /// <summary><c>lines</c> and <c>lines-small</c>: <c>rotate(360/n·i ± 180)</c>, delay <c>dur·i/n − dur</c>.</summary>
    private static SpinnerElement Spoke(double dur, int index, int total) =>
        new(0, 0, dur * index / total - dur, 360.0 / total * index + (index < total / 2.0 ? 180 : -180));

    /// <summary><c>lines-sharp</c> and <c>lines-sharp-small</c>: <c>rotate(30i ± 180)</c>, delay <c>dur·i/n − dur</c>.</summary>
    private static SpinnerElement SharpSpoke(double dur, int index, int total) =>
        new(0, 0, dur * index / total - dur, 30 * index + (index < 6 ? 180 : -180));

    private static readonly SpinnerConfig Bubbles = new()
    {
        Dur = 1000, Count = 9, Animation = SpinnerAnimation.ScaleOut,
        Radius = 5, Fill = true,
        Element = Ring,
    };

    private static readonly SpinnerConfig Circles = new()
    {
        Dur = 1000, Count = 8, Animation = SpinnerAnimation.FadeOut,
        Radius = 5, Fill = true,
        Element = Ring,
    };

    private static readonly SpinnerConfig Circular = new()
    {
        Dur = 1400, Count = 1, Animation = SpinnerAnimation.Circular,
        ViewBox = new Rect(24, 24, 48, 48), Center = new Point(48, 48), Radius = 20,
        StrokeWidth = CircularStrokeWidth,
        Element = static (_, _, _) => new SpinnerElement(0, 0, 0, 0),
    };

    private static readonly SpinnerConfig Crescent = new()
    {
        Dur = 750, Count = 1, Animation = SpinnerAnimation.Rotate,
        Radius = 26, StrokeWidth = CrescentStrokeWidth,
        // stroke-dasharray: 128px; stroke-dashoffset: 82px. One value is repeated, so the gap is 128 too.
        Dash = (128, 128, 82),
        Element = static (_, _, _) => new SpinnerElement(0, 0, 0, 0),
    };

    private static readonly SpinnerConfig Dots = new()
    {
        Dur = 750, Count = 3, Animation = SpinnerAnimation.Dots,
        Radius = 6, Fill = true,
        // left: 32 − 32i %, and animation-delay: −110i ms, whatever the duration.
        Element = static (_, index, _) => new SpinnerElement(32 - 32 * index, 0, -110 * index, 0),
    };

    private static readonly SpinnerConfig Lines = new()
    {
        Dur = 1000, Count = 8, IsLine = true, Animation = SpinnerAnimation.FadeOut,
        Y1 = 14, Y2 = 26, StrokeWidth = LinesStrokeWidth, LineCap = PenLineCap.Round,
        Element = Spoke,
    };

    private static readonly SpinnerConfig LinesSmall = new()
    {
        Dur = 1000, Count = 8, IsLine = true, Animation = SpinnerAnimation.FadeOut,
        Y1 = 12, Y2 = 20, StrokeWidth = LinesStrokeWidth, LineCap = PenLineCap.Round,
        Element = Spoke,
    };

    private static readonly SpinnerConfig LinesSharp = new()
    {
        Dur = 1000, Count = 12, IsLine = true, Animation = SpinnerAnimation.FadeOut,
        Y1 = 17, Y2 = 29, StrokeWidth = LinesSharpStrokeWidth, LineCap = PenLineCap.Round,
        Element = SharpSpoke,
    };

    private static readonly SpinnerConfig LinesSharpSmall = new()
    {
        Dur = 1000, Count = 12, IsLine = true, Animation = SpinnerAnimation.FadeOut,
        Y1 = 12, Y2 = 20, StrokeWidth = LinesSharpStrokeWidth, LineCap = PenLineCap.Round,
        Element = SharpSpoke,
    };

    /// <summary>The config for <paramref name="name"/>.</summary>
    public static SpinnerConfig Get(SpinnerName name) => name switch
    {
        SpinnerName.Bubbles => Bubbles,
        SpinnerName.Circles => Circles,
        SpinnerName.Circular => Circular,
        SpinnerName.Crescent => Crescent,
        SpinnerName.Dots => Dots,
        SpinnerName.Lines => Lines,
        SpinnerName.LinesSmall => LinesSmall,
        SpinnerName.LinesSharp => LinesSharp,
        SpinnerName.LinesSharpSmall => LinesSharpSmall,
        // SPINNERS[spinnerName] ?? SPINNERS['lines']: an enum can only be out of range by a cast.
        _ => Lines,
    };
}
