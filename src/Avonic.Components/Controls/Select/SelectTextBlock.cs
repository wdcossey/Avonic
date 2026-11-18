using Avalonia;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avonic.Components.Behaviors;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>ion-select</c>'s <c>.select-text</c>: the value's text or the placeholder, one line, trimmed with an ellipsis, and at least
/// <c>min-width: 2ch</c> wide: twice the advance of its font's <c>0</c>, CSS's <c>ch</c> (measured in Chromium: 17.98 at 16 px
/// Roboto, the same as the input's). Given at style priority, bound before its styles, so an app's <c>MinWidth</c> wins.
/// </summary>
public class SelectTextBlock : TextBlock
{
    private readonly StyleValue<double> _minWidth = new();

    public SelectTextBlock()
    {
        Bind(MinWidthProperty, _minWidth, BindingPriority.Style);
        UpdateMinWidth();
    }

    protected override Type StyleKeyOverride => typeof(SelectTextBlock);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FontFamilyProperty || change.Property == FontSizeProperty || change.Property == FontStyleProperty
            || change.Property == FontWeightProperty || change.Property == FontStretchProperty)
        {
            UpdateMinWidth();
        }
    }

    private void UpdateMinWidth()
    {
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        using var layout = new TextLayout("0", typeface, FontSize, null);
        _minWidth.Set(2 * layout.WidthIncludingTrailingWhitespace);
    }
}
