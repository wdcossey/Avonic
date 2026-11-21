using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-split-pane", Order = 78)]
public partial class SplitPanePage : UserControl
{
    public SplitPanePage()
    {
        InitializeComponent();
        Pane.IonSplitPaneVisible += (_, e) => Status.Text = e.Visible ? "Side pane shown" : "Side pane hidden: open the menu with the button";
        WhenGroup.IonChange += (_, e) => Pane.When = e.Value as string;
        DisabledToggle.IonChange += (_, e) => Pane.Disabled = e.Checked;
        SideInbox.Click += (_, _) => Status.Text = "Inbox picked";
    }
}
