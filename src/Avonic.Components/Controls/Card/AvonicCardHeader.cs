using Avalonia;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-card-header</c>: the top of a card, usually with an <see cref="AvonicCardTitle"/> and an
/// <see cref="AvonicCardSubtitle"/> in a panel. Its look is in the theme's <c>Styles/Card.axaml</c>.
/// </summary>
public class AvonicCardHeader : ContentControl
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicCardHeader>();

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
}
