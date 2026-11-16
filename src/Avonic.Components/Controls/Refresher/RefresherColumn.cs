using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>ion-refresher-content</c>'s own box: <c>display: flex; flex-direction: column; justify-content: center</c>, its children
/// one under the other at its full width, centred down it even when they're taller than it, as a flex column overflows both
/// ways. A <see cref="StackPanel"/> centred instead overflows only down: md's cancelling state shows both icons, 84 high in the
/// refresher's 60, and Chromium puts them 12 above its top (#167).
/// </summary>
public class RefresherColumn : Panel
{
    // The children's heights added up at the last measure, which their DesiredSize keeps until the next
    private double _height;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var height = 0.0;
        var child = availableSize.WithHeight(double.PositiveInfinity);
        foreach (var control in Children)
        {
            control.Measure(child);
            width = Math.Max(width, control.DesiredSize.Width);
            height += control.DesiredSize.Height;
        }

        _height = height;
        return new Size(width, height);
    }

    /// <summary>
    /// Each child at the column's width, or its own if that's wider, as a <see cref="StackPanel"/> gives it, so that a child too
    /// wide for the column keeps its size.
    /// </summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = (finalSize.Height - _height) / 2;
        foreach (var control in Children)
        {
            control.Arrange(new Rect(0, y, Math.Max(finalSize.Width, control.DesiredSize.Width), control.DesiredSize.Height));
            y += control.DesiredSize.Height;
        }

        return finalSize;
    }
}
