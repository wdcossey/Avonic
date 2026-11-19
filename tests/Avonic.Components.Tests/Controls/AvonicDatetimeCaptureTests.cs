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

    internal static void Near(double[] expected, Rect actual, double tolerance = 0.5)
    {
        var got = new[] { actual.X, actual.Y, actual.Width, actual.Height };
        for (var i = 0; i < 4; i++)
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
    });
}
