using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Behaviors;

/// <summary>
/// Behaviours A1–A7 from docs/components/ion-ripple-effect/PLAN.md, read from Ionic's
/// utils/tap-click. Pointer input is real (headless); time is the test clock, so the
/// 100 ms / 150 ms defers are asserted at the exact millisecond.
/// </summary>
public class ActivatableTests
{
    private const double W = 240, H = 96;
    private static readonly Point Centre = new(W / 2, H / 2);

    /// <summary>A 240×96 activatable surface at the window's top-left, with a ripple inside.</summary>
    private sealed class Fixture : IDisposable
    {
        public readonly Window Window;
        public readonly Border Surface;
        public readonly Panel  Inner;
        public readonly AvonicRippleEffect Ripple;
        private readonly Scene _scene;

        public Fixture(bool instant = false, Control? content = null)
        {
            Ripple = new AvonicRippleEffect();
            Inner  = new Panel { Background = Brushes.Transparent, Children = { Ripple } };
            if (content is not null) Inner.Children.Add(content);
            Surface = new Border
            {
                Width = W, Height = H,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.LightGray,
                Child = Inner,
            };
            Activatable.SetIsEnabled(Surface, true);
            Activatable.SetIsInstant(Surface, instant);
            _scene = new Scene(Surface);
            Window = _scene.Window;
        }

        public void Press(Point p, MouseButton button = MouseButton.Left) => Window.MouseDown(p, button, RawInputModifiers.None);
        public void Release(Point p) => Window.MouseUp(p, MouseButton.Left, RawInputModifiers.None);
        public void Move(Point p) => Window.MouseMove(p, RawInputModifiers.None);

        public bool IsActivated => Surface.Classes.Contains(":activated");

        public void Dispose() => _scene.Dispose();
    }

    // ── A1: 100 ms defer, instant skips it, only the secondary button is ignored ─────────────

    [AvaloniaFact]
    public void A1_Press_Activates_After_100ms_Defer()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre);
        Assert.False(f.IsActivated);
        Assert.Empty(f.Ripple.Ripples);

        clock.At(99);
        Assert.False(f.IsActivated);

        clock.At(100);
        Assert.True(f.IsActivated);
        var r = Assert.Single(f.Ripple.Ripples);
        Assert.Equal(TimeSpan.FromMilliseconds(100), r.StartedAt);
    }

    [AvaloniaFact]
    public void A1_Instant_Surface_Activates_On_Press()
    {
        using var clock = new TestClock();
        using var f = new Fixture(instant: true);

        f.Press(Centre);

        Assert.True(f.IsActivated);
        Assert.Single(f.Ripple.Ripples);
    }

    [AvaloniaFact]
    public void A1_Right_Button_Is_Ignored()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre, MouseButton.Right);
        clock.At(500);

        Assert.False(f.IsActivated);
        Assert.Empty(f.Ripple.Ripples);
    }

    [AvaloniaFact]
    public void A1_Middle_Button_Activates_Like_Ionic()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre, MouseButton.Middle);             // Ionic ignores only `ev.button === 2`
        clock.At(100);

        Assert.True(f.IsActivated);
    }

    [AvaloniaFact]
    public void A1_Primary_Press_While_Secondary_Is_Held_Activates()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre, MouseButton.Right);              // ignored, and still held…
        f.Press(Centre, MouseButton.Left);               // …this press is button 0
        clock.At(100);

        Assert.True(f.IsActivated);
    }

    // ── A2: ripple starts at the original press position ─────────────────────────────────────

    [AvaloniaFact]
    public void A2_Ripple_Starts_At_The_Press_Position()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(new Point(30, 20));
        clock.At(100);

        var r = Assert.Single(f.Ripple.Ripples);
        Assert.Equal(30 - r.InitialSize / 2, Canvas.GetLeft(r));
        Assert.Equal(20 - r.InitialSize / 2, Canvas.GetTop(r));
    }

    // ── A3: quick tap still ripples ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A3_Release_Before_Defer_Activates_Then_Releases()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre);
        clock.At(30);
        f.Release(Centre);

        var r = Assert.Single(f.Ripple.Ripples);
        Assert.Equal(TimeSpan.FromMilliseconds(30), r.StartedAt);
        Assert.Equal(TimeSpan.FromMilliseconds(30), r.ReleaseRequestedAt);
        Assert.True(f.IsActivated);                      // cleared 150 ms later (A4)

        clock.At(500);                                   // the defer must not fire again
        Assert.Single(f.Ripple.Ripples);
    }

    [AvaloniaFact]
    public void A3_Quick_Tap_Ripple_Starts_At_The_Release_Point()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(new Point(30, 20));
        clock.At(30);
        f.Release(new Point(80, 60));                    // setActivatedElement(undefined, pointerUp)

        var r = Assert.Single(f.Ripple.Ripples);
        Assert.Equal(80 - r.InitialSize / 2, Canvas.GetLeft(r));
        Assert.Equal(60 - r.InitialSize / 2, Canvas.GetTop(r));
    }

    // ── A4: clearing :activated ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A4_Release_Within_150ms_Of_Activation_Clears_150ms_After_Release()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre);
        clock.At(140);                                   // activated at 100
        f.Release(Centre);

        var r = Assert.Single(f.Ripple.Ripples);
        Assert.Equal(TimeSpan.FromMilliseconds(140), r.ReleaseRequestedAt);
        Assert.True(f.IsActivated);

        clock.At(289);
        Assert.True(f.IsActivated);
        clock.At(290);
        Assert.False(f.IsActivated);
    }

    [AvaloniaFact]
    public void A4_Release_Later_Than_150ms_After_Activation_Clears_Immediately()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre);
        clock.At(400);
        f.Release(Centre);

        Assert.False(f.IsActivated);
    }

    [AvaloniaFact]
    public void A4_Instant_Surface_Clears_Activated_Immediately_On_Release()
    {
        using var clock = new TestClock();
        using var f = new Fixture(instant: true);

        f.Press(Centre);
        f.Release(Centre);

        Assert.False(f.IsActivated);
    }

    [AvaloniaFact]
    public void A4_RePress_During_The_Clear_Window_Restarts_Cleanly()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre);  clock.At(140);  f.Release(Centre);     // clear scheduled for 290
        clock.At(200);
        f.Press(Centre);                                          // setActivatedElement drops the old state
        Assert.False(f.IsActivated);

        clock.At(290);                                            // the stale clear must not fire…
        Assert.False(f.IsActivated);
        clock.At(300);                                            // …and the new defer activates at 300
        Assert.True(f.IsActivated);

        f.Move(Centre + new Vector(0, 40));                       // no gesture here: still pressed
        f.Release(Centre);
        clock.At(1000);
        Assert.False(f.IsActivated);
    }

    // ── A5: cancel (pointer capture lost) ────────────────────────────────────────────────────

    /// <summary>
    /// The mechanism A5 relies on: while pressed, the pressed element holds pointer capture, so a
    /// gesture recogniser taking the pointer (or anything releasing it) raises PointerCaptureLost there.
    /// </summary>
    [AvaloniaFact]
    public void A5_Pressed_Element_Holds_Capture_During_The_Press()
    {
        using var clock = new TestClock();
        using var f = new Fixture();
        IInputElement? captured = null;
        f.Inner.PointerMoved += (_, e) => captured = e.Pointer.Captured;

        f.Press(Centre);
        f.Move(Centre + new Vector(0, 5));

        Assert.Same(f.Inner, captured);
    }

    [AvaloniaFact]
    public void A5_Cancel_Before_Defer_Means_No_Ripple_At_All()
    {
        using var clock = new TestClock();
        using var f = new Fixture();
        f.Inner.PointerMoved += (_, e) => e.Pointer.Capture(null);     // a gesture takes the pointer

        f.Press(Centre);
        clock.At(50);
        f.Move(Centre + new Vector(0, 30));
        clock.At(500);

        Assert.False(f.IsActivated);
        Assert.Empty(f.Ripple.Ripples);
    }

    [AvaloniaFact]
    public void A5_Cancel_After_Activation_Clears_Immediately_And_Releases_Ripple()
    {
        using var clock = new TestClock();
        using var f = new Fixture();
        f.Inner.PointerMoved += (_, e) => e.Pointer.Capture(null);

        f.Press(Centre);
        clock.At(140);
        Assert.True(f.IsActivated);

        f.Move(Centre + new Vector(0, 30));

        Assert.False(f.IsActivated);
        var r = Assert.Single(f.Ripple.Ripples);
        Assert.Equal(TimeSpan.FromMilliseconds(140), r.ReleaseRequestedAt);
    }

    [AvaloniaFact]
    public void A5_Cancelled_Press_Allows_The_Next_Press()
    {
        using var clock = new TestClock();
        using var f = new Fixture();
        var cancelOnce = true;
        f.Inner.PointerMoved += (_, e) => { if (cancelOnce) { cancelOnce = false; e.Pointer.Capture(null); } };

        f.Press(Centre);  f.Move(Centre + new Vector(0, 30));           // cancelled
        f.Release(Centre);
        clock.At(1000);
        f.Press(Centre);  clock.At(1100);

        Assert.True(f.IsActivated);
    }

    [AvaloniaFact]
    public void A5_Capture_Already_Held_Elsewhere_Is_Watched_There()
    {
        using var clock = new TestClock();
        using var f = new Fixture();
        // Something earlier in the tunnel takes capture before Activatable sees the press.
        f.Window.AddHandler(InputElement.PointerPressedEvent, (_, e) => e.Pointer.Capture(f.Window),
            Avalonia.Interactivity.RoutingStrategies.Tunnel);

        f.Press(Centre);
        clock.At(100);
        Assert.True(f.IsActivated);

        f.Release(new Point(300, 300));                  // released far outside the surface
        clock.At(1000);

        Assert.False(f.IsActivated);
        Assert.NotNull(Assert.Single(f.Ripple.Ripples).ReleaseRequestedAt);
    }

    [AvaloniaFact]
    public void A5_Child_Recapturing_On_Press_Is_Still_The_Same_Press()
    {
        using var clock = new TestClock();
        var child = new Border { Width = W, Height = H, Background = Brushes.Transparent };
        child.PointerPressed += (_, e) => e.Pointer.Capture(child);   // a Thumb-like child (bubble phase)
        using var f = new Fixture(content: child);

        f.Press(Centre);
        clock.At(100);
        Assert.True(f.IsActivated);
        Assert.Single(f.Ripple.Ripples);

        f.Release(Centre);
        clock.At(1000);
        Assert.False(f.IsActivated);
    }

    [AvaloniaFact]
    public void A5_Capture_Taken_By_An_Element_Outside_The_Surface_Cancels()
    {
        using var clock = new TestClock();
        var child = new Border { Width = W, Height = H, Background = Brushes.Transparent };
        using var f = new Fixture(content: child);
        child.PointerPressed += (_, e) => e.Pointer.Capture(f.Window);   // an outer gesture claims the pointer

        f.Press(Centre);
        clock.At(500);

        Assert.False(f.IsActivated);
        Assert.Empty(f.Ripple.Ripples);
    }

    // ── A6: one active surface at a time; innermost wins ─────────────────────────────────────

    [AvaloniaFact]
    public void A6_Second_Press_While_Active_Is_Ignored()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre);
        clock.At(140);
        f.Press(new Point(10, 10));
        clock.At(300);

        Assert.Single(f.Ripple.Ripples);
    }

    [AvaloniaFact]
    public void A6_Only_One_Surface_Is_Active_Application_Wide()
    {
        using var clock = new TestClock();
        var a = new Border { Width = 100, Height = 50, Background = Brushes.Gray };
        var b = new Border { Width = 100, Height = 50, Background = Brushes.Gray };
        Activatable.SetIsEnabled(a, true);
        Activatable.SetIsEnabled(b, true);
        using var scene = new Scene(new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { a, b } });

        scene.Window.MouseDown(new Point(50, 25), MouseButton.Left, RawInputModifiers.None);
        clock.At(100);
        scene.Window.MouseDown(new Point(50, 75), MouseButton.Left, RawInputModifiers.None);   // activatableEle is set → ignored
        clock.At(300);

        Assert.Contains(":activated", a.Classes);
        Assert.DoesNotContain(":activated", b.Classes);
    }

    [AvaloniaFact]
    public void A6_Innermost_Activatable_Wins()
    {
        using var clock = new TestClock();
        var innerRipple  = new AvonicRippleEffect();
        var innerSurface = new Border { Width = 80, Height = 40, Background = Brushes.DarkGray, Child = new Panel { Children = { innerRipple } },
                                        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        Activatable.SetIsEnabled(innerSurface, true);
        using var f = new Fixture(content: innerSurface);           // an activatable button inside an activatable item

        f.Press(new Point(40, 20));                                  // on the inner surface
        clock.At(100);

        Assert.Contains(":activated", innerSurface.Classes);
        Assert.Single(innerRipple.Ripples);
        Assert.False(f.IsActivated);
        Assert.Empty(f.Ripple.Ripples);

        f.Release(new Point(40, 20));
        clock.At(1000);
        f.Press(new Point(200, 80));                                 // outside the inner surface → outer activates
        clock.At(1100);
        Assert.True(f.IsActivated);
    }

    [AvaloniaFact]
    public void A6_Surface_Without_A_Ripple_Does_Not_Borrow_A_Nested_Surfaces_Ripple()
    {
        using var clock = new TestClock();
        var innerRipple  = new AvonicRippleEffect();
        var innerSurface = new Border { Width = 80, Height = 40, Background = Brushes.DarkGray, Child = new Panel { Children = { innerRipple } },
                                        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        Activatable.SetIsEnabled(innerSurface, true);
        var outer = new Border { Width = W, Height = H, Background = Brushes.LightGray, Child = innerSurface,
                                 HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        Activatable.SetIsEnabled(outer, true);                       // an item with no ripple of its own
        using var scene = new Scene(outer);

        scene.Window.MouseDown(new Point(200, 80), MouseButton.Left, RawInputModifiers.None);   // on the outer's padding
        clock.At(100);

        Assert.Contains(":activated", outer.Classes);
        Assert.Empty(innerRipple.Ripples);                           // querySelector does not pierce the nested component
    }

    // ── A7: global switches ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A7_RippleEffect_Config_Off_Activates_Without_Ripple()
    {
        using var clock = new TestClock();
        AvonicConfig.RippleEffect = false;
        try
        {
            using var f = new Fixture();
            f.Press(Centre);
            clock.At(140);

            Assert.True(f.IsActivated);
            Assert.Empty(f.Ripple.Ripples);
        }
        finally
        {
            AvonicConfig.RippleEffect = true;
        }
    }

    [AvaloniaFact]
    public void A7_Animated_Config_Off_Activates_Without_Ripple()
    {
        using var clock = new TestClock();
        AvonicConfig.Animated = false;
        try
        {
            using var f = new Fixture();
            f.Press(Centre);
            clock.At(140);

            Assert.True(f.IsActivated);
            Assert.Empty(f.Ripple.Ripples);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Disabling_The_Behaviour_Stops_Responding()
    {
        using var clock = new TestClock();
        using var f = new Fixture();
        Activatable.SetIsEnabled(f.Surface, false);

        f.Press(Centre);
        clock.At(140);

        Assert.False(f.IsActivated);
        Assert.Empty(f.Ripple.Ripples);
    }

    [AvaloniaFact]
    public void Disabling_While_Activated_Clears_The_State()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Press(Centre);
        clock.At(140);
        Activatable.SetIsEnabled(f.Surface, false);

        Assert.False(f.IsActivated);
        Assert.NotNull(Assert.Single(f.Ripple.Ripples).ReleaseRequestedAt);
    }

    [AvaloniaFact]
    public void Leaving_The_Tree_While_Pressed_Cancels_And_Frees_The_Global_Slot()
    {
        using var clock = new TestClock();
        var f = new Fixture();
        f.Press(Centre);
        clock.At(140);
        f.Dispose();                                     // window closed mid-press

        using var g = new Fixture();
        g.Press(Centre);
        clock.At(300);
        Assert.True(g.IsActivated);
    }
}
