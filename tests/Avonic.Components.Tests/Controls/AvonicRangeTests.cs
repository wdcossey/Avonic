using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-range in Chromium, at 390 px wide, and the tests are named after the behaviours in
/// docs/components/ion-range/PLAN.md. Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub, which
/// has the same parts, a 42 px slider and a 36 px knob handle.
/// </summary>
public class AvonicRangeTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Danger = Color.Parse("#C5000F");

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Every event the range raises, in order, as Ionic's names and values.</summary>
    private sealed class Log
    {
        public List<string> All { get; } = [];

        public static Log Of(AvonicRange range)
        {
            var log = new Log();
            range.IonKnobMoveStart += (_, e) => log.All.Add($"start {e.Value}");
            range.IonInput += (_, e) => log.All.Add($"input {e.Value}");
            range.IonChange += (_, e) => log.All.Add($"change {e.Value}");
            range.IonKnobMoveEnd += (_, e) => log.All.Add($"end {e.Value}");
            range.IonFocus += (_, _) => log.All.Add("focus");
            range.IonBlur += (_, _) => log.All.Add("blur");
            return log;
        }

        public List<string> Take()
        {
            var all = All.ToList();
            All.Clear();
            return all;
        }
    }

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static RangeSlider Slider(AvonicRange range) => Part<RangeSlider>(range, "PART_Slider");

    private static RangeKnobHandle HandleA(AvonicRange range) => Part<RangeKnobHandle>(range, "PART_KnobHandleA");

    private static RangeKnobHandle HandleB(AvonicRange range) => Part<RangeKnobHandle>(range, "PART_KnobHandleB");

    private static Rect Bounds(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    /// <summary>A point at a fraction of the slider's width, from its left, in the window.</summary>
    private static Point At(AvonicRange range, Scene scene, double fraction)
    {
        var slider = Bounds(Slider(range), scene.Window);
        return new Point(slider.X + slider.Width * fraction, slider.Center.Y);
    }

    private static void AssertNear(double expected, double actual, double tolerance = 0.5) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Click(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Settle();
    }

    private static void Key(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Settle();
    }

    /// <summary>A mouse drag, each event at its own time on the clock, so that the gesture's frames run between them.</summary>
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

    /// <summary>A range on the stub, the width of the window.</summary>
    private static Scene Stub(AvonicRange range, double width = 390)
    {
        var scene = new Scene(new StackPanel { Children = { range } }, width);
        GestureTests.Render();
        return scene;
    }

    /// <summary>Ranges in items, on white, with the theme, and no transitions, so that end values show at once.</summary>
    private static Scene InItems(params AvonicRange[] ranges)
    {
        var holder = new StackPanel();
        foreach (var range in ranges)
        {
            holder.Children.Add(new AvonicItem { Content = range });
        }

        return Themed(holder);
    }

    private static Scene Themed(Control content)
    {
        var scene = new Scene(content);
        scene.Window.Background = Brushes.White;
        Settle();
        NoTransitions(content);
        GestureTests.Render();
        return scene;
    }

    private static void NoTransitions(Visual root)
    {
        foreach (var animatable in root.GetVisualDescendants().OfType<Animatable>())
        {
            animatable.Transitions = null;
        }
    }

    private static Color Pixel(Scene scene, int x, int y)
    {
        Settle();
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, x, y);
    }

    private static void AssertClose(Color expected, Color actual, int tolerance = 4)
    {
        Assert.InRange(actual.R, expected.R - tolerance, expected.R + tolerance);
        Assert.InRange(actual.G, expected.G - tolerance, expected.G + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
    }

    private static Color OverWhite(Color color, double alpha) => Color.FromRgb(
        (byte)Math.Round(color.R * alpha + 255 * (1 - alpha)),
        (byte)Math.Round(color.G * alpha + 255 * (1 - alpha)),
        (byte)Math.Round(color.B * alpha + 255 * (1 - alpha)));

    private static Matrix Transform(Visual visual) =>
        Assert.IsAssignableFrom<TransformOperations>(visual.RenderTransform).Value;

    private static double Scale(Visual visual) => Transform(visual).M11;

    // ── 1: where the knob is ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R01_The_Knob_Is_At_Its_Ratio_And_The_Bar_Runs_To_It()
    {
        var range = new AvonicRange { Value = 40 };
        using var scene = Stub(range);

        // 36 px handle centred on 40 % of 390.
        AssertNear(156 - 18, Bounds(HandleA(range), scene.Window).X);
        var bar = Bounds(Part<Control>(range, "PART_BarActive"), scene.Window);
        AssertNear(0, bar.X);
        AssertNear(156, bar.Width);

        range.Min = 20;
        range.Max = 60;
        GestureTests.Render();
        AssertNear(195 - 18, Bounds(HandleA(range), scene.Window).X);
    }

    [AvaloniaFact]
    public void R01_A_Value_Out_Of_Range_Is_Not_Clamped_And_The_Knob_Stops_At_The_End()
    {
        var range = new AvonicRange { Value = 150 };
        using var scene = Stub(range);

        Assert.Equal(new RangeValue(150), range.Value);
        AssertNear(390 - 18, Bounds(HandleA(range), scene.Window).X);
        Assert.Equal(100, HandleA(range).Value);
    }

    // ── 2: a value set in code ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R02_Setting_The_Value_Raises_IonInput_When_It_Differs_And_Never_IonChange()
    {
        var range = new AvonicRange { Value = 40 };
        using var scene = Stub(range);
        var log = Log.Of(range);

        range.Value = 55;
        range.Value = 55;

        Assert.Equal(["input 55"], log.Take());
    }

    [AvaloniaFact]
    public void R02_The_Value_It_Loads_With_Raises_Nothing()
    {
        using var clock = new TestClock();
        var inputs = 0;
        var range = new AvonicRange { Debounce = 0 };
        range.IonInput += (_, _) => inputs++;
        range.Value = 50;
        using var scene = Stub(range);
        clock.At(100);

        Assert.Equal(0, inputs);
    }

    // ── 3: a tap ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R03_A_Click_On_The_Bar_Moves_The_Knob_There_With_Start_Input_Change_End()
    {
        var range = new AvonicRange { Value = 40 };
        using var scene = Stub(range);
        var log = Log.Of(range);

        Click(scene, At(range, scene, 0.75));
        Assert.Equal(["start 40", "input 75", "change 75", "end 75"], log.Take());
        Assert.Equal(new RangeValue(75), range.Value);

        // Measured click same: no input, as nothing changed, but a change all the same.
        Click(scene, At(range, scene, 0.75));
        Assert.Equal(["focus", "start 75", "change 75", "end 75"], log.Take());
    }

    [AvaloniaFact]
    public void R03_A_Press_Does_Not_Move_It_And_A_Move_Under_The_Threshold_Is_A_Tap_At_The_Release()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 30 };
        using var scene = Stub(range);
        var log = Log.Of(range);
        var mouse = new Mouse(scene, clock);

        mouse.Down(At(range, scene, 0.5), 0);
        Assert.Empty(log.All);
        Assert.Equal(new RangeValue(30), range.Value);

        // 6 px, under the 10 px threshold: measured 6px move, the value at the release.
        var release = At(range, scene, 0.5) + new Point(6, 0);
        mouse.Move(release, 16).Up(release, 32);
        Assert.Equal(["start 30", "input 52", "change 52", "end 52"], log.Take());
    }

    [AvaloniaFact]
    public void R03_A_Tap_With_A_Finger_Moves_The_Knob()
    {
        var range = new AvonicRange { Value = 70 };
        using var scene = Stub(range);
        var log = Log.Of(range);

        var at = At(range, scene, 0.2);
        using (var finger = scene.Window.TouchBegin(at, RawInputModifiers.None))
        {
            scene.Window.TouchEnd(finger, at, RawInputModifiers.None);
        }

        Settle();
        Assert.Equal(["start 70", "input 20", "change 20", "end 20"], log.Take());
    }

    // ── 4: a drag ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R04_A_Drag_Captures_At_10_Px_Raises_Input_Once_A_Frame_And_Change_At_The_Release()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 75 };
        using var scene = Stub(range);
        var log = Log.Of(range);
        var mouse = new Mouse(scene, clock);
        var start = At(range, scene, 0.75);

        // The press is on the knob, which takes focus, as measured.
        mouse.Down(start, 0).Move(start - new Point(8, 0), 16);
        Assert.Equal(["focus"], log.Take());

        // Past the threshold: the capture. The next move is the first frame's.
        mouse.Move(start - new Point(39, 0), 32);
        Assert.Equal(["start 75"], log.Take());
        mouse.Move(start - new Point(39, 0), 48);
        clock.At(64);
        Assert.Equal(["input 65"], log.Take());
        Assert.Contains(":range-pressed", range.Classes);
        Assert.Contains(":range-knob-pressed", HandleA(range).Classes);

        // Two moves in one frame: one input, with the first.
        mouse.Move(start - new Point(78, 0), 70).Move(start - new Point(117, 0), 72);
        clock.At(86);
        Assert.Equal(["input 55"], log.Take());

        mouse.Up(start - new Point(117, 0), 96);
        Assert.Equal(["input 45", "change 45", "end 45"], log.Take());
        Assert.DoesNotContain(":range-pressed", range.Classes);
        Assert.DoesNotContain(":range-knob-pressed", HandleA(range).Classes);
    }

    [AvaloniaFact]
    public void R04_The_Knob_Follows_The_Pointer_Exactly_And_The_Value_Rounds_To_The_Step()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 0, Step = 10 };
        using var scene = Stub(range);
        var mouse = new Mouse(scene, clock);
        var start = At(range, scene, 0);

        // 0.33 of 390 is 128.7: the knob there, the value 30.
        mouse.Down(start, 0).Move(start + new Point(60, 0), 16).Move(start + new Point(128.7, 0), 32);
        clock.At(48);
        GestureTests.Render();
        AssertNear(128.7 - 18, Bounds(HandleA(range), scene.Window).X, 1);
        Assert.Equal(new RangeValue(30), range.Value);
        mouse.Up(start + new Point(128.7, 0), 64);

        // One knob stays where it was let go (behaviour 22).
        AssertNear(128.7 - 18, Bounds(HandleA(range), scene.Window).X, 1);
    }

    // ── 5: snaps and ticks ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R05_Snaps_Puts_The_Knob_On_A_Step_And_Ticks_Mark_Each_With_The_Active_Ones_Between()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 50, Step = 10, Snaps = true };
        using var scene = Stub(range);
        var ticks = Part<RangeTicks>(range, "PART_Ticks");

        // Measured r8: 11 ticks, 6 active.
        Assert.Equal(11, ticks.Items.Count);
        Assert.Equal(6, ticks.Items.Count(tick => tick.IsActive));
        Assert.Equal(0.3, ticks.Items[3].Ratio, 6);

        var mouse = new Mouse(scene, clock);
        var start = At(range, scene, 0.5);
        mouse.Down(start, 0).Move(start - new Point(30, 0), 16).Move(start - new Point(60, 0), 32);
        clock.At(48);
        GestureTests.Render();
        // 0.346 snaps to 0.3.
        AssertNear(117 - 18, Bounds(HandleA(range), scene.Window).X);
        mouse.Up(start - new Point(60, 0), 64);
        Assert.Equal(4, ticks.Items.Count(tick => tick.IsActive));

        range.Ticks = false;
        Assert.Empty(ticks.Items);
        range.Ticks = true;
        range.Snaps = false;
        Assert.Empty(ticks.Items);
        range.Snaps = true;
        range.Step = 0;
        Assert.Empty(ticks.Items);
    }

    // ── 6: keys ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R06_The_Arrows_Step_A_Focused_Knob_And_No_Other_Key_Does()
    {
        var range = new AvonicRange { Value = 40 };
        using var scene = Stub(range);
        HandleA(range).Focus(NavigationMethod.Tab);
        Settle();
        var log = Log.Of(range);

        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal(["start 40", "input 41", "change 41", "end 41"], log.Take());
        Key(scene, PhysicalKey.ArrowUp);
        Key(scene, PhysicalKey.ArrowDown);
        Key(scene, PhysicalKey.ArrowLeft);
        Assert.Equal(new RangeValue(40), range.Value);
        log.Take();

        foreach (var key in new[] { PhysicalKey.Home, PhysicalKey.End, PhysicalKey.PageUp, PhysicalKey.PageDown, PhysicalKey.Space, PhysicalKey.Enter })
        {
            Key(scene, key);
        }

        Assert.Empty(log.All);

        // At the limit: still start, change and end.
        range.Value = 100;
        log.Take();
        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal(["start 100", "change 100", "end 100"], log.Take());
    }

    [AvaloniaFact]
    public void R06_A_Step_With_Snaps_And_A_Fractional_Step_Rounds_To_Its_Decimals()
    {
        var range = new AvonicRange { Min = 0, Max = 1, Step = 0.1, Value = 0.6 };
        using var scene = Stub(range);
        HandleA(range).Focus(NavigationMethod.Tab);
        Settle();

        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal(new RangeValue(0.7), range.Value);
    }

    [AvaloniaFact]
    public void R06_The_Arrows_Do_Not_Swap_In_RTL()
    {
        var range = new AvonicRange { Value = 40, FlowDirection = FlowDirection.RightToLeft };
        using var scene = Stub(range);
        HandleA(range).Focus(NavigationMethod.Tab);
        Settle();

        Key(scene, PhysicalKey.ArrowLeft);
        Assert.Equal(new RangeValue(39), range.Value);
    }

    // ── 7: two knobs ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R07_Two_Knobs_Cross_Keeping_Their_Identity_And_Swapping_Lower_And_Upper()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { DualKnobs = true, Value = new RangeValue(20, 80) };
        using var scene = Stub(range);
        var log = Log.Of(range);
        var (a, b) = (HandleA(range), HandleB(range));
        Assert.True(b.IsVisible);
        Assert.Contains(":lower", a.Classes);
        Assert.Contains(":upper", b.Classes);

        var mouse = new Mouse(scene, clock);
        var start = At(range, scene, 0.2);
        // The first frame's move picks the knob, as setPressedKnob does in onMove: A, near the press.
        mouse.Down(start, 0).Move(start + new Point(20, 0), 16).Move(start + new Point(30, 0), 32);
        clock.At(48);
        Assert.Contains(":range-pressed-a", range.Classes);
        Assert.Contains(":range-pressed-lower", range.Classes);
        mouse.Move(start + new Point(273, 0), 64);
        clock.At(80);
        Assert.Contains(":range-pressed-upper", range.Classes);
        mouse.Up(start + new Point(273, 0), 96);

        Assert.Equal(new RangeValue(80, 90), range.Value);
        Assert.Equal(90, a.Value);
        Assert.Contains(":upper", a.Classes);
        Assert.Contains(":lower", b.Classes);
        Assert.Equal("change 80, 90", log.All[^2]);
    }

    [AvaloniaFact]
    public void R07_A_Press_Moves_The_Nearer_Knob_And_B_On_A_Tie()
    {
        var range = new AvonicRange { DualKnobs = true, Value = new RangeValue(40, 60) };
        using var scene = Stub(range);

        Click(scene, At(range, scene, 0.5));
        Assert.Equal(new RangeValue(40, 50), range.Value);
        Assert.Equal(50, HandleB(range).Value);

        Click(scene, At(range, scene, 0.1));
        Assert.Equal(new RangeValue(10, 50), range.Value);
        Assert.Equal(10, HandleA(range).Value);
    }

    [AvaloniaFact]
    public void R07_A_Value_Set_In_Code_Moves_The_Knobs_Least_And_A_Number_Or_A_Pair_Is_Read_As_Ionic_Reads_It()
    {
        var range = new AvonicRange { DualKnobs = true, Value = new RangeValue(20, 80) };
        using var scene = Stub(range);

        // A is at 20, B at 80: 70 and 90 go to them so that neither jumps across.
        range.Value = new RangeValue(70, 90);
        Assert.Equal(70, HandleA(range).Value);
        // From 70 and 90, 10 and 75 move them 75 in all, and 75 and 10 would move them 85.
        range.Value = new RangeValue(10, 75);
        Assert.Equal(10, HandleA(range).Value);
        Assert.Equal(75, HandleB(range).Value);

        // Measured set 70 number: { lower: 0, upper: 70 }.
        range.Value = 70;
        Assert.Equal(0, HandleA(range).Value);
        Assert.Equal(70, HandleB(range).Value);

        var single = new AvonicRange { Value = new RangeValue(10, 30) };
        using var other = Stub(single);
        Assert.Equal(30, HandleA(single).Value);
        Assert.False(HandleB(single).IsVisible);
    }

    [AvaloniaFact]
    public void R07_A_Is_The_Lower_Knob_Whatever_Order_The_Props_Were_Set_In()
    {
        var range = new AvonicRange { Value = new RangeValue(20, 80), DualKnobs = true };
        using var scene = Stub(range);

        Assert.Equal(20, HandleA(range).Value);
        Assert.Equal(80, HandleB(range).Value);
    }

    // ── 8: activeBarStart ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R08_The_Active_Bar_Runs_Between_ActiveBarStart_And_The_Knob_On_Either_Side()
    {
        var range = new AvonicRange { ActiveBarStart = 50, Value = 20, Snaps = true, Step = 10 };
        using var scene = Stub(range);
        var bar = Part<Control>(range, "PART_BarActive");

        AssertNear(78, Bounds(bar, scene.Window).X);
        AssertNear(117, Bounds(bar, scene.Window).Width);
        Assert.Equal(4, Part<RangeTicks>(range, "PART_Ticks").Items.Count(tick => tick.IsActive));

        range.Value = 80;
        GestureTests.Render();
        AssertNear(195, Bounds(bar, scene.Window).X);
        AssertNear(117, Bounds(bar, scene.Window).Width);
    }

    [AvaloniaFact]
    public void R08_ActiveBarStart_Is_Checked_Once_Every_Prop_Is_Set()
    {
        // Before Max is set it's over the default 100, and it stays: Ionic checks it at connectedCallback.
        var range = new AvonicRange { ActiveBarStart = 150, Max = 200, Value = 20 };
        using var scene = Stub(range);

        Assert.Equal(150, range.ActiveBarStart);
        AssertNear(39, Bounds(Part<Control>(range, "PART_BarActive"), scene.Window).X);
    }

    [AvaloniaFact]
    public void R08_ActiveBarStart_Outside_Min_To_Max_Moves_To_The_Nearer_End()
    {
        var range = new AvonicRange { ActiveBarStart = 150 };
        using var scene = Stub(range);
        Assert.Equal(100, range.ActiveBarStart);

        range.ActiveBarStart = -5;
        Assert.Equal(0, range.ActiveBarStart);
    }

    // ── 9: the pin ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R09_The_Pin_Shows_The_Formatted_Value_And_Rises_While_Pressed_Or_Keyboard_Focused() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Pin = true, Value = 42.5, Step = 0.5 };
        using var scene = InItems(range);
        var pin = Part<Panel>(range, "PART_PinA");
        var knob = Part<Border>(range, "PART_KnobA");

        Assert.True(pin.IsVisible);
        // Math.round: a half goes up.
        Assert.Equal("43", Part<TextBlock>(range, "PART_PinTextA").Text);
        AssertNear(0.01, Scale(pin), 1e-6);

        // The pin shows the knob's value, which is on a step.
        range.PinFormatter = value => $"{value:0.0} °C";
        Assert.Equal("42.5 °C", Part<TextBlock>(range, "PART_PinTextA").Text);

        HandleA(range).Focus(NavigationMethod.Tab);
        Settle();
        Assert.Equal(1, Scale(pin), 6);
        Assert.Equal(-24, Transform(pin).M32, 6);
        // With a pin the knob stays at 0.67.
        AssertNear(0.67, Scale(knob), 1e-6);

        // Room for it: measured r3, 28 px above.
        Assert.Equal(70, range.Bounds.Height);

        // The pin's circle, above its tip's rounded top, and the tip, below the circle, in pin-background.
        var handle = Bounds(HandleA(range), scene.Window);
        AssertClose(Primary, Pixel(scene, (int)handle.Center.X, (int)(handle.Y - 22.5)));
        AssertClose(Primary, Pixel(scene, (int)handle.Center.X, (int)handle.Y + 5));
    });

    // ── 10: focus ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R10_Focus_Raises_IonFocus_Entering_And_IonBlur_Leaving_And_Nothing_Between_Knobs()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { DualKnobs = true, Value = new RangeValue(20, 80) };
        var after = new Button { Content = "after" };
        using var scene = new Scene(new StackPanel { Children = { range, after } });
        GestureTests.Render();
        var log = Log.Of(range);

        HandleA(range).Focus(NavigationMethod.Tab);
        Settle();
        Assert.Equal(["focus"], log.Take());

        HandleB(range).Focus(NavigationMethod.Tab);
        clock.At(1);
        Settle();
        Assert.Empty(log.All);
        Assert.Contains(":ion-focused", HandleB(range).Classes);
        Assert.DoesNotContain(":ion-focused", HandleA(range).Classes);

        after.Focus(NavigationMethod.Tab);
        clock.At(2);
        Settle();
        Assert.Equal(["blur"], log.Take());
        Assert.DoesNotContain(":ion-focused", HandleB(range).Classes);
    }

    [AvaloniaFact]
    public void R10_The_Knobs_Are_Tab_Stops_In_Order()
    {
        var before = new Button { Content = "before" };
        var range = new AvonicRange { DualKnobs = true, Value = new RangeValue(20, 80) };
        using var scene = new Scene(new StackPanel { Children = { before, range } });
        GestureTests.Render();
        before.Focus();
        Settle();

        Key(scene, PhysicalKey.Tab);
        Assert.True(HandleA(range).IsFocused);
        Key(scene, PhysicalKey.Tab);
        Assert.True(HandleB(range).IsFocused);
    }

    [AvaloniaFact]
    public void R10_A_Press_On_A_Knob_Focuses_It_Without_Showing_Focus()
    {
        var range = new AvonicRange { Value = 50 };
        using var scene = Stub(range);

        Click(scene, Bounds(HandleA(range), scene.Window).Center);
        Assert.True(HandleA(range).IsFocused);
        Assert.DoesNotContain(":ion-focused", HandleA(range).Classes);

        // Focus in code shows it, as Ionic's focusFromPointer is only set by a press.
        var other = new AvonicRange { Value = 50 };
        using var second = Stub(other);
        HandleA(other).Focus();
        Settle();
        Assert.Contains(":ion-focused", HandleA(other).Classes);
    }

    // ── 11: disabled ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R11_Disabled_Takes_No_Tap_Drag_Or_Focus()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 40, IsEnabled = false };
        using var scene = Stub(range);
        var log = Log.Of(range);

        Click(scene, At(range, scene, 0.8));
        var mouse = new Mouse(scene, clock);
        var start = At(range, scene, 0.4);
        mouse.Down(start, 0).Move(start + new Point(60, 0), 16).Move(start + new Point(60, 0), 32).Up(start + new Point(60, 0), 48);

        Assert.Empty(log.All);
        Assert.Equal(new RangeValue(40), range.Value);
        Assert.False(HandleA(range).Focus());
        Assert.Contains(":range-disabled", range.Classes);
    }

    [AvaloniaFact]
    public void R11_Disabling_It_Mid_Drag_Ends_The_Drag()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 50 };
        using var scene = Stub(range);
        var mouse = new Mouse(scene, clock);
        var start = At(range, scene, 0.5);
        mouse.Down(start, 0).Move(start + new Point(20, 0), 16).Move(start + new Point(39, 0), 32);
        clock.At(48);
        var log = Log.Of(range);

        // disabledChanged: gesture.enable(false), which ends a captured gesture with onEnd.
        range.IsEnabled = false;
        Settle();
        Assert.Equal(["change 60", "end 60"], log.Take());
        Assert.DoesNotContain(":range-pressed", range.Classes);
        mouse.Up(start + new Point(39, 0), 64);
    }

    [AvaloniaFact]
    public void R11_Disabled_Greys_The_Bar_And_Shrinks_The_Knob_In_A_White_Ring() => InstalledTheme.Run(_ =>
    {
        var range = new AvonicRange { Value = 40, IsEnabled = false, Label = "Off" };
        using var scene = InItems(range);
        var knob = Part<Border>(range, "PART_KnobA");

        AssertNear(0.55, Scale(knob), 1e-6);
        Assert.Equal(0.38, Part<Control>(range, "PART_Label").Opacity, 6);
        var bar = Bounds(Part<Control>(range, "PART_Bar"), scene.Window);
        AssertClose(Color.Parse("#BFBFBF"), Pixel(scene, (int)bar.Right - 4, (int)bar.Center.Y));

        // The ring: 5 px scaled to 0.55 around the 9.9 px knob, white over the bar.
        var centre = Bounds(knob, scene.Window).Center;
        AssertClose(Colors.White, Pixel(scene, (int)(centre.X + 6.2), (int)centre.Y));
    });

    // ── 12: in an item ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R12_In_An_Item_With_Nothing_At_An_End_It_Is_Padded_18_Px_There() => InstalledTheme.Run(_ =>
    {
        var labelled = new AvonicRange { Label = "Label", Value = 40 };
        var bare = new AvonicRange { Value = 0 };
        var end = new AvonicRange { Label = "End", LabelPlacement = RangeLabelPlacement.End, Value = 70 };
        var slots = new AvonicRange { Value = 40, StartContent = "Low", EndContent = "High" };
        using var scene = InItems(labelled, bare, end, slots);

        Assert.Equal(new Thickness(0, 0, 18, 0), labelled.Padding);
        Assert.Equal(new Thickness(18, 0, 18, 0), bare.Padding);
        Assert.Equal(new Thickness(18, 0, 0, 0), end.Padding);
        Assert.Equal(default, slots.Padding);
        Assert.Contains(":in-item", bare.Classes);

        // Measured r2: the slider from 34 to 356, its knob at the start at 16.
        var slider = Bounds(Slider(bare), scene.Window);
        AssertNear(34, slider.X);
        AssertNear(322, slider.Width);
        AssertNear(16, Bounds(HandleA(bare), scene.Window).X);

        var outside = new AvonicRange();
        using var alone = Stub(outside);
        Assert.DoesNotContain(":in-item", outside.Classes);
        Assert.DoesNotContain(":range-item-start-adjustment", outside.Classes);
    });

    [AvaloniaFact]
    public void R12_In_An_Items_Start_Slot_It_Grows_With_Flex_3() => InstalledTheme.Run(_ =>
    {
        var range = new AvonicRange { Value = 50 };
        var holder = new StackPanel { Children = { new AvonicItem { StartContent = range } } };
        using var scene = Themed(holder);

        // Measured: 265.5 of the row, with 18 px padding at both ends, the slider 229.5.
        AssertNear(265.5, range.Bounds.Width, 1);
        AssertNear(229.5, Slider(range).Bounds.Width, 1);
    });

    [AvaloniaFact]
    public void R12_An_Item_Counts_Ranges_As_Inputs()
    {
        var item = new AvonicItem
        {
            Content = new StackPanel { Children = { new AvonicRange(), new AvonicRange() } },
        };
        using var scene = new Scene(item);
        Settle();

        Assert.Contains(":item-multiple-inputs", item.Classes);

        // One is an input, not a cover such as a checkbox: the item doesn't become pressable.
        var single = new AvonicItem { Content = new AvonicRange() };
        using var other = new Scene(single);
        Settle();
        Assert.DoesNotContain(":ion-activatable", single.Classes);
        Assert.DoesNotContain(":item-multiple-inputs", single.Classes);
    }

    // ── 13: the label ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R13_Each_Label_Placement_Lays_Out_As_Measured() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicRange { Label = "Label", Value = 40 };
        var stacked = new AvonicRange { Label = "Stacked", LabelPlacement = RangeLabelPlacement.Stacked, Value = 30 };
        var fixedLabel = new AvonicRange { Label = "A fixed label that is far too long", LabelPlacement = RangeLabelPlacement.Fixed, Value = 50 };
        var end = new AvonicRange { Label = "End", LabelPlacement = RangeLabelPlacement.End, Value = 70 };
        var none = new AvonicRange();
        using var scene = InItems(start, stacked, fixedLabel, end, none);

        // Measured r1: label at 16, slider from 70.7, 285.3 wide.
        var label = Bounds(Part<Control>(start, "PART_Label"), scene.Window);
        AssertNear(16, label.X);
        AssertNear(70.7, Bounds(Slider(start), scene.Window).X, 1);
        AssertNear(285.3, Bounds(Slider(start), scene.Window).Width, 1);

        // r4: the label above at 0.75, from the start corner, the slider 35 px below its top.
        var stackedLabel = Bounds(Part<Control>(stacked, "PART_Label"), scene.Window);
        var stackedRange = Bounds(stacked, scene.Window);
        AssertNear(34, stackedLabel.X);
        AssertNear(stackedRange.Y + 10, stackedLabel.Y);
        AssertNear(241.5, stackedLabel.Width, 1);
        AssertNear(stackedRange.Y + 45, Bounds(Slider(stacked), scene.Window).Y, 1);
        AssertNear(87, stackedRange.Height, 1);

        // r5: 100 px, wrapping, and the slider from 132.
        Assert.Equal(100, Part<Control>(fixedLabel, "PART_Label").Bounds.Width);
        Assert.True(Part<TextBlock>(fixedLabel, "PART_LabelText").Bounds.Height > 40);
        AssertNear(132, Bounds(Slider(fixedLabel), scene.Window).X, 1);

        // r6: the label after the slider, 16 px after it.
        var endLabel = Bounds(Part<Control>(end, "PART_Label"), scene.Window);
        var endSlider = Bounds(Slider(end), scene.Window);
        AssertNear(34, endSlider.X);
        AssertNear(endSlider.Right + 16, endLabel.X, 1);

        // No label: no room.
        Assert.False(Part<Control>(none, "PART_Label").IsVisible);
    });

    [AvaloniaFact]
    public void R13_A_Long_Label_Squeezes_The_Slider_To_Nothing() => InstalledTheme.Run(_ =>
    {
        var range = new AvonicRange { Label = "A label that is long enough to need wrapping around", Value = 40 };
        using var scene = InItems(range);

        // Measured r12: the label 324 wide, on two lines, and the slider 0.
        AssertNear(324, Part<Control>(range, "PART_Label").Bounds.Width, 1);
        AssertNear(0, Slider(range).Bounds.Width, 0.5);
    });

    [AvaloniaFact]
    public void R13_Label_Beats_The_Slot_And_A_Slotted_Label_Is_One_Line_Of_At_Most_200_Px() => InstalledTheme.Run(_ =>
    {
        var range = new AvonicRange { Label = "Text", LabelContent = new TextBlock { Text = "Slot" } };
        var slotted = new AvonicRange { LabelContent = new TextBlock { Text = "A label in the slot that is much too long for 200 px" } };
        using var scene = InItems(range, slotted);

        Assert.True(Part<TextBlock>(range, "PART_LabelText").IsVisible);
        Assert.False(Part<ContentPresenter>(range, "PART_LabelContent").IsVisible);
        Assert.Contains(":range-item-end-adjustment", range.Classes);
        Assert.DoesNotContain(":range-item-start-adjustment", range.Classes);

        var presenter = Part<ContentPresenter>(slotted, "PART_LabelContent");
        Assert.True(presenter.IsVisible);
        AssertNear(200, presenter.Bounds.Width, 1);
        Assert.True(presenter.Bounds.Height < 25);

        // label = "" is still a label.
        range.Label = "";
        Assert.DoesNotContain(":label-text-wrapper-hidden", range.Classes);
    });

    // ── 14: slots ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R14_The_Slots_Are_14_Px_From_The_Slider_With_12_Px_Text_And_24_Px_Icons() => InstalledTheme.Run(_ =>
    {
        var text = new AvonicRange { Value = 40, StartContent = "Low", EndContent = "High" };
        var icons = new AvonicRange { Value = 40, StartContent = new AvonicIcon { IconName = "sunny-outline" }, EndContent = new AvonicIcon { IconName = "sunny" } };
        using var scene = InItems(text, icons);

        var start = Part<ContentPresenter>(text, "PART_Start");
        Assert.Equal(12, Assert.IsType<TextBlock>(start.Child).FontSize);
        var slider = Bounds(Slider(text), scene.Window);
        AssertNear(slider.X - 14, Bounds(start, scene.Window).Right, 0.5);
        AssertNear(slider.Right + 14, Bounds(Part<ContentPresenter>(text, "PART_End"), scene.Window).X, 0.5);

        // Measured r8: 24 px icons, the slider from 54 to 336.
        var icon = (AvonicIcon)icons.StartContent!;
        Assert.Equal(24, icon.Bounds.Width);
        AssertNear(54, Bounds(Slider(icons), scene.Window).X, 1);
        AssertNear(282, Bounds(Slider(icons), scene.Window).Width, 1);
    });

    [AvaloniaFact]
    public void R14_Each_Slot_Takes_Its_Template()
    {
        static IDataTemplate Template(string prefix) =>
            new FuncDataTemplate<string>((text, _) => new TextBlock { Text = prefix + text });

        var range = new AvonicRange
        {
            StartContent = "a", StartContentTemplate = Template("start "),
            EndContent = "b", EndContentTemplate = Template("end "),
            LabelContent = "c", LabelContentTemplate = Template("label "),
        };
        using var scene = Stub(range);

        Assert.Equal("start a", Assert.IsType<TextBlock>(Part<ContentPresenter>(range, "PART_Start").Child).Text);
        Assert.Equal("end b", Assert.IsType<TextBlock>(Part<ContentPresenter>(range, "PART_End").Child).Text);
        Assert.Equal("label c", Assert.IsType<TextBlock>(Part<ContentPresenter>(range, "PART_LabelContent").Child).Text);
    }

    // ── 15: RTL ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R15_RTL_Mirrors_The_Knob_And_Counts_A_Press_From_The_Right()
    {
        var range = new AvonicRange { Value = 25, FlowDirection = FlowDirection.RightToLeft };
        using var scene = Stub(range);

        // 25 % from the right.
        AssertNear(292.5 - 18, Bounds(HandleA(range), scene.Window).X);

        Click(scene, At(range, scene, 0.1));
        Assert.Equal(new RangeValue(90), range.Value);
    }

    [AvaloniaFact]
    public void R15_A_Drag_In_RTL_Counts_From_The_Right()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 50, FlowDirection = FlowDirection.RightToLeft };
        using var scene = Stub(range);
        var mouse = new Mouse(scene, clock);
        var start = At(range, scene, 0.5);

        mouse.Down(start, 0).Move(start - new Point(39, 0), 16).Move(start - new Point(39, 0), 32).Up(start - new Point(39, 0), 48);
        Assert.Equal(new RangeValue(60), range.Value);
    }

    // ── 16: colour ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R16_The_Bars_And_Knob_Are_Primary_And_A_Colour_Replaces_It() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicRange { Value = 50 };
        var danger = new AvonicRange { Value = 50, Color = "danger" };
        using var scene = InItems(plain, danger);

        foreach (var (range, colour) in new[] { (plain, Primary), (danger, Danger) })
        {
            var bar = Bounds(Part<Control>(range, "PART_Bar"), scene.Window);
            AssertClose(colour, Pixel(scene, (int)(bar.X + bar.Width * 0.25), (int)bar.Y));
            AssertClose(OverWhite(colour, 0.26), Pixel(scene, (int)(bar.X + bar.Width * 0.8), (int)bar.Y));
            AssertClose(colour, Pixel(scene, (int)Bounds(Part<Border>(range, "PART_KnobA"), scene.Window).Center.X, (int)bar.Y));

            // An inactive tick, 2 px at 80 %, in the colour over the light bar.
            range.Snaps = true;
            range.Step = 10;
            GestureTests.Render();
            AssertClose(colour, Pixel(scene, (int)(bar.X + bar.Width * 0.8) + 1, (int)bar.Y));
        }
    });

    // ── 17: debounce ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R17_Debounce_Delays_IonInput_To_After_The_Last_Change_And_Not_IonChange()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 50, Debounce = 200 };
        using var scene = Stub(range);
        HandleA(range).Focus(NavigationMethod.Tab);
        Settle();
        var log = Log.Of(range);

        Key(scene, PhysicalKey.ArrowRight);
        clock.At(60);
        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal(["start 50", "change 51", "end 51", "start 51", "change 52", "end 52"], log.Take());

        clock.At(259);
        Assert.Empty(log.All);
        clock.At(260);
        Assert.Equal(["input 52"], log.Take());

        range.Debounce = null;
        range.Value = 10;
        Assert.Equal(["input 10"], log.Take());
    }

    // ── 18: in a content ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R18_A_Drag_Turns_The_Contents_Scroll_Off_And_Back()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 50 };
        var content = new AvonicContent { Content = new StackPanel { Children = { range, new Border { Height = 2000 } } } };
        using var scene = new Scene(content);
        GestureTests.Render();
        var mouse = new Mouse(scene, clock);
        var start = At(range, scene, 0.5);

        mouse.Down(start, 0).Move(start + new Point(30, 0), 16).Move(start + new Point(30, 0), 32);
        clock.At(48);
        GestureTests.Render();
        Assert.False(content.ScrollY);
        mouse.Up(start + new Point(30, 0), 64);
        Assert.True(content.ScrollY);

        // A tap leaves an app's own setting alone.
        content.ScrollY = false;
        Click(scene, At(range, scene, 0.2));
        Assert.False(content.ScrollY);
    }

    [AvaloniaFact]
    public void R18_A_Vertical_Drag_Does_Not_Capture()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 50 };
        using var scene = Stub(range);
        var log = Log.Of(range);
        var start = At(range, scene, 0.8);
        var finger = new GestureTests.Finger(scene, clock);

        finger.Down(start.X, start.Y, 0).Move(start.X, start.Y - 15, 16).Move(start.X, start.Y - 30, 32);
        Assert.Empty(log.All);
        Assert.DoesNotContain(":range-pressed", range.Classes);
    }

    // ── 19: the halo ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R19_The_Halo_Grows_When_Focused_Pressed_Or_Activated() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var range = new AvonicRange { Value = 50 };
        using var scene = InItems(range);
        var halo = Part<Border>(range, "PART_HaloA");
        var knob = Part<Border>(range, "PART_KnobA");

        Assert.Equal(1, Scale(halo), 6);
        Assert.Equal(0.13, halo.Opacity, 6);
        AssertNear(0.67, Scale(knob), 1e-6);

        HandleA(range).Focus(NavigationMethod.Tab);
        Settle();
        Assert.Equal(2, Scale(halo), 6);
        Assert.Equal(0.13, halo.Opacity, 6);
        // No pin: the knob grows to 1.
        Assert.Equal(1, Scale(knob), 6);

        // tap-click: activated after 100 ms of a press on the knob.
        HandleA(range).Focus(NavigationMethod.Unspecified);
        var mouse = new Mouse(scene, clock);
        var centre = Bounds(HandleA(range), scene.Window).Center;
        mouse.Down(centre, 1000);
        clock.At(1101);
        Settle();
        Assert.Contains(":activated", HandleA(range).Classes);
        Assert.Equal(0.25, halo.Opacity, 6);
        mouse.Up(centre, 1200);

    });

    [AvaloniaFact]
    public void R19_A_Mouse_Over_The_Knob_Grows_Its_Halo() => InstalledTheme.Run(_ =>
    {
        var range = new AvonicRange { Value = 50 };
        using var scene = InItems(range);
        var halo = Part<Border>(range, "PART_HaloA");

        scene.Window.MouseMove(Bounds(HandleA(range), scene.Window).Center, RawInputModifiers.None);
        Settle();
        Assert.False(HandleA(range).IsFocused);
        Assert.Contains(":ion-hover", HandleA(range).Classes);
        Assert.Equal(2, Scale(halo), 6);
        Assert.Equal(0.13, halo.Opacity, 6);
    });

    // ── 20: automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R20_Each_Knob_Is_A_Slider_Named_As_Chromium_Names_It()
    {
        var label = new AvonicRange { Label = "Label", Value = 40 };
        var pin = new AvonicRange { Label = "Pin", Pin = true, Value = 60 };
        var slots = new AvonicRange { Value = 40, StartContent = "Low", EndContent = "High" };
        var aria = new AvonicRange { Label = "Both", Value = 40 };
        AutomationProperties.SetName(aria, "Aria");
        var dual = new AvonicRange { Label = "Dual", DualKnobs = true, Value = new RangeValue(20, 80) };
        var disabled = new AvonicRange { Label = "Dis", IsEnabled = false, Value = 40 };
        using var scene = new Scene(new StackPanel { Children = { label, pin, slots, aria, dual, disabled } });
        GestureTests.Render();

        AutomationPeer Peer(RangeKnobHandle handle) => ControlAutomationPeer.CreatePeerForElement(handle);

        Assert.Equal(AutomationControlType.Slider, Peer(HandleA(label)).GetAutomationControlType());
        Assert.Equal("Label", Peer(HandleA(label)).GetName());
        Assert.Equal("Pin 60", Peer(HandleA(pin)).GetName());
        Assert.Equal("Low High", Peer(HandleA(slots)).GetName());
        Assert.Equal("Aria", Peer(HandleA(aria)).GetName());
        Assert.Equal("Dual 80", Peer(HandleA(dual)).GetName());
        Assert.Equal("Dual 20", Peer(HandleB(dual)).GetName());

        var value = Assert.IsAssignableFrom<IRangeValueProvider>(Peer(HandleA(label)));
        Assert.Equal((0, 100, 40), (value.Minimum, value.Maximum, value.Value));
        Assert.False(value.IsReadOnly);
        value.SetValue(90);
        Assert.Equal(new RangeValue(41), label.Value);
        Assert.True(Assert.IsAssignableFrom<IRangeValueProvider>(Peer(HandleA(disabled))).IsReadOnly);
    }

    // ── 21: min, max and step ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R21_Min_Max_And_Step_That_Are_Not_Finite_Go_Back_To_Their_Defaults()
    {
        var range = new AvonicRange { Min = double.NaN, Max = double.PositiveInfinity, Step = double.NaN };
        Assert.Equal((0, 100, 1), (range.Min, range.Max, range.Step));
    }

    [AvaloniaFact]
    public void R21_A_Change_Of_Min_Moves_The_Knob_Not_The_Value()
    {
        var range = new AvonicRange { Value = 50 };
        using var scene = Stub(range);
        range.Min = 40;
        GestureTests.Render();

        Assert.Equal(new RangeValue(50), range.Value);
        AssertNear(390 / 6.0 - 18, Bounds(HandleA(range), scene.Window).X);
    }

    [AvaloniaFact]
    public void R21_A_Fractional_Step_Rounds_To_Its_Decimals_And_Step_0_To_Min_And_Max()
    {
        var range = new AvonicRange { Min = 0, Max = 1, Step = 0.1, Value = 0.3 };
        using var scene = Stub(range);

        Click(scene, At(range, scene, 0.666));
        Assert.Equal(new RangeValue(0.7), range.Value);

        // Measured step 0: 33 at a third, not 33.33.
        range.Max = 100;
        range.Step = 0;
        Click(scene, At(range, scene, 1 / 3.0));
        Assert.Equal(new RangeValue(33), range.Value);
    }

    [AvaloniaFact]
    public void R21_A_Tiny_Step_Keeps_Its_Decimals_As_JavaScript_Writes_Them()
    {
        var range = new AvonicRange { Min = 0, Max = 0.001, Step = 0.00001, Value = 0 };
        using var scene = Stub(range);

        // 0.00001 is "0.00001" in JavaScript, five places, where .NET writes 1E-05.
        Click(scene, At(range, scene, 0.5));
        Assert.Equal(new RangeValue(0.0005), range.Value);
    }

    // ── 22: two knobs snap on release ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R22_With_Two_Knobs_A_Release_Puts_Each_On_Its_Value()
    {
        using var clock = new TestClock();
        var range = new AvonicRange { DualKnobs = true, Value = new RangeValue(20, 80), Step = 10 };
        using var scene = Stub(range);
        var mouse = new Mouse(scene, clock);
        var start = At(range, scene, 0.8);

        // To 0.9 less 4 px: the knob follows exactly while dragging, and snaps to 90 % at the release.
        var to = At(range, scene, 0.9) - new Point(4, 0);
        mouse.Down(start, 0).Move(start + new Point(20, 0), 16).Move(to, 32);
        clock.At(48);
        GestureTests.Render();
        AssertNear(to.X - 18, Bounds(HandleB(range), scene.Window).X);
        mouse.Up(to, 64);
        AssertNear(351 - 18, Bounds(HandleB(range), scene.Window).X);
    }

    // ── 23: host classes ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R23_The_Host_And_Knob_Classes_Follow_Ionics()
    {
        var range = new AvonicRange { Value = 0, Pin = true, DualKnobs = true };
        using var scene = Stub(range);

        Assert.Contains(":range-dual-knobs", range.Classes);
        Assert.Contains(":range-has-pin", range.Classes);
        Assert.Contains(":range-value-min", range.Classes);
        Assert.Contains(":label-placement-start", range.Classes);
        Assert.Contains(":range-knob-min", HandleA(range).Classes);

        range.Value = new RangeValue(10, 100);
        Assert.Contains(":range-value-max", range.Classes);
        Assert.DoesNotContain(":range-value-min", range.Classes);
        Assert.Contains(":range-knob-max", HandleB(range).Classes);

        range.LabelPlacement = RangeLabelPlacement.Stacked;
        Assert.Contains(":label-placement-stacked", range.Classes);
        Assert.DoesNotContain(":label-placement-start", range.Classes);
    }

    private sealed class Volume : System.ComponentModel.INotifyPropertyChanged
    {
        private int _level = 30;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                PropertyChanged?.Invoke(this, new(nameof(Level)));
            }
        }
    }

    [AvaloniaFact]
    public void RangeValue_Binds_Both_Ways_To_An_Int()
    {
        var model = new Volume();
        var range = new AvonicRange { DataContext = model };
        range.Bind(AvonicRange.ValueProperty, new Avalonia.Data.Binding(nameof(Volume.Level)));
        using var scene = Stub(range);
        Assert.Equal(new RangeValue(30), range.Value);

        Click(scene, At(range, scene, 0.6));
        Assert.Equal(60, model.Level);
    }

    // ── RangeValue ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void RangeValue_Reads_A_Number_Or_A_Pair_From_Xaml_Text_And_Converts_A_Number()
    {
        Assert.Equal(new RangeValue(40), RangeValue.Parse("40"));
        Assert.Equal(new RangeValue(20, 80), RangeValue.Parse("20, 80"));
        Assert.Equal("20, 80", new RangeValue(20, 80).ToString());
        Assert.NotEqual(new RangeValue(80), new RangeValue(0, 80));
        RangeValue fromNumber = 12.5;
        Assert.Equal(12.5, (double)fromNumber);
        Assert.Equal((0, 80), (new RangeValue(80).Lower, new RangeValue(80).Upper));
    }
}
