using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// <see cref="AvonicItemOptions"/>'s items panel: <c>display: flex</c> in a row, packed to the options' side
/// (<c>justify-content</c>), each option as tall as the row. On a full swipe an expandable option goes to the outer end
/// (<c>order: 1</c>, or <c>-1</c> for start options), and what no longer fits leaves on the inner side, as a flex row that
/// overflows its end does. Logical, so RTL mirrors it.
/// </summary>
public class ItemOptionsPanel : Panel
{
    private AvonicItemOptions? Options => this.FindAncestorOfType<AvonicItemOptions>();

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var options = Options;
        var atEnd = options?.Side != ItemOptionsSide.Start;
        var swiping = options?.IsSwiping == true;

        // order: the expandable ones last for end options and first for start ones, in a full swipe; DOM order otherwise.
        var ordered = Children.Where(c => c.IsVisible).ToList();
        if (swiping)
        {
            ordered = atEnd
                ? ordered.OrderBy(c => c is AvonicItemOption { Expandable: true } ? 1 : 0).ToList()
                : ordered.OrderBy(c => c is AvonicItemOption { Expandable: true } ? 0 : 1).ToList();
        }

        var total = ordered.Sum(c => c.DesiredSize.Width);
        var x = atEnd ? finalSize.Width - total : 0;
        foreach (var child in ordered)
        {
            child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
            x += child.DesiredSize.Width;
        }

        return finalSize;
    }
}
