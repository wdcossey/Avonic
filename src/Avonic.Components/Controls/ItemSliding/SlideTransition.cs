using Avalonia.Animation.Easings;

namespace Avonic.Components.Controls;

/// <summary>
/// A CSS transition of one number with a timing function, on <see cref="AvonicConfig"/>'s clock, with CSS's rules for an
/// interrupted one: it starts from the value shown, and one that goes back to where the running one began is shortened by how far
/// that one got (the reversing shortening factor, CSS Transitions §3).
/// </summary>
internal sealed class SlideTransition(Easing easing)
{
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

    /// <summary>The value shown. Exactly the target once done.</summary>
    public double At(TimeSpan now)
    {
        var progress = Progress(now);
        return progress >= 1 ? _to : _from + (_to - _from) * easing.Ease(progress);
    }

    private double Progress(TimeSpan now) =>
        _duration <= TimeSpan.Zero ? 1 : Math.Clamp((now - _start).Ticks / (double)_duration.Ticks, 0, 1);

    public void Start(double to, TimeSpan now, TimeSpan duration)
    {
        var running = IsRunning(now);
        var current = At(now);
        if (running ? to == _to : to == current)
        {
            return;
        }

        if (running && to == _reversingStart)
        {
            // How far the running one got, in output progress, scaled by its own factor.
            var span = _to - _from;
            var got = span == 0 ? 1 : (current - _from) / span;
            _shortening = Math.Clamp(Math.Abs(got * _shortening) + 1 - _shortening, 0, 1);
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
