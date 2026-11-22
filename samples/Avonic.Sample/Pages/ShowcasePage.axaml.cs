using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

/// <summary>A small app built only from Avonic controls, first on the index: the screens the README shows.</summary>
[SamplePage("showcase", Order = 0)]
public partial class ShowcasePage : UserControl
{
    /// <summary>The tabs, in the tab bar's order: each is one screen.</summary>
    public static IReadOnlyList<string> Screens { get; } = ["inbox", "discover", "profile", "settings"];

    /// <summary>
    /// What the inbox opens, pictured as screens of their own: <c>menu</c>, the side menu open over it, and <c>message</c>, a
    /// message pushed on its nav, with the back button.
    /// </summary>
    public static IReadOnlyList<string> InboxScreens { get; } = ["menu", "message"];

    public ShowcasePage() => InitializeComponent();

    /// <summary>The tabs, so that a screen can be selected from code.</summary>
    public AvonicTabs ScreenTabs => Tabs;

    /// <summary>The side menu, which the inbox's menu button opens.</summary>
    public AvonicMenu Menu => SideMenu;

    /// <summary>The inbox tab's nav, whose root is the inbox.</summary>
    public AvonicNav Inbox => InboxNav;
}
