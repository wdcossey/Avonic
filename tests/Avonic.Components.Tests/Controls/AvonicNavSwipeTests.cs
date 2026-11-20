using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicNavTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// N23 in docs/components/ion-nav/PLAN.md: the swipe back (<c>swipe-back.js</c>, <c>nav.js</c>'s <c>canStart</c>, <c>onStart</c>,
/// <c>onMove</c>, <c>onEnd</c>), off by default as in md. A 390-wide window; a finger moved every 16 ms.
/// </summary>
public class AvonicNavSwipeTests
{
    /// <summary>A nav with two pages, settled at 1000 and the log cleared.</summary>
    private static Stage TwoDeep(Action<AvonicNav>? set = null)
    {
        var stage = new Stage(nav =>
        {
            nav.Root = new TestPage("one");
            set?.Invoke(nav);
        });
        stage.Done(Task.FromResult(true));
        stage.Stack(new TestPage("two"));
        stage.At(1000);
        stage.Log.Clear();
        return stage;
    }

    private static Control Top(Stage stage) => stage.Nav.Pages.Single(p => p.Name == "two");

    /// <summary>
    /// A finger down at <paramref name="x"/> at 1000, moved by <paramref name="step"/> toward <paramref name="to"/> every 16 ms,
    /// each move's frame run; then lifted 16 ms after the last, unless <paramref name="lift"/> is false. The top page's
    /// <c>translateY</c> after each move.
    /// </summary>
    private static List<double> Swipe(Stage stage, double x, double to, double step, bool lift = true) =>
        Swipe(stage, x, to, step, out _, lift);

    private static List<double> Swipe(Stage stage, double x, double to, double step, out Gestures.GestureTests.Finger finger, bool lift = true)
    {
        var start = Math.Max(1000, stage.Clock.Now.TotalMilliseconds);
        finger = new Gestures.GestureTests.Finger(stage.Scene, stage.Clock).Down(x, 400, start);
        var seen = new List<double>();
        var at = start;
        while (Math.Abs(to - x) > 1e-9)
        {
            x += Math.Sign(to - x) * Math.Min(step, Math.Abs(to - x));
            at += 16;
            finger.Move(x, 400, at);
            stage.At(at + 16);
            seen.Add(TranslateY(Top(stage)));
        }

        if (lift)
        {
            finger.Up(x, 400, at + 16);
            Stage.Settle();
        }

        return seen;
    }

    [AvaloniaFact]
    public void N23_Off_by_default_as_md_has_it_and_the_config_turns_it_on_for_a_nav_with_none()
    {
        using (var stage = TwoDeep())
        {
            Assert.False(stage.Nav.NavSwipeGesture);
            Swipe(stage, 10, 300, 10);
            stage.After(1000);
            Assert.Equal(2, stage.Nav.GetLength());
        }

        AvonicConfig.SwipeBackEnabled = true;
        try
        {
            using (var stage = TwoDeep())
            {
                Assert.True(stage.Nav.NavSwipeGesture);
            }

            using (var stage = TwoDeep(nav => nav.NavSwipeGesture = false))
            {
                Assert.False(stage.Nav.NavSwipeGesture);
            }
        }
        finally
        {
            AvonicConfig.SwipeBackEnabled = false;
        }
    }

    [AvaloniaFact]
    public void N23_A_swipe_from_the_edge_drives_the_back_transition_linearly_and_completes_past_half()
    {
        using var stage = TwoDeep(nav => nav.NavSwipeGesture = true);
        var seen = Swipe(stage, 10, 260, 10, out var finger, lift: false);

        // Held linear at the drag's share of the width, past the 10 of the threshold.
        var last = seen[^1];
        Assert.Equal(40 * ((260 - 10.0 - 10) / 390), last, 0);
        Assert.True(stage.Nav.Pages.Single(p => p.Name == "one").IsVisible);
        Assert.Contains(stage.Log, l => l.StartsWith("willLeave two", StringComparison.Ordinal));

        finger.Up(260, 400, stage.Clock.Now.TotalMilliseconds);
        stage.After(800);
        Assert.Equal(["one"], Names(stage.Nav));
        Assert.Contains(stage.Log, l => l.StartsWith("didEnter one", StringComparison.Ordinal));
    }

    [AvaloniaFact]
    public void N23_A_short_slow_swipe_goes_back_to_the_page_it_left()
    {
        using var stage = TwoDeep(nav => nav.NavSwipeGesture = true);
        Swipe(stage, 10, 60, 1);
        stage.After(1000);
        Assert.Equal(["one", "two"], Names(stage.Nav));
        var two = Top(stage);
        Assert.True(two.IsVisible);
        Assert.Equal(0, TranslateY(two));
        Assert.Equal(1, GroupOpacity(two));
        Assert.False(stage.Nav.Pages.Single(p => p.Name == "one").IsVisible);
        Assert.DoesNotContain(stage.Log, l => l.StartsWith("didEnter", StringComparison.Ordinal));

        // And it can swipe again.
        Swipe(stage, 10, 300, 10);
        stage.After(1000);
        Assert.Equal(["one"], Names(stage.Nav));
    }

    [AvaloniaFact]
    public void N23_Only_from_the_start_edge_and_the_end_edge_in_RTL()
    {
        using (var stage = TwoDeep(nav => nav.NavSwipeGesture = true))
        {
            Swipe(stage, 60, 350, 10);
            stage.After(1000);
            Assert.Equal(2, stage.Nav.GetLength());
        }

        using (var stage = TwoDeep(nav => { nav.NavSwipeGesture = true; nav.FlowDirection = FlowDirection.RightToLeft; }))
        {
            Swipe(stage, 10, 300, 10);
            stage.After(1000);
            Assert.Equal(2, stage.Nav.GetLength());

            Swipe(stage, 380, 90, 10);
            stage.After(1000);
            Assert.Equal(1, stage.Nav.GetLength());
        }
    }

    [AvaloniaFact]
    public void N23_Not_at_the_root_nor_while_a_transition_runs()
    {
        using var stage = new Stage(nav => { nav.Root = new TestPage("one"); nav.NavSwipeGesture = true; });
        stage.Done(Task.FromResult(true));
        stage.At(1000);
        var finger = new Gestures.GestureTests.Finger(stage.Scene, stage.Clock).Down(10, 400, 1000);
        finger.Move(100, 400, 1016).Move(200, 400, 1032).Up(200, 400, 1048);
        stage.After(600);
        Assert.Empty(stage.Log);
    }

    [AvaloniaFact]
    public void N23_With_animations_off_it_pops_at_the_end_and_blocks_clicks_meanwhile()
    {
        using var stage = TwoDeep(nav => { nav.NavSwipeGesture = true; nav.Animated = false; });
        Swipe(stage, 10, 300, 10, out var finger, lift: false);
        Assert.Equal(2, stage.Nav.GetLength());
        Assert.False(stage.Nav.IsHitTestVisible);

        finger.Up(300, 400, stage.Clock.Now.TotalMilliseconds);
        stage.After(600);
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.True(stage.Nav.IsHitTestVisible);
    }
}
