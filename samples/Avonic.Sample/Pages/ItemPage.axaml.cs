using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avonic.Sample.Pages;

[SamplePage("ion-item", Order = 6)]
public partial class ItemPage : UserControl
{
    private int _clicks;

    public ItemPage() => InitializeComponent();

    private void OnItemClick(object? sender, RoutedEventArgs e) =>
        ClickCount.Text = $"Clicks: {++_clicks}";
}
