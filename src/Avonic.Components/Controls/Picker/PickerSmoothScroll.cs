using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// Chromium's programmatic smooth scroll, <c>scroll({ top, behavior: 'smooth' })</c>, vertically, on <see cref="AvonicConfig"/>'s
/// clock: √distance / 60 seconds, <c>cubic-bezier(0.42, 0, 0.58, 1)</c>, as <see cref="SmoothScroll"/> measured it sideways for the
/// segment; measured again here, 136 px down a column in 200 ms. <see cref="SmoothScroll"/> only scrolls sideways, so this is its
/// vertical copy. Scrolling by anything else in the middle stops it, as a person's scroll stops Chromium's.
/// </summary>
internal sealed class PickerSmoothScroll(ScrollViewer scroller)
{
    private IDisposable? _frame;
    private double _from;
    private double _to;
    private double _last;
    private TimeSpan _start;
    private TimeSpan _duration;

    public bool IsRunning => _frame is not null;

    /// <summary><c>scroll</c>, smoothly or at once, to a top already clamped and snapped.</summary>
    public void To(double y, bool smooth)
    {
        Stop();
        var from = scroller.Offset.Y;
        if (!smooth || Math.Abs(y - from) < 0.5)
        {
            Set(y);
            return;
        }

        _from = from;
        _to = y;
        _last = from;
        _start = AvonicConfig.Now();
        _duration = SmoothScroll.DurationFor(y - from);
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
        if (Math.Abs(scroller.Offset.Y - _last) > 0.5)
        {
            // Someone else scrolled it.
            return;
        }

        var progress = Math.Clamp((AvonicConfig.Now() - _start).Ticks / (double)_duration.Ticks, 0, 1);
        Set(progress >= 1 ? _to : _from + (_to - _from) * SmoothScroll.Easing.Ease(progress));
        if (progress < 1)
        {
            _frame = AvonicConfig.RequestFrame(scroller, OnFrame);
        }
    }

    private void Set(double y)
    {
        scroller.Offset = new Vector(scroller.Offset.X, y);
        _last = scroller.Offset.Y;
    }
}
