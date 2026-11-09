using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.checkbox-wrapper</c>, <c>display: flex</c> around two children: the label (<c>.label-text-wrapper</c>), then the box
/// (<c>.native-wrapper</c>), in that order. A row for <see cref="CheckboxLabelPlacement.Start"/> and
/// <see cref="CheckboxLabelPlacement.Fixed"/>, a reversed row for <see cref="CheckboxLabelPlacement.End"/>, and a column for
/// <see cref="CheckboxLabelPlacement.Stacked"/>, with <c>justify-content</c> from <see cref="Justify"/> and <c>align-items</c>
/// from <see cref="Alignment"/>. Avalonia mirrors it in RTL, as it does any panel.
/// </summary>
/// <remarks>
/// In a row the label has <c>overflow: hidden</c>, so no minimum, and the box can't shrink below its content, so the label takes
/// what the box leaves. In a column the label is as wide as its one line, up to <c>calc(100% / scale)</c>, the scale being its
/// <see cref="Css.ScaleProperty"/>, which the theme sets with the label's <c>scale()</c>.
/// </remarks>
public sealed class CheckboxWrapper : Panel
{
    /// <summary>The checkbox's <c>labelPlacement</c>.</summary>
    public static readonly StyledProperty<CheckboxLabelPlacement> LabelPlacementProperty =
        AvonicCheckbox.LabelPlacementProperty.AddOwner<CheckboxWrapper>();

    /// <summary>The checkbox's <c>justify</c>. Unset, it's <c>space-between</c>, or <c>start</c> with the label at the end.</summary>
    public static readonly StyledProperty<CheckboxJustify?> JustifyProperty =
        AvonicCheckbox.JustifyProperty.AddOwner<CheckboxWrapper>();

    /// <summary>The checkbox's <c>alignment</c>. Unset, it's <c>center</c>.</summary>
    public static readonly StyledProperty<CheckboxAlignment?> AlignmentProperty =
        AvonicCheckbox.AlignmentProperty.AddOwner<CheckboxWrapper>();

    static CheckboxWrapper()
    {
        AffectsMeasure<CheckboxWrapper>(LabelPlacementProperty, JustifyProperty, AlignmentProperty);
    }

    public CheckboxLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public CheckboxJustify? Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    public CheckboxAlignment? Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    /// <summary>The label's width on one line: its <c>max-content</c>.</summary>
    private double _labelWidth;

    /// <summary>The width the label was last measured at, in a row: infinite unless it had to shrink.</summary>
    private double _labelSpace = double.PositiveInfinity;

    private bool Stacked => LabelPlacement == CheckboxLabelPlacement.Stacked;

    /// <summary><c>calc(100% / 0.75)</c>'s divisor: the label's scale, or 1.</summary>
    private static double ScaleOf(Control label) => label.GetValue(Css.ScaleProperty) is > 0 and var scale ? scale : 1;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children is not [var label, var native])
        {
            return base.MeasureOverride(availableSize);
        }

        native.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var box = native.DesiredSize;

        if (Stacked)
        {
            // Shrink-to-fit on one line, up to the width over the scale. A label longer than that is that wide, and clipped: its
            // one line is kept, as the trimmed text is narrower.
            var limit = availableSize.Width / ScaleOf(label);
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _labelWidth = label.DesiredSize.Width;
            if (_labelWidth > limit)
            {
                label.Measure(new Size(limit, double.PositiveInfinity));
            }

            var width = Math.Max(_labelWidth, box.Width);
            return new Size(Math.Min(width, availableSize.Width), label.DesiredSize.Height + box.Height);
        }

        // flex-basis is the label's one line, and it shrinks to what the box leaves, which is then its width, though its text
        // is trimmed narrower.
        var space = Math.Max(0, availableSize.Width - box.Width);
        label.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        _labelWidth = label.DesiredSize.Width;
        _labelSpace = double.PositiveInfinity;
        if (_labelWidth > space)
        {
            label.Measure(new Size(space, availableSize.Height));
            _labelSpace = space;
        }

        return new Size(Math.Min(_labelWidth, space) + box.Width, Math.Max(label.DesiredSize.Height, box.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children is not [var label, var native])
        {
            return base.ArrangeOverride(finalSize);
        }

        var box = native.DesiredSize;
        var start = Alignment == CheckboxAlignment.Start;

        if (Stacked)
        {
            var labelWidth = Math.Min(_labelWidth, finalSize.Width / ScaleOf(label));
            var labelHeight = label.DesiredSize.Height;

            // justify-content on the column: space-between unless set. The column is as tall as both, unless something stretches it.
            var free = Math.Max(0, finalSize.Height - labelHeight - box.Height);
            var (labelY, boxY) = (Justify ?? CheckboxJustify.SpaceBetween) switch
            {
                CheckboxJustify.Start => (0d, labelHeight),
                CheckboxJustify.End => (free, free + labelHeight),
                _ => (0d, finalSize.Height - box.Height),
            };

            label.Arrange(new Rect(start ? 0 : (finalSize.Width - labelWidth) / 2, labelY, labelWidth, labelHeight));
            native.Arrange(new Rect(start ? 0 : (finalSize.Width - box.Width) / 2, boxY, box.Width, box.Height));
            return finalSize;
        }

        // The label takes what the box leaves, measured again if that's less than it was measured with.
        var labelSpace = Math.Max(0, finalSize.Width - box.Width);
        if (labelSpace < _labelSpace && labelSpace < _labelWidth)
        {
            label.Measure(new Size(labelSpace, finalSize.Height));
            _labelSpace = labelSpace;
        }

        var labelSize = new Size(Math.Min(_labelWidth, labelSpace), label.DesiredSize.Height);
        var end = LabelPlacement == CheckboxLabelPlacement.End;

        // row-reverse puts the box first. justify-content: start and end are the line's own start and end, not the flex
        // direction's, so the box stays on the left in either.
        var (first, firstSize, second, secondSize) = end ? (native, box, label, labelSize) : (label, labelSize, native, box);
        var justify = Justify ?? (end ? CheckboxJustify.Start : CheckboxJustify.SpaceBetween);
        var gap = Math.Max(0, finalSize.Width - firstSize.Width - secondSize.Width);
        var (firstX, secondX) = justify switch
        {
            CheckboxJustify.Start => (0d, firstSize.Width),
            CheckboxJustify.End => (gap, gap + firstSize.Width),
            _ => (0d, finalSize.Width - secondSize.Width),
        };

        first.Arrange(new Rect(firstX, Cross(firstSize.Height), firstSize.Width, firstSize.Height));
        second.Arrange(new Rect(secondX, Cross(secondSize.Height), secondSize.Width, secondSize.Height));
        return finalSize;

        double Cross(double height) => start ? 0 : (finalSize.Height - height) / 2;
    }
}
