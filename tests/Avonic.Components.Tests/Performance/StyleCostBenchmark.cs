using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avonic.Components.Controls;
using Avonic.Sample.Diagnostics;
using Avonic.Themes;

namespace Avonic.Components.Tests.Performance;

/// <summary>
/// What the theme costs in time, by hand, at each tier's roll-up: a CI machine's timings mean nothing. It does nothing
/// unless <c>AVONIC_BENCHMARK</c> is the absolute path of a file, which gets a row for the table in
/// docs/infra/performance/README.md. That also leaves Avalonia's diagnostics off, as an app has them, so this has to run
/// on its own. Alternates with and without the theme, because warm-up flatters whichever comes second. Run it in Release.
/// </summary>
public class StyleCostBenchmark
{
    /// <summary>Names the file for the row.</summary>
    internal const string Variable = "AVONIC_BENCHMARK";

    private const int Elements = 5000;
    private const int Depth = 20;
    private const int WarmUpRounds = 4;
    private const int Rounds = 25;

    private static double Attach(Func<Control> make)
    {
        var panel = new StackPanel();
        for (var i = 0; i < Elements; i++)
        {
            panel.Children.Add(make());
        }

        Control root = panel;
        for (var i = 0; i < Depth; i++)
        {
            root = new Border { Child = root };
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var stopwatch = Stopwatch.StartNew();
        using var scene = new Scene(root);
        Dispatcher.UIThread.RunJobs();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static double Median(List<double> values) => values.Order().ElementAt(values.Count / 2);

    [AvaloniaFact]
    public void Attach_Time()
    {
        if (Environment.GetEnvironmentVariable(Variable) is not { } file)
        {
            return;
        }

#if DEBUG
        Assert.Fail("Run this in Release: a Debug timing in the table would mean nothing.");
#endif
        var styles = Application.Current!.Styles;
        var census = StyleCensus.Count(new AvonicTheme());
        var cells = new List<string>();
        foreach (var make in new Func<Control>[] { () => new TextBlock { Text = "x" }, () => new Border() })
        {
            var without = new List<double>();
            var with = new List<double>();
            for (var round = 0; round < WarmUpRounds + Rounds; round++)
            {
                var plain = Attach(make);

                var theme = new AvonicTheme();
                styles.Add(theme);
                double themed;
                try
                {
                    themed = Attach(make);
                }
                finally
                {
                    styles.Remove(theme);
                }

                if (round >= WarmUpRounds)
                {
                    without.Add(plain);
                    with.Add(themed);
                }
            }

            var before = Median(without) * 1000 / Elements;
            var after = Median(with) * 1000 / Elements;
            cells.Add(string.Create(CultureInfo.InvariantCulture, $"{before:0.0} to {after:0.0} ({after - before:+0.0;-0.0})"));
        }

        var controls = typeof(AvonicItem).Assembly.GetExportedTypes().Count(t =>
            t.Namespace == typeof(AvonicItem).Namespace && t.Name.StartsWith("Avonic", StringComparison.Ordinal) && t.IsSubclassOf(typeof(StyledElement)));
        var when = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        File.AppendAllText(file, $"| {when} | {controls} | {census.Rules} | {census.Alternatives} | {cells[0]} | {cells[1]} |{Environment.NewLine}");
    }
}
