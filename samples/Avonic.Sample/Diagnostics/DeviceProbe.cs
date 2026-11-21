using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// What a <c>-p:AvonicPerf=true</c> run can see of the app's state on a device, cheaply, to tell apart why every page grew 5 to 10
/// times slower after the datetime's month timing on the Pixel 8 (#258): the collector (counts, pauses, the heap), work the UI thread
/// keeps doing above <c>Background</c> (layout passes, late posts, its own CPU time with no layout or collection to show for it), or
/// something left in the tree or still alive (the visuals under the top level, automation peers, the page left last). Each figure is
/// a delta over a window or a count at its end. docs/infra/performance/README.md, "Page costs on a device".
/// </summary>
public static class DeviceProbe
{
    /// <summary>How long <see cref="Busy"/> watches.</summary>
    public static readonly TimeSpan BusyWindow = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan PostEvery = TimeSpan.FromMilliseconds(100);

    /// <summary>The process's and the collector's counters at one moment.</summary>
    public readonly record struct Snapshot(long Timestamp, int Gen0, int Gen1, int Gen2, TimeSpan GcPause, TimeSpan ProcessCpu, double? UiThreadCpuMs)
    {
        /// <summary>Now. On the UI thread, for its own CPU time.</summary>
        public static Snapshot Take() => new(
            Stopwatch.GetTimestamp(),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            GC.GetTotalPauseDuration(),
            DeviceProbe.ProcessCpu(),
            DeviceProbe.ThreadCpuMs());

        /// <summary><c>gc 3/1/0, gc pause 12.3 ms, heap 85.2 MB</c>: collections of each generation and their pauses since, and the heap now.</summary>
        public string Gc(Snapshot since) => string.Create(
            CultureInfo.InvariantCulture,
            $"gc {Gen0 - since.Gen0}/{Gen1 - since.Gen1}/{Gen2 - since.Gen2}, gc pause {(GcPause - since.GcPause).TotalMilliseconds:0.0} ms, heap {HeapBytes() / 1048576.0:0.0} MB");

        /// <summary>
        /// The heap now. <c>GC.GetTotalMemory(false)</c> can read below 0 right after collections (seen headless, after four of each
        /// generation in a test run: -3.1 MB); then the heap at the last collection.
        /// </summary>
        private static long HeapBytes() => GC.GetTotalMemory(false) is >= 0 and var bytes ? bytes : GC.GetGCMemoryInfo().HeapSizeBytes;

        /// <summary><c>cpu 1830 ms, ui thread 1790 ms</c>: the process's CPU time and the UI thread's since, where the platform says.</summary>
        public string Cpu(Snapshot since) => string.Create(
            CultureInfo.InvariantCulture,
            $"cpu {(ProcessCpu - since.ProcessCpu).TotalMilliseconds:0} ms, ui thread {(UiThreadCpuMs is { } now && since.UiThreadCpuMs is { } then ? $"{now - then:0} ms" : "n/a")}");
    }

    /// <summary>Counts the top level's layout passes from now until it's disposed.</summary>
    public sealed class LayoutPasses : IDisposable
    {
        private readonly TopLevel? _top;

        public LayoutPasses(TopLevel? top)
        {
            _top = top;
            if (_top is not null)
            {
                _top.LayoutUpdated += OnLayout;
            }
        }

        public int Count { get; private set; }

        private void OnLayout(object? sender, EventArgs e) => Count++;

        public void Dispose()
        {
            if (_top is not null)
            {
                _top.LayoutUpdated -= OnLayout;
            }
        }
    }

    /// <summary>
    /// <c>, gc 3/1/0, gc pause 12.3 ms, heap 85.2 MB, cpu 210 ms, ui thread 190 ms, passes 4, tree 1234, peers 0</c>: what a page's
    /// open cost beside its time, appended to its line. Not "layout": <c>table.js</c> reads the line's own layout.
    /// </summary>
    public static string PageExtras(Snapshot before, int passes, TopLevel? top)
    {
        var now = Snapshot.Take();
        var (tree, peers) = Tree(top);
        return string.Create(CultureInfo.InvariantCulture, $", {now.Gc(before)}, {now.Cpu(before)}, passes {passes}, tree {tree}, peers {peers}");
    }

    /// <summary>The visuals under the top level, itself included, and how many of them have an automation peer already.</summary>
    public static (int Visuals, int Peers) Tree(Visual? root)
    {
        if (root is null)
        {
            return (0, 0);
        }

        var visuals = 1;
        var peers = HasPeer(root) ? 1 : 0;
        foreach (var visual in root.GetVisualDescendants())
        {
            visuals++;
            if (HasPeer(visual))
            {
                peers++;
            }
        }

        return (visuals, peers);
    }

    // FromElement returns a peer the control already has, and makes none.
    private static bool HasPeer(Visual visual) => visual is Control control && ControlAutomationPeer.FromElement(control) is not null;

    /// <summary>
    /// What the UI thread does over <see cref="BusyWindow"/> with nothing asked of it: its layout passes, its frames (asked for each
    /// frame, as an animation would, so their gaps show how long the thread was held), how late a post at <c>Default</c> and at
    /// <c>Background</c> priority ran (one of each every 100 ms), the collector, and the CPU time. And the page left last, if
    /// <paramref name="left"/> still has it: whether it's still in a top level, and how often its calendar's scroller moved.
    /// <c>label: busy: 2.0 s, cpu 1830 ms, ui thread 1790 ms, passes 0, frames 120 longest 16.9 ms, default post 20 median 0.3 max
    /// 1.2 ms, background post 20/20 median 0.5 max 3.1 ms, gc 0/0/0, gc pause 0.0 ms, heap 85.2 MB, tree 312, peers 0, left page
    /// gone</c>.
    /// </summary>
    public static async Task<string> Busy(string label, TopLevel? top, WeakReference? left)
    {
        var before = Snapshot.Take();
        using var passes = new LayoutPasses(top);
        var frames = new List<long>();
        var over = false;

        void OnFrame(TimeSpan _)
        {
            if (over)
            {
                return;
            }

            frames.Add(Stopwatch.GetTimestamp());
            top!.RequestAnimationFrame(OnFrame);
        }

        top?.RequestAnimationFrame(OnFrame);

        // The page left last, if it's alive: in a tree or not, and its calendars' scrollers moving.
        var leftState = "gone";
        var scrollerMoves = 0;
        var scrollers = new List<ScrollViewer>();
        void OnScroller(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ScrollViewer.OffsetProperty || e.Property == ScrollViewer.ExtentProperty || e.Property == ScrollViewer.ViewportProperty)
            {
                scrollerMoves++;
            }
        }

        if (left is not null)
        {
            leftState = LeftPage(left, scrollers);
            foreach (var scroller in scrollers)
            {
                scroller.PropertyChanged += OnScroller;
            }
        }

        var defaultLate = new List<double>();
        var backgroundLate = new List<double>();
        var posted = 0;
        var started = Stopwatch.GetTimestamp();
        try
        {
            while (Stopwatch.GetElapsedTime(started) < BusyWindow)
            {
                posted++;
                var at = Stopwatch.GetTimestamp();
                Dispatcher.UIThread.Post(() => defaultLate.Add(Stopwatch.GetElapsedTime(at).TotalMilliseconds), DispatcherPriority.Default);
                Dispatcher.UIThread.Post(() => backgroundLate.Add(Stopwatch.GetElapsedTime(at).TotalMilliseconds), DispatcherPriority.Background);
                await Task.Delay(PostEvery);
            }
        }
        finally
        {
            over = true;
            foreach (var scroller in scrollers)
            {
                scroller.PropertyChanged -= OnScroller;
            }
        }

        // The scrollers held the page through the window; let go of them, then ask again, from fresh turns of the dispatcher.
        var watched = scrollers.Count;
        scrollers.Clear();
        var collectedAfter = left is null || !left.IsAlive ? (bool?)null : await PerfChecks.CollectedAsync(left);

        var after = Snapshot.Take();
        var window = Stopwatch.GetElapsedTime(started);
        var longest = 0.0;
        for (var i = 1; i < frames.Count; i++)
        {
            longest = Math.Max(longest, Stopwatch.GetElapsedTime(frames[i - 1], frames[i]).TotalMilliseconds);
        }

        var (tree, treePeers) = Tree(top);
        static string Late(List<double> values) => values.Count == 0 ? "none ran" : string.Create(
            CultureInfo.InvariantCulture, $"median {MonthChangeTiming.Median(values):0.0} max {values.Max():0.0} ms");

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{label}: busy: {window.TotalSeconds:0.0} s, {after.Cpu(before)}, passes {passes.Count}, frames {frames.Count} longest {longest:0.0} ms, default post {defaultLate.Count}/{posted} {Late(defaultLate)}, background post {backgroundLate.Count}/{posted} {Late(backgroundLate)}, {after.Gc(before)}, tree {tree}, peers {treePeers}, left page {leftState}{(watched > 0 ? $", its scrollers changed {scrollerMoves} times" : "")}{collectedAfter switch { true => ", then collected", false => ", STILL ALIVE after the window and a collection", null => "" }}");
    }

    /// <summary>
    /// What's left of the page: <c>gone</c>, or alive, in a top level or not, with its visuals and peers; and its calendars'
    /// scrollers, to watch. Its own method, so that no local of the probe's state machine holds the page past it.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static string LeftPage(WeakReference left, List<ScrollViewer> scrollers)
    {
        if (left.Target is not Control page)
        {
            return "gone";
        }

        var (visuals, peers) = Tree(page);
        scrollers.AddRange(page.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.Name == "PART_CalendarBody"));
        return $"alive, {(TopLevel.GetTopLevel(page) is null ? "out of the tree" : "STILL IN A TOP LEVEL")}, {visuals} visuals, {peers} peers";
    }

    internal static TimeSpan ProcessCpu()
    {
        try
        {
            return Process.GetCurrentProcess().TotalProcessorTime;
        }
        catch (Exception)
        {
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// The calling thread's CPU time in ms, from Linux's <c>/proc/thread-self/stat</c> (user and system time, in clock ticks of
    /// 100 Hz, Android's and Linux's <c>USER_HZ</c>); null where there's no such file.
    /// </summary>
    internal static double? ThreadCpuMs()
    {
        try
        {
            const string path = "/proc/thread-self/stat";
            if (!File.Exists(path))
            {
                return null;
            }

            var stat = File.ReadAllText(path);
            // The command name is in parentheses and may hold spaces: fields count from after its closing one.
            var fields = stat[(stat.LastIndexOf(')') + 2)..].Split(' ');
            // Fields 14 and 15 of the whole line, utime and stime, are 12 and 13 here (the state is field 3, index 0).
            return (long.Parse(fields[11], CultureInfo.InvariantCulture) + long.Parse(fields[12], CultureInfo.InvariantCulture)) * 10.0;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
