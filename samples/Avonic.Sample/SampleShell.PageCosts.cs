#if AVONIC_PERF
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace Avonic.Sample;

/// <summary>
/// A build with <c>-p:AvonicPerf=true</c> (with <c>-c Release</c>, the build an app ships) opens every page by itself, a few rounds,
/// and writes each page's cost line, prefixed <c>AVONIC-PERF</c>, to the console: logcat's <c>DOTNET</c> tag on Android. With
/// <c>AVONIC_PERF_OUT</c> set, it also appends them to that file, and with <c>AVONIC_PERF_EXIT=1</c> a desktop app closes after.
/// docs/infra/performance/README.md, "Page costs on a device".
/// </summary>
public partial class SampleShell
{
    private const int PageCostRounds = 3;
    private static bool _pageCostsStarted;
    private TaskCompletionSource<string>? _pendingReport;

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
        // Past the first frames and the start-up work.
        await Task.Delay(TimeSpan.FromSeconds(3));
        for (var round = 1; round <= PageCostRounds; round++)
        {
            foreach (var (title, create) in Pages)
            {
                // The device page's benchmark is measured on its own.
                if (title == "device")
                {
                    continue;
                }

                _pendingReport = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    BeginTiming();
                    ShowPage(title, create());
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
                ShowIndex();
                await Task.Delay(400);
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
