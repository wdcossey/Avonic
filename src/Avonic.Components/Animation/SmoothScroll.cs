using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Avonic.Components.Animation;

/// <summary>
/// Chromium's programmatic smooth scroll, <c>scrollTo({ behavior: 'smooth' })</c>, along one axis, on <see cref="AvonicConfig"/>'s
/// clock: √distance / 60 seconds, <c>cubic-bezier(0.42, 0, 0.58, 1)</c>. Measured in Chromium over 84, 390 and 780 px sideways
/// (<c>docs/components/ion-segment/PLAN.md</c>), and 136 px down a picker column in 200 ms (<c>docs/components/ion-picker/PLAN.md</c>).
/// Scrolling by anything else in the middle stops it, as a person's scroll stops Chromium's. The segment and the segment view
/// scroll sideways, the picker column down.
/// </summary>
internal sealed class SmoothScroll(ScrollViewer scroller, Orientation orientation = Orientation.Horizontal)
{
    internal static readonly CubicBezierEasing Easing = new(0.42, 0, 0.58, 1);

    private readonly bool _vertical = orientation == Orientation.Vertical;
    private IDisposable? _frame;
    private double _from;
    private double _to;
    private double _last;
    private TimeSpan _start;
    private TimeSpan _duration;

    public bool IsRunning => _frame is not null;

    internal static TimeSpan DurationFor(double distance) => TimeSpan.FromSeconds(Math.Sqrt(Math.Abs(distance)) / 60);

    /// <summary>
    /// <c>scrollTo</c> along the axis, smoothly or at once: <c>left</c> when horizontal, <c>top</c> when vertical. The other axis
    /// stays where it is.
    /// </summary>
    public void To(double position, bool smooth)
    {
        var max = _vertical
            ? Math.Max(0, scroller.Extent.Height - scroller.Viewport.Height)
            : Math.Max(0, scroller.Extent.Width - scroller.Viewport.Width);
        var target = Math.Clamp(position, 0, max);

        // The same target again, while it's going there, as Ionic's checkButton asks after valueChanged has: it carries on. Not if
        // something else has scrolled it since its last frame, which ends this one at the next.
        if (smooth && IsRunning && Math.Abs(target - _to) < 0.5 && Math.Abs(Current - _last) <= 0.5)
        {
            return;
        }

        Stop();
        var from = Current;
        if (!smooth || Math.Abs(target - from) < 0.5)
        {
            Set(target);
            return;
        }

        _from = from;
        _to = target;
        _last = from;
        _start = AvonicConfig.Now();
        _duration = DurationFor(target - from);
        _frame = AvonicConfig.RequestFrame(scroller, OnFrame);
    }

    public void Stop()
    {
        _frame?.Dispose();
        _frame = null;
    }

    private double Current => _vertical ? scroller.Offset.Y : scroller.Offset.X;

    private void OnFrame()
    {
        _frame = null;
        if (Math.Abs(Current - _last) > 0.5)
        {
            // Someone else scrolled it.
            return;
        }

        var progress = Math.Clamp((AvonicConfig.Now() - _start).Ticks / (double)_duration.Ticks, 0, 1);
        Set(progress >= 1 ? _to : _from + (_to - _from) * Easing.Ease(progress));
        if (progress < 1)
        {
            _frame = AvonicConfig.RequestFrame(scroller, OnFrame);
        }
    }

    private void Set(double position)
    {
        scroller.Offset = _vertical ? new Vector(scroller.Offset.X, position) : new Vector(position, scroller.Offset.Y);
        _last = Current;
    }
}
