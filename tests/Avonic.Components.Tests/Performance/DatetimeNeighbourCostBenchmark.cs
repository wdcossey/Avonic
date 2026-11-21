using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Sample.Diagnostics;
using Avonic.Themes;

namespace Avonic.Components.Tests.Performance;

/// <summary>
/// #248, spike B, by hand: what the scrolled calendar costs to open, in the frames after, and at the first press, when its months
/// either side are made at once (as before), only when needed, or a frame after the first. It does nothing unless
/// <c>AVONIC_DATETIME_COST</c> is the absolute path of a file, which gets the table. Headless, the real clock and frames (no
/// <see cref="TestClock"/>), the variants alternated in each round and the first two rounds dropped. Run it in Release; the Pixel is
/// the number, not this.
/// </summary>
public class DatetimeNeighbourCostBenchmark
{
    internal const string Variable = "AVONIC_DATETIME_COST";

    private const int WarmUpRounds = 2;
    private const int Rounds = 10;

    private sealed record Sample(double OpenMs, int Elements, double AfterMs, double LongestAfterMs, double PressMs, int DaysAfter, double[] Frames);

    [AvaloniaFact]
    public void Open_After_And_Press()
    {
        if (Environment.GetEnvironmentVariable(Variable) is not { } file)
        {
            return;
        }

#if DEBUG
        Assert.Fail("Run this in Release: a Debug timing would mean nothing.");
#endif
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        var theme = new AvonicTheme();
        styles.Add(theme);
        var previousNow = AvonicDatetime.Now;
        AvonicDatetime.Now = () => new DateTime(2023, 6, 20, 10, 0, 0);
        try
        {
            using var scene = new Scene(new Border(), 390, 844);
            scene.Window.Background = Brushes.White;
            scene.Window.SetRenderScaling(2);
            Dispatcher.UIThread.RunJobs();
            DatetimeNeighbourMonths[] variants = [DatetimeNeighbourMonths.AtOnce, DatetimeNeighbourMonths.WhenNeeded, DatetimeNeighbourMonths.AfterFirstFrame];
            var samples = new Dictionary<(DatetimeNeighbourMonths, int), List<Sample>>();
            for (var round = 0; round < WarmUpRounds + Rounds; round++)
            {
                foreach (var count in PageCostSet.DatetimeCounts)
                {
                    foreach (var variant in round % 2 == 0 ? variants : variants.Reverse())
                    {
                        var sample = Measure(scene, variant, count);
                        if (round >= WarmUpRounds)
                        {
                            (samples.TryGetValue((variant, count), out var list) ? list : samples[(variant, count)] = []).Add(sample);
                        }
                    }
                }
            }

            var lines = new List<string>
            {
                $"| Variant | Datetimes | Elements | Open, median | Frames after, sum | Longest frame after | First press | Days a calendar, before it |",
                "|---|---:|---:|---:|---:|---:|---:|---:|",
            };
            foreach (var count in PageCostSet.DatetimeCounts)
            {
                foreach (var variant in variants)
                {
                    var list = samples[(variant, count)];
                    lines.Add(string.Create(CultureInfo.InvariantCulture,
                        $"| {variant} | {count} | {list[0].Elements} | {Median(list.Select(s => s.OpenMs)):0.0} ms | {Median(list.Select(s => s.AfterMs)):0.0} ms | {Median(list.Select(s => s.LongestAfterMs)):0.0} ms | {Median(list.Select(s => s.PressMs)):0.0} ms | {list[0].DaysAfter} |"));
                }
            }

            lines.Add("");
            lines.Add("Frames after opening, median of each, ms:");
            foreach (var ((variant, count), list) in samples)
            {
                lines.Add(string.Create(CultureInfo.InvariantCulture,
                    $"{variant} x{count}: {string.Join(" ", Enumerable.Range(0, 16).Select(i => Median(list.Select(s => s.Frames[i])).ToString("0.0", CultureInfo.InvariantCulture)))}"));
            }

            File.AppendAllLines(file, lines);
        }
        finally
        {
            AvonicDatetime.Now = previousNow;
            AvonicDatetime.NeighbourMonthsOverride = null;
            styles.Remove(theme);
            styles.Remove(baseTheme);
        }
    }

    private static Sample Measure(Scene scene, DatetimeNeighbourMonths variant, int count)
    {
        scene.Window.Content = new Border();
        Frame();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        AvonicDatetime.NeighbourMonthsOverride = variant;
        var stopwatch = Stopwatch.StartNew();
        var page = new DatetimeCostView(count);
        scene.Window.Content = page;
        scene.Window.UpdateLayout();
        stopwatch.Stop();
        AvonicDatetime.NeighbourMonthsOverride = null;
        var open = stopwatch.Elapsed.TotalMilliseconds;
        var elements = page.GetVisualDescendants().Count();

        // The frames after: what the deferred months cost, each frame timed on its own.
        double after = 0, longest = 0;
        var frames = new double[40];
        for (var i = 0; i < frames.Length; i++)
        {
            var frame = frames[i] = Frame();
            after += frame;
            longest = Math.Max(longest, frame);
        }

        var daysAfter = page.GetVisualDescendants().OfType<DatetimeCalendarDay>().Count() / count;

        // The first press on the first calendar, and its layout: what a finger pays before its first move.
        var scroller = page.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Content is DatetimeCalendarBodyPanel);
        var finger = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        var at = scroller.TranslatePoint(new Point(scroller.Bounds.Width / 2, scroller.Bounds.Height / 2), scene.Window) ?? default;
        stopwatch.Restart();
        scroller.RaiseEvent(new PointerPressedEventArgs(scroller, finger, scene.Window, at, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        scene.Window.UpdateLayout();
        stopwatch.Stop();
        var press = stopwatch.Elapsed.TotalMilliseconds;
        scroller.RaiseEvent(new PointerReleasedEventArgs(scroller, finger, scene.Window, at, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));

        return new Sample(open, elements, after, longest, press, daysAfter, frames);
    }

    /// <summary>A frame: the render timer's tick (frame callbacks), the jobs it posts, and the layout.</summary>
    private static double Frame()
    {
        var stopwatch = Stopwatch.StartNew();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }
}
