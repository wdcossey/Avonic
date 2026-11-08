using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Behaviours B1–B17 from docs/components/ion-progress-bar/PLAN.md, read from progress-bar.js and progress-bar.md.css
/// and measured in Chromium at 358 wide. The numbers are Ionic's, not the implementation's.
/// </summary>
public class AvonicProgressBarTests
{
    private const double W = 358;
    private const double Margin = 16;

    /// <summary>A bar 358 wide at (16, 16) in a white 390 × 200 window, with the theme installed.</summary>
    private sealed class Fixture : IDisposable
    {
        public readonly AvonicProgressBar Bar;
        public readonly Panel Host;
        private readonly Scene _scene;

        public Fixture(AvonicProgressBar bar, Control? around = null)
        {
            Bar = bar;
            Host = new Panel { Margin = new Thickness(Margin), VerticalAlignment = VerticalAlignment.Top, Children = { bar } };
            Control content = Host;
            if (around is Decorator decorator)
            {
                decorator.Child = Host;
                content = decorator;
            }

            _scene = new Scene(new Panel { Background = Brushes.White, Children = { content } }, 390, 200);
        }

        public Window Window => _scene.Window;

        public T Part<T>(string name) where T : Control =>
            Bar.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

        /// <summary>A part's horizontal extent in the host's coordinates, after every transform, the bar's own included.</summary>
        public (double X, double End) Span(string name)
        {
            // A part that has just been shown is measured at the next layout pass
            Window.UpdateLayout();
            var part = Part<Control>(name);
            var matrix = part.TransformToVisual(Host)!.Value;
            var rect = new Rect(part.Bounds.Size).TransformToAABB(matrix);
            return (rect.X, rect.Right);
        }

        public WriteableBitmap Frame()
        {
            Dispatcher.UIThread.RunJobs();
            return Window.CaptureRenderedFrame()!;
        }

        /// <summary>The pixel in the middle of the bar's height, at <paramref name="x"/> in the host's coordinates.</summary>
        public static Color PixelAt(WriteableBitmap frame, double x) => Frames.PixelAt(frame, (int)(Margin + x), (int)Margin + 2);

        public void Dispose() => _scene.Dispose();
    }

    private static readonly Color Primary = Color.FromRgb(0, 84, 233);

    /// <summary>rgba(0, 84, 233, 0.3) on white, as measured in Chromium.</summary>
    private static readonly Color PrimaryTrack = Color.FromRgb(178, 203, 248);

    private static void AssertColour(Color expected, Color actual, int tolerance = 3)
    {
        Assert.True(
            Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance && Math.Abs(expected.B - actual.B) <= tolerance,
            $"expected {expected}, got {actual}");
    }

    private static void AssertWhite(Color actual) => AssertColour(Colors.White, actual);

    private static void AssertSpan((double X, double End) expected, (double X, double End) actual, double tolerance = 0.5)
    {
        Assert.True(
            Math.Abs(expected.X - actual.X) <= tolerance && Math.Abs(expected.End - actual.End) <= tolerance,
            $"expected {expected.X:0.0}..{expected.End:0.0}, got {actual.X:0.0}..{actual.End:0.0}");
    }

    // ── B1: the host, and accessibility ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Fills_Its_Parent_4_High_Clips_And_Is_A_ProgressBar_To_Automation() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.4 });

        Assert.Equal(new Size(W, 4), f.Bar.Bounds.Size);
        Assert.True(f.Bar.ClipToBounds);

        var peer = ControlAutomationPeer.CreatePeerForElement(f.Bar);
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        Assert.True(peer.IsControlElement());
        var range = peer.GetProvider<IRangeValueProvider>();
        Assert.NotNull(range);
        Assert.Equal(0, range.Minimum);
        Assert.Equal(1, range.Maximum);
        Assert.Equal(0.4, range.Value);
        Assert.True(range.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => range.SetValue(0.5));

        // A change of value is announced, and so is aria-valuenow going and coming with the type
        var changes = new List<(object? Old, object? New)>();
        peer.PropertyChanged += (_, e) => changes.Add((e.OldValue, e.NewValue));
        f.Bar.Value = 0.6;
        Assert.Equal([(0.4, 0.6)], changes);

        // aria-valuenow is dropped when indeterminate
        f.Bar.Type = ProgressBarType.Indeterminate;
        Assert.Null(peer.GetProvider<IRangeValueProvider>());
        f.Bar.Value = 0.7;
        f.Bar.Type = ProgressBarType.Determinate;
        Assert.Equal([(0.4, 0.6), (0.6, null), (null, 0.7)], changes);
    });

    // ── B2: determinate ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_The_Progress_Is_The_Width_Times_Value_From_The_Left() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.25 });

        AssertSpan((0, 89.5), f.Span("PART_Progress"));
        AssertSpan((0, W), f.Span("PART_Track"));

        var frame = f.Frame();
        AssertColour(Primary, Fixture.PixelAt(frame, 88));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 91));
    });

    // ── B3: clamping is for display only ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Value_And_Buffer_Are_Clamped_For_Display_Only() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        AvonicConfig.Animated = false;
        try
        {
            using var f = new Fixture(new AvonicProgressBar { Value = 1.5, Buffer = -0.5 });
            var range = ControlAutomationPeer.CreatePeerForElement(f.Bar).GetProvider<IRangeValueProvider>()!;

            AssertSpan((0, W), f.Span("PART_Progress"));
            AssertSpan((0, 0), f.Span("PART_Track"));
            Assert.Equal(1.5, range.Value);

            f.Bar.Value = -1;
            f.Bar.Buffer = 1.5;
            AssertSpan((0, 0), f.Span("PART_Progress"));
            AssertSpan((0, W), f.Span("PART_Track"));
            Assert.Equal(-1, range.Value);

            // clamp() keeps NaN, and scaleX(NaN) is an invalid transform, which style.transform ignores: the scale stays
            f.Bar.Value = double.NaN;
            AssertSpan((0, 0), f.Span("PART_Progress"));
            f.Bar.Buffer = double.NaN;
            AssertSpan((0, W), f.Span("PART_Track"));
            f.Bar.Buffer = 0.5;
            f.Bar.Buffer = double.NaN;
            AssertSpan((0, 179), f.Span("PART_Track"));

            // and a new element has no transform at all
            using var created = new Fixture(new AvonicProgressBar { Value = double.NaN });
            AssertSpan((0, W), created.Span("PART_Progress"));
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    });

    // ── B4: the buffer and its stream ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Buffer_Ends_The_Track_And_Dots_Fill_The_Rest_Every_10_Px() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.3, Buffer = 0.75 });

        AssertSpan((0, 107.4), f.Span("PART_Progress"));
        AssertSpan((0, 268.5), f.Span("PART_Track"));

        var frame = f.Frame();
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 200));
        // Measured: dot centres at 280, 290, … at animation time 0, 4.24 wide, in the track's colour
        foreach (var x in new[] { 280.0, 290, 300, 350 })
        {
            AssertColour(PrimaryTrack, Fixture.PixelAt(frame, x), 8);
            // 2.12 either side of the centre, so the pixels from x - 2 to x + 2 are covered
            AssertColour(PrimaryTrack, Fixture.PixelAt(frame, x + 1), 8);
            AssertColour(PrimaryTrack, Fixture.PixelAt(frame, x - 2), 8);
            AssertWhite(Fixture.PixelAt(frame, x + 3));
            AssertWhite(Fixture.PixelAt(frame, x + 5));
        }

        // Nothing in the stream shows left of the buffer's end, where the track is
        f.Bar.Buffer = 0.5;
        clock.At(200);
        // The dots are 4.44 px left of 180, 190, …
        frame = f.Frame();
        AssertWhite(Fixture.PixelAt(frame, 190.5));
        // The track is translucent, so a dot under it would show darker
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 165.5));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 175.5));
    });

    [AvaloniaFact]
    public void B4_A_Template_Applied_Again_At_The_Same_Size_Ends_The_Dots_At_The_Buffer() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.3 });

        // Buffer 1: nothing moves, so no frame puts it right
        var template = f.Bar.Template;
        f.Bar.Template = null;
        f.Window.UpdateLayout();
        f.Bar.Template = template;
        f.Window.UpdateLayout();
        Assert.False(f.Bar.IsFrameLoopRunning);

        // No dots under the track: a dot at 280 would show darker
        var frame = f.Frame();
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 280));
        AssertSpan((0, W), f.Span("PART_Track"));
        AssertSpan((0, 0.3 * W), f.Span("PART_Progress"));
    });

    // ── B5: the stream moves ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_The_Dots_Stream_Left_10_Px_Every_450_Ms_From_The_Render() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.3, Buffer = 0.75 });

        // Measured: at 225 ms the dots are at 275, 285, …. The frame loop gets there by itself, at 224.
        clock.At(224);
        var frame = f.Frame();
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 285), 8);
        AssertWhite(Fixture.PixelAt(frame, 280));

        // At 450 ms they're back where they started, one pitch on
        clock.At(450);
        f.Bar.Tick();
        frame = f.Frame();
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 290), 8);
        AssertWhite(Fixture.PixelAt(frame, 285));
    });

    [AvaloniaFact]
    public void B5_The_Stream_Runs_While_Hidden_So_It_Comes_Back_In_Phase() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.3 });

        // Measured: after a second at buffer 1, buffer 0.5 shows the stream at 1567 ms, not 0
        clock.At(1000);
        f.Bar.Buffer = 0.5;
        clock.At(1225);
        f.Bar.Tick();
        var frame = f.Frame();
        // 1225 ms is 325 ms into a period: the dots are 7.2 px left of 290, 300, …
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 282.8), 8);
        AssertWhite(Fixture.PixelAt(frame, 287.8));
    });

    // ── B6: value transitions ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_A_Change_Of_Value_Eases_Linearly_Over_150_Ms() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.2 });

        // Not on the first render
        AssertSpan((0, 0.2 * W), f.Span("PART_Progress"));

        clock.At(100);
        f.Bar.Value = 0.8;
        clock.At(175);
        f.Bar.Tick();
        AssertSpan((0, 0.5 * W), f.Span("PART_Progress"));

        // The frame loop finishes it by itself, and stops
        clock.At(400);
        AssertSpan((0, 0.8 * W), f.Span("PART_Progress"));
        Assert.False(f.Bar.IsFrameLoopRunning);
    });

    [AvaloniaFact]
    public void B6_A_Value_Set_Before_The_First_Frame_Does_Not_Ease() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar());

        // Measured: a value set in the task that appends the bar is drawn at once, and one set a frame later eases from 0
        f.Bar.Value = 0.6;
        AssertSpan((0, 0.6 * W), f.Span("PART_Progress"));
        Assert.False(f.Bar.IsFrameLoopRunning);

        clock.At(20);
        f.Bar.Value = 0.2;
        clock.At(95);
        f.Bar.Tick();
        AssertSpan((0, 0.4 * W), f.Span("PART_Progress"));
    });

    [AvaloniaFact]
    public void B6_An_Interrupted_Change_Continues_From_Where_It_Is_And_A_Reversal_Is_Shortened() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.2 });

        // Another target: a fresh 150 ms from the value shown, measured as 42.4 px per 33 ms from 167.2
        clock.At(100);
        f.Bar.Value = 0.8;
        clock.At(175);
        f.Bar.Value = 1;
        clock.At(250);
        f.Bar.Tick();
        AssertSpan((0, 0.75 * W), f.Span("PART_Progress"));

        // Back to where it began: shortened by how far it got, measured as about 67 ms from 0.45 of the way
        clock.At(1000);
        f.Bar.Value = 0.2;
        clock.At(2000);
        f.Bar.Value = 0.8;
        clock.At(2075);
        f.Bar.Value = 0.2;
        clock.At(2112.5);
        f.Bar.Tick();
        AssertSpan((0, 0.35 * W), f.Span("PART_Progress"));
        clock.At(2150);
        f.Bar.Tick();
        AssertSpan((0, 0.2 * W), f.Span("PART_Progress"));
    });

    // ── B7: buffer transitions ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_A_Change_Of_Buffer_Eases_And_The_Dots_Stay_Where_They_Are() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.1, Buffer = 0.75 });

        // One period of the stream after the render, so that the dots are where they began
        clock.At(450);
        f.Bar.Buffer = 0.25;
        clock.At(525);
        f.Bar.Tick();
        AssertSpan((0, 0.5 * W), f.Span("PART_Track"));

        // The window moves, the dots don't: 75 ms more streams them 1.67 px left of 190, 200, …
        var frame = f.Frame();
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 188.3), 8);
        AssertWhite(Fixture.PixelAt(frame, 193.3));
        AssertWhite(Fixture.PixelAt(frame, 183.3));
        // The dot at 178.3 is cut where the window begins, at 179, so the translucent track over it stays the same
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 177));

        clock.At(600);
        f.Bar.Tick();
        AssertSpan((0, 0.25 * W), f.Span("PART_Track"));
    });

    // ── B8: indeterminate ────────────────────────────────────────────────────────────────────

    public static TheoryData<double, double, double, double, double> Indeterminate => new()
    {
        // ms, primary x..xEnd, secondary x..xEnd: measured in Chromium with the animations paused at that time
        { 0, -355.0, -326.4, -31.8, -3.2 },
        { 250, -355.0, -326.4, -14.8, 91.2 },
        { 500, -351.5, -322.9, 19.6, 215.0 },
        { 750, -304.0, -273.1, 76.3, 325.9 },
        { 1000, -216.2, -109.4, 178.5, 417.9 },
        { 1250, -98.8, 108.0, 348.9, 445.5 },
        { 1500, 102.7, 277.5, 465.5, 492.3 },
        { 1750, 289.2, 363.0, 524.1, 537.5 },
        { 3000, -216.2, -109.4, 178.5, 417.9 },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Indeterminate))]
    public void B8_Indeterminate_Slides_Two_Bars_On_A_2_S_Loop(double ms, double primaryX, double primaryEnd, double secondaryX, double secondaryEnd) =>
        InstalledTheme.Run(_ =>
        {
            using var clock = new TestClock();
            using var f = new Fixture(new AvonicProgressBar { Type = ProgressBarType.Indeterminate });

            clock.At(ms);
            f.Bar.Tick();
            AssertSpan((primaryX, primaryEnd), f.Span("PART_ProgressPrimary"));
            AssertSpan((secondaryX, secondaryEnd), f.Span("PART_ProgressSecondary"));
            AssertSpan((0, W), f.Span("PART_Track"));
            Assert.False(f.Part<Control>("PART_Progress").IsVisible);
            Assert.False(f.Part<Control>("PART_Stream").IsVisible);
        });

    [AvaloniaFact]
    public void B8_Indeterminate_Paints_The_Bars_Over_The_Track() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Type = ProgressBarType.Indeterminate });

        // The frame loop runs by itself. At 1008, the secondary bar is 181..421
        clock.At(1008);
        var frame = f.Frame();
        AssertColour(Primary, Fixture.PixelAt(frame, 300));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 100));
        // overflow: hidden. The secondary bar goes on to 421, past the host's end at 358
        AssertWhite(Fixture.PixelAt(frame, 365));
    });

    // ── B9: a change of type ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_A_Change_Of_Type_Restarts_The_Animations_And_Eases_The_Kept_Element() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.25, Buffer = 0.5 });

        // Measured: determinate 0.25 → indeterminate, the track eases from the progress's 89.5 to the full width,
        // and the bars start at 0
        clock.At(1000);
        f.Bar.Type = ProgressBarType.Indeterminate;
        // Measured 67 ms in: the track at 208.8, and the secondary bar inside it, scaled with it, at -17.6..6.7
        clock.At(1067);
        f.Bar.Tick();
        AssertSpan((0, 208.8), f.Span("PART_Track"), 1);
        AssertSpan((-17.6, 6.7), f.Span("PART_ProgressSecondary"), 1);
        // and the primary bar, at its time-0 span of -355.0..-326.4, scaled with the track by 0.585
        AssertSpan((-207.7, -190.9), f.Span("PART_ProgressPrimary"), 1);
        clock.At(1075);
        f.Bar.Tick();
        AssertSpan((0, 223.75), f.Span("PART_Track"));
        clock.At(1250);
        f.Bar.Tick();
        AssertSpan((-14.8, 91.2), f.Span("PART_ProgressSecondary"));
        AssertSpan((0, W), f.Span("PART_Track"));

        // Measured: indeterminate → determinate 0.25, the progress eases from the full width, the track doesn't ease,
        // and the stream starts at 0
        f.Bar.Type = ProgressBarType.Determinate;
        clock.At(1325);
        f.Bar.Tick();
        AssertSpan((0, 223.75), f.Span("PART_Progress"));
        AssertSpan((0, 179), f.Span("PART_Track"));
        clock.At(1475);
        f.Bar.Tick();
        AssertSpan((0, 89.5), f.Span("PART_Progress"));
        var frame = f.Frame();
        // 225 ms after the render: dots at 185, 195, …
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 195), 8);
        AssertWhite(Fixture.PixelAt(frame, 190));

        // The track is a new element in each render, so a change of buffer in flight doesn't carry over a change of type
        f.Bar.Buffer = 1;
        clock.At(1500);
        f.Bar.Type = ProgressBarType.Indeterminate;
        f.Bar.Type = ProgressBarType.Determinate;
        AssertSpan((0, W), f.Span("PART_Track"));
    });

    // ── B10: reversed ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Reversed_Mirrors_The_Bar_About_Its_Centre() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.3, Buffer = 0.6, Reversed = true });

        // Measured: the progress at 250.6..358, the track at 143.2..358, the dots from 0 to 143.2
        AssertSpan((250.6, W), f.Span("PART_Progress"));
        AssertSpan((143.2, W), f.Span("PART_Track"));
        var frame = f.Frame();
        AssertColour(Primary, Fixture.PixelAt(frame, 252));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 200));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 78), 8);
        AssertWhite(Fixture.PixelAt(frame, 73));

        // And the stream runs the other way: right, 10 px every 450 ms
        clock.At(225);
        f.Bar.Tick();
        frame = f.Frame();
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 83), 8);
        AssertWhite(Fixture.PixelAt(frame, 78));

        f.Bar.Reversed = false;
        AssertSpan((0, 107.4), f.Span("PART_Progress"));
    });

    [AvaloniaFact]
    public void B10_An_Apps_Transform_Replaces_The_Reversal() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.3, Reversed = true });

        f.Bar.RenderTransform = new TranslateTransform(0, 0);
        AssertSpan((0, 107.4), f.Span("PART_Progress"));
    });

    [AvaloniaFact]
    public void B10_An_Apps_Style_Transform_Against_A_Later_Reversal() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.3 });
        f.Window.Styles.Add(new Avalonia.Styling.Style(x => x.OfType<AvonicProgressBar>())
        {
            Setters = { new Avalonia.Styling.Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0)) },
        });
        Dispatcher.UIThread.RunJobs();

        f.Bar.Reversed = true;
        AssertSpan((0, 107.4), f.Span("PART_Progress"));

        // and when the bar arrives after the style, as it does in an app
        f.Bar.Reversed = false;
        f.Host.Children.Remove(f.Bar);
        f.Host.Children.Add(f.Bar);
        Dispatcher.UIThread.RunJobs();
        f.Bar.Reversed = true;
        AssertSpan((0, 107.4), f.Span("PART_Progress"));
        f.Bar.Reversed = false;
        f.Bar.Reversed = true;
        AssertSpan((0, 107.4), f.Span("PART_Progress"));

        // and with the app's style gone, the reversal shows
        f.Window.Styles.RemoveAt(f.Window.Styles.Count - 1);
        Dispatcher.UIThread.RunJobs();
        AssertSpan((250.6, 358), f.Span("PART_Progress"));
    });

    // ── B11: right to left ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Right_To_Left_Reverses_The_Bar_And_Reversed_Puts_It_Back() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var around = new Border { FlowDirection = FlowDirection.RightToLeft };
        using var f = new Fixture(new AvonicProgressBar { Value = 0.3, Buffer = 0.6 }, around);

        // The pixels, as the screen shows them: a mirrored subtree in Avalonia, and a scaleX(-1) in Ionic
        var frame = f.Frame();
        AssertColour(Primary, Fixture.PixelAt(frame, 252));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 200));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 78), 8);
        AssertWhite(Fixture.PixelAt(frame, 73));

        f.Bar.Reversed = true;
        frame = f.Frame();
        AssertColour(Primary, Fixture.PixelAt(frame, 105));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 200));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 280), 8);
        AssertWhite(Fixture.PixelAt(frame, 285));

        around.FlowDirection = FlowDirection.LeftToRight;
        frame = f.Frame();
        AssertColour(Primary, Fixture.PixelAt(frame, 252));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 78), 8);
    });

    // ── B12: colour ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_Color_Paints_The_Progress_In_The_Base_And_The_Track_And_Dots_At_0_3() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.5, Buffer = 0.75, Color = "danger" });

        // Measured: progress rgb(197, 0, 15); track and dots rgba(197, 0, 15, 0.3), which is 238, 179, 183 on white
        var danger = Color.FromRgb(197, 0, 15);
        var dangerTrack = Color.FromRgb(238, 179, 183);
        var frame = f.Frame();
        AssertColour(danger, Fixture.PixelAt(frame, 100));
        AssertColour(dangerTrack, Fixture.PixelAt(frame, 200));
        AssertColour(dangerTrack, Fixture.PixelAt(frame, 290), 8);

        f.Bar.Type = ProgressBarType.Indeterminate;
        clock.At(1000);
        f.Bar.Tick();
        frame = f.Frame();
        AssertColour(danger, Fixture.PixelAt(frame, 300));
        AssertColour(dangerTrack, Fixture.PixelAt(frame, 100));

        // At 1500 the primary bar is at 102.7..277.5, and the secondary one is past the end
        clock.At(1500);
        f.Bar.Tick();
        frame = f.Frame();
        AssertColour(danger, Fixture.PixelAt(frame, 200));
        AssertColour(dangerTrack, Fixture.PixelAt(frame, 50));
    });

    [AvaloniaFact]
    public void B12_No_Color_In_A_Coloured_Ancestor_Keeps_The_Defaults_And_An_Unknown_One_Draws_Nothing() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var around = new Border();
        Avonic.Components.Behaviors.ColorPalette.SetColor(around, "danger");
        using var f = new Fixture(new AvonicProgressBar { Value = 0.5, Buffer = 0.75 }, around);

        var frame = f.Frame();
        AssertColour(Primary, Fixture.PixelAt(frame, 100));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 200));
        AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 290), 8);

        // var() of an unset variable is invalid at computed-value time: no background
        Avonic.Components.Behaviors.ColorPalette.SetColor(around, null);
        f.Bar.Color = "nonexistent";
        frame = f.Frame();
        AssertWhite(Fixture.PixelAt(frame, 100));
        AssertWhite(Fixture.PixelAt(frame, 200));
        AssertWhite(Fixture.PixelAt(frame, 290));
    });

    // ── B13: host classes ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_Host_Classes_Follow_Type_Direction_And_A_Buffer_Of_Exactly_1()
    {
        var bar = new AvonicProgressBar();
        Assert.Contains(":type-determinate", bar.Classes);
        Assert.DoesNotContain(":type-indeterminate", bar.Classes);
        Assert.Contains(":solid", bar.Classes);
        Assert.DoesNotContain(":reversed", bar.Classes);

        bar.Type = ProgressBarType.Indeterminate;
        Assert.Contains(":type-indeterminate", bar.Classes);
        Assert.DoesNotContain(":type-determinate", bar.Classes);

        // buffer === 1, before clamping
        bar.Buffer = 1.5;
        Assert.DoesNotContain(":solid", bar.Classes);
        bar.Buffer = 1;
        Assert.Contains(":solid", bar.Classes);

        bar.Reversed = true;
        Assert.Contains(":reversed", bar.Classes);
        bar.FlowDirection = FlowDirection.RightToLeft;
        Assert.DoesNotContain(":reversed", bar.Classes);
        bar.Reversed = false;
        Assert.Contains(":reversed", bar.Classes);
    }

    // ── B14: AvonicConfig.Animated ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_Animated_Off_Snaps_Changes_And_Stands_The_Animations_At_0() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        AvonicConfig.Animated = false;
        try
        {
            using var f = new Fixture(new AvonicProgressBar { Value = 0.2, Buffer = 0.75 });

            f.Bar.Value = 0.8;
            AssertSpan((0, 0.8 * W), f.Span("PART_Progress"));
            Assert.False(f.Bar.IsFrameLoopRunning);

            clock.At(225);
            f.Bar.Tick();
            var frame = f.Frame();
            AssertColour(PrimaryTrack, Fixture.PixelAt(frame, 300), 8);
            AssertWhite(Fixture.PixelAt(frame, 305));

            f.Bar.Type = ProgressBarType.Indeterminate;
            AssertSpan((0, W), f.Span("PART_Track"));
            clock.At(1000);
            f.Bar.Tick();
            AssertSpan((-355.0, -326.4), f.Span("PART_ProgressPrimary"));
            AssertSpan((-31.8, -3.2), f.Span("PART_ProgressSecondary"));
            Assert.False(f.Bar.IsFrameLoopRunning);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    });

    // ── B15: the resource keys ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_The_Two_Keys_Paint_The_Track_The_Dots_And_The_Progress() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.5, Buffer = 0.75 });

        var background = Color.FromRgb(0, 160, 0);
        var progress = Color.FromRgb(160, 0, 160);
        f.Window.Resources["Avonic.ProgressBar.Background"] = new SolidColorBrush(background);
        f.Window.Resources["Avonic.ProgressBar.ProgressBackground"] = new SolidColorBrush(progress);

        var frame = f.Frame();
        AssertColour(progress, Fixture.PixelAt(frame, 100));
        AssertColour(background, Fixture.PixelAt(frame, 200));
        AssertColour(background, Fixture.PixelAt(frame, 290), 8);

        f.Bar.Type = ProgressBarType.Indeterminate;
        clock.At(1000);
        f.Bar.Tick();
        frame = f.Frame();
        AssertColour(progress, Fixture.PixelAt(frame, 300));
        AssertColour(background, Fixture.PixelAt(frame, 100));
    });

    // ── B16: detaching ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B16_Detaching_Stops_The_Frames_And_Attaching_Starts_The_Animations_From_0() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Type = ProgressBarType.Indeterminate });

        clock.At(500);
        Assert.True(f.Bar.IsFrameLoopRunning);
        f.Host.Children.Remove(f.Bar);
        Assert.False(f.Bar.IsFrameLoopRunning);

        clock.At(1000);
        f.Host.Children.Add(f.Bar);
        Dispatcher.UIThread.RunJobs();
        Assert.True(f.Bar.IsFrameLoopRunning);
        clock.At(1250);
        f.Bar.Tick();
        AssertSpan((-14.8, 91.2), f.Span("PART_ProgressSecondary"));
    });

    [AvaloniaFact]
    public void B16_A_Bar_Attached_While_Hidden_Starts_Its_Animations_When_Shown() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();

        // Hidden itself: Avalonia doesn't measure it, so it has no template yet
        using (var f = new Fixture(new AvonicProgressBar { Type = ProgressBarType.Indeterminate, IsVisible = false }))
        {
            clock.At(1300);
            Assert.False(f.Bar.IsFrameLoopRunning);
            f.Bar.IsVisible = true;
            clock.At(1550);
            f.Bar.Tick();
            AssertSpan((-14.8, 91.2), f.Span("PART_ProgressSecondary"));
        }

        // Inside a hidden ancestor
        var around = new Border { IsVisible = false };
        using (var f = new Fixture(new AvonicProgressBar { Type = ProgressBarType.Indeterminate }, around))
        {
            clock.At(2800);
            Assert.False(f.Bar.IsFrameLoopRunning);
            around.IsVisible = true;
            clock.At(3050);
            f.Bar.Tick();
            AssertSpan((-14.8, 91.2), f.Span("PART_ProgressSecondary"));
        }
    });

    [AvaloniaFact]
    public void B16_Hiding_Stops_The_Frames_And_Showing_Starts_The_Animations_From_0() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Type = ProgressBarType.Indeterminate, Value = 0.3 });

        // display: none on an ancestor stops the animations, as on the bar
        f.Host.IsVisible = false;
        clock.At(500);
        Assert.False(f.Bar.IsFrameLoopRunning);

        clock.At(1000);
        f.Host.IsVisible = true;
        Assert.True(f.Bar.IsFrameLoopRunning);
        clock.At(1250);
        f.Bar.Tick();
        AssertSpan((-14.8, 91.2), f.Span("PART_ProgressSecondary"));

        // Hiding and showing within one frame, the bar or an ancestor, restarts nothing: measured in Chromium, where
        // display: none and back in one task leave every animation's currentTime as it was, as no style change sees it
        clock.At(1300);
        f.Bar.IsVisible = false;
        f.Bar.IsVisible = true;
        f.Host.IsVisible = false;
        f.Host.IsVisible = true;
        clock.At(1500);
        f.Bar.Tick();
        AssertSpan((19.6, 215.0), f.Span("PART_ProgressSecondary"));

        // and a change made while hidden doesn't ease
        f.Bar.IsVisible = false;
        clock.At(1600);
        f.Bar.Type = ProgressBarType.Determinate;
        f.Bar.IsVisible = true;
        AssertSpan((0, 0.3 * W), f.Span("PART_Progress"));
    });

    // ── B17: frames only while something moves ──────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_Frames_Run_Only_While_Something_Moves() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        using var f = new Fixture(new AvonicProgressBar { Value = 0.5 });

        // A solid determinate bar is still
        Assert.False(f.Bar.IsFrameLoopRunning);

        // The stream moves while it shows
        f.Bar.Buffer = 0.75;
        clock.At(1000);
        Assert.True(f.Bar.IsFrameLoopRunning);

        // and stops once the window has closed
        f.Bar.Buffer = 1;
        clock.At(1200);
        Assert.False(f.Bar.IsFrameLoopRunning);

        f.Bar.Type = ProgressBarType.Indeterminate;
        clock.At(5000);
        Assert.True(f.Bar.IsFrameLoopRunning);
    });
}
