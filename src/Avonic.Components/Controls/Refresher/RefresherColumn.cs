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

        return new Size(width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var height = Children.Sum(control => control.DesiredSize.Height);
        var y = (finalSize.Height - height) / 2;
        foreach (var control in Children)
        {
            control.Arrange(new Rect(0, y, finalSize.Width, control.DesiredSize.Height));
            y += control.DesiredSize.Height;
        }

        return finalSize;
    }
}
