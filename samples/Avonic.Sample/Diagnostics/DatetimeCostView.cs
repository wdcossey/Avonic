using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// A page of <see cref="Count"/> plain datetimes, for the page-cost rounds only (<c>datetime-x1</c>, <c>-x2</c>, <c>-x4</c>): the cost
/// of one datetime, where the sample's own page mixes eight kinds. It has no <see cref="SamplePageAttribute"/>, so the sample's list
/// doesn't show it, and it's a view, not a <c>...Page</c>, as every type so named must have one. Each is the default presentation,
/// a calendar and a time, with its value and locale pinned, so that every run shows the same month, June 2023, and no today ring.
/// The <c>-grid</c> pages (<c>datetime-x1-grid</c> and so on) are the same datetimes in <see cref="DatetimeCalendarRendering.SingleGrid"/>,
/// to compare with (#248, spike A). The <c>-eager</c> and <c>-press</c> pages (<c>datetime-x1-eager</c> and so on) are the scrolled
/// calendar with the months either side of the working one made at once, as before, and only when first needed, against the default,
/// a frame after the first (#248, spike B, <see cref="DatetimeNeighbours"/>). docs/infra/performance/README.md, "Page costs on a device".
/// </summary>
public sealed class DatetimeCostView : UserControl
{
    /// <summary>The value each datetime shows, the sample page's.</summary>
    public const string PinnedValue = "2023-06-15T13:47:00";

    /// <summary>The locale each datetime shows, so that its month names read the same on any device.</summary>
    public const string PinnedLocale = "en-US";

    /// <summary>
    /// The <see cref="AppContext"/> data that picks, for the datetimes made while it's set, when the scrolled calendar makes the months
    /// either side (the library's <c>AvonicDatetime.NeighbourMonthsSwitch</c>): a measuring switch of spike B's, not an API.
    /// </summary>
    public const string NeighbourMonthsSwitch = "Avonic.Datetime.NeighbourMonths";

    public DatetimeCostView(int count, DatetimeCalendarRendering rendering = DatetimeCalendarRendering.ScrolledMonths,
        DatetimeNeighbours neighbours = DatetimeNeighbours.Default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        Count = count;
        Rendering = rendering;
        Neighbours = neighbours;
        var stack = new StackPanel { Spacing = 16, Margin = new Thickness(16) };
        // Set only for these datetimes, and put back as it was after, so a value the app set stays.
        var previous = AppContext.GetData(NeighbourMonthsSwitch);
        if (neighbours != DatetimeNeighbours.Default)
        {
            AppContext.SetData(NeighbourMonthsSwitch, neighbours.ToString());
        }

        try
        {
            for (var i = 0; i < count; i++)
            {
                var datetime = new AvonicDatetime { Value = PinnedValue, Locale = PinnedLocale };
                if (rendering != DatetimeCalendarRendering.ScrolledMonths)
                {
                    datetime.CalendarRendering = rendering;
                }

                stack.Children.Add(datetime);
            }
        }
        finally
        {
            if (neighbours != DatetimeNeighbours.Default)
            {
                AppContext.SetData(NeighbourMonthsSwitch, previous);
            }
        }

        // As the sample's page is laid out: a scroller, 16 in, 16 between.
        Content = new ScrollViewer { Content = stack };
    }

    /// <summary>How many datetimes it holds.</summary>
    public int Count { get; }

    /// <summary>How each draws its calendar.</summary>
    public DatetimeCalendarRendering Rendering { get; }

    /// <summary>When the scrolled calendar makes the months either side.</summary>
    public DatetimeNeighbours Neighbours { get; }

    /// <summary>
    /// The page's title in the page-cost lines: <c>datetime-x1</c> and so on, and <c>datetime-x1-grid</c> and so on for
    /// <see cref="DatetimeCalendarRendering.SingleGrid"/>, <c>-eager</c>, <c>-press</c> and <c>-idle</c> for <see cref="DatetimeNeighbours"/>.
    /// </summary>
    public static string Title(int count, DatetimeCalendarRendering rendering = DatetimeCalendarRendering.ScrolledMonths,
        DatetimeNeighbours neighbours = DatetimeNeighbours.Default) =>
        rendering == DatetimeCalendarRendering.SingleGrid ? $"datetime-x{count}-grid"
        : neighbours == DatetimeNeighbours.AtOnce ? $"datetime-x{count}-eager"
        : neighbours == DatetimeNeighbours.WhenNeeded ? $"datetime-x{count}-press"
        : neighbours == DatetimeNeighbours.AfterFirstFrame ? $"datetime-x{count}-idle"
        : $"datetime-x{count}";
}

/// <summary>When a perf page's scrolled calendars make the months either side of the working one (#248, spike B).</summary>
public enum DatetimeNeighbours
{
    /// <summary>The library's default: a frame after the calendar is first drawn, or sooner when needed.</summary>
    Default,

    /// <summary>With the working month, as Ionic's and as before spike B: the <c>-eager</c> pages.</summary>
    AtOnce,

    /// <summary>Only when first needed: a press on the calendar, a scroll, a change of month. The <c>-press</c> pages.</summary>
    WhenNeeded,

    /// <summary>
    /// After the first frames, two weeks a frame, or sooner when needed: the default, named, for the <c>datetime-x1-idle</c> page,
    /// whose first swipe is timed, so that the plain <c>datetime-x1</c>'s month changes are timed as in the runs kept before.
    /// </summary>
    AfterFirstFrame,
}
