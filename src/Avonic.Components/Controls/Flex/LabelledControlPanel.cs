using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>Where the label goes, as a form control's <c>labelPlacement</c> says.</summary>
public enum LabelledControlPlacement
{
    /// <summary>Before the control: on the left in LTR, on the right in RTL.</summary>
    Start,

    /// <summary>After the control: <c>flex-direction: row-reverse</c>.</summary>
    End,

    /// <summary>Before the control, at a width that doesn't shrink: <c>flex: 0 0 100px</c>, the width set by the theme.</summary>
    Fixed,

    /// <summary>Above the control: <c>flex-direction: column</c>.</summary>
    Stacked,
}

/// <summary>A form control's <c>justify</c>: <c>justify-content</c> along the line.</summary>
public enum LabelledControlJustify
{
    Start,
    End,
    SpaceBetween,
}

/// <summary>A form control's <c>alignment</c>: <c>align-items</c> across the line.</summary>
public enum LabelledControlAlignment
{
    Start,
    Center,
}

/// <summary>
/// The <c>.checkbox-wrapper</c>, <c>.radio-wrapper</c> and <c>.toggle-wrapper</c> of <c>ion-checkbox</c>, <c>ion-radio</c> and
/// <c>ion-toggle</c>: <c>display: flex</c> around two children, the label (<c>.label-text-wrapper</c>) first and the control's own
/// part (<c>.native-wrapper</c>) last, as their CSS lays them out for each <c>labelPlacement</c>, <c>justify</c> and
/// <c>alignment</c>. A child that isn't visible takes no part, as <c>display: none</c> doesn't. Avalonia mirrors it in RTL, as it
/// does any panel, so it lays out left to right.
/// </summary>
/// <remarks>
/// In a row the label has <c>overflow: hidden</c>, so no minimum, and the control's part can't shrink below its content, so the
/// label takes what the part leaves: its flex basis is its one line, and its width is that or the space left, whichever is less,
/// though its text is trimmed narrower. A fixed label doesn't shrink. In a column the label is as wide as its one line, up to
/// <c>calc(100% / scale)</c>, the scale being its <see cref="Css.ScaleProperty"/>, which a theme that scales it sets: md's 0.75.
/// Unset, it's 1, so a template that doesn't scale the label keeps it to the line. Each control keeps its own enums;
/// <see cref="FromControl"/> maps them onto this panel's by name in a template.
/// </remarks>
public sealed class LabelledControlPanel : Panel
{
    public static readonly StyledProperty<LabelledControlPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<LabelledControlPanel, LabelledControlPlacement>(nameof(LabelPlacement));

    /// <summary>Unset, it's <c>space-between</c>, or <see cref="EndJustify"/> with the label at the end.</summary>
    public static readonly StyledProperty<LabelledControlJustify?> JustifyProperty =
        AvaloniaProperty.Register<LabelledControlPanel, LabelledControlJustify?>(nameof(Justify));

    /// <summary>Unset, it's <c>center</c>.</summary>
    public static readonly StyledProperty<LabelledControlAlignment?> AlignmentProperty =
        AvaloniaProperty.Register<LabelledControlPanel, LabelledControlAlignment?>(nameof(Alignment));

    /// <summary>
    /// <c>justify-content</c> with the label at the end and no <see cref="Justify"/>: <c>start</c> for a checkbox and a toggle, whose
    /// end rule sets it, and <c>space-between</c> for a radio, whose end rule only reverses the row.
    /// </summary>
    public static readonly StyledProperty<LabelledControlJustify> EndJustifyProperty =
        AvaloniaProperty.Register<LabelledControlPanel, LabelledControlJustify>(nameof(EndJustify), LabelledControlJustify.Start);

    /// <summary>A control's own <c>LabelPlacement</c>, <c>Justify</c> or <c>Alignment</c>, as this panel's value of the same name.</summary>
    public static readonly IValueConverter FromControl = new SameNameEnumConverter();

    static LabelledControlPanel()
    {
        AffectsMeasure<LabelledControlPanel>(LabelPlacementProperty);
        AffectsArrange<LabelledControlPanel>(JustifyProperty, AlignmentProperty, EndJustifyProperty);
    }

    public LabelledControlPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public LabelledControlJustify? Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    public LabelledControlAlignment? Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    public LabelledControlJustify EndJustify
    {
        get => GetValue(EndJustifyProperty);
        set => SetValue(EndJustifyProperty, value);
    }

    /// <summary>The label's width on one line: its <c>max-content</c>.</summary>
    private double _labelWidth;

    private bool Stacked => LabelPlacement == LabelledControlPlacement.Stacked;

    private (Control? Label, Control? Native) Parts()
    {
        var label = Children.Count > 1 ? Children[0] : null;
        var native = Children.Count > 0 ? Children[^1] : null;
        return (label is { IsVisible: true } ? label : null, native is { IsVisible: true } ? native : null);
    }

    /// <summary><c>calc(100% / 0.75)</c>'s divisor: the label's <see cref="Css.ScaleProperty"/>, 1 for a label that isn't scaled.</summary>
    private static double ScaleOf(Control label) => label.GetValue(Css.ScaleProperty) is > 0 and var scale ? scale : 1;

    protected override Size MeasureOverride(Size availableSize)
    {
        var (label, native) = Parts();

        // The control's part keeps its min-content, so it's measured first, and the label gets what's left.
        native?.Measure(Size.Infinity);
        var box = native?.DesiredSize ?? default;
        _labelWidth = 0;

        if (Stacked)
        {
            // Shrink-to-fit on one line, up to the width over the scale. A label longer than that is that wide, and clipped.
            var labelSize = default(Size);
            if (label is not null)
            {
                var limit = availableSize.Width / ScaleOf(label);
                label.Measure(Size.Infinity);
                _labelWidth = label.DesiredSize.Width;
                if (_labelWidth > limit)
                {
                    label.Measure(new Size(limit, double.PositiveInfinity));
                }

                labelSize = label.DesiredSize;
            }

            var width = Math.Max(_labelWidth, box.Width);
            return new Size(Math.Min(width, availableSize.Width), labelSize.Height + box.Height);
        }

        // flex-basis is the label's one line, and it shrinks to what the part leaves. A fixed label is flex: 0 0 100px.
        var space = LabelSpace(availableSize.Width, box.Width);
        var rowLabel = default(Size);
        if (label is not null)
        {
            label.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            _labelWidth = label.DesiredSize.Width;
            if (_labelWidth > space)
            {
                label.Measure(new Size(space, availableSize.Height));
            }

            rowLabel = new Size(Math.Min(_labelWidth, space), label.DesiredSize.Height);
        }

        return new Size(rowLabel.Width + box.Width, Math.Max(rowLabel.Height, box.Height));
    }

    private double LabelSpace(double width, double box) =>
        LabelPlacement == LabelledControlPlacement.Fixed ? double.PositiveInfinity : Math.Max(0, width - box);

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (label, native) = Parts();
        var box = native?.DesiredSize ?? default;

        if (Stacked)
        {
            ArrangeColumn(label, native, box, finalSize);
        }
        else
        {
            ArrangeRow(label, native, box, finalSize);
        }

        return finalSize;
    }

    /// <summary><c>flex-direction: row</c>, or <c>row-reverse</c> for end, with <c>align-items</c> across.</summary>
    private void ArrangeRow(Control? label, Control? native, Size box, Size finalSize)
    {
        // The label takes what the part leaves. Measured again only in a measure, as a measure here would ask for another one; a
        // TextBlock arranged narrower trims to it.
        var labelWidth = label is null ? 0 : Math.Min(_labelWidth, LabelSpace(finalSize.Width, box.Width));
        var reverse = LabelPlacement == LabelledControlPlacement.End;

        // The wrapper is space-between, and end's own rule may set start. A justify rule comes after both.
        var justify = Justify ?? (reverse ? EndJustify : LabelledControlJustify.SpaceBetween);

        // Left to right, as the line is laid out in LTR: row-reverse puts the label after the part.
        var (first, firstWidth, second, secondWidth) = reverse ? (native, box.Width, label, labelWidth) : (label, labelWidth, native, box.Width);
        var free = finalSize.Width - firstWidth - secondWidth;

        double x1, x2;
        if (justify == LabelledControlJustify.SpaceBetween)
        {
            // Two items at either end. One alone, or no room, falls back to flex-start, which is the main start: the right for
            // row-reverse.
            if (first is not null && second is not null && free >= 0)
            {
                (x1, x2) = (0, finalSize.Width - secondWidth);
            }
            else if (reverse)
            {
                x2 = finalSize.Width - secondWidth;
                x1 = x2 - firstWidth;
            }
            else
            {
                (x1, x2) = (0, firstWidth);
            }
        }
        else
        {
            // start and end are the line's inline start and end, whatever the flex direction: measured.
            x1 = justify == LabelledControlJustify.End ? free : 0;
            x2 = x1 + firstWidth;
        }

        Place(first, x1, firstWidth, finalSize.Height);
        Place(second, x2, secondWidth, finalSize.Height);
    }

    private void Place(Control? child, double x, double width, double height)
    {
        if (child is null)
        {
            return;
        }

        var y = Alignment == LabelledControlAlignment.Start ? 0 : (height - child.DesiredSize.Height) / 2;
        child.Arrange(new Rect(x, y, width, child.DesiredSize.Height));
    }

    /// <summary><c>flex-direction: column</c>: the label above the part, packed by <c>justify</c>, and centred across.</summary>
    private void ArrangeColumn(Control? label, Control? native, Size box, Size finalSize)
    {
        var labelSize = label is null
            ? default
            : new Size(Math.Min(_labelWidth, finalSize.Width / ScaleOf(label)), label.DesiredSize.Height);
        var justify = Justify ?? LabelledControlJustify.SpaceBetween;
        var free = finalSize.Height - labelSize.Height - box.Height;

        double yLabel, yNative;
        if (justify == LabelledControlJustify.SpaceBetween && label is not null && native is not null && free >= 0)
        {
            (yLabel, yNative) = (0, finalSize.Height - box.Height);
        }
        else
        {
            yLabel = justify == LabelledControlJustify.End ? free : 0;
            yNative = yLabel + labelSize.Height;
        }

        // A stacked label may be wider than the line, by up to a third, and centring then hangs it over both sides.
        double Across(double width) => Alignment == LabelledControlAlignment.Start ? 0 : (finalSize.Width - width) / 2;
        label?.Arrange(new Rect(Across(labelSize.Width), yLabel, labelSize.Width, labelSize.Height));
        native?.Arrange(new Rect(Across(box.Width), yNative, box.Width, box.Height));
    }

    /// <summary>One enum's value as another's of the same name; null stays null.</summary>
    private sealed class SameNameEnumConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Enum source)
            {
                return null;
            }

            var target = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return target.IsEnum ? Enum.Parse(target, source.ToString()) : value;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
