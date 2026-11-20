using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicNavTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-nav-link</c> (L1 to L4) and the nav's hardware back extension (N24), in docs/components/ion-nav/PLAN.md.
/// </summary>
public class AvonicNavLinkTests
{
    /// <summary>A root page holding <paramref name="content"/> at the top left, 200 x 48.</summary>
    private static TestPage PageWith(Control content)
    {
        var page = new TestPage("one") { Child = new StackPanel { Children = { content } } };
        content.Width = 200;
        content.Height = 48;
        content.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        return page;
    }

    /// <summary>On the text's first glyph: a <see cref="TextBlock"/> takes a press only on its text.</summary>
    private static readonly Point OnText = new(6, 8);

    private static void Click(Window window, Point at)
    {
        window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static Stage Loaded(Control root)
    {
        var stage = new Stage(nav => nav.Root = root);
        stage.Done(Task.FromResult(true));
        stage.Render();
        return stage;
    }

    // ── L1 to L4 ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void L1_Forward_pushes_the_component_with_its_props_on_the_closest_nav()
    {
        var link = new AvonicNavLink
        {
            Component = typeof(PageTwo),
            ComponentProps = new Dictionary<string, object?> { ["label"] = "from link" },
            Content = new TextBlock { Text = "Go" },
        };
        using var stage = Loaded(PageWith(link));
        Click(stage.Window, OnText);
        stage.After(600);
        Assert.Equal(2, stage.Nav.GetLength());
        var two = Assert.IsType<PageTwo>(stage.Nav.GetActive()!.Element);
        Assert.Equal("from link", two.Label);
    }

    [AvaloniaFact]
    public void L1_Root_sets_the_root_and_Back_pops_each_with_its_RouterAnimation()
    {
        var used = 0;
        AnimationBuilder counting = (_, options) =>
        {
            used++;
            return AvonicAnimation.Create().AddElement(((NavTransitionOptions)options!).EnteringElement).Duration(50).FromTo("opacity", 0.5, 1);
        };
        var back = new AvonicNavLink { RouterDirection = NavLinkRouterDirection.Back, RouterAnimation = counting, Content = new TextBlock { Text = "Back" } };
        var root = new AvonicNavLink { RouterDirection = NavLinkRouterDirection.Root, Component = typeof(PageThree), Content = new TextBlock { Text = "Root" } };
        using var stage = Loaded(new TestPage("one"));
        stage.Stack(new TestPage("two"), PageWith(back));
        stage.Render();

        Click(stage.Window, OnText);
        stage.After(600);
        Assert.Equal(["one", "two"], Names(stage.Nav));
        Assert.Equal(1, used);

        stage.Done(stage.Nav.Push(PageWith(root)));
        stage.Render();
        Click(stage.Window, OnText);
        stage.After(600);
        Assert.Equal(["three"], Names(stage.Nav));
    }

    [AvaloniaFact]
    public void L2_A_button_inside_from_a_tap_or_a_key_and_a_double_tap_push_once()
    {
        var button = new AvonicButton { Content = "Go" };
        var link = new AvonicNavLink { Component = typeof(PageTwo), Content = button };
        using var stage = Loaded(PageWith(link));

        // A double click: the second is skipped while the first is busy.
        Click(stage.Window, new Point(20, 20));
        Click(stage.Window, new Point(20, 20));
        stage.After(600);
        Assert.Equal(2, stage.Nav.GetLength());

        stage.Done(stage.Nav.Pop());
        stage.Render();
        button.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        stage.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        stage.After(600);
        Assert.Equal(2, stage.Nav.GetLength());

        stage.Done(stage.Nav.Pop());
        stage.Render();
        button.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        stage.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        stage.After(600);
        Assert.Equal(2, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void L2_A_stock_Button_inside_clicked_by_a_key_pushes()
    {
        var button = new Button { Content = "Go" };
        var link = new AvonicNavLink { Component = typeof(PageTwo), Content = button };
        using var stage = Loaded(PageWith(link));
        button.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        stage.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        stage.After(600);
        Assert.Equal(2, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void L3_With_no_nav_or_no_component_nothing_happens()
    {
        var link = new AvonicNavLink { Content = new TextBlock { Text = "Nothing" } };
        using var stage = Loaded(PageWith(link));
        Click(stage.Window, OnText);
        stage.After(600);
        Assert.Empty(stage.Log);
        Assert.Equal(1, stage.Nav.GetLength());

        var outside = new AvonicNavLink { Component = typeof(PageTwo), Content = new TextBlock { Text = "Outside" }, Width = 200, Height = 48 };
        using var scene = new Scene(outside);
        Click(scene.Window, OnText);
        Assert.Equal(1, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void L4_The_link_isnt_focusable_and_adds_no_automation_element()
    {
        var text = new TextBlock { Text = "Go" };
        var link = new AvonicNavLink { Component = typeof(PageTwo), Content = text };
        using var stage = Loaded(PageWith(link));
        Assert.False(link.Focusable);
        Assert.IsType<NoneAutomationPeer>(ControlAutomationPeer.CreatePeerForElement(link));
    }

    // ── N24: the hardware back button (extension) ────────────────────────────────────────────

    private static RoutedEventArgs Back(Window window)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        window.RaiseEvent(e);
        Dispatcher.UIThread.RunJobs();
        return e;
    }

    [AvaloniaFact]
    public void N24_Back_pops_while_the_nav_can_go_back_and_is_left_alone_at_the_root()
    {
        using var stage = Loaded(new TestPage("one"));
        stage.Stack(new TestPage("two"));
        var e = Back(stage.Window);
        Assert.True(e.Handled);
        stage.After(600);
        Assert.Equal(["one"], Names(stage.Nav));

        Assert.False(Back(stage.Window).Handled);
        stage.After(600);
        Assert.Equal(["one"], Names(stage.Nav));
    }

    [AvaloniaFact]
    public void N24_HandleBackButton_off_leaves_the_request()
    {
        using var stage = Loaded(new TestPage("one"));
        stage.Nav.HandleBackButton = false;
        stage.Stack(new TestPage("two"));
        Assert.False(Back(stage.Window).Handled);
        stage.After(600);
        Assert.Equal(2, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void N24_The_innermost_nav_pops_first()
    {
        var inner = new AvonicNav { Root = new TestPage("a") };
        using var stage = Loaded(new TestPage("one"));
        stage.Stack(new TestPage("two") { Child = inner });
        stage.Done(inner.Push(new TestPage("b")));

        Assert.True(Back(stage.Window).Handled);
        stage.After(600);
        Assert.Equal(1, inner.GetLength());
        Assert.Equal(2, stage.Nav.GetLength());

        Assert.True(Back(stage.Window).Handled);
        stage.After(600);
        Assert.Equal(1, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void N24_A_nav_on_a_hidden_page_takes_no_back_request()
    {
        var inner = new AvonicNav { Root = new TestPage("a") };
        using var stage = Loaded(new TestPage("one"));
        stage.Stack(new TestPage("two") { Child = inner });
        stage.Done(inner.Push(new TestPage("b")));
        stage.Stack(new TestPage("three"));

        Back(stage.Window);
        stage.After(600);
        Assert.Equal(2, inner.GetLength());
        Assert.Equal(2, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void N24_In_an_app_it_registers_at_0_under_an_apps_higher_priority()
    {
        var app = new AvonicApp();
        using var stage = new Stage(nav => nav.Root = new TestPage("one"), wrap: nav => { app.Content = nav; return app; });
        stage.Done(Task.FromResult(true));
        stage.Stack(new TestPage("two"));
        var taken = new List<int>();
        var priority = 1;
        stage.Window.AddHandler(HardwareBackButton.IonBackButtonEvent, (_, e) =>
        {
            var at = priority;
            e.Register(at, _ => taken.Add(at));
        });

        // An app's handler above 0 comes first, and takes it.
        Back(stage.Window);
        stage.After(600);
        Assert.Equal([1], taken);
        Assert.Equal(2, stage.Nav.GetLength());

        // Below 0, the nav's comes first.
        priority = -1;
        Back(stage.Window);
        stage.After(600);
        Assert.Equal([1], taken);
        Assert.Equal(1, stage.Nav.GetLength());
    }
}
