using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>The numbers are from real ion-item-group in Chromium, at 390 px wide. See docs/components/ion-item-group/PLAN.md.</summary>
public class AvonicItemGroupTests
{
    private const string Long = "Very long text that certainly must overflow the phone viewport because it just goes on and on and on and on and on";

    private static readonly Color Danger = Color.FromRgb(197, 0, 15);
    private static readonly Color Token = Color.FromRgb(18, 52, 86);

    private static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static void Run() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void G1_It_Stacks_What_Is_In_It_With_Nothing_Of_Its_Own() => InstalledTheme.Run(_ =>
    {
        var divider = new AvonicItemDivider();
        var item = new AvonicItem();
        var group = new AvonicItemGroup { Items = { divider, item } };
        using var scene = new Scene(new StackPanel { Children = { group } });

        // Measured fg, without the list: 31 and 48.
        Assert.Equal(new Rect(0, 0, 390, 79), group.Bounds);
        Assert.Equal(31, item.TranslatePoint(default, group)!.Value.Y);
    });

    [AvaloniaFact]
    public void G1_Empty_It_Has_No_Height() => InstalledTheme.Run(_ =>
    {
        var group = new AvonicItemGroup();
        using var scene = new Scene(new StackPanel { Children = { group } });

        Assert.Equal(0, group.Bounds.Height);
        Assert.Equal(390, group.Bounds.Width);
    });

    [AvaloniaFact]
    public void G1_It_Works_On_The_Stub_And_From_A_Template()
    {
        var group = new AvonicItemGroup
        {
            ItemsSource = new[] { "a", "b" },
            ItemTemplate = new FuncDataTemplate<string>((text, _) => new TextBlock { Text = text }),
        };
        using var scene = new Scene(group);

        Assert.Equal(["a", "b"], group.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text));
    }

    [AvaloniaFact]
    public void G1_It_Takes_Another_Panel()
    {
        var group = new AvonicItemGroup
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new WrapPanel()),
            Items = { new TextBlock { Text = "x" } },
        };
        using var scene = new Scene(group);

        Assert.Single(group.GetVisualDescendants().OfType<WrapPanel>());
    }

    [AvaloniaFact]
    public void G2_It_Reads_As_A_Group() => InstalledTheme.Run(_ =>
    {
        var group = new AvonicItemGroup();
        using var scene = new Scene(group);

        var peer = ControlAutomationPeer.CreatePeerForElement(group);
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.True(peer.IsControlElement());
    });

    [AvaloniaFact]
    public void G3_A_Label_In_A_Group_Acts_As_It_Does_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicLabel { Content = "x" };
        var danger = new AvonicLabel { Content = "x", Color = "danger" };
        var inHeader = new AvonicLabel { Content = "x" };
        using var scene = new Scene(new AvonicItemGroup
        {
            Items = { plain, danger, new AvonicListHeader { Content = inHeader } },
        });

        Assert.Equal(new Thickness(0), plain.Margin);
        Assert.Equal(Danger, Solid(TextElement.GetForeground(danger)));

        // Measured gl2 and ghl: the token is read, even in a header, and a label's own colour wins.
        scene.Window.Resources["Avonic.Label.Color"] = new SolidColorBrush(Token);
        Run();
        Assert.Equal(Token, Solid(TextElement.GetForeground(plain)));
        Assert.Equal(Token, Solid(TextElement.GetForeground(inHeader)));
        Assert.Equal(Danger, Solid(TextElement.GetForeground(danger)));
    });

    [AvaloniaFact]
    public void G3_A_Label_Inherits_And_Uses_The_Font_Token() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var group = new AvonicItemGroup { Items = { label } };
        TextElement.SetForeground(group, new SolidColorBrush(Token));
        using var scene = new Scene(group);
        scene.Window.Resources["Avonic.FontFamily"] = new FontFamily("Courier New");
        Run();

        Assert.Equal(Token, Solid(TextElement.GetForeground(label)));
        Assert.Equal("Courier New", label.FontFamily.Name);
    });

    [AvaloniaFact]
    public void G3_A_Nowrap_Label_Gets_An_Ellipsis_And_Clips() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = Long, Classes = { "ion-text-nowrap" } };
        using var scene = new Scene(new AvonicItemGroup { Items = { label } });

        var text = label.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(TextTrimming.CharacterEllipsis, text.TextTrimming);
        Assert.True(label.ClipToBounds);
    });

    [AvaloniaFact]
    public void G4_Every_Button_Item_Is_A_Tab_Stop_And_Arrows_Do_Not_Move_Focus() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicItem { Button = true };
        var second = new AvonicItem { Button = true };
        using var scene = new Scene(new AvonicItemGroup { Items = { first, second } });

        scene.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Run();
        Assert.True(first.IsFocused);

        scene.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        Run();
        Assert.True(first.IsFocused);

        scene.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Run();
        Assert.True(second.IsFocused);
    });
}
