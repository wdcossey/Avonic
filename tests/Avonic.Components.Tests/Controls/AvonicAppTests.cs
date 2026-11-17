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
using Avalonia.VisualTree;
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

    // Ionic's docs: "If no previous view exists in the navigation stack, nothing will happen." Capacitor's App plugin takes every
    // back press, so the app stays.
    [AvaloniaFact]
    public void B3_The_Back_Request_Is_Handled_Even_When_Nothing_Registers_Unless_The_Extension_Says_Not()
    {
        using var scene = InApp(out _);
        var raised = 0;
        scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, _) => raised++);
        Assert.True(Back(scene.Window).Handled);

        var before = AvonicConfig.HandleUnclaimedBack;
        AvonicConfig.HandleUnclaimedBack = false;
        try
        {
            Assert.False(Back(scene.Window).Handled);
            scene.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) => e.Register(0, _ => { }));
            Assert.True(Back(scene.Window).Handled);
        }
        finally
        {
            AvonicConfig.HandleUnclaimedBack = before;
        }

        Assert.Equal(3, raised);
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

    [AvaloniaFact]
    public void B4_An_Overlay_Shown_Before_The_App_Was_Attached_Still_Goes_Through_The_Registry()
    {
        using var stage = new AvonicOverlayTests.Stage();
        var first = stage.Watch(new TestOverlay(), "first");
        first.Present(stage.Window);
        stage.At(200);
        _ = first.Dismiss();
        stage.At(400);

        var content = stage.Window.Content;
        stage.Window.Content = null;
        stage.Window.Content = new AvonicApp { Content = content };
        AvonicOverlayTests.Stage.Settle();
        var log = new List<string>();
        stage.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) => e.Register(200, _ => log.Add("app@200")));
        var overlay = stage.Watch(new TestOverlay(), "b");
        overlay.Present(stage.Window);
        stage.At(600);

        stage.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        stage.At(800);
        Assert.Equal(["app@200"], log);
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

        /// <summary>A phone's pane: at the bottom of a 390 by 844 top level.</summary>
        public void Set(InputPaneState state, double height, double durationMs) =>
            Set(state, state == InputPaneState.Open ? new Rect(0, 844 - height, 390, height) : default, durationMs);

        /// <summary>
        /// A pane at <paramref name="occluded"/>, in the top level's coordinates. Windows' reaches wherever the touch keyboard is on the
        /// screen, inside the window or not, with no animation; closed, it's an empty rect.
        /// </summary>
        public void Set(InputPaneState state, Rect occluded, double durationMs = 0)
        {
            var start = OccludedRect;
            State = state;
            OccludedRect = occluded;
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

        // Given while focus is elsewhere, and then reached by Tab: it shows, as Ionic's focusin gives it its own.
        stage.App.SetFocus(stage.Second);
        stage.Second.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Shows(stage.Second));
    }

    // ── I: input shims (scroll assist and scroll padding) ────────────────────────────────────

    /// <summary>
    /// The measured page: a 56 high header, then a content with 11 rows of 48, the field's item, and <paramref name="after"/> more
    /// rows, in 390 by 844, in an app with the shims on.
    /// </summary>
    private sealed class ShimStage : IDisposable
    {
        private readonly bool _shims = AvonicConfig.InputShims;
        private readonly Func<TopLevel, IInputPane?> _paneOf = KeyboardController.InputPaneOf;
        private readonly Func<string, bool> _isPlatform = InputShims.IsPlatform;

        /// <param name="pane">The top level's input pane, as on Windows, Android and iOS; none by default.</param>
        /// <param name="platform">The platform, as <see cref="OperatingSystem.IsOSPlatform"/> names it: Android by default.</param>
        /// <param name="before">How many rows of 48 are before the field's item: 11 by default, 1 with <paramref name="safe"/>.</param>
        public ShimStage(int after = 11, bool inApp = true, bool safe = false, bool shims = true, FakePane? pane = null,
            string platform = "android", int? before = null)
        {
            AvonicConfig.InputShims = shims;
            InputShims.IsPlatform = os => os == platform;
            Clock = new TestClock();
            var rows = new StackPanel();
            for (var i = 0; i < (before ?? (safe ? 1 : 11)); i++)
            {
                rows.Children.Add(new Border { Height = 48 });
            }

            Input = new AvonicInput();
            Item = new AvonicItem { Content = Input, Height = 48 };
            rows.Children.Add(Item);
            for (var i = 0; i < after; i++)
            {
                rows.Children.Add(new Border { Height = 48 });
            }

            Content = new AvonicContent { Content = rows };
            var header = Other;
            DockPanel.SetDock(header, Dock.Top);
            var page = new DockPanel { Children = { header, Content } };
            Control root = inApp ? new AvonicApp { Content = page } : page;
            if (pane is not null)
            {
                var previous = _paneOf;
                KeyboardController.InputPaneOf = top => ReferenceEquals(top.Content, root) ? pane.Pane : previous(top);
            }

            Scene = new Scene(root);
            Dispatcher.UIThread.RunJobs();
        }

        public TestClock Clock { get; }

        public AvonicInput Input { get; }

        public AvonicItem Item { get; }

        /// <summary>The header: a button, to move focus to.</summary>
        public Button Other { get; } = new() { Height = 56, Content = "Other" };

        public AvonicContent Content { get; }

        public Scene Scene { get; }

        public double Offset => Content.GetScrollElement()!.Offset.Y;

        public TextBox Native => Input.GetVisualDescendants().OfType<TextBox>().First();

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
            AvonicConfig.InputShims = _shims;
            KeyboardController.InputPaneOf = _paneOf;
            InputShims.IsPlatform = _isPlatform;
        }
    }

    // Measured: the item at 584 to 632 in a content from 56, 844 high: --keyboard-offset 290px at once, scrollTop 15 in the next
    // frame, 128 by 408 ms (min(400, 128 / 0.3)), and 0px about 120 ms after the blur.
    [AvaloniaFact]
    public void I1_A_Field_The_Keyboard_Would_Cover_Pads_The_Content_And_Scrolls_It_Above_The_Keyboard()
    {
        using var stage = new ShimStage();
        Assert.Equal(new Rect(0, 584, 390, 48), new Rect(stage.Item.TranslatePoint(default, stage.Scene.Window)!.Value, stage.Item.Bounds.Size));
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(290, stage.Content.KeyboardOffset);
        Assert.Equal(0, stage.Offset);

        stage.At(100);
        Assert.InRange(stage.Offset, 1, 127);
        stage.At(450);
        Assert.Equal(128, stage.Offset);

        stage.Other.Focus();
        Dispatcher.UIThread.RunJobs();
        var blurred = stage.Clock.Now.TotalMilliseconds;
        stage.At(blurred + 119);
        Assert.Equal(290, stage.Content.KeyboardOffset);
        stage.At(blurred + 120);
        Assert.Equal(0, stage.Content.KeyboardOffset);
    }

    // Measured on the short page: room for 78 only, so it waits a second for the keyboard, then scrolls as far as it can.
    [AvaloniaFact]
    public void I2_With_Too_Little_Room_It_Waits_For_The_Keyboard_Or_A_Second()
    {
        using var stage = new ShimStage(after: 0);
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(290, stage.Content.KeyboardOffset);
        stage.At(999);
        Assert.Equal(0, stage.Offset);
        stage.At(1500);
        Assert.Equal(78, stage.Offset);
    }

    private static void KeyboardShows(Window window, double height) =>
        window.RaiseEvent(new KeyboardDidShowEventArgs(KeyboardAssist.IonKeyboardDidShowEvent, window, height));

    [AvaloniaFact]
    public void I2_The_Keyboard_Showing_Ends_The_Wait()
    {
        using var stage = new ShimStage(after: 0);
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(300);
        KeyboardShows(stage.Scene.Window, 290);
        stage.At(350);
        Assert.InRange(stage.Offset, 1, 77);
        stage.At(800);
        Assert.Equal(78, stage.Offset);
    }

    // scroll-assist.js: a later ionKeyboardDidShow is the keyboard changing, and runs it again with the keyboard's own height,
    // without waiting. Here the first does too, once the focus's scroll has ended (a deviation: see I8). With 300 after the
    // scroll to 128, the item's bottom at 504 is 10 below 844 - 300 - 50; with 400 after that, the bottom at 494 is 100 below 394.
    [AvaloniaFact]
    public void I5_A_Later_Keyboard_Show_Runs_It_Again_With_The_Keyboard_s_Height()
    {
        using var stage = new ShimStage();
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(450);
        KeyboardShows(stage.Scene.Window, 300);
        Assert.Equal(300, stage.Content.KeyboardOffset);
        stage.At(900);
        Assert.Equal(138, stage.Offset);

        KeyboardShows(stage.Scene.Window, 400);
        Assert.Equal(400, stage.Content.KeyboardOffset);
        stage.At(1400);
        Assert.Equal(238, stage.Offset);
    }

    // Measured on a Pixel 8: the keyboard is about 355, and Ionic's 290 left the field under it, as Avalonia doesn't scroll a
    // focused field into what the keyboard leaves, as Chrome does. A first show of 355 during the focus's scroll runs it again when
    // that ends: the item's bottom at 504 is 65 below 844 - 355 - 50.
    [AvaloniaFact]
    public void I8_The_Keyboard_s_Own_Height_Runs_It_Again_When_The_Scroll_Ends()
    {
        using var stage = new ShimStage();
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(200);
        KeyboardShows(stage.Scene.Window, 355);
        Assert.Equal(290, stage.Content.KeyboardOffset);
        stage.At(450);
        Assert.Equal(355, stage.Content.KeyboardOffset);
        stage.At(1000);
        Assert.Equal(193, stage.Offset);
    }

    // Measured on a Pixel 8: the keyboard's 354.3 is above the 24 high navigation bar, which the top level reaches under, edge to
    // edge. Without the bar taken off, the item ended 26 above the keyboard. With it, the bottom at 632 is 152 below
    // 844 - 24 - 290 - 50.
    [AvaloniaFact]
    public void I9_The_Navigation_Bar_Under_The_Keyboard_Is_Not_Room()
    {
        var previous = AppSafeArea.InsetsOf;
        var fake = new FakeInsets();
        fake.Set(new Thickness(0, 50.3, 0, 24));
        AppSafeArea.InsetsOf = _ => fake.Insets;
        try
        {
            using var stage = new ShimStage();
            stage.Native.Focus();
            Dispatcher.UIThread.RunJobs();
            stage.At(600);
            Assert.Equal(152, stage.Offset);
        }
        finally
        {
            AppSafeArea.InsetsOf = previous;
        }
    }

    // The same height as the estimate changes nothing.
    [AvaloniaFact]
    public void I8_A_Keyboard_Of_The_Estimated_Height_Changes_Nothing()
    {
        using var stage = new ShimStage();
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(200);
        KeyboardShows(stage.Scene.Window, 290);
        stage.At(1000);
        Assert.Equal(128, stage.Offset);
        Assert.Equal(290, stage.Content.KeyboardOffset);
    }

    // input-shims.js: scrollAssist, scrollPadding and keyboardHeight from the config.
    [AvaloniaFact]
    public void I6_ScrollAssist_ScrollPadding_And_KeyboardHeight_Come_From_The_Config()
    {
        var (assist, padding, height) = (AvonicConfig.ScrollAssist, AvonicConfig.ScrollPadding, AvonicConfig.KeyboardHeight);
        try
        {
            AvonicConfig.ScrollAssist = false;
            using (var off = new ShimStage())
            {
                off.Native.Focus();
                Dispatcher.UIThread.RunJobs();
                off.At(500);
                Assert.Equal(0, off.Content.KeyboardOffset);
                Assert.Equal(0, off.Offset);
            }

            // Without padding there's still room on this page, 1104 - 788, so it scrolls the same.
            AvonicConfig.ScrollAssist = true;
            AvonicConfig.ScrollPadding = false;
            using (var unpadded = new ShimStage())
            {
                unpadded.Native.Focus();
                Dispatcher.UIThread.RunJobs();
                unpadded.At(500);
                Assert.Equal(0, unpadded.Content.KeyboardOffset);
                Assert.Equal(128, unpadded.Offset);
            }

            // A 400 keyboard: the item's bottom, 632, is 238 below 844 - 400 - 50.
            AvonicConfig.ScrollPadding = true;
            AvonicConfig.KeyboardHeight = 400;
            using var taller = new ShimStage();
            taller.Native.Focus();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(400, taller.Content.KeyboardOffset);
            taller.At(500);
            Assert.Equal(238, taller.Offset);
        }
        finally
        {
            (AvonicConfig.ScrollAssist, AvonicConfig.ScrollPadding, AvonicConfig.KeyboardHeight) = (assist, padding, height);
        }
    }

    // scroll-assist.js: a password field with too little room waits for a second ionKeyboardDidShow, for iOS's passwords bar.
    [AvaloniaFact]
    public void I7_A_Password_Field_Waits_For_A_Second_Keyboard_Show()
    {
        using var stage = new ShimStage(after: 0);
        stage.Input.Type = InputType.Password;
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(200);
        KeyboardShows(stage.Scene.Window, 290);
        stage.At(500);
        Assert.Equal(0, stage.Offset);
        KeyboardShows(stage.Scene.Window, 290);
        stage.At(950);
        Assert.Equal(78, stage.Offset);
    }

    [AvaloniaFact]
    public void I3_A_Field_Already_Clear_Of_The_Keyboard_Neither_Pads_Nor_Scrolls()
    {
        using var stage = new ShimStage(safe: true);
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(500);
        Assert.Equal(0, stage.Content.KeyboardOffset);
        Assert.Equal(0, stage.Offset);
    }

    [AvaloniaFact]
    public void I4_Nothing_With_The_Shims_Off_Or_Without_An_App()
    {
        foreach (var make in new Func<ShimStage>[] { () => new ShimStage(shims: false), () => new ShimStage(inApp: false) })
        {
            using (var stage = make())
            {
                stage.Native.Focus();
                Dispatcher.UIThread.RunJobs();
                stage.At(500);
                Assert.Equal(0, stage.Content.KeyboardOffset);
                Assert.Equal(0, stage.Offset);
            }
        }
    }

    // Second review: a field that can't reach its place doesn't run the assist again and again. With 355 the rerun pads by 355,
    // which gives room for 143 (576 + 355 - 788), short of the 193 it wants: it scrolls that far, once, and everything stops.
    [AvaloniaFact]
    public void I10_A_Field_That_Cannot_Reach_Its_Place_Stops()
    {
        using var stage = new ShimStage(after: 0);
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(300);
        KeyboardShows(stage.Scene.Window, 355);
        stage.At(3000);
        Assert.Equal(143, stage.Offset);
        Assert.Equal(0, stage.Clock.Pending);
    }

    /// <summary>Fields with <paramref name="rowsBefore"/> 48 rows before each, then 11 more, under a 56 header, in an app.</summary>
    private sealed class FieldsStage : IDisposable
    {
        private readonly bool _shims = AvonicConfig.InputShims;
        private readonly Func<string, bool> _isPlatform = InputShims.IsPlatform;

        /// <summary>On Android, as Ionic's flow is a phone's.</summary>
        public FieldsStage(params int[] rowsBefore)
        {
            AvonicConfig.InputShims = true;
            InputShims.IsPlatform = os => os == "android";
            Clock = new TestClock();
            var rows = new StackPanel();
            foreach (var count in rowsBefore)
            {
                for (var i = 0; i < count; i++)
                {
                    rows.Children.Add(new Border { Height = 48 });
                }

                var input = new AvonicInput();
                Inputs.Add(input);
                rows.Children.Add(new AvonicItem { Content = input, Height = 48 });
            }

            for (var i = 0; i < 11; i++)
            {
                rows.Children.Add(new Border { Height = 48 });
            }

            Content = new AvonicContent { Content = rows };
            var header = new Button { Height = 56, Content = "Other" };
            DockPanel.SetDock(header, Dock.Top);
            App = new AvonicApp { Content = new DockPanel { Children = { header, Content } } };
            Other = header;
            Scene = new Scene(App);
            Dispatcher.UIThread.RunJobs();
        }

        public TestClock Clock { get; }

        public List<AvonicInput> Inputs { get; } = [];

        public AvonicContent Content { get; }

        public AvonicApp App { get; }

        public Button Other { get; }

        public Scene Scene { get; }

        public double Offset => Content.GetScrollElement()!.Offset.Y;

        public void Focus(int field)
        {
            Inputs[field].GetVisualDescendants().OfType<TextBox>().First().Focus();
            Dispatcher.UIThread.RunJobs();
        }

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
            AvonicConfig.InputShims = _shims;
            InputShims.IsPlatform = _isPlatform;
        }
    }

    // Second review: a field focused while the keyboard is already open reckons with its own height, as no show comes for it.
    // A at 488 to 536: 32 with 290, then 65 more with 355, 97. B at 584 to 632, so 535 at 97: with 355, 96 more, 193.
    [AvaloniaFact]
    public void I11_A_Second_Field_With_The_Keyboard_Open_Reckons_With_Its_Height()
    {
        using var stage = new FieldsStage(9, 1);
        stage.Focus(0);
        stage.At(300);
        KeyboardShows(stage.Scene.Window, 355);
        stage.At(1000);
        Assert.Equal(97, stage.Offset);

        stage.Focus(1);
        Assert.Equal(355, stage.Content.KeyboardOffset);
        stage.At(1600);
        Assert.Equal(193, stage.Offset);

        // Closed, the next field reckons with the estimate again.
        stage.Scene.Window.RaiseEvent(new RoutedEventArgs(KeyboardAssist.IonKeyboardDidHideEvent, stage.Scene.Window));
        stage.Other.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(2000);
        stage.Content.GetScrollElement()!.Offset = default;
        Dispatcher.UIThread.RunJobs();
        stage.Focus(1);
        Assert.Equal(290, stage.Content.KeyboardOffset);
    }

    // Second review: an app taken out with a padding pending forgets it, so a field clear of the keyboard in the next one isn't
    // padded by it. Field 0 at 104 to 152 is clear; field 1 at 632 to 680 isn't.
    [AvaloniaFact]
    public void I12_An_App_Taken_Out_Forgets_Its_Padding()
    {
        using var stage = new FieldsStage(1, 10);
        stage.Focus(1);
        stage.At(600);
        Assert.Equal(290, stage.Content.KeyboardOffset);
        stage.Other.Focus();
        Dispatcher.UIThread.RunJobs();

        stage.Scene.Window.Content = null;
        Dispatcher.UIThread.RunJobs();
        stage.Scene.Window.Content = stage.App;
        Dispatcher.UIThread.RunJobs();
        stage.Content.GetScrollElement()!.Offset = default;
        Dispatcher.UIThread.RunJobs();

        stage.Focus(0);
        stage.At(1500);
        Assert.Equal(0, stage.Content.KeyboardOffset);
    }

    // Second review: a SetFocus ring reached by Tab, then focus moved by code: it goes, as Ionic's keyboard-mode focusin replaces
    // the set.
    [AvaloniaFact]
    public void F6_A_Ring_From_SetFocus_Reached_By_Tab_Goes_When_Code_Moves_Focus()
    {
        using var stage = new FocusStage();
        stage.App.SetFocus(stage.Second);
        stage.Second.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Shows(stage.Second));

        stage.First.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.False(Shows(stage.Second));
    }

    // scroll-data.js rounds with Math.round, which takes a half up: 12.5 above the safe top is -12, not -13.
    [AvaloniaFact]
    public void I13_The_Scroll_Amount_Rounds_A_Half_Up_As_Math_Round_Does()
    {
        var (amount, _, _) = InputShims.ScrollData(new Rect(0, 58.5, 390, 48), new Rect(0, 56, 390, 788), 290, 844);
        Assert.Equal(-12, amount);
    }

    // input-shims.js reads scrollAssist, scrollPadding and keyboardHeight once, when ion-app starts the shims.
    [AvaloniaFact]
    public void I14_The_Config_Is_Read_When_The_App_Is_Attached()
    {
        var before = AvonicConfig.ScrollAssist;
        try
        {
            using var stage = new ShimStage();
            AvonicConfig.ScrollAssist = false;
            stage.Native.Focus();
            Dispatcher.UIThread.RunJobs();
            stage.At(500);
            Assert.Equal(128, stage.Offset);
        }
        finally
        {
            AvonicConfig.ScrollAssist = before;
        }
    }

    // ── W: the input shims on Windows, following the touch keyboard (#189) ───────────────────

    // A deviation: Ionic's needInputShims() is iOS and Chrome on Android, and off in a desktop browser; Windows' touch keyboard covers
    // the window as a phone's does.
    [Fact]
    public void W1_The_Shims_Are_On_By_Default_On_Android_iOS_And_Windows()
    {
        foreach (var on in new[] { "android", "ios", "windows" })
        {
            Assert.True(AvonicConfig.InputShimsByDefault(os => os == on), on);
        }

        foreach (var off in new[] { "linux", "macos", "freebsd", "browser" })
        {
            Assert.False(AvonicConfig.InputShimsByDefault(os => os == off), off);
        }
    }

    // Windows' pane (Avalonia's WindowsInputPane) is there in every window, keyboard or not, and reports the touch keyboard with no
    // animation. Focus alone does nothing; the pane opening, 300 high, pads by 300 and scrolls the item's bottom, 632, to
    // 844 - 300 - 50: 138.
    [AvaloniaFact]
    public void W2_On_Windows_A_Field_s_Focus_Waits_For_The_Pane_And_Reckons_With_Its_Height()
    {
        var pane = new FakePane();
        using var stage = new ShimStage(pane: pane, platform: "windows");
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(1500);
        Assert.Equal(0, stage.Content.KeyboardOffset);
        Assert.Equal(0, stage.Offset);

        pane.Set(InputPaneState.Open, new Rect(0, 544, 390, 300));
        stage.At(1501);
        Assert.Equal(300, stage.Content.KeyboardOffset);
        stage.At(2000);
        Assert.Equal(138, stage.Offset);
    }

    // With the touch keyboard already open, focus runs it at once with the pane's height, as a phone's next field does (I11).
    [AvaloniaFact]
    public void W3_On_Windows_A_Field_Focused_With_The_Pane_Open_Runs_At_Once()
    {
        var pane = new FakePane();
        using var stage = new ShimStage(pane: pane, platform: "windows");
        pane.Set(InputPaneState.Open, new Rect(0, 544, 390, 300));
        stage.At(1);
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(300, stage.Content.KeyboardOffset);
        stage.At(500);
        Assert.Equal(138, stage.Offset);
    }

    // Windows gives the keyboard's place on the screen, in the window's coordinates, so a window that ends above the bottom of the
    // screen is covered by part of it: a 400 high keyboard from 600, in a window 844 high, covers 244.
    [AvaloniaFact]
    public void W4_The_Keyboard_s_Height_Is_What_It_Covers_Of_The_Top_Level()
    {
        using (var keyboard = new KeyboardStage())
        {
            keyboard.Pane.Set(InputPaneState.Open, new Rect(0, 600, 390, 400));
            keyboard.Clock.At(0);
            keyboard.Pane.Set(InputPaneState.Open, new Rect(0, 900, 390, 300));
            keyboard.Clock.At(0);
            Assert.Equal(["show 244@0", "hide@0"], keyboard.Log);
        }

        // The item's bottom, 632, to 844 - 244 - 50: 82.
        var pane = new FakePane();
        using var stage = new ShimStage(pane: pane, platform: "windows");
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        pane.Set(InputPaneState.Open, new Rect(0, 600, 390, 400));
        stage.At(1);
        Assert.Equal(244, stage.Content.KeyboardOffset);
        stage.At(500);
        Assert.Equal(82, stage.Offset);
    }

    // A touch keyboard that doesn't reach the window is no keyboard: the item at 776 to 824 would otherwise scroll 30, to 50 above
    // the window's bottom.
    [AvaloniaFact]
    public void W5_On_Windows_A_Keyboard_Below_The_Window_Changes_Nothing()
    {
        var pane = new FakePane();
        using var stage = new ShimStage(pane: pane, platform: "windows", before: 15);
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        pane.Set(InputPaneState.Open, new Rect(0, 900, 390, 300));
        stage.At(1500);
        Assert.Equal(0, stage.Content.KeyboardOffset);
        Assert.Equal(0, stage.Offset);
    }

    // The keyboard closed and the field left, then focused again and the keyboard opened at the same height: it runs again, as the
    // last run's height belongs to the last focus.
    [AvaloniaFact]
    public void W7_On_Windows_Each_Focus_Runs_When_The_Pane_Opens_Even_At_The_Same_Height()
    {
        var pane = new FakePane();
        using var stage = new ShimStage(pane: pane, platform: "windows");
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        pane.Set(InputPaneState.Open, new Rect(0, 544, 390, 300));
        stage.At(500);
        Assert.Equal(138, stage.Offset);

        pane.Set(InputPaneState.Closed, default);
        stage.Other.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(1000);
        stage.Content.GetScrollElement()!.Offset = default;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, stage.Content.KeyboardOffset);

        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        pane.Set(InputPaneState.Open, new Rect(0, 544, 390, 300));
        stage.At(1001);
        Assert.Equal(300, stage.Content.KeyboardOffset);
        stage.At(1500);
        Assert.Equal(138, stage.Offset);
    }

    // On a phone the pane is there too, and focus still reckons with Ionic's estimate at once, as the keyboard comes with it.
    [AvaloniaFact]
    public void W6_On_A_Phone_Focus_Reckons_With_The_Estimate_Before_The_Pane_Opens()
    {
        using var stage = new ShimStage(pane: new FakePane());
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(290, stage.Content.KeyboardOffset);
        stage.At(500);
        Assert.Equal(128, stage.Offset);
    }

    // A Windows top level with no pane (a hosted one, or Windows before 10) hears no keyboard, and so does nothing, where a phone's
    // estimate would pad and scroll on every focus of a PC with a keyboard of its own.
    [AvaloniaFact]
    public void W8_On_Windows_With_No_Pane_Focus_Does_Nothing()
    {
        using var stage = new ShimStage(platform: "windows");
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(1500);
        Assert.Equal(0, stage.Content.KeyboardOffset);
        Assert.Equal(0, stage.Offset);
    }

    // A deviation: Ionic's padding stays until the field loses focus, as a phone's keyboard closes with it. Windows' touch keyboard
    // can be closed with the field still focused, or undocked, or end off the window: each takes the padding away 120 ms later, and
    // the next opening runs it again.
    [AvaloniaFact]
    public void W9_On_Windows_The_Padding_Goes_With_The_Keyboard_And_Comes_Back_With_It()
    {
        var pane = new FakePane();
        using var stage = new ShimStage(pane: pane, platform: "windows");
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        pane.Set(InputPaneState.Open, new Rect(0, 544, 390, 300));
        stage.At(500);
        Assert.Equal(300, stage.Content.KeyboardOffset);

        pane.Set(InputPaneState.Closed, default);
        stage.At(619);
        Assert.Equal(300, stage.Content.KeyboardOffset);
        stage.At(620);
        Assert.Equal(0, stage.Content.KeyboardOffset);
        Assert.True(stage.Native.IsFocused);

        stage.Content.GetScrollElement()!.Offset = default;
        Dispatcher.UIThread.RunJobs();
        pane.Set(InputPaneState.Open, new Rect(0, 544, 390, 300));
        stage.At(621);
        Assert.Equal(300, stage.Content.KeyboardOffset);
        stage.At(1100);
        Assert.Equal(138, stage.Offset);

        pane.Set(InputPaneState.Open, new Rect(0, 900, 390, 300));
        stage.At(1219);
        Assert.Equal(300, stage.Content.KeyboardOffset);
        stage.At(1220);
        Assert.Equal(0, stage.Content.KeyboardOffset);
    }

    // With the keyboard already open no show is coming, so a field without room yet doesn't wait a second for one: on the short
    // page the padding gives 88 (576 + 300 - 788), and it scrolls that far at once.
    [AvaloniaFact]
    public void W10_On_Windows_A_Field_Focused_With_The_Pane_Open_Does_Not_Wait()
    {
        var pane = new FakePane();
        using var stage = new ShimStage(after: 0, pane: pane, platform: "windows");
        pane.Set(InputPaneState.Open, new Rect(0, 544, 390, 300));
        stage.At(1);
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.At(500);
        Assert.Equal(88, stage.Offset);
    }

    // The window as it is when the keyboard opens, not at the focus: resized from 844 to 600, with a 200 keyboard from 400, the
    // item's bottom, 632, goes to 600 - 200 - 50: 282. With 844, it would be 82, under the keyboard.
    [AvaloniaFact]
    public void W11_On_Windows_The_Window_s_Height_Is_Read_When_The_Keyboard_Opens()
    {
        var pane = new FakePane();
        using var stage = new ShimStage(pane: pane, platform: "windows");
        stage.Native.Focus();
        Dispatcher.UIThread.RunJobs();
        stage.Scene.Window.Height = 600;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(600, stage.Scene.Window.ClientSize.Height);

        pane.Set(InputPaneState.Open, new Rect(0, 400, 390, 200));
        stage.At(1);
        Assert.Equal(200, stage.Content.KeyboardOffset);
        stage.At(500);
        Assert.Equal(282, stage.Offset);
    }

    // ── T: the text a page gets from html and body ──────────────────────────────────────────

    // Measured H: font-size 16px in ion-app, the browser's default; html's font-family and body's colours are Ionic's core.css.
    [AvaloniaFact]
    public void T1_Text_In_An_App_Is_16_In_Avonic_s_Font_And_Text_Colour_On_Its_Background() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "Text" };
        var outside = new TextBlock { Text = "Outside" };
        var app = new AvonicApp { Content = text };
        using var scene = new Scene(new StackPanel { Children = { app, outside } });

        Assert.Equal(16, text.FontSize);
        Assert.Equal(app.FindResource("Avonic.FontFamily"), text.FontFamily);
        Assert.Equal(((Avalonia.Media.ISolidColorBrush)app.FindResource("Avonic.TextColorBrush")!).Color,
            ((Avalonia.Media.ISolidColorBrush)text.Foreground!).Color);
        Assert.Equal(((Avalonia.Media.ISolidColorBrush)app.FindResource("Avonic.BackgroundColorBrush")!).Color,
            ((Avalonia.Media.ISolidColorBrush)app.Background!).Color);
        Assert.NotEqual(16, outside.FontSize);
        Assert.False(app.ClipToBounds);

        // Avonic's text colour is black, as Avalonia's default is, so a colour of its own shows the key is what's read.
        app.Resources["Avonic.TextColorBrush"] = Avalonia.Media.Brushes.Red;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Avalonia.Media.Colors.Red, ((Avalonia.Media.ISolidColorBrush)text.Foreground!).Color);
    });

    [AvaloniaFact]
    public void T1_An_App_s_Own_Style_Wins() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock { Text = "Text" };
        using var scene = Scene.Styled(new AvonicApp { Content = text }, Scene.AppStyle<AvonicApp>(TemplatedControl.FontSizeProperty, 20d));
        Assert.Equal(20, text.FontSize);
    });

    // ── A: the safe area ─────────────────────────────────────────────────────────────────────

    private sealed class FakeInsets
    {
        private EventHandler<SafeAreaChangedArgs>? _changed;

        public FakeInsets()
        {
            Insets = DispatchProxy.Create<IInsetsManager, Proxy>();
            ((Proxy)(object)Insets).Owner = this;
        }

        public IInsetsManager Insets { get; }

        public Thickness Padding { get; private set; }

        public void Set(Thickness padding)
        {
            Padding = padding;
            _changed?.Invoke(Insets, new SafeAreaChangedArgs(padding));
            Dispatcher.UIThread.RunJobs();
        }

        public class Proxy : DispatchProxy
        {
            public FakeInsets? Owner { get; set; }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                var owner = Owner!;
                switch (targetMethod?.Name)
                {
                    case "get_SafeAreaPadding":
                        return owner.Padding;
                    case "add_SafeAreaChanged":
                        owner._changed += (EventHandler<SafeAreaChangedArgs>)args![0]!;
                        return null;
                    case "remove_SafeAreaChanged":
                        owner._changed -= (EventHandler<SafeAreaChangedArgs>)args![0]!;
                        return null;
                    default:
                        return targetMethod?.ReturnType is { IsValueType: true } type && type != typeof(void) ? Activator.CreateInstance(type) : null;
                }
            }
        }
    }

    private sealed class SafeAreaStage : IDisposable
    {
        private readonly Func<TopLevel, IInsetsManager?> _previous = AppSafeArea.InsetsOf;
        private readonly Avalonia.Themes.Simple.SimpleTheme _theme = new();

        public SafeAreaStage(bool padded)
        {
            // A base theme, whose window has the overlay layer.
            Application.Current!.Styles.Insert(0, _theme);
            Fake.Set(new Thickness(0, 50.3, 0, 24));
            Root = new Panel();
            TopLevel.SetAutoSafeAreaPadding(Root, padded);
            App = new AvonicApp { Content = new Border() };
            Root.Children.Add(App);
            Window = new Window { Width = 390, Height = 844, Content = Root };
            var insets = Fake.Insets;
            var window = Window;
            var previous = _previous;
            AppSafeArea.InsetsOf = candidate => ReferenceEquals(candidate, window) ? insets : previous(candidate);
            Window.Show();
            Dispatcher.UIThread.RunJobs();
        }

        public FakeInsets Fake { get; } = new();

        public Panel Root { get; }

        public AvonicApp App { get; }

        public Window Window { get; }

        public double? Key(Control from, string side) => from.FindResource("Avonic.SafeArea" + side) as double?;

        public void Dispose()
        {
            Window.Close();
            Dispatcher.UIThread.RunJobs();
            AppSafeArea.InsetsOf = _previous;
            Application.Current!.Styles.Remove(_theme);
        }
    }

    // Measured on a Pixel 8 (docs/infra/android/COMPARISON.md): top 50.3, bottom 24 in portrait.
    [AvaloniaFact]
    public void A1_With_The_Root_Not_Padded_By_Avalonia_The_Keys_Are_The_Insets_For_The_App_And_Its_Overlays()
    {
        using var stage = new SafeAreaStage(padded: false);
        Assert.Equal(50.3, stage.Key(stage.App, "Top"));
        Assert.Equal(24, stage.Key(stage.App, "Bottom"));
        Assert.Equal(0, stage.Key(stage.App, "Left"));

        // What's in the window's overlay layer, outside the app, as an overlay is.
        var layer = Avalonia.Controls.Primitives.OverlayLayer.GetOverlayLayer(stage.Window)!;
        var inLayer = new Border();
        layer.Children.Add(inLayer);
        Assert.Equal(50.3, stage.Key(inLayer, "Top"));
        layer.Children.Remove(inLayer);

        stage.Fake.Set(new Thickness(50.3, 28.2, 0, 24));
        Assert.Equal(28.2, stage.Key(stage.App, "Top"));
        Assert.Equal(50.3, stage.Key(stage.App, "Left"));
    }

    [AvaloniaFact]
    public void A2_While_Avalonia_Pads_The_Root_The_Keys_Are_Left_To_The_Theme()
    {
        using var stage = new SafeAreaStage(padded: true);
        Assert.Null(stage.Key(stage.App, "Top"));

        TopLevel.SetAutoSafeAreaPadding(stage.Root, false);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(50.3, stage.Key(stage.App, "Top"));

        TopLevel.SetAutoSafeAreaPadding(stage.Root, true);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(stage.Key(stage.App, "Top"));
    }

    [AvaloniaFact]
    public void A1_A_Key_The_App_Sets_On_The_Top_Level_Itself_Wins()
    {
        using var stage = new SafeAreaStage(padded: false);
        stage.Window.Resources["Avonic.SafeAreaTop"] = 12d;
        Assert.Equal(12, stage.Key(stage.App, "Top"));
        Assert.Equal(24, stage.Key(stage.App, "Bottom"));

        stage.Root.Children.Remove(stage.App);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(12, stage.Key(stage.Root, "Top"));
    }

    [AvaloniaFact]
    public void A3_An_App_Taken_Out_Leaves_The_Keys_To_The_Theme()
    {
        using var stage = new SafeAreaStage(padded: false);
        stage.Root.Children.Remove(stage.App);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(stage.Key(stage.Root, "Top"));
        stage.Fake.Set(new Thickness(0, 10, 0, 0));
        Assert.Null(stage.Key(stage.Root, "Top"));
    }

    // Measured M6: after a touch, a scripted focus() keeps the ring setFocus gave; only focus to the body clears it.
    [AvaloniaFact]
    public void F5_On_A_Focusable_Surface_It_Stays_Through_Focus_Moved_By_Code()
    {
        using var stage = new FocusStage();
        stage.App.SetFocus(stage.Second);
        stage.Second.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(Shows(stage.Second));

        stage.First.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(Shows(stage.Second));

        stage.Second.Focus();
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
