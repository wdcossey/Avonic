using Avonic.Components;

namespace Avonic.Components.Tests;

/// <summary>
/// Replaces <see cref="AvonicConfig.Now"/>, <see cref="AvonicConfig.Schedule"/>, <see cref="AvonicConfig.RequestFrame"/>
/// and <see cref="AvonicConfig.EventTime"/> for the lifetime of the test, so every Avonic timeline — ripple animation,
/// tap-click defers, frames and a gesture's event times — is asserted exactly instead of approximately. Advancing the clock
/// with <see cref="At"/> fires each scheduled callback at its own due time, in order. A gesture's events are timed by this
/// clock, as headless input's timestamps can't be set.
/// </summary>
internal sealed class TestClock : IDisposable
{
    private readonly Func<TimeSpan>                       _previousNow      = AvonicConfig.Now;
    private readonly Func<Action, TimeSpan, IDisposable>  _previousSchedule = AvonicConfig.Schedule;
    private readonly Func<Avalonia.Visual, Action, IDisposable> _previousFrame = AvonicConfig.RequestFrame;
    private readonly Func<Avalonia.Input.PointerEventArgs, double> _previousEventTime = AvonicConfig.EventTime;
    private readonly List<Scheduled>                      _scheduled        = new();

    public TimeSpan Now { get; private set; }

    public TestClock()
    {
        AvonicConfig.Now      = () => Now;
        AvonicConfig.Schedule = (callback, delay) =>
        {
            var item = new Scheduled(Now + delay, callback);
            _scheduled.Add(item);
            return item;
        };
        AvonicConfig.RequestFrame = (_, callback) => AvonicConfig.Schedule(callback, AvonicConfig.FrameInterval);
        AvonicConfig.EventTime = _ => Now.TotalMilliseconds;
    }

    /// <summary>Advance to <paramref name="milliseconds"/>, running every callback due on the way.</summary>
    public TimeSpan At(double milliseconds)
    {
        var target = TimeSpan.FromMilliseconds(milliseconds);
        while (true)
        {
            var next = _scheduled.Where(s => !s.IsDisposed && s.Due <= target).OrderBy(s => s.Due).FirstOrDefault();
            if (next is null) break;
            _scheduled.Remove(next);
            Now = next.Due;              // callbacks observe the time they were due for
            next.Callback();
        }
        Now = target;
        return Now;
    }

    public void Dispose()
    {
        AvonicConfig.Now      = _previousNow;
        AvonicConfig.Schedule = _previousSchedule;
        AvonicConfig.RequestFrame = _previousFrame;
        AvonicConfig.EventTime = _previousEventTime;
    }

    private sealed class Scheduled(TimeSpan due, Action callback) : IDisposable
    {
        public TimeSpan Due      { get; } = due;
        public Action   Callback { get; } = callback;
        public bool     IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }
}
