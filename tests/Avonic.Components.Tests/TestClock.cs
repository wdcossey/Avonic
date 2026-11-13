using Avonic.Components;

namespace Avonic.Components.Tests;

/// <summary>
/// Replaces <see cref="AvonicConfig.Now"/>, <see cref="AvonicConfig.Schedule"/>, <see cref="AvonicConfig.RequestFrame"/>
/// and <see cref="AvonicConfig.EventTime"/> for the lifetime of the test, so every Avonic timeline — ripple animation,
/// tap-click defers, frames and a gesture's event times — is asserted exactly instead of approximately. Time starts at zero
/// and moves only when <see cref="At"/> moves it, which runs each callback that falls due on the way, at its own due time,
/// in order. A gesture's events are timed by this clock, as headless input's timestamps can't be set, unless
/// <c>timesEvents</c> is false.
/// </summary>
/// <remarks>
/// Avalonia's <c>Transitions</c> and <c>Animation</c> don't read this clock. The hooks are process-wide, so only one
/// <see cref="TestClock"/> can be installed at a time, and a test doesn't hold one across an <c>await</c>: other tests share
/// the UI thread and could run in the gap. The guards here are CornflowerUI's, from its copy of this harness.
/// </remarks>
internal sealed class TestClock : IDisposable
{
    /// <summary>More callbacks than this in one <see cref="At"/> is a timeline rescheduling itself without moving on.</summary>
    private const int MaxCallbacksPerAt = 100_000;

    /// <summary>The clock installed now, if any: the hooks have no owner of their own to ask.</summary>
    private static TestClock? _installed;

    private readonly Func<TimeSpan>                       _previousNow      = AvonicConfig.Now;
    private readonly Func<Action, TimeSpan, IDisposable>  _previousSchedule = AvonicConfig.Schedule;
    private readonly Func<Avalonia.Visual, Action, IDisposable> _previousFrame = AvonicConfig.RequestFrame;
    private readonly Func<Avalonia.Input.PointerEventArgs, double> _previousEventTime = AvonicConfig.EventTime;
    private readonly List<Scheduled>                      _scheduled        = new();
    private bool _advancing;
    private bool _disposed;

    public TimeSpan Now { get; private set; }

    /// <summary>Callbacks scheduled and not yet run or cancelled.</summary>
    public int Pending => _scheduled.Count(s => !s.IsDisposed);

    /// <param name="timesEvents">Whether a gesture's events are timed by this clock. False keeps their own timestamps.</param>
    public TestClock(bool timesEvents = true)
    {
        if (_installed is not null)
        {
            throw new InvalidOperationException("A TestClock is already installed. Dispose it first.");
        }

        _installed = this;
        AvonicConfig.Now      = () => Now;
        AvonicConfig.Schedule = Schedule;
        AvonicConfig.RequestFrame = (_, callback) => AvonicConfig.Schedule(callback, AvonicConfig.FrameInterval);
        if (timesEvents)
        {
            AvonicConfig.EventTime = _ => Now.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Advance to <paramref name="milliseconds"/>, running every callback due on the way. A callback sees <see cref="Now"/>
    /// as the time it was due for, and one it schedules runs too if it falls due by the target. Callbacks due at the same
    /// time run in the order they were scheduled. Time doesn't go back.
    /// </summary>
    public TimeSpan At(double milliseconds)
    {
        var target = TimeSpan.FromMilliseconds(milliseconds);
        if (target < Now)
        {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), $"The clock is at {Now.TotalMilliseconds} ms and doesn't go back.");
        }

        if (_advancing)
        {
            throw new InvalidOperationException("At was called from a callback it was running.");
        }

        _advancing = true;
        try
        {
            var ran = 0;
            while (_scheduled.Where(s => !s.IsDisposed && s.Due <= target).OrderBy(s => s.Due).FirstOrDefault() is { } next)
            {
                if (++ran > MaxCallbacksPerAt)
                {
                    throw new InvalidOperationException($"More than {MaxCallbacksPerAt} callbacks by {target.TotalMilliseconds} ms: a timeline is rescheduling itself without a delay.");
                }

                _scheduled.Remove(next);
                Now = next.Due;              // callbacks observe the time they were due for
                next.Callback();
            }
        }
        finally
        {
            _advancing = false;
        }

        _scheduled.RemoveAll(s => s.IsDisposed);
        Now = target;
        return Now;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        AvonicConfig.Now      = _previousNow;
        AvonicConfig.Schedule = _previousSchedule;
        AvonicConfig.RequestFrame = _previousFrame;
        AvonicConfig.EventTime = _previousEventTime;
        _installed = null;
    }

    /// <summary>As <see cref="Avalonia.Threading.DispatcherTimer"/>, a negative delay is refused.</summary>
    private IDisposable Schedule(Action callback, TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        var item = new Scheduled(Now + delay, callback);
        _scheduled.Add(item);
        return item;
    }

    private sealed class Scheduled(TimeSpan due, Action callback) : IDisposable
    {
        public TimeSpan Due      { get; } = due;
        public Action   Callback { get; } = callback;
        public bool     IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }
}
