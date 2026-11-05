using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.button-inner</c>: a flex row with centred content, where <c>[slot=start]</c> and <c>[slot=end]</c> don't
/// shrink, and the default slot does. Children go in slot order, and the one named <c>PART_Content</c> is the one
/// that shrinks.
/// </summary>
/// <remarks>
/// <c>display: inline-block</c> shrinks to fit: as wide as its content on one line, or everything available if that
/// doesn't fit. Avalonia would give the width of the longest line after wrapping, so this takes all of it, which
/// also holds a slotted icon at the edge, as flex does.
/// </remarks>
public sealed class ButtonInner : Panel
{
    private bool _wrapped;

    protected override Size MeasureOverride(Size availableSize)
    {
        var unlimited = new Size(double.PositiveInfinity, availableSize.Height);
        double fixedWidth = 0, height = 0;
        Control? shrinks = null;

        foreach (var child in Children)
        {
            if (child.Name == "PART_Content")
            {
                shrinks = child;
                continue;
            }

            child.Measure(unlimited);
            fixedWidth += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        _wrapped = false;
        if (shrinks is null)
        {
            return new Size(fixedWidth, height);
        }

        shrinks.Measure(unlimited);
        var room = Math.Max(0, availableSize.Width - fixedWidth);
        var width = fixedWidth + shrinks.DesiredSize.Width;
        if (shrinks.DesiredSize.Width > room)
        {
            _wrapped = true;
            shrinks.Measure(new Size(room, availableSize.Height));
            width = availableSize.Width;
        }

        return new Size(width, Math.Max(height, shrinks.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var fixedWidth = Children.Where(child => child.Name != "PART_Content").Sum(child => child.DesiredSize.Width);

        double WidthOf(Control child) =>
            _wrapped && child.Name == "PART_Content" ? Math.Max(0, finalSize.Width - fixedWidth) : child.DesiredSize.Width;

        var x = Math.Max(0, (finalSize.Width - Children.Sum(WidthOf)) / 2);
        foreach (var child in Children)
        {
            var width = WidthOf(child);
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }
}
