using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.button-inner</c>: a flex row where <c>[slot=start]</c> and <c>[slot=end]</c> don't shrink, and the default slot
/// does. Children go in slot order, and the one named <c>PART_Content</c> is the one that shrinks. The template centres
/// this panel in the button, which is <c>justify-content: center</c>.
/// </summary>
/// <remarks>
/// <c>display: inline-block</c> shrinks to fit: as wide as its content on one line, or everything available if that
/// doesn't fit. Avalonia would give the width of the longest line after wrapping. Measuring on one line first gives
/// CSS's answer, because a control is never wider than what's available, and it also keeps a slotted icon at the edge.
/// </remarks>
public sealed class ButtonInner : Panel
{
    private const string ShrinkingChild = "PART_Content";

    protected override Size MeasureOverride(Size availableSize)
    {
        var oneLine = new Size(double.PositiveInfinity, availableSize.Height);
        double width = 0, height = 0;
        Control? shrinks = null;

        foreach (var child in Children)
        {
            child.Measure(oneLine);
            width += child.DesiredSize.Width;
            if (child.Name == ShrinkingChild)
            {
                shrinks = child;
            }
            else
            {
                height = Math.Max(height, child.DesiredSize.Height);
            }
        }

        if (shrinks is not null)
        {
            if (width > availableSize.Width)
            {
                var others = width - shrinks.DesiredSize.Width;
                shrinks.Measure(new Size(Math.Max(0, availableSize.Width - others), availableSize.Height));
            }

            height = Math.Max(height, shrinks.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rest = finalSize.Width - Children.Where(child => child.Name != ShrinkingChild).Sum(child => child.DesiredSize.Width);

        double x = 0;
        foreach (var child in Children)
        {
            var width = child.Name == ShrinkingChild ? Math.Max(0, rest) : child.DesiredSize.Width;
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }
}
