using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>border-radius: var(--border-radius)</c>, for <see cref="AvonicAvatar"/> and <see cref="AvonicThumbnail"/>, whose default
/// is <c>50%</c>, which no <see cref="CornerRadius"/> can say. The variable's key can hold a <see cref="CornerRadius"/>, a number,
/// or a string: a percentage such as <c>50%</c>, which is that fraction of the box's width across and of its height down, as in
/// CSS, or anything <see cref="CornerRadius.Parse"/> reads, with or without <c>px</c>. Anything else is invalid, and an invalid
/// <c>var()</c> leaves <c>border-radius</c> at its initial value, 0. The values are the key's value, then the element's
/// <c>Bounds</c>.
/// </summary>
public static class BorderRadius
{
    /// <summary>The radius as a <see cref="CornerRadius"/>, for a <c>Border</c>'s background.</summary>
    public static readonly IMultiValueConverter ToCornerRadius = new CornerRadiusConverter();

    /// <summary>The radius as a <c>Clip</c>, for an image with <c>overflow: hidden</c>. Null when there are no round corners.</summary>
    public static readonly IMultiValueConverter ToClip = new ClipConverter();

    /// <summary>The four corners' radii across and down, in the order top left, top right, bottom right, bottom left.</summary>
    private readonly record struct Radii(Size TopLeft, Size TopRight, Size BottomRight, Size BottomLeft)
    {
        public bool IsZero => TopLeft == default && TopRight == default && BottomRight == default && BottomLeft == default;

        public bool IsUniform => TopLeft == TopRight && TopRight == BottomRight && BottomRight == BottomLeft;
    }

    private static Radii Resolve(object? value, Size size)
    {
        if (value is string text && text.Trim() is { Length: > 1 } trimmed && trimmed.EndsWith('%')
            && double.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) && percent >= 0)
        {
            var corner = new Size(size.Width * percent / 100, size.Height * percent / 100);
            return Clamp(new Radii(corner, corner, corner, corner), size);
        }

        var radius = Parse(value);
        return Clamp(new Radii(
            new Size(radius.TopLeft, radius.TopLeft),
            new Size(radius.TopRight, radius.TopRight),
            new Size(radius.BottomRight, radius.BottomRight),
            new Size(radius.BottomLeft, radius.BottomLeft)), size);
    }

    private static CornerRadius Parse(object? value)
    {
        switch (value)
        {
            case CornerRadius radius:
                return radius;
            case double number when number >= 0:
                return new CornerRadius(number);
            case int number when number >= 0:
                return new CornerRadius(number);
            case string text:
                try
                {
                    var radius = CornerRadius.Parse(text.Replace("px", "", StringComparison.Ordinal));
                    return radius is { TopLeft: >= 0, TopRight: >= 0, BottomRight: >= 0, BottomLeft: >= 0 } ? radius : default;
                }
                catch (FormatException)
                {
                    return default;
                }
            default:
                return default;
        }
    }

    /// <summary>CSS scales every radius down by the same factor when two on one side would overlap.</summary>
    private static Radii Clamp(Radii r, Size size)
    {
        var factor = 1.0;
        void Fit(double side, double sum)
        {
            if (sum > side && sum > 0)
            {
                factor = Math.Min(factor, side / sum);
            }
        }

        Fit(size.Width, r.TopLeft.Width + r.TopRight.Width);
        Fit(size.Width, r.BottomLeft.Width + r.BottomRight.Width);
        Fit(size.Height, r.TopLeft.Height + r.BottomLeft.Height);
        Fit(size.Height, r.TopRight.Height + r.BottomRight.Height);

        return factor == 1 ? r : new Radii(r.TopLeft * factor, r.TopRight * factor, r.BottomRight * factor, r.BottomLeft * factor);
    }

    private static bool TryGetSize(IList<object?> values, out Size size)
    {
        size = values.Count > 1 && values[1] is Rect bounds ? bounds.Size : default;
        return values.Count > 0;
    }

    private sealed class CornerRadiusConverter : IMultiValueConverter
    {
        // A CornerRadius has one radius for each corner, so an elliptical corner takes the smaller of its two.
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!TryGetSize(values, out var size))
            {
                return AvaloniaProperty.UnsetValue;
            }

            var r = Resolve(values[0], size);
            static double Of(Size corner) => Math.Min(corner.Width, corner.Height);
            return new CornerRadius(Of(r.TopLeft), Of(r.TopRight), Of(r.BottomRight), Of(r.BottomLeft));
        }
    }

    private sealed class ClipConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!TryGetSize(values, out var size))
            {
                return AvaloniaProperty.UnsetValue;
            }

            var r = Resolve(values[0], size);
            var rect = new Rect(size);
            if (r.IsZero)
            {
                return null;
            }

            if (r.IsUniform)
            {
                return new RectangleGeometry(rect, r.TopLeft.Width, r.TopLeft.Height);
            }

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(r.TopLeft.Width, 0), true);
                context.LineTo(new Point(size.Width - r.TopRight.Width, 0));
                Arc(context, new Point(size.Width, r.TopRight.Height), r.TopRight);
                context.LineTo(new Point(size.Width, size.Height - r.BottomRight.Height));
                Arc(context, new Point(size.Width - r.BottomRight.Width, size.Height), r.BottomRight);
                context.LineTo(new Point(r.BottomLeft.Width, size.Height));
                Arc(context, new Point(0, size.Height - r.BottomLeft.Height), r.BottomLeft);
                context.LineTo(new Point(0, r.TopLeft.Height));
                Arc(context, new Point(r.TopLeft.Width, 0), r.TopLeft);
                context.EndFigure(true);
            }

            return geometry;
        }

        private static void Arc(StreamGeometryContext context, Point to, Size radius)
        {
            if (radius.Width > 0 && radius.Height > 0)
            {
                context.ArcTo(to, radius, 0, false, SweepDirection.Clockwise);
            }
            else
            {
                context.LineTo(to);
            }
        }
    }
}
