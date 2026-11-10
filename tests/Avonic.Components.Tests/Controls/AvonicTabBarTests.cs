using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicTabsCaptureTests;
using static Avonic.Components.Tests.Controls.AvonicTabsTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>ion-tab-bar's behaviours, from <c>tab-bar.js</c> and <c>tab-bar.md.css</c>: see <c>docs/components/ion-tabs/PLAN.md</c>.</summary>
public class AvonicTabBarTests
{
    internal static Rect Box(Visual visual, Visual to) => new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(to)!.Value);

    /// <summary>A phone's 2 device pixels to the pixel, which layout rounds to, as Chromium's half pixels in the measurements.</summary>
    internal static void Phone(Scene scene)
    {
        scene.Window.SetRenderScaling(2);
        AvonicTabsTests.Settle();
    }

    internal static Color ColorOf(IBrush? brush) => brush is ISolidColorBrush solid ? Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity, MidpointRounding.AwayFromZero), solid.Color.R, solid.Color.G, solid.Color.B) : default;

    private static T Part<T>(Visual control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static StackPanel Top(params Control[] children)
    {
        var panel = new StackPanel();
        foreach (var child in children)
        {
            panel.Children.Add(child);
        }

        return panel;
    }

    // ── Selection ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_SelectedTab_Marks_Its_Button_From_The_Load_On()
    {
        var a = Button("a", "A");
        var b = Button("b", "B");
        var bar = Bar("b", a, b);

        // Before the load, the watcher is skipped.
        Assert.False(b.Selected);

        using var scene = new Scene(Top(bar));
        Assert.True(b.Selected);
        Assert.False(a.Selected);

        bar.SelectedTab = "a";
        Assert.True(a.Selected);
        Assert.False(b.Selected);

        // Measured: an id no button has deselects them all, and none at all changes nothing.
        bar.SelectedTab = "zzz";
        Assert.False(a.Selected);
        a.Selected = true;
        bar.SelectedTab = null;
        Assert.True(a.Selected);
    }

    [AvaloniaFact]
    public void B1_A_Button_Keeps_Its_Own_Selected_Until_The_Bar_Has_A_Tab()
    {
        var a = Button("a", "A");
        a.Selected = true;
        var bar = Bar(null, a, Button("b", "B"));
        using var scene = new Scene(Top(bar));

        Assert.True(a.Selected);
    }

    [AvaloniaFact]
    public void B1_Buttons_From_An_ItemsSource_Are_Marked_At_The_Load()
    {
        var bar = new AvonicTabBar
        {
            SelectedTab = "b",
            ItemsSource = new[] { "a", "b" },
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((tab, _) => Button(tab, tab)),
        };
        using var scene = new Scene(Top(bar));

        var buttons = bar.GetVisualDescendants().OfType<AvonicTabButton>().ToList();
        Assert.Equal(2, buttons.Count);
        Assert.True(buttons.Single(button => button.Tab == "b").Selected);
    }

    [AvaloniaFact]
    public void B2_A_Click_With_No_Tabs_Around_Selects_Nothing()
    {
        var a = Button("a", "A");
        var b = Button("b", "B");
        var bar = Bar("a", a, b);
        using var scene = new Scene(Top(bar));

        Tap(scene, Centre(b, scene));

        Assert.Equal("a", bar.SelectedTab);
        Assert.True(a.Selected);
        Assert.False(b.Selected);
    }

    // ── Layout ───────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_The_Buttons_Share_The_Width_Up_To_168_Centred() => InstalledTheme.Run(_ =>
    {
        var four = Bar(null, Button("a", "Listen now", "play-circle"), Button("b", "Radio", "radio"), Button("c", "Library", "library"), Button("d", "Search", "search"));
        var one = Bar(null, Button("a", "One button, max width", "calendar"));
        var two = Bar(null, Button("a", "Top"), Button("b", "Slot"));
        using var scene = new Scene(Top(four, one, two));
        Phone(scene);

        // Measured: 97.5 each; one at 168 from x 111; two at 168 from x 27.
        Assert.Equal([0, 97.5, 195, 292.5], four.Items.OfType<Control>().Select(b => Box(b, four).X));
        Assert.All(four.Items.OfType<Control>(), b => Assert.Equal(97.5, b.Bounds.Width));
        Assert.Equal(new Rect(111, 1, 168, 56), Box((Control)one.Items[0]!, one));
        Assert.Equal(27, Box((Control)two.Items[0]!, two).X);
    });

    [AvaloniaFact]
    public void B3_A_Button_Is_At_Least_Its_Min_Content() => InstalledTheme.Run(_ =>
    {
        var bar = Bar(null, Button("a", "An extremely long label that cannot possibly fit", "map"), Button("b", "B"), Button("c", "C"));
        using var scene = new Scene(Top(bar));
        Phone(scene);

        // Measured: 168, 111, 111.
        Assert.Equal([168, 111, 111], bar.Items.OfType<Control>().Select(b => b.Bounds.Width));
    });

    [AvaloniaFact]
    public void B4_The_Bar_Is_56_High_Inside_A_Border_On_Top_With_The_Safe_Areas() => InstalledTheme.Run(_ =>
    {
        var bar = Bar(null, Button("a", "A"));
        var padded = Bar(null, Button("a", "A"));
        padded.Resources["Avonic.SafeAreaBottom"] = 20.0;
        padded.Resources["Avonic.SafeAreaLeft"] = 5.0;
        padded.Resources["Avonic.SafeAreaTop"] = 30.0;
        using var scene = new Scene(Top(bar, padded));

        Assert.Equal(57, bar.Bounds.Height);
        Assert.Equal(Colors.White, ColorOf(bar.Background));
        Assert.Equal(Color.Parse("#595959"), ColorOf(bar.Foreground));
        Assert.Equal(new Thickness(0, 1, 0, 0), bar.BorderThickness);
        Assert.Equal(Color.Parse("#12000000"), ColorOf(bar.BorderBrush));
        // padding-bottom and padding-left from the safe areas, and none on top.
        Assert.Equal(new Thickness(5, 0, 0, 20), padded.Padding);
        Assert.Equal(77, padded.Bounds.Height);
    });

    [AvaloniaFact]
    public void B4_In_The_Top_Slot_The_Border_Is_At_The_Bottom_With_The_Top_Safe_Area() => InstalledTheme.Run(_ =>
    {
        var bar = Bar(null, Button("a", "A"));
        bar.Resources["Avonic.SafeAreaTop"] = 30.0;
        bar.Resources["Avonic.SafeAreaBottom"] = 20.0;
        var tabs = new AvonicTabs { TopContent = bar, Content = Page("a", "A") };
        using var scene = new Scene(tabs);

        Assert.Equal(new Thickness(0, 0, 0, 1), bar.BorderThickness);
        Assert.Equal(new Thickness(0, 30, 0, 0), bar.Padding);

        // Moved to the bottom slot, it's an ordinary bar again.
        tabs.TopContent = null;
        tabs.BottomContent = bar;
        AvonicTabsTests.Settle();
        Assert.Equal(new Thickness(0, 1, 0, 0), bar.BorderThickness);
    });

    // ── Colour ───────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_A_Colour_Paints_The_Bar_And_Its_Buttons() => InstalledTheme.Run(_ =>
    {
        var a = Button("a", "A", "calendar");
        var b = Button("b", "B", "map");
        var bar = Bar("a", a, b);
        bar.Color = "primary";
        using var scene = new Scene(Top(bar));

        // Measured: the bar rgb(0, 84, 233), rgba(255, 255, 255, 0.7); a button's the same, a selected one's white.
        var primary = Color.Parse("#0054E9");
        Assert.Equal(primary, ColorOf(bar.Background));
        Assert.Equal(Color.FromArgb(179, 255, 255, 255), ColorOf(bar.Foreground));
        Assert.Equal(primary, ColorOf(b.Background));
        Assert.Equal(Color.FromArgb(179, 255, 255, 255), ColorOf(b.Foreground));
        Assert.Equal(Colors.White, ColorOf(a.Foreground));
        // The ripple is the contrast.
        Assert.Equal(Colors.White, ColorOf(Part<AvonicRippleEffect>(b, "PART_Ripple").Foreground));

        // Without, the buttons have the bar's background and colour.
        bar.Color = null;
        Assert.Equal(Colors.White, ColorOf(b.Background));
        Assert.Equal(Color.Parse("#595959"), ColorOf(b.Foreground));
        Assert.Equal(primary, ColorOf(Part<AvonicRippleEffect>(b, "PART_Ripple").Foreground));
    });

    [AvaloniaFact]
    public void B5_A_Variable_Set_On_The_Bar_Reaches_Its_Buttons() => InstalledTheme.Run(_ =>
    {
        var b = Button("b", "B");
        var bar = Bar(null, b);
        bar.Resources["Avonic.TabBar.Background"] = new SolidColorBrush(Colors.Red);
        bar.Resources["Avonic.TabBar.Color"] = new SolidColorBrush(Colors.Green);
        using var scene = new Scene(Top(bar));

        Assert.Equal(Colors.Red, ColorOf(bar.Background));
        Assert.Equal(Colors.Red, ColorOf(b.Background));
        Assert.Equal(Colors.Green, ColorOf(b.Foreground));
    });

    [AvaloniaFact]
    public void B6_Translucent_Clears_The_Buttons_Background_Whatever_Their_Own() => InstalledTheme.Run(_ =>
    {
        var b = Button("b", "B");
        b.Resources["Avonic.TabButton.Background"] = new SolidColorBrush(Colors.Red);
        var bar = Bar(null, b);
        using var scene = new Scene(Top(bar));
        Assert.Equal(Colors.Red, ColorOf(b.Background));

        bar.Translucent = true;

        // Measured, with --background: red on the button: rgba(0, 0, 0, 0).
        Assert.Equal(Colors.Transparent, ColorOf(b.Background));
    });

    [AvaloniaFact]
    public void B7_The_Bar_Is_A_Tab_List()
    {
        var bar = Bar(null, Button("a", "A"));
        using var scene = new Scene(Top(bar));

        Assert.Equal(AutomationControlType.Tab, ControlAutomationPeer.CreatePeerForElement(bar).GetAutomationControlType());
    }
}
