using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicSkeletonTextCaptureTests
{
    // The same page as the --html at the top of docs/components/ion-skeleton-text/COMPARISON.md, at 500 ms, as
    // ref/page-500ms.png has every shimmer paused at 500 ms
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var (frame, skeletons, running) = Render();
        Frames.Save(frame, "avonic-skeleton-text-page.png");

        // Measured in Ionic: the first three at y 20, 34 and 48, 358 wide, the second 200, the third 20 high, and the label's at
        // 221, 241 and 261, 20 apart
        Assert.Equal(new Rect(0, 4, 358, 10), skeletons[0].Bounds);
        Assert.Equal(new Rect(0, 18, 200, 10), skeletons[1].Bounds);
        Assert.Equal(new Rect(0, 32, 358, 20), skeletons[2].Bounds);
        Assert.Equal([6, 26, 46], skeletons.Skip(7).Take(3).Select(skeleton => skeleton.Bounds.Y));
        Assert.Equal(skeletons.Count(skeleton => skeleton.Animated), running);
        Assert.Equal(9, running);
    });

    // Ionic's animated config reaches skeleton text: with it off, nothing shimmers, and the animated ones are plain
    [AvaloniaFact]
    public void Reference_Page_With_Animated_Off_Is_Plain() => InstalledTheme.Run(_ =>
    {
        AvonicConfig.Animated = false;
        try
        {
            var (frame, _, running) = Render();
            Frames.Save(frame, "avonic-skeleton-text-page-animated-off.png");
            Assert.Equal(0, running);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    });

    private static AvonicSkeletonText Animated(double width = double.NaN, Thickness? margin = null)
    {
        var skeleton = new AvonicSkeletonText { Animated = true, Width = width };
        if (!double.IsNaN(width))
        {
            // A block with a width starts at the left
            skeleton.HorizontalAlignment = HorizontalAlignment.Left;
        }

        if (margin is { } value)
        {
            skeleton.Margin = value;
        }

        return skeleton;
    }

    /// <summary>The frame at 500 ms, the skeletons, and how many were requesting frames then.</summary>
    private static (WriteableBitmap Frame, List<AvonicSkeletonText> Skeletons, int Running) Render()
    {
        using var clock = new TestClock();
        var padding = new StackPanel
        {
            Margin = new Thickness(16),
            Children =
            {
                // CSS collapses the 4 below one block and the 4 above the next into 4; a StackPanel doesn't, so the next two
                // have no margin above, as an app would set it
                new AvonicSkeletonText(),
                new AvonicSkeletonText { Width = 200, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 4) },
                new AvonicSkeletonText { Animated = true, Height = 20, Margin = new Thickness(0, 0, 0, 4) },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 16,
                    Children =
                    {
                        new AvonicAvatar { Content = Animated(), VerticalAlignment = VerticalAlignment.Top },
                        new AvonicThumbnail { Content = new AvonicSkeletonText(), VerticalAlignment = VerticalAlignment.Top },
                    },
                },
            },
        };

        // In Ionic these are in an h3, which has 2 above and below in a label, and in a p, which has 2 below. A skeleton can't
        // be in a TextBlock, so it carries those margins itself. The widths are 80 %, 60 % and 30 % of the label's 286.
        var list = new AvonicList
        {
            Items =
            {
                new AvonicListHeader { Content = Animated(80) },
                new AvonicItem
                {
                    StartContent = new AvonicThumbnail { Content = Animated() },
                    Content = new AvonicLabel
                    {
                        Content = new StackPanel
                        {
                            Children =
                            {
                                Animated(228.8, new Thickness(0, 6)),
                                Animated(171.6, new Thickness(0, 4, 0, 6)),
                                Animated(85.8, new Thickness(0, 4, 0, 6)),
                            },
                        },
                    },
                },
                new AvonicItem
                {
                    StartContent = new AvonicAvatar { Content = Animated() },
                    Content = new AvonicLabel { Content = Animated() },
                },
            },
        };
        var page = new StackPanel { Background = Brushes.White, Children = { padding, list } };
        TextElement.SetFontSize(page, 16);

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        scene.Window.UpdateLayout();

        clock.At(500);
        var skeletons = page.GetVisualDescendants().OfType<AvonicSkeletonText>().ToList();
        foreach (var skeleton in skeletons)
        {
            skeleton.Tick();
        }

        Dispatcher.UIThread.RunJobs();
        return (scene.Window.CaptureRenderedFrame()!, skeletons, skeletons.Count(skeleton => skeleton.IsFrameLoopRunning));
    }
}
