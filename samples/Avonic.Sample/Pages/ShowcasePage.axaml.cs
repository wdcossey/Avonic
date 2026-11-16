using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

/// <summary>A small app built only from Avonic controls, first on the index: the screens the README shows.</summary>
[SamplePage("showcase", Order = 0)]
public partial class ShowcasePage : UserControl
{
    /// <summary>The tabs, in the tab bar's order: each is one screen.</summary>
    public static IReadOnlyList<string> Screens { get; } = ["inbox", "discover", "profile", "settings"];

    public ShowcasePage() => InitializeComponent();

    /// <summary>The tabs, so that a screen can be selected from code.</summary>
    public AvonicTabs ScreenTabs => Tabs;
}
