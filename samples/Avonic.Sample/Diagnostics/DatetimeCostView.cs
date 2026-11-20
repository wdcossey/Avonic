using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Diagnostics;

/// <summary>
/// A page of <see cref="Count"/> plain datetimes, for the page-cost rounds only (<c>datetime-x1</c>, <c>-x2</c>, <c>-x4</c>): the cost
/// of one datetime, where the sample's own page mixes eight kinds. It has no <see cref="SamplePageAttribute"/>, so the sample's list
/// doesn't show it, and it's a view, not a <c>...Page</c>, as every type so named must have one. Each is the default presentation,
/// a calendar and a time, with its value and locale pinned, so that every run shows the same month, June 2023, and no today ring.
/// docs/infra/performance/README.md, "Page costs on a device".
/// </summary>
public sealed class DatetimeCostView : UserControl
{
    /// <summary>The value each datetime shows, the sample page's.</summary>
    public const string PinnedValue = "2023-06-15T13:47:00";

    /// <summary>The locale each datetime shows, so that its month names read the same on any device.</summary>
    public const string PinnedLocale = "en-US";

    public DatetimeCostView(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        Count = count;
        var stack = new StackPanel { Spacing = 16, Margin = new Thickness(16) };
        for (var i = 0; i < count; i++)
        {
            stack.Children.Add(new AvonicDatetime { Value = PinnedValue, Locale = PinnedLocale });
        }

        // As the sample's page is laid out: a scroller, 16 in, 16 between.
        Content = new ScrollViewer { Content = stack };
    }

    /// <summary>How many datetimes it holds.</summary>
    public int Count { get; }

    /// <summary>The page's title in the page-cost lines: <c>datetime-x1</c> and so on.</summary>
    public static string Title(int count) => $"datetime-x{count}";
}
