using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicBackdropCaptureTests
{
    // The same content as the --html at the top of docs/components/ion-backdrop/COMPARISON.md.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        static Panel Row(string text, AvonicBackdrop backdrop) => new()
        {
            Height = 200,
            Background = Brushes.White,
            Children = { new TextBlock { Text = text, Margin = new Thickness(16), FontSize = 16, Foreground = Brushes.Black }, backdrop },
        };

        var page = new StackPanel
        {
            Children =
            {
                Row("Default: opacity 0.01", new AvonicBackdrop()),
                Row("As an overlay leaves it: 0.32", new AvonicBackdrop { Opacity = 0.32 }),
                Row("Not visible, at 0.32", new AvonicBackdrop { Visible = false, Opacity = 0.32 }),
            },
        };

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "avonic-ion-backdrop-page.png");

        // Chromium's frame has 252, 173 and 255 there: white under black at 0.01, 0.32, and transparent. Skia's blending
        // rounds each of the first two one level higher: 253 and 174.
        Assert.Equal(Color.FromRgb(253, 253, 253), Frames.PixelAt(frame, 600, 300));
        Assert.Equal(Color.FromRgb(174, 174, 174), Frames.PixelAt(frame, 600, 700));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 600, 1100));
    });
}
