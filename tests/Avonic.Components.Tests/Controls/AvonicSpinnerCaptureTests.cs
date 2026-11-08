using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, as <c>avonic-ion-spinner-page.png</c>,
/// for <c>docs/components/ion-spinner/COMPARISON.md</c>.
/// </summary>
public class AvonicSpinnerCaptureTests
{
    // The same scene as the command at the top of COMPARISON.md, which seeks every animation to 350 ms.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();

        static StackPanel Row(params AvonicSpinner[] spinners)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            row.Children.AddRange(spinners);
            return row;
        }

        static AvonicSpinner Spinner(SpinnerName? name = null, string? color = null, double? size = null)
        {
            var spinner = new AvonicSpinner { SpinnerName = name, Color = color };
            if (size is { } s)
            {
                spinner.Width = s;
                spinner.Height = s;
            }

            return spinner;
        }

        var page = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 16,
            Children =
            {
                Row(Enum.GetValues<SpinnerName>().Select(name => Spinner(name)).ToArray()),
                Row(Spinner(), Spinner(SpinnerName.Crescent, "danger"), Spinner(SpinnerName.Lines, "primary"), Spinner(SpinnerName.Dots, "success")),
                Row(Spinner(size: 80), Spinner(SpinnerName.Lines, size: 80), Spinner(SpinnerName.Crescent, size: 80), Spinner(SpinnerName.Dots, size: 80)),
            },
        };
        Avalonia.Controls.Documents.TextElement.SetForeground(page, Brushes.Black);

        using var scene = new Scene(page);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        clock.At(350);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-ion-spinner-page.png"), PngBitmapEncoderOptions.Default);
        }

        // The large circular spinner's arc, at 350 ms, is over the bottom: dash 50.5 from 7.5 along, 21° to 166°.
        // Its centre is at (16 + 40, 16 + 28 + 16 + 28 + 16 + 40) px, r 33.3 px, at 2×.
        var centre = new Point(56, 144);
        Assert.True(Frames.PixelAt(frame, (int)(centre.X * 2), (int)((centre.Y + 33) * 2)).R < 64);
        Assert.True(Frames.PixelAt(frame, (int)(centre.X * 2), (int)((centre.Y - 33) * 2)).R > 192);
    });
}
