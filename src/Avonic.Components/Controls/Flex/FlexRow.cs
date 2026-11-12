using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>display: flex</c>, in one row, as the shadow parts of <c>ion-item</c> and <c>ion-item-divider</c> are, and
/// <c>ion-input-otp</c>'s group. It's the algorithm from the flexbox spec, section 9.7, without what those rows don't use:
/// <c>flex-basis</c> is the content, 0, or a <see cref="FlexBasisProperty"/>, <c>flex-shrink</c> is 1, and there's no
/// wrapping. See <c>docs/components/ion-item/PLAN.md</c>. Avalonia mirrors it in RTL, as it does any panel.
/// </summary>
public sealed class FlexRow : Panel
{
    /// <summary>CSS <c>gap</c>: the space between two shown children.</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<FlexRow, double>(nameof(Spacing));

    /// <summary>The flex container's own padding.</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<FlexRow>();

    /// <summary>
    /// <c>justify-content</c>: <see cref="HorizontalAlignment.Center"/> is <c>center</c>, <see cref="HorizontalAlignment.Right"/>
    /// <c>flex-end</c>, and the rest <c>normal</c>, at the start. A row that doesn't fit overflows both sides when centred, and the
    /// start when at the end, as CSS's does.
    /// </summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<FlexRow>();

    /// <summary>
    /// A child's <c>flex-basis</c>, as CSS <c>width</c> is one, which, unlike Avalonia's <c>Width</c>, still lets it shrink: to its
    /// <c>MinWidth</c>, which here stands for a <c>min-width</c> that's set, and not content's. Unset (NaN), the basis is the content,
    /// or 0 with <see cref="FlexProperty"/>. A child with a basis doesn't grow.
    /// </summary>
    public static readonly AttachedProperty<double> FlexBasisProperty =
        AvaloniaProperty.RegisterAttached<FlexRow, Control, double>("FlexBasis", double.NaN);

    /// <summary>
    /// Below this, a slot looks for its content's longest word rather than break inside words, and above it the break is what
    /// happens. CSS looks at every width, but that would measure every row twice, and it's a narrow slot that gets silly: at
    /// 6 wide it's a character a line and 400 high, and at 64 it's a few lines.
    /// </summary>
    private const double NarrowSlot = 64;

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
        AffectsMeasure<FlexRow>(SpacingProperty, PaddingProperty);
        AffectsArrange<FlexRow>(HorizontalContentAlignmentProperty);
        AffectsParentMeasure<FlexRow>(FlexBasisProperty);
    }

    public static double GetFlex(Control control) => control.GetValue(FlexProperty);

    public static void SetFlex(Control control, double value) => control.SetValue(FlexProperty, value);

    public static double GetFlexBasis(Control control) => control.GetValue(FlexBasisProperty);

    public static void SetFlexBasis(Control control, double value) => control.SetValue(FlexBasisProperty, value);

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    private sealed class Item(Control child)
    {
        public Control Child { get; } = child;

        /// <summary><c>flex</c>, from a part of the template or from what's in a slot.</summary>
        public double Flex { get; init; }

        /// <summary>The flex comes from a part of the template, which has <c>overflow: hidden</c>.</summary>
        public bool IsPart { get; init; }

        /// <summary>The basis is <see cref="FlexBasisProperty"/>, and the minimum is set.</summary>
        public bool HasBasis { get; init; }

        /// <summary>The outer base size, with margins, padding and borders.</summary>
        public double Base { get; set; }

        /// <summary>The inner base size, which is what shrinking is in proportion to.</summary>
        public double Inner { get; set; }

        public double? Minimum { get; set; }

        public double Size { get; set; }

        public bool Frozen { get; set; }

        /// <summary><see cref="Child"/> was last measured on one line, at <see cref="Base"/>.</summary>
        public bool MeasuredOnOneLine { get; set; }

        /// <summary>The width on one line of something that's <c>flex</c>, whose <see cref="Base"/> is 0.</summary>
        public double OneLine { get; set; }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var inner = availableSize.Deflate(padding);
        var oneLine = new Size(double.PositiveInfinity, inner.Height);
        var bounded = !double.IsInfinity(inner.Width);

        _items.Clear();
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            var basis = GetFlexBasis(child);
            if (!double.IsNaN(basis))
            {
                // width: var(--width), and its min-width. Measured once it has its size, below.
                var margin = MinContent.Horizontal(child.Margin);
                var outer = Math.Max(child.MinWidth, Math.Min(child.MaxWidth, basis)) + margin;
                _items.Add(new Item(child)
                {
                    HasBasis = true,
                    Base = outer,
                    Inner = Math.Max(0, outer - MinContent.OwnBox(child)),
                    Minimum = child.MinWidth + margin,
                });
                continue;
            }

            var own = GetFlex(child);
            if (bounded && own > 0)
            {
                _items.Add(new Item(child) { Flex = own, IsPart = true, Base = MinContent.OwnBox(child) });
                continue;
            }

            // A presenter makes its child here, which is what styles a slotted child, so its flex and margins are known.
            child.ApplyTemplate();

            var item = new Item(child)
            {
                Flex = own > 0 ? own : MinContent.Slotted(child) is { } slotted ? GetFlex(slotted) : 0,
                IsPart = own > 0,
            };
            _items.Add(item);

            child.Measure(oneLine);

            var box = MinContent.Box(child);
            if (bounded && item.Flex > 0)
            {
                item.Base = box;
                item.OneLine = child.DesiredSize.Width;
            }
            else
            {
                item.Base = child.DesiredSize.Width;
                item.Inner = Math.Max(0, item.Base - box);
                item.MeasuredOnOneLine = true;
            }
        }

        var gaps = Gaps();
        var available = inner.Width - gaps;
        Resolve(available);

        // A flex label's minimum only matters if it got less than one line, and most don't, so most don't look for their words.
        var again = false;
        foreach (var item in _items)
        {
            if (bounded && !item.IsPart && item.Flex > 0 && item.Minimum is null && item.Size < item.OneLine)
            {
                item.Minimum = FlexMinimum(item, available);
                again = true;
            }
        }

        if (again)
        {
            Resolve(available);
        }

        double width = 0, height = 0;
        foreach (var item in _items)
        {
            if (item.HasBasis)
            {
                item.Child.Measure(new Size(item.Size, inner.Height));
                width += item.Size;
            }
            else
            {
                if (!item.MeasuredOnOneLine || item.Size < item.Base)
                {
                    item.Child.Measure(new Size(MeasuredWidth(item, oneLine), inner.Height));
                }

                width += bounded ? item.Child.DesiredSize.Width : item.Size;
            }

            height = Math.Max(height, item.Child.DesiredSize.Height);
        }

        // No wider than its content, so that an item aligned to the left is as wide as what's in it. Arranged wider, it shares that
        // out. Measure clamps this to what's available.
        return new Size(width + gaps + padding.Left + padding.Right, height + padding.Top + padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var inner = new Rect(finalSize).Deflate(Padding);
        var gaps = Gaps();
        var available = inner.Width - gaps;
        if (available != _resolvedFor)
        {
            Resolve(available);
        }

        var spacing = Spacing;
        var x = inner.X;
        var alignment = HorizontalContentAlignment;
        if (alignment is HorizontalAlignment.Center or HorizontalAlignment.Right)
        {
            var free = available - _items.Sum(item => item.Size);
            x += alignment == HorizontalAlignment.Center ? free / 2 : free;
        }

        foreach (var item in _items)
        {
            item.Child.Arrange(new Rect(x, inner.Y, item.Size, inner.Height));
            x += item.Size + spacing;
        }

        return finalSize;
    }

    /// <summary>The gaps between the shown children.</summary>
    private double Gaps() => _items.Count > 1 ? Spacing * (_items.Count - 1) : 0;

    /// <summary>
    /// What to measure with, which isn't the size when something has been squeezed. A <c>TextBlock</c> doesn't wrap at all at a
    /// width of 0, and CSS wraps a label at every character there, as <c>max-width: 100%</c> takes away its minimum. Anything
    /// else in <c>.input-wrapper</c> keeps its <c>min-content</c>, and <c>overflow: hidden</c> clips it.
    /// </summary>
    private static double MeasuredWidth(Item item, Size oneLine)
    {
        item.Child.ApplyTemplate();
        if (item.Size >= NarrowSlot || MinContent.Slotted(item.Child) is not { } slotted)
        {
            return item.Size;
        }

        if (GetFlex(slotted) > 0)
        {
            return Math.Max(item.Size, MinContent.Box(item.Child) + 1);
        }

        item.Child.Measure(oneLine);
        return Math.Max(item.Size, MinContent.OfItem(item.Child));
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

        double hypothetical = 0;
        foreach (var item in _items)
        {
            hypothetical += Math.Max(item.Base, item.Minimum ?? 0);
        }

        var growing = hypothetical < width;
        var unfrozen = 0;

        foreach (var item in _items)
        {
            // Only what's overflowing needs this, and most rows don't.
            if (!growing && item.Minimum is null)
            {
                item.Minimum = item.IsPart ? item.Base : item.Flex > 0 ? FlexMinimum(item, width) : MinContent.OfItem(item.Child);
            }

            item.Size = Math.Max(item.Base, item.Minimum ?? 0);
            item.Frozen = growing ? item.Flex <= 0 : item.Inner <= 0 || item.Size > item.Base;
            unfrozen += item.Frozen ? 0 : 1;
        }

        while (unfrozen > 0)
        {
            double free = width, total = 0;
            foreach (var item in _items)
            {
                free -= item.Frozen ? item.Size : item.Base;
                total += item.Frozen ? 0 : growing ? item.Flex : item.Inner;
            }

            // Factors that add up to less than 1 only take that much of the free space.
            var shares = growing ? Math.Max(total, 1) : total;

            var violated = false;
            foreach (var item in _items)
            {
                if (item.Frozen)
                {
                    continue;
                }

                var size = item.Base + (free * (growing ? item.Flex : item.Inner) / shares);
                item.Size = Math.Max(size, item.Minimum ?? 0);
                if (item.Size > size)
                {
                    item.Frozen = true;
                    unfrozen--;
                    violated = true;
                }
            }

            if (!violated)
            {
                break;
            }
        }
    }

    /// <summary>A flex label's minimum, as <c>max-width: 100%</c> limits its content to the row.</summary>
    private static double FlexMinimum(Item item, double width)
    {
        var box = MinContent.Box(item.Child);
        return Math.Min(MinContent.OfItem(item.Child) - box, width) + box;
    }
}
