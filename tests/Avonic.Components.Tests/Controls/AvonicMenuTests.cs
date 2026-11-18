using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Themes;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-menu</c>, md, on the test clock: B1 to B19 and B24 in docs/components/ion-menu/PLAN.md, from <c>menu.js</c>,
/// <c>utils/menu-controller</c>, <c>menu.md.css</c> and the measurements in <c>ref/measurements.txt</c>. A 390 x 844 window holds a
/// grid with the menu, then the main content, as Ionic's examples put them. Frames come every 16 ms, so an open played at 0 shows
/// local time t at the first frame at or after 16 + t: 144 at 160, and it ends at the frame at 320.
/// </summary>
public class AvonicMenuTests
{
    internal const double Done = 320;

    private static readonly CubicBezierEasing OpenEasing = new(0, 0, 0.2, 1);
    private static readonly CubicBezierEasing CloseEasing = new(0.4, 0, 0.6, 1);

    /// <summary>A grid with a menu and the main content, named <c>Main</c>, in a window with <c>SimpleTheme</c> and <c>AvonicTheme</c>.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Avalonia.Themes.Simple.SimpleTheme _simple = new();
        private readonly AvonicTheme _theme = new();

        public Stage(Action<AvonicMenu>? set = null, double width = 390, double height = 844, bool attach = true, TestClock? clock = null)
        {
            Application.Current!.Styles.Insert(0, _simple);
            Application.Current.Styles.Add(_theme);
            Clock = clock ?? new TestClock();
            MainButton = new Button { Name = "MainButton", Content = "Main", Height = 40, VerticalAlignment = VerticalAlignment.Top };
            Main = new Border { Name = "Main", Background = Brushes.White, Child = MainButton };
            A = new Button { Name = "A", Content = "A", Height = 40 };
            B = new Button { Name = "B", Content = "B", Height = 40 };
            C = new Button { Name = "C", Content = "C", Height = 40 };
            Menu = new AvonicMenu { ContentId = "Main", Content = new StackPanel { Children = { A, B, C } } };
            set?.Invoke(Menu);
            Root = new Grid();
            Scope = new NameScope();
            NameScope.SetNameScope(Root, Scope);
            Scope.Register("Main", Main);
            Root.Children.Add(Menu);
            Root.Children.Add(Main);
            Watch(Menu);
            Scene = new Scene(attach ? Root : new Panel(), width, height);
        }

        public TestClock Clock { get; }

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public Grid Root { get; }

        public NameScope Scope { get; }

        public AvonicMenu Menu { get; }

        public Border Main { get; }

        public Button MainButton { get; }

        public Button A { get; }

        public Button B { get; }

        public Button C { get; }

        public List<string> Log { get; } = [];

        public IInputElement? Focused => Window.FocusManager!.GetFocusedElement();

        public Border Container
        {
            get
            {
                Menu.ApplyTemplate();
                return Menu.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Container");
            }
        }

        public AvonicBackdrop Backdrop => Menu.GetVisualDescendants().OfType<AvonicBackdrop>().Single();

        public Border ContentShadow => Menu.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_ContentShadow");

        public void Attach() => ((Panel)Window.Content!).Children.Add(Root);

        public AvonicMenu Watch(AvonicMenu menu, string name = "")
        {
            string Now() => $"@{Clock.Now.TotalMilliseconds}";
            menu.IonWillOpen += (_, _) => Log.Add($"{name}ionWillOpen{Now()}");
            menu.IonDidOpen += (_, _) => Log.Add($"{name}ionDidOpen{Now()}");
            menu.IonWillClose += (_, e) => Log.Add($"{name}ionWillClose({e.Role}){Now()}");
            menu.IonDidClose += (_, e) => Log.Add($"{name}ionDidClose({e.Role}){Now()}");
            return menu;
        }

        public void At(double ms)
        {
            while (Clock.Now.TotalMilliseconds < ms)
            {
                Clock.At(Math.Min(Math.Floor(Clock.Now.TotalMilliseconds) + 1, ms));
                Settle();
            }

            Settle();
        }

        public void After(double ms) => At(Clock.Now.TotalMilliseconds + ms);

        public static void Settle() => Dispatcher.UIThread.RunJobs();

        public void Render()
        {
            for (var i = 0; i < 2; i++)
            {
                Settle();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            Settle();
        }

        /// <summary>Opens the menu at once, runs to the frame that ends its playback, and clears the log.</summary>
        public void Opened()
        {
            var opened = Menu.Open(false);
            Settle();
            Assert.True(opened.IsCompleted && opened.Result);
            After(16);
            Log.Clear();
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
            Application.Current.Styles.Remove(_simple);
        }
    }

    internal static double TranslateX(Visual element) => element.RenderTransform?.Value.M31 ?? 0;

    internal static void Near(double expected, double actual, double tolerance = 1e-3) =>
        Assert.True(Math.Abs(expected - actual) <= tolerance, $"expected {expected}, was {actual}");

    /// <summary>Where <paramref name="element"/>'s left edge is in the window, its transform included.</summary>
    internal static double WindowX(Visual element, Window window) => element.TranslatePoint(default, window)!.Value.X;

    // ── B1: open ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Closed_It_Is_Hidden_And_Open_Shows_It_With_Will_Then_Did()
    {
        using var stage = new Stage();
        Assert.False(stage.Menu.IsVisible);
        Assert.False(stage.Menu.IsOpen());

        var opened = stage.Menu.Open();
        Assert.True(stage.Menu.IsVisible);
        Assert.Equal(["ionWillOpen@0"], stage.Log);

        stage.At(Done - 1);
        Assert.False(opened.IsCompleted);
        Assert.False(stage.Menu.IsOpen());

        stage.At(Done);
        Assert.Equal(["ionWillOpen@0", "ionDidOpen@320"], stage.Log);
        Assert.True(opened.IsCompleted && opened.Result);
        Assert.True(stage.Menu.IsOpen());
        Assert.True(stage.Menu.IsVisible);
    }

    [AvaloniaFact]
    public void B1_Close_Hides_It_Once_Its_Animation_Has_Ended()
    {
        using var stage = new Stage();
        stage.Opened();
        stage.At(1008);

        var closed = stage.Menu.Close();
        Assert.Equal(["ionWillClose()@1008"], stage.Log);
        stage.At(1008 + Done - 1);
        Assert.True(stage.Menu.IsVisible);
        stage.At(1008 + Done);
        Assert.Equal(["ionWillClose()@1008", "ionDidClose()@1328"], stage.Log);
        Assert.True(closed.IsCompleted && closed.Result);
        Assert.False(stage.Menu.IsVisible);
        Assert.False(stage.Menu.IsOpen());
    }

    // ── B2: the overlay's animation ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Overlay_The_Container_Slides_In_From_Its_Width_And_8_And_The_Backdrop_Darkens_To_A_Quarter()
    {
        using var stage = new Stage();
        _ = stage.Menu.Open();
        Assert.Equal(304, stage.Container.Bounds.Width);
        Near(-312, TranslateX(stage.Container));
        Near(0.01, stage.Backdrop.Opacity);
        Assert.True(stage.Backdrop.IsVisible);

        stage.At(160);
        var eased = OpenEasing.Ease(144 / 300d);
        Near(-312 * (1 - eased), TranslateX(stage.Container));
        Near(0.01 + (0.25 - 0.01) * eased, stage.Backdrop.Opacity);

        stage.At(Done);
        Near(0, TranslateX(stage.Container));
        Near(0.25, stage.Backdrop.Opacity);
        Assert.Equal(0, WindowX(stage.Container, stage.Window));
    }

    [AvaloniaFact]
    public void B2_Closing_Plays_It_Back_On_The_Reverse_Easing()
    {
        using var stage = new Stage();
        stage.Opened();
        stage.At(1008);

        // Played at 1008, a frame's time: its frames are 1024 + t.
        _ = stage.Menu.Close();
        stage.At(1024 + 96);

        // Reversed: 96 ms back is the easing at 1 - 96 / 300 of the forward animation.
        var eased = CloseEasing.Ease(1 - 96 / 300d);
        Near(-312 * (1 - eased), TranslateX(stage.Container));
        Near(0.01 + (0.25 - 0.01) * eased, stage.Backdrop.Opacity);

        stage.At(1008 + Done);
        Assert.False(stage.Menu.IsVisible);
        Assert.False(stage.Backdrop.IsVisible);
    }

    [AvaloniaFact]
    public void B2_With_Animated_Off_In_The_Config_It_Opens_At_The_Next_Frame()
    {
        using var stage = new Stage();
        AvonicConfig.Animated = false;
        try
        {
            var opened = stage.Menu.Open();
            Near(0, TranslateX(stage.Container));
            stage.At(16);
            Assert.True(opened.IsCompleted && opened.Result);
            Assert.Equal(["ionWillOpen@0", "ionDidOpen@16"], stage.Log);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    }

    [AvaloniaFact]
    public void B2_Open_Not_Animated_Is_Open_At_Once()
    {
        using var stage = new Stage();
        var opened = stage.Menu.Open(false);
        Stage.Settle();
        Assert.True(opened.IsCompleted && opened.Result);
        Assert.Equal(["ionWillOpen@0", "ionDidOpen@0"], stage.Log);
        Near(0, TranslateX(stage.Container));
        Near(0.25, stage.Backdrop.Opacity);
    }

    // ── B3: push and reveal ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Push_Moves_The_Menu_In_And_The_Content_Along_And_Shows_No_Backdrop()
    {
        using var stage = new Stage(m => m.Type = MenuType.Push);
        _ = stage.Menu.Open();
        Near(-304, TranslateX(stage.Container));
        Near(0, TranslateX(stage.Main));
        Assert.False(stage.Backdrop.IsVisible);

        stage.At(160);
        var eased = OpenEasing.Ease(144 / 300d);
        Near(-304 * (1 - eased), TranslateX(stage.Container));
        Near(304 * eased, TranslateX(stage.Main));

        stage.At(Done);
        Near(0, TranslateX(stage.Container));
        Near(304, TranslateX(stage.Main));
        Assert.Equal(304, WindowX(stage.Main, stage.Window));

        _ = stage.Menu.Close(false);
        Stage.Settle();
        Assert.Null(stage.Main.RenderTransform);
    }

    [AvaloniaFact]
    public void B3_Reveal_Moves_Only_The_Content_And_The_Container_Stays_Put()
    {
        using var stage = new Stage(m => m.Type = MenuType.Reveal);
        _ = stage.Menu.Open();
        Near(0, TranslateX(stage.Container));
        stage.At(160);
        Near(0, TranslateX(stage.Container));
        Near(304 * OpenEasing.Ease(144 / 300d), TranslateX(stage.Main));
        stage.At(Done);
        Near(304, TranslateX(stage.Main));
        Assert.False(stage.Backdrop.IsVisible);
    }

    [AvaloniaFact]
    public void B3_Push_And_Reveal_Draw_The_Contents_Shadow_On_Its_Box_And_Move_It_With_The_Content()
    {
        using var stage = new Stage(m => m.Type = MenuType.Reveal);
        stage.Opened();
        stage.Render();
        var shadow = stage.ContentShadow;
        Assert.True(shadow.IsVisible);
        Assert.Equal(new Size(390, 844), shadow.Bounds.Size);
        Assert.Equal(304, WindowX(shadow, stage.Window));
        Assert.Equal(Avonic.Components.Behaviors.CssBoxShadow.Parse("4px 0px 16px rgba(0, 0, 0, 0.18)"), shadow.BoxShadow);

        stage.Menu.Type = MenuType.Overlay;
        Assert.Equal(default, shadow.BoxShadow);
    }

    // ── B4: the side ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_The_End_Side_Is_At_The_Right_And_Comes_From_It()
    {
        using var stage = new Stage(m => m.Side = MenuSide.End);
        _ = stage.Menu.Open();
        Near(312, TranslateX(stage.Container));
        stage.At(Done);
        Assert.Equal(86, WindowX(stage.Container, stage.Window));
    }

    [AvaloniaFact]
    public void B4_In_Right_To_Left_The_Start_Side_Is_At_The_Right_And_Its_Movement_Is_Physical()
    {
        using var stage = new Stage();
        stage.Window.FlowDirection = FlowDirection.RightToLeft;
        _ = stage.Menu.Open();
        Stage.Settle();

        // From off the right edge, as Ionic's translateX(312px), physically.
        Near(390 + 8, WindowX(stage.Container, stage.Window) - 0, 1);
        stage.At(Done);
        Near(86, PhysicalLeft(stage.Container, stage.Window), 0.5);
    }

    [AvaloniaFact]
    public void B4_In_Right_To_Left_Push_Moves_The_Content_Left()
    {
        using var stage = new Stage(m => m.Type = MenuType.Push);
        stage.Window.FlowDirection = FlowDirection.RightToLeft;
        stage.Opened();
        Near(-304, PhysicalLeft(stage.Main, stage.Window), 0.5);
    }

    /// <summary>The physical left edge of <paramref name="element"/> in the window, mirrored or not.</summary>
    private static double PhysicalLeft(Visual element, Window window)
    {
        var m = element.TransformToVisual(window)!.Value;
        return new Rect(element.Bounds.Size).TransformToAABB(m).X;
    }

    // ── B5: the width ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_The_Container_Is_304_Wide_And_The_Hosts_Height()
    {
        using var stage = new Stage();
        stage.Opened();
        Assert.Equal(new Rect(0, 0, 304, 844), stage.Container.Bounds);
    }

    [AvaloniaFact]
    public void B5_Up_To_340_Wide_It_Is_264_Wide_Whatever_The_Width_Key()
    {
        using var stage = new Stage(width: 320, height: 640);
        stage.Menu.Resources["Avonic.Menu.Width"] = 200d;
        stage.Opened();
        Assert.Equal(264, stage.Container.Bounds.Width);
        Assert.True(stage.Menu.Classes.Contains(AvonicMenu.SmallWidth) || ((IPseudoClasses)stage.Menu.Classes).Contains(AvonicMenu.SmallWidth));
    }

    [AvaloniaFact]
    public void B5_The_Size_Keys_Size_The_Container()
    {
        using var stage = new Stage();
        stage.Menu.Resources["Avonic.Menu.Width"] = "50%";
        stage.Menu.Resources["Avonic.Menu.MaxWidth"] = "180px";
        stage.Menu.Resources["Avonic.Menu.Height"] = "300px";
        stage.Opened();
        Assert.Equal(new Rect(0, 0, 180, 300), stage.Container.Bounds);

        // The animation is made from the width it has.
        _ = stage.Menu.Close(false);
        _ = stage.Menu.Open();
        Near(-188, TranslateX(stage.Container));
    }

    // ── B6: roles ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Close_Carries_Its_Role_To_Both_Events()
    {
        using var stage = new Stage();
        stage.Opened();
        _ = stage.Menu.Close(false, "cancel");
        Stage.Settle();
        Assert.Equal(["ionWillClose(cancel)@16", "ionDidClose(cancel)@16"], stage.Log);
    }

    [AvaloniaFact]
    public void B6_The_Events_Bubble()
    {
        using var stage = new Stage();
        var seen = new List<string>();
        stage.Root.AddHandler(AvonicMenu.IonWillOpenEvent, (_, _) => seen.Add("willOpen"));
        stage.Root.AddHandler(AvonicMenu.IonDidOpenEvent, (_, _) => seen.Add("didOpen"));
        stage.Root.AddHandler(AvonicMenu.IonWillCloseEvent, (_, e) => seen.Add($"willClose {e.Role}"));
        stage.Root.AddHandler(AvonicMenu.IonDidCloseEvent, (_, e) => seen.Add($"didClose {e.Role}"));
        stage.Opened();
        _ = stage.Menu.Close(false, "x");
        Stage.Settle();
        Assert.Equal(["willOpen", "didOpen", "willClose x", "didClose x"], seen);
    }

    // ── B7: one at a time ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_Opening_A_Second_Menu_Closes_The_First_At_Once_With_No_Role()
    {
        using var stage = new Stage(m => m.MenuId = "first");
        var second = stage.Watch(new AvonicMenu { MenuId = "second", ContentId = "Main", Side = MenuSide.End }, "second.");
        stage.Root.Children.Insert(1, second);
        Stage.Settle();
        stage.Opened();

        var opened = second.Open();
        Assert.Equal(["ionWillClose()@16", "ionDidClose()@16", "second.ionWillOpen@16"], stage.Log);
        Assert.False(stage.Menu.IsOpen());
        Assert.False(stage.Menu.IsVisible);
        stage.After(Done);
        Assert.True(opened.IsCompleted && opened.Result);
        Assert.True(second.IsOpen());
        Assert.Same(second, MenuController.GetOpen());
    }

    [AvaloniaFact]
    public void B7_While_A_Menu_Is_Animating_Nothing_Opens_Or_Closes()
    {
        using var stage = new Stage();
        var second = new AvonicMenu { MenuId = "second", ContentId = "Main", Side = MenuSide.End };
        stage.Root.Children.Insert(1, second);
        Stage.Settle();

        _ = stage.Menu.Open();
        Assert.True(MenuController.IsAnimating());
        var close = stage.Menu.Close();
        var toggle = stage.Menu.Toggle();
        var other = second.Open();
        Assert.True(close.IsCompleted && !close.Result);
        Assert.True(toggle.IsCompleted && !toggle.Result);
        Assert.True(other.IsCompleted && !other.Result);
        stage.At(Done);
        Assert.False(MenuController.IsAnimating());
        Assert.True(stage.Menu.IsOpen());
        Assert.False(second.IsOpen());
    }

    [AvaloniaFact]
    public void B7_An_Open_Menus_Open_And_A_Closed_Ones_Close_Do_Nothing()
    {
        using var stage = new Stage();
        var close = stage.Menu.Close();
        Assert.True(close.IsCompleted && !close.Result);
        stage.Opened();
        var open = stage.Menu.Open();
        Assert.True(open.IsCompleted && !open.Result);
        Assert.Empty(stage.Log);

        var toggled = stage.Menu.Toggle(false);
        Assert.True(toggled.IsCompleted && toggled.Result);
        Assert.False(stage.Menu.IsOpen());
    }

    // ── B8: disabled ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_Disabling_An_Open_Menu_Closes_It_At_Once_With_The_Gesture_Role_And_No_Will_Event()
    {
        using var stage = new Stage();
        stage.Opened();
        stage.Menu.IsEnabled = false;
        Assert.Equal(["ionDidClose(gesture)@16"], stage.Log);
        Assert.False(stage.Menu.IsOpen());
        Assert.False(stage.Menu.IsVisible);
        Assert.False(stage.Menu.IsActive());

        var open = stage.Menu.Open();
        Assert.True(open.IsCompleted && !open.Result);
    }

    [AvaloniaFact]
    public void B8_A_Closed_Menu_Disabled_Still_Raises_Did_Close_As_Ionics_Does()
    {
        using var stage = new Stage();
        stage.Menu.IsEnabled = false;
        Assert.Equal(["ionDidClose(gesture)@0"], stage.Log);
    }

    [AvaloniaFact]
    public void B8_A_Menu_Disabled_At_Load_Raises_Did_Close_At_Connect_And_At_Load()
    {
        using var stage = new Stage(m => m.IsEnabled = false);
        Assert.Equal(["ionDidClose(gesture)@0", "ionDidClose(gesture)@0"], stage.Log);
    }

    [AvaloniaFact]
    public void B8_Disabled_During_Its_Opening_It_Closes_And_Open_Returns_False()
    {
        using var stage = new Stage();
        var opened = stage.Menu.Open();
        stage.At(160);
        stage.Menu.IsEnabled = false;
        Stage.Settle();
        Assert.True(opened.IsCompleted && !opened.Result);
        Assert.Equal(["ionWillOpen@0", "ionDidClose(gesture)@160"], stage.Log);
        Assert.False(stage.Menu.IsVisible);
        stage.At(1000);
        Assert.DoesNotContain(stage.Log, l => l.StartsWith("ionDidOpen", StringComparison.Ordinal));
    }

    // ── B9: a click outside ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_A_Click_Outside_The_Container_Closes_It_With_The_Backdrop_Role()
    {
        using var stage = new Stage();
        stage.Opened();
        stage.At(1000);
        stage.Click(new Point(360, 400));
        Assert.Equal(["ionWillClose(backdrop)@1000"], stage.Log);
        stage.After(Done);
        Assert.False(stage.Menu.IsOpen());
    }

    [AvaloniaFact]
    public void B9_A_Click_In_The_Container_Does_Not()
    {
        using var stage = new Stage();
        stage.Opened();
        stage.At(1000);
        stage.Click(new Point(150, 400));
        Assert.Equal(Array.Empty<string>(), stage.Log);
        stage.Click(new Point(150, 20));
        Assert.Equal(Array.Empty<string>(), stage.Log);
        Assert.True(stage.Menu.IsOpen());
    }

    [AvaloniaFact]
    public void B9_Push_A_Click_On_The_Moved_Content_Closes_It_As_The_Content_Takes_No_Input()
    {
        using var stage = new Stage(m => m.Type = MenuType.Push);
        var clicked = 0;
        stage.MainButton.Click += (_, _) => clicked++;
        stage.Opened();
        stage.At(1000);

        // The main button is at 304 now, under the press.
        stage.Click(new Point(320, 20));
        Assert.Equal(0, clicked);
        Assert.Equal(["ionWillClose(backdrop)@1000"], stage.Log);
    }

    [AvaloniaFact]
    public void B9_A_Click_Within_100_ms_Of_A_Swipes_End_Does_Nothing()
    {
        using var stage = new Stage();
        stage.Opened();
        stage.At(1000);

        // A slow mouse drag on the backdrop, 12 px left, which goes back open: captured at 10 px, released at 1208, and back
        // open in a few frames.
        stage.Render();
        stage.Window.MouseDown(new Point(360, 400), MouseButton.Left);
        for (var i = 1; i <= 12; i++)
        {
            stage.At(1000 + (16 * i));
            stage.Window.MouseMove(new Point(360 - i, 400));
            Stage.Settle();
        }

        stage.At(1208);
        stage.Window.MouseUp(new Point(348, 400), MouseButton.Left);
        Stage.Settle();
        stage.At(1208 + 64);
        Assert.Equal("ionWillClose(gesture)@1160", stage.Log[0]);
        Assert.StartsWith("ionDidOpen@", stage.Log[1]);
        Assert.True(stage.Menu.IsOpen());
        stage.Log.Clear();

        // A click 80 ms after the release: nothing. 112 ms after: it closes.
        stage.At(1208 + 80);
        stage.Click(new Point(360, 600));
        Assert.Empty(stage.Log);
        stage.At(1208 + 112);
        stage.Click(new Point(360, 600));
        Assert.Equal(["ionWillClose(backdrop)@1320"], stage.Log);
    }

    // ── B10: the content while open ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_The_Content_Takes_No_Input_And_Is_Offscreen_To_Automation_While_It_Is_Shown()
    {
        using var stage = new Stage();
        var peer = ControlAutomationPeer.CreatePeerForElement(stage.Main);
        Assert.True(stage.Main.IsHitTestVisible);
        Assert.False(peer.IsOffscreen());

        _ = stage.Menu.Open();
        Assert.False(stage.Main.IsHitTestVisible);
        Assert.True(peer.IsOffscreen());
        stage.At(Done);
        _ = stage.Menu.Close();
        stage.After(96);
        Assert.False(stage.Main.IsHitTestVisible);
        stage.After(Done);
        Assert.True(stage.Main.IsHitTestVisible);
        Assert.False(peer.IsOffscreen());
    }

    // ── B11: Escape ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Escape_On_The_Menu_Closes_It_With_The_Backdrop_Role()
    {
        using var stage = new Stage();
        stage.Opened();
        Assert.Same(stage.Menu, stage.Focused);
        stage.Key(PhysicalKey.Escape);
        Assert.Equal(["ionWillClose(backdrop)@16"], stage.Log);
    }

    // ── B12: focus ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_Opening_Clears_The_Focus_And_The_Menu_Takes_It_Once_Open()
    {
        using var stage = new Stage();
        stage.MainButton.Focus();
        _ = stage.Menu.Open();
        Assert.Null(stage.Focused);
        stage.At(Done);
        Assert.Same(stage.Menu, stage.Focused);
    }

    [AvaloniaFact]
    public void B12_Focus_Leaving_The_Menu_Lands_On_Its_Last_Element_As_Ionics_Trap_Does()
    {
        using var stage = new Stage();
        stage.Opened();
        stage.Key(PhysicalKey.Tab);
        Assert.Same(stage.A, stage.Focused);
        stage.Key(PhysicalKey.Tab);
        stage.Key(PhysicalKey.Tab);
        Assert.Same(stage.C, stage.Focused);

        // Measured: past the last, Tab stays on it; focus given to the content goes to it too.
        stage.Key(PhysicalKey.Tab);
        Assert.Same(stage.C, stage.Focused);
        stage.MainButton.Focus();
        Stage.Settle();
        Assert.Same(stage.C, stage.Focused);
    }

    [AvaloniaFact]
    public void B12_An_Overlay_On_Top_Keeps_Its_Own_Focus()
    {
        // handleFocus: the top presented overlay doesn't hold the menu, so the menu's trap leaves focus in it.
        using var stage = new Stage();
        stage.Opened();
        var inModal = new Button { Content = "In modal", Height = 40 };
        var modal = new AvonicModal { Content = new StackPanel { Children = { inModal } } };
        _ = modal.Present(stage.Window);
        stage.After(800);
        inModal.Focus();
        Stage.Settle();
        Assert.Same(inModal, stage.Focused);
    }

    [AvaloniaFact]
    public void B12_After_A_Close_The_Trap_Is_Gone_And_Focus_Is_Not_Given_Back()
    {
        using var stage = new Stage();
        stage.MainButton.Focus();
        stage.Opened();
        _ = stage.Menu.Close(false);
        Stage.Settle();
        Assert.Null(stage.Focused);
        stage.MainButton.Focus();
        Assert.Same(stage.MainButton, stage.Focused);
    }

    // ── B13: back ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_Without_An_App_Back_Closes_An_Open_Menu()
    {
        using var stage = new Stage();
        stage.Opened();
        var args = new Avalonia.Interactivity.RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Window.RaiseEvent(args);
        Assert.True(args.Handled);
        Assert.Equal(["ionWillClose()@16"], stage.Log);

        stage.After(Done);
        var again = new Avalonia.Interactivity.RoutedEventArgs(TopLevel.BackRequestedEvent);
        stage.Window.RaiseEvent(again);
        Assert.False(again.Handled);
    }

    [AvaloniaFact]
    public void B13_In_An_App_An_Open_Menu_Registers_At_99_To_Close()
    {
        using var stage = new Stage();
        stage.Opened();
        var args = new BackButtonEventArgs(HardwareBackButton.IonBackButtonEvent, stage.Window);
        stage.Window.RaiseEvent(args);
        var entry = Assert.Single(args.Handlers);
        Assert.Equal(99, entry.Priority);
        var task = entry.Handler(() => { });
        Assert.NotNull(task);
        Assert.Equal(["ionWillClose()@16"], stage.Log);
        stage.After(Done);
        Assert.True(task!.IsCompleted);
    }

    [AvaloniaFact]
    public void B13_A_Closed_Menu_Registers_Nothing()
    {
        using var stage = new Stage();
        var args = new BackButtonEventArgs(HardwareBackButton.IonBackButtonEvent, stage.Window);
        stage.Window.RaiseEvent(args);
        Assert.Empty(args.Handlers);
    }

    // ── B16: type ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B16_A_Type_Left_Null_Is_The_Configs_Then_Overlay_At_The_First_Attach()
    {
        using (var stage = new Stage())
        {
            Assert.Equal(MenuType.Overlay, stage.Menu.Type);
        }

        AvonicConfig.MenuType = MenuType.Reveal;
        try
        {
            using var stage = new Stage();
            Assert.Equal(MenuType.Reveal, stage.Menu.Type);
        }
        finally
        {
            AvonicConfig.MenuType = null;
        }

        AvonicConfig.MenuType = MenuType.Reveal;
        try
        {
            using var stage = new Stage(m => m.Type = MenuType.Push);
            Assert.Equal(MenuType.Push, stage.Menu.Type);
        }
        finally
        {
            AvonicConfig.MenuType = null;
        }
    }

    [AvaloniaFact]
    public void B16_A_Change_Of_Type_Makes_The_Next_Animation_The_New_Types()
    {
        using var stage = new Stage();
        stage.Opened();
        _ = stage.Menu.Close(false);
        Stage.Settle();
        stage.Menu.Type = MenuType.Reveal;
        Assert.Null(stage.Menu.CurrentAnimation);
        _ = stage.Menu.Open();
        stage.At(1000);
        Near(304, TranslateX(stage.Main));
        Near(0, TranslateX(stage.Container));
    }

    // ── B17: detached ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_Detached_While_Open_It_Closes_At_Once_And_Leaves_The_Controller()
    {
        using var stage = new Stage(m => m.Type = MenuType.Push);
        stage.Opened();
        Assert.Contains(stage.Menu, MenuController.GetMenus());
        stage.Root.Children.Remove(stage.Menu);
        Stage.Settle();
        Assert.False(stage.Menu.IsOpen());
        Assert.True(stage.Main.IsHitTestVisible);
        Assert.Null(stage.Main.RenderTransform);
        Assert.DoesNotContain(stage.Menu, MenuController.GetMenus());
    }

    // ── B18: no content ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_Without_Its_Content_It_Logs_An_Error_And_Is_Not_Registered_But_Still_Opens()
    {
        var sink = new ErrorSink();
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            using var stage = new Stage(m => m.ContentId = "Nothing");
            Assert.Contains(sink.Messages, m => m.Contains("Must have a \"content\" element"));
            Assert.DoesNotContain(stage.Menu, MenuController.GetMenus());
            var opened = stage.Menu.Open();
            stage.At(Done);
            Assert.True(opened.IsCompleted && opened.Result);
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    [AvaloniaFact]
    public void B20_A_Button_Whose_Menu_Has_No_Animation_Logs_The_Error()
    {
        var sink = new ErrorSink();
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            using var stage = new Stage();
            var button = new AvonicMenuButton { VerticalAlignment = VerticalAlignment.Bottom };
            stage.Root.Children.Add(button);
            Stage.Settle();
            stage.Menu.Type = null;
            ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(button)).Invoke();
            Stage.Settle();
            Assert.Contains(sink.Messages, m => m.Contains("couldn't toggle", StringComparison.Ordinal));
            Assert.False(MenuController.IsAnimating());
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    private sealed class ErrorSink : Avalonia.Logging.ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(Avalonia.Logging.LogEventLevel level, string area) => level >= Avalonia.Logging.LogEventLevel.Warning;

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate) => Messages.Add(messageTemplate);

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Messages.Add(messageTemplate);
    }

    // ── B19: the controller ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_Get_Finds_By_Side_Enabled_First_By_Id_Or_The_First_Enabled()
    {
        using var stage = new Stage(m => m.MenuId = "one");
        var two = new AvonicMenu { MenuId = "two", ContentId = "Main" };
        var end = new AvonicMenu { MenuId = "end", ContentId = "Main", Side = MenuSide.End };
        stage.Root.Children.Insert(1, two);
        stage.Root.Children.Insert(2, end);
        Stage.Settle();

        Assert.Same(stage.Menu, MenuController.Get());
        Assert.Same(stage.Menu, MenuController.Get("start"));
        Assert.Same(end, MenuController.Get("end"));
        Assert.Same(two, MenuController.Get("two"));
        Assert.Null(MenuController.Get("three"));

        stage.Menu.IsEnabled = false;
        Assert.Same(two, MenuController.Get());
        Assert.Same(two, MenuController.Get("start"));
        Assert.False(MenuController.IsEnabled("one"));
        Assert.True(MenuController.IsEnabled("two"));

        // None enabled on a side: the first on it.
        end.IsEnabled = false;
        Assert.Same(end, MenuController.Get("end"));
        Assert.Equal([stage.Menu, two, end], MenuController.GetMenus());
    }

    [AvaloniaFact]
    public void B19_Open_Close_Toggle_Enable_And_SwipeGesture_By_Name()
    {
        using var stage = new Stage(m => m.MenuId = "one");
        var opened = MenuController.Open("one");
        Assert.True(MenuController.IsAnimating());
        stage.At(Done);
        Assert.True(opened.IsCompleted && opened.Result);
        Assert.True(MenuController.IsOpen());
        Assert.True(MenuController.IsOpen("one"));
        Assert.Same(stage.Menu, MenuController.GetOpen());

        var closed = MenuController.Close();
        stage.After(Done + 16);
        Assert.True(closed.IsCompleted && closed.Result);
        Assert.False(MenuController.IsOpen());

        var toggled = MenuController.Toggle("one");
        stage.After(Done + 16);
        Assert.True(toggled.IsCompleted && toggled.Result);
        Assert.True(stage.Menu.IsOpen());

        Assert.Same(stage.Menu, MenuController.SwipeGesture(false, "one"));
        Assert.False(stage.Menu.MenuSwipeGesture);
        Assert.Same(stage.Menu, MenuController.Enable(false, "one"));
        Assert.False(stage.Menu.IsEnabled);
        Assert.False(stage.Menu.IsOpen());
        Assert.Null(MenuController.Enable(true, "nothing"));
        var nothing = MenuController.Open("nothing");
        Assert.True(nothing.IsCompleted && !nothing.Result);
    }

    [AvaloniaFact]
    public void B19_RegisterAnimation_Replaces_A_Types_Animation()
    {
        using var stage = new Stage(m => m.Type = MenuType.Push);
        MenuAnimationContext? given = null;
        MenuController.RegisterAnimation(MenuType.Push, menu =>
        {
            given = menu;
            return AvonicAnimation.Create().Duration(100).AddElement(menu.Container).FromTo("opacity", 0.5, 1);
        });
        try
        {
            _ = stage.Menu.Open();
            Assert.NotNull(given);
            Assert.Same(stage.Menu, given!.Menu);
            Assert.Same(stage.Main, given.Content);
            Assert.Equal(304, given.Width);
            Assert.False(given.IsEndSide);
            Near(0.5, stage.Container.Opacity);
            stage.At(128);
            Assert.True(stage.Menu.IsOpen());
        }
        finally
        {
            MenuController.RegisterAnimation(MenuType.Push, MenuAnimations.Push);
        }
    }

    // ── B24: automation ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B24_It_Is_A_Navigation_Landmark_Named_Menu_Unless_The_App_Names_It()
    {
        using var stage = new Stage();
        var peer = ControlAutomationPeer.CreatePeerForElement(stage.Menu);
        Assert.Equal(AutomationLandmarkType.Navigation, peer.GetLandmarkType());
        Assert.Equal("menu", peer.GetName());
        AutomationProperties.SetName(stage.Menu, "Main menu");
        Assert.Equal("Main menu", peer.GetName());
    }

    // ── B14, B15: the swipe ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// A finger from <paramref name="x"/> to <paramref name="to"/> at 400 high, <paramref name="step"/> px every 16 ms from 1000, each
    /// move's frame run before the next; then lifted 16 ms after the last. Returns the container's <c>translateX</c> after each
    /// move's frame.
    /// </summary>
    private static List<double> Swipe(Stage stage, double x, double to, double step, bool lift = true)
    {
        stage.At(1000);
        var finger = new Gestures.GestureTests.Finger(stage.Scene, stage.Clock).Down(x, 400, 1000);
        var seen = new List<double>();
        var at = 1000d;
        while (Math.Abs(to - x) > 1e-9)
        {
            x += Math.Sign(to - x) * Math.Min(step, Math.Abs(to - x));
            at += 16;
            finger.Move(x, 400, at);
            stage.At(at + 16);
            seen.Add(TranslateX(stage.Container));
        }

        if (lift)
        {
            finger.Up(x, 400, at + 16);
            Stage.Settle();
        }

        return seen;
    }

    [AvaloniaFact]
    public void B14_A_Swipe_From_The_Edge_Opens_It_Following_The_Finger_Linearly()
    {
        using var stage = new Stage();
        var seen = Swipe(stage, 10, 210, 8);

        // Captured at 26 (16 px, past the threshold of 10), where the swipe starts again; then each move's frame shows
        // -312 + 312 × delta / 304, unless it's 0.
        Assert.Equal("ionWillOpen@1032", stage.Log[0]);
        Near(-312, seen[1]);
        for (var i = 2; i < seen.Count; i++)
        {
            var delta = 26 + (8 * (i - 1)) - 26;
            Near(-312 + (312d * delta / 304), seen[i]);
        }

        stage.At(2000);
        Assert.Equal(2, stage.Log.Count);
        Assert.StartsWith("ionDidOpen@", stage.Log[1]);
        Assert.True(stage.Menu.IsOpen());
        Near(0, TranslateX(stage.Container));
    }

    [AvaloniaFact]
    public void B14_Only_Within_MaxEdgeStart_Of_The_Windows_Edge()
    {
        using (var stage = new Stage())
        {
            Swipe(stage, 51, 251, 8);
            stage.At(2000);
            Assert.Empty(stage.Log);
        }

        using (var stage = new Stage())
        {
            Swipe(stage, 50, 250, 8);
            stage.At(2000);
            Assert.True(stage.Menu.IsOpen());
        }

        using (var stage = new Stage(m => m.MaxEdgeStart = 100))
        {
            Swipe(stage, 90, 290, 8);
            stage.At(2000);
            Assert.True(stage.Menu.IsOpen());
        }
    }

    [AvaloniaFact]
    public void B14_The_End_Side_Opens_From_The_Right_Edge()
    {
        using var stage = new Stage(m => m.Side = MenuSide.End);
        var seen = Swipe(stage, 385, 185, 8);
        Near(312 - (312 * (8d * (seen.Count - 2)) / 304), seen[^1]);
        stage.At(2000);
        Assert.True(stage.Menu.IsOpen());
        Assert.Equal(86, WindowX(stage.Container, stage.Window));
    }

    [AvaloniaFact]
    public void B14_A_Short_Slow_Swipe_Goes_Back_Closed()
    {
        using var stage = new Stage();
        Swipe(stage, 10, 40, 1);
        stage.At(3000);
        Assert.Equal("ionWillOpen@1160", stage.Log[0]);
        Assert.StartsWith("ionDidClose(gesture)@", stage.Log[1]);
        Assert.False(stage.Menu.IsOpen());
        Assert.False(stage.Menu.IsVisible);
    }

    [AvaloniaFact]
    public void B14_A_Swipe_Past_Half_The_Width_Opens_It_However_Slow()
    {
        using var stage = new Stage();
        Swipe(stage, 10, 190, 1);
        stage.At(5000);
        Assert.True(stage.Menu.IsOpen());
    }

    [AvaloniaFact]
    public void B14_Nothing_Starts_With_The_Gesture_Off_Or_Disabled_Or_Another_Menu_Open()
    {
        using (var stage = new Stage(m => m.MenuSwipeGesture = false))
        {
            Swipe(stage, 10, 210, 8);
            stage.At(2000);
            Assert.Empty(stage.Log);
        }

        using (var stage = new Stage(m => m.IsEnabled = false))
        {
            stage.Log.Clear();
            Swipe(stage, 10, 210, 8);
            stage.At(2000);
            Assert.Empty(stage.Log);
        }

        using (var stage = new Stage())
        {
            var other = new AvonicMenu { ContentId = "Main", Side = MenuSide.End };
            stage.Root.Children.Insert(1, other);
            Stage.Settle();
            _ = other.Open(false);
            stage.After(16);
            // This menu's swipe doesn't start; the open one's does, from anywhere, and a swipe right closes it.
            Swipe(stage, 10, 210, 8);
            stage.At(2000);
            Assert.Empty(stage.Log);
            Assert.False(other.IsOpen());
        }
    }

    [AvaloniaFact]
    public void B14_Nothing_Starts_While_A_Modal_Is_Presented()
    {
        using var stage = new Stage();
        var modal = new AvonicModal { Content = new Border() };
        _ = modal.Present(stage.Window);
        stage.At(800);
        Swipe(stage, 10, 210, 8);
        stage.At(2000);
        Assert.Empty(stage.Log);
    }

    [AvaloniaFact]
    public void B15_An_Open_Menu_Swipes_Closed_From_Anywhere()
    {
        using var stage = new Stage();
        stage.Opened();
        var seen = Swipe(stage, 280, 130, 15);

        // The first frame after the capture: the step clamped to 0.9999 of the way, then linear back.
        Near(-312 * 0.0001, seen[0], 1e-6);
        Near(-312 * (135d / 304), seen[^1]);
        stage.At(3000);
        Assert.Equal("ionWillClose(gesture)@1016", stage.Log[0]);
        Assert.StartsWith("ionDidClose(gesture)@", stage.Log[1]);
        Assert.False(stage.Menu.IsVisible);
    }

    [AvaloniaFact]
    public void B15_A_Short_Slow_Swipe_On_An_Open_Menu_Goes_Back_Open()
    {
        // M14: a short swipe on the backdrop raises ionWillClose, then ionDidOpen as it goes back.
        using var stage = new Stage();
        stage.Opened();
        Swipe(stage, 300, 280, 1);
        stage.At(3000);
        Assert.Equal(2, stage.Log.Count);
        Assert.StartsWith("ionWillClose(gesture)@", stage.Log[0]);
        Assert.StartsWith("ionDidOpen@", stage.Log[1]);
        Assert.True(stage.Menu.IsOpen());
        Assert.Equal(0, TranslateX(stage.Container), 6);
    }

    // ── What Avalonia's controls go through that a document's elements rarely do (code review, pass 1) ─────────────────────

    [AvaloniaFact]
    public void B16_A_Type_Changed_While_Opening_Lets_The_Open_End_And_The_Close_Is_The_New_Types()
    {
        using var stage = new Stage();
        var opened = stage.Menu.Open();
        stage.At(160);
        stage.Menu.Type = MenuType.Reveal;
        stage.At(Done);
        Assert.True(opened.IsCompleted && opened.Result);
        Assert.False(MenuController.IsAnimating());
        Near(0, TranslateX(stage.Container));

        var closed = stage.Menu.Close();
        stage.After(48);
        Assert.True(TranslateX(stage.Main) > 200, $"the content at {TranslateX(stage.Main)}");
        stage.After(Done);
        Assert.True(closed.IsCompleted && closed.Result);
        Assert.Null(stage.Main.RenderTransform);
    }

    [AvaloniaFact]
    public void B16_A_Type_Changed_While_Open_Keeps_It_In_Place_Until_It_Closes()
    {
        using var stage = new Stage();
        stage.Opened();
        stage.Menu.Type = MenuType.Push;
        stage.Render();
        Assert.Equal(0, WindowX(stage.Container, stage.Window));
        Assert.True(stage.Menu.IsOpen());

        var closed = stage.Menu.Close(false);
        Stage.Settle();
        Assert.True(closed.IsCompleted && closed.Result);
        Assert.False(stage.Menu.IsVisible);
        Assert.Null(stage.Main.RenderTransform);
    }

    [AvaloniaFact]
    public void B16_With_No_Animation_For_Its_Type_Open_Faults_And_Leaves_No_Menu_Animating()
    {
        using var stage = new Stage();
        stage.Menu.Type = null;
        var opened = stage.Menu.Open();
        Stage.Settle();
        Assert.True(opened.IsFaulted);
        Assert.False(MenuController.IsAnimating());
        Assert.False(stage.Menu.IsVisible);
        Assert.True(stage.Main.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void B17_Detached_While_Opening_It_Ends_Closed_And_Opens_Again_Once_Back()
    {
        using var stage = new Stage();
        var opened = stage.Menu.Open();
        stage.At(160);
        stage.Root.Children.Remove(stage.Menu);
        stage.At(400);
        Assert.True(opened.IsCompleted && !opened.Result);
        Assert.False(MenuController.IsAnimating());
        Assert.True(stage.Main.IsHitTestVisible);

        stage.Root.Children.Insert(0, stage.Menu);
        Stage.Settle();
        var again = stage.Menu.Open();
        Assert.True(Avonic.Components.Gestures.GestureController.Instance.IsScrollDisabled);
        stage.After(Done + 16);
        Assert.True(again.IsCompleted && again.Result);
    }

    [AvaloniaFact]
    public void B8_Disabled_During_A_Swipe_The_Next_Open_Still_Opens()
    {
        using var stage = new Stage();
        Swipe(stage, 10, 100, 8, lift: false);
        stage.Menu.IsEnabled = false;
        Stage.Settle();
        stage.Menu.IsEnabled = true;
        Stage.Settle();
        stage.Log.Clear();
        var opened = stage.Menu.Open();
        stage.After(Done + 16);
        Assert.True(opened.IsCompleted && opened.Result);

        // Nothing of the swipe's is left on the animation: one open, shown, the content covered.
        Assert.Equal(["ionWillOpen", "ionDidOpen"], stage.Log.Select(l => l[..l.IndexOf('@')]));
        Assert.True(stage.Menu.IsOpen());
        Assert.True(stage.Menu.IsVisible);
        Assert.False(stage.Main.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void B14_Nothing_Starts_While_Another_Menu_Is_Opening()
    {
        using var stage = new Stage();
        var second = new AvonicMenu { MenuId = "second", ContentId = "Main", Side = MenuSide.End };
        stage.Root.Children.Insert(1, second);
        Stage.Settle();
        stage.At(1000);
        _ = second.Open();
        Swipe(stage, 10, 210, 8);
        stage.At(3000);
        Assert.Empty(stage.Log);
        Assert.False(stage.Menu.IsOpen());
        Assert.True(second.IsOpen());
    }

    [AvaloniaFact]
    public void B14_A_Change_Of_Direction_Moves_The_Swipe_To_The_Other_Edge()
    {
        using var stage = new Stage();
        stage.Window.FlowDirection = FlowDirection.RightToLeft;
        Stage.Settle();
        Swipe(stage, 385, 185, 8);
        stage.At(3000);
        Assert.True(stage.Menu.IsOpen());
    }

    [AvaloniaFact]
    public void B3_The_Contents_Shadow_Follows_Its_Box_While_Open()
    {
        using var stage = new Stage(m => m.Type = MenuType.Reveal);
        stage.Opened();
        stage.Render();
        stage.Main.Margin = new Thickness(0, 100, 0, 0);
        stage.Render();
        Assert.Equal(new Size(390, 744), stage.ContentShadow.Bounds.Size);
        Assert.Equal(100, stage.ContentShadow.TranslatePoint(default, stage.Window)!.Value.Y);
    }

    [AvaloniaFact]
    public void B4_Push_Moves_A_Right_To_Left_Content_Right_In_A_Left_To_Right_Window()
    {
        using var stage = new Stage(m => m.Type = MenuType.Push);
        stage.Main.FlowDirection = FlowDirection.RightToLeft;
        stage.Opened();
        Near(304, PhysicalLeft(stage.Main, stage.Window), 0.5);
    }

    // ── Found by the breakage run ──────────────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_The_Overlay_Is_At_Z_Index_1000_And_Reveal_And_Push_At_0()
    {
        // M1, M3, M4: menu-type-overlay's z-index 1000; reveal's 0; push's auto.
        using var stage = new Stage();
        Assert.Equal(1000, stage.Menu.ZIndex);
        stage.Menu.Type = MenuType.Reveal;
        Assert.Equal(0, stage.Menu.ZIndex);
        stage.Menu.Type = MenuType.Push;
        Assert.Equal(0, stage.Menu.ZIndex);
    }

    [AvaloniaFact]
    public void B4_The_Side_Away_From_The_Edge_Has_No_Safe_Area()
    {
        // :host(.menu-side-start) .menu-inner { --ion-safe-area-right: 0px }, and the left for the end side.
        using var stage = new Stage();
        stage.Root.Resources["Avonic.SafeAreaLeft"] = 24d;
        stage.Root.Resources["Avonic.SafeAreaRight"] = 24d;
        _ = stage.Menu.Open(false);
        Stage.Settle();
        Assert.True(stage.Container.TryFindResource("Avonic.SafeAreaRight", out var right));
        Assert.Equal(0d, right);
        Assert.True(stage.Container.TryFindResource("Avonic.SafeAreaLeft", out var left));
        Assert.Equal(24d, left);

        stage.Menu.Side = MenuSide.End;
        Assert.True(stage.Container.TryFindResource("Avonic.SafeAreaLeft", out left));
        Assert.Equal(0d, left);
        Assert.True(stage.Container.TryFindResource("Avonic.SafeAreaRight", out right));
        Assert.Equal(24d, right);
    }

    [AvaloniaFact]
    public void B14_Nothing_Starts_While_Another_Menu_Is_Open_Even_One_With_No_Swipe()
    {
        using var stage = new Stage();
        var other = new AvonicMenu { ContentId = "Main", Side = MenuSide.End, MenuSwipeGesture = false };
        stage.Root.Children.Insert(1, other);
        Stage.Settle();
        _ = other.Open(false);
        stage.After(16);
        Swipe(stage, 10, 210, 8);
        stage.At(2000);
        Assert.Empty(stage.Log);
        Assert.True(other.IsOpen());
    }

    [AvaloniaFact]
    public void B14_A_Short_Flick_Faster_Than_0_2_Px_A_Ms_Opens_It()
    {
        // 25 px a frame, 1.6 px/ms, for 60 px, far short of half the width.
        using var stage = new Stage();
        Swipe(stage, 10, 70, 25);
        stage.At(3000);
        Assert.True(stage.Menu.IsOpen());
    }

    [AvaloniaFact]
    public void B19_Close_With_No_Menu_Closes_The_Open_One_Not_The_First()
    {
        using var stage = new Stage(m => m.MenuId = "first");
        var second = new AvonicMenu { MenuId = "second", ContentId = "Main", Side = MenuSide.End };
        stage.Root.Children.Insert(1, second);
        Stage.Settle();
        _ = second.Open(false);
        stage.After(16);
        var closed = MenuController.Close();
        stage.After(Done + 16);
        Assert.True(closed.IsCompleted && closed.Result);
        Assert.False(second.IsOpen());
    }

    [AvaloniaFact]
    public void B2_The_Overlays_Container_Has_Its_Shadow_And_Push_And_Reveal_None()
    {
        // M2: :host(.menu-type-overlay) .menu-inner { box-shadow: 4px 0px 16px rgba(0, 0, 0, 0.18) }; M3, M4: none.
        using var stage = new Stage();
        Assert.Equal(Avonic.Components.Behaviors.CssBoxShadow.Parse("4px 0px 16px rgba(0, 0, 0, 0.18)"), stage.Container.BoxShadow);
        stage.Menu.Type = MenuType.Push;
        Assert.Equal(default, stage.Container.BoxShadow);
    }

    [AvaloniaFact]
    public void B4_Push_Follows_A_Change_Of_The_Contents_Direction_Between_Opens()
    {
        // The content in a wrapper of its own, which turns right to left after a first open: the next push still moves the content
        // physically right, as CSS's translateX is physical.
        using var stage = new Stage(m => m.Type = MenuType.Push);
        stage.Root.Children.Remove(stage.Main);
        var wrapper = new Border { Child = stage.Main };
        stage.Root.Children.Add(wrapper);
        Stage.Settle();
        stage.Opened();
        Near(304, PhysicalLeft(stage.Main, stage.Window), 0.5);
        _ = stage.Menu.Close(false);
        Stage.Settle();

        wrapper.FlowDirection = FlowDirection.RightToLeft;
        Stage.Settle();
        stage.Opened();
        Near(304, PhysicalLeft(stage.Main, stage.Window), 0.5);
    }

    [AvaloniaFact]
    public void B14_A_Swipe_With_No_Animation_For_Its_Type_Ends_Where_It_Started()
    {
        // The will event, then the did event of the state it stays in, and nothing left animating.
        using var stage = new Stage();
        stage.Menu.Type = null;
        Swipe(stage, 10, 210, 8);
        stage.At(3000);
        Assert.Equal(["ionWillOpen", "ionDidClose(gesture)"], stage.Log.Select(l => l[..l.IndexOf('@')]));
        Assert.False(MenuController.IsAnimating());
        Assert.False(stage.Menu.IsVisible);
        Assert.True(stage.Main.IsHitTestVisible);
    }
}
