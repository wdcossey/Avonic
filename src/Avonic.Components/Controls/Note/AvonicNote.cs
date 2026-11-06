using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-note</c>: a small piece of text, usually at the end of an <see cref="AvonicItem"/>. Its look is in
/// the theme's <c>Styles/Note.axaml</c>.
/// </summary>
public class AvonicNote : ContentControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicNote>();

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
}
