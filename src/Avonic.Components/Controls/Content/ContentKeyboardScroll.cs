using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// A browser's keyboard scroll, on <see cref="AvonicConfig"/>'s clock: Chromium's <c>ScrollOffsetAnimationCurve</c> for a key,
/// a constant 150 ms (<c>kConstantDuration</c> 9 / <c>kDurationDivisor</c> 60) of <c>cubic-bezier(0.42, 0, 0.58, 1)</c>, from
/// the key. A key during one changes its target as the curve's <c>UpdateTarget</c> does: a new segment from where it is, at most
/// 150 ms and at most 2.5 times what the distance left takes at the speed it has, starting at that speed. Measured in Chromium
/// 141 against a model of this, within a pixel at each frame (<c>docs/components/ion-content/PLAN.md</c>). Not
/// <see cref="SmoothScroll"/>, which is Chromium's <c>scrollTo({ behavior: 'smooth' })</c>, with a duration of √distance / 60 s
/// and no change of target. Scrolling by anything else in the middle stops it.
/// </summary>
internal sealed class ContentKeyboardScroll(ScrollViewer scroller)
{
    /// <summary>How long a key's scroll takes, in milliseconds.</summary>
    internal const double Duration = 150;

    // kEpsilon, and the fudge factor for the ease-out in VelocityBasedDurationBound.
    private const double Epsilon = 0.01;
    private const double DurationBoundFactor = 2.5;

    internal static readonly CubicBezierEasing EaseInOut = new(0.42, 0, 0.58, 1);

    private IDisposable? _frame;
    private CubicBezierEasing _timing = EaseInOut;
    private Vector _initial;
    private Vector _target;
    private Vector _last;
    private TimeSpan _start;

    // In milliseconds from _start, as the curve's last_retarget_ and total_animation_duration_.
    private double _lastRetarget;
    private double _total;

    public bool IsRunning => _frame is not null;

    /// <summary>
    /// Where it's going, or where it is when it isn't going anywhere: what the next key adds to. Not where it was going if something
    /// else has scrolled it since its last frame, which ends it.
    /// </summary>
    public Vector Target => IsRunning && !MovedByOthers ? _target : scroller.Offset;

    /// <summary>
    /// Running, and nobody else has scrolled it since its last frame, and it hasn't reached its end: Chromium removes a finished
    /// animation, so a key after that starts afresh.
    /// </summary>
    private bool Continues => IsRunning && !MovedByOthers && (AvonicConfig.Now() - _start).TotalMilliseconds < _total;

    private bool MovedByOthers =>
        Math.Abs(scroller.Offset.X - _last.X) > 0.5 || Math.Abs(scroller.Offset.Y - _last.Y) > 0.5;

    public void To(Vector target)
    {
        if (!Continues)
        {
            Stop();
            _timing = EaseInOut;
            _initial = _last = scroller.Offset;
            _target = target;
            _start = AvonicConfig.Now();
            _lastRetarget = 0;
            _total = Duration;
            _frame = AvonicConfig.RequestFrame(scroller, OnFrame);
            return;
        }

        UpdateTarget((AvonicConfig.Now() - _start).TotalMilliseconds, target);
    }

    public void Stop()
    {
        _frame?.Dispose();
        _frame = null;
    }

    private void UpdateTarget(double t, Vector target)
    {
        if (Math.Abs(MaximumDimension(_target - target)) < Epsilon)
        {
            _target = target;
            return;
        }

        var delayedBy = Math.Max(0, _lastRetarget - t);
        t = Math.Max(t, _lastRetarget);
        var current = ValueAt(t);
        var newDelta = target - current;

        if (Math.Abs(MaximumDimension(newDelta)) < Epsilon)
        {
            _lastRetarget = _total = t;
            _target = target;
            return;
        }

        var segment = Math.Max(0, Duration - delayedBy);
        var oldDuration = _total - _lastRetarget;
        if (oldDuration <= 0)
        {
            _timing = EaseInOut;
            _initial = current;
            _target = target;
            _lastRetarget = t;
            _total = t + segment;
            return;
        }

        var velocity = VelocityAt(t);
        var newDuration = Math.Min(segment, DurationBound(velocity, newDelta));
        var slope = velocity * (newDuration / 1000) / MaximumDimension(newDelta);
        _timing = new CubicBezierEasing(EaseInOut.X1, EaseInOut.X1 * Math.Clamp(slope, -1000, 1000), EaseInOut.X2, EaseInOut.Y2);
        _initial = current;
        _target = target;
        _total = t + newDuration;
        _lastRetarget = t;
    }

    /// <summary><c>GetValue</c>.</summary>
    private Vector ValueAt(double t)
    {
        var duration = _total - _lastRetarget;
        if (duration <= 0 || t >= _total)
        {
            return _target;
        }

        if (t <= _lastRetarget)
        {
            return _initial;
        }

        return _initial + (_target - _initial) * _timing.Ease((t - _lastRetarget) / duration);
    }

    /// <summary><c>CalculateVelocity</c>, in pixels a second along the larger axis.</summary>
    private double VelocityAt(double t)
    {
        var duration = _total - _lastRetarget;
        var slope = _timing.Slope((t - _lastRetarget) / duration);
        return slope * (MaximumDimension(_target - _initial) / (duration / 1000));
    }

    /// <summary><c>VelocityBasedDurationBound</c>, in milliseconds.</summary>
    private static double DurationBound(double velocity, Vector newDelta)
    {
        var distance = MaximumDimension(newDelta);
        if (Math.Abs(velocity) < Epsilon)
        {
            return double.PositiveInfinity;
        }

        var bound = distance / velocity * DurationBoundFactor * 1000;
        return bound < 0 ? double.PositiveInfinity : bound;
    }

    private static double MaximumDimension(Vector v) => Math.Abs(v.X) > Math.Abs(v.Y) ? v.X : v.Y;

    private void OnFrame()
    {
        _frame = null;
        if (MovedByOthers)
        {
            // Someone else scrolled it.
            return;
        }

        var t = (AvonicConfig.Now() - _start).TotalMilliseconds;
        scroller.SetCurrentValue(ScrollViewer.OffsetProperty, ValueAt(t));
        _last = scroller.Offset;
        if (t < _total)
        {
            _frame = AvonicConfig.RequestFrame(scroller, OnFrame);
        }
    }
}
