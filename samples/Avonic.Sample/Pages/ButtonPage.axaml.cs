using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avonic.Sample.Pages;

[SamplePage("ion-button", Order = 10)]
public partial class ButtonPage : UserControl
{
    private int _clicks;

    public ButtonPage() => InitializeComponent();

    private void OnClick(object? sender, RoutedEventArgs e) =>
        ClickCount.Text = $"Clicks: {++_clicks}";
}
