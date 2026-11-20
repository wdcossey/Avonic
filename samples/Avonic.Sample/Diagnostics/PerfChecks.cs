using System.Diagnostics;
using System.Globalization;
using Avalonia.Threading;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// What the page-cost rounds check after the month changes (#248), so that a run on a device says when the measuring has left
/// something behind, or the app has stopped getting idle time: frame callbacks still asked for, the timed page still alive, and
/// how long a <see cref="DispatcherPriority.Background"/> post waits. A Background post waits for idle time: on Android only the
/// looper's idle handler runs it, so a run whose posts wait reads every later page as slow, and its calendar never changes
/// month, whose scroll's rest is a Background timer. docs/infra/performance/README.md, "Page costs on a device".
/// </summary>
public static class PerfChecks
{
    /// <summary>A Background post waiting longer than this is a warning.</summary>
    public static readonly TimeSpan SlowBackground = TimeSpan.FromMilliseconds(100);

    /// <summary>How long a <see cref="DispatcherPriority.Background"/> post waits to run, in ms; null if it hasn't by <paramref name="limit"/>.</summary>
    public static async Task<double?> BackgroundPostMs(TimeSpan limit)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var ran = await Dispatcher.UIThread.InvokeAsync(Stopwatch.GetTimestamp, DispatcherPriority.Background).GetTask().WaitAsync(limit);
            return Stopwatch.GetElapsedTime(started, ran).TotalMilliseconds;
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether what <paramref name="page"/> refers to has been collected, after full collections. A short weak reference is cleared
    /// once its target can't be reached, before any finalizer runs, so this waits for none, which could need the UI thread.
    /// </summary>
    public static bool Collected(WeakReference page)
    {
        for (var i = 0; i < 2 && page.IsAlive; i++)
        {
            GC.Collect();
        }

        return !page.IsAlive;
    }

    /// <summary>
    /// <c>datetime-month: round 1, check: 0 frames asked for, page collected, background post 0.4 ms before and 0.6 ms after</c>,
    /// with the platform's state, then a warning line for each thing wrong.
    /// </summary>
    public static IEnumerable<string> Lines(string label, int round, int pendingFrames, bool collected, double? before, double? after, string? platform)
    {
        static string Ms(double? ms) => ms is { } v ? string.Create(CultureInfo.InvariantCulture, $"{v:0.0} ms") : "over 10 s";

        yield return $"{label}: round {round}, check: {pendingFrames} frames asked for, page {(collected ? "collected" : "still alive")}, background post {Ms(before)} before and {Ms(after)} after{(platform is { Length: > 0 } ? ", " + platform : "")}";
        if (pendingFrames != 0)
        {
            yield return $"{label}: round {round}, warning: {pendingFrames} frame callbacks still asked for after the timing: a frame loop is still running, and the app draws continuously";
        }

        if (!collected)
        {
            yield return $"{label}: round {round}, warning: the timed page is still alive after it was left and a full collection: something still holds it";
        }

        if (after is not { } a || a > SlowBackground.TotalMilliseconds)
        {
            yield return $"{label}: round {round}, warning: a Background post waited {Ms(after)} after the timing (before it: {Ms(before)}): the app gets no idle time, and later pages read slow";
        }
    }
}
