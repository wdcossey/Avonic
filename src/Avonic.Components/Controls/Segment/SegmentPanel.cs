using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-segment</c>'s <c>display: grid</c>, one row of columns, as Chromium sizes them (measured in
/// <c>docs/components/ion-segment/PLAN.md</c>, S11 and S13):
/// <list type="bullet">
///   <item>Not scrollable, <c>grid-auto-columns: minmax(auto, MaxColumnWidth)</c>: each column starts at its child's
///   <c>MinWidth</c>, its minimum contribution, and they grow by equal amounts to fill the row, each stopping at
///   <see cref="MaxColumnWidth"/>; <c>justify-content: center</c> centres them, overflowing on both sides when they don't fit.</item>
///   <item>Scrollable, <c>minmax(min-content, 1fr)</c>: each column is the larger of its child's content width and an equal
///   share of the row, and they start at the start.</item>
/// </list>
/// Every child is as high as the highest, as <c>align-items: stretch</c> makes them.
/// </summary>
public sealed class SegmentPanel : Panel
{
    /// <summary>The columns' most, as <c>minmax(auto, 360px)</c> is md's. Infinite by default.</summary>
    public static readonly StyledProperty<double> MaxColumnWidthProperty =
        AvaloniaProperty.Register<SegmentPanel, double>(nameof(MaxColumnWidth), double.PositiveInfinity);

    private double[] _columns = [];
    private double _start;

    static SegmentPanel()
    {
        AffectsMeasure<SegmentPanel>(MaxColumnWidthProperty);
    }

    public double MaxColumnWidth
    {
        get => GetValue(MaxColumnWidthProperty);
        set => SetValue(MaxColumnWidthProperty, value);
    }


    protected override Size MeasureOverride(Size availableSize)
    {
        var children = Children.Where(child => child.IsVisible).ToList();
        // A scroller around the panel doesn't pass the segment's width on, and the fr columns are a share of it.
        var segment = this.FindAncestorOfType<AvonicSegment>();
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : segment?.RowWidth ?? double.PositiveInfinity;
        var scrollable = segment?.Scrollable == true;

        // The base sizes: what each column can't be less than.
        var bases = new double[children.Count];
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (scrollable)
            {
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                bases[i] = child.DesiredSize.Width;
            }
            else
            {
                bases[i] = child.MinWidth + child.Margin.Left + child.Margin.Right;
            }
        }

        if (!scrollable && !double.IsFinite(width))
        {
            // With no width to fill, as under a max-content constraint, each column grows to its limit: MaxColumnWidth, or with
            // none, its content's width.
            for (var i = 0; i < children.Count; i++)
            {
                children[i].Measure(new Size(double.PositiveInfinity, availableSize.Height));
                bases[i] = Math.Max(bases[i], double.IsFinite(MaxColumnWidth) ? MaxColumnWidth : children[i].DesiredSize.Width);
            }
        }

        _columns = scrollable ? Flexible(bases, width) : Grown(bases, width, MaxColumnWidth);
        var total = _columns.Sum();
        _start = scrollable || !double.IsFinite(width) ? 0 : (width - total) / 2;

        var height = 0.0;
        for (var i = 0; i < children.Count; i++)
        {
            children[i].Measure(new Size(_columns[i], availableSize.Height));
            height = Math.Max(height, children[i].DesiredSize.Height);
        }

        return new Size(scrollable && double.IsFinite(width) ? Math.Max(width, total) : double.IsFinite(width) ? width : total, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = Children.Where(child => child.IsVisible).ToList();
        var x = _start;
        for (var i = 0; i < children.Count && i < _columns.Length; i++)
        {
            children[i].Arrange(new Rect(x, 0, _columns[i], finalSize.Height));
            x += _columns[i];
        }

        return finalSize;
    }

    /// <summary>
    /// CSS Grid's "maximize tracks": the free space goes to every column by equal amounts, and a column stops growing at its
    /// limit, the larger of its base and <paramref name="max"/>.
    /// </summary>
    private static double[] Grown(double[] bases, double width, double max)
    {
        var columns = (double[])bases.Clone();
        if (!double.IsFinite(width))
        {
            return columns;
        }

        var free = width - columns.Sum();
        var growing = Enumerable.Range(0, columns.Length).Where(i => Math.Max(bases[i], max) > columns[i]).ToList();
        while (free > 1e-9 && growing.Count > 0)
        {
            var share = free / growing.Count;
            var limited = growing.Where(i => Math.Max(bases[i], max) - columns[i] <= share).ToList();
            if (limited.Count == 0)
            {
                foreach (var i in growing)
                {
                    columns[i] += share;
                }

                break;
            }

            foreach (var i in limited)
            {
                free -= Math.Max(bases[i], max) - columns[i];
                columns[i] = Math.Max(bases[i], max);
                growing.Remove(i);
            }
        }

        return columns;
    }

    /// <summary>
    /// CSS Grid's flexible lengths for <c>minmax(min-content, 1fr)</c>: one fr is an equal share of the row, and a column whose
    /// content is wider than its share keeps its content's width and leaves the rest to the others.
    /// </summary>
    private static double[] Flexible(double[] bases, double width)
    {
        var columns = (double[])bases.Clone();
        if (!double.IsFinite(width) || columns.Length == 0 || columns.Sum() >= width)
        {
            return columns;
        }

        var flexible = Enumerable.Range(0, columns.Length).ToList();
        var fixedWidth = 0.0;
        while (true)
        {
            var fr = (width - fixedWidth) / flexible.Count;
            var inflexible = flexible.Where(i => bases[i] > fr).ToList();
            if (inflexible.Count == 0)
            {
                foreach (var i in flexible)
                {
                    columns[i] = fr;
                }

                return columns;
            }

            foreach (var i in inflexible)
            {
                fixedWidth += bases[i];
                flexible.Remove(i);
            }

            if (flexible.Count == 0)
            {
                return columns;
            }
        }
    }
}
