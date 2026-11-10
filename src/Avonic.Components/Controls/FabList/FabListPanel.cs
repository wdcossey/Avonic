using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// <see cref="AvonicFabList"/>'s items panel: <c>display: flex</c> with <c>align-items: center</c>, a column for the bottom side,
/// <c>column-reverse</c> for the top, <c>row-reverse</c> for the start and a row for the end, from the list's
/// <see cref="AvonicFabList.Side"/>. The children keep their order, so that Tab follows the list's, as it follows the DOM.
/// </summary>
public class FabListPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var (row, _) = Direction();
        var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
        double main = 0, cross = 0;
        foreach (var child in Children)
        {
            child.Measure(infinite);
            var size = child.DesiredSize;
            main += row ? size.Width : size.Height;
            cross = Math.Max(cross, row ? size.Height : size.Width);
        }

        return row ? new Size(main, cross) : new Size(cross, main);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (row, reverse) = Direction();
        var length = row ? finalSize.Width : finalSize.Height;
        var crossLength = row ? finalSize.Height : finalSize.Width;
        double position = 0;
        foreach (var child in Children)
        {
            var size = child.DesiredSize;
            var main = row ? size.Width : size.Height;
            var cross = row ? size.Height : size.Width;

            // A reversed flex line starts at the far end.
            var start = reverse ? length - position - main : position;
            var across = (crossLength - cross) / 2;
            child.Arrange(row ? new Rect(start, across, main, cross) : new Rect(across, start, cross, main));
            position += main;
        }

        return finalSize;
    }

    private (bool Row, bool Reverse) Direction() => this.FindAncestorOfType<AvonicFabList>()?.Side switch
    {
        FabListSide.Top => (false, true),
        FabListSide.Start => (true, true),
        FabListSide.End => (true, false),
        _ => (false, false),
    };
}
