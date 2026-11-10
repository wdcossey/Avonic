using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// Chromium's programmatic smooth scroll, <c>scrollTo({ behavior: 'smooth' })</c>, on <see cref="AvonicConfig"/>'s clock: √distance / 60
/// seconds, <c>cubic-bezier(0.42, 0, 0.58, 1)</c>. Measured in Chromium over 84, 390 and 780 px; see <c>docs/components/ion-segment/PLAN.md</c>.
/// Scrolling by anything else in the middle stops it, as a person's scroll stops Chromium's.
/// </summary>
internal sealed class SmoothScroll(ScrollViewer scroller)
{
    internal static readonly CubicBezierEasing Easing = new(0.42, 0, 0.58, 1);

    private IDisposable? _frame;
    private double _from;
    private double _to;
    private double _last;
    private TimeSpan _start;
    private TimeSpan _duration;

    public bool IsRunning => _frame is not null;

    internal static TimeSpan DurationFor(double distance) => TimeSpan.FromSeconds(Math.Sqrt(Math.Abs(distance)) / 60);

    /// <summary><c>scrollTo</c>, smoothly or at once.</summary>
    public void To(double x, bool smooth)
    {
        var max = Math.Max(0, scroller.Extent.Width - scroller.Viewport.Width);
        var target = Math.Clamp(x, 0, max);

        // The same target again, while it's going there, as Ionic's checkButton asks after valueChanged has: it carries on.
        if (smooth && IsRunning && Math.Abs(target - _to) < 0.5)
        {
            return;
        }

        Stop();
        var from = scroller.Offset.X;
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

    private void OnFrame()
    {
        _frame = null;
        if (Math.Abs(scroller.Offset.X - _last) > 0.5)
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

    private void Set(double x)
    {
        scroller.Offset = new Vector(x, scroller.Offset.Y);
        _last = scroller.Offset.X;
    }
}
