using System.Globalization;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's menu animations, <c>utils/menu-controller/animations/</c>, md: what <see cref="MenuController"/> registers for each
/// <see cref="MenuType"/>. Each moves the parts with physical distances, as CSS's <c>translateX</c> is.
/// </summary>
internal static class MenuAnimations
{
    /// <summary><c>baseAnimation</c>: 300 ms in md, the Material guide's time for an element leaving and coming back.</summary>
    internal const double Duration = 300;

    /// <summary>The overlay type's backdrop opacity in md, a literal in <c>overlay.js</c>.</summary>
    internal const double OverlayBackdropOpacity = 0.25;

    /// <summary>The push type's backdrop opacity, a literal in <c>push.js</c>. The backdrop isn't displayed for push.</summary>
    internal const double PushBackdropOpacity = 0.32;

    /// <summary>The overlay's extra distance off screen, so that its shadow is off too: <c>menu.width + 8</c>.</summary>
    internal const double OverlayShadowRoom = 8;

    private static AvonicAnimation Base() => AvonicAnimation.Create().Duration(Duration);

    /// <summary><c>menuOverlayAnimation</c>: the menu slides over the content, which doesn't move.</summary>
    internal static AvonicAnimation Overlay(MenuAnimationContext menu)
    {
        var width = menu.Width + OverlayShadowRoom;
        var closedX = menu.IsEndSide ? width : -width;
        var menuAnimation = AvonicAnimation.Create()
            .AddElement(menu.Container)
            .FromTo("transform", TranslateX(closedX), TranslateX(0));
        var backdropAnimation = AvonicAnimation.Create()
            .AddElement(menu.Backdrop)
            .FromTo("opacity", 0.01, OverlayBackdropOpacity);
        return Base().AddAnimation(menuAnimation, backdropAnimation);
    }

    /// <summary><c>menuPushAnimation</c>: the content slides over to show the menu, which slides in with it.</summary>
    internal static AvonicAnimation Push(MenuAnimationContext menu)
    {
        var width = menu.Width;
        var contentOpenedX = menu.IsEndSide ? -width : width;
        var menuClosedX = menu.IsEndSide ? width : -width;
        var menuAnimation = AvonicAnimation.Create()
            .AddElement(menu.Container)
            .FromTo("transform", TranslateX(menuClosedX), TranslateX(0));
        var backdropAnimation = AvonicAnimation.Create()
            .AddElement(menu.Backdrop)
            .FromTo("opacity", 0.01, PushBackdropOpacity);
        return Base().AddAnimation([menuAnimation, .. Content(menu, contentOpenedX), backdropAnimation]);
    }

    /// <summary><c>menuRevealAnimation</c>: the content slides over to show the menu under it, which doesn't move.</summary>
    internal static AvonicAnimation Reveal(MenuAnimationContext menu)
    {
        var openedX = menu.Width * (menu.IsEndSide ? -1 : 1);
        return Base().AddAnimation([.. Content(menu, openedX)]);
    }

    /// <summary>
    /// The content's <c>translateX(0)</c> to <c>translateX(openedX)</c>, and the same for the part that draws its shadow. The content
    /// is the app's, in its own coordinates, so a mirrored parent gets the distance negated; the shadow part is the menu's, laid out
    /// left to right. Neither if there's no content, as Ionic's <c>addElement(undefined)</c> adds nothing.
    /// </summary>
    private static IEnumerable<AvonicAnimation> Content(MenuAnimationContext menu, double openedX)
    {
        if (menu.Content is { } content)
        {
            yield return AvonicAnimation.Create()
                .AddElement(content)
                .FromTo("transform", TranslateX(0), TranslateX(openedX * menu.ContentSign));
        }

        if (menu.ContentShadow is { } shadow)
        {
            yield return AvonicAnimation.Create()
                .AddElement(shadow)
                .FromTo("transform", TranslateX(0), TranslateX(openedX));
        }
    }

    private static string TranslateX(double x) => string.Create(CultureInfo.InvariantCulture, $"translateX({x}px)");
}
