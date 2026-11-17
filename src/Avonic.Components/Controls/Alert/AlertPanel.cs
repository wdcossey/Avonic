using System.Globalization;
using Avalonia;
using Avalonia.Controls;

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
        var minWidth = Length(ContentMinWidth, width) ?? 0;
        var maxWidth = Length(ContentMaxWidth, width) ?? double.PositiveInfinity;
        var minHeight = Length(ContentMinHeight, height) ?? 0;
        var maxHeight = Length(ContentMaxHeight, height) ?? double.PositiveInfinity;
        var fixedWidth = Length(ContentWidth, width);
        var fixedHeight = Length(ContentHeight, height);

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

    /// <summary>
    /// A CSS size in px, or null for one that isn't a length (<c>auto</c>, <c>none</c>, nothing, or a percentage of an infinite
    /// basis).
    /// </summary>
    internal static double? Length(object? value, double basis) => value switch
    {
        double d => double.IsNaN(d) ? null : d,
        int i => i,
        string s => Parse(s.Trim().ToLowerInvariant(), basis),
        _ => null,
    };

    private static double? Parse(string s, double basis)
    {
        if (s.StartsWith("min(", StringComparison.Ordinal) && s.EndsWith(')'))
        {
            var parts = s[4..^1].Split(',');
            double? min = null;
            foreach (var part in parts)
            {
                if (Parse(part.Trim(), basis) is not { } value)
                {
                    return null;
                }

                min = min is { } m ? Math.Min(m, value) : value;
            }

            return min;
        }

        // Sums and differences, as in calc(), left to right: split at the last operator of either kind.
        var minus = s.LastIndexOf(" - ", StringComparison.Ordinal);
        var plus = s.LastIndexOf(" + ", StringComparison.Ordinal);
        var at = Math.Max(minus, plus);
        if (at > 0)
        {
            return Parse(s[..at].Trim(), basis) is { } a && Parse(s[(at + 3)..].Trim(), basis) is { } b
                ? at == minus ? a - b : a + b
                : null;
        }

        foreach (var unit in new[] { "%", "vw", "vh" })
        {
            if (s.EndsWith(unit, StringComparison.Ordinal))
            {
                return double.TryParse(s[..^unit.Length], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent)
                       && !double.IsInfinity(basis)
                    ? basis * percent / 100
                    : null;
            }
        }

        if (s.EndsWith("px", StringComparison.Ordinal))
        {
            s = s[..^2];
        }

        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var px) ? px : null;
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(value, max));
}
