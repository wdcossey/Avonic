namespace Avonic.Sample.Diagnostics;

/// <summary>
/// The pages a <c>-p:AvonicPerf=true</c> build opens in each round: every sample page but the device page, whose benchmark is its
/// own, then the perf-only <see cref="DatetimeCostView"/>s, which the sample's list doesn't show. A filter, from
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

    /// <summary>
    /// The pages to open, in order. <paramref name="only"/> is a list of terms split by commas or spaces: a page is kept when its
    /// title contains any of them, ignoring case. Empty or null keeps every page.
    /// </summary>
    public static IReadOnlyList<SamplePageEntry> Select(IEnumerable<SamplePageEntry> samplePages, string? only)
    {
        var all = samplePages
            .Where(page => page.Title != "device")
            .Concat(DatetimeCounts.Select(count => new SamplePageEntry(DatetimeCostView.Title(count), () => new DatetimeCostView(count))));
        var terms = Terms(only);
        return terms.Length == 0
            ? [.. all]
            : [.. all.Where(page => terms.Any(term => page.Title.Contains(term, StringComparison.OrdinalIgnoreCase)))];
    }

    /// <summary>The filter's terms.</summary>
    public static string[] Terms(string? only) =>
        only?.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
}
