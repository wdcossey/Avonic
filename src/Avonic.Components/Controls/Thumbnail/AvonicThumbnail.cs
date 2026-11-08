using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-thumbnail</c>: a square picture, often at the start of an <see cref="AvonicItem"/>. It has no properties of
/// its own. Its look is in the theme's <c>Controls/AvonicThumbnail.axaml</c> and <c>Styles/Thumbnail.axaml</c>, and its size and
/// margins in an item's or a divider's slot in <c>Styles/Item.axaml</c> and <c>Styles/ItemDivider.axaml</c>.
/// </summary>
public class AvonicThumbnail : ContentControl
{
    static AvonicThumbnail()
    {
        // The host has no overflow: hidden. Only a slotted image is cut to the radius.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicThumbnail>(false);
    }
}
