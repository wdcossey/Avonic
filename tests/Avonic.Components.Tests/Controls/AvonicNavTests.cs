using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
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
using Avonic.Themes;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-nav</c>, md, on the test clock: N1 to N22 in docs/components/ion-nav/PLAN.md, from <c>nav.js</c>,
/// <c>utils/transition</c>, <c>md.transition.js</c> and <c>ref/measurements.txt</c>. Frames come every 16 ms. A push of a new
/// page waits a frame for it (Ionic's <c>componentOnReady</c>), so at 16 its will-events come and its animation plays, which
/// starts at the frame at 32 and ends at the first frame past 312: 320. A pop of a page already built plays at once: it starts at
/// 16 and ends at 224.
/// </summary>
public class AvonicNavTests
{
    internal const double PushDone = 320;
    internal const double PopDone = 224;

    internal static readonly CubicBezierEasing ForwardEasing = new(0.36, 0.66, 0.04, 1);
    internal static readonly CubicBezierEasing BackEasing = new(0.47, 0, 0.745, 0.715);

    /// <summary>A page that logs its lifecycle events to the stage that's running.</summary>
    internal class TestPage : Border
    {
        public static readonly StyledProperty<int> CountProperty = AvaloniaProperty.Register<TestPage, int>(nameof(Count));

        public TestPage(string name)
        {
            Name = name;
            Background = Brushes.White;
            Stage.Watch(this);
            Constructed?.Invoke(this);
        }

        public TestPage()
            : this("page")
        {
        }

        public static Action<TestPage>? Constructed { get; set; }

        public string? Label { get; set; }

        public int Count
        {
            get => GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        /// <summary>What <see cref="Label"/> and <see cref="Count"/> were when the page joined the tree.</summary>
        public string? AtAttach { get; private set; }

        protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            AtAttach = $"{Label}/{Count}";
        }
    }

    internal sealed class PageOne() : TestPage("one");

    internal sealed class PageTwo() : TestPage("two");

    internal sealed class PageThree() : TestPage("three");

    /// <summary>A view model, whose page a data template builds.</summary>
    internal sealed record DetailViewModel(string Title);

    /// <summary>A nav filling a 390 x 844 window with <c>SimpleTheme</c> and <c>AvonicTheme</c>, on a test clock.</summary>
    internal sealed class Stage : IDisposable
    {
        private readonly Avalonia.Themes.Simple.SimpleTheme _simple = new();
        private readonly AvonicTheme _theme = new();

        public Stage(Action<AvonicNav>? set = null, bool attach = true, Func<AvonicNav, Control>? wrap = null)
        {
            Application.Current!.Styles.Insert(0, _simple);
            Application.Current.Styles.Add(_theme);
            Clock = new TestClock();
            Current = this;
            Nav = new AvonicNav();
            Nav.IonNavWillChange += (_, _) => Log.Add($"ionNavWillChange{T}");
            Nav.IonNavDidChange += (_, _) => Log.Add($"ionNavDidChange{T}");
            set?.Invoke(Nav);
            Root = wrap?.Invoke(Nav) ?? Nav;
            Scene = new Scene(attach ? Root : new Panel());
        }

        public static Stage? Current { get; private set; }

        public TestClock Clock { get; }

        public Scene Scene { get; }

        public Window Window => Scene.Window;

        public Control Root { get; }

        public AvonicNav Nav { get; }

        public List<string> Log { get; } = [];

        private string T => $"@{Clock.Now.TotalMilliseconds}";

        public void Attach() => ((Panel)Window.Content!).Children.Add(Root);

        /// <summary>Logs a page's lifecycle events to the stage running when each comes.</summary>
        public static void Watch(Control page)
        {
            void On(RoutedEvent<RoutedEventArgs> e, string what) =>
                page.AddHandler(e, (_, _) => Current?.Log.Add($"{what} {page.Name ?? page.GetType().Name}{Current.T}"));
            On(AvonicNav.IonViewWillEnterEvent, "willEnter");
            On(AvonicNav.IonViewDidEnterEvent, "didEnter");
            On(AvonicNav.IonViewWillLeaveEvent, "willLeave");
            On(AvonicNav.IonViewDidLeaveEvent, "didLeave");
            On(AvonicNav.IonViewWillUnloadEvent, "willUnload");
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

        /// <summary>Runs a call to its end, 600 ms on, and clears the log.</summary>
        public T Done<T>(Task<T> task)
        {
            After(600);
            Assert.True(task.IsCompleted, "the call didn't end");
            Log.Clear();
            return task.Result;
        }

        /// <summary>The nav with <paramref name="pages"/> pushed, one by one, unanimated, and the log cleared.</summary>
        public void Stack(params Control[] pages)
        {
            foreach (var page in pages)
            {
                Done(Nav.Push(page, opts: new NavOptions { Animated = false }));
            }
        }

        public void Render()
        {
            for (var i = 0; i < 2; i++)
            {
                Settle();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            Settle();
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
            Application.Current!.Styles.Remove(_simple);
            Application.Current.Styles.Remove(_theme);
            Current = null;
            TestPage.Constructed = null;
        }
    }

    /// <summary>The page's opacity as a group: its own, times its mask's, which a transition's opacity is.</summary>
    internal static double GroupOpacity(Visual visual) => visual.Opacity * ((visual.OpacityMask as ISolidColorBrush)?.Opacity ?? 1);

    /// <summary>A task's result, once it has ended: a broken nav fails the test rather than hang it on <c>.Result</c>.</summary>
    internal static bool Ended(Task<bool> task)
    {
        Assert.True(task.IsCompletedSuccessfully, $"the call hasn't ended successfully: {task.Status}");
        return task.Result;
    }

    internal static double TranslateY(Visual visual) => visual.RenderTransform?.Value.M32 ?? 0;

    internal static bool CanGoBackClass(Control page) => page.Classes.Contains(AvonicNav.CanGoBackPseudoClass);

    // ── N1, N2: the root ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void N1_Root_is_set_at_the_first_attach_unanimated_with_its_events()
    {
        var one = new TestPage("one");
        using var stage = new Stage(nav => nav.Root = one, attach: false);
        stage.After(100);
        Assert.Empty(stage.Log);
        Assert.Equal(0, stage.Nav.GetLength());

        stage.Attach();
        Assert.Equal(["ionNavWillChange@100"], stage.Log);
        stage.At(116);
        Assert.Equal(["ionNavWillChange@100", "willEnter one@116", "didEnter one@116", "ionNavDidChange@116"], stage.Log);
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.False(stage.Nav.CanGoBack());
        Assert.Same(one, stage.Nav.GetActive()!.Element);
        Assert.Same(one, stage.Nav.GetActive()!.Component);
        Assert.Contains("ion-page", one.Classes);
        Assert.False(CanGoBackClass(one));
        Assert.Equal(1, GroupOpacity(one));
        Assert.True(one.IsVisible);
        Assert.Same(stage.Nav, one.Parent);
        Assert.Equal(new Rect(0, 0, 390, 844), one.Bounds);
    }

    [AvaloniaFact]
    public void N1_A_call_before_the_load_waits_for_it_and_runs_after_the_root()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"), attach: false);
        var push = stage.Nav.Push(new TestPage("two"));
        stage.After(100);
        Assert.False(push.IsCompleted);
        Assert.Empty(stage.Log);

        stage.Attach();
        stage.After(600);
        Assert.True(Ended(push));
        Assert.Equal(["one", "two"], Names(stage.Nav));
        Assert.Equal("ionNavWillChange@100", stage.Log[0]);
        Assert.Equal("willEnter one@116", stage.Log[1]);
    }

    [AvaloniaFact]
    public void N2_Root_changed_after_load_sets_the_root_unanimated_and_RootParams_alone_does_nothing()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        stage.Stack(new TestPage("two"));

        stage.Nav.RootParams = new Dictionary<string, object?> { ["label"] = "x" };
        stage.After(100);
        Assert.Empty(stage.Log);

        stage.Nav.RootParams = new Dictionary<string, object?> { ["label"] = "root" };
        stage.Nav.Root = typeof(PageThree);
        stage.After(16);
        Assert.Equal(
            ["ionNavWillChange@1300", "willLeave one@1316", "didLeave one@1316", "willUnload one@1316",
             "willLeave two@1316", "willEnter three@1316", "didEnter three@1316", "didLeave two@1316", "willUnload two@1316",
             "ionNavDidChange@1316"],
            stage.Log);
        Assert.Equal(1, stage.Nav.GetLength());
        var three = Assert.IsType<PageThree>(stage.Nav.GetActive()!.Element);
        Assert.Equal("root", three.Label);
    }

    // ── N5: push ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void N5_Push_runs_the_md_forward_transition_with_Ionics_events()
    {
        var one = new TestPage("one");
        using var stage = new Stage(nav => nav.Root = one);
        stage.Done(Task.FromResult(true));
        stage.Clock.At(1000);
        stage.Log.Clear();

        var two = new TestPage("two");
        var push = stage.Nav.Push(two);
        // ionNavWillChange inside the call; the page built and in the nav, invisible, until its animation.
        Assert.Equal(["ionNavWillChange@1000"], stage.Log);
        Assert.Same(stage.Nav, two.Parent);
        Assert.Equal(0, GroupOpacity(two));
        stage.At(1015);
        Assert.Equal(0, GroupOpacity(two));

        // At the next frame: will-events, leaving first, and the animation's first values.
        stage.At(1016);
        Assert.Equal(["ionNavWillChange@1000", "willLeave one@1016", "willEnter two@1016"], stage.Log);
        Assert.Equal(0.01, GroupOpacity(two), 6);
        Assert.Equal(40, TranslateY(two), 6);

        // It starts at the frame at 1032, and shows local time t at 1032 + t.
        foreach (var t in new[] { 64.0, 144, 224 })
        {
            stage.At(1032 + t);
            var progress = ForwardEasing.Ease(t / 280);
            Assert.Equal(0.01 + (0.99 * progress), GroupOpacity(two), 4);
            Assert.Equal(40 * (1 - progress), TranslateY(two), 4);

            // Faded as one picture, as CSS's opacity is: a mask, not Avalonia's per-part Opacity.
            Assert.Equal(1, two.Opacity);
            Assert.IsAssignableFrom<ISolidColorBrush>(two.OpacityMask);

            // The leaving page doesn't move.
            Assert.Equal(1, GroupOpacity(one));
            Assert.Equal(0, TranslateY(one));
            Assert.True(one.IsVisible);
        }

        stage.At(1000 + PushDone - 1);
        Assert.False(push.IsCompleted);
        stage.At(1000 + PushDone);
        Assert.Equal(
            ["ionNavWillChange@1000", "willLeave one@1016", "willEnter two@1016",
             "didEnter two@1320", "didLeave one@1320", "ionNavDidChange@1320"],
            stage.Log);
        Assert.True(push.IsCompletedSuccessfully);
        Assert.True(Ended(push));
        Assert.Equal(1, GroupOpacity(two));
        Assert.Null(two.OpacityMask);
        Assert.Null(two.RenderTransform);
        Assert.False(one.IsVisible);
        Assert.True(CanGoBackClass(two));
        Assert.Equal(2, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void N5_The_animated_opacity_replaces_the_pages_own_as_CSSs_does_and_it_comes_back_after()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        var two = new TestPage("two") { Opacity = 0.5 };
        _ = stage.Nav.Push(two);
        stage.At(600 + 32 + 144);
        Assert.Equal(0.01 + (0.99 * ForwardEasing.Ease(144 / 280.0)), GroupOpacity(two), 4);
        stage.After(600);
        Assert.Equal(0.5, GroupOpacity(two));
    }

    [AvaloniaFact]
    public void N5_IonNavDidChange_comes_before_an_awaiting_caller_continues()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        var order = new List<string>();
        stage.Nav.IonNavDidChange += (_, _) => order.Add("didChange");
        _ = Continue();
        stage.After(600);
        Assert.Equal(["didChange", "continued"], order);

        async Task Continue()
        {
            await stage.Nav.Push(new TestPage("two"));
            order.Add("continued");
        }
    }

    // ── N6: pop ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void N6_Pop_runs_the_md_back_transition_and_the_popped_page_leaves_the_nav()
    {
        var one = new TestPage("one");
        var two = new TestPage("two");
        using var stage = new Stage(nav => nav.Root = one);
        stage.Done(Task.FromResult(true));
        stage.Stack(two);
        stage.Clock.At(2000);
        stage.Log.Clear();

        var pop = stage.Nav.Pop();
        Stage.Settle();
        Assert.Equal(["ionNavWillChange@2000", "willLeave two@2000", "willEnter one@2000"], stage.Log);
        Assert.True(one.IsVisible);
        Assert.Equal(1, GroupOpacity(two));
        Assert.Equal(0, TranslateY(two), 6);

        foreach (var t in new[] { 48.0, 96, 160 })
        {
            stage.At(2016 + t);
            var progress = BackEasing.Ease(t / 200);
            Assert.Equal(1 - progress, GroupOpacity(two), 4);
            Assert.Equal(40 * progress, TranslateY(two), 4);
            Assert.Equal(1, GroupOpacity(one));
            Assert.Equal(0, TranslateY(one));
        }

        stage.At(2000 + PopDone);
        Assert.Equal(
            ["ionNavWillChange@2000", "willLeave two@2000", "willEnter one@2000",
             "didEnter one@2224", "didLeave two@2224", "willUnload two@2224", "ionNavDidChange@2224"],
            stage.Log);
        Assert.True(Ended(pop));
        Assert.Null(two.Parent);
        Assert.Null(two.GetVisualParent());
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.True(one.IsVisible);
    }

    // ── N8, N9: the pages during a transition ────────────────────────────────────────────────

    [AvaloniaFact]
    public void N8_ZIndex_and_hit_testing_during_a_transition()
    {
        var one = new TestPage("one");
        var two = new TestPage("two");
        using var stage = new Stage(nav => nav.Root = one);
        stage.Done(Task.FromResult(true));

        _ = stage.Nav.Push(two);
        stage.After(16);
        Assert.Equal(101, two.ZIndex);
        Assert.Equal(100, one.ZIndex);
        Assert.False(one.IsHitTestVisible);
        Assert.False(two.IsHitTestVisible);
        stage.After(600);
        Assert.True(two.IsHitTestVisible);
        Assert.True(one.IsHitTestVisible);

        _ = stage.Nav.Pop();
        Stage.Settle();
        Assert.Equal(99, one.ZIndex);
        Assert.Equal(100, two.ZIndex);
        Assert.False(one.IsHitTestVisible);
        stage.After(600);
        Assert.True(one.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void N8_An_apps_own_IsHitTestVisible_comes_back_after_a_transition()
    {
        var one = new TestPage("one");
        var two = new TestPage("two") { IsHitTestVisible = false };
        using var stage = new Stage(nav => nav.Root = one);
        stage.Done(Task.FromResult(true));
        stage.Done(stage.Nav.Push(two));
        Assert.False(two.IsHitTestVisible);
        two.IsHitTestVisible = true;
        Assert.True(two.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void N9_CanGoBack_on_the_entering_page_and_ShowGoBack_overrides_it()
    {
        var one = new TestPage("one");
        var two = new TestPage("two");
        var three = new TestPage("three");
        using var stage = new Stage(nav => nav.Root = one);
        stage.Done(Task.FromResult(true));
        Assert.False(CanGoBackClass(one));

        stage.Done(stage.Nav.Push(two));
        Assert.True(CanGoBackClass(two));

        stage.Done(stage.Nav.Push(three, opts: new NavOptions { ShowGoBack = false }));
        Assert.False(CanGoBackClass(three));
    }

    // ── N10, N11, N21: the queue and failures ────────────────────────────────────────────────

    [AvaloniaFact]
    public void N10_Calls_run_one_at_a_time_and_SkipIfBusy_is_false_at_once()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        var a = stage.Nav.Push(new TestPage("two"));
        var b = stage.Nav.Push(new TestPage("three"));
        stage.After(50);
        var c = stage.Nav.Push(new TestPage("four"), opts: new NavOptions { SkipIfBusy = true });
        Assert.True(c.IsCompletedSuccessfully);
        Assert.False(Ended(c));

        stage.At(600 + PushDone);
        Assert.True(Ended(a));
        Assert.False(b.IsCompleted);
        Assert.Equal(["ionNavWillChange@600", "willLeave one@616", "willEnter two@616", "didEnter two@920", "didLeave one@920",
                      "ionNavDidChange@920", "ionNavWillChange@920"], stage.Log.Take(7));
        stage.After(600);
        Assert.True(Ended(b));
        Assert.Equal(3, stage.Nav.GetLength());
        Assert.Equal("three", stage.Nav.GetActive()!.Element!.Name);
    }

    [AvaloniaFact]
    public void N11_A_pop_at_the_root_fails_with_Ionics_message_and_the_nav_works_after()
    {
        var one = new TestPage("one");
        using var stage = new Stage(nav => nav.Root = one);
        stage.Done(Task.FromResult(true));
        var done = new List<string>();

        var pop = stage.Nav.Pop(done: (completed, requires, _, _, _, error) => done.Add($"{completed} {requires} {error?.Message}"));
        stage.After(100);
        Assert.True(pop.IsFaulted);
        Assert.Equal("navigation stack needs at least one root page", pop.Exception!.InnerException!.Message);
        Assert.Equal(["False False navigation stack needs at least one root page"], done);
        Assert.Equal(["ionNavWillChange@600"], stage.Log);
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.True(one.IsVisible);

        Assert.True(stage.Done(stage.Nav.Push(new TestPage("two"))));
        Assert.Equal(2, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void N11_An_empty_nav_has_no_views_to_remove()
    {
        using var stage = new Stage();
        Assert.Null(stage.Nav.GetActive());
        Assert.False(stage.Nav.CanGoBack());
        var pop = stage.Nav.Pop();
        stage.After(100);
        Assert.True(pop.IsFaulted);
        Assert.Equal("no views in the stack to be removed", pop.Exception!.InnerException!.Message);
    }

    [AvaloniaFact]
    public void N21_A_failure_drops_the_calls_queued_behind_it_and_each_completes_false()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        var dropped = new List<string>();
        var pushA = stage.Nav.Push(new TestPage("two"));
        var pop1 = stage.Nav.Pop();
        var pop2 = stage.Nav.Pop();
        var pushB = stage.Nav.Push(new TestPage("three"), done: (completed, _, _, _, _, error) => dropped.Add($"{completed} {error}"));
        stage.After(2000);

        Assert.True(Ended(pushA));
        Assert.True(Ended(pop1));
        Assert.True(pop2.IsFaulted);
        // Ionic leaves this one pending for ever (ref/extra.txt X1b): it's dropped, and completes false.
        Assert.True(pushB.IsCompletedSuccessfully);
        Assert.False(Ended(pushB));
        Assert.Equal(["False "], dropped);
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.Equal("one", stage.Nav.GetActive()!.Element!.Name);
    }

    // ── N12, N13, N14: the stack ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void N12_PopToRoot_removes_the_middle_pages_at_once_with_their_events()
    {
        var two = new TestPage("two");
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        stage.Stack(two, new TestPage("three"));

        var pop = stage.Nav.PopToRoot();
        Stage.Settle();
        Assert.Equal(
            ["ionNavWillChange@1800", "willLeave two@1800", "didLeave two@1800", "willUnload two@1800",
             "willLeave three@1800", "willEnter one@1800"],
            stage.Log);
        Assert.Null(two.Parent);
        stage.After(600);
        Assert.True(Ended(pop));
        Assert.Equal(1, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void N12_RemoveIndex_in_the_middle_has_no_transition()
    {
        var two = new TestPage("two");
        var three = new TestPage("three");
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        stage.Stack(two, three);

        var remove = stage.Nav.RemoveIndex(1);
        Stage.Settle();
        Assert.Equal(
            ["ionNavWillChange@1800", "willLeave two@1800", "didLeave two@1800", "willUnload two@1800", "ionNavDidChange@1800"],
            stage.Log);
        Assert.True(Ended(remove));
        Assert.Equal(["one", "three"], Names(stage.Nav));
        Assert.True(three.IsVisible);
    }

    [AvaloniaFact]
    public void N12_PopTo_a_view_and_an_index()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        stage.Stack(new TestPage("two"), new TestPage("three"), new TestPage("four"));

        Assert.True(stage.Done(stage.Nav.PopTo(stage.Nav.GetByIndex(1)!)));
        Assert.Equal(2, stage.Nav.GetLength());
        Assert.Equal("two", stage.Nav.GetActive()!.Element!.Name);

        stage.Stack(new TestPage("five"));
        Assert.True(stage.Done(stage.Nav.PopTo(0)));
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.Equal("one", stage.Nav.GetActive()!.Element!.Name);
    }

    [AvaloniaFact]
    public void N13_Insert_in_the_middle_has_no_transition_and_InsertPages_builds_only_the_last()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        stage.Stack(new TestPage("two"));

        var insert = stage.Nav.Insert(1, typeof(PageThree));
        stage.After(16);
        Assert.Equal(["ionNavWillChange@1200", "ionNavDidChange@1216"], stage.Log);
        Assert.True(Ended(insert));
        Assert.Equal(["one", "three", "two"], Names(stage.Nav));
        Assert.Equal("two", stage.Nav.GetActive()!.Element!.Name);
        stage.Log.Clear();

        var built = new List<string>();
        TestPage.Constructed = page => built.Add(page.Name!);
        var pages = stage.Nav.InsertPages(-1, [typeof(PageOne), new NavComponentWithProps(typeof(PageTwo), new Dictionary<string, object?> { ["label"] = "last" })]);
        stage.After(600);
        Assert.True(Ended(pages));
        Assert.Equal(["two"], built);
        Assert.Equal(5, stage.Nav.GetLength());
        Assert.Null(stage.Nav.GetByIndex(3)!.Element);
        Assert.Equal("last", ((TestPage)stage.Nav.GetActive()!.Element!).Label);

        // Popped into, the page with none is built.
        Assert.True(stage.Done(stage.Nav.Pop()));
        Assert.Equal(["two", "one"], built);
        Assert.Equal("one", stage.Nav.GetActive()!.Element!.Name);
    }

    [AvaloniaFact]
    public void N14_SetRoot_and_SetPages_are_unanimated_unless_asked()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        stage.Stack(new TestPage("two"));

        var root = stage.Nav.SetRoot(new TestPage("three"));
        stage.After(16);
        Assert.True(root.IsCompleted);
        Assert.Equal(["three"], Names(stage.Nav));
        stage.Log.Clear();

        var four = new TestPage("four");
        var pages = stage.Nav.SetPages([new TestPage("x"), four], new NavOptions { Animated = true });
        stage.After(16);
        Assert.False(pages.IsCompleted);
        stage.After(100);
        // Back: the leaving page sinks over the new top.
        Assert.True(TranslateY(stage.Nav.Pages.First(p => p.Name == "three")) > 0);
        stage.After(600);
        Assert.True(Ended(pages));
        Assert.Equal(["-", "four"], Names(stage.Nav));
        Assert.Null(stage.Nav.GetByIndex(0)!.Element);
    }

    // ── N15, N16: animation off, and the options ─────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void N15_With_animations_off_the_events_come_in_one_go(bool navAnimated, bool configAnimated)
    {
        AvonicConfig.Animated = configAnimated;
        try
        {
            using var stage = new Stage(nav => { nav.Root = new TestPage("one"); nav.Animated = navAnimated; });
            stage.Done(Task.FromResult(true));
            var push = stage.Nav.Push(new TestPage("two"));
            stage.After(16);
            Assert.Equal(
                ["ionNavWillChange@600", "willLeave one@616", "willEnter two@616", "didEnter two@616", "didLeave one@616", "ionNavDidChange@616"],
                stage.Log);
            Assert.True(Ended(push));
            Assert.Equal(1, GroupOpacity(stage.Nav.GetActive()!.Element!));
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    }

    [AvaloniaFact]
    public void N16_Duration_replaces_the_duration_and_Direction_back_on_a_push_runs_the_back_animation()
    {
        var one = new TestPage("one");
        using var stage = new Stage(nav => nav.Root = one);
        stage.Done(Task.FromResult(true));

        var two = new TestPage("two");
        var push = stage.Nav.Push(two, opts: new NavOptions { Duration = 1000 });
        stage.At(600 + 32 + 496);
        Assert.Equal(0.01 + (0.99 * ForwardEasing.Ease(0.496)), GroupOpacity(two), 4);
        stage.After(600);
        Assert.True(Ended(push));
        stage.Log.Clear();

        var three = new TestPage("three");
        _ = stage.Nav.Push(three, opts: new NavOptions { Direction = NavDirection.Back });
        stage.After(16 + 16 + 96);
        // The leaving page sinks, under which the entering one waits.
        Assert.Equal(40 * BackEasing.Ease(0.48), TranslateY(two), 4);
        Assert.Equal(99, three.ZIndex);
        Assert.Equal(1, GroupOpacity(three));
    }

    [AvaloniaFact]
    public void N16_The_calls_builder_beats_Animation_which_beats_md_and_a_pop_back_uses_the_one_pushed_with()
    {
        var used = new List<string>();
        AnimationBuilder Named(string name) => (baseEl, options) =>
        {
            used.Add($"{name} {((NavTransitionOptions)options!).Direction}");
            return AvonicAnimation.Create().AddElement(((NavTransitionOptions)options!).EnteringElement).Duration(50)
                .FromTo("opacity", 0.5, 1);
        };

        using var stage = new Stage(nav => { nav.Root = new TestPage("one"); nav.Animation = Named("nav"); });
        stage.Done(Task.FromResult(true));
        stage.Done(stage.Nav.Push(new TestPage("two")));
        stage.Done(stage.Nav.Push(new TestPage("three"), opts: new NavOptions { AnimationBuilder = Named("call") }));
        Assert.Equal(["nav Forward", "call Forward"], used);

        // Back to two: two was left by the push with "call".
        stage.Done(stage.Nav.Pop());
        Assert.Equal("call Back", used[^1]);
    }

    [AvaloniaFact]
    public void N16_An_apps_builder_that_fades_without_removing_the_invisible_class_still_shows_the_page()
    {
        // In CSS the Web Animation's opacity beats .ion-page-invisible's 0, as here the animation's mask beats the class's.
        using var stage = new Stage(nav =>
        {
            nav.Root = new TestPage("one");
            nav.Animation = (_, options) => AvonicAnimation.Create()
                .AddElement(((NavTransitionOptions)options!).EnteringElement).Duration(160).FromTo("opacity", 0, 1);
        });
        stage.Done(Task.FromResult(true));
        var two = new TestPage("two");
        _ = stage.Nav.Push(two);
        stage.After(32 + 80);
        Assert.Equal(0.5, GroupOpacity(two), 2);
        stage.After(600);
        Assert.Equal(1, GroupOpacity(two));
    }

    [AvaloniaFact]
    public void N16_AvonicConfig_NavAnimation_is_the_transition_when_the_call_and_the_nav_give_none()
    {
        var used = 0;
        AvonicConfig.NavAnimation = (_, options) =>
        {
            used++;
            return AvonicAnimation.Create().AddElement(((NavTransitionOptions)options!).EnteringElement).Duration(50).FromTo("opacity", 0.5, 1);
        };
        try
        {
            using var stage = new Stage(nav => nav.Root = new TestPage("one"));
            stage.Done(Task.FromResult(true));
            stage.Done(stage.Nav.Push(new TestPage("two")));
            Assert.Equal(1, used);
            stage.Nav.Animation = AvonicNav.MdTransitionAnimation;
            stage.Done(stage.Nav.Pop());
            Assert.Equal(1, used);
        }
        finally
        {
            AvonicConfig.NavAnimation = null;
        }
    }

    [AvaloniaFact]
    public void N10_A_call_from_an_IonNavWillChange_handler_waits_its_turn()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        Task<bool>? skipped = null;
        Task<bool>? queued = null;
        void OnWillChange(object? sender, RoutedEventArgs e)
        {
            stage.Nav.IonNavWillChange -= OnWillChange;
            skipped = stage.Nav.Push(new TestPage("skipped"), opts: new NavOptions { SkipIfBusy = true });
            queued = stage.Nav.Push(new TestPage("three"));
        }

        stage.Nav.IonNavWillChange += OnWillChange;
        var push = stage.Nav.Push(new TestPage("two"));
        Assert.True(skipped!.IsCompletedSuccessfully);
        Assert.False(Ended(skipped));
        Assert.False(queued!.IsCompleted);
        stage.After(1200);
        Assert.True(Ended(push));
        Assert.True(Ended(queued));
        Assert.Equal(["one", "two", "three"], Names(stage.Nav));
    }

    [AvaloniaFact]
    public void N3_A_page_already_in_the_tree_fails_and_leaves_the_nav_as_it_was()
    {
        var one = new TestPage("one");
        using var stage = new Stage(nav => nav.Root = one);
        stage.Done(Task.FromResult(true));
        var push = stage.Nav.Push(one);
        stage.After(100);
        Assert.True(push.IsFaulted);
        Assert.Equal(["one"], Names(stage.Nav));
        Assert.Single(stage.Nav.Pages);
        Assert.True(stage.Done(stage.Nav.Push(new TestPage("two"))));
    }

    [AvaloniaFact]
    public void N3_A_view_whose_page_failed_to_build_can_be_pushed_again()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        var view = new ViewController(new DetailViewModel("later"));
        var first = stage.Nav.Push(view);
        stage.After(100);
        Assert.True(first.IsFaulted);

        stage.Nav.DataTemplates.Add(new FuncDataTemplate<DetailViewModel>((vm, _) => new TestPage("detail") { Label = vm.Title }));
        Assert.True(stage.Done(stage.Nav.Push(view)));
        Assert.Equal("later", ((TestPage)view.Element!).Label);
    }

    // ── N17, N18, N19: getters, hidden pages, events ─────────────────────────────────────────

    [AvaloniaFact]
    public void N17_Getters()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        stage.Stack(new TestPage("two"), new TestPage("three"));
        var nav = stage.Nav;
        Assert.Equal(3, nav.GetLength());
        Assert.Equal("three", nav.GetActive()!.Element!.Name);
        Assert.Equal("two", nav.GetPrevious()!.Element!.Name);
        Assert.Equal("one", nav.GetPrevious(nav.GetByIndex(1))!.Element!.Name);
        Assert.Null(nav.GetPrevious(nav.GetByIndex(0)));
        Assert.True(nav.CanGoBack());
        Assert.False(nav.CanGoBack(nav.GetByIndex(0)));
        Assert.Null(nav.GetByIndex(3));
        Assert.Null(nav.GetByIndex(-1));
        Assert.Same(nav, nav.GetActive()!.Nav);
    }

    [AvaloniaFact]
    public void N18_Pages_under_the_top_are_hidden_keep_their_scroll_and_an_apps_page_can_be_pushed_again()
    {
        var scroller = new ScrollViewer { Content = new Border { Height = 3000 } };
        var one = new UserControl { Name = "one", Content = scroller };
        using var stage = new Stage(nav => nav.Root = one);
        stage.Done(Task.FromResult(true));
        stage.Render();
        scroller.Offset = new Vector(0, 600);
        stage.Render();

        var two = new TestPage("two");
        stage.Done(stage.Nav.Push(two));
        Assert.False(one.IsVisible);
        Assert.DoesNotContain(ControlAutomationPeer.CreatePeerForElement(stage.Nav).GetChildren(),
            p => ReferenceEquals((p as ControlAutomationPeer)?.Owner, one));

        stage.Done(stage.Nav.Pop());
        stage.Render();
        Assert.True(one.IsVisible);
        Assert.Equal(600, scroller.Offset.Y);

        Assert.True(stage.Done(stage.Nav.Push(two)));
        Assert.Same(two, stage.Nav.GetActive()!.Element);
        Assert.Equal(1, GroupOpacity(two));
    }

    [AvaloniaFact]
    public void N19_The_events_dont_bubble()
    {
        var heard = new List<string>();
        using var stage = new Stage(nav => nav.Root = new TestPage("one"), wrap: nav => new Border { Child = nav });
        foreach (var e in new RoutedEvent[] { AvonicNav.IonNavWillChangeEvent, AvonicNav.IonNavDidChangeEvent, AvonicNav.IonViewWillEnterEvent,
                     AvonicNav.IonViewDidEnterEvent, AvonicNav.IonViewWillLeaveEvent, AvonicNav.IonViewDidLeaveEvent, AvonicNav.IonViewWillUnloadEvent })
        {
            stage.Root.AddHandler(e, (EventHandler<RoutedEventArgs>)((_, _) => heard.Add(e.Name)), handledEventsToo: true);
            stage.Nav.AddHandler(e, (EventHandler<RoutedEventArgs>)((_, args) =>
            {
                if (args.Source != stage.Nav)
                {
                    heard.Add("nav " + e.Name);
                }
            }), handledEventsToo: true);
        }

        stage.Done(Task.FromResult(true));
        stage.Done(stage.Nav.Push(new TestPage("two")));
        _ = stage.Nav.Pop();
        stage.After(600);
        Assert.Empty(heard);
        Assert.Contains("willUnload two@1424", stage.Log);
    }

    // ── N3, N4, N22: page forms and props ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void N3_Each_page_form_builds_its_page()
    {
        using var stage = new Stage(nav =>
        {
            nav.Root = new TestPage("one");
            nav.DataTemplates.Add(new FuncDataTemplate<DetailViewModel>((vm, _) => new TestPage("detail") { Label = vm.Title }));
        });
        stage.Done(Task.FromResult(true));

        stage.Done(stage.Nav.Push(typeof(PageTwo)));
        Assert.IsType<PageTwo>(stage.Nav.GetActive()!.Element);
        Assert.Equal(typeof(PageTwo), stage.Nav.GetActive()!.Component);

        Func<Control> factory = () => new TestPage("made");
        stage.Done(stage.Nav.Push(factory));
        Assert.Equal("made", stage.Nav.GetActive()!.Element!.Name);

        var vm = new DetailViewModel("Detail");
        stage.Done(stage.Nav.Push(vm));
        var detail = (TestPage)stage.Nav.GetActive()!.Element!;
        Assert.Equal("Detail", detail.Label);
        Assert.Same(vm, detail.DataContext);
        Assert.Same(vm, stage.Nav.GetActive()!.Component);

        var view = new ViewController(typeof(PageThree));
        stage.Done(stage.Nav.Push(view));
        Assert.Same(view, stage.Nav.GetActive());
        Assert.IsType<PageThree>(view.Element);
    }

    [AvaloniaFact]
    public void N3_A_page_inherits_the_navs_DataContext_unless_its_form_sets_one()
    {
        using var stage = new Stage(nav => { nav.Root = new TestPage("one"); nav.DataContext = "shell"; });
        stage.Done(Task.FromResult(true));
        Assert.Equal("shell", stage.Nav.GetActive()!.Element!.DataContext);
    }

    [AvaloniaFact]
    public void N3_A_form_with_no_page_fails_as_a_missing_delegate_does()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        var push = stage.Nav.Push(new DetailViewModel("x"));
        stage.After(100);
        Assert.True(push.IsFaulted);
        Assert.StartsWith("framework delegate is missing", push.Exception!.InnerException!.Message);
        Assert.Equal(1, stage.Nav.GetLength());

        var wrongType = stage.Nav.Push(typeof(string));
        stage.After(100);
        Assert.True(wrongType.IsFaulted);
    }

    [AvaloniaFact]
    public void N4_ComponentProps_are_set_by_name_before_the_page_is_attached()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        stage.Done(stage.Nav.Push(typeof(PageTwo), new Dictionary<string, object?>
        {
            ["label"] = "from push",
            ["Count"] = "7",
            ["Width"] = 200.0,
            ["nothing"] = 1,
            ["count2"] = "x",
        }));
        var two = (PageTwo)stage.Nav.GetActive()!.Element!;
        Assert.Equal("from push", two.Label);
        Assert.Equal(7, two.Count);
        Assert.Equal(200, two.Width);
        Assert.Equal("from push/7", two.AtAttach);
        Assert.Equal(stage.Nav.GetActive()!.Params!["label"], "from push");
    }

    [AvaloniaFact]
    public void N22_InsertPages_and_SetPages_take_a_bare_page_form()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        Assert.True(stage.Done(stage.Nav.InsertPages(-1, [typeof(PageTwo)])));
        Assert.True(stage.Done(stage.Nav.SetPages([typeof(PageOne), typeof(PageThree)])));
        Assert.Equal(2, stage.Nav.GetLength());
        Assert.IsType<PageThree>(stage.Nav.GetActive()!.Element);
    }

    // ── N7: ready before it moves ────────────────────────────────────────────────────────────

    /// <summary>A page whose layout is measured: it records whether it was laid out by the time it's told it will enter.</summary>
    private sealed class MeasuredPage : TestPage
    {
        public MeasuredPage()
            : base("measured")
        {
            AvonicNav.AddIonViewWillEnterHandler(this, (_, _) => MeasuredAtWillEnter = IsMeasureValid && Bounds.Height > 0);
        }

        public bool MeasuredAtWillEnter { get; private set; }
    }

    [AvaloniaFact]
    public void N7_The_entering_page_is_laid_out_before_the_animation_starts()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        var page = new MeasuredPage();
        _ = stage.Nav.Push(page);

        // Nothing lays the page out between the frame and the transition but the nav itself.
        stage.Clock.At(stage.Clock.Now.TotalMilliseconds + 16);
        Stage.Settle();
        Assert.True(page.MeasuredAtWillEnter);
    }

    [AvaloniaFact]
    public void N7_DeepWait_false_starts_without_the_layout_and_ViewIsReady_is_awaited_first()
    {
        using var stage = new Stage(nav => nav.Root = new TestPage("one"));
        stage.Done(Task.FromResult(true));
        var page = new MeasuredPage();
        _ = stage.Nav.Push(page, opts: new NavOptions { DeepWait = false });
        stage.Clock.At(stage.Clock.Now.TotalMilliseconds + 16);
        Stage.Settle();
        Assert.False(page.MeasuredAtWillEnter);
        stage.After(600);

        var ready = new TaskCompletionSource();
        Control? given = null;
        var two = new TestPage("two");
        var push = stage.Nav.Push(two, opts: new NavOptions { ViewIsReady = el => { given = el; return ready.Task; } });
        stage.After(100);
        Assert.Same(two, given);
        Assert.DoesNotContain(stage.Log, l => l.StartsWith("willEnter two", StringComparison.Ordinal));
        ready.SetResult();
        stage.After(600);
        Assert.Contains(stage.Log, l => l.StartsWith("willEnter two", StringComparison.Ordinal));
        Assert.True(Ended(push));
    }

    // ── N20: a detach keeps the stack ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void N20_A_nav_moved_to_another_parent_keeps_its_stack()
    {
        var holder = new Border();
        using var stage = new Stage(nav => nav.Root = new TestPage("one"), wrap: nav => new StackPanel { Children = { new Border { Child = nav }, holder } });
        stage.Done(Task.FromResult(true));
        stage.Stack(new TestPage("two"));

        ((Border)stage.Nav.Parent!).Child = null;
        stage.After(100);
        holder.Child = stage.Nav;
        stage.After(100);

        Assert.Empty(stage.Log);
        Assert.Equal(["one", "two"], Names(stage.Nav));
        Assert.True(stage.Nav.GetActive()!.Element!.IsVisible);
        Assert.True(stage.Done(stage.Nav.Pop()));
        Assert.Equal(["one"], Names(stage.Nav));
    }

    [AvaloniaFact]
    public void N20_A_nav_in_an_inline_modal_keeps_its_stack_through_present_and_dismiss()
    {
        // Ionic's nav in an inline ion-modal is empty once the modal is presented (ref/extra.txt X7).
        var nav = new AvonicNav { Root = new TestPage("one") };
        var modal = new AvonicModal { Content = nav };
        using var stage = new Stage(wrap: _ => new Panel { Children = { modal } });
        stage.After(100);

        _ = modal.Present();
        stage.After(600);
        Assert.IsType<Avonic.Components.Overlays.OverlayHost>(modal.Parent);
        Assert.Equal(["one"], Names(nav));
        Assert.True(nav.GetActive()!.Element!.IsVisible);
        Assert.True(stage.Done(nav.Push(new TestPage("two"))));

        _ = modal.Dismiss();
        stage.After(600);
        _ = modal.Present();
        stage.After(600);
        Assert.Equal(["one", "two"], Names(nav));
        Assert.True(nav.GetActive()!.Element!.IsVisible);
    }

    internal static string[] Names(AvonicNav nav) =>
        Enumerable.Range(0, nav.GetLength()).Select(i => nav.GetByIndex(i)!.Element?.Name ?? "-").ToArray();
}
