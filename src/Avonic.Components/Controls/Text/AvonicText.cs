using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-text</c> around anything: its content takes <see cref="Color"/>. To colour words in a sentence, use
/// <see cref="AvonicTextSpan"/>, because a box can't flow with the words around it.
/// </summary>
public class AvonicText : ContentControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicText>();

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
}
