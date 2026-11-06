using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>display: flex</c>, in one row, as the shadow parts of <c>ion-item</c> and <c>ion-item-divider</c> are. It's the
/// algorithm from the flexbox spec, section 9.7, without what those rows don't use: <c>flex-basis</c> is either the
/// content or 0, <c>flex-shrink</c> is 1, there's no <c>gap</c>, and there's no wrapping. See
/// <c>docs/components/ion-item/PLAN.md</c>. Avalonia mirrors it in RTL, as it does any panel.
/// </summary>
public sealed class FlexRow : Panel
{
    /// <summary>
    /// CSS's one-number <c>flex</c>. 0 is <c>flex: initial</c>, and <c>n</c> is <c>flex: n</c>: grows by <c>n</c>, from a
    /// basis of 0. On a child of a <see cref="FlexSlot"/>, a style can set it, as a <c>&lt;slot&gt;</c> has no box and its
    /// children are the row's items. A part of the template that sets it has <c>overflow: hidden</c>, so no minimum.
    /// </summary>
    public static readonly AttachedProperty<double> FlexProperty =
        AvaloniaProperty.RegisterAttached<FlexRow, Control, double>("Flex");

    private readonly List<Item> _items = [];
    private double _resolvedFor;

    static FlexRow()
    {
        FlexProperty.Changed.AddClassHandler<Control>((control, _) => control.FindAncestorOfType<FlexRow>()?.InvalidateMeasure());
    }

    public static double GetFlex(Control control) => control.GetValue(FlexProperty);

    public static void SetFlex(Control control, double value) => control.SetValue(FlexProperty, value);

    private sealed class Item(Control child)
    {
        public Control Child { get; } = child;

        /// <summary><c>flex</c>, from a part of the template or from what's in a slot.</summary>
        public double Flex { get; init; }

        /// <summary>The flex comes from a part of the template, which has <c>overflow: hidden</c>.</summary>
        public bool IsPart { get; init; }

        /// <summary>The outer base size, with margins, padding and borders.</summary>
        public double Base { get; set; }

        /// <summary>The inner base size, which is what shrinking is in proportion to.</summary>
        public double Inner { get; set; }

        public double? Minimum { get; set; }

        public double Size { get; set; }

        public bool Frozen { get; set; }

        /// <summary><see cref="Child"/> was last measured on one line, at <see cref="Base"/>.</summary>
        public bool MeasuredOnOneLine { get; set; }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var oneLine = new Size(double.PositiveInfinity, availableSize.Height);
        var bounded = !double.IsInfinity(availableSize.Width);

        _items.Clear();
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            var own = GetFlex(child);
            if (bounded && own > 0)
            {
                _items.Add(new Item(child) { Flex = own, IsPart = true, Base = OwnBox(child) });
                continue;
            }

            // A presenter makes its child here, which is what styles a slotted child, so its flex and margins are known.
            child.ApplyTemplate();

            var item = new Item(child)
            {
                Flex = own > 0 ? own : child is ContentPresenter { Child: { } slotted } ? GetFlex(slotted) : 0,
                IsPart = own > 0,
            };
            _items.Add(item);

            var box = Box(child);
            if (bounded && item.Flex > 0)
            {
                // max-width: 100% limits the content, and min-content needs one-line sizes to work from.
                child.Measure(oneLine);
                item.Base = box;
                item.Minimum = Math.Min(MinimumOf(child) - box, availableSize.Width) + box;
            }
            else
            {
                child.Measure(oneLine);
                item.Base = child.DesiredSize.Width;
                item.Inner = Math.Max(0, item.Base - box);
                item.MeasuredOnOneLine = true;
            }
        }

        Resolve(availableSize.Width);

        double width = 0, height = 0;
        foreach (var item in _items)
        {
            if (!item.MeasuredOnOneLine || item.Size < item.Base)
            {
                item.Child.Measure(new Size(MeasuredWidth(item, oneLine), availableSize.Height));
            }

            width += item.Size;
            height = Math.Max(height, item.Child.DesiredSize.Height);
        }

        return new Size(bounded ? availableSize.Width : width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (finalSize.Width != _resolvedFor)
        {
            Resolve(finalSize.Width);
        }

        double x = 0;
        foreach (var item in _items)
        {
            item.Child.Arrange(new Rect(x, 0, item.Size, finalSize.Height));
            x += item.Size;
        }

        return finalSize;
    }

    /// <summary>
    /// What to measure with, which isn't the size when something has been squeezed to nothing. A <c>TextBlock</c> doesn't
    /// wrap at all at a width of 0, and CSS wraps a label at every character there, as <c>max-width: 100%</c> takes away
    /// its minimum. Anything else in <c>.input-wrapper</c> keeps its <c>min-content</c>, and <c>overflow: hidden</c> clips it.
    /// </summary>
    private static double MeasuredWidth(Item item, Size oneLine)
    {
        item.Child.ApplyTemplate();
        if (item.Size >= 1 || item.Child is not ContentPresenter { Child: { } slotted } slot)
        {
            return item.Size;
        }

        if (GetFlex(slotted) > 0)
        {
            return 1;
        }

        slot.Measure(oneLine);
        return MinContent(slot);
    }

    /// <summary>Section 9.7, "Resolving Flexible Lengths".</summary>
    private void Resolve(double width)
    {
        _resolvedFor = width;

        if (double.IsInfinity(width))
        {
            foreach (var item in _items)
            {
                item.Size = item.Base;
            }

            return;
        }

        var growing = _items.Sum(item => Math.Max(item.Base, item.Minimum ?? 0)) < width;

        foreach (var item in _items)
        {
            // Only what's overflowing needs this, and most rows don't.
            if (!growing && item.Minimum is null)
            {
                item.Minimum = item.IsPart ? item.Base : MinimumOf(item.Child);
            }

            item.Size = Math.Max(item.Base, item.Minimum ?? 0);
            item.Frozen = growing ? item.Flex <= 0 : item.Inner <= 0 || item.Size > item.Base;
        }

        while (_items.Any(item => !item.Frozen))
        {
            var unfrozen = _items.Where(item => !item.Frozen).ToList();
            var free = width - _items.Sum(item => item.Frozen ? item.Size : item.Base);
            var total = unfrozen.Sum(item => growing ? item.Flex : item.Inner);

            var violated = false;
            foreach (var item in unfrozen)
            {
                var size = item.Base + (free * (growing ? item.Flex : item.Inner) / total);
                item.Size = Math.Max(size, item.Minimum ?? 0);
                if (item.Size > size)
                {
                    item.Frozen = true;
                    violated = true;
                }
            }

            if (!violated)
            {
                break;
            }
        }
    }

    private static double Horizontal(Thickness thickness) => thickness.Left + thickness.Right;

    /// <summary>What a box of width 0 still takes: margin, padding and borders.</summary>
    private static double OwnBox(Control control) => Horizontal(control.Margin) + control switch
    {
        ContentPresenter presenter => Horizontal(presenter.Padding) + Horizontal(presenter.BorderThickness),
        Border border => Horizontal(border.Padding) + Horizontal(border.BorderThickness),
        TemplatedControl templated => Horizontal(templated.Padding) + Horizontal(templated.BorderThickness),
        _ => 0,
    };

    /// <summary><see cref="OwnBox"/>, and a slotted child's, which is the row's item.</summary>
    private static double Box(Control control) =>
        OwnBox(control) + (control is ContentPresenter { Child: { } child } ? OwnBox(child) : 0);

    /// <summary><c>min-width: auto</c> on one of the row's items. Needs it measured on one line first.</summary>
    private static double MinimumOf(Control child)
    {
        var item = child is ContentPresenter { Child: { } slotted } ? slotted : child;

        // contain: strict. Size containment leaves it no minimum, so it shrinks with the rest. Measured.
        if (item is AvonicIcon)
        {
            return Box(child);
        }

        // overflow: hidden, which also takes it away. A TemplatedControl clips by default, so only a set value counts.
        if (item.ClipToBounds && item.IsSet(ClipToBoundsProperty))
        {
            return Box(child);
        }

        return MinContent(child);
    }

    /// <summary>
    /// CSS's <c>min-content</c>, from the one-line sizes just measured, because <c>Measure</c> clamps to what's available
    /// and so can't report it. Text can wrap, and nothing else can shrink.
    /// </summary>
    private static double MinContent(Control control)
    {
        if (!double.IsNaN(control.Width))
        {
            return control.DesiredSize.Width;
        }

        if (control is TextBlock text)
        {
            return Math.Min(control.DesiredSize.Width, Horizontal(text.Margin) + Horizontal(text.Padding) + LongestWord(text));
        }

        var children = control.GetVisualChildren().OfType<Control>().Where(child => child.IsVisible).ToList();
        if (children.Count == 0)
        {
            return control.DesiredSize.Width;
        }

        var adds = control is FlexRow or ButtonInner or StackPanel { Orientation: Orientation.Horizontal };
        var oneLine = adds ? children.Sum(child => child.DesiredSize.Width) : children.Max(child => child.DesiredSize.Width);
        var least = adds ? children.Sum(MinContent) : children.Max(MinContent);

        return Math.Max(Horizontal(control.Margin) + control.MinWidth, control.DesiredSize.Width - oneLine + least);
    }

    private static double LongestWord(TextBlock text)
    {
        if (text.TextWrapping == TextWrapping.NoWrap)
        {
            return double.PositiveInfinity;
        }

        var content = text.Inlines is { Count: > 0 } inlines ? inlines.Text : text.Text;
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        using var layout = new TextLayout(
            content,
            new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch),
            text.FontSize,
            null,
            textWrapping: TextWrapping.WrapWithOverflow,
            maxWidth: 1);

        // Up, so the word isn't a fraction of a pixel too wide for itself and broken at its last letter.
        return Math.Ceiling(layout.Width);
    }
}
