using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avonic.Sample.Pages;

[SamplePage("ion-toolbar", Order = 12)]
public partial class ToolbarPage : UserControl
{
    private int _clicks;

    public ToolbarPage() => InitializeComponent();

    private void OnEditClick(object? sender, RoutedEventArgs e) =>
        Status.Content = $"Edit tapped {++_clicks} time{(_clicks == 1 ? "" : "s")}";
}
