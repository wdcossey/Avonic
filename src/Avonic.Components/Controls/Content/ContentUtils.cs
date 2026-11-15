using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>utils/content</c>: how a control finds the content it scrolls with. <see cref="AvonicInfiniteScroll"/> and
/// <see cref="AvonicReorderGroup"/> call <see cref="FindClosestIonContent"/> and <see cref="GetScrollElement"/>, as their
/// <c>.js</c> does; <see cref="AvonicRefresher"/> finds its content its own way (<c>closest('ion-content')</c>, in its fixed slot),
/// then a scroll host in it with <see cref="FindScrollHost"/>, and calls <see cref="GetScrollElementWithin"/>.
/// </summary>
internal static class ContentUtils
{
    /// <summary>
    /// <c>contentEl.querySelector('.ion-content-scroll-host')</c>: the first control in <paramref name="content"/>, in document
    /// order, with <see cref="AvonicContent.ScrollHostClass"/>. What an app puts in it, as a <c>querySelector</c> searches the light
    /// DOM and not the content's shadow root: its <see cref="AvonicContent.Content"/> and <see cref="AvonicContent.FixedContent"/>,
    /// in that order, and their logical descendants, as the slots' presenters, not the content, are their logical parents. Content
    /// that is data, shown through a template, isn't searched.
    /// </summary>
    public static Control? FindScrollHost(AvonicContent content)
    {
        foreach (var slot in new[] { content.Content, content.FixedContent })
        {
            if (slot is not Control root)
            {
                continue;
            }

            foreach (var logical in root.GetSelfAndLogicalDescendants())
            {
                if (logical is Control control && control.Classes.Contains(AvonicContent.ScrollHostClass))
                {
                    return control;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// <c>getScrollElement(host)</c>, for a caller outside the host: an <see cref="AvonicContent"/>'s
    /// <see cref="AvonicContent.GetScrollElement"/>; the host, if it's a <see cref="ScrollViewer"/>; otherwise the first one in it,
    /// such as a <c>ListBox</c>'s template has, as a host with the class scrolls itself in a browser and only a
    /// <see cref="ScrollViewer"/> can here.
    /// </summary>
    public static ScrollViewer? GetScrollElementWithin(Control host)
    {
        switch (host)
        {
            case AvonicContent content:
                return content.GetScrollElement();
            case ScrollViewer scroller:
                return scroller;
            case TemplatedControl templated:
                templated.ApplyTemplate();
                break;
        }

        return host.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
    }

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
