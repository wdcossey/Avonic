using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Overlays;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Ionic's <c>ion-app</c> and the utilities it starts. The measured cases (M1 and the rest) are from real Ionic 8 in Chromium, md,
/// 390 x 844, with Capacitor's events dispatched by hand: see docs/components/ion-app/PLAN.md.
/// </summary>
public class AvonicAppTests
{
    private static readonly string Focused = FocusVisible.FocusedPseudoClass;

    private static RoutedEventArgs Back(Window window)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        window.RaiseEvent(e);
        Dispatcher.UIThread.RunJobs();
        return e;
    }

    private static Scene InApp(out AvonicApp app, Control? content = null)
    {
        app = new AvonicApp { Content = content ?? new Panel() };
        return new Scene(app);
    }

    // ── B: the hardware back button ──────────────────────────────────────────────────────────

    // Measured M1: handlers at 0, 100, 100, 99, -1: the second 100 runs, and its next() runs the first 100, which stops.
    [AvaloniaFact]
    public void B1_The_Highest_Priority_Runs_And_Of_Equal_Ones_The_Last_Registered_And_Next_Runs_The_Next()
    {
        using var scene = InApp(out _);
        var log = new List<string>();
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) =>
        {
            e.Register(0, next => { log.Add("p0"); next(); });
            e.Register(100, _ => log.Add("p100-first"));
            e.Register(100, next => { log.Add("p100-second"); next(); });
            e.Register(99, next => { log.Add("p99"); next(); });
            e.Register(-1, _ => log.Add("p-1"));
        });

        Back(scene.Window);
        Assert.Equal(["p100-second", "p100-first"], log);
    }

    // Measured M2d: a handler at 5 that calls next() twice runs 4 and then 3.
    [AvaloniaFact]
    public void B1_Each_Call_Of_Next_Runs_One_More()
    {
        using var scene = InApp(out _);
        var log = new List<string>();
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) =>
        {
            e.Register(5, next => { log.Add("5"); next(); next(); });
            e.Register(4, _ => log.Add("4"));
            e.Register(3, _ => log.Add("3"));
        });

        Back(scene.Window);
        Assert.Equal(["5", "4", "3"], log);
    }

    // Measured M2: a handler's 300 ms promise: a back press at 100 ms is ignored, one at 400 runs.
    [AvaloniaFact]
    public void B2_While_A_Handler_Is_Still_Running_A_Back_Press_Does_Nothing()
    {
        using var scene = InApp(out _);
        var runs = 0;
        TaskCompletionSource? pending = null;
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) => e.Register(10, _ =>
        {
            runs++;
            pending = new TaskCompletionSource();
            return pending.Task;
        }));

        Back(scene.Window);
        var busy = Back(scene.Window);
        Assert.Equal(1, runs);
        Assert.True(busy.Handled);

        pending!.SetResult();
        Dispatcher.UIThread.RunJobs();
        Back(scene.Window);
        Assert.Equal(2, runs);
    }

    // Measured M2b: a handler that throws doesn't leave it busy.
    [AvaloniaFact]
    public void B2_A_Handler_That_Throws_Does_Not_Leave_It_Busy()
    {
        using var scene = InApp(out _);
        var log = new List<string>();
        var throws = true;
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) => e.Register(20, _ =>
        {
            log.Add(throws ? "throws" : "after");
            if (throws)
            {
                throws = false;
                throw new InvalidOperationException("x");
            }
        }));

        Back(scene.Window);
        Back(scene.Window);
        Assert.Equal(["throws", "after"], log);
    }

    [AvaloniaFact]
    public void B3_The_Back_Request_Is_Handled_Only_When_Something_Registers()
    {
        using var scene = InApp(out _);
        var register = false;
        var raised = 0;
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) =>
        {
            raised++;
            if (register)
            {
                e.Register(0, _ => { });
            }
        });

        Assert.False(Back(scene.Window).Handled);
        register = true;
        Assert.True(Back(scene.Window).Handled);
        Assert.Equal(2, raised);
    }

    // Measured M3b: hardwareBackButton true, no ion-app: no ionBackButton.
    [AvaloniaFact]
    public void B4_Without_An_App_Nothing_Is_Raised()
    {
        using var scene = new Scene(new Panel());
        var raised = 0;
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, _) => raised++);

        Back(scene.Window);
        Assert.Equal(0, raised);
    }

    // Measured M8: an alert with backdropDismiss takes the back press at 100 over the app's 0; one without leaves it to the app.
    [AvaloniaFact]
    public void B4_In_An_App_An_Overlay_Takes_The_Back_Press_At_100_And_One_That_Cannot_Leaves_It()
    {
        using var stage = new AvonicOverlayTests.Stage();
        var app = new AvonicApp();
        var content = stage.Window.Content;
        stage.Window.Content = null;
        app.Content = content;
        stage.Window.Content = app;
        AvonicOverlayTests.Stage.Settle();
        Assert.True(HardwareBackButton.IsStarted(stage.Window));

        var log = new List<string>();
        stage.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) => e.Register(0, _ => log.Add("app@0")));
        var overlay = stage.Watch(new TestOverlay());
        overlay.Present(stage.Window);
        stage.At(200);

        stage.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        stage.At(400);
        Assert.Contains("didDismiss(,backdrop)@376", stage.Log);
        Assert.Empty(log);

        var stays = stage.Watch(new TestOverlay { BackdropDismiss = false }, "b");
        stays.Present(stage.Window);
        stage.At(600);
        stage.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        stage.At(800);
        Assert.Equal(["app@0"], log);
        Assert.DoesNotContain(stage.Log, entry => entry.StartsWith("bwillDismiss", StringComparison.Ordinal));
    }

    // Measured M7: hardwareBackButton false: no ionBackButton.
    [AvaloniaFact]
    public void B5_With_The_Config_Off_Nothing_Is_Raised_And_An_Overlay_Stays()
    {
        var before = AvonicConfig.HardwareBackButton;
        AvonicConfig.HardwareBackButton = false;
        try
        {
            using var stage = new AvonicOverlayTests.Stage();
            var content = stage.Window.Content;
            stage.Window.Content = null;
            stage.Window.Content = new AvonicApp { Content = content };
            AvonicOverlayTests.Stage.Settle();
            var raised = 0;
            stage.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, _) => raised++);
            var overlay = stage.Watch(new TestOverlay());
            overlay.Present(stage.Window);
            stage.At(200);

            var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
            stage.Window.RaiseEvent(e);
            stage.At(400);
            Assert.Equal(0, raised);
            Assert.False(e.Handled);
            Assert.DoesNotContain(stage.Log, entry => entry.Contains("Dismiss", StringComparison.Ordinal));
        }
        finally
        {
            AvonicConfig.HardwareBackButton = before;
        }
    }

    [AvaloniaFact]
    public void B6_An_App_Taken_Out_Stops_Raising_It()
    {
        using var scene = InApp(out _);
        var raised = 0;
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, _) => raised++);
        Back(scene.Window);

        scene.Window.Content = new Panel();
        Dispatcher.UIThread.RunJobs();
        Back(scene.Window);
        Assert.Equal(1, raised);
        Assert.False(HardwareBackButton.IsStarted(scene.Window));
    }

    [AvaloniaFact]
    public void B7_A_Back_Request_The_App_Has_Already_Handled_Is_Left_Alone()
    {
        var inner = new Button();
        using var scene = InApp(out _, inner);
        var raised = 0;
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, _) => raised++);
        inner.AddHandler(TopLevel.BackRequestedEvent, (_, e) => e.Handled = true);

        inner.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, raised);
    }

    [AvaloniaFact]
    public void B8_Alt_Left_Is_A_Back_Press_On_Desktop()
    {
        using var scene = InApp(out _);
        var raised = 0;
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, _) => raised++);

        scene.Window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        scene.Window.KeyReleaseQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.Alt);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, raised);
    }

    // ── K: keyboard assist ───────────────────────────────────────────────────────────────────

    private sealed class FakePane
    {
        private EventHandler<InputPaneStateEventArgs>? _stateChanged;

        public FakePane()
        {
            Pane = DispatchProxy.Create<IInputPane, Proxy>();
            ((Proxy)(object)Pane).Owner = this;
        }

        public IInputPane Pane { get; }

        public InputPaneState State { get; private set; }

        public Rect OccludedRect { get; private set; }

        public void Set(InputPaneState state, double height, double durationMs)
        {
            var start = OccludedRect;
            State = state;
            OccludedRect = state == InputPaneState.Open ? new Rect(0, 844 - height, 390, height) : default;
            _stateChanged?.Invoke(Pane, new InputPaneStateEventArgs(state, start, OccludedRect, TimeSpan.FromMilliseconds(durationMs), null));
            Dispatcher.UIThread.RunJobs();
        }

        public class Proxy : DispatchProxy
        {
            public FakePane? Owner { get; set; }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                var owner = Owner!;
                switch (targetMethod?.Name)
                {
                    case "get_State":
                        return owner.State;
                    case "get_OccludedRect":
                        return owner.OccludedRect;
                    case "add_StateChanged":
                        owner._stateChanged += (EventHandler<InputPaneStateEventArgs>)args![0]!;
                        return null;
                    case "remove_StateChanged":
                        owner._stateChanged -= (EventHandler<InputPaneStateEventArgs>)args![0]!;
                        return null;
                    default:
                        throw new NotSupportedException(targetMethod?.Name);
                }
            }
        }
    }

    /// <summary>A window in an app, given a fake input pane before the app is attached.</summary>
    private sealed class KeyboardStage : IDisposable
    {
        private readonly Func<TopLevel, IInputPane?> _previous = KeyboardController.InputPaneOf;

        public KeyboardStage(bool inApp = true)
        {
            Clock = new TestClock();
            Window = new Window { Width = 390, Height = 844 };
            var pane = Pane.Pane;
            var window = Window;
            var previous = _previous;
            KeyboardController.InputPaneOf = candidate => ReferenceEquals(candidate, window) ? pane : previous(candidate);
            Window.Content = inApp ? new AvonicApp() : new Panel();
            Window.AddHandler(KeyboardAssist.IonKeyboardDidShowEvent, (_, e) => Log.Add($"show {e.KeyboardHeight}@{Clock.Now.TotalMilliseconds}"));
            Window.AddHandler(KeyboardAssist.IonKeyboardDidHideEvent, (_, _) => Log.Add($"hide@{Clock.Now.TotalMilliseconds}"));
            Window.Show();
            Dispatcher.UIThread.RunJobs();
        }

        public TestClock Clock { get; }

        public FakePane Pane { get; } = new();

        public Window Window { get; }

        public List<string> Log { get; } = [];

        public void Dispose()
        {
            Window.Close();
            Dispatcher.UIThread.RunJobs();
            KeyboardController.InputPaneOf = _previous;
            Clock.Dispose();
        }
    }

    // Measured M5: keyboardDidShow 300, keyboardDidShow 320, keyboardDidHide twice: show 300, show 320, hide, hide.
    [AvaloniaFact]
    public void K1_Each_Opening_And_Closing_Raises_Its_Event_With_The_Height_Once_The_Pane_Has_Moved()
    {
        using var stage = new KeyboardStage();
        stage.Pane.Set(InputPaneState.Open, 300, 250);
        stage.Clock.At(249);
        Assert.Empty(stage.Log);
        stage.Clock.At(250);
        Assert.Equal(["show 300@250"], stage.Log);

        stage.Pane.Set(InputPaneState.Open, 320, 0);
        stage.Clock.At(250);
        stage.Pane.Set(InputPaneState.Closed, 0, 200);
        stage.Clock.At(450);
        stage.Pane.Set(InputPaneState.Closed, 0, 0);
        stage.Clock.At(450);
        Assert.Equal(["show 300@250", "show 320@250", "hide@450", "hide@450"], stage.Log);
    }

    [AvaloniaFact]
    public void K2_A_Change_Before_The_Last_Has_Finished_Replaces_It()
    {
        using var stage = new KeyboardStage();
        stage.Pane.Set(InputPaneState.Open, 300, 250);
        stage.Clock.At(100);
        stage.Pane.Set(InputPaneState.Closed, 0, 150);
        stage.Clock.At(400);
        Assert.Equal(["hide@250"], stage.Log);
    }

    [AvaloniaFact]
    public void K3_Without_An_App_Nothing_Is_Raised()
    {
        using var stage = new KeyboardStage(inApp: false);
        stage.Pane.Set(InputPaneState.Open, 300, 0);
        stage.Clock.At(100);
        Assert.Empty(stage.Log);
    }

    // ── S: the status bar tap ────────────────────────────────────────────────────────────────

    private sealed class StatusStage : IDisposable
    {
        public StatusStage(bool inApp = true, bool covered = false)
        {
            Clock = new TestClock();
            Content = new AvonicContent { Content = new Border { Height = 3000 } };
            var root = new Panel { Children = { Content } };
            if (covered)
            {
                root.Children.Add(new Border { Background = Avalonia.Media.Brushes.Transparent });
            }

            Scene = new Scene(inApp ? new AvonicApp { Content = root } : root);
            Content.GetScrollElement()!.UpdateLayout();
            Scroll.Offset = new Vector(0, 600);
            Render();
        }

        public TestClock Clock { get; }

        public AvonicContent Content { get; }

        public ScrollViewer Scroll => Content.GetScrollElement()!;

        public Scene Scene { get; }

        public void Render()
        {
            for (var i = 0; i < 2; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            Dispatcher.UIThread.RunJobs();
        }

        public void Tap() => Scene.Window.RaiseEvent(new RoutedEventArgs(StatusTap.StatusTapEvent));

        public void At(double ms)
        {
            while (Clock.Now.TotalMilliseconds < ms)
            {
                Clock.At(Math.Min(Math.Floor(Clock.Now.TotalMilliseconds) + 1, ms));
                Dispatcher.UIThread.RunJobs();
            }
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    // Measured M4: scrollTop 600 → 0 by about 300 ms after the frame it starts in, with --overflow hidden throughout, and back after.
    [AvaloniaFact]
    public void S1_A_Status_Tap_Scrolls_The_Content_In_The_Middle_To_The_Top_In_300_Ms_With_Its_Scrolling_Held()
    {
        using var stage = new StatusStage();
        stage.Tap();
        Assert.True(stage.Content.ScrollY);

        stage.At(16);
        Assert.False(stage.Content.ScrollY);
        Assert.Equal(600, stage.Scroll.Offset.Y);

        stage.At(160);
        Assert.InRange(stage.Scroll.Offset.Y, 1, 599);
        Assert.False(stage.Content.ScrollY);

        stage.At(400);
        Assert.Equal(0, stage.Scroll.Offset.Y);
        Assert.True(stage.Content.ScrollY);
    }

    [AvaloniaFact]
    public void S1_The_Content_s_Own_ScrollY_Comes_Back()
    {
        using var stage = new StatusStage();
        stage.Content.ScrollY = false;
        stage.Tap();
        stage.At(400);
        Assert.Equal(0, stage.Scroll.Offset.Y);
        Assert.False(stage.Content.ScrollY);
    }

    // Measured M4b: a fixed element over the middle: nothing.
    [AvaloniaFact]
    public void S2_Nothing_When_The_Middle_Of_The_Window_Is_Not_In_A_Content()
    {
        using var stage = new StatusStage(covered: true);
        stage.Tap();
        stage.At(400);
        Assert.Equal(600, stage.Scroll.Offset.Y);
    }

    [AvaloniaFact]
    public void S3_Nothing_With_The_Config_Off_Or_Without_An_App()
    {
        var before = AvonicConfig.StatusTap;
        AvonicConfig.StatusTap = false;
        try
        {
            using var stage = new StatusStage();
            stage.Tap();
            stage.At(400);
            Assert.Equal(600, stage.Scroll.Offset.Y);
        }
        finally
        {
            AvonicConfig.StatusTap = before;
        }

        using var noApp = new StatusStage(inApp: false);
        noApp.Tap();
        noApp.At(400);
        Assert.Equal(600, noApp.Scroll.Offset.Y);
    }

    // ── F: setFocus ──────────────────────────────────────────────────────────────────────────

    private static bool Shows(Control control) => ((IPseudoClasses)control.Classes).Contains(Focused);

    private sealed class FocusStage : IDisposable
    {
        public FocusStage()
        {
            A = new Border { Width = 50, Height = 50, Background = Avalonia.Media.Brushes.Transparent };
            B = new Border { Width = 50, Height = 50 };
            C = new Border { Width = 50, Height = 50 };
            First = new Button { Content = "1" };
            Second = new Button { Content = "2" };
            FocusVisible.SetIsEnabled(Second, true);
            App = new AvonicApp { Content = new StackPanel { Children = { A, B, C, First, Second } } };
            Scene = new Scene(App);
        }

        public AvonicApp App { get; }

        public Border A { get; }

        public Border B { get; }

        public Border C { get; }

        public Button First { get; }

        public Button Second { get; }

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public void Key(PhysicalKey key)
        {
            Window.KeyPressQwerty(key, RawInputModifiers.None);
            Window.KeyReleaseQwerty(key, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        public void Dispose() => Scene.Dispose();
    }

    // Measured M6: setFocus([a, b]) a and b; setFocus([cc]) only cc; setFocus([]) none.
    [AvaloniaFact]
    public void F1_SetFocus_Shows_The_Ring_On_Exactly_Those_Elements()
    {
        using var stage = new FocusStage();
        stage.App.SetFocus(stage.A, stage.B);
        Assert.True(Shows(stage.A));
        Assert.True(Shows(stage.B));
        Assert.False(Shows(stage.C));

        stage.App.SetFocus(stage.C);
        Assert.False(Shows(stage.A));
        Assert.False(Shows(stage.B));
        Assert.True(Shows(stage.C));

        stage.App.SetFocus();
        Assert.False(Shows(stage.C));
    }

    // Measured M6: Tab keeps it, 'a' clears it; a mousedown or touchstart clears it.
    [AvaloniaFact]
    public void F2_A_Focus_Key_Keeps_It_And_Any_Other_Key_Or_A_Press_Clears_It()
    {
        using var stage = new FocusStage();
        stage.App.SetFocus(stage.C);
        stage.Window.KeyPressQwerty(PhysicalKey.ShiftLeft, RawInputModifiers.None);
        stage.Window.KeyReleaseQwerty(PhysicalKey.ShiftLeft, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Shows(stage.C));

        stage.Key(PhysicalKey.A);
        Assert.False(Shows(stage.C));

        stage.App.SetFocus(stage.A);
        stage.Window.MouseDown(new Point(380, 830), MouseButton.Left);
        stage.Window.MouseUp(new Point(380, 830), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.False(Shows(stage.A));
    }

    // Measured M6: after a touch, b1.focus() leaves a; after Tab, focus on b1 clears a and shows b1's; a blur to the body clears it.
    [AvaloniaFact]
    public void F3_Focus_By_Code_Keeps_It_Focus_By_Keyboard_Or_Focus_Leaving_Everything_Clears_It()
    {
        using var stage = new FocusStage();
        stage.App.SetFocus(stage.A);
        stage.First.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(Shows(stage.A));

        stage.Second.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.False(Shows(stage.A));
        Assert.True(Shows(stage.Second));

        stage.App.SetFocus(stage.A);
        Assert.False(Shows(stage.Second));
        // The focused button is turned off, and Avalonia leaves nothing focused, as a blur leaves the body.
        stage.Second.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();
        Assert.Null(stage.Window.FocusManager!.GetFocusedElement());
        Assert.False(Shows(stage.A));
    }

    [AvaloniaFact]
    public void F4_SetFocus_On_The_Element_With_Keyboard_Focus_Keeps_It_Until_Focus_Moves_On()
    {
        using var stage = new FocusStage();
        stage.Second.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        stage.App.SetFocus(stage.Second);
        Assert.True(Shows(stage.Second));

        stage.First.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.False(Shows(stage.Second));
    }

    // ── P: the page ──────────────────────────────────────────────────────────────────────────

    // Measured: ion-app's host is "md ion-page hydrated", position absolute, inset 0, overflow visible.
    [AvaloniaFact]
    public void P1_It_Is_A_Page_That_Fills_What_It_Is_In_And_Does_Not_Clip()
    {
        var child = new Border();
        using var scene = InApp(out var app, child);
        Assert.Contains(AvonicApp.PageClass, app.Classes);
        Assert.Equal(new Rect(0, 0, 390, 844), app.Bounds);
        Assert.Equal(new Rect(0, 0, 390, 844), child.Bounds);
        Assert.False(app.ClipToBounds);
    }
}
