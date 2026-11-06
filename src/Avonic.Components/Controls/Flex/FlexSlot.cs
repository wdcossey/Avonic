using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;

namespace Avonic.Components.Controls;

/// <summary>
/// A <c>&lt;slot&gt;</c> in a <see cref="FlexRow"/>, styled as a <c>ContentPresenter</c>. A slot has no box, so its child is
/// the row's item: this fills the row, and <c>VerticalContentAlignment</c> is <c>align-items</c>. A child that sets its own
/// <c>VerticalAlignment</c> has <c>align-self</c>, which a <c>ContentPresenter</c> would ignore. A panel stands for the slot,
/// so it fills it too, except a vertical stack, which is a group of its own, as a column in a <c>&lt;div&gt;</c> is.
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
        if (Child is { } child && AlignsItself(child))
        {
            child.Arrange(new Rect(finalSize).Deflate(Padding + BorderThickness));
            return finalSize;
        }

        return base.ArrangeOverride(finalSize);
    }

    private static bool AlignsItself(Control child) =>
        child.IsSet(VerticalAlignmentProperty) || child is Panel and not StackPanel { Orientation: Orientation.Vertical };
}
