using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.toggle-wrapper</c> in <c>ion-toggle</c>'s shadow root: a flex row of the label and the track, or a column when the label
/// is stacked, with <c>justify-content</c> and <c>align-items</c> as <c>toggle.md.css</c> sets them. Its first child is the label
/// and its last the track's wrapper; one that isn't visible takes no part, as <c>display: none</c> doesn't. Avalonia mirrors it in
/// RTL, as it does any panel. See <c>docs/components/ion-toggle/PLAN.md</c>.
/// </summary>
public sealed class TogglePanel : Panel
{
    /// <summary><c>max-width: calc(100% / 0.75)</c> on a stacked label, which the theme scales to 0.75.</summary>
    public const double StackedLabelScale = 0.75;

    /// <summary>The toggle's <see cref="AvonicToggle.LabelPlacement"/>.</summary>
    public static readonly StyledProperty<ToggleLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<TogglePanel, ToggleLabelPlacement>(nameof(LabelPlacement), ToggleLabelPlacement.Start);

    /// <summary>The toggle's <see cref="AvonicToggle.Justify"/>.</summary>
    public static readonly StyledProperty<ToggleJustify?> JustifyProperty =
        AvaloniaProperty.Register<TogglePanel, ToggleJustify?>(nameof(Justify));

    /// <summary>The toggle's <see cref="AvonicToggle.Alignment"/>.</summary>
    public static readonly StyledProperty<ToggleAlignment?> AlignmentProperty =
        AvaloniaProperty.Register<TogglePanel, ToggleAlignment?>(nameof(Alignment));

    static TogglePanel()
    {
        AffectsMeasure<TogglePanel>(LabelPlacementProperty);
        AffectsArrange<TogglePanel>(JustifyProperty, AlignmentProperty);
    }

    public ToggleLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public ToggleJustify? Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    public ToggleAlignment? Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    private (Control? Label, Control? Native) Parts()
    {
        var label = Children.Count > 1 ? Children[0] : null;
        var native = Children.Count > 0 ? Children[^1] : null;
        return (label is { IsVisible: true } ? label : null, native is { IsVisible: true } ? native : null);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var (label, native) = Parts();

        // The track's wrapper keeps its min-content, so it's measured first, and the label gets what's left.
        native?.Measure(Size.Infinity);
        var nativeSize = native?.DesiredSize ?? default;

        if (LabelPlacement == ToggleLabelPlacement.Stacked)
        {
            label?.Measure(new Size(availableSize.Width / StackedLabelScale, double.PositiveInfinity));
            var labelSize = label?.DesiredSize ?? default;
            return new Size(Math.Max(labelSize.Width, nativeSize.Width), labelSize.Height + nativeSize.Height);
        }

        // overflow: hidden takes away the label's minimum, so it shrinks to fit, as a measure clamps it to what it's given. A fixed
        // label doesn't shrink (flex: 0 0 100px; min-width: 100px), so it isn't given less.
        var labelRoom = LabelPlacement == ToggleLabelPlacement.Fixed
            ? double.PositiveInfinity
            : Math.Max(0, availableSize.Width - nativeSize.Width);
        label?.Measure(new Size(labelRoom, double.PositiveInfinity));
        var rowLabel = label?.DesiredSize ?? default;
        return new Size(rowLabel.Width + nativeSize.Width, Math.Max(rowLabel.Height, nativeSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (label, native) = Parts();
        var nativeSize = native?.DesiredSize ?? default;
        var labelSize = label?.DesiredSize ?? default;

        if (LabelPlacement == ToggleLabelPlacement.Stacked)
        {
            ArrangeColumn(label, labelSize, native, nativeSize, finalSize);
        }
        else
        {
            ArrangeRow(label, labelSize, native, nativeSize, finalSize);
        }

        return finalSize;
    }

    /// <summary><c>flex-direction: row</c>, or <c>row-reverse</c> for end, with <c>align-items</c> across.</summary>
    private void ArrangeRow(Control? label, Size labelSize, Control? native, Size nativeSize, Size finalSize)
    {
        var placement = LabelPlacement;
        var reverse = placement == ToggleLabelPlacement.End;

        // The label has already shrunk to fit, in the measure.
        var labelWidth = labelSize.Width;

        // .toggle-wrapper is space-between, and end's own rule sets start. A justify rule comes after both.
        var justify = Justify ?? (reverse ? ToggleJustify.Start : ToggleJustify.SpaceBetween);
        var free = finalSize.Width - labelWidth - nativeSize.Width;

        // Left to right, as the line is laid out in LTR: row-reverse puts the label after the track.
        var first = reverse ? (native, nativeSize.Width) : (label, labelWidth);
        var second = reverse ? (label, labelWidth) : (native, nativeSize.Width);

        double x1, x2;
        if (justify == ToggleJustify.SpaceBetween)
        {
            // Two items at either end. One alone, or no room, falls back to flex-start, which is the main start: the right for
            // row-reverse.
            if (first.Item1 is not null && second.Item1 is not null && free >= 0)
            {
                x1 = 0;
                x2 = finalSize.Width - second.Item2;
            }
            else if (reverse)
            {
                x2 = finalSize.Width - second.Item2;
                x1 = x2 - first.Item2;
            }
            else
            {
                x1 = 0;
                x2 = first.Item2;
            }
        }
        else
        {
            // start and end are the line's inline start and end, whatever the flex direction: measured.
            x1 = justify == ToggleJustify.End ? free : 0;
            x2 = x1 + first.Item2;
        }

        Place(first.Item1, x1, first.Item2, finalSize.Height);
        Place(second.Item1, x2, second.Item2, finalSize.Height);
    }

    private void Place(Control? child, double x, double width, double height)
    {
        if (child is null)
        {
            return;
        }

        var y = Alignment == ToggleAlignment.Start ? 0 : (height - child.DesiredSize.Height) / 2;
        child.Arrange(new Rect(x, y, width, child.DesiredSize.Height));
    }

    /// <summary><c>flex-direction: column</c>: the label above the track, packed down by <c>justify</c>, and centred across.</summary>
    private void ArrangeColumn(Control? label, Size labelSize, Control? native, Size nativeSize, Size finalSize)
    {
        var justify = Justify ?? ToggleJustify.SpaceBetween;
        var free = finalSize.Height - labelSize.Height - nativeSize.Height;

        double yLabel, yNative;
        if (justify == ToggleJustify.SpaceBetween && label is not null && native is not null && free >= 0)
        {
            yLabel = 0;
            yNative = finalSize.Height - nativeSize.Height;
        }
        else
        {
            yLabel = justify == ToggleJustify.End ? free : 0;
            yNative = yLabel + labelSize.Height;
        }

        // A stacked label may be wider than the line, by up to a third, and centring then hangs it over both sides.
        double Across(Size size) => Alignment == ToggleAlignment.Start ? 0 : (finalSize.Width - size.Width) / 2;
        label?.Arrange(new Rect(Across(labelSize), yLabel, labelSize.Width, labelSize.Height));
        native?.Arrange(new Rect(Across(nativeSize), yNative, nativeSize.Width, nativeSize.Height));
    }
}
