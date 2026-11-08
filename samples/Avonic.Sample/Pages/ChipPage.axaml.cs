using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-chip", Order = 19)]
public partial class ChipPage : UserControl
{
    private int _clicks;

    public ChipPage() => InitializeComponent();

    /// <summary>A filter chip: each click turns it on or off, as an Ionic app's click listener would.</summary>
    private void OnFilterClick(object? sender, RoutedEventArgs e)
    {
        _clicks++;
        FilterChip.Outline = !FilterChip.Outline;
        FilterChip.Color = FilterChip.Outline ? "primary" : null;
        if (FilterChip.Items[1] is AvonicLabel label)
        {
            label.Content = FilterChip.Outline ? "Filter: on" : "Filter: off";
        }

        ClickCount.Text = _clicks == 1 ? "1 click" : $"{_clicks} clicks";
    }
}
