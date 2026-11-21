using System.Globalization;
using Avalonia;

namespace Avonic.Components.Controls;

/// <summary>
/// A CSS media query list, as <c>window.matchMedia</c> reads it, against a window's size: what <see cref="AvonicSplitPane.When"/>
/// takes as a string. Media Queries 4 as far as a split pane needs: a list (a comma, any query matching), <c>not</c> and <c>only</c>,
/// media types (<c>all</c> and <c>screen</c> match, any other doesn't), <c>and</c>, <c>or</c>, brackets, <c>width</c> and
/// <c>height</c> with <c>min-</c> and <c>max-</c>, plain and in the range forms, <c>orientation</c>, <c>aspect-ratio</c> with
/// <c>min-</c> and <c>max-</c>, and lengths in <c>px</c>, <c>em</c> and <c>rem</c> (16 px), <c>cm</c>, <c>mm</c>, <c>q</c>,
/// <c>in</c>, <c>pt</c>, <c>pc</c>, or <c>0</c>. A feature it doesn't read, or a bracket that doesn't parse as one, is unknown, so
/// <c>not</c> of it is unknown too, and it matches only through <c>or</c> or a comma, as Chromium's do; a query that is malformed
/// outside its brackets is <c>not all</c>. Pinned to
/// Chromium's <c>matchMedia</c> in <c>docs/components/ion-split-pane/ref/media-queries.txt</c>.
/// </summary>
internal static class MediaQuery
{
    private const double FontSize = 16;

    private enum Tri
    {
        False,
        True,
        Unknown,
    }

    /// <summary>Whether <paramref name="query"/> matches a viewport of <paramref name="size"/>, in CSS pixels.</summary>
    public static bool Matches(string query, Size size)
    {
        var tokens = Tokenize(query);
        if (tokens is null)
        {
            return false;
        }

        // An empty list matches, as matchMedia('') does; ion-split-pane checks for it before.
        if (tokens.Count == 0)
        {
            return true;
        }

        var start = 0;
        var depth = 0;
        for (var i = 0; i <= tokens.Count; i++)
        {
            if (i == tokens.Count || (tokens[i].Kind == Kind.Comma && depth == 0))
            {
                if (new Parser(tokens.GetRange(start, i - start), size).Query() == Tri.True)
                {
                    return true;
                }

                start = i + 1;
                continue;
            }

            depth += tokens[i].Kind == Kind.Open ? 1 : tokens[i].Kind == Kind.Close ? -1 : 0;
        }

        return false;
    }

    // ── Tokens ───────────────────────────────────────────────────────────────────────────────

    private enum Kind
    {
        Ident,
        Number,
        Open,
        Close,
        Comma,
        Colon,
        Slash,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        Equal,
        Other,
    }

    private readonly record struct Token(Kind Kind, string Text = "", double Number = 0, string Unit = "");

    private static List<Token>? Tokenize(string text)
    {
        var tokens = new List<Token>();
        var s = text.ToLowerInvariant();
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c is '(' or ')' or ',' or ':' or '/' or '=')
            {
                tokens.Add(new Token(c switch
                {
                    '(' => Kind.Open,
                    ')' => Kind.Close,
                    ',' => Kind.Comma,
                    ':' => Kind.Colon,
                    '/' => Kind.Slash,
                    _ => Kind.Equal,
                }));
                i++;
            }
            else if (c is '<' or '>')
            {
                var orEqual = i + 1 < s.Length && s[i + 1] == '=';
                tokens.Add(new Token(c == '<' ? (orEqual ? Kind.LessEqual : Kind.Less) : (orEqual ? Kind.GreaterEqual : Kind.Greater)));
                i += orEqual ? 2 : 1;
            }
            else if (char.IsDigit(c) || (c is '.' or '+' or '-' && i + 1 < s.Length && (char.IsDigit(s[i + 1]) || s[i + 1] == '.')))
            {
                var begin = i;
                i++;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.'))
                {
                    i++;
                }

                // An exponent: 9.92e2px.
                if (i + 1 < s.Length && s[i] == 'e' && (char.IsDigit(s[i + 1]) || (s[i + 1] is '+' or '-' && i + 2 < s.Length && char.IsDigit(s[i + 2]))))
                {
                    i += 2;
                    while (i < s.Length && char.IsDigit(s[i]))
                    {
                        i++;
                    }
                }

                if (!double.TryParse(s[begin..i], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    return null;
                }

                var unitStart = i;
                while (i < s.Length && (char.IsLetter(s[i]) || s[i] == '%'))
                {
                    i++;
                }

                tokens.Add(new Token(Kind.Number, Number: number, Unit: s[unitStart..i]));
            }
            else if (char.IsLetter(c) || c is '-' or '_')
            {
                var begin = i;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] is '-' or '_'))
                {
                    i++;
                }

                tokens.Add(new Token(Kind.Ident, s[begin..i]));
            }
            else
            {
                tokens.Add(new Token(Kind.Other, c.ToString()));
                i++;
            }
        }

        return tokens;
    }

    // ── Parser ───────────────────────────────────────────────────────────────────────────────

    private sealed class Malformed : Exception;

    private sealed class Parser(List<Token> tokens, Size size)
    {
        private int _at;

        private Token? Peek => _at < tokens.Count ? tokens[_at] : null;

        /// <summary>One query of the list; a malformed one is <c>not all</c>.</summary>
        public Tri Query()
        {
            try
            {
                var result = QueryCore();
                return _at == tokens.Count ? result : Tri.False;
            }
            catch (Malformed)
            {
                return Tri.False;
            }
        }

        private Tri QueryCore()
        {
            if (tokens.Count == 0)
            {
                throw new Malformed();
            }

            // <media-condition>, which starts with a bracket or `not (`.
            if (Peek is { Kind: Kind.Open } || (IsIdent("not") && Next(1) is { Kind: Kind.Open }))
            {
                return Condition(allowOr: true);
            }

            // [not | only]? <media-type> [and <media-condition-without-or>]?
            var negate = false;
            if (IsIdent("not"))
            {
                negate = true;
                _at++;
            }
            else if (IsIdent("only"))
            {
                _at++;
            }

            if (Peek is not { Kind: Kind.Ident } type || type.Text is "and" or "or" or "not" or "only")
            {
                throw new Malformed();
            }

            _at++;
            var result = type.Text is "all" or "screen" ? Tri.True : Tri.False;
            if (IsIdent("and"))
            {
                _at++;
                result = And(result, Condition(allowOr: false));
            }

            return negate ? Not(result) : result;
        }

        private Tri Condition(bool allowOr)
        {
            if (IsIdent("not"))
            {
                _at++;
                return Not(InParens());
            }

            var result = InParens();
            string? joiner = null;
            while (IsIdent("and") || IsIdent("or"))
            {
                var word = Peek!.Value.Text;
                if ((joiner is not null && joiner != word) || (word == "or" && !allowOr))
                {
                    throw new Malformed();
                }

                joiner = word;
                _at++;
                var next = InParens();
                result = word == "and" ? And(result, next) : Or(result, next);
            }

            return result;
        }

        private Tri InParens()
        {
            Expect(Kind.Open);

            // ( <media-condition> )
            if (Peek is { Kind: Kind.Open } || (IsIdent("not") && Next(1) is { Kind: Kind.Open }))
            {
                var inner = Condition(allowOr: true);
                Expect(Kind.Close);
                return inner;
            }

            var close = _at;
            var depth = 1;
            while (close < tokens.Count)
            {
                depth += tokens[close].Kind == Kind.Open ? 1 : tokens[close].Kind == Kind.Close ? -1 : 0;
                if (depth == 0)
                {
                    break;
                }

                close++;
            }

            if (close == tokens.Count)
            {
                throw new Malformed();
            }

            var feature = tokens.GetRange(_at, close - _at);
            _at = close + 1;
            // A bracket that doesn't parse as a feature is general-enclosed: unknown, so it can still match through `or` or a
            // comma, as Chromium's (min-width) or (min-width: 500px) does.
            try
            {
                return Feature(feature);
            }
            catch (Malformed)
            {
                return Tri.Unknown;
            }
        }

        // ── Features ─────────────────────────────────────────────────────────────────────────

        private Tri Feature(List<Token> t)
        {
            // <mf-boolean>: (width)
            if (t is [{ Kind: Kind.Ident } name])
            {
                return name.Text switch
                {
                    "width" => Bool(size.Width != 0),
                    "height" => Bool(size.Height != 0),
                    "aspect-ratio" or "orientation" => Tri.True,
                    _ when name.Text.StartsWith("min-", StringComparison.Ordinal) || name.Text.StartsWith("max-", StringComparison.Ordinal)
                        => throw new Malformed(),
                    _ => Tri.Unknown,
                };
            }

            // <mf-plain>: (min-width: 500px)
            if (t is [{ Kind: Kind.Ident } plain, { Kind: Kind.Colon }, ..])
            {
                return Plain(plain.Text, t.GetRange(2, t.Count - 2));
            }

            return Range(t);
        }

        private Tri Plain(string name, List<Token> value)
        {
            var prefix = name.StartsWith("min-", StringComparison.Ordinal) ? "min" : name.StartsWith("max-", StringComparison.Ordinal) ? "max" : null;
            var feature = prefix is null ? name : name[4..];
            double? actual = feature switch
            {
                "width" => size.Width,
                "height" => size.Height,
                "aspect-ratio" => null,
                _ => double.NaN,
            };
            if (actual is double.NaN)
            {
                if (feature == "orientation" && prefix is null && value is [{ Kind: Kind.Ident } o] && o.Text is "portrait" or "landscape")
                {
                    var portrait = size.Height >= size.Width;
                    return Bool(o.Text == "portrait" ? portrait : !portrait);
                }

                // A feature it doesn't read: unknown, unless it's one it reads written wrongly.
                if (feature == "orientation")
                {
                    throw new Malformed();
                }

                return Tri.Unknown;
            }

            if (feature == "aspect-ratio")
            {
                var ratio = Ratio(value) ?? throw new Malformed();
                var compare = Compare(size.Width * ratio.Den, size.Height * ratio.Num);
                return Bool(prefix switch { "min" => compare >= 0, "max" => compare <= 0, _ => compare == 0 });
            }

            var length = Length(value) ?? throw new Malformed();
            return Bool(prefix switch { "min" => actual >= length, "max" => actual <= length, _ => actual == length });
        }

        /// <summary><c>(width &gt;= 600px)</c>, <c>(600px &lt;= width)</c>, <c>(400px &lt; width &lt; 700px)</c>.</summary>
        private Tri Range(List<Token> t)
        {
            var ops = new List<int>();
            for (var i = 0; i < t.Count; i++)
            {
                if (t[i].Kind is Kind.Less or Kind.LessEqual or Kind.Greater or Kind.GreaterEqual or Kind.Equal)
                {
                    ops.Add(i);
                }
            }

            if (ops.Count is < 1 or > 2)
            {
                throw new Malformed();
            }

            var parts = new List<List<Token>>();
            var from = 0;
            foreach (var op in ops)
            {
                parts.Add(t.GetRange(from, op - from));
                from = op + 1;
            }

            parts.Add(t.GetRange(from, t.Count - from));
            var nameIndex = parts.FindIndex(p => p is [{ Kind: Kind.Ident }]);
            if (nameIndex < 0 || (ops.Count == 2 && nameIndex != 1))
            {
                throw new Malformed();
            }

            var name = parts[nameIndex][0].Text;
            if (name is not ("width" or "height" or "aspect-ratio"))
            {
                // A double range of something it doesn't read, or a value where the name should be.
                return name.StartsWith("min-", StringComparison.Ordinal) || name.StartsWith("max-", StringComparison.Ordinal)
                    ? throw new Malformed()
                    : Tri.Unknown;
            }

            var result = Tri.True;
            for (var i = 0; i < ops.Count; i++)
            {
                var left = Value(name, parts[i], i == nameIndex);
                var right = Value(name, parts[i + 1], i + 1 == nameIndex);
                var compare = Compare(left, right);
                var holds = t[ops[i]].Kind switch
                {
                    Kind.Less => compare < 0,
                    Kind.LessEqual => compare <= 0,
                    Kind.Greater => compare > 0,
                    Kind.GreaterEqual => compare >= 0,
                    _ => compare == 0,
                };
                result = And(result, Bool(holds));
            }

            if (ops.Count == 2 && (IsLess(t[ops[0]].Kind) != IsLess(t[ops[1]].Kind) || t[ops[0]].Kind == Kind.Equal || t[ops[1]].Kind == Kind.Equal))
            {
                throw new Malformed();
            }

            return result;
        }

        private static bool IsLess(Kind kind) => kind is Kind.Less or Kind.LessEqual;

        /// <summary>A side of a range: the feature's own value, or a length (a ratio for <c>aspect-ratio</c>, as width over height).</summary>
        private double Value(string name, List<Token> part, bool isName)
        {
            if (name == "aspect-ratio")
            {
                if (isName)
                {
                    return size.Height == 0 ? double.PositiveInfinity : size.Width / size.Height;
                }

                var ratio = Ratio(part) ?? throw new Malformed();
                return ratio.Num / ratio.Den;
            }

            return isName ? (name == "width" ? size.Width : size.Height) : Length(part) ?? throw new Malformed();
        }

        private static int Compare(double a, double b) => Math.Abs(a - b) < 1e-9 ? 0 : a < b ? -1 : 1;

        private static double? Length(List<Token> value)
        {
            if (value is not [{ Kind: Kind.Number } n])
            {
                return null;
            }

            double? scale = n.Unit switch
            {
                "px" => 1,
                "em" or "rem" => FontSize,
                "in" => 96,
                "cm" => 96 / 2.54,
                "mm" => 96 / 25.4,
                "q" => 96 / 101.6,
                "pt" => 96 / 72.0,
                "pc" => 16,
                "" when n.Number == 0 => 1,
                _ => null,
            };
            return n.Number * scale;
        }

        private static (double Num, double Den)? Ratio(List<Token> value) => value switch
        {
            [{ Kind: Kind.Number, Unit: "" } a, { Kind: Kind.Slash }, { Kind: Kind.Number, Unit: "" } b] when a.Number > 0 && b.Number > 0
                => (a.Number, b.Number),
            [{ Kind: Kind.Number, Unit: "" } a] when a.Number > 0 => (a.Number, 1),
            _ => null,
        };

        // ── Helpers ──────────────────────────────────────────────────────────────────────────

        private bool IsIdent(string text) => Peek is { Kind: Kind.Ident } t && t.Text == text;

        private Token? Next(int offset) => _at + offset < tokens.Count ? tokens[_at + offset] : null;

        private void Expect(Kind kind)
        {
            if (Peek is not { } t || t.Kind != kind)
            {
                throw new Malformed();
            }

            _at++;
        }

        private static Tri Bool(bool value) => value ? Tri.True : Tri.False;

        private static Tri Not(Tri value) => value switch { Tri.True => Tri.False, Tri.False => Tri.True, _ => Tri.Unknown };

        private static Tri And(Tri a, Tri b) =>
            a == Tri.False || b == Tri.False ? Tri.False : a == Tri.Unknown || b == Tri.Unknown ? Tri.Unknown : Tri.True;

        private static Tri Or(Tri a, Tri b) =>
            a == Tri.True || b == Tri.True ? Tri.True : a == Tri.Unknown || b == Tri.Unknown ? Tri.Unknown : Tri.False;
    }
}
