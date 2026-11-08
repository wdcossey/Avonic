using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>.buffer-circles</c>, the <c>stream</c> part of <see cref="AvonicProgressBar"/>'s template: a row of dots
/// in <see cref="Fill"/>, one every 10 px, which the bar moves left by 10 px every 450 ms. In Ionic it's a tiled
/// <c>radial-gradient</c>; a tiled brush with a bound colour can't be written in a template, so it's drawn.
/// </summary>
/// <remarks>
/// The geometry is Ionic's, with no CSS variable: a 10 × 10 tile with a circle of radius 30 % of the tile's
/// farthest-corner distance, which is 0.3 × 5√2. Ionic makes the element 10 px wider than the bar on each side, as
/// <c>left: -10px; right: -10px</c>, so that a dot at an edge is cut, not missing; this draws every dot that touches its
/// bounds instead. The two <c>.buffer-circles-container</c>s, which move a window over the dots while the dots stay put,
/// are <see cref="WindowStart"/>: nothing is drawn left of it.
/// </remarks>
public sealed class ProgressBarStream : Control
{
    /// <summary><c>background-size: 10px 10px</c>, and the distance <c>@keyframes buffering</c> moves: one period.</summary>
    internal const double Pitch = 10;

    /// <summary><c>radial-gradient(ellipse at center, … 30%, transparent 30%)</c> in a 10 × 10 tile.</summary>
    internal static readonly double DotRadius = 0.3 * Math.Sqrt(50);

    /// <summary>The dots' brush: Ionic's <c>--background</c>, or the base colour at 0.3 under <c>.ion-color</c>.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ProgressBarStream, IBrush?>(nameof(Fill));

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    static ProgressBarStream()
    {
        AffectsRender<ProgressBarStream>(FillProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<ProgressBarStream>(false);
    }

    private double _phase;

    /// <summary>How far left the pattern has moved, 0 to <see cref="Pitch"/>. At 0 a dot's centre is at local x = 10k.</summary>
    internal double Phase
    {
        get => _phase;
        set
        {
            if (_phase != value)
            {
                _phase = value;
                InvalidateVisual();
            }
        }
    }

    private double _windowStart;

    /// <summary>Where the dots start to show, in px from the left: the buffer's end. At the width or beyond, none show.</summary>
    internal double WindowStart
    {
        get => _windowStart;
        set
        {
            if (_windowStart != value)
            {
                _windowStart = value;
                InvalidateVisual();
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        if (Fill is not { } fill || _windowStart >= width)
        {
            return;
        }

        var start = Math.Max(0, _windowStart);
        var centreY = Bounds.Height / 2;
        using (context.PushClip(new Rect(start, 0, width - start, Bounds.Height)))
        {
            for (var x = -_phase; x - DotRadius < width; x += Pitch)
            {
                if (x + DotRadius > start)
                {
                    context.DrawEllipse(fill, null, new Point(x, centreY), DotRadius, DotRadius);
                }
            }
        }
    }
}
