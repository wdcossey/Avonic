using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// The alert wrapper's <c>display: flex; flex-direction: column</c>: its children one under another, as wide as the column. When
/// they're taller than the column may be (the wrapper's <c>--max-height</c>), the children marked <see cref="ShrinksProperty"/>,
/// the message and the input group, which are <c>overflow-y: auto</c> and so have a minimum height of 0, give up the difference
/// in proportion to their heights, as <c>flex-shrink: 1</c> does, and scroll. The others keep their height, as their minimum is
/// their content.
/// </summary>
public class AlertColumn : Panel
{
    /// <summary>A flex item that can shrink to nothing: one with <c>overflow</c> other than <c>visible</c>.</summary>
    public static readonly AttachedProperty<bool> ShrinksProperty =
        AvaloniaProperty.RegisterAttached<AlertColumn, Control, bool>("Shrinks");

    private double[] _heights = [];

    static AlertColumn()
    {
        AffectsParentMeasure<AlertColumn>(ShrinksProperty);
    }

    public static bool GetShrinks(Control control) => control.GetValue(ShrinksProperty);

    public static void SetShrinks(Control control, bool value) => control.SetValue(ShrinksProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        var children = Children;
        _heights = new double[children.Count];
        double width = 0, total = 0, shrinkable = 0;
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            _heights[i] = child.DesiredSize.Height;
            width = Math.Max(width, child.DesiredSize.Width);
            total += _heights[i];
            if (GetShrinks(child))
            {
                shrinkable += _heights[i];
            }
        }

        var overflow = total - availableSize.Height;
        if (overflow > 0 && shrinkable > 0)
        {
            var share = Math.Min(overflow, shrinkable) / shrinkable;
            total = 0;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (GetShrinks(child) && _heights[i] > 0)
                {
                    _heights[i] -= _heights[i] * share;
                    child.Measure(new Size(availableSize.Width, _heights[i]));
                }

                total += _heights[i];
            }
        }

        return new Size(width, total);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = 0.0;
        var children = Children;
        for (var i = 0; i < children.Count && i < _heights.Length; i++)
        {
            children[i].Arrange(new Rect(0, y, finalSize.Width, _heights[i]));
            y += _heights[i];
        }

        return finalSize;
    }
}
