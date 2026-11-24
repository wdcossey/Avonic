using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// The modal's host as <c>modal.md.css</c> lays it out: <c>display: flex; align-items: center; justify-content: center</c> around
/// the wrapper, which is <c>--width</c> by <c>--height</c>, within <c>--min-*</c> and <c>--max-*</c>. Each size is a number (px) or a
/// CSS string read by <see cref="CssLength"/>: a percentage of the host, <c>px</c>, <c>vw</c> and <c>vh</c> of the window, <c>calc()</c>,
/// <c>min()</c>, a content size (<c>auto</c>, <c>fit-content</c>, <c>max-content</c>, <c>min-content</c>), or <c>none</c> for a maximum. As in CSS, the
/// maximum applies and then the minimum, which wins, and a wrapper larger than the host overflows both sides equally. A sheet's
/// wrapper is <c>position: absolute; bottom: 0</c>: <see cref="VerticalContentAlignment"/> <see cref="VerticalAlignment.Bottom"/>.
/// </summary>
public class ModalPanel : Panel
{
    public static readonly StyledProperty<object?> ContentWidthProperty =
        AvaloniaProperty.Register<ModalPanel, object?>(nameof(ContentWidth));

    public static readonly StyledProperty<object?> ContentMinWidthProperty =
        AvaloniaProperty.Register<ModalPanel, object?>(nameof(ContentMinWidth));

    public static readonly StyledProperty<object?> ContentMaxWidthProperty =
        AvaloniaProperty.Register<ModalPanel, object?>(nameof(ContentMaxWidth));

    public static readonly StyledProperty<object?> ContentHeightProperty =
        AvaloniaProperty.Register<ModalPanel, object?>(nameof(ContentHeight));

    public static readonly StyledProperty<object?> ContentMinHeightProperty =
        AvaloniaProperty.Register<ModalPanel, object?>(nameof(ContentMinHeight));

    public static readonly StyledProperty<object?> ContentMaxHeightProperty =
        AvaloniaProperty.Register<ModalPanel, object?>(nameof(ContentMaxHeight));

    /// <summary>Where the wrapper sits up and down: centred, as the host's flex box puts it, or at the bottom, as a sheet's.</summary>
    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<ModalPanel>(new(VerticalAlignment.Center));

    /// <summary>
    /// A sheet's <c>--height</c>: <c>Avonic.Modal.Height</c> if the app set it, else <c>calc(100% - (offset + gap))</c>, the offset
    /// being <c>Avonic.ModalOffsetTop</c> (Ionic's <c>--ion-modal-offset-top</c>) and the gap the parameter, md's 10 px.
    /// </summary>
    public static readonly IMultiValueConverter SheetHeight = new SheetHeightConverter();

    private Size _content;

    static ModalPanel()
    {
        AffectsMeasure<ModalPanel>(ContentWidthProperty, ContentMinWidthProperty, ContentMaxWidthProperty, ContentHeightProperty,
            ContentMinHeightProperty, ContentMaxHeightProperty);
        AffectsArrange<ModalPanel>(VerticalContentAlignmentProperty);
    }

    /// <summary><c>--width</c>.</summary>
    public object? ContentWidth { get => GetValue(ContentWidthProperty); set => SetValue(ContentWidthProperty, value); }

    /// <summary><c>--min-width</c>.</summary>
    public object? ContentMinWidth { get => GetValue(ContentMinWidthProperty); set => SetValue(ContentMinWidthProperty, value); }

    /// <summary><c>--max-width</c>.</summary>
    public object? ContentMaxWidth { get => GetValue(ContentMaxWidthProperty); set => SetValue(ContentMaxWidthProperty, value); }

    /// <inheritdoc cref="VerticalContentAlignmentProperty"/>
    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    /// <summary><c>--height</c>.</summary>
    public object? ContentHeight { get => GetValue(ContentHeightProperty); set => SetValue(ContentHeightProperty, value); }

    /// <summary><c>--min-height</c>.</summary>
    public object? ContentMinHeight { get => GetValue(ContentMinHeightProperty); set => SetValue(ContentMinHeightProperty, value); }

    /// <summary><c>--max-height</c>.</summary>
    public object? ContentMaxHeight { get => GetValue(ContentMaxHeightProperty); set => SetValue(ContentMaxHeightProperty, value); }

    private IDisposable? _viewport;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewport = CssLength.MeasureOnViewportChange(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _viewport?.Dispose();
        _viewport = null;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        var height = availableSize.Height;
        var viewport = TopLevel.GetTopLevel(this)?.ClientSize ?? availableSize;
        var minWidth = CssLength.Read(ContentMinWidth, width, viewport) ?? 0;
        var maxWidth = CssLength.Read(ContentMaxWidth, width, viewport) ?? double.PositiveInfinity;
        var minHeight = CssLength.Read(ContentMinHeight, height, viewport) ?? 0;
        var maxHeight = CssLength.Read(ContentMaxHeight, height, viewport) ?? double.PositiveInfinity;
        var fixedWidth = CssLength.Read(ContentWidth, width, viewport) is { } w ? Clamp(w, minWidth, maxWidth) : (double?)null;
        var fixedHeight = CssLength.Read(ContentHeight, height, viewport) is { } h ? Clamp(h, minHeight, maxHeight) : (double?)null;

        var desired = default(Size);
        foreach (var child in Children)
        {
            // A content size is the child's own, up to the host and the maximum.
            child.Measure(new Size(fixedWidth ?? Math.Min(width, maxWidth), fixedHeight ?? Math.Min(height, maxHeight)));
            var size = new Size(
                fixedWidth ?? Clamp(child.DesiredSize.Width, minWidth, maxWidth),
                fixedHeight ?? Clamp(child.DesiredSize.Height, minHeight, maxHeight));
            child.Measure(size);
            desired = new Size(Math.Max(desired.Width, size.Width), Math.Max(desired.Height, size.Height));
        }

        _content = desired;
        return new Size(double.IsInfinity(width) ? desired.Width : width, double.IsInfinity(height) ? desired.Height : height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var top = VerticalContentAlignment switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Bottom => finalSize.Height - _content.Height,
            _ => (finalSize.Height - _content.Height) / 2,
        };
        var rect = new Rect((finalSize.Width - _content.Width) / 2, top, _content.Width, _content.Height);
        foreach (var child in Children)
        {
            child.Arrange(rect);
        }

        return finalSize;
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));

    /// <summary><see cref="SheetHeight"/>.</summary>
    private sealed class SheetHeightConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count > 0 && Css.IsSet(values[0]))
            {
                return values[0];
            }

            var offset = values.Count > 1 && values[1] is double d ? d : 0;
            var gap = parameter switch
            {
                double g => g,
                string g when double.TryParse(g, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => 0,
            };
            return FormattableString.Invariant($"calc(100% - {offset + gap}px)");
        }
    }
}
