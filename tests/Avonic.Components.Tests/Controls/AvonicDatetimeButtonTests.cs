using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicDatetimeCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-datetime-button</c>, md, on the test clock, today pinned to 2023-06-20 10:00 and the culture en-US: B1 to B15 in
/// docs/components/ion-datetime-button/PLAN.md, from <c>datetime-button.js</c> and the measurements in <c>ref/behaviour.json</c>
/// (its cases are named in each test).
/// </summary>
public class AvonicDatetimeButtonTests
{
    internal const string June15 = "2023-06-15T13:47:00";

    public enum Place
    {
        Modal,
        Popover,
        Page,
    }

    /// <summary>The scene of the measurements: the button first, 16 in, then the datetime named <c>dt</c> in its overlay or the page.</summary>
    internal static (AvonicDatetimeButton Button, AvonicDatetime Datetime) Stage(Scene scene, TestClock clock, AvonicDatetime datetime,
        Place place = Place.Modal, AvonicDatetimeButton? button = null, Control? around = null)
    {
        button ??= new AvonicDatetimeButton();
        button.Datetime ??= "dt";
        datetime.Locale = "en-US";
        var page = new StackPanel { Margin = new Thickness(16) };
        var scope = new NameScope();
        NameScope.SetNameScope(page, scope);
        scope.Register("dt", datetime);
        if (around is Decorator decorator)
        {
            decorator.Child = button;
            page.Children.Add(decorator);
        }
        else
        {
            page.Children.Add(button);
        }

        page.Children.Add(place switch
        {
            Place.Modal => new AvonicModal { KeepContentsMounted = true, Content = datetime },
            Place.Popover => new AvonicPopover { KeepContentsMounted = true, Content = datetime },
            _ => datetime,
        });
        scene.Window.Content = page;
        Settle(clock, 400);
        return (button, datetime);
    }

    internal static DatetimeButtonNative DateButton(AvonicDatetimeButton button) => Native(button, "PART_DateButton");

    internal static DatetimeButtonNative TimeButton(AvonicDatetimeButton button) => Native(button, "PART_TimeButton");

    private static DatetimeButtonNative Native(AvonicDatetimeButton button, string name) =>
        button.GetVisualDescendants().OfType<DatetimeButtonNative>().First(b => b.Name == name);

    /// <summary>What each button shows, as Ionic's <c>dateText</c> and <c>timeText</c>: null where there's no button.</summary>
    private static (string? Date, string? Time) Texts(AvonicDatetimeButton button) =>
        (Shown(DateButton(button)), Shown(TimeButton(button)));

    private static string? Shown(DatetimeButtonNative native) => native.IsVisible ? native.Text : null;

    private static void Tap(Scene scene, TestClock clock, Visual target)
    {
        AvonicPickerColumnTests.Click(scene, AvonicPickerColumnTests.Centre(target, scene));
        Settle(clock, 600);
    }

    private static ExpandCollapseState Expanded(DatetimeButtonNative native) =>
        ((IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(native)).ExpandCollapseState;

    /// <summary>The colour a button's text has, its transition left out.</summary>
    private static Color TextColor(DatetimeButtonNative native) => ((ISolidColorBrush)native.Foreground!).Color;

    private static void NoTransitions(AvonicDatetimeButton button)
    {
        DateButton(button).Transitions = [];
        TimeButton(button).Transitions = [];
    }

    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Black = Colors.Black;

    // ── B1: the datetime by name ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Finds_The_Datetime_By_Name_And_Shows_Its_Value() => Run((scene, clock) =>
    {
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        Assert.Same(datetime, button.DatetimeElement);
        Assert.Equal(("Jun 15, 2023", "1:47 PM"), Texts(button));
    });

    [AvaloniaTheory]
    [InlineData(null, "[ion-datetime-button] - An ID associated with an ion-datetime instance is required to function properly.")]
    [InlineData("nothere", "[ion-datetime-button] - No ion-datetime instance found for ID 'nothere'.")]
    [InlineData("other", "[ion-datetime-button] - Expected an ion-datetime instance for ID 'other' but received 'Border' instead.")]
    public void B1_Without_A_Datetime_It_Logs_Ionics_Error_And_Shows_Nothing(string? name, string error) => Run((scene, clock) =>
    {
        // noId, wrongId, notDatetime: the host 0 high.
        var button = new AvonicDatetimeButton { Datetime = name ?? "" };
        var page = new StackPanel { Margin = new Thickness(16) };
        var scope = new NameScope();
        NameScope.SetNameScope(page, scope);
        scope.Register("other", new Border());
        page.Children.Add(button);
        scene.Window.Content = page;
        Settle(clock, 400);
        Assert.Equal(error, button.LastError);
        Assert.Null(button.DatetimeElement);
        Assert.Equal(0, button.Bounds.Height);
    });

    [AvaloniaFact]
    public void B1_The_Name_Is_Read_Once_At_Load() => Run((scene, clock) =>
    {
        // componentWillLoad reads datetime; datetime has no watcher.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        button.Datetime = "nothere";
        Settle(clock, 100);
        Assert.Same(datetime, button.DatetimeElement);
        Assert.Null(button.LastError);
    });

    [AvaloniaFact]
    public void B1_The_Name_Is_Found_In_A_Name_Scope_Around_The_Nearest() => Run((scene, clock) =>
    {
        // getElementById sees the whole document: a button in a template or a user control, with a scope of its own, finds a datetime
        // named in the page's.
        var inner = new StackPanel();
        NameScope.SetNameScope(inner, new NameScope());
        var button = new AvonicDatetimeButton { Datetime = "dt" };
        inner.Children.Add(button);
        var (_, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, button: new AvonicDatetimeButton { Datetime = "dt" });
        ((StackPanel)datetime.Parent!.Parent!).Children.Add(inner);
        Settle(clock, 100);
        Assert.Same(datetime, button.DatetimeElement);
        Assert.Equal(("Jun 15, 2023", "1:47 PM"), Texts(button));
    });

    [AvaloniaFact]
    public void B1_A_Name_That_Comes_After_The_First_Attach_Is_Looked_Up_Then() => Run((scene, clock) =>
    {
        // A binding can bring it after the attach, where a framework sets Ionic's props before load.
        var button = new AvonicDatetimeButton { Datetime = "" };
        var (_, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, button: button);
        Assert.Null(button.DatetimeElement);
        button.Datetime = "dt";
        Settle(clock, 100);
        Assert.Same(datetime, button.DatetimeElement);
        Assert.Equal(("Jun 15, 2023", "1:47 PM"), Texts(button));
    });

    [AvaloniaFact]
    public void B1_A_Name_Given_While_Out_Of_The_Tree_Is_Looked_Up_At_The_Next_Attach() => Run((scene, clock) =>
    {
        var button = new AvonicDatetimeButton { Datetime = "" };
        var (_, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, button: button);
        var page = (StackPanel)button.Parent!;
        page.Children.Remove(button);
        button.Datetime = "dt";
        page.Children.Insert(0, button);
        Settle(clock, 100);
        Assert.Same(datetime, button.DatetimeElement);
        Assert.Equal(("Jun 15, 2023", "1:47 PM"), Texts(button));
    });

    [AvaloniaFact]
    public void B1_The_Datetime_Is_Read_Once_It_Has_Loaded_With_An_Apps_Style() => Run((scene, clock) =>
    {
        // componentOnReady: the datetime after it, styled time-only by the app, gives a time button only, and its value lists are
        // its own when it has no value.
        scene.Window.Styles.Add(new Avalonia.Styling.Style(x => x.OfType<AvonicDatetime>())
        {
            Setters =
            {
                new Avalonia.Styling.Setter(AvonicDatetime.PresentationProperty, DatetimePresentation.Time),
                new Avalonia.Styling.Setter(AvonicDatetime.HourValuesProperty, (DatetimeValueList?)new[] { 8, 14 }),
            },
        });
        var (button, _) = Stage(scene, clock, new AvonicDatetime());
        Assert.Equal((null, "8:00 AM"), Texts(button));
    });

    // ── B2: the text of each presentation ────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(DatetimePresentation.DateTime, false, "Jun 15, 2023", "1:47 PM")]
    [InlineData(DatetimePresentation.TimeDate, false, "Jun 15, 2023", "1:47 PM")]
    [InlineData(DatetimePresentation.Date, false, "Jun 15, 2023", null)]
    [InlineData(DatetimePresentation.Time, false, null, "1:47 PM")]
    [InlineData(DatetimePresentation.MonthYear, false, "June 2023", null)]
    [InlineData(DatetimePresentation.Month, false, "June", null)]
    [InlineData(DatetimePresentation.Year, false, "2023", null)]
    [InlineData(DatetimePresentation.DateTime, true, "Jun 15, 2023 1:47 PM", null)]
    [InlineData(DatetimePresentation.TimeDate, true, "Jun 15, 2023 1:47 PM", null)]
    public void B2_Each_Presentation_Has_Its_Text(DatetimePresentation presentation, bool preferWheel, string? date, string? time) => Run((scene, clock) =>
    {
        // dateTime, timeDate, date, time, monthYear, month, year, preferWheel, x_wheelTimeDate.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15, Presentation = presentation, PreferWheel = preferWheel });
        Assert.Equal((date, time), Texts(button));
    });

    [AvaloniaFact]
    public void B2_Format_Options_Shape_The_Text() => Run((scene, clock) =>
    {
        // x_formats.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        datetime.FormatOptions = new DatetimeFormatOptions
        {
            Date = new DateTimeFormatOptions { Weekday = DateTimeFormatText.Short, Month = DateTimeFormatMonth.Long, Day = DateTimeFormatNumber.TwoDigit },
            Time = new DateTimeFormatOptions { Hour = DateTimeFormatNumber.TwoDigit, Minute = DateTimeFormatNumber.TwoDigit },
        };
        datetime.Value = "2023-06-16T09:05:00";
        Assert.Equal(("Fri, June 16", "09:05 AM"), Texts(button));
        datetime.FormatOptions = new DatetimeFormatOptions { Time = new DateTimeFormatOptions { Hour = DateTimeFormatNumber.TwoDigit, Minute = DateTimeFormatNumber.TwoDigit } };
        datetime.Value = "2023-06-17T09:05:00";
        Assert.Equal(("Jun 17, 2023", "09:05 AM"), Texts(button));
    });

    [AvaloniaTheory]
    [InlineData(DatetimePresentation.MonthYear, "Jul 24", null)]
    [InlineData(DatetimePresentation.Date, "7/15", null)]
    [InlineData(DatetimePresentation.Time, null, "1 PM")]
    public void B2_Format_Options_Of_Each_Presentation(DatetimePresentation presentation, string? date, string? time) => Run((scene, clock) =>
    {
        // x_fo4_month-year, x_fo5_date, x_fo6_time.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Presentation = presentation });
        datetime.FormatOptions = new DatetimeFormatOptions
        {
            Date = presentation == DatetimePresentation.MonthYear
                ? new DateTimeFormatOptions { Month = DateTimeFormatMonth.Short, Year = DateTimeFormatNumber.TwoDigit }
                : new DateTimeFormatOptions { Day = DateTimeFormatNumber.Numeric, Month = DateTimeFormatMonth.Numeric },
            Time = new DateTimeFormatOptions { Hour = DateTimeFormatNumber.Numeric },
        };
        datetime.Value = "2024-07-15T13:47:00";
        Assert.Equal((date, time), Texts(button));
    });

    [AvaloniaTheory]
    [InlineData("ja-JP", null, "2023年6月15日", "13:47")]
    [InlineData("en-GB", null, "15 Jun 2023", "13:47")]
    [InlineData("de-DE", null, "15. Juni 2023", "13:47")]
    [InlineData("ko-KR", null, "2023년 6월 15일", "오후 1:47")]
    [InlineData("ar-EG", null, "١٥ يونيو ٢٠٢٣", "١:٤٧ م")]
    [InlineData("fr-FR", null, "15 juin 2023", "13:47")]
    [InlineData("en-US", DatetimeHourCycle.H23, "Jun 15, 2023", "13:47")]
    public void B2_The_Datetimes_Locale_And_Hour_Cycle(string locale, DatetimeHourCycle? hourCycle, string date, string time) => Run((scene, clock) =>
    {
        // locales: the datetime's own helpers, so the text is the datetime's on every platform; Chromium's strings where .NET's ICU
        // was measured against Chromium (Windows: Linux's gives some locales' AM and PM in English, as the datetime's README says).
        var datetime = new AvonicDatetime { Value = June15, HourCycle = hourCycle };
        var (button, _) = Stage(scene, clock, datetime);
        datetime.Locale = locale;
        datetime.Value = "2023-06-15T13:47";
        var culture = DatetimeIntl.Culture(locale);
        var parts = DatetimeUtils.ParseDate("2023-06-15T13:47")!.Value;
        var own = DatetimeIntl.LocalizedDateTime(culture, parts,
            new DateTimeFormatOptions { Month = DateTimeFormatMonth.Short, Day = DateTimeFormatNumber.Numeric, Year = DateTimeFormatNumber.Numeric });
        var ownTime = DatetimeIntl.LocalizedTime(culture, parts, DatetimeIntl.GetHourCycle(culture, hourCycle), null);
        Assert.Equal((own, ownTime), Texts(button));
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal((date, time), Texts(button));
        }
    });

    // ── B3: the values ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_No_Value_Is_The_Datetimes_Default_Part() => Run((scene, clock) =>
    {
        // noValue: today; defaultValues: today snapped to the value lists.
        var (button, _) = Stage(scene, clock, new AvonicDatetime());
        Assert.Equal(("Jun 20, 2023", "10:00 AM"), Texts(button));
        var (snapped, _) = Stage(scene, clock, new AvonicDatetime { HourValues = new[] { 8, 14 }, MinuteValues = new[] { 20, 40 } });
        Assert.Equal(("Jun 20, 2023", "8:20 AM"), Texts(snapped));
    });

    [AvaloniaFact]
    public void B3_A_Value_That_Doesnt_Parse_Leaves_The_Text_And_None_Is_Today_Again() => Run((scene, clock) =>
    {
        // setValue: Feb 29 2024; nonsense keeps it; undefined is today.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        datetime.Value = "2024-02-29T07:05:00";
        Assert.Equal(("Feb 29, 2024", "7:05 AM"), Texts(button));
        datetime.Value = "nonsense";
        Assert.Equal(("Feb 29, 2024", "7:05 AM"), Texts(button));
        datetime.Value = null;
        Assert.Equal(("Jun 20, 2023", "10:00 AM"), Texts(button));
    });

    [AvaloniaFact]
    public void B3_A_Date_Alone_Has_No_Time_And_A_Time_Alone_Is_In_January() => Run((scene, clock) =>
    {
        // dateOnlyValue, timeValueInDateTime.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = "2023-06-15" });
        Assert.Equal(("Jun 15, 2023", "Invalid Time"), Texts(button));
        datetime.Value = "13:47";
        Assert.Equal(("Jan 1, 2023", "1:47 PM"), Texts(button));
    });

    // ── B4: multiple ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Multiple_Dates_Are_Counted_Unless_There_Is_One() => Run((scene, clock) =>
    {
        // x_multiple.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Multiple = true });
        Assert.Equal(("0 days", null), Texts(button));
        datetime.Value = new[] { "2023-06-05", "2023-06-09", "2023-06-15" };
        Assert.Equal(("3 days", null), Texts(button));
        datetime.Value = new[] { "2023-06-05" };
        Assert.Equal(("Jun 5, 2023", null), Texts(button));
        datetime.Value = Array.Empty<string>();
        Assert.Equal(("0 days", null), Texts(button));
    });

    [AvaloniaFact]
    public void B4_The_Formatter_Is_Given_The_Strings_And_Its_Exception_Is_Logged() => Run((scene, clock) =>
    {
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Multiple = true });
        datetime.TitleSelectedDatesFormatter = values => $"Picked {values.Count}: {string.Join(" & ", values)}";
        datetime.Value = new[] { "2023-06-05", "2023-06-09" };
        Assert.Equal(("Picked 2: 2023-06-05 & 2023-06-09", null), Texts(button));
        datetime.TitleSelectedDatesFormatter = _ => throw new InvalidOperationException("boom");
        datetime.Value = new[] { "2023-06-06", "2023-06-09" };
        Assert.Equal(("2 days", null), Texts(button));
        Assert.StartsWith("[ion-datetime-button] - Exception in provided `titleSelectedDatesFormatter`:", button.LastError);
    });

    [AvaloniaFact]
    public void B4_Multiple_With_A_Time_Shows_The_First() => Run((scene, clock) =>
    {
        // x_multipleDateTime.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Multiple = true });
        datetime.Value = new[] { "2023-06-05T10:00", "2023-06-09T11:00" };
        Assert.Equal(("Jun 5, 2023", "10:00 AM"), Texts(button));
    });

    // ── B5: updates ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Follows_A_Confirmed_Choice_Not_One_Waiting_For_Done() => Run((scene, clock) =>
    {
        // chooseButtons: 22 chosen keeps the 15th; confirmed, the 22nd.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = June15, ShowDefaultButtons = true });
        Tap(scene, clock, DateButton(button));
        var day = datetime.CalendarMonths!.Months[1].Days.First(d => d.IsVisible && !d.IsPadding && !d.IsAdjacent && d.Parts.Day == 22);
        Tap(scene, clock, day);
        Assert.Equal(("Jun 15, 2023", null), Texts(button));
        datetime.Confirm(closeOverlay: true);
        Settle(clock, 600);
        Assert.Equal(("Jun 22, 2023", null), Texts(button));
    });

    [AvaloniaFact]
    public void B5_A_Change_Of_Locale_Or_Presentation_Alone_Changes_Nothing() => Run((scene, clock) =>
    {
        // setValue: de-DE and date change nothing; the next value is in German, both buttons still.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        datetime.Locale = "de-DE";
        datetime.Presentation = DatetimePresentation.Date;
        Assert.Equal(("Jun 15, 2023", "1:47 PM"), Texts(button));
        datetime.Value = "2024-03-01T07:05:00";
        Assert.Equal(("1. März 2024", "07:05"), Texts(button));
    });

    [AvaloniaFact]
    public void B5_Detached_And_Attached_Again_It_Shows_The_Value_It_Missed() => Run((scene, clock) =>
    {
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        var page = (StackPanel)button.Parent!;
        page.Children.Remove(button);
        datetime.Value = "2024-02-29T07:05:00";
        page.Children.Insert(0, button);
        Settle(clock, 100);
        Assert.Equal(("Feb 29, 2024", "7:05 AM"), Texts(button));
    });

    // ── B6: a tap ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_The_Date_Button_Shows_The_Date_In_The_Modal() => Run((scene, clock) =>
    {
        // modalDate: date-time → date, the modal presented.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        var modal = (AvonicModal)datetime.Parent!;
        Tap(scene, clock, DateButton(button));
        Assert.Equal(DatetimePresentation.Date, datetime.Presentation);
        Assert.True(modal.IsShown);
    });

    [AvaloniaFact]
    public void B6_The_Time_Button_Shows_The_Time_In_The_Modal() => Run((scene, clock) =>
    {
        // modalTime: date-time → time.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        var modal = (AvonicModal)datetime.Parent!;
        Tap(scene, clock, TimeButton(button));
        Assert.Equal(DatetimePresentation.Time, datetime.Presentation);
        Assert.True(modal.IsShown);
    });

    [AvaloniaTheory]
    [InlineData(DatetimePresentation.DateTime)]
    [InlineData(DatetimePresentation.TimeDate)]
    public void B6_The_Wheels_Keep_Their_Presentation(DatetimePresentation presentation) => Run((scene, clock) =>
    {
        // modalWheel, x_wheelTimeDate: unchanged.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15, Presentation = presentation, PreferWheel = true });
        Tap(scene, clock, DateButton(button));
        Assert.Equal(presentation, datetime.Presentation);
        Assert.True(((AvonicModal)datetime.Parent!).IsShown);
    });

    [AvaloniaFact]
    public void B6_An_Apps_Binding_On_The_Presentation_Still_Drives_It() => Run((scene, clock) =>
    {
        // The button sets it as Ionic sets the prop, which a framework's binding sets again when its own value changes.
        var holder = new Border { Tag = DatetimePresentation.DateTime };
        var datetime = new AvonicDatetime { Value = June15 };
        datetime.Bind(AvonicDatetime.PresentationProperty, new Avalonia.Data.Binding("Tag") { Source = holder });
        var (button, _) = Stage(scene, clock, datetime);
        Tap(scene, clock, TimeButton(button));
        Assert.Equal(DatetimePresentation.Time, datetime.Presentation);
        holder.Tag = DatetimePresentation.Date;
        Assert.Equal(DatetimePresentation.Date, datetime.Presentation);
    });

    [AvaloniaTheory]
    [InlineData(false, 58, 47, 320, 344)]
    [InlineData(true, 196.44, 47, 181.56, 200)]
    public void B6_The_Popover_Is_Placed_By_The_Button_Tapped(bool time, double x, double y, double width, double height) => Run((scene, clock) =>
    {
        // popoverDate, popoverTime: the popover under the button tapped, 12 in from the right, as wide as its datetime.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, Place.Popover);
        Tap(scene, clock, time ? TimeButton(button) : DateButton(button));
        Assert.True(((AvonicPopover)datetime.Parent!).IsShown);
        Near([x, y, width, height], Box(datetime, scene.Window), 1);
    });

    [AvaloniaFact]
    public void B6_A_Time_Only_Popover_Starts_At_Its_Button() => Run((scene, clock) =>
    {
        // popoverTimeOnly: 153.55, 47.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15, Presentation = DatetimePresentation.Time }, Place.Popover);
        Tap(scene, clock, TimeButton(button));
        Near([153.55, 47, 181.56, 200], Box(datetime, scene.Window), 1);
    });

    [AvaloniaFact]
    public void B6_The_Popover_Waits_For_The_Datetime_To_Render_Its_New_Presentation() => Run((scene, clock) =>
    {
        // popoverDate: ionRender at 70 ms, ionPopoverWillPresent at 72. A frame after the presentation, not at the click.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, Place.Popover);
        var popover = (AvonicPopover)datetime.Parent!;
        var events = new List<string>();
        datetime.Rendered += (_, _) => events.Add("ionRender");
        popover.IonPopoverWillPresent += (_, _) => events.Add("willPresent");
        AvonicPickerColumnTests.Click(scene, AvonicPickerColumnTests.Centre(DateButton(button), scene));
        Assert.Empty(events);
        Settle(clock, 600);
        Assert.Equal(["ionRender", "willPresent"], events);
    });

    [AvaloniaFact]
    public void B6_A_Button_Removed_Before_The_Datetime_Renders_Presents_Nothing() => Run((scene, clock) =>
    {
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, Place.Popover);
        AvonicPickerColumnTests.Click(scene, AvonicPickerColumnTests.Centre(DateButton(button), scene));
        ((StackPanel)button.Parent!).Children.Remove(button);
        Settle(clock, 600);
        Assert.False(((AvonicPopover)datetime.Parent!).IsShown);
    });

    [AvaloniaFact]
    public void B6_In_The_Page_Only_The_Presentation_Changes() => Run((scene, clock) =>
    {
        // inline: time, the datetime 200 high, the time button the active one.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, Place.Page);
        NoTransitions(button);
        Tap(scene, clock, TimeButton(button));
        Assert.Equal(DatetimePresentation.Time, datetime.Presentation);
        Assert.Equal(Primary, TextColor(TimeButton(button)));
        Assert.Equal(Black, TextColor(DateButton(button)));
    });

    // ── B7: active ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_Active_While_The_Modal_Shows_The_Datetime() => Run((scene, clock) =>
    {
        // modalDate: off before, on from the present, off once dismissed; aria-expanded on both.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        NoTransitions(button);
        Assert.Equal(Black, TextColor(DateButton(button)));
        Assert.Equal(ExpandCollapseState.Collapsed, Expanded(DateButton(button)));
        Tap(scene, clock, DateButton(button));
        Assert.Equal(Primary, TextColor(DateButton(button)));
        Assert.Equal(Black, TextColor(TimeButton(button)));
        Assert.Equal(ExpandCollapseState.Expanded, Expanded(DateButton(button)));
        Assert.Equal(ExpandCollapseState.Expanded, Expanded(TimeButton(button)));
        _ = ((AvonicModal)datetime.Parent!).Dismiss();
        Settle(clock, 800);
        Assert.Equal(Black, TextColor(DateButton(button)));
        Assert.Equal(ExpandCollapseState.Collapsed, Expanded(TimeButton(button)));
    });

    [AvaloniaTheory]
    [InlineData(DatetimePresentation.DateTime, true)]
    [InlineData(DatetimePresentation.TimeDate, false)]
    public void B7_In_The_Page_The_Presentations_Button_Is_Active_From_The_Start(DatetimePresentation presentation, bool date) => Run((scene, clock) =>
    {
        // inline, inlineTimeDate.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15, Presentation = presentation }, Place.Page);
        NoTransitions(button);
        Assert.Equal(date ? Primary : Black, TextColor(DateButton(button)));
        Assert.Equal(date ? Black : Primary, TextColor(TimeButton(button)));
        Assert.True(button.IsDatetimeActive);
    });

    [AvaloniaFact]
    public void B7_Hidden_Around_It_Is_Inactive() => Run((scene, clock) =>
    {
        // inlineHidden: display: none around the datetime, off; shown, on; hidden, off.
        var wrap = new Border();
        var datetime = new AvonicDatetime { Value = June15 };
        var (button, _) = Stage(scene, clock, datetime, Place.Page);
        var page = (StackPanel)button.Parent!;
        page.Children.Remove(datetime);
        wrap.Child = datetime;
        page.Children.Add(wrap);
        Settle(clock, 100);
        Assert.True(button.IsDatetimeActive);
        wrap.IsVisible = false;
        Settle(clock, 100);
        Assert.False(button.IsDatetimeActive);
        Assert.Equal(ExpandCollapseState.Collapsed, Expanded(DateButton(button)));
        wrap.IsVisible = true;
        Settle(clock, 100);
        Assert.True(button.IsDatetimeActive);
    });

    [AvaloniaFact]
    public void B7_It_Takes_One_Percent_Of_The_Datetime_In_View() => Run((scene, clock) =>
    {
        // y_ratio: 3 px of 391 in a scroller isn't active, 4 is, and back again the same.
        var datetime = new AvonicDatetime { Value = June15 };
        var scroller = new ScrollViewer
        {
            Height = 300,
            Content = new StackPanel { Children = { new Control { Height = 400 }, datetime, new Control { Height = 400 } } },
        };
        var button = new AvonicDatetimeButton { Datetime = "dt" };
        var page = new StackPanel { Margin = new Thickness(16) };
        var scope = new NameScope();
        NameScope.SetNameScope(page, scope);
        scope.Register("dt", datetime);
        datetime.Locale = "en-US";
        page.Children.Add(button);
        page.Children.Add(scroller);
        scene.Window.Content = page;
        Settle(clock, 400);
        Assert.False(button.IsDatetimeActive);
        foreach (var (px, active) in new[] { (3, false), (4, true), (8, true), (4, true), (3, false), (0, false) })
        {
            scroller.Offset = new Vector(0, 100 + px);
            Settle(clock, 50);
            Assert.True(active == button.IsDatetimeActive, $"{px} px in view: expected {(active ? "active" : "inactive")}");
        }
    });

    [AvaloniaFact]
    public void B7_Active_In_A_Sheet_That_Slides_Up() => Run((scene, clock) =>
    {
        // A sheet's enter moves it by a render transform alone, which causes no layout pass: the check at its present's end sees it.
        var datetime = new AvonicDatetime { Value = June15 };
        var (button, _) = Stage(scene, clock, datetime);
        var modal = (AvonicModal)datetime.Parent!;
        modal.Breakpoints = [0, 1];
        modal.InitialBreakpoint = 1;
        Tap(scene, clock, DateButton(button));
        Assert.True(modal.IsShown);
        Assert.True(button.IsDatetimeActive);
        Assert.Equal(ExpandCollapseState.Expanded, Expanded(DateButton(button)));
    });

    [AvaloniaFact]
    public void B7_Removed_From_The_Window_It_Is_Collapsed() => Run((scene, clock) =>
    {
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, Place.Page);
        Assert.True(button.IsDatetimeActive);
        ((StackPanel)button.Parent!).Children.Remove(button);
        Assert.False(button.IsDatetimeActive);
        Assert.Equal(ExpandCollapseState.Collapsed, Expanded(DateButton(button)));
    });

    [AvaloniaFact]
    public void B7_Not_Shown_Without_A_Template_Or_Size() =>
        Assert.False(AvonicDatetimeButton.IsShown(new AvonicDatetime(), new Window()));

    // ── B8: the overlay ──────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(false, 35, 250, 320, 344)]
    [InlineData(true, 104.22, 322, 181.56, 200)]
    public void B8_The_Modal_Is_The_Datetimes_Size(bool time, double x, double y, double width, double height) => Run((scene, clock) =>
    {
        // modalDate, modalTime: fit-content, centred; a calendar 320 wide.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        var modal = (AvonicModal)datetime.Parent!;
        Assert.Contains(AvonicDatetimeButton.OverlayClass, modal.Classes);
        Tap(scene, clock, time ? TimeButton(button) : DateButton(button));
        Near([x, y, width, height], Box(datetime, scene.Window), 1);
        var wrapper = modal.GetVisualDescendants().OfType<Control>().First(c => c.Name == "PART_Content");
        Near([x, y, width, height], Box(wrapper, scene.Window), 1);
    });

    [AvaloniaFact]
    public void B8_An_Apps_Own_Size_On_The_Overlay_Wins() => Run((scene, clock) =>
    {
        var datetime = new AvonicDatetime { Value = June15 };
        var (button, _) = Stage(scene, clock, datetime);
        var modal = (AvonicModal)datetime.Parent!;
        modal.Resources["Avonic.Modal.Width"] = "300";
        Tap(scene, clock, DateButton(button));
        var wrapper = modal.GetVisualDescendants().OfType<Control>().First(c => c.Name == "PART_Content");
        Assert.Equal(300, wrapper.Bounds.Width, 0.5);
    });

    [AvaloniaFact]
    public void B8_Without_The_Class_A_Calendar_Is_Its_Own_Width() => Run((scene, clock) =>
    {
        // The rule needs the class: a datetime in a modal with no button keeps 350.
        var datetime = new AvonicDatetime { Value = June15, Locale = "en-US" };
        var modal = new AvonicModal { KeepContentsMounted = true, Content = datetime };
        scene.Window.Content = new StackPanel { Children = { modal } };
        Settle(clock, 400);
        _ = modal.Present(scene.Window);
        Settle(clock, 600);
        Assert.Equal(350, datetime.Bounds.Width, 0.5);
    });

    // ── B9: disabled ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_Disabled_Takes_No_Tap_And_Fades_Each_Button() => Run((scene, clock) =>
    {
        // disabled: pointer-events none, both buttons at 0.4.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, button: new AvonicDatetimeButton { IsEnabled = false });
        Tap(scene, clock, DateButton(button));
        Assert.False(((AvonicModal)datetime.Parent!).IsShown);
        Assert.Equal(DatetimePresentation.DateTime, datetime.Presentation);
        foreach (var native in new[] { DateButton(button), TimeButton(button) })
        {
            Assert.False(native.IsEffectivelyEnabled);
            Assert.Equal(0.4, ((ISolidColorBrush)native.OpacityMask!).Opacity, 3);
        }
    });

    // ── B10: the slots ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_The_Slots_Take_The_Place_Of_The_Text_In_A_Button_That_Shows() => Run((scene, clock) =>
    {
        // slots, slotsDateOnly: My date and My time; with a date presentation, no time button for My time.
        var mine = new TextBlock { Text = "My date" };
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15, Presentation = DatetimePresentation.Date },
            button: new AvonicDatetimeButton { DateTargetContent = mine, TimeTargetContent = new TextBlock { Text = "My time" } });
        Assert.True(mine.IsEffectivelyVisible);
        Assert.Same(DateButton(button), mine.FindAncestorOfType<DatetimeButtonNative>());
        var fallback = DateButton(button).GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "PART_Text");
        Assert.False(fallback.IsVisible);
        Assert.False(TimeButton(button).IsVisible);
        Assert.Equal("My date", ControlAutomationPeer.CreatePeerForElement(DateButton(button)).GetName());
    });

    // ── B11: the native buttons ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Each_Button_Has_A_Ripple_And_Is_Activatable() => Run((scene, clock) =>
    {
        // x_tapPress: ion-activatable, ion-activated while pressed, the ripple.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        foreach (var native in new[] { DateButton(button), TimeButton(button) })
        {
            Assert.True(Activatable.GetIsEnabled(native));
            Assert.Single(native.GetVisualDescendants().OfType<AvonicRippleEffect>());
        }
    });

    [AvaloniaFact]
    public void B11_Tab_Reaches_The_Date_Then_The_Time_And_Enter_And_Space_Click() => Run((scene, clock) =>
    {
        // keyboard, x_space: the date button, then the time button; Enter on the time opens the time; Space on the date, the date.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        var modal = (AvonicModal)datetime.Parent!;
        AvonicPickerColumnTests.Key(scene, PhysicalKey.Tab);
        Assert.Same(DateButton(button), scene.Window.FocusManager!.GetFocusedElement());
        AvonicPickerColumnTests.Key(scene, PhysicalKey.Tab);
        Assert.Same(TimeButton(button), scene.Window.FocusManager!.GetFocusedElement());
        AvonicPickerColumnTests.Key(scene, PhysicalKey.Enter);
        Settle(clock, 600);
        Assert.True(modal.IsShown);
        Assert.Equal(DatetimePresentation.Time, datetime.Presentation);
        _ = modal.Dismiss();
        Settle(clock, 800);
        DateButton(button).Focus(NavigationMethod.Tab);
        AvonicPickerColumnTests.Key(scene, PhysicalKey.Space);
        Settle(clock, 600);
        Assert.True(modal.IsShown);
        Assert.Equal(DatetimePresentation.Date, datetime.Presentation);
    });

    [AvaloniaFact]
    public void B11_The_Focus_Ring_Shows_For_The_Keyboard_Only() => Run((scene, clock) =>
    {
        // keyboard-focus: Chromium's ring on :focus-visible; none from a tap.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, Place.Page);
        var date = DateButton(button);
        var ring = date.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_FocusRing");
        var timeRing = TimeButton(button).GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_FocusRing");
        Tap(scene, clock, date);
        Assert.True(date.IsFocused);
        Assert.False(ring.IsVisible);
        AvonicPickerColumnTests.Key(scene, PhysicalKey.Tab);
        Assert.True(TimeButton(button).IsFocused);
        Assert.True(timeRing.IsVisible);
        Assert.False(ring.IsVisible);
        ring = timeRing;
        Assert.Equal(new Thickness(-1), ring.Margin);
        Assert.Equal(new Thickness(2), ring.BorderThickness);
        Assert.Equal(Color.Parse("#E59700"), ((ISolidColorBrush)ring.BorderBrush!).Color);
    });

    [AvaloniaFact]
    public void B11_Each_Button_Is_Named_By_Its_Text_And_Invoked_Or_Expanded_By_Automation() => Run((scene, clock) =>
    {
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        var peer = ControlAutomationPeer.CreatePeerForElement(TimeButton(button));
        Assert.Equal("1:47 PM", peer.GetName());
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        ((IExpandCollapseProvider)peer).Expand();
        Settle(clock, 600);
        Assert.True(((AvonicModal)datetime.Parent!).IsShown);
        Assert.Equal(DatetimePresentation.Time, datetime.Presentation);
    });

    [AvaloniaTheory]
    [InlineData(10, true)]
    [InlineData(20, false)]
    public void B11_A_Finger_That_Moves_15_Px_Or_More_Doesnt_Click(double drift, bool clicks) => Run((scene, clock) =>
    {
        // A native <button>'s click (NativeButton): a finger that moved 14 px clicks, 16 doesn't, though it ends on the button.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        var native = DateButton(button);
        var box = Box(native, scene.Window);
        var at = new Point(box.Center.X - 30, box.Center.Y);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var finger = scene.Window.TouchBegin(at, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        scene.Window.TouchMove(finger, at + new Point(drift, 0), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        scene.Window.TouchEnd(finger, at + new Point(drift, 0), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Settle(clock, 600);
        Assert.Equal(clicks, ((AvonicModal)datetime.Parent!).IsShown);
    });

    // ── B12: layout ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_The_Row_Wraps_And_Centres() => Run((scene, clock) =>
    {
        // narrow: 148 wide (a 150 box with a 1 px border), two lines, 62 high, each button centred in its line.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15 },
            around: new Border { Width = 150, BorderThickness = new Thickness(1), BorderBrush = Brushes.LightGray, HorizontalAlignment = HorizontalAlignment.Left });
        Near([17, 17, 148, 62], Box(button, scene.Window), 1);
        Near([33.23, 17, 115.53, 31], Box(DateButton(button), scene.Window), 1);
        Near([49.55, 48, 82.89, 31], Box(TimeButton(button), scene.Window), 1);
    });

    [AvaloniaFact]
    public void B12_Right_To_Left_Puts_The_Date_On_The_Right() => Run((scene, clock) =>
    {
        // x_rtl: the date at 180.69, the time at 93.8.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, around: new Border { FlowDirection = FlowDirection.RightToLeft });
        Near([180.69, 16, 115.53, 31], Box(DateButton(button), scene.Window), 1);
        Near([93.8, 16, 82.89, 31], Box(TimeButton(button), scene.Window), 1);
    });

    // ── B13: ADR 0005 ────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(DatetimePresentation.Month, "Jul")]
    [InlineData(DatetimePresentation.Year, "24")]
    public void B13_Month_And_Year_Are_Formatted_By_The_Date_Options(DatetimePresentation presentation, string text) => Run((scene, clock) =>
    {
        // x_fo0_month, x_fo2_year: Ionic reads formatOptions.time there (01:47 PM, and 2024 with the date's ignored).
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Presentation = presentation });
        datetime.FormatOptions = new DatetimeFormatOptions
        {
            Date = presentation == DatetimePresentation.Month
                ? new DateTimeFormatOptions { Month = DateTimeFormatMonth.Short }
                : new DateTimeFormatOptions { Year = DateTimeFormatNumber.TwoDigit },
            Time = new DateTimeFormatOptions { Hour = DateTimeFormatNumber.TwoDigit, Minute = DateTimeFormatNumber.TwoDigit },
        };
        datetime.Value = "2024-07-15T13:47:00";
        Assert.Equal((text, null), Texts(button));
    });

    // ── B14: a popover's datetime never shown ────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_A_Popovers_Datetime_Never_Shown_Still_Opens() => Run((scene, clock) =>
    {
        // Without a template the datetime still raises Rendered after a new presentation, so the wait ends.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, Place.Popover);
        Assert.Null(datetime.CalendarBody);
        Tap(scene, clock, DateButton(button));
        Assert.True(((AvonicPopover)datetime.Parent!).IsShown);
    });

    // ── B15: the colour ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_The_Active_Colour_Is_The_Colours_Base() => Run((scene, clock) =>
    {
        // x_colorActive: danger's rgb(197, 0, 15); the other button black.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15 }, Place.Page, new AvonicDatetimeButton { Color = "danger" });
        NoTransitions(button);
        Assert.Equal(Color.FromRgb(197, 0, 15), TextColor(DateButton(button)));
        Assert.Equal(Black, TextColor(TimeButton(button)));
    });

    [AvaloniaFact]
    public void B15_The_Colour_Eases_Over_150_Ms() => Run((scene, clock) =>
    {
        // x_firstColor: transition: 150ms color ease-in-out, from black even on the first frame.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        var transition = Assert.IsType<BrushTransition>(Assert.Single(DateButton(button).Transitions!));
        Assert.Equal(TimeSpan.FromMilliseconds(150), transition.Duration);
        Assert.Equal(TemplatedControl.ForegroundProperty, transition.Property);
        Assert.Equal(0.5, transition.Easing.Ease(0.5), 3);
        Assert.Equal(0.0817, transition.Easing.Ease(0.2), 3);
    });
}
