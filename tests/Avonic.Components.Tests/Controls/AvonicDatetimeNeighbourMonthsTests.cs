using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicDatetimeCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// #248, spike B: the scrolled calendar makes the days of the months either side of the working one only when they're first needed,
/// or a frame after it's first drawn (<see cref="DatetimeNeighbourMonths"/>), where Ionic renders all three at once. What a user sees
/// must be Ionic's: each month is made before it can come into view, and the scroller keeps its three pages, offset and snap points
/// meanwhile. The scrolled calendar's own behaviour is <see cref="AvonicDatetimeTests"/>, unchanged.
/// </summary>
public class AvonicDatetimeNeighbourMonthsTests
{
    private const string June15 = "2023-06-15T13:47:00";

    private static void With(DatetimeNeighbourMonths mode, Action<Scene, TestClock> test) => Run((scene, clock) =>
    {
        AvonicDatetime.NeighbourMonthsOverride = mode;
        try
        {
            test(scene, clock);
        }
        finally
        {
            AvonicDatetime.NeighbourMonthsOverride = null;
        }
    });

    /// <summary>The datetime shown and laid out once, as at its first frame, the clock not moved.</summary>
    private static AvonicDatetime Open(Scene scene, params AvonicDatetime[] datetimes)
    {
        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.AddRange(datetimes);
        scene.Window.Content = new ScrollViewer { Content = stack };
        Dispatcher.UIThread.RunJobs();
        scene.Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        return datetimes[0];
    }

    private static AvonicDatetime Plain(string? value = June15) => new() { Presentation = DatetimePresentation.Date, Value = value };

    private static int[] Days(AvonicDatetime datetime) => [.. datetime.CalendarMonths!.Months.Select(m => m.Days.Count)];

    private static DatetimeCalendarDay Day(AvonicDatetime datetime, int index, int day) =>
        datetime.CalendarMonths!.Months[index].Days.First(d => d.IsVisible && !d.IsPadding && !d.IsAdjacent && d.Parts.Day == day);

    private static string MonthShown(AvonicDatetime datetime) => Part<TextBlock>(datetime, "PART_MonthYearText").Text!;

    private static void Click(Scene scene, Visual target)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        AvonicPickerColumnTests.Click(scene, Box(target, scene.Window).Center);
    }

    // ── When they're made ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void By_Default_The_Months_Either_Side_Come_After_Two_Frames_Two_Weeks_A_Frame_The_Next_First() => With(AvonicDatetime.DefaultNeighbourMonths, (scene, clock) =>
    {
        Assert.Equal(DatetimeNeighbourMonths.AfterFirstFrame, AvonicDatetime.DefaultNeighbourMonths);
        var datetime = Open(scene, Plain());
        Assert.Equal([0, 42, 0], Days(datetime));

        int[][] frames = [[0, 42, 0], [0, 42, 0], [0, 42, 14], [0, 42, 28], [0, 42, 42], [14, 42, 42], [28, 42, 42], [42, 42, 42], [42, 42, 42]];
        foreach (var expected in frames)
        {
            Settle(clock, 16);
            Assert.Equal(expected, Days(datetime));
        }

        // Once made, nothing more is asked for.
        Assert.Equal(0, clock.Pending);
    });

    [AvaloniaFact]
    public void At_Once_As_Ionic_Every_Month_Has_Its_Days_At_The_First_Layout() => With(DatetimeNeighbourMonths.AtOnce, (scene, _) =>
    {
        var datetime = Open(scene, Plain());
        Assert.Equal([42, 42, 42], Days(datetime));
    });

    [AvaloniaFact]
    public void When_Needed_Only_The_Working_Month_Has_Days_At_Rest() => With(DatetimeNeighbourMonths.WhenNeeded, (scene, clock) =>
    {
        var datetime = Open(scene, Plain());
        Settle(clock, 400);
        Assert.Equal([0, 42, 0], Days(datetime));
        Assert.Equal("June 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void Two_Datetimes_Take_Turns_A_Part_A_Frame() => With(DatetimeNeighbourMonths.AfterFirstFrame, (scene, clock) =>
    {
        var a = Plain();
        var b = Plain();
        Open(scene, a, b);
        Settle(clock, 48);
        // One of them has its first part; which, is the order their frames come in.
        var (first, second) = Days(a)[2] > 0 ? (a, b) : (b, a);
        Assert.Equal([0, 42, 14], Days(first));
        Assert.Equal([0, 42, 0], Days(second));
        Settle(clock, 80);
        Assert.Equal([42, 42, 42], Days(first));
        Assert.Equal([0, 42, 0], Days(second));
        Settle(clock, 16);
        Assert.Equal([0, 42, 14], Days(second));
        Settle(clock, 80);
        Assert.Equal([42, 42, 42], Days(second));
    });

    [AvaloniaFact]
    public void A_Datetime_That_Leaves_First_Asks_For_No_More_Frames_And_Gives_Up_Its_Turn() => With(DatetimeNeighbourMonths.AfterFirstFrame, (scene, clock) =>
    {
        var gone = Open(scene, Plain());
        Settle(clock, 48);
        Assert.Equal([0, 42, 14], Days(gone));
        scene.Window.Content = null;
        Settle(clock, 200);
        Assert.Equal([0, 42, 14], Days(gone));
        Assert.Equal(0, clock.Pending);

        var next = Open(scene, Plain());
        Settle(clock, 48);
        Assert.Equal([0, 42, 14], Days(next));
    });

    // ── The scroller meanwhile ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Meanwhile_The_Scroller_Keeps_Three_Pages_The_Working_Month_In_View_And_Its_Snap_Points() => With(DatetimeNeighbourMonths.WhenNeeded, (scene, clock) =>
    {
        var datetime = Open(scene, Plain());
        Settle(clock, 400);
        var body = datetime.CalendarBody!;
        var width = body.Viewport.Width;
        Assert.Equal(3 * width, body.Extent.Width, 1);
        Assert.Equal(width, body.Offset.X, 1);
        Assert.Equal([0d, width, 2 * width],
            datetime.CalendarMonths!.GetIrregularSnapPoints(Avalonia.Layout.Orientation.Horizontal, SnapPointsAlignment.Near));
        // As tall as the working month: six rows of 42 and md's 4 above and below, the empty pages too.
        Assert.All(datetime.CalendarMonths.Months, m => Assert.Equal(6 * 42 + 8, m.Bounds.Height, 1));

        // A month outside min and max isn't snapped to, made or not.
        var bounded = Open(scene, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, Min = "2023-06-10", Max = "2023-07-20" });
        Settle(clock, 400);
        Assert.Equal([0, 42, 0], Days(bounded));
        Assert.Equal([1d * bounded.CalendarMonths!.PageWidth, 2 * bounded.CalendarMonths.PageWidth],
            bounded.CalendarMonths.GetIrregularSnapPoints(Avalonia.Layout.Orientation.Horizontal, SnapPointsAlignment.Near));
    });

    // ── Made before they're seen ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A_Press_On_The_Calendar_Makes_Both_Before_The_Finger_Moves() => With(DatetimeNeighbourMonths.WhenNeeded, (scene, clock) =>
    {
        var datetime = Open(scene, Plain());
        Settle(clock, 400);
        var at = Box(Day(datetime, 1, 21), scene.Window).Center;
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Assert.Equal([42, 42, 42], Days(datetime));
        Assert.Equal((5, 31), (Day(datetime, 0, 31).Parts.Month!.Value, Day(datetime, 0, 31).Parts.Day!.Value));
        Assert.Equal(7, Day(datetime, 2, 1).Parts.Month);
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
    });

    [AvaloniaFact]
    public void Next_And_Previous_Make_Their_Month_At_The_Click_Before_The_Scroll() => With(DatetimeNeighbourMonths.WhenNeeded, (scene, clock) =>
    {
        var datetime = Open(scene, Plain());
        Settle(clock, 400);
        var width = datetime.CalendarBody!.Viewport.Width;
        Click(scene, Part<AvonicButton>(datetime, "PART_NextButton"));
        Assert.Equal(width, datetime.CalendarBody.Offset.X, 1);
        Assert.Equal([0, 42, 42], Days(datetime));
        Settle(clock, 600);
        Assert.Equal("July 2023", MonthShown(datetime));

        Click(scene, Part<AvonicButton>(datetime, "PART_PreviousButton"));
        Assert.Equal(width, datetime.CalendarBody.Offset.X, 1);
        Assert.Equal([42, 42, 42], Days(datetime));
        Assert.Equal(6, Day(datetime, 0, 15).Parts.Month);
        Settle(clock, 600);
        Assert.Equal("June 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void An_Offset_Toward_A_Month_Makes_It_Before_The_Next_Layout() => With(DatetimeNeighbourMonths.WhenNeeded, (scene, clock) =>
    {
        // A wheel, a key on the scroller, automation's ScrollPattern: whatever moves the offset.
        var datetime = Open(scene, Plain());
        Settle(clock, 400);
        var body = datetime.CalendarBody!;
        var width = body.Viewport.Width;
        body.Offset = new Vector(width * 1.5, 0);
        Assert.Equal([0, 42, 42], Days(datetime));
        scene.Window.UpdateLayout();
        Assert.True(Day(datetime, 2, 1).Bounds.Width > 0);

        body.Offset = new Vector(width * 0.5, 0);
        Assert.Equal([42, 42, 42], Days(datetime));
    });

    [AvaloniaFact]
    public void A_Value_In_Another_Month_Draws_That_Month_With_Its_Days_Before_The_Scroll() => With(DatetimeNeighbourMonths.WhenNeeded, (scene, clock) =>
    {
        var datetime = Open(scene, Plain());
        Settle(clock, 400);
        datetime.Value = "2023-09-03";
        Assert.Equal(9, datetime.CalendarMonths!.Months[2].Month);
        Assert.True(Day(datetime, 2, 3).IsActive);
        Assert.Equal(9, Day(datetime, 2, 3).Parts.Month);
        Settle(clock, 800);
        Assert.Equal("September 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void Todays_Ring_Shows_In_A_Month_Made_Later() => With(DatetimeNeighbourMonths.WhenNeeded, (scene, clock) =>
    {
        // Today is pinned to 2023-06-20; May shown, so June is the next month.
        var datetime = Open(scene, Plain("2023-05-15T13:47:00"));
        Settle(clock, 400);
        Click(scene, Part<AvonicButton>(datetime, "PART_NextButton"));
        Assert.True(Day(datetime, 2, 20).IsToday);
        Assert.False(Day(datetime, 2, 19).IsToday);
    });

    [AvaloniaFact]
    public void A_Day_Of_A_Month_Made_Later_Is_Chosen_By_A_Click() => With(DatetimeNeighbourMonths.WhenNeeded, (scene, clock) =>
    {
        // Scrolled to July and clicked before the scroll rests, as a finger can.
        var datetime = Open(scene, Plain());
        Settle(clock, 400);
        var body = datetime.CalendarBody!;
        body.Offset = new Vector(2 * body.Viewport.Width, 0);
        scene.Window.UpdateLayout();
        Click(scene, Day(datetime, 2, 10));
        Assert.Equal("2023-07-10T13:47:00", datetime.Value.ToString());
    });

    [AvaloniaFact]
    public void Keys_Change_The_Month_At_Once_As_Ionics_Do_With_No_Month_Either_Side_Made() => With(DatetimeNeighbourMonths.WhenNeeded, (scene, clock) =>
    {
        var datetime = Open(scene, Plain());
        Settle(clock, 400);
        datetime.CalendarBody!.Focus(NavigationMethod.Tab);
        Settle(clock, 50);
        scene.Window.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.None);
        Settle(clock, 50);
        var focused = (DatetimeCalendarDay)TopLevel.GetTopLevel(datetime)!.FocusManager!.GetFocusedElement()!;
        Assert.Equal((7, 15), (focused.Parts.Month!.Value, focused.Parts.Day!.Value));
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Equal([0, 42, 0], Days(datetime));
    });

    [AvaloniaFact]
    public void The_Single_Grid_Makes_No_Month_Either_Side() => With(DatetimeNeighbourMonths.AfterFirstFrame, (scene, clock) =>
    {
        var datetime = Open(scene, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, CalendarRendering = DatetimeCalendarRendering.SingleGrid });
        Settle(clock, 400);
        Assert.Empty(datetime.CalendarMonths!.Months);
        Assert.Equal(0, clock.Pending);
    });
}
