using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The scroll bar a content has with <see cref="AvonicContent.ScrollBars"/> on, an extension: desktop Chromium's on Windows 11, measured
/// with no emulation. See "A draggable scroll bar, by config (#175)" in docs/components/ion-content/PLAN.md, whose numbers these have:
/// 30 rows of 48, 1440, in 390 by 300.
/// </summary>
public class AvonicContentScrollBarTests
{
    private static ScrollViewer Scroll(AvonicContent content) =>
        content.GetVisualDescendants().OfType<ScrollViewer>().First(control => control.Name == "PART_Scroll");

    private static ScrollBar Bar(AvonicContent content, Orientation orientation) =>
        content.GetVisualDescendants().OfType<ScrollBar>().First(bar => bar.Orientation == orientation);

    private static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static ContentScrollIndicator Indicator(AvonicContent content) =>
        content.GetVisualDescendants().OfType<ContentScrollIndicator>().First(i => i.Orientation == Orientation.Vertical);

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Render()
    {
        Settle();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();
    }

    private static Panel Box(Control child, double height = 300) =>
        new() { Height = height, VerticalAlignment = VerticalAlignment.Top, Children = { child } };

    private static AvonicContent Tall(double height = 1440, double width = double.NaN, bool bars = true) => new()
    {
        ScrollBars = bars,
        Content = new Border { Height = height, Width = width, Background = Brushes.Transparent },
    };

    private static Rect Box(Visual visual, Visual root) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root)!.Value);

    private static bool Shown(Visual visual) => visual.IsEffectivelyVisible && visual.Bounds.Width > 0 && visual.Bounds.Height > 0;

    private static void Press(Scene scene, Point at)
    {
        Render();
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Left);
        Settle();
    }

    private static void Release(Scene scene, Point at)
    {
        scene.Window.MouseUp(at, MouseButton.Left);
        Settle();
    }

    // ── S1 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S1_Off_By_Default_There_Is_No_Bar_And_Nothing_Takes_The_Width() => InstalledTheme.Run(_ =>
    {
        var content = Tall(bars: false);
        using var scene = new Scene(Box(content));

        Assert.False(content.ScrollBars);
        Assert.Equal(new Size(390, 300), Scroll(content).Viewport);
        Assert.DoesNotContain(content.GetVisualDescendants().OfType<ScrollBar>(), Shown);
        Assert.True(Indicator(content).IsVisible);
    });

    // ── S2 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S2_A_Vertical_Bar_Is_15_Thick_At_The_End_Edge_And_Takes_The_Width() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        using var scene = new Scene(Box(content));

        var bar = Bar(content, Orientation.Vertical);
        Assert.True(Shown(bar));
        Assert.Equal(new Rect(375, 0, 15, 300), Box(bar, scene.Window));
        // Measured: clientWidth 375 of 390.
        Assert.Equal(new Size(375, 300), Scroll(content).Viewport);
        Assert.False(Shown(Bar(content, Orientation.Horizontal)));
    });

    [AvaloniaFact]
    public void S2_In_RTL_The_Bar_Is_On_The_Left() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        content.FlowDirection = FlowDirection.RightToLeft;
        using var scene = new Scene(Box(content));

        Assert.Equal(new Rect(0, 0, 15, 300), Box(Bar(content, Orientation.Vertical), scene.Window));
    });

    [AvaloniaFact]
    public void S2_With_Both_Axes_A_Bar_Along_The_Bottom_And_The_Corner_In_The_Track_Colour() => InstalledTheme.Run(_ =>
    {
        var content = Tall(1440, 1560);
        content.ScrollX = true;
        using var scene = new Scene(Box(content));

        // Measured: clientWidth 375 and clientHeight 285 of 390 by 300.
        Assert.Equal(new Size(375, 285), Scroll(content).Viewport);
        Assert.Equal(new Rect(0, 285, 375, 15), Box(Bar(content, Orientation.Horizontal), scene.Window));
        Assert.Equal(new Rect(375, 0, 15, 285), Box(Bar(content, Orientation.Vertical), scene.Window));
        var corner = Part<Border>(Scroll(content), "PART_ScrollBarCorner");
        Assert.Equal(new Rect(375, 285, 15, 15), Box(corner, scene.Window));
        Assert.Equal(Color.Parse("#FCFCFC"), Assert.IsAssignableFrom<ISolidColorBrush>(corner.Background).Color);
    });

    // ── S3 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S3_Content_That_Fits_Has_No_Bar() => InstalledTheme.Run(_ =>
    {
        var content = Tall(200);
        using var scene = new Scene(Box(content));

        Assert.False(Shown(Bar(content, Orientation.Vertical)));
        Assert.Equal(new Size(390, 300), Scroll(content).Viewport);
    });

    [AvaloniaFact]
    public void S3_ScrollY_Off_Has_No_Bar_As_Overflow_Hidden_Has_None() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        content.ScrollY = false;
        using var scene = new Scene(Box(content));

        Assert.False(Shown(Bar(content, Orientation.Vertical)));
        Assert.Equal(390, Scroll(content).Viewport.Width);
    });

    [AvaloniaFact]
    public void S3_Turning_It_Off_Takes_The_Bar_Away() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        using var scene = new Scene(Box(content));
        content.ScrollBars = false;
        Settle();

        Assert.False(Shown(Bar(content, Orientation.Vertical)));
        Assert.Equal(390, Scroll(content).Viewport.Width);
    });

    // ── S4 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S4_The_Config_Is_The_Default_For_Contents_Made_After_It()
    {
        var before = new AvonicContent();
        try
        {
            AvonicConfig.ContentScrollBars = true;
            var after = new AvonicContent();
            Assert.True(after.ScrollBars);
            Assert.False(before.ScrollBars);

            // At style priority: a local value wins, and clearing it gives the config's back.
            after.ScrollBars = false;
            Assert.False(after.ScrollBars);
            after.ClearValue(AvonicContent.ScrollBarsProperty);
            Assert.True(after.ScrollBars);
        }
        finally
        {
            AvonicConfig.ContentScrollBars = false;
        }

        Assert.False(new AvonicContent().ScrollBars);
    }

    [AvaloniaFact]
    public void S4_An_Apps_Style_Beats_The_Config() => InstalledTheme.Run(_ =>
    {
        AvonicContent content;
        try
        {
            AvonicConfig.ContentScrollBars = true;
            content = Tall(bars: false);
            content.ClearValue(AvonicContent.ScrollBarsProperty);
        }
        finally
        {
            AvonicConfig.ContentScrollBars = false;
        }

        using var scene = Scene.Styled(Box(content), Scene.AppStyle<AvonicContent>(AvonicContent.ScrollBarsProperty, false));

        Assert.False(content.ScrollBars);
        Assert.False(Shown(Bar(content, Orientation.Vertical)));
    });

    [AvaloniaFact]
    public void S4_The_Config_Turns_The_Bar_On() => InstalledTheme.Run(_ =>
    {
        AvonicContent content;
        try
        {
            AvonicConfig.ContentScrollBars = true;
            content = new AvonicContent { Content = new Border { Height = 1440 } };
        }
        finally
        {
            AvonicConfig.ContentScrollBars = false;
        }

        using var scene = new Scene(Box(content));

        Assert.True(Shown(Bar(content, Orientation.Vertical)));
        Assert.Equal(375, Scroll(content).Viewport.Width);
    });

    // ── S5 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S5_The_Indicator_Never_Shows_With_A_Bar() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        Render();
        scene.Window.MouseWheel(new Point(100, 100), new Vector(0, -2), RawInputModifiers.None);
        Settle();

        Assert.Equal(100, Scroll(content).Offset.Y);
        Assert.False(Indicator(content).IsEffectivelyVisible);
    });

    // ── S6 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S6_Dragging_The_Thumb_Scrolls_In_Proportion() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        using var scene = new Scene(Box(content));

        // The thumb is 55 at 18: measured 50 down scrolls 273, 1140 × 50 / (264 − 55).
        var from = new Point(382, 45);
        Press(scene, from);
        scene.Window.MouseMove(from + new Point(0, 25));
        scene.Window.MouseMove(from + new Point(0, 50));
        Settle();
        Release(scene, from + new Point(0, 50));

        Assert.Equal(1140 * 50 / 209d, Scroll(content).Offset.Y, 3);
    });

    [AvaloniaFact]
    public void S6_Dragging_The_Horizontal_Thumb_Scrolls_Across() => InstalledTheme.Run(_ =>
    {
        var content = Tall(1440, 1560);
        content.ScrollX = true;
        using var scene = new Scene(Box(content));

        var thumb = Part<Thumb>(Bar(content, Orientation.Horizontal), "PART_Thumb");
        var from = Box(thumb, scene.Window).Center;
        Press(scene, from);
        scene.Window.MouseMove(from + new Point(30, 0));
        Settle();
        Release(scene, from + new Point(30, 0));

        Assert.True(Scroll(content).Offset.X > 100, $"{Scroll(content).Offset.X}");
        Assert.Equal(0, Scroll(content).Offset.Y);
    });

    // ── S7 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S7_An_Arrow_Scrolls_40_On_The_Press_Eased_As_A_Key() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));

        Press(scene, new Point(382, 292));
        clock.At(64);
        var partWay = Scroll(content).Offset.Y;
        clock.At(150);
        Release(scene, new Point(382, 292));
        clock.At(400);

        // On the keys' curve from the press, as Chromium eases a click: a frame at 64 ms, then there at 150.
        Assert.Equal(40 * Avonic.Components.Animation.CubicBezierEasing.CssEaseInOut.Ease(64 / 150d), partWay, 3);
        Assert.Equal(40, Scroll(content).Offset.Y);
    });

    [AvaloniaFact]
    public void S7_A_Second_Arrow_During_One_Adds_To_Its_Target() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));

        Press(scene, new Point(382, 292));
        Release(scene, new Point(382, 292));
        clock.At(50);
        Press(scene, new Point(382, 292));
        Release(scene, new Point(382, 292));
        clock.At(600);

        Assert.Equal(80, Scroll(content).Offset.Y);
    });

    [AvaloniaFact]
    public void S7_The_Up_Arrow_Goes_Back_And_Stops_At_The_Top() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        Scroll(content).Offset = new Vector(0, 30);

        Press(scene, new Point(382, 8));
        Release(scene, new Point(382, 8));
        clock.At(400);

        Assert.Equal(0, Scroll(content).Offset.Y);
    });

    [AvaloniaFact]
    public void S7_The_Track_Scrolls_A_Page_Of_87_And_A_Half_Percent() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));

        Press(scene, new Point(382, 250));
        Release(scene, new Point(382, 250));
        clock.At(400);

        // Measured: 262 of 300.
        Assert.Equal(262, Scroll(content).Offset.Y);

        Press(scene, new Point(382, 25));
        Release(scene, new Point(382, 25));
        clock.At(800);
        Assert.Equal(0, Scroll(content).Offset.Y);
    });

    [AvaloniaFact]
    public void S7_The_Horizontal_Arrow_And_Track_Go_Across_By_The_Width() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall(1440, 1560);
        content.ScrollX = true;
        using var scene = new Scene(Box(content));

        Press(scene, new Point(366, 292));
        Release(scene, new Point(366, 292));
        clock.At(400);
        Assert.Equal(new Vector(40, 0), Scroll(content).Offset);

        Press(scene, new Point(300, 292));
        Release(scene, new Point(300, 292));
        clock.At(800);
        // A page of the 375 wide viewport: 328.
        Assert.Equal(new Vector(40 + 328, 0), Scroll(content).Offset);
    });

    [AvaloniaFact]
    public void S7_Avalonias_Own_Steps_Never_Act() => InstalledTheme.Run(_ =>
    {
        // Without the content, Avalonia's ScrollBar would go 16 an arrow, and a whole viewport a page, at once.
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));

        Press(scene, new Point(382, 292));
        Assert.Equal(0, Scroll(content).Offset.Y);
        Release(scene, new Point(382, 292));
        clock.At(400);
        Assert.Equal(40, Scroll(content).Offset.Y);
    });

    // ── S8 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S8_The_Buttons_Click_On_The_Press_And_Repeat_After_250_Every_50() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        using var scene = new Scene(Box(content));

        foreach (var name in new[] { "PART_LineUp", "PART_LineDown", "PART_PageUp", "PART_PageDown" })
        {
            var button = Part<RepeatButton>(Bar(content, Orientation.Vertical), name);
            Assert.Equal(ClickMode.Press, button.ClickMode);
            Assert.Equal(250, button.Delay);
            Assert.Equal(50, button.Interval);
            Assert.False(button.Focusable);
        }
    });

    [AvaloniaFact]
    public void S8_A_Held_Arrow_Repeats() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        var button = Part<RepeatButton>(Bar(content, Orientation.Vertical), "PART_LineDown");

        Press(scene, new Point(382, 292));
        // What the button's timer does while it's held.
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Release(scene, new Point(382, 292));
        clock.At(600);

        Assert.Equal(120, Scroll(content).Offset.Y);
    });

    [AvaloniaFact]
    public void S8_A_Held_Track_Stops_Once_The_Thumb_Covers_The_Pointer() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        var button = Part<RepeatButton>(Bar(content, Orientation.Vertical), "PART_PageDown");

        // Held at 150: the thumb, 55 long on 209 of travel for 1140, goes from 18 to 66 at 262, and to 114 at 524, which covers it.
        Press(scene, new Point(382, 150));
        for (var i = 0; i < 6; i++)
        {
            clock.At(clock.Now.TotalMilliseconds + 50);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        Release(scene, new Point(382, 150));
        clock.At(1000);

        Assert.Equal(524, Scroll(content).Offset.Y);
    });

    [AvaloniaFact]
    public void S8_The_Press_On_The_Track_Always_Pages() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));

        // Just below the thumb: the page carries it past the pointer, as Chromium's does.
        Press(scene, new Point(382, 76));
        Release(scene, new Point(382, 76));
        clock.At(400);

        Assert.Equal(262, Scroll(content).Offset.Y);
    });

    [AvaloniaFact]
    public void S8_A_Held_Track_Up_Stops_Once_The_Thumb_Covers_The_Pointer() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = new Scene(Box(content));
        Scroll(content).Offset = new Vector(0, 1140);
        var button = Part<RepeatButton>(Bar(content, Orientation.Vertical), "PART_PageUp");

        // The thumb from 227 to 282; held at 150: 878 puts it at 179, 616 at 131, which covers it.
        Press(scene, new Point(382, 150));
        for (var i = 0; i < 6; i++)
        {
            clock.At(clock.Now.TotalMilliseconds + 50);
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        Release(scene, new Point(382, 150));
        clock.At(1000);

        Assert.Equal(1140 - 2 * 262, Scroll(content).Offset.Y);
    });

    // ── S9 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S9_The_Buttons_Are_18_And_The_Thumb_Its_Share_Of_The_Rest_Drawn_9_Thick() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        using var scene = new Scene(Box(content));
        var bar = Bar(content, Orientation.Vertical);

        Assert.Equal(new Rect(375, 0, 15, 18), Box(Part<RepeatButton>(bar, "PART_LineUp"), scene.Window));
        Assert.Equal(new Rect(375, 282, 15, 18), Box(Part<RepeatButton>(bar, "PART_LineDown"), scene.Window));
        var thumb = Part<Thumb>(bar, "PART_Thumb");
        // Measured: 55 at 18 for 1440 in 300; the whole bar takes the pointer.
        Assert.Equal(new Rect(375, 18, 15, 55), Box(thumb, scene.Window));
        Assert.Equal(new Rect(378, 18, 9, 55), Box(Part<Border>(thumb, "PART_ThumbFill"), scene.Window));
        Assert.Equal(new CornerRadius(4.5), Part<Border>(thumb, "PART_ThumbFill").CornerRadius);
    });

    [AvaloniaFact]
    public void S9_The_Thumb_Is_At_Least_17_Long() => InstalledTheme.Run(_ =>
    {
        var content = Tall(30000);
        using var scene = new Scene(Box(content));

        Assert.Equal(17, Part<Thumb>(Bar(content, Orientation.Vertical), "PART_Thumb").Bounds.Height);
    });

    [AvaloniaFact]
    public void S9_The_Colours_And_The_Hover() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        using var scene = new Scene(Box(content));
        var bar = Bar(content, Orientation.Vertical);
        var fill = Part<Border>(Part<Thumb>(bar, "PART_Thumb"), "PART_ThumbFill");
        var arrow = Part<RepeatButton>(bar, "PART_LineDown").GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single();
        static Color Of(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

        Assert.Equal(Color.Parse("#FCFCFC"), Of(bar.Background));
        Assert.Equal(Color.Parse("#8B8B8B"), Of(fill.Background));
        Assert.Equal(Color.Parse("#8B8B8B"), Of(arrow.Fill));

        Render();
        scene.Window.MouseMove(new Point(382, 45));
        Settle();
        Assert.Equal(Color.Parse("#636363"), Of(fill.Background));
        Assert.Equal(Color.Parse("#8B8B8B"), Of(arrow.Fill));

        scene.Window.MouseMove(new Point(382, 292));
        Settle();
        Assert.Equal(Color.Parse("#8B8B8B"), Of(fill.Background));
        Assert.Equal(Color.Parse("#636363"), Of(arrow.Fill));
    });

    [AvaloniaFact]
    public void S9_The_Thumb_Stays_Dark_While_Dragged_Off_It() => InstalledTheme.Run(_ =>
    {
        var content = Tall();
        using var scene = new Scene(Box(content));
        var fill = Part<Border>(Part<Thumb>(Bar(content, Orientation.Vertical), "PART_Thumb"), "PART_ThumbFill");

        Press(scene, new Point(382, 45));
        scene.Window.MouseMove(new Point(200, 45));
        Settle();

        Assert.Equal(Color.Parse("#636363"), Assert.IsAssignableFrom<ISolidColorBrush>(fill.Background).Color);
        Release(scene, new Point(200, 45));
    });
}
