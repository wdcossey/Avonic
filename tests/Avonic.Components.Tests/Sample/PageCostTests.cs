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
        var expected = Titles(SamplePages).Where(t => t != "device").Concat(["datetime-x1", "datetime-x2", "datetime-x4"]);

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

        Assert.Equal(["datetime-x1"], Titles(PageCostSet.Select(SamplePages, "DATETIME-X1")));
        Assert.Equal(["ion-button", "datetime-x2"], Titles(PageCostSet.Select(SamplePages, "ion-button, datetime-x2")));
        Assert.Equal(["datetime-x1", "datetime-x2", "datetime-x4"], Titles(PageCostSet.Select(SamplePages, "datetime-x")));
        Assert.Empty(PageCostSet.Select(SamplePages, "no-such-page"));
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
        });
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
