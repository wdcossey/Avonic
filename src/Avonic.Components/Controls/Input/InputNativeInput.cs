using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;

namespace Avonic.Components.Controls;

/// <summary>
/// The native <c>&lt;input&gt;</c> of <see cref="AvonicInput"/>: a <see cref="TextBox"/> that, while <see cref="AvonicInput"/> is
/// <c>width: auto</c>, wants the width Chromium gives an <c>&lt;input&gt;</c> of 20 characters, its <c>size</c>'s default, and
/// takes less when there's less. See <c>docs/components/ion-input/README.md</c>.
/// </summary>
/// <remarks>
/// Chromium's <c>LayoutTextControlSingleLine</c>: 20 × the character's width, rounded up, plus the font's widest glyph less one
/// character when the font gives an average width, as Roboto does, rounded up to a pixel. The character is the <c>x</c> glyph's
/// advance, as Chromium on Windows takes it where the font gives Skia no average; the widest glyph is the <c>head</c> table's
/// <c>xMax - xMin</c>, rounded. Fitted to Chromium at 14 sizes of Roboto from 12 to 32 px: the same at 11, among them 16 px's 199
/// and 24 px's 298, and 1 px out at 12, 28 and 32 px, where Chromium's own advance seems to be whole.
/// </remarks>
public sealed class InputNativeInput : TextBox, INativeFieldAutomation
{
    /// <summary>HTML's <c>size</c> when it isn't set, which <c>ion-input</c> doesn't set.</summary>
    public const int DefaultSize = 20;

    /// <summary>Each typeface's widest glyph per em, or null, read once.</summary>
    private static readonly ConditionalWeakTable<GlyphTypeface, StrongBox<double?>> s_widest = new();

    private readonly NativeFieldAutomation _automation;
    private bool _sizesToCharacters;
    private double _sizedWidth = double.NaN;

    public InputNativeInput() => _automation = new NativeFieldAutomation(this);

    NativeFieldAutomation INativeFieldAutomation.Automation => _automation;

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
        var character = layout.WidthIncludingTrailingWhitespace;
        var width = Math.Ceiling(character * DefaultSize);

        if (FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface)
            && s_widest.GetValue(glyphTypeface, static face => new StrongBox<double?>(WidestGlyphPerEm(face))).Value is { } perEm)
        {
            width += Math.Round(perEm * FontSize, MidpointRounding.AwayFromZero) - character;
        }

        return Math.Ceiling(width);
    }

    /// <summary>
    /// The <c>head</c> table's <c>xMax - xMin</c> over its units per em, when the <c>OS/2</c> table gives an average width, as
    /// Chromium's <c>HasValidAvgCharWidth</c> asks. Null if the font hasn't the tables. Read once per typeface.
    /// </summary>
    private static double? WidestGlyphPerEm(GlyphTypeface glyphTypeface)
    {
        var font = glyphTypeface.PlatformTypeface;
        if (!font.TryGetTable(new OpenTypeTag('h', 'e', 'a', 'd'), out var headTable) || headTable.Length < 44
            || !font.TryGetTable(new OpenTypeTag('O', 'S', '/', '2'), out var os2Table) || os2Table.Length < 4)
        {
            return null;
        }

        var head = headTable.Span;
        var unitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(head[18..]);
        var xMin = BinaryPrimitives.ReadInt16BigEndian(head[36..]);
        var xMax = BinaryPrimitives.ReadInt16BigEndian(head[40..]);
        return unitsPerEm == 0 || BinaryPrimitives.ReadInt16BigEndian(os2Table.Span[2..]) == 0
            ? null
            : (double)(xMax - xMin) / unitsPerEm;
    }
}
