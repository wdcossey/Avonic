using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avonic.Components.Behaviors;

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

    /// <summary>
    /// The accordion's values for it, bound at style priority as it's made, before any style reaches it, so that an app's style on
    /// it still wins, as <c>::part(content)</c> does.
    /// </summary>
    public AccordionContentClip()
    {
        Bind(IsHitTestVisibleProperty, NoHit, BindingPriority.Style);
        Bind(IsVisibleProperty, Hidden, BindingPriority.Style);
    }

    /// <summary><c>:host(.accordion-readonly) #content { pointer-events: none }</c>.</summary>
    internal StyleValue<bool> NoHit { get; } = new();

    /// <summary><c>:host(.accordion-collapsed) #content { display: none }</c>.</summary>
    internal StyleValue<bool> Hidden { get; } = new();

    /// <summary>Its height with no <c>max-height</c>: the child's own, which is <c>#content-wrapper</c>'s <c>offsetHeight</c>, and the padding.</summary>
    internal double NaturalHeight => (Child?.DesiredSize.Height ?? 0) + Padding.Top + Padding.Bottom;

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
