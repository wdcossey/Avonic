using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The scroll indicator, an extension: what Chromium draws on a scroller in Android emulation, measured. See "Keyboard scrolling and
/// the scroll indicator" in docs/components/ion-content/PLAN.md, whose numbers these tests have.
/// </summary>
public class AvonicContentIndicatorTests
{
    private static ScrollViewer Scroll(AvonicContent content) =>
        content.GetVisualDescendants().OfType<ScrollViewer>().First(control => control.Name == "PART_Scroll");

    private static ContentScrollIndicator Indicator(AvonicContent content, Orientation orientation) =>
        content.GetVisualDescendants().OfType<ContentScrollIndicator>().First(i => i.Orientation == orientation && i.TemplatedParent == Scroll(content));

    private static ContentScrollIndicator Vertical(AvonicContent content) => Indicator(content, Orientation.Vertical);

    private static ContentScrollIndicator Horizontal(AvonicContent content) => Indicator(content, Orientation.Horizontal);

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Render()
    {
        Settle();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();
    }

    private static Panel Box(Control child, double height = 300) =>
        new() { Height = height, VerticalAlignment = VerticalAlignment.Top, Children = { child } };

    private static AvonicContent Tall(double height = 1200, double width = double.NaN) =>
        new() { Content = new Border { Height = height, Width = width, Background = Brushes.Transparent } };

    private static void Wheel(Scene scene, double dy, double dx = 0)
    {
        Render();
        scene.Window.MouseWheel(new Point(100, 100), new Vector(-dx / 50, -dy / 50), RawInputModifiers.None);
        Settle();
    }

    private static void Drag(Scene scene, double dy)
    {
        Render();
        var from = new Point(100, 250);
        var finger = scene.Window.TouchBegin(from, RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            scene.Window.TouchMove(finger, new Point(from.X, from.Y - dy * i / 10), RawInputModifiers.None);
            Settle();
        }

        scene.Window.TouchEnd(finger, new Point(from.X, from.Y - dy), RawInputModifiers.None);
        Settle();
    }

    private static Rect Box(Visual visual, Visual root) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root)!.Value);

    // ── I1 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(1200, 300, 300, 75, 75)]
    [InlineData(30000, 300, 300, 2, 4)]
    [InlineData(3000, 844, 300, 84, 237)]
    public void I1_The_Thumb_Is_Its_Share_Of_The_Track_Rounded_Down_And_At_Least_4(double extent, double viewport, double offset, double place, double length) =>
        InstalledTheme.Run(_ =>
        {
            var content = Tall(extent);
            using var scene = new Scene(Box(content, viewport));
            Scroll(content).Offset = new Vector(0, offset);
            Settle();

            var indicator = Vertical(content);
            Assert.Equal(new Rect(0, place, 4, length), indicator.Thumb);
            // Flush with the end edge, the whole side.
            Assert.Equal(new Rect(386, 0, 4, viewport), Box(indicator, scene.Window));
        });

    [AvaloniaFact]
    public void I1_In_RTL_It_Is_On_The_Left() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        content.FlowDirection = FlowDirection.RightToLeft;
        using var scene = new Scene(Box(content));

        Assert.Equal(new Rect(0, 0, 4, 300), Box(Vertical(content), scene.Window));
    });

    [AvaloniaFact]
    public void I1_With_Both_Axes_Each_Track_Leaves_The_Corner() => InstalledTheme.Run(_ =>
    {
        // 1560 by 1200 in 390 by 300, at 300, 300: 74 long at 74 down, and 96 long at 74 across.
        var content = Tall(1200, 1560);
        content.ScrollX = true;
        using var scene = new Scene(Box(content));
        Scroll(content).Offset = new Vector(300, 300);
        Settle();

        Assert.Equal(new Rect(0, 74, 4, 74), Vertical(content).Thumb);
        Assert.Equal(new Rect(74, 0, 96, 4), Horizontal(content).Thumb);
        Assert.Equal(new Rect(0, 296, 390, 4), Box(Horizontal(content), scene.Window));
    });

    // ── I2 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void I2_It_Shows_On_The_Wheel()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        Assert.Equal(0, Vertical(content).ThumbOpacity);

        Wheel(scene, 100);

        Assert.Equal(100, Scroll(content).Offset.Y);
        Assert.Equal(1, Vertical(content).ThumbOpacity);
        // Not on an axis that can't scroll, which draws nothing.
        Assert.Equal(0, Horizontal(content).ThumbOpacity);
    }

    [AvaloniaFact]
    public void I2_It_Shows_On_A_Drag()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        Scroll(content).IsScrollInertiaEnabled = false;
        Settle();

        Drag(scene, 100);

        Assert.True(Scroll(content).Offset.Y > 0);
        Assert.Equal(1, Vertical(content).ThumbOpacity);
    }

    [AvaloniaFact]
    public void I2_Not_On_A_Script_A_Scroll_Method_Or_A_Key()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));

        Scroll(content).Offset = new Vector(0, 100);
        Settle();
        _ = content.ScrollToPoint(null, 400, 300);
        clock.At(400);
        Scroll(content).Focus(NavigationMethod.Tab);
        scene.Window.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.None);
        Settle();
        clock.At(800);

        Assert.Equal(662, Scroll(content).Offset.Y);
        Assert.Equal(0, Vertical(content).ThumbOpacity);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void I2_A_Script_After_The_Users_Scroll_Shows_Nothing(bool drag)
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        Scroll(content).IsScrollInertiaEnabled = false;
        Settle();
        if (drag)
        {
            Drag(scene, 100);
        }
        else
        {
            Wheel(scene, 100);
        }

        clock.At(1000);
        Assert.Equal(0, Vertical(content).ThumbOpacity);

        Scroll(content).Offset = new Vector(0, 500);
        Settle();

        Assert.Equal(0, Vertical(content).ThumbOpacity);
    }

    [AvaloniaFact]
    public void I2_A_Nested_Scroller_The_Wheel_Passes_To_Shows_Its_Own()
    {
        // The inner one fits, so the wheel goes on to the outer, whose indicator shows, and the inner's doesn't.
        using var clock = new TestClock();
        var inner = new AvonicContent { Content = new Border { Height = 50 } };
        var outer = new AvonicContent { Content = new StackPanel { Children = { new Panel { Height = 200, Children = { inner } }, new Border { Height = 3000 } } } };
        using var scene = new Scene(Box(outer));

        Wheel(scene, 100);

        Assert.Equal(100, Scroll(outer).Offset.Y);
        Assert.Equal(1, Vertical(outer).ThumbOpacity);
        Assert.Equal(0, Vertical(inner).ThumbOpacity);
    }

    // ── I3 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void I3_It_Stays_300ms_Then_Fades_Steadily_Over_300ms()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        var indicator = Vertical(content);

        Wheel(scene, 100);
        clock.At(299);
        Assert.Equal(1, indicator.ThumbOpacity);
        clock.At(316);
        Assert.Equal(1 - 16 / 300d, indicator.ThumbOpacity, 6);
        clock.At(460);
        Assert.Equal(1 - 160 / 300d, indicator.ThumbOpacity, 6);
        clock.At(604);
        Assert.Equal(0, indicator.ThumbOpacity);
    }

    [AvaloniaFact]
    public void I3_A_New_Scroll_Brings_It_Straight_Back_And_Waits_Again()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        var indicator = Vertical(content);

        Wheel(scene, 100);
        clock.At(400);
        Assert.True(indicator.ThumbOpacity < 1);

        Wheel(scene, 100);
        Assert.Equal(1, indicator.ThumbOpacity);
        clock.At(699);
        Assert.Equal(1, indicator.ThumbOpacity);
        clock.At(1100);
        Assert.Equal(0, indicator.ThumbOpacity);
    }

    [AvaloniaFact]
    public void I3_A_Scroll_During_The_Wait_Waits_From_That_Scroll()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        var indicator = Vertical(content);

        Wheel(scene, 100);
        clock.At(200);
        Wheel(scene, 100);
        clock.At(499);
        Assert.Equal(1, indicator.ThumbOpacity);
        clock.At(516);
        Assert.Equal(1 - 16 / 300d, indicator.ThumbOpacity, 6);
    }

    // ── I4 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void I4_Nothing_On_An_Axis_That_Cant_Scroll() => InstalledTheme.Run(_ =>
    {
        var content = Tall(200);
        using var scene = new Scene(Box(content));

        Assert.Null(Vertical(content).Thumb);
        Assert.Null(Horizontal(content).Thumb);

        // ForceOverscroll's one pixel is somewhere to go: 300 × 300 / 301, rounded down.
        content.ForceOverscroll = true;
        Settle();
        Assert.Equal(new Rect(0, 0, 4, 299), Vertical(content).Thumb);
    });

    // ── I5 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void I5_It_Takes_No_Input_And_Is_Under_The_Fixed_Slot() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var under = new Border { Height = 1200, Background = Brushes.Transparent };
        var content = new AvonicContent { Content = under };
        using var scene = new Scene(Box(content));
        Wheel(scene, 100);
        Render();

        // The thumb is 75 long at 25, so this is on it.
        Assert.Equal(new Rect(0, 25, 4, 75), Vertical(content).Thumb);
        Assert.Equal(under, scene.Window.InputHitTest(new Point(388, 50)));
        // In the scroll part, which comes before the fixed slot.
        Assert.Contains(Scroll(content), Vertical(content).GetVisualAncestors());
    });

    // ── I6 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void I6_It_Draws_Half_Grey_From_A_Token() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        scene.Window.Background = Brushes.White;
        Wheel(scene, 300);

        // rgba(128, 128, 128, 0.5) on white is 191, and nothing to the side of it. The thumb is at 75, 75 long.
        var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.Equal(191, Pixel(frame, 388, 110).R, 1.5);
        Assert.Equal(255, Pixel(frame, 384, 110).R, 1.5);
        Assert.Equal(255, Pixel(frame, 388, 160).R, 1.5);

        // An app's own colour.
        content.Resources["Avonic.Content.ScrollIndicatorBrush"] = Brushes.Red;
        Settle();
        frame = scene.Window.CaptureRenderedFrame()!;
        Assert.Equal(Colors.Red, Pixel(frame, 388, 110));
    });

    private static Color Pixel(Avalonia.Media.Imaging.WriteableBitmap frame, int x, int y) => Frames.PixelAt(frame, x, y);
}