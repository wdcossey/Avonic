using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// The popover's <c>.popover-wrapper</c>, its <c>wrapper</c>: the host's whole box, in which the content (<c>.popover-content</c>,
/// <c>position: absolute</c>) is <see cref="ContentWidth"/> by <see cref="ContentHeight"/> within their minimums and maximums,
/// each read as the modal's are (<see cref="ModalPanel"/>): a number (px), a percentage of the host, <c>auto</c>, or <c>none</c> for
/// a maximum. The popover gives it the place its enter animation works out; until then the content sits in the middle, where an
/// absolute box with no inset sits in the host's centring flex box. <see cref="OffsetX"/> and <see cref="OffsetY"/> are added to
/// the place, as <c>calc(… + var(--offset-x))</c>.
/// </summary>
public class PopoverPanel : Panel
{
    public static readonly StyledProperty<object?> ContentWidthProperty =
        AvaloniaProperty.Register<PopoverPanel, object?>(nameof(ContentWidth));

    public static readonly StyledProperty<object?> ContentMinWidthProperty =
        AvaloniaProperty.Register<PopoverPanel, object?>(nameof(ContentMinWidth));

    public static readonly StyledProperty<object?> ContentMaxWidthProperty =
        AvaloniaProperty.Register<PopoverPanel, object?>(nameof(ContentMaxWidth));

    public static readonly StyledProperty<object?> ContentHeightProperty =
        AvaloniaProperty.Register<PopoverPanel, object?>(nameof(ContentHeight));

    public static readonly StyledProperty<object?> ContentMinHeightProperty =
        AvaloniaProperty.Register<PopoverPanel, object?>(nameof(ContentMinHeight));

    public static readonly StyledProperty<object?> ContentMaxHeightProperty =
        AvaloniaProperty.Register<PopoverPanel, object?>(nameof(ContentMaxHeight));

    /// <summary><c>--offset-x</c>: px to the right of the place, whatever the direction.</summary>
    public static readonly StyledProperty<double> OffsetXProperty =
        AvaloniaProperty.Register<PopoverPanel, double>(nameof(OffsetX));

    /// <summary><c>--offset-y</c>: px down from the place.</summary>
    public static readonly StyledProperty<double> OffsetYProperty =
        AvaloniaProperty.Register<PopoverPanel, double>(nameof(OffsetY));

    /// <summary>
    /// A nested popover's <c>--offset-x</c>: the app's value (the first), if it's set; else md's (the second) on side <c>left</c>, and
    /// on <c>start</c> left to right and <c>end</c> right to left; minus it on the other two; 0 above and below. Then the side and the
    /// flow direction.
    /// </summary>
    public static readonly IMultiValueConverter NestedOffsetX = new FuncMultiValueConverter<object?, double>(values =>
    {
        var list = values.ToList();
        if (list.Count > 0 && Css.IsSet(list[0]) && list[0] is double or float or int)
        {
            return Convert.ToDouble(list[0], System.Globalization.CultureInfo.InvariantCulture);
        }

        var offset = list.Count > 1 && list[1] is double or float or int ? Convert.ToDouble(list[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
        var rtl = list.Count > 3 && list[3] is FlowDirection.RightToLeft;
        return (list.Count > 2 ? list[2] : null) switch
        {
            PopoverSide.Left => offset,
            PopoverSide.Right => -offset,
            PopoverSide.Start => rtl ? -offset : offset,
            PopoverSide.End => rtl ? offset : -offset,
            _ => 0,
        };
    });

    private Placement? _placement;
    private Size _content;

    static PopoverPanel()
    {
        AffectsMeasure<PopoverPanel>(ContentWidthProperty, ContentMinWidthProperty, ContentMaxWidthProperty, ContentHeightProperty,
            ContentMinHeightProperty, ContentMaxHeightProperty, OffsetYProperty);
        AffectsArrange<PopoverPanel>(OffsetXProperty);
    }

    /// <summary><c>--width</c>.</summary>
    public object? ContentWidth { get => GetValue(ContentWidthProperty); set => SetValue(ContentWidthProperty, value); }

    /// <summary><c>--min-width</c>.</summary>
    public object? ContentMinWidth { get => GetValue(ContentMinWidthProperty); set => SetValue(ContentMinWidthProperty, value); }

    /// <summary><c>--max-width</c>.</summary>
    public object? ContentMaxWidth { get => GetValue(ContentMaxWidthProperty); set => SetValue(ContentMaxWidthProperty, value); }

    /// <summary><c>--height</c>.</summary>
    public object? ContentHeight { get => GetValue(ContentHeightProperty); set => SetValue(ContentHeightProperty, value); }

    /// <summary><c>--min-height</c>.</summary>
    public object? ContentMinHeight { get => GetValue(ContentMinHeightProperty); set => SetValue(ContentMinHeightProperty, value); }

    /// <summary><c>--max-height</c>.</summary>
    public object? ContentMaxHeight { get => GetValue(ContentMaxHeightProperty); set => SetValue(ContentMaxHeightProperty, value); }

    /// <inheritdoc cref="OffsetXProperty"/>
    public double OffsetX { get => GetValue(OffsetXProperty); set => SetValue(OffsetXProperty, value); }

    /// <inheritdoc cref="OffsetYProperty"/>
    public double OffsetY { get => GetValue(OffsetYProperty); set => SetValue(OffsetYProperty, value); }

    /// <summary>
    /// Where the content goes, in the host's coordinates, left to right whatever the direction, as CSS's <c>left</c> is:
    /// <c>top</c>, <c>left</c>, maybe <c>bottom</c> (the box then reaching down to that far from the bottom), and the inline
    /// <c>--width</c> of <see cref="PopoverSize.Cover"/>. Null: none, as before the enter animation and after the leave.
    /// </summary>
    internal Placement? Place
    {
        get => _placement;
        set
        {
            _placement = value;
            InvalidateMeasure();
        }
    }

    /// <summary>The content's place: see <see cref="Place"/>. <paramref name="Mirrored"/>: the panel lays out right to left.</summary>
    internal readonly record struct Placement(double Top, double Left, double? Bottom, double? Width, bool Mirrored);

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        var height = availableSize.Height;
        var minWidth = ModalPanel.Length(ContentMinWidth, width) ?? 0;
        var maxWidth = ModalPanel.Length(ContentMaxWidth, width) ?? double.PositiveInfinity;
        var minHeight = ModalPanel.Length(ContentMinHeight, height) ?? 0;
        var maxHeight = ModalPanel.Length(ContentMaxHeight, height) ?? double.PositiveInfinity;
        var fixedWidth = (_placement?.Width ?? ModalPanel.Length(ContentWidth, width)) is { } w ? Clamp(w, minWidth, maxWidth) : (double?)null;
        double? fixedHeight = ModalPanel.Length(ContentHeight, height) is { } h ? Clamp(h, minHeight, maxHeight) : null;

        // height: auto with both top and bottom: what's between them.
        if (fixedHeight is null && _placement is { Bottom: { } bottom } place && !double.IsInfinity(height))
        {
            fixedHeight = Clamp(height - (place.Top + OffsetY) - bottom, minHeight, maxHeight);
        }

        var desired = default(Size);
        foreach (var child in Children)
        {
            // A content size is the child's own, up to the host and the maximum; a minimum larger than that is the arranged box.
            child.Measure(new Size(fixedWidth ?? Math.Min(width, maxWidth), fixedHeight ?? Math.Min(height, maxHeight)));
            var size = new Size(
                fixedWidth ?? Clamp(child.DesiredSize.Width, minWidth, maxWidth),
                fixedHeight ?? Clamp(child.DesiredSize.Height, minHeight, maxHeight));
            desired = new Size(Math.Max(desired.Width, size.Width), Math.Max(desired.Height, size.Height));
        }

        _content = desired;
        return new Size(double.IsInfinity(width) ? desired.Width : width, double.IsInfinity(height) ? desired.Height : height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Rect rect;
        if (_placement is { } place)
        {
            var left = place.Left + OffsetX;
            rect = new Rect(place.Mirrored ? finalSize.Width - left - _content.Width : left, place.Top + OffsetY, _content.Width, _content.Height);
        }
        else
        {
            rect = new Rect((finalSize.Width - _content.Width) / 2, (finalSize.Height - _content.Height) / 2, _content.Width, _content.Height);
        }

        foreach (var child in Children)
        {
            child.Arrange(rect);
        }

        return finalSize;
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
}
