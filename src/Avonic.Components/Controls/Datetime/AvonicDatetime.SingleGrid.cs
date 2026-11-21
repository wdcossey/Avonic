using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Not Ionic's: <see cref="DatetimeCalendarRendering.SingleGrid"/>, a calendar of one month of 42 days, drawn again in place when
/// the month changes, in place of Ionic's three months in a scroller (#248). The state, the days and what they do are the
/// scrolled calendar's; only how the month changes differs: a fade, and a swipe past a threshold.
/// docs/components/ion-datetime/README.md, "Extensions".
/// </summary>
public partial class AvonicDatetime
{
    /// <summary>
    /// Not Ionic's: how the calendar draws its months. <see cref="DatetimeCalendarRendering.ScrolledMonths"/>, Ionic's, by default,
    /// or <see cref="AvonicConfig.DatetimeCalendarRendering"/> when the datetime is made, at style priority, so a value set here or in
    /// a style wins.
    /// </summary>
    public static readonly StyledProperty<DatetimeCalendarRendering> CalendarRenderingProperty =
        AvaloniaProperty.Register<AvonicDatetime, DatetimeCalendarRendering>(nameof(CalendarRendering));

    /// <summary>
    /// The fade out, md's fade through: its outgoing part, the first 90 ms of 300, with md's accelerate curve, its standard
    /// <c>cubic-bezier(0.4, 0, 1, 1)</c>.
    /// </summary>
    internal const double GridFadeOutMs = 90;

    internal const string GridFadeOutEasing = "cubic-bezier(0.4, 0, 1, 1)";

    /// <summary>The fade in: the other 210 ms, with md's decelerate curve, <c>cubic-bezier(0, 0, 0.2, 1)</c>.</summary>
    internal const double GridFadeInMs = 210;

    internal const string GridFadeInEasing = "cubic-bezier(0, 0, 0.2, 1)";

    /// <summary>How far a swipe goes, in px, to change the month.</summary>
    internal const double GridSwipeDistance = 50;

    /// <summary>How fast a shorter swipe goes, in px per ms, to change the month: a flick.</summary>
    internal const double GridSwipeVelocity = 0.3;

    /// <summary>The least a flick goes, in px, past the gesture's own threshold.</summary>
    internal const double GridSwipeFlickDistance = 20;

    private DatetimeCalendarMonth? _calendarGrid;
    private Gesture? _gridGesture;
    private AvonicAnimation? _gridFade;
    private bool _gridFadingIn;
    private DatetimeParts? _gridTarget;
    private bool _gridKeyChange;

    /// <inheritdoc cref="CalendarRenderingProperty"/>
    public DatetimeCalendarRendering CalendarRendering
    {
        get => GetValue(CalendarRenderingProperty);
        set => SetValue(CalendarRenderingProperty, value);
    }

    /// <summary>The single grid, <c>PART_CalendarGrid</c>.</summary>
    internal DatetimeCalendarMonth? CalendarGrid => _calendarGrid;

    /// <summary>The single grid draws the calendar: asked for, and the template has one.</summary>
    internal bool UsesSingleGrid => CalendarRendering == DatetimeCalendarRendering.SingleGrid && _calendarGrid is not null;

    /// <summary>Whether the swipe listens.</summary>
    internal bool HasGridSwipe => _gridGesture is not null;

    /// <summary>The month a fade is on its way to, if one is.</summary>
    internal DatetimeParts? GridTarget => _gridTarget;

    private void AttachGrid(DatetimeCalendarMonth? grid)
    {
        _calendarGrid = grid;
        if (grid is null)
        {
            return;
        }

        // CSS's opacity fades the days and their focus rings as one picture.
        AnimationGroupOpacity.SetIsEnabled(grid, true);
        grid.AddHandler(KeyDownEvent, OnCalendarKeyDown, RoutingStrategies.Tunnel);
        grid.GotFocus += OnCalendarFocus;
        UpdateGridGesture();
    }

    private void DetachGrid()
    {
        if (_calendarGrid is { } grid)
        {
            grid.RemoveHandler(KeyDownEvent, OnCalendarKeyDown);
            grid.GotFocus -= OnCalendarFocus;
        }

        FinishGridChange();
        _gridGesture?.Destroy();
        _gridGesture = null;
        _calendarGrid = null;
    }

    /// <summary>
    /// The swipe, while the grid draws a grid presentation's calendar in the tree; none otherwise (a wheel or a time has no calendar
    /// to swipe), so that nothing is left listening.
    /// </summary>
    private void UpdateGridGesture(bool? inTree = null)
    {
        var wanted = UsesSingleGrid && IsGridStyle && (inTree ?? TopLevel.GetTopLevel(this) is not null);
        if (!wanted)
        {
            _gridGesture?.Destroy();
            _gridGesture = null;
            return;
        }

        if (_gridGesture is not null)
        {
            return;
        }

        _gridGesture = Gesture.Create(new GestureConfig
        {
            Element = _calendarGrid!,
            GestureName = "datetime-month-swipe",
            Direction = GestureDirection.X,
            CanStart = _ => IsEffectivelyEnabled && !_showMonthAndYear && IsGridStyle,
            OnEnd = OnGridSwipeEnd,
        });
        _gridGesture.Enable();
    }

    /// <summary>
    /// A swipe past <see cref="GridSwipeDistance"/>, or a flick, changes the month once, at its end: toward the next month when the
    /// finger goes toward the start of the line (left, or right in RTL), as a scrolled calendar's next month comes from its end.
    /// </summary>
    private void OnGridSwipeEnd(GestureDetail detail)
    {
        var dx = detail.DeltaX;
        var far = Math.Abs(dx) >= GridSwipeDistance;
        var flick = Math.Abs(dx) >= GridSwipeFlickDistance && Math.Abs(detail.VelocityX) >= GridSwipeVelocity && Math.Sign(detail.VelocityX) == Math.Sign(dx);
        if (!far && !flick)
        {
            return;
        }

        var towardStart = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft ? dx > 0 : dx < 0;
        if (towardStart)
        {
            NextMonth();
        }
        else
        {
            PrevMonth();
        }
    }

    /// <summary>
    /// A change of month in the grid, by a button, a swipe or <c>animateToDate</c>: to the forced month, or the one after (or
    /// before) the month shown or on its way. A month outside <see cref="Min"/> and <see cref="Max"/> isn't reached, as a scrolled
    /// calendar doesn't settle on it. Faded when the grid is shown and <see cref="AvonicConfig.Animated"/> is on; at once otherwise.
    /// </summary>
    private void ChangeGridMonth(bool next)
    {
        var from = _gridTarget ?? _working;
        var target = _forceRenderDate ?? (next ? DatetimeUtils.GetNextMonth(from) : DatetimeUtils.GetPreviousMonth(from));
        if (IsMonthOutOfRange(target))
        {
            // animateToDate's wait ends with no change, rather than hold every later change to a month it can't reach.
            _forceDateScrolling?.TrySetResult();
            return;
        }

        _gridKeyChange = false;
        StartGridChange(target);
    }

    /// <summary>
    /// A key to a day in another month, in the grid: faded there as a button's change is, with the day focused once the new month is
    /// drawn, so that its ring fades in with the days. At once, as the scrolled calendar's keys are, with
    /// <see cref="AvonicConfig.Animated"/> off or the calendar not shown. A key during the fade goes on from the day on its way.
    /// </summary>
    private bool ChangeGridMonthByKey(DatetimeParts to)
    {
        if (!AvonicConfig.Animated || !IsCalendarReady || (to.Month == _working.Month && to.Year == _working.Year))
        {
            return false;
        }

        _gridKeyChange = true;
        StartGridChange(_working with { Month = to.Month, Day = to.Day, Year = to.Year });
        return true;
    }

    /// <summary>The day a key's change is on its way to, for the next key to go on from: the focused day still shows the old month.</summary>
    private DatetimeParts? GridKeyTarget => _gridKeyChange ? _gridTarget : null;

    private void StartGridChange(DatetimeParts target)
    {
        _gridTarget = target;
        if (!AvonicConfig.Animated || !IsCalendarReady)
        {
            FinishGridChange();
            return;
        }

        if (_gridFade is not null && !_gridFadingIn)
        {
            // Fading out already: the new target is shown when it ends.
            return;
        }

        FadeGridOut();
    }

    /// <summary>The days fade out from what they show now, in a share of the time as long as the way left, then the month changes.</summary>
    private void FadeGridOut()
    {
        var grid = _calendarGrid!;
        var shown = AnimationGroupOpacity.Of(grid);
        StopGridFade();
        _gridFadingIn = false;
        var fade = AvonicAnimation.Create()
            .AddElement(grid)
            .Duration(GridFadeOutMs * shown)
            .Easing(GridFadeOutEasing)
            .FromTo("opacity", shown, 0.0);
        _gridFade = fade;
        fade.OnFinish((_, _) =>
        {
            if (_gridFade != fade)
            {
                return;
            }

            SwapGridMonth();
            FadeGridIn(fade);
        }, oneTimeCallback: true);
        _ = fade.Play();
    }

    private void FadeGridIn(AvonicAnimation fadeOut)
    {
        var grid = _calendarGrid!;
        _gridFadingIn = true;
        var fade = AvonicAnimation.Create()
            .AddElement(grid)
            .Duration(GridFadeInMs)
            .Easing(GridFadeInEasing)
            .FromTo("opacity", 0.0, 1.0);
        _gridFade = fade;
        fade.OnFinish((_, _) =>
        {
            if (_gridFade != fade)
            {
                return;
            }

            StopGridFade();
        }, oneTimeCallback: true);

        // The new animation's first value is shown at once, so the old one's can go without a frame at full opacity.
        _ = fade.Play();
        fadeOut.Destroy();
    }

    private void StopGridFade()
    {
        _gridFade?.Destroy();
        _gridFade = null;
        _gridFadingIn = false;
    }

    /// <summary>
    /// A change on its way dropped, its fade with it, for a state set by another way (a value, a key, a selection, the month/year
    /// wheel), which the change's month would otherwise overwrite when its fade ends. animateToDate's wait ends with it.
    /// </summary>
    internal void CancelGridChange()
    {
        if (_gridTarget is null && _gridFade is null)
        {
            return;
        }

        StopGridFade();
        _gridTarget = null;
        _gridKeyChange = false;
        _forceDateScrolling?.TrySetResult();
    }

    /// <summary>Any month on its way shown now, and no fade left: for a change with no animation, and as the grid goes.</summary>
    private void FinishGridChange()
    {
        StopGridFade();
        SwapGridMonth();
    }

    /// <summary>
    /// Ionic's <c>updateActiveMonth</c>, for the grid: the target month the working one, drawn, and <c>animateToDate</c>'s wait over.
    /// A focused day gives its focus to the grid, as a scrolled calendar's to its scroller: the day now shows another date. After a
    /// key's change, the key's day takes it, as the scrolled calendar's keys focus the working day.
    /// </summary>
    private void SwapGridMonth()
    {
        if (_gridTarget is not { } target)
        {
            return;
        }

        var dayHadFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is DatetimeCalendarDay focused
                          && focused.FindAncestorOfType<AvonicDatetime>() == this;
        var byKey = _gridKeyChange;
        _gridTarget = null;
        _gridKeyChange = false;
        _working = _working with { Month = target.Month, Day = target.Day, Year = target.Year };
        Render();
        _forceDateScrolling?.TrySetResult();
        if (dayHadFocus && byKey)
        {
            // The key's day, focused before the days fade in, so that its ring shows with them.
            FocusWorkingDay();
        }
        else if (dayHadFocus)
        {
            _calendarGrid?.Focus();
        }
    }

    /// <summary>Ionic's <c>datetime-ready</c>, for the grid: laid out and shown, so a fade can be seen.</summary>
    private bool IsGridReady => _calendarGrid is { Bounds.Width: > 0 } && IsEffectivelyVisible;

    private void OnCalendarRenderingChanged()
    {
        // A change on its way in either rendering lands now: the grid's fade, or the scroller's scroll and its rest, which a
        // hidden scroller would otherwise end on a month of its own; and animateToDate's wait with it.
        FinishGridChange();
        _smooth?.Stop();
        _scrollRest?.Dispose();
        _scrollRest = null;
        ResolveForcedWithoutScroll();
        UpdateGridGesture();
        Render();
        if (!UsesSingleGrid && _calendarBody is { Viewport.Width: > 0 } body)
        {
            // The scroller comes back with the working month in view.
            body.Offset = new Vector(body.Viewport.Width, 0);
        }
    }
}
