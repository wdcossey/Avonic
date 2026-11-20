using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// Where <see cref="AvonicNav"/>'s pages are drawn: each over the others at the nav's full size, as <c>.ion-page</c>'s
/// <c>position: absolute; inset: 0</c> places it, in the order of their <c>z-index</c>. The pages are the nav's logical children
/// and this part's visual children, as a <see cref="ContentPresenter"/> shows its host's content.
/// </summary>
public class NavPagesHost : Control
{
    private AvonicNav? _nav;

    static NavPagesHost()
    {
        // .ion-page has no overflow rule: a page's content isn't clipped by the nav.
        ClipToBoundsProperty.OverrideDefaultValue<NavPagesHost>(false);
    }

    internal void Attach(AvonicNav nav)
    {
        if (ReferenceEquals(_nav, nav))
        {
            return;
        }

        Detach();
        _nav = nav;
        foreach (var page in nav.Pages)
        {
            VisualChildren.Add(page);
        }

        InvalidateMeasure();
    }

    internal void Detach()
    {
        if (_nav is null)
        {
            return;
        }

        VisualChildren.Clear();
        _nav = null;
    }

    internal void Add(Control page)
    {
        VisualChildren.Add(page);
        InvalidateMeasure();
    }

    internal void Remove(Control page)
    {
        VisualChildren.Remove(page);
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = default(Size);
        foreach (var child in VisualChildren)
        {
            if (child is Control page)
            {
                page.Measure(availableSize);
                size = new Size(Math.Max(size.Width, page.DesiredSize.Width), Math.Max(size.Height, page.DesiredSize.Height));
            }
        }

        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in VisualChildren)
        {
            if (child is Control page)
            {
                page.Arrange(new Rect(finalSize));
            }
        }

        return finalSize;
    }
}
