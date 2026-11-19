using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames and boxes for the compare step, beside Ionic's in docs/components/ion-datetime/ref/, measured on the same phone
/// (390 x 844 at 2x) with the datetime 16 in from the top left, the clock at 2023-06-20 10:00 and the locale en-US:
/// <c>ref/layout.json</c> and <c>ref/locales.json</c>. With <c>AVONIC_CAPTURE_DIR</c> set the frames are saved.
/// </summary>
public class AvonicDatetimeCaptureTests
{
    internal static readonly DateTime Today = new(2023, 6, 20, 10, 0, 0);

    internal static void Run(Action<Scene, TestClock> test, double width = 390, double height = 844, string culture = "en-US") =>
        AvonicAlertCaptureTests.Run((scene, clock) =>
        {
            var previousNow = AvonicDatetime.Now;
            var previousCulture = CultureInfo.CurrentCulture;
            AvonicDatetime.Now = () => Today;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            try
            {
                test(scene, clock);
            }
            finally
            {
                AvonicDatetime.Now = previousNow;
                CultureInfo.CurrentCulture = previousCulture;
            }
        }, width, height);

    /// <summary>The datetime 16 in, as Ionic's stage pads it.</summary>
    internal static AvonicDatetime Show(Scene scene, TestClock clock, AvonicDatetime datetime, FlowDirection direction = FlowDirection.LeftToRight)
    {
        scene.Window.Content = new StackPanel { Margin = new Thickness(16), FlowDirection = direction, Children = { datetime } };
        Settle(clock, 400);
        return datetime;
    }

    internal static void Settle(TestClock clock, double ms)
    {
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + ms);
        Dispatcher.UIThread.RunJobs();
    }

    internal static void Save(Scene scene, string name)
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        Frames.Save(scene.Window.CaptureRenderedFrame()!, name);
    }

    internal static Rect Box(Visual visual, Visual relativeTo) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(relativeTo)!.Value);

    internal static void Near(double[] expected, Rect actual, double tolerance = 0.5) =>
        Near(expected, [actual.X, actual.Y, actual.Width, actual.Height], tolerance);

    internal static void Near(double[] expected, double[] got, double tolerance = 0.5)
    {
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs(expected[i] - got[i]) <= tolerance,
                $"Expected [{string.Join(", ", expected)}], got [{string.Join(", ", got.Select(v => Math.Round(v, 2)))}]");
        }
    }

    internal static T Part<T>(AvonicDatetime datetime, string name) where T : Control =>
        datetime.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    internal static void Dump(Scene scene, AvonicDatetime datetime, string name)
    {
        var dir = Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR");
        if (dir is null) return;
        var lines = datetime.GetVisualDescendants().OfType<Control>().Where(c => !string.IsNullOrEmpty(c.Name) && c.IsEffectivelyVisible && c.FindAncestorOfType<DatetimeCalendarDay>() is null && c.FindAncestorOfType<AvonicButton>() is null)
            .Select(c => { var b = Box(c, datetime); return $"{c.Name} {Math.Round(b.X,2)} {Math.Round(b.Y,2)} {Math.Round(b.Width,2)} {Math.Round(b.Height,2)}"; });
        File.WriteAllLines(Path.Combine(dir, name + ".txt"), lines);
    }

    [AvaloniaFact]
    public void Reference_DateTime() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-page.png");
        Dump(scene, datetime, "dateTime");
        Near([16, 16, 350, 391], Box(datetime, scene.Window));
        // M4: June 1 at 201.13, 88 (the calendar at 84, its padding 4 and 10, columns 47.14 apart), 42 x 42.
        var first = datetime.CalendarMonths!.Months[1].Days.First(d => d.IsVisible && !d.IsPadding);
        Near([201.13, 88, 42, 42], Box(first, datetime));
    });

    [AvaloniaFact]
    public void Reference_Title() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = "2023-06-15T13:47:00", ShowDefaultTitle = true, ShowDefaultButtons = true, ShowClearButton = true });
        Save(scene, "avonic-datetime-title.png");
        Dump(scene, datetime, "title");
        Near([16, 16, 350, 567], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_TitleSlot() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = "2023-06-15T13:47:00", TitleContent = "Pick a day" });
        Save(scene, "avonic-datetime-titleSlot.png");
        Dump(scene, datetime, "titleSlot");
        Near([16, 16, 350, 515], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Date() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-date.png");
        Dump(scene, datetime, "date");
        Near([16, 16, 350, 344], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_TimeDate() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.TimeDate, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-timeDate.png");
        Dump(scene, datetime, "timeDate");
        Near([16, 16, 350, 391], Box(datetime, scene.Window));
        // time-date: the time row first, then the calendar.
        Assert.True(Box(Part<Control>(datetime, "PART_Time"), datetime).Bottom <= Box(datetime.CalendarBody!, datetime).Top);
    });

    [AvaloniaFact]
    public void Reference_Time() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Time, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-time.png");
        Dump(scene, datetime, "time");
        Near([16, 16, 350, 200], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Month() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Month, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-month.png");
        Dump(scene, datetime, "month");
        Near([16, 16, 350, 200], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Year() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Year, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-year.png");
        Dump(scene, datetime, "year");
        Near([16, 16, 350, 200], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_MonthYear() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.MonthYear, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-monthYear.png");
        Dump(scene, datetime, "monthYear");
        Near([16, 16, 350, 200], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_WheelDate() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, PreferWheel = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-wheelDate.png");
        Dump(scene, datetime, "wheelDate");
        Near([16, 16, 350, 200], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_WheelDateTime() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.DateTime, PreferWheel = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-wheelDateTime.png");
        Dump(scene, datetime, "wheelDateTime");
        Near([16, 16, 350, 200], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Buttons() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, ShowDefaultButtons = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-buttons.png");
        Dump(scene, datetime, "buttons");
        Near([16, 16, 350, 396], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_MinMax() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Min = "2023-06-10", Max = "2023-07-20", Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-minMax.png");
        Dump(scene, datetime, "minMax");
        Near([16, 16, 350, 344], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Adjacent() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, ShowAdjacentDays = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-adjacent.png");
        Dump(scene, datetime, "adjacent");
        Near([16, 16, 350, 344], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_FirstDay() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, FirstDayOfWeek = 1, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-firstDay.png");
        Dump(scene, datetime, "firstDay");
        Near([16, 16, 350, 344], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Color() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Color = "danger", ShowDefaultTitle = true, ShowDefaultButtons = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-color.png");
        Dump(scene, datetime, "color");
        Near([16, 16, 350, 520], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Disabled() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { IsEnabled = false, ShowDefaultButtons = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-disabled.png");
        Dump(scene, datetime, "disabled");
        Near([16, 16, 350, 443], Box(datetime, scene.Window));
        // opacity: 0.4 of each day as a whole: a mask, not Opacity, which would fade its text and its circle each on its own.
        var day = datetime.CalendarMonths!.Months[1].Days.First(d => d.IsVisible && !d.IsPadding);
        Assert.Equal(0.4, day.OpacityMask!.Opacity, 3);
        Assert.Equal(1, day.Opacity);
    });

    [AvaloniaFact]
    public void Reference_Readonly() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Readonly = true, ShowDefaultButtons = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-readonly.png");
        Dump(scene, datetime, "readonly");
        Near([16, 16, 350, 443], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Cover() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Size = DatetimeSize.Cover, Presentation = DatetimePresentation.Date, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-cover.png");
        Dump(scene, datetime, "cover");
        Near([16, 16, 358, 344], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Hour24() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { HourCycle = DatetimeHourCycle.H23, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-hour24.png");
        Dump(scene, datetime, "hour24");
        Near([16, 16, 350, 391], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_MultipleNoValue() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Multiple = true, ShowDefaultTitle = true });
        Save(scene, "avonic-datetime-multipleNoValue.png");
        Dump(scene, datetime, "multipleNoValue");
        Near([16, 16, 350, 468], Box(datetime, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_MonthYearOpen() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Presentation = DatetimePresentation.Date, Value = "2023-06-15T13:47:00" });
        Part<Button>(datetime, "PART_MonthYearButton").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Settle(clock, 600);
        Save(scene, "avonic-datetime-monthYearOpen.png");
        Dump(scene, datetime, "monthYearOpen");
        // M7: the header 48, the wheel 350 x 200 under it, the host 248.
        Near([16, 16, 350, 248], Box(datetime, scene.Window));
        Near([0, 48, 350, 200], Box(datetime.MonthYearWheel!, datetime));
    });

    [AvaloniaFact]
    public void Reference_TimePopover() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = "2023-06-15T13:47:00" });
        Part<Button>(datetime, "PART_TimeButton").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Settle(clock, 800);
        Save(scene, "avonic-datetime-timePopover.png");
        // M9: the button at (267.1, 368, 82.9, 31) in the window; the popover's content 250 x 200 with its top 10 above the button's bottom.
        var button = Part<Button>(datetime, "PART_TimeButton");
        Near([267.1, 368, 82.9, 31], Box(button, scene.Window), 1);
        var content = datetime.TimeWheel!;
        Near([128, 389, 250, 200], Box(content, scene.Window), 1);
    });

    [AvaloniaFact]
    public void Reference_LocaleArRtl() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "ar-EG", ShowDefaultTitle = true, Value = "2023-06-15T13:47:00" }, FlowDirection.RightToLeft);
        Save(scene, "avonic-datetime-localeArRtl.png");
        Dump(scene, datetime, "localeArRtl");
        Assert.Equal("يونيو ٢٠٢٣", Part<TextBlock>(datetime, "PART_MonthYearText").Text);
        // The host 24 in from the left: the right edge of its line in RTL. Its height isn't Chromium's 515: Roboto has no Arabic,
        // and the fallback font's lines are taller in Avalonia than in Chromium (COMPARISON.md).
        var box = Box(datetime, scene.Window);
        Near([24, 16, 350], [box.X, box.Y, box.Width]);
    });

    [AvaloniaFact]
    public void Reference_LocaleArWheel() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "ar-EG", Presentation = DatetimePresentation.DateTime, PreferWheel = true, Value = "2023-06-15T13:47:00" }, FlowDirection.RightToLeft);
        Save(scene, "avonic-datetime-localeArWheel.png");
    });

    [AvaloniaFact]
    public void Reference_LocaleJa() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "ja-JP", ShowDefaultTitle = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-localeJa.png");
        Assert.Equal("2023年6月", Part<TextBlock>(datetime, "PART_MonthYearText").Text);
    });

    [AvaloniaFact]
    public void Reference_LocaleJaWheel() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "ja-JP", Presentation = DatetimePresentation.Date, PreferWheel = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-localeJaWheel.png");
        // Year first in Japanese (wheel-order-year-first).
        Assert.Equal(["Select a year", "Select a month", "Select a day"], datetime.Wheel!.Items.OfType<AvonicPickerColumn>().Select(c => Avalonia.Automation.AutomationProperties.GetName(c)));
    });

    [AvaloniaFact]
    public void Reference_LocaleEnGb() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "en-GB", Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-localeEnGb.png");
        Assert.Equal("13:47", Part<TextBlock>(datetime, "PART_TimeText").Text);
    });

    [AvaloniaFact]
    public void Reference_LocaleEnGbWheel() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "en-GB", Presentation = DatetimePresentation.Date, PreferWheel = true, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-localeEnGbWheel.png");
        Assert.Equal(["Select a day", "Select a month", "Select a year"], datetime.Wheel!.Items.OfType<AvonicPickerColumn>().Select(c => Avalonia.Automation.AutomationProperties.GetName(c)));
    });

    [AvaloniaFact]
    public void Reference_LocaleDe() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "de-DE", ShowDefaultTitle = true, FirstDayOfWeek = 1, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-localeDe.png");
    });

    [AvaloniaFact]
    public void Reference_LocaleKoTime() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Locale = "ko-KR", Presentation = DatetimePresentation.Time, Value = "2023-06-15T13:47:00" });
        Save(scene, "avonic-datetime-localeKoTime.png");
        // The day period first in Korean.
        Assert.Equal("Select a day period", Avalonia.Automation.AutomationProperties.GetName(datetime.Wheel!.Items.OfType<AvonicPickerColumn>().First()));
    });
}
