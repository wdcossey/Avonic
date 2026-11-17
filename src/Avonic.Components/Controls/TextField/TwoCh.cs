using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic 9's <c>min-width: 2ch</c> on the native <c>&lt;input&gt;</c> and <c>&lt;textarea&gt;</c>, so that a long label or wide
/// slotted content leaves the value some room: twice the advance of the field's <c>0</c> glyph, CSS's <c>ch</c>. Measured in
/// Chromium: 17.98 at 16 px Roboto, 44.96 at 40 px. Given at style priority, bound before the field's styles, so an app's
/// <c>MinWidth</c> wins. The field calls <see cref="Update"/> when its font changes.
/// </summary>
internal sealed class TwoCh
{
    private readonly TextBox _field;
    private readonly StyleValue<double> _minWidth = new();

    public TwoCh(TextBox field)
    {
        _field = field;
        field.Bind(Layoutable.MinWidthProperty, _minWidth, BindingPriority.Style);
    }

    /// <summary>Whether <paramref name="property"/> is one of the font's, which change <c>ch</c>.</summary>
    public static bool IsFont(AvaloniaProperty property) =>
        property == TemplatedControl.FontFamilyProperty || property == TemplatedControl.FontSizeProperty
        || property == TemplatedControl.FontStyleProperty || property == TemplatedControl.FontWeightProperty
        || property == TemplatedControl.FontStretchProperty;

    /// <summary><c>1ch</c> in <paramref name="field"/>'s font: the advance of <c>0</c>.</summary>
    public static double Ch(TextBox field)
    {
        var typeface = new Typeface(field.FontFamily, field.FontStyle, field.FontWeight, field.FontStretch);
        using var layout = new TextLayout("0", typeface, field.FontSize, null);
        return layout.WidthIncludingTrailingWhitespace;
    }

    /// <summary>Works <c>2ch</c> out again, in the field's font now.</summary>
    public void Update() => _minWidth.Set(2 * Ch(_field));
}
