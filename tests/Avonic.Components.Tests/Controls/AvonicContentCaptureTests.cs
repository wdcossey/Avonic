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

    private static AvonicContent Rows(int count)
    {
        var rows = new StackPanel();
        for (var i = 1; i <= count; i++)
        {
            rows.Children.Add(Row($"Row {i}"));
        }

        return new AvonicContent { Content = rows };
    }

    // ref/indicator.png: 30 rows in 390 by 300, a wheel of 300, 100 ms later.
    [AvaloniaFact]
    public void Indicator_Frame() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Rows(30);
        using var scene = new Scene(content, 390, 300);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        scene.Window.MouseWheel(new Point(100, 100), new Vector(0, -6), Avalonia.Input.RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        clock.At(100);
        Frames.Save(scene.Window.CaptureRenderedFrame()!, "avonic-content-indicator.png");
        Assert.Equal(300, content.GetScrollElement()!.Offset.Y);

        clock.At(1000);
        Frames.Save(scene.Window.CaptureRenderedFrame()!, "avonic-content-indicator-gone.png");
    });

    // ref/scroll-bar.png: the same rows with ScrollBars on, desktop Chromium's bar, scrolled to 300, and both axes, 1560 wide.
    [AvaloniaFact]
    public void Scroll_Bar_Frame() => InstalledTheme.Run(_ =>
    {
        var content = Rows(30);
        content.ScrollBars = true;
        var wide = Rows(30);
        wide.ScrollBars = true;
        wide.ScrollX = true;
        ((StackPanel)wide.Content!).Width = 1560;
        var stage = new Grid { RowDefinitions = new RowDefinitions("300,300"), Children = { content, wide } };
        Grid.SetRow(wide, 1);
        using var scene = new Scene(stage, 390, 600);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        content.GetScrollElement()!.Offset = new Vector(0, 300);
        wide.GetScrollElement()!.Offset = new Vector(300, 300);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        Frames.Save(scene.Window.CaptureRenderedFrame()!, "avonic-content-scroll-bar.png");

        Assert.Equal(new Size(375, 300), content.GetScrollElement()!.Viewport);
        Assert.Equal(new Size(375, 285), wide.GetScrollElement()!.Viewport);
    });

    // ref/focus-ring.png: the same rows, 10 in from the edges, Tab, then Page Down.
    [AvaloniaFact]
    public void Focus_Ring_Frame()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                var content = Rows(30);
                content.Margin = new Thickness(10);
                using var scene = new Scene(content, 390, 300);
                scene.Window.Background = Brushes.White;
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();

                scene.Window.KeyPressQwerty(Avalonia.Input.PhysicalKey.Tab, Avalonia.Input.RawInputModifiers.None);
                scene.Window.KeyPressQwerty(Avalonia.Input.PhysicalKey.PageDown, Avalonia.Input.RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
                clock.At(400);
                Frames.Save(scene.Window.CaptureRenderedFrame()!, "avonic-content-focus-ring.png");

                // 87.5% of 280, rounded down, as Chromium's 245.
                Assert.Equal(245, content.GetScrollElement()!.Offset.Y);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }
}
