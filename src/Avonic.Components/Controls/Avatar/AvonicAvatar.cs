using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-avatar</c>: a round picture, often at the start of an <see cref="AvonicItem"/>. It has no properties of its own.
/// Its look is in the theme's <c>Controls/AvonicAvatar.axaml</c> and <c>Styles/Avatar.axaml</c>, and its size and margins in
/// an item's or a divider's slot in <c>Styles/Item.axaml</c> and <c>Styles/ItemDivider.axaml</c>.
/// </summary>
public class AvonicAvatar : ContentControl
{
    static AvonicAvatar()
    {
        // The host has no overflow: hidden. Only a slotted image is cut to the radius.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicAvatar>(false);
    }
}
