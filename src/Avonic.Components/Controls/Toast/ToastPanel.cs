using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// The <see cref="AvonicToast"/>'s host, which places its one child, the wrapper, as <c>.toast-wrapper</c> is placed in
/// <c>toast.md.css</c>: <c>position: absolute</c> between <see cref="Start"/> and <see cref="End"/> (<c>inset-inline</c>, so
/// mirrored in a right-to-left flow by Avalonia), <c>margin-inline: auto</c>, so that with <c>width: auto</c> it fills between them
/// up to <see cref="ContentMaxWidth"/>, and is centred between them when it doesn't fill; as tall as its content within the height
/// keys; at the top (<c>top: 0</c>), the bottom (<c>bottom: 0</c>), or in the middle at <see cref="MiddleTop"/>, which the enter
/// animation sets (Ionic's inline <c>top</c>), and is 0, the static position, until then.
/// </summary>
/// <remarks>
/// Sizes are CSS's content-box ones (the wrapper's <c>box-sizing</c>), so the wrapper's border is added to them; each is a number
/// (px) or a CSS string, read by <see cref="CssLength.Read(object?, double, Size)"/>: a percentage of the window's width or
/// height, <c>auto</c> for none. An over-constrained width (wider than the room between the insets) starts at the start inset, as
/// CSS 2.1 §10.3.7 has it.
/// </remarks>
public class ToastPanel : Panel
{
    public static readonly StyledProperty<object?> StartProperty =
        AvaloniaProperty.Register<ToastPanel, object?>(nameof(Start));

    public static readonly StyledProperty<object?> EndProperty =
        AvaloniaProperty.Register<ToastPanel, object?>(nameof(End));

    public static readonly StyledProperty<object?> ContentWidthProperty =
        AvaloniaProperty.Register<ToastPanel, object?>(nameof(ContentWidth));

    public static readonly StyledProperty<object?> ContentMinWidthProperty =
        AvaloniaProperty.Register<ToastPanel, object?>(nameof(ContentMinWidth));

    public static readonly StyledProperty<object?> ContentMaxWidthProperty =
        AvaloniaProperty.Register<ToastPanel, object?>(nameof(ContentMaxWidth));

    public static readonly StyledProperty<object?> ContentHeightProperty =
        AvaloniaProperty.Register<ToastPanel, object?>(nameof(ContentHeight));

    public static readonly StyledProperty<object?> ContentMinHeightProperty =
        AvaloniaProperty.Register<ToastPanel, object?>(nameof(ContentMinHeight));

    public static readonly StyledProperty<object?> ContentMaxHeightProperty =
        AvaloniaProperty.Register<ToastPanel, object?>(nameof(ContentMaxHeight));

    /// <summary>The toast's <see cref="AvonicToast.Position"/>: which edge the wrapper is at.</summary>
    public static readonly StyledProperty<ToastPosition> PositionProperty =
        AvaloniaProperty.Register<ToastPanel, ToastPosition>(nameof(Position));

    /// <summary>The middle's <c>top</c>, from the enter animation; NaN, the default, is 0.</summary>
    public static readonly StyledProperty<double> MiddleTopProperty =
        AvaloniaProperty.Register<ToastPanel, double>(nameof(MiddleTop), double.NaN);

    static ToastPanel()
    {
        AffectsMeasure<ToastPanel>(StartProperty, EndProperty, ContentWidthProperty, ContentMinWidthProperty, ContentMaxWidthProperty,
            ContentHeightProperty, ContentMinHeightProperty, ContentMaxHeightProperty);
        AffectsArrange<ToastPanel>(PositionProperty, MiddleTopProperty);
    }

    public object? Start { get => GetValue(StartProperty); set => SetValue(StartProperty, value); }

    public object? End { get => GetValue(EndProperty); set => SetValue(EndProperty, value); }

    public object? ContentWidth { get => GetValue(ContentWidthProperty); set => SetValue(ContentWidthProperty, value); }

    public object? ContentMinWidth { get => GetValue(ContentMinWidthProperty); set => SetValue(ContentMinWidthProperty, value); }

    public object? ContentMaxWidth { get => GetValue(ContentMaxWidthProperty); set => SetValue(ContentMaxWidthProperty, value); }

    public object? ContentHeight { get => GetValue(ContentHeightProperty); set => SetValue(ContentHeightProperty, value); }

    public object? ContentMinHeight { get => GetValue(ContentMinHeightProperty); set => SetValue(ContentMinHeightProperty, value); }

    public object? ContentMaxHeight { get => GetValue(ContentMaxHeightProperty); set => SetValue(ContentMaxHeightProperty, value); }

    public ToastPosition Position { get => GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    public double MiddleTop { get => GetValue(MiddleTopProperty); set => SetValue(MiddleTopProperty, value); }

    private Rect _slot;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        var height = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;
        var viewport = new Size(width, height);
        var start = CssLength.Read(Start, width, viewport) ?? 0;
        var end = CssLength.Read(End, width, viewport) ?? 0;
        var room = Math.Max(0, width - start - end);

        foreach (var child in Children)
        {
            var border = child is Border b ? b.BorderThickness : default;
            var borderWidth = border.Left + border.Right;
            var borderHeight = border.Top + border.Bottom;

            var minWidth = CssLength.Read(ContentMinWidth, width, viewport) ?? 0;
            var maxWidth = CssLength.Read(ContentMaxWidth, width, viewport) ?? double.PositiveInfinity;
            var contentWidth = CssLength.Read(ContentWidth, width, viewport) ?? Math.Max(0, room - borderWidth);
            contentWidth = Math.Max(minWidth, Math.Min(contentWidth, maxWidth));
            var outerWidth = contentWidth + borderWidth;

            var minHeight = CssLength.Read(ContentMinHeight, height, viewport) ?? 0;
            var maxHeight = CssLength.Read(ContentMaxHeight, height, viewport) ?? double.PositiveInfinity;
            var fixedHeight = CssLength.Read(ContentHeight, height, viewport);
            // Measured with no limit down, always: a limit would clamp what it wants, and a later change inside, measured against the
            // same limit, would then never reach this panel. The heights are applied at the arrange.
            child.Measure(new Size(outerWidth, double.PositiveInfinity));
            var contentHeight = fixedHeight ?? child.DesiredSize.Height - borderHeight;
            contentHeight = Math.Max(minHeight, Math.Min(contentHeight, maxHeight));
            var outerHeight = contentHeight + borderHeight;

            // margin-inline: auto splits what's left; none is negative, so an over-wide wrapper starts at the start inset.
            var x = start + Math.Max(0, (room - outerWidth) / 2);
            _slot = new Rect(x, 0, outerWidth, outerHeight);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            var y = Position switch
            {
                ToastPosition.Top => 0,
                ToastPosition.Bottom => finalSize.Height - _slot.Height,
                _ => double.IsNaN(MiddleTop) ? 0 : MiddleTop,
            };
            child.Arrange(_slot.WithY(y));
        }

        return finalSize;
    }
}
