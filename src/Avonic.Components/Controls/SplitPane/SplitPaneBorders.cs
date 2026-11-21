using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// The part of <see cref="AvonicSplitPane"/>'s template that draws each side's border, <c>--border</c>, where
/// <see cref="SplitPanePanel"/> leaves room for it, in the panel's <see cref="SplitPanePanel.BorderBrush"/>: a panel can't draw over
/// its own background in Avalonia. It takes no input.
/// </summary>
public class SplitPaneBorders : Control
{
    static SplitPaneBorders()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<SplitPaneBorders>(false);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (TemplatedParent is not AvonicSplitPane { PanelRoot: { BorderBrush: { } brush } panel } || panel.TransformToVisual(this) is not { } toHere)
        {
            return;
        }

        foreach (var rect in panel.BorderRects())
        {
            context.FillRectangle(brush, rect.TransformToAABB(toHere));
        }
    }
}
