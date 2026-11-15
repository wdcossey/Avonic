using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>ion-img</c>'s <c>&lt;img part="image"&gt;</c>, <see cref="AvonicImg"/>'s <c>PART_Image</c>: an <see cref="Image"/> that takes
/// the picture's fit from the control at style priority, bound as it's made, before any style reaches it, so that an app's style
/// for it wins, as <c>ion-img::part(image)</c> beats <c>::slotted(ion-img) { object-fit: cover }</c> in an avatar or a thumbnail.
/// Its own type, not <c>Image</c>, to styles, as a page's <c>img</c> rule doesn't reach into the shadow root: an app's
/// <c>Image</c> style leaves it alone, and <c>c|ImgNativeImage</c> reaches it.
/// </summary>
public sealed class ImgNativeImage : Image
{
    public ImgNativeImage() => Bind(StretchProperty, Fit, BindingPriority.Style);

    /// <summary><c>object-fit</c>: cover in an avatar or a thumbnail, and otherwise the <c>contain</c> an image has by default.</summary>
    internal StyleValue<Stretch> Fit { get; } = new();
}
