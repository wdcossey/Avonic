using System.Globalization;
using Avalonia;

namespace Avonic.Components.Animation;

/// <summary>
/// A CSS <c>transform</c> list, as the md animations write them: <c>translateX</c>, <c>translateY</c>, <c>translate</c>,
/// <c>scale</c>, <c>scaleX</c>, <c>scaleY</c> and <c>rotate</c>, with lengths in <c>px</c> or <c>%</c> of the element's own size,
/// and angles in <c>deg</c>; or <c>none</c>. A length keeps its pixels and its percentage apart, as CSS's <c>calc()</c> does, so
/// it's resolved against the size when the matrix is made, and interpolating before resolving is the same as after.
/// </summary>
internal sealed class CssTransform
{
    internal static readonly CssTransform None = new([]);

    private CssTransform(IReadOnlyList<Function> functions) => Functions = functions;

    internal IReadOnlyList<Function> Functions { get; }

    /// <summary>One function, as its primitive: a translate, a scale or a rotation, which is how CSS interpolates two of them.</summary>
    /// <param name="Kind">The primitive.</param>
    /// <param name="X">Pixels of x, or the x scale, or degrees.</param>
    /// <param name="XPercent">Percent of the width.</param>
    /// <param name="Y">Pixels of y, or the y scale.</param>
    /// <param name="YPercent">Percent of the height.</param>
    internal readonly record struct Function(FunctionKind Kind, double X, double XPercent, double Y, double YPercent);

    internal enum FunctionKind
    {
        Translate,
        Scale,
        Rotate,
    }

    /// <exception cref="ArgumentException">Anything this doesn't take.</exception>
    internal static CssTransform Parse(string text)
    {
        var value = text.Trim();
        if (value is "none" or "")
        {
            return None;
        }

        var functions = new List<Function>();
        var rest = value.AsSpan();
        while (!rest.IsEmpty)
        {
            var open = rest.IndexOf('(');
            var close = rest.IndexOf(')');
            if (open <= 0 || close < open)
            {
                throw Invalid(text);
            }

            var name = rest[..open].Trim().ToString();
            var args = rest[(open + 1)..close].ToString().Split(',', StringSplitOptions.TrimEntries);
            functions.Add(ParseFunction(name, args, text));
            rest = rest[(close + 1)..].TrimStart();
        }

        return new CssTransform(functions);
    }

    /// <summary>
    /// CSS's interpolation of two lists. <c>none</c> is the other list's identity functions; two lists of the same primitives
    /// interpolate function by function. Lists that differ switch at the midpoint, where CSS would interpolate their matrices:
    /// no md animation has one.
    /// </summary>
    internal static CssTransform Interpolate(CssTransform from, CssTransform to, double progress)
    {
        var a = from.Functions.Count == 0 ? to.Identity() : from;
        var b = to.Functions.Count == 0 ? from.Identity() : to;
        if (a.Functions.Count != b.Functions.Count || a.Functions.Where((f, i) => f.Kind != b.Functions[i].Kind).Any())
        {
            return progress < 0.5 ? from : to;
        }

        var functions = new Function[a.Functions.Count];
        for (var i = 0; i < functions.Length; i++)
        {
            var f = a.Functions[i];
            var g = b.Functions[i];
            functions[i] = new Function(
                f.Kind,
                Lerp(f.X, g.X, progress),
                Lerp(f.XPercent, g.XPercent, progress),
                Lerp(f.Y, g.Y, progress),
                Lerp(f.YPercent, g.YPercent, progress));
        }

        return new CssTransform(functions);
    }

    /// <summary>The matrix for an element of <paramref name="size"/>, about its origin: Avalonia's <c>RenderTransformOrigin</c>, centre by default as CSS's is.</summary>
    internal Matrix ToMatrix(Size size)
    {
        // CSS applies the last function first. Avalonia's matrices take a row vector, so the first applied is on the left.
        var matrix = Matrix.Identity;
        for (var i = Functions.Count - 1; i >= 0; i--)
        {
            var f = Functions[i];
            matrix *= f.Kind switch
            {
                FunctionKind.Translate => Matrix.CreateTranslation(f.X + f.XPercent / 100 * size.Width, f.Y + f.YPercent / 100 * size.Height),
                FunctionKind.Scale => Matrix.CreateScale(f.X, f.Y),
                _ => Matrix.CreateRotation(f.X * Math.PI / 180),
            };
        }

        return matrix;
    }

    private CssTransform Identity() =>
        new(Functions.Select(f => f.Kind == FunctionKind.Scale ? new Function(f.Kind, 1, 0, 1, 0) : new Function(f.Kind, 0, 0, 0, 0)).ToArray());

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static Function ParseFunction(string name, string[] args, string text)
    {
        switch (name)
        {
            case "translateX" when args.Length == 1:
            {
                var (px, pct) = Length(args[0], text);
                return new Function(FunctionKind.Translate, px, pct, 0, 0);
            }
            case "translateY" when args.Length == 1:
            {
                var (px, pct) = Length(args[0], text);
                return new Function(FunctionKind.Translate, 0, 0, px, pct);
            }
            case "translate" when args.Length is 1 or 2:
            {
                var (x, xPct) = Length(args[0], text);
                var (y, yPct) = args.Length == 2 ? Length(args[1], text) : (0, 0);
                return new Function(FunctionKind.Translate, x, xPct, y, yPct);
            }
            case "scale" when args.Length is 1 or 2:
            {
                var x = Number(args[0], text);
                return new Function(FunctionKind.Scale, x, 0, args.Length == 2 ? Number(args[1], text) : x, 0);
            }
            case "scaleX" when args.Length == 1:
                return new Function(FunctionKind.Scale, Number(args[0], text), 0, 1, 0);
            case "scaleY" when args.Length == 1:
                return new Function(FunctionKind.Scale, 1, 0, Number(args[0], text), 0);
            case "rotate" when args.Length == 1 && args[0].EndsWith("deg", StringComparison.Ordinal):
                return new Function(FunctionKind.Rotate, Number(args[0][..^3], text), 0, 0, 0);
            default:
                throw Invalid(text);
        }
    }

    private static (double Pixels, double Percent) Length(string arg, string text)
    {
        if (arg.EndsWith('%'))
        {
            return (0, Number(arg[..^1], text));
        }

        if (arg.EndsWith("px", StringComparison.Ordinal))
        {
            return (Number(arg[..^2], text), 0);
        }

        // CSS takes a unitless zero as a length.
        return Number(arg, text) == 0 ? (0, 0) : throw Invalid(text);
    }

    private static double Number(string arg, string text) =>
        double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : throw Invalid(text);

    private static ArgumentException Invalid(string text) =>
        new($"'{text}' isn't a transform this supports: translateX, translateY, translate, scale, scaleX, scaleY or rotate, in px, % or deg.");
}
