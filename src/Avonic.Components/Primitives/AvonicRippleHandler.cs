using System;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Rendering.Composition;

namespace Avonic.Components.Primitives;

/// <summary>
/// Composition-layer handler that drives a single ripple animation instance.
/// Lifecycle: send <see cref="FirstStepMessage"/> to start, <see cref="SecondStepMessage"/> to begin fade-out.
/// </summary>
/// <remarks>
/// Rendering uses Avalonia's Composition API, attributed to Material.Avalonia (MIT).<br/>
/// Geometry inspired by Ionic's <c>ion-ripple-effect</c> (expand from origin toward centre),
/// adapted for direct radius-based drawing (Ionic uses CSS scale transforms on a sized div).
/// </remarks>
internal sealed class AvonicRippleHandler : CompositionCustomVisualHandler
{
    // ── Two-step protocol ─────────────────────────────────────────────────────

    public static readonly object FirstStepMessage  = new();
    public static readonly object SecondStepMessage = new();

    // ── Timing constants ──────────────────────────────────────────────────────

    // Ionic CSS values: expand 225ms cubic-bezier(0.4,0,0.2,1), fade-in 75ms, fade-out 150ms.
    // Fade-out is deferred until the expand finishes so a fast desktop click always shows
    // the full expansion before disappearing.

    private static readonly TimeSpan ExpandDuration  = TimeSpan.FromMilliseconds(225);
    private static readonly TimeSpan FadeInDuration  = TimeSpan.FromMilliseconds(75);
    private static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(150);
    private static readonly Easing   ExpandEasing    = new CubicEaseOut();

    // ── Rendering params ──────────────────────────────────────────────────────

    private readonly IImmutableBrush _brush;
    private readonly double          _opacity;
    private readonly RoundedRect     _clipRect;

    // ── Geometry ──────────────────────────────────────────────────────────────

    private readonly double _originX, _originY;  // press point (or centre for unbounded)
    private readonly double _centerX, _centerY;  // bounds centre — lerp target
    private readonly double _startRadius;
    private readonly double _endRadius;           // radius (not diameter) to fill the surface

    // ── Animation state ───────────────────────────────────────────────────────

    private TimeSpan  _elapsed;
    private TimeSpan? _lastServerTime;
    private TimeSpan? _secondStepAt;

    // ── Constructor ───────────────────────────────────────────────────────────

    public AvonicRippleHandler(
        IImmutableBrush brush,
        double          opacity,
        CornerRadius    cornerRadius,
        double          pressX,
        double          pressY,
        double          width,
        double          height,
        bool            unbounded)
    {
        _brush   = brush;
        _opacity = opacity;

        _clipRect = new RoundedRect(
            new Rect(0, 0, width, height),
            cornerRadius.TopLeft,
            cornerRadius.TopRight,
            cornerRadius.BottomRight,
            cornerRadius.BottomLeft);

        _centerX = width  * 0.5;
        _centerY = height * 0.5;

        _originX = unbounded ? _centerX : pressX;
        _originY = unbounded ? _centerY : pressY;

        // Start radius: small relative to the shorter dimension so the ripple
        // begins as a visible dot regardless of the control's aspect ratio.
        // Using minDim (not maxDim) prevents the initial circle from already
        // filling a wide/short button at frame 0 (which caused the "outside-in" look).
        var minDim      = Math.Min(width, height);
        var initialSize = Math.Floor(minDim * 0.5); // diameter
        _startRadius = Math.Max(4.0, initialSize * 0.5);

        // End radius: distance from origin to the farthest corner, plus padding,
        // so the ripple always fully covers the surface.
        // NOTE: Ionic's maxRadius is the final *diameter* of a scaled CSS div;
        // DrawEllipse takes a *radius*, so we use half of Ionic's value here.
        var hypotenuse = Math.Sqrt(width * width + height * height);
        _endRadius = unbounded
            ? hypotenuse * 0.5           // centre → corner (fills all quadrants)
            : hypotenuse * 0.5 + 10.0;  // + Ionic's PADDING = 10
    }

    // ── CompositionCustomVisualHandler ────────────────────────────────────────

    public override void OnRender(ImmediateDrawingContext ctx)
    {
        Tick();

        var expandProgress = ExpandEasing.Ease(
            Math.Min((double)_elapsed.Ticks / ExpandDuration.Ticks, 1.0));

        // Radius expands from _startRadius to _endRadius
        var radius = _startRadius + (_endRadius - _startRadius) * expandProgress;

        // Centre lerps from press origin toward bounds centre (Ionic translate-end)
        var cx = _originX + (_centerX - _originX) * expandProgress;
        var cy = _originY + (_centerY - _originY) * expandProgress;

        // Opacity: quick linear fade-in, then hold at full opacity until SecondStep,
        // then fade-out. If SecondStep was deferred, hold at full until deferred time.
        double currentOpacity;
        if (_secondStepAt is { } secondStepAt)
        {
            var sinceSecondStep = _elapsed - secondStepAt;
            if (sinceSecondStep.Ticks <= 0)
            {
                // Still within the deferred window — hold at peak opacity
                currentOpacity = _opacity;
            }
            else
            {
                var fadeOutProgress = Math.Min(
                    (double)sinceSecondStep.Ticks / FadeOutDuration.Ticks, 1.0);
                currentOpacity = _opacity * (1.0 - fadeOutProgress);
            }
        }
        else
        {
            var fadeInProgress = Math.Min(
                (double)_elapsed.Ticks / FadeInDuration.Ticks, 1.0);
            currentOpacity = _opacity * fadeInProgress;
        }

        using (ctx.PushClip(_clipRect))
        using (ctx.PushOpacity(currentOpacity, default))
        {
            ctx.DrawEllipse(_brush, null, new Point(cx, cy), radius, radius);
        }
    }

    public override void OnMessage(object message)
    {
        if (message == FirstStepMessage)
        {
            _elapsed        = TimeSpan.Zero;
            _lastServerTime = null;
            _secondStepAt   = null;
            Next();
        }
        else if (message == SecondStepMessage)
        {
            Tick();
            // Defer fade-out until the expand has fully played. This ensures a fast
            // desktop click always shows the full expansion, matching mobile behaviour
            // where the finger naturally holds for longer than 225ms.
            _secondStepAt = _elapsed < ExpandDuration ? ExpandDuration : _elapsed;
            Next();
        }
    }

    public override void OnAnimationFrameUpdate()
    {
        var expanding = _elapsed < ExpandDuration;
        var fadingIn  = !_secondStepAt.HasValue && _elapsed < FadeInDuration;

        // Animation must continue through the deferred window AND the actual fade-out
        var fadingOut = _secondStepAt.HasValue
                        && _elapsed < _secondStepAt.Value + FadeOutDuration;

        if (expanding || fadingIn || fadingOut)
            Next();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void Tick()
    {
        if (_lastServerTime.HasValue)
            _elapsed += CompositionNow - _lastServerTime.Value;
        _lastServerTime = CompositionNow;
    }

    private void Next()
    {
        Invalidate();
        RegisterForNextAnimationFrameUpdate();
    }
}
