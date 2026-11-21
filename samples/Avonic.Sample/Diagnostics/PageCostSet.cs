using Avonic.Components.Controls;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// The pages a <c>-p:AvonicPerf=true</c> build opens in each round: every sample page but the device page, whose benchmark is its
/// own, then the perf-only <see cref="DatetimeCostView"/>s, which the sample's list doesn't show: the scrolled calendar (Ionic's, its months either
/// side of the working one made after its first frames since #248's spike B, so 297 elements for one, not the 725 of earlier runs), then
/// the same in <see cref="DatetimeCalendarRendering.SingleGrid"/> (<c>-grid</c>), so that one run compares them. A filter, from
/// <see cref="OnlyVariable"/> on desktop or the intent extra of the same name on Android, keeps a part of them.
/// docs/infra/performance/README.md, "Page costs on a device".
/// </summary>
public static class PageCostSet
{
    /// <summary>The environment variable, and the Android intent extra, that name the pages to open.</summary>
    public const string OnlyVariable = "AVONIC_PERF_ONLY";

    /// <summary>How many datetimes each perf-only datetime page holds.</summary>
    public static IReadOnlyList<int> DatetimeCounts { get; } = [1, 2, 4];

    /// <summary>The page whose month changes are timed, after its open: the one with a single datetime.</summary>
    public static string MonthChangePage { get; } = DatetimeCostView.Title(1);

    /// <summary>The single grid's page whose month changes are timed: the one with a single datetime.</summary>
    public static string GridMonthChangePage { get; } = DatetimeCostView.Title(1, DatetimeCalendarRendering.SingleGrid);

    /// <summary>The label of the scrolled calendar's month-change lines, as the baseline has it.</summary>
    public const string MonthChangeLabel = "datetime-month";

    /// <summary>The label of the single grid's month-change lines.</summary>
    public const string GridMonthChangeLabel = "datetime-month-grid";

    /// <summary>The label of the month changes timed on a page, or null for a page whose month changes aren't timed.</summary>
    public static string? MonthChangeLabelOf(string title) =>
        title == MonthChangePage ? MonthChangeLabel : title == GridMonthChangePage ? GridMonthChangeLabel : null;

    /// <summary>
    /// The pages to open, in order. <paramref name="only"/> is a list of terms split by commas or spaces: a page is kept when its
    /// title contains any of them, ignoring case, or, for a term that starts with <c>=</c>, is the rest of it. Empty or null keeps
    /// every page.
    /// </summary>
    public static IReadOnlyList<SamplePageEntry> Select(IEnumerable<SamplePageEntry> samplePages, string? only)
    {
        var all = samplePages
            .Where(page => page.Title != "device")
            .Concat(DatetimeCounts.Select(count => new SamplePageEntry(DatetimeCostView.Title(count), () => new DatetimeCostView(count))))
            .Concat(DatetimeCounts.Select(count => new SamplePageEntry(
                DatetimeCostView.Title(count, DatetimeCalendarRendering.SingleGrid),
                () => new DatetimeCostView(count, DatetimeCalendarRendering.SingleGrid))));
        var terms = Terms(only);
        return terms.Length == 0
            ? [.. all]
            : [.. all.Where(page => terms.Any(term => Matches(page.Title, term)))];
    }

    /// <summary>The filter's terms.</summary>
    public static string[] Terms(string? only) =>
        only?.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

    private static bool Matches(string title, string term) =>
        term.StartsWith('=')
            ? title.Equals(term[1..], StringComparison.OrdinalIgnoreCase)
            : title.Contains(term, StringComparison.OrdinalIgnoreCase);
}
