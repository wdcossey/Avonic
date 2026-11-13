using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avonic.Sample.Diagnostics;

namespace Avonic.Components.Tests.Performance;

/// <summary>The line along the bottom of the sample in a Debug build.</summary>
public class PerformanceMonitorTests
{
    [Fact]
    public void The_Line_Has_Each_Number()
    {
        var line = PerformanceMonitor.Format("ion-card", 41.4, 214, 98, 174, 2.14, 3.44);

        Assert.Equal("ion-card: 41 ms, 214 elements, 98 rules (174), layout 2.1, render 3.4", line);
    }

    [AvaloniaFact]
    public void It_Reports_A_Page_Once_It_Is_In() => InstalledTheme.Run(_ =>
    {
        using var monitor = new PerformanceMonitor();
        var host = new ContentControl();
        using var scene = new Scene(host);
        var page = new StackPanel { Children = { new TextBlock(), new TextBlock() } };
        string? line = null;

        monitor.Begin();
        host.Content = page;
        monitor.Report("a page", page, text => line = text);
        Assert.Null(line);
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(line);
        Assert.StartsWith("a page: ", line, StringComparison.Ordinal);
        Assert.Contains("3 elements", line, StringComparison.Ordinal);
    });

    // Styles apply as the page is attached, which is before it can be reported.
    [AvaloniaFact]
    public void The_Time_Includes_Making_The_Page()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        string? line = null;

        monitor.Begin();
        Thread.Sleep(60);
        monitor.Report("a page", page, text => line = text);
        Dispatcher.UIThread.RunJobs();

        var opened = int.Parse(System.Text.RegularExpressions.Regex.Match(line!, "a page: ([0-9]+) ms").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(opened >= 50, line);
    }

    // Against a rename in Avalonia: the line would read zero for ever otherwise.
    [AvaloniaTheory]
    [InlineData(PerformanceMonitor.LayoutMeasure)]
    [InlineData(PerformanceMonitor.LayoutArrange)]
    [InlineData(PerformanceMonitor.Render)]
    public void Avalonias_Meter_Still_Has(string instrument)
    {
        using var scene = new Scene(new Panel());
        var published = new List<string>();
        using var listener = new System.Diagnostics.Metrics.MeterListener();
        listener.InstrumentPublished = (found, _) =>
        {
            if (found.Meter.Name == "Avalonia.Diagnostic.Meter")
            {
                published.Add(found.Name);
            }
        };
        listener.Start();

        Assert.Contains(instrument, published);
    }

    // Not render: its pass can come after the line is read. See the known limits in docs/infra/performance/README.md.
    [AvaloniaFact]
    public void Layout_Comes_From_Avalonias_Own_Meter()
    {
        using var monitor = new PerformanceMonitor();
        var host = new ContentControl();
        using var scene = new Scene(host);
        var page = new StackPanel();
        for (var i = 0; i < 200; i++)
        {
            page.Children.Add(new TextBlock { Text = "x" });
        }

        string? line = null;
        monitor.Begin();
        host.Content = page;
        monitor.Report("a page", page, text => line = text);
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain("layout 0.0", line!, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_Page_That_Has_Gone_Is_Not_Reported()
    {
        using var monitor = new PerformanceMonitor();
        var host = new ContentControl();
        using var scene = new Scene(host);
        var page = new Panel();
        host.Content = page;
        string? line = null;

        monitor.Begin();
        monitor.Report("a page", page, text => line = text);
        host.Content = null;
        Dispatcher.UIThread.RunJobs();

        Assert.Null(line);
    }

    // Through the sample's own window, so that this covers the wiring: a Debug build shows it, and a Release build doesn't.
    [AvaloniaFact]
    public void The_Sample_Shows_The_Line_Only_In_A_Debug_Build_And_Not_On_The_Index() => InstalledTheme.Run(_ =>
    {
        var window = new Avonic.Sample.MainWindow();
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var shell = window.FindControl<Avonic.Sample.SampleShell>("Shell")!;
            var line = shell.FindControl<TextBlock>("PerformanceLine")!;
            var index = (StackPanel)((ScrollViewer)shell.FindControl<ContentControl>("Host")!.Content!).Content!;
            var open = index.Children.OfType<Button>().First(b => (string?)b.Content == "ion-card");
            Assert.False(line.IsVisible);

            open.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

#if DEBUG
            Assert.True(line.IsVisible);
            Assert.StartsWith("ion-card: ", line.Text, StringComparison.Ordinal);
#else
            Assert.False(line.IsVisible);
#endif

            var back = shell.FindControl<Button>("BackButton")!;
            back.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.False(line.IsVisible);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    });

    // Large enough that what Avalonia itself reports meanwhile can't change the leading digits.
    [AvaloniaFact]
    public void Measure_And_Arrange_Are_Layout_And_Render_Is_Separate()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        string? line = null;

        monitor.Begin();
        monitor.Report("a page", page, text => line = text);
        monitor.Record(PerformanceMonitor.LayoutMeasure, 10000);
        monitor.Record(PerformanceMonitor.LayoutArrange, 20000);
        monitor.Record(PerformanceMonitor.Render, 50000);
        Dispatcher.UIThread.RunJobs();

        Assert.Matches("layout 300[0-9][0-9][.][0-9], render 500[0-9][0-9][.][0-9]$", line!);
    }

    [AvaloniaFact]
    public void Times_Start_Again_With_Each_Page()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        monitor.Record(PerformanceMonitor.LayoutMeasure, 90000);
        monitor.Record(PerformanceMonitor.Render, 90000);
        string? line = null;

        monitor.Begin();
        monitor.Report("a page", page, text => line = text);
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain("9000", line!, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void It_Counts_The_Theme_Rules() => InstalledTheme.Run(theme =>
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        var (rules, alternatives) = StyleCensus.Count(theme);
        string? line = null;

        monitor.Begin();
        monitor.Report("a page", page, text => line = text);
        Dispatcher.UIThread.RunJobs();

        Assert.True(rules > 0 && alternatives >= rules);
        Assert.Contains($"{rules} rules ({alternatives})", line!, StringComparison.Ordinal);
    });

    // The rules of the page and of each of its ancestors are checked against it too; a rule further down isn't.
    [AvaloniaFact]
    public void It_Counts_The_Rules_Of_The_Application_The_Window_And_The_Page()
    {
        static Style Rule(Func<Selector?, Selector> selector)
        {
            var rule = new Style(selector);
            rule.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
            return rule;
        }

        var (appRules, appAlternatives) = StyleCensus.Count(Application.Current!.Styles);
        var appRule = Rule(x => x.OfType<Border>());
        using var monitor = new PerformanceMonitor();
        var below = new Panel { Styles = { Rule(x => x.OfType<Canvas>()) } };
        var page = new Panel { Styles = { Rule(x => x.OfType<TextBox>()) }, Children = { below } };
        using var scene = Scene.Styled(page, Rule(x => Selectors.Or(x.OfType<Image>(), x.OfType<Grid>())));
        Application.Current.Styles.Add(appRule);
        string? line = null;
        try
        {
            monitor.Begin();
            monitor.Report("a page", page, text => line = text);
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            Application.Current.Styles.Remove(appRule);
        }

        Assert.Contains($"{appRules + 3} rules ({appAlternatives + 4})", line!, StringComparison.Ordinal);
    }

    // Avalonia reads the switch once, at start, so turning it off now changes only what the monitor reads.
    [AvaloniaFact]
    public void Without_Avalonias_Diagnostics_The_Line_Says_So()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        string? line = null;
        AppContext.SetSwitch(PerformanceMonitor.DiagnosticsSwitch, false);
        try
        {
            monitor.Begin();
            monitor.Report("a page", page, text => line = text);
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            AppContext.SetSwitch(PerformanceMonitor.DiagnosticsSwitch, true);
        }

        Assert.Equal("a page: Avalonia's diagnostics are off. Call PerformanceMonitor.EnableDiagnostics() before Avalonia starts.", line);
    }

    // Each monitor hears every measurement in the process until it's disposed.
    [AvaloniaFact]
    public void A_Monitor_Listens_Until_It_Is_Disposed_And_Disposing_Twice_Counts_Once()
    {
        var before = PerformanceMonitor.Listening;
        var monitor = new PerformanceMonitor();
        Assert.Equal(before + 1, PerformanceMonitor.Listening);

        monitor.Dispose();
        monitor.Dispose();

        Assert.Equal(before, PerformanceMonitor.Listening);
    }
}
