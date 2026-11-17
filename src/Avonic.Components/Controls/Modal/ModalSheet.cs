using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avonic.Components.Animation;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>createSheetGesture</c> (<c>modal/gestures/sheet.js</c>), line for line: the sheet modal's drag, its snapping, and its
/// backdrop by breakpoint, over the animation <see cref="AvonicModal"/> made for it, whose children are found by their ids. Made
/// after the present's enter animation, and gone with the dismiss.
/// </summary>
internal sealed class ModalSheet
{
    /// <summary><c>FLICK_PROJECTION_MS</c>: how far ahead, in milliseconds, a drag's velocity is projected when snapping.</summary>
    internal const double FlickProjectionMs = 350;

    /// <summary><c>canDismissMaxStep</c>: how far down a drag goes when <c>canDismiss</c> stops it.</summary>
    internal const double CanDismissMaxStep = 0.95;

    /// <summary>The snap's duration, <c>progressEnd(1, 0, animated ? 500 : 0)</c>.</summary>
    internal const double SnapDuration = 500;

    private readonly AvonicModal _baseEl;
    private readonly AvonicBackdrop _backdropEl;
    private readonly double _backdropBreakpoint;
    private readonly AvonicAnimation _animation;
    private readonly IReadOnlyList<double> _breakpoints;
    private readonly bool _expandToScroll;
    private readonly Func<double> _getCurrentBreakpoint;
    private readonly Action _onDismiss;
    private readonly Action<double> _onBreakpointChange;
    private readonly AvonicContent? _contentEl;
    private readonly bool _initialContentScrollY;
    private readonly double _height;
    private readonly double _maxBreakpoint;
    private readonly double _minBreakpoint;
    private readonly AvonicAnimation? _wrapperAnimation;
    private readonly AvonicAnimation? _backdropAnimation;
    private readonly AvonicAnimation? _contentAnimation;

    private double _currentBreakpoint;
    private double _offset;
    private bool _canDismissBlocksGesture;
    private Control? _cachedScrollEl;

    /// <summary>The content the press is on, from <c>canStart</c>, for letting go of an upward drag that scrolls it.</summary>
    private Control? _pressContent;

    internal ModalSheet(
        AvonicModal baseEl,
        AvonicBackdrop backdropEl,
        ModalWrapper wrapperEl,
        double initialBreakpoint,
        double backdropBreakpoint,
        AvonicAnimation animation,
        IReadOnlyList<double> breakpoints,
        bool expandToScroll,
        Func<double> getCurrentBreakpoint,
        Action onDismiss,
        Action<double> onBreakpointChange)
    {
        _baseEl = baseEl;
        _backdropEl = backdropEl;
        _backdropBreakpoint = backdropBreakpoint;
        _animation = animation;
        _breakpoints = breakpoints;
        _expandToScroll = expandToScroll;
        _getCurrentBreakpoint = getCurrentBreakpoint;
        _onDismiss = onDismiss;
        _onBreakpointChange = onBreakpointChange;

        // baseEl.querySelector('ion-content'): the first in the modal's content, in document order.
        _contentEl = FirstContent(baseEl.Content);

        // Cache the initial value so the gesture restores it instead of forcing scrolling on.
        _initialContentScrollY = _contentEl?.ScrollY ?? true;

        // The height of the sheet the user actually sees: the wrapper's clientHeight, inside its border.
        _height = wrapperEl.Bounds.Height - wrapperEl.BorderThickness.Top - wrapperEl.BorderThickness.Bottom;
        _currentBreakpoint = initialBreakpoint;
        _maxBreakpoint = breakpoints.Count > 0 ? breakpoints[^1] : 0;
        _minBreakpoint = breakpoints.Count > 0 ? breakpoints[0] : 0;
        _wrapperAnimation = animation.ChildAnimations.FirstOrDefault(a => a.Id == "wrapperAnimation");
        _backdropAnimation = animation.ChildAnimations.FirstOrDefault(a => a.Id == "backdropAnimation");
        _contentAnimation = animation.ChildAnimations.FirstOrDefault(a => a.Id == "contentAnimation");

        // After the entering animation, the animation goes from offset 0 to 1 so that users can swipe either way, held at the
        // current breakpoint.
        if (_wrapperAnimation is not null && _backdropAnimation is not null)
        {
            SetDefaultKeyframes();
            animation.ProgressStart(true, 1 - _currentBreakpoint);
            UpdateBackdrop(_currentBreakpoint);
        }

        if (_contentEl is not null && _currentBreakpoint != _maxBreakpoint && expandToScroll)
        {
            _contentEl.SetCurrentValue(AvonicContent.ScrollYProperty, false);
        }

        Gesture = Gesture.Create(new GestureConfig
        {
            Element = wrapperEl,
            GestureName = "modalSheet",
            GesturePriority = 40,
            Direction = GestureDirection.Y,
            Threshold = 10,
            CanStart = CanStart,
            OnStart = OnStart,
            OnMove = OnMove,
            OnEnd = OnEnd,
            ReleaseAgainstDirectionWhen = ReleasesUpwardDrag,
        });
    }

    internal Gesture Gesture { get; }

    /// <summary>Ionic's <c>getBackdropValueForSheet</c> (<c>modal/utils.js</c>): the backdrop's share of its opacity at a breakpoint.</summary>
    internal static double GetBackdropValueForSheet(double x, double backdropBreakpoint)
    {
        if (backdropBreakpoint == 1)
        {
            return 0;
        }

        var slope = 1 / (1 - backdropBreakpoint);
        var b = -(backdropBreakpoint * slope);
        return x * slope + b;
    }

    /// <summary>Ionic's <c>calculateSpringStep</c> (<c>gestures/utils.js</c>): the spring a blocked drag down feels.</summary>
    internal static double CalculateSpringStep(double t) =>
        0.00255275 * Math.Pow(2.71828, -14.9619 * t) - 1.00255 * Math.Pow(2.71828, -0.0380968 * t) + 1;

    /// <summary><c>calc(var(--backdrop-opacity) * x)</c>, an opacity, clamped as CSS computes one.</summary>
    internal static double BackdropOpacity(AvonicModal modal, double share) => Math.Clamp(modal.BackdropOpacity() * share, 0, 1);

    /// <summary>A percentage as Ionic's template strings write it.</summary>
    internal static string Percent(double value) => value.ToString(CultureInfo.InvariantCulture) + "%";

    /// <summary><c>resetContentScroll</c>: the content's scrolling as the app declared it.</summary>
    internal void ResetContentScroll() => _contentEl?.SetCurrentValue(AvonicContent.ScrollYProperty, _initialContentScrollY);

    /// <summary>
    /// <c>moveSheetToBreakpoint</c>: to <paramref name="breakpoint"/>, or back where it was when <paramref name="canDismiss"/> stops a
    /// move to 0, from <paramref name="breakpointOffset"/> (0 open, 1 closed). Completes when it's there, or at once when it dismisses.
    /// </summary>
    internal Task MoveSheetToBreakpoint(double breakpoint, double breakpointOffset, bool canDismiss, bool animated)
    {
        // canDismiss only prevents snapping when users are trying to dismiss.
        var shouldPreventDismiss = canDismiss && breakpoint == 0;
        var snapToBreakpoint = shouldPreventDismiss ? _currentBreakpoint : breakpoint;
        var shouldRemainOpen = snapToBreakpoint != 0;
        _currentBreakpoint = 0;

        // The animation plays from the last offset to the closest snap point.
        if (_wrapperAnimation is not null && _backdropAnimation is not null)
        {
            _wrapperAnimation.Keyframes([
                new AnimationKeyframe(0) { ["transform"] = $"translateY({Percent(breakpointOffset * 100)})" },
                new AnimationKeyframe(1) { ["transform"] = $"translateY({Percent((1 - snapToBreakpoint) * 100)})" },
            ]);
            _backdropAnimation.Keyframes([
                new AnimationKeyframe(0)
                {
                    ["opacity"] = BackdropOpacity(_baseEl, GetBackdropValueForSheet(1 - breakpointOffset, _backdropBreakpoint)),
                },
                new AnimationKeyframe(1)
                {
                    ["opacity"] = BackdropOpacity(_baseEl, GetBackdropValueForSheet(snapToBreakpoint, _backdropBreakpoint)),
                },
            ]);

            // The content scrolls at any breakpoint when expandToScroll is off, so it's as tall as what shows.
            _contentAnimation?.Keyframes([
                new AnimationKeyframe(0) { ["maxHeight"] = Percent((1 - breakpointOffset) * 100) },
                new AnimationKeyframe(1) { ["maxHeight"] = Percent(snapToBreakpoint * 100) },
            ]);
            _animation.ProgressStep(0);
        }

        // The gesture stays off until the snap has finished.
        Gesture.Enable(false);
        if (shouldPreventDismiss)
        {
            _ = HandleCanDismiss();
        }
        else if (!shouldRemainOpen)
        {
            _onDismiss();
        }

        // Scrolling comes back at once when it'll be fully open, or scrolls anywhere, as native iOS allows it at the release.
        if (_contentEl is not null && (snapToBreakpoint == _maxBreakpoint || !_expandToScroll))
        {
            _contentEl.SetCurrentValue(AvonicContent.ScrollYProperty, _initialContentScrollY);
        }

        var moved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _animation
            .OnFinish((_, _) =>
            {
                if (!shouldRemainOpen)
                {
                    moved.TrySetResult();
                    return;
                }

                if (_wrapperAnimation is null || _backdropAnimation is null)
                {
                    Gesture.Enable(true);
                    moved.TrySetResult();
                    return;
                }

                // Then the animation from 0 to 1 again, at the snapped position.
                AvonicConfig.RequestFrame(_baseEl, () =>
                {
                    SetDefaultKeyframes();
                    _animation.ProgressStart(true, 1 - snapToBreakpoint);
                    _currentBreakpoint = snapToBreakpoint;
                    _onBreakpointChange(_currentBreakpoint);

                    // The backdrop takes input above backdropBreakpoint.
                    UpdateBackdrop(_currentBreakpoint);
                    Gesture.Enable(true);
                    moved.TrySetResult();
                });
            }, oneTimeCallback: true)
            .ProgressEnd(1, 0, animated ? SnapDuration : 0);
        return moved.Task;
    }

    /// <summary>
    /// <c>calculateSnapBreakpoint</c>: the breakpoint nearest where the drag would be <see cref="FlickProjectionMs"/> later at its
    /// speed, so that a flick counts. On a tie, the first.
    /// </summary>
    internal double CalculateSnapBreakpoint(double deltaY, double velocityY)
    {
        var currentProgress = CalculateProgress(deltaY + velocityY * FlickProjectionMs);
        return _breakpoints.Aggregate((a, b) => Math.Abs(b - currentProgress) < Math.Abs(a - currentProgress) ? b : a);
    }

    /// <summary><c>calculateProgress</c>: how open the sheet is part way through a drag, 1 fully open, rounded to thousandths.</summary>
    private double CalculateProgress(double deltaY)
    {
        var progress = _currentBreakpoint - deltaY / _height;
        // JavaScript's Math.round: halves go up.
        var rounded = Math.Floor(progress * 1000 + 0.5) / 1000;
        return Math.Clamp(rounded, 0, 1);
    }

    /// <summary><c>calculateEventProgress</c>: 0 at the lowest breakpoint and 1 at the highest, for the drag events.</summary>
    private double CalculateEventProgress(double deltaY)
    {
        var progress = (CalculateProgress(deltaY) - _minBreakpoint) / (_maxBreakpoint - _minBreakpoint);
        return double.IsNaN(progress) ? 0 : Math.Clamp(progress, 0, 1);
    }

    private void SetDefaultKeyframes()
    {
        // SheetDefaults: the wrapper from 0% to 100% down, the backdrop from var(--backdrop-opacity) to 0.01, or to 0 at
        // 1 - backdropBreakpoint and on, and the content from 100% to 0% of the wrapper.
        var full = BackdropOpacity(_baseEl, 1);
        _wrapperAnimation!.Keyframes([
            new AnimationKeyframe(0) { ["transform"] = "translateY(0%)" },
            new AnimationKeyframe(1) { ["transform"] = "translateY(100%)" },
        ]);
        _backdropAnimation!.Keyframes(_backdropBreakpoint != 0
            ?
            [
                new AnimationKeyframe(0) { ["opacity"] = full },
                new AnimationKeyframe(1 - _backdropBreakpoint) { ["opacity"] = 0 },
                new AnimationKeyframe(1) { ["opacity"] = 0 },
            ]
            :
            [
                new AnimationKeyframe(0) { ["opacity"] = full },
                new AnimationKeyframe(1) { ["opacity"] = 0.01 },
            ]);
        _contentAnimation?.Keyframes([
            new AnimationKeyframe(0) { ["maxHeight"] = "100%" },
            new AnimationKeyframe(1) { ["maxHeight"] = "0%" },
        ]);
    }

    /// <summary>
    /// <c>shouldEnableBackdrop</c>, then <c>enableBackdrop</c> or <c>disableBackdrop</c>: above <c>backdropBreakpoint</c>, with the
    /// focus trap and the backdrop on, the backdrop takes input and the focus trap is on; otherwise neither, so the page behind
    /// takes input. An explicit opt-out of either is never undone.
    /// </summary>
    private void UpdateBackdrop(double breakpoint)
    {
        // pointer-events on the host as well as the backdrop: the host draws nothing and takes no input of its own here, and its
        // wrapper keeps pointer-events: auto, so the backdrop's is what changes.
        var enable = breakpoint > _backdropBreakpoint && _baseEl.FocusTrap && _baseEl.ShowBackdrop;
        _backdropEl.SetValue(Avalonia.Input.InputElement.IsHitTestVisibleProperty, enable);
        _baseEl.SheetBackdropOff = !enable;
    }

    private bool CanStart(GestureDetail detail)
    {
        // The content can change between drags (a nav inside the modal), so it's found from the press each time.
        var contentEl = detail.Event?.Source is Visual source ? ContentUtils.FindClosestIonContent(source) : null;
        _pressContent = contentEl;
        _currentBreakpoint = _getCurrentBreakpoint();

        // Upward swipes on the content can't move the sheet anyway, so this only blocks swiping the sheet down from the content.
        if (contentEl is not null && (!_expandToScroll || _currentBreakpoint == 1))
        {
            return CanSwipeOnContent(contentEl);
        }

        return true;
    }

    /// <summary>
    /// Not in Ionic: an upward drag on content that scrolls can't move the sheet (at its top breakpoint, or with
    /// <c>expandToScroll</c> off), and Ionic's passive gesture lets the browser scroll the content all the same. Avalonia gives a
    /// pointer to one owner, so the gesture lets go of it for the content's <see cref="ScrollViewer"/>.
    /// </summary>
    private bool ReleasesUpwardDrag() =>
        _pressContent is not null && (!_expandToScroll || _getCurrentBreakpoint() == _maxBreakpoint);

    /// <summary>
    /// <c>canSwipeOnContent</c>: only when the content is scrolled to the top, so as not to fight its scrolling, and never on content
    /// with a refresher.
    /// </summary>
    private static bool CanSwipeOnContent(Control contentEl)
    {
        var scrollEl = ContentUtils.GetScrollElementWithin(contentEl);
        return !HasRefresher(contentEl) && (scrollEl is null || scrollEl.Offset.Y == 0);
    }

    /// <summary><c>findRefresherInContent</c>: an <see cref="AvonicRefresher"/> in what the app put in the content.</summary>
    private static bool HasRefresher(Control contentEl)
    {
        IEnumerable<object?> slots = contentEl is AvonicContent content ? [content.Content, content.FixedContent] : [contentEl];
        return slots.OfType<ILogical>().SelectMany(s => s.GetSelfAndLogicalDescendants()).OfType<AvonicRefresher>().Any();
    }

    /// <summary>The first <see cref="AvonicContent"/> in the modal's content, as <c>querySelector</c> finds it.</summary>
    private static AvonicContent? FirstContent(object? content) =>
        content is ILogical root ? root.GetSelfAndLogicalDescendants().OfType<AvonicContent>().FirstOrDefault() : null;

    private void OnStart(GestureDetail detail)
    {
        // canDismiss other than true lets the drag go down only so far, when 0 is a breakpoint.
        _canDismissBlocksGesture = !_baseEl.CanDismiss.IsTrue && _minBreakpoint == 0;

        // The scroll element, for onMove and onEnd.
        if (!_expandToScroll)
        {
            var targetEl = detail.Event?.Source is Visual source ? ContentUtils.FindClosestIonContent(source) : null;
            _cachedScrollEl = targetEl is AvonicContent content ? content.GetScrollElement() : targetEl;
        }

        // Pulling down may be pulling on the content, which mustn't scroll at the same time.
        if (detail.DeltaY > 0 && _contentEl is not null)
        {
            _contentEl.SetCurrentValue(AvonicContent.ScrollYProperty, false);
        }

        // Dismisses an open keyboard, and puts focus on the modal.
        AvonicConfig.RequestFrame(_baseEl, () => _baseEl.Focus());
        _animation.ProgressStart(true, 1 - _currentBreakpoint);
        _baseEl.RaiseDragStart();
    }

    private void OnMove(GestureDetail detail)
    {
        // With expandToScroll off, an upward swipe within the scrollable content doesn't move the sheet.
        if (!_expandToScroll && detail.DeltaY <= 0 && _cachedScrollEl is not null)
        {
            return;
        }

        // Pulling down, after scrolling back to the top, mustn't scroll the content.
        if (detail.DeltaY > 0 && _contentEl is not null)
        {
            _contentEl.SetCurrentValue(AvonicContent.ScrollYProperty, false);
        }

        // Where the animation is, from where the user dragged.
        var initialStep = 1 - _currentBreakpoint;
        double? secondToLastBreakpoint = _breakpoints.Count > 1 ? 1 - _breakpoints[1] : null;
        var step = initialStep + detail.DeltaY / _height;
        var isAttemptingDismissWithCanDismiss = secondToLastBreakpoint is { } second && step >= second && _canDismissBlocksGesture;

        // Blocked from dismissing, it can't be hidden completely, and it springs past the breakpoint after 0.
        var maxStep = isAttemptingDismissWithCanDismiss ? CanDismissMaxStep : 0.9999;
        var processedStep = isAttemptingDismissWithCanDismiss && secondToLastBreakpoint is { } s
            ? s + CalculateSpringStep((step - s) / (maxStep - s))
            : step;
        _offset = Math.Clamp(processedStep, 0.0001, maxStep);
        _animation.ProgressStep(_offset);

        var snapBreakpoint = CalculateSnapBreakpoint(detail.DeltaY, detail.VelocityY);
        _baseEl.RaiseDragMove(new ModalDragEventArgs(
            AvonicModal.IonDragMoveEvent, detail.CurrentY, detail.DeltaY, detail.VelocityY, CalculateEventProgress(detail.DeltaY), snapBreakpoint, null));
    }

    private void OnEnd(GestureDetail detail)
    {
        var snapBreakpoint = CalculateSnapBreakpoint(detail.DeltaY, detail.VelocityY);

        // canDismiss false snaps to 0 but never dismisses; a function dismisses after it's answered, so it may.
        var shouldPreventDismiss = _canDismissBlocksGesture && snapBreakpoint == 0;
        var isDismissing = shouldPreventDismiss ? _baseEl.CanDismiss.Handler is not null : snapBreakpoint == 0;
        var progress = CalculateEventProgress(detail.DeltaY);

        // With expandToScroll off, an upward swipe in content that isn't at its top doesn't move the sheet.
        if (!_expandToScroll && detail.DeltaY <= 0 && _cachedScrollEl is not null && ScrollTop(_cachedScrollEl) > 0)
        {
            _baseEl.RaiseDragEnd(new ModalDragEventArgs(
                AvonicModal.IonDragEndEvent, detail.CurrentY, detail.DeltaY, detail.VelocityY, progress, snapBreakpoint, false));
            return;
        }

        // The swipe is the user's, so it always animates.
        _ = MoveSheetToBreakpoint(snapBreakpoint, _offset, _canDismissBlocksGesture, animated: true);
        _baseEl.RaiseDragEnd(new ModalDragEventArgs(
            AvonicModal.IonDragEndEvent, detail.CurrentY, detail.DeltaY, detail.VelocityY, progress, snapBreakpoint, isDismissing));
    }

    private static double ScrollTop(Control scrollEl) =>
        (scrollEl as ScrollViewer ?? ContentUtils.GetScrollElementWithin(scrollEl))?.Offset.Y ?? 0;

    /// <summary>
    /// <c>handleCanDismiss</c>: a <c>canDismiss</c> function is asked, with the role <c>gesture</c>; true dismisses with
    /// <c>handler</c>, once the snap back has finished.
    /// </summary>
    private async Task HandleCanDismiss()
    {
        if (_baseEl.CanDismiss.Handler is not { } handler)
        {
            return;
        }

        if (!await handler(null, Overlays.OverlayRoles.Gesture))
        {
            return;
        }

        if (_animation.IsRunning())
        {
            _animation.OnFinish((_, _) => _baseEl.DismissFromInput(Overlays.OverlayRoles.Handler), oneTimeCallback: true);
        }
        else
        {
            _baseEl.DismissFromInput(Overlays.OverlayRoles.Handler);
        }
    }
}
