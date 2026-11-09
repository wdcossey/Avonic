using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;

namespace Avonic.Components.Tests.Overlays;

/// <summary>
/// Ionic's <c>utils/overlays</c>, with a test overlay in an <c>AvonicContent</c> page in a window, on the test clock, with real
/// headless keys and pointers. The measured cases (L1 and the rest) are in docs/infra/overlays/PLAN.md. Frames come every 16 ms,
/// so the test overlay's 150 ms animations, played at 0, finish at the frame at 176.
/// </summary>
public class AvonicOverlayTests
{
    private const double Finished = 176;

    /// <summary>A page with things to focus, in a window with <c>SimpleTheme</c>, whose template has the overlay layer.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Avalonia.Themes.Simple.SimpleTheme _theme = new();

        public Stage()
        {
            Application.Current!.Styles.Insert(0, _theme);
            Clock = new TestClock();
            Opener = new Button { Content = "Opener" };
            Other = new Button { Content = "Other" };
            Text = new TextBox();
            Scene = new Scene(new AvonicContent { Content = new StackPanel { Children = { Opener, Other, Text } } });
        }

        public TestClock Clock { get; }

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public Button Opener { get; }

        public Button Other { get; }

        public TextBox Text { get; }

        public List<string> Log { get; } = [];

        public IInputElement? Focused => Window.FocusManager!.GetFocusedElement();

        /// <summary>A millisecond at a time, so that what a frame completes runs at that frame's time, as it would.</summary>
        public void At(double ms)
        {
            while (Clock.Now.TotalMilliseconds < ms)
            {
                Clock.At(Math.Min(Math.Floor(Clock.Now.TotalMilliseconds) + 1, ms));
                Settle();
            }

            Settle();
        }

        public static void Settle() => Dispatcher.UIThread.RunJobs();

        /// <summary>Hit tests read the last rendered scene; an overlay layer that's just been made can take a second tick to reach it.</summary>
        public void Render()
        {
            for (var i = 0; i < 2; i++)
            {
                Settle();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            Settle();
        }

        /// <summary>Logs every event of the overlay, with the clock's time.</summary>
        public T Watch<T>(T overlay, string name = "")
            where T : AvonicOverlay
        {
            overlay.IonWillPresent += (_, _) => Log.Add($"{name}willPresent@{Clock.Now.TotalMilliseconds}");
            overlay.IonDidPresent += (_, _) => Log.Add($"{name}didPresent@{Clock.Now.TotalMilliseconds}");
            overlay.IonWillDismiss += (_, e) => Log.Add($"{name}willDismiss({e.Data},{e.Role})@{Clock.Now.TotalMilliseconds}");
            overlay.IonDidDismiss += (_, e) => Log.Add($"{name}didDismiss({e.Data},{e.Role})@{Clock.Now.TotalMilliseconds}");
            return overlay;
        }

        public void Key(PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
        {
            Window.KeyPressQwerty(key, modifiers);
            Window.KeyReleaseQwerty(key, modifiers);
            Settle();
        }

        public void Click(Point point)
        {
            Render();
            Window.MouseDown(point, MouseButton.Left);
            Window.MouseUp(point, MouseButton.Left);
            Settle();
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
            Application.Current!.Styles.Remove(_theme);
        }
    }

    /// <summary>The backdrop under a point, if what the hit test finds there is in one.</summary>
    private static AvonicBackdrop? BackdropAt(Stage stage, Point point)
    {
        stage.Render();
        return ((Visual?)stage.Window.InputHitTest(point))?.FindAncestorOfType<AvonicBackdrop>(includeSelf: true);
    }

    private static OverlayLayer Layer(Visual overlay) => (OverlayLayer)overlay.GetVisualParent()!.GetVisualParent()!;

    // ── O1: present ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O1_Present_Shows_It_In_The_Overlay_Layer_With_Its_First_Values_And_Raises_The_Events_Around_The_Animation()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay(TestOverlay.Button("Inside")));
        var visibleAtWillPresent = true;
        overlay.IonWillPresent += (_, _) => visibleAtWillPresent = overlay.IsVisible;

        var presented = overlay.Present(stage.Window);

        // L1: willPresent at once, hidden until the animation starts; then shown with its first values, before any frame.
        Assert.Equal(["willPresent@0"], stage.Log);
        Assert.False(visibleAtWillPresent);
        Assert.True(overlay.IsVisible);
        Assert.Same(OverlayLayer.GetOverlayLayer(stage.Window), Layer(overlay));
        Assert.Equal(0.01, overlay.Backdrop.Opacity, 6);
        Assert.Equal(0.01, overlay.Box.Opacity, 6);
        Assert.False(overlay.Backdrop.IsHitTestVisible);

        Stage.Settle();
        Assert.Equal(new Rect(0, 0, 390, 844), overlay.Bounds);

        stage.At(160);
        Assert.False(presented.IsCompleted);
        stage.At(Finished);
        Assert.Equal(["willPresent@0", "didPresent@176"], stage.Log);
        Assert.True(presented.IsCompleted);
        // L2: the end values are held while it's presented.
        Assert.Equal(0.32, overlay.Backdrop.Opacity, 6);
        Assert.Equal(1, overlay.Box.Opacity, 6);
        Assert.True(overlay.Backdrop.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void O1_The_Overlay_Stays_The_Size_Of_The_Window()
    {
        using var stage = new Stage();
        var overlay = new TestOverlay();
        overlay.Present(stage.Window);
        stage.At(Finished);

        stage.Window.Width = 300;
        stage.Window.Height = 500;
        Stage.Settle();
        Assert.Equal(new Rect(0, 0, 300, 500), overlay.Bounds);
    }

    [AvaloniaFact]
    public void O1_EnterAnimation_And_LeaveAnimation_Replace_Mds_And_Every_Builder_Gets_The_Options()
    {
        using var stage = new Stage();
        var options = new object();
        var seen = new List<object?>();
        var overlay = new TestOverlay { Options = options };
        overlay.EnterAnimation = (element, opts) =>
        {
            seen.Add(opts);
            return AvonicAnimation.Create().AddElement(((TestOverlay)element).Box).FromTo("opacity", 0.5, 0.6).Duration(50);
        };

        overlay.Present(stage.Window);
        Assert.Equal(0.5, overlay.Box.Opacity, 6);
        Assert.Equal(1, overlay.Backdrop.Opacity, 6);
        stage.At(100);
        overlay.Dismiss();
        Assert.Equal(0.99, overlay.Box.Opacity, 6);

        overlay.LeaveAnimation = (_, opts) =>
        {
            seen.Add(opts);
            return AvonicAnimation.Create();
        };
        stage.At(400);
        overlay.Present(stage.Window);
        stage.At(800);
        overlay.Dismiss();
        // An animation with no elements finishes at once.
        Stage.Settle();
        Assert.Null(overlay.GetVisualParent());

        Assert.Equal([options, options, options], seen);
        Assert.Equal([options], overlay.OptionsSeen);
    }

    // ── O2: animated ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O2_Animated_False_Shows_The_End_Values_At_Once_And_Raises_DidPresent_At_The_Next_Frame()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay { Animated = false });

        overlay.Present(stage.Window);
        Assert.Equal(0.32, overlay.Backdrop.Opacity, 6);
        stage.At(16);
        Assert.Equal(["willPresent@0", "didPresent@16"], stage.Log);

        overlay.Dismiss();
        Assert.Equal(0, overlay.Backdrop.Opacity, 6);
        stage.At(32);
        Assert.Equal(["willPresent@0", "didPresent@16", "willDismiss(,)@16", "didDismiss(,)@32"], stage.Log);
    }

    [AvaloniaFact]
    public void O2_The_Global_Animated_Off_Does_The_Same()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay());
        AvonicConfig.Animated = false;
        try
        {
            overlay.Present(stage.Window);
            stage.At(16);
            Assert.Equal(["willPresent@0", "didPresent@16"], stage.Log);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    }

    // ── O3, O6: the lock ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O3_Presenting_Twice_At_Once_Or_While_Presented_Presents_Once()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay());

        var first = overlay.Present(stage.Window);
        var second = overlay.Present(stage.Window);
        stage.At(Finished);
        Assert.True(first.IsCompleted && second.IsCompleted);

        var third = overlay.Present(stage.Window);
        stage.At(400);
        Assert.True(third.IsCompleted);
        Assert.Equal(["willPresent@0", "didPresent@176"], stage.Log);
    }

    [AvaloniaFact]
    public void O6_Dismissing_During_The_Enter_Animation_Waits_For_DidPresent()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay());

        overlay.Present(stage.Window);
        var dismissed = overlay.Dismiss("d", "r");
        stage.At(Finished);
        Assert.Equal(["willPresent@0", "didPresent@176", "willDismiss(d,r)@176"], stage.Log);

        stage.At(400);
        Assert.Equal("didDismiss(d,r)@352", stage.Log[^1]);
        Assert.True(dismissed.Result);
    }

    // ── O4, O5, O7: dismiss ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O4_Dismiss_Raises_The_Events_Around_The_Leave_Animation_Then_Removes_It_And_Its_Held_Values()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay());
        overlay.Present(stage.Window);
        stage.At(200);
        var host = overlay.GetVisualParent();

        var dismissed = overlay.Dismiss(42, "ok");
        Assert.Equal("willDismiss(42,ok)@200", stage.Log[^1]);
        Assert.Equal(0.32, overlay.Backdrop.Opacity, 6);
        stage.At(296);
        Assert.True(overlay.Backdrop.Opacity < 0.32);
        Assert.Same(host, overlay.GetVisualParent());

        stage.At(376);
        Assert.Equal("didDismiss(42,ok)@376", stage.Log[^1]);
        Assert.True(dismissed.Result);
        Assert.Null(overlay.GetVisualParent());
        Assert.False(overlay.IsVisible);
        // Every animation is destroyed: the stub's backdrop has no theme, so its own opacity is 1.
        Assert.Equal(1, overlay.Backdrop.Opacity);
        Assert.Equal(1, overlay.Box.Opacity);
        Assert.True(overlay.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void O4_Dismiss_Is_False_When_It_Is_Not_Presented()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay());
        var never = overlay.Dismiss();
        Stage.Settle();
        Assert.False(never.Result);

        overlay.Present(stage.Window);
        stage.At(200);
        var first = overlay.Dismiss();
        var second = overlay.Dismiss();
        stage.At(400);
        Assert.True(first.Result);
        Assert.False(second.Result);
        Assert.Single(stage.Log, entry => entry.StartsWith("didDismiss", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void O4_The_Gesture_Role_Skips_The_Leave_Animation()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay());
        overlay.Present(stage.Window);
        stage.At(200);

        overlay.Dismiss(null, OverlayRoles.Gesture);
        Stage.Settle();
        Assert.Equal("didDismiss(,gesture)@200", stage.Log[^1]);
        Assert.Null(overlay.GetVisualParent());
    }

    [AvaloniaFact]
    public void O4_A_Dismissed_Overlay_Can_Be_Presented_Again_Above_What_Came_Since()
    {
        using var stage = new Stage();
        var overlay = new TestOverlay();
        var other = new TestOverlay();
        overlay.Present(stage.Window);
        stage.At(200);
        overlay.Dismiss();
        stage.At(400);
        other.Present(stage.Window);
        stage.At(600);

        overlay.Present(stage.Window);
        stage.At(800);
        Assert.True(overlay.ZIndex > other.ZIndex);
        Assert.Same(other.GetVisualParent(), overlay.GetVisualParent());
    }

    [AvaloniaFact]
    public void O5_OnWillDismiss_And_OnDidDismiss_Complete_With_The_Next_Detail_After_The_Overlay_Has_Gone()
    {
        using var stage = new Stage();
        var overlay = new TestOverlay();
        overlay.Present(stage.Window);
        stage.At(200);
        var will = overlay.OnWillDismiss();
        var did = overlay.OnDidDismiss();
        Visual? parentWhenDid = overlay;
        did.ContinueWith(_ => parentWhenDid = overlay.GetVisualParent(), TaskScheduler.FromCurrentSynchronizationContext());

        overlay.Dismiss("data", "role");
        Stage.Settle();
        Assert.Equal(new OverlayEventDetail("data", "role"), will.Result);
        Assert.False(did.IsCompleted);

        stage.At(400);
        Assert.Equal(new OverlayEventDetail("data", "role"), did.Result);
        Assert.Null(parentWhenDid);

        // The next one, not the last.
        var next = overlay.OnDidDismiss();
        Stage.Settle();
        Assert.False(next.IsCompleted);
    }

    [AvaloniaFact]
    public void O7_It_Cannot_Be_Hit_During_Its_Dismiss()
    {
        using var stage = new Stage();
        var overlay = new TestOverlay(TestOverlay.Button("Inside"));
        overlay.Present(stage.Window);
        stage.At(200);
        var point = new Point(195, 20);
        Assert.Same(overlay.Backdrop, BackdropAt(stage, point));

        overlay.Dismiss();
        Assert.Null(BackdropAt(stage, point));
        Assert.NotNull(stage.Window.InputHitTest(point));
    }

    // ── O8: stacking ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O8_Each_Overlay_Is_Above_The_Last_By_Its_Index()
    {
        using var stage = new Stage();
        var first = new TestOverlay();
        var second = new TestOverlay();
        var toast = new TestOverlay { Base = 60000 };
        first.Present(stage.Window);
        second.Present(stage.Window);
        toast.Present(stage.Window);
        stage.At(200);

        Assert.True(first.OverlayIndex > 0);
        Assert.Equal(20000 + first.OverlayIndex, first.ZIndex);
        Assert.Equal(first.ZIndex + 1, second.ZIndex);
        Assert.Equal(60000 + second.OverlayIndex + 1, toast.ZIndex);

        Assert.Same(toast.Backdrop, BackdropAt(stage, new Point(20, 20)));
        toast.Dismiss();
        stage.At(400);
        Assert.Same(second.Backdrop, BackdropAt(stage, new Point(20, 20)));
    }

    // ── O9: Escape and the back button ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O9_Escape_Dismisses_The_Top_Overlay_With_The_Backdrop_Role()
    {
        using var stage = new Stage();
        var under = stage.Watch(new TestOverlay(), "under ");
        var top = stage.Watch(new TestOverlay(), "top ");
        under.Present(stage.Window);
        top.Present(stage.Window);
        stage.At(200);

        stage.Key(PhysicalKey.Escape);
        stage.At(400);
        Assert.Contains("top didDismiss(,backdrop)@376", stage.Log);
        Assert.DoesNotContain(stage.Log, entry => entry.StartsWith("under willDismiss", StringComparison.Ordinal));

        stage.Key(PhysicalKey.Escape);
        stage.At(600);
        Assert.Contains("under didDismiss(,backdrop)@576", stage.Log);
    }

    [AvaloniaFact]
    public void O9_Escape_Does_Nothing_When_The_Top_Overlay_Is_Not_Dismissable_Even_With_One_Under_It_That_Is()
    {
        using var stage = new Stage();
        var under = stage.Watch(new TestOverlay(), "under ");
        var top = stage.Watch(new TestOverlay { BackdropDismiss = false }, "top ");
        under.Present(stage.Window);
        top.Present(stage.Window);
        stage.At(200);

        stage.Key(PhysicalKey.Escape);
        stage.At(400);
        Assert.DoesNotContain(stage.Log, entry => entry.Contains("Dismiss", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void O9_Escape_During_A_Leave_Animation_Does_Not_Reach_The_One_Under_It()
    {
        using var stage = new Stage();
        var under = stage.Watch(new TestOverlay(), "under ");
        var top = stage.Watch(new TestOverlay(), "top ");
        under.Present(stage.Window);
        top.Present(stage.Window);
        stage.At(200);

        top.Dismiss();
        stage.At(250);
        stage.Key(PhysicalKey.Escape);
        stage.At(600);
        Assert.DoesNotContain(stage.Log, entry => entry.StartsWith("under willDismiss", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void O9_The_Back_Button_Dismisses_The_Top_Overlay_And_Is_Handled_Only_Then()
    {
        using var stage = new Stage();
        var handled = new List<bool>();
        var top = stage.Watch(new TestOverlay { BackdropDismiss = false });
        top.Present(stage.Window);
        stage.At(200);
        // After the overlay's own handler, which the first present adds.
        stage.Window.AddHandler(TopLevel.BackRequestedEvent, (_, e) => handled.Add(e.Handled), handledEventsToo: true);

        // Alt+Left is Avalonia's back on desktop, as Android's back button is on a phone.
        stage.Key(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        stage.At(400);
        Assert.Equal([false], handled);
        Assert.DoesNotContain(stage.Log, entry => entry.Contains("Dismiss", StringComparison.Ordinal));

        top.BackdropDismiss = true;
        stage.Key(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        stage.At(600);
        Assert.Equal([false, true], handled);
        Assert.Contains("didDismiss(,backdrop)@576", stage.Log);
    }

    [AvaloniaFact]
    public void O9_A_Back_Request_The_App_Has_Already_Handled_Is_Left_Alone()
    {
        using var stage = new Stage();
        stage.Opener.AddHandler(TopLevel.BackRequestedEvent, (_, e) => e.Handled = true);
        var top = stage.Watch(new TestOverlay());
        stage.Opener.Focus();
        top.Present(stage.Window);
        stage.At(200);

        stage.Opener.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        stage.At(400);
        Assert.DoesNotContain(stage.Log, entry => entry.Contains("Dismiss", StringComparison.Ordinal));
    }

    // ── O10: the backdrop ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O10_A_Tap_On_The_Backdrop_Dismisses_With_The_Backdrop_Role_When_It_Is_Tappable()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay { BackdropDismiss = false });
        overlay.Present(stage.Window);
        stage.At(200);

        stage.Click(new Point(20, 20));
        stage.At(400);
        Assert.DoesNotContain(stage.Log, entry => entry.Contains("Dismiss", StringComparison.Ordinal));

        overlay.BackdropDismiss = true;
        stage.Click(new Point(20, 20));
        stage.At(600);
        Assert.Contains("didDismiss(,backdrop)@576", stage.Log);
    }

    [AvaloniaFact]
    public void O10_The_Backdrop_Takes_No_Tap_During_The_Enter_Animation()
    {
        using var stage = new Stage();
        var overlay = stage.Watch(new TestOverlay());
        overlay.Present(stage.Window);
        stage.At(64);

        stage.Click(new Point(20, 20));
        stage.At(400);
        Assert.DoesNotContain(stage.Log, entry => entry.Contains("Dismiss", StringComparison.Ordinal));
    }

    // ── O11: focus ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O11_Focus_Leaves_At_Present_Goes_To_The_Overlay_After_It_And_Comes_Back_After_The_Dismiss()
    {
        using var stage = new Stage();
        var overlay = new TestOverlay();
        stage.Opener.Focus();

        overlay.Present(stage.Window);
        Assert.Null(stage.Focused);
        stage.At(200);
        Assert.Same(overlay, stage.Focused);

        overlay.Dismiss();
        stage.At(400);
        Assert.Same(stage.Opener, stage.Focused);
    }

    [AvaloniaFact]
    public void O11_Focus_Is_Not_Given_Back_When_Something_Has_It_After_The_Dismiss()
    {
        using var stage = new Stage();
        // Scoped, as the alert of F6 is, so that the trap leaves focus behind it alone.
        var overlay = new TestOverlay { Scoped = true };
        stage.Opener.Focus();
        overlay.Present(stage.Window);
        stage.At(200);
        overlay.IonWillDismiss += (_, _) => stage.Other.Focus();

        overlay.Dismiss();
        stage.At(400);
        Assert.Same(stage.Other, stage.Focused);
    }

    [AvaloniaFact]
    public void O11_Without_KeyboardClose_The_Overlay_Takes_No_Focus_And_Focus_Inside_Stays()
    {
        using var stage = new Stage();
        var plain = new TestOverlay { KeyboardClose = false };
        plain.Present(stage.Window);
        stage.At(200);
        Assert.Null(stage.Focused);
        plain.Dismiss();
        stage.At(400);

        var inside = TestOverlay.Button("Inside");
        var overlay = new TestOverlay(inside);
        overlay.IonDidPresent += (_, _) => inside.Focus();
        overlay.Present(stage.Window);
        stage.At(600);
        Assert.Same(inside, stage.Focused);
    }

    [AvaloniaFact]
    public void O11_A_Focused_TextBox_Is_Blurred_As_The_Animation_Starts()
    {
        using var stage = new Stage();
        var overlay = new TestOverlay { KeyboardClose = false };
        overlay.IonWillPresent += (_, _) => stage.Text.Focus();
        overlay.Present(stage.Window);
        Assert.Same(stage.Text, stage.Focused);
        stage.At(200);
        overlay.Dismiss();
        stage.At(400);

        var closing = new TestOverlay();
        closing.IonWillPresent += (_, _) => stage.Text.Focus();
        closing.Present(stage.Window);
        Assert.Null(stage.Focused);
    }

    [AvaloniaFact]
    public void O11_An_Overlay_That_Does_Not_Steal_Focus_Leaves_It_Where_It_Is()
    {
        using var stage = new Stage();
        var toast = new TestOverlay { Steals = false, KeyboardClose = false };
        stage.Opener.Focus();
        toast.Present(stage.Window);
        Assert.Same(stage.Opener, stage.Focused);
        stage.At(200);

        // Not given back after either: the app's focus elsewhere is kept.
        stage.Other.Focus();
        toast.Dismiss();
        stage.At(400);
        Assert.Same(stage.Other, stage.Focused);
    }

    // ── O12, O13, O14: the focus trap ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void O12_Tab_And_Shift_Tab_Cycle_Inside_The_Overlay()
    {
        using var stage = new Stage();
        var m1 = TestOverlay.Button("m1");
        var m2 = TestOverlay.Button("m2");
        var overlay = new TestOverlay(m1, m2);
        stage.Opener.Focus();
        overlay.Present(stage.Window);
        stage.At(200);

        var seen = new List<IInputElement?>();
        for (var i = 0; i < 4; i++)
        {
            stage.Key(PhysicalKey.Tab);
            seen.Add(stage.Focused);
        }

        for (var i = 0; i < 3; i++)
        {
            stage.Key(PhysicalKey.Tab, RawInputModifiers.Shift);
            seen.Add(stage.Focused);
        }

        // F4: m1, m2, m1, m2; m1, m2, m1.
        Assert.Equal<IInputElement?>([m1, m2, m1, m2, m1, m2, m1], seen);
    }

    [AvaloniaFact]
    public void O12_Focus_Given_To_Something_Behind_Moves_Inside_To_The_First_Or_The_Last()
    {
        using var stage = new Stage();
        var m1 = TestOverlay.Button("m1");
        var m2 = TestOverlay.Button("m2");
        var overlay = new TestOverlay(m1, m2);
        overlay.Present(stage.Window);
        stage.At(200);

        m2.Focus();
        stage.Other.Focus();
        Assert.Same(m1, stage.Focused);

        // F5: the first is where focus was, so the last.
        stage.Other.Focus();
        Assert.Same(m2, stage.Focused);
    }

    [AvaloniaFact]
    public void O12_With_Nothing_Focusable_Inside_The_Overlay_Itself_Takes_It()
    {
        using var stage = new Stage();
        var overlay = new TestOverlay(new TextBlock { Text = "Nothing to focus" }) { KeyboardClose = false };
        overlay.Present(stage.Window);
        stage.At(200);

        stage.Other.Focus();
        Assert.Same(overlay, stage.Focused);
    }

    [AvaloniaFact]
    public void O13_The_Scoped_Trap_Wraps_At_Its_Sentinels_And_Leaves_Focus_Behind_It_Alone()
    {
        using var stage = new Stage();
        var a1 = TestOverlay.Button("a1");
        var a2 = TestOverlay.Button("a2");
        var overlay = new TestOverlay(a1, a2) { Scoped = true };
        overlay.Present(stage.Window);
        stage.At(200);

        a1.Focus();
        stage.Key(PhysicalKey.Tab);
        Assert.Same(a2, stage.Focused);
        stage.Key(PhysicalKey.Tab);
        Assert.Same(a1, stage.Focused);
        stage.Key(PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.Same(a2, stage.Focused);

        // F3.
        stage.Other.Focus();
        Assert.Same(stage.Other, stage.Focused);
    }

    [AvaloniaFact]
    public void O14_Only_The_Top_Overlay_That_Steals_Focus_Traps_It()
    {
        using var stage = new Stage();
        var inside = TestOverlay.Button("Inside");
        var dialog = new TestOverlay(inside);
        var toast = new TestOverlay(TestOverlay.Button("Toast")) { Steals = false, KeyboardClose = false };
        dialog.Present(stage.Window);
        toast.Present(stage.Window);
        stage.At(200);

        stage.Other.Focus();
        Assert.Same(inside, stage.Focused);
    }

    [AvaloniaFact]
    public void O14_A_Focus_Moving_To_A_Toast_Goes_Back_To_The_Last_Focus_In_The_Overlay()
    {
        using var stage = new Stage();
        var inside = TestOverlay.Button("Inside");
        var dialog = new TestOverlay(inside, TestOverlay.Button("Second"));
        var toastButton = TestOverlay.Button("Toast");
        var toast = new TestOverlay(toastButton) { Steals = false, KeyboardClose = false };
        dialog.Present(stage.Window);
        toast.Present(stage.Window);
        stage.At(200);
        inside.Focus();

        toastButton.Focus();
        Assert.Same(inside, stage.Focused);
    }

    [AvaloniaFact]
    public void O14_An_Overlay_With_Its_Trap_Disabled_Traps_Nothing()
    {
        using var stage = new Stage();
        var under = new TestOverlay(TestOverlay.Button("Under"));
        var top = new TestOverlay(TestOverlay.Button("Top")) { Trap = false };
        under.Present(stage.Window);
        top.Present(stage.Window);
        stage.At(200);

        stage.Other.Focus();
        Assert.Same(stage.Other, stage.Focused);
    }

    // ── O15: which top level ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avalonia's own desktop lifetime, which is internal in 12, as its interfaces can't be implemented outside it. Made by
    /// reflection, so that the test goes through the real one.
    /// </summary>
    private static IClassicDesktopStyleApplicationLifetime DesktopLifetime() =>
        (IClassicDesktopStyleApplicationLifetime)Activator.CreateInstance(
            typeof(Window).Assembly.GetType("Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime", throwOnError: true)!,
            nonPublic: true)!;

    [AvaloniaFact]
    public void O15_Present_Uses_The_Applications_Top_Level()
    {
        using var stage = new Stage();
        var desktop = DesktopLifetime();
        Assert.Null(OverlayHost.TopLevelOf(desktop));
        desktop.MainWindow = stage.Window;
        Assert.Same(stage.Window, OverlayHost.TopLevelOf(desktop));
        Assert.Null(OverlayHost.TopLevelOf(null));

        // TestApp has no lifetime, so Present() has nowhere to go.
        Assert.Null(Application.Current!.ApplicationLifetime);
        Assert.Throws<InvalidOperationException>(() => { _ = new TestOverlay().Present(); });
    }

    [AvaloniaFact]
    public async Task O15_A_Top_Level_With_No_Overlay_Layer_Cannot_Present()
    {
        // TestApp has no base theme, so its window has no template and no layers.
        using var clock = new TestClock();
        using var scene = new Scene(new Panel());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TestOverlay().Present(scene.Window));
    }
}
