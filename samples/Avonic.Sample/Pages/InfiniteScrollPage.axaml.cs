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

    private IDisposable? _load;

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
        _load = DispatcherTimer.RunOnce(() =>
        {
            _load = null;
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

    // A load that's under way is dropped and completed first: disabled while loading, an infinite scroll never fires again,
    // as in Ionic.
    private void CancelLoad()
    {
        _load?.Dispose();
        _load = null;
        Infinite.Complete();
    }

    private void OnDisabled(object? sender, RoutedEventArgs e)
    {
        CancelLoad();
        Infinite.IsEnabled = Disabled.IsChecked != true;
    }

    private void OnReset(object? sender, RoutedEventArgs e)
    {
        CancelLoad();
        Rows.Children.Clear();
        AddRows();
        Infinite.IsEnabled = true;
        Disabled.IsChecked = false;
    }
}
