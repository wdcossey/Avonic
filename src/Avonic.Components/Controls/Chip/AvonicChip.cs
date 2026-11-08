using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-chip</c>: a small activatable box holding a row of children, such as an icon and a label. The look is in
/// the theme. See <c>docs/components/ion-chip/README.md</c>.
/// </summary>
/// <remarks>
/// Its default slot holds several children and <c>core.json</c> lists no slot, so they're <see cref="ItemsControl.Items"/>.
/// Each is held in a <see cref="FlexSlot"/>, as a <c>&lt;slot&gt;</c>'s child is in an item, for <c>align-items</c>. A control
/// is still the chip's logical child, so the theme's <c>::slotted</c> rules are <c>c|AvonicChip &gt; c|X</c>, and the chip
/// counts its children for <c>:nth-child</c> by item, not by container, as an <see cref="ItemsControl"/> otherwise counts a
/// wrapped child as no child at all.
/// </remarks>
[TemplatePart("PART_Native", typeof(Border))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(":outline")]
public class AvonicChip : ItemsControl, IChildIndexProvider
{
    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicChip>();

    /// <summary>Ionic <c>outline</c>: a border and no background.</summary>
    public static readonly StyledProperty<bool> OutlineProperty =
        AvaloniaProperty.Register<AvonicChip, bool>(nameof(Outline));

    static AvonicChip()
    {
        // vertical-align: middle. A default, not a theme's value, as FlexSlot would take that for align-self.
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicChip>(VerticalAlignment.Center);

        OutlineProperty.Changed.AddClassHandler<AvonicChip>((chip, _) => chip.PseudoClasses.Set(":outline", chip.Outline));
    }

    private EventHandler<ChildIndexChangedEventArgs>? _childIndexChanged;

    public AvonicChip()
    {
        // ion-activatable, always. Not ion-focusable: the chip takes focus only if an app makes it focusable, as tabindex does.
        Activatable.SetIsEnabled(this, true);

        // :first-child and :last-child follow the children as they come and go.
        Items.CollectionChanged += (_, _) => _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.ChildIndexesReset);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool Outline
    {
        get => GetValue(OutlineProperty);
        set => SetValue(OutlineProperty, value);
    }

    /// <summary>Every child is slotted, a control or not, so that the theme's rules reach each the same way.</summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<FlexSlot>(item, out recycleKey);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new FlexSlot();

    /// <summary><c>align-items: center</c>: the slot's content alignment, at style priority, so that an app's value wins.</summary>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        container.SetValue(ContentPresenter.VerticalContentAlignmentProperty, VerticalAlignment.Center, BindingPriority.Style);
    }

    /// <summary>
    /// <c>display: inline-flex</c> is as wide as its content on one line, or as the space there is when that's too narrow, as
    /// shrink-to-fit is. Measured at the space, wrapped text reports its longest line, which would leave a chip narrower than
    /// Ionic's, so it's measured on one line first.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (double.IsInfinity(availableSize.Width))
        {
            return base.MeasureOverride(availableSize);
        }

        var oneLine = base.MeasureOverride(availableSize.WithWidth(double.PositiveInfinity));
        if (oneLine.Width <= availableSize.Width)
        {
            return oneLine;
        }

        // Content that can't shrink that far still overflows, as min-content does.
        var wrapped = base.MeasureOverride(availableSize);
        return wrapped.WithWidth(Math.Max(wrapped.Width, availableSize.Width));
    }

    /// <summary>
    /// <c>:nth-child</c> for a slotted control, which is the chip's logical child but not its container: the item's index.
    /// <see cref="ItemsControl"/> gives only a container's, so a child held in a <see cref="FlexSlot"/> would match nothing.
    /// </summary>
    int IChildIndexProvider.GetChildIndex(ILogical child) => child is Control control
        ? ItemsView.IndexOf(control) is >= 0 and var index ? index : IndexFromContainer(control)
        : -1;

    bool IChildIndexProvider.TryGetTotalCount(out int count)
    {
        count = ItemsView.Count;
        return true;
    }

    event EventHandler<ChildIndexChangedEventArgs>? IChildIndexProvider.ChildIndexChanged
    {
        add => _childIndexChanged += value;
        remove => _childIndexChanged -= value;
    }

    /// <summary>An <see cref="ItemsControl"/> moves focus with the arrow keys. A chip is a plain element, and doesn't.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
    }
}
