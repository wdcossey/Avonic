using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// One ripple circle: Ionic's <c>div.ripple-effect</c>. Its position, size and end-state are fixed
/// at creation; <see cref="Update"/> advances it along Ionic's timeline from the shared clock.
/// </summary>
internal sealed class RippleVisual : Control
{
    // ── Ionic constants (ripple-effect.css / ripple-effect.js) ───────────────────────────────

    /// <summary><c>rippleAnimation</c>: 225 ms, cubic-bezier(0.4, 0, 0.2, 1).</summary>
    internal static readonly TimeSpan ExpandDuration = TimeSpan.FromMilliseconds(225);

    /// <summary><c>fadeInAnimation</c>: 75 ms, linear, 0 → 0.16.</summary>
    internal static readonly TimeSpan FadeInDuration = TimeSpan.FromMilliseconds(75);

    /// <summary><c>fadeOutAnimation</c>: 150 ms, linear, 0.16 → 0.</summary>
    internal static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(150);

    /// <summary><c>removeRipple</c>: the element is removed 200 ms after fade-out starts.</summary>
    internal static readonly TimeSpan RemoveDelay = TimeSpan.FromMilliseconds(200);

    /// <summary><c>addRipple</c> resolves its release callback after <c>225 + 100</c> ms.</summary>
    internal static readonly TimeSpan MinimumLifetime = TimeSpan.FromMilliseconds(225 + 100);

    internal const double MaxOpacity = 0.16;

    internal static readonly Easing ExpandEasing = new SplineEasing(0.4, 0, 0.2, 1);

    // ── Fill (Ionic: background-color: currentColor) ─────────────────────────────────────────

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<RippleVisual, IBrush?>(nameof(Fill));

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    static RippleVisual()
    {
        AffectsRender<RippleVisual>(FillProperty);
    }

    // ── Geometry fixed at creation (addRipple) ───────────────────────────────────────────────

    /// <summary>Diameter of the circle before scaling: <c>⌊max(w, h) × 0.5⌋</c>.</summary>
    internal double InitialSize { get; init; }

    /// <summary><c>--final-scale</c>: <c>maxRadius / initialSize</c>.</summary>
    internal double FinalScale { get; init; }

    /// <summary><c>--translate-end</c>: drift toward the host's centre while expanding.</summary>
    internal Vector TranslateEnd { get; init; }

    // ── Timeline ─────────────────────────────────────────────────────────────────────────────

    internal TimeSpan StartedAt { get; init; }

    /// <summary>Set when the release callback is invoked; fade-out may not start before <see cref="MinimumLifetime"/>.</summary>
    internal TimeSpan? ReleaseRequestedAt { get; set; }

    /// <summary>When fade-out actually began.</summary>
    internal TimeSpan? FadeOutStartedAt { get; private set; }

    /// <summary>Eased expansion progress, 0 → 1 over <see cref="ExpandDuration"/>.</summary>
    internal double Progress { get; private set; }

    /// <summary>True once the element would have been removed from the DOM.</summary>
    internal bool IsRemoved { get; private set; }

    /// <summary>
    /// Fully expanded and faded in, with no release requested: nothing will change until the
    /// release action is invoked (Ionic's <c>forwards</c> animations idle at their end state).
    /// </summary>
    internal bool IsAtRest => ReleaseRequestedAt is null && FadeOutStartedAt is null && Progress >= 1 && Opacity >= MaxOpacity;

    /// <summary>Advance to <paramref name="now"/>, redrawing only if something visible changed.</summary>
    internal void Update(TimeSpan now)
    {
        var (progress, opacity) = (Progress, Opacity);
        Advance(now);
        if (progress != Progress || opacity != Opacity)
            InvalidateVisual();
    }

    private void Advance(TimeSpan now)
    {
        var elapsed = now - StartedAt;

        Progress = ExpandEasing.Ease(Fraction(elapsed, ExpandDuration));

        if (FadeOutStartedAt is null)
        {
            Opacity = MaxOpacity * Fraction(elapsed, FadeInDuration);

            if (ReleaseRequestedAt is { } requested)
            {
                var earliest = StartedAt + MinimumLifetime;
                var fadeAt   = requested > earliest ? requested : earliest;
                if (now >= fadeAt)
                    FadeOutStartedAt = fadeAt;
            }
        }

        if (FadeOutStartedAt is { } fadeStart)
        {
            var fading = now - fadeStart;
            Opacity    = MaxOpacity * (1 - Fraction(fading, FadeOutDuration));
            IsRemoved  = fading >= RemoveDelay;
        }
    }

    private static double Fraction(TimeSpan elapsed, TimeSpan duration) =>
        Math.Clamp(elapsed / duration, 0, 1);

    // ── Drawing: transform: translate(translateEnd × p) scale(1 + (finalScale − 1) × p) ──────

    public override void Render(DrawingContext context)
    {
        if (Fill is null || InitialSize <= 0)
            return;

        var radius = InitialSize / 2;
        var scale  = 1 + (FinalScale - 1) * Progress;
        var offset = TranslateEnd * Progress;
        var centre = new Point(radius + offset.X, radius + offset.Y);

        context.DrawEllipse(Fill, null, centre, radius * scale, radius * scale);
    }
}
