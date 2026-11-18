using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// The menu's host as <c>menu.md.css</c> places <c>.menu-inner</c> in it: <c>position: absolute; top: 0; bottom: 0</c> at the left
/// edge, or the right with <see cref="IsEndSide"/>, <c>--width</c> by <c>--height</c> within <c>--min-*</c> and <c>--max-*</c>. Each
/// size is a number (px) or a CSS string, as the alert reads them (<see cref="AlertPanel.Length(object?, double, Size)"/>): a
/// percentage of the host, <c>px</c>, <c>vw</c> and <c>vh</c> of the top level, <c>calc()</c>, <c>min()</c>, or <c>auto</c>. A width of
/// <c>auto</c> is shrink-to-fit, the content's max-content at most the host's width; a height of <c>auto</c> is the host's, as
/// <c>top: 0; bottom: 0</c> makes it. The maximum applies and then the minimum, which wins, as in CSS. It's laid out left to right,
/// so its side is physical, as <c>isEndSide</c> is.
/// </summary>
public class MenuPanel : Panel
{
    public static readonly StyledProperty<object?> ContentWidthProperty =
        AvaloniaProperty.Register<MenuPanel, object?>(nameof(ContentWidth));

    public static readonly StyledProperty<object?> ContentMinWidthProperty =
        AvaloniaProperty.Register<MenuPanel, object?>(nameof(ContentMinWidth));

    public static readonly StyledProperty<object?> ContentMaxWidthProperty =
        AvaloniaProperty.Register<MenuPanel, object?>(nameof(ContentMaxWidth));

    public static readonly StyledProperty<object?> ContentHeightProperty =
        AvaloniaProperty.Register<MenuPanel, object?>(nameof(ContentHeight));

    public static readonly StyledProperty<object?> ContentMinHeightProperty =
        AvaloniaProperty.Register<MenuPanel, object?>(nameof(ContentMinHeight));

    public static readonly StyledProperty<object?> ContentMaxHeightProperty =
        AvaloniaProperty.Register<MenuPanel, object?>(nameof(ContentMaxHeight));

    /// <summary>The content is at the right edge: Ionic's <c>isEndSide</c>, physical.</summary>
    public static readonly StyledProperty<bool> IsEndSideProperty =
        AvaloniaProperty.Register<MenuPanel, bool>(nameof(IsEndSide));

    private Size _content;

    static MenuPanel()
    {
        AffectsMeasure<MenuPanel>(ContentWidthProperty, ContentMinWidthProperty, ContentMaxWidthProperty, ContentHeightProperty,
            ContentMinHeightProperty, ContentMaxHeightProperty);
        AffectsArrange<MenuPanel>(IsEndSideProperty);
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

    /// <inheritdoc cref="IsEndSideProperty"/>
    public bool IsEndSide { get => GetValue(IsEndSideProperty); set => SetValue(IsEndSideProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        var height = availableSize.Height;
        var viewport = TopLevel.GetTopLevel(this)?.ClientSize ?? availableSize;
        var minWidth = AlertPanel.Length(ContentMinWidth, width, viewport) ?? 0;
        var maxWidth = AlertPanel.Length(ContentMaxWidth, width, viewport) ?? double.PositiveInfinity;
        var minHeight = AlertPanel.Length(ContentMinHeight, height, viewport) ?? 0;
        var maxHeight = AlertPanel.Length(ContentMaxHeight, height, viewport) ?? double.PositiveInfinity;
        var fixedWidth = AlertPanel.Length(ContentWidth, width, viewport);
        var fixedHeight = AlertPanel.Length(ContentHeight, height, viewport);

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
                // Shrink-to-fit: the max-content, at most the room, then the maximum and the minimum.
                child.Measure(Size.Infinity);
                w = Clamp(Math.Min(child.DesiredSize.Width, width), minWidth, maxWidth);
            }

            var h = Clamp(fixedHeight ?? (double.IsInfinity(height) ? 0 : height), minHeight, maxHeight);
            if (fixedHeight is null && double.IsInfinity(height))
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
        var rect = new Rect(IsEndSide ? finalSize.Width - _content.Width : 0, 0, _content.Width, _content.Height);
        foreach (var child in Children)
        {
            child.Arrange(rect);
        }

        return finalSize;
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
}
