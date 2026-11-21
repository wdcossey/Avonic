using Avalonia.Controls;
using Avalonia.Input;

namespace Avonic.Components.Controls;

/// <summary>
/// The scrolled calendar's months either side of the working one, made when first needed (#248, spike B): until then an empty page
/// of the calendar's width, so the scroller's extent and snap points are unchanged, and each is made before it can come into view: at
/// a press on the calendar, before the next or previous month's scroll (a button, a value in another month, an adjacent day), and at
/// any change of the scroller's offset toward one (a wheel, a key, automation), within the same layout pass, so before it's drawn;
/// and in any case after the calendar's first two frames, two weeks of days a frame. Measured against Ionic's three months at once and
/// against making them only when needed on the Pixel 8: docs/infra/performance/README.md, "The months either side".
/// </summary>
public partial class AvonicDatetime
{
    /// <summary>
    /// Whether the months either side are made after the calendar's first frames as well as when needed: always, but tests turn it
    /// off to see the triggers alone.
    /// </summary>
    internal static bool NeighbourMonthsAfterFirstFrames { get; set; } = true;

    /// <summary><see cref="NeighbourMonthsAfterFirstFrames"/> as it was when this datetime was made, so a test's change reaches no other.</summary>
    private readonly bool _neighbourMonthsAfterFirstFrames = NeighbourMonthsAfterFirstFrames;

    private IDisposable? _neighbourFrame;

    /// <summary>
    /// Makes the days of the month at <paramref name="index"/> (0 or 2) not made yet, at most <paramref name="count"/>, and draws them,
    /// those alone. True when it made any.
    /// </summary>
    private bool EnsureNeighbourDays(int index, int count = DatetimeCalendarMonth.DayCount)
    {
        if (UsesSingleGrid || _calendarMonths is not { Months.Count: 3 } panel)
        {
            return false;
        }

        var month = panel.Months[index];
        var made = month.Days.Count;
        // A month not drawn yet (no Month) has nothing to draw; RenderCalendar makes and draws the months together.
        if (month.Month == 0 || !month.AddDays(count))
        {
            return false;
        }

        HearDayClicks(month);
        RenderMonth(Culture, month, month.Month, month.Year, fromDay: made);
        return true;
    }

    /// <summary>The month either side that an offset of <paramref name="x"/> brings into view, made now if it isn't.</summary>
    private void EnsureNeighbourDaysToward(double x)
    {
        if (_calendarBody is not { Viewport.Width: > 0 } body || _calendarMonths is null || _calendarMonths.HasNeighbourDays)
        {
            return;
        }

        var middle = body.Viewport.Width;
        if (x < middle - 0.5)
        {
            EnsureNeighbourDays(0);
        }
        else if (x > middle + 0.5)
        {
            EnsureNeighbourDays(2);
        }
    }

    /// <summary>
    /// A touch or a pen on the calendar may start a swipe either way: both months, before it moves. Not a mouse, which doesn't drag
    /// the scroller (Avalonia's scroll gesture is a touch's and a pen's), so a click on a day pays nothing for months it won't see.
    /// </summary>
    private void OnCalendarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse)
        {
            return;
        }

        EnsureNeighbourDays(2);
        EnsureNeighbourDays(0);
    }

    /// <summary>The days a frame makes, at most, after the first frames: two weeks.</summary>
    internal const int NeighbourDaysPerFrame = 14;

    /// <summary>The frames let go by after the calendar's first layout before the first part: its first two draws.</summary>
    internal const int NeighbourFramesWaited = 2;

    /// <summary>The datetime making its months either side, a part a frame: one at a time, so a page of several takes turns.</summary>
    private static WeakReference<AvonicDatetime>? s_neighbourTurn;

    /// <summary>When the turn's datetime last made a part, on <see cref="AvonicConfig.Now"/>.</summary>
    private static TimeSpan s_neighbourTurnAt;

    /// <summary>A turn whose datetime has made nothing for this long is given up: its window may get no frames (minimised, paused).</summary>
    internal static readonly TimeSpan NeighbourTurnStale = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Once the calendar has its size, after its first two frames, two weeks of
    /// days a frame, the next month's then the previous's, one datetime at a time, so no frame makes much. A press, a scroll or a
    /// change of month before they're done makes the rest at once.
    /// </summary>
    private void ScheduleNeighbourDays()
    {
        if (!_neighbourMonthsAfterFirstFrames || _neighbourFrame is not null || UsesSingleGrid
            || _calendarMonths is not { Months.Count: 3, HasNeighbourDays: false } panel)
        {
            return;
        }

        _neighbourFrame = AvonicConfig.RequestFrame(this, () => NeighbourPart(panel, NeighbourFramesWaited));
    }

    private void NeighbourPart(DatetimeCalendarBodyPanel panel, int wait)
    {
        _neighbourFrame = null;
        if (_calendarMonths != panel || UsesSingleGrid || panel.HasNeighbourDays || TopLevel.GetTopLevel(this) is null)
        {
            ReleaseNeighbourTurn();
            return;
        }

        if (wait <= 0 && TakeNeighbourTurn())
        {
            var made = EnsureNeighbourDays(panel.Months[2].HasAllDays ? 0 : 2, NeighbourDaysPerFrame);
            s_neighbourTurnAt = AvonicConfig.Now();
            if (!made)
            {
                // Nothing to make (never expected): stop, rather than ask for frames for ever holding the turn.
                ReleaseNeighbourTurn();
                return;
            }

            if (panel.HasNeighbourDays)
            {
                // The turn is kept to the end of this frame, so another datetime's first part comes in the next.
                _neighbourFrame = AvonicConfig.RequestFrame(this, () =>
                {
                    _neighbourFrame = null;
                    ReleaseNeighbourTurn();
                });
                return;
            }
        }

        _neighbourFrame = AvonicConfig.RequestFrame(this, () => NeighbourPart(panel, wait - 1));
    }

    /// <summary>The turn, unless another datetime still in a window has it, with a frame on its way and a part made lately.</summary>
    private bool TakeNeighbourTurn()
    {
        if (s_neighbourTurn is { } turn && turn.TryGetTarget(out var owner) && owner != this && owner._neighbourFrame is not null
            && TopLevel.GetTopLevel(owner) is not null && AvonicConfig.Now() - s_neighbourTurnAt < NeighbourTurnStale)
        {
            return false;
        }

        if (s_neighbourTurn is null || !s_neighbourTurn.TryGetTarget(out var current) || current != this)
        {
            s_neighbourTurn = new WeakReference<AvonicDatetime>(this);
            s_neighbourTurnAt = AvonicConfig.Now();
        }

        return true;
    }

    private void ReleaseNeighbourTurn()
    {
        if (s_neighbourTurn is { } turn && turn.TryGetTarget(out var owner) && owner == this)
        {
            s_neighbourTurn = null;
        }
    }

    private void CancelNeighbourDays()
    {
        _neighbourFrame?.Dispose();
        _neighbourFrame = null;
        ReleaseNeighbourTurn();
    }
}
