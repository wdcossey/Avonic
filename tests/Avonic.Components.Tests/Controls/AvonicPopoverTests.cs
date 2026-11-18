using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using Avonic.Themes;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-popover</c>, md, on the test clock: B1 to B18 in docs/components/ion-popover/PLAN.md, from <c>popover.js</c>,
/// <c>utils.js</c>, <c>md.enter.js</c>, <c>md.leave.js</c>, <c>popover.md.css</c> and <c>ref/measurements.txt</c>. Frames come
/// every 16 ms, so the enter animation, played at 0, shows local time t at 16 + t and finishes at the frame at 320; the leave,
/// 150 ms, at 176.
/// </summary>
public class AvonicPopoverTests
{
    internal const double Entered = 320;
    internal const double Left = 176;

    private static readonly CubicBezierEasing EnterEasing = new(0.36, 0.66, 0.04, 1);
    private static readonly CubicBezierEasing LeaveEasing = new(0.25, 0.1, 0.25, 1);

    /// <summary>
    /// A page in a phone-sized window with <c>SimpleTheme</c>, whose template has the overlay layer, and <c>AvonicTheme</c>: a
    /// <see cref="Canvas"/>, so that a trigger can go anywhere, as the measurements' <c>position: fixed</c> buttons do.
    /// </summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Avalonia.Themes.Simple.SimpleTheme _simple = new();
        private readonly AvonicTheme _theme = new();

        public Stage(double width = 390, double height = 844)
        {
            Application.Current!.Styles.Insert(0, _simple);
            Application.Current.Styles.Add(_theme);
            Clock = new TestClock();
            Page = new Canvas();
            NameScope.SetNameScope(Page, Names);
            Scene = new Scene(Page, width, height);
        }

        /// <summary>The page's name scope, as XAML gives a page one, where a popover looks its trigger up.</summary>
        public NameScope Names { get; } = new();

        public TestClock Clock { get; }

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public Canvas Page { get; }

        public List<string> Log { get; } = [];

        public IInputElement? Focused => Window.FocusManager!.GetFocusedElement();

        /// <summary>A 100 by 40 box at <paramref name="x"/>, <paramref name="y"/>, as the measurements' triggers.</summary>
        public Border Trigger(double x, double y, double width = 100, double height = 40, string name = "Trigger")
        {
            var trigger = new Border { Name = name, Width = width, Height = height, Background = Brushes.LightGray };
            Canvas.SetLeft(trigger, x);
            Canvas.SetTop(trigger, y);
            Page.Children.Add(trigger);
            Names.Register(name, trigger);
            Settle();
            return trigger;
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

        /// <summary>Runs on: the clock to <paramref name="ms"/> past now.</summary>
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

        public AvonicPopover Watch(AvonicPopover popover, string name = "")
        {
            string Now() => $"@{Clock.Now.TotalMilliseconds}";
            popover.IonPopoverWillPresent += (_, _) => Log.Add($"{name}ionPopoverWillPresent{Now()}");
            popover.WillPresent += (_, _) => Log.Add($"{name}willPresent{Now()}");
            popover.IonPopoverDidPresent += (_, _) => Log.Add($"{name}ionPopoverDidPresent{Now()}");
            popover.DidPresent += (_, _) => Log.Add($"{name}didPresent{Now()}");
            popover.IonPopoverWillDismiss += (_, e) => Log.Add($"{name}ionPopoverWillDismiss({e.Data},{e.Role}){Now()}");
            popover.WillDismiss += (_, e) => Log.Add($"{name}willDismiss({e.Data},{e.Role}){Now()}");
            popover.IonPopoverDidDismiss += (_, e) => Log.Add($"{name}ionPopoverDidDismiss({e.Data},{e.Role}){Now()}");
            popover.DidDismiss += (_, e) => Log.Add($"{name}didDismiss({e.Data},{e.Role}){Now()}");
            return popover;
        }

        public void Key(PhysicalKey key)
        {
            Window.KeyPressQwerty(key, RawInputModifiers.None);
            Window.KeyReleaseQwerty(key, RawInputModifiers.None);
            Settle();
        }

        public void Click(Point point, MouseButton button = MouseButton.Left)
        {
            Render();
            Window.MouseDown(point, button);
            Window.MouseUp(point, button);
            Settle();
        }

        public void Move(Point point)
        {
            Render();
            Window.MouseMove(point);
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

    /// <summary>A popover of <paramref name="height"/> px of content, as the measurements' <c>&lt;div style="height:120px"&gt;</c>.</summary>
    internal static AvonicPopover Popover(double height = 120) => new() { Content = new Border { Height = height } };

    /// <summary>The content's box in the window, as <c>getBoundingClientRect</c> reads it.</summary>
    internal static Rect Box(AvonicPopover popover, Visual relativeTo)
    {
        var content = popover.ContentPart!;
        return new Rect(content.Bounds.Size).TransformToAABB(content.TransformToVisual(relativeTo)!.Value);
    }

    internal static double Scale(Visual element) => element.RenderTransform?.Value.M11 ?? 1;

    /// <summary>A finished task's result. Never <c>.Result</c> on one still running: it would block the UI thread it waits for.</summary>
    internal static T Done<T>(Task<T> task)
    {
        Assert.True(task.IsCompleted, "The task hasn't finished.");
        return task.Result;
    }

    // ── B1, B2: md's animations ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Enter_Is_Mds_Over_300ms_With_A_150ms_Wrapper()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 400);
        var popover = Popover();
        popover.Present(new PopoverEvent(trigger));
        Stage.Settle();
        var backdrop = popover.Backdrop!;
        var wrapper = popover.Wrapper!;
        var content = popover.ContentPart!;

        Assert.Equal(0.01, backdrop.Opacity, 6);
        Assert.Equal(0.01, wrapper.Opacity, 6);
        Assert.Equal(0.8, Scale(content), 6);
        Assert.False(backdrop.IsHitTestVisible);

        // M1: at 22 ms Chromium had the wrapper at 0.2518 and the scale at 0.82212, a frame of its own after 6.
        foreach (var local in new[] { 32.0, 96, 144, 224 })
        {
            stage.At(16 + local);
            var e = EnterEasing.Ease(local / 300);
            Assert.Equal(0.01 + (0.32 - 0.01) * e, backdrop.Opacity, 4);
            Assert.Equal(local < 150 ? 0.01 + 0.99 * EnterEasing.Ease(local / 150) : 1, wrapper.Opacity, 4);
            Assert.Equal(0.8 + 0.2 * e, Scale(content), 4);
            Assert.False(backdrop.IsHitTestVisible);
        }

        stage.At(Entered);
        Assert.Equal(0.32, backdrop.Opacity, 6);
        Assert.Equal(1, wrapper.Opacity, 6);
        Assert.Equal(1, Scale(content), 6);
        Assert.True(backdrop.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void B1_Not_Animated_The_Wrapper_Still_Fades_Over_Its_Own_150ms()
    {
        // M1b: duration(0) is the parent's; the wrapper keeps its own 150 ms, the backdrop and the scale are at their ends at once.
        using var stage = new Stage();
        var popover = Popover();
        popover.Animated = false;
        popover.Present(stage.Window);
        stage.At(16);
        Assert.Equal(0.32, popover.Backdrop!.Opacity, 6);
        Assert.Equal(1, Scale(popover.ContentPart!), 6);
        stage.At(16 + 64);
        Assert.Equal(0.01 + 0.99 * EnterEasing.Ease(64 / 150.0), popover.Wrapper!.Opacity, 4);
        stage.At(176);
        Assert.Equal(1, popover.Wrapper.Opacity, 6);
    }

    [AvaloniaFact]
    public void B1_The_Scale_Grows_From_The_Origin_The_Side_And_Alignment_Give()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 200);
        foreach (var (side, alignment, x, y) in new[]
                 {
                     (PopoverSide.Bottom, PopoverAlignment.Center, 0.5, 0.0),
                     (PopoverSide.Top, PopoverAlignment.Center, 0.5, 1.0),
                     (PopoverSide.Left, PopoverAlignment.End, 0.0, 1.0),  // clamped to the left edge: x left
                     (PopoverSide.Right, PopoverAlignment.Center, 1.0, 0.5), // clamped to the right edge: x right
                 })
        {
            var popover = Popover();
            popover.Side = side;
            popover.Alignment = alignment;
            popover.Present(new PopoverEvent(trigger));
            stage.After(Entered);
            var origin = popover.ContentPart!.RenderTransformOrigin;
            Assert.Equal(new RelativePoint(x, y, RelativeUnit.Relative), origin);
            popover.Dismiss();
            stage.After(Left);
        }
    }

    [AvaloniaFact]
    public void B1_Backdrop_Opacity_Follows_The_Popovers_Key_Then_The_Global_One()
    {
        using var stage = new Stage();
        var popover = Popover();
        stage.Window.Resources["Avonic.BackdropOpacity"] = 0.5;
        popover.Present(stage.Window);
        stage.At(Entered);
        Assert.Equal(0.5, popover.Backdrop!.Opacity, 6);
        popover.Dismiss();
        stage.After(Left);

        popover.Resources["Avonic.Popover.BackdropOpacity"] = 0.2;
        popover.Present(stage.Window);
        stage.After(Entered);
        Assert.Equal(0.2, popover.Backdrop!.Opacity, 6);
    }

    [AvaloniaFact]
    public void B2_Leave_Is_Mds_Over_150ms_And_Takes_The_Place_Away()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 400);
        var popover = Popover();
        popover.Present(new PopoverEvent(trigger));
        stage.At(Entered);
        var backdrop = popover.Backdrop!;
        var wrapper = popover.Wrapper!;
        Assert.NotNull(wrapper.Place);

        var start = stage.Clock.Now.TotalMilliseconds;
        var dismissed = popover.Dismiss("d", "ok");
        Stage.Settle();
        Assert.Equal(0.32, backdrop.Opacity, 6);
        Assert.Equal(0.99, wrapper.Opacity, 6);
        foreach (var local in new[] { 16.0, 64, 112 })
        {
            stage.At(start + 16 + local);
            var e = LeaveEasing.Ease(local / 150);
            Assert.Equal(0.32 * (1 - e), backdrop.Opacity, 4);
            Assert.Equal(0.99 * (1 - e), wrapper.Opacity, 4);
            Assert.Equal(1, Scale(popover.ContentPart!), 6);
            Assert.NotNull(wrapper.Place);
        }

        stage.At(start + Left);
        Assert.True(Done(dismissed));
        Assert.Null(wrapper.Place);
        Assert.Equal(0, wrapper.Opacity, 6);
        Assert.Equal(new RelativePoint(0, 0, RelativeUnit.Relative), popover.ContentPart!.RenderTransformOrigin);
        Assert.Null(popover.GetVisualParent());
    }

    // ── B3: events ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_The_Popovers_Events_Come_Before_The_Generic_Ones()
    {
        using var stage = new Stage();
        var popover = stage.Watch(Popover());
        popover.Present(stage.Window);
        stage.At(Entered);
        popover.Dismiss(7, "ok");
        stage.After(Left);

        Assert.Equal(
        [
            "ionPopoverWillPresent@0", "willPresent@0", "ionPopoverDidPresent@320", "didPresent@320",
            "ionPopoverWillDismiss(7,ok)@320", "willDismiss(7,ok)@320", "ionPopoverDidDismiss(7,ok)@496", "didDismiss(7,ok)@496",
        ], stage.Log);
    }

    [AvaloniaFact]
    public void B3_The_Popovers_Events_Bubble()
    {
        using var stage = new Stage();
        var popover = Popover();
        var heard = new List<string>();
        stage.Page.Children.Add(popover);
        stage.Page.AddHandler(AvonicPopover.IonPopoverWillPresentEvent, (_, _) => heard.Add("ionPopoverWillPresent"));
        stage.Window.AddHandler(AvonicPopover.IonPopoverDidDismissEvent, (_, _) => heard.Add("ionPopoverDidDismiss"));
        popover.Present();
        stage.At(Entered);
        popover.Dismiss();
        stage.After(Left);

        // Teleported to the overlay layer, it bubbles there, to the window.
        Assert.Equal(["ionPopoverDidDismiss"], heard);
    }
}
