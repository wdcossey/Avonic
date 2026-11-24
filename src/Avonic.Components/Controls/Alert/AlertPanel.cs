using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// The alert's host as <c>alert.css</c> lays it out: <c>display: flex; align-items: center; justify-content: center</c> around the
/// wrapper, which is <c>--width</c> by <c>--height</c> within <c>--min-*</c> and <c>--max-*</c>. A width of <c>auto</c> is CSS's
/// fit-content: the wrapper's max-content (measured with no limit, so a long label or message counts at its full length), at most
/// the room. Each size is a number (px) or a CSS string: <c>px</c>, a percentage of the host, <c>vw</c> and <c>vh</c> (the host is
/// the window), a sum or difference of two (<c>100vh - 96px</c>), <c>min(a, b)</c>, or <c>auto</c> and <c>none</c>. The maximum
/// applies and then the minimum, which wins, as in CSS.
/// </summary>
public class AlertPanel : Panel
{
    public static readonly StyledProperty<object?> ContentWidthProperty =
        AvaloniaProperty.Register<AlertPanel, object?>(nameof(ContentWidth));

    public static readonly StyledProperty<object?> ContentMinWidthProperty =
        AvaloniaProperty.Register<AlertPanel, object?>(nameof(ContentMinWidth));

    public static readonly StyledProperty<object?> ContentMaxWidthProperty =
        AvaloniaProperty.Register<AlertPanel, object?>(nameof(ContentMaxWidth));

    public static readonly StyledProperty<object?> ContentHeightProperty =
        AvaloniaProperty.Register<AlertPanel, object?>(nameof(ContentHeight));

    public static readonly StyledProperty<object?> ContentMinHeightProperty =
        AvaloniaProperty.Register<AlertPanel, object?>(nameof(ContentMinHeight));

    public static readonly StyledProperty<object?> ContentMaxHeightProperty =
        AvaloniaProperty.Register<AlertPanel, object?>(nameof(ContentMaxHeight));

    private Size _content;

    static AlertPanel()
    {
        AffectsMeasure<AlertPanel>(ContentWidthProperty, ContentMinWidthProperty, ContentMaxWidthProperty, ContentHeightProperty,
            ContentMinHeightProperty, ContentMaxHeightProperty);
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

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        var height = availableSize.Height;
        var minWidth = CssLength.Read(ContentMinWidth, width, availableSize) ?? 0;
        var maxWidth = CssLength.Read(ContentMaxWidth, width, availableSize) ?? double.PositiveInfinity;
        var minHeight = CssLength.Read(ContentMinHeight, height, availableSize) ?? 0;
        var maxHeight = CssLength.Read(ContentMaxHeight, height, availableSize) ?? double.PositiveInfinity;
        var fixedWidth = CssLength.Read(ContentWidth, width, availableSize);
        var fixedHeight = CssLength.Read(ContentHeight, height, availableSize);

        var desired = default(Size);
        foreach (var child in Children)
        {
            double w;
            if (fixedWidth is { } fw)
            {
                w = Clamp(fw, minWidth, maxWidth);
            }
            else
            {
                // fit-content: the max-content, at most the room, then the maximum and the minimum.
                child.Measure(Size.Infinity);
                w = Clamp(Math.Min(child.DesiredSize.Width, width), minWidth, maxWidth);
            }

            double h;
            if (fixedHeight is { } fh)
            {
                h = Clamp(fh, minHeight, maxHeight);
            }
            else
            {
                child.Measure(new Size(w, double.PositiveInfinity));
                h = Clamp(child.DesiredSize.Height, minHeight, maxHeight);
            }

            child.Measure(new Size(w, h));
            desired = new Size(Math.Max(desired.Width, w), Math.Max(desired.Height, h));
        }

        _content = desired;
        return new Size(double.IsInfinity(width) ? desired.Width : width, double.IsInfinity(height) ? desired.Height : height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rect = new Rect(
            (finalSize.Width - _content.Width) / 2,
            (finalSize.Height - _content.Height) / 2,
            _content.Width,
            _content.Height);
        foreach (var child in Children)
        {
            child.Arrange(rect);
        }

        return finalSize;
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
}
