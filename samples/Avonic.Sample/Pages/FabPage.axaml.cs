using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avonic.Sample.Pages;

/// <summary>ion-fab, ion-fab-button and ion-fab-list, which are one issue.</summary>
[SamplePage("ion-fab", Order = 30)]
public partial class FabPage : UserControl
{
    public FabPage() => InitializeComponent();

    private void OnAction(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string name })
        {
            Status.Text = $"Tapped {name}";
        }
    }
}
