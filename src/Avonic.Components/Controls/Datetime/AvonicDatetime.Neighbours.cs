using Avalonia.Controls;
using Avalonia.Input;

namespace Avonic.Components.Controls;

/// <summary>
/// When the scrolled calendar makes the days of the months either side of the working one (#248, spike B). Ionic renders all three
/// months at once; what a user sees is the same in each, as a month is made before it can come into view.
/// </summary>
internal enum DatetimeNeighbourMonths
{
    /// <summary>With the working month, as Ionic's: 126 days at the first draw.</summary>
    AtOnce,

    /// <summary>When first needed: a press on the calendar, a scroll, the next or previous month, a value in another month.</summary>
    WhenNeeded,

    /// <summary>
    /// As <see cref="WhenNeeded"/>, and in any case a frame after the calendar is first laid out: the next month, then the previous
    /// a frame later.
    /// </summary>
    AfterFirstFrame,
}

/// <summary>
/// The scrolled calendar's months either side of the working one, made when first needed (#248, spike B): until then an empty page
/// of the calendar's width, so the scroller's extent and snap points are unchanged, and each is made before it can come into view: at
/// a press on the calendar, before the next or previous month's scroll (a button, a value in another month, an adjacent day), and at
/// any change of the scroller's offset toward one (a wheel, a key, automation), within the same layout pass, so before it's drawn.
/// </summary>
public partial class AvonicDatetime
{
    /// <summary>The <see cref="AppContext"/> data that picks <see cref="DatetimeNeighbourMonths"/> for the perf build's pages (spike only).</summary>
    internal const string NeighbourMonthsSwitch = "Avonic.Datetime.NeighbourMonths";

    /// <summary>Tests pick the variant here; otherwise <see cref="NeighbourMonthsSwitch"/>, then <see cref="DefaultNeighbourMonths"/>.</summary>
    internal static DatetimeNeighbourMonths? NeighbourMonthsOverride { get; set; }

    internal const DatetimeNeighbourMonths DefaultNeighbourMonths = DatetimeNeighbourMonths.AfterFirstFrame;

    private readonly DatetimeNeighbourMonths _neighbourMonths = NeighbourMonthsNow();

    private IDisposable? _neighbourFrame;

    private static DatetimeNeighbourMonths NeighbourMonthsNow() =>
        NeighbourMonthsOverride
        ?? (AppContext.GetData(NeighbourMonthsSwitch) is string text && Enum.TryParse<DatetimeNeighbourMonths>(text, out var mode)
            ? mode
            : DefaultNeighbourMonths);

    /// <summary>Makes the days of the month at <paramref name="index"/> (0 or 2) not made yet, at most <paramref name="count"/>, and draws them.</summary>
    private void EnsureNeighbourDays(int index, int count = DatetimeCalendarMonth.DayCount)
    {
        if (UsesSingleGrid || _calendarMonths is not { Months.Count: 3 } panel)
        {
            return;
        }

        var month = panel.Months[index];
        // A month not drawn yet (no Month) has nothing to draw; RenderCalendar makes and draws the months together.
        if (month.Month == 0 || !month.AddDays(count))
        {
            return;
        }

        HearDayClicks(month);
        RenderMonth(Culture, month, month.Month, month.Year);
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

    /// <summary>A press on the calendar may start a swipe either way: both months, before the finger moves.</summary>
    private void OnCalendarPressed(object? sender, PointerPressedEventArgs e)
    {
        EnsureNeighbourDays(2);
        EnsureNeighbourDays(0);
    }

    /// <summary>The days a frame makes, at most, in <see cref="DatetimeNeighbourMonths.AfterFirstFrame"/>: two weeks.</summary>
    internal const int NeighbourDaysPerFrame = 14;

    /// <summary>The frames <see cref="DatetimeNeighbourMonths.AfterFirstFrame"/> lets go by after the calendar's first layout: its first two draws.</summary>
    internal const int NeighbourFramesWaited = 2;

    /// <summary>The datetime making its months either side, a part a frame: one at a time, so a page of several takes turns.</summary>
    private static WeakReference<AvonicDatetime>? s_neighbourTurn;

    /// <summary>
    /// <see cref="DatetimeNeighbourMonths.AfterFirstFrame"/>: once the calendar has its size, after its first two frames, two weeks of
    /// days a frame, the next month's then the previous's, one datetime at a time, so no frame makes much. A press, a scroll or a
    /// change of month before they're done makes the rest at once.
    /// </summary>
    private void ScheduleNeighbourDays()
    {
        if (_neighbourMonths != DatetimeNeighbourMonths.AfterFirstFrame || _neighbourFrame is not null || UsesSingleGrid
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
            EnsureNeighbourDays(panel.Months[2].HasAllDays ? 0 : 2, NeighbourDaysPerFrame);
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

    /// <summary>The turn, unless another datetime still in a window has it and a frame on its way.</summary>
    private bool TakeNeighbourTurn()
    {
        if (s_neighbourTurn is { } turn && turn.TryGetTarget(out var owner) && owner != this && owner._neighbourFrame is not null
            && TopLevel.GetTopLevel(owner) is not null)
        {
            return false;
        }

        s_neighbourTurn = new WeakReference<AvonicDatetime>(this);
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
