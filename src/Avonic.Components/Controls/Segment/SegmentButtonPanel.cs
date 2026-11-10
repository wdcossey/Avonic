using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-segment-button</c>'s <c>.button-inner</c>, a flex box centred both ways, whose direction is the button's
/// <see cref="Layout"/>: a column for icon-top, icon-hide and label-hide, a reversed column for icon-bottom, a row for icon-start
/// and a reversed row for icon-end. Icons go first, as <c>::slotted(ion-icon) { order: -1 }</c> puts them, and the rest follow
/// in order. In a row, what doesn't fit shrinks, except the icons (<c>flex-shrink: 0</c>).
/// </summary>
public sealed class SegmentButtonPanel : Panel
{
    public static readonly StyledProperty<SegmentButtonLayout> LayoutProperty =
        AvaloniaProperty.Register<SegmentButtonPanel, SegmentButtonLayout>(nameof(Layout));

    private List<Control> _ordered = [];
    private double[] _widths = [];

    static SegmentButtonPanel()
    {
        AffectsMeasure<SegmentButtonPanel>(LayoutProperty);
    }

    public SegmentButtonLayout Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    private bool IsRow => Layout is SegmentButtonLayout.IconStart or SegmentButtonLayout.IconEnd;

    private List<Control> Ordered()
    {
        var visible = Children.Where(child => child.IsVisible).ToList();
        var ordered = visible.Where(child => child is AvonicIcon).Concat(visible.Where(child => child is not AvonicIcon)).ToList();
        if (Layout is SegmentButtonLayout.IconEnd or SegmentButtonLayout.IconBottom)
        {
            ordered.Reverse();
        }

        return ordered;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _ordered = Ordered();
        _widths = new double[_ordered.Count];
        if (!IsRow)
        {
            double width = 0, height = 0;
            foreach (var child in _ordered)
            {
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                width = Math.Max(width, child.DesiredSize.Width);
                height += child.DesiredSize.Height;
            }

            return new Size(width, height);
        }

        // A row: each at its width, then the ones that can shrink give up what doesn't fit, in proportion to their width.
        for (var i = 0; i < _ordered.Count; i++)
        {
            _ordered[i].Measure(new Size(double.PositiveInfinity, availableSize.Height));
            _widths[i] = _ordered[i].DesiredSize.Width;
        }

        var over = _widths.Sum() - availableSize.Width;
        var shrinking = Enumerable.Range(0, _ordered.Count).Where(i => _ordered[i] is not AvonicIcon).Sum(i => _widths[i]);
        if (over > 0 && shrinking > 0)
        {
            var shrinkable = Enumerable.Range(0, _ordered.Count).Where(i => _ordered[i] is not AvonicIcon).ToList();
            var shrinkableWidth = shrinkable.Sum(i => _widths[i]);
            foreach (var i in shrinkable)
            {
                _widths[i] = Math.Max(0, _widths[i] - over * _widths[i] / shrinkableWidth);
                _ordered[i].Measure(new Size(_widths[i], availableSize.Height));
            }
        }

        return new Size(_widths.Sum(), _ordered.Count == 0 ? 0 : _ordered.Max(child => child.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!IsRow)
        {
            var y = (finalSize.Height - _ordered.Sum(child => child.DesiredSize.Height)) / 2;
            foreach (var child in _ordered)
            {
                var size = child.DesiredSize;
                var width = Math.Min(size.Width, finalSize.Width);
                child.Arrange(new Rect((finalSize.Width - width) / 2, y, width, size.Height));
                y += size.Height;
            }

            return finalSize;
        }

        var x = (finalSize.Width - _widths.Sum()) / 2;
        for (var i = 0; i < _ordered.Count; i++)
        {
            var height = _ordered[i].DesiredSize.Height;
            _ordered[i].Arrange(new Rect(x, (finalSize.Height - height) / 2, _widths[i], height));
            x += _widths[i];
        }

        return finalSize;
    }
}
