using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;
using static Avonic.Components.Tests.Controls.AvonicDatetimeCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Not Ionic's: <see cref="DatetimeCalendarRendering.SingleGrid"/> (#248, spike A), one 7x6 grid drawn again in place. What it
/// shows is checked against the scrolled calendar's working month, day by day, so that the two renderings agree; then what only
/// it does: the fade, its swipe, and the change with <see cref="AvonicConfig.Animated"/> off. The scrolled calendar's own tests are
/// <see cref="AvonicDatetimeTests"/>, unchanged.
/// </summary>
public class AvonicDatetimeSingleGridTests
{
    private const string June15 = "2023-06-15T13:47:00";

    private static AvonicDatetime Grid(string? value = June15, Action<AvonicDatetime>? set = null)
    {
        var datetime = new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = value, CalendarRendering = DatetimeCalendarRendering.SingleGrid };
        set?.Invoke(datetime);
        return datetime;
    }

    private static string MonthShown(AvonicDatetime datetime) => Part<TextBlock>(datetime, "PART_MonthYearText").Text!;

    private static DatetimeCalendarDay Day(AvonicDatetime datetime, int day) =>
        datetime.CalendarGrid!.Days.First(d => d.IsVisible && !d.IsPadding && !d.IsAdjacent && d.Parts.Day == day);

    private static string[] ActiveDays(AvonicDatetime datetime) =>
        [.. datetime.CalendarGrid!.Days.Where(d => d.IsVisible && d.IsActive && !d.IsAdjacent).Select(d => $"{d.Parts.Month}-{d.Parts.Day}")];

    private static double Shown(AvonicDatetime datetime) => AnimationGroupOpacity.Of(datetime.CalendarGrid!);

    private static void Click(Scene scene, Visual target)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        AvonicPickerColumnTests.Click(scene, Box(target, scene.Window).Center);
    }

    private static AvonicButton Next(AvonicDatetime datetime) => Part<AvonicButton>(datetime, "PART_NextButton");

    private static AvonicButton Previous(AvonicDatetime datetime) => Part<AvonicButton>(datetime, "PART_PreviousButton");

    /// <summary>
    /// A finger from the grid's centre by <paramref name="dx"/>, in steps of 5, then lifted: with no time between, so no flick, or
    /// <paramref name="msPerStep"/> of the test clock between each.
    /// </summary>
    private static void Swipe(Scene scene, AvonicDatetime datetime, double dx, Visual? from = null, TestClock? clock = null, double msPerStep = 0)
    {
        GestureTests.Render();
        var at = Box(from ?? datetime.CalendarGrid!, scene.Window).Center;
        var finger = scene.Window.TouchBegin(at, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        var steps = (int)Math.Abs(dx / 5);
        for (var i = 1; i <= steps; i++)
        {
            if (clock is not null)
            {
                clock.At(clock.Now.TotalMilliseconds + msPerStep);
            }

            scene.Window.TouchMove(finger, at + new Point(Math.Sign(dx) * 5 * i, 0), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        scene.Window.TouchEnd(finger, at + new Point(dx, 0), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Everything a day shows and does, as text, to compare the two renderings.</summary>
    private static string[] Cells(DatetimeCalendarMonth month) =>
    [
        .. month.Days.Where(d => d.IsVisible).Select(d =>
            $"{d.Parts.Year}-{d.Parts.Month}-{d.Parts.Day}/{d.Parts.DayOfWeek} '{d.Content}' [{AutomationProperties.GetName(d)}] {AutomationProperties.GetAccessibilityView(d)}"
            + $" pad={d.IsPadding} adj={d.IsAdjacent} act={d.IsActive} today={d.IsToday} dis={d.IsDayDisabled} en={d.IsEnabled}"
            + $" hit={d.IsHitTestVisible} hl={d.Highlight?.TextColor}/{d.Highlight?.BackgroundColor}/{d.Highlight?.Border}"
            + $" classes={string.Join(' ', d.Classes.Where(c => c.StartsWith(':')).Order())}"),
    ];

    // ── What the grid shows ──────────────────────────────────────────────────────────────────

    public static TheoryData<string, string, int, bool, bool> Months => new()
    {
        // value, locale, firstDayOfWeek, showAdjacentDays, constraints (min, max, isDateEnabled, highlightedDates)
        { June15, "en-US", 0, false, false },
        { "2024-02-29", "fr-FR", 1, false, false },
        { "2023-12-01", "ar-EG", 6, false, true },
        { "2023-01-31", "ja-JP", 0, true, false },
        { "2023-09-10", "de-DE", 1, true, true },
        { "2021-05-01", "en-US", 3, false, true },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Months))]
    public void The_Grid_Shows_The_Same_Days_As_The_Scrolled_Calendar(string value, string locale, int firstDay, bool adjacent, bool constrained) => Run((scene, clock) =>
    {
        void Set(AvonicDatetime d)
        {
            d.Presentation = DatetimePresentation.Date;
            d.Value = value;
            d.Locale = locale;
            d.FirstDayOfWeek = firstDay;
            d.ShowAdjacentDays = adjacent;
            if (constrained)
            {
                var v = DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                d.Min = v.AddDays(-20).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                d.Max = v.AddDays(20).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                d.IsDateEnabled = iso => DateTime.Parse(iso, System.Globalization.CultureInfo.InvariantCulture).DayOfWeek != DayOfWeek.Wednesday;
                d.HighlightedDates = new Func<string, DatetimeHighlightStyle?>(iso => iso.EndsWith("-06", StringComparison.Ordinal) ? new DatetimeHighlightStyle { TextColor = "blue", Border = "1px solid red" } : null);
            }
        }

        var scrolled = new AvonicDatetime();
        Set(scrolled);
        Show(scene, clock, scrolled);
        var expected = Cells(scrolled.CalendarMonths!.Months[1]);
        var heading = MonthShown(scrolled);
        var names = Part<Avalonia.Controls.Primitives.UniformGrid>(scrolled, "PART_CalendarDaysOfWeek").Children.OfType<TextBlock>().Select(t => t.Text).ToArray();

        var grid = Grid(set: Set);
        Show(scene, clock, grid);
        Assert.Equal(expected, Cells(grid.CalendarGrid!));
        Assert.Equal(heading, MonthShown(grid));
        Assert.Equal(names, Part<Avalonia.Controls.Primitives.UniformGrid>(grid, "PART_CalendarDaysOfWeek").Children.OfType<TextBlock>().Select(t => t.Text));
        Assert.Equal(AccessibilityView.Default, AutomationProperties.GetAccessibilityView(grid.CalendarGrid!));
    });

    [AvaloniaFact]
    public void Each_Rendering_Makes_Only_Its_Own_Days() => Run((scene, clock) =>
    {
        var scrolled = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        Assert.Equal(126, scrolled.GetVisualDescendants().OfType<DatetimeCalendarDay>().Count());
        Assert.Empty(scrolled.CalendarGrid!.Days);
        Assert.False(scrolled.CalendarGrid.IsVisible);

        var grid = Show(scene, clock, Grid());
        Assert.Equal(42, grid.GetVisualDescendants().OfType<DatetimeCalendarDay>().Count());
        Assert.Empty(grid.CalendarMonths!.Months);
        Assert.False(grid.CalendarBody!.IsEffectivelyVisible);
        Assert.True(grid.CalendarGrid!.IsEffectivelyVisible);
        // As tall as the scrolled calendar: six rows of 42 and md's 4 above and below.
        Assert.Equal(6 * 42 + 8, grid.CalendarGrid.Bounds.Height, 1);
    });

    // ── A change of month ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Next_Fades_The_Days_Out_Then_In_On_The_Test_Clock() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid());
        Click(scene, Next(datetime));
        Assert.Equal(1, Shown(datetime), 3);
        Settle(clock, 48);
        // Fading out, md's accelerate: past half the time, less than half the way.
        Assert.InRange(Shown(datetime), 0.4, 0.95);
        Assert.Equal("June 2023", MonthShown(datetime));
        // 90 ms from the frame after the click, ended at the first frame past it.
        Settle(clock, 80);
        // Out, swapped and on its way in.
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Equal(7, datetime.CalendarGrid!.Month);
        Assert.InRange(Shown(datetime), 0, 0.6);
        Settle(clock, 240);
        Assert.Equal(1, Shown(datetime), 3);
        Assert.Null(datetime.CalendarGrid.OpacityMask);
        Assert.Null(datetime.GridTarget);
        Click(scene, Previous(datetime));
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void With_Animated_Off_The_Month_Changes_At_Once() => Run((scene, clock) =>
    {
        var previous = AvonicConfig.Animated;
        AvonicConfig.Animated = false;
        try
        {
            var datetime = Show(scene, clock, Grid());
            Click(scene, Next(datetime));
            Assert.Equal("July 2023", MonthShown(datetime));
            Assert.Equal(1, Shown(datetime), 3);
            Assert.Null(datetime.CalendarGrid!.OpacityMask);
            Settle(clock, 30);
            Assert.Equal(1, Shown(datetime), 3);
            Click(scene, Previous(datetime));
            Assert.Equal("June 2023", MonthShown(datetime));
        }
        finally
        {
            AvonicConfig.Animated = previous;
        }
    });

    [AvaloniaFact]
    public void Clicks_During_A_Fade_Add_Up() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid());
        Click(scene, Next(datetime));
        Settle(clock, 30);
        Click(scene, Next(datetime));
        Settle(clock, 400);
        Assert.Equal("August 2023", MonthShown(datetime));
        Assert.Equal(1, Shown(datetime), 3);
    });

    [AvaloniaFact]
    public void A_Value_In_Another_Month_Fades_There_With_No_IonChange() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid());
        var changes = 0;
        datetime.IonChange += (_, _) => changes++;
        datetime.Value = "2023-09-03";
        Settle(clock, 50);
        Assert.True(Shown(datetime) < 1);
        Settle(clock, 600);
        Assert.Equal("September 2023", MonthShown(datetime));
        Assert.Equal(["9-3"], ActiveDays(datetime));
        Assert.Equal(0, changes);
        // animateToDate's wait is over: the next month is the one after September.
        Click(scene, Next(datetime));
        Settle(clock, 400);
        Assert.Equal("October 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void An_Adjacent_Day_Selects_And_Shows_Its_Month() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid(set: d => d.ShowAdjacentDays = true));
        Click(scene, datetime.CalendarGrid!.Days.First(d => d.IsVisible && d.IsAdjacent && d.Parts.Day == 3));
        Settle(clock, 600);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Equal(["7-3"], ActiveDays(datetime));
        Assert.Equal("2023-07-03T13:47:00", datetime.Value.ToString());
    });

    // ── The swipe ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A_Swipe_Past_The_Threshold_Changes_The_Month_Once() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid());
        Swipe(scene, datetime, -80);
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
        Swipe(scene, datetime, 160);
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void A_Short_Slow_Swipe_Changes_Nothing_And_A_Swipe_From_A_Day_Selects_Nothing() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid());
        var changes = 0;
        datetime.IonChange += (_, _) => changes++;
        Swipe(scene, datetime, -30);
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));
        Swipe(scene, datetime, -80, from: Day(datetime, 21));
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Equal(0, changes);
        Assert.Equal(June15, datetime.Value.ToString());
    });

    [AvaloniaFact]
    public void A_Short_Fast_Flick_Changes_The_Month_Its_Way() => Run((scene, clock) =>
    {
        // 30 px in 6 steps of 8 ms: about 0.6 px/ms, past 20 px and 0.3 px/ms, short of 50 px.
        var datetime = Show(scene, clock, Grid());
        Swipe(scene, datetime, -30, clock: clock, msPerStep: 8);
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
        Swipe(scene, datetime, 30, clock: clock, msPerStep: 8);
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));

        // Slower, 30 px in 6 steps of 40 ms: no flick.
        Swipe(scene, datetime, -30, clock: clock, msPerStep: 40);
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void In_Rtl_A_Flick_To_The_Right_Is_The_Next_Month() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid(), FlowDirection.RightToLeft);
        Swipe(scene, datetime, 30, clock: clock, msPerStep: 8);
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void A_State_Set_During_A_Fade_Wins_Over_The_Month_On_Its_Way() => Run((scene, clock) =>
    {
        // A value in the month shown, set while the fade heads to July: June stays, with the value's day.
        var datetime = Show(scene, clock, Grid());
        Click(scene, Next(datetime));
        Settle(clock, 40);
        datetime.Value = "2023-06-03";
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));
        Assert.Equal(["6-3"], ActiveDays(datetime));
        Assert.Equal(1, Shown(datetime), 3);

        // The month/year wheel opened during a fade: the month stays where it was.
        Click(scene, Next(datetime));
        Settle(clock, 40);
        Click(scene, Part<Button>(datetime, "PART_MonthYearButton"));
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));
        Assert.Null(datetime.GridTarget);
    });

    [AvaloniaFact]
    public void A_Wheel_Presentation_Has_No_Swipe() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid(set: d => d.PreferWheel = true));
        Assert.False(datetime.HasGridSwipe);
        datetime.PreferWheel = false;
        Settle(clock, 100);
        Assert.True(datetime.HasGridSwipe);
    });

    [AvaloniaFact]
    public void In_Rtl_A_Swipe_To_The_Right_Is_The_Next_Month() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid(), FlowDirection.RightToLeft);
        Swipe(scene, datetime, 80);
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
        Swipe(scene, datetime, -80);
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void A_Disabled_Datetime_Doesnt_Swipe() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid(set: d => d.IsEnabled = false));
        Swipe(scene, datetime, -80);
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));
    });

    // ── Min and max ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A_Month_Outside_Min_And_Max_Is_Not_Reached() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid(set: d => { d.Min = "2023-05-10"; d.Max = "2023-07-10"; }));
        Click(scene, Next(datetime));
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.False(Next(datetime).IsEnabled);
        Swipe(scene, datetime, -80);
        Settle(clock, 400);
        datetime.NextMonth();
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Equal(1, Shown(datetime), 3);

        Swipe(scene, datetime, 80);
        Settle(clock, 400);
        Swipe(scene, datetime, 80);
        Settle(clock, 400);
        Swipe(scene, datetime, 80);
        Settle(clock, 400);
        Assert.Equal("May 2023", MonthShown(datetime));
        Assert.False(Previous(datetime).IsEnabled);
        Assert.False(Day(datetime, 9).IsEnabled);
        Assert.True(Day(datetime, 10).IsEnabled);
    });

    // ── Selection, focus and the keyboard ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A_Selection_Survives_Changes_Of_Month() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid());
        Click(scene, Day(datetime, 20));
        Assert.Equal("2023-06-20T13:47:00", datetime.Value.ToString());
        Click(scene, Next(datetime));
        Settle(clock, 400);
        Assert.Empty(ActiveDays(datetime));
        Click(scene, Previous(datetime));
        Settle(clock, 400);
        Assert.Equal(["6-20"], ActiveDays(datetime));
        Assert.Equal("2023-06-20T13:47:00", datetime.Value.ToString());
    });

    [AvaloniaFact]
    public void Tab_Reaches_The_Working_Day_And_The_Keys_Cross_Month_Edges() => Run((scene, clock) =>
    {
        var before = new Button { Content = "before" };
        var datetime = Grid(value: "2023-06-30");
        scene.Window.Content = new StackPanel { Margin = new Thickness(16), Children = { before, datetime } };
        Settle(clock, 400);
        Assert.All(datetime.CalendarGrid!.Days, d => Assert.False(d.IsTabStop));
        Assert.True(datetime.CalendarGrid.IsTabStop);

        // The grid takes Tab, and hands focus to the working day.
        datetime.CalendarGrid.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        var focused = scene.Window.FocusManager!.GetFocusedElement();
        Assert.Same(Day(datetime, 30), focused);

        // Right from June 30: July 1, faded to as a button's change is (the keyboard's own tests are below).
        AvonicPickerColumnTests.Key(scene, PhysicalKey.ArrowRight);
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Same(Day(datetime, 1), scene.Window.FocusManager.GetFocusedElement());
        Assert.Equal(1, Shown(datetime), 3);

        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageDown);
        Settle(clock, 400);
        Assert.Equal("August 2023", MonthShown(datetime));
        Assert.Same(Day(datetime, 1), scene.Window.FocusManager.GetFocusedElement());

        // A swipe from the focused day: the grid keeps the focus, as the scroller does.
        Swipe(scene, datetime, -80, from: Day(datetime, 1));
        Settle(clock, 400);
        Assert.Equal("September 2023", MonthShown(datetime));
        Assert.Same(datetime.CalendarGrid, scene.Window.FocusManager.GetFocusedElement());
    });

    // ── The keyboard's fade ──────────────────────────────────────────────────────────────────

    /// <summary>The grid shown, with the day of <paramref name="value"/> focused by the keyboard, as Tab leaves it.</summary>
    private static AvonicDatetime FocusedGrid(Scene scene, TestClock clock, string value)
    {
        var datetime = Show(scene, clock, Grid(value));
        datetime.CalendarGrid!.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(Day(datetime, DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture).Day), Focused(scene));
        return datetime;
    }

    private static object? Focused(Scene scene) => scene.Window.FocusManager!.GetFocusedElement();

    /// <summary>The focused day's ring: <c>:focus</c>, and its part shown, inside a grid shown in full.</summary>
    private static void AssertRingShown(AvonicDatetime datetime, DatetimeCalendarDay day)
    {
        Assert.Contains(":focus", day.Classes);
        Assert.Equal(1, day.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Focus").Opacity, 3);
        Assert.Equal(1, Shown(datetime), 3);
        Assert.Null(datetime.CalendarGrid!.OpacityMask);
    }

    [AvaloniaFact]
    public void A_Key_To_Another_Month_Fades_There_And_Focuses_Its_Day() => Run((scene, clock) =>
    {
        var datetime = FocusedGrid(scene, clock, "2023-06-30");
        AvonicPickerColumnTests.Key(scene, PhysicalKey.ArrowRight);
        Settle(clock, 48);
        // Fading out: June still shows, its day still focused, as a button's change shows June until the swap.
        Assert.InRange(Shown(datetime), 0.4, 0.95);
        Assert.Equal("June 2023", MonthShown(datetime));
        Assert.Same(Day(datetime, 30), Focused(scene));
        Settle(clock, 80);
        // Swapped: July drawn, the key's day focused before the days fade in, so its ring fades in with them.
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.InRange(Shown(datetime), 0, 0.6);
        Assert.Same(Day(datetime, 1), Focused(scene));
        Settle(clock, 300);
        AssertRingShown(datetime, Day(datetime, 1));
        Assert.Null(datetime.GridTarget);

        // PageUp, and Shift+PageDown, a year: each fades, and ends on its day.
        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageUp);
        Settle(clock, 400);
        Assert.Equal("June 2023", MonthShown(datetime));
        AssertRingShown(datetime, Day(datetime, 1));
        scene.Window.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.Shift);
        scene.Window.KeyReleaseQwerty(PhysicalKey.PageDown, RawInputModifiers.Shift);
        Dispatcher.UIThread.RunJobs();
        Settle(clock, 48);
        Assert.Equal("June 2023", MonthShown(datetime));
        Settle(clock, 400);
        Assert.Equal("June 2024", MonthShown(datetime));
        Assert.Same(Day(datetime, 1), Focused(scene));
        AssertRingShown(datetime, Day(datetime, 1));
    });

    [AvaloniaFact]
    public void Keys_During_A_Key_Fade_Go_On_From_The_Day_On_Its_Way() => Run((scene, clock) =>
    {
        // PageDown from June 30 to July 30, then Right during the fade: July 31, not July 1 from the June 30 still focused.
        var datetime = FocusedGrid(scene, clock, "2023-06-30");
        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageDown);
        Settle(clock, 30);
        AvonicPickerColumnTests.Key(scene, PhysicalKey.ArrowRight);
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Same(Day(datetime, 31), Focused(scene));
        AssertRingShown(datetime, Day(datetime, 31));

        // PageDown then PageUp during the fade: July stays, the change dropped, the day focused once July has faded back in.
        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageDown);
        Settle(clock, 30);
        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageUp);
        Settle(clock, 300);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Null(datetime.GridTarget);
        Assert.Same(Day(datetime, 31), Focused(scene));
        AssertRingShown(datetime, Day(datetime, 31));
    });

    [AvaloniaFact]
    public void Home_And_End_During_A_Key_Fade_Go_On_From_The_Day_On_Its_Way() => Run((scene, clock) =>
    {
        // PageDown from June 30 to July 30, a Sunday, then End during the fade: Saturday August 5, the end of July 30's week.
        var datetime = FocusedGrid(scene, clock, "2023-06-30");
        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageDown);
        Settle(clock, 30);
        AvonicPickerColumnTests.Key(scene, PhysicalKey.End);
        Settle(clock, 400);
        Assert.Equal("August 2023", MonthShown(datetime));
        AssertRingShown(datetime, Day(datetime, 5));
        Assert.Same(Day(datetime, 5), Focused(scene));

        // PageUp to July 5, a Wednesday, then Home during the fade: Sunday July 2.
        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageUp);
        Settle(clock, 30);
        AvonicPickerColumnTests.Key(scene, PhysicalKey.Home);
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Same(Day(datetime, 2), Focused(scene));
    });

    [AvaloniaTheory]
    [InlineData(PhysicalKey.Enter)]
    [InlineData(PhysicalKey.Space)]
    public void Enter_Or_Space_During_A_Key_Fade_Chooses_The_Keys_Day(PhysicalKey key) => Run((scene, clock) =>
    {
        // Right from June 30, then the key while June still fades out with the focus: July 1 is chosen, not June 30.
        var datetime = FocusedGrid(scene, clock, "2023-06-30");
        var changes = new List<string?>();
        datetime.IonChange += (_, e) => changes.Add(e.Value?.ToString());
        AvonicPickerColumnTests.Key(scene, PhysicalKey.ArrowRight);
        Settle(clock, 30);
        AvonicPickerColumnTests.Key(scene, key);
        Assert.Equal("July 2023", MonthShown(datetime));
        Settle(clock, 400);
        Assert.Equal(["2023-07-01"], changes);
        Assert.Equal(["7-1"], ActiveDays(datetime));
        Assert.Same(Day(datetime, 1), Focused(scene));
        Assert.Equal(1, Shown(datetime), 3);
        // No day is left pressed: the one that was fading out never had the key.
        Assert.DoesNotContain(datetime.CalendarGrid!.Days, d => d.IsPressed);
    });

    [AvaloniaFact]
    public void A_Change_Dropped_During_The_Fade_Out_Fades_Back_In_And_A_Key_During_The_Fade_In_Lets_It_End() => Run((scene, clock) =>
    {
        // PageDown, then PageUp back to June while June fades out: June fades back in from where it was, rather than jump.
        var datetime = FocusedGrid(scene, clock, "2023-06-30");
        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageDown);
        Settle(clock, 64);
        var faded = Shown(datetime);
        Assert.InRange(faded, 0.1, 0.9);
        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageUp);
        Settle(clock, 32);
        Assert.Null(datetime.GridTarget);
        Assert.Equal("June 2023", MonthShown(datetime));
        Assert.InRange(Shown(datetime), faded, 0.99);
        Settle(clock, 300);
        AssertRingShown(datetime, Day(datetime, 30));

        // PageDown to July, then Left within July during its fade in: the focus moves at once, and the fade goes on to its end.
        AvonicPickerColumnTests.Key(scene, PhysicalKey.PageDown);
        Settle(clock, 160);
        Assert.Equal("July 2023", MonthShown(datetime));
        var fadingIn = Shown(datetime);
        Assert.InRange(fadingIn, 0.01, 0.9);
        AvonicPickerColumnTests.Key(scene, PhysicalKey.ArrowLeft);
        Settle(clock, 20);
        Assert.Same(Day(datetime, 29), Focused(scene));
        Assert.InRange(Shown(datetime), fadingIn, 0.99);
        Settle(clock, 300);
        AssertRingShown(datetime, Day(datetime, 29));
    });

    [AvaloniaFact]
    public void A_Switch_To_The_Scrolled_Calendar_During_A_Key_Fade_Focuses_The_Keys_Day_There() => Run((scene, clock) =>
    {
        var datetime = FocusedGrid(scene, clock, "2023-06-30");
        AvonicPickerColumnTests.Key(scene, PhysicalKey.ArrowRight);
        Settle(clock, 30);
        datetime.CalendarRendering = DatetimeCalendarRendering.ScrolledMonths;
        Settle(clock, 100);
        Assert.Equal("July 2023", MonthShown(datetime));
        var day = Assert.IsType<DatetimeCalendarDay>(Focused(scene));
        Assert.Equal((7, 1), (day.Parts.Month!.Value, day.Parts.Day!.Value));
        Assert.Same(datetime.CalendarMonths!.Months[1], day.FindAncestorOfType<DatetimeCalendarMonth>());
    });

    [AvaloniaFact]
    public void A_Key_Within_The_Month_Moves_At_Once() => Run((scene, clock) =>
    {
        var datetime = FocusedGrid(scene, clock, "2023-06-30");
        AvonicPickerColumnTests.Key(scene, PhysicalKey.ArrowLeft);
        Assert.Null(datetime.GridTarget);
        Settle(clock, 20);
        Assert.Same(Day(datetime, 29), Focused(scene));
        AssertRingShown(datetime, Day(datetime, 29));
    });

    [AvaloniaFact]
    public void With_Animated_Off_A_Key_Changes_The_Month_At_Once() => Run((scene, clock) =>
    {
        var previous = AvonicConfig.Animated;
        AvonicConfig.Animated = false;
        try
        {
            var datetime = FocusedGrid(scene, clock, "2023-06-30");
            AvonicPickerColumnTests.Key(scene, PhysicalKey.ArrowRight);
            Assert.Equal("July 2023", MonthShown(datetime));
            Assert.Null(datetime.GridTarget);
            Settle(clock, 20);
            Assert.Same(Day(datetime, 1), Focused(scene));
            AssertRingShown(datetime, Day(datetime, 1));
        }
        finally
        {
            AvonicConfig.Animated = previous;
        }
    });

    [AvaloniaFact]
    public void The_Scrolled_Calendar_Keeps_Ionics_Keys_At_Once() => Run((scene, clock) =>
    {
        // Ionic's: setWorkingParts draws the month at once, and the day is focused on the next frame. No fade.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = "2023-06-30" });
        datetime.CalendarBody!.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        AvonicPickerColumnTests.Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal("July 2023", MonthShown(datetime));
        Settle(clock, 20);
        var day = (DatetimeCalendarDay)Focused(scene)!;
        Assert.Equal((7, 1), (day.Parts.Month!.Value, day.Parts.Day!.Value));
        Assert.Same(datetime.CalendarMonths!.Months[1], day.FindAncestorOfType<DatetimeCalendarMonth>());
        Assert.Null(datetime.GridTarget);
        Assert.Equal(1, AnimationGroupOpacity.Of(datetime.CalendarBody), 3);
    });

    // ── The option ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void The_Default_Comes_From_The_Config_At_Style_Priority() => Run((scene, clock) =>
    {
        Assert.Equal(DatetimeCalendarRendering.ScrolledMonths, AvonicConfig.DatetimeCalendarRendering);
        Assert.Equal(DatetimeCalendarRendering.ScrolledMonths, new AvonicDatetime().CalendarRendering);
        AvonicConfig.DatetimeCalendarRendering = DatetimeCalendarRendering.SingleGrid;
        try
        {
            var datetime = new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 };
            Assert.Equal(DatetimeCalendarRendering.SingleGrid, datetime.CalendarRendering);
            Show(scene, clock, datetime);
            Assert.True(datetime.CalendarGrid!.IsEffectivelyVisible);
            Assert.Empty(datetime.CalendarMonths!.Months);

            // An app's style wins.
            var styled = new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 };
            styled.Styles.Add(new Avalonia.Styling.Style(x => x.OfType<AvonicDatetime>())
            {
                Setters = { new Avalonia.Styling.Setter(AvonicDatetime.CalendarRenderingProperty, DatetimeCalendarRendering.ScrolledMonths) },
            });
            Show(scene, clock, styled);
            Assert.Equal(DatetimeCalendarRendering.ScrolledMonths, styled.CalendarRendering);
            Assert.True(styled.CalendarBody!.IsEffectivelyVisible);
        }
        finally
        {
            AvonicConfig.DatetimeCalendarRendering = DatetimeCalendarRendering.ScrolledMonths;
        }
    });

    [AvaloniaFact]
    public void The_Rendering_Can_Change_On_A_Shown_Datetime() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        datetime.CalendarRendering = DatetimeCalendarRendering.SingleGrid;
        Settle(clock, 100);
        Assert.True(datetime.CalendarGrid!.IsEffectivelyVisible);
        Assert.Equal(["6-15"], ActiveDays(datetime));
        Click(scene, Next(datetime));
        Settle(clock, 400);
        Assert.Equal("July 2023", MonthShown(datetime));

        datetime.CalendarRendering = DatetimeCalendarRendering.ScrolledMonths;
        Settle(clock, 100);
        Assert.True(datetime.CalendarBody!.IsEffectivelyVisible);
        Assert.Equal(7, datetime.CalendarMonths!.Months[1].Month);
        Assert.Equal(datetime.CalendarBody.Viewport.Width, datetime.CalendarBody.Offset.X, 1);
    });

    [AvaloniaFact]
    public void Leaving_The_Tree_Mid_Fade_Ends_At_The_Month_And_Leaves_Nothing_Running() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, Grid());
        Click(scene, Next(datetime));
        Settle(clock, 30);
        scene.Window.Content = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Null(datetime.GridTarget);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Equal(1, Shown(datetime), 3);
    });

    // The previous and next buttons, and the default buttons, used to take their colour from a brush made in the template's own
    // Resources with a DynamicResource colour: the theme held each such brush, and through it the datetime, for as long as the
    // theme lived (#258's Pixel run). Each rendering, with the footer's buttons too.
    [AvaloniaTheory]
    [InlineData(DatetimeCalendarRendering.ScrolledMonths, false)]
    [InlineData(DatetimeCalendarRendering.SingleGrid, false)]
    [InlineData(DatetimeCalendarRendering.ScrolledMonths, true)]
    public void A_Datetime_That_Drew_Its_Calendar_Is_Collected_Once_Left(DatetimeCalendarRendering rendering, bool buttons) => Run((scene, clock) =>
    {
        var weak = ShowAndLeave(scene, clock, rendering, buttons);
        Settle(clock, 1000);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        for (var i = 0; i < 3 && weak.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(weak.IsAlive);
    });

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference ShowAndLeave(Scene scene, TestClock clock, DatetimeCalendarRendering rendering, bool buttons)
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = June15, CalendarRendering = rendering, ShowDefaultButtons = buttons });
        Assert.True(Next(datetime).IsEffectivelyVisible);
        scene.Window.Content = null;
        return new WeakReference(datetime);
    }

    [AvaloniaFact]
    public void Capture_Single_Grid() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = June15, CalendarRendering = DatetimeCalendarRendering.SingleGrid });
        Save(scene, "avonic-ion-datetime-single-grid.png");
        Assert.Equal(42, datetime.CalendarGrid!.Days.Count);
    });
}
