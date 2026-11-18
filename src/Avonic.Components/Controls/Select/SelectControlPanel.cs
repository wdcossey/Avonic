using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// The flex box of <c>ion-select</c>'s <c>.select-control</c>: the label part (the first child) and <c>.native-wrapper</c> (the
/// second), in the order and direction the placement gives. Measured in Chromium, see <c>docs/components/ion-select/PLAN.md</c>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>A row (start, fixed; end, reversed): both as wide as their content, packed by <see cref="Justify"/> (null is the
/// wrapper's <c>space-between</c>, which <c>justify-content: inherit</c> takes), and centred across. The container gives way
/// first (<c>flex-shrink: 100000</c>), down to nothing (<c>overflow: hidden</c>); then the label (<c>min-width: 0</c>).</item>
/// <item>A column (floating, stacked): the label at the top, as wide as its content within
/// <c>(width + slots − <see cref="LabelInset"/>) / <see cref="LabelScale"/></c>, and the container the row's width and the rest
/// of its height (<c>flex-grow: 1; width: 100%</c>). <see cref="IsLabelOutOfFlow"/> is the outline's <c>position: absolute</c>
/// label, which takes no room.</item>
/// </list>
/// <c>start</c> and <c>end</c> in <c>justify-content</c> are the writing mode's, not the flex line's, so a reversed row packs to
/// the same side. Avalonia mirrors it in RTL, as it does any panel.
/// </remarks>
public sealed class SelectControlPanel : Panel
{
    public static readonly StyledProperty<SelectLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<SelectControlPanel, SelectLabelPlacement>(nameof(LabelPlacement));

    /// <summary>The select's <c>justify</c>, which applies only in a row. Null is <c>space-between</c>.</summary>
    public static readonly StyledProperty<SelectJustify?> JustifyProperty =
        AvaloniaProperty.Register<SelectControlPanel, SelectJustify?>(nameof(Justify));

    /// <summary>The scale in the label's <c>max-width: calc(100% / scale)</c>, in a column. 1 when it isn't scaled.</summary>
    public static readonly StyledProperty<double> LabelScaleProperty =
        AvaloniaProperty.Register<SelectControlPanel, double>(nameof(LabelScale), 1);

    /// <summary>What's taken from the width before the scale, in a column: the outline's 8 px.</summary>
    public static readonly StyledProperty<double> LabelInsetProperty =
        AvaloniaProperty.Register<SelectControlPanel, double>(nameof(LabelInset));

    /// <summary>The label is <c>position: absolute</c>: it takes no room, at the top start.</summary>
    public static readonly StyledProperty<bool> IsLabelOutOfFlowProperty =
        AvaloniaProperty.Register<SelectControlPanel, bool>(nameof(IsLabelOutOfFlow));

    /// <summary>The outline's floating label: its room adds the start and end containers' widths back.</summary>
    public static readonly StyledProperty<bool> LabelSpansSlotsProperty =
        AvaloniaProperty.Register<SelectControlPanel, bool>(nameof(LabelSpansSlots));

    /// <summary>The width of <c>.select-start</c>, which the template binds.</summary>
    public static readonly StyledProperty<double> StartWidthProperty =
        AvaloniaProperty.Register<SelectControlPanel, double>(nameof(StartWidth));

    /// <summary>The width of <c>.select-end</c>, which the template binds.</summary>
    public static readonly StyledProperty<double> EndWidthProperty =
        AvaloniaProperty.Register<SelectControlPanel, double>(nameof(EndWidth));

    /// <summary>
    /// How much of <c>translate(var(--internal-start-container-adjustment), …)</c> the label has: 0 at rest, 1 when the outline's
    /// label floats. The theme eases it with the float.
    /// </summary>
    public static readonly AttachedProperty<double> StartShiftProperty =
        AvaloniaProperty.RegisterAttached<SelectControlPanel, Control, double>("StartShift");

    static SelectControlPanel()
    {
        AffectsMeasure<SelectControlPanel>(LabelPlacementProperty, JustifyProperty, LabelScaleProperty, LabelInsetProperty,
            IsLabelOutOfFlowProperty, LabelSpansSlotsProperty);
    }

    public SelectLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public SelectJustify? Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
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

    private bool IsColumn => LabelPlacement is SelectLabelPlacement.Floating or SelectLabelPlacement.Stacked;

    private Control? Label => Children.Count > 0 && Children[0].IsVisible ? Children[0] : null;

    private Control? Container => Children.Count > 1 ? Children[1] : null;

    /// <summary>The widths given at the last measure: the label's and the container's.</summary>
    private double _labelWidth;
    private double _containerWidth;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if ((change.Property == StartWidthProperty || change.Property == EndWidthProperty) && LabelSpansSlots)
        {
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var label = Label;
        var container = Container;
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
            container?.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - labelHeight)));
            var containerSize = container?.DesiredSize ?? default;
            var labelWidth = IsLabelOutOfFlow ? 0 : Math.Min(label?.DesiredSize.Width ?? 0, availableSize.Width);
            return new Size(Math.Max(labelWidth, containerSize.Width), labelHeight + containerSize.Height);
        }

        // Each at its content's width first, its flex basis.
        label?.Measure(availableSize.WithWidth(double.PositiveInfinity));
        container?.Measure(availableSize.WithWidth(double.PositiveInfinity));
        var labelBasis = label?.DesiredSize.Width ?? 0;
        var containerBasis = container?.DesiredSize.Width ?? 0;

        _labelWidth = labelBasis;
        _containerWidth = containerBasis;
        if (double.IsFinite(availableSize.Width) && labelBasis + containerBasis > availableSize.Width)
        {
            // The container gives way first, to nothing; then the label, down to its margin.
            _containerWidth = Math.Max(0, availableSize.Width - labelBasis);
            _labelWidth = Math.Min(labelBasis, availableSize.Width);
            label?.Measure(availableSize.WithWidth(_labelWidth));
            container?.Measure(availableSize.WithWidth(_containerWidth));
        }

        var height = Math.Max(label?.DesiredSize.Height ?? 0, container?.DesiredSize.Height ?? 0);
        return new Size(_labelWidth + _containerWidth, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var label = Label;
        var container = Container;
        if (IsColumn)
        {
            var labelSize = label?.DesiredSize ?? default;
            // align-items: start: as wide as its content, which can be wider than the row before the scale.
            label?.Arrange(new Rect(0, 0, labelSize.Width, labelSize.Height));
            var top = IsLabelOutOfFlow ? 0 : labelSize.Height;
            container?.Arrange(new Rect(0, top, finalSize.Width, Math.Max(0, finalSize.Height - top)));
            return finalSize;
        }

        var reversed = LabelPlacement == SelectLabelPlacement.End;
        var free = Math.Max(0, finalSize.Width - _labelWidth - _containerWidth);

        // The line's two boxes, left to right: the label first, or the container first when reversed.
        var (first, firstWidth, second, secondWidth) = reversed
            ? (container, _containerWidth, label, _labelWidth)
            : (label, _labelWidth, container, _containerWidth);
        var (firstX, secondX) = Justify switch
        {
            SelectJustify.Start => (0d, firstWidth),
            SelectJustify.End => (free, free + firstWidth),
            _ => (0d, firstWidth + free),
        };

        // align-items: center, each its own height.
        Place(first, firstX, firstWidth, finalSize.Height);
        Place(second, secondX, secondWidth, finalSize.Height);
        return finalSize;
    }

    private static void Place(Control? child, double x, double width, double height)
    {
        if (child is null)
        {
            return;
        }

        var childHeight = Math.Min(child.DesiredSize.Height, height);
        child.Arrange(new Rect(x, (height - childHeight) / 2, width, childHeight));
    }
}
