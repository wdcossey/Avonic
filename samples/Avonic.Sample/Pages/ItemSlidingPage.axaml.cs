using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-item-sliding", Order = 31)]
public partial class ItemSlidingPage : UserControl
{
    public ItemSlidingPage()
    {
        InitializeComponent();
        for (var i = 1; i <= 15; i++)
        {
            var option = new AvonicItemOption { Content = "More" };
            option.Click += OnOption;
            var options = new AvonicItemOptions();
            options.Items.Add(option);
            var sliding = new AvonicItemSliding();
            sliding.Items.Add(new AvonicItem { Content = new AvonicLabel { Content = $"Row {i}" } });
            sliding.Items.Add(options);
            More.Items.Add(sliding);
        }
    }

    private void OnDisabledItem(object? sender, RoutedEventArgs e) => Log.Text = "Clicked the disabled row's item";

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
