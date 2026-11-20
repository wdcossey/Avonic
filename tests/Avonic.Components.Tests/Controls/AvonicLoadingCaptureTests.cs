using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames and boxes for the compare step, beside Ionic's in docs/components/ion-loading/ref/, measured on the same phone
/// (390 x 844 at 2x): <c>ref/measure.json</c>. Ionic's spinners were paused at 0 for its frames, so these are too. With
/// <c>AVONIC_CAPTURE_DIR</c> set the frames are saved. The base theme is there for the overlay layer.
/// </summary>
public class AvonicLoadingCaptureTests
{
    /// <summary>The measured page under the loading: a toolbar, a heading, a paragraph and a button.</summary>
    private static Control Page() => new DockPanel
    {
        Children =
        {
            new AvonicHeader { [DockPanel.DockProperty] = Dock.Top, Items = { new AvonicToolbar { Content = new AvonicTitle { Content = "Loading" } } } },
            new StackPanel
            {
                Margin = new Thickness(16),
                Children =
                {
                    new TextBlock { Text = "The page", FontSize = 32, FontWeight = FontWeight.Medium, Margin = new Thickness(0, 21, 0, 10) },
                    new TextBlock { Text = "Under the loading indicator.", FontSize = 16, Margin = new Thickness(0, 0, 0, 16) },
                    new AvonicButton { Content = "Under", HorizontalAlignment = HorizontalAlignment.Left },
                },
            },
        },
    };

    private static void Run(Action<Scene, TestClock> test, FlowDirection direction = FlowDirection.LeftToRight) =>
        AvonicAlertCaptureTests.Run((scene, clock) =>
        {
            // ref/: every infinite animation paused at its start. The application's styles, as the overlay host isn't in the
            // window's logical tree.
            var paused = Scene.AppStyle<AvonicSpinner>(AvonicSpinner.PausedProperty, true);
            Application.Current!.Styles.Add(paused);
            try
            {
                scene.Window.FlowDirection = direction;
                scene.Window.Content = Page();
                Dispatcher.UIThread.RunJobs();
                test(scene, clock);
            }
            finally
            {
                Application.Current.Styles.Remove(paused);
            }
        });

    private static void Save(Scene scene, string name)
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        Frames.Save(scene.Window.CaptureRenderedFrame()!, name);
    }

    private static AvonicLoading Present(Scene scene, TestClock clock, AvonicLoading loading)
    {
        loading.Present(scene.Window);
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 300);
        Dispatcher.UIThread.RunJobs();
        return loading;
    }

    private static Rect Box(Visual visual, Scene scene) => new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(scene.Window)!.Value);

    /// <summary>Within 0.5 at 2x, where Avalonia rounds the layout to half pixels.</summary>
    private static void Near(double[] expected, Rect actual, double tolerance = 0.5)
    {
        var got = new[] { actual.X, actual.Y, actual.Width, actual.Height };
        for (var i = 0; i < 4; i++)
        {
            Assert.True(Math.Abs(expected[i] - got[i]) <= tolerance,
                $"Expected [{string.Join(", ", expected)}], got [{string.Join(", ", got.Select(v => Math.Round(v, 2)))}]");
        }
    }

    private static Border Content(AvonicLoading loading) =>
        loading.GetVisualDescendants().OfType<Border>().First(c => c.Name == "PART_Content");

    [AvaloniaFact]
    public void Reference_Basic() => Run((scene, clock) =>
    {
        var loading = Present(scene, clock, new AvonicLoading { Message = "Loading..." });
        Save(scene, "avonic-ion-loading-page.png");

        // M4.
        Near([118.44, 382.5, 153.11, 79], Box(loading.Wrapper!, scene));
        Near([142.44, 406.5, 28, 28], Box(loading.SpinnerPart!, scene));
        Near([186.44, 414, 61.11, 16], Box(Content(loading), scene));
    });

    [AvaloniaFact]
    public void Reference_No_Spinner() => Run((scene, clock) =>
    {
        var loading = Present(scene, clock, new AvonicLoading { Message = "Loading...", Spinner = null });
        Save(scene, "avonic-ion-loading-no-spinner.png");
        Near([140.44, 390, 109.11, 64], Box(loading.Wrapper!, scene));
    });

    [AvaloniaFact]
    public void Reference_No_Message() => Run((scene, clock) =>
    {
        var loading = Present(scene, clock, new AvonicLoading());
        Save(scene, "avonic-ion-loading-no-message.png");
        Near([157, 382.5, 76, 79], Box(loading.Wrapper!, scene));
    });

    [AvaloniaFact]
    public void Reference_Long() => Run((scene, clock) =>
    {
        var loading = Present(scene, clock, new AvonicLoading
        {
            Message = "Please wait while we load your data from the server. This could take a little while.",
        });
        Save(scene, "avonic-ion-loading-long.png");

        // M6.
        Near([55, 374, 280, 96], Box(loading.Wrapper!, scene));
        Near([123, 398, 188, 48], Box(Content(loading), scene));
    });

    [AvaloniaFact]
    public void Reference_No_Backdrop() => Run((scene, clock) =>
    {
        var loading = Present(scene, clock, new AvonicLoading { Message = "Loading...", ShowBackdrop = false });
        Save(scene, "avonic-ion-loading-no-backdrop.png");
        Near([118.44, 382.5, 153.11, 79], Box(loading.Wrapper!, scene));
    });

    [AvaloniaFact]
    public void Reference_Custom() => Run((scene, clock) =>
    {
        // M11: .custom-loading { --background: #222; --spinner-color: #fff; color: #fff }, as the loading's keys.
        var loading = new AvonicLoading { Message = "Loading...", CssClass = "custom-loading", Foreground = Brushes.White };
        loading.Resources["Avonic.Loading.Background"] = new SolidColorBrush(Color.Parse("#222"));
        loading.Resources["Avonic.Loading.SpinnerColor"] = Brushes.White;
        Present(scene, clock, loading);
        Save(scene, "avonic-ion-loading-custom.png");
    });

    [AvaloniaFact]
    public void Reference_Sized() => Run((scene, clock) =>
    {
        // M7: --width: 200px; --min-height: 150px.
        var loading = new AvonicLoading { Message = "Loading..." };
        loading.Resources["Avonic.Loading.Width"] = "200px";
        loading.Resources["Avonic.Loading.MinHeight"] = "150px";
        Present(scene, clock, loading);
        Save(scene, "avonic-ion-loading-sized.png");
        Near([95, 347, 200, 150], Box(loading.Wrapper!, scene));
        Near([142.44, 406.5, 28, 28], Box(loading.SpinnerPart!, scene));
    });

    [AvaloniaFact]
    public void Reference_Rtl() => Run((scene, clock) =>
    {
        var loading = Present(scene, clock, new AvonicLoading { Message = "Loading..." });
        Save(scene, "avonic-ion-loading-rtl.png");

        // M12.
        Near([219.56, 406.5, 28, 28], Box(loading.SpinnerPart!, scene));
        Near([142.44, 414, 61.13, 16], Box(Content(loading), scene));
    }, FlowDirection.RightToLeft);

    [AvaloniaTheory]
    [InlineData(SpinnerName.Bubbles, "bubbles")]
    [InlineData(SpinnerName.Circles, "circles")]
    [InlineData(SpinnerName.Circular, "circular")]
    [InlineData(SpinnerName.Crescent, "crescent")]
    [InlineData(SpinnerName.Dots, "dots")]
    [InlineData(SpinnerName.Lines, "lines")]
    [InlineData(SpinnerName.LinesSharp, "lines-sharp")]
    [InlineData(SpinnerName.LinesSharpSmall, "lines-sharp-small")]
    [InlineData(SpinnerName.LinesSmall, "lines-small")]
    public void Reference_Spinner(SpinnerName name, string file) => Run((scene, clock) =>
    {
        var loading = Present(scene, clock, new AvonicLoading { Message = "Loading...", Spinner = name });
        Save(scene, $"avonic-ion-loading-spinner-{file}.png");

        // M8: the same boxes whatever the name.
        Near([142.44, 406.5, 28, 28], Box(loading.SpinnerPart!, scene));
    });
}
