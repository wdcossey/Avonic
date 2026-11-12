using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-searchbar", Order = 26)]
public partial class SearchbarPage : UserControl
{
    public SearchbarPage() => InitializeComponent();

    private void Log(string line) => LogText.Text = line + "\n" + LogText.Text;

    private void OnLoggedInput(object? sender, SearchbarInputEventArgs e) => Log($"ionInput: {e.Value}");

    private void OnLoggedChange(object? sender, SearchbarChangeEventArgs e) => Log($"ionChange: {e.Value}");

    private void OnLoggedClear(object? sender, RoutedEventArgs e) => Log("ionClear");

    private void OnLoggedCancel(object? sender, RoutedEventArgs e) => Log("ionCancel");
}
