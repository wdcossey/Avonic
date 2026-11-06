using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;

namespace Avonic.Components.Controls;

/// <summary>
/// A <c>&lt;slot&gt;</c> in a <see cref="FlexRow"/>, styled as a <c>ContentPresenter</c>. A slot has no box, so its child is
/// the row's item: this fills the row, and <c>VerticalContentAlignment</c> is <c>align-items</c>. A child that sets its own
/// <c>VerticalAlignment</c> has <c>align-self</c>, which a <c>ContentPresenter</c> would ignore. A panel stands for the slot,
/// so it fills it too, except a vertical stack, which is a group of its own, as a column in a <c>&lt;div&gt;</c> is. A control
/// that's aligned some way by default uses <c>OverrideDefaultValue</c> rather than its theme, as a theme's value counts as set.
/// </summary>
public sealed class FlexSlot : ContentPresenter
{
    private Control? _watched;

    protected override Type StyleKeyOverride => typeof(ContentPresenter);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ChildProperty)
        {
            if (_watched is not null)
            {
                _watched.PropertyChanged -= OnChildPropertyChanged;
            }

            _watched = Child;
            if (_watched is not null)
            {
                _watched.PropertyChanged += OnChildPropertyChanged;
            }
        }
    }

    /// <summary>The child's own arrange can't move it out of the rectangle it's been given, which this decides.</summary>
    private void OnChildPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == VerticalAlignmentProperty)
        {
            InvalidateArrange();
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is not { } child)
        {
            return finalSize;
        }

        var inner = new Rect(finalSize).Deflate(Padding + BorderThickness);
        var height = inner.Height;
        var y = inner.Y;
        if (!AlignsItself(child) && VerticalContentAlignment != VerticalAlignment.Stretch)
        {
            height = Math.Min(child.DesiredSize.Height, inner.Height);
            y += VerticalContentAlignment switch
            {
                VerticalAlignment.Center => (inner.Height - height) / 2,
                VerticalAlignment.Bottom => inner.Height - height,
                _ => 0,
            };
        }

        // A slot squeezed below what its content was measured at. overflow: hidden clips it, where a TextBlock arranged narrower
        // would wrap again, and no longer fit the height it asked for.
        child.Arrange(new Rect(inner.X, y, Math.Max(inner.Width, child.DesiredSize.Width), height));
        return finalSize;
    }

    private static bool AlignsItself(Control child) =>
        child.IsSet(VerticalAlignmentProperty) || child is Panel and not StackPanel { Orientation: Orientation.Vertical };
}
