using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicContentCaptureTests
{
    private static AvonicItem Row(string text) => new() { Content = new AvonicLabel { Content = text } };

    private static Border Slot(double width, double height, IBrush brush, HorizontalAlignment horizontal, VerticalAlignment vertical, Thickness margin) => new()
    {
        Width = width,
        Height = height,
        Background = brush,
        HorizontalAlignment = horizontal,
        VerticalAlignment = vertical,
        Margin = margin,
    };

    // The command at the top of docs/components/ion-content/COMPARISON.md renders the same scene.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        // Padding, scrolled by 20, with a fixed square at the end of the bottom corner.
        var padded = new AvonicContent
        {
            Content = new StackPanel { Children = { Row("One"), Row("Two"), Row("Three"), Row("Four"), Row("Five") } },
            FixedContent = Slot(40, 40, Brushes.Red, HorizontalAlignment.Right, VerticalAlignment.Bottom, new Thickness(0, 0, 10, 10)),
        };
        padded.Resources["Avonic.Content.PaddingStart"] = 10d;
        padded.Resources["Avonic.Content.PaddingTop"] = 20d;
        padded.Resources["Avonic.Content.PaddingEnd"] = 30d;
        padded.Resources["Avonic.Content.PaddingBottom"] = 40d;

        var primary = new AvonicContent { Color = "primary", Content = "A primary content", FixedContent = "Fixed" };
        var unknown = new AvonicContent { Color = "nonexistent", Content = "An unknown colour" };

        var custom = new AvonicContent { Content = "Its own background and colour", FixedContent = "Fixed" };
        custom.Resources["Avonic.Content.Background"] = new SolidColorBrush(Color.Parse("#123456"));
        custom.Resources["Avonic.Content.Color"] = new SolidColorBrush(Color.Parse("#FEDCBA"));

        // Fullscreen, with translucent bands around it, and the content showing through them.
        var fullscreen = new AvonicContent
        {
            Fullscreen = true,
            Content = new Border { Height = 300, Background = new SolidColorBrush(Color.Parse("#FFD966")) },
        };
        Grid.SetRow(fullscreen, 1);
        var head = new Border { ZIndex = 10, Background = new SolidColorBrush(Color.Parse("#4D0000FF")) };
        var foot = new Border { ZIndex = 10, Background = new SolidColorBrush(Color.Parse("#4DFF0000")) };
        Grid.SetRow(foot, 2);
        var page = new Grid { RowDefinitions = new RowDefinitions("40,*,30"), Children = { head, fullscreen, foot } };

        var rtl = new AvonicContent
        {
            FlowDirection = FlowDirection.RightToLeft,
            Content = "RTL, with a start padding",
            FixedContent = Slot(30, 30, Brushes.Green, HorizontalAlignment.Left, VerticalAlignment.Top, default),
        };
        rtl.Resources["Avonic.Content.PaddingStart"] = 50d;

        var stage = new Grid
        {
            RowDefinitions = new RowDefinitions("200,80,80,80,160,80"),
            Children = { padded, primary, unknown, custom, page, rtl },
        };
        var rows = new Control[] { padded, primary, unknown, custom, page, rtl };
        for (var i = 0; i < rows.Length; i++)
        {
            Grid.SetRow(rows[i], i);
        }

        using var scene = new Scene(stage, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        padded.ScrollToPoint(null, 20);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-content-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        Assert.Equal(390, padded.Bounds.Width);
        Assert.Equal(20, padded.GetScrollElement()!.Offset.Y);
    });
}
