using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicNavTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-back-button</c>, md: K1 to K19 in docs/components/ion-back-button/PLAN.md, from <c>back-button.js</c>,
/// <c>back-button.md.css</c>, core.css's <c>.can-go-back &gt; ion-header ion-back-button</c>, <c>openURL</c> and
/// <c>ref/measurements.txt</c>. A nav's pages are <see cref="TestPage"/>s on its stage's clock.
/// </summary>
public class AvonicBackButtonTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────────────────────

    /// <summary>A toolbar with <paramref name="button"/> in its start buttons, as an app writes one.</summary>
    private static AvonicToolbar Toolbar(AvonicBackButton button, string? color = null) => new()
    {
        Color = color,
        StartContent = new AvonicButtons { Items = { button } },
        Content = new AvonicTitle { Content = "Page" },
    };

    /// <summary>A header holding a toolbar with <paramref name="button"/>.</summary>
    private static AvonicHeader Header(AvonicBackButton button) => new() { Items = { Toolbar(button) } };

    /// <summary>A page as an app writes one: a header at the top of a <see cref="DockPanel"/>, then <paramref name="content"/>.</summary>
    private static TestPage Page(string name, AvonicBackButton? headerButton, Control? content = null)
    {
        var dock = new DockPanel();
        if (headerButton is not null)
        {
            var header = Header(headerButton);
            DockPanel.SetDock(header, Dock.Top);
            dock.Children.Add(header);
        }

        dock.Children.Add(new AvonicContent { Content = content ?? new Panel() });
        return new TestPage(name) { Child = dock };
    }

    /// <summary>A stage whose nav's root is <paramref name="root"/>, loaded and drawn.</summary>
    private static Stage Loaded(Control root)
    {
        var stage = new Stage(nav => nav.Root = root);
        stage.Done(Task.FromResult(true));
        stage.Render();
        return stage;
    }

    /// <summary>A stage with no nav: <paramref name="content"/> fills the window.</summary>
    private static Stage NoNav(Control content)
    {
        var stage = new Stage(wrap: _ => content);
        stage.Render();
        return stage;
    }

    private static void Click(Stage stage, Control control)
    {
        stage.Render();
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), stage.Window)!.Value;
        stage.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        stage.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static T Part<T>(Visual control, string name) where T : Visual =>
        control.GetVisualDescendants().OfType<T>().Single(v => (v as Control)?.Name == name);

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    /// <summary>Every <see cref="AvonicBackButton.DefaultHrefRequested"/> that reaches the window.</summary>
    private static List<BackButtonDefaultHrefEventArgs> Requests(Stage stage)
    {
        var requests = new List<BackButtonDefaultHrefEventArgs>();
        stage.Window.AddHandler(AvonicBackButton.DefaultHrefRequestedEvent, (_, e) => requests.Add(e));
        return requests;
    }

    // ── K1: hidden until there's somewhere to go ──────────────────────────────────────────────

    [AvaloniaFact]
    public void K1_Hidden_By_Default_And_Shown_By_A_DefaultHref_Even_An_Empty_One()
    {
        var button = new AvonicBackButton();
        using var stage = NoNav(Toolbar(button));
        Assert.False(button.IsVisible);

        button.DefaultHref = "/home";
        Assert.True(button.IsVisible);
        button.DefaultHref = "";
        Assert.True(button.IsVisible);
        button.DefaultHref = null;
        Assert.False(button.IsVisible);
    }

    // An app's ion-back-button { display } beats the shadow root's display: none, whenever it hides. A value given at style
    // priority when it hides would come after the app's style, and win.
    [AvaloniaFact]
    public void K1_An_Apps_Style_Survives_It_Hiding_After_It()
    {
        var button = new AvonicBackButton { DefaultHref = "/" };
        using var scene = Scene.Styled(new StackPanel { Children = { button } }, Scene.AppStyle<AvonicBackButton>(Visual.IsVisibleProperty, true));

        button.DefaultHref = null;
        Dispatcher.UIThread.RunJobs();

        Assert.True(button.IsVisible);
    }

    // ── K2, K3: shown in the header of a page that can go back ───────────────────────────────

    [AvaloniaFact]
    public void K2_Shown_In_The_Header_Of_A_Pushed_Page_And_Hidden_On_The_Root()
    {
        var rootButton = new AvonicBackButton();
        var twoButton = new AvonicBackButton();
        using var stage = Loaded(Page("one", rootButton));
        Assert.False(rootButton.IsVisible);

        var two = Page("two", twoButton);
        var push = stage.Nav.Push(two);

        // It follows the pseudoclass the moment the nav sets it, before the transition has ended.
        while (!CanGoBackClass(two))
        {
            stage.After(1);
            Assert.False(twoButton.IsVisible && !CanGoBackClass(two));
        }

        Assert.True(twoButton.IsVisible);
        Assert.False(push.IsCompleted);
        stage.Done(push);
        Assert.True(twoButton.IsEffectivelyVisible);
        Assert.False(rootButton.IsVisible);
    }

    [AvaloniaFact]
    public void K2_Hidden_Again_When_Its_Page_Loses_Can_Go_Back()
    {
        var button = new AvonicBackButton();
        var two = Page("two", button);
        using var stage = Loaded(Page("one", null));
        stage.Done(stage.Nav.Push(two));
        Assert.True(button.IsVisible);

        // The page under it goes, with no transition, and a page over it is popped: it enters again with nothing under it, and
        // the nav takes can-go-back off it, while it's still attached.
        stage.Done(stage.Nav.Push(Page("three", null)));
        stage.Done(stage.Nav.RemoveIndex(0));
        Assert.True(button.IsVisible);
        stage.Done(stage.Nav.Pop());
        Assert.False(CanGoBackClass(two));
        Assert.False(button.IsVisible);
    }

    [AvaloniaFact]
    public void K3_Not_Shown_In_The_Content_Nor_In_A_Header_That_Isnt_The_Pages_Child()
    {
        var inContent = new AvonicBackButton();
        var inNestedHeader = new AvonicBackButton();
        var header = new AvonicBackButton();
        var content = new StackPanel { Children = { inContent, Header(inNestedHeader) } };
        using var stage = Loaded(Page("one", null));
        var two = Page("two", header, content);
        stage.Done(stage.Nav.Push(two));

        Assert.True(CanGoBackClass(two));
        Assert.True(header.IsVisible);
        Assert.False(inContent.IsVisible);
        Assert.False(inNestedHeader.IsVisible);
    }

    [AvaloniaFact]
    public void K3_Panels_Between_The_Page_And_Its_Header_Stand_For_The_Page_And_A_Border_Doesnt()
    {
        var throughPanels = new AvonicBackButton();
        var throughBorder = new AvonicBackButton();
        var grid = new Grid { Children = { new StackPanel { Children = { Header(throughPanels) } } } };
        var page = new TestPage("two") { Child = new DockPanel { Children = { grid, new Border { Child = Header(throughBorder) } } } };
        using var stage = Loaded(Page("one", null));
        stage.Done(stage.Nav.Push(page));

        Assert.True(throughPanels.IsVisible);
        Assert.False(throughBorder.IsVisible);
    }

    // ── K4 to K6: the click ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void K4_A_Click_Pops_The_Closest_Nav_And_A_Second_During_The_Pop_Does_Nothing()
    {
        var button = new AvonicBackButton();
        using var stage = Loaded(Page("one", null));
        stage.Stack(Page("two", null), Page("three", button));
        Assert.Equal(3, stage.Nav.GetLength());

        Click(stage, button);
        stage.After(50);
        Click(stage, button);
        stage.After(600);
        Assert.Equal(2, stage.Nav.GetLength());
        Assert.Equal("two", stage.Nav.GetActive()!.Element!.Name);
    }

    [AvaloniaFact]
    public void K4_In_A_Nav_Inside_Another_Nav_Page_It_Pops_The_Inner_One()
    {
        var button = new AvonicBackButton();
        var inner = new AvonicNav { Root = Page("inner-one", null) };
        using var stage = Loaded(Page("one", null));
        stage.Stack(new TestPage("outer-two") { Child = inner });
        stage.Done(inner.Push(Page("inner-two", button)));
        stage.Render();

        Click(stage, button);
        stage.After(600);
        Assert.Equal(1, inner.GetLength());
        Assert.Equal(2, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void K5_The_Pop_Plays_Its_RouterAnimation()
    {
        var used = 0;
        AnimationBuilder counting = (_, options) =>
        {
            used++;
            return AvonicAnimation.Create().AddElement(((NavTransitionOptions)options!).LeavingElement!).Duration(50).FromTo("opacity", 1, 0.5);
        };
        var button = new AvonicBackButton { RouterAnimation = counting };
        using var stage = Loaded(Page("one", null));
        stage.Stack(Page("two", button));

        Click(stage, button);
        stage.After(600);
        Assert.Equal(1, used);
        Assert.Equal(1, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void K6_With_Nothing_To_Pop_And_No_DefaultHref_A_Click_Does_Nothing_Though_A_Stale_Page_Still_Shows_It()
    {
        // Push, then take the page under away with no transition: can-go-back stays, as Ionic sets it only at a transition.
        var button = new AvonicBackButton();
        using var stage = Loaded(Page("one", null));
        var two = Page("two", button);
        stage.Done(stage.Nav.Push(two));
        stage.Done(stage.Nav.RemoveIndex(0));
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.False(stage.Nav.CanGoBack());
        Assert.True(button.IsVisible);

        var requests = Requests(stage);
        Click(stage, button);
        stage.After(600);
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.Empty(requests);
        Assert.Empty(stage.Log);
    }

    // ── K7: DefaultHrefRequested ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void K7_With_Nothing_To_Pop_A_Click_Asks_The_App_For_The_DefaultHref_With_The_RouterAnimation()
    {
        AnimationBuilder animation = (_, _) => AvonicAnimation.Create();
        var button = new AvonicBackButton { DefaultHref = "/home", RouterAnimation = animation };
        using var stage = Loaded(Page("one", button));
        var requests = Requests(stage);

        Click(stage, button);
        var request = Assert.Single(requests);
        Assert.Equal("/home", request.DefaultHref);
        Assert.Same(animation, request.RouterAnimation);
        Assert.Same(button, request.Source);
        Assert.Equal(1, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void K7_It_Asks_Nothing_When_It_Pops()
    {
        var button = new AvonicBackButton { DefaultHref = "/home" };
        using var stage = Loaded(Page("one", null));
        stage.Stack(Page("two", button));
        var requests = Requests(stage);

        Click(stage, button);
        stage.After(600);
        Assert.Empty(requests);
        Assert.Equal(1, stage.Nav.GetLength());
    }

    [AvaloniaFact]
    public void K7_It_Asks_Only_For_What_Ionics_OpenURL_Gives_The_Router()
    {
        var button = new AvonicBackButton();
        using var stage = NoNav(new StackPanel { Children = { button } });
        var requests = Requests(stage);

        foreach (var href in new[] { "#top", "https://example.com", "mailto:someone", "", "Http:x", "home", "/a#b" })
        {
            button.DefaultHref = href;
            Click(stage, button);
        }

        Assert.Equal(["", "Http:x", "home", "/a#b"], requests.Select(r => r.DefaultHref));
    }

    // ── K8: showGoBack ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void K8_ShowGoBack_False_Hides_The_Header_Button_But_A_Shown_One_There_Still_Pops()
    {
        var header = new AvonicBackButton();
        var shown = new AvonicBackButton { DefaultHref = "/x" };
        using var stage = Loaded(Page("one", null));
        stage.Done(stage.Nav.Push(Page("two", header, new StackPanel { Children = { shown } }), opts: new NavOptions { ShowGoBack = false }));
        Assert.False(header.IsVisible);
        Assert.True(stage.Nav.CanGoBack());

        var requests = Requests(stage);
        Click(stage, shown);
        stage.After(600);
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.Empty(requests);
    }

    // ── K9, K10: disabled, keys, automation's invoke ─────────────────────────────────────────

    [AvaloniaFact]
    public void K9_Disabled_It_Neither_Pops_Nor_Asks_And_Is_At_Half_Opacity()
    {
        var button = new AvonicBackButton { IsEnabled = false, DefaultHref = "/home" };
        using var stage = Loaded(Page("one", null));
        stage.Stack(Page("two", button));
        var requests = Requests(stage);
        Assert.Equal(0.5, button.Opacity);

        Click(stage, button);
        ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(button)).Invoke();
        stage.After(600);
        Assert.Equal(2, stage.Nav.GetLength());
        Assert.Empty(requests);
    }

    [AvaloniaFact]
    public void K10_Enter_Space_And_Invoke_Click_It_As_A_Native_Button()
    {
        var button = new AvonicBackButton();
        using var stage = Loaded(Page("one", null));
        stage.Stack(Page("two", null), Page("three", null), Page("four", button));

        button.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        stage.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        stage.After(600);
        Assert.Equal(3, stage.Nav.GetLength());

        var three = new AvonicBackButton();
        stage.Done(stage.Nav.Push(Page("four", three)));
        three.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        stage.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        stage.After(600);
        Assert.Equal(3, stage.Nav.GetLength());

        var invoked = new AvonicBackButton();
        stage.Done(stage.Nav.Push(Page("four", invoked)));
        ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(invoked)).Invoke();
        stage.After(600);
        Assert.Equal(3, stage.Nav.GetLength());
    }

    // ── K11, K12: the icon and the text ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void K11_The_Icon_Is_Mds_Arrow_Mirrored_In_RTL_Or_The_Config_Or_Its_Own_And_Empty_Is_None()
    {
        var button = new AvonicBackButton { DefaultHref = "/" };
        using var stage = NoNav(new StackPanel { Children = { button } });
        var icon = Part<AvonicIcon>(button, "PART_Icon");
        Assert.Equal("arrow-back-sharp", icon.Icon);
        Assert.True(icon.FlipRtl);
        Assert.True(icon.IsVisible);

        var previous = AvonicConfig.BackButtonIcon;
        try
        {
            // Read at each render: a change of a prop reads it again.
            AvonicConfig.BackButtonIcon = "home";
            button.Color = "danger";
            Assert.Equal("home", icon.Icon);
            Assert.True(icon.FlipRtl);

            button.Icon = "close";
            Assert.Equal("close", icon.Icon);
            Assert.Null(icon.FlipRtl);

            button.Icon = "";
            Assert.False(icon.IsVisible);
        }
        finally
        {
            AvonicConfig.BackButtonIcon = previous;
        }
    }

    [AvaloniaFact]
    public void K12_No_Text_In_Md_Its_Text_Uppercase_Or_The_Configs_And_Empty_Is_None()
    {
        var button = new AvonicBackButton { DefaultHref = "/" };
        using var stage = NoNav(new StackPanel { Children = { button } });
        var text = Part<TextBlock>(button, "PART_Text");
        Assert.False(text.IsVisible);

        button.Text = "Back";
        Assert.True(text.IsVisible);
        Assert.Equal("BACK", text.Text);
        button.TextTransform = ButtonTextTransform.None;
        Assert.Equal("Back", text.Text);

        var previous = AvonicConfig.BackButtonText;
        try
        {
            AvonicConfig.BackButtonText = "Cfg";
            button.Text = null;
            Assert.Equal("Cfg", text.Text);
            button.Text = "";
            Assert.False(text.IsVisible);
            Assert.True(((IPseudoClasses)button.Classes).Contains(":icon-only"));
        }
        finally
        {
            AvonicConfig.BackButtonText = previous;
        }
    }

    // ── K13, K14: sizes and the ripple ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void K13_Icon_Only_It_Is_48_Square_Round_With_An_Unbounded_Ripple()
    {
        var button = new AvonicBackButton { DefaultHref = "/" };
        using var stage = NoNav(Toolbar(button));
        Assert.Equal(new Size(48, 48), button.Bounds.Size);
        var native = Part<Border>(button, "PART_Native");
        Assert.Equal(new CornerRadius(24), native.CornerRadius);
        Assert.Equal(RippleEffectType.Unbounded, Part<AvonicRippleEffect>(button, "PART_Ripple").Type);
        var icon = Part<AvonicIcon>(button, "PART_Icon");
        Assert.Equal(new Rect(12, 12, 24, 24), icon.Bounds.WithX(icon.TranslatePoint(default, button)!.Value.X).WithY(icon.TranslatePoint(default, button)!.Value.Y));
    }

    [AvaloniaFact]
    public void K13_With_Text_It_Is_32_High_With_Corners_Of_4_And_A_Bounded_Ripple()
    {
        var button = new AvonicBackButton { DefaultHref = "/", Text = "Back" };
        using var stage = NoNav(Toolbar(button));
        Assert.Equal(32, button.Bounds.Height);
        // Measured 92.02: 12, the icon's 24, the text's 36.02 and its 4 either side, 12.
        Assert.InRange(button.Bounds.Width, 91, 93);
        Assert.Equal(new CornerRadius(4), Part<Border>(button, "PART_Native").CornerRadius);
        Assert.Equal(RippleEffectType.Bounded, Part<AvonicRippleEffect>(button, "PART_Ripple").Type);
        Assert.False(((IPseudoClasses)button.Classes).Contains(":icon-only"));
    }

    [AvaloniaFact]
    public void K14_Icon_Only_And_Stretched_Across_A_Line_It_Is_As_Tall_As_It_Is_Wide()
    {
        var iconOnly = new AvonicBackButton { DefaultHref = "/" };
        var withText = new AvonicBackButton { DefaultHref = "/", Text = "Back" };
        using var stage = NoNav(new StackPanel { Children = { iconOnly, withText } });
        Assert.Equal(new Size(390, 390), iconOnly.Bounds.Size);
        Assert.Equal(new Size(390, 32), withText.Bounds.Size);

        // Given text, it's 32 high again; and back to a square without it.
        iconOnly.Text = "Back";
        stage.Render();
        Assert.Equal(32, iconOnly.Bounds.Height);
        iconOnly.Text = null;
        stage.Render();
        Assert.Equal(390, iconOnly.Bounds.Height);
    }

    // ── K15: the config's defaultHref ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void K15_The_Configs_DefaultHref_Is_Read_Once_When_It_Is_First_Attached_If_It_Has_None()
    {
        var previous = AvonicConfig.BackButtonDefaultHref;
        try
        {
            AvonicConfig.BackButtonDefaultHref = "/cfg";
            var unset = new AvonicBackButton();
            var own = new AvonicBackButton { DefaultHref = "/mine" };
            Assert.Null(unset.DefaultHref);
            using var stage = NoNav(new StackPanel { Children = { unset, own } });
            Assert.Equal("/cfg", unset.DefaultHref);
            Assert.True(unset.IsVisible);
            Assert.Equal("/mine", own.DefaultHref);

            // Cleared later, it isn't read again.
            unset.DefaultHref = null;
            var panel = (StackPanel)unset.Parent!;
            panel.Children.Remove(unset);
            panel.Children.Add(unset);
            Dispatcher.UIThread.RunJobs();
            Assert.Null(unset.DefaultHref);
            Assert.False(unset.IsVisible);
        }
        finally
        {
            AvonicConfig.BackButtonDefaultHref = previous;
        }
    }

    // ── K16, K17: colour, hover, focus, the native's opacity ─────────────────────────────────

    [AvaloniaFact]
    public void K16_The_Native_Takes_The_Base_Colour_The_Toolbars_Colour_Or_A_Coloured_Toolbars_Contrast()
    {
        var plain = new AvonicBackButton { DefaultHref = "/" };
        var danger = new AvonicBackButton { DefaultHref = "/", Color = "danger" };
        var inPrimary = new AvonicBackButton { DefaultHref = "/" };
        using var stage = NoNav(new StackPanel { Children = { Toolbar(plain), Toolbar(danger), Toolbar(inPrimary, "primary") } });

        Assert.Equal(Color.FromRgb(0x42, 0x42, 0x42), ColorOf(Part<Border>(plain, "PART_Native").GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(Color.FromRgb(0xc5, 0x00, 0x0f), ColorOf(Part<Border>(danger, "PART_Native").GetValue(TextElement.ForegroundProperty)));
        Assert.True(((IPseudoClasses)inPrimary.Classes).Contains(":in-toolbar-color"));
        Assert.Equal(Colors.White, ColorOf(inPrimary.Foreground));
    }

    [AvaloniaFact]
    public void K16_In_A_Toolbar_With_No_Colour_It_Takes_The_Toolbar_Colour_Variable()
    {
        var button = new AvonicBackButton { DefaultHref = "/" };
        var toolbar = Toolbar(button);
        using var stage = NoNav(toolbar);
        stage.Window.Resources["Avonic.ToolbarColorBrush"] = new SolidColorBrush(Colors.Blue);
        toolbar.Foreground = new SolidColorBrush(Colors.Red);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Blue, ColorOf(button.Foreground));
    }

    [AvaloniaFact]
    public void K17_Focused_By_Key_The_Overlay_Is_Its_Colour_At_0_12_And_Hovered_At_0_04_And_A_Colours_Base()
    {
        var first = new Button { Content = "Before" };
        var button = new AvonicBackButton { DefaultHref = "/" };
        var danger = new AvonicBackButton { DefaultHref = "/", Color = "danger" };
        using var stage = NoNav(new StackPanel { Children = { first, Toolbar(button), Toolbar(danger) } });
        var overlay = Part<Border>(button, "PART_Overlay");
        Assert.Equal(0, overlay.Opacity);

        first.Focus();
        stage.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        stage.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(button, stage.Window.FocusManager!.GetFocusedElement());
        Assert.Equal(0.12, overlay.Opacity);
        Assert.Equal(Color.FromRgb(0x42, 0x42, 0x42), ColorOf(overlay.Background));

        first.Focus();
        stage.Render();
        var at = danger.TranslatePoint(new Point(24, 24), stage.Window)!.Value;
        stage.Window.MouseMove(at, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        var dangerOverlay = Part<Border>(danger, "PART_Overlay");
        Assert.Equal(0.04, dangerOverlay.Opacity);
        Assert.Equal(Color.FromRgb(0xc5, 0x00, 0x0f), ColorOf(dangerOverlay.Background));
    }

    [AvaloniaFact]
    public void K17_The_Natives_Opacity_Eases_Over_100_ms()
    {
        var button = new AvonicBackButton { DefaultHref = "/" };
        using var stage = NoNav(new StackPanel { Children = { button } });
        var native = Part<Border>(button, "PART_Native");
        Assert.Equal(1, native.Opacity);
        button.Resources["Avonic.BackButton.Opacity"] = 0.2;
        Dispatcher.UIThread.RunJobs();
        Assert.True(native.Opacity > 0.2, $"at once {native.Opacity}");
    }

    // ── K18: automation ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void K18_A_Button_Named_Back_Its_Text_As_Written_Or_The_Apps_Name_With_No_Children()
    {
        var button = new AvonicBackButton { DefaultHref = "/" };
        using var stage = NoNav(new StackPanel { Children = { button } });
        var peer = ControlAutomationPeer.CreatePeerForElement(button);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("back", peer.GetName());
        Assert.Empty(peer.GetChildren());

        button.Text = "Back";
        Assert.Equal("Back", peer.GetName());
        AutomationProperties.SetName(button, "Go home");
        Assert.Equal("Go home", peer.GetName());

        button.IsEnabled = false;
        Assert.False(peer.IsEnabled());
    }

    // ── K20: DefaultPage (extension) ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void K20_A_DefaultPage_Shows_It_As_A_DefaultHref_Does()
    {
        var button = new AvonicBackButton();
        using var stage = NoNav(Toolbar(button));
        Assert.False(button.IsVisible);
        button.DefaultPage = typeof(PageTwo);
        Assert.True(button.IsVisible);
        button.DefaultPage = null;
        Assert.False(button.IsVisible);
    }

    [AvaloniaFact]
    public void K20_With_Nothing_To_Pop_It_Sets_The_DefaultPage_As_The_Nav_Root_And_Asks_Nothing()
    {
        // The nav doesn't animate a root that replaces its only page, as Ionic's doesn't (ref/measurements.txt B6: setRoot with a
        // back direction and a builder calls no builder), so the back direction and RouterAnimation it's given don't play.
        var button = new AvonicBackButton { DefaultPage = typeof(PageTwo), DefaultHref = "/home" };
        using var stage = Loaded(Page("one", button));
        var requests = Requests(stage);

        Click(stage, button);
        stage.After(600);
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.IsType<PageTwo>(stage.Nav.GetActive()!.Element);
        Assert.Empty(requests);
    }

    [AvaloniaFact]
    public void K20_It_Pops_First_When_The_Nav_Can_Go_Back()
    {
        var button = new AvonicBackButton { DefaultPage = typeof(PageThree) };
        using var stage = Loaded(Page("one", null));
        stage.Stack(Page("two", button));

        Click(stage, button);
        stage.After(600);
        Assert.Equal(1, stage.Nav.GetLength());
        Assert.Equal("one", stage.Nav.GetActive()!.Element!.Name);
    }

    [AvaloniaFact]
    public void K20_With_No_Nav_Around_The_Click_Is_The_DefaultHrefs()
    {
        var withHref = new AvonicBackButton { DefaultPage = typeof(PageTwo), DefaultHref = "/home" };
        var pageOnly = new AvonicBackButton { DefaultPage = typeof(PageTwo) };
        using var stage = NoNav(new StackPanel { Children = { withHref, pageOnly } });
        var requests = Requests(stage);

        Click(stage, withHref);
        Click(stage, pageOnly);
        Assert.Equal(["/home"], requests.Select(r => r.DefaultHref));
    }

    // ── K19: no hardware back of its own ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void K19_A_Hardware_Back_Request_Pops_Only_Through_The_Nav_Not_The_Back_Button()
    {
        var button = new AvonicBackButton { DefaultHref = "/home" };
        using var stage = Loaded(Page("one", null));
        stage.Stack(Page("two", null), Page("three", button));
        stage.Nav.HandleBackButton = false;
        var requests = Requests(stage);

        stage.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        stage.After(600);
        Assert.Equal(3, stage.Nav.GetLength());
        Assert.Empty(requests);

        stage.Nav.HandleBackButton = true;
        stage.Window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));
        stage.After(600);
        Assert.Equal(2, stage.Nav.GetLength());
    }
}
