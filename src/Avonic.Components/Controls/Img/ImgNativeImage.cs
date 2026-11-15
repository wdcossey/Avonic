using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>ion-img</c>'s <c>&lt;img part="image"&gt;</c>, <see cref="AvonicImg"/>'s <c>PART_Image</c>: an <see cref="Image"/>, styled as
/// one, that takes the picture's fit from the control at style priority, bound as it's made, before any style reaches it, so that an
/// app's style for it wins, as a page's rule beats <c>::slotted(ion-img) { object-fit: cover }</c> in an avatar or a thumbnail.
/// </summary>
public sealed class ImgNativeImage : Image
{
    public ImgNativeImage() => Bind(StretchProperty, Fit, BindingPriority.Style);

    /// <summary>The look of an <see cref="Image"/>, so that an app's <c>Image</c> styles reach it.</summary>
    protected override Type StyleKeyOverride => typeof(Image);

    /// <summary><c>object-fit</c>: cover in an avatar or a thumbnail, and otherwise the <c>contain</c> an image has by default.</summary>
    internal StyleValue<Stretch> Fit { get; } = new();
}
