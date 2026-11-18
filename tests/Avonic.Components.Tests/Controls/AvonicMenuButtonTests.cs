using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Stage = Avonic.Components.Tests.Controls.AvonicMenuTests.Stage;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-menu-button</c> and <c>ion-menu-toggle</c>, md: B20 to B23 in docs/components/ion-menu/PLAN.md, from
/// <c>menu-button.js</c>, <c>menu-toggle.js</c>, <c>menu-toggle-util.js</c>, <c>menu-button.md.css</c> and the measurements in
/// <c>ref/measurements.txt</c>. The button is in a toolbar at the top of the main content, which takes no input while the menu is
/// open, as Ionic's has <c>pointer-events: none</c>.
/// </summary>
public class AvonicMenuButtonTests
{
    private const double Done = AvonicMenuTests.Done;

    /// <summary>The stage's main content: a toolbar with <paramref name="button"/> in its start buttons, then the main button.</summary>
    private static AvonicMenuButton InToolbar(Stage stage, AvonicMenuButton? button = null, string? toolbarColor = null)
    {
        button ??= new AvonicMenuButton();
        var toolbar = new AvonicToolbar
        {
            Color = toolbarColor,
            StartContent = new AvonicButtons { Items = { button } },
            Content = new AvonicTitle { Content = "Page" },
        };
        stage.Main.Child = null;
        (stage.MainButton.Parent as Panel)?.Children.Remove(stage.MainButton);
        stage.Main.Child = new DockPanel { Children = { toolbar, stage.MainButton } };
        DockPanel.SetDock(toolbar, Dock.Top);
        AvonicMenuTests.Stage.Settle();
        stage.Render();
        return button;
    }

    private static Rect Box(Visual element, Visual to) =>
        new Rect(element.Bounds.Size).TransformToAABB(element.TransformToVisual(to)!.Value);

    // ── B20: the button toggles its menu ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_A_Click_Toggles_The_Menu()
    {
        using var stage = new Stage();
        var button = InToolbar(stage);
        stage.Click(Box(button, stage.Window).Center);
        Assert.Equal(["ionWillOpen@0"], stage.Log);
        stage.At(Done);
        Assert.True(stage.Menu.IsOpen());
    }

    [AvaloniaFact]
    public void B20_Enter_And_Space_Toggle_It_As_A_Native_Button()
    {
        using var stage = new Stage();
        var button = InToolbar(stage);
        button.Focus();
        stage.Key(PhysicalKey.Enter);
        Assert.Equal(["ionWillOpen@0"], stage.Log);
        stage.At(Done);
        _ = stage.Menu.Close(false);
        AvonicMenuTests.Stage.Settle();
        stage.Log.Clear();
        stage.After(16);
        button.Focus();
        stage.Key(PhysicalKey.Space);
        Assert.Equal(["ionWillOpen@336"], stage.Log);
    }

    [AvaloniaFact]
    public void B20_Menu_Names_The_Menu_By_Its_Id_Or_Its_Side()
    {
        using var stage = new Stage(m => m.MenuId = "first");
        var end = stage.Watch(new AvonicMenu { MenuId = "second", ContentId = "Main", Side = MenuSide.End }, "end.");
        stage.Root.Children.Insert(1, end);
        var button = InToolbar(stage, new AvonicMenuButton { Menu = "end" });
        stage.Click(Box(button, stage.Window).Center);
        Assert.Equal(["end.ionWillOpen@0"], stage.Log);
        stage.At(Done);
        _ = end.Close(false);
        stage.After(16);
        stage.Log.Clear();
        button.Menu = "second";
        stage.Click(Box(button, stage.Window).Center);
        Assert.Equal(["end.ionWillOpen@336"], stage.Log);
    }

    [AvaloniaFact]
    public void B20_It_Hides_While_Its_Menu_Is_Not_Active_And_Follows_The_Menu()
    {
        using var stage = new Stage();
        var button = InToolbar(stage);
        Assert.True(button.IsVisible);

        stage.Menu.IsEnabled = false;
        Assert.False(button.IsVisible);

        stage.Menu.IsEnabled = true;
        Assert.True(button.IsVisible);

        button.AutoHide = false;
        stage.Menu.IsEnabled = false;
        Assert.True(button.IsVisible);
    }

    [AvaloniaFact]
    public void B20_With_No_Menu_It_Is_Hidden()
    {
        using var stage = new Stage(m => m.ContentId = "Nowhere");
        var button = InToolbar(stage);
        Assert.False(button.IsVisible);
    }

    [AvaloniaFact]
    public void B20_Disabled_It_Toggles_Nothing_And_Is_Half_Faded()
    {
        using var stage = new Stage();
        var button = InToolbar(stage, new AvonicMenuButton { IsEnabled = false });
        stage.Click(Box(button, stage.Window).Center);
        Assert.Empty(stage.Log);
        Assert.Equal(0.5, button.Opacity);
        var peer = (IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(button);
        Assert.False(ControlAutomationPeer.CreatePeerForElement(button).IsEnabled());
        peer.Invoke();
        Assert.Empty(stage.Log);
    }

    // ── B21: the look ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B21_It_Is_48_Square_At_4_4_With_The_Menu_Sharp_Icon_24_Centred()
    {
        using var stage = new Stage();
        var button = InToolbar(stage);
        Assert.Equal(new Rect(4, 4, 48, 48), Box(button, stage.Window));
        var icon = button.GetVisualDescendants().OfType<AvonicIcon>().Single(i => i.Name == "PART_Icon");
        Assert.Equal("menu-sharp", icon.IconName);
        Assert.Equal(new Rect(16, 16, 24, 24), Box(icon, stage.Window));
    }

    [AvaloniaFact]
    public void B21_The_Config_Menu_Icon_Is_Read_When_It_Renders()
    {
        using var stage = new Stage();
        AvonicConfig.MenuIcon = "menu-outline";
        try
        {
            var button = InToolbar(stage);
            var icon = button.GetVisualDescendants().OfType<AvonicIcon>().Single(i => i.Name == "PART_Icon");
            Assert.Equal("menu-outline", icon.IconName);
        }
        finally
        {
            AvonicConfig.MenuIcon = null;
        }
    }

    [AvaloniaFact]
    public void B21_Content_Replaces_The_Icon()
    {
        using var stage = new Stage();
        var mine = new AvonicIcon { IconName = "star" };
        var button = InToolbar(stage, new AvonicMenuButton { Content = mine });
        var icon = button.GetVisualDescendants().OfType<AvonicIcon>().Single(i => i.Name == "PART_Icon");
        Assert.False(icon.IsVisible);
        Assert.True(mine.IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void B21_Its_Colour_Is_The_Toolbars_And_Color_Gives_The_Native_Part_Its_Base()
    {
        using var stage = new Stage();
        var plain = InToolbar(stage);
        var native = plain.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Native");
        Assert.Equal(Color.FromRgb(0x42, 0x42, 0x42), ((ISolidColorBrush)native.GetValue(Avalonia.Controls.Documents.TextElement.ForegroundProperty)!).Color);

        var danger = InToolbar(stage, new AvonicMenuButton { Color = "danger" });
        native = danger.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Native");
        Assert.Equal(Color.FromRgb(0xc5, 0x00, 0x0f), ((ISolidColorBrush)native.GetValue(Avalonia.Controls.Documents.TextElement.ForegroundProperty)!).Color);

        var white = InToolbar(stage, new AvonicMenuButton(), "primary");
        Assert.True(((IPseudoClasses)white.Classes).Contains(":in-toolbar-color"));
        Assert.Equal(Colors.White, ((ISolidColorBrush)white.Foreground!).Color);
    }

    [AvaloniaFact]
    public void B21_Focused_By_Key_Its_Overlay_Is_Its_Colour_At_0_12_And_Hovered_At_0_04()
    {
        using var stage = new Stage();
        var button = InToolbar(stage);
        var overlay = button.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Overlay");
        Assert.Equal(0, overlay.Opacity);

        stage.Key(PhysicalKey.Tab);
        Assert.Same(button, stage.Focused);
        Assert.Equal(0.12, overlay.Opacity);
        Assert.Equal(Color.FromRgb(0x42, 0x42, 0x42), ((ISolidColorBrush)overlay.Background!).Color);

        stage.MainButton.Focus();
        stage.Window.MouseMove(Box(button, stage.Window).Center, RawInputModifiers.None);
        AvonicMenuTests.Stage.Settle();
        Assert.Equal(0.04, overlay.Opacity);
    }

    [AvaloniaFact]
    public void B21_Its_Ripple_Is_Unbounded()
    {
        using var stage = new Stage();
        var button = InToolbar(stage);
        var ripple = button.GetVisualDescendants().OfType<AvonicRippleEffect>().Single();
        Assert.Equal(RippleEffectType.Unbounded, ripple.Type);
        Assert.True(Activatable.GetIsEnabled(button));
        Assert.True(FocusVisible.GetIsEnabled(button));
    }

    // ── B22: automation ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B22_It_Is_A_Button_Named_Menu_And_Invoking_It_Toggles()
    {
        using var stage = new Stage();
        var button = InToolbar(stage);
        var peer = ControlAutomationPeer.CreatePeerForElement(button);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("menu", peer.GetName());
        AutomationProperties.SetName(button, "Open the menu");
        Assert.Equal("Open the menu", peer.GetName());

        ((IInvokeProvider)peer).Invoke();
        Assert.Equal(["ionWillOpen@0"], stage.Log);
    }

    // ── B23: the toggle ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B23_A_Click_On_A_Button_In_The_Toggle_Toggles_The_Menu()
    {
        using var stage = new Stage();
        var inside = new Button { Content = "Toggle", Height = 40 };
        var toggle = new AvonicMenuToggle { Content = inside };
        stage.Main.Child = new StackPanel { Children = { toggle } };
        stage.Render();
        stage.Click(Box(inside, stage.Window).Center);
        Assert.Equal(["ionWillOpen@0"], stage.Log);
    }

    [AvaloniaFact]
    public void B23_Enter_On_A_Button_In_The_Toggle_Toggles_It_And_A_Toggle_In_The_Menu_Closes_It()
    {
        using var stage = new Stage();
        var inMenu = new AvonicButton { Content = "Close" };
        var toggle = new AvonicMenuToggle { Content = inMenu };
        ((StackPanel)stage.Menu.Content!).Children.Add(toggle);
        stage.Opened();
        inMenu.Focus();
        stage.Key(PhysicalKey.Enter);
        Assert.Equal(["ionWillClose()@16"], stage.Log);
    }

    [AvaloniaFact]
    public void B23_The_Toggle_Hides_While_Its_Menu_Is_Not_Active()
    {
        using var stage = new Stage();
        var toggle = new AvonicMenuToggle { Content = new Button { Content = "Toggle" } };
        stage.Main.Child = new StackPanel { Children = { toggle } };
        AvonicMenuTests.Stage.Settle();
        Assert.True(toggle.IsVisible);
        stage.Menu.IsEnabled = false;
        Assert.False(toggle.IsVisible);
        toggle.AutoHide = false;
        Assert.True(toggle.IsVisible);

        var none = new AvonicMenuToggle { Menu = "nothing" };
        ((StackPanel)stage.Main.Child).Children.Add(none);
        AvonicMenuTests.Stage.Settle();
        Assert.False(none.IsVisible);
    }
}
