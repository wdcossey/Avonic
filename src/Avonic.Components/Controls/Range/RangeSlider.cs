using Avalonia;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>.range-slider</c> in <c>ion-range</c>'s shadow root: <c>position: relative</c>, with its parts placed by percentages. A child
/// with <see cref="StartProperty"/> set has its start edge, margin included, at that fraction of the width, as <c>left: n%</c> does;
/// one with <see cref="EndProperty"/> too reaches to that fraction from the end, as <c>right: n%</c> does. Any other child fills the
/// width. Each is as tall as the slider, and aligned in it by its own <see cref="Layoutable.VerticalAlignment"/>. It asks for no width
/// of its own, as <c>contain: size</c> doesn't, and Avalonia mirrors it in RTL, as it does any panel.
/// </summary>
public sealed class RangeSlider : Panel
{
    /// <summary>The fraction of the width that a child's start edge is at: <c>left</c>, or <c>right</c> in RTL.</summary>
    public static readonly AttachedProperty<double> StartProperty =
        AvaloniaProperty.RegisterAttached<RangeSlider, Control, double>("Start", double.NaN);

    /// <summary>The fraction of the width that a child's end edge is from the end: <c>right</c>, or <c>left</c> in RTL.</summary>
    public static readonly AttachedProperty<double> EndProperty =
        AvaloniaProperty.RegisterAttached<RangeSlider, Control, double>("End", double.NaN);

    static RangeSlider()
    {
        AffectsParentArrange<RangeSlider>(StartProperty, EndProperty);
    }

    public static double GetStart(Control control) => control.GetValue(StartProperty);

    public static void SetStart(Control control, double value) => control.SetValue(StartProperty, value);

    public static double GetEnd(Control control) => control.GetValue(EndProperty);

    public static void SetEnd(Control control, double value) => control.SetValue(EndProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }

        // contain: size: the parts are absolutely placed, and the slider's own height is var(--height), which the theme sets.
        return new Size(0, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = finalSize.Width;
        foreach (var child in Children)
        {
            var start = GetStart(child);
            var end = GetEnd(child);
            if (double.IsNaN(start))
            {
                child.Arrange(new Rect(0, 0, width, finalSize.Height));
            }
            else if (double.IsNaN(end))
            {
                child.Arrange(new Rect(start * width, 0, child.DesiredSize.Width, finalSize.Height));
            }
            else
            {
                var left = start * width;
                child.Arrange(new Rect(left, 0, Math.Max(0, width - end * width - left), finalSize.Height));
            }
        }

        return finalSize;
    }
}
