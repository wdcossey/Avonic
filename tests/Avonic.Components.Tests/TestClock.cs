using Avonic.Components;

namespace Avonic.Components.Tests;

/// <summary>
/// Replaces <see cref="AvonicConfig.Now"/> and <see cref="AvonicConfig.Schedule"/> for the
/// lifetime of the test, so every Avonic timeline — ripple animation and tap-click defers —
/// is asserted exactly instead of approximately. Advancing the clock with <see cref="At"/>
/// fires each scheduled callback at its own due time, in order.
/// </summary>
internal sealed class TestClock : IDisposable
{
    private readonly Func<TimeSpan>                       _previousNow      = AvonicConfig.Now;
    private readonly Func<Action, TimeSpan, IDisposable>  _previousSchedule = AvonicConfig.Schedule;
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
    }

    private sealed class Scheduled(TimeSpan due, Action callback) : IDisposable
    {
        public TimeSpan Due      { get; } = due;
        public Action   Callback { get; } = callback;
        public bool     IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }
}
