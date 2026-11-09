using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicProgressBarCaptureTests
{
    // The same bars as the --html at the top of docs/components/ion-progress-bar/COMPARISON.md, at 1000 ms, as
    // ref/page-1000ms.png has every animation paused at 1000 ms.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var (frame, page) = Render();
        Frames.Save(frame, "avonic-progress-bar-page.png");

        // Measured in Ionic: every bar 358 × 4, 24 apart
        Assert.All(page.Children, bar => Assert.Equal(new Size(358, 4), bar.Bounds.Size));
        Assert.Equal(16 + 24 * 9, page.Children[9].Bounds.Y + 16);
    });

    // Ionic's animated config doesn't reach a progress bar, so with it off the page at 1000 ms is the same frame, every byte.
    [AvaloniaFact]
    public void Reference_Page_With_Animated_Off_Is_The_Same_Frame() => InstalledTheme.Run(_ =>
    {
        var (expected, _) = Render();
        AvonicConfig.Animated = false;
        try
        {
            var (frame, _) = Render();
            Frames.Save(frame, "avonic-progress-bar-page-animated-off.png");
            Frames.AssertSameFrame(expected, frame);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    });

    private static (WriteableBitmap Frame, StackPanel Page) Render()
    {
        using var clock = new TestClock();
        var page = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 20,
            Children =
            {
                new AvonicProgressBar { Value = 0.25 },
                new AvonicProgressBar { Value = 0.5 },
                new AvonicProgressBar { Value = 1 },
                new AvonicProgressBar { Value = 0.3, Buffer = 0.6 },
                new AvonicProgressBar { Value = 0.3, Buffer = 0.6, Reversed = true },
                new AvonicProgressBar { Value = 0.5, Buffer = 0.75, Color = "danger" },
                new AvonicProgressBar { Value = 0.5, Color = "success" },
                new AvonicProgressBar { Type = ProgressBarType.Indeterminate },
                new AvonicProgressBar { Type = ProgressBarType.Indeterminate, Color = "warning" },
                new AvonicProgressBar { Type = ProgressBarType.Indeterminate, Reversed = true },
            },
        };

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        clock.At(1000);
        foreach (var bar in page.Children.OfType<AvonicProgressBar>())
        {
            bar.Tick();
        }

        Dispatcher.UIThread.RunJobs();
        return (scene.Window.CaptureRenderedFrame()!, page);
    }
}
