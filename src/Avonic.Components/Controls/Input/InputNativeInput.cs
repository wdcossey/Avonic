using System.Buffers.Binary;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Avonic.Components.Controls;

/// <summary>
/// The native <c>&lt;input&gt;</c> of <see cref="AvonicInput"/>: a <see cref="TextBox"/> that, while <see cref="AvonicInput"/> is
/// <c>width: auto</c>, wants the width Chromium gives an <c>&lt;input&gt;</c> of 20 characters, its <c>size</c>'s default, and
/// takes less when there's less. See <c>docs/components/ion-input/README.md</c>.
/// </summary>
/// <remarks>
/// Chromium's <c>LayoutTextControlSingleLine</c>: 20 × the character's width, rounded up, plus the font's widest glyph less one
/// character when the font gives an average width, as Roboto does. The character is the <c>x</c> glyph's advance, rounded, as
/// Chromium on Windows takes it, where the font gives Skia no average: the textarea's <c>cols</c> measure the same. The widest
/// glyph is the <c>head</c> table's <c>xMax - xMin</c>, rounded. Measured 199 wide at 16 px Roboto and 298 at 24 px, which this
/// gives.
/// </remarks>
public sealed class InputNativeInput : TextBox
{
    /// <summary>HTML's <c>size</c> when it isn't set, which <c>ion-input</c> doesn't set.</summary>
    public const int DefaultSize = 20;

    private bool _sizesToCharacters;
    private double _sizedWidth = double.NaN;

    /// <summary>The look of a <see cref="TextBox"/>, as an app's own <c>TextBox</c> styles reach the input's field too.</summary>
    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <summary>
    /// Wants the width of <see cref="DefaultSize"/> characters, as Chromium's <c>&lt;input&gt;</c> does, rather than its text's: the
    /// input is <c>width: auto</c>, in an item's start or end slot.
    /// </summary>
    internal bool SizesToCharacters
    {
        get => _sizesToCharacters;
        set
        {
            if (_sizesToCharacters != value)
            {
                _sizesToCharacters = value;
                InvalidateMeasure();
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FontFamilyProperty || change.Property == FontSizeProperty || change.Property == FontStyleProperty
            || change.Property == FontWeightProperty || change.Property == FontStretchProperty)
        {
            _sizedWidth = double.NaN;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        if (!_sizesToCharacters)
        {
            return size;
        }

        if (double.IsNaN(_sizedWidth))
        {
            _sizedWidth = SizedWidth();
        }

        var width = _sizedWidth + Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
        return new Size(Math.Min(width, availableSize.Width), size.Height);
    }

    /// <summary><c>PreferredContentLogicalWidth</c> for <see cref="DefaultSize"/> characters.</summary>
    private double SizedWidth()
    {
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        using var layout = new TextLayout("x", typeface, FontSize, null);
        var character = Math.Round(layout.WidthIncludingTrailingWhitespace, MidpointRounding.AwayFromZero);
        var width = Math.Ceiling(character * DefaultSize);

        if (FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface)
            && WidestGlyph(glyphTypeface, FontSize) is { } widest)
        {
            width += widest - character;
        }

        return width;
    }

    /// <summary>
    /// The <c>head</c> table's <c>xMax - xMin</c> at <paramref name="size"/>, rounded, when the <c>OS/2</c> table gives an average
    /// width, as Chromium's <c>HasValidAvgCharWidth</c> asks. Null if the font's tables can't be read.
    /// </summary>
    private static double? WidestGlyph(GlyphTypeface glyphTypeface, double size)
    {
        try
        {
            if (!glyphTypeface.PlatformTypeface.TryGetStream(out var stream))
            {
                return null;
            }

            using (stream)
            {
                var font = new byte[stream.Length];
                stream.ReadExactly(font);
                if (Table(font, "head") is not { Length: >= 44 } head || Table(font, "OS/2") is not { Length: >= 4 } os2
                    || BinaryPrimitives.ReadInt16BigEndian(os2[2..]) == 0)
                {
                    return null;
                }

                var unitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(head[18..]);
                var xMin = BinaryPrimitives.ReadInt16BigEndian(head[36..]);
                var xMax = BinaryPrimitives.ReadInt16BigEndian(head[40..]);
                return unitsPerEm == 0 ? null : Math.Round((xMax - xMin) * size / unitsPerEm, MidpointRounding.AwayFromZero);
            }
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>A table of a single font's file, by its tag. Null for a collection, or a tag it hasn't got.</summary>
    private static ReadOnlySpan<byte> Table(byte[] font, string tag)
    {
        var tables = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4));
        for (var i = 0; i < tables; i++)
        {
            var record = font.AsSpan(12 + i * 16, 16);
            if (record[0] == tag[0] && record[1] == tag[1] && record[2] == tag[2] && record[3] == tag[3])
            {
                var offset = (int)BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(record[12..]);
                return font.AsSpan(offset, length);
            }
        }

        return null;
    }
}
