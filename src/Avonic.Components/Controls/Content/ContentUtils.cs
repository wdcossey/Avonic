using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>utils/content</c>: how a control finds the content it scrolls with. <see cref="AvonicInfiniteScroll"/> and
/// <see cref="AvonicReorderGroup"/> call both, as their <c>.js</c> does; <see cref="AvonicRefresher"/> finds its content its own way
/// (<c>closest('ion-content')</c>, in its fixed slot) and calls <see cref="GetScrollElement"/>.
/// </summary>
internal static class ContentUtils
{
    /// <summary>
    /// <c>findClosestIonContent</c>: <c>el.closest('ion-content, .ion-content-scroll-host')</c>, which starts at the element itself
    /// and stops at the first <see cref="AvonicContent"/>, or control with <see cref="AvonicContent.ScrollHostClass"/>.
    /// </summary>
    public static Control? FindClosestIonContent(Visual el)
    {
        foreach (var visual in el.GetSelfAndVisualAncestors())
        {
            if (visual is AvonicContent || (visual is Control control && control.Classes.Contains(AvonicContent.ScrollHostClass)))
            {
                return (Control)visual;
            }
        }

        return null;
    }

    /// <summary>
    /// <c>getScrollElement(host)</c>: an <see cref="AvonicContent"/>'s <see cref="AvonicContent.GetScrollElement"/>, which a replaced
    /// template may not have. A host with the class scrolls itself in a browser; here only a <see cref="ScrollViewer"/> can, so it's
    /// the host, or the nearest one around <paramref name="from"/> on the way up to it, as a <c>ListBox</c>'s template has.
    /// </summary>
    public static ScrollViewer? GetScrollElement(Control host, Visual from)
    {
        if (host is AvonicContent content)
        {
            return content.GetScrollElement();
        }

        if (host is ScrollViewer scroller)
        {
            return scroller;
        }

        for (var visual = from; visual is not null && !ReferenceEquals(visual, host); visual = visual.GetVisualParent())
        {
            if (visual is ScrollViewer inside)
            {
                return inside;
            }
        }

        return null;
    }
}
