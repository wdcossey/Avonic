using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicCardCaptureTests
{
    private static Control HeaderAndContent(string? headerColor = null) => new StackPanel
    {
        Children =
        {
            new AvonicCardHeader
            {
                Color = headerColor,
                Content = new StackPanel
                {
                    Children =
                    {
                        new AvonicCardTitle { Content = "Card title" },
                        new AvonicCardSubtitle { Content = "Card subtitle" },
                    },
                },
            },
            new AvonicCardContent { Content = "Card content" },
        },
    };

    // The same content as the command at the top of docs/components/ion-card/COMPARISON.md.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var page = new StackPanel
        {
            // CSS collapses the margins between cards. See the plan.
            Spacing = -10,
            Children =
            {
                new AvonicCard { Content = HeaderAndContent() },
                new AvonicCard { Button = true, Content = new AvonicCardContent { Content = "Button card" } },
                new AvonicCard { Color = "danger", Content = HeaderAndContent() },
                new AvonicCard { Content = HeaderAndContent("primary") },
                new AvonicCard
                {
                    Content = new AvonicCardContent
                    {
                        Content = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock { Text = "Heading one", Classes = { "h1" } },
                                new TextBlock { Text = "Heading two", Classes = { "h2" } },
                                new TextBlock { Text = "Heading three", Classes = { "h3" } },
                                new TextBlock { Text = "Paragraph", Classes = { "p" } },
                            },
                        },
                    },
                },
                new AvonicCard { IsEnabled = false, Content = new AvonicCardContent { Content = "Disabled card" } },
                new AvonicCard { FlowDirection = FlowDirection.RightToLeft, Content = HeaderAndContent() },
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
            frame.Save(Path.Combine(dir, "avonic-card-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        Assert.All(page.Children, card => Assert.Equal(370, card.Bounds.Width));
    });

    // The same scene as ref-tap: a 338 by 47 button card, in a clip 8 larger all round, pressed in the middle.
    [AvaloniaFact]
    public void Press_Frame() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var card = new AvonicCard { Button = true, Margin = new Thickness(8), Content = new AvonicCardContent { Content = "Button card" } };
        using var scene = new Scene(card, 354, 63);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        var centre = new Point(177, 31.5);
        scene.Window.MouseDown(centre, Avalonia.Input.MouseButton.Left, Avalonia.Input.RawInputModifiers.None);
        clock.At(160);
        var ripple = card.GetVisualDescendants().OfType<AvonicRippleEffect>().Single();
        ripple.Tick();
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-card-press-160ms.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        Assert.Single(ripple.Ripples);
        scene.Window.MouseUp(centre, Avalonia.Input.MouseButton.Left, Avalonia.Input.RawInputModifiers.None);
    });
}
