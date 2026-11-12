using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-item-sliding", Order = 31)]
public partial class ItemSlidingPage : UserControl
{
    public ItemSlidingPage() => InitializeComponent();

    private void OnOption(object? sender, RoutedEventArgs e)
    {
        if (sender is AvonicItemOption option)
        {
            Log.Text = $"Clicked {option.Content}";
        }
    }

    private void OnSwipe(object? sender, ItemOptionsSwipeEventArgs e) => Log.Text = $"Full swipe to the {e.Side} options";

    private void OnOpenStart(object? sender, RoutedEventArgs e) => Methods.Open(ItemOptionsSide.Start);

    private void OnOpenEnd(object? sender, RoutedEventArgs e) => Methods.Open(ItemOptionsSide.End);

    private void OnCloseOpened(object? sender, RoutedEventArgs e) =>
        Log.Text = Methods.CloseOpened() ? "Closed the open item" : "None was open";
}
