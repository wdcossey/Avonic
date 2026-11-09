using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Behaviours 1–11 from docs/components/ion-skeleton-text/PLAN.md, read from skeleton-text.js and skeleton-text.css and
/// measured in Chromium at 358 wide. The numbers are Ionic's, not the implementation's.
/// </summary>
public class AvonicSkeletonTextTests
{
    /// <summary>A white page at (16, 16), 358 wide, at 16 px, as Chromium's body.</summary>
    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { Margin = new Thickness(16), Background = Brushes.White };
        TextElement.SetFontSize(page, 16);
        page.Children.AddRange(children);
        return page;
    }

    private static Scene Show(Control content, double height = 844)
    {
        var scene = new Scene(new Panel { Background = Brushes.White, Children = { content } }, 390, height);
        scene.Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    /// <summary>The red channel of the row through the middle of <paramref name="skeleton"/>, at x from its left.</summary>
    private static int[] Row(Scene scene, AvonicSkeletonText skeleton, params int[] xs)
    {
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        var origin = skeleton.TranslatePoint(new Point(0, skeleton.Bounds.Height / 2), scene.Window)!.Value;
        return xs.Select(x => (int)Frames.PixelAt(frame, (int)origin.X + x, (int)origin.Y).R).ToArray();
    }

    /// <summary>The red channel at (x, y) in <paramref name="control"/>.</summary>
    private static int PixelIn(Scene scene, Control control, double x, double y)
    {
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        var point = control.TranslatePoint(new Point(x, y), scene.Window)!.Value;
        return Frames.PixelAt(frame, (int)point.X, (int)point.Y).R;
    }

    // ── 1. The box ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_A_Block_As_Wide_As_Its_Parent_10_High_With_4_Above_And_Below_Whatever_The_Font() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicSkeletonText();
        var big = new AvonicSkeletonText();
        var small = new AvonicSkeletonText();
        var page = Page(plain, new Panel { Children = { big } }, new Panel { Children = { small } });
        TextElement.SetFontSize((Panel)page.Children[1], 40);
        TextElement.SetFontSize((Panel)page.Children[2], 8);
        TextElement.SetFontFamily((Panel)page.Children[2], new FontFamily("serif"));
        using var scene = Show(page);

        // Measured s1, s2, s2b: 358 × 10 at y 20, 34 and 48 in the window
        Assert.Equal(new Rect(0, 4, 358, 10), plain.Bounds);
        Assert.Equal(new Size(358, 10), big.Bounds.Size);
        Assert.Equal(new Size(358, 10), small.Bounds.Size);
        Assert.Equal(new Thickness(0, 4), plain.Margin);
        Assert.Equal(18, page.Children[1].Bounds.Y);
        Assert.Equal(36, page.Children[2].Bounds.Y);
    });

    [AvaloniaFact]
    public void B1_In_A_Taller_Box_It_Stays_10_High_At_The_Top() => InstalledTheme.Run(_ =>
    {
        var skeleton = new AvonicSkeletonText();
        var tall = new Panel { MinHeight = 50, Children = { skeleton } };
        using var scene = Show(Page(tall));

        // A block with height auto: at the top, as in a div that something else makes taller
        Assert.Equal(new Rect(0, 4, 358, 10), skeleton.Bounds);
    });

    // ── 2. height: inherit ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_A_Parent_With_A_Height_Gives_It_That_Height_And_Its_Own_Size_Wins() => InstalledTheme.Run(_ =>
    {
        var inherits = new AvonicSkeletonText();
        var own = new AvonicSkeletonText { Width = 100, Height = 30, HorizontalAlignment = HorizontalAlignment.Left };
        var parent = new Panel { Height = 50, Children = { inherits } };
        using var scene = Show(Page(parent, own));

        // Measured s3: 358 × 50 in a parent 50 high; s8: 100 × 30
        Assert.Equal(new Size(358, 50), inherits.Bounds.Size);
        Assert.Equal(new Size(100, 30), own.Bounds.Size);

        parent.Height = double.NaN;
        scene.Window.UpdateLayout();
        Assert.Equal(new Size(358, 10), inherits.Bounds.Size);
    });

    // ── 3. Background ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_The_Background_Is_The_Text_Colour_At_0065_And_The_Keys_Change_It() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicSkeletonText();
        var red = new AvonicSkeletonText();
        var set = new AvonicSkeletonText();
        var redPanel = new Panel { Children = { red } };
        redPanel.Resources["Avonic.SkeletonText.BackgroundRgb"] = Color.FromRgb(255, 0, 0);
        var setPanel = new Panel { Children = { set } };
        setPanel.Resources["Avonic.SkeletonText.Background"] = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        setPanel.Resources["Avonic.SkeletonText.BackgroundRgb"] = Color.FromRgb(0, 0, 255);
        using var scene = Show(Page(plain, redPanel, setPanel));

        // Measured: rgba(0, 0, 0, 0.067), 17/255; s6 rgba(255, 0, 0, 0.067); s5's --background beats the rgb
        Assert.Equal(Color.FromArgb(17, 0, 0, 0), ((ISolidColorBrush)plain.Background!).Color);
        Assert.Equal(Color.FromArgb(17, 255, 0, 0), ((ISolidColorBrush)red.Background!).Color);
        Assert.Equal(Color.FromArgb(128, 255, 0, 0), ((ISolidColorBrush)set.Background!).Color);
        Assert.Equal([238], Row(scene, plain, 100));
    });

    [AvaloniaFact]
    public void B3_The_Text_Colour_Follows_The_Palette() => InstalledTheme.Run(_ =>
    {
        var skeleton = new AvonicSkeletonText();
        var panel = new Panel { Children = { skeleton } };
        panel.Resources["Avonic.TextColor"] = Color.FromRgb(255, 255, 255);
        using var scene = Show(Page(panel));
        Assert.Equal(Color.FromArgb(17, 255, 255, 255), ((ISolidColorBrush)skeleton.Background!).Color);
    });

    // ── 4. Radius ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_The_Radius_Is_The_Parents_Unless_The_Key_Sets_It() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicSkeletonText();
        var inRound = new AvonicSkeletonText();
        var keyed = new AvonicSkeletonText();
        var percent = new AvonicSkeletonText { Height = 20 };
        var round = new Border { CornerRadius = new CornerRadius(12), Child = inRound };
        var keyedPanel = new Panel { Children = { keyed } };
        keyedPanel.Resources["Avonic.SkeletonText.BorderRadius"] = "6px";
        var percentPanel = new Panel { Children = { percent } };
        percentPanel.Resources["Avonic.SkeletonText.BorderRadius"] = "50%";
        var keyedInRound = new AvonicSkeletonText();
        var roundKeyed = new Border { CornerRadius = new CornerRadius(12), Child = keyedInRound };
        roundKeyed.Resources["Avonic.SkeletonText.BorderRadius"] = 3.0;
        using var scene = Show(Page(plain, round, keyedPanel, percentPanel, roundKeyed));

        // Measured: 0; s4 12 from its parent; s5 6
        Assert.Equal(default, plain.CornerRadius);
        Assert.Equal(new CornerRadius(12), inRound.CornerRadius);
        // 6, which CSS also draws at 5, as two corners can't overlap on a box 10 high
        Assert.Equal(new CornerRadius(5), keyed.CornerRadius);
        Assert.Equal(new CornerRadius(10), percent.CornerRadius);
        Assert.Equal(new CornerRadius(3), keyedInRound.CornerRadius);

        // The corners are cut: white at the corner of the one in a 12 radius, grey in its middle
        Assert.InRange(PixelIn(scene, inRound, 0.5, 0.5), 250, 255);
        Assert.Equal(238, PixelIn(scene, inRound, 100, 0.5));
        round.CornerRadius = new CornerRadius(2);
        scene.Window.UpdateLayout();
        Assert.Equal(new CornerRadius(2), inRound.CornerRadius);
    });

    // ── 5. The shimmer ───────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(0, new[] { 238, 238, 238, 238, 238, 238, 238, 238, 238 })]
    [InlineData(250, new[] { 229, 234, 238, 238, 238, 238, 238, 238, 238 })]
    [InlineData(500, new[] { 238, 238, 238, 230, 221, 229, 238, 238, 238 })]
    [InlineData(750, new[] { 238, 238, 238, 238, 238, 238, 238, 230, 223 })]
    public void B5_Animated_The_Band_Moves_800_A_Second_From_Minus_400(double ms, int[] measured) => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var skeleton = new AvonicSkeletonText { Animated = true, Height = 20 };
        using var scene = Show(Page(skeleton));

        clock.At(ms);
        skeleton.Tick();

        // Measured in Chromium, the red channel on white, at these x
        var row = Row(scene, skeleton, 0, 32, 64, 100, 144, 200, 264, 300, 357);
        Assert.All(row.Zip(measured), pair => Assert.InRange(pair.First, pair.Second - 1, pair.Second + 1));
        Assert.Equal(-400 + 800 * ms / 1000, skeleton.ShimmerOffset);
    });

    [AvaloniaFact]
    public void B5_In_Right_To_Left_The_Band_Still_Runs_From_The_Left() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var skeleton = new AvonicSkeletonText { Animated = true, Height = 20 };
        var page = Page(skeleton);
        page.FlowDirection = FlowDirection.RightToLeft;
        using var scene = Show(page);
        clock.At(250);
        skeleton.Tick();

        // Measured in Chromium with dir=rtl at 250 ms: the same as left to right, from the left: background-position is physical
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        var y = (int)skeleton.TranslatePoint(new Point(0, 10), scene.Window)!.Value.Y;
        var row = new[] { 0, 32, 64, 100, 300, 357 }.Select(x => (int)Frames.PixelAt(frame, 16 + x, y).R).ToArray();
        Assert.All(row.Zip(new[] { 229, 234, 238, 238, 238, 238 }), pair => Assert.InRange(pair.First, pair.Second - 1, pair.Second + 1));
    });

    [AvaloniaFact]
    public void B5_Animated_It_Ignores_The_Background_Key_And_Reads_The_Rgb() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var skeleton = new AvonicSkeletonText { Animated = true, Height = 20 };
        var panel = new Panel { Children = { skeleton } };
        panel.Resources["Avonic.SkeletonText.Background"] = new SolidColorBrush(Colors.Blue);
        // As a brush, as most of Avonic's colour keys are: its colour is read
        panel.Resources["Avonic.SkeletonText.BackgroundRgb"] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        using var scene = Show(Page(panel));
        clock.At(500);
        skeleton.Tick();

        // Measured s7: the gradient in red, and no blue. Red stays 255; green at the peak is 255 × (1 − 0.135)
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        var origin = skeleton.TranslatePoint(new Point(144, 10), scene.Window)!.Value;
        var peak = Frames.PixelAt(frame, (int)origin.X, (int)origin.Y);
        Assert.Equal(255, peak.R);
        Assert.InRange(peak.G, 220, 222);
        Assert.Equal(peak.G, peak.B);
    });

    // ── 6. The global config ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_The_Config_Is_Read_At_The_First_Attach_And_When_Animated_Changes() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        AvonicConfig.Animated = false;
        try
        {
            var skeleton = new AvonicSkeletonText { Animated = true };
            var page = Page(skeleton);
            using var scene = Show(page);
            Assert.False(skeleton.IsFrameLoopRunning);
            Assert.Equal(Color.FromArgb(17, 0, 0, 0), ((ISolidColorBrush)skeleton.Background!).Color);

            // Measured: config.set('animated', true) alone changes nothing
            AvonicConfig.Animated = true;
            page.Children.Remove(skeleton);
            page.Children.Add(skeleton);
            scene.Window.UpdateLayout();
            Assert.False(skeleton.IsFrameLoopRunning);

            // animated off and on again renders, and reads it
            skeleton.Animated = false;
            skeleton.Animated = true;
            Assert.True(skeleton.IsFrameLoopRunning);
            Assert.Null(skeleton.Background);

            // Measured: off again, then attached again, it keeps the shimmer
            AvonicConfig.Animated = false;
            page.Children.Remove(skeleton);
            page.Children.Add(skeleton);
            scene.Window.UpdateLayout();
            Assert.True(skeleton.IsFrameLoopRunning);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    });

    // ── 7. In media ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_In_An_Avatar_Or_A_Thumbnail_It_Fills_With_No_Margin_A_Circle_In_The_Avatar() => InstalledTheme.Run(_ =>
    {
        var inAvatar = new AvonicSkeletonText();
        var deep = new AvonicSkeletonText();
        var inThumbnail = new AvonicSkeletonText();
        var avatar = new AvonicAvatar { Content = inAvatar };
        var thumbnail = new AvonicThumbnail { Content = new Panel { Children = { deep } } };
        var other = new AvonicThumbnail { Content = inThumbnail };
        using var scene = Show(Page(avatar, thumbnail, other));

        // Measured: in-media, margin 0, the box's size; 50% in an avatar, from the avatar
        Assert.Equal(new Rect(0, 0, 64, 64), inAvatar.Bounds);
        Assert.Equal(new CornerRadius(32), inAvatar.CornerRadius);
        Assert.Equal(new Rect(0, 0, 48, 48), deep.Bounds);
        Assert.Equal(new Rect(0, 0, 48, 48), inThumbnail.Bounds);
        Assert.Equal(default, inThumbnail.CornerRadius);

        // The circle: white at the corner, grey in the middle
        Assert.Equal(255, PixelIn(scene, inAvatar, 2, 2));
        Assert.Equal(238, PixelIn(scene, inAvatar, 32, 32));
    });

    [AvaloniaFact]
    public void B7_Moved_Later_It_Keeps_What_It_Had_At_Its_First_Render() => InstalledTheme.Run(_ =>
    {
        var moved = new AvonicSkeletonText();
        var thumbnail = new AvonicThumbnail { Content = moved };
        var stays = new AvonicSkeletonText();
        var page = Page(thumbnail, stays);
        using var scene = Show(page);

        // Measured b: moved out of a thumbnail it keeps in-media, at 390 × 10 with no margin in a 390 div
        thumbnail.Content = null;
        page.Children.Add(moved);
        scene.Window.UpdateLayout();
        Assert.Equal(new Thickness(0), moved.Margin);
        Assert.Equal(new Size(358, 10), moved.Bounds.Size);

        // Measured c: made outside and moved in, it doesn't get it
        page.Children.Remove(stays);
        var other = new AvonicThumbnail { Content = stays };
        page.Children.Add(other);
        scene.Window.UpdateLayout();
        Assert.Equal(new Thickness(0, 4), stays.Margin);
    });

    // ── 8. ionStyle ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_It_Tells_An_Item_Once_So_That_The_Item_Clips_Its_Label() => InstalledTheme.Run(_ =>
    {
        var skeleton = new AvonicSkeletonText();
        var label = new AvonicLabel { Content = new StackPanel { Children = { skeleton } } };
        var item = new AvonicItem { Content = label };
        var told = new List<IReadOnlyDictionary<string, bool>>();
        var page = Page(item);
        page.AddHandler(IonStyle.IonStyleEvent, (_, e) => told.Add(e.Styles), handledEventsToo: true);
        using var scene = Show(page);

        // Measured: item-skeleton-text on the item, overflow: hidden on the label
        Assert.Contains(":item-skeleton-text", item.Classes);
        // Set, not the default a TemplatedControl clips by, which FlexRow doesn't count
        Assert.True(label.IsSet(Visual.ClipToBoundsProperty));
        Assert.True(label.ClipToBounds);
        Assert.Single(told, styles => styles.ContainsKey("skeleton-text") && styles["skeleton-text"]);

        // Once: componentWillLoad
        var panel = (StackPanel)label.Content!;
        panel.Children.Remove(skeleton);
        panel.Children.Add(skeleton);
        scene.Window.UpdateLayout();
        Assert.Single(told, styles => styles.ContainsKey("skeleton-text"));
    });

    // ── 9. Hit testing ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_A_Press_Goes_To_What_Is_Under_It() => InstalledTheme.Run(_ =>
    {
        var skeleton = new AvonicSkeletonText();
        var under = new Border { Background = Brushes.White, Child = skeleton };
        using var scene = Show(Page(under));
        Dispatcher.UIThread.RunJobs();
        scene.Window.CaptureRenderedFrame();

        object? source = null;
        under.PointerPressed += (_, e) => source = e.Source;
        scene.Window.MouseDown(skeleton.TranslatePoint(new Point(100, 5), scene.Window)!.Value, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(skeleton.TranslatePoint(new Point(100, 5), scene.Window)!.Value, MouseButton.Left, RawInputModifiers.None);
        Assert.Same(under, source);
    });

    // ── 10. Frames ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Detaching_Stops_The_Frames_And_Attaching_Restarts_The_Band() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var skeleton = new AvonicSkeletonText { Animated = true };
        var plain = new AvonicSkeletonText();
        var page = Page(skeleton, plain);
        using var scene = Show(page);
        Assert.True(skeleton.IsFrameLoopRunning);
        Assert.False(plain.IsFrameLoopRunning);

        clock.At(300);
        Assert.Equal(-400 + 800 * 0.288, skeleton.ShimmerOffset, 6);

        page.Children.Remove(skeleton);
        Assert.False(skeleton.IsFrameLoopRunning);
        clock.At(700);
        page.Children.Add(skeleton);
        Assert.True(skeleton.IsFrameLoopRunning);
        Assert.Equal(-400, skeleton.ShimmerOffset);
        clock.At(950);
        skeleton.Tick();
        Assert.Equal(-400 + 800 * 0.25, skeleton.ShimmerOffset, 6);
    });

    [AvaloniaFact]
    public void B10_Hiding_Stops_The_Frames_And_Showing_Restarts_The_Band_But_Not_Within_One_Frame() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var skeleton = new AvonicSkeletonText { Animated = true };
        var wrap = new Panel { Children = { skeleton } };
        using var scene = Show(Page(wrap));
        clock.At(200);

        // Hidden and shown between two frames: nothing restarts
        wrap.IsVisible = false;
        wrap.IsVisible = true;
        clock.At(400);
        Assert.Equal(-400 + 800 * 0.4, skeleton.ShimmerOffset, 6);

        // A frame that finds it hidden stops
        wrap.IsVisible = false;
        clock.At(500);
        Assert.False(skeleton.IsFrameLoopRunning);

        wrap.IsVisible = true;
        Assert.True(skeleton.IsFrameLoopRunning);
        Assert.Equal(-400, skeleton.ShimmerOffset);
        clock.At(600);
        skeleton.Tick();
        Assert.Equal(-400 + 800 * 0.1, skeleton.ShimmerOffset, 6);
    });

    [AvaloniaFact]
    public void B10_Animated_Off_Stops_The_Frames_And_On_Again_Restarts_The_Band() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var skeleton = new AvonicSkeletonText { Animated = true };
        using var scene = Show(Page(skeleton));
        clock.At(300);

        skeleton.Animated = false;
        Assert.False(skeleton.IsFrameLoopRunning);
        Assert.Equal(Color.FromArgb(17, 0, 0, 0), ((ISolidColorBrush)skeleton.Background!).Color);

        clock.At(450);
        skeleton.Animated = true;
        Assert.Equal(-400, skeleton.ShimmerOffset);
        clock.At(700);
        skeleton.Tick();
        Assert.Equal(-400 + 800 * 0.25, skeleton.ShimmerOffset, 6);
    });

    // ── 11. Automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_It_Is_Not_In_The_Automation_Tree()
    {
        var skeleton = new AvonicSkeletonText();
        using var scene = new Scene(skeleton);
        var peer = ControlAutomationPeer.CreatePeerForElement(skeleton);
        Assert.False(peer.IsControlElement());
        Assert.False(peer.IsContentElement());
    }
}
