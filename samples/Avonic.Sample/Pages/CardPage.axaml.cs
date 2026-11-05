using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avonic.Sample.Pages;

[SamplePage("ion-card", Order = 13)]
public partial class CardPage : UserControl
{
    private int _clicks;

    public CardPage() => InitializeComponent();

    private void OnCardClick(object? sender, RoutedEventArgs e) =>
        ClickCount.Text = $"Clicks: {++_clicks}";
}
