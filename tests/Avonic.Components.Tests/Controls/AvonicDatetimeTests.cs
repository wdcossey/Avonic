using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using static Avonic.Components.Tests.Controls.AvonicDatetimeCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-datetime</c>, md, on the test clock, with today pinned to 2023-06-20 10:00 and the locale en-US: B1 to B34 in
/// docs/components/ion-datetime/PLAN.md, from <c>datetime.js</c> and its <c>utils/</c>, and the measurements in <c>ref/</c>
/// (<c>behaviour.json</c>'s cases are named in each test).
/// </summary>
public class AvonicDatetimeTests
{
    private const string June15 = "2023-06-15T13:47:00";

    private sealed class Log
    {
        public List<string> Events { get; } = [];

        public static Log Of(AvonicDatetime datetime, bool focus = false)
        {
            var log = new Log();
            datetime.IonChange += (_, e) => log.Events.Add($"ionChange:{e.Value?.ToString() ?? "undefined"}");
            datetime.IonCancel += (_, _) => log.Events.Add("ionCancel");
            if (focus)
            {
                datetime.IonFocus += (_, _) => log.Events.Add("ionFocus");
                datetime.IonBlur += (_, _) => log.Events.Add("ionBlur");
            }
            return log;
        }
    }

    private static DatetimeCalendarDay Day(AvonicDatetime datetime, int day, int month = 1) =>
        datetime.CalendarMonths!.Months[month].Days.First(d => d.IsVisible && !d.IsPadding && !d.IsAdjacent && d.Parts.Day == day);

    private static void Click(Scene scene, Visual target)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var box = Box(target, scene.Window);
        AvonicPickerColumnTests.Click(scene, box.Center);
    }

    private static string MonthShown(AvonicDatetime datetime) => Part<TextBlock>(datetime, "PART_MonthYearText").Text!;

    private static string[] ActiveDays(AvonicDatetime datetime) =>
        [.. datetime.CalendarMonths!.Months[1].Days.Where(d => d.IsVisible && d.IsActive && !d.IsAdjacent).Select(d => $"{d.Parts.Month}-{d.Parts.Day}")];

    // ── B2, B3, B4: a value in code ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_A_Value_Shows_Its_Month_Selects_Its_Day_And_Raises_No_IonChange() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        var log = Log.Of(datetime);
        Assert.Equal("June 2023", MonthShown(datetime));
        Assert.Equal(["6-15"], ActiveDays(datetime));

        // valueProgrammatic: September, scrolled to, with no ionChange.
        datetime.Value = "2023-09-03";
        Settle(clock, 800);
        Assert.Equal("September 2023", MonthShown(datetime));
        Assert.Equal(["9-3"], ActiveDays(datetime));
        Assert.Empty(log.Events);
    });

    [AvaloniaFact]
    public void B2_A_Value_In_Another_Month_Scrolls_There_Smoothly() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        var body = datetime.CalendarBody!;
        var width = body.Viewport.Width;
        datetime.Value = "2023-09-03";
        // The target month is drawn as the next one, and the scroll is on its way to it.
        Assert.Equal(9, datetime.CalendarMonths!.Months[2].Month);
        Settle(clock, 100);
        Assert.InRange(body.Offset.X, width + 1, 2 * width - 1);
        Settle(clock, 600);
        Assert.Equal(width, body.Offset.X, 1);
        Assert.Equal(9, datetime.CalendarMonths.Months[1].Month);
    });

    [AvaloniaFact]
    public void B3_A_Value_With_A_Zone_Keeps_Its_Wall_Clock() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimeMode(), Value = "2023-06-15T13:47:00+05:30" });
        Assert.Equal("1:47 PM", Part<TextBlock>(datetime, "PART_TimeText").Text);
        Click(scene, Day(datetime, 20));
        Assert.Equal("2023-06-20T13:47:00", datetime.Value.ToString());
    });

    private static DatetimePresentation DatetimeMode() => DatetimePresentation.DateTime;

    [AvaloniaFact]
    public void B2_An_Unparsable_Value_Changes_Nothing() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        datetime.Value = "not a date";
        Settle(clock, 100);
        Assert.Equal(["6-15"], ActiveDays(datetime));
    });

    // ── B4, B5: selecting ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_A_Click_On_A_Day_Confirms_At_Once_Keeping_The_Time_And_Again_On_The_Same_Day() => Run((scene, clock) =>
    {
        // clickDay: 2023-06-20T13:47:00, then the same again.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        var log = Log.Of(datetime);
        Click(scene, Day(datetime, 20));
        Click(scene, Day(datetime, 20));
        Assert.Equal("2023-06-20T13:47:00", datetime.Value.ToString());
        Assert.Equal(["ionChange:2023-06-20T13:47:00", "ionChange:2023-06-20T13:47:00"], log.Events);
        Assert.Equal(["6-20"], ActiveDays(datetime));
    });

    [AvaloniaFact]
    public void B4_With_No_Value_A_Day_Takes_Todays_Time() => Run((scene, clock) =>
    {
        // clickDayNoValue: 2023-06-22T10:00:00.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date });
        Assert.Empty(ActiveDays(datetime));
        Assert.Equal("June 2023", MonthShown(datetime));
        Click(scene, Day(datetime, 22));
        Assert.Equal("2023-06-22T10:00:00", datetime.Value.ToString());
    });

    [AvaloniaFact]
    public void B5_Readonly_Selects_Nothing_But_Browses() => Run((scene, clock) =>
    {
        // readonly: the click changes nothing; next still moves the month.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, Readonly = true });
        var log = Log.Of(datetime);
        Click(scene, Day(datetime, 21));
        Assert.Equal(June15, datetime.Value.ToString());
        Click(scene, Part<AvonicButton>(datetime, "PART_NextButton"));
        Settle(clock, 600);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.DoesNotContain(log.Events, e => e.StartsWith("ionChange", StringComparison.Ordinal));
    });

    [AvaloniaFact]
    public void B28_Readonly_Browses_The_Month_Year_Wheel_But_Selects_Nothing_There() => Run((scene, clock) =>
    {
        // The month/year wheel takes the pointer when read-only (pointer-events: initial); setActiveParts returns before a selection.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, Readonly = true });
        var log = Log.Of(datetime);
        Click(scene, Part<Button>(datetime, "PART_MonthYearButton"));
        Settle(clock, 400);
        var months = datetime.MonthYearWheel!.Items.OfType<AvonicPickerColumn>().First(c => c.Classes.Contains("month-column"));
        Click(scene, months.Items.OfType<AvonicPickerColumnOption>().First(o => Equals(o.Value, 8)));
        Settle(clock, 400);
        Assert.Equal("August 2023", MonthShown(datetime));
        Assert.Equal(June15, datetime.Value.ToString());
        Assert.Empty(log.Events);
    });

    // ── B6, B7, B8, B10: confirm, cancel, reset, clear ───────────────────────────────────────

    [AvaloniaFact]
    public void B6_With_The_Default_Buttons_A_Selection_Waits_For_Done() => Run((scene, clock) =>
    {
        // buttonsConfirm.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, ShowDefaultButtons = true });
        var log = Log.Of(datetime);
        Click(scene, Day(datetime, 20));
        Assert.Equal(June15, datetime.Value.ToString());
        Assert.Equal(["6-20"], ActiveDays(datetime));
        Assert.Empty(log.Events);
        datetime.Confirm();
        datetime.Confirm();
        Assert.Equal(["ionChange:2023-06-20T13:47:00", "ionChange:2023-06-20T13:47:00"], log.Events);

        Click(scene, Day(datetime, 25));
        Click(scene, Part<AvonicButton>(datetime, "PART_CancelButton"));
        Assert.Equal("ionCancel", log.Events[^1]);
        Assert.Equal(["6-20"], ActiveDays(datetime));

        Click(scene, Day(datetime, 26));
        Click(scene, Part<AvonicButton>(datetime, "PART_ConfirmButton"));
        Assert.Equal("2023-06-26T13:47:00", datetime.Value.ToString());
        Assert.Equal("ionChange:2023-06-26T13:47:00", log.Events[^1]);
    });

    [AvaloniaFact]
    public void B6_The_Buttons_Slot_Also_Waits_For_Confirm() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime
        {
            Presentation = DatetimePresentation.Date, Value = June15, ButtonsContent = new AvonicButtons { Items = { new AvonicButton { Content = "Mine" } } },
        });
        var log = Log.Of(datetime);
        Click(scene, Day(datetime, 20));
        Assert.Empty(log.Events);
        Assert.False(Part<AvonicButtons>(datetime, "PART_DefaultButtons").IsVisible);
        Assert.True(Part<ContentPresenter>(datetime, "PART_ButtonsSlot").IsEffectivelyVisible);
    });

    [AvaloniaFact]
    public void B6_Confirm_With_Nothing_Chosen_Writes_No_Value_In_The_Grid_And_The_Working_Parts_With_A_Wheel() => Run((scene, clock) =>
    {
        // confirmNoSelection: ionChange with no value; confirmNoSelectionWheel: 2023-06-20T10:00:00.
        var grid = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, ShowDefaultButtons = true });
        var gridLog = Log.Of(grid);
        grid.Confirm();
        Assert.Null(grid.Value);
        Assert.Equal(["ionChange:undefined"], gridLog.Events);

        var wheel = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, PreferWheel = true, ShowDefaultButtons = true });
        wheel.Confirm();
        Assert.Equal("2023-06-20T10:00:00", wheel.Value.ToString());
    });

    [AvaloniaFact]
    public void B34_Done_With_Nothing_Chosen_On_A_Time_Wheel_Writes_What_It_Shows() => Run((scene, clock) =>
    {
        // ADR 0005: Ionic writes no value here (confirmNoSelectionTime), though the wheel always shows; Avonic writes the time shown.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Time, ShowDefaultButtons = true });
        datetime.Confirm();
        Assert.Equal("2023-06-20T10:00:00", datetime.Value.ToString());
    });

    [AvaloniaFact]
    public void B7_Cancel_Discards_The_Selection_And_Goes_Back_To_The_Value() => Run((scene, clock) =>
    {
        // cancelRevert: July 3 chosen, cancel → June again, the 15th.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, ShowDefaultButtons = true });
        var log = Log.Of(datetime);
        Click(scene, Part<AvonicButton>(datetime, "PART_NextButton"));
        Settle(clock, 600);
        Click(scene, Day(datetime, 3));
        Assert.Equal(["7-3"], ActiveDays(datetime));
        datetime.Cancel();
        Settle(clock, 800);
        Assert.Equal(["ionCancel"], log.Events);
        Assert.Equal("June 2023", MonthShown(datetime));
        Assert.Equal(["6-15"], ActiveDays(datetime));
    });

    [AvaloniaFact]
    public void B8_Reset_Moves_The_State_Not_The_Value() => Run((scene, clock) =>
    {
        // reset: February with the 11th; then nothing selected, today's month.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        datetime.Reset("2023-02-11");
        Settle(clock, 800);
        Assert.Equal("February 2023", MonthShown(datetime));
        Assert.Equal(["2-11"], ActiveDays(datetime));
        Assert.Equal(June15, datetime.Value.ToString());
        datetime.Reset();
        Settle(clock, 800);
        Assert.Equal("June 2023", MonthShown(datetime));
        Assert.Empty(ActiveDays(datetime));
    });

    [AvaloniaFact]
    public void B10_Clear_Clears_The_Value_With_IonChange() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, ShowClearButton = true });
        var log = Log.Of(datetime);
        Click(scene, Part<AvonicButton>(datetime, "PART_ClearButton"));
        Assert.Null(datetime.Value);
        Assert.Equal(["ionChange:undefined"], log.Events);
        Assert.Empty(ActiveDays(datetime));
    });

    // ── B9, B6, B7: the parent overlay ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_B7_Confirm_And_Cancel_Close_The_Parent_Popover_With_Their_Roles() => Run((scene, clock) =>
    {
        var datetime = new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 };
        var popover = new AvonicPopover { Content = datetime, KeepContentsMounted = true };
        scene.Window.Content = new StackPanel { Children = { popover } };
        var roles = new List<string?>();
        popover.IonPopoverDidDismiss += (_, e) => roles.Add(e.Role);
        _ = popover.Present(scene.Window);
        Settle(clock, 600);
        datetime.Confirm(closeOverlay: true);
        Settle(clock, 600);
        _ = popover.Present(scene.Window);
        Settle(clock, 600);
        datetime.Cancel(closeOverlay: true);
        Settle(clock, 600);
        Assert.Equal([AvonicDatetime.ConfirmRole, AvonicDatetime.CancelRole], roles);
    });

    [AvaloniaFact]
    public void B9_The_Parent_Overlays_Present_Discards_An_Unconfirmed_Selection() => Run((scene, clock) =>
    {
        // unconfirmedInPopover: 22 chosen and dismissed is still shown; presented again, the 15th.
        var datetime = new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, ShowDefaultButtons = true };
        var popover = new AvonicPopover { Content = datetime, KeepContentsMounted = true };
        scene.Window.Content = new StackPanel { Children = { popover } };
        _ = popover.Present(scene.Window);
        Settle(clock, 600);
        Click(scene, Day(datetime, 22));
        _ = popover.Dismiss();
        Settle(clock, 600);
        Assert.Equal(["6-22"], ActiveDays(datetime));
        _ = popover.Present(scene.Window);
        Settle(clock, 600);
        Assert.Equal(["6-15"], ActiveDays(datetime));
        Assert.Equal(June15, datetime.Value.ToString());
    });

    // ── B11 to B19: the grid ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Each_Month_Is_A_Grid_Of_Seven_From_The_First_Day_Of_Week() => Run((scene, clock) =>
    {
        // firstDay: Monday first, June 1 2023 is a Thursday, so three blanks.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, FirstDayOfWeek = 1 });
        var days = datetime.CalendarMonths!.Months[1].Days.Where(d => d.IsVisible).ToList();
        Assert.Equal(3, days.TakeWhile(d => d.IsPadding).Count());
        var names = Part<UniformGrid>(datetime, "PART_CalendarDaysOfWeek").Children.OfType<TextBlock>().Select(t => t.Text);
        Assert.Equal(["M", "T", "W", "T", "F", "S", "S"], names);
    });

    [AvaloniaFact]
    public void B11_Adjacent_Days_Fill_Six_Weeks() => Run((scene, clock) =>
    {
        // adjacent: 28 to 31 May, June, 1 to 8 July.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, ShowAdjacentDays = true });
        var days = datetime.CalendarMonths!.Months[1].Days.Where(d => d.IsVisible).ToList();
        Assert.Equal(42, days.Count);
        Assert.Equal("28a,29a,30a,31a,1", string.Join(",", days.Take(5).Select(d => d.Parts.Day + (d.IsAdjacent ? "a" : ""))));
        Assert.Equal("8a", days[^1].Parts.Day + "a");
    });

    [AvaloniaFact]
    public void B33_An_Adjacent_Day_Waits_For_Done_And_Shows_Its_Month() => Run((scene, clock) =>
    {
        // ADR 0005: Ionic confirms at once here (adjacent: ionChange with 2023-07-03T13:47:00 before Done).
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, ShowAdjacentDays = true, ShowDefaultButtons = true });
        var log = Log.Of(datetime);
        var july3 = datetime.CalendarMonths!.Months[1].Days.First(d => d.IsVisible && d.IsAdjacent && d.Parts.Day == 3);
        Click(scene, july3);
        Settle(clock, 800);
        Assert.Empty(log.Events);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Equal(["7-3"], ActiveDays(datetime));
        datetime.Confirm();
        Assert.Equal("2023-07-03T13:47:00", datetime.Value.ToString());
    });

    [AvaloniaFact]
    public void B12_A_Swipe_Resting_On_The_Next_Month_Moves_There_50ms_Later() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        var body = datetime.CalendarBody!;
        var width = body.Viewport.Width;
        body.Offset = new Vector(2 * width, 0);
        Settle(clock, 40);
        Assert.Equal("June 2023", MonthShown(datetime));
        Settle(clock, 30);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Equal(width, body.Offset.X, 1);

        // Not lined up: nothing.
        body.Offset = new Vector(width * 1.5, 0);
        Settle(clock, 100);
        Assert.Equal("July 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void B12_B13_A_Month_Outside_Min_And_Max_Is_Neither_Reached_Nor_Snapped_To() => Run((scene, clock) =>
    {
        // minMax: previous is disabled in the min month, next at the max month; days outside disabled.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, Min = "2023-06-10", Max = "2023-07-20" });
        Assert.False(Part<AvonicButton>(datetime, "PART_PreviousButton").IsEnabled);
        Assert.True(Part<AvonicButton>(datetime, "PART_NextButton").IsEnabled);
        Assert.True(Day(datetime, 9).IsDayDisabled);
        Assert.False(Day(datetime, 9).IsEnabled);
        Assert.False(Day(datetime, 10).IsDayDisabled);
        Assert.Equal([1d * datetime.CalendarMonths!.PageWidth, 2 * datetime.CalendarMonths.PageWidth],
            datetime.CalendarMonths.GetIrregularSnapPoints(Avalonia.Layout.Orientation.Horizontal, SnapPointsAlignment.Near));

        var body = datetime.CalendarBody!;
        body.Offset = new Vector(0, 0);
        Settle(clock, 100);
        Assert.Equal("June 2023", MonthShown(datetime));

        Click(scene, Part<AvonicButton>(datetime, "PART_NextButton"));
        Settle(clock, 600);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.False(Part<AvonicButton>(datetime, "PART_NextButton").IsEnabled);
        Assert.False(Day(datetime, 20).IsDayDisabled);
        Assert.True(Day(datetime, 21).IsDayDisabled);
    });

    [AvaloniaFact]
    public void B13_Next_Scrolls_Smoothly_And_At_Once_When_Not_Animated() => Run((scene, clock) =>
    {
        // nextPrev: about 300 ms for 350 px.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        var body = datetime.CalendarBody!;
        var width = body.Viewport.Width;
        Click(scene, Part<AvonicButton>(datetime, "PART_NextButton"));
        Settle(clock, 150);
        Assert.InRange(body.Offset.X, width + 50, 2 * width - 50);
        Settle(clock, 600);
        Assert.Equal("July 2023", MonthShown(datetime));

        AvonicConfig.Animated = false;
        try
        {
            Click(scene, Part<AvonicButton>(datetime, "PART_PreviousButton"));
            Settle(clock, 16);
            Assert.True(body.Offset.X < 1 || MonthShown(datetime) == "June 2023");
            Settle(clock, 100);
            Assert.Equal("June 2023", MonthShown(datetime));
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    });

    [AvaloniaFact]
    public void B15_Today_Selected_And_Disabled_Days_And_isDateEnabled() => Run((scene, clock) =>
    {
        // isDateEnabled: weekends off, given "2023-05-01" style strings, for the three months.
        var asked = new List<string>();
        var datetime = new AvonicDatetime
        {
            Presentation = DatetimePresentation.Date, Value = June15,
            IsDateEnabled = iso => { asked.Add(iso); return DateTime.Parse(iso, System.Globalization.CultureInfo.InvariantCulture).DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday); },
        };
        Show(scene, clock, datetime);
        Assert.True(Day(datetime, 20).IsToday);
        Assert.True(Day(datetime, 15).IsActive);
        Assert.True(Day(datetime, 17).IsDayDisabled);
        Assert.True(Day(datetime, 18).IsDayDisabled);
        Assert.False(Day(datetime, 19).IsDayDisabled);
        Assert.Contains("2023-05-01", asked);
        Assert.Contains("2023-07-31", asked);
        Assert.DoesNotContain(asked, iso => iso.Contains('T'));
        Assert.Equal("Today, Tuesday, June 20", AutomationProperties.GetName(Day(datetime, 20)));
        Assert.Equal("Thursday, June 15", AutomationProperties.GetName(Day(datetime, 15)));
    });

    [AvaloniaFact]
    public void B15_The_Value_Lists_Disable_Days_Months_And_Years() => Run((scene, clock) =>
    {
        // monthValues: June and August, days 1, 15 and 30, years 2023 and 2024.
        var datetime = Show(scene, clock, new AvonicDatetime
        {
            Presentation = DatetimePresentation.Date, Value = June15, MonthValues = "6,8", DayValues = "1,15,30", YearValues = "2023,2024",
        });
        Assert.False(Day(datetime, 15).IsDayDisabled);
        Assert.True(Day(datetime, 16).IsDayDisabled);
        Assert.True(Day(datetime, 1, month: 2).IsDayDisabled); // July isn't in monthValues.
    });

    [AvaloniaFact]
    public void B16_Multiple_Adds_And_Removes_Dates_And_The_Header_Counts_Them() => Run((scene, clock) =>
    {
        // multiple: ["2023-06-05"], "Mon, Jun 5"; then 2 days, 3 days, and 9 removed; the formatter's text.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Multiple = true, ShowDefaultTitle = true });
        var header = Part<TextBlock>(datetime, "PART_DatetimeSelectedDate");
        Click(scene, Day(datetime, 5));
        Assert.Equal("2023-06-05", datetime.Value.ToString());
        Assert.True(datetime.Value!.Value.IsList);
        Assert.Equal("Mon, Jun 5", header.Text);
        Click(scene, Day(datetime, 9));
        Assert.Equal("2 days", header.Text);
        Click(scene, Day(datetime, 12));
        Assert.Equal("3 days", header.Text);
        Click(scene, Day(datetime, 9));
        Assert.Equal("2023-06-05,2023-06-12", datetime.Value.ToString());
        datetime.TitleSelectedDatesFormatter = dates => "F:" + string.Join(";", dates);
        Assert.Equal("F:2023-06-05;2023-06-12", header.Text);
    });

    [AvaloniaFact]
    public void B2_A_List_Value_Shows_Its_Last_Dates_Month() => Run((scene, clock) =>
    {
        // multipleValue: June and July → July.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Multiple = true });
        datetime.Value = new[] { "2023-06-02", "2023-07-04" };
        Settle(clock, 800);
        Assert.Equal("July 2023", MonthShown(datetime));
    });

    [AvaloniaFact]
    public void B17_Highlighted_Dates_Colour_A_Day_But_Not_The_Selected_One() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime
        {
            Presentation = DatetimePresentation.Date, Value = June15,
            HighlightedDates = new DatetimeHighlight[]
            {
                new() { Date = "2023-06-05", TextColor = "#800080", BackgroundColor = "#ffc0cb" },
                new() { Date = "2023-06-15", TextColor = "red", BackgroundColor = "yellow" },
                new() { Date = "2023-06-20", Border = "1px solid green" },
            },
        });
        Assert.Equal(Color.Parse("#800080"), ((ISolidColorBrush)Day(datetime, 5).Foreground!).Color);
        Assert.Equal(Color.Parse("#ffc0cb"), ((ISolidColorBrush)Day(datetime, 5).Background!).Color);
        Assert.Null(Day(datetime, 15).Highlight);
        Assert.Equal(new Thickness(1), Day(datetime, 20).BorderThickness);
        Assert.Equal(Colors.Green, ((ISolidColorBrush)Day(datetime, 20).BorderBrush!).Color);

        datetime.HighlightedDates = new Func<string, DatetimeHighlightStyle?>(iso => iso == "2023-06-06" ? new DatetimeHighlightStyle { TextColor = "blue" } : null);
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)Day(datetime, 6).Foreground!).Color);
        Assert.Null(Day(datetime, 5).Highlight);
    });

    [AvaloniaFact]
    public void B18_The_Month_Year_Button_Opens_The_Wheels_And_A_Month_There_Is_A_Selection() => Run((scene, clock) =>
    {
        // monthYear: Hide year picker; August → ionChange 2023-08-15T13:47:00; 2021 → 2021-08-15T13:47:00.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        var log = Log.Of(datetime);
        var toggle = Part<Button>(datetime, "PART_MonthYearButton");
        Assert.Equal("Show year picker", AutomationProperties.GetName(toggle));
        Click(scene, toggle);
        Settle(clock, 400);
        Assert.Equal("Hide year picker", AutomationProperties.GetName(toggle));
        Assert.True(datetime.MonthYearWheel!.IsEffectivelyVisible);
        Assert.False(datetime.CalendarBody!.IsEffectivelyVisible);
        var months = datetime.MonthYearWheel.Items.OfType<AvonicPickerColumn>().First(c => c.Classes.Contains("month-column"));
        Click(scene, months.Items.OfType<AvonicPickerColumnOption>().First(o => Equals(o.Value, 8)));
        Settle(clock, 400);
        Assert.Equal("2023-08-15T13:47:00", datetime.Value.ToString());
        Assert.Equal("August 2023", MonthShown(datetime));
        Assert.Equal(["ionChange:2023-08-15T13:47:00"], log.Events);
        Click(scene, toggle);
        Settle(clock, 400);
        Assert.True(datetime.CalendarBody.IsEffectivelyVisible);
    });

    [AvaloniaFact]
    public void B19_The_Keyboard_Moves_Between_Days_And_Selects() => Run((scene, clock) =>
    {
        // keyboard: the calendar hands focus to the active day; Right 16, Down 23, Left 22, Up 15, Home 11, End 17, PageDown July 17,
        // PageUp, Shift+PageDown June 2024, Shift+PageUp; Enter selects.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        var body = datetime.CalendarBody!;
        body.Focus(NavigationMethod.Tab);
        Settle(clock, 50);
        DatetimeCalendarDay Focused() => (DatetimeCalendarDay)TopLevel.GetTopLevel(datetime)!.FocusManager!.GetFocusedElement()!;
        Assert.Equal(15, Focused().Parts.Day);

        void Press(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            scene.Window.KeyPressQwerty(key == Key.PageDown ? PhysicalKey.PageDown : key == Key.PageUp ? PhysicalKey.PageUp : key == Key.Home ? PhysicalKey.Home
                : key == Key.End ? PhysicalKey.End : key == Key.Left ? PhysicalKey.ArrowLeft : key == Key.Right ? PhysicalKey.ArrowRight
                : key == Key.Up ? PhysicalKey.ArrowUp : key == Key.Down ? PhysicalKey.ArrowDown : PhysicalKey.Enter, modifiers);
            Settle(clock, 50);
        }

        Press(Key.Right);
        Assert.Equal(16, Focused().Parts.Day);
        Press(Key.Down);
        Assert.Equal(23, Focused().Parts.Day);
        Press(Key.Left);
        Assert.Equal(22, Focused().Parts.Day);
        Press(Key.Up);
        Assert.Equal(15, Focused().Parts.Day);
        Press(Key.Home);
        Assert.Equal(11, Focused().Parts.Day);
        Press(Key.End);
        Assert.Equal(17, Focused().Parts.Day);
        Press(Key.PageDown);
        Assert.Equal((7, 17), (Focused().Parts.Month!.Value, Focused().Parts.Day!.Value));
        Press(Key.PageUp);
        Press(Key.PageDown, RawInputModifiers.Shift);
        Assert.Equal("June 2024", MonthShown(datetime));
        Press(Key.PageUp, RawInputModifiers.Shift);
        Assert.Equal("June 2023", MonthShown(datetime));
        Press(Key.Enter);
        Assert.Equal("2023-06-17T13:47:00", datetime.Value.ToString());
    });

    [AvaloniaFact]
    public void B19_A_Move_To_A_Day_Before_Min_Does_Nothing() => Run((scene, clock) =>
    {
        // keyboardMin: min the 14th, Left from 15 → 14, Left again → 14.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, Min = "2023-06-14" });
        Day(datetime, 15).Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Settle(clock, 50);
        scene.Window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Settle(clock, 50);
        Assert.Equal(14, ((DatetimeCalendarDay)TopLevel.GetTopLevel(datetime)!.FocusManager!.GetFocusedElement()!).Parts.Day);
    });

    [AvaloniaFact]
    public void B19_Days_Are_Out_Of_The_Tab_Order() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        Assert.All(datetime.CalendarMonths!.Months.SelectMany(m => m.Days), d => Assert.False(d.IsTabStop));
        Assert.True(datetime.CalendarBody!.IsTabStop);
    });

    // ── B20 to B22: the time ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_The_Time_Button_Opens_A_Popover_Of_Wheels_And_A_Choice_Is_A_Selection() => Run((scene, clock) =>
    {
        // timeButton: 3 → 15:47; AM → 03:47; the button active while open.
        var datetime = Show(scene, clock, new AvonicDatetime { Value = June15 });
        var log = Log.Of(datetime);
        var button = Part<Button>(datetime, "PART_TimeButton");
        Assert.Equal("1:47 PM", Part<TextBlock>(datetime, "PART_TimeText").Text);
        Click(scene, button);
        Settle(clock, 600);
        Assert.Contains(":active", button.Classes);
        var columns = datetime.TimeWheel!.Items.OfType<AvonicPickerColumn>().ToList();
        Assert.Equal(["Select an hour", "Select a minute", "Select a day period"], columns.Select(c => AutomationProperties.GetName(c)));
        Click(scene, columns[0].Items.OfType<AvonicPickerColumnOption>().ElementAt(3));
        Settle(clock, 400);
        Assert.Equal("2023-06-15T15:47:00", datetime.Value.ToString());
        Click(scene, columns[2].Items.OfType<AvonicPickerColumnOption>().First());
        Settle(clock, 400);
        Assert.Equal("2023-06-15T03:47:00", datetime.Value.ToString());
        Assert.Equal(["ionChange:2023-06-15T15:47:00", "ionChange:2023-06-15T03:47:00"], log.Events);
        _ = datetime.TimePopover!.Dismiss();
        Settle(clock, 600);
        Assert.DoesNotContain(":active", button.Classes);
        Assert.Equal("3:47 AM", Part<TextBlock>(datetime, "PART_TimeText").Text);
    });

    [AvaloniaFact]
    public void B21_The_Hours_Follow_The_Cycle_And_Max_On_Its_Day() => Run((scene, clock) =>
    {
        // minMaxTime: max 17:15 on the 20th leaves 12 … 5 PM.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Time, Value = June15, HourCycle = DatetimeHourCycle.H23 });
        var hours = datetime.Wheel!.Items.OfType<AvonicPickerColumn>().First();
        Assert.Equal(24, hours.Items.Count);
        Assert.Equal("00", hours.Items.OfType<AvonicPickerColumnOption>().First().Content);
        Assert.Equal(2, datetime.Wheel.Items.Count); // no AM/PM

        var limited = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Time, Value = "2023-06-20T13:47:00", Max = "2023-06-20T17:15" });
        var limitedHours = limited.Wheel!.Items.OfType<AvonicPickerColumn>().First().Items.OfType<AvonicPickerColumnOption>().Select(o => (string)o.Content!);
        Assert.Equal(["12", "1", "2", "3", "4", "5"], limitedHours);
    });

    // ── B23, B24: wheels ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B23_The_Date_Wheel_Has_Month_Day_Year_And_A_Choice_Is_A_Selection() => Run((scene, clock) =>
    {
        // wheelChange: a month chosen → its date with the time (Ionic's February is out of view here; April is in it), and its days.
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, PreferWheel = true, Value = June15 });
        var columns = datetime.Wheel!.Items.OfType<AvonicPickerColumn>().ToList();
        Assert.Equal(["Select a month", "Select a day", "Select a year"], columns.Select(c => AutomationProperties.GetName(c)));
        Assert.Equal(101, columns[2].Items.Count);
        Click(scene, columns[0].Items.OfType<AvonicPickerColumnOption>().First(o => Equals(o.Value, 4)));
        Settle(clock, 600);
        Assert.Equal("2023-04-15T13:47:00", datetime.Value.ToString());
        Assert.Equal(30, datetime.Wheel.Items.OfType<AvonicPickerColumn>().First(c => c.Classes.Contains("day-column")).Items.Count);
    });

    [AvaloniaFact]
    public void B23_The_DateTime_Wheel_Has_One_Date_Column_With_Today() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.DateTime, PreferWheel = true, Value = June15 });
        var date = datetime.Wheel!.Items.OfType<AvonicPickerColumn>().First();
        var options = date.Items.OfType<AvonicPickerColumnOption>().ToList();
        Assert.Equal(92, options.Count);
        Assert.Equal("Mon, May 1", options[0].Content);
        Assert.Equal("Today", options.First(o => Equals(o.Value, "2023-6-20")).Content);
        Assert.Equal("2023-6-15", date.Value);
        Assert.Equal(4, datetime.Wheel.Items.Count);
    });

    // ── B25 to B31 ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B25_The_Header_Shows_With_The_Title_And_The_Selected_Date() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = June15, ShowDefaultTitle = true });
        Assert.True(Part<Control>(datetime, "PART_DatetimeHeader").IsEffectivelyVisible);
        Assert.Equal("Thu, Jun 15", Part<TextBlock>(datetime, "PART_DatetimeSelectedDate").Text);
        Assert.Equal("Select Date", datetime.ShownTitle);

        var slotted = Show(scene, clock, new AvonicDatetime { Value = June15, TitleContent = "Pick a day" });
        Assert.True(Part<Control>(slotted, "PART_DatetimeHeader").IsEffectivelyVisible);

        var none = Show(scene, clock, new AvonicDatetime { Value = June15 });
        Assert.False(Part<Control>(none, "PART_DatetimeHeader").IsEffectivelyVisible);
    });

    [AvaloniaFact]
    public void B25_FormatOptions_Writes_The_Header_And_The_Time() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime
        {
            Value = June15, ShowDefaultTitle = true,
            FormatOptions = new DatetimeFormatOptions
            {
                Date = new DateTimeFormatOptions { Weekday = DateTimeFormatText.Long, Month = DateTimeFormatMonth.Long, Day = DateTimeFormatNumber.Numeric },
                Time = new DateTimeFormatOptions { Hour = DateTimeFormatNumber.TwoDigit, Minute = DateTimeFormatNumber.TwoDigit },
            },
        });
        Assert.Equal("Thursday, June 15", Part<TextBlock>(datetime, "PART_DatetimeSelectedDate").Text);
        Assert.Equal("01:47 PM", Part<TextBlock>(datetime, "PART_TimeText").Text);
    });

    [AvaloniaFact]
    public void B26_The_Footer_Buttons_Show_And_Are_Disabled_When_Readonly() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = June15, ShowDefaultButtons = true, Readonly = true, DoneText = "OK" });
        Assert.True(Part<AvonicButton>(datetime, "PART_ConfirmButton").IsEffectivelyVisible);
        Assert.False(Part<AvonicButton>(datetime, "PART_ClearButton").IsEffectivelyVisible);
        Assert.False(Part<AvonicButton>(datetime, "PART_ConfirmButton").IsEnabled);
        Assert.Equal("OK", Part<AvonicButton>(datetime, "PART_ConfirmButton").Content);
    });

    [AvaloniaFact]
    public void B27_Disabled_Takes_Nothing() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, IsEnabled = false });
        var log = Log.Of(datetime);
        Click(scene, Day(datetime, 20));
        Click(scene, Part<AvonicButton>(datetime, "PART_NextButton"));
        Settle(clock, 600);
        Assert.Empty(log.Events);
        Assert.Equal("June 2023", MonthShown(datetime));
        Assert.False(Day(datetime, 20).IsEffectivelyEnabled);
    });

    [AvaloniaFact]
    public void B29_In_An_Item_It_Is_Interactive_And_A_Datetime() => Run((scene, clock) =>
    {
        var datetime = new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 };
        var item = new AvonicItem { Content = datetime };
        scene.Window.Content = new AvonicList { Items = { item } };
        Settle(clock, 100);
        Assert.Contains(":item-interactive", item.Classes);
        Assert.Contains(":item-datetime", item.Classes);
        datetime.IsEnabled = false;
        Assert.Contains(":item-interactive-disabled", item.Classes);
    });

    [AvaloniaFact]
    public void B30_Fixed_Is_At_Most_350_And_Cover_Fills() => Run((scene, clock) =>
    {
        var fixedOne = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        Assert.Equal(350, fixedOne.Bounds.Width);
        var cover = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, Size = DatetimeSize.Cover });
        Assert.Equal(358, cover.Bounds.Width);
    });

    // ── Focus ────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void IonFocus_And_IonBlur_Come_Once_As_Focus_Enters_And_Leaves() => Run((scene, clock) =>
    {
        var datetime = new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 };
        var after = new Button { Content = "After" };
        scene.Window.Content = new StackPanel { Margin = new Thickness(16), Children = { datetime, after } };
        Settle(clock, 400);
        var log = Log.Of(datetime, focus: true);
        var buttonFocus = 0;
        datetime.AddHandler(AvonicButton.IonFocusEvent, (_, _) => buttonFocus++, handledEventsToo: false);
        Part<Button>(datetime, "PART_MonthYearButton").Focus(NavigationMethod.Tab);
        Part<AvonicButton>(datetime, "PART_NextButton").Focus(NavigationMethod.Tab);
        Day(datetime, 15).Focus(NavigationMethod.Tab);
        after.Focus(NavigationMethod.Tab);
        Assert.Equal(["ionFocus", "ionBlur"], log.Events);
        Assert.Equal(0, buttonFocus);
    });

    // ── Review pass 1 ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A_Time_Alone_In_The_Grid_Shows_The_Default_Month_And_Its_Time() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = "13:47" });
        Assert.Equal("June 2023", MonthShown(datetime));
        Assert.Equal("1:47 PM", Part<TextBlock>(datetime, "PART_TimeText").Text);
        datetime.Reset("09:05");
        Settle(clock, 100);
        Assert.Equal("9:05 AM", Part<TextBlock>(datetime, "PART_TimeText").Text);
    });

    [AvaloniaFact]
    public void A_Value_In_Year_One_Or_9999_Shows_Without_The_Month_Beyond() => Run((scene, clock) =>
    {
        var first = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = "0001-01-01T00:00:00" });
        Assert.Equal("January 1", MonthShown(first));
        Assert.DoesNotContain(first.CalendarMonths!.Months[0].Days, d => d.IsVisible);
        var last = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = "9999-12-31" });
        Assert.Equal("December 9999", MonthShown(last));
    });

    [AvaloniaFact]
    public void A_Null_String_Is_No_Value() => Run((scene, clock) =>
    {
        string? none = null;
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = none });
        Assert.Empty(ActiveDays(datetime));
        Assert.Equal(string.Empty, datetime.Value.ToString());
    });

    [AvaloniaFact]
    public void An_Adjacent_Day_Scrolls_Forward_From_The_Month_Shown() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, ShowAdjacentDays = true });
        var body = datetime.CalendarBody!;
        var width = body.Viewport.Width;
        Click(scene, datetime.CalendarMonths!.Months[1].Days.First(d => d.IsVisible && d.IsAdjacent && d.Parts.Day == 3));
        // June stays in the middle, July is drawn next, and the scroll is on its way there.
        Assert.Equal(6, datetime.CalendarMonths.Months[1].Month);
        Assert.Equal(7, datetime.CalendarMonths.Months[2].Month);
        Settle(clock, 100);
        Assert.InRange(body.Offset.X, width + 1, 2 * width - 1);
        Settle(clock, 600);
        Assert.Equal("July 2023", MonthShown(datetime));
        Assert.Equal(width, body.Offset.X, 1);
    });

    [AvaloniaFact]
    public void Highlights_Read_Css_Colours_And_A_Border_With_No_Colour_Is_The_Text_Colour() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime
        {
            Presentation = DatetimePresentation.Date, Value = June15,
            HighlightedDates = new DatetimeHighlight[]
            {
                new() { Date = "2023-06-05", BackgroundColor = "#ff000080" },
                new() { Date = "2023-06-06", TextColor = "rgb(0, 128, 0)", Border = "2px solid" },
            },
        });
        // CSS's #rrggbbaa: half-transparent red, not Avalonia's #aarrggbb.
        Assert.Equal(Color.FromArgb(0x80, 0xff, 0, 0), ((ISolidColorBrush)Day(datetime, 5).Background!).Color);
        Assert.Equal(Color.FromRgb(0, 128, 0), ((ISolidColorBrush)Day(datetime, 6).BorderBrush!).Color);
        Assert.Equal(new Thickness(2), Day(datetime, 6).BorderThickness);
    });

    [AvaloniaFact]
    public void The_Day_Period_Comes_First_Of_The_Whole_Wheel_In_Korean() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "ko-KR", Presentation = DatetimePresentation.DateTime, PreferWheel = true, Value = June15 });
        Assert.Equal(["Select a day period", "Select a date", "Select an hour", "Select a minute"],
            datetime.Wheel!.Items.OfType<AvonicPickerColumn>().Select(c => AutomationProperties.GetName(c)));
    });

    [AvaloniaFact]
    public void The_Year_First_Alignment_Goes_With_The_Locale() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "ja-JP", Presentation = DatetimePresentation.Date, PreferWheel = true, Value = June15 });
        // Japanese: year, month, day; the day at the end. en-US: month, day, year; the day in the middle, centred.
        var day = datetime.Wheel!.Items.OfType<AvonicPickerColumn>().First(c => c.Classes.Contains("day-column"));
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Right, day.HorizontalContentAlignment);
        datetime.Locale = "en-US";
        Settle(clock, 100);
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Center, day.HorizontalContentAlignment);
    });

    [AvaloniaFact]
    public void A_Presentation_Change_Raises_One_Rendered() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15 });
        var rendered = 0;
        datetime.Rendered += (_, _) => rendered++;
        Click(scene, Part<Button>(datetime, "PART_MonthYearButton"));
        Settle(clock, 100);
        datetime.Presentation = DatetimePresentation.Time;
        Settle(clock, 100);
        Assert.Equal(1, rendered);
        Assert.False(datetime.ShowMonthAndYear);
    });
}
