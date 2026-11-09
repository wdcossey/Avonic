using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>ion-row</c>'s <c>display: flex; flex-wrap: wrap</c>, for <see cref="AvonicCol"/>s. It's the flexbox algorithm of
/// sections 9.3 and 9.7 for what a column can be (see <see cref="ColumnKind"/>), with percentages of the row's width. A child
/// that isn't a column is <c>flex: 0 1 auto</c>, or grows by its <see cref="FlexRow.FlexProperty"/> if it has one, as a grid's
/// <c>flex: 1</c> does. Each line is as tall as its tallest child, and every child is arranged that tall, which is
/// <c>align-items: stretch</c>: a child's own <c>VerticalAlignment</c> is then <c>align-self</c>. Avalonia mirrors it in RTL,
/// as it does any panel, so the start is the left here.
/// </summary>
public sealed class RowPanel : Panel
{
    /// <summary>Chromium lays out in 1/64 px, so three columns of 33.33% fit on a line.</summary>
    private const double Tolerance = 0.01;

    private readonly List<Item> _items = [];
    private readonly List<Line> _lines = [];
    private double _resolvedFor = double.NaN;

    static RowPanel()
    {
        FlexRow.FlexProperty.Changed.AddClassHandler<Control>((control, _) =>
            (control.GetVisualParent() as RowPanel ?? control.GetVisualParent()?.GetVisualParent() as RowPanel)?.InvalidateMeasure());
    }

    private sealed class Item(Control child, AvonicCol? column)
    {
        public Control Child { get; } = child;

        public AvonicCol? Column { get; } = column;

        /// <summary><c>flex-grow</c>.</summary>
        public double Grow { get; set; }

        /// <summary><c>flex-shrink: 1</c>, rather than 0.</summary>
        public bool Shrinks { get; set; }

        /// <summary>The <c>min-width: auto</c> isn't known until it's needed, and most rows don't need it.</summary>
        public bool HasContentMinimum { get; set; }

        /// <summary>The outer flex base size: the border box and the margins.</summary>
        public double Base { get; set; }

        /// <summary>What the child takes at a width of 0: padding, borders and margins.</summary>
        public double Box { get; set; }

        /// <summary>The outer width on one line.</summary>
        public double MaxContent { get; set; }

        public double? Minimum { get; set; }

        public double Maximum { get; set; } = double.PositiveInfinity;

        /// <summary>The column's offset: a margin at the start.</summary>
        public double Lead { get; set; }

        /// <summary>Push or pull: <c>position: relative</c>, which moves nothing else.</summary>
        public double Shift { get; set; }

        public double Size { get; set; }

        /// <summary>The size before clamping, in the pass that's resolving.</summary>
        public double Target { get; set; }

        public bool Frozen { get; set; }

        /// <summary>Clamped by the maximum, then the minimum, which wins.</summary>
        public double Clamp(double size) => Math.Max(Minimum ?? Box, Math.Min(size, Maximum));

        public double Hypothetical => Clamp(Base);
    }

    private readonly record struct Line(int Start, int End)
    {
        public double Height { get; init; }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        var bounded = !double.IsInfinity(width);
        var oneLine = new Size(double.PositiveInfinity, double.PositiveInfinity);

        _items.Clear();
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            // A presenter makes its column here, so it's known before measuring.
            child.ApplyTemplate();

            // display: none, as a column an ItemTemplate hid inside its container is.
            if (child is ContentPresenter { Child.IsVisible: false })
            {
                continue;
            }
            var column = ColumnIn(child);

            // A sized column's width is a fraction of the row, so its content only needs measuring at that width.
            var oneLineNeeded = !bounded || column is not { Kind: ColumnKind.Fixed };
            if (oneLineNeeded)
            {
                child.Measure(oneLine);
            }

            var item = new Item(child, column) { MaxContent = oneLineNeeded ? child.DesiredSize.Width : 0, Box = MinContent.Box(child) };
            Prepare(item, width, bounded);
            _items.Add(item);
        }

        Resolve(width);

        // A column's min-content only matters if it got less than one line, and most don't, so most don't look for their words.
        // It has to be found before anything is measured again, as it reads the one-line sizes.
        for (var pass = 0; bounded && pass < _items.Count; pass++)
        {
            var again = false;
            foreach (var item in _items)
            {
                if (item.HasContentMinimum && item.Minimum is null && item.Size < item.MaxContent - Tolerance)
                {
                    item.Minimum = Math.Min(Math.Max(item.Box, MinContent.OfItem(item.Child)), item.Maximum);
                    again = true;
                }
            }

            if (!again)
            {
                break;
            }

            Resolve(width);
        }

        double height = 0, widest = 0;
        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            double lineHeight = 0, lineWidth = 0;
            for (var j = line.Start; j < line.End; j++)
            {
                var item = _items[j];
                item.Child.Measure(new Size(item.Size, double.PositiveInfinity));
                lineHeight = Math.Max(lineHeight, item.Child.DesiredSize.Height);
                lineWidth += item.Lead + item.Size;
            }

            _lines[i] = line with { Height = lineHeight };
            height += lineHeight;
            widest = Math.Max(widest, lineWidth);
        }

        // A row is a block, as wide as what it's in. Measure clamps this to what's available.
        return new Size(bounded ? width : widest, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Math.Abs(finalSize.Width - _resolvedFor) > Tolerance && !double.IsInfinity(_resolvedFor))
        {
            foreach (var item in _items)
            {
                Prepare(item, finalSize.Width, true);
            }

            Resolve(finalSize.Width);
            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                double lineHeight = 0;
                for (var j = line.Start; j < line.End; j++)
                {
                    lineHeight = Math.Max(lineHeight, _items[j].Child.DesiredSize.Height);
                }

                _lines[i] = line with { Height = lineHeight };
            }
        }

        double y = 0;
        foreach (var line in _lines)
        {
            double x = 0;
            for (var j = line.Start; j < line.End; j++)
            {
                var item = _items[j];
                x += item.Lead;
                item.Child.Arrange(new Rect(x + item.Shift, y, item.Size, line.Height));
                x += item.Size;
            }

            y += line.Height;
        }

        return finalSize;
    }

    /// <summary>What <paramref name="child"/> is as a flex item, from <c>col.css</c> and the column's inline style.</summary>
    private static void Prepare(Item item, double width, bool bounded)
    {
        var column = item.Column;
        var margins = MinContent.Horizontal(item.Child.Margin) + (item.Child is ContentPresenter { Child: { } inner } ? MinContent.Horizontal(inner.Margin) : 0);
        var flex = column is null ? FlexRow.GetFlex(item.Child is ContentPresenter { Child: { } slotted } ? slotted : item.Child) : 0;
        var kind = column?.Kind ?? (flex > 0 ? ColumnKind.Grow : ColumnKind.Content);

        item.Lead = bounded && column is not null ? column.OffsetFraction * width : 0;
        item.Shift = bounded && column is not null ? column.ShiftFraction * width : 0;
        item.Grow = 0;
        item.Shrinks = false;
        item.HasContentMinimum = false;
        item.Maximum = double.PositiveInfinity;

        // A percentage of a row with no width is auto, and so is a basis of 0 with nothing to grow into.
        if (!bounded)
        {
            item.Base = item.MaxContent;
            item.Minimum = item.MaxContent;
            return;
        }

        switch (kind)
        {
            case ColumnKind.Grow:
                // flex: 1 1 0, and a column's max-width: 100%. min-width: auto is no more than its width: 100%.
                item.Base = item.Box;
                item.Grow = column is null ? flex : 1;
                item.Shrinks = true;
                item.HasContentMinimum = true;
                item.Maximum = column is null ? double.PositiveInfinity : width + margins;
                break;

            case ColumnKind.Fixed:
                // flex: 0 0 n%; width: n%; max-width: n%, of the border box, which can't be less than its padding.
                item.Base = Math.Max(column!.Fraction * width + margins, item.Box);
                item.Minimum = item.Base;
                item.Maximum = item.Base;
                break;

            case ColumnKind.Auto:
                // flex: 0 0 auto, and col.css's max-width: 100%.
                item.Base = item.MaxContent;
                item.Minimum = item.Box;
                item.Maximum = Math.Max(width + margins, item.Box);
                break;

            default:
                // flex: 0 1 auto, with no maximum.
                item.Base = item.MaxContent;
                item.Shrinks = true;
                item.HasContentMinimum = true;
                break;
        }
    }

    /// <summary>Sections 9.3, collecting the lines, and 9.7, resolving each line's flexible lengths.</summary>
    private void Resolve(double width)
    {
        _resolvedFor = width;
        _lines.Clear();

        if (double.IsInfinity(width))
        {
            foreach (var item in _items)
            {
                item.Size = item.Base;
            }

            _lines.Add(new Line(0, _items.Count));
            return;
        }

        var start = 0;
        double used = 0;
        for (var i = 0; i < _items.Count; i++)
        {
            var outer = _items[i].Lead + _items[i].Hypothetical;
            if (i > start && used + outer > width + Tolerance)
            {
                _lines.Add(new Line(start, i));
                start = i;
                used = 0;
            }

            used += outer;
        }

        if (start < _items.Count)
        {
            _lines.Add(new Line(start, _items.Count));
        }

        foreach (var line in _lines)
        {
            ResolveLine(line, width);
        }
    }

    private void ResolveLine(Line line, double width)
    {
        double used = 0;
        for (var j = line.Start; j < line.End; j++)
        {
            used += _items[j].Lead + _items[j].Hypothetical;
        }

        // 9.7 step 1 to 3: grow or shrink, and freeze what can't, or is already past its base the other way.
        var growing = used < width;
        var unfrozen = 0;
        for (var j = line.Start; j < line.End; j++)
        {
            var item = _items[j];
            var hypothetical = item.Hypothetical;
            var flexible = growing ? item.Grow > 0 : item.Shrinks && item.Base > item.Box;
            item.Frozen = !flexible || (growing ? item.Base > hypothetical : item.Base < hypothetical);
            item.Size = item.Frozen ? hypothetical : item.Base;
            unfrozen += item.Frozen ? 0 : 1;
        }

        var initialFree = Free(line, width);
        while (unfrozen > 0)
        {
            // Shrinking is in proportion to the inner base size, as flex-shrink is 1.
            double factors = 0;
            for (var j = line.Start; j < line.End; j++)
            {
                var item = _items[j];
                factors += item.Frozen ? 0 : growing ? item.Grow : item.Base - item.Box;
            }

            // Factors that add up to less than 1 only take that much of the free space.
            var free = Free(line, width);
            if (growing && factors < 1 && Math.Abs(initialFree * factors) < Math.Abs(free))
            {
                free = initialFree * factors;
            }

            double violation = 0;
            for (var j = line.Start; j < line.End; j++)
            {
                var item = _items[j];
                if (item.Frozen)
                {
                    continue;
                }

                var share = growing ? item.Grow : item.Base - item.Box;
                item.Target = factors > 0 ? item.Base + (free * share / factors) : item.Base;
                item.Size = item.Clamp(item.Target);
                violation += item.Size - item.Target;
            }

            // Step 4d: freeze everything if nothing was clamped, else what was clamped the way the total went.
            for (var j = line.Start; j < line.End; j++)
            {
                var item = _items[j];
                if (!item.Frozen && (Math.Abs(violation) < 1e-9
                    || (violation > 0 ? item.Size > item.Target : item.Size < item.Target)))
                {
                    item.Frozen = true;
                    unfrozen--;
                }
            }
        }
    }

    /// <summary>The line's free space: frozen items at their size, the others at their base.</summary>
    private double Free(Line line, double width)
    {
        var free = width;
        for (var j = line.Start; j < line.End; j++)
        {
            var item = _items[j];
            free -= item.Lead + (item.Frozen ? item.Size : item.Base);
        }

        return free;
    }

    /// <summary>A column, or a column an <c>ItemTemplate</c> made, which its container wraps.</summary>
    private static AvonicCol? ColumnIn(Control child) =>
        child as AvonicCol ?? (child as ContentPresenter)?.Child as AvonicCol;
}
