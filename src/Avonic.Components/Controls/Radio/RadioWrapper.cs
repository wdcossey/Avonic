using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.radio-wrapper</c> in <c>ion-radio</c>'s shadow root: a flex line of two children, the label and the native wrapper, in
/// that order, as <c>radio.md.css</c> lays them out for each placement, <c>justify</c> and <c>alignment</c>. Only that: two
/// children, the label shrinking to nothing and the radio keeping its size, as <c>overflow: hidden</c> gives the label no minimum,
/// except a fixed label, which keeps its width.
/// Avalonia mirrors it in RTL, as it does any panel, so it lays out left to right. See <c>docs/components/ion-radio/PLAN.md</c>.
/// </summary>
public sealed class RadioWrapper : Panel
{
    /// <summary><c>scale(0.75)</c> on a stacked label, whose box can be <c>100% / 0.75</c> wide.</summary>
    public const double StackedScale = 0.75;

    public static readonly StyledProperty<RadioLabelPlacement> LabelPlacementProperty =
        AvonicRadio.LabelPlacementProperty.AddOwner<RadioWrapper>();

    public static readonly StyledProperty<RadioJustify?> JustifyProperty =
        AvonicRadio.JustifyProperty.AddOwner<RadioWrapper>();

    public static readonly StyledProperty<RadioAlignment?> AlignmentProperty =
        AvonicRadio.AlignmentProperty.AddOwner<RadioWrapper>();

    static RadioWrapper()
    {
        AffectsMeasure<RadioWrapper>(LabelPlacementProperty);
        AffectsArrange<RadioWrapper>(JustifyProperty, AlignmentProperty);
    }

    public RadioLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public RadioJustify? Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    public RadioAlignment? Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    private Control? Label => Children.Count > 0 ? Children[0] : null;

    private Control? Native => Children.Count > 1 ? Children[1] : null;

    private bool Stacked => LabelPlacement == RadioLabelPlacement.Stacked;

    private bool Fixed => LabelPlacement == RadioLabelPlacement.Fixed;

    protected override Size MeasureOverride(Size availableSize)
    {
        var native = Measure(Native, availableSize);
        if (Stacked)
        {
            // max-width: calc(100% / 0.75), of the line, which has no width yet when the radio is as wide as its content.
            var label = Measure(Label, new Size(availableSize.Width / StackedScale, double.PositiveInfinity));
            return new Size(Math.Min(Math.Max(label.Width, native.Width), availableSize.Width), label.Height + native.Height);
        }

        // flex: 0 0 100px: a fixed label never shrinks, and the line overflows.
        var labelSpace = Fixed ? double.PositiveInfinity : Math.Max(0, availableSize.Width - native.Width);
        var row = Measure(Label, availableSize.WithWidth(labelSpace));
        return new Size(row.Width + native.Width, Math.Max(row.Height, native.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var label = Label;
        var native = Native;
        var hasLabel = label?.IsVisible == true;
        var nativeSize = native?.DesiredSize ?? default;

        if (Stacked)
        {
            var labelSize = hasLabel ? label!.DesiredSize.WithWidth(Math.Min(label.DesiredSize.Width, finalSize.Width / StackedScale)) : default;
            var (labelY, nativeY) = Pack(finalSize.Height, labelSize.Height, nativeSize.Height, hasLabel, reversed: false);
            Arrange(label, new Rect(Across(finalSize.Width, labelSize.Width), labelY, labelSize.Width, labelSize.Height));
            Arrange(native, new Rect(Across(finalSize.Width, nativeSize.Width), nativeY, nativeSize.Width, nativeSize.Height));
            return finalSize;
        }

        var labelWidth = !hasLabel ? 0
            : Fixed ? label!.DesiredSize.Width
            : Math.Min(label!.DesiredSize.Width, Math.Max(0, finalSize.Width - nativeSize.Width));
        var reversed = LabelPlacement == RadioLabelPlacement.End;

        // In visual order, left to right: row-reverse puts the radio first.
        var (firstWidth, secondWidth) = reversed ? (nativeSize.Width, labelWidth) : (labelWidth, nativeSize.Width);
        var (firstX, secondX) = Pack(finalSize.Width, firstWidth, secondWidth, hasLabel, reversed);
        var (labelX, nativeX) = reversed ? (secondX, firstX) : (firstX, secondX);

        Arrange(label, new Rect(labelX, Across(finalSize.Height, label?.DesiredSize.Height ?? 0), labelWidth, label?.DesiredSize.Height ?? 0));
        Arrange(native, new Rect(nativeX, Across(finalSize.Height, nativeSize.Height), nativeSize.Width, nativeSize.Height));
        return finalSize;
    }

    /// <summary>
    /// <c>justify-content</c> along the line, for two items in visual order. <c>start</c> and <c>end</c> are the line's own, even in
    /// <c>row-reverse</c>. <c>space-between</c>, the default, is <c>flex-start</c> with one item, which in <c>row-reverse</c> is the
    /// right: the radio alone, when there's no label.
    /// </summary>
    private (double First, double Second) Pack(double length, double first, double second, bool both, bool reversed)
    {
        var free = Math.Max(0, length - first - second);
        return Justify switch
        {
            RadioJustify.Start => (0, first),
            RadioJustify.End => (free, free + first),
            _ when both => (0, length - second),
            _ when reversed => (free + second, free),
            _ => (0, first),
        };
    }

    /// <summary><c>align-items</c>: <c>center</c>, from <c>.radio-wrapper</c>, unless <c>alignment</c> is <c>start</c>.</summary>
    private double Across(double length, double size) =>
        Alignment == RadioAlignment.Start ? 0 : (length - size) / 2;

    private static Size Measure(Control? child, Size available)
    {
        if (child is null)
        {
            return default;
        }

        child.Measure(available);
        return child.DesiredSize;
    }

    private static void Arrange(Control? child, Rect rect) => child?.Arrange(rect);
}
