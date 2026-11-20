using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's swipe to go back (<c>utils/gesture/swipe-back.js</c> and <c>nav.js</c>'s <c>canStart</c>, <c>onStart</c>,
/// <c>onMove</c>, <c>onEnd</c>): a drag from the start edge drives the back transition, and completes it or not by its distance and
/// speed. Off by default, as md has it.
/// </summary>
public partial class AvonicNav
{
    /// <summary>
    /// Ionic <c>swipeGesture</c>: whether a drag from within 50 of the window's start edge goes back. Null takes
    /// <see cref="AvonicConfig.SwipeBackEnabled"/> (false, md's) when the nav is first attached. Named as
    /// <c>SegmentSwipeGesture</c> is, as <c>InputElement.SwipeGesture</c> is an event.
    /// </summary>
    public static readonly StyledProperty<bool?> NavSwipeGestureProperty =
        AvaloniaProperty.Register<AvonicNav, bool?>(nameof(NavSwipeGesture));

    /// <summary>Ionic's <c>threshold</c>: how near the edge, in device-independent pixels, a swipe must start.</summary>
    internal const double SwipeEdge = 50;

    /// <summary>Ionic's: above the 100 of other horizontal swipes, such as a sliding item's.</summary>
    internal const int SwipeGesturePriority = 101;

    /// <summary>The longest a swipe's end plays for, in ms.</summary>
    internal const double SwipeMaxDuration = 540;

    private readonly ConfigOnLoad<bool?> _swipeConfig;
    private Gesture? _gesture;
    private bool _rtl;
    private readonly StyleValue<bool> _swipeClicksBlocked = new();
    private Action? _unblockSwipeClicks;

    public bool? NavSwipeGesture
    {
        get => GetValue(NavSwipeGestureProperty);
        set => SetValue(NavSwipeGestureProperty, value);
    }

    /// <summary>Ionic's <c>componentWillLoad</c> and <c>componentDidLoad</c> for the swipe: the config's default, then the gesture.</summary>
    partial void LoadSwipeGesture()
    {
        _swipeConfig.Load(() => AvonicConfig.SwipeBackEnabled);
        _gesture = Gesture.Create(new GestureConfig
        {
            Element = this,
            GestureName = "goback-swipe",
            GesturePriority = SwipeGesturePriority,
            Threshold = 10,
            CanStart = CanStartSwipe,
            OnStart = _ => OnSwipeStart(),
            OnMove = OnSwipeMove,
            OnEnd = OnSwipeEnd,
        });
        SwipeGestureChanged();
    }

    /// <summary>Ionic <c>swipeGestureChanged</c>.</summary>
    private void SwipeGestureChanged() => _gesture?.Enable(NavSwipeGesture == true);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == NavSwipeGestureProperty)
        {
            SwipeGestureChanged();
        }
    }

    /// <summary>The window's width, Ionic's <c>win.innerWidth</c>.</summary>
    private double WindowWidth => TopLevel.GetTopLevel(this)?.ClientSize.Width ?? Bounds.Width;

    /// <summary>
    /// <c>swipe-back.js</c>'s <c>canStart</c>, with <c>nav.js</c>'s: from the start edge (the right one in RTL, read at each
    /// gesture), with nothing transitioning or queued, and a page to go back to.
    /// </summary>
    private bool CanStartSwipe(GestureDetail detail)
    {
        _rtl = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;
        var atEdge = _rtl ? detail.StartX >= WindowWidth - SwipeEdge : detail.StartX <= SwipeEdge;
        return atEdge
            && !_gestureOrAnimationInProgress
            && NavSwipeGesture == true
            && !_isTransitioning
            && _transInstr.Count == 0
            && CanGoBack();
    }

    /// <summary>
    /// Ionic <c>onStart</c>: a pop held for the gesture. With animations off there's nothing to drive, so the pop waits for the
    /// end, and the nav takes no click meanwhile (<c>blockSwipeClicks</c>).
    /// </summary>
    private void OnSwipeStart()
    {
        _gestureOrAnimationInProgress = true;
        if (!(Animated && AvonicConfig.Animated))
        {
            _swipeClicksBlocked.Set(false);
            _unblockSwipeClicks = () => AvonicConfig.Schedule(() => _swipeClicksBlocked.Clear(), TimeSpan.Zero);
            return;
        }

        _ = Pop(new NavOptions { Direction = NavDirection.Back, ProgressAnimation = true });
    }

    private double DeltaX(GestureDetail detail) => _rtl ? -detail.DeltaX : detail.DeltaX;

    /// <summary>Ionic <c>onMove</c>: the transition held at the drag's share of the window's width.</summary>
    private void OnSwipeMove(GestureDetail detail) => _sbAni?.ProgressStep(DeltaX(detail) / WindowWidth);

    /// <summary>
    /// <c>swipe-back.js</c>'s <c>onEnd</c> and <c>nav.js</c>'s: it completes moving forward, faster than 0.2 or past half the width;
    /// it plays to the end or back to the start over the time the rest of the way takes at the drag's speed, at most 540 ms, with the
    /// curve reversed going back.
    /// </summary>
    private void OnSwipeEnd(GestureDetail detail)
    {
        var delta = DeltaX(detail);
        var width = WindowWidth;
        var stepValue = delta / width;
        var velocity = _rtl ? -detail.VelocityX : detail.VelocityX;
        var z = width / 2.0;
        var shouldComplete = velocity >= 0 && (velocity > 0.2 || delta > z);
        var missing = shouldComplete ? 1 - stepValue : stepValue;
        var missingDistance = missing * width;
        var realDur = 0.0;
        if (missingDistance > 5)
        {
            realDur = Math.Min(missingDistance / Math.Abs(velocity), SwipeMaxDuration);
        }

        var step = stepValue <= 0 ? 0.01 : Math.Clamp(stepValue, 0, 0.9999);
        if (_sbAni is { } ani)
        {
            ani.OnFinish((_, _) => _gestureOrAnimationInProgress = false, oneTimeCallback: true);

            // The animation is reversed going back, so the curve is too, and the step, which is linear, is read on it.
            var newStepValue = shouldComplete ? -0.001 : 0.001;
            if (!shouldComplete)
            {
                ani.Easing("cubic-bezier(1, 0, 0.68, 0.28)");
                newStepValue += FirstOrZero(CubicBezier.GetTimeGivenProgression(new(0, 0), new(1, 0), new(0.68, 0.28), new(1, 1), step));
            }
            else
            {
                newStepValue += FirstOrZero(CubicBezier.GetTimeGivenProgression(new(0, 0), new(0.32, 0.72), new(0, 1), new(1, 1), step));
            }

            ani.ProgressEnd(shouldComplete ? 1 : 0, newStepValue, realDur);
        }
        else
        {
            _gestureOrAnimationInProgress = false;
            if (_unblockSwipeClicks is { } unblock)
            {
                _unblockSwipeClicks = null;
                if (shouldComplete)
                {
                    _ = Pop(new NavOptions { Direction = NavDirection.Back });
                }

                unblock();
            }
        }
    }

    /// <summary>Ionic reads <c>[0]</c> of the solutions; with none it would be <c>NaN</c>, so 0 here, the start.</summary>
    private static double FirstOrZero(IReadOnlyList<double> solutions) => solutions.Count > 0 ? solutions[0] : 0;
}
