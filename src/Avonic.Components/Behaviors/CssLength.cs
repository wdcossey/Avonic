using System.Globalization;
using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Behaviors;

/// <summary>
/// A CSS length as a size variable gives it (<c>--width</c>, <c>--max-height</c>, <c>--start</c>): a number (px) or a string of
/// <c>px</c>, a percentage of a basis, <c>vw</c> and <c>vh</c> of a viewport, sums and differences (in <c>calc()</c> or not, CSS's spaces
/// or other white space around the operator), brackets, <c>min()</c>, <c>max()</c> and <c>clamp()</c>, nested as CSS nests them. <c>auto</c>, <c>none</c>, the
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
        double d => double.IsFinite(d) ? d : null,
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
            return Parse(s[..at], basis, viewport) is { } a && Parse(s[(at + 1)..], basis, viewport) is { } b
                ? s[at] == '-' ? a - b : a + b
                : null;
        }

        // One function call, or brackets, around the whole value.
        var open = s.IndexOf('(');
        if (open >= 0 && s[^1] == ')' && Closing(s, open) == s.Length - 1)
        {
            var name = s[..open].Trim();
            var parts = Arguments(s[(open + 1)..^1]);
            var values = new double[parts.Count];
            for (var i = 0; i < parts.Count; i++)
            {
                if (Parse(parts[i], basis, viewport) is not { } value)
                {
                    return null;
                }

                values[i] = value;
            }

            return name switch
            {
                "" or "calc" when values.Length == 1 => values[0],
                "min" => values.Min(),
                "max" => values.Max(),
                // clamp(min, value, max): the minimum wins, as in CSS.
                "clamp" when values.Length == 3 => Math.Max(values[0], Math.Min(values[1], values[2])),
                _ => null,
            };
        }

        if (s.EndsWith('%'))
        {
            return Percent(s[..^1], basis);
        }

        if (s.EndsWith("vw", StringComparison.Ordinal))
        {
            return Percent(s[..^2], viewport.Width);
        }

        if (s.EndsWith("vh", StringComparison.Ordinal))
        {
            return Percent(s[..^2], viewport.Height);
        }

        return Number(s.EndsWith("px", StringComparison.Ordinal) ? s[..^2] : s);
    }

    /// <summary>A share of <paramref name="of"/>, or null for one of an infinite basis.</summary>
    private static double? Percent(string number, double of) =>
        Number(number) is { } percent && double.IsFinite(of) ? of * percent / 100 : null;

    /// <summary>A finite number, so that <c>nan</c> and <c>infinity</c>, which .NET reads, are no length, as in CSS.</summary>
    private static double? Number(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) ? value : null;

    /// <summary>
    /// Measures <paramref name="panel"/> again when its top level's client size changes, which <c>vw</c> and <c>vh</c> are of: a panel
    /// whose own room stays the same would keep its old size. From the attach; dispose it at the detach.
    /// </summary>
    internal static IDisposable? MeasureOnViewportChange(Control panel)
    {
        if (TopLevel.GetTopLevel(panel) is not { } top)
        {
            return null;
        }

        void Changed(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TopLevel.ClientSizeProperty)
            {
                panel.InvalidateMeasure();
            }
        }

        top.PropertyChanged += Changed;
        return new Unsubscribe(() => top.PropertyChanged -= Changed);
    }

    private sealed class Unsubscribe(Action action) : IDisposable
    {
        public void Dispose() => action();
    }

    /// <summary>Where the last <c>+</c> or <c>-</c> outside brackets is, with white space either side, as CSS wants it, or -1.</summary>
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
                case '+' or '-' when depth == 0 && char.IsWhiteSpace(s[i - 1]) && i + 1 < s.Length && char.IsWhiteSpace(s[i + 1]):
                    return i;
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
