using Avalonia;
using Avalonia.Controls.Documents;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-text</c> in a sentence: words in a <see cref="Avalonia.Controls.TextBlock"/> that take
/// <see cref="Color"/>, flowing and wrapping with the words around them. Around anything else, use
/// <see cref="AvonicText"/>.
/// </summary>
public class AvonicTextSpan : Span
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicTextSpan>();

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
}
