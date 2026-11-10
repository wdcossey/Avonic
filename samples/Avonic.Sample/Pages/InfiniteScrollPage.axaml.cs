using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-infinite-scroll", Order = 36)]
public partial class InfiniteScrollPage : UserControl
{
    private const int Batch = 20;
    private const int Max = 100;

    public InfiniteScrollPage()
    {
        InitializeComponent();
        AddRows();
    }

    private void AddRows()
    {
        var start = Rows.Children.Count;
        for (var i = 1; i <= Batch; i++)
        {
            Rows.Children.Add(new AvonicItem { Content = new AvonicLabel { Content = $"Item {start + i}" } });
        }

        Status.Text = string.Create(CultureInfo.InvariantCulture, $"{Rows.Children.Count} items");
    }

    private void OnInfinite(object? sender, RoutedEventArgs e)
    {
        Status.Text = "Loading…";
        DispatcherTimer.RunOnce(() =>
        {
            AddRows();
            Infinite.Complete();
            if (Rows.Children.Count >= Max)
            {
                Infinite.IsEnabled = false;
                Disabled.IsChecked = true;
                Status.Text = string.Create(CultureInfo.InvariantCulture, $"{Rows.Children.Count} items: no more, so it's disabled");
            }
        }, TimeSpan.FromMilliseconds(500));
    }

    private void OnDisabled(object? sender, RoutedEventArgs e) => Infinite.IsEnabled = Disabled.IsChecked != true;

    private void OnReset(object? sender, RoutedEventArgs e)
    {
        Rows.Children.Clear();
        AddRows();
        Infinite.IsEnabled = true;
        Disabled.IsChecked = false;
    }
}
