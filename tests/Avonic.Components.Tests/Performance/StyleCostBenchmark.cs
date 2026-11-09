using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
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

    // The shape is StyleCost's, which the sample's runner on a phone shares.
    private static double Attach(Func<Control> make)
    {
        var root = StyleCost.Tree(make);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var stopwatch = Stopwatch.StartNew();
        using var scene = new Scene(root);
        Dispatcher.UIThread.RunJobs();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

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
        foreach (var make in StyleCost.Kinds)
        {
            var without = new List<double>();
            var with = new List<double>();
            for (var round = 0; round < StyleCost.WarmUpRounds + StyleCost.Rounds; round++)
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

                if (round >= StyleCost.WarmUpRounds)
                {
                    without.Add(plain);
                    with.Add(themed);
                }
            }

            cells.Add(StyleCost.Cell(without, with));
        }

        File.AppendAllText(file, StyleCost.Row(DateTime.Now, census, cells) + Environment.NewLine);
    }
}
