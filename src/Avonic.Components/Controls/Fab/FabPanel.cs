using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// <see cref="AvonicFab"/>'s items panel: what CSS does with the fab's light DOM. Its button and anything else in the flow go one
/// under another, the fab as wide as the widest (<c>width: fit-content</c>), and its lists are out of the flow
/// (<c>position: absolute</c>), each placed outside the fab by a percentage of the fab's width, which Avalonia has no form for.
/// </summary>
/// <remarks>
/// A list's <c>margin</c> is <c>calc(100% + 10px)</c> on the side it opens to: the <c>100%</c> is here, and the 10 px is the list's
/// own <c>Margin</c>, in its theme. With <see cref="AvonicFab.Edge"/>, <c>fab.css</c> replaces a fab button's top or bottom margin
/// with <c>-50%</c>, or <c>(-100% + 16px) / 2</c> for a small one, and a list's with <c>50%</c>, so that the button sits half out of
/// the fab and the lists stay beside it.
/// </remarks>
public class FabPanel : Panel
{
    /// <summary><c>fab.css</c>'s 16 px in <c>calc((-100% + 16px) / 2)</c>: a small button's top and bottom margins.</summary>
    private const double SmallMargins = 16;

    private double _width;

    protected override Size MeasureOverride(Size availableSize)
    {
        // fit-content: as wide as the content wants, whatever the room.
        var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
        double width = 0;
        foreach (var child in Children)
        {
            child.Measure(infinite);
            if (ListIn(child) is null)
            {
                width = Math.Max(width, child.DesiredSize.Width);
            }
        }

        _width = width;
        var (edgeTop, edgeBottom) = Edges();
        double height = 0;
        foreach (var child in Children)
        {
            if (ListIn(child) is null)
            {
                height += child.DesiredSize.Height + EdgeShift(child, width, edgeTop, edgeBottom);
            }
        }

        return new Size(width, Math.Max(0, height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = _width;
        var (edgeTop, edgeBottom) = Edges();
        double y = 0;
        foreach (var child in Children)
        {
            if (ListIn(child) is not null)
            {
                continue;
            }

            // display: block at its own width, from the start. An edge's top margin moves it up; a bottom margin only
            // shortens the fab.
            var shift = EdgeShift(child, width, edgeTop, edgeBottom);
            child.Arrange(new Rect(0, y + (edgeTop ? shift : 0), child.DesiredSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height + shift;
        }

        var height = Math.Max(0, y);
        foreach (var child in Children)
        {
            if (ListIn(child) is { } list)
            {
                child.Arrange(ListBox(list, child.DesiredSize, width, height, edgeTop, edgeBottom));
            }
        }

        return finalSize;
    }

    /// <summary>
    /// <c>fab-list.css</c>: <c>top: 0</c>, or <c>bottom: 0</c> for the top side, and <c>inset-inline-start</c> or <c>-end: 0</c>
    /// for the sides, with the margin toward the button. A box here is the list's margin box, so the <c>100%</c> is all of it.
    /// </summary>
    private static Rect ListBox(AvonicFabList list, Size size, double width, double height, bool edgeTop, bool edgeBottom)
    {
        var half = width / 2;
        return list.Side switch
        {
            // margin-top: calc(100% + 10px), or calc(50% + 10px) beside a top edge.
            FabListSide.Bottom => new Rect(0, edgeTop ? half : width, size.Width, size.Height),

            // bottom: 0; margin-bottom: calc(100% + 10px), or calc(50% + 10px) beside a bottom edge.
            FabListSide.Top => new Rect(0, height - (edgeBottom ? half : width) - size.Height, size.Width, size.Height),

            // inset-inline-end: 0; margin-inline-end: calc(100% + 10px), so the end is at 100% - 100%; margin-top: -50% beside a top edge.
            FabListSide.Start => new Rect(-size.Width, edgeTop ? -half : 0, size.Width, size.Height),

            // inset-inline-start: 0; margin-inline-start: calc(100% + 10px).
            _ => new Rect(width, edgeTop ? -half : 0, size.Width, size.Height),
        };
    }

    /// <summary>How much an edge's margin changes a fab button's height in the flow: the new margin less its own.</summary>
    private static double EdgeShift(Control child, double width, bool edgeTop, bool edgeBottom)
    {
        if (Unwrapped(child) is not AvonicFabButton button || !(edgeTop || edgeBottom))
        {
            return 0;
        }

        var margin = button.Size == FabButtonSize.Small ? (-width + SmallMargins) / 2 : -width / 2;
        return margin - (edgeTop ? button.Margin.Top : button.Margin.Bottom);
    }

    /// <summary>A child from <c>ItemsSource</c> and a template is in a presenter: what it shows is what counts.</summary>
    private static Control Unwrapped(Control child) => child is ContentPresenter { Child: { } shown } ? shown : child;

    private static AvonicFabList? ListIn(Control child) => Unwrapped(child) as AvonicFabList;

    private (bool Top, bool Bottom) Edges()
    {
        var fab = this.FindAncestorOfType<AvonicFab>();
        if (fab is not { Edge: true })
        {
            return (false, false);
        }

        return (fab.Vertical == FabVertical.Top, fab.Vertical == FabVertical.Bottom);
    }
}
