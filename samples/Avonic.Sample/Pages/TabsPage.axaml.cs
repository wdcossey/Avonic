using Avalonia.Controls;

namespace Avonic.Sample.Pages;

/// <summary>ion-tabs, ion-tab, ion-tab-bar and ion-tab-button, which are one issue.</summary>
[SamplePage("ion-tabs", Order = 29)]
public partial class TabsPage : UserControl
{
    public TabsPage()
    {
        InitializeComponent();

        Tabs.IonTabsDidChange += (_, e) => Changes.Text = $"IonTabsDidChange: {e.Tab}";
        ToLibrary.Click += (_, _) => Tabs.Select("library");
    }
}
