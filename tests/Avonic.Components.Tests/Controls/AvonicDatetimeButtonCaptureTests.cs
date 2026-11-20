using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicDatetimeButtonTests;
using static Avonic.Components.Tests.Controls.AvonicDatetimeCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames and boxes for the compare step, beside Ionic's in docs/components/ion-datetime-button/ref/frames/, on the same phone
/// (390 x 844 at 2x), the button 16 in from the top left, the clock at 2023-06-20 10:00 and the locale en-US: the cases of
/// <c>ref/behaviour.json</c>. With <c>AVONIC_CAPTURE_DIR</c> set the frames are saved.
/// </summary>
public class AvonicDatetimeButtonCaptureTests
{
    private static void Tap(Scene scene, TestClock clock, Visual target)
    {
        AvonicPickerColumnTests.Click(scene, AvonicPickerColumnTests.Centre(target, scene));
        Settle(clock, 800);
    }

    [AvaloniaFact]
    public void Reference_DateTime() => Run((scene, clock) =>
    {
        // dateTime: the host 358 x 31 at 16, 16; the date 115.53 wide at 93.78, the time 82.89 at 213.31.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15 });
        Save(scene, "avonic-datetime-button-page.png");
        Near([16, 16, 358, 31], Box(button, scene.Window));
        Near([93.78, 16, 115.53, 31], Box(DateButton(button), scene.Window), 1);
        Near([213.31, 16, 82.89, 31], Box(TimeButton(button), scene.Window), 1);
    });

    [AvaloniaTheory]
    [InlineData(DatetimePresentation.Date, false, "date", 137.23, 115.53)]
    [InlineData(DatetimePresentation.Time, false, "time", 153.55, 82.89)]
    [InlineData(DatetimePresentation.MonthYear, false, "monthYear", -1, -1)]
    [InlineData(DatetimePresentation.DateTime, true, "preferWheel", -1, -1)]
    public void Reference_Presentations(DatetimePresentation presentation, bool preferWheel, string name, double x, double width) => Run((scene, clock) =>
    {
        // date, time: one button, centred.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15, Presentation = presentation, PreferWheel = preferWheel });
        Save(scene, $"avonic-datetime-button-{name}.png");
        if (x >= 0)
        {
            var native = presentation == DatetimePresentation.Time ? TimeButton(button) : DateButton(button);
            Near([x, 16, width, 31], Box(native, scene.Window), 1);
        }
    });

    [AvaloniaFact]
    public void Reference_Disabled() => Run((scene, clock) =>
    {
        Stage(scene, clock, new AvonicDatetime { Value = June15 }, button: new AvonicDatetimeButton { IsEnabled = false });
        Save(scene, "avonic-datetime-button-disabled.png");
    });

    [AvaloniaFact]
    public void Reference_Slots() => Run((scene, clock) =>
    {
        // slots: My date 80.95 wide at 111.94, My time 81.16 at 196.89.
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15 },
            button: new AvonicDatetimeButton { DateTargetContent = new TextBlock { Text = "My date" }, TimeTargetContent = new TextBlock { Text = "My time" } });
        Save(scene, "avonic-datetime-button-slots.png");
        Near([111.94, 16, 80.95, 31], Box(DateButton(button), scene.Window), 1);
        Near([196.89, 16, 81.16, 31], Box(TimeButton(button), scene.Window), 1);
    });

    [AvaloniaFact]
    public void Reference_Narrow() => Run((scene, clock) =>
    {
        Stage(scene, clock, new AvonicDatetime { Value = June15 },
            around: new Border { Width = 150, BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")), HorizontalAlignment = HorizontalAlignment.Left });
        Save(scene, "avonic-datetime-button-narrow.png");
    });

    [AvaloniaTheory]
    [InlineData(DatetimePresentation.DateTime, "inline")]
    [InlineData(DatetimePresentation.TimeDate, "inlineTimeDate")]
    public void Reference_Inline(DatetimePresentation presentation, string name) => Run((scene, clock) =>
    {
        // inline: the datetime under the button, 16 + 31 down, the presentation's button active.
        var (_, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15, Presentation = presentation }, Place.Page);
        Settle(clock, 400);
        Save(scene, $"avonic-datetime-button-{name}.png");
        Near([16, 47, 350, 391], Box(datetime, scene.Window), 1);
    });

    [AvaloniaFact]
    public void Reference_ColorActive() => Run((scene, clock) =>
    {
        Stage(scene, clock, new AvonicDatetime { Value = June15 }, Place.Page, new AvonicDatetimeButton { Color = "danger" });
        Settle(clock, 400);
        Save(scene, "avonic-datetime-button-colorActive.png");
    });

    [AvaloniaTheory]
    [InlineData(Place.Modal, false, DatetimePresentation.DateTime, "modalDate")]
    [InlineData(Place.Modal, true, DatetimePresentation.DateTime, "modalTime")]
    [InlineData(Place.Popover, false, DatetimePresentation.DateTime, "popoverDate")]
    [InlineData(Place.Popover, true, DatetimePresentation.DateTime, "popoverTime")]
    [InlineData(Place.Popover, true, DatetimePresentation.Time, "popoverTimeOnly")]
    public void Reference_Opened(Place place, bool time, DatetimePresentation presentation, string name) => Run((scene, clock) =>
    {
        var (button, _) = Stage(scene, clock, new AvonicDatetime { Value = June15, Presentation = presentation }, place);
        Tap(scene, clock, time ? TimeButton(button) : DateButton(button));
        Save(scene, $"avonic-datetime-button-{name}-open.png");
    });

    [AvaloniaFact]
    public void Reference_WheelOpened() => Run((scene, clock) =>
    {
        // modalWheel: 350 x 200 at 20, 322.
        var (button, datetime) = Stage(scene, clock, new AvonicDatetime { Value = June15, PreferWheel = true });
        Tap(scene, clock, DateButton(button));
        Save(scene, "avonic-datetime-button-modalWheel-open.png");
        Near([20, 322, 350, 200], Box(datetime, scene.Window), 1);
    });

    [AvaloniaFact]
    public void Reference_KeyboardFocus() => Run((scene, clock) =>
    {
        Stage(scene, clock, new AvonicDatetime { Value = June15 });
        AvonicPickerColumnTests.Key(scene, PhysicalKey.Tab);
        Save(scene, "avonic-datetime-button-keyboard-focus.png");
    });

    [AvaloniaFact]
    public void Reference_Locales() => Run((scene, clock) =>
    {
        // locales: seven buttons, one under the other.
        var page = new StackPanel { Margin = new Thickness(16) };
        var scope = new NameScope();
        NameScope.SetNameScope(page, scope);
        var cases = new (string Locale, DatetimeHourCycle? Cycle)[] { ("ja-JP", null), ("en-GB", null), ("de-DE", null), ("ko-KR", null), ("ar-EG", null), ("fr-FR", null), ("en-US", DatetimeHourCycle.H23) };
        for (var i = 0; i < cases.Length; i++)
        {
            var datetime = new AvonicDatetime { Value = June15, Locale = cases[i].Locale, HourCycle = cases[i].Cycle };
            scope.Register($"dt{i}", datetime);
            page.Children.Add(new AvonicDatetimeButton { Datetime = $"dt{i}" });
            page.Children.Add(new AvonicModal { KeepContentsMounted = true, Content = datetime });
        }

        scene.Window.Content = page;
        Settle(clock, 400);
        Save(scene, "avonic-datetime-button-locales.png");
    });
}
