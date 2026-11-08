using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-thumbnail</c>: a square picture, often at the start of an <see cref="AvonicItem"/>. It has no properties of
/// its own. Its look is in the theme's <c>Controls/AvonicThumbnail.axaml</c> and <c>Styles/Thumbnail.axaml</c>.
/// </summary>
[PseudoClasses(":in-item")]
public class AvonicThumbnail : ContentControl
{
    static AvonicThumbnail()
    {
        // The host has no overflow: hidden. Only a slotted image is cut to the radius.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicThumbnail>(false);
    }

    /// <summary>
    /// <c>:in-item</c>, when an item, a divider or a group is around it. Ionic has no such class: it's how a theme's
    /// <c>::slotted(ion-thumbnail[slot=start])</c> beats the item's <c>::slotted([slot=start])</c>, as a rule with a pseudoclass
    /// beats one without, where CSS goes by the attribute it adds.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        PseudoClasses.Set(":in-item", this.FindAncestorOfType<IItemHost>() is not null);
    }
}
