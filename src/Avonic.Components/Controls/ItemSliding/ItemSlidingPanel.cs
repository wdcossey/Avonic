using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <see cref="AvonicItemSliding"/>'s items panel: what CSS does with its light DOM. The item, and anything else in the flow, one
/// under another (<c>display: block</c>); the options out of the flow (<c>position: absolute; height: 100%</c>), as tall as the
/// flow and as wide as the sliding item (<c>width: 100%</c>, which they only show with). Logical, so RTL mirrors it.
/// </summary>
public class ItemSlidingPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            if (child is AvonicItemOptions)
            {
                continue;
            }

            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }

        foreach (var child in Children)
        {
            if (child is AvonicItemOptions)
            {
                child.Measure(new Size(double.PositiveInfinity, height));
            }
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        foreach (var child in Children)
        {
            if (child is AvonicItemOptions)
            {
                child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            }
            else
            {
                child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height;
            }
        }

        return finalSize;
    }
}
