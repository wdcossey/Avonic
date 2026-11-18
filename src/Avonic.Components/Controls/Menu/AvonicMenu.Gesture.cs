using Avalonia;
using Avalonia.Logging;
using Avonic.Components.Animation;
using Avonic.Components.Gestures;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>The menu's swipe, Ionic's <c>menu-swipe</c> gesture on the document: here on the top level.</summary>
public partial class AvonicMenu
{
    /// <summary>The easing a swipe's release plays on, and that <c>getTimeGivenProgression</c> reads the step through.</summary>
    private const string SwipeEasing = "cubic-bezier(0.4, 0.0, 0.6, 1)";

    /// <summary>A swipe's release plays over 300 ms.</summary>
    private const double SwipeEndDuration = 300;

    /// <summary>The speed, in px/ms, past which a release completes whatever the distance.</summary>
    private const double CompletingVelocity = 0.2;

    /// <summary>Ionic's <c>canSwipe</c>.</summary>
    private bool CanSwipe() => MenuSwipeGesture && !IsMenuAnimating && IsActiveCore;

    /// <summary>
    /// Ionic's <c>canStart</c>: never while a modal is presented; an open menu, from anywhere; a closed one, only while no other menu
    /// is open, and from within <see cref="MaxEdgeStart"/> of the window's edge on its side.
    /// </summary>
    private bool CanStart(GestureDetail detail)
    {
        if ((_topLevel is { } top && OverlayHost.IsModalPresentedIn(top)) || !CanSwipe())
        {
            return false;
        }

        if (IsOpenState)
        {
            return true;
        }

        // Ionic checks only for an open menu (_getOpenSync); one still opening is refused here too, as the controller refuses any
        // open while a menu animates, so that two can't end up open.
        if (MenuController.GetOpen() is not null || MenuController.IsAnimating())
        {
            return false;
        }

        return CheckEdgeSide(_topLevel?.ClientSize.Width ?? 0, detail.CurrentX, _isEndSide, MaxEdgeStart);
    }

    /// <summary>Ionic's <c>checkEdgeSide</c>: <paramref name="posX"/> within <paramref name="maxEdgeStart"/> of that edge.</summary>
    internal static bool CheckEdgeSide(double innerWidth, double posX, bool isEndSide, double maxEdgeStart) =>
        isEndSide ? posX >= innerWidth - maxEdgeStart : posX <= maxEdgeStart;

    /// <summary>Ionic's <c>computeDelta</c>: how far the swipe has gone the way that moves the menu, never less than 0.</summary>
    internal static double ComputeDelta(double deltaX, bool isOpen, bool isEndSide) =>
        Math.Max(0, isOpen != isEndSide ? -deltaX : deltaX);

    private Task OnWillStart(GestureDetail detail)
    {
        BeforeAnimation(!IsOpenState, GestureRole);
        try
        {
            LoadAnimation();
        }
        catch (Exception ex)
        {
            // No animation for its type, or a builder that threw: the swipe ends where it started, with its did event, and the menu
            // isn't left animating. Logged, not thrown, as it would be thrown from the input that moved the finger.
            UndoBeforeAnimation(GestureRole);
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "[AvonicMenu] - The swipe has no animation: {Error}", ex.Message);
        }

        return Task.CompletedTask;
    }

    private void OnStart(GestureDetail detail)
    {
        if (!IsMenuAnimating || _animation is null)
        {
            return;
        }

        // The cloned animation should not use an easing curve during seek.
        _animation.ProgressStart(true, IsOpenState ? 1 : 0);
    }

    private void OnMove(GestureDetail detail)
    {
        if (!IsMenuAnimating || _animation is null || _width is not { } width)
        {
            return;
        }

        var delta = ComputeDelta(detail.DeltaX, IsOpenState, _isEndSide);
        var stepValue = delta / width;
        _animation.ProgressStep(IsOpenState ? 1 - stepValue : stepValue);
    }

    /// <summary>
    /// Ionic's <c>onEnd</c>: a release past half the width, or faster than 0.2 px/ms, the way that moves the menu completes it;
    /// otherwise it goes back. The animation plays from where the finger left it, on <see cref="SwipeEasing"/>, the step converted
    /// from linear to that curve's time.
    /// </summary>
    private void OnEnd(GestureDetail detail)
    {
        if (!IsMenuAnimating || _animation is null || _width is not { } width)
        {
            return;
        }

        var isOpen = IsOpenState;
        var isEndSide = _isEndSide;
        var delta = ComputeDelta(detail.DeltaX, isOpen, isEndSide);
        var stepValue = delta / width;
        var velocity = detail.VelocityX;
        var z = width / 2.0;
        var shouldCompleteRight = velocity >= 0 && (velocity > CompletingVelocity || detail.DeltaX > z);
        var shouldCompleteLeft = velocity <= 0 && (velocity < -CompletingVelocity || detail.DeltaX < -z);
        var shouldComplete = isOpen
            ? isEndSide ? shouldCompleteRight : shouldCompleteLeft
            : isEndSide ? shouldCompleteLeft : shouldCompleteRight;
        var shouldOpen = !isOpen && shouldComplete;
        if (isOpen && !shouldComplete)
        {
            shouldOpen = true;
        }

        _lastOnEnd = detail.CurrentTime;

        // Account for rounding errors.
        var newStepValue = shouldComplete ? 0.001 : -0.001;

        // The step can be negative, and a cubic bezier's time can't.
        var adjustedStepValue = stepValue < 0 ? 0.01 : stepValue;

        // The animation is reversed here, so the easing is too, and the step, linear, is converted to that curve's time.
        var times = CubicBezier.GetTimeGivenProgression(new Point(0, 0), new Point(0.4, 0), new Point(0.6, 1), new Point(1, 1),
            Math.Clamp(adjustedStepValue, 0, 0.9999));
        newStepValue += times.Count > 0 && !double.IsNaN(times[0]) ? times[0] : 0;
        var playTo = IsOpenState ? !shouldComplete : shouldComplete;
        _animation
            .Easing(SwipeEasing)
            .OnFinish((_, _) => AfterAnimation(shouldOpen, GestureRole), oneTimeCallback: true)
            .ProgressEnd(playTo ? 1 : 0, IsOpenState ? 1 - newStepValue : newStepValue, SwipeEndDuration);
    }
}
