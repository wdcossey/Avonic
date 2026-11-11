using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-toggle in Chromium, at 390 px wide, and the tests are named after the behaviours in
/// docs/components/ion-toggle/PLAN.md. Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub,
/// which has the same parts and the same track and handle.
/// </summary>
public class AvonicToggleTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Danger = Color.Parse("#C5000F");

    private static readonly AttachedProperty<bool> SourceProperty =
        AvaloniaProperty.RegisterAttached<AvonicToggleTests, StyledElement, bool>("Source");

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────

    private sealed class Changes
    {
        public List<(bool Checked, string? Value)> All { get; } = [];

        public static Changes Of(AvonicToggle toggle)
        {
            var changes = new Changes();
            toggle.IonChange += (_, e) => changes.All.Add((e.Checked, e.Value));
            return changes;
        }
    }

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static Control Track(AvonicToggle toggle) => Part<Control>(toggle, "PART_Track");

    private static Control Handle(AvonicToggle toggle) => Part<Control>(toggle, "PART_Handle");

    private static Control Label(AvonicToggle toggle) => Part<Control>(toggle, "PART_Label");

    private static Rect Bounds(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static Point Centre(Visual visual, Scene scene) => Bounds(visual, scene.Window).Center;

    private static void AssertBox(double x, double y, double width, double height, Rect actual)
    {
        // Layout rounds to whole pixels, and Chromium keeps fractions.
        Assert.InRange(actual.X, x - 0.5, x + 0.5);
        Assert.InRange(actual.Y, y - 0.5, y + 0.5);
        Assert.InRange(actual.Width, width - 0.5, width + 0.5);
        Assert.InRange(actual.Height, height - 0.5, height + 0.5);
    }

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Click(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Settle();
    }

    private static void Touch(Scene scene, Point down, Point up)
    {
        using var finger = scene.Window.TouchBegin(down, RawInputModifiers.None);
        if (up != down)
        {
            scene.Window.TouchMove(finger, up, RawInputModifiers.None);
        }

        scene.Window.TouchEnd(finger, up, RawInputModifiers.None);
        Settle();
    }

    private static void PressKey(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        Settle();
    }

    private static void ReleaseKey(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Settle();
    }

    /// <summary>A mouse drag, each event at its own time on the clock, so the gesture's frames run between them.</summary>
    private sealed class Mouse(Scene scene, TestClock clock)
    {
        public Mouse Down(Point at, double time)
        {
            clock.At(time);
            GestureTests.Render();
            scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
            Settle();
            return this;
        }

        public Mouse Move(Point at, double time)
        {
            clock.At(time);
            scene.Window.MouseMove(at, RawInputModifiers.LeftMouseButton);
            Settle();
            return this;
        }

        public Mouse Up(Point at, double time)
        {
            clock.At(time);
            scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
            Settle();
            return this;
        }
    }

    /// <summary>A toggle on the stub, the width of the window, with a label.</summary>
    private static (AvonicToggle Toggle, Scene Scene) Stub(string? label = "Label", bool isChecked = false)
    {
        var toggle = new AvonicToggle { Content = label, Checked = isChecked };
        var scene = new Scene(new StackPanel { Children = { toggle } });
        GestureTests.Render();
        return (toggle, scene);
    }

    /// <summary>Toggles in items, in a column on white, with the theme, and no transitions so that end values show at once.</summary>
    private static Scene InItems(params AvonicToggle[] toggles)
    {
        var holder = new StackPanel();
        foreach (var toggle in toggles)
        {
            holder.Children.Add(new AvonicItem { Content = toggle });
        }

        return Themed(holder);
    }

    private static Scene Themed(Control content)
    {
        var scene = new Scene(content);
        scene.Window.Background = Brushes.White;
        Settle();
        foreach (var animatable in content.GetVisualDescendants().OfType<Animatable>())
        {
            animatable.Transitions = null;
        }

        GestureTests.Render();
        return scene;
    }

    private static Color Pixel(Scene scene, int x, int y)
    {
        Settle();
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, x, y);
    }

    private static void AssertClose(Color expected, Color actual, int tolerance = 3)
    {
        Assert.InRange(actual.R, expected.R - tolerance, expected.R + tolerance);
        Assert.InRange(actual.G, expected.G - tolerance, expected.G + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
    }

    /// <summary>A colour at an alpha over white, as Chromium blends it.</summary>
    private static Color OverWhite(Color color, double alpha) => Color.FromRgb(
        (byte)Math.Round(color.R * alpha + 255 * (1 - alpha)),
        (byte)Math.Round(color.G * alpha + 255 * (1 - alpha)),
        (byte)Math.Round(color.B * alpha + 255 * (1 - alpha)));

    private static double TranslateX(Visual visual) =>
        Assert.IsAssignableFrom<TransformOperations>(visual.RenderTransform).Value.M31;

    // ── 1: a click ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T01_A_Click_On_The_Label_The_Track_Or_The_Gap_Flips_It_With_One_IonChange() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Default" };
        var changes = Changes.Of(toggle);
        using var scene = InItems(toggle);
        var label = Bounds(Label(toggle), scene.Window);
        var track = Bounds(Track(toggle), scene.Window);

        Click(scene, new Point(label.X + 5, label.Y + 5));
        Click(scene, track.Center);
        Click(scene, new Point((label.Right + track.X) / 2, track.Center.Y));
        // The empty end of the row, inside the toggle, which fills the item's row.
        Click(scene, new Point(track.Right - 1, 4));

        Assert.Equal([(true, "on"), (false, "on"), (true, "on"), (false, "on")], changes.All);
        Assert.False(toggle.Checked);
    });

    [AvaloniaFact]
    public void T01_A_Double_Click_Flips_It_Twice()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        var changes = Changes.Of(toggle);
        var label = Centre(Label(toggle), scene);

        scene.Window.MouseDown(label, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(label, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseDown(label, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(label, MouseButton.Left, RawInputModifiers.None);
        Settle();

        Assert.Equal(2, changes.All.Count);
        Assert.False(toggle.Checked);
    }

    [AvaloniaFact]
    public void T01_A_Tap_Flips_It_And_A_Finger_That_Moved_Away_Does_Not()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        var changes = Changes.Of(toggle);
        var label = Bounds(Label(toggle), scene.Window);
        var at = new Point(label.X + 5, label.Center.Y);

        Touch(scene, at, at);
        Assert.True(toggle.Checked);
        // Measured touchTap: a tap focuses it too.
        Assert.True(toggle.IsFocused);

        // 20 px along the label, which isn't the gesture's element: no click, as Chromium's measured touchDragLabel.
        Touch(scene, at, at + new Point(20, 0));
        Assert.True(toggle.Checked);
        Assert.Single(changes.All);
    }

    [AvaloniaFact]
    public void T01_A_Click_On_A_Button_In_The_Label_Is_The_Buttons()
    {
        // TestApp has no base theme, so the button gets a template of its own to be pressed.
        var info = new Button
        {
            Width = 60,
            Height = 20,
            Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Gray }),
        };
        var (toggle, scene) = Stub(null);
        using var _ = scene;
        toggle.Content = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "Label" }, info } };
        Settle();
        var changes = Changes.Of(toggle);
        var clicks = 0;
        info.Click += (_, _) => clicks++;

        Click(scene, Centre(info, scene));
        Assert.Equal(1, clicks);
        Assert.Empty(changes.All);

        // The text beside it is still the label's.
        Click(scene, Centre(toggle.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Label"), scene));
        Assert.Single(changes.All);
    }

    [AvaloniaFact]
    public void T01_IonChange_Carries_The_Value_As_It_Is_And_Bubbles()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        toggle.Value = null;
        var bubbled = new List<ToggleChangeEventArgs>();
        scene.Window.AddHandler(AvonicToggle.IonChangeEvent, (_, e) => bubbled.Add(e));

        Click(scene, Centre(Track(toggle), scene));
        toggle.Value = "wifi";
        Click(scene, Centre(Track(toggle), scene));

        Assert.Equal([(true, null), (false, "wifi")], bubbled.Select(e => (e.Checked, e.Value)));
        Assert.All(bubbled, e => Assert.Same(toggle, e.Source));
    }

    // ── 2: in code ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T02_Setting_Checked_In_Code_Raises_Nothing_And_A_Click_Keeps_A_Binding()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        var changes = Changes.Of(toggle);
        var source = new StyledElement();
        var property = SourceProperty;
        toggle.Bind(AvonicToggle.CheckedProperty, source.GetObservable(property));

        toggle.Checked = true;
        toggle.Checked = false;
        source.SetValue(property, true);
        Assert.Empty(changes.All);
        Assert.True(toggle.Checked);

        // The binding survives the toggle setting it.
        Click(scene, Centre(Track(toggle), scene));
        Assert.False(toggle.Checked);
        source.SetValue(property, false);
        source.SetValue(property, true);
        Assert.True(toggle.Checked);
    }

    // ── 3: keys ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T03_Space_Flips_It_On_Key_Down_And_Again_On_Each_Repeat_And_Enter_Does_Nothing()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        var changes = Changes.Of(toggle);
        toggle.Focus(NavigationMethod.Tab);

        PressKey(scene, PhysicalKey.Space);
        Assert.Equal([(true, "on")], changes.All);
        PressKey(scene, PhysicalKey.Space);
        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);
        Assert.Equal(3, changes.All.Count);

        PressKey(scene, PhysicalKey.Enter);
        ReleaseKey(scene, PhysicalKey.Enter);
        Assert.Equal(3, changes.All.Count);
        Assert.True(toggle.Checked);
    }

    // ── 4: disabled ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T04_Disabled_It_Takes_No_Click_Key_Or_Drag_And_Is_No_Tab_Stop()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        using var clock = new TestClock();
        var changes = Changes.Of(toggle);
        var before = new Button { Content = "Before" };
        ((StackPanel)toggle.Parent!).Children.Insert(0, before);
        Settle();
        toggle.IsEnabled = false;
        Settle();

        var track = Centre(Track(toggle), scene);
        Click(scene, track);
        new Mouse(scene, clock).Down(track, 0).Move(track + new Point(6, 0), 20).Move(track + new Point(30, 0), 40).Up(track + new Point(30, 0), 60);
        before.Focus(NavigationMethod.Tab);
        PressKey(scene, PhysicalKey.Tab);
        Assert.False(toggle.IsFocused);
        toggle.Focus();
        PressKey(scene, PhysicalKey.Space);
        ((IToggleProvider)ControlAutomationPeer.CreatePeerForElement(toggle)).Toggle();

        Assert.Empty(changes.All);
        Assert.False(toggle.Checked);
    }

    [AvaloniaFact]
    public void T04_In_A_Disabled_Item_It_Keeps_Its_Look() => InstalledTheme.Run(_ =>
    {
        var inside = new AvonicToggle { Content = "Inside" };
        var own = new AvonicToggle { Content = "Own", IsEnabled = false };
        var holder = new StackPanel { Children = { new AvonicItem { IsEnabled = false, Content = inside }, new AvonicItem { Content = own } } };
        using var scene = Themed(holder);

        // toggle-disabled comes from its own prop only: the disabled item fades it, and the toggle adds nothing.
        Assert.Null(inside.OpacityMask);
        Assert.NotNull(own.OpacityMask);
    });

    [AvaloniaFact]
    public void T04_Disabled_Mid_Drag_It_Flips_No_More()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        using var clock = new TestClock();
        var changes = Changes.Of(toggle);
        var start = Centre(Track(toggle), scene);
        var finger = new GestureTests.Finger(scene, clock);

        finger.Down(start.X, start.Y, 0).Move(start.X + 5, start.Y, 20).Move(start.X + 16, start.Y, 40);
        clock.At(60);
        Assert.Single(changes.All);

        // disabledChanged: gesture.enable(false), which ends the drag.
        toggle.IsEnabled = false;
        Settle();
        finger.Move(start.X - 20, start.Y, 60);
        clock.At(80);
        finger.Up(start.X - 20, start.Y, 80);
        Assert.Single(changes.All);
        Assert.DoesNotContain(":activated", toggle.Classes);
    }

    [AvaloniaFact]
    public void T04_Disabled_The_Toggle_Fades_To_0_38_As_A_Group() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Disabled", IsEnabled = false };
        using var scene = InItems(toggle);
        var handle = Bounds(Handle(toggle), scene.Window);

        // CSS's opacity fades the toggle as one picture, so the white handle hides the track under its right half: white.
        // Faded on its own, the handle would let the track show through, measured 233.
        AssertClose(Colors.White, Pixel(scene, (int)(handle.Right - 4), (int)handle.Center.Y), 2);
        // The track beside it: black at 0.39, then at 0.38.
        AssertClose(OverWhite(Colors.Black, 0.39 * 0.38), Pixel(scene, (int)(handle.Right + 8), (int)handle.Center.Y));
    });

    [AvaloniaFact]
    public void T04_Disabled_A_Bare_Toggle_Still_Shows_The_Handle_Over_Its_Edge() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { IsEnabled = false, Margin = new Thickness(10) };
        using var scene = Themed(new StackPanel { Children = { toggle } });
        scene.Window.Background = Brushes.Black;

        // The handle hangs 3 px over the 14 px toggle. Faded as a group on black: white at 0.38.
        AssertClose(Color.FromRgb(97, 97, 97), Pixel(scene, 20, 8), 6);
    });

    // ── 5: a drag ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T05_A_Drag_On_The_Track_Flips_It_Past_10_Px_From_The_Capture_Each_Way()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        using var clock = new TestClock();
        var changes = Changes.Of(toggle);
        var start = Centre(Track(toggle), scene);
        var finger = new GestureTests.Finger(scene, clock);

        // The capture is at 5 px, so that's where deltaX counts from: 15 is 10 past it, and 16 is more.
        finger.Down(start.X, start.Y, 0).Move(start.X + 5, start.Y, 20).Move(start.X + 15, start.Y, 40);
        clock.At(60);
        Assert.Empty(changes.All);
        finger.Move(start.X + 16, start.Y, 60);
        clock.At(80);
        Assert.Equal([(true, "on")], changes.All);

        // Far off the track, then back past -10 from the capture.
        finger.Move(start.X + 200, start.Y, 80).Move(start.X - 5, start.Y, 100);
        clock.At(120);
        Assert.Single(changes.All);
        finger.Move(start.X - 6, start.Y, 120);
        clock.At(140);
        finger.Up(start.X - 6, start.Y, 140);

        Assert.Equal([(true, "on"), (false, "on")], changes.All);
        Assert.False(toggle.Checked);
    }

    [AvaloniaFact]
    public void T05_A_Mouse_Drag_Flips_It_And_The_Release_Does_Not_Click()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        using var clock = new TestClock();
        var changes = Changes.Of(toggle);
        var start = Centre(Track(toggle), scene);

        new Mouse(scene, clock).Down(start, 0).Move(start + new Point(5, 0), 20).Move(start + new Point(25, 0), 40);
        clock.At(60);
        new Mouse(scene, clock).Up(start + new Point(25, 0), 60);

        Assert.Equal([(true, "on")], changes.All);
    }

    [AvaloniaFact]
    public void T05_A_Vertical_Drag_Or_One_Under_The_Threshold_Is_Not_A_Drag_And_Clicks()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        using var clock = new TestClock();
        var changes = Changes.Of(toggle);
        var start = Centre(Track(toggle), scene);

        // 4 px: no gesture, and a mouse clicks.
        new Mouse(scene, clock).Down(start, 0).Move(start + new Point(4, 0), 20).Up(start + new Point(4, 0), 40);
        Assert.Equal([(true, "on")], changes.All);

        // Sideways after a sideways capture flips; this goes down 6, so it's not captured, and the release clicks.
        new Mouse(scene, clock).Down(start, 400).Move(start + new Point(0, 6), 420).Move(start + new Point(-30, 6), 440).Up(start + new Point(-30, 6), 460);
        Assert.Equal([(true, "on"), (false, "on")], changes.All);
    }

    // ── 6: from the label ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T06_A_Mouse_Drag_From_The_Label_Is_No_Gesture_And_Clicks_On_Release()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        using var clock = new TestClock();
        var changes = Changes.Of(toggle);
        var label = Bounds(Label(toggle), scene.Window);
        var at = new Point(label.X + 2, label.Center.Y);

        new Mouse(scene, clock).Down(at, 0).Move(at + new Point(10, 0), 20).Move(at + new Point(40, 0), 40);
        clock.At(60);
        Assert.Empty(changes.All);
        new Mouse(scene, clock).Up(at + new Point(40, 0), 60);

        Assert.Equal([(true, "on")], changes.All);
    }

    // ── 7: lastDrag ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T07_A_Click_Within_300_Ms_Of_A_Drag_Is_Dropped_And_One_After_Counts()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        using var clock = new TestClock();
        var changes = Changes.Of(toggle);
        var start = Centre(Track(toggle), scene);

        new Mouse(scene, clock).Down(start, 0).Move(start + new Point(5, 0), 20).Move(start + new Point(25, 0), 40);
        clock.At(60);
        new Mouse(scene, clock).Up(start + new Point(25, 0), 60);
        Assert.Single(changes.All);

        clock.At(350);
        Click(scene, start);
        Assert.Single(changes.All);

        // The drag ended at 60, so 361 is past 300 after it.
        clock.At(361);
        Click(scene, start);
        Assert.Equal([(true, "on"), (false, "on")], changes.All);
    }

    // ── 8: the drag's start ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T08_The_Drag_Start_Focuses_It_And_It_Is_Activated_Until_The_End()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        using var clock = new TestClock();
        var focused = 0;
        toggle.IonFocus += (_, _) => focused++;
        var start = Centre(Track(toggle), scene);
        var finger = new GestureTests.Finger(scene, clock);

        // A touch doesn't focus on press, so the focus is the gesture's.
        finger.Down(start.X, start.Y, 0);
        Assert.False(toggle.IsFocused);
        Assert.DoesNotContain(":activated", toggle.Classes);

        finger.Move(start.X + 5, start.Y, 20);
        Assert.True(toggle.IsFocused);
        Assert.Equal(1, focused);
        Assert.Contains(":activated", toggle.Classes);

        finger.Up(start.X + 5, start.Y, 40);
        Assert.DoesNotContain(":activated", toggle.Classes);
    }

    // ── 9: RTL ───────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T09_In_RTL_A_Drag_Left_Turns_It_On_And_Right_Does_Not()
    {
        var toggle = new AvonicToggle { Content = "RTL", FlowDirection = FlowDirection.RightToLeft };
        using var scene = new Scene(new StackPanel { Children = { toggle } });
        using var clock = new TestClock();
        GestureTests.Render();
        var changes = Changes.Of(toggle);
        var start = Centre(Track(toggle), scene);

        new GestureTests.Finger(scene, clock).Down(start.X, start.Y, 0).Move(start.X + 5, start.Y, 20).Move(start.X + 30, start.Y, 40).Up(start.X + 30, start.Y, 60);
        Assert.Empty(changes.All);

        new GestureTests.Finger(scene, clock).Down(start.X, start.Y, 400).Move(start.X - 5, start.Y, 420).Move(start.X - 16, start.Y, 440);
        clock.At(460);
        Assert.Equal([(true, "on")], changes.All);
    }

    [AvaloniaFact]
    public void T09_In_RTL_The_Handle_Sits_At_The_Right_When_Off_And_The_Left_When_On() => InstalledTheme.Run(_ =>
    {
        var off = new AvonicToggle { Content = "Off" };
        var on = new AvonicToggle { Content = "On", Checked = true };
        var holder = new StackPanel { FlowDirection = FlowDirection.RightToLeft, Children = { new AvonicItem { Content = off }, new AvonicItem { Content = on } } };
        using var scene = Themed(holder);

        // Measured with dir="rtl": the track at 16, the handle at 32 when off and 16 when on. By pixel, as a bounding box
        // through a render transform under RTL's mirror isn't where it's drawn.
        AssertBox(16, 16.5, 36, 14, Bounds(Track(off), scene.Window));
        AssertClose(OverWhite(Colors.Black, 0.39), Pixel(scene, 22, 23));
        AssertClose(Colors.White, Pixel(scene, 42, 23));
        AssertClose(Primary, Pixel(scene, 26, 48 + 23));
        AssertClose(OverWhite(Primary, 0.5), Pixel(scene, 46, 48 + 23));
    });

    // ── 10: focus ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T10_A_Press_Focuses_It_With_IonFocus_And_Focus_Leaving_Raises_IonBlur_Both_Bubbling()
    {
        var other = new Button { Content = "Other" };
        var inLabel = new Button { Content = "In the label" };
        var toggle = new AvonicToggle { Content = "Label" };
        using var scene = new Scene(new StackPanel { Children = { toggle, other } });
        var log = new List<string>();
        scene.Window.AddHandler(AvonicToggle.IonFocusEvent, (_, _) => log.Add("focus"));
        scene.Window.AddHandler(AvonicToggle.IonBlurEvent, (_, _) => log.Add("blur"));

        Click(scene, Centre(Track(toggle), scene));
        Assert.True(toggle.IsFocused);
        other.Focus();
        Settle();
        Assert.Equal(["focus", "blur"], log);

        // Focus doesn't bubble in the DOM, so focus on something in the label isn't the toggle's.
        toggle.Content = inLabel;
        Settle();
        inLabel.Focus();
        other.Focus();
        Settle();
        Assert.Equal(["focus", "blur"], log);

        Assert.True(toggle.Focusable);
    }

    // ── 11: the focus ring ───────────────────────────────────────────────────────────────────

    // There's no outline: none, so Chromium draws its own ring on keyboard focus: measured outline: auto 1px. Here that's the
    // base theme's adorner, so the test adds one, as the sample has it.
    [AvaloniaFact]
    public void T11_Keyboard_Focus_Shows_The_Base_Themes_Focus_Adorner()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var toggle = new AvonicToggle { Content = "Label" };
                using var scene = new Scene(new StackPanel { Children = { toggle } });
                var layer = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(toggle)!;

                PressKey(scene, PhysicalKey.Tab);
                Assert.True(toggle.IsFocused);
                Assert.Single(layer.Children);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    // ── 12: in an item ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T12_A_Tap_On_The_Items_Padding_Clicks_The_Toggle_Unless_It_Is_Disabled() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Default" };
        var disabled = new AvonicToggle { Content = "Disabled", IsEnabled = false };
        var changes = Changes.Of(toggle);
        var disabledChanges = Changes.Of(disabled);
        using var scene = InItems(toggle, disabled);

        Click(scene, new Point(5, 20));
        Assert.Equal([(true, "on")], changes.All);
        Assert.True(toggle.Checked);

        Click(scene, new Point(5, 48 + 20));
        Assert.Empty(disabledChanges.All);
        Assert.False(disabled.Checked);
    });

    // item.js's render() keeps getFirstInteractive's control, and sets the click only if there was one. A toggle raises no
    // ionStyle when it's enabled, so the item doesn't render again, and a tap on its padding does nothing.
    [AvaloniaFact]
    public void T12_A_Toggle_Enabled_After_The_Item_Rendered_Takes_No_Tap_On_The_Padding() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Later", IsEnabled = false };
        var changes = Changes.Of(toggle);
        using var scene = InItems(toggle);

        toggle.IsEnabled = true;
        Dispatcher.UIThread.RunJobs();
        Click(scene, new Point(5, 20));

        Assert.Empty(changes.All);
    });

    // ── 13: an item's layout ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T13_In_An_Item_It_Fills_The_Row_With_10_Px_Around_The_Label() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Default" };
        using var scene = InItems(toggle);

        // Measured, with the item at the top: the host 16, 0, 358 by 47; the label 10 down; the track 36 by 14, centred.
        AssertBox(16, 0, 358, 47, Bounds(toggle, scene.Window));
        Assert.Equal(new Thickness(0, 10, 16, 10), Label(toggle).Margin);
        AssertBox(338, 16.5, 36, 14, Bounds(Track(toggle), scene.Window));
        AssertBox(338, 13.5, 20, 20, Bounds(Handle(toggle), scene.Window));
    });

    [AvaloniaFact]
    public void T13_Stacked_In_An_Item_Has_10_And_16_Around_The_Label_And_10_Under_The_Track() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Stacked centred", LabelPlacement = ToggleLabelPlacement.Stacked };
        using var scene = InItems(toggle);

        // Measured: 69 high, the label's layout box 10 down, and the track 16 under it, centred across, 10 off the bottom.
        var label = Bounds(Label(toggle), scene.Window);
        var labelHeight = Label(toggle).Bounds.Height;
        Assert.Equal(new Thickness(0, 10, 0, 16), Label(toggle).Margin);
        AssertBox(16, 0, 358, 10 + labelHeight + 16 + 14 + 10, Bounds(toggle, scene.Window));
        AssertBox(177, 10 + labelHeight + 16, 36, 14, Bounds(Track(toggle), scene.Window));

        // scale(0.75) from the middle, so centred across: measured 195, the middle of the host.
        Assert.InRange(label.Center.X, 194.5, 195.5);
        Assert.InRange(label.Width, Label(toggle).Bounds.Width * 0.75 - 0.5, Label(toggle).Bounds.Width * 0.75 + 0.5);
    });

    // ── 14: no label ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T14_With_No_Label_The_Track_Is_At_The_Start_Of_The_Row() => InstalledTheme.Run(_ =>
    {
        var none = new AvonicToggle();
        var empty = new AvonicToggle { Content = "" };
        using var scene = InItems(none, empty);

        AssertBox(16, 16.5, 36, 14, Bounds(Track(none), scene.Window));
        AssertBox(16, 48 + 16.5, 36, 14, Bounds(Track(empty), scene.Window));
        Assert.False(Label(none).IsVisible);
    });

    // ── 15: placements ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T15_Label_At_The_End_Packs_Track_Then_Label_At_The_Start() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "End", LabelPlacement = ToggleLabelPlacement.End };
        using var scene = InItems(toggle);

        // Measured: the track at 16, the label at 68, 16 after the track.
        AssertBox(16, 16.5, 36, 14, Bounds(Track(toggle), scene.Window));
        Assert.InRange(Bounds(Label(toggle), scene.Window).X - Label(toggle).Margin.Left, 51.5, 52.5);
        Assert.Equal(new Thickness(16, 10, 0, 10), Label(toggle).Margin);
    });

    [AvaloniaFact]
    public void T15_A_Fixed_Label_Is_100_Px_Trimmed_And_The_Track_Stays_At_The_End() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "A fixed label that is far too long", LabelPlacement = ToggleLabelPlacement.Fixed };
        using var scene = InItems(toggle);

        // Measured: 100 wide, at 14, centred down the 47 px row with its 10 px margins.
        AssertBox(16, (47 - Label(toggle).Bounds.Height) / 2, 100, Label(toggle).Bounds.Height, Bounds(Label(toggle), scene.Window));
        AssertBox(338, 16.5, 36, 14, Bounds(Track(toggle), scene.Window));
        var text = Label(toggle).GetVisualDescendants().OfType<TextBlock>().First();
        Assert.True(text.TextLayout.TextLines[0].HasCollapsed);
    });

    [AvaloniaFact]
    public void T15_A_Fixed_Label_Does_Not_Shrink_In_A_Narrow_Row() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "A fixed label", LabelPlacement = ToggleLabelPlacement.Fixed };
        using var scene = Themed(new StackPanel { Width = 130, HorizontalAlignment = HorizontalAlignment.Left, Children = { toggle } });

        // flex: 0 0 100px; min-width: 100px: the track is pushed out instead, 16 after the label.
        Assert.Equal(100, Label(toggle).Bounds.Width);
        Assert.InRange(Bounds(Track(toggle), scene.Window).X, 115.5, 116.5);
    });

    [AvaloniaFact]
    public void T15_Stacked_Alone_It_Is_As_Wide_As_The_Labels_Layout_Box_With_Both_Centred() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Stacked alone", LabelPlacement = ToggleLabelPlacement.Stacked };
        using var scene = Themed(new StackPanel { Children = { toggle } });

        // Measured: the host as wide as the label's unscaled box, 49 high = label + 16 + 14, the track centred under it.
        var labelWidth = Label(toggle).Bounds.Width;
        var labelHeight = Label(toggle).Bounds.Height;
        AssertBox(0, 0, labelWidth, labelHeight + 16 + 14, Bounds(toggle, scene.Window));
        AssertBox((labelWidth - 36) / 2, labelHeight + 16, 36, 14, Bounds(Track(toggle), scene.Window));
        // text-align: center, which a wrapped hint's lines inherit.
        toggle.HelperText = "Helper";
        Settle();
        Assert.Equal(TextAlignment.Center, Part<TextBlock>(toggle, "PART_HelperText").TextAlignment);
        Assert.Equal(new RelativePoint(0.5, 0.5, RelativeUnit.Relative), Label(toggle).RenderTransformOrigin);
    });

    // ── 16: justify and alignment ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T16_Justify_Packs_The_Row_And_Alignment_Start_Puts_A_Stacked_Toggle_At_The_Start() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicToggle { Content = "Justify start", Justify = ToggleJustify.Start };
        var end = new AvonicToggle { Content = "Justify end", Justify = ToggleJustify.End };
        var between = new AvonicToggle { Content = "Between", Justify = ToggleJustify.SpaceBetween };
        var aligned = new AvonicToggle { Content = "Align start", LabelPlacement = ToggleLabelPlacement.Stacked, Alignment = ToggleAlignment.Start };
        using var scene = Themed(new StackPanel { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Children = { start, end, between, aligned } });

        // Measured: justify start puts the track 16 after the label; end puts both at the end; display: block fills 300.
        var startLabel = Label(start).Bounds.Width;
        Assert.InRange(Bounds(Track(start), scene.Window).X, startLabel + 16 - 0.5, startLabel + 16 + 0.5);
        Assert.InRange(Bounds(Track(end), scene.Window).Right, 299.5, 300.5);
        var endLabel = Bounds(Label(end), scene.Window);
        Assert.InRange(endLabel.X, 300 - 36 - 16 - endLabel.Width - 0.5, 300 - 36 - 16 - endLabel.Width + 0.5);
        Assert.InRange(Bounds(Label(between), scene.Window).X, -0.5, 0.5);
        Assert.InRange(Bounds(Track(between), scene.Window).Right, 299.5, 300.5);
        Assert.All(new[] { start, end, between, aligned }, toggle => Assert.InRange(toggle.Bounds.Width, 299.5, 300.5));

        // alignment start: the label scaled from its top left, and the track at 0, 16 under the label's unscaled box.
        var label = Bounds(Label(aligned), scene.Window);
        var top = Bounds(aligned, scene.Window).Y;
        AssertBox(0, top, Label(aligned).Bounds.Width * 0.75, Label(aligned).Bounds.Height * 0.75, label);
        AssertBox(0, top + Label(aligned).Bounds.Height + 16, 36, 14, Bounds(Track(aligned), scene.Window));
    });

    [AvaloniaFact]
    public void T16_Alignment_Start_Puts_A_Row_At_The_Top_And_Justify_Packs_A_Tall_Stacked_Column() => InstalledTheme.Run(_ =>
    {
        var row = new AvonicToggle { Content = "Top", Alignment = ToggleAlignment.Start };
        var between = new AvonicToggle { Content = "Between", LabelPlacement = ToggleLabelPlacement.Stacked, Height = 120 };
        var start = new AvonicToggle { Content = "Start", LabelPlacement = ToggleLabelPlacement.Stacked, Justify = ToggleJustify.Start, Height = 120 };
        var end = new AvonicToggle { Content = "End", LabelPlacement = ToggleLabelPlacement.Stacked, Justify = ToggleJustify.End, Height = 120 };
        using var scene = Themed(new StackPanel { Children = { new AvonicItem { Content = row }, between, start, end } });

        // align-items: start: the track's wrapper at the top of the 47 px row, the label 10 down by its margin.
        AssertBox(338, 0, 36, 14, Bounds(Track(row), scene.Window));
        Assert.InRange(Bounds(Label(row), scene.Window).Y, 9.5, 10.5);

        // A column taller than its content: space-between puts the track at the bottom, start right under the label, end
        // both at the bottom.
        var labelHeight = Label(between).Bounds.Height + 16;
        Assert.InRange(Bounds(Track(between), scene.Window).Bottom - Bounds(between, scene.Window).Y, 119.5, 120.5);
        Assert.InRange(Bounds(Track(start), scene.Window).Y - Bounds(start, scene.Window).Y, labelHeight - 0.5, labelHeight + 0.5);
        Assert.InRange(Bounds(Track(end), scene.Window).Bottom - Bounds(end, scene.Window).Y, 119.5, 120.5);
        Assert.InRange(Bounds(Track(end), scene.Window).Y - Bounds(end, scene.Window).Y, 120 - 14 - 0.5, 120 - 14 + 0.5);
        Assert.InRange(Label(end).Bounds.Y, 120 - 14 - labelHeight - 0.5, 120 - 14 - labelHeight + 0.5);
    });

    [AvaloniaFact]
    public void T15_A_Long_Stacked_Label_Is_Up_To_A_Third_Wider_Than_The_Row_And_Scales_Back_To_It() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle
        {
            Content = "A stacked label that is much too long to fit on one line across the whole of the row of an item",
            LabelPlacement = ToggleLabelPlacement.Stacked,
        };
        using var scene = InItems(toggle);

        // max-width: calc(100% / 0.75), then scale(0.75) from the middle: the label's box is 358 / 0.75, drawn 358 wide, as CSS's
        // box is the max-width, though the trimmed text is narrower. Rounded to a pixel.
        var drawn = Bounds(Label(toggle), scene.Window);
        Assert.InRange(Label(toggle).Bounds.Width, 358 / 0.75 - 0.5, 358 / 0.75 + 1);
        Assert.InRange(drawn.Width, 358 - 4.5, 358 + 0.5);
        Assert.InRange(drawn.Center.X, 194.5, 195.5);
    });

    // ── 17: inline ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T17_Outside_An_Item_It_Is_As_Wide_As_Its_Content() => InstalledTheme.Run(_ =>
    {
        var bare = new AvonicToggle { Margin = new Thickness(10) };
        var labelled = new AvonicToggle { Content = "Alone" };
        using var scene = Themed(new StackPanel { Children = { bare, labelled } });

        // Measured: 36 by 14 with no label, and label + 16 + 36 with one.
        Assert.Equal(new Size(36, 14), bare.Bounds.Size);
        Assert.InRange(labelled.Bounds.Width, Label(labelled).Bounds.Width + 16 + 36 - 0.5, Label(labelled).Bounds.Width + 16 + 36 + 0.5);

        // The handle hangs 3 px over a bare toggle, and isn't clipped: on black, the row above the toggle is the handle's white.
        scene.Window.Background = Brushes.Black;
        AssertClose(Colors.White, Pixel(scene, 20, 8), 8);
    });

    // ── 18: a long label ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T18_A_Long_Label_Is_Trimmed_And_Does_Not_Push_The_Track_Out() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "A label that is far too long to fit on one line beside the track of a toggle" };
        using var scene = InItems(toggle);

        AssertBox(338, 16.5, 36, 14, Bounds(Track(toggle), scene.Window));
        Assert.InRange(Bounds(Label(toggle), scene.Window).Right, 0, 338 - 16 + 0.5);
        var text = Label(toggle).GetVisualDescendants().OfType<TextBlock>().First();
        Assert.True(text.TextLayout.TextLines[0].HasCollapsed);
        Assert.Single(text.TextLayout.TextLines);
    });

    // ── 19: helper and error text ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T19_Helper_Text_Shows_Until_Touched_And_Invalid_Then_The_Error_Text_Replaces_It() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Helper", HelperText = "Helper text", ErrorText = "Error text" };
        var plain = new AvonicToggle { Content = "Plain" };
        var helperOnly = new AvonicToggle { Content = "Helper only", HelperText = "Helper text" };
        using var scene = InItems(toggle, plain, helperOnly);
        Assert.True(Part<TextBlock>(helperOnly, "PART_HelperText").IsEffectivelyVisible);
        var helper = Part<TextBlock>(toggle, "PART_HelperText");
        var error = Part<TextBlock>(toggle, "PART_ErrorText");

        Assert.True(helper.IsEffectivelyVisible);
        Assert.False(error.IsEffectivelyVisible);
        Assert.Equal(12, helper.FontSize);
        // padding-top 4: measured 4 between the label's text and the helper's.
        Assert.Equal(4, Part<Control>(toggle, "PART_Bottom").Margin.Top);
        AssertClose(Color.Parse("#4D4D4D"), Assert.IsAssignableFrom<ISolidColorBrush>(helper.Foreground).Color, 0);

        // One class isn't enough: checkInvalidState wants both.
        toggle.Classes.Add("ion-invalid");
        Settle();
        Assert.True(helper.IsEffectivelyVisible);
        toggle.Classes.Add("ion-touched");
        Settle();
        Assert.False(helper.IsEffectivelyVisible);
        Assert.True(error.IsEffectivelyVisible);
        AssertClose(Danger, Assert.IsAssignableFrom<ISolidColorBrush>(error.Foreground).Color, 0);

        toggle.Classes.Remove("ion-touched");
        Settle();
        Assert.True(helper.IsEffectivelyVisible);
        Assert.False(error.IsEffectivelyVisible);

        // Neither: no bottom, so no 4 px.
        Assert.False(Part<Control>(plain, "PART_Bottom").IsEffectivelyVisible);
    });

    // ── 20: colours ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T20_The_Colours_Of_Each_State_And_A_Color_Beats_The_Tokens() => InstalledTheme.Run(_ =>
    {
        var off = new AvonicToggle { Content = "Off" };
        var on = new AvonicToggle { Content = "On", Checked = true };
        var danger = new AvonicToggle { Content = "Danger", Checked = true, Color = "danger" };
        danger.Resources["Avonic.Toggle.HandleBackgroundChecked"] = new SolidColorBrush(Colors.Lime);
        danger.Resources["Avonic.Toggle.TrackBackgroundChecked"] = new SolidColorBrush(Colors.Lime);
        using var scene = InItems(off, on, danger);

        Color At(AvonicToggle toggle, double dx)
        {
            var track = Bounds(Track(toggle), scene.Window);
            return Pixel(scene, (int)(track.X + dx), (int)track.Center.Y);
        }

        // Measured: rgba(0, 0, 0, 0.39) and white; rgba(primary, 0.5) and primary; the handle moved 16 to the right.
        AssertClose(OverWhite(Colors.Black, 0.39), At(off, 32));
        AssertClose(Colors.White, At(off, 8));
        AssertClose(OverWhite(Primary, 0.5), At(on, 8));
        AssertClose(Primary, At(on, 28));
        Assert.Equal(16, TranslateX(Part<Control>(on, "PART_IconWrapper")));
        AssertClose(OverWhite(Danger, 0.5), At(danger, 8));
        AssertClose(Danger, At(danger, 28));

        // A Color unchecked shows nothing of it.
        danger.Checked = false;
        AssertClose(OverWhite(Colors.Black, 0.39), At(danger, 32));
    });

    [AvaloniaFact]
    public void T20_The_Tokens_Apply() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Tokens", Checked = true };
        toggle.Resources["Avonic.Toggle.TrackBackgroundChecked"] = new SolidColorBrush(Colors.Lime);
        toggle.Resources["Avonic.Toggle.HandleBackgroundChecked"] = new SolidColorBrush(Colors.Magenta);
        toggle.Resources["Avonic.Toggle.HandleWidth"] = 10.0;
        toggle.Resources["Avonic.Toggle.HandleHeight"] = 30.0;
        toggle.Resources["Avonic.Toggle.HandleMaxHeight"] = 24.0;
        toggle.Resources["Avonic.Toggle.HandleSpacing"] = 2.0;
        toggle.Resources["Avonic.Toggle.BorderRadius"] = 0.0;
        toggle.Resources["Avonic.Toggle.HandleBorderRadius"] = "2px";
        toggle.Resources["Avonic.Toggle.HandleBoxShadow"] = BoxShadows.Parse("0 0 0 0 Red");
        using var scene = InItems(toggle);

        var handle = (Border)Handle(toggle);
        var track = (Border)Track(toggle);
        Assert.Equal(Colors.Lime, Assert.IsAssignableFrom<ISolidColorBrush>(track.Background).Color);
        Assert.Equal(Colors.Magenta, Assert.IsAssignableFrom<ISolidColorBrush>(handle.Background).Color);
        Assert.Equal(new CornerRadius(0), track.CornerRadius);
        Assert.Equal(new CornerRadius(2), handle.CornerRadius);
        Assert.Equal(BoxShadows.Parse("0 0 0 0 Red"), handle.BoxShadow);
        // max-height beats height; left: spacing; checked: wrapper 36 - 10, and the handle back by twice the spacing.
        Assert.Equal(new Size(10, 24), handle.Bounds.Size);
        Assert.Equal(2, handle.Margin.Left);
        Assert.Equal(26, TranslateX(Part<Control>(toggle, "PART_IconWrapper")));
        Assert.Equal(-4, TranslateX(handle));

        toggle.Checked = false;
        toggle.Resources["Avonic.Toggle.TrackBackground"] = new SolidColorBrush(Colors.Navy);
        toggle.Resources["Avonic.Toggle.HandleBackground"] = new SolidColorBrush(Colors.Orange);
        Settle();
        Assert.Equal(Colors.Navy, Assert.IsAssignableFrom<ISolidColorBrush>(track.Background).Color);
        Assert.Equal(Colors.Orange, Assert.IsAssignableFrom<ISolidColorBrush>(handle.Background).Color);
    });

    // ── 21: transitions ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T21_The_Handle_Moves_Over_160_Ms_And_The_Track_Recolours_Over_160_Ms_Ease() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicToggle { Content = "Transitions" };
        using var scene = new Scene(new AvonicItem { Content = toggle });

        static void AssertTransition<T>(Control part, AvaloniaProperty property, double x1, double y1, double x2, double y2) where T : ITransition
        {
            var animation = Assert.Single(part.Transitions!.OfType<T>().OfType<TransitionBase>(), t => t.Property == property);
            Assert.Equal(TimeSpan.FromMilliseconds(160), animation.Duration);
            var easing = Assert.IsType<CubicBezierEasing>(animation.Easing);
            Assert.Equal((x1, y1, x2, y2), (easing.X1, easing.Y1, easing.X2, easing.Y2));
        }

        AssertTransition<TransformOperationsTransition>(Part<Control>(toggle, "PART_IconWrapper"), Visual.RenderTransformProperty, 0.4, 0, 0.2, 1);
        AssertTransition<TransformOperationsTransition>(Handle(toggle), Visual.RenderTransformProperty, 0.4, 0, 0.2, 1);
        AssertTransition<BrushTransition>(Handle(toggle), Border.BackgroundProperty, 0.4, 0, 0.2, 1);
        AssertTransition<BrushTransition>(Track(toggle), Border.BackgroundProperty, 0.25, 0.1, 0.25, 1);

        // And it does ease: just after the flip, the handle hasn't arrived.
        toggle.Checked = true;
        Settle();
        Assert.InRange(TranslateX(Part<Control>(toggle, "PART_IconWrapper")), 0, 15.9);
    });

    // ── 22: on/off labels ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T22_On_Off_Labels_Put_An_18_Px_Icon_In_The_Handle() => InstalledTheme.Run(_ =>
    {
        var off = new AvonicToggle { Content = "Off", EnableOnOffLabels = true };
        var on = new AvonicToggle { Content = "On", EnableOnOffLabels = true, Checked = true };
        var danger = new AvonicToggle { Content = "Danger", EnableOnOffLabels = true, Checked = true, Color = "warning" };
        var none = new AvonicToggle { Content = "None" };
        var inWarning = new AvonicToggle { Content = "In a warning item", EnableOnOffLabels = true, Checked = true };
        using var scene = InItems(off, on, danger, none, inWarning);
        inWarning.FindAncestorOfType<AvonicItem>()!.Color = "warning";
        Settle();

        static AvonicIcon Icon(AvonicToggle toggle) => Part<AvonicIcon>(toggle, "PART_SwitchIcon");

        Assert.Equal("remove-outline", Icon(off).IconName);
        Assert.Equal(Colors.Black, Assert.IsAssignableFrom<ISolidColorBrush>(Icon(off).Foreground).Color);
        Assert.Equal("checkmark-outline", Icon(on).IconName);
        Assert.Equal(Colors.White, Assert.IsAssignableFrom<ISolidColorBrush>(Icon(on).Foreground).Color);
        // var(ion-color-contrast): warning's is black, and it inherits from a coloured item too.
        Assert.Equal(Colors.Black, Assert.IsAssignableFrom<ISolidColorBrush>(Icon(danger).Foreground).Color);
        Assert.Equal(Colors.Black, Assert.IsAssignableFrom<ISolidColorBrush>(Icon(inWarning).Foreground).Color);
        AssertBox(1, 1, 18, 18, Bounds(Icon(off), Handle(off)));
        Assert.False(Icon(none).IsEffectivelyVisible);
        Assert.True(Icon(off).IsEffectivelyVisible);
    });

    // ── 23: automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T23_Automation_Is_A_Toggle_Named_By_Its_Label_And_Helper_Text() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicToggle { Content = "Default" };
        var helper = new AvonicToggle { Content = "Helper", HelperText = "Helper text", ErrorText = "Error" };
        var bare = new AvonicToggle();
        var named = new AvonicToggle();
        AutomationProperties.SetName(named, "Named");
        var labelledAndNamed = new AvonicToggle { Content = "Label" };
        AutomationProperties.SetName(labelledAndNamed, "Named");
        var helperOnly = new AvonicToggle { Content = "Helper only", HelperText = "Thanks" };
        using var scene = InItems(plain, helper, bare, named, labelledAndNamed, helperOnly);

        static AutomationPeer Peer(AvonicToggle toggle) => ControlAutomationPeer.CreatePeerForElement(toggle);

        // Measured in Chromium's accessibility tree.
        Assert.Equal(AutomationControlType.Button, Peer(plain).GetAutomationControlType());
        Assert.Equal("Default", Peer(plain).GetName());
        Assert.Equal("Helper Helper text", Peer(helper).GetName());
        Assert.Equal("Helper text", Peer(helper).GetHelpText());
        Assert.Equal("", Peer(bare).GetName());
        Assert.Equal("Named", Peer(named).GetName());
        // aria-labelledby beats aria-label: measured, the label's text.
        Assert.Equal("Label", Peer(labelledAndNamed).GetName());

        helper.Classes.Add("ion-touched");
        helper.Classes.Add("ion-invalid");
        Assert.Equal("Helper", Peer(helper).GetName());
        Assert.Equal("Error", Peer(helper).GetHelpText());

        // Invalid with no error text: aria-describedby is the helper's element, which is empty while invalid.
        helperOnly.Classes.Add("ion-touched");
        helperOnly.Classes.Add("ion-invalid");
        Assert.True(string.IsNullOrEmpty(Peer(helperOnly).GetHelpText()));
    });

    [AvaloniaFact]
    public void T23_The_Name_And_Help_Text_Announce_Their_Changes()
    {
        var (toggle, scene) = Stub("Terms");
        using var _ = scene;
        toggle.HelperText = "Thanks";
        toggle.ErrorText = "Required";
        var peer = ControlAutomationPeer.CreatePeerForElement(toggle);
        var raised = new List<(AutomationProperty Property, object? Value)>();
        peer.PropertyChanged += (_, e) => raised.Add((e.Property, e.NewValue));

        // Touched alone changes neither, so nothing is announced.
        toggle.Classes.Add("ion-touched");
        Assert.Empty(raised);

        // The app marks it invalid, as on blur: a new description and a new name, as the helper text leaves it.
        toggle.Classes.Add("ion-invalid");
        Assert.Contains((AutomationElementIdentifiers.HelpTextProperty, (object?)"Required"), raised);
        Assert.Contains((AutomationElementIdentifiers.NameProperty, (object?)"Terms"), raised);

        raised.Clear();
        toggle.ErrorText = "Turn it on";
        Assert.Equal([(AutomationElementIdentifiers.HelpTextProperty, (object?)"Turn it on")], raised);

        // A pseudoclass, a flip, announces nothing of the name or the help text, which haven't changed.
        raised.Clear();
        toggle.Checked = true;
        Assert.DoesNotContain(raised, change => change.Property == AutomationElementIdentifiers.NameProperty || change.Property == AutomationElementIdentifiers.HelpTextProperty);
    }

    [AvaloniaFact]
    public void T23_Toggling_Through_Automation_Is_A_Click_And_The_State_Follows()
    {
        var (toggle, scene) = Stub();
        using var _ = scene;
        var changes = Changes.Of(toggle);
        var peer = ControlAutomationPeer.CreatePeerForElement(toggle);
        var provider = Assert.IsAssignableFrom<IToggleProvider>(peer);
        var raised = new List<object?>();
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == TogglePatternIdentifiers.ToggleStateProperty)
            {
                raised.Add(e.NewValue);
            }
        };

        Assert.Equal(ToggleState.Off, provider.ToggleState);
        provider.Toggle();
        Assert.Equal(ToggleState.On, provider.ToggleState);
        Assert.Equal([(true, "on")], changes.All);
        toggle.Checked = false;
        Assert.Equal([ToggleState.On, ToggleState.Off], raised);
    }

    // ── 24: in a slot ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T24_In_An_Items_End_Slot_It_Has_No_Margin_And_An_Apps_Plain_Style_Still_Wins() => InstalledTheme.Run(_ =>
    {
        var end = new AvonicToggle { Content = "End slot" };
        var start = new AvonicToggle { Content = "Start slot" };
        var holder = new StackPanel
        {
            Children =
            {
                new AvonicItem { Content = new AvonicLabel { Content = "Label beside" }, EndContent = end },
                new AvonicItem { StartContent = start, Content = new AvonicLabel { Content = "Label beside" } },
            },
        };
        using var scene = Themed(holder);

        // Measured: the end-slot toggle reaches the inner padding's edge, 374, and 16 from the label's end, which isn't its margin.
        Assert.Equal(new Thickness(0), end.Margin);
        Assert.Equal(new Thickness(0), start.Margin);
        Assert.InRange(Bounds(end, scene.Window).Right, 373.5, 374.5);
        Assert.InRange(Bounds(Track(end), scene.Window).X, 337.5, 338.5);

        var app = new Style(x => x.OfType<AvonicToggle>()) { Setters = { new Setter(Layoutable.MarginProperty, new Thickness(3)) } };
        Application.Current!.Styles.Add(app);
        try
        {
            Settle();
            Assert.Equal(new Thickness(3), end.Margin);
        }
        finally
        {
            Application.Current!.Styles.Remove(app);
        }
    });

    [AvaloniaFact]
    public void T24_In_The_Default_Slot_An_App_Themes_Margin_Stays() => InstalledTheme.Run(_ =>
    {
        // Ionic has no rule for a toggle in the default slot, so nothing here beats a margin the app's own theme gives it.
        var toggle = new AvonicToggle { Content = "Default slot" };
        var theme = new ControlTheme(typeof(AvonicToggle)) { BasedOn = (ControlTheme)Application.Current!.FindResource(typeof(AvonicToggle))! };
        theme.Setters.Add(new Setter(Layoutable.MarginProperty, new Thickness(5)));
        toggle.Theme = theme;
        using var scene = InItems(toggle);
        Assert.Equal(new Thickness(5), toggle.Margin);
    });

    [AvaloniaFact]
    public void T24_The_Item_Counts_It_As_An_Input()
    {
        IItemControl toggle = new AvonicToggle();
        Assert.Equal(ItemControlType.Toggle, toggle.ItemControlType);
    }
}
