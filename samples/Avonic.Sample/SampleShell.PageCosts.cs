#if AVONIC_PERF
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Sample.Diagnostics;

namespace Avonic.Sample;

/// <summary>
/// A build with <c>-p:AvonicPerf=true</c> (with <c>-c Release</c>, the build an app ships) opens every page by itself, a few rounds,
/// and writes each page's cost line, prefixed <c>AVONIC-PERF</c>, to the console: logcat's <c>DOTNET</c> tag on Android. The pages
/// are <see cref="PageCostSet"/>'s: the sample's, and perf-only pages of 1, 2 and 4 datetimes, on the first of which the month
/// changes are timed too (<see cref="MonthChangeTiming"/>). With <c>AVONIC_PERF_ONLY</c> set (on Android, the intent extra of that
/// name, which the head copies into <see cref="PageCostsOnly"/>) only the pages it names. With <c>AVONIC_PERF_OUT</c> set, it also
/// appends the lines to that file, and with <c>AVONIC_PERF_EXIT=1</c> a desktop app closes after.
/// docs/infra/performance/README.md, "Page costs on a device".
/// </summary>
public partial class SampleShell
{
    private const int PageCostRounds = 3;

    /// <summary>Changes of month timed each round, forward and then back.</summary>
    private const int MonthChanges = 10;

    private const string MonthChangeLabel = "datetime-month";

    private static bool _pageCostsStarted;
    private TaskCompletionSource<string>? _pendingReport;

    /// <summary>
    /// Which pages the rounds open (<see cref="PageCostSet.Select"/>): <c>AVONIC_PERF_ONLY</c>, or what an Android head sets from
    /// its intent before the shell starts. Null or empty is every page.
    /// </summary>
    public static string? PageCostsOnly { get; set; } = Environment.GetEnvironmentVariable(PageCostSet.OnlyVariable);

    /// <summary>
    /// What a head knows of its platform's state that can slow the app, logged at the start and in each round's check: on Android,
    /// whether accessibility is on and which services are, as Avalonia's accessibility bridge answers them on the UI thread.
    /// </summary>
    public static Func<string>? PageCostsPlatform { get; set; }

    partial void Reported(string line)
    {
        Write(line);
        _pendingReport?.TrySetResult(line);
    }

    partial void StartPageCosts()
    {
        if (_pageCostsStarted)
        {
            return;
        }

        _pageCostsStarted = true;
        Dispatcher.UIThread.Post(() => _ = RunPageCosts(), DispatcherPriority.Background);
    }

    private async Task RunPageCosts()
    {
        Write(string.Create(CultureInfo.InvariantCulture,
            $"start: {RuntimeInformation.OSDescription}, {RuntimeInformation.ProcessArchitecture}, {Environment.ProcessorCount} cores, {RuntimeInformation.FrameworkDescription}"));
        var pages = PageCostSet.Select(Pages, PageCostsOnly);
        if (!string.IsNullOrWhiteSpace(PageCostsOnly))
        {
            Write($"only: {PageCostsOnly}: {string.Join(", ", pages.Select(p => p.Title))}");
        }

        if (pages.Count == 0)
        {
            Write("no page matches");
        }

        if (Platform() is { Length: > 0 } platform)
        {
            Write($"platform: {platform}");
        }

        // Past the first frames and the start-up work.
        await Task.Delay(TimeSpan.FromSeconds(3));
        for (var round = 1; round <= PageCostRounds && pages.Count > 0; round++)
        {
            foreach (var (title, create) in pages)
            {
                _pendingReport = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                Control? page;
                try
                {
                    BeginTiming();
                    page = create();
                    ShowPage(title, page);
                }
                catch (Exception ex)
                {
                    Write($"{title}: failed to open: {ex.GetType().Name}: {ex.Message}");
                    ShowIndex();
                    continue;
                }

                if (await Task.WhenAny(_pendingReport.Task, Task.Delay(TimeSpan.FromSeconds(30))) != _pendingReport.Task)
                {
                    Write($"{title}: no report in 30 s");
                }

                // Let it draw, then back to the list, as a user would go.
                await Task.Delay(400);
                if (title != PageCostSet.MonthChangePage)
                {
                    page = null;
                    ShowIndex();
                    await Task.Delay(400);
                    continue;
                }

                // The month changes, then what they may have left behind, once their page is gone: a frame loop, the page itself,
                // or no idle time.
                var before = await PerfChecks.BackgroundPostMs(TimeSpan.FromSeconds(10));
                await TimeMonthChanges(page, round);
                var timed = new WeakReference(page);
                page = null;
                ShowIndex();
                await Task.Delay(400);
                var after = await PerfChecks.BackgroundPostMs(TimeSpan.FromSeconds(10));
                foreach (var line in PerfChecks.Lines(MonthChangeLabel, round, MonthChangeTiming.PendingFrames, PerfChecks.Collected(timed), before, after, Platform()))
                {
                    Write(line);
                }
            }

            Write($"round {round} done");
        }

        _pendingReport = null;
        Write("done");
        if (Environment.GetEnvironmentVariable("AVONIC_PERF_EXIT") == "1"
            && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    /// <summary>Ten months forward and ten back on the page's datetime, each change a line, then the round's.</summary>
    private static async Task TimeMonthChanges(Control page, int round)
    {
        try
        {
            var datetime = page.GetVisualDescendants().OfType<AvonicDatetime>().First();
            var changes = await new MonthChangeTiming(datetime).Run(MonthChanges, MonthChanges, change => Write(change.Line(MonthChangeLabel, round)));
            Write(MonthChangeTiming.Summary(MonthChangeLabel, round, changes));
        }
        catch (Exception ex)
        {
            Write($"{MonthChangeLabel}: round {round}, failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string? Platform()
    {
        try
        {
            return PageCostsPlatform?.Invoke();
        }
        catch (Exception ex)
        {
            return $"platform unknown: {ex.GetType().Name}";
        }
    }

    private static void Write(string line)
    {
        Console.WriteLine("AVONIC-PERF " + line);
        if (Environment.GetEnvironmentVariable("AVONIC_PERF_OUT") is { Length: > 0 } path)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}
#endif
