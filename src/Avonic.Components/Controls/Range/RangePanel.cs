using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.range-wrapper</c> in <c>ion-range</c>'s shadow root: a flex row of the label and the native wrapper, reversed when the label
/// is at the end, or a column when it's stacked. Its first child is the label and its last the native wrapper; one that isn't
/// visible takes no part, as <c>display: none</c> doesn't. Avalonia mirrors it in RTL, as it does any panel.
/// </summary>
/// <remarks>
/// In a row the native wrapper grows into what's left (<c>flex-grow: 1</c>), and keeps its own content's width, as its minimum is
/// its slots' and the slider's is 0 (<c>contain: size</c>); the label (<c>flex: 0 1 auto</c>) shrinks to what's left of that, and
/// wraps, so that a long label squeezes the slider to nothing, as measured. Both are centred across (<c>align-items: center</c>).
/// In a column both are as wide as the panel (<c>align-items: stretch</c>), and the theme scales the label, which doesn't change its
/// layout box, as a CSS transform doesn't.
/// </remarks>
public sealed class RangePanel : Panel
{
    /// <summary>The range's <see cref="AvonicRange.LabelPlacement"/>.</summary>
    public static readonly StyledProperty<RangeLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<RangePanel, RangeLabelPlacement>(nameof(LabelPlacement));

    static RangePanel()
    {
        AffectsMeasure<RangePanel>(LabelPlacementProperty);
    }

    public RangeLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    /// <summary>The label's width in a row, from the measure: text that wraps measures narrower than the box it fills.</summary>
    private double _labelWidth;

    private (Control? Label, Control? Native) Parts()
    {
        var label = Children.Count > 1 ? Children[0] : null;
        var native = Children.Count > 0 ? Children[^1] : null;
        return (label is { IsVisible: true } ? label : null, native is { IsVisible: true } ? native : null);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var (label, native) = Parts();

        if (LabelPlacement == RangeLabelPlacement.Stacked)
        {
            label?.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            native?.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            var labelSize = label?.DesiredSize ?? default;
            var nativeSize = native?.DesiredSize ?? default;
            return new Size(Math.Max(labelSize.Width, nativeSize.Width), labelSize.Height + nativeSize.Height);
        }

        // The native wrapper's minimum first, then the label gets what's left of the row.
        native?.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var nativeMinimum = native?.DesiredSize.Width ?? 0;
        // The label's width is its flex basis, its max-content, shrunk to what's left: text that wraps still fills that.
        label?.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var labelRoom = Math.Max(0, availableSize.Width - nativeMinimum);
        var labelWidth = Math.Min(label?.DesiredSize.Width ?? 0, labelRoom);
        label?.Measure(new Size(labelWidth, availableSize.Height));
        labelWidth = Math.Max(labelWidth, label?.DesiredSize.Width ?? 0);
        _labelWidth = labelWidth;

        // Then the native wrapper again, at all that's left, which is what it's arranged at.
        if (native is not null && double.IsFinite(availableSize.Width))
        {
            native.Measure(new Size(Math.Max(nativeMinimum, availableSize.Width - labelWidth), availableSize.Height));
        }

        var height = Math.Max(label?.DesiredSize.Height ?? 0, native?.DesiredSize.Height ?? 0);
        return new Size(labelWidth + (native?.DesiredSize.Width ?? 0), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (label, native) = Parts();
        var labelSize = label?.DesiredSize ?? default;

        if (LabelPlacement == RangeLabelPlacement.Stacked)
        {
            label?.Arrange(new Rect(0, 0, finalSize.Width, labelSize.Height));
            native?.Arrange(new Rect(0, labelSize.Height, finalSize.Width, native.DesiredSize.Height));
            return finalSize;
        }

        labelSize = new Size(label is null ? 0 : Math.Min(_labelWidth, finalSize.Width), labelSize.Height);
        var nativeWidth = Math.Max(0, finalSize.Width - labelSize.Width);
        var reverse = LabelPlacement == RangeLabelPlacement.End;
        var labelX = reverse ? nativeWidth : 0;
        var nativeX = reverse ? 0 : labelSize.Width;

        label?.Arrange(new Rect(labelX, (finalSize.Height - labelSize.Height) / 2, labelSize.Width, labelSize.Height));
        native?.Arrange(new Rect(nativeX, (finalSize.Height - native.DesiredSize.Height) / 2, nativeWidth, native.DesiredSize.Height));
        return finalSize;
    }
}
