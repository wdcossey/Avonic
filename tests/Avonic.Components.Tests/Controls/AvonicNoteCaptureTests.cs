using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicNoteCaptureTests
{
    private const string Long = "A very long note that certainly does not fit on one line of a phone-sized row at all, however small it is";

    // The same content as the --html at the top of docs/components/ion-note/COMPARISON.md.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        static AvonicLabel Label(string text) => new() { Content = text };

        var page = new StackPanel
        {
            Children =
            {
                new AvonicNote { Content = "A note on its own" },
                new AvonicNote { Content = "A danger note", Color = "danger" },
                new AvonicItem { Content = Label("In the end slot"), EndContent = new AvonicNote { Content = "Yesterday" } },
                new AvonicItem { StartContent = new AvonicNote { Content = "Start" }, Content = Label("In the start slot") },
                new AvonicItem { Content = new AvonicNote { Content = "In the default slot" } },
                new AvonicItem
                {
                    Content = new AvonicLabel
                    {
                        Content = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock { Text = "A tall row", Classes = { "h2" } },
                                new TextBlock { Text = "The note stays at the top", Classes = { "p" } },
                                new TextBlock { Text = "and the icon stays centred", Classes = { "p" } },
                            },
                        },
                    },
                    EndContent = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new AvonicNote { Content = "Monday", Margin = new Thickness(0, 0, 16, 0) },
                            new AvonicIcon { IconName = "star" },
                        },
                    },
                },
                new AvonicItem { Color = "primary", Content = Label("A primary item"), EndContent = new AvonicNote { Content = "Stays grey" } },
                new AvonicItem { Color = "primary", Content = Label("A primary item"), EndContent = new AvonicNote { Content = "Light", Color = "light" } },
                new AvonicItem { Content = new AvonicLabel { Content = "Stacked", Position = LabelPosition.Stacked }, EndContent = new AvonicNote { Content = "Beside it" } },
                new AvonicItem { Content = new AvonicNote { Content = Long + ", in the default slot" } },
                new AvonicItem { EndContent = new AvonicNote { Content = Long + ", in the end slot" } },
                new AvonicItem
                {
                    Content = Label("RTL"),
                    EndContent = new AvonicNote { Content = "End" },
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
            frame.Save(Path.Combine(dir, "avonic-note-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        Assert.Equal(48, page.Children[2].Bounds.Height);

        // The gap the plan records: a long note in the end slot is one line, however wide, and the item cuts it off.
        var longEnd = (AvonicNote)((AvonicItem)page.Children[10]).EndContent!;
        Assert.True(longEnd.Bounds.Width > page.Bounds.Width);
    });
}
