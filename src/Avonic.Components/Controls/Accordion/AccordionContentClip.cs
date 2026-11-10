using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>#content</c> in <c>ion-accordion</c>'s shadow root: a block with <c>overflow: hidden</c> whose <c>max-height</c> the
/// accordion animates. Its child keeps its own height, as a block's does, and what doesn't fit is clipped, so the content slides
/// rather than squeezes. <see cref="Layoutable.MaxHeight"/> is the <c>max-height</c>.
/// </summary>
public class AccordionContentClip : Decorator
{
    static AccordionContentClip()
    {
        ClipToBoundsProperty.OverrideDefaultValue<AccordionContentClip>(true);
    }

    /// <summary>The child's own height, which is <c>#content-wrapper</c>'s <c>offsetHeight</c>.</summary>
    internal double ChildHeight => Child?.DesiredSize.Height ?? 0;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
        {
            return default;
        }

        var padding = Padding;
        child.Measure(new Size(Math.Max(0, availableSize.Width - padding.Left - padding.Right), double.PositiveInfinity));
        return new Size(child.DesiredSize.Width + padding.Left + padding.Right, child.DesiredSize.Height + padding.Top + padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            var padding = Padding;
            child.Arrange(new Rect(
                padding.Left,
                padding.Top,
                Math.Max(0, finalSize.Width - padding.Left - padding.Right),
                child.DesiredSize.Height));
        }

        return finalSize;
    }
}
