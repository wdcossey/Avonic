using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// What a change of month costs a calendar, as a user's tap on its next or previous month button makes it: for the page-cost
/// rounds of a <c>-p:AvonicPerf=true</c> build, on the <c>datetime-x1</c> page (#248). It finds the buttons by their automation
/// names, <see cref="NextName"/> and <see cref="PreviousName"/>, and invokes them through their automation peers, which raise
/// the button's click, so that any design of the calendar that keeps those names is timed the same way, with no private method
/// called. The month heading, a <c>TextBlock</c> reading the month as en-US's <c>MMMM yyyy</c>, says when the new month is in.
/// </summary>
/// <remarks>
/// Each change is timed from just before the click by the top level's frame callbacks (<see cref="TopLevel.RequestAnimationFrame"/>),
/// asked for again from each one, so that every frame in the window is seen, whatever the calendar animates with. Avalonia paces
/// them by its composition batches, one a frame of the render loop, so a frame the UI thread was too busy to give shows as a long
/// gap. They don't need Avalonia's meter, which .NET for Android's <c>MetricsSupport</c> turns off. The first frame drawn is the
/// compositor's next batch after the click, which its <c>Rendered</c> task says has been drawn.
/// docs/infra/performance/README.md, "Page costs on a device".
/// </remarks>
public sealed class MonthChangeTiming
{
    /// <summary>The next-month button's automation name.</summary>
    public const string NextName = "Next month";

    /// <summary>The previous-month button's automation name.</summary>
    public const string PreviousName = "Previous month";

    /// <summary>A frame of a 60 Hz display, and two of them.</summary>
    public const double Frame60Ms = 1000.0 / 60;

    public const double Frame30Ms = 1000.0 / 30;

    /// <summary>
    /// The least each change is watched for, from the click: longer than any transition the calendar should have, so that the
    /// whole of one is seen, however it's drawn (Ionic's scroll is about 0.3 s, and its new month comes 50 ms after).
    /// </summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(1);

    /// <summary>The most each change is watched for, when the heading never shows the month it went to.</summary>
    public static readonly TimeSpan MaxWindow = TimeSpan.FromSeconds(5);

    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
    private const string HeadingFormat = "MMMM yyyy";

    private readonly Control _root;
    private readonly TimeSpan _window;
    private TextBlock? _heading;

    /// <param name="root">The calendar, or a page with one calendar.</param>
    /// <param name="window">The least each change is watched for: <see cref="DefaultWindow"/> unless given.</param>
    public MonthChangeTiming(Control root, TimeSpan? window = null)
    {
        _root = root;
        _window = window ?? DefaultWindow;
    }

    /// <summary>
    /// <paramref name="forward"/> changes to the next month, then <paramref name="back"/> to the previous, one after the other,
    /// each once the last one's window is over. <paramref name="each"/> hears each as it's done.
    /// </summary>
    public async Task<IReadOnlyList<MonthChange>> Run(int forward, int back, Action<MonthChange>? each = null)
    {
        var changes = new List<MonthChange>();
        for (var i = 1; i <= forward + back; i++)
        {
            var next = i <= forward;
            var change = await Change(next, next ? i : i - forward);
            changes.Add(change);
            each?.Invoke(change);
        }

        return changes;
    }

    private async Task<MonthChange> Change(bool next, int index)
    {
        var name = next ? NextName : PreviousName;
        var top = TopLevel.GetTopLevel(_root) ?? throw new InvalidOperationException("The calendar isn't in a top level.");
        var button = _root.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => AutomationProperties.GetName(c) == name)
                     ?? throw new InvalidOperationException($"No control named \"{name}\" to automation.");
        var peer = ControlAutomationPeer.CreatePeerForElement(button);
        var invoke = peer.GetProvider<IInvokeProvider>() ?? throw new InvalidOperationException($"\"{name}\" can't be invoked.");
        if (!peer.IsEnabled())
        {
            throw new InvalidOperationException($"\"{name}\" is disabled.");
        }

        var from = Heading(rescan: true);
        DateTime? target = from?.AddMonths(next ? 1 : -1);
        var expected = target?.ToString(HeadingFormat, EnUs);
        var compositor = ElementComposition.GetElementVisual(top)?.Compositor;

        var frames = new List<long>();
        var rendered = new StrongBox<long>();
        var headingAt = 0L;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var start = Stopwatch.GetTimestamp();
        invoke.Invoke();

        // The next batch is the first frame with the click's changes in it. Its Rendered task completes on the render thread, as
        // it's drawn: only the time is taken there.
        compositor?.RequestCompositionBatchCommitAsync().Rendered.ContinueWith(
            _ => Volatile.Write(ref rendered.Value, Stopwatch.GetTimestamp()),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        void OnFrame(TimeSpan _)
        {
            var now = Stopwatch.GetTimestamp();
            frames.Add(now);
            var elapsed = Stopwatch.GetElapsedTime(start, now);
            if (headingAt == 0 && expected is not null && Shows(expected, rescan: elapsed >= _window))
            {
                headingAt = now;
            }

            if ((elapsed >= _window && (headingAt != 0 || expected is null)) || elapsed >= MaxWindow)
            {
                done.TrySetResult();
                return;
            }

            top.RequestAnimationFrame(OnFrame);
        }

        top.RequestAnimationFrame(OnFrame);
        await done.Task;

        var gaps = new double[frames.Count];
        for (var i = 0; i < frames.Count; i++)
        {
            gaps[i] = Stopwatch.GetElapsedTime(i == 0 ? start : frames[i - 1], frames[i]).TotalMilliseconds;
        }

        var drawn = Volatile.Read(ref rendered.Value);
        return new MonthChange(
            next,
            index,
            from?.ToString(HeadingFormat, EnUs),
            expected,
            drawn == 0 ? null : Stopwatch.GetElapsedTime(start, drawn).TotalMilliseconds,
            headingAt == 0 ? null : Stopwatch.GetElapsedTime(start, headingAt).TotalMilliseconds,
            FrameGaps.Of(gaps));
    }

    /// <summary>The month the heading shows, found again when asked to or when the one found last has gone.</summary>
    private DateTime? Heading(bool rescan)
    {
        if (rescan || _heading is null || TopLevel.GetTopLevel(_heading) is null)
        {
            _heading = _root.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.IsEffectivelyVisible && Month(t.Text) is not null);
        }

        return Month(_heading?.Text);
    }

    private bool Shows(string expected, bool rescan)
    {
        if (_heading?.Text == expected)
        {
            return true;
        }

        // A design may build its heading again: look for it once the window is over, not on every frame before.
        return rescan && Heading(rescan: true)?.ToString(HeadingFormat, EnUs) == expected;
    }

    private static DateTime? Month(string? text) =>
        DateTime.TryParseExact(text, HeadingFormat, EnUs, DateTimeStyles.None, out var month) ? month : null;

    /// <summary>
    /// <c>datetime-month: round 2, 20 changes, first median 21.0 max 35.0 ms, heading median 405.0 max 460.0 ms, longest median 40.0
    /// max 120.0 ms, gap median 8.3 ms, 1212 frames, 33 over 16.7 ms, 12 over 33 ms</c>: medians and maxima over the round's changes,
    /// and totals.
    /// </summary>
    public static string Summary(string label, int round, IReadOnlyList<MonthChange> changes)
    {
        static string MedianMax(IEnumerable<double?> values)
        {
            var known = values.OfType<double>().ToArray();
            return known.Length == 0 ? "n/a" : string.Create(CultureInfo.InvariantCulture, $"median {Median(known):0.0} max {known.Max():0.0} ms");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{label}: round {round}, {changes.Count} changes, first {MedianMax(changes.Select(c => c.FirstMs))}, heading {MedianMax(changes.Select(c => c.HeadingMs))}, longest {MedianMax(changes.Select(c => (double?)c.Gaps.LongestMs))}, gap median {Median([.. changes.Select(c => c.Gaps.MedianMs)]):0.0} ms, {changes.Sum(c => c.Gaps.Frames)} frames, {changes.Sum(c => c.Gaps.Over60)} over 16.7 ms, {changes.Sum(c => c.Gaps.Over30)} over 33 ms");
    }

    internal static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.Order().ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }
}

/// <summary>The frames seen in one change's window: how many, the longest gap and the median, and how many gaps were long.</summary>
/// <param name="Frames">Frame callbacks in the window.</param>
/// <param name="LongestMs">The longest gap, the first from the click.</param>
/// <param name="MedianMs">The median gap: the display's frame, when nothing is slow.</param>
/// <param name="Over60">Gaps over a 60 Hz frame, 16.7 ms.</param>
/// <param name="Over30">Gaps over two of them, 33.3 ms.</param>
public readonly record struct FrameGaps(int Frames, double LongestMs, double MedianMs, int Over60, int Over30)
{
    /// <summary>The gaps between frames, in milliseconds, the first from the click.</summary>
    public static FrameGaps Of(IReadOnlyList<double> gaps) => new(
        gaps.Count,
        gaps.Count == 0 ? 0 : gaps.Max(),
        MonthChangeTiming.Median(gaps),
        gaps.Count(g => g > MonthChangeTiming.Frame60Ms),
        gaps.Count(g => g > MonthChangeTiming.Frame30Ms));
}

/// <summary>One change of month.</summary>
/// <param name="Next">To the next month, or the previous.</param>
/// <param name="Index">Which change in that direction, from 1.</param>
/// <param name="From">The heading before the click, if one was found.</param>
/// <param name="To">The heading it should show after.</param>
/// <param name="FirstMs">From the click until the first frame with it was drawn; null if that was never heard.</param>
/// <param name="HeadingMs">From the click until a frame in which the heading showed the new month; null if it never did.</param>
/// <param name="Gaps">The frames in the window.</param>
public sealed record MonthChange(bool Next, int Index, string? From, string? To, double? FirstMs, double? HeadingMs, FrameGaps Gaps)
{
    /// <summary>
    /// <c>datetime-month: round 1, next 3, August 2023 to September 2023, first 23.4 ms, heading 412.0 ms, 61 frames, longest 48.0 ms,
    /// gap median 8.3 ms, 3 over 16.7 ms, 1 over 33 ms</c>. <c>tools/page-costs/table.js</c> reads it.
    /// </summary>
    public string Line(string label, int round) => string.Create(
        CultureInfo.InvariantCulture,
        $"{label}: round {round}, {(Next ? "next" : "previous")} {Index}, {From ?? "?"} to {To ?? "?"}, first {Ms(FirstMs)}, heading {Ms(HeadingMs)}, {Gaps.Frames} frames, longest {Gaps.LongestMs:0.0} ms, gap median {Gaps.MedianMs:0.0} ms, {Gaps.Over60} over 16.7 ms, {Gaps.Over30} over 33 ms");

    private static string Ms(double? value) => value is { } ms ? string.Create(CultureInfo.InvariantCulture, $"{ms:0.0} ms") : "n/a";
}
