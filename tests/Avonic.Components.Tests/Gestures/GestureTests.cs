using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Gestures;

namespace Avonic.Components.Tests.Gestures;

/// <summary>
/// Ionic's <c>createGesture</c> and gesture controller, with real headless input on the test clock. The expected numbers are
/// what Ionic's own <c>createGesture</c> gave for the same events, under "Measured: Ionic's numbers" in docs/infra/gesture/PLAN.md.
/// </summary>
public class GestureTests
{
    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────

    /// <summary>What every callback saw, copied, as the detail is one object updated in place.</summary>
    internal sealed record Seen(
        string Name,
        double At,
        double StartX,
        double StartY,
        double StartTime,
        double CurrentX,
        double CurrentY,
        double CurrentTime,
        double DeltaX,
        double DeltaY,
        double VelocityX,
        double VelocityY,
        PointerEventArgs? Event)
    {
        public override string ToString() =>
            $"{Name} t={At} start=({StartX},{StartY},{StartTime}) cur=({CurrentX},{CurrentY},{CurrentTime}) delta=({DeltaX},{DeltaY}) v=({VelocityX},{VelocityY})";
    }

    /// <summary>
    /// The test clock, and the gesture's event times taken from it, as headless input's timestamps can't be set. The default, the
    /// events' own timestamps, has its own test.
    /// </summary>
    internal sealed class GestureClock : IDisposable
    {
        private readonly TestClock _clock = new();
        private readonly Func<PointerEventArgs, double> _previous = Gesture.EventTime;

        public GestureClock() => Gesture.EventTime = _ => AvonicConfig.Now().TotalMilliseconds;

        public TimeSpan Now => _clock.Now;

        public TimeSpan At(double milliseconds) => _clock.At(milliseconds);

        public void Dispose()
        {
            Gesture.EventTime = _previous;
            _clock.Dispose();
        }
    }

    internal sealed class Log(GestureClock clock)
    {
        public List<Seen> All { get; } = [];

        public IEnumerable<string> Names => All.Select(seen => seen.Name);

        public Seen Single(string name) => Assert.Single(All, seen => seen.Name == name);

        public void Add(string name, GestureDetail? detail = null)
        {
            var at = clock.Now.TotalMilliseconds;
            All.Add(detail is null
                ? new Seen(name, at, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null)
                : new Seen(name, at, detail.StartX, detail.StartY, detail.StartTime, detail.CurrentX, detail.CurrentY, detail.CurrentTime,
                    detail.DeltaX, detail.DeltaY, detail.VelocityX, detail.VelocityY, detail.Event));
        }
    }

    internal static Gesture Make(
        InputElement element,
        Log log,
        string name = "g",
        int priority = 0,
        GestureDirection direction = GestureDirection.X,
        double threshold = 10,
        double maxAngle = 40,
        bool disableScroll = false,
        bool blurOnStart = false,
        Func<GestureDetail, bool>? canStart = null,
        Func<GestureDetail, Task>? onWillStart = null,
        bool enable = true)
    {
        var gesture = Gesture.Create(new GestureConfig
        {
            Element = element,
            GestureName = name,
            GesturePriority = priority,
            Direction = direction,
            Threshold = threshold,
            MaxAngle = maxAngle,
            DisableScroll = disableScroll,
            BlurOnStart = blurOnStart,
            CanStart = detail =>
            {
                log.Add($"{name}.canStart");
                return canStart?.Invoke(detail) ?? true;
            },
            OnWillStart = onWillStart,
            OnStart = detail => log.Add($"{name}.onStart", detail),
            OnMove = detail => log.Add($"{name}.onMove", detail),
            OnEnd = detail => log.Add($"{name}.onEnd", detail),
            NotCaptured = detail => log.Add($"{name}.notCaptured", detail),
        });
        if (enable)
        {
            gesture.Enable();
        }

        return gesture;
    }

    /// <summary>A box at the window's top left, so that its coordinates are the window's.</summary>
    internal static Border Box(double width = 390, double height = 300) => new()
    {
        Width = width,
        Height = height,
        Background = Brushes.Transparent,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
    };

    internal static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>Hit tests read the last rendered scene.</summary>
    internal static void Render()
    {
        Settle();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();
    }

    /// <summary>A finger on the window, each event at its own time on the clock.</summary>
    internal sealed class Finger(Scene scene, GestureClock clock)
    {
        private IHeadlessTouchPointer? _pointer;

        public Finger Down(double x, double y, double at)
        {
            clock.At(at);
            Render();
            _pointer = scene.Window.TouchBegin(new Point(x, y), RawInputModifiers.None);
            Settle();
            return this;
        }

        public Finger Move(double x, double y, double at)
        {
            clock.At(at);
            scene.Window.TouchMove(_pointer!, new Point(x, y), RawInputModifiers.None);
            Settle();
            return this;
        }

        public Finger Up(double x, double y, double at)
        {
            clock.At(at);
            scene.Window.TouchEnd(_pointer!, new Point(x, y), RawInputModifiers.None);
            Settle();
            return this;
        }
    }

    private static void AssertSeen(Seen seen, double at, (double X, double Y, double T) start, (double X, double Y, double T) current,
        (double X, double Y) delta, (double X, double Y) velocity)
    {
        var because = seen.ToString();
        Assert.True(at == seen.At, $"time: {because}");
        Assert.True(Near(start.X, seen.StartX) && Near(start.Y, seen.StartY) && start.T == seen.StartTime, $"start: {because}");
        Assert.True(Near(current.X, seen.CurrentX) && Near(current.Y, seen.CurrentY) && current.T == seen.CurrentTime, $"current: {because}");
        Assert.True(Near(delta.X, seen.DeltaX) && Near(delta.Y, seen.DeltaY), $"delta: {because}");
        Assert.True(Near(velocity.X, seen.VelocityX) && Near(velocity.Y, seen.VelocityY), $"velocity: {because}");
    }

    private static bool Near(double expected, double actual) => Math.Abs(expected - actual) < 5e-6;

    // ── G1: enable ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G1_A_Gesture_Starts_Disabled_And_Enable_Turns_It_On()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, enable: false);

        new Finger(scene, clock).Down(100, 100, 1000).Move(130, 100, 1016).Up(130, 100, 1032);
        Assert.Empty(log.All);

        gesture.Enable();
        new Finger(scene, clock).Down(100, 100, 2000).Move(130, 100, 2016).Up(130, 100, 2032);
        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
    }

    [AvaloniaFact]
    public void G1_Disabling_A_Captured_Gesture_Ends_It_With_The_Last_Detail_And_Ignores_The_Rest_Of_The_Press()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        var finger = new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Move(130, 100, 1032);
        clock.At(1048);
        gesture.Enable(false);
        finger.Move(140, 100, 1064).Up(140, 100, 1080);

        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onEnd"], log.Names);
        AssertSeen(log.Single("g.onEnd"), 1048, (120, 100, 1016), (130, 100, 1032), (10, 0), (0.7, 0));

        gesture.Enable();
        new Finger(scene, clock).Down(100, 100, 2000).Move(120, 100, 2016).Up(120, 100, 2032);
        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onEnd", "g.canStart", "g.onStart", "g.onEnd"], log.Names);
        AssertSeen(log.All[^1], 2032, (120, 100, 2016), (120, 100, 2032), (0, 0), (0.2625, 0));
    }

    // ── G2: threshold ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G2_Nothing_Below_The_Threshold_And_At_It_The_Start_Resets_To_The_Capturing_Move()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        var finger = new Finger(scene, clock).Down(100, 100, 1000).Move(109.9, 100, 1016);
        Assert.Equal(["g.canStart"], log.Names);

        finger.Move(110, 100, 1032).Move(130, 105, 1048).Up(130, 105, 1080);

        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onEnd"], log.Names);
        // The start is the capturing move, but the delta is still that move's until the next one recalculates it.
        AssertSeen(log.Single("g.onStart"), 1032, (110, 100, 1032), (110, 100, 1032), (10, 0), (0.134312, 0));
        AssertSeen(log.Single("g.onMove"), 1064, (110, 100, 1032), (130, 105, 1048), (20, 5), (0.915294, 0.21875));
        AssertSeen(log.Single("g.onEnd"), 1080, (110, 100, 1032), (130, 105, 1080), (20, 5), (0.274588, 0.065625));
    }

    // ── G3: direction ────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(39, true)]
    [InlineData(41, false)]
    [InlineData(180 - 39, true)]
    [InlineData(180 - 41, false)]
    public void G3_Within_MaxAngle_Of_The_Axis_Captures_Either_Way_Along_It(double degrees, bool captures)
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        var angle = degrees * Math.PI / 180;
        var (x, y) = (200 + 20 * Math.Cos(angle), 100 + 20 * Math.Sin(angle));
        new Finger(scene, clock).Down(200, 100, 1000).Move(x, y, 1016).Up(x, y, 1032);

        Assert.Equal(captures ? ["g.canStart", "g.onStart", "g.onEnd"] : ["g.canStart", "g.notCaptured"], log.Names);
        if (degrees == 41)
        {
            AssertSeen(log.Single("g.notCaptured"), 1016, (200, 100, 1000), (215.094192, 113.121181, 1016), (15.094192, 13.121181),
                (0.660371, 0.574052));
        }
    }

    [AvaloniaFact]
    public void G3_A_Smaller_MaxAngle_Refuses_What_The_Default_Takes()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, maxAngle: 20);

        var angle = 30 * Math.PI / 180;
        var (x, y) = (100 + 20 * Math.Cos(angle), 100 + 20 * Math.Sin(angle));
        new Finger(scene, clock).Down(100, 100, 1000).Move(x, y, 1016).Up(x, y, 1032);

        Assert.Equal(["g.canStart", "g.notCaptured"], log.Names);
    }

    [AvaloniaFact]
    public void G3_The_Wrong_Axis_Aborts_At_Once_And_The_Rest_Of_The_Press_Is_Ignored()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        new Finger(scene, clock).Down(100, 100, 1000).Move(100, 115, 1016).Move(150, 115, 1032).Up(150, 115, 1048);

        Assert.Equal(["g.canStart", "g.notCaptured"], log.Names);
        AssertSeen(log.Single("g.notCaptured"), 1016, (100, 100, 1000), (100, 115, 1016), (0, 15), (0, 0.65625));
    }

    [AvaloniaFact]
    public void G3_Leftward_Along_X_Counts()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        new Finger(scene, clock).Down(100, 100, 1000).Move(88, 101, 1016).Move(60, 101, 1032).Up(60, 101, 1048);

        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onEnd"], log.Names);
        AssertSeen(log.Single("g.onStart"), 1016, (88, 101, 1016), (88, 101, 1016), (-12, 1), (-0.525, 0.04375));
        AssertSeen(log.Single("g.onMove"), 1048, (88, 101, 1016), (60, 101, 1032), (-28, 0), (-1.3825, 0.013125));
        AssertSeen(log.Single("g.onEnd"), 1048, (88, 101, 1016), (60, 101, 1048), (-28, 0), (-0.41475, 0.003938));
    }

    [AvaloniaFact]
    public void G3_Direction_Y_With_Its_Own_Threshold()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, direction: GestureDirection.Y, threshold: 5);

        var finger = new Finger(scene, clock).Down(100, 100, 1000).Move(101, 96, 1016);
        Assert.Equal(["g.canStart"], log.Names);

        finger.Move(101, 94, 1032).Move(101, 60, 1048).Up(101, 60, 1080);

        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onEnd"], log.Names);
        AssertSeen(log.Single("g.onStart"), 1032, (101, 94, 1032), (101, 94, 1032), (1, -6), (0.013125, -0.14));
        AssertSeen(log.Single("g.onMove"), 1064, (101, 94, 1032), (101, 60, 1048), (0, -34), (0.003938, -1.5295));
        AssertSeen(log.Single("g.onEnd"), 1080, (101, 94, 1032), (101, 60, 1080), (0, -34), (0.001181, -0.45885));
    }

    // ── G4, G5: threshold 0, velocity and frames ─────────────────────────────────────────────

    [AvaloniaFact]
    public void G4_G5_Threshold_Zero_Captures_At_The_Press_And_OnMove_Takes_The_First_Move_Of_Each_Frame()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, threshold: 0);

        var finger = new Finger(scene, clock).Down(100, 100, 1000);
        Assert.Equal(["g.canStart", "g.onStart"], log.Names);
        AssertSeen(log.Single("g.onStart"), 1000, (100, 100, 1000), (100, 100, 1000), (0, 0), (0, 0));

        // Four moves inside one frame: the first is the frame's, and the others are dropped, not merged.
        finger.Move(108, 100, 1004).Move(116, 100, 1008).Move(124, 100, 1012).Move(132, 100, 1016)
            .Move(140, 100, 1024).Move(148, 100, 1048)
            // A gap of 120 ms keeps the velocity, as does the move dropped at the same time.
            .Move(168, 110, 1168).Move(173, 110, 1200).Move(178, 110, 1200).Up(178, 110, 1240);

        var moves = log.All.Where(seen => seen.Name == "g.onMove").ToList();
        Assert.Equal(5, moves.Count);
        AssertSeen(moves[0], 1020, (100, 100, 1000), (108, 100, 1004), (8, 0), (1.4, 0));
        AssertSeen(moves[1], 1040, (100, 100, 1000), (140, 100, 1024), (40, 0), (1.54, 0));
        AssertSeen(moves[2], 1064, (100, 100, 1000), (148, 100, 1048), (48, 0), (0.695333, 0));
        AssertSeen(moves[3], 1184, (100, 100, 1000), (168, 110, 1168), (68, 10), (0.695333, 0));
        AssertSeen(moves[4], 1216, (100, 100, 1000), (173, 110, 1200), (73, 10), (0.317975, 0));
        AssertSeen(log.Single("g.onEnd"), 1240, (100, 100, 1000), (178, 110, 1240), (78, 10), (0.182893, 0));
    }

    [AvaloniaFact]
    public void G5_A_Move_At_The_Same_Time_And_One_After_100_Ms_Leave_The_Velocity_Alone()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        new Finger(scene, clock).Down(100, 100, 1000).Move(103, 100, 1016).Move(106, 100, 1016).Move(107, 100, 1200).Up(107, 100, 1216);

        AssertSeen(log.Single("g.notCaptured"), 1216, (100, 100, 1000), (107, 100, 1216), (7, 0), (0.039375, 0));
    }

    // ── G6: release before the threshold ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G6_A_Release_Before_The_Threshold_Is_NotCaptured_At_The_Release()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        new Finger(scene, clock).Down(100, 100, 1000).Move(103, 100, 1016).Up(105, 101, 1032);

        Assert.Equal(["g.canStart", "g.notCaptured"], log.Names);
        var seen = log.Single("g.notCaptured");
        AssertSeen(seen, 1032, (100, 100, 1000), (105, 101, 1032), (5, 1), (0.126875, 0.04375));
        Assert.IsType<PointerReleasedEventArgs>(seen.Event);
    }

    [AvaloniaFact]
    public void G6_A_Mouse_Works_As_A_Finger_Does()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        clock.At(1000);
        Render();
        scene.Window.MouseDown(new Point(100, 100), MouseButton.Left, RawInputModifiers.None);
        clock.At(1016);
        scene.Window.MouseMove(new Point(120, 100), RawInputModifiers.LeftMouseButton);
        clock.At(1032);
        scene.Window.MouseUp(new Point(120, 100), MouseButton.Left, RawInputModifiers.None);
        Settle();

        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
        AssertSeen(log.Single("g.onEnd"), 1032, (120, 100, 1016), (120, 100, 1032), (0, 0), (0.2625, 0));
    }

    // ── G7: canStart and the controller refusing ─────────────────────────────────────────────

    [AvaloniaFact]
    public void G7_CanStart_False_Ignores_The_Press_With_No_NotCaptured()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, canStart: _ => false);

        new Finger(scene, clock).Down(100, 100, 1000).Move(130, 100, 1016).Up(130, 100, 1032);

        Assert.Equal(["g.canStart"], log.Names);
    }

    [AvaloniaFact]
    public void G7_While_One_Gesture_Is_Captured_Another_Is_Asked_CanStart_And_Then_Refused()
    {
        using var clock = new GestureClock();
        var a = Box(150, 200);
        var b = Box(150, 200);
        b.HorizontalAlignment = HorizontalAlignment.Right;
        using var scene = new Scene(new Panel { Width = 390, Children = { a, b } });
        var log = new Log(clock);
        using var ga = Make(a, log, name: "a", threshold: 0);
        using var gb = Make(b, log, name: "b", threshold: 0);

        var first = new Finger(scene, clock).Down(100, 100, 1000);
        Assert.True(GestureController.Instance.IsCaptured);
        new Finger(scene, clock).Down(300, 100, 1016).Move(330, 100, 1032).Up(330, 100, 1048);
        first.Up(100, 100, 1064);

        Assert.Equal(["a.canStart", "a.onStart", "b.canStart", "a.onEnd"], log.Names);
        Assert.False(GestureController.Instance.IsCaptured);
    }

    // ── G8: priority ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G8_The_Lower_Priority_Reaching_Its_Threshold_First_Loses_And_The_Higher_Captures_Later()
    {
        using var clock = new GestureClock();
        var inner = Box(200, 200);
        var outer = new Border { Width = 390, Height = 300, Background = Brushes.Transparent, Child = inner, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(outer);
        var log = new Log(clock);
        using var go = Make(outer, log, name: "outer", priority: 30, threshold: 5);
        using var gi = Make(inner, log, name: "inner", priority: 100, threshold: 20);

        new Finger(scene, clock).Down(100, 100, 1000).Move(110, 100, 1016).Move(125, 100, 1032).Up(125, 100, 1048);

        Assert.Equal(["inner.canStart", "outer.canStart", "outer.notCaptured", "inner.onStart", "inner.onEnd"], log.Names);
        AssertSeen(log.Single("outer.notCaptured"), 1016, (100, 100, 1000), (110, 100, 1016), (10, 0), (0.4375, 0));
        AssertSeen(log.Single("inner.onStart"), 1032, (125, 100, 1032), (125, 100, 1032), (25, 0), (0.7875, 0));
    }

    [AvaloniaFact]
    public void G8_On_A_Tie_The_Gesture_Created_Later_Wins()
    {
        using var clock = new GestureClock();
        var inner = Box(200, 200);
        var outer = new Border { Width = 390, Height = 300, Background = Brushes.Transparent, Child = inner, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(outer);
        var log = new Log(clock);
        using var first = Make(inner, log, name: "first", priority: 50);
        using var second = Make(outer, log, name: "second", priority: 50);

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Up(120, 100, 1032);

        Assert.Equal(["first.canStart", "second.canStart", "first.notCaptured", "second.onStart", "second.onEnd"], log.Names);
    }

    [AvaloniaFact]
    public void G8_A_Threshold_Zero_Gesture_That_Loses_At_The_Press_Can_Start_Again()
    {
        // A deviation: Ionic leaves hasStartedPan set here, and the gesture never starts again until it's disabled.
        using var clock = new GestureClock();
        var inner = Box(200, 200);
        var outer = new Border { Width = 390, Height = 300, Background = Brushes.Transparent, Child = inner, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(outer);
        var log = new Log(clock);
        using var hi = Make(inner, log, name: "hi", priority: 100);
        using var zero = Make(outer, log, name: "zero", priority: 10, threshold: 0);

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Up(120, 100, 1032);
        Assert.Equal(["hi.canStart", "zero.canStart", "hi.onStart", "hi.onEnd"], log.Names);

        new Finger(scene, clock).Down(300, 250, 2000).Up(300, 250, 2016);
        Assert.Equal(["hi.canStart", "zero.canStart", "hi.onStart", "hi.onEnd", "zero.canStart", "zero.onStart", "zero.onEnd"], log.Names);
    }

    // ── G9: capture handoff ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G9_Capturing_Takes_The_Pointer_From_A_Pressed_Button_And_Moves_Outside_Keep_Arriving()
    {
        using var clock = new GestureClock();
        var clicks = 0;
        // TestApp has no base theme, so the button gets a template that takes input.
        var button = new Button
        {
            Width = 100,
            Height = 50,
            Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Transparent }),
        };
        button.Click += (_, _) => clicks++;
        var box = Box(200, 100);
        box.Child = button;
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        // The button is at (50, 25) to (150, 75), the box to 200 wide.
        var finger = new Finger(scene, clock).Down(100, 50, 1000);
        Assert.True(button.IsPressed);

        finger.Move(120, 50, 1016);
        Assert.False(button.IsPressed);

        finger.Move(300, 400, 1032).Move(310, 400, 1048);
        clock.At(1064);
        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onMove"], log.Names);
        Assert.Equal((190, 350), (log.All[^1].DeltaX, log.All[^1].DeltaY));

        finger.Up(100, 50, 1080);
        Assert.Equal(["g.canStart", "g.onStart", "g.onMove", "g.onMove", "g.onEnd"], log.Names);
        Assert.Equal(0, clicks);
    }

    [AvaloniaTheory]
    [InlineData("the element")]
    [InlineData("inside it")]
    [InlineData("around it")]
    public void G9_Capturing_Cancels_The_Pressed_Activatable_Surface(string where)
    {
        // Ionic's ionGestureCaptured reaches tap-click's cancelActive, whichever surface is active.
        using var clock = new GestureClock();
        var box = Box(200, 100);
        Control root = box;
        Control surface = box;
        if (where == "inside it")
        {
            surface = new Border { Background = Brushes.Transparent };
            box.Child = surface;
        }
        else if (where == "around it")
        {
            surface = new Border { Background = Brushes.Transparent, Child = box, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            root = surface;
        }

        Activatable.SetIsInstant(surface, true);
        Activatable.SetIsEnabled(surface, true);
        using var scene = new Scene(root);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        var finger = new Finger(scene, clock).Down(100, 50, 1000);
        Assert.Contains(Activatable.ActivatedPseudoClass, surface.Classes);

        finger.Move(105, 50, 1016);
        Assert.Contains(Activatable.ActivatedPseudoClass, surface.Classes);

        finger.Move(120, 50, 1032);
        Assert.Equal(["g.canStart", "g.onStart"], log.Names);
        Assert.DoesNotContain(Activatable.ActivatedPseudoClass, surface.Classes);

        finger.Up(120, 50, 1048);
        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
        Assert.DoesNotContain(Activatable.ActivatedPseudoClass, surface.Classes);
    }

    [AvaloniaFact]
    public void G9_An_Outer_Gesture_Still_Deciding_Is_Not_Cancelled_By_The_Handoff()
    {
        // Ionic's outer gesture keeps getting moves, and gives up when the controller refuses it at its own threshold.
        using var clock = new GestureClock();
        var inner = Box(200, 200);
        var outer = new Border { Width = 390, Height = 300, Background = Brushes.Transparent, Child = inner, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(outer);
        var log = new Log(clock);
        using var go = Make(outer, log, name: "outer", priority: 30, threshold: 30);
        using var gi = Make(inner, log, name: "inner", priority: 100, threshold: 10);

        var finger = new Finger(scene, clock).Down(100, 100, 1000).Move(110, 100, 1016);
        Assert.Equal(["inner.canStart", "outer.canStart", "inner.onStart"], log.Names);

        finger.Move(125, 100, 1032);
        Assert.Equal(["inner.canStart", "outer.canStart", "inner.onStart"], log.Names);

        finger.Move(135, 100, 1048);
        Assert.Equal(["inner.canStart", "outer.canStart", "inner.onStart", "inner.onMove", "outer.notCaptured"], log.Names);
        Assert.Equal(35, log.Single("outer.notCaptured").DeltaX);
        finger.Up(135, 100, 1064);
    }

    [AvaloniaFact]
    public void G9_A_Gesture_Inside_The_One_That_Captures_Is_NotCaptured_At_The_Capture_And_Can_Start_Again()
    {
        // The pointer is the outer element's, so the inner one gets no more of it. Ionic's inner gesture goes on getting moves, and
        // is refused at its own threshold: the same callback, sooner.
        using var clock = new GestureClock();
        var inner = Box(200, 200);
        var outer = new Border { Width = 390, Height = 300, Background = Brushes.Transparent, Child = inner, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(outer);
        var log = new Log(clock);
        using var go = Make(outer, log, name: "outer", priority: 100, threshold: 10);
        using var gi = Make(inner, log, name: "inner", priority: 30, threshold: 30);

        new Finger(scene, clock).Down(100, 100, 1000).Move(110, 100, 1016).Move(150, 100, 1032).Up(150, 100, 1048);
        Assert.Equal(["inner.canStart", "outer.canStart", "inner.notCaptured", "outer.onStart", "outer.onMove", "outer.onEnd"], log.Names);
        Assert.Equal(10, log.Single("inner.notCaptured").DeltaX);

        new Finger(scene, clock).Down(100, 100, 2000).Move(100, 140, 2016).Up(100, 140, 2032);
        Assert.Equal(["inner.canStart", "outer.canStart", "inner.notCaptured", "outer.onStart", "outer.onMove", "outer.onEnd",
            "inner.canStart", "outer.canStart", "inner.notCaptured", "outer.notCaptured"], log.Names);
    }

    // ── G10: cancel on capture lost ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G10_Another_Element_Taking_The_Pointer_Ends_A_Captured_Gesture_With_OnEnd()
    {
        using var clock = new GestureClock();
        var box = Box(200, 100);
        var other = Box(100, 100);
        other.HorizontalAlignment = HorizontalAlignment.Right;
        using var scene = new Scene(new Panel { Width = 390, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { box, other } });
        var log = new Log(clock);
        using var gesture = Make(box, log);
        IPointer? pointer = null;
        box.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, handledEventsToo: true);

        var finger = new Finger(scene, clock).Down(100, 50, 1000).Move(120, 50, 1016).Move(130, 50, 1032);
        clock.At(1040);
        pointer!.Capture(other);
        Settle();

        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
        AssertSeen(log.Single("g.onEnd"), 1040, (120, 50, 1016), (130, 50, 1032), (10, 0), (0.7, 0));
        Assert.False(GestureController.Instance.IsCaptured);

        finger.Move(150, 50, 1056).Up(150, 50, 1072);
        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
    }

    [AvaloniaFact]
    public void G10_The_Platform_Cancelling_The_Touch_Ends_An_Undecided_Gesture_With_NotCaptured()
    {
        using var clock = new GestureClock();
        var box = Box(200, 100);
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);
        Pointer? pointer = null;
        box.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer as Pointer, handledEventsToo: true);

        var finger = new Finger(scene, clock).Down(100, 50, 1000).Move(104, 50, 1016);
        clock.At(1032);
        // What the touch device does with a TouchCancel.
        pointer!.Dispose();
        Settle();

        Assert.Equal(["g.canStart", "g.notCaptured"], log.Names);
        Assert.Equal(4, log.Single("g.notCaptured").DeltaX);

        // The headless device still has the finger, so lift it.
        finger.Up(104, 50, 1048);
        Assert.Equal(["g.canStart", "g.notCaptured"], log.Names);
    }

    [AvaloniaFact]
    public void G10_The_Element_Leaving_The_Tree_Ends_The_Gesture()
    {
        using var clock = new GestureClock();
        var box = Box(200, 100);
        var holder = new Panel { Children = { box } };
        using var scene = new Scene(holder);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        new Finger(scene, clock).Down(100, 50, 1000).Move(130, 50, 1016);
        holder.Children.Clear();
        Settle();

        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
        Assert.False(GestureController.Instance.IsCaptured);
    }

    [AvaloniaFact]
    public void G10_A_Child_Capturing_On_Its_Own_Press_Is_The_Same_Press()
    {
        using var clock = new GestureClock();
        var child = Box(100, 50);
        var box = Box(200, 100);
        box.Child = child;
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);
        // The press lands on the box, and then a child takes the pointer, inside the element.
        box.AddHandler(InputElement.PointerPressedEvent, (_, e) => e.Pointer.Capture(child), handledEventsToo: true);

        new Finger(scene, clock).Down(180, 90, 1000).Move(190, 90, 1016).Up(190, 90, 1032);

        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
    }

    [AvaloniaFact]
    public void G10_A_Capture_At_The_Press_Taken_Away_In_OnStart_Ends_The_Gesture()
    {
        using var clock = new GestureClock();
        var box = Box(200, 100);
        var other = Box(100, 100);
        other.HorizontalAlignment = HorizontalAlignment.Right;
        using var scene = new Scene(new Panel { Width = 390, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Children = { box, other } });
        var log = new Log(clock);
        var takeAway = true;
        using var gesture = Gesture.Create(new GestureConfig
        {
            Element = box,
            GestureName = "g",
            Threshold = 0,
            OnStart = detail =>
            {
                log.Add("g.onStart", detail);
                if (takeAway)
                {
                    detail.Event!.Pointer.Capture(other);
                }
            },
            OnEnd = detail => log.Add("g.onEnd", detail),
            NotCaptured = detail => log.Add("g.notCaptured", detail),
        });
        gesture.Enable();

        // The cancel comes before onStart has returned, so, as Ionic's pointerUp does then, it fires nothing, and lets go.
        new Finger(scene, clock).Down(100, 50, 1000).Move(120, 50, 1016).Up(120, 50, 1032);
        Assert.Equal(["g.onStart"], log.Names);
        Assert.False(GestureController.Instance.IsCaptured);

        takeAway = false;
        new Finger(scene, clock).Down(100, 50, 2000).Up(100, 50, 2016);
        Assert.Equal(["g.onStart", "g.onStart", "g.onEnd"], log.Names);
    }

    [AvaloniaFact]
    public void G10_A_Child_Letting_Go_As_It_Handles_The_Release_Is_Still_The_Release()
    {
        using var clock = new GestureClock();
        var child = Box(100, 50);
        var box = Box(200, 100);
        box.Child = child;
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);
        child.AddHandler(InputElement.PointerReleasedEvent, (_, e) => e.Pointer.Capture(null), handledEventsToo: true);

        new Finger(scene, clock).Down(50, 25, 1000).Move(53, 25, 1016).Up(55, 26, 1032);

        Assert.Equal(["g.canStart", "g.notCaptured"], log.Names);
        var seen = log.Single("g.notCaptured");
        AssertSeen(seen, 1032, (50, 25, 1000), (55, 26, 1032), (5, 1), (0.126875, 0.04375));
        Assert.IsType<PointerReleasedEventArgs>(seen.Event);
    }

    [AvaloniaFact]
    public void G10_After_A_Mouse_Release_The_Next_Press_Can_Still_Be_Cancelled()
    {
        // A mouse is one pointer for every press, so nothing from one release may linger into the next press.
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);
        IPointer? pointer = null;
        box.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, handledEventsToo: true);

        clock.At(1000);
        Render();
        scene.Window.MouseDown(new Point(100, 100), MouseButton.Left, RawInputModifiers.None);
        clock.At(1016);
        scene.Window.MouseUp(new Point(103, 100), MouseButton.Left, RawInputModifiers.None);
        clock.At(2000);
        scene.Window.MouseDown(new Point(100, 100), MouseButton.Left, RawInputModifiers.None);
        clock.At(2016);
        scene.Window.MouseMove(new Point(104, 100), RawInputModifiers.LeftMouseButton);
        clock.At(2032);
        pointer!.Capture(null);
        Settle();

        Assert.Equal(["g.canStart", "g.notCaptured", "g.canStart", "g.notCaptured"], log.Names);
        Assert.Equal(4, log.All[^1].DeltaX);
    }

    // ── G12: disableScroll and blockers ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G12_DisableScroll_Disables_Scroll_From_The_Capture_To_The_Release()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, disableScroll: true);
        var changes = new List<bool>();
        void OnChanged(object? sender, EventArgs e) => changes.Add(GestureController.Instance.IsScrollDisabled);
        GestureController.Instance.ScrollDisabledChanged += OnChanged;
        try
        {
            var finger = new Finger(scene, clock).Down(100, 100, 1000).Move(105, 100, 1016);
            Assert.False(GestureController.Instance.IsScrollDisabled);
            finger.Move(115, 100, 1032);
            Assert.True(GestureController.Instance.IsScrollDisabled);
            finger.Up(115, 100, 1048);
            Assert.False(GestureController.Instance.IsScrollDisabled);
            Assert.Equal([true, false], changes);
        }
        finally
        {
            GestureController.Instance.ScrollDisabledChanged -= OnChanged;
        }
    }

    [AvaloniaFact]
    public void G12_Without_DisableScroll_Capturing_Leaves_Scroll_Alone()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        var finger = new Finger(scene, clock).Down(100, 100, 1000).Move(115, 100, 1016);
        Assert.True(GestureController.Instance.IsCaptured);
        Assert.False(GestureController.Instance.IsScrollDisabled);
        finger.Up(115, 100, 1032);
    }

    [AvaloniaFact]
    public void G12_A_Blocker_Disables_A_Gesture_By_Name_And_Scroll_While_Blocked()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, name: "a");
        using var other = Make(new Border(), log, name: "b");
        using var blocker = GestureController.Instance.CreateBlocker(["a"], disableScroll: true);

        blocker.Block();
        Assert.True(GestureController.Instance.IsScrollDisabled);
        new Finger(scene, clock).Down(100, 100, 1000).Move(130, 100, 1016).Up(130, 100, 1032);
        Assert.Equal(["a.canStart"], log.Names);

        blocker.Unblock();
        Assert.False(GestureController.Instance.IsScrollDisabled);
        new Finger(scene, clock).Down(100, 100, 2000).Move(130, 100, 2016).Up(130, 100, 2032);
        Assert.Equal(["a.canStart", "a.canStart", "a.onStart", "a.onEnd"], log.Names);

        blocker.Block();
        blocker.Destroy();
        Assert.False(GestureController.Instance.IsScrollDisabled);
        blocker.Block();
        Assert.False(GestureController.Instance.IsScrollDisabled);
    }

    [AvaloniaFact]
    public void G12_A_Blocker_Of_Another_Name_Leaves_A_Gesture_Alone()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, name: "a");
        using var blocker = GestureController.Instance.CreateBlocker(["b"]);

        blocker.Block();
        Assert.False(GestureController.Instance.IsScrollDisabled);
        new Finger(scene, clock).Down(100, 100, 1000).Move(130, 100, 1016).Up(130, 100, 1032);
        Assert.Equal(["a.canStart", "a.onStart", "a.onEnd"], log.Names);
    }

    // ── G13: onWillStart ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G13_OnStart_Waits_For_OnWillStart_And_A_Release_Before_Then_Fires_Nothing()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        var willStart = new TaskCompletionSource();
        using var gesture = Make(box, log, onWillStart: _ =>
        {
            log.Add("onWillStart");
            return willStart.Task;
        });

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Move(130, 100, 1032).Up(130, 100, 1048);
        Assert.Equal(["g.canStart", "onWillStart"], log.Names);

        willStart.SetResult();
        Settle();

        Assert.Equal(["g.canStart", "onWillStart", "g.onStart"], log.Names);
        // The moves after the capture were dropped, and the release didn't recalculate.
        AssertSeen(log.Single("g.onStart"), 1048, (120, 100, 1016), (120, 100, 1016), (20, 0), (0.875, 0));
    }

    [AvaloniaFact]
    public void G13_A_Completed_OnWillStart_Starts_At_Once()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, onWillStart: _ => Task.CompletedTask);

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Move(130, 100, 1032);
        clock.At(1048);

        Assert.Equal(["g.canStart", "g.onStart", "g.onMove"], log.Names);
    }

    // ── G14: blurOnStart ─────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void G14_BlurOnStart_Clears_The_Focus_Before_OnStart(bool blur)
    {
        using var clock = new GestureClock();
        var focused = new TextBox { Width = 100, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        var box = Box();
        using var scene = new Scene(new Panel { Children = { box, focused } });
        var log = new Log(clock);
        bool? focusedAtStart = null;
        using var gesture = Gesture.Create(new GestureConfig
        {
            Element = box,
            GestureName = "g",
            BlurOnStart = blur,
            OnStart = _ => focusedAtStart = focused.IsFocused,
        });
        gesture.Enable();
        focused.Focus();
        Assert.True(focused.IsFocused);

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Up(120, 100, 1032);

        Assert.Equal(!blur, focusedAtStart);
    }

    // ── G15: the captured event ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G15_IonGestureCaptured_Is_Raised_With_The_Name_And_IsCaptured_Holds_To_The_Release()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, name: "item-swipe");
        var captured = new List<string>();
        void OnCaptured(object? sender, GestureCapturedEventArgs e) => captured.Add(e.GestureName);
        GestureController.Instance.IonGestureCaptured += OnCaptured;
        try
        {
            var finger = new Finger(scene, clock).Down(100, 100, 1000).Move(105, 100, 1016);
            Assert.Empty(captured);
            Assert.False(GestureController.Instance.IsCaptured);

            finger.Move(115, 100, 1032);
            Assert.Equal(["item-swipe"], captured);
            Assert.True(GestureController.Instance.IsCaptured);

            finger.Up(115, 100, 1048);
            Assert.False(GestureController.Instance.IsCaptured);
        }
        finally
        {
            GestureController.Instance.IonGestureCaptured -= OnCaptured;
        }
    }

    // ── G16: destroy, and one pointer at a time ──────────────────────────────────────────────

    [AvaloniaFact]
    public void G16_Destroy_Lets_Go_Of_The_Controller_And_Stops_Listening()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        var gesture = Make(box, log);

        var finger = new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016);
        Assert.True(GestureController.Instance.IsCaptured);

        gesture.Destroy();
        Assert.False(GestureController.Instance.IsCaptured);
        finger.Move(130, 100, 1032).Up(130, 100, 1048);
        gesture.Enable();
        new Finger(scene, clock).Down(100, 100, 2000).Move(120, 100, 2016).Up(120, 100, 2032);

        Assert.Equal(["g.canStart", "g.onStart"], log.Names);
    }

    [AvaloniaFact]
    public void G16_A_Second_Finger_While_One_Is_Tracked_Is_Ignored()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        var first = new Finger(scene, clock).Down(100, 100, 1000).Move(104, 100, 1016);
        new Finger(scene, clock).Down(200, 200, 1024).Move(240, 200, 1032).Up(240, 200, 1040);
        Assert.Equal(["g.canStart"], log.Names);

        first.Move(120, 100, 1048).Up(120, 100, 1064);
        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
        AssertSeen(log.Single("g.onStart"), 1048, (120, 100, 1048), (120, 100, 1048), (20, 0), (0.4025, 0));
    }

    [AvaloniaFact]
    public void G5_Times_Are_The_Events_Own_Timestamps()
    {
        // Ionic's now(ev) is ev.timeStamp. The clock stands still here, so only the events' times can give a velocity.
        using var clock = new TestClock();
        var box = Box();
        using var scene = new Scene(box);
        var seen = new List<GestureDetail>();
        double startTime = 0;
        using var gesture = Gesture.Create(new GestureConfig
        {
            Element = box,
            GestureName = "g",
            OnStart = detail => startTime = detail.StartTime,
            OnEnd = seen.Add,
        });
        gesture.Enable();
        var finger = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        var pressed = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed);
        var moving = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other);
        var released = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased);

        box.RaiseEvent(new PointerPressedEventArgs(box, finger, scene.Window, new Point(100, 100), 1000, pressed, KeyModifiers.None));
        box.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, box, finger, scene.Window, new Point(120, 100), 1016, moving, KeyModifiers.None));
        box.RaiseEvent(new PointerReleasedEventArgs(box, finger, scene.Window, new Point(120, 100), 1032, released, KeyModifiers.None, MouseButton.Left));

        Assert.Equal(1016, startTime);
        var end = Assert.Single(seen);
        Assert.Equal(1032, end.CurrentTime);
        Assert.Equal(0.2625, end.VelocityX, 6);
    }

    [AvaloniaFact]
    public void G1_Disabling_In_CanStart_Follows_That_Press_To_Its_End_And_Ignores_The_Next_Until_Enabled()
    {
        // Measured: Ionic's listeners for the press are added after canStart, so they follow it.
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        Gesture? gesture = null;
        gesture = Make(box, log, canStart: _ =>
        {
            gesture!.Enable(false);
            return true;
        });

        using (gesture)
        {
            new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Up(120, 100, 1032);
            new Finger(scene, clock).Down(100, 100, 2000).Up(100, 100, 2016);
            Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
            AssertSeen(log.Single("g.onEnd"), 1032, (120, 100, 1016), (120, 100, 1032), (0, 0), (0.2625, 0));

            gesture.Enable();
            new Finger(scene, clock).Down(100, 100, 3000).Up(100, 100, 3016);
            Assert.Equal(["g.canStart", "g.onStart", "g.onEnd", "g.canStart", "g.notCaptured"], log.Names);
        }
    }

    [AvaloniaFact]
    public void G1_Disabling_In_OnStart_At_The_Press_Ends_It_And_Its_Moves_Decide_Nothing()
    {
        // As Ionic's: enable(false) before onStart has returned gives no onEnd. pointerDown returns true, so the press is still
        // followed; its pan recogniser was never started, so the moves detect nothing, and the release, uncaptured, is notCaptured.
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        Gesture? gesture = null;
        gesture = Gesture.Create(new GestureConfig
        {
            Element = box,
            GestureName = "g",
            Threshold = 0,
            OnStart = detail =>
            {
                log.Add("g.onStart", detail);
                gesture!.Enable(false);
            },
            OnMove = detail => log.Add("g.onMove", detail),
            OnEnd = detail => log.Add("g.onEnd", detail),
            NotCaptured = detail => log.Add("g.notCaptured", detail),
        });
        gesture.Enable();

        using (gesture)
        {
            new Finger(scene, clock).Down(100, 100, 1000).Move(140, 120, 1016).Move(180, 140, 1032).Up(180, 140, 1048);
            clock.At(1100);

            Assert.Equal(["g.onStart", "g.notCaptured"], log.Names);
            Assert.False(GestureController.Instance.IsCaptured);
        }
    }

    [AvaloniaFact]
    public void G16_Destroy_Drops_A_Move_Queued_For_The_Next_Frame()
    {
        // A deviation: Ionic's queued frame still calls onMove after destroy(), on a control that may have gone.
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        var gesture = Make(box, log);

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Move(130, 100, 1032);
        gesture.Destroy();
        clock.At(1100);

        Assert.Equal(["g.canStart", "g.onStart"], log.Names);
    }

    [AvaloniaFact]
    public void G16_Destroy_While_OnWillStart_Is_Pending_Never_Starts()
    {
        // A deviation: Ionic's promise still calls onStart after destroy(), on a control that may have gone.
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        var willStart = new TaskCompletionSource();
        var gesture = Make(box, log, onWillStart: _ => willStart.Task);

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016);
        gesture.Destroy();
        willStart.SetResult();
        Settle();

        Assert.Equal(["g.canStart"], log.Names);
    }

    [AvaloniaFact]
    public void G13_An_OnWillStart_Settling_After_The_Next_Press_Started_Does_Not_Start_That_Press()
    {
        // A deviation: Ionic's first promise fires onStart during the second press, and lets its moves through before its own
        // onWillStart settles.
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        var tasks = new List<TaskCompletionSource>();
        using var gesture = Make(box, log, onWillStart: _ =>
        {
            var task = new TaskCompletionSource();
            tasks.Add(task);
            return task.Task;
        });

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Up(120, 100, 1032);
        var second = new Finger(scene, clock).Down(100, 100, 2000).Move(120, 100, 2016);
        tasks[0].SetResult();
        Settle();
        second.Move(130, 100, 2032);
        clock.At(2048);
        Assert.Equal(["g.canStart", "g.canStart"], log.Names);

        tasks[1].SetResult();
        Settle();
        second.Move(140, 100, 2064);
        clock.At(2080);
        second.Up(140, 100, 2096);

        Assert.Equal(["g.canStart", "g.canStart", "g.onStart", "g.onMove", "g.onEnd"], log.Names);
    }

    [AvaloniaFact]
    public void G13_An_OnWillStart_With_No_Task_Starts_At_Once()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log, onWillStart: _ => null!);

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Up(120, 100, 1032);

        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
    }

    [AvaloniaFact]
    public void G16_Data_Set_In_CanStart_Reaches_The_Callbacks_And_Is_Not_Cleared_By_The_Next_Press()
    {
        using var clock = new GestureClock();
        var box = Box();
        using var scene = new Scene(box);
        var seen = new List<object?>();
        var presses = 0;
        using var gesture = Gesture.Create(new GestureConfig
        {
            Element = box,
            GestureName = "g",
            CanStart = detail =>
            {
                seen.Add(detail.Data);
                detail.Data ??= $"set at press {++presses}";
                return true;
            },
            OnEnd = detail => seen.Add(detail.Data),
        });
        gesture.Enable();

        new Finger(scene, clock).Down(100, 100, 1000).Move(120, 100, 1016).Up(120, 100, 1032);
        new Finger(scene, clock).Down(100, 100, 2000).Up(100, 100, 2016);

        Assert.Equal([null, "set at press 1", "set at press 1"], seen);
    }

    [AvaloniaFact]
    public void G16_The_Gesture_Follows_A_Press_On_A_Child_That_Handles_It()
    {
        using var clock = new GestureClock();
        var child = Box(100, 50);
        child.PointerPressed += (_, e) => e.Handled = true;
        child.PointerMoved += (_, e) => e.Handled = true;
        var box = Box(200, 100);
        box.Child = child;
        using var scene = new Scene(box);
        var log = new Log(clock);
        using var gesture = Make(box, log);

        new Finger(scene, clock).Down(100, 50, 1000).Move(120, 50, 1016).Up(120, 50, 1032);

        Assert.Equal(["g.canStart", "g.onStart", "g.onEnd"], log.Names);
    }
}
