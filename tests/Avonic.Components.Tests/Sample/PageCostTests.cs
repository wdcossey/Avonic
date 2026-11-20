using System.Diagnostics;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Sample;
using Avonic.Sample.Diagnostics;
using static Avonic.Components.Tests.Controls.AvonicDatetimeCaptureTests;

namespace Avonic.Components.Tests.Sample;

/// <summary>
/// What a <c>-p:AvonicPerf=true</c> build measures, which tests can't build with (#248): the pages its rounds open and its filter,
/// the perf-only datetime pages, and the month changes timed through the calendar's own buttons, in the lines
/// <c>tools/page-costs/table.js</c> reads. Timings aren't checked: a CI machine's mean nothing.
/// docs/infra/performance/README.md, "Page costs on a device".
/// </summary>
public partial class PageCostTests
{
    private static readonly IReadOnlyList<SamplePageEntry> SamplePages = SamplePageCatalog.Discover(typeof(SampleShell).Assembly);

    // As tools/page-costs/table.js reads them: keep the two in step.
    [GeneratedRegex(@"^([\w-]+): round (\d+), (next|previous) (\d+), .* to .*, first ([\d.]+ ms|n/a), heading ([\d.]+ ms|n/a), (\d+) frames, longest ([\d.]+) ms, gap median ([\d.]+) ms, (\d+) over 16\.7 ms, (\d+) over 33 ms$")]
    private static partial Regex ChangeLine();

    [GeneratedRegex(@"^([\w-]+): (\d+) ms, (\d+) elements, .*layout ([\d.]+), render ([\d.]+)")]
    private static partial Regex PageLine();

    private static string[] Titles(IEnumerable<SamplePageEntry> pages) => [.. pages.Select(p => p.Title)];

    // ── The pages ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void With_No_Filter_Every_Sample_Page_But_The_Device_Page_Opens_Then_The_Datetime_Pages()
    {
        var expected = Titles(SamplePages).Where(t => t != "device")
            .Concat(["datetime-x1", "datetime-x2", "datetime-x4", "datetime-x1-grid", "datetime-x2-grid", "datetime-x4-grid"]);

        Assert.Equal(expected, Titles(PageCostSet.Select(SamplePages, null)));
        Assert.Equal(expected, Titles(PageCostSet.Select(SamplePages, " ")));
    }

    [Fact]
    public void The_Datetime_Pages_Are_Not_In_The_Samples_List()
    {
        Assert.DoesNotContain(Titles(SamplePages), t => t.StartsWith("datetime-x", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(DatetimeCostView).GetCustomAttributes(typeof(SamplePageAttribute), false), _ => true);
    }

    [Fact]
    public void A_Filter_Keeps_The_Pages_Whose_Titles_Hold_Any_Of_Its_Terms()
    {
        var datetime = Titles(PageCostSet.Select(SamplePages, "datetime"));
        Assert.Contains("ion-datetime", datetime);
        Assert.Contains("datetime-x1", datetime);
        Assert.Contains("datetime-x4", datetime);
        Assert.DoesNotContain("ion-button", datetime);

        Assert.Equal(["datetime-x1", "datetime-x1-grid"], Titles(PageCostSet.Select(SamplePages, "DATETIME-X1")));
        Assert.Equal(["ion-button", "datetime-x2", "datetime-x2-grid"], Titles(PageCostSet.Select(SamplePages, "ion-button, datetime-x2")));
        Assert.Equal(["datetime-x1", "datetime-x2", "datetime-x4", "datetime-x1-grid", "datetime-x2-grid", "datetime-x4-grid"],
            Titles(PageCostSet.Select(SamplePages, "datetime-x")));
        // ion-grid is a sample page: "x1-grid" and so on, or "datetime", for the grid pages.
        Assert.Equal(["ion-grid", "datetime-x1-grid", "datetime-x2-grid", "datetime-x4-grid"], Titles(PageCostSet.Select(SamplePages, "-grid")));
        Assert.Empty(PageCostSet.Select(SamplePages, "no-such-page"));

        // A term with "=" is a whole title.
        Assert.Equal(["ion-datetime", "ion-datetime-button"], Titles(PageCostSet.Select(SamplePages, "ion-datetime")));
        Assert.Equal(["ion-datetime", "datetime-x1"], Titles(PageCostSet.Select(SamplePages, "=ION-DATETIME,=datetime-x1")));
    }

    [AvaloniaFact]
    public void A_Datetime_Page_Holds_That_Many_Plain_Datetimes_On_A_Pinned_Month() => Run((scene, clock) =>
    {
        var page = new DatetimeCostView(4);
        scene.Window.Content = page;
        Settle(clock, 400);

        var datetimes = page.GetVisualDescendants().OfType<AvonicDatetime>().ToArray();
        Assert.Equal(4, datetimes.Length);
        Assert.All(datetimes, d =>
        {
            Assert.Equal(DatetimeCostView.PinnedValue, d.Value.ToString());
            Assert.Equal("en-US", d.Locale);
            Assert.Equal(DatetimePresentation.DateTime, d.Presentation);
            Assert.Equal("June 2023", d.GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "PART_MonthYearText").Text);
            Assert.Equal(DatetimeCalendarRendering.ScrolledMonths, d.CalendarRendering);
        });
    });

    [AvaloniaFact]
    public void A_Grid_Page_Holds_The_Same_Datetimes_As_Single_Grids() => Run((scene, clock) =>
    {
        var page = new DatetimeCostView(2, DatetimeCalendarRendering.SingleGrid);
        scene.Window.Content = page;
        Settle(clock, 400);

        var datetimes = page.GetVisualDescendants().OfType<AvonicDatetime>().ToArray();
        Assert.Equal(2, datetimes.Length);
        Assert.All(datetimes, d =>
        {
            Assert.Equal(DatetimeCostView.PinnedValue, d.Value.ToString());
            Assert.Equal(DatetimeCalendarRendering.SingleGrid, d.CalendarRendering);
            Assert.Equal(42, d.GetVisualDescendants().OfType<DatetimeCalendarDay>().Count());
        });
        Assert.Equal("datetime-x2-grid", DatetimeCostView.Title(2, DatetimeCalendarRendering.SingleGrid));
        Assert.Equal(PageCostSet.MonthChangeLabel, PageCostSet.MonthChangeLabelOf("datetime-x1"));
        Assert.Equal(PageCostSet.GridMonthChangeLabel, PageCostSet.MonthChangeLabelOf("datetime-x1-grid"));
        Assert.Null(PageCostSet.MonthChangeLabelOf("datetime-x2-grid"));
    });

    [AvaloniaFact]
    public void A_Grids_Month_Changes_Are_Timed_The_Same_Way() => Run((scene, clock) =>
    {
        var page = new DatetimeCostView(1, DatetimeCalendarRendering.SingleGrid);
        scene.Window.Content = page;
        Settle(clock, 400);
        var datetime = page.GetVisualDescendants().OfType<AvonicDatetime>().Single();

        var run = new MonthChangeTiming(datetime, TimeSpan.FromMilliseconds(20)).Run(2, 2);
        Drive(clock, run);

        var changes = run.Result;
        Assert.Equal(
            ["next 1 June 2023 to July 2023", "next 2 July 2023 to August 2023", "previous 1 August 2023 to July 2023", "previous 2 July 2023 to June 2023"],
            changes.Select(c => $"{(c.Next ? "next" : "previous")} {c.Index} {c.From} to {c.To}"));
        Assert.All(changes, c => Assert.NotNull(c.HeadingMs));
        Assert.Matches(ChangeLine(), changes[0].Line(PageCostSet.GridMonthChangeLabel, 1));
        Assert.Equal(0, MonthChangeTiming.PendingFrames);
    });

    // ── The month changes ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Month_Changes_Go_Through_The_Calendars_Buttons_And_Are_Timed_Until_The_Heading_Shows_The_Month() => Run((scene, clock) =>
    {
        var page = new DatetimeCostView(1);
        scene.Window.Content = page;
        Settle(clock, 400);
        var datetime = page.GetVisualDescendants().OfType<AvonicDatetime>().Single();
        var heard = new List<MonthChange>();

        var run = new MonthChangeTiming(datetime, TimeSpan.FromMilliseconds(20)).Run(2, 2, heard.Add);
        Drive(clock, run);

        var changes = run.Result;
        Assert.Equal(heard, changes);
        Assert.Equal(
            ["next 1 June 2023 to July 2023", "next 2 July 2023 to August 2023", "previous 1 August 2023 to July 2023", "previous 2 July 2023 to June 2023"],
            changes.Select(c => $"{(c.Next ? "next" : "previous")} {c.Index} {c.From} to {c.To}"));
        Assert.All(changes, c =>
        {
            Assert.NotNull(c.HeadingMs);
            Assert.NotNull(c.FirstMs);
            Assert.True(c.Gaps.Frames > 0);
            Assert.True(c.Gaps.LongestMs >= c.Gaps.MedianMs);
        });
        Assert.Equal("June 2023", datetime.GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "PART_MonthYearText").Text);
    });

    [AvaloniaFact]
    public void A_Calendar_With_No_Next_Month_Button_Says_So() => Run((scene, clock) =>
    {
        var datetime = new AvonicDatetime { Presentation = DatetimePresentation.Time, Value = DatetimeCostView.PinnedValue };
        Show(scene, clock, datetime);

        var run = new MonthChangeTiming(datetime).Run(1, 0);
        Drive(clock, run);

        Assert.True(run.IsFaulted);
        Assert.Contains("Next month", run.Exception!.InnerException!.Message, StringComparison.Ordinal);
    });

    [AvaloniaFact]
    public void A_Change_With_No_Frames_Fails_Rather_Than_Wait_For_Ever() => Run((scene, clock) =>
    {
        var datetime = Show(scene, clock, new AvonicDatetime { Value = DatetimeCostView.PinnedValue, Locale = "en-US" });

        // No render tick, as with the display off: no frame callback comes.
        var run = new MonthChangeTiming(datetime, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(100)).Run(1, 0);
        var until = DateTime.UtcNow.AddSeconds(30);
        while (!run.IsCompleted && DateTime.UtcNow < until)
        {
            Dispatcher.UIThread.RunJobs();
        }

        Assert.True(run.IsFaulted);
        Assert.IsType<TimeoutException>(run.Exception!.InnerException);

        // The frame it asked for never came, and is off the count: a later check doesn't take it for a frame loop.
        Assert.Equal(0, MonthChangeTiming.PendingFrames);
    });

    // ── What a timing leaves behind (#248: on the Pixel, every page after the first timing was 5 to 10 times slower) ──────────

    // A peer stays on its control for good, and on Android it's what Avalonia's accessibility bridge works from: the measuring
    // mustn't add any.
    [AvaloniaFact]
    public void A_Timing_Leaves_No_Automation_Peer_On_The_Calendars_Buttons() => Run((scene, clock) =>
    {
        var page = new DatetimeCostView(1);
        scene.Window.Content = page;
        Settle(clock, 400);
        var datetime = page.GetVisualDescendants().OfType<AvonicDatetime>().Single();
        var buttons = datetime.GetVisualDescendants().OfType<Control>()
            .Where(c => Avalonia.Automation.AutomationProperties.GetName(c) is MonthChangeTiming.NextName or MonthChangeTiming.PreviousName)
            .ToArray();

        var run = new MonthChangeTiming(datetime, TimeSpan.FromMilliseconds(20)).Run(1, 1);
        Drive(clock, run);

        Assert.True(run.IsCompletedSuccessfully);
        Assert.Equal(2, buttons.Length);
        Assert.All(buttons, b => Assert.Null(Avalonia.Automation.Peers.ControlAutomationPeer.FromElement(b)));
    });

    // Once its page has gone and the clock has run what was due (the calendar's 50 ms scroll rest among it): no frame is asked for,
    // and nothing is scheduled. Whether the page is collected isn't asked here: this harness keeps even an untimed page alive, so
    // the device run checks it (PerfChecks.Collected).
    [AvaloniaFact]
    public void After_A_Timing_And_Its_Page_No_Frame_Is_Asked_For_And_Nothing_Is_Scheduled() => Run((scene, clock) =>
    {
        var page = new DatetimeCostView(1);
        scene.Window.Content = page;
        Settle(clock, 400);
        var run = new MonthChangeTiming(page.GetVisualDescendants().OfType<AvonicDatetime>().Single(), TimeSpan.FromMilliseconds(20)).Run(1, 1);
        Drive(clock, run);
        Assert.True(run.IsCompletedSuccessfully);

        scene.Window.Content = null;
        Settle(clock, 2000);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, MonthChangeTiming.PendingFrames);
        Assert.Equal(0, clock.Pending);
    });

    // #258's Pixel run: after round 1's month timing the timed page stayed alive ("something still holds it") and every later page
    // was 5 to 10 times slower. The holder: the calendar's previous and next buttons took their colour from a SolidColorBrush made in
    // the template's own Resources with a DynamicResource colour, which resolves through the theme's dictionary, so the theme, which
    // lives as long as the app, held each brush, and through it the buttons, the datetime and its page. Every datetime that drew its
    // calendar leaked so, timed or not, on v2 too. Each rendering, timed: the page goes once left, and nothing asks for a frame or
    // waits on a timer for it.
    [AvaloniaTheory]
    [InlineData(DatetimeCalendarRendering.ScrolledMonths)]
    [InlineData(DatetimeCalendarRendering.SingleGrid)]
    public void A_Timed_Page_Is_Collected_Once_Left_And_Leaves_No_Frame_Or_Timer(DatetimeCalendarRendering rendering) => Run((scene, clock) =>
    {
        var page = TimeAndLeave(scene, clock, rendering);
        Settle(clock, 2000);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        for (var i = 0; i < 3 && page.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(page.IsAlive, "the timed page is still alive after it was left and a full collection");
        Assert.Equal(0, MonthChangeTiming.PendingFrames);
        Assert.Equal(0, clock.Pending);
    });

    // Its own method, so that no local of the test holds the page.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference TimeAndLeave(Scene scene, TestClock clock, DatetimeCalendarRendering rendering)
    {
        var page = new DatetimeCostView(1, rendering);
        scene.Window.Content = page;
        Settle(clock, 400);
        var run = new MonthChangeTiming(page.GetVisualDescendants().OfType<AvonicDatetime>().Single(), TimeSpan.FromMilliseconds(20)).Run(2, 2);
        Drive(clock, run);
        Assert.True(run.IsCompletedSuccessfully);
        scene.Window.Content = null;
        return new WeakReference(page);
    }

    // DeviceProbe (#258's device diagnostics): its busy window over a page just left, and what it says of that page; it keeps none.
    [AvaloniaFact]
    public void The_Busy_Probe_Reports_The_Ui_Thread_And_The_Page_Left_And_Keeps_Neither() => Run((scene, clock) =>
    {
        var left = OpenAndLeave(scene, clock);
        var top = TopLevel.GetTopLevel(scene.Window);
        Settle(clock, 100);

        var busy = DeviceProbe.Busy("datetime-x1: round 1, after", top, left);
        var until = DateTime.UtcNow.AddSeconds(30);
        while (!busy.IsCompleted && DateTime.UtcNow < until)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        Assert.True(busy.IsCompletedSuccessfully);
        var line = busy.Result;
        Assert.StartsWith("datetime-x1: round 1, after: busy: ", line, StringComparison.Ordinal);
        Assert.Matches(@"passes \d+, frames \d+ longest [\d.]+ ms, default post \d+/\d+ (median|none)", line);
        Assert.Matches(@"background post \d+/\d+ .*gc \d+/\d+/\d+, gc pause [\d.]+ ms, heap [\d.]+ MB, tree \d+, peers 0, left page ", line);
        // Headless keeps a page until a collection: here it was alive, out of the tree, with its calendar's scroller watched.
        Assert.Contains("left page alive, out of the tree", line, StringComparison.Ordinal);
        Assert.Contains("its scrollers changed 0 times, then collected", line, StringComparison.Ordinal);

        for (var i = 0; i < 3 && left.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(left.IsAlive);
        Assert.Matches(@", gc \d+/\d+/\d+, gc pause [\d.]+ ms, heap [\d.]+ MB, cpu \d+ ms, ui thread (\d+ ms|n/a), passes 2, tree \d+, peers 0$",
            DeviceProbe.PageExtras(DeviceProbe.Snapshot.Take(), 2, top));
    });

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference OpenAndLeave(Scene scene, TestClock clock)
    {
        var page = new DatetimeCostView(1);
        scene.Window.Content = page;
        Settle(clock, 400);
        scene.Window.Content = null;
        return new WeakReference(page);
    }

    // The page left in the middle of a change: the change ends at once, rather than keep the app drawing until its window is over.
    [AvaloniaFact]
    public void A_Page_Left_During_A_Change_Ends_It_At_Once_And_No_Frame_Is_Asked_For_After() => Run((scene, clock) =>
    {
        var page = new DatetimeCostView(1);
        scene.Window.Content = page;
        Settle(clock, 400);
        var run = new MonthChangeTiming(page.GetVisualDescendants().OfType<AvonicDatetime>().Single()).Run(1, 0);
        for (var i = 0; i < 3; i++)
        {
            clock.At(clock.Now.TotalMilliseconds + 16);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        var left = clock.Now;
        scene.Window.Content = null;
        Drive(clock, run);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        // Drive draws a frame each 16 ms of the test clock: the change ends within the first, not at its window's end.
        Assert.True(clock.Now - left <= TimeSpan.FromMilliseconds(32), $"the change went on for {(clock.Now - left).TotalMilliseconds:0} ms of frames after its page was left");
        Assert.True(run.IsFaulted);
        Assert.Equal(0, MonthChangeTiming.PendingFrames);
    });

    // Round 1 of the baseline run (2026-11-20, 3201219): ten nexts that never left June ("heading n/a", the full 5 s each), then
    // "the calendar left the tree" at the first previous. Here the scroll rest, the 50 ms Background timer that Android runs only
    // in idle time, never runs, as there: the month stays, and the calendar stays in the tree, through the previous too. So the
    // first symptom is the idle time the device didn't give, and the second came from outside the calendar and the timing (the
    // shell's back, or the activity): ShowIndex is the only thing that takes the page away, and the timing calls it only after.
    // The single grid's change waits on no timer, only on frames: it lands with no idle time at all.
    [AvaloniaTheory]
    [InlineData(DatetimeCalendarRendering.ScrolledMonths)]
    [InlineData(DatetimeCalendarRendering.SingleGrid)]
    public void With_No_Idle_Time_The_Calendar_Stays_In_The_Tree(DatetimeCalendarRendering rendering) => Run((scene, clock) =>
    {
        var page = new DatetimeCostView(1, rendering);
        scene.Window.Content = page;
        Settle(clock, 400);
        var datetime = page.GetVisualDescendants().OfType<AvonicDatetime>().Single();
        var detached = 0;
        datetime.DetachedFromVisualTree += (_, _) => detached++;

        var schedule = AvonicConfig.Schedule;
        AvonicConfig.Schedule = (callback, delay) => delay == AvonicDatetime.ScrollRest ? new CancellationTokenSource() : schedule(callback, delay);
        try
        {
            var run = new MonthChangeTiming(datetime, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(400)).Run(10, 1);
            Drive(clock, run);

            Assert.True(run.IsCompletedSuccessfully, run.Exception?.ToString());
            Assert.Equal(0, detached);
            var changes = run.Result;
            if (rendering == DatetimeCalendarRendering.ScrolledMonths)
            {
                Assert.All(changes.Take(10), c => Assert.Equal(("June 2023", "July 2023", (double?)null), (c.From, c.To, c.HeadingMs)));
                Assert.Equal(("June 2023", "May 2023"), (changes[10].From, changes[10].To));
            }
            else
            {
                Assert.All(changes, c => Assert.NotNull(c.HeadingMs));
                Assert.Equal(("April 2024", "March 2024"), (changes[9].To, changes[10].To));
            }
        }
        finally
        {
            AvonicConfig.Schedule = schedule;
        }
    });

    // As the rounds do: a second timing, on a new page, after the first one's page has gone.
    [AvaloniaFact]
    public void A_Second_Timing_On_A_New_Page_Sees_Its_Months_Change() => Run((scene, clock) =>
    {
        foreach (var round in new[] { 1, 2 })
        {
            var page = new DatetimeCostView(1);
            scene.Window.Content = page;
            Settle(clock, 400);
            var run = new MonthChangeTiming(page.GetVisualDescendants().OfType<AvonicDatetime>().Single(), TimeSpan.FromMilliseconds(20)).Run(2, 0);
            Drive(clock, run);

            Assert.Equal(["June 2023 to July 2023", "July 2023 to August 2023"], run.Result.Select(c => $"{c.From} to {c.To}"));
            Assert.All(run.Result, c => Assert.NotNull(c.HeadingMs));
            scene.Window.Content = null;
            Settle(clock, 100);
        }
    });

    [AvaloniaFact]
    public void A_Background_Post_Is_Timed() => Run((scene, clock) =>
    {
        var post = PerfChecks.BackgroundPostMs(TimeSpan.FromSeconds(10));
        Drive(clock, post);

        Assert.True(post.Result is >= 0);
    });

    [Fact]
    public void The_Check_Warns_Of_A_Frame_Loop_A_Live_Page_And_No_Idle_Time()
    {
        Assert.Equal(
            ["datetime-month: round 1, check: 0 frames asked for, page collected, background post 0.4 ms before and 0.6 ms after, accessibility off"],
            PerfChecks.Lines("datetime-month", 1, 0, true, 0.4, 0.6, "accessibility off"));

        var bad = PerfChecks.Lines("datetime-month", 2, 1, false, 0.4, null, null).ToArray();
        Assert.Equal("datetime-month: round 2, check: 1 frames asked for, page still alive, background post 0.4 ms before and over 10 s after", bad[0]);
        Assert.Equal(3, bad.Count(l => l.Contains(", warning: ", StringComparison.Ordinal)));
        Assert.All(bad, l => Assert.DoesNotMatch(ChangeLine(), l));
    }

    // The trap of #258's Pixel runs: on battery the screen locked after its timeout and Android moved the app to the little cores.
    [Fact]
    public void The_Check_Says_The_Cpuset_And_Warns_When_The_App_Isnt_In_Front()
    {
        Assert.Equal(
            ["datetime-month: round 1, check: 0 frames asked for, page collected, background post 0.4 ms before and 0.6 ms after, accessibility off, cpuset /top-app"],
            PerfChecks.Lines("datetime-month", 1, 0, true, 0.4, 0.6, "accessibility off", "/top-app", null));

        var locked = PerfChecks.Lines("datetime-month", 2, 0, true, 0.4, 0.6, null, "/background", "screen off, keyguard locked").ToArray();
        Assert.Equal("datetime-month: round 2, check: 0 frames asked for, page collected, background post 0.4 ms before and 0.6 ms after, cpuset /background, screen off, keyguard locked", locked[0]);
        Assert.Equal("datetime-month: round 2, warning: the app wasn't in front (cpuset /background, screen off, keyguard locked): Android gives it the little cores, and every time is slower; keep the screen on (docs/infra/performance/README.md)", locked[1]);
        Assert.Equal(2, locked.Length);

        // Each alone is enough; where there's no cpuset file (the desktop) and nothing to say, nothing.
        Assert.Contains(PerfChecks.Lines("m", 1, 0, true, 0.4, 0.6, null, "/top-app", "activity not resumed"), l => l.Contains(", warning: the app wasn't in front (cpuset /top-app, activity not resumed)", StringComparison.Ordinal));
        Assert.Contains(PerfChecks.Lines("m", 1, 0, true, 0.4, 0.6, null, "/foreground", null), l => l.Contains(", warning: the app wasn't in front (cpuset /foreground)", StringComparison.Ordinal));
        Assert.Single(PerfChecks.Lines("m", 1, 0, true, 0.4, 0.6, null, null, null));
    }

    // ── The lines ────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_Change_And_A_Round_Are_Lines_That_Table_Js_Reads_And_The_Page_Line_Does_Not_Take()
    {
        var known = new MonthChange(true, 3, "August 2023", "September 2023", 23.44, 412, FrameGaps.Of([30, 8, 8.5, 40, 8]));
        var unknown = new MonthChange(false, 1, null, null, null, null, FrameGaps.Of([]));

        var line = known.Line("datetime-month", 2);
        Assert.Equal("datetime-month: round 2, next 3, August 2023 to September 2023, first 23.4 ms, heading 412.0 ms, 5 frames, longest 40.0 ms, gap median 8.5 ms, 2 over 16.7 ms, 1 over 33 ms", line);
        Assert.Matches(ChangeLine(), line);
        Assert.Matches(ChangeLine(), unknown.Line("datetime-month", 1));
        Assert.DoesNotMatch(PageLine(), line);

        var summary = MonthChangeTiming.Summary("datetime-month", 2, [known, known with { FirstMs = 30, Gaps = FrameGaps.Of([10, 51]) }]);
        Assert.Equal(
            "datetime-month: round 2, 2 changes, first median 26.7 max 30.0 ms, heading median 412.0 max 412.0 ms, longest median 45.5 max 51.0 ms, gap median 19.5 ms, 7 frames, 3 over 16.7 ms, 2 over 33 ms",
            summary);
        Assert.DoesNotMatch(ChangeLine(), summary);
        Assert.DoesNotMatch(PageLine(), summary);
    }

    [Fact]
    public void A_Change_With_No_Frames_Is_Left_Out_Of_The_Rounds_Gap_Median()
    {
        var changes = new[] { new MonthChange(true, 1, null, null, null, null, FrameGaps.Of([8, 9])), new MonthChange(true, 2, null, null, null, null, FrameGaps.Of([])) };

        Assert.Contains("gap median 8.5 ms", MonthChangeTiming.Summary("datetime-month", 1, changes), StringComparison.Ordinal);
    }

    // The lines as the run writes them, read by the script that tabulates them, so that the two can't drift apart.
    [Fact]
    public void Table_Js_Reads_The_Lines_A_Run_Writes()
    {
        var page = PerformanceMonitor.Format("datetime-x1", 180, 650, 98, 174, 0, 0);
        string[] Run(double longest) =>
        [
            "AVONIC-PERF start: Test, Arm64, 9 cores, .NET",
            "AVONIC-PERF " + page + ", gc 1/0/0, gc pause 1.2 ms, heap 50.0 MB, cpu 100 ms, ui thread n/a, passes 3, tree 700, peers 0",
            "AVONIC-PERF datetime-x1: round 1, after: busy: 2.0 s, cpu 40 ms, ui thread n/a, passes 0, frames 120 longest 17.0 ms, default post 20/20 median 0.1 max 0.5 ms, background post 20/20 median 0.2 max 0.9 ms, gc 0/0/0, gc pause 0.0 ms, heap 50.0 MB, tree 300, peers 0, left page gone",
            .. Enumerable.Range(1, 2).SelectMany(round => new[]
            {
                "AVONIC-PERF " + new MonthChange(true, 1, "June 2023", "July 2023", 20, 400, FrameGaps.Of([10, longest, 8])).Line("datetime-month", round),
                "AVONIC-PERF " + new MonthChange(false, 1, "July 2023", "June 2023", null, null, FrameGaps.Of([8])).Line("datetime-month", round),
                "AVONIC-PERF " + MonthChangeTiming.Summary("datetime-month", round, []),
            }.Concat(PerfChecks.Lines("datetime-month", round, 0, true, 0.4, round == 2 ? 900 : 0.5, null).Select(l => "AVONIC-PERF " + l))),
        ];
        var before = Path.GetTempFileName();
        var after = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(before, Run(40));
            File.WriteAllLines(after, Run(20));

            var one = Node(before);
            Assert.Contains("| datetime-x1 | 650 | 180 | 180 | 0 |", one, StringComparison.Ordinal);
            Assert.Contains("datetime-month: month changes", one, StringComparison.Ordinal);
            Assert.Contains("| Changes | 2 | 2 | 2 |", one, StringComparison.Ordinal);
            Assert.Contains("| Longest gap, most | 40 | 40 | 40 |", one, StringComparison.Ordinal);
            Assert.Contains("| Gap between frames, median | 9.0 | 9.0 | 9.0 |", one, StringComparison.Ordinal);
            Assert.Contains("| Gaps over 33 ms, per round | 1 | 1 | 1 |", one, StringComparison.Ordinal);

            var two = Node(before, after);
            Assert.Contains("| **all** | | | 180 | 180 | 1.00 |", two, StringComparison.Ordinal);
            Assert.Contains("| Longest gap, median | 24 | 14 | 0.58 |", two, StringComparison.Ordinal);

            // The checks, a warning in bold, and none of them read as a change.
            Assert.Contains("- datetime-month: round 1, check: 0 frames asked for, page collected, background post 0.4 ms before and 0.5 ms after", one, StringComparison.Ordinal);
            Assert.Contains("- **datetime-month: round 2, warning: a Background post waited 900.0 ms", one, StringComparison.Ordinal);
            Assert.Contains("| Changes | 2 | 2 | 2 |", one, StringComparison.Ordinal);
            Assert.Contains("after: checks", two, StringComparison.Ordinal);

            // DeviceProbe's busy line is listed with the checks; its figures on the page line don't change the table.
            Assert.Contains("- datetime-x1: round 1, after: busy: 2.0 s, cpu 40 ms", one, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(before);
            File.Delete(after);
        }
    }

    private static string Node(params string[] runs)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Avonic.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var start = new ProcessStartInfo("node") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(Path.Combine(dir.FullName, "tools", "page-costs", "table.js"));
        foreach (var run in runs)
        {
            start.ArgumentList.Add(run);
        }

        Process process;
        try
        {
            process = Process.Start(start)!;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Assert.Skip("node isn't on the PATH");
            throw;
        }

        using (process)
        {
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, error);
            return output.Replace("\r\n", "\n", StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_Gap_Counts_Only_Once_It_Is_Over_A_Frame_And_Over_Two()
    {
        var gaps = FrameGaps.Of([16.6, 16.7, 16.8, 33.3, 33.4]);

        Assert.Equal(new FrameGaps(5, 33.4, 16.8, 4, 1), gaps);
    }

    /// <summary>The app's loop: the test clock a frame at a time, and a frame drawn, until <paramref name="task"/> ends.</summary>
    private static void Drive(TestClock clock, Task task)
    {
        var until = DateTime.UtcNow.AddSeconds(30);
        while (!task.IsCompleted && DateTime.UtcNow < until)
        {
            clock.At(clock.Now.TotalMilliseconds + 16);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        Assert.True(task.IsCompleted, "the run never ended");
    }
}
