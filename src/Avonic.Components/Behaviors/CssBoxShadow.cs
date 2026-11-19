using System.Globalization;
using Avalonia.Media;

namespace Avonic.Components.Behaviors;

/// <summary>
/// A CSS <c>box-shadow</c> as Avalonia's <see cref="BoxShadows"/>, with the blur converted, so that a theme or an app keeps CSS's
/// numbers: <c>CssBoxShadow.Parse("0 3px 1px -2px rgba(0, 0, 0, 0.2), 0 2px 2px 0 rgba(0, 0, 0, 0.14)")</c>, or
/// <c>{b:CssShadow '…'}</c> in XAML. The offsets and the spread are CSS's already; only the blur differs (see <see cref="Blur"/>).
/// Measured against Chromium in <c>docs/infra/box-shadow/README.md</c>.
/// </summary>
public static class CssBoxShadow
{
    // Skia's three box blurs approximate a Gaussian of sigma s by a window of floor(s * 3 * sqrt(2 * pi) / 4 + 0.5) pixels,
    // which is a Gaussian of about half that window. CSS's blur B is a Gaussian of sigma B / 2, so the window is B: s is B / this.
    private static readonly double BoxWindowPerSigma = 3 * Math.Sqrt(2 * Math.PI) / 4;

    // Avalonia hands Skia a blur A as sigma A / (2 * sqrt(3)) + 0.5.
    private static readonly double BlurPerSigma = 2 * Math.Sqrt(3);
    private const double SigmaAtZero = 0.5;

    // A whole-pixel blur lands in the middle of its window, and a half-pixel one exactly on the edge between two, where float
    // rounding in Avalonia or Skia would pick either. A twentieth of a pixel makes it the wider, which measured closer: 1.5 px was
    // 7 levels out with a 2 px window and 27 with none.
    private const double HalfPixelNudge = 0.05;

    /// <summary>
    /// The <see cref="BoxShadow.Blur"/> that draws as CSS's blur <paramref name="cssBlur"/> does in Chromium, measured on Skia's CPU
    /// rasteriser: <c>2√3 ((B + 0.05) / (3√(2π)/4) − 0.5)</c>, about <c>1.8426 B − 1.6399</c>, and 0 below about 0.89 px, where
    /// Avalonia would draw no blur anyway.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A negative blur, which CSS rejects.</exception>
    public static double Blur(double cssBlur)
    {
        if (cssBlur < 0 || double.IsNaN(cssBlur))
        {
            throw new ArgumentOutOfRangeException(nameof(cssBlur), cssBlur, "A box-shadow's blur can't be negative.");
        }

        return Math.Max(0, BlurPerSigma * ((cssBlur + HalfPixelNudge) / BoxWindowPerSigma - SigmaAtZero));
    }

    /// <summary>
    /// Reads a CSS <c>box-shadow</c>: <c>none</c>, or shadows between commas, each two to four lengths (offset x, offset y, blur,
    /// spread) in <c>px</c> or unitless, a colour before or after them, and <c>inset</c>. A colour is <c>rgb()</c> or <c>rgba()</c>,
    /// <c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c> (CSS's order, alpha last), <c>transparent</c>, or a name or <c>hsl()</c> Avalonia
    /// knows. Each blur goes through <see cref="Blur"/>.
    /// </summary>
    /// <exception cref="FormatException">Anything else, including a shadow with no colour: CSS's would be <c>currentColor</c>,
    /// which a <see cref="BoxShadow"/> can't follow.</exception>
    public static BoxShadows Parse(string css)
    {
        ArgumentNullException.ThrowIfNull(css);
        var text = css.Trim();
        if (text.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return default;
        }

        var shadows = Split(text, ',').Select(layer => ParseOne(layer, css)).ToArray();
        return shadows.Length == 1 ? new BoxShadows(shadows[0]) : new BoxShadows(shadows[0], shadows[1..]);
    }

    private static BoxShadow ParseOne(string layer, string css)
    {
        var tokens = Split(layer, ' ');
        if (tokens.Count == 0)
        {
            throw Error(css, "an empty shadow");
        }

        var inset = false;
        Color? color = null;
        var lengths = new List<double>();
        var lengthsEnded = false;
        foreach (var token in tokens)
        {
            if (token.Equals("inset", StringComparison.OrdinalIgnoreCase))
            {
                if (inset)
                {
                    throw Error(css, "inset twice");
                }

                inset = true;
                lengthsEnded |= lengths.Count > 0;
            }
            else if (TryLength(token, out var length))
            {
                if (lengthsEnded)
                {
                    throw Error(css, "lengths that aren't together");
                }

                lengths.Add(length);
            }
            else
            {
                if (color is not null)
                {
                    throw Error(css, $"two colours, or '{token}', which isn't a length");
                }

                color = ParseColor(token, css);
                lengthsEnded |= lengths.Count > 0;
            }
        }

        if (lengths.Count is < 2 or > 4)
        {
            throw Error(css, $"{lengths.Count} lengths, where a shadow has two to four");
        }

        if (color is null)
        {
            throw Error(css, "a shadow with no colour (CSS's currentColor, which a BoxShadow can't follow)");
        }

        var blur = lengths.Count > 2 ? lengths[2] : 0;
        if (blur < 0)
        {
            throw Error(css, "a negative blur");
        }

        return new BoxShadow
        {
            OffsetX = lengths[0],
            OffsetY = lengths[1],
            Blur = Blur(blur),
            Spread = lengths.Count > 3 ? lengths[3] : 0,
            Color = color.Value,
            IsInset = inset,
        };
    }

    private static bool TryLength(string token, out double value)
    {
        var number = token.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? token[..^2] : token;
        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    }

    /// <summary>A CSS colour (hex with alpha last, rgb(), rgba(), hsl(), names, transparent), or false.</summary>
    internal static bool TryParseColor(string token, out Color color)
    {
        try
        {
            color = ParseColor(token.Trim(), token);
            return true;
        }
        catch (FormatException)
        {
            color = default;
            return false;
        }
    }

    private static Color ParseColor(string token, string css)
    {
        if (token.StartsWith('#'))
        {
            return ParseHex(token[1..]) ?? throw Error(css, $"the colour '{token}'");
        }

        if (token.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            return Color.FromArgb(0, 0, 0, 0); // rgba(0, 0, 0, 0), where Avalonia's Transparent is white
        }

        var open = token.IndexOf('(');
        if (open > 0 && token.EndsWith(')'))
        {
            var name = token[..open].Trim().ToLowerInvariant();
            if (name is "rgb" or "rgba")
            {
                return ParseRgb(token[(open + 1)..^1]) ?? throw Error(css, $"the colour '{token}'");
            }
        }

        // Names and hsl(), as Avalonia reads them; not its other forms (#aarrggbb, hsv()), which CSS rejects. Lower case, as CSS
        // ignores it, with CSS's names Avalonia's list doesn't have.
        var lower = token.ToLowerInvariant();
        if (open <= 0)
        {
            lower = lower == "rebeccapurple" ? "#663399" : lower.Replace("grey", "gray", StringComparison.Ordinal);
        }

        var cssForm = open <= 0 || lower[..open].Trim() is "hsl" or "hsla";
        return cssForm && Color.TryParse(lower, out var parsed) ? parsed : throw Error(css, $"the colour '{token}'");
    }

    private static Color? ParseHex(string hex)
    {
        if (hex.Length is not (3 or 4 or 6 or 8) || !hex.All(Uri.IsHexDigit))
        {
            return null;
        }

        int Digit(int i) => Convert.ToInt32(hex.Substring(i, 1), 16);
        int Pair(int i) => Convert.ToInt32(hex.Substring(i, 2), 16);
        return hex.Length switch
        {
            3 => Color.FromRgb((byte)(Digit(0) * 17), (byte)(Digit(1) * 17), (byte)(Digit(2) * 17)),
            4 => Color.FromArgb((byte)(Digit(3) * 17), (byte)(Digit(0) * 17), (byte)(Digit(1) * 17), (byte)(Digit(2) * 17)),
            6 => Color.FromRgb((byte)Pair(0), (byte)Pair(2), (byte)Pair(4)),
            _ => Color.FromArgb((byte)Pair(6), (byte)Pair(0), (byte)Pair(2), (byte)Pair(4)),
        };
    }

    // rgb(r, g, b), rgba(r, g, b, a), and the space syntax, rgb(r g b) or rgb(r g b / a); each a number or a percentage, the three
    // channels all one or all the other with commas, as CSS's legacy syntax has them.
    private static Color? ParseRgb(string args)
    {
        string[] parts;
        string? alpha = null;
        if (args.Contains(','))
        {
            parts = args.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 4)
            {
                alpha = parts[3];
                parts = parts[..3];
            }

            if (parts.Length == 3 && parts.Select(p => p.EndsWith('%')).Distinct().Count() != 1)
            {
                return null;
            }
        }
        else
        {
            var slash = args.Split('/', StringSplitOptions.TrimEntries);
            if (slash.Length > 2)
            {
                return null;
            }

            parts = slash[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries); // any whitespace
            alpha = slash.Length == 2 ? slash[1] : null;
        }

        if (parts.Length != 3)
        {
            return null;
        }

        var r = Channel(parts[0], 255);
        var g = Channel(parts[1], 255);
        var b = Channel(parts[2], 255);
        var a = alpha is null ? 1 : Channel(alpha, 1);
        if (r is null || g is null || b is null || a is null)
        {
            return null;
        }

        static byte Byte(double v) => (byte)Math.Round(Math.Clamp(v, 0, 255), MidpointRounding.AwayFromZero);
        return Color.FromArgb(Byte(a.Value * 255), Byte(r.Value), Byte(g.Value), Byte(b.Value));
    }

    private static double? Channel(string text, double full)
    {
        var percent = text.EndsWith('%');
        var number = percent ? text[..^1] : text;
        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
        {
            return null;
        }

        return percent ? value / 100 * full : value;
    }

    // Splits at a separator outside parentheses, dropping empty pieces for spaces.
    private static List<string> Split(string text, char separator)
    {
        var pieces = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i <= text.Length; i++)
        {
            var c = i < text.Length ? text[i] : separator;
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
            }
            else if (depth == 0 && (c == separator || (separator == ' ' && char.IsWhiteSpace(c))) || i == text.Length)
            {
                var piece = text[start..i].Trim();
                if (piece.Length > 0 || separator != ' ')
                {
                    pieces.Add(piece);
                }

                start = i + 1;
            }
        }

        return pieces;
    }

    private static FormatException Error(string css, string what) =>
        new($"Can't read the box-shadow '{css}': {what}.");
}
