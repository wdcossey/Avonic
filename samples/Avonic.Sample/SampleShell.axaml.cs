using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Reactive;

namespace Avonic.Sample;

/// <summary>
/// The sample: the page list, a page, and Back. <see cref="MainWindow"/> holds one on desktop, and a single-view lifetime, as on
/// Android, has one as its main view. See docs/infra/android/PLAN.md.
/// </summary>
public partial class SampleShell : UserControl
{
    /// <summary>One entry per Avonic control, marked with <see cref="SamplePageAttribute"/>.</summary>
    private static readonly IReadOnlyList<SamplePageEntry> Pages =
        SamplePageCatalog.Discover(typeof(SampleShell).Assembly);

    private Diagnostics.PerformanceMonitor? _performance;
    private TopLevel? _topLevel;
    private IDisposable? _back;
    private IDisposable? _safeArea;

    private readonly ScrollViewer _index;

    public SampleShell()
    {
        InitializeComponent();
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

        // It scrolls, as a phone may be shorter than the list.
        _index = new ScrollViewer { Content = list };
        ShowIndex();
    }

    /// <summary>Whether a page is shown, rather than the list.</summary>
    public bool IsShowingPage => BackButton.IsVisible;

    /// <summary>Where the device page's benchmark attaches its trees.</summary>
    public Panel BenchmarkArea => BenchmarkHost;

    private void ShowIndex()
    {
        PerformanceLine.IsVisible = false;
        PageTitle.Text = "Avonic";
        BackButton.IsVisible = false;
        Host.Content = _index;
    }

    private void ShowPage(string title, Control page)
    {
        PageTitle.Text = title;
        BackButton.IsVisible = true;
        Host.Content = page;
        ReportTiming(title, page);
    }

    private void OnBack(object? sender, RoutedEventArgs e) => ShowIndex();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        // After every handler on the route, so that an overlay's, which is added when the first one is presented, comes first.
        _back = TopLevel.BackRequestedEvent.RouteFinished.Subscribe(new AnonymousObserver<RoutedEventArgs>(OnBackRequestFinished));
        _safeArea = SafeAreaMode.PadShell(this, Header, _index, PerformanceLine);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _back?.Dispose();
        _back = null;
        _safeArea?.Dispose();
        _safeArea = null;
        _performance?.Dispose();
        _performance = null;
    }

    /// <summary>
    /// What's left of a back request once everything else has seen it: from a page, the list, as an Ionic app's router goes back
    /// at the lowest priority. On the list it's left alone, so that Android leaves the app.
    /// </summary>
    private void OnBackRequestFinished(RoutedEventArgs e)
    {
        // RouteFinished is for every TopLevel's, so only this one's: Avalonia raises it on the TopLevel, and an app may from inside.
        if (e.Handled || !IsShowingPage || _topLevel is null || e.Source is not Visual source || TopLevel.GetTopLevel(source) != _topLevel)
        {
            return;
        }

        e.Handled = true;
        ShowIndex();
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
