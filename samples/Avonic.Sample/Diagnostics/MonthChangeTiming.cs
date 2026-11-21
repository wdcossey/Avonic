using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// What a change of month costs a calendar, as a user's tap on its next or previous month button makes it: for the page-cost
/// rounds of a <c>-p:AvonicPerf=true</c> build, on the <c>datetime-x1</c> page (#248). It finds the buttons by their automation
/// names, <see cref="NextName"/> and <see cref="PreviousName"/>, and raises the routed <c>Click</c> event their type registers, as
/// a tap ends in, so that any design of the calendar that keeps those names is timed the same way, with no private method
/// called. The month heading, a <c>TextBlock</c> reading the month as en-US's <c>MMMM yyyy</c>, says when the new month is in.
/// </summary>
/// <remarks>
/// <para>
/// Each change is timed from just before the click by the top level's frame callbacks (<see cref="TopLevel.RequestAnimationFrame"/>),
/// asked for again from each one, so that every frame in the window is seen, whatever the calendar animates with. Avalonia paces
/// them by its composition batches, one a frame of the render loop, so a frame the UI thread was too busy to give shows as a long
/// gap. They don't need Avalonia's meter, which .NET for Android's <c>MetricsSupport</c> turns off. The first frame drawn is the
/// compositor's next batch after the click, which its <c>Rendered</c> task says has been drawn.
/// </para>
/// <para>
/// It leaves nothing behind: no automation peer (a peer stays on its control, and on Android it's what Avalonia's accessibility
/// bridge works from), no frame asked for once a change is over (<see cref="PendingFrames"/>), no timer, and no reference to the
/// calendar once it has run. Raising the event, not a tap, it doesn't run a <c>Command</c> on the button or what a native button
/// does around it (<c>NativeButton.Click</c>): a calendar that changes month through those isn't timed. A change whose calendar leaves the tree ends at once. docs/infra/performance/README.md, "Page costs
/// on a device".
/// </para>
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

    /// <summary>How often, after the window, the whole calendar is searched for the heading, so that searching adds little to the gaps.</summary>
    private static readonly TimeSpan LookAgain = TimeSpan.FromMilliseconds(250);

    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
    private const string HeadingFormat = "MMMM yyyy";

    private static int _pendingFrames;

    /// <summary>The frames a timed swipe's finger takes to drag the calendar a month: 12, about 100 ms at 120 Hz.</summary>
    public const int SwipeFrames = 12;

    private readonly TimeSpan _window;
    private readonly TimeSpan _maxWindow;
    private readonly bool _bySwipe;
    private Control? _root;
    private TextBlock? _heading;

    /// <param name="root">The calendar, or a page with one calendar.</param>
    /// <param name="window">The least each change is watched for: <see cref="DefaultWindow"/> unless given.</param>
    /// <param name="maxWindow">The most: <see cref="MaxWindow"/> unless given. With no frame for twice that, the change fails.</param>
    /// <param name="bySwipe">
    /// Each change a finger's swipe on the scrolled calendar instead of a click (#248, spike B): a touch pressed on its scroller (the
    /// <see cref="ScrollViewer"/> of its <see cref="DatetimeCalendarBodyPanel"/>), the offset moved a month over the next
    /// <see cref="SwipeFrames"/> frames, as a finger's moves are, from each frame's callback, then the touch lifted; the calendar's
    /// scroll rest then changes the month. Its first frame after the press shows what the press cost.
    /// </param>
    public MonthChangeTiming(Control root, TimeSpan? window = null, TimeSpan? maxWindow = null, bool bySwipe = false)
    {
        _root = root;
        _window = window ?? DefaultWindow;
        _maxWindow = maxWindow ?? MaxWindow;
        _bySwipe = bySwipe;
    }

    /// <summary>
    /// Frame callbacks asked for by any timing and not yet called, in the whole process: 0 once every timing is over, unless one
    /// has left a frame loop running, which would keep the app drawing.
    /// </summary>
    public static int PendingFrames => Volatile.Read(ref _pendingFrames);

    /// <summary>
    /// <paramref name="forward"/> changes to the next month, then <paramref name="back"/> to the previous, one after the other,
    /// each once the last one's window is over. <paramref name="each"/> hears each as it's done. It runs once: after it, the timing
    /// holds no reference to the calendar.
    /// </summary>
    public async Task<IReadOnlyList<MonthChange>> Run(int forward, int back, Action<MonthChange>? each = null)
    {
        var root = _root ?? throw new InvalidOperationException("A timing runs once.");
        try
        {
            var changes = new List<MonthChange>();
            for (var i = 1; i <= forward + back; i++)
            {
                var next = i <= forward;
                var change = await Change(root, next, next ? i : i - forward);
                changes.Add(change);
                each?.Invoke(change);
            }

            return changes;
        }
        finally
        {
            _root = null;
            _heading = null;
        }
    }

    private async Task<MonthChange> Change(Control root, bool next, int index)
    {
        var name = _bySwipe ? (next ? "a swipe to the next month" : "a swipe to the previous month") : next ? NextName : PreviousName;
        var top = TopLevel.GetTopLevel(root) ?? throw new InvalidOperationException("The calendar isn't in a top level.");
        Control? button = null;
        RoutedEvent? click = null;
        ScrollViewer? scroller = null;
        if (_bySwipe)
        {
            scroller = root.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault(s => s.Content is DatetimeCalendarBodyPanel && s.IsEffectivelyVisible)
                       ?? throw new InvalidOperationException("No scrolled calendar to swipe.");
        }
        else
        {
            button = root.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => AutomationProperties.GetName(c) == name)
                     ?? throw new InvalidOperationException($"No control named \"{name}\" to automation.");
            click = ClickEvent(button.GetType()) ?? throw new InvalidOperationException($"\"{name}\" has no Click event.");
            if (!button.IsEffectivelyEnabled)
            {
                throw new InvalidOperationException($"\"{name}\" is disabled.");
            }
        }

        var finger = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        var at = scroller is null ? default : scroller.TranslatePoint(new Point(scroller.Bounds.Width / 2, scroller.Bounds.Height / 2), top) ?? default;
        var swipeFrom = scroller?.Offset.X ?? 0;
        var swipeTo = scroller is null ? 0 : next ? 2 * scroller.Viewport.Width : 0;
        var swipeStep = 0;

        var from = Heading(root, rescan: true);
        DateTime? target = from?.AddMonths(next ? 1 : -1);
        var expected = target?.ToString(HeadingFormat, EnUs);
        var compositor = ElementComposition.GetElementVisual(top)?.Compositor;

        var frames = new List<long>();
        var rendered = new StrongBox<long>();
        var headingAt = 0L;
        var lastLook = 0L;
        var start = 0L;
        var over = false;
        var asked = 0;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // A calendar that leaves the tree ends its change at once, rather than have frames drawn for it until the window is over.
        void OnDetached(object? sender, VisualTreeAttachmentEventArgs e) =>
            done.TrySetException(new InvalidOperationException("The calendar left the tree during a change."));

        void Ask()
        {
            asked++;
            Interlocked.Increment(ref _pendingFrames);
            top.RequestAnimationFrame(OnFrame);
        }

        void OnFrame(TimeSpan _)
        {
            // A callback that comes after its change was over was taken off the count then.
            if (over)
            {
                return;
            }

            asked--;
            Interlocked.Decrement(ref _pendingFrames);
            if (done.Task.IsCompleted)
            {
                return;
            }

            try
            {
                if (scroller is not null && swipeStep < SwipeFrames)
                {
                    // The finger's move for this frame, before its layout, as input comes; lifted at the last.
                    swipeStep++;
                    scroller.Offset = new Vector(swipeFrom + (swipeTo - swipeFrom) * swipeStep / SwipeFrames, 0);
                    if (swipeStep == SwipeFrames)
                    {
                        scroller.RaiseEvent(new PointerReleasedEventArgs(scroller, finger, top, at, 0,
                            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
                    }
                }

                var now = Stopwatch.GetTimestamp();
                frames.Add(now);
                var elapsed = Stopwatch.GetElapsedTime(start, now);
                if (headingAt == 0 && expected is not null)
                {
                    // Looked for again once the window is over, in case a design shows the month elsewhere, but not on every frame.
                    var look = elapsed >= _window && Stopwatch.GetElapsedTime(lastLook, now) >= LookAgain;
                    if (look)
                    {
                        lastLook = now;
                    }

                    if (Shows(root, expected, look))
                    {
                        headingAt = now;
                    }
                }

                if ((elapsed >= _window && (headingAt != 0 || expected is null)) || elapsed >= _maxWindow)
                {
                    done.TrySetResult();
                    return;
                }

                Ask();
            }
            catch (Exception ex)
            {
                done.TrySetException(ex);
            }
        }

        root.DetachedFromVisualTree += OnDetached;
        try
        {
            start = Stopwatch.GetTimestamp();
            if (scroller is not null)
            {
                scroller.RaiseEvent(new PointerPressedEventArgs(scroller, finger, top, at, 0,
                    new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            }
            else
            {
                button!.RaiseEvent(new RoutedEventArgs(click!, button));
            }

            // The compositor's next batch, the one not yet committed, which is committed on this thread after the click's layout:
            // the first frame with the click's changes in it. Its Rendered task completes on the render thread, as it's drawn: only
            // the time is taken there.
            compositor?.RequestCompositionBatchCommitAsync().Rendered.ContinueWith(
                _ => Volatile.Write(ref rendered.Value, Stopwatch.GetTimestamp()),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Ask();

            // Frames stop while the display is off or the app is in the background: say so, rather than wait for ever.
            var limit = _maxWindow + _maxWindow;
            try
            {
                await done.Task.WaitAsync(limit);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"No frame for {limit.TotalSeconds:0.#} s after \"{name}\": is the display off, or the app in the background?");
            }
        }
        finally
        {
            // However it ended, nothing of it goes on: a frame callback still asked for (one that may never come, when frames
            // have stopped) is taken off the count now, and returns at once if it comes, asking for no other.
            over = true;
            Interlocked.Add(ref _pendingFrames, -asked);
            asked = 0;
            root.DetachedFromVisualTree -= OnDetached;
        }

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

    /// <summary>
    /// The routed <c>Click</c> event of the button's type or the nearest base type that registers one: Avalonia's registry lists an
    /// event under the type that registered it only.
    /// </summary>
    private static RoutedEvent? ClickEvent(Type? type)
    {
        for (; type is not null; type = type.BaseType)
        {
            if (RoutedEventRegistry.Instance.GetRegistered(type).FirstOrDefault(e => e.Name == "Click") is { } click)
            {
                return click;
            }
        }

        return null;
    }

    /// <summary>The month the heading shows, found again when asked to or when the one found last has gone.</summary>
    private DateTime? Heading(Control root, bool rescan)
    {
        if (rescan || _heading is null || TopLevel.GetTopLevel(_heading) is null)
        {
            _heading = root.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.IsEffectivelyVisible && Month(t.Text) is not null);
        }

        return Month(_heading?.Text);
    }

    /// <summary>
    /// Whether the heading shows <paramref name="expected"/>: the one found last, or, when it has left the tree (a design that
    /// builds its heading again) or <paramref name="lookAgain"/> asks, the one found now.
    /// </summary>
    private bool Shows(Control root, string expected, bool lookAgain) =>
        (_heading?.Text == expected && TopLevel.GetTopLevel(_heading) is not null)
        || Heading(root, lookAgain)?.ToString(HeadingFormat, EnUs) == expected;

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
            $"{label}: round {round}, {changes.Count} changes, first {MedianMax(changes.Select(c => c.FirstMs))}, heading {MedianMax(changes.Select(c => c.HeadingMs))}, longest {MedianMax(changes.Select(c => (double?)c.Gaps.LongestMs))}, gap median {Median([.. changes.Where(c => c.Gaps.Frames > 0).Select(c => c.Gaps.MedianMs)]):0.0} ms, {changes.Sum(c => c.Gaps.Frames)} frames, {changes.Sum(c => c.Gaps.Over60)} over 16.7 ms, {changes.Sum(c => c.Gaps.Over30)} over 33 ms");
    }

    /// <summary>
    /// The median, the middle two averaged, as <c>tools/page-costs/table.js</c> takes it; 0 for none. Not <see cref="StyleCost.Median"/>,
    /// which takes the upper of the two and needs a value.
    /// </summary>
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
