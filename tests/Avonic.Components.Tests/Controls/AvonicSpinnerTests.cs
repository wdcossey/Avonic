using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Behaviours B1–B19 from docs/components/ion-spinner/PLAN.md, read from Ionic's spinner.js, spinner-configs.js and
/// spinner.css. Every expected number is what Chromium computed for real Ionic at that instant, with the animations
/// seeked there, not a value worked out from this implementation.
/// </summary>
public class AvonicSpinnerTests
{
    private const double Tolerance = 1e-4;

    /// <summary>A spinner on a shown window, on a manual clock that starts at 0 when it's attached.</summary>
    private sealed class Fixture : IDisposable
    {
        public readonly TestClock Clock = new();
        public readonly AvonicSpinner Spinner;
        public readonly StackPanel Host;
        private readonly Scene _scene;

        public Fixture(SpinnerName? name = null, double size = 28, double? duration = null, bool paused = false)
        {
            Spinner = new AvonicSpinner { SpinnerName = name, Width = size, Height = size, Duration = duration, Paused = paused };
            Host = new StackPanel { Children = { Spinner } };
            _scene = new Scene(Host);
        }

        public Window Window => _scene.Window;

        public IReadOnlyList<SpinnerElementFrame> At(double milliseconds)
        {
            Clock.At(milliseconds);
            return Spinner.CurrentFrame();
        }

        public void Dispose()
        {
            _scene.Dispose();
            Clock.Dispose();
        }
    }

    private static void Equal(IReadOnlyList<double> expected, IEnumerable<double> actual)
    {
        var values = actual.ToList();
        Assert.Equal(expected.Count, values.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.True(Math.Abs(expected[i] - values[i]) < Tolerance, $"element {i}: expected {expected[i]}, got {values[i]}");
        }
    }

    private static void Near(double expected, double actual, double tolerance = Tolerance) =>
        Assert.True(Math.Abs(expected - actual) < tolerance, $"expected {expected}, got {actual}");

    private static WriteableBitmap Capture(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        return window.CaptureRenderedFrame()!;
    }

    // ── B1: the default name ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_With_No_Name_It_Is_Circular_And_A_Name_Draws_That_Spinner()
    {
        using var f = new Fixture();

        // getName(): this.name || config.get('spinner') || (md ? 'circular'). Measured class spinner-circular, one svg.
        var frame = f.At(350);
        Assert.Single(frame);
        Near(50.5, frame[0].DashArray);

        f.Spinner.SpinnerName = SpinnerName.Lines;
        Assert.Equal(8, f.Spinner.CurrentFrame().Count);

        f.Spinner.SpinnerName = null;
        Assert.Single(f.Spinner.CurrentFrame());
    }

    // ── B2: each name's elements ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Each_Name_Has_Its_Elements_Shapes_And_Paint()
    {
        // From spinner-configs.js and spinner.css, each measured as the computed style of the shadow DOM's elements.
        var expected = new (SpinnerName Name, int Count, bool Line, double R, double Y1, double Y2, bool Fill, double Stroke, PenLineCap Cap)[]
        {
            (SpinnerName.Bubbles, 9, false, 5, 0, 0, true, 0, PenLineCap.Flat),
            (SpinnerName.Circles, 8, false, 5, 0, 0, true, 0, PenLineCap.Flat),
            (SpinnerName.Circular, 1, false, 20, 0, 0, false, 5.6, PenLineCap.Flat),
            (SpinnerName.Crescent, 1, false, 26, 0, 0, false, 4, PenLineCap.Flat),
            (SpinnerName.Dots, 3, false, 6, 0, 0, true, 0, PenLineCap.Flat),
            (SpinnerName.Lines, 8, true, 0, 14, 26, false, 7, PenLineCap.Round),
            (SpinnerName.LinesSmall, 8, true, 0, 12, 20, false, 7, PenLineCap.Round),
            (SpinnerName.LinesSharp, 12, true, 0, 17, 29, false, 4, PenLineCap.Round),
            (SpinnerName.LinesSharpSmall, 12, true, 0, 12, 20, false, 4, PenLineCap.Round),
        };

        foreach (var e in expected)
        {
            var config = SpinnerConfigs.Get(e.Name);
            Assert.Equal(e.Count, config.Count);
            Assert.Equal(e.Line, config.IsLine);
            Assert.Equal(e.Fill, config.Fill);
            Assert.Equal(e.Stroke, config.StrokeWidth);
            Assert.Equal(e.Cap, config.LineCap);
            if (e.Line)
            {
                Assert.Equal((e.Y1, e.Y2), (config.Y1, config.Y2));
                Assert.Equal(new Point(32, 32), config.Center);
            }
            else
            {
                Assert.Equal(e.R, config.Radius);
            }
        }

        // circular: viewBox 24 24 48 48, cx = cy = 48. The rest: 0 0 64 64, translate(32,32).
        Assert.Equal(new Rect(24, 24, 48, 48), SpinnerConfigs.Get(SpinnerName.Circular).ViewBox);
        Assert.Equal(new Point(48, 48), SpinnerConfigs.Get(SpinnerName.Circular).Center);
        Assert.Equal(new Rect(0, 0, 64, 64), SpinnerConfigs.Get(SpinnerName.Dots).ViewBox);
        Assert.Equal(new Point(32, 32), SpinnerConfigs.Get(SpinnerName.Bubbles).Center);

        // The offsets of the ring and the dots, as measured: top 20.5692 %, left 24.5134 % for bubble 1.
        using var f = new Fixture(SpinnerName.Bubbles);
        var bubble = f.At(0)[1];
        Near(24.5134, bubble.Left);
        Near(20.5692, bubble.Top);
        f.Spinner.SpinnerName = SpinnerName.Circles;
        Near(22.6274, f.Spinner.CurrentFrame()[1].Left);
        f.Spinner.SpinnerName = SpinnerName.Dots;
        Equal([32, 0, -32], f.Spinner.CurrentFrame().Select(e => e.Left));
        f.Spinner.SpinnerName = SpinnerName.Lines;
        Equal([180, 225, 270, 315, 0, 45, 90, 135], f.Spinner.CurrentFrame().Select(e => e.Rotation));
        f.Spinner.SpinnerName = SpinnerName.LinesSharp;
        Near(210, f.Spinner.CurrentFrame()[1].Rotation);
        Near(0, f.Spinner.CurrentFrame()[6].Rotation);
    }

    // ── B3: default durations ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Each_Name_Has_Its_Own_Duration()
    {
        // dur: bubbles, circles and the lines 1000, circular 1400, crescent and dots 750. Measured at 100 ms.
        using (var f = new Fixture(SpinnerName.Lines)) Near(0.9, f.At(100)[0].Opacity);
        using (var f = new Fixture(SpinnerName.Circles)) Near(0.9, f.At(100)[0].Opacity);
        using (var f = new Fixture(SpinnerName.Bubbles)) Near(0.9, f.At(100)[0].Scale);
        using (var f = new Fixture(SpinnerName.Crescent)) Near(48, f.At(100)[0].Rotation);
        using (var f = new Fixture(SpinnerName.Dots)) Near(0.84, f.At(100)[0].Scale);
        using (var f = new Fixture(SpinnerName.Circular))
        {
            Near(5.04952, f.At(100)[0].DashArray);
            Near(1, f.At(1400)[0].DashArray);
        }
    }

    // ── B4: duration ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_A_Duration_Over_10_Replaces_The_Default_And_A_Change_Keeps_The_Time()
    {
        // duration="2000" on lines: measured 0.95 at 100 ms, and 0.475 for the first line at 1050.
        using (var f = new Fixture(SpinnerName.Lines, duration: 2000))
        {
            Near(0.95, f.At(100)[0].Opacity);
            Equal([0.475, 0.6, 0.725, 0.85, 0.975, 0.1, 0.225, 0.35], f.At(1050).Select(e => e.Opacity));
        }

        // duration="10" is ignored: crescent's own 750 ms. duration="11" counts.
        using (var f = new Fixture(SpinnerName.Crescent, duration: 10)) Near(48, f.At(100)[0].Rotation);
        using (var f = new Fixture(SpinnerName.Crescent, duration: 11)) Near(360 * (100 / 11.0 % 1), f.At(100)[0].Rotation);

        // Infinity is over 10, but Infinityms isn't a valid animation-duration. It's the name's own here (see COMPARISON.md),
        // not a spinner that never moves.
        using (var f = new Fixture(SpinnerName.Crescent, duration: double.PositiveInfinity)) Near(48, f.At(100)[0].Rotation);

        // A change on a running animation keeps its currentTime (measured 350 → 400 over 50 ms), so the phase is elapsed / new.
        using (var f = new Fixture(SpinnerName.Crescent))
        {
            f.At(300);
            f.Spinner.Duration = 3000;
            Near(360 * 350 / 3000.0, f.At(350)[0].Rotation);
        }
    }

    // ── B5: the stagger ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Ring_And_Line_Elements_Are_Staggered_And_The_Brightest_Moves_Clockwise()
    {
        // Measured at 0, 175 and 250 ms. Line i is turned 45i° clockwise from 12 o'clock, so the brightest, 0 then 1
        // then 2, moves clockwise.
        using (var f = new Fixture(SpinnerName.Lines))
        {
            Equal([1, 0.125, 0.25, 0.375, 0.5, 0.625, 0.75, 0.875], f.At(0).Select(e => e.Opacity));
            Equal([0.825, 0.95, 0.075, 0.2, 0.325, 0.45, 0.575, 0.7], f.At(175).Select(e => e.Opacity));
            Equal([0.75, 0.875, 1, 0.125, 0.25, 0.375, 0.5, 0.625], f.At(250).Select(e => e.Opacity));
        }

        using (var f = new Fixture(SpinnerName.Bubbles))
        {
            Equal([1, 1 / 9.0, 2 / 9.0, 3 / 9.0, 4 / 9.0, 5 / 9.0, 6 / 9.0, 7 / 9.0, 8 / 9.0], f.At(0).Select(e => e.Scale));
            Equal([0.65, 0.761111, 0.872222, 0.983333, 0.0944444, 0.205556, 0.316667, 0.427778, 0.538889],
                f.At(350).Select(e => e.Scale).Select(v => Math.Round(v, 6)));
        }

        using (var f = new Fixture(SpinnerName.Circles))
        {
            Equal([0.65, 0.775, 0.9, 0.025, 0.15, 0.275, 0.4, 0.525], f.At(350).Select(e => e.Opacity));
        }

        using (var f = new Fixture(SpinnerName.LinesSharp))
        {
            Equal([0.9, 0.983333, 0.0666667, 0.15, 0.233333, 0.316667, 0.4, 0.483333, 0.566667, 0.65, 0.733333, 0.816667],
                f.At(100).Select(e => e.Opacity).Select(v => Math.Round(v, 7)));
        }
    }

    // ── B6: dots ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Dots_Pulse_110_ms_Apart()
    {
        using var f = new Fixture(SpinnerName.Dots);

        // Measured scale / opacity at 0, 250 and 375 ms.
        var at0 = f.At(0);
        Equal([1, 0.824, 0.648], at0.Select(e => e.Scale));
        Equal([0.9, 0.724, 0.548], at0.Select(e => e.Opacity));
        Equal([0.6, 0.424, 0.552], f.At(250).Select(e => e.Scale));
        var at375 = f.At(375);
        Equal([0.4, 0.576, 0.752], at375.Select(e => e.Scale));
        Equal([0.3, 0.476, 0.652], at375.Select(e => e.Opacity));
    }

    // ── B7: crescent ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_Crescent_Is_A_Fixed_Arc_From_3_OClock_Turning_Clockwise()
    {
        using var f = new Fixture(SpinnerName.Crescent);

        // stroke-dasharray 128, offset 82: the stretch from 0 to 46 of a 2π·26 circle, which is 101.4°.
        var arc = Assert.Single(f.At(0)[0].Arcs!);
        Near(0, arc.Start);
        Near(46, arc.End);
        Near(101.37, arc.End / 26 * 180 / Math.PI, 0.01);

        // Measured rotate(48deg) at 100 ms, 120° at 1000 and 180° at 375.
        Near(48, f.At(100)[0].Rotation);
        Near(180, f.At(375)[0].Rotation);
        Near(120, f.At(1000)[0].Rotation);
    }

    // ── B8: circular ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_Circular_Does_Not_Turn_And_Its_Arc_Grows_Then_Runs_Off_The_End()
    {
        using var f = new Fixture(SpinnerName.Circular);

        // Measured stroke-dasharray / stroke-dashoffset, ease-in-out in each half.
        var expected = new (double Time, double Dash, double Offset)[]
        {
            (0, 1, 0), (100, 5.04952, -0.613563), (175, 13.787, -1.93743), (250, 27.3276, -3.98904), (350, 50.5, -7.5),
            (500, 83.1995, -12.4545), (700, 100, -15), (750, 100, -16.0941), (1000, 100, -56.6274), (1050, 100, -70), (1400, 1, 0),
        };
        foreach (var (time, dash, offset) in expected)
        {
            var frame = f.At(time)[0];
            Near(dash, frame.DashArray, 1e-3);
            Near(offset, frame.DashOffset, 1e-3);

            // matrix(1, 0, 0, 1, 0, 0) at every instant measured.
            Assert.Equal(0, frame.Rotation);
            Assert.Equal(1, frame.Scale);
        }

        // At 1050 the dash is from 70 along to the circle's end, 2π·20, with no wrap back to the start.
        var arc = Assert.Single(f.At(1050)[0].Arcs!);
        Near(70, arc.Start, 1e-3);
        Near(2 * Math.PI * 20, arc.End);
    }

    // ── B9: paused ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_Paused_Freezes_And_Clearing_It_Carries_On_From_There()
    {
        using var f = new Fixture(SpinnerName.Lines);

        f.At(200);
        f.Spinner.Paused = true;
        Near(0.8, f.At(500)[0].Opacity);     // still 200 ms in

        f.Spinner.Paused = false;
        Near(0.7, f.At(600)[0].Opacity);     // 300 ms in, not 600 and not 100

        // Paused from the start, it shows the first instant.
        using var g = new Fixture(SpinnerName.Lines, paused: true);
        Equal([1, 0.125, 0.25, 0.375, 0.5, 0.625, 0.75, 0.875], g.At(400).Select(e => e.Opacity));
    }

    // ── B10: a change of name ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_A_New_Name_Keeps_The_Elements_That_Keep_Their_Keyframes_And_Restarts_The_Rest()
    {
        // Measured: lines → lines-sharp kept the first 8 at their time and started the other 4.
        using (var f = new Fixture(SpinnerName.Lines))
        {
            f.At(300);
            f.Spinner.SpinnerName = SpinnerName.LinesSharp;
            var frame = f.At(400);
            Near(0.6, frame[0].Opacity);                                  // 400 ms in
            Near(1 - (100 + 1000 / 3.0) / 1000, frame[8].Opacity);        // 100 ms in, with its own delay
        }

        // circles → lines: the same svgs and the same spinner-fade-out, so they carry on.
        using (var f = new Fixture(SpinnerName.Circles))
        {
            f.At(300);
            f.Spinner.SpinnerName = SpinnerName.Lines;
            Near(0.6, f.At(400)[0].Opacity);
        }

        // dots → bubbles: spinner-dots becomes spinner-scale-out, so every one starts again.
        using (var f = new Fixture(SpinnerName.Dots))
        {
            f.At(300);
            f.Spinner.SpinnerName = SpinnerName.Bubbles;
            Near(0.9, f.At(400)[0].Scale);
        }
    }

    // ── B11: leaving the tree ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Leaving_The_Tree_Stops_The_Frames_And_Coming_Back_Starts_Again()
    {
        using var f = new Fixture(SpinnerName.Lines);
        f.At(300);
        Assert.True(f.Spinner.IsRequestingFrames);

        f.Host.Children.Remove(f.Spinner);
        Dispatcher.UIThread.RunJobs();
        Assert.False(f.Spinner.IsRequestingFrames);

        f.At(400);
        f.Host.Children.Add(f.Spinner);
        Dispatcher.UIThread.RunJobs();
        Assert.True(f.Spinner.IsRequestingFrames);
        Near(0.95, f.At(450)[0].Opacity);    // measured: a re-inserted spinner's animations start again

        // Paused when it left, and running by the time it's back: new animations, from 0, not shifted by the old pause.
        f.Spinner.Paused = true;
        f.At(500);
        f.Host.Children.Remove(f.Spinner);
        Dispatcher.UIThread.RunJobs();
        f.Spinner.Paused = false;
        f.At(600);
        f.Host.Children.Add(f.Spinner);
        Dispatcher.UIThread.RunJobs();
        Near(0.95, f.At(650)[0].Opacity);
    }

    [AvaloniaFact]
    public void B11_Being_Hidden_Stops_The_Frames_And_Being_Shown_Starts_Again()
    {
        using var f = new Fixture(SpinnerName.Lines);
        f.At(300);

        // IsVisible is CSS's display: none, on the spinner or an ancestor. Measured: hidden, the animations are gone, and
        // shown again 100 ms later, they're 100 ms in.
        f.Spinner.IsVisible = false;
        Assert.False(f.Spinner.IsRequestingFrames);
        f.At(400);
        f.Spinner.IsVisible = true;
        Assert.True(f.Spinner.IsRequestingFrames);
        Near(0.9, f.At(500)[0].Opacity);

        f.Host.IsVisible = false;
        Assert.False(f.Spinner.IsRequestingFrames);
        f.At(600);
        f.Host.IsVisible = true;
        Assert.True(f.Spinner.IsRequestingFrames);
        Near(0.95, f.At(650)[0].Opacity);

        // A window that's hidden isn't display: none. A hidden page doesn't cancel its animations.
        f.Window.Hide();
        f.Window.Show();
        Dispatcher.UIThread.RunJobs();
        Near(0.9, f.At(700)[0].Opacity);
    }

    // ── B12: colour ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_The_Colour_Inherits_And_Follows_Color_And_The_Variable() => InstalledTheme.Run(_ =>
    {
        var inherited = Avalonia.Media.Color.FromRgb(1, 2, 3);
        var variable = new SolidColorBrush(Avalonia.Media.Color.FromRgb(18, 52, 86));

        var plain = new AvonicSpinner();
        var danger = new AvonicSpinner { Color = "danger" };
        var nonsense = new AvonicSpinner { Color = "nonsense" };
        var withVariable = new AvonicSpinner();
        withVariable.Resources["Avonic.Spinner.Color"] = variable;
        var dangerWithVariable = new AvonicSpinner { Color = "danger" };
        dangerWithVariable.Resources["Avonic.Spinner.Color"] = variable;
        var appColour = new AvonicSpinner { Color = "danger", Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(9, 9, 9)) };

        var panel = new StackPanel { Children = { plain, danger, nonsense, withVariable, dangerWithVariable, appColour } };
        TextElement.SetForeground(panel, new SolidColorBrush(inherited));
        using var scene = new Scene(panel);

        static Avalonia.Media.Color? Of(AvonicSpinner s) => (s.Foreground as ISolidColorBrush)?.Color;

        // Measured rgb(1, 2, 3) in such a parent, rgb(197, 0, 15) for danger, rgb(1, 2, 3) for an unknown colour,
        // rgb(18, 52, 86) for the variable, danger over the variable, and an app's own color over danger.
        Assert.Equal(inherited, Of(plain));
        Assert.Equal(Avalonia.Media.Color.FromRgb(197, 0, 15), Of(danger));
        Assert.Equal(inherited, Of(nonsense));
        Assert.Equal(variable.Color, Of(withVariable));
        Assert.Equal(Avalonia.Media.Color.FromRgb(197, 0, 15), Of(dangerWithVariable));
        Assert.Equal(Avalonia.Media.Color.FromRgb(9, 9, 9), Of(appColour));
    });

    // ── B13: size ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_It_Is_28_Square_And_A_Set_Size_Scales_By_The_Smaller_Side() => InstalledTheme.Run(_ =>
    {
        var spinner = new AvonicSpinner();
        using (new Scene(new Grid { Children = { spinner } }))
        {
            Assert.Equal(new Rect(0, 0, 28, 28), spinner.Bounds);
        }

        // 56 × 28 dots, measured: the svg is 32 % of 56 in, so dot 0's centre is at 28 + 17.92, and r 6 is 6 × 28 / 64.
        var dots = SpinnerConfigs.Get(SpinnerName.Dots);
        var dot = SpinnerFrame.Compute(dots, 750, 0, 0);
        var wide = SpinnerFrame.Transform(dots, dot, new Size(56, 28));
        var dotCentre = dots.Center.Transform(wide);
        Near(28 + 17.92, dotCentre.X);
        Near(14, dotCentre.Y);
        Near(28 / 64.0 * dot.Scale, wide.M11);

        // 28 × 56 circles: left and top are each of their own side. Measured centre (20.34, 40.67) for circle 1.
        var circles = SpinnerConfigs.Get(SpinnerName.Circles);
        var tall = SpinnerFrame.Transform(circles, SpinnerFrame.Compute(circles, 1000, 1, 0), new Size(28, 56));
        var centre = circles.Center.Transform(tall);
        Near(14 + 0.226274 * 28, centre.X);
        Near(28 + 0.226274 * 56, centre.Y);
        Near(28 / 64.0, tall.M11);
    });

    // ── B14: right to left ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_It_Is_Not_Mirrored_Right_To_Left()
    {
        using var clock = new TestClock();

        // Paused at 0 ms, dot 0, on the right, is the largest. Measured identical in dir="rtl".
        WriteableBitmap Draw(FlowDirection direction)
        {
            var spinner = new AvonicSpinner { SpinnerName = SpinnerName.Dots, Width = 64, Height = 64, Paused = true, Foreground = Brushes.Black };
            var panel = new StackPanel { FlowDirection = direction, Background = Brushes.White, Children = { spinner } };
            using var scene = new Scene(panel, 64, 64);
            return Capture(scene.Window);
        }

        var ltr = Draw(FlowDirection.LeftToRight);
        var rtl = Draw(FlowDirection.RightToLeft);

        // Dot 0 is at x = 32 + 20.48, r = 6 × its scale of 1: its centre is ink, and the mirror image of it is a smaller dot's edge.
        Assert.True(Frames.PixelAt(ltr, 52, 32).R < 128);
        for (var x = 0; x < 64; x += 2)
        {
            Assert.Equal(Frames.PixelAt(ltr, x, 32), Frames.PixelAt(rtl, x, 32));
        }
    }

    // ── B15: what it is ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_It_Is_A_Progress_Bar_Not_Focusable_That_Sits_At_The_Start_And_Takes_Input_On_Its_Box()
    {
        using var clock = new TestClock();
        var spinner = new AvonicSpinner { SpinnerName = SpinnerName.Dots, Width = 40, Height = 40, Paused = true, Foreground = Brushes.Black };
        using var scene = new Scene(new Grid { Width = 200, Height = 200, Children = { spinner } }, 200, 200);

        // role="progressbar"
        var peer = ControlAutomationPeer.CreatePeerForElement(spinner);
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        Assert.False(spinner.Focusable);

        // display: inline-block: at the start and top, not centred in a stretching parent.
        Assert.Equal(new Point(0, 0), spinner.Bounds.Position);

        // A host box takes input where nothing's drawn, at its corner.
        Capture(scene.Window);
        Assert.Same(spinner, scene.Window.InputHitTest(new Point(2, 2)));
    }

    // ── B16: frames ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B16_Frames_Are_Asked_For_Only_While_Running_And_Each_Redraws()
    {
        using var clock = new TestClock();
        var spinner = new AvonicSpinner { SpinnerName = SpinnerName.Lines, Width = 128, Height = 128, Foreground = Brushes.Black };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { spinner } }, 128, 128);
        Assert.True(spinner.IsRequestingFrames);

        spinner.Paused = true;
        Assert.False(spinner.IsRequestingFrames);
        spinner.Paused = false;
        Assert.True(spinner.IsRequestingFrames);

        // Line 0, at 12 o'clock, is opaque at 0 ms and 0.75 at 250, with nothing but the frames to redraw it.
        Near(0, Frames.PixelAt(Capture(scene.Window), 64, 24).R, 3);
        clock.At(250);
        Near(64, Frames.PixelAt(Capture(scene.Window), 64, 24).R, 3);
        Assert.True(spinner.IsRequestingFrames);
    }

    // ── B17: AvonicConfig.Animated ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_Animated_Off_Pauses_It_And_On_Again_Carries_On()
    {
        AvonicConfig.Animated = false;
        try
        {
            using var f = new Fixture(SpinnerName.Lines);
            Assert.False(f.Spinner.IsRequestingFrames);
            Assert.True(f.Spinner.IsWatchingAnimated);
            Near(1, f.At(400)[0].Opacity);

            // Turned back on, it carries on at its next look, every 250 ms, here at 500, with no change of property.
            Assert.Equal(250, AvonicSpinner.AnimatedCheckInterval.TotalMilliseconds);
            AvonicConfig.Animated = true;
            f.At(500);
            Assert.True(f.Spinner.IsRequestingFrames);
            Near(0.9, f.At(600)[0].Opacity);

            // Switched off while running, it stops at the next frame, and asks for no more.
            AvonicConfig.Animated = false;
            f.At(1000);
            Assert.False(f.Spinner.IsRequestingFrames);
            var frozen = f.Spinner.CurrentFrame()[0].Opacity;
            Near(frozen, f.At(1600)[0].Opacity);

            // Paused by the app as well, it doesn't look at all, and stays paused when animations come back.
            f.Spinner.Paused = true;
            AvonicConfig.Animated = true;
            f.At(2500);
            Assert.False(f.Spinner.IsRequestingFrames);
            Assert.False(f.Spinner.IsWatchingAnimated);
            Near(frozen, f.Spinner.CurrentFrame()[0].Opacity);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    }

    [AvaloniaFact]
    public void B18_A_Change_Redraws_It_Between_Frames_And_While_Paused()
    {
        using var clock = new TestClock();
        var spinner = new AvonicSpinner { SpinnerName = SpinnerName.Lines, Duration = 100, Width = 128, Height = 128, Foreground = Brushes.Black };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { spinner } }, 128, 128);

        static int Ink(WriteableBitmap frame, int x, int y) => 255 - Frames.PixelAt(frame, x, y).R;

        // Line 0, at 12 o'clock, on a 100 ms cycle: at 240, a frame, its opacity is 0.6. Paused at 248, before the next
        // frame, it shows 248, at 0.52.
        clock.At(240);
        Near(0.6 * 255, Ink(Capture(scene.Window), 64, 24), 3);
        clock.At(248);
        spinner.Paused = true;
        Near(0.52 * 255, Ink(Capture(scene.Window), 64, 24), 3);

        // Paused, a new duration is a new phase of the same 248 ms: 0.752 of a 1000 ms cycle.
        spinner.Duration = 1000;
        Near(0.752 * 255, Ink(Capture(scene.Window), 64, 24), 3);

        // Paused, a new name is drawn, with its own stroke: lines-sharp's line 0 keeps its time, 8 px wide, not lines' 14.
        spinner.SpinnerName = SpinnerName.LinesSharp;
        var sharp = Capture(scene.Window);
        Near(0.752 * 255, Ink(sharp, 64, 20), 3);
        Assert.Equal(0, Ink(sharp, 69, 20));
    }

    // ── B19: host classes ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_The_Host_Has_A_Class_For_Its_Name_And_For_Paused()
    {
        using var f = new Fixture();
        Assert.Equal(1, f.Spinner.Opacity);
        f.Window.Styles.Add(new Style(x => x.OfType<AvonicSpinner>().Class(":name-dots"))
        {
            Setters = { new Setter(Visual.OpacityProperty, 0.5) },
        });
        f.Window.Styles.Add(new Style(x => x.OfType<AvonicSpinner>().Class(":paused"))
        {
            Setters = { new Setter(Layoutable.MarginProperty, new Thickness(3)) },
        });

        Dispatcher.UIThread.RunJobs();

        static string[] Names(AvonicSpinner s) => s.Classes.Where(c => c.StartsWith(":name-", StringComparison.Ordinal)).ToArray();

        // render(): 'spinner-' + getName(), measured spinner-circular with no name, so an app's ion-spinner.spinner-dots
        // selects only the dots.
        Assert.Equal([":name-circular"], Names(f.Spinner));
        f.Spinner.SpinnerName = SpinnerName.Dots;
        Assert.Equal([":name-dots"], Names(f.Spinner));
        Assert.Equal(0.5, f.Spinner.Opacity);
        f.Spinner.SpinnerName = SpinnerName.LinesSharpSmall;
        Assert.Equal([":name-lines-sharp-small"], Names(f.Spinner));
        Assert.Equal(1, f.Spinner.Opacity);

        // 'spinner-paused': paused || _testing, where Animated off stands in for _testing.
        Assert.Equal(default, f.Spinner.Margin);
        f.Spinner.Paused = true;
        Assert.Equal(new Thickness(3), f.Spinner.Margin);
        f.Spinner.Paused = false;
        Assert.Equal(default, f.Spinner.Margin);

        AvonicConfig.Animated = false;
        try
        {
            f.At(100);
            Assert.Equal(new Thickness(3), f.Spinner.Margin);
            AvonicConfig.Animated = true;
            f.At(400);
            Assert.Equal(default, f.Spinner.Margin);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    }

    // ── B18: it draws the frame ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_It_Draws_The_Frame_In_Its_Colour()
    {
        using var clock = new TestClock();

        // A 128 px spinner on white, drawn `elapsed` ms after it's attached, paused there.
        WriteableBitmap Draw(SpinnerName name, double elapsed)
        {
            var spinner = new AvonicSpinner { SpinnerName = name, Width = 128, Height = 128, Foreground = Brushes.Black };
            using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { spinner } }, 128, 128);
            clock.At(clock.Now.TotalMilliseconds + elapsed);
            spinner.Paused = true;
            return Capture(scene.Window);
        }

        static int Ink(WriteableBitmap frame, int x, int y) => 255 - Frames.PixelAt(frame, x, y).R;

        // lines at 0 ms: line 0 at 12 o'clock is opaque, line 4 at 6 o'clock is half, line 2 at 3 o'clock a quarter.
        var lines = Draw(SpinnerName.Lines, 0);
        Near(255, Ink(lines, 64, 24), 3);
        Near(128, Ink(lines, 64, 104), 3);
        Near(64, Ink(lines, 104, 64), 3);
        Assert.Equal(0, Ink(lines, 64, 64));

        // circular at 1050 ms: the arc is from 200° round over the top to 3 o'clock, r 53.3 px about the middle.
        var circular = Draw(SpinnerName.Circular, 1050);
        Assert.True(Ink(circular, 64, 11) > 200);      // 12 o'clock
        Assert.True(Ink(circular, 116, 55) > 200);     // just above 3 o'clock
        Assert.Equal(0, Ink(circular, 116, 73));       // just below it
        Assert.Equal(0, Ink(circular, 11, 64));        // 9 o'clock
        Assert.Equal(0, Ink(circular, 64, 117));       // 6 o'clock

        // crescent at 0 ms: 101° clockwise from 3 o'clock, r 28.2 px.
        var crescent = Draw(SpinnerName.Crescent, 0);
        Assert.True(Ink(crescent, 64, 116) > 200);     // 6 o'clock, 90°
        Assert.Equal(0, Ink(crescent, 64, 12));        // 12 o'clock
        Assert.Equal(0, Ink(crescent, 12, 64));        // 9 o'clock

        // bubbles at 0 ms: bubble 0, at 3 o'clock, is full size and opaque.
        var bubbles = Draw(SpinnerName.Bubbles, 0);
        Assert.True(Ink(bubbles, 64 + 41, 64) > 250);

        // Bubble 1, at (24.5 %, 20.6 %), is scaled to a ninth, r 10 / 9 px, about its own centre, so 4.6 px from it is clear.
        Assert.Equal(0, Ink(bubbles, 100, 90));

        // A new colour on a stroked spinner that's already drawn: the stroke follows it.
        var lit = new AvonicSpinner { SpinnerName = SpinnerName.Crescent, Width = 128, Height = 128, Paused = true, Foreground = Brushes.Black };
        using var scene = new Scene(new StackPanel { Background = Brushes.White, Children = { lit } }, 128, 128);
        Capture(scene.Window);
        lit.Foreground = Brushes.Red;
        var red = Frames.PixelAt(Capture(scene.Window), 64, 116);
        Assert.True(red.R > 200 && red.G < 60, $"expected red, got {red}");
    }
}
