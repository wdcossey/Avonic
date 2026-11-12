using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The behaviours in docs/components/ion-item-sliding/PLAN.md, read from <c>item-sliding.js</c>, <c>item-options.js</c> and
/// <c>item-option.js</c> and measured in Chromium. Drags are real touches on the headless window, on the test clock, so the
/// gesture's capture and frames run as they do in an app.
/// </summary>
public class AvonicItemSlidingTests
{
    // End options 70 + 80 = 150 wide, start options 80, an item 48 high, in a 390 wide window.
    private const double EndWidth = 150;
    private const double StartWidth = 80;
    private const double Y = 24;

    private sealed class Fixture : IDisposable
    {
        public required AvonicItemSliding Sliding { get; init; }
        public required AvonicItem Item { get; init; }
        public AvonicItemOptions? Start { get; init; }
        public AvonicItemOptions? End { get; init; }
        public required Scene Scene { get; init; }
        public required TestClock Clock { get; init; }
        public List<(double Amount, double Ratio)> Drags { get; } = [];
        public List<ItemOptionsSide> Swipes { get; } = [];

        /// <summary>Where the item is drawn, in the window: its render transform included.</summary>
        public double ItemX => Item.TranslatePoint(default, Scene.Window)!.Value.X;

        public GestureTests.Finger Finger() => new(Scene, Clock);

        public void At(double ms)
        {
            Clock.At(ms);
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    /// <summary>An option as wide as its content, as Ionic's are, with content of a set width, so that the stubs give exact numbers.</summary>
    private static AvonicItemOption Option(double width, string text, bool expandable = false) =>
        new() { Content = new Border { Width = width, Height = 10, Tag = text }, Expandable = expandable };

    private static Fixture Make(bool start = true, bool end = true, FlowDirection flow = FlowDirection.LeftToRight,
        Func<AvonicItemSliding, Control>? wrap = null, AvonicItem? item = null)
    {
        var clock = new TestClock();
        item ??= new AvonicItem { MinHeight = 48, Content = "Item" };
        var startOptions = start ? new AvonicItemOptions { Side = ItemOptionsSide.Start, Items = { Option(StartWidth, "Archive") } } : null;
        var endOptions = end ? new AvonicItemOptions { Items = { Option(70, "Favorite"), Option(80, "Delete", expandable: true) } } : null;
        var sliding = new AvonicItemSliding { FlowDirection = flow };
        if (startOptions is not null)
        {
            sliding.Items.Add(startOptions);
        }

        sliding.Items.Add(item);
        if (endOptions is not null)
        {
            sliding.Items.Add(endOptions);
        }

        var scene = new Scene(wrap?.Invoke(sliding) ?? new StackPanel { Children = { sliding } });
        GestureTests.Render();
        var fixture = new Fixture { Sliding = sliding, Item = item, Start = startOptions, End = endOptions, Scene = scene, Clock = clock };
        sliding.IonDrag += (_, e) => fixture.Drags.Add((e.Amount, e.Ratio));
        sliding.AddHandler(AvonicItemOptions.IonSwipeEvent, (_, e) => fixture.Swipes.Add(e.Side));
        return fixture;
    }

    /// <summary>
    /// A finger from <paramref name="from"/> to <paramref name="to"/> in 10 px steps a frame apart, then held still for six frames,
    /// so the velocity has died away, then lifted. The gesture captures at the first step.
    /// </summary>
    private static double SlowDrag(Fixture f, double t, double from, double to, bool lift = true) => SlowDrag(f, t, from, to, lift, out _);

    private static double SlowDrag(Fixture f, double t, double from, double to, bool lift, out GestureTests.Finger finger)
    {
        finger = f.Finger().Down(from, Y, t);
        var step = Math.Sign(to - from) * 10;
        var x = from;
        while (Math.Abs(to - x) > 0.001)
        {
            t += 16;
            x = Math.Abs(to - x) < 10 ? to : x + step;
            finger.Move(x, Y, t);
        }

        for (var i = 0; i < 6; i++)
        {
            t += 16;
            finger.Move(x, Y, t);
        }

        t += 16;
        if (lift)
        {
            finger.Up(x, Y, t);
        }
        else
        {
            f.At(t);
        }

        return t;
    }

    private static Point Centre(Visual visual, Visual root) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root)!.Value).Center;

    // ── S1–S3: the drag ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S01_A_Drag_Moves_The_Item_From_The_Capturing_Move_At_Once()
    {
        using var f = Make();
        // Captured at 290, 10 px in (threshold 5), so 200 is 90 from it.
        f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32);
        f.At(48);

        Assert.Equal(90, f.Sliding.GetOpenAmount(), 3);
        Assert.Equal(-90, f.ItemX, 3);
    }

    [AvaloniaFact]
    public void S02_It_Only_Opens_Towards_A_Side_With_Options()
    {
        using (var f = Make(start: false))
        {
            f.Finger().Down(100, Y, 0).Move(110, Y, 16).Move(200, Y, 32);
            f.At(48);
            Assert.Equal(0, f.Sliding.GetOpenAmount());
            Assert.Equal(0, f.ItemX);
        }

        using (var f = Make(end: false))
        {
            f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32);
            f.At(48);
            Assert.Equal(0, f.Sliding.GetOpenAmount());
        }

        using (var f = Make(start: false, end: false))
        {
            f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32);
            f.At(48);
            Assert.Equal(0, f.ItemX);
            Assert.Empty(f.Drags);
        }
    }

    [AvaloniaFact]
    public void S03_Past_The_Options_Width_It_Is_Elastic()
    {
        using var f = Make();
        f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(90, Y, 32);
        f.At(48);

        // 200 raw: 150 + 50 × 0.55
        Assert.Equal(EndWidth + 50 * AvonicItemSliding.ElasticFactor, f.Sliding.GetOpenAmount(), 3);
        Assert.Equal(-(EndWidth + 50 * AvonicItemSliding.ElasticFactor), f.ItemX, 3);
    }

    [AvaloniaFact]
    public void S03_Past_The_Start_Options_Width_It_Is_Elastic_Too()
    {
        using var f = Make();
        f.Finger().Down(100, Y, 0).Move(110, Y, 16).Move(230, Y, 32);
        f.At(48);

        // 120 raw to the right: −80 − 40 × 0.55
        Assert.Equal(-StartWidth - 40 * AvonicItemSliding.ElasticFactor, f.Sliding.GetOpenAmount(), 3);
    }

    // ── S4–S7: the release ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S04_S06_A_Full_Swipe_Released_Rests_Open_And_Raises_IonSwipe_On_Those_Options()
    {
        using var f = Make();
        // Raw 260 is 150 + 110 × 0.55 = 210.5, past 150 + 30.
        var t = SlowDrag(f, 0, 300, 30);

        Assert.Equal([ItemOptionsSide.End], f.Swipes);
        Assert.Equal(EndWidth, f.Sliding.GetOpenAmount(), 3);
        f.At(t + 600);
        Assert.Equal(-EndWidth, f.ItemX, 3);
    }

    [AvaloniaFact]
    public void S04_Just_Short_Of_The_Swipe_Margin_There_Is_No_IonSwipe()
    {
        using var f = Make();
        // 150 + (raw − 150) × 0.55 = 179 at a raw 202.7: under 180. Moves are 10 px, so stop at raw 200 (177.5).
        SlowDrag(f, 0, 300, 100);

        Assert.Empty(f.Swipes);
        Assert.Equal(EndWidth, f.Sliding.GetOpenAmount(), 3);
    }

    [AvaloniaFact]
    public void S05_A_Slow_Release_Opens_Past_Half_And_Closes_Under_It()
    {
        using (var f = Make())
        {
            SlowDrag(f, 0, 300, 210); // 80 open, over 75
            Assert.Equal(EndWidth, f.Sliding.GetOpenAmount(), 3);
        }

        using (var f = Make())
        {
            SlowDrag(f, 0, 300, 230); // 60 open, under 75
            Assert.Equal(0, f.Sliding.GetOpenAmount());
        }
    }

    [AvaloniaFact]
    public void S05_A_Fast_Release_Follows_Its_Direction_Wherever_It_Is()
    {
        using (var f = Make())
        {
            // 40 open, under half, but moving fast towards open: opens.
            f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(270, Y, 32).Move(250, Y, 48).Up(250, Y, 64);
            Assert.Equal(EndWidth, f.Sliding.GetOpenAmount(), 3);
        }
    }

    [AvaloniaFact]
    public void S05_A_Fast_Flick_Back_Closes_An_Item_Dragged_Past_Half()
    {
        using var f = Make();
        var finger = f.Finger().Down(300, Y, 0).Move(290, Y, 16);
        double t = 16, x = 290;
        for (var i = 0; i < 12; i++)
        {
            t += 16;
            x -= 10;
            finger.Move(x, Y, t); // to 170: 120 open
        }

        for (var i = 0; i < 6; i++)
        {
            t += 16;
            finger.Move(x, Y, t);
        }

        // 30 px back in two frames: 0.94 px/ms towards closed, and still 90 open, over half.
        finger.Move(x + 15, Y, t + 16).Move(x + 30, Y, t + 32).Up(x + 30, Y, t + 32);
        Assert.Equal(0, f.Sliding.GetOpenAmount());
    }

    [AvaloniaFact]
    public void S07_IonDrag_Carries_Amount_And_Ratio_And_Is_Not_Raised_For_Closing()
    {
        using var f = Make();
        f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32).Move(260, Y, 48).Up(260, Y, 64);

        Assert.Contains((90.0, 90 / EndWidth), f.Drags);
        var count = f.Drags.Count;
        f.Sliding.Close();
        f.At(2000);
        Assert.Equal(count, f.Drags.Count);
        Assert.DoesNotContain(f.Drags, d => d.Amount == 0);
    }

    // ── S8–S10: movement, closing, input ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S08_It_Eases_To_Where_It_Rests_Over_500_Ms()
    {
        using var f = Make();
        var t = SlowDrag(f, 0, 300, 210); // released at 80, rests at 150
        var easing = AvonicItemSliding.TransitionEasing;

        // Frames are 16 ms apart from the release, so the frame at 256 ms is the one shown.
        f.At(t + 256);
        Assert.Equal(-(80 + 70 * easing.Ease(0.512)), f.ItemX, 3);
        f.At(t + 512);
        Assert.Equal(-EndWidth, f.ItemX, 3);
    }

    [AvaloniaFact]
    public void S08_A_Drag_That_Starts_While_It_Eases_Takes_It_From_Where_It_Was_Going()
    {
        using var f = Make();
        f.Sliding.Open(ItemOptionsSide.End);
        f.At(116);
        Assert.True(f.ItemX > -EndWidth + 5);

        // style.transition = 'none' at the capture ends the transition at its end value.
        var finger = f.Finger().Down(300, Y, 120).Move(290, Y, 136);
        Assert.Equal(-EndWidth, f.ItemX, 3);
        finger.Move(280, Y, 152);
        f.At(168);
        Assert.Equal(EndWidth + 10 * AvonicItemSliding.ElasticFactor, f.Sliding.GetOpenAmount(), 3);
    }

    [AvaloniaFact]
    public void S08_A_Drag_Started_And_Released_While_It_Eases_Leaves_It_Where_It_Was_Going()
    {
        using var f = Make();
        f.Sliding.Open(ItemOptionsSide.End);
        f.At(116);

        // Captured and lifted with no move in between: the ease was ended at its end value, and there's nowhere else to go.
        f.Finger().Down(300, Y, 120).Move(290, Y, 136).Up(290, Y, 136);
        f.At(200);
        Assert.Equal(-EndWidth, f.ItemX, 3);
    }

    [AvaloniaFact]
    public void S09_Closing_Keeps_The_Options_Shown_Without_Input_And_The_Gesture_Off_For_600_Ms()
    {
        using var f = Make();
        f.Sliding.Open(ItemOptionsSide.End);
        f.At(1000);
        Assert.True(f.End!.IsEffectivelyVisible);

        f.Sliding.Close();
        f.At(1300);
        GestureTests.Render();
        Assert.True(f.End.IsEffectivelyVisible);
        var option = f.End.Items.OfType<AvonicItemOption>().Last();
        var hit = f.Scene.Window.InputHitTest(Centre(option, f.Scene.Window)) as Visual;
        Assert.False(hit is not null && (hit == option || hit.GetVisualAncestors().Contains(option)));

        // A drag now does nothing.
        f.Finger().Down(300, Y, 1400).Move(290, Y, 1416).Move(200, Y, 1432).Up(200, Y, 1448);
        Assert.Equal(0, f.Sliding.GetOpenAmount());

        f.At(1600);
        Assert.False(f.End.IsEffectivelyVisible);
        f.Finger().Down(300, Y, 2000).Move(290, Y, 2016).Move(200, Y, 2032);
        f.At(2048);
        Assert.Equal(90, f.Sliding.GetOpenAmount(), 3);
    }

    [AvaloniaFact]
    public void S09_A_Drag_Back_Past_Closed_Ends_The_Drag_And_An_Open_Soon_After_Stays_Open()
    {
        using var f = Make(start: false);
        // Right, with only end options: the amount is held at 0, which closes, and ends the drag (Ionic's gesture.enable(false)).
        f.Finger().Down(100, Y, 0).Move(110, Y, 16).Move(150, Y, 32);
        f.At(48);
        Assert.Equal(0, f.Sliding.GetOpenAmount());

        f.Sliding.Open(ItemOptionsSide.End);
        f.At(700);
        Assert.Equal(EndWidth, f.Sliding.GetOpenAmount(), 3);
        Assert.True(f.End!.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void S10_While_Open_A_Tap_On_The_Item_Clicks_Nothing_In_It()
    {
        var item = new AvonicItem { MinHeight = 48, Content = "Item", Button = true };
        using var f = Make(item: item);
        var clicks = 0;
        item.Click += (_, _) => clicks++;

        f.Finger().Down(100, Y, 0).Up(100, Y, 16);
        Assert.Equal(1, clicks);

        f.Sliding.Open(ItemOptionsSide.End);
        f.At(1000);
        f.Finger().Down(100, Y, 1100).Up(100, Y, 1116);
        Assert.Equal(1, clicks);
        Assert.Equal(EndWidth, f.Sliding.GetOpenAmount(), 3);
    }

    [AvaloniaFact]
    public void S11_Pressing_Another_Closes_The_Open_One_And_Its_Tap_Still_Clicks()
    {
        using var clock = new TestClock();
        var a = new AvonicItemSliding { Items = { new AvonicItem { MinHeight = 48, Content = "A" }, new AvonicItemOptions { Items = { Option(70, "X") } } } };
        var bItem = new AvonicItem { MinHeight = 48, Content = "B", Button = true };
        var b = new AvonicItemSliding { Items = { bItem, new AvonicItemOptions { Items = { Option(70, "Y") } } } };
        using var scene = new Scene(new StackPanel { Children = { a, b } });
        GestureTests.Render();
        var clicks = 0;
        bItem.Click += (_, _) => clicks++;

        a.Open();
        clock.At(1000);
        Assert.Equal(70, a.GetOpenAmount(), 3);

        new GestureTests.Finger(scene, clock).Down(100, 72, 1100).Up(100, 72, 1116);
        Assert.Equal(0, a.GetOpenAmount());
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void S12_A_Press_Within_15_Px_Of_The_Left_Edge_Does_Not_Start()
    {
        using var f = Make();
        f.Finger().Down(14, Y, 0).Move(24, Y, 16).Move(100, Y, 32);
        f.At(48);
        Assert.Equal(0, f.Sliding.GetOpenAmount());

        f.Finger().Down(16, Y, 100).Move(26, Y, 116).Move(66, Y, 132);
        f.At(148);
        Assert.Equal(-40, f.Sliding.GetOpenAmount(), 3);
    }

    // ── S13–S15: methods ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S13_Open_Opens_A_Side_At_The_Next_Frame_With_The_Ease()
    {
        using var f = Make();
        f.Sliding.Open(ItemOptionsSide.End);
        Assert.Equal(0, f.Sliding.GetOpenAmount());
        f.At(16);
        Assert.Equal(EndWidth, f.Sliding.GetOpenAmount(), 3);
        Assert.True(f.ItemX > -EndWidth + 1);
        f.At(532);
        Assert.Equal(-EndWidth, f.ItemX, 3);

        // The same side again does nothing.
        var drags = f.Drags.Count;
        f.Sliding.Open(ItemOptionsSide.End);
        f.At(600);
        Assert.Equal(drags, f.Drags.Count);
    }

    [AvaloniaFact]
    public void S13_Open_With_No_Side_Opens_The_First_Found_And_A_Side_Without_Options_Does_Nothing()
    {
        using (var f = Make())
        {
            f.Sliding.Open();
            f.At(1000);
            Assert.Equal(-StartWidth, f.Sliding.GetOpenAmount(), 3);
        }

        using (var f = Make(start: false))
        {
            f.Sliding.Open(ItemOptionsSide.Start);
            f.At(1000);
            Assert.Equal(0, f.Sliding.GetOpenAmount());
        }
    }

    [AvaloniaFact]
    public void S13_Open_Closes_Any_Other_Open_Item_First()
    {
        using var clock = new TestClock();
        var a = new AvonicItemSliding { Items = { new AvonicItem { MinHeight = 48 }, new AvonicItemOptions { Items = { Option(70, "X") } } } };
        var b = new AvonicItemSliding { Items = { new AvonicItem { MinHeight = 48 }, new AvonicItemOptions { Items = { Option(70, "Y") } } } };
        using var scene = new Scene(new StackPanel { Children = { a, b } });
        GestureTests.Render();

        a.Open();
        clock.At(1000);
        b.Open();
        clock.At(2000);
        Assert.Equal(0, a.GetOpenAmount());
        Assert.Equal(70, b.GetOpenAmount(), 3);
    }

    [AvaloniaFact]
    public void S14_Close_And_CloseOpened()
    {
        using var clock = new TestClock();
        var a = new AvonicItemSliding { Items = { new AvonicItem { MinHeight = 48 }, new AvonicItemOptions { Items = { Option(70, "X") } } } };
        var b = new AvonicItemSliding { Items = { new AvonicItem { MinHeight = 48 }, new AvonicItemOptions { Items = { Option(70, "Y") } } } };
        using var scene = new Scene(new StackPanel { Children = { a, b } });
        GestureTests.Render();

        Assert.False(b.CloseOpened());
        a.Open();
        clock.At(1000);
        Assert.True(b.CloseOpened());
        Assert.Equal(0, a.GetOpenAmount());
        Assert.False(b.CloseOpened());

        a.Open();
        clock.At(2000);
        a.Close();
        Assert.Equal(0, a.GetOpenAmount());
        Assert.False(b.CloseOpened());
        clock.At(3000);
        Assert.Equal(0, a.TranslatePoint(default, scene.Window)!.Value.X);
    }

    [AvaloniaFact]
    public void S15_The_Sliding_Ratio_Is_Positive_For_The_Right_And_Negative_For_The_Left()
    {
        using var f = Make();
        Assert.Equal(0, f.Sliding.GetSlidingRatio());
        var finger = f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32);
        f.At(48);
        Assert.Equal(90 / EndWidth, f.Sliding.GetSlidingRatio(), 6);

        finger.Up(200, Y, 64);
        f.Sliding.Close();
        f.At(1000);
        f.Sliding.Open(ItemOptionsSide.Start);
        f.At(2000);
        Assert.Equal(-1, f.Sliding.GetSlidingRatio(), 6);
    }

    // ── S16–S20 ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S16_Options_Are_Hidden_Until_Active_Then_Only_The_Open_Side_Shows_As_Wide_As_The_Item()
    {
        using var f = Make();
        Assert.False(f.Start!.IsEffectivelyVisible);
        Assert.False(f.End!.IsEffectivelyVisible);

        f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(250, Y, 32);
        f.At(48);
        Assert.True(f.End.IsEffectivelyVisible);
        Assert.False(f.Start.IsEffectivelyVisible);
        Assert.Equal(f.Sliding.Bounds.Width, f.End.Bounds.Width);
        Assert.Equal(f.Item.Bounds.Height, f.End.Bounds.Height);

        // Behind the item: the item draws over them.
        Assert.True(f.Item.ZIndex > f.End.ZIndex);
    }

    [AvaloniaFact]
    public void S17_In_RTL_End_Options_Are_On_The_Left_And_A_Drag_Right_Opens_Them()
    {
        using var f = Make(start: false, flow: FlowDirection.RightToLeft);
        f.Finger().Down(100, Y, 0).Move(110, Y, 16).Move(150, Y, 32);
        f.At(48);

        Assert.Equal(-40, f.Sliding.GetOpenAmount(), 3);
        var item = new Rect(f.Item.Bounds.Size).TransformToAABB(f.Item.TransformToVisual(f.Scene.Window)!.Value);
        Assert.Equal(40, item.X, 3);
        GestureTests.Render();

        // Packed to the left; in an RTL row the first in the DOM is the furthest in, so Delete is at the edge.
        var options = f.End!.Items.OfType<AvonicItemOption>().ToList();
        Assert.Equal(80, Left(options[0], f.Scene.Window), 3);
        Assert.Equal(0, Left(options[1], f.Scene.Window), 3);
    }

    private static double Left(Visual visual, Visual root) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root)!.Value).X;

    [AvaloniaFact]
    public void S18_Disabled_It_Does_Not_Slide()
    {
        using var f = Make();
        f.Sliding.IsEnabled = false;
        f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32);
        f.At(48);
        Assert.Equal(0, f.Sliding.GetOpenAmount());

        f.Sliding.IsEnabled = true;
        f.Finger().Down(300, Y, 100).Move(290, Y, 116).Move(200, Y, 132);
        f.At(148);
        Assert.Equal(90, f.Sliding.GetOpenAmount(), 3);
    }

    [AvaloniaFact]
    public void S19_A_Drag_Turns_The_Contents_ScrollY_Off_Until_It_Ends()
    {
        AvonicContent? content = null;
        using var f = Make(wrap: sliding => content = new AvonicContent { Content = new StackPanel { Children = { sliding } } });
        Assert.True(content!.ScrollY);

        var finger = f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32);
        f.At(48);
        Assert.False(content.ScrollY);
        finger.Up(200, Y, 64);
        Assert.True(content.ScrollY);
    }

    [AvaloniaFact]
    public void S19_A_Content_That_Did_Not_Scroll_Stays_That_Way()
    {
        AvonicContent? content = null;
        using var f = Make(wrap: sliding => content = new AvonicContent { ScrollY = false, Content = new StackPanel { Children = { sliding } } });
        SlowDrag(f, 0, 300, 200);
        Assert.False(content!.ScrollY);
    }

    [AvaloniaFact]
    public void S20_Options_Added_Later_Are_Found_And_A_Swapped_Item_Is_The_One_That_Moves()
    {
        using var f = Make(start: false, end: false);
        var options = new AvonicItemOptions { Items = { Option(60, "Late") } };
        f.Sliding.Items.Add(options);
        GestureTests.Render();
        var other = new AvonicItem { MinHeight = 48, Content = "Other" };
        f.Sliding.Items.Remove(f.Item);
        f.Sliding.Items.Insert(0, other);
        GestureTests.Render();

        f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(250, Y, 32);
        f.At(48);
        Assert.Equal(40, f.Sliding.GetOpenAmount(), 3);
        Assert.Equal(-40, other.TranslatePoint(default, f.Scene.Window)!.Value.X, 3);
    }

    [AvaloniaFact]
    public void S04_A_Full_Swipe_To_The_Start_Raises_IonSwipe_On_The_Start_Options()
    {
        using var f = Make();
        SlowDrag(f, 0, 30, 300); // raw 260 left: −80 − 180 × 0.55

        Assert.Equal([ItemOptionsSide.Start], f.Swipes);
        Assert.Equal(-StartWidth, f.Sliding.GetOpenAmount(), 3);
    }

    [AvaloniaFact]
    public void S12_In_RTL_The_Edge_Is_The_Right_One()
    {
        using var f = Make(flow: FlowDirection.RightToLeft);
        f.Finger().Down(380, Y, 0).Move(370, Y, 16).Move(300, Y, 32);
        f.At(48);
        Assert.Equal(0, f.Sliding.GetOpenAmount());

        f.Finger().Down(370, Y, 100).Move(360, Y, 116).Move(330, Y, 132);
        f.At(148);
        Assert.Equal(30, f.Sliding.GetOpenAmount(), 3);
    }

    [AvaloniaFact]
    public void S21_In_A_Scroller_A_Vertical_Drag_Scrolls_And_A_Sideways_One_Slides()
    {
        AvonicContent? content = null;
        using var f = Make(wrap: sliding => content = new AvonicContent
        {
            Content = new StackPanel { Children = { sliding, new Border { Height = 2000 } } },
        });
        var scroller = content!.GetVisualDescendants().OfType<ScrollViewer>().First();
        scroller.IsScrollInertiaEnabled = false;

        var finger = f.Finger().Down(200, Y + 100, 0);
        for (var i = 1; i <= 10; i++)
        {
            finger.Move(200, Y + 100 - 8 * i, 16 * i);
        }

        finger.Up(200, Y + 20, 176);
        Assert.True(scroller.Offset.Y > 0);
        Assert.Equal(0, f.Sliding.GetOpenAmount());

        scroller.Offset = default;
        GestureTests.Render();
        finger = f.Finger().Down(300, Y, 1000);
        for (var i = 1; i <= 10; i++)
        {
            finger.Move(300 - 8 * i, Y, 1000 + 16 * i);
        }

        finger.Move(220, Y + 60, 1176).Move(220, Y + 120, 1192);
        f.At(1208);
        Assert.Equal(0, scroller.Offset.Y);
        Assert.Equal(72, f.Sliding.GetOpenAmount(), 3);
    }

    [AvaloniaFact]
    public void S10_An_Item_Added_After_Attach_And_Opened_By_Code_Takes_No_Tap()
    {
        using var clock = new TestClock();
        var options = new AvonicItemOptions { Items = { Option(70, "Favorite") } };
        var sliding = new AvonicItemSliding { Items = { options } };
        using var scene = new Scene(new StackPanel { Children = { sliding } });
        GestureTests.Render();
        var late = new AvonicItem { MinHeight = 48, Content = "Late", Button = true };
        sliding.Items.Insert(0, late);
        GestureTests.Render();
        var clicks = 0;
        late.Click += (_, _) => clicks++;

        // open() finds the item, as it was null.
        sliding.Open(ItemOptionsSide.End);
        clock.At(1000);
        new GestureTests.Finger(scene, clock).Down(100, Y, 1100).Up(100, Y, 1116);
        Assert.Equal(70, sliding.GetOpenAmount(), 3);
        Assert.Equal(0, clicks);
        Assert.True(late.ZIndex > options.ZIndex);
    }

    [AvaloniaFact]
    public void S17_Turning_RTL_On_At_Run_Time_Swaps_The_Sides()
    {
        using var f = Make(start: false);
        f.Sliding.FlowDirection = FlowDirection.RightToLeft;
        GestureTests.Render();

        f.Finger().Down(100, Y, 0).Move(110, Y, 16).Move(150, Y, 32);
        f.At(48);
        Assert.Equal(-40, f.Sliding.GetOpenAmount(), 3);
        Assert.True(f.End!.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void S18_Disabled_Mid_Drag_The_Drag_Ends_Where_It_Is()
    {
        using var f = Make();
        var t = SlowDrag(f, 0, 300, 190, false, out var finger); // 100 open, slow
        f.Sliding.IsEnabled = false;
        f.At(t + 16);
        // gesture.enable(false) ends the drag: onEnd rests it open, past half.
        Assert.Equal(EndWidth, f.Sliding.GetOpenAmount(), 3);

        finger.Move(250, Y, t + 32);
        f.At(t + 48);
        Assert.Equal(EndWidth, f.Sliding.GetOpenAmount(), 3);
        finger.Up(250, Y, t + 64);
    }

    [AvaloniaFact]
    public void S19_A_Sliding_Item_Removed_Mid_Drag_Gives_The_Content_Its_Scrolling_Back()
    {
        AvonicContent? content = null;
        StackPanel? column = null;
        using var f = Make(wrap: sliding => content = new AvonicContent { Content = column = new StackPanel { Children = { sliding } } });

        f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32);
        f.At(48);
        Assert.False(content!.ScrollY);
        column!.Children.Remove(f.Sliding);
        Dispatcher.UIThread.RunJobs();
        Assert.True(content.ScrollY);
    }

    [AvaloniaFact]
    public void S20_Options_Removed_Later_Are_Forgotten()
    {
        using var f = Make(start: false);
        f.Sliding.Items.Remove(f.End);
        GestureTests.Render();

        f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32);
        f.At(48);
        Assert.Equal(0, f.Sliding.GetOpenAmount());
        Assert.Equal(0, f.ItemX);
    }

    // ── Options and option ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O01_Options_Pack_To_Their_Side_In_Order()
    {
        using var f = Make();
        f.Sliding.Open(ItemOptionsSide.End);
        f.At(1000);
        GestureTests.Render();
        var end = f.End!.Items.OfType<AvonicItemOption>().Select(o => o.TranslatePoint(default, f.Scene.Window)!.Value.X).ToList();
        Assert.Equal([240.0, 310.0], end);

        f.Sliding.Open(ItemOptionsSide.Start);
        f.At(2000);
        GestureTests.Render();
        Assert.Equal(0, f.Start!.Items.OfType<AvonicItemOption>().Single().TranslatePoint(default, f.Scene.Window)!.Value.X);
    }

    [AvaloniaFact]
    public void O02_A_Full_Swipe_Grows_The_Expandable_Option_Over_The_Row_And_It_Snaps_Back()
    {
        using var f = Make();
        var options = f.End!.Items.OfType<AvonicItemOption>().ToList();
        var delete = options[1];
        // Make Delete first in the DOM, so the swipe has to move it to the outer end.
        f.End.Items.Remove(delete);
        f.End.Items.Insert(0, delete);
        GestureTests.Render();

        var t = SlowDrag(f, 0, 300, 30, false, out var finger);
        f.At(t + 700);
        GestureTests.Render();
        var box = new Rect(delete.Bounds.Size).TransformToAABB(delete.TransformToVisual(f.Scene.Window)!.Value);
        Assert.Equal(390, box.Right, 1);
        Assert.Equal(80 + 390, box.Width, 1);

        // Its content stays at the outer end.
        var native = delete.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Native");
        Assert.Equal(310, native.TranslatePoint(default, f.Scene.Window)!.Value.X, 1);

        finger.Up(30, Y, t + 720);
        GestureTests.Render();
        Assert.Equal(80, delete.Bounds.Width, 1);
    }

    [AvaloniaFact]
    public void O02_The_Expansion_Eases_In_Over_600_Ms()
    {
        using var f = Make();
        var delete = f.End!.Items.OfType<AvonicItemOption>().Last();

        // Captured at 290; at 60 the raw 230 is 194 open, past 180, from the frame at 48.
        var finger = f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(60, Y, 32);
        f.At(352);
        GestureTests.Render();
        var expected = 80 + 390 * AvonicItemOptions.ExpandEasing.Ease(304 / 600.0);
        Assert.InRange(delete.Bounds.Width, expected - 1, expected + 1); // layout rounds to whole pixels

        f.At(700);
        GestureTests.Render();
        Assert.Equal(80 + 390, delete.Bounds.Width, 1);
        finger.Up(60, Y, 716);
    }

    [AvaloniaFact]
    public void O03_In_A_List_With_Full_Lines_Options_Have_A_Bottom_Line() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicItemOptions { Side = ItemOptionsSide.Start, Items = { Option(80, "A") } };
        var end = new AvonicItemOptions { Items = { Option(80, "B") } };
        var list = new AvonicList { Lines = ListLines.Full, Items = { new AvonicItemSliding { Items = { start, new AvonicItem(), end } } } };
        using var scene = new Scene(list);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, start.BorderThickness.Bottom);
        Assert.Equal(1, end.BorderThickness.Bottom);

        list.Lines = ListLines.Inset;
        Assert.Equal(0, start.BorderThickness.Bottom);
        Assert.Equal(1, end.BorderThickness.Bottom);

        list.Lines = null;
        Assert.Equal(0, end.BorderThickness.Bottom);
    });

    [AvaloniaFact]
    public void O04_An_Option_Is_14_Medium_Uppercase_With_Its_Slots_In_Ionic_Places() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicItemOption { Content = "Favorite" };
        var iconOnly = new AvonicItemOption { IconOnlyContent = new AvonicIcon { IconName = "pin" } };
        var top = new AvonicItemOption { Content = "Star", TopContent = new AvonicIcon { IconName = "star" } };
        var start = new AvonicItemOption { Content = "Off", StartContent = new AvonicIcon { IconName = "trash" } };
        using var scene = new Scene(new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Height = 48, Children = { text, iconOnly, top, start } });
        GestureTests.Render();

        Assert.Equal(14, text.FontSize);
        Assert.Equal(FontWeight.Medium, text.FontWeight);
        Assert.Contains(text.GetVisualDescendants().OfType<TextBlock>(), b => b.Text == "FAVORITE");

        // Measured in Chromium: 0.7em padding each side, an icon-only icon at 1.8em with 10 px each side.
        Assert.InRange(iconOnly.Bounds.Width, 64, 66);
        var icon = (AvonicIcon)iconOnly.IconOnlyContent!;
        Assert.InRange(icon.Bounds.Width, 25, 26);

        // A top icon above the text, both centred in the 48: icon at 9, text at 23.
        var topIcon = (AvonicIcon)top.TopContent!;
        Assert.InRange(topIcon.TranslatePoint(default, top)!.Value.Y, 8, 10);

        // A start icon 5 px before the text.
        var startIcon = (AvonicIcon)start.StartContent!;
        var label = start.GetVisualDescendants().OfType<TextBlock>().Single(b => b.Text == "OFF");
        Assert.InRange(label.TranslatePoint(default, start)!.Value.X - startIcon.TranslatePoint(default, start)!.Value.X - startIcon.Bounds.Width, 4.5, 5.5);
        Assert.InRange(startIcon.TranslatePoint(default, start)!.Value.X, 9, 11);
    });

    [AvaloniaFact]
    public void O05_Primary_By_Default_And_A_Colour_Uses_Its_Base_And_Contrast() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicItemOption { Content = "A" };
        var danger = new AvonicItemOption { Content = "B", Color = "danger" };
        using var scene = new Scene(new StackPanel { Children = { plain, danger } });
        Dispatcher.UIThread.RunJobs();

        Color Resource(string key) => (Color)plain.FindResource(key)!;
        Assert.Equal(Resource("Avonic.Color.Primary"), ((ISolidColorBrush)plain.Background!).Color);
        Assert.Equal(Resource("Avonic.Color.PrimaryContrast"), ((ISolidColorBrush)plain.Foreground!).Color);
        Assert.Equal(Resource("Avonic.Color.Danger"), ((ISolidColorBrush)danger.Background!).Color);
        Assert.Equal(Resource("Avonic.Color.DangerContrast"), ((ISolidColorBrush)danger.Foreground!).Color);
    });

    [AvaloniaFact]
    public void O06_A_Tap_On_An_Option_Clicks_It_And_Runs_Its_Command()
    {
        using var f = Make();
        var option = f.End!.Items.OfType<AvonicItemOption>().First();
        var clicks = 0;
        var commands = 0;
        option.Click += (_, _) => clicks++;
        option.Command = new Command(() => commands++);

        f.Sliding.Open(ItemOptionsSide.End);
        f.At(1000);
        GestureTests.Render();
        var at = Centre(option, f.Scene.Window);
        f.Finger().Down(at.X, at.Y, 1100).Up(at.X, at.Y, 1116);

        Assert.Equal(1, clicks);
        Assert.Equal(1, commands);
    }

    [AvaloniaFact]
    public void O07_A_Disabled_Option_Takes_No_Click_And_No_Focus_And_Its_Content_Fades() => InstalledTheme.Run(_ =>
    {
        var option = new AvonicItemOption { Content = "Off", IsEnabled = false, Width = 80, Height = 48 };
        using var scene = new Scene(new StackPanel { Children = { option } });
        GestureTests.Render();
        var clicks = 0;
        option.Click += (_, _) => clicks++;

        scene.Window.MouseDown(Centre(option, scene.Window), MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(Centre(option, scene.Window), MouseButton.Left, RawInputModifiers.None);
        Assert.Equal(0, clicks);
        Assert.False(option.Focus());
        var native = option.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Native");
        Assert.Equal(0.5, native.Opacity);
        Assert.Equal(1, option.Opacity);
    });

    [AvaloniaFact]
    public void O08_An_Option_Is_A_Button_Named_By_Its_Text_That_Invokes_A_Click()
    {
        var option = new AvonicItemOption { Content = "Favorite", StartContent = "A" };
        using var scene = new Scene(new StackPanel { Children = { option } });
        Dispatcher.UIThread.RunJobs();
        var clicks = 0;
        option.Click += (_, _) => clicks++;

        var peer = ControlAutomationPeer.CreatePeerForElement(option);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("A Favorite", peer.GetName());
        ((IInvokeProvider)peer).Invoke();
        Assert.Equal(1, clicks);
        Assert.True(option.Focusable);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void O09_The_Outermost_Option_Is_Padded_By_The_Safe_Area_On_Its_Side(FlowDirection flow) => InstalledTheme.Run(_ =>
    {
        static AvonicItemOption Fixed(string tag) => new() { Content = new Border { Width = 40, Height = 10, Tag = tag } };
        var start = new AvonicItemOptions { Side = ItemOptionsSide.Start, Items = { Fixed("a") } };
        var end = new AvonicItemOptions { Items = { Fixed("b"), Fixed("c") } };
        var sliding = new AvonicItemSliding { FlowDirection = flow, Items = { start, new AvonicItem { MinHeight = 48 }, end } };
        var root = new StackPanel { Children = { sliding } };
        root.Resources["Avonic.SafeAreaLeft"] = 20.0;
        root.Resources["Avonic.SafeAreaRight"] = 30.0;
        using var clock = new TestClock();
        using var scene = new Scene(root);
        GestureTests.Render();

        sliding.Open(ItemOptionsSide.End);
        clock.At(1000);
        GestureTests.Render();
        var options = end.Items.OfType<AvonicItemOption>().ToList();
        // 40 + 0.7em each side; the outer one also has the safe area of the physical side it's on.
        var rtl = flow == FlowDirection.RightToLeft;
        Assert.Equal(59.6, options[0].Bounds.Width, 0);
        Assert.Equal(59.6 + (rtl ? 20 : 30), options[1].Bounds.Width, 0);
        // The content stays clear of it: the outer edge of the native button is the safe area in from the row's edge.
        var native = options[1].GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Native");
        var box = new Rect(native.Bounds.Size).TransformToAABB(native.TransformToVisual(scene.Window)!.Value);
        if (rtl)
        {
            Assert.Equal(20, box.X, 0);
        }
        else
        {
            Assert.Equal(390 - 30, box.Right, 0);
        }

        Assert.Equal(59.6 + (rtl ? 30 : 20), start.Items.OfType<AvonicItemOption>().Single().DesiredSize.Width, 0);
    });

    [AvaloniaFact]
    public void A_Plain_Style_From_The_App_Wins_Over_The_Options_Being_Shown()
    {
        // No activator: a plain type rule, which would lose to a value bound after it at the same priority. In place before the
        // window shows, as an app's styles are.
        var style = new Style(x => x.OfType<AvonicItemOptions>()) { Setters = { new Setter(Visual.IsVisibleProperty, false) } };
        Application.Current!.Styles.Add(style);
        try
        {
            using var f = Make();
            f.Finger().Down(300, Y, 0).Move(290, Y, 16).Move(200, Y, 32);
            f.At(48);
            Assert.False(f.End!.IsVisible);
        }
        finally
        {
            Application.Current!.Styles.Remove(style);
        }
    }

    private sealed class Command(Action run) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run();
    }
}
