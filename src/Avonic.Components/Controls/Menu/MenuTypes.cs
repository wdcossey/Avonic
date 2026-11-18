using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>MenuType</c>: how the menu comes in.</summary>
public enum MenuType
{
    /// <summary><c>overlay</c>: the menu slides over the content, which stays where it is, behind a backdrop. md's default.</summary>
    Overlay,

    /// <summary><c>reveal</c>: the content slides away to show the menu under it, which doesn't move.</summary>
    Reveal,

    /// <summary><c>push</c>: the menu slides in and pushes the content along with it.</summary>
    Push,
}

/// <summary>Ionic's <c>Side</c>: which side of the view the menu is on, in the flow direction.</summary>
public enum MenuSide
{
    /// <summary><c>start</c>: the left in left-to-right, the right in right-to-left.</summary>
    Start,

    /// <summary><c>end</c>: the right in left-to-right, the left in right-to-left.</summary>
    End,
}

/// <summary>Ionic's <c>MenuCloseEventDetail</c>, for <see cref="AvonicMenu.IonWillClose"/> and <see cref="AvonicMenu.IonDidClose"/>.</summary>
public sealed class MenuCloseEventArgs : RoutedEventArgs
{
    internal MenuCloseEventArgs(RoutedEvent routedEvent, string? role)
        : base(routedEvent)
    {
        Role = role;
    }

    /// <summary>
    /// The role of what closed the menu: the one given to <see cref="AvonicMenu.Close"/>, <c>backdrop</c> for a click outside it or
    /// Escape, <c>gesture</c> for a swipe or a disable, or null.
    /// </summary>
    public string? Role { get; }
}

/// <summary>
/// What a menu animation is made from: Ionic's <c>MenuI</c> as the builders in <c>animations/</c> read it. Given to a builder
/// registered with <see cref="MenuController.RegisterAnimation"/>.
/// </summary>
/// <param name="Menu">The menu.</param>
/// <param name="Container">Its <c>.menu-inner</c>, <c>PART_Container</c>.</param>
/// <param name="Backdrop">Its <c>PART_Backdrop</c>.</param>
/// <param name="Content">The main content, its <c>ContentId</c>, if it was found.</param>
/// <param name="ContentShadow">
/// The part that draws the main content's shadow for push and reveal, over the content's box: an animation that moves the content
/// moves this the same way. Not in Ionic, where the shadow is the content's own.
/// </param>
/// <param name="Width">The container's width, Ionic's <c>menu.width</c>.</param>
/// <param name="IsEndSide">
/// Ionic's <c>isEndSide</c>: the menu is at the right, physically, as the end side in left-to-right or the start in right-to-left.
/// </param>
/// <param name="ContentSign">
/// 1, or -1 when the content's parent is laid out right to left: a <c>translateX</c> on the content is mirrored there, and the
/// animations give it physical distances, as CSS's are, through this.
/// </param>
public sealed record MenuAnimationContext(
    AvonicMenu Menu,
    Control Container,
    Control Backdrop,
    Control? Content,
    Control? ContentShadow,
    double Width,
    bool IsEndSide,
    int ContentSign);

/// <summary>Makes a menu's animation from its <see cref="MenuAnimationContext"/>: Ionic's animation builder for a menu type.</summary>
public delegate AvonicAnimation MenuAnimationBuilder(MenuAnimationContext context);
