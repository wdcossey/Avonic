using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avonic.Components.Controls;

/// <summary>
/// One SVG, parsed to the layers a browser would paint for it. Ionicons puts the SVG in the DOM, so
/// what it looks like comes from the browser's defaults and <c>icon.css</c>, reproduced here. Shared
/// between every icon showing the same artwork, so it must not change after parsing.
/// </summary>
/// <remarks>
/// A subset of SVG: <c>path</c>, <c>circle</c>, <c>ellipse</c>, <c>rect</c>, <c>line</c>,
/// <c>polyline</c>, <c>polygon</c> and <c>g</c>, with presentation attributes. That covers every
/// built-in icon. Other elements, and <c>style</c>, aren't drawn.
/// </remarks>
internal sealed partial class SvgArtwork
{
    public static readonly SvgArtwork Empty = new(null, []);

    /// <summary>
    /// A stack overflow can't be caught, so nesting past this is treated as invalid. A browser's parser
    /// has a limit of its own, and no real icon comes close.
    /// </summary>
    private const int MaxDepth = 256;

    private SvgArtwork(Rect? viewBox, IReadOnlyList<Layer> layers)
    {
        ViewBox = viewBox;
        Layers = layers;
        UsesStrokeWidthVariable = layers.Any(layer => layer.StrokeWidthFromVariable);
    }

    public Rect? ViewBox { get; }

    public IReadOnlyList<Layer> Layers { get; }

    /// <summary>Only a custom SVG with the <c>ionicon-stroke-width</c> class reads the variable.</summary>
    public bool UsesStrokeWidthVariable { get; }

    /// <summary>
    /// How to paint one element. <c>null</c> paint is <c>none</c>; <see cref="Paint.CurrentColor"/> is
    /// the icon's colour.
    /// </summary>
    internal sealed record Layer(
        Geometry Geometry,
        Matrix Transform,
        Paint? Fill,
        Paint? Stroke,
        double StrokeWidth,
        bool StrokeWidthFromVariable,
        PenLineCap LineCap,
        PenLineJoin LineJoin,
        double MiterLimit);

    internal sealed record Paint(IImmutableBrush? Brush)
    {
        public static readonly Paint CurrentColor = new((IImmutableBrush?)null);
    }

    /// <summary>What inherits down the tree. The initial values are SVG's, not Avalonia's.</summary>
    private sealed record Inherited(
        Paint? Fill,
        Paint? Stroke,
        double StrokeWidth,
        bool StrokeWidthFromVariable,
        PenLineCap LineCap,
        PenLineJoin LineJoin,
        double MiterLimit,
        bool EvenOdd,
        Matrix Transform);

    /// <summary>A blank icon if the text isn't a valid, safe SVG, as <c>validateContent</c> gives.</summary>
    public static SvgArtwork Parse(string svg)
    {
        try
        {
            // Design tools export a <!DOCTYPE>, so it's skipped rather than rejected. Ignoring it, with no
            // resolver, means entities are never expanded and nothing external is fetched.
            using var reader = XmlReader.Create(new StringReader(svg.TrimStart()), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true,
            });

            return reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "svg" ? ParseRoot(reader) : Empty;
        }
        catch (XmlException)
        {
            return Empty;
        }
    }

    private static SvgArtwork ParseRoot(XmlReader reader)
    {
        // .ionicon { stroke: currentColor } in icon.css, which beats a stroke attribute on the root.
        // The fill comes from the host: fill: currentColor.
        var isIonicon = HasClass(reader, "ionicon");
        var root = Apply(reader, new Inherited(
            Paint.CurrentColor, null, 1, false, PenLineCap.Flat, PenLineJoin.Miter, 4, false, Matrix.Identity));
        if (isIonicon)
        {
            root = root with { Stroke = Paint.CurrentColor };
        }

        var viewBox = ParseViewBox(reader.GetAttribute("viewBox"));
        var layers = new List<Layer>();

        if (HasEventAttribute(reader) || !ReadChildren(reader, root, layers))
        {
            return Empty;
        }

        return new SvgArtwork(viewBox, layers);
    }

    /// <summary>False if the content is unsafe, which blanks the whole icon.</summary>
    private static bool ReadChildren(XmlReader reader, Inherited parent, List<Layer> layers)
    {
        if (reader.IsEmptyElement)
        {
            return true;
        }

        if (reader.Depth > MaxDepth)
        {
            return false;
        }

        var depth = reader.Depth;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                return true;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            // isValid: no scripts, and no attribute starting with "on".
            if (reader.LocalName.Equals("script", StringComparison.OrdinalIgnoreCase) || HasEventAttribute(reader))
            {
                return false;
            }

            var state = Apply(reader, parent);
            var geometry = CreateGeometry(reader, state);
            if (geometry is not null)
            {
                layers.Add(new Layer(
                    geometry, state.Transform, state.Fill, state.Stroke, state.StrokeWidth,
                    state.StrokeWidthFromVariable, state.LineCap, state.LineJoin, state.MiterLimit));
            }

            // Only a group's children inherit. Skip anything else's, so <defs> and friends draw nothing.
            if (reader.LocalName == "g")
            {
                if (!ReadChildren(reader, state, layers))
                {
                    return false;
                }
            }
            else if (!reader.IsEmptyElement && !SkipElement(reader))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Moves past an element, but still blanks the icon if it hides a script.</summary>
    private static bool SkipElement(XmlReader reader)
    {
        var depth = reader.Depth;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)
            {
                return true;
            }

            if (reader.NodeType == XmlNodeType.Element &&
                (reader.LocalName.Equals("script", StringComparison.OrdinalIgnoreCase) || HasEventAttribute(reader)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasEventAttribute(XmlReader reader)
    {
        if (reader.MoveToFirstAttribute())
        {
            do
            {
                if (reader.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                {
                    reader.MoveToElement();
                    return true;
                }
            }
            while (reader.MoveToNextAttribute());

            reader.MoveToElement();
        }

        return false;
    }

    private static bool HasClass(XmlReader reader, string name) =>
        reader.GetAttribute("class") is { } classes &&
        classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(name);

    private static Inherited Apply(XmlReader reader, Inherited parent)
    {
        var state = parent;

        if (reader.GetAttribute("fill") is { } fill)
        {
            state = state with { Fill = ParsePaint(fill, parent.Fill) };
        }

        if (reader.GetAttribute("stroke") is { } stroke)
        {
            state = state with { Stroke = ParsePaint(stroke, parent.Stroke) };
        }

        if (ParseLength(reader.GetAttribute("stroke-width")) is { } width)
        {
            state = state with { StrokeWidth = width, StrokeWidthFromVariable = false };
        }

        state = reader.GetAttribute("stroke-linecap") switch
        {
            "butt" => state with { LineCap = PenLineCap.Flat },
            "round" => state with { LineCap = PenLineCap.Round },
            "square" => state with { LineCap = PenLineCap.Square },
            _ => state,
        };

        state = reader.GetAttribute("stroke-linejoin") switch
        {
            "miter" => state with { LineJoin = PenLineJoin.Miter },
            "round" => state with { LineJoin = PenLineJoin.Round },
            "bevel" => state with { LineJoin = PenLineJoin.Bevel },
            _ => state,
        };

        if (ParseLength(reader.GetAttribute("stroke-miterlimit")) is { } miter)
        {
            state = state with { MiterLimit = miter };
        }

        state = reader.GetAttribute("fill-rule") switch
        {
            "evenodd" => state with { EvenOdd = true },
            "nonzero" => state with { EvenOdd = false },
            _ => state,
        };

        if (reader.GetAttribute("transform") is { } transform)
        {
            state = state with { Transform = ParseTransform(transform) * parent.Transform };
        }

        // icon.css classes. They beat presentation attributes, because they're CSS.
        if (HasClass(reader, "ionicon-fill-none"))
        {
            state = state with { Fill = null };
        }

        if (HasClass(reader, "ionicon-stroke-width"))
        {
            state = state with { StrokeWidthFromVariable = true };
        }

        return state;
    }

    private static Paint? ParsePaint(string value, Paint? inherited) => value.Trim() switch
    {
        "none" => null,
        "currentColor" or "currentcolor" => Paint.CurrentColor,
        "inherit" => inherited,
        var text => Color.TryParse(CssToAvaloniaHex(text), out var color) ? new Paint(new ImmutableSolidColorBrush(color)) : inherited,
    };

    /// <summary>CSS's <c>#rrggbbaa</c> and <c>#rgba</c> put alpha last, and Avalonia's put it first.</summary>
    private static string CssToAvaloniaHex(string text)
    {
        if (text.Length is not (5 or 9) || text[0] != '#')
        {
            return text;
        }

        var alphaDigits = text.Length == 9 ? 2 : 1;
        return string.Concat("#", text.AsSpan(text.Length - alphaDigits), text.AsSpan(1, text.Length - 1 - alphaDigits));
    }

    private static double? ParseLength(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var number = value.Trim();
        if (number.EndsWith("px", StringComparison.Ordinal))
        {
            number = number[..^2];
        }

        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static double Number(XmlReader reader, string name) => ParseLength(reader.GetAttribute(name)) ?? 0;

    private static Rect? ParseViewBox(string? value)
    {
        var numbers = Numbers(value);
        return numbers is [var x, var y, var width, var height] && width > 0 && height > 0
            ? new Rect(x, y, width, height)
            : null;
    }

    /// <summary>SVG number lists can run together: <c>10-20</c> and <c>.5.5</c> are two numbers each.</summary>
    private static double[] Numbers(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var matches = SvgNumber().Matches(value);
        var numbers = new double[matches.Count];
        for (var i = 0; i < numbers.Length; i++)
        {
            numbers[i] = double.Parse(matches[i].ValueSpan, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        return numbers;
    }

    [GeneratedRegex(@"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")]
    private static partial Regex SvgNumber();

    private static Matrix ParseTransform(string value)
    {
        var result = Matrix.Identity;
        var remaining = value.AsSpan();

        while (true)
        {
            var open = remaining.IndexOf('(');
            var close = remaining.IndexOf(')');
            if (open < 0 || close < open)
            {
                return result;
            }

            var name = remaining[..open].Trim(" ,\t\r\n").ToString();
            var args = Numbers(remaining[(open + 1)..close].ToString());
            remaining = remaining[(close + 1)..];

            // Each function applies inside the ones before it, so it multiplies on the left.
            result = TransformFunction(name, args) * result;
        }
    }

    private static Matrix TransformFunction(string name, double[] args) => (name, args) switch
    {
        ("matrix", [var a, var b, var c, var d, var e, var f]) => new Matrix(a, b, c, d, e, f),
        ("translate", [var x]) => Matrix.CreateTranslation(x, 0),
        ("translate", [var x, var y]) => Matrix.CreateTranslation(x, y),
        ("scale", [var s]) => Matrix.CreateScale(s, s),
        ("scale", [var x, var y]) => Matrix.CreateScale(x, y),
        ("skewX", [var angle]) => new Matrix(1, 0, Math.Tan(Matrix.ToRadians(angle)), 1, 0, 0),
        ("skewY", [var angle]) => new Matrix(1, Math.Tan(Matrix.ToRadians(angle)), 0, 1, 0, 0),
        ("rotate", [var angle]) => Matrix.CreateRotation(Matrix.ToRadians(angle)),
        ("rotate", [var angle, var x, var y]) =>
            Matrix.CreateTranslation(-x, -y) * Matrix.CreateRotation(Matrix.ToRadians(angle)) * Matrix.CreateTranslation(x, y),
        _ => Matrix.Identity,
    };

    private static Geometry? CreateGeometry(XmlReader reader, Inherited state)
    {
        var path = reader.LocalName switch
        {
            "path" => reader.GetAttribute("d"),
            "circle" => EllipsePath(Number(reader, "cx"), Number(reader, "cy"), Number(reader, "r"), Number(reader, "r")),
            "ellipse" => EllipsePath(Number(reader, "cx"), Number(reader, "cy"), Number(reader, "rx"), Number(reader, "ry")),
            "rect" => RectPath(reader),
            "line" => Invariant($"M{Number(reader, "x1")} {Number(reader, "y1")}L{Number(reader, "x2")} {Number(reader, "y2")}"),
            "polyline" => PointsPath(reader.GetAttribute("points"), closed: false),
            "polygon" => PointsPath(reader.GetAttribute("points"), closed: true),
            _ => null,
        };

        return string.IsNullOrWhiteSpace(path) ? null : SvgPath.Parse(path, state.EvenOdd);
    }

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    private static string? EllipsePath(double cx, double cy, double rx, double ry) =>
        rx <= 0 || ry <= 0
            ? null
            : Invariant($"M{cx - rx} {cy}A{rx} {ry} 0 1 1 {cx + rx} {cy}A{rx} {ry} 0 1 1 {cx - rx} {cy}Z");

    private static string? RectPath(XmlReader reader)
    {
        double x = Number(reader, "x"), y = Number(reader, "y"), width = Number(reader, "width"), height = Number(reader, "height");
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        // An omitted radius takes the other's value, and neither can pass half the side.
        var givenX = ParseLength(reader.GetAttribute("rx"));
        var givenY = ParseLength(reader.GetAttribute("ry"));
        var rx = Math.Min(givenX ?? givenY ?? 0, width / 2);
        var ry = Math.Min(givenY ?? givenX ?? 0, height / 2);

        if (rx <= 0 || ry <= 0)
        {
            return Invariant($"M{x} {y}H{x + width}V{y + height}H{x}Z");
        }

        return Invariant(
            $"M{x + rx} {y}H{x + width - rx}A{rx} {ry} 0 0 1 {x + width} {y + ry}V{y + height - ry}A{rx} {ry} 0 0 1 {x + width - rx} {y + height}H{x + rx}A{rx} {ry} 0 0 1 {x} {y + height - ry}V{y + ry}A{rx} {ry} 0 0 1 {x + rx} {y}Z");
    }

    private static string? PointsPath(string? points, bool closed)
    {
        var numbers = Numbers(points);
        if (numbers.Length < 4)
        {
            return null;
        }

        var parts = new List<string>();
        for (var i = 0; i + 1 < numbers.Length; i += 2)
        {
            parts.Add(Invariant($"{(i == 0 ? "M" : "L")}{numbers[i]} {numbers[i + 1]}"));
        }

        return string.Concat(parts) + (closed ? "Z" : "");
    }
}
