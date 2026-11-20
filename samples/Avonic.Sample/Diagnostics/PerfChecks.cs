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
    /// <summary>
    /// Linux's <c>/proc/self/cpuset</c>: on Android, <c>/top-app</c> while the app is in front, and <c>/background</c> (the little
    /// cores) once the screen locks or the app goes behind. Null where there's no such file.
    /// </summary>
    public static string? Cpuset()
    {
        try
        {
            return File.Exists("/proc/self/cpuset") ? File.ReadAllText("/proc/self/cpuset").Trim() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="Collected"/>, tried again over the next few hundred milliseconds, each from a fresh turn of the dispatcher. On
    /// Android the runtime is Mono, whose collector scans each thread's stack conservatively: a reference left in a stack slot the
    /// thread hasn't reused yet keeps the page alive for a while, though nothing holds it. Alive after every try is alive.
    /// </summary>
    public static async Task<bool> CollectedAsync(WeakReference page)
    {
        for (var i = 0; i < 4; i++)
        {
            if (Collected(page))
            {
                return true;
            }

            await Task.Delay(100);
        }

        return Collected(page);
    }

    /// <summary>A full collection, finalizers run, and another: whether the page has gone. Its own frame, holding nothing.</summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static bool Collected(WeakReference page)
    {
        for (var i = 0; i < 2 && page.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return !page.IsAlive;
    }

    /// <summary>
    /// <c>datetime-month: round 1, check: 0 frames asked for, page collected, background post 0.4 ms before and 0.6 ms after</c>,
    /// with the platform's state, then a warning line for each thing wrong.
    /// </summary>
    public static IEnumerable<string> Lines(string label, int round, int pendingFrames, bool collected, double? before, double? after, string? platform,
        string? cpuset = null, string? notInFront = null)
    {
        static string Ms(double? ms) => ms is { } v ? string.Create(CultureInfo.InvariantCulture, $"{v:0.0} ms") : "over 10 s";

        yield return $"{label}: round {round}, check: {pendingFrames} frames asked for, page {(collected ? "collected" : "still alive")}, background post {Ms(before)} before and {Ms(after)} after{(platform is { Length: > 0 } ? ", " + platform : "")}{(cpuset is null ? "" : ", cpuset " + cpuset)}{(notInFront is null ? "" : ", " + notInFront)}";
        if (notInFront is not null || cpuset is { } set && set != "/top-app")
        {
            yield return $"{label}: round {round}, warning: the app wasn't in front ({string.Join(", ", new[] { cpuset is null ? null : "cpuset " + cpuset, notInFront }.OfType<string>())}): Android gives it the little cores, and every time is slower; keep the screen on (docs/infra/performance/README.md)";
        }

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
