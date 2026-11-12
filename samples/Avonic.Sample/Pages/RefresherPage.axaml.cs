using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-refresher", Order = 35)]
public partial class RefresherPage : UserControl
{
    private int _count;
    private IDisposable? _load;

    public RefresherPage()
    {
        InitializeComponent();
        for (var i = 0; i < 20; i++)
        {
            AddRow();
        }
    }

    private void AddRow()
    {
        _count++;
        Rows.Items.Insert(0, new AvonicItem { Content = new AvonicLabel { Content = string.Create(CultureInfo.InvariantCulture, $"Item {_count}") } });
    }

    private void OnRefresh(object? sender, RefresherEventArgs e)
    {
        Status.Text = "Refreshing…";
        _load = DispatcherTimer.RunOnce(() =>
        {
            _load = null;
            AddRow();
            e.Complete();
        }, TimeSpan.FromSeconds(2));
    }

    private void OnPullEnd(object? sender, RefresherPullEndEventArgs e) =>
        Status.Text = e.Reason == RefresherPullEndReason.Complete ? "Refreshed: a new row at the top" : "Cancelled";

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        _load?.Dispose();
        _load = null;
        Refresher.Cancel();
    }

    private void OnDisabled(object? sender, RoutedEventArgs e) => Refresher.IsEnabled = Disabled.IsChecked != true;
}
