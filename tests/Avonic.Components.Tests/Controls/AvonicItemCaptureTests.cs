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
    private const string LongLabel = "A very long label that certainly has to wrap, because it does not fit";
    private const string LongNote = "A very long note that certainly has to wrap because it does not fit on one line";

    /// <summary>The layout page in COMPARISON.md: the same rows, in the same order, as its --html.</summary>
    [AvaloniaFact]
    public void Layout_Page() => InstalledTheme.Run(_ =>
    {
        static AvonicLabel Label(string text) => new() { Content = text };
        static AvonicNote Note(string text) => new() { Content = text };

        var page = new StackPanel
        {
            Children =
            {
                new AvonicItem { StartContent = Label("Start"), Content = Label("Default") },
                new AvonicItem { StartContent = Label(LongLabel), Content = Label("Default") },
                new AvonicItem { Content = Label("Default"), EndContent = Label("End") },
                new AvonicItem { Content = Label("Default"), EndContent = Label(LongLabel) },
                new AvonicItem { StartContent = Label("Start"), Content = Label("Default"), EndContent = Label("End") },
                new AvonicItem { StartContent = Label(LongLabel), Content = Label("Default"), EndContent = Label(LongLabel) },
                new AvonicItem { Detail = true, Content = Label("Default"), EndContent = Label(LongLabel) },
                new AvonicItem { StartContent = new AvonicIcon { IconName = "star" }, Content = Label("Default"), EndContent = Label(LongLabel) },
                new AvonicItem { Content = Label("Default"), EndContent = Note(LongNote) },
                new AvonicItem { StartContent = Note("Short"), Content = Label("Default"), EndContent = Note("Short") },
                new AvonicItem { Content = "Plain default words", EndContent = Note(LongNote) },
                new AvonicItem { StartContent = new AvonicLabel { Content = "Fixed", Position = LabelPosition.Fixed }, Content = Label("Default") },
                new AvonicItem { StartContent = new AvonicLabel { Content = "Stacked start", Position = LabelPosition.Stacked }, Content = Label("Default") },
                new AvonicItem { Color = "primary", StartContent = Label("Start"), Content = Label("Default"), EndContent = Label("End") },
                new AvonicItem { FlowDirection = FlowDirection.RightToLeft, StartContent = Label("Start"), Content = Label("Default"), EndContent = Note(LongNote) },
                new AvonicItemDivider { StartContent = Label("Start"), Content = Label("Default") },
                new AvonicItemDivider { StartContent = Label(LongLabel), Content = Label("Default") },
                new AvonicItemDivider { Content = Label("Default"), EndContent = Label(LongLabel) },
            },
        };
        using var scene = new Scene(page, 390, 1700);
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-layout.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        Assert.Equal(48, page.Children[0].Bounds.Height);
    });

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
