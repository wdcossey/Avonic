using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-segment-view's and ion-segment-content's behaviours, and a segment paging them, from <c>segment-view.js</c> and
/// <c>segment.js</c>, measured in Chromium: see <c>docs/components/ion-segment/PLAN.md</c>.
/// </summary>
public class AvonicSegmentViewTests
{
    private sealed record Page(StackPanel Root, AvonicSegment Segment, AvonicSegmentView View, ScrollViewer Scroll, List<object?> Changes, List<(double Ratio, bool Manual)> Scrolls);

    private static Page Build(string? value = "v1")
    {
        var segment = new AvonicSegment { Value = value };
        foreach (var name in new[] { "v1", "v2", "v3" })
        {
            segment.Items.Add(new AvonicSegmentButton { Value = name, ContentId = name, Items = { new AvonicLabel { Content = name } } });
        }

        var view = new AvonicSegmentView { Height = 64 };
        foreach (var name in new[] { "v1", "v2", "v3" })
        {
            view.Items.Add(new AvonicSegmentContent { Name = name, Content = new TextBlock { Text = name + " content" } });
        }

        var changes = new List<object?>();
        segment.IonChange += (_, e) => changes.Add(e.Value);
        var scrolls = new List<(double, bool)>();
        view.IonSegmentViewScroll += (_, e) => scrolls.Add((e.ScrollRatio, e.IsManualScroll));
        var root = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Children = { segment, view } };
        return new Page(root, segment, view, null!, changes, scrolls);
    }

    private static Page Show(Page page, Scene scene)
    {
        Dispatcher.UIThread.RunJobs();
        GestureTests.Render();
        var scroll = page.View.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Name == "PART_Scroll");
        return page with { Scroll = scroll };
    }

    private static Point Centre(Visual visual, Scene scene) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(scene.Window)!.Value).Center;

    /// <summary>Frame by frame, with the layout pass between that raises the scroller's ScrollChanged.</summary>
    private static void Frames(TestClock clock, double until)
    {
        while (clock.Now.TotalMilliseconds + 16 <= until)
        {
            clock.At(clock.Now.TotalMilliseconds + 16);
            Dispatcher.UIThread.RunJobs();
        }

        clock.At(until);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Tap(Scene scene, Point at)
    {
        GestureTests.Render();
        scene.Window.MouseMove(at, RawInputModifiers.None);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        GestureTests.Render();
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void V4_At_Load_The_View_Jumps_To_The_Checked_Buttons_Content()
    {
        using var clock = new TestClock();
        var built = Build("v2");
        using var scene = new Scene(built.Root);
        var page = Show(built, scene);

        Assert.Equal(390, page.Scroll.Offset.X);
    }

    [AvaloniaFact]
    public void V6_Each_Content_Is_The_Views_Size()
    {
        var built = Build();
        using var scene = new Scene(built.Root);
        var page = Show(built, scene);

        Assert.All(page.View.Items.OfType<AvonicSegmentContent>(), content => Assert.Equal(new Size(390, 64), content.Bounds.Size));
        Assert.Equal(390 * 3, page.Scroll.Extent.Width);
    }

    [AvaloniaFact]
    public void V1_V2_V4_A_Tap_Scrolls_The_View_Smoothly_With_No_Hit_Testing_Until_100_ms_After()
    {
        using var clock = new TestClock();
        var built = Build();
        using var scene = new Scene(built.Root);
        var page = Show(built, scene);
        var files = page.Segment.Items.OfType<AvonicSegmentButton>().Last();

        Tap(scene, Centre(files, scene));
        Assert.Equal("v3", page.Segment.Value);
        Assert.Equal(["v3"], page.Changes);
        Assert.False(page.View.IsHitTestVisible);

        // 780 px in √780 / 60 s, eased in and out, a frame at a time.
        var start = clock.Now.TotalMilliseconds;
        var duration = Math.Sqrt(780) / 60 * 1000;
        Frames(clock, start + 16 * 15);
        Assert.Equal(780 * Components.Controls.SmoothScroll.Easing.Ease(16 * 15 / duration), page.Scroll.Offset.X, 3);
        Frames(clock, start + duration + 16);
        Assert.Equal(780, page.Scroll.Offset.X);
        Assert.NotEmpty(page.Scrolls);
        Assert.All(page.Scrolls, scroll => Assert.False(scroll.Manual));
        Assert.Equal(1, page.Scrolls[^1].Ratio);

        // Hit testing comes back 100 ms after the last scroll.
        Assert.False(page.View.IsHitTestVisible);
        Frames(clock, start + duration + 16 + 100);
        Assert.True(page.View.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void V3_A_Persons_Scroll_Moves_The_Checked_Button_Without_Scrolling_Back()
    {
        using var clock = new TestClock();
        var built = Build();
        using var scene = new Scene(built.Root);
        var page = Show(built, scene);
        clock.At(200);

        page.Scroll.Offset = new Vector(500, 0);
        Dispatcher.UIThread.RunJobs();
        // round(500 / 780 × 2) = 1. The first manual scroll raises ionChange even without a change.
        Assert.Equal("v2", page.Segment.Value);
        Assert.Equal((500 / 780.0, true), page.Scrolls[^1]);

        page.Scroll.Offset = new Vector(600, 0);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("v3", page.Segment.Value);
        page.Scroll.Offset = new Vector(620, 0);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["v2", "v3"], page.Changes);

        clock.At(1000);
        Assert.Equal(620, page.Scroll.Offset.X);
    }

    [AvaloniaFact]
    public void V3_The_First_Manual_Scroll_Raises_IonChange_Even_At_The_Same_Content()
    {
        using var clock = new TestClock();
        var built = Build();
        using var scene = new Scene(built.Root);
        var page = Show(built, scene);
        clock.At(200);

        page.Scroll.Offset = new Vector(100, 0);
        Dispatcher.UIThread.RunJobs();
        page.Scroll.Offset = new Vector(120, 0);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("v1", page.Segment.Value);
        Assert.Equal(["v1"], page.Changes);
    }

    [AvaloniaFact]
    public void V4_A_Value_In_Code_Scrolls_And_Null_Becomes_The_First_Buttons()
    {
        using var clock = new TestClock();
        var built = Build();
        using var scene = new Scene(built.Root);
        var page = Show(built, scene);

        page.Segment.Value = "v2";
        clock.At(1000);
        Assert.Equal(390, page.Scroll.Offset.X);

        page.Segment.Value = null;
        Assert.Equal("v1", page.Segment.Value);
        clock.At(2000);
        Assert.Equal(0, page.Scroll.Offset.X);
        Assert.Empty(page.Changes);
    }

    [AvaloniaFact]
    public void V5_A_Finger_Pages_The_View_And_Not_When_Swiping_Is_Off()
    {
        foreach (var swipe in new[] { true, false })
        {
            using var clock = new TestClock(timesEvents: false);
            var built = Build();
            built.View.SegmentViewSwipeGesture = swipe;
            using var scene = new Scene(built.Root);
            var page = Show(built, scene);
            var y = Centre(page.View, scene).Y;
            // Hit testing is off for 100 ms after the load's SetContent, as pointer-events is.
            clock.At(200);
            GestureTests.Render();

            var finger = scene.Window.TouchBegin(new Point(300, y), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            for (var x = 290; x >= 100; x -= 10)
            {
                scene.Window.TouchMove(finger, new Point(x, y), RawInputModifiers.None);
                Dispatcher.UIThread.RunJobs();
            }

            // 200 px, less the recogniser's start distance.
            Assert.InRange(page.Scroll.Offset.X, swipe ? 150 : 0, swipe ? 200 : 0);
            scene.Window.TouchEnd(finger, new Point(100, y), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [AvaloniaFact]
    public void V7_Disabling_The_Segment_Disables_Its_View() => InstalledTheme.Run(_ =>
    {
        var built = Build();
        using var scene = new Scene(built.Root);
        var page = Show(built, scene);

        page.Segment.IsEnabled = false;
        Assert.False(page.View.IsEnabled);
        Assert.Equal(0.3, page.View.Opacity);
        page.Segment.IsEnabled = true;
        Assert.True(page.View.IsEnabled);
    });

    // ── Found in review ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R6_A_View_That_Leaves_Mid_Scroll_Comes_Back_Taking_Input()
    {
        using var clock = new TestClock();
        var built = Build();
        var host = new Border { Child = built.Root };
        using var scene = new Scene(host);
        var page = Show(built, scene);
        clock.At(200);

        page.Segment.Value = "v3";
        Assert.False(page.View.IsHitTestVisible);
        host.Child = null;
        Dispatcher.UIThread.RunJobs();
        host.Child = built.Root;
        Dispatcher.UIThread.RunJobs();

        Assert.True(page.View.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void R7_With_Swiping_Off_A_Wheel_Still_Scrolls_A_Content_But_Not_The_View()
    {
        using var clock = new TestClock();
        var built = Build();
        built.View.SegmentViewSwipeGesture = false;
        ((AvonicSegmentContent)built.View.Items[0]!).Content = new Border { Height = 1000, Background = Avalonia.Media.Brushes.Transparent };
        using var scene = new Scene(built.Root);
        var page = Show(built, scene);
        clock.At(200);
        GestureTests.Render();
        var inner = ((AvonicSegmentContent)page.View.Items[0]!).GetVisualDescendants().OfType<ScrollViewer>().First();
        var at = Centre(page.View, scene);

        scene.Window.MouseWheel(at, new Vector(0, -1), RawInputModifiers.None);
        scene.Window.MouseWheel(at, new Vector(-1, 0), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(inner.Offset.Y > 0);
        Assert.Equal(0, page.Scroll.Offset.X);
    }

    [AvaloniaFact]
    public void R8_A_Resized_View_Keeps_Its_Content_The_Views_Width_And_In_View()
    {
        using var clock = new TestClock();
        var built = Build("v2");
        var host = new Border { Width = 390, HorizontalAlignment = HorizontalAlignment.Left, Child = built.Root };
        using var scene = new Scene(host);
        var page = Show(built, scene);
        Assert.Equal(390, page.Scroll.Offset.X);

        host.Width = 300;
        Dispatcher.UIThread.RunJobs();
        GestureTests.Render();

        Assert.All(page.View.Items.OfType<AvonicSegmentContent>(), content => Assert.Equal(300, content.Bounds.Width));
        Assert.Equal(300, page.Scroll.Offset.X);
    }

    [AvaloniaFact]
    public void R12_A_View_Narrowed_On_Its_Last_Content_Keeps_It()
    {
        using var clock = new TestClock();
        var built = Build("v3");
        var host = new Border { Width = 390, HorizontalAlignment = HorizontalAlignment.Left, Child = built.Root };
        using var scene = new Scene(host);
        var page = Show(built, scene);
        Assert.Equal(780, page.Scroll.Offset.X);

        host.Width = 300;
        Dispatcher.UIThread.RunJobs();
        GestureTests.Render();

        Assert.Equal(600, page.Scroll.Offset.X);
        Assert.Equal("v3", page.Segment.Value);
    }
}
