using Avalonia.Animation.Easings;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>transition: transform 300ms</c> on one child's <c>translateY</c>, on <see cref="AvonicConfig"/>'s clock, with CSS's rules for
/// an interrupted transition: it starts from the value shown, and one that goes back to where the running one began is shortened by
/// how far that one got, through its timing function.
/// </summary>
internal sealed class ReorderTransition
{
    /// <summary><c>ease</c>, the default timing function.</summary>
    private static readonly Easing Ease = new CubicBezierEasing(0.25, 0.1, 0.25, 1);

    private double _from;
    private double _to;
    private TimeSpan _start;
    private TimeSpan _duration;
    private double _reversingStart;
    private double _shortening = 1;

    /// <summary>The value it's going to.</summary>
    public double Target => _to;

    public void Snap(double value)
    {
        _from = _to = _reversingStart = value;
        _duration = TimeSpan.Zero;
        _shortening = 1;
    }

    public bool IsRunning(TimeSpan now) => _duration > TimeSpan.Zero && now - _start < _duration;

    /// <summary>The value shown: exactly the target once done.</summary>
    public double At(TimeSpan now) => Linear(now) is var progress && progress >= 1 ? _to : _from + (_to - _from) * Ease.Ease(progress);

    private double Linear(TimeSpan now) =>
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
            // The reversing shortening factor: the running one's eased progress, scaled by its own factor.
            _shortening = Math.Clamp(Math.Abs(Ease.Ease(Linear(now)) * _shortening + 1 - _shortening), 0, 1);
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
