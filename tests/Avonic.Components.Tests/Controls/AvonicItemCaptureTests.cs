using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicItemCaptureTests
{
    // The same content as DEFAULT_HTML['ion-item'] in tools/ionic-ref/render.js.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        static AvonicIcon Icon(string name) => new() { IconName = name };

        var page = new StackPanel
        {
            Children =
            {
                new AvonicItem { Content = "Default lines" },
                new AvonicItem { Content = "Lines full", Lines = ItemLines.Full },
                new AvonicItem { Content = "Lines none", Lines = ItemLines.None },
                new AvonicItem { Content = "Button with detail", Button = true, Detail = true },
                new AvonicItem { Content = "Icons", StartContent = Icon("heart"), EndContent = Icon("star") },
                new AvonicItem { Content = "Danger", Color = "danger", Button = true, Detail = true, StartContent = Icon("heart") },
                new AvonicItem { Content = "Primary", Color = "primary" },
                new AvonicItem { Content = "Disabled", Button = true, IsEnabled = false },
                new AvonicItem
                {
                    Content = "RTL",
                    Detail = true,
                    StartContent = Icon("heart"),
                    EndContent = Icon("star"),
                    FlowDirection = FlowDirection.RightToLeft,
                },
            },
        };

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-item-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        // The nine rows from the reference: 48, 49, 48, 48, 49, 48, 48, 48, 49.
        Assert.Equal(435, page.Children.Sum(row => row.Bounds.Height));
    });
}
