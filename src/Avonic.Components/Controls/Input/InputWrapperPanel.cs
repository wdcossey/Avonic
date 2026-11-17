using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// The flex box of <c>ion-input</c>'s <c>.input-control</c>: the label part (the first child) and the native input (the second), in
/// the order and direction the placement gives. Since Ionic 9 the start and end slots are outside it, in <c>.input-start</c> and
/// <c>.input-end</c>. Measured in Chromium, see <c>docs/components/ion-input/PLAN.md</c>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>A row (start, fixed, and end, reversed): the label takes its width, which its <c>MaxWidth</c> limits, and the field the
/// rest, both the row's height (<c>align-items: stretch</c>). The label shrinks (<c>min-width: 0</c>) before the field goes below
/// its <c>MinWidth</c>, <c>min-width: 2ch</c>.</item>
/// <item>A column (floating, stacked): the label at the top, as wide as its content, and the field the rest of the height. The
/// label's room is <c>(width + slots − <see cref="LabelInset"/>) / <see cref="LabelScale"/></c>, as its <c>max-width</c> says,
/// since a transform doesn't change its box; the slots' width counts only with <see cref="LabelSpansSlots"/>.</item>
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

    /// <summary>
    /// The outline's floating label: its room adds the start and end containers' widths back, as the notch spans the whole
    /// outline (<c>--internal-start-end-container-width</c>).
    /// </summary>
    public static readonly StyledProperty<bool> LabelSpansSlotsProperty =
        AvaloniaProperty.Register<InputWrapperPanel, bool>(nameof(LabelSpansSlots));

    /// <summary>The width of <c>.input-start</c>, which the template binds. <c>createStartEndContainerController</c> measures it.</summary>
    public static readonly StyledProperty<double> StartWidthProperty =
        AvaloniaProperty.Register<InputWrapperPanel, double>(nameof(StartWidth));

    /// <summary>The width of <c>.input-end</c>, which the template binds.</summary>
    public static readonly StyledProperty<double> EndWidthProperty =
        AvaloniaProperty.Register<InputWrapperPanel, double>(nameof(EndWidth));

    /// <summary>
    /// How much of <c>translate(var(--internal-start-container-adjustment), …)</c> the label has: 0 at rest, 1 when the outline's
    /// label floats. A theme eases it with the float, and the converter multiplies it by the start width, so a change of the start
    /// slot moves the label at once, as <c>skip-label-transition</c> does.
    /// </summary>
    public static readonly AttachedProperty<double> StartShiftProperty =
        AvaloniaProperty.RegisterAttached<InputWrapperPanel, Control, double>("StartShift");

    static InputWrapperPanel()
    {
        AffectsMeasure<InputWrapperPanel>(LabelPlacementProperty, LabelScaleProperty, LabelInsetProperty, IsLabelOutOfFlowProperty,
            LabelSpansSlotsProperty);
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

    public bool LabelSpansSlots
    {
        get => GetValue(LabelSpansSlotsProperty);
        set => SetValue(LabelSpansSlotsProperty, value);
    }

    public double StartWidth
    {
        get => GetValue(StartWidthProperty);
        set => SetValue(StartWidthProperty, value);
    }

    public double EndWidth
    {
        get => GetValue(EndWidthProperty);
        set => SetValue(EndWidthProperty, value);
    }

    public static double GetStartShift(Control control) => control.GetValue(StartShiftProperty);

    public static void SetStartShift(Control control, double value) => control.SetValue(StartShiftProperty, value);

    /// <summary>The label's width with all the row's room, measured last: its flex basis.</summary>
    private double _labelBasis;

    private bool IsColumn => LabelPlacement is InputLabelPlacement.Floating or InputLabelPlacement.Stacked;

    private Control? Label => Children.Count > 0 ? Children[0] : null;

    private Control? Field => Children.Count > 1 ? Children[1] : null;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // The slots' widths change the label's room only when it spans them.
        if ((change.Property == StartWidthProperty || change.Property == EndWidthProperty) && LabelSpansSlots)
        {
            InvalidateMeasure();
        }
    }

    /// <summary>The field's <c>min-width</c>, with its margin: what the label leaves it in a row.</summary>
    private static double FieldMinimum(Control? field) =>
        field is null || !field.IsVisible ? 0 : field.MinWidth + field.Margin.Left + field.Margin.Right;

    protected override Size MeasureOverride(Size availableSize)
    {
        var label = Label;
        var field = Field;
        if (IsColumn)
        {
            var room = availableSize.Width;
            if (double.IsFinite(room))
            {
                var slots = LabelSpansSlots ? StartWidth + EndWidth : 0;
                room = Math.Max(0, room + slots - LabelInset) / (LabelScale > 0 ? LabelScale : 1);
            }

            label?.Measure(new Size(room, availableSize.Height));
            var labelHeight = IsLabelOutOfFlow ? 0 : label?.DesiredSize.Height ?? 0;
            field?.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - labelHeight)));
            var fieldSize = field?.DesiredSize ?? default;
            var labelWidth = IsLabelOutOfFlow ? 0 : Math.Min(label?.DesiredSize.Width ?? 0, availableSize.Width);
            return new Size(Math.Max(labelWidth, fieldSize.Width), labelHeight + fieldSize.Height);
        }

        // The label's own width first, as its flex basis: a trimmed label would want only its trimmed text.
        label?.Measure(availableSize);
        _labelBasis = label?.DesiredSize.Width ?? 0;
        var labelRoom = Math.Max(0, availableSize.Width - FieldMinimum(field));
        if (_labelBasis > labelRoom)
        {
            label!.Measure(availableSize.WithWidth(labelRoom));
        }

        var labelSize = new Size(Math.Min(_labelBasis, labelRoom), label?.DesiredSize.Height ?? 0);
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

        // min-width: 0 on the label and 2ch on the field: the label gives way first.
        var width = Math.Min(_labelBasis, Math.Max(0, finalSize.Width - FieldMinimum(field)));
        var reversed = LabelPlacement == InputLabelPlacement.End;
        label?.Arrange(new Rect(reversed ? finalSize.Width - width : 0, 0, width, finalSize.Height));
        field?.Arrange(new Rect(reversed ? 0 : width, 0, Math.Max(0, finalSize.Width - width), finalSize.Height));
        return finalSize;
    }
}
