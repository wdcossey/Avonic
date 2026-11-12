using Avalonia.Animation.Easings;

namespace Avonic.Components.Animation;

/// <summary>
/// A CSS transition of one number with a timing function, on <see cref="AvonicConfig"/>'s clock, with CSS's rules for an interrupted
/// one: it starts from the value shown, and one that goes back to where the running one began is shortened by how far that one got
/// through its timing function (the reversing shortening factor, CSS Transitions §3). No delay. Used by the progress bar
/// (<c>transform 150ms linear</c>), the reorder group (<c>transform 300ms</c>, <c>ease</c>) and the sliding item
/// (<c>transform 500ms cubic-bezier(0.36, 0.66, 0.04, 1)</c>).
/// </summary>
/// <param name="easing">The timing function.</param>
/// <param name="value">The value before anything has started: what <see cref="At"/> and <see cref="Target"/> give.</param>
internal sealed class CssTransition(Easing easing, double value)
{
    /// <summary><c>linear</c>.</summary>
    internal static readonly Easing Linear = new LinearEasing();

    private double _from = value;
    private double _to = value;
    private TimeSpan _start;
    private TimeSpan _duration;
    private double _reversingStart = value;
    private double _shortening = 1;

    /// <summary>The value it's going to.</summary>
    public double Target => _to;

    /// <summary>The value at once, with no transition.</summary>
    public void Snap(double value)
    {
        _from = _to = _reversingStart = value;
        _duration = TimeSpan.Zero;
        _shortening = 1;
    }

    public bool IsRunning(TimeSpan now) => _duration > TimeSpan.Zero && now - _start < _duration;

    /// <summary>The value shown. Exactly the target once done, so that a later change compares against it as CSS does.</summary>
    public double At(TimeSpan now) => Progress(now) is var progress && progress >= 1 ? _to : _from + (_to - _from) * easing.Ease(progress);

    private double Progress(TimeSpan now) =>
        _duration <= TimeSpan.Zero ? 1 : Math.Clamp((now - _start).Ticks / (double)_duration.Ticks, 0, 1);

    /// <summary>CSS Transitions §3, "starting of transitions", with no delay.</summary>
    public void Start(double to, TimeSpan now, TimeSpan duration)
    {
        var running = IsRunning(now);
        var current = At(now);
        if (running ? to == _to : to == current)
        {
            return;
        }

        if (running && to == current)
        {
            Snap(to);
            return;
        }

        if (running && to == _reversingStart)
        {
            // The reversing shortening factor: |the running one's output progress × its own factor + 1 − its own factor|, the
            // output progress being its eased progress.
            _shortening = Math.Clamp(Math.Abs(easing.Ease(Progress(now)) * _shortening + 1 - _shortening), 0, 1);
            _reversingStart = _to;
        }
        else
        {
            _shortening = 1;
            _reversingStart = current;
        }

        _from = current;
        _to = to;
        _start = now;
        _duration = TimeSpan.FromTicks((long)(duration.Ticks * _shortening));
        if (_duration <= TimeSpan.Zero)
        {
            Snap(to);
        }
    }
}
