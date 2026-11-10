using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// The flex box of <c>ion-input</c>'s <c>.input-wrapper</c>: the label part (the first child) and <c>.native-wrapper</c> (the
/// second), in the order and direction the placement gives. Measured in Chromium, see <c>docs/components/ion-input/PLAN.md</c>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>A row (start, fixed, and end, reversed): the label takes its width, which its <c>MaxWidth</c> limits, and the field the
/// rest, both the row's height (<c>align-items: stretch</c>).</item>
/// <item>A column (floating, stacked): the label at the top, as wide as its content, and the field the rest of the height. The
/// label's room is <c>(width − <see cref="LabelInset"/>) / <see cref="LabelScale"/></c>, as its <c>max-width</c> says, since
/// a transform doesn't change its box.</item>
/// <item><see cref="IsLabelOutOfFlow"/>: the outline's <c>position: absolute</c> label, at the top start, taking no room.</item>
/// </list>
/// Avalonia mirrors it in RTL, as it does any panel.
/// </remarks>
public sealed class InputWrapperPanel : Panel
{
    /// <summary>Which of the five layouts. <see cref="AvonicInput"/> binds it to its own.</summary>
    public static readonly StyledProperty<InputLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<InputWrapperPanel, InputLabelPlacement>(nameof(LabelPlacement));

    /// <summary>The scale in the label's <c>max-width: calc(100% / scale)</c>, in a column. 1 when it isn't scaled.</summary>
    public static readonly StyledProperty<double> LabelScaleProperty =
        AvaloniaProperty.Register<InputWrapperPanel, double>(nameof(LabelScale), 1);

    /// <summary>What's taken from the width before the scale, in a column: the outline's 8 px.</summary>
    public static readonly StyledProperty<double> LabelInsetProperty =
        AvaloniaProperty.Register<InputWrapperPanel, double>(nameof(LabelInset));

    /// <summary>The label is <c>position: absolute</c>: it takes no room, at the top start.</summary>
    public static readonly StyledProperty<bool> IsLabelOutOfFlowProperty =
        AvaloniaProperty.Register<InputWrapperPanel, bool>(nameof(IsLabelOutOfFlow));

    static InputWrapperPanel()
    {
        AffectsMeasure<InputWrapperPanel>(LabelPlacementProperty, LabelScaleProperty, LabelInsetProperty, IsLabelOutOfFlowProperty);
    }

    public InputLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public double LabelScale
    {
        get => GetValue(LabelScaleProperty);
        set => SetValue(LabelScaleProperty, value);
    }

    public double LabelInset
    {
        get => GetValue(LabelInsetProperty);
        set => SetValue(LabelInsetProperty, value);
    }

    public bool IsLabelOutOfFlow
    {
        get => GetValue(IsLabelOutOfFlowProperty);
        set => SetValue(IsLabelOutOfFlowProperty, value);
    }

    private bool IsColumn => LabelPlacement is InputLabelPlacement.Floating or InputLabelPlacement.Stacked;

    private Control? Label => Children.Count > 0 ? Children[0] : null;

    private Control? Field => Children.Count > 1 ? Children[1] : null;

    protected override Size MeasureOverride(Size availableSize)
    {
        var label = Label;
        var field = Field;
        if (IsColumn)
        {
            var room = availableSize.Width;
            if (double.IsFinite(room))
            {
                room = Math.Max(0, room - LabelInset) / (LabelScale > 0 ? LabelScale : 1);
            }

            label?.Measure(new Size(room, availableSize.Height));
            var labelHeight = IsLabelOutOfFlow ? 0 : label?.DesiredSize.Height ?? 0;
            field?.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - labelHeight)));
            var fieldSize = field?.DesiredSize ?? default;
            var labelWidth = IsLabelOutOfFlow ? 0 : Math.Min(label?.DesiredSize.Width ?? 0, availableSize.Width);
            return new Size(Math.Max(labelWidth, fieldSize.Width), labelHeight + fieldSize.Height);
        }

        label?.Measure(availableSize);
        var labelSize = label?.DesiredSize ?? default;
        field?.Measure(new Size(Math.Max(0, availableSize.Width - labelSize.Width), availableSize.Height));
        var fieldDesired = field?.DesiredSize ?? default;
        return new Size(labelSize.Width + fieldDesired.Width, Math.Max(labelSize.Height, fieldDesired.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var label = Label;
        var field = Field;
        if (IsColumn)
        {
            var labelSize = label?.DesiredSize ?? default;
            // As wide as its content, which can be wider than the row before the scale: align-items: start.
            label?.Arrange(new Rect(0, 0, labelSize.Width, labelSize.Height));
            var top = IsLabelOutOfFlow ? 0 : labelSize.Height;
            field?.Arrange(new Rect(0, top, finalSize.Width, Math.Max(0, finalSize.Height - top)));
            return finalSize;
        }

        var width = Math.Min(label?.DesiredSize.Width ?? 0, finalSize.Width);
        var reversed = LabelPlacement == InputLabelPlacement.End;
        label?.Arrange(new Rect(reversed ? finalSize.Width - width : 0, 0, width, finalSize.Height));
        field?.Arrange(new Rect(reversed ? 0 : width, 0, Math.Max(0, finalSize.Width - width), finalSize.Height));
        return finalSize;
    }
}
