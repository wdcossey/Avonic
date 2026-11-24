using System.Globalization;
using Avalonia;

namespace Avonic.Components.Behaviors;

/// <summary>
/// A CSS length as a size variable gives it (<c>--width</c>, <c>--max-height</c>, <c>--start</c>): a number (px) or a string of <c>px</c>, a
/// percentage of a basis, <c>vw</c> and <c>vh</c> of the viewport, a sum or difference of those (in <c>calc()</c> or not), <c>min(a, b)</c>, or
/// <c>auto</c>, <c>none</c> and the content sizes, which aren't lengths. What the overlays' panels and the split pane's read.
/// </summary>
internal static class CssLength
{
    /// <summary>
    /// A CSS size in px, or null for one that isn't a length (<c>auto</c>, <c>none</c>, nothing, or a percentage of an infinite
    /// basis). A percentage is of <paramref name="basis"/>; <c>vw</c> and <c>vh</c> are of the viewport's width and height.
    /// </summary>
    internal static double? Read(object? value, double basis, Size viewport) => value switch
    {
        double d => double.IsNaN(d) ? null : d,
        int i => i,
        string s => Parse(s.Trim().ToLowerInvariant(), basis, viewport),
        _ => null,
    };

    /// <summary><see cref="Read(object?, double, Size)"/> where the viewport is the basis both ways.</summary>
    internal static double? Read(object? value, double basis) => Read(value, basis, new Size(basis, basis));

    private static double? Parse(string s, double basis, Size viewport)
    {
        foreach (var function in new[] { "calc(", "min(" })
        {
            if (s.StartsWith(function, StringComparison.Ordinal) && s.EndsWith(')'))
            {
                var inner = s[function.Length..^1];
                if (function == "calc(")
                {
                    return Parse(inner.Trim(), basis, viewport);
                }

                double? min = null;
                foreach (var part in inner.Split(','))
                {
                    if (Parse(part.Trim(), basis, viewport) is not { } value)
                    {
                        return null;
                    }

                    min = min is { } m ? Math.Min(m, value) : value;
                }

                return min;
            }
        }

        // Sums and differences, as in calc(), left to right: split at the last operator of either kind.
        var minus = s.LastIndexOf(" - ", StringComparison.Ordinal);
        var plus = s.LastIndexOf(" + ", StringComparison.Ordinal);
        var at = Math.Max(minus, plus);
        if (at > 0)
        {
            return Parse(s[..at].Trim(), basis, viewport) is { } a && Parse(s[(at + 3)..].Trim(), basis, viewport) is { } b
                ? at == minus ? a - b : a + b
                : null;
        }

        foreach (var (unit, of) in new[] { ("%", basis), ("vw", viewport.Width), ("vh", viewport.Height) })
        {
            if (s.EndsWith(unit, StringComparison.Ordinal))
            {
                return double.TryParse(s[..^unit.Length], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent)
                       && !double.IsInfinity(of)
                    ? of * percent / 100
                    : null;
            }
        }

        if (s.EndsWith("px", StringComparison.Ordinal))
        {
            s = s[..^2];
        }

        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var px) ? px : null;
    }
}
