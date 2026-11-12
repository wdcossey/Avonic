using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;
using static Avonic.Components.Tests.Controls.AvonicTabsCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-tabs' and ion-tab's behaviours, from <c>tabs.js</c> and <c>tab.js</c>, measured in Chromium: see
/// <c>docs/components/ion-tabs/PLAN.md</c>.
/// </summary>
public class AvonicTabsTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────────────────────

    internal static void Settle() => Dispatcher.UIThread.RunJobs();

    internal static Point Centre(Visual visual, Scene scene) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(scene.Window)!.Value).Center;

    internal static void Tap(Scene scene, Point at)
    {
        GestureTests.Render();
        scene.Window.MouseMove(at, RawInputModifiers.None);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Settle();
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Settle();
    }

    private sealed record Fixture(AvonicTabs Tabs, AvonicTabBar Bar, AvonicTab Home, AvonicTab Radio, AvonicTabButton HomeButton, AvonicTabButton RadioButton);

    private static Fixture Make()
    {
        var homeButton = Button("home", "Home", "home");
        var radioButton = Button("radio", "Radio", "radio");
        var bar = Bar(null, homeButton, radioButton);
        var home = Page("home", "Home");
        var radio = Page("radio", "Radio");
        return new Fixture(Tabs(bar, home, radio), bar, home, radio, homeButton, radioButton);
    }

    private static List<string> Log(AvonicTabs tabs)
    {
        var log = new List<string>();
        tabs.IonTabsWillChange += (_, e) => log.Add("will:" + e.Tab);
        tabs.IonTabsDidChange += (_, e) => log.Add("did:" + e.Tab);
        return log;
    }

    // ── The tabs ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T1_At_Load_The_First_Tab_Is_Selected_With_Both_Events_And_The_Bar_Is_Told()
    {
        var f = Make();
        var log = Log(f.Tabs);
        Assert.False(f.Home.IsVisible);

        using var scene = new Scene(f.Tabs);

        Assert.True(f.Home.IsVisible);
        Assert.False(f.Radio.IsVisible);
        Assert.Equal(["will:home", "did:home"], log);
        Assert.Equal("home", f.Tabs.GetSelected());
        Assert.Equal("home", f.Bar.SelectedTab);
        Assert.True(f.HomeButton.Selected);
        Assert.False(f.RadioButton.Selected);
    }

    [AvaloniaFact]
    public void T1_A_Tab_Selected_Before_The_Load_Is_Kept()
    {
        var f = Make();
        Assert.True(f.Tabs.Select("radio"));

        using var scene = new Scene(f.Tabs);

        Assert.Equal("radio", f.Tabs.GetSelected());
        Assert.True(f.Radio.IsVisible);
        Assert.False(f.Home.IsVisible);
        Assert.Equal("radio", f.Bar.SelectedTab);
    }

    [AvaloniaFact]
    public void T1_Tabs_And_A_Bar_That_Templates_Make_Are_Found()
    {
        var tabs = new AvonicTabs
        {
            Content = "pages",
            ContentTemplate = new FuncDataTemplate<string>((_, _) => new Panel { Children = { Page("one", "One"), Page("two", "Two") } }),
            BottomContent = "bar",
            BottomContentTemplate = new FuncDataTemplate<string>((_, _) => Bar(null, Button("one", "One"), Button("two", "Two"))),
        };
        using var scene = new Scene(tabs);

        Assert.Equal("one", tabs.GetSelected());
        var bar = tabs.GetVisualDescendants().OfType<AvonicTabBar>().Single();
        Assert.Equal("one", bar.SelectedTab);
        Assert.True(bar.Items.OfType<AvonicTabButton>().First().Selected);
        Assert.True(tabs.GetTab("one")!.IsVisible);
    }

    [AvaloniaFact]
    public void T2_A_Click_On_A_Tab_Button_Selects_Its_Tab_And_A_Click_On_The_Selected_One_Changes_Nothing()
    {
        var f = Make();
        using var scene = new Scene(f.Tabs);
        var log = Log(f.Tabs);

        Tap(scene, Centre(f.RadioButton, scene));

        Assert.Equal(["will:radio", "did:radio"], log);
        Assert.True(f.Radio.IsVisible);
        Assert.False(f.Home.IsVisible);
        Assert.True(f.RadioButton.Selected);
        Assert.False(f.HomeButton.Selected);

        log.Clear();
        Tap(scene, Centre(f.RadioButton, scene));
        Assert.Empty(log);
        Assert.True(f.Radio.IsVisible);
    }

    [AvaloniaFact]
    public void T3_Select_Is_False_For_A_Missing_Tab_The_Selected_Tab_Or_During_A_Selection()
    {
        var f = Make();
        using var scene = new Scene(f.Tabs);
        var log = Log(f.Tabs);

        Assert.False(f.Tabs.Select("nope"));
        Assert.False(f.Tabs.Select("home"));
        Assert.False(f.Tabs.Select(f.Home));
        Assert.Empty(log);

        // From a handler, a selection is under way, so a second is refused, as Ionic's transitioning flag refuses it.
        bool? nested = null;
        f.Tabs.IonTabsWillChange += (_, _) => nested ??= f.Tabs.Select("home");
        Assert.True(f.Tabs.Select("radio"));
        Assert.False(nested);
        Assert.Equal("radio", f.Tabs.GetSelected());

        // By reference too.
        Assert.True(f.Tabs.Select(f.Home));
        Assert.True(f.Home.IsVisible);
    }

    [AvaloniaFact]
    public void T4_WillChange_Comes_Before_Anything_Changes_And_DidChange_After_The_Tabs_And_The_Bar()
    {
        var f = Make();
        using var scene = new Scene(f.Tabs);
        string? atWill = null, atDid = null;
        string State() => $"{f.Home.IsVisible} {f.Radio.IsVisible} {f.Bar.SelectedTab} {f.HomeButton.Selected} {f.RadioButton.Selected}";
        f.Tabs.IonTabsWillChange += (_, _) => atWill = State();
        f.Tabs.IonTabsDidChange += (_, _) => atDid = State();

        f.Tabs.Select("radio");

        // Measured: will:radio bar=home sel=home; did:radio bar=radio sel=radio.
        Assert.Equal("True False home True False", atWill);
        Assert.Equal("False True radio False True", atDid);
    }

    [AvaloniaFact]
    public void T4_The_Events_Do_Not_Bubble()
    {
        var f = Make();
        var outer = new Border { Child = f.Tabs };
        var heard = 0;
        outer.AddHandler(AvonicTabs.IonTabsWillChangeEvent, (_, _) => heard++);
        outer.AddHandler(AvonicTabs.IonTabsDidChangeEvent, (_, _) => heard++);
        using var scene = new Scene(outer);

        f.Tabs.Select("radio");

        Assert.Equal(0, heard);
    }

    [AvaloniaFact]
    public void T5_GetSelected_And_GetTab()
    {
        var f = Make();
        Assert.Null(f.Tabs.GetSelected());
        using var scene = new Scene(f.Tabs);

        Assert.Equal("home", f.Tabs.GetSelected());
        Assert.Same(f.Radio, f.Tabs.GetTab("radio"));
        Assert.Null(f.Tabs.GetTab("zzz"));
        var elsewhere = new AvonicTab { Tab = "elsewhere" };
        Assert.Same(elsewhere, f.Tabs.GetTab(elsewhere));
    }

    [AvaloniaFact]
    public void T6_Only_The_First_Bar_Is_Told_And_A_Tab_With_No_Id_Leaves_It()
    {
        var f = Make();
        var topButton = Button("radio", "Radio");
        var top = Bar(null, topButton);
        f.Tabs.TopContent = top;
        using var scene = new Scene(f.Tabs);

        // querySelector('ion-tab-bar') finds the top one first.
        Assert.Equal("home", top.SelectedTab);
        Assert.Null(f.Bar.SelectedTab);

        // tab === undefined: the bar keeps what it has.
        var anonymous = new AvonicTab();
        ((Panel)f.Tabs.Content!).Children.Add(anonymous);
        Assert.True(f.Tabs.Select(anonymous));
        Assert.True(anonymous.IsVisible);
        Assert.Equal("home", top.SelectedTab);
    }

    [AvaloniaFact]
    public void T6_A_Bar_In_A_Bar_Slot_Comes_Before_One_In_A_Page()
    {
        var f = Make();
        var inPage = Bar("a", Button("a", "A"));
        f.Home.Content = inPage;
        using var scene = new Scene(f.Tabs);

        Assert.Equal("home", f.Bar.SelectedTab);
        Assert.Equal("a", inPage.SelectedTab);
    }

    [AvaloniaFact]
    public void T4_A_Handler_That_Throws_Leaves_The_Selection_As_It_Was()
    {
        var f = Make();
        using var scene = new Scene(f.Tabs);
        var throwing = true;
        f.Tabs.IonTabsWillChange += (_, _) =>
        {
            if (throwing)
            {
                throw new InvalidOperationException("from the app");
            }
        };

        Assert.Throws<InvalidOperationException>(() => f.Tabs.Select("radio"));
        Assert.Equal("home", f.Tabs.GetSelected());
        Assert.True(f.Home.IsVisible);
        Assert.False(f.Radio.IsVisible);

        throwing = false;
        Assert.True(f.Tabs.Select("radio"));
        Assert.True(f.Radio.IsVisible);
    }

    [AvaloniaFact]
    public void T6_Tabs_Anywhere_In_The_Slots_Are_Its_Tabs()
    {
        var deep = Page("deep", "Deep");
        var tabs = new AvonicTabs { Content = new Border { Child = new Panel { Children = { new Border { Child = deep } } } } };
        using var scene = new Scene(tabs);

        Assert.True(deep.IsVisible);
        Assert.Equal("deep", tabs.GetSelected());
    }

    [AvaloniaFact]
    public void T7_The_Default_Slot_Takes_What_The_Bars_Leave() => InstalledTheme.Run(_ =>
    {
        var f = Make();
        var topBar = Bar(null, Button("x", "Top"));
        f.Tabs.TopContent = topBar;
        using var scene = new Scene(f.Tabs, 390, 844);

        // Measured, with a bottom bar only: a tab 787 high over a bar 57 high at y 787.
        Assert.Equal(new Rect(0, 787, 390, 57), f.Bar.Bounds.TransformToAABB(f.Bar.GetVisualParent()!.TransformToVisual(scene.Window)!.Value));
        Assert.Equal(new Rect(0, 0, 390, 57), topBar.Bounds.TransformToAABB(topBar.GetVisualParent()!.TransformToVisual(scene.Window)!.Value));
        Assert.Equal(new Size(390, 844 - 57 - 57), f.Home.Bounds.Size);
    });

    // ── The tab ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A1_A_Tab_Is_Hidden_Until_It_Is_Active()
    {
        var tab = Page("a", "A");
        using var scene = new Scene(new Panel { Children = { tab } });

        Assert.False(tab.IsVisible);
        Assert.False(tab.IsActive);

        tab.SetActive();
        Assert.True(tab.IsVisible);
    }

    // :host(.tab-hidden) { display: none !important } beats an app's ion-tab rule, whenever the tab is hidden: from the start,
    // and after it was shown, when a value given at style priority would lose to the style or beat it by the order alone.
    [AvaloniaFact]
    public void A1_An_Apps_Plain_Style_Does_Not_Show_A_Hidden_Tab()
    {
        var a = Page("a", "A");
        var b = Page("b", "B");
        var tabs = new AvonicTabs { Content = new Panel { Children = { a, b } } };
        using var scene = Scene.Styled(tabs, Scene.AppStyle<AvonicTab>(Visual.IsVisibleProperty, true));
        // The first is selected at the start, and the second hidden from it.
        Assert.True(a.IsActive);
        Assert.False(b.IsVisible);

        tabs.Select("b");
        Dispatcher.UIThread.RunJobs();

        Assert.False(a.IsVisible);
        Assert.True(b.IsVisible);
    }

    // The deviation in the README: an app's own value shows a hidden tab, as it does everywhere here.
    [AvaloniaFact]
    public void A1_An_Apps_Own_IsVisible_Shows_A_Hidden_Tab()
    {
        var b = Page("b", "B");
        b.IsVisible = true;
        using var scene = new Scene(new AvonicTabs { Content = new Panel { Children = { Page("a", "A"), b } } });

        Assert.False(b.IsActive);
        Assert.True(b.IsVisible);
    }

    [AvaloniaFact]
    public void A2_A_Component_Is_Built_At_The_First_Activation_Once_As_A_Page()
    {
        LazyPage.Built = 0;
        var lazy = new AvonicTab { Tab = "lazy", Component = typeof(LazyPage) };
        var tabs = new AvonicTabs { Content = new Panel { Children = { Page("first", "First"), lazy } } };
        using var scene = new Scene(tabs);

        Assert.Equal(0, LazyPage.Built);
        Assert.Null(lazy.Content);

        tabs.Select("lazy");
        var page = Assert.IsType<LazyPage>(lazy.Content);
        Assert.Contains("ion-page", page.Classes);
        Assert.DoesNotContain("ion-page", lazy.Classes);

        tabs.Select("first");
        tabs.Select("lazy");
        Assert.Equal(1, LazyPage.Built);
        Assert.Same(page, lazy.Content);

        // Without a component, the tab is the page.
        Assert.Contains("ion-page", tabs.GetTab("first")!.Classes);
    }

    [AvaloniaFact]
    public void A2_A_Lazy_First_Tab_Logs_No_Error_And_One_With_Content_Does()
    {
        var previous = Avalonia.Logging.Logger.Sink;
        var sink = new ErrorSink();
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            var tabs = new AvonicTabs { Content = new Panel { Children = { new AvonicTab { Tab = "lazy", Component = typeof(LazyPage) } } } };
            using (new Scene(tabs))
            {
                Assert.IsType<LazyPage>(tabs.GetTab("lazy")!.Content);
            }

            Assert.DoesNotContain(sink.Errors, error => error.Contains("[ion-tab]"));

            using (new Scene(new Panel { Children = { new AvonicTab { Component = typeof(LazyPage), Content = "Inline" } } }))
            {
                Assert.Contains(sink.Errors, error => error.Contains("[ion-tab] - You can not use a lazy-loaded component"));
            }
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    private sealed class ErrorSink : Avalonia.Logging.ILogSink
    {
        public List<string> Errors { get; } = [];

        public bool IsEnabled(Avalonia.Logging.LogEventLevel level, string area) => level >= Avalonia.Logging.LogEventLevel.Error;

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate) => Errors.Add(messageTemplate);

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Errors.Add(messageTemplate + string.Join(",", propertyValues));
    }

    [AvaloniaFact]
    public void A2_A_Component_Beside_Inline_Content_Is_Never_Built()
    {
        LazyPage.Built = 0;
        var both = new AvonicTab { Tab = "b", Component = typeof(LazyPage), Content = "Inline" };
        using var scene = new Scene(new Panel { Children = { both } });

        both.SetActive();

        Assert.Equal(0, LazyPage.Built);
        Assert.Equal("Inline", both.Content);
    }

    [AvaloniaFact]
    public void A2_A_Component_Can_Be_A_Control_And_Inline_Content_Is_Kept()
    {
        var control = new TextBlock { Text = "Made" };
        var withComponent = new AvonicTab { Tab = "a", Component = control };
        var both = new AvonicTab { Tab = "b", Component = new TextBlock(), Content = "Inline" };
        using var scene = new Scene(new Panel { Children = { withComponent, both } });

        withComponent.SetActive();
        both.SetActive();

        Assert.Same(control, withComponent.Content);
        Assert.Equal("Inline", both.Content);
    }

    [AvaloniaFact]
    public void A3_SetActive_Shows_The_Tab_And_Hides_No_Other()
    {
        var f = Make();
        using var scene = new Scene(f.Tabs);

        f.Radio.SetActive();

        Assert.True(f.Radio.IsVisible);
        Assert.True(f.Home.IsVisible);
        Assert.Equal("home", f.Tabs.GetSelected());
    }

    [AvaloniaFact]
    public void A4_A_Tab_Is_A_Pane_Named_By_Its_Tab_Button()
    {
        var f = Make();
        f.Home.Content = new TextBlock { Text = "The page's own text" };
        using var scene = new Scene(f.Tabs);

        var peer = ControlAutomationPeer.CreatePeerForElement(f.Home);
        Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
        Assert.Equal("Home", peer.GetName());
        Assert.Same(ControlAutomationPeer.CreatePeerForElement(f.HomeButton), peer.GetLabeledBy());
    }

    private sealed class LazyPage : Border
    {
        public static int Built;

        public LazyPage() => Built++;
    }
}
