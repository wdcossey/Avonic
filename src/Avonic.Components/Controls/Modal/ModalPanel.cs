using System.Globalization;
using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// The modal's host as <c>modal.md.css</c> lays it out: <c>display: flex; align-items: center; justify-content: center</c> around
/// the wrapper, which is <c>--width</c> by <c>--height</c>, within <c>--min-*</c> and <c>--max-*</c>. Each size is a number (px) or a
/// CSS string: a percentage of the host, <c>px</c>, a content size (<c>auto</c>, <c>fit-content</c>, <c>max-content</c>,
/// <c>min-content</c>), or <c>none</c> for a maximum. As in CSS, the maximum applies and then the minimum, which wins, and a wrapper
/// larger than the host overflows both sides equally.
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

    private Size _content;

    static ModalPanel()
    {
        AffectsMeasure<ModalPanel>(ContentWidthProperty, ContentMinWidthProperty, ContentMaxWidthProperty, ContentHeightProperty,
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
        var minWidth = Length(ContentMinWidth, width) ?? 0;
        var maxWidth = Length(ContentMaxWidth, width) ?? double.PositiveInfinity;
        var minHeight = Length(ContentMinHeight, height) ?? 0;
        var maxHeight = Length(ContentMaxHeight, height) ?? double.PositiveInfinity;
        var fixedWidth = Length(ContentWidth, width) is { } w ? Clamp(w, minWidth, maxWidth) : (double?)null;
        var fixedHeight = Length(ContentHeight, height) is { } h ? Clamp(h, minHeight, maxHeight) : (double?)null;

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

    /// <summary>
    /// A CSS size in px, or null for one that isn't a length: a content size, <c>auto</c>, <c>none</c>, or nothing. A percentage of
    /// an infinite basis is null too.
    /// </summary>
    internal static double? Length(object? value, double basis)
    {
        switch (value)
        {
            case double d:
                return double.IsNaN(d) ? null : d;
            case int i:
                return i;
            case string s:
                s = s.Trim().ToLowerInvariant();
                if (s.EndsWith('%') && double.TryParse(s[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
                {
                    return double.IsInfinity(basis) ? null : basis * percent / 100;
                }

                if (s.EndsWith("px", StringComparison.Ordinal))
                {
                    s = s[..^2];
                }

                return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var px) ? px : null;
            default:
                return null;
        }
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
}
