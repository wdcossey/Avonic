using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// The part of <see cref="AvonicItemOption"/>'s template between the host's background and <c>PART_Native</c>: on a full swipe it
/// adds an expandable option's <c>padding-left: 100%</c> (or right) before or after its child, so that the background covers the
/// row and the content stays where it was. It adds nothing otherwise.
/// </summary>
public sealed class ItemOptionExpander : Decorator
{
    private AvonicItemOption? Option => TemplatedParent as AvonicItemOption;

    protected override Size MeasureOverride(Size availableSize)
    {
        var expansion = Option?.Expansion ?? 0;
        if (Child is not { } child)
        {
            return new Size(expansion, 0);
        }

        child.Measure(availableSize.Deflate(new Thickness(expansion, 0, 0, 0)));
        return new Size(child.DesiredSize.Width + expansion, child.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var expansion = Math.Min(Option?.Expansion ?? 0, finalSize.Width);
        var before = Option?.ExpandsBefore ?? true;
        Child?.Arrange(new Rect(before ? expansion : 0, 0, finalSize.Width - expansion, finalSize.Height));
        return finalSize;
    }
}
