using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avonic.Sample;

public partial class MainWindow : Window
{
    /// <summary>One entry per Avonic control, marked with <see cref="SamplePageAttribute"/>.</summary>
    private static readonly IReadOnlyList<SamplePageEntry> Pages =
        SamplePageCatalog.Discover(typeof(MainWindow).Assembly);

    private Diagnostics.PerformanceMonitor? _performance;

    public MainWindow()
    {
        InitializeComponent();
        ShowIndex();
    }

    private void ShowIndex()
    {
        var list = new StackPanel { Margin = new(16) };
        foreach (var (title, create) in Pages)
        {
            var button = new Button { Content = title, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, Margin = new(0, 0, 0, 8) };
            button.Click += (_, _) =>
            {
                BeginTiming();
                ShowPage(title, create());
            };
            list.Children.Add(button);
        }

        PerformanceLine.IsVisible = false;
        PageTitle.Text = "Avonic";
        BackButton.IsVisible = false;
        Host.Content = list;
    }

    private void ShowPage(string title, Control page)
    {
        PageTitle.Text = title;
        BackButton.IsVisible = true;
        Host.Content = new ScrollViewer { Content = page };
        ReportTiming(title, page);
    }

    private void OnBack(object? sender, RoutedEventArgs e) => ShowIndex();

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _performance?.Dispose();
    }

    // Only called in a Debug build. A Release build still compiles them, so CI sees them.
    [Conditional("DEBUG")]
    private void BeginTiming() => (_performance ??= new Diagnostics.PerformanceMonitor()).Begin();

    [Conditional("DEBUG")]
    private void ReportTiming(string title, Control page) => _performance?.Report(title, page, line =>
    {
        PerformanceLine.Text = line;
        PerformanceLine.IsVisible = true;
    });
}
