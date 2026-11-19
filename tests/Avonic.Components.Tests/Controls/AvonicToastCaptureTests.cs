using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames and boxes for the compare step, beside Ionic's in docs/components/ion-toast/ref/, which are measured on the same
/// phone (390 x 844) and a 1024 x 768 tablet: <c>ref/layout.json</c>. With <c>AVONIC_CAPTURE_DIR</c> set the frames are saved. The
/// base theme is there for the overlay layer.
/// </summary>
public class AvonicToastCaptureTests
{
    /// <summary>Ionic's page under the toast: a heading, a paragraph, a button, and a button at 8, 400.</summary>
    internal static Control Page() => new Canvas
    {
        Children =
        {
            new StackPanel
            {
                Margin = new Thickness(16),
                Children =
                {
                    new TextBlock { Text = "Toasts", FontSize = 32, FontWeight = FontWeight.Medium, Margin = new Thickness(0, 21, 0, 10) },
                    new TextBlock { Text = "The page under the toast.", FontSize = 16, Margin = new Thickness(0, 0, 0, 16) },
                    new Button { Content = "Open", FontSize = 16, Padding = new Thickness(16, 8), HorizontalAlignment = HorizontalAlignment.Left },
                },
            },
            new Button { Content = "Under", Width = 120, Height = 40, [Canvas.LeftProperty] = 8.0, [Canvas.TopProperty] = 400.0 },
        },
    };

    private static void Run(Action<Scene, TestClock> test, double width = 390, double height = 844, Control? page = null) =>
        AvonicAlertCaptureTests.Run((scene, clock) =>
        {
            scene.Window.Content = page ?? Page();
            Dispatcher.UIThread.RunJobs();
            test(scene, clock);
        }, width, height);

    private static void Save(Scene scene, string name)
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        Frames.Save(scene.Window.CaptureRenderedFrame()!, name);
    }

    private static AvonicToast Present(Scene scene, TestClock clock, AvonicToast toast)
    {
        toast.Present(scene.Window);
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 500);
        Dispatcher.UIThread.RunJobs();
        return toast;
    }

    /// <summary>A box in the window, with its render transform, as <c>getBoundingClientRect</c> reads it.</summary>
    internal static Rect Box(Visual visual, Visual window) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(window)!.Value);

    private static void Near(double[] expected, Rect actual, double tolerance = 0.5)
    {
        var got = new[] { actual.X, actual.Y, actual.Width, actual.Height };
        for (var i = 0; i < 4; i++)
        {
            Assert.True(Math.Abs(expected[i] - got[i]) <= tolerance,
                $"Expected [{string.Join(", ", expected)}], got [{string.Join(", ", got.Select(v => Math.Round(v, 2)))}]");
        }
    }

    internal static T Part<T>(AvonicToast toast, string name) where T : Control =>
        toast.GetVisualDescendants().OfType<T>().First(c => c.Name == name && ReferenceEquals(c.TemplatedParent, toast));

    private static ToastButton[] UndoDismiss() => [new() { Text = "Undo" }, new() { Text = "Dismiss", Role = "cancel" }];

    [AvaloniaFact]
    public void Reference_Basic() => Run((scene, clock) =>
    {
        var toast = Present(scene, clock, new AvonicToast { Message = "Hello World" });
        Save(scene, "avonic-toast-page.png");

        // M4, ref/layout.json "basic".
        Near([8, 788, 374, 48], Box(toast.Wrapper!, scene.Window));
        Near([24, 802, 342, 20], Box(Part<TextBlock>(toast, "PART_Message"), scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Header() => Run((scene, clock) =>
    {
        var toast = Present(scene, clock, new AvonicToast { Header = "Toast header", Message = "Toast message", Icon = "globe", Buttons = UndoDismiss() });
        Save(scene, "avonic-toast-header.png");

        // M5, "header".
        Near([8, 766, 374, 70], Box(toast.Wrapper!, scene.Window));
        Near([24, 791.2, 19.6, 19.6], Box(Part<AvonicIcon>(toast, "PART_Icon"), scene.Window));
        Near([43.59, 766, 168.53, 70], Box(toast.Content!, scene.Window), 1.1);
        Near([59.59, 780, 136.53, 20], Box(Part<TextBlock>(toast, "PART_Header"), scene.Window), 1.1);
        Near([59.59, 802, 136.53, 20], Box(Part<TextBlock>(toast, "PART_Message"), scene.Window), 1.1);
        Near([212.13, 783, 71.25, 36], Box(toast.EndButtonControls[0], scene.Window), 1);
        Near([283.38, 783, 90.63, 36], Box(toast.EndButtonControls[1], scene.Window), 1);
    });

    [AvaloniaFact]
    public void Reference_Stacked() => Run((scene, clock) =>
    {
        var toast = Present(scene, clock, new AvonicToast
        {
            Header = "Stacked", Message = "A longer message that wraps onto a second line in the toast.", Layout = ToastLayout.Stacked, Buttons = UndoDismiss(),
        });
        Save(scene, "avonic-toast-stacked.png");

        // M6, "stacked".
        Near([8, 702, 374, 134], Box(toast.Wrapper!, scene.Window));
        Near([8, 702, 374, 90], Box(toast.Content!, scene.Window));
        Near([212.13, 792, 71.25, 36], Box(toast.EndButtonControls[0], scene.Window), 1);
        Near([283.38, 792, 90.63, 36], Box(toast.EndButtonControls[1], scene.Window), 1);
    });

    [AvaloniaFact]
    public void Reference_Top_And_Middle() => Run((scene, clock) =>
    {
        var top = Present(scene, clock, new AvonicToast { Message = "At the top", Position = ToastPosition.Top });
        Save(scene, "avonic-toast-top.png");
        Near([8, 8, 374, 48], Box(top.Wrapper!, scene.Window));
        top.Dismiss();
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 400);

        var middle = Present(scene, clock, new AvonicToast { Message = "In the middle", Position = ToastPosition.Middle });
        Save(scene, "avonic-toast-middle.png");
        Near([8, 398, 374, 48], Box(middle.Wrapper!, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Icon_Only() => Run((scene, clock) =>
    {
        var toast = Present(scene, clock, new AvonicToast
        {
            Message = "Icon-only button",
            Buttons = [new ToastButton { Icon = "close", Role = "cancel", HtmlAttributes = new Dictionary<string, object?> { ["aria-label"] = "Close the toast" } }],
        });
        Save(scene, "avonic-toast-icon-only.png");

        // M8, "iconOnly".
        Near([8, 788, 374, 48], Box(toast.Wrapper!, scene.Window));
        Near([338, 794, 36, 36], Box(toast.EndButtonControls[0], scene.Window));
        Near([8, 788, 330, 48], Box(toast.Content!, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Colors() => Run((scene, clock) =>
    {
        var primary = Present(scene, clock, new AvonicToast { Message = "Primary", Color = "primary", Buttons = UndoDismiss() });
        Save(scene, "avonic-toast-primary.png");
        Near([212.13, 794, 71.25, 36], Box(primary.EndButtonControls[0], scene.Window), 1);
        primary.Dismiss();
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 400);

        var danger = Present(scene, clock, new AvonicToast { Message = "Danger", Color = "danger", Icon = "alert-circle", Buttons = ["Retry"] });
        Save(scene, "avonic-toast-danger.png");
        Near([297.38, 794, 76.63, 36], Box(danger.EndButtonControls[0], scene.Window), 1);
    });

    [AvaloniaFact]
    public void Reference_Start_Buttons() => Run((scene, clock) =>
    {
        var toast = Present(scene, clock, new AvonicToast
        {
            Message = "Start buttons", Buttons = [new ToastButton { Text = "Left", Side = ToastButtonSide.Start }, "Right"],
        });
        Save(scene, "avonic-toast-start.png");

        // M10, "start".
        Near([16, 794, 65.22, 36], Box(toast.StartButtonControls[0], scene.Window), 1);
        Near([81.22, 788, 218, 48], Box(toast.Content!, scene.Window), 1);
        Near([299.22, 794, 74.78, 36], Box(toast.EndButtonControls[0], scene.Window), 1);
        toast.Dismiss();
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 400);

        var stacked = Present(scene, clock, new AvonicToast
        {
            Message = "Stacked, start buttons", Layout = ToastLayout.Stacked, Buttons = [new ToastButton { Text = "Left", Side = ToastButtonSide.Start }],
        });
        Save(scene, "avonic-toast-stacked-start.png");

        // "stackedStart".
        Near([8, 744, 374, 92], Box(stacked.Wrapper!, scene.Window));
        Near([308.78, 752, 65.22, 36], Box(stacked.StartButtonControls[0], scene.Window), 1);
        Near([8, 788, 374, 48], Box(stacked.Content!, scene.Window));
    });

    [AvaloniaFact]
    public void Reference_Long() => Run((scene, clock) =>
    {
        var toast = Present(scene, clock, new AvonicToast
        {
            Message = "A long message that wraps over several lines, so that the toast grows taller, and the buttons stay centred beside it, as the container aligns its items to the centre.",
            Buttons = ["OK"],
        });
        Save(scene, "avonic-toast-long.png");

        // M11, "long".
        Near([8, 728, 374, 108], Box(toast.Wrapper!, scene.Window));
        Near([323.83, 764, 50.17, 36], Box(toast.EndButtonControls[0], scene.Window), 1);
    });

    [AvaloniaFact]
    public void Reference_Tablet() => Run((scene, clock) =>
    {
        var toast = Present(scene, clock, new AvonicToast { Header = "Toast header", Message = "Toast message", Icon = "globe", Buttons = UndoDismiss() });
        Save(scene, "avonic-toast-tablet.png");

        // M13, "tabletHeader".
        Near([162, 690, 700, 70], Box(toast.Wrapper!, scene.Window));
        Near([692.13, 707, 71.25, 36], Box(toast.EndButtonControls[0], scene.Window), 1);
    }, 1024, 768);

    [AvaloniaFact]
    public void Reference_Rtl() => Run((scene, clock) =>
    {
        scene.Window.FlowDirection = FlowDirection.RightToLeft;
        var toast = new AvonicToast
        {
            Message = "RTL", Icon = "globe", Buttons = [new ToastButton { Text = "Start", Side = ToastButtonSide.Start }, "End"],
        };
        toast.Resources["Avonic.Toast.Start"] = 20.0;
        toast.Resources["Avonic.Toast.End"] = 40.0;
        Present(scene, clock, toast);
        Save(scene, "avonic-toast-rtl.png");

        // M17, "rtl".
        Near([40, 788, 330, 48], Box(toast.Wrapper!, scene.Window));
        Near([285.27, 794, 76.73, 36], Box(toast.StartButtonControls[0], scene.Window), 1);
        Near([48, 794, 59.52, 36], Box(toast.EndButtonControls[0], scene.Window), 1);
    });
}
