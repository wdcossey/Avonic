using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// The root of <see cref="AvonicMenu"/>'s template: a panel that fills the menu with each child, as a <see cref="Panel"/> does, except
/// <c>PART_ContentShadow</c>, which it puts over the main content's box, without its transform: the content's own <c>box-shadow</c>
/// for push and reveal in Ionic. Placed here, in the panel's own arrange, it stays there whatever pass arranges the panel.
/// </summary>
public class MenuRootPanel : Panel
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        var menu = TemplatedParent as AvonicMenu;
        foreach (var child in Children)
        {
            child.Arrange(menu is not null && child.Name == "PART_ContentShadow" ? menu.ContentShadowRect(this) : new Rect(finalSize));
        }

        return finalSize;
    }
}
