using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
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
/// wrapped child as no child at all. One made from <see cref="ItemsControl.ItemTemplate"/> is the slot's logical child, so
/// the rules also have <c>c|AvonicChip &gt; ContentPresenter &gt; c|X</c>.
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

        return base.MeasureOverride(availableSize).WithWidth(availableSize.Width);
    }

    /// <summary>
    /// <c>:nth-child</c> for a slotted control, which is the chip's logical child but not its container: the item's index.
    /// <see cref="ItemsControl"/> gives only a container's, so a child held in a <see cref="FlexSlot"/> would match nothing. A
    /// slot is counted by its item too, and not by <see cref="ItemsControl.IndexFromContainer"/>, which is still the old index
    /// when <see cref="ItemsControl.Items"/> raises its change, as the panel moves its containers afterwards. Two equal values
    /// in <see cref="ItemsControl.ItemsSource"/> both count as the first.
    /// </summary>
    int IChildIndexProvider.GetChildIndex(ILogical child) => child switch
    {
        _ when ItemsView.IndexOf(child) is >= 0 and var index => index,
        ContentPresenter { Content: { } item } => ItemsView.IndexOf(item),
        _ => -1,
    };

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

    protected override AutomationPeer OnCreateAutomationPeer() => new ChipAutomationPeer(this);

    /// <summary>
    /// A plain element to Chromium, with no role, where an <see cref="ItemsControl"/>'s own peer would call it a list. Its
    /// children speak for themselves, and <c>aria-disabled</c> is the peer's <c>IsEnabled</c>.
    /// </summary>
    private sealed class ChipAutomationPeer(AvonicChip owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override bool IsContentElementCore() => false;

        protected override bool IsControlElementCore() => true;
    }

}
