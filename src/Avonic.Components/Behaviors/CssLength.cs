using System.Globalization;
using Avalonia;

namespace Avonic.Components.Behaviors;

/// <summary>
/// A CSS length as a size variable gives it (<c>--width</c>, <c>--max-height</c>, <c>--start</c>): a number (px) or a string of
/// <c>px</c>, a percentage of a basis, <c>vw</c> and <c>vh</c> of a viewport, sums and differences (in <c>calc()</c> or not, CSS's spaces
/// around the operator), brackets, <c>min()</c>, <c>max()</c> and <c>clamp()</c>, nested as CSS nests them. <c>auto</c>, <c>none</c>, the
/// content sizes, <c>*</c> and <c>/</c>, and anything else, are no length: null, which a panel reads as its own rule for no length.
/// What the overlays' panels and the split pane's read.
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
        s = s.Trim();
        if (s.Length == 0)
        {
            return null;
        }

        // Sums and differences, left to right: split at the last operator outside any brackets.
        var at = LastOperator(s);
        if (at > 0)
        {
            return Parse(s[..at], basis, viewport) is { } a && Parse(s[(at + 3)..], basis, viewport) is { } b
                ? s[at + 1] == '-' ? a - b : a + b
                : null;
        }

        // One function call, or brackets, around the whole value.
        var open = s.IndexOf('(');
        if (open >= 0 && s[^1] == ')' && Closing(s, open) == s.Length - 1)
        {
            var name = s[..open].Trim();
            var args = Arguments(s[(open + 1)..^1]).Select(part => Parse(part, basis, viewport)).ToList();
            if (args.Count == 0 || args.Any(x => x is null))
            {
                return null;
            }

            var values = args.Select(x => x!.Value).ToList();
            return name switch
            {
                "" or "calc" when values.Count == 1 => values[0],
                "min" => values.Min(),
                "max" => values.Max(),
                // clamp(min, value, max): the minimum wins, as in CSS.
                "clamp" when values.Count == 3 => Math.Max(values[0], Math.Min(values[1], values[2])),
                _ => null,
            };
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

    /// <summary>Where the last <c> + </c> or <c> - </c> outside brackets starts (its space), or -1.</summary>
    private static int LastOperator(string s)
    {
        var depth = 0;
        for (var i = s.Length - 1; i > 0; i--)
        {
            switch (s[i])
            {
                case ')':
                    depth++;
                    break;
                case '(':
                    depth--;
                    break;
                case '+' or '-' when depth == 0 && s[i - 1] == ' ' && i + 1 < s.Length && s[i + 1] == ' ':
                    return i - 1;
            }
        }

        return -1;
    }

    /// <summary>The index of the bracket that closes the one at <paramref name="open"/>, or -1.</summary>
    private static int Closing(string s, int open)
    {
        var depth = 0;
        for (var i = open; i < s.Length; i++)
        {
            if (s[i] == '(')
            {
                depth++;
            }
            else if (s[i] == ')' && --depth == 0)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>A function's arguments: split at the commas outside brackets.</summary>
    private static List<string> Arguments(string inner)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < inner.Length; i++)
        {
            switch (inner[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    parts.Add(inner[start..i]);
                    start = i + 1;
                    break;
            }
        }

        parts.Add(inner[start..]);
        return parts;
    }
}
