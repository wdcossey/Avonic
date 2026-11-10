using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-breadcrumbs</c>: a wrapping row of <see cref="AvonicBreadcrumb"/>s. It decides which is last, which is active, which
/// have a separator, and which collapse behind the indicator. The look is in the theme. See
/// <c>docs/components/ion-breadcrumbs/README.md</c>.
/// </summary>
/// <remarks>
/// Its children have no slot in <c>core.json</c>, so they're <see cref="ItemsControl.Items"/>. Breadcrumbs anywhere inside count,
/// in order, as <c>querySelectorAll</c> finds them.
/// </remarks>
[PseudoClasses(":in-toolbar")]
public class AvonicBreadcrumbs : ItemsControl
{
    /// <summary>Ionic <c>color</c>. The breadcrumbs inside take its base.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicBreadcrumbs>();

    /// <summary>Ionic <c>maxItems</c>: the most breadcrumbs to show before collapsing. Null never collapses.</summary>
    public static readonly StyledProperty<int?> MaxItemsProperty =
        AvaloniaProperty.Register<AvonicBreadcrumbs, int?>(nameof(MaxItems));

    /// <summary>
    /// Ionic <c>itemsBeforeCollapse</c>: how many show before the indicator. If it and <see cref="ItemsAfterCollapse"/> come to more
    /// than <see cref="MaxItems"/>, nothing collapses.
    /// </summary>
    public static readonly StyledProperty<int> ItemsBeforeCollapseProperty =
        AvaloniaProperty.Register<AvonicBreadcrumbs, int>(nameof(ItemsBeforeCollapse), 1);

    /// <summary>Ionic <c>itemsAfterCollapse</c>: how many show after the indicator.</summary>
    public static readonly StyledProperty<int> ItemsAfterCollapseProperty =
        AvaloniaProperty.Register<AvonicBreadcrumbs, int>(nameof(ItemsAfterCollapse), 1);

    /// <summary>
    /// Ionic <c>ionCollapsedClick</c>: the collapsed indicator was clicked. It carries the hidden breadcrumbs, usually for a popover
    /// listing them, and the indicator, to place one by.
    /// </summary>
    public static readonly RoutedEvent<BreadcrumbCollapsedClickEventArgs> IonCollapsedClickEvent =
        RoutedEvent.Register<AvonicBreadcrumbs, BreadcrumbCollapsedClickEventArgs>(nameof(IonCollapsedClick), RoutingStrategies.Bubble);

    /// <summary>Ionic's <c>activeChanged</c>: this made a breadcrumb active. Never reset, as in Ionic.</summary>
    private bool _activeChanged;

    /// <summary>The breadcrumbs the last look found, so that one attached or detached with nothing changed is no <c>slotchange</c>.</summary>
    private List<AvonicBreadcrumb> _breadcrumbs = [];

    /// <summary>Loaded: <c>slotchange</c> and the watchers only come once the component has loaded, which here is first attached.</summary>
    private bool _loaded;

    private bool _slotChangePending;
    private bool _itemsChanged;

    static AvonicBreadcrumbs()
    {
        // overflow: visible, so that a focused breadcrumb's shadow isn't cut off at the row's edge. A TemplatedControl clips by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicBreadcrumbs>(false);

        // @Listen('collapsedClick'), from a breadcrumb anywhere inside.
        AvonicBreadcrumb.CollapsedClickEvent.AddClassHandler<AvonicBreadcrumbs>((breadcrumbs, e) => breadcrumbs.OnCollapsedClick(e));
    }

    public AvonicBreadcrumbs()
    {
        // slotchange.
        Items.CollectionChanged += (_, _) => SlotChanged(itemsChanged: true);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public int? MaxItems
    {
        get => GetValue(MaxItemsProperty);
        set => SetValue(MaxItemsProperty, value);
    }

    public int ItemsBeforeCollapse
    {
        get => GetValue(ItemsBeforeCollapseProperty);
        set => SetValue(ItemsBeforeCollapseProperty, value);
    }

    public int ItemsAfterCollapse
    {
        get => GetValue(ItemsAfterCollapseProperty);
        set => SetValue(ItemsAfterCollapseProperty, value);
    }

    public event EventHandler<BreadcrumbCollapsedClickEventArgs>? IonCollapsedClick
    {
        add => AddHandler(IonCollapsedClickEvent, value);
        remove => RemoveHandler(IonCollapsedClickEvent, value);
    }

    /// <summary>
    /// <c>slotchange</c>: the children changed, or a breadcrumb inside was attached or detached, as one made from an
    /// <see cref="ItemsControl.ItemTemplate"/> is after its item. A browser fires it once, after the task that made the changes, so
    /// several changes at once are looked at together, which matters to the active one: see <c>B3</c> in the tests.
    /// </summary>
    internal void SlotChanged(bool itemsChanged = false)
    {
        _itemsChanged |= itemsChanged;
        if (_loaded && !_slotChangePending)
        {
            _slotChangePending = true;
            Dispatcher.UIThread.Post(OnSlotChange);
        }
    }

    /// <summary><c>slotChanged</c>. A breadcrumb attached or detached with nothing changed, as when the panel is made, is no slotchange.</summary>
    private void OnSlotChange()
    {
        _slotChangePending = false;
        var itemsChanged = _itemsChanged;
        _itemsChanged = false;
        var breadcrumbs = GetBreadcrumbs();
        if (itemsChanged || !breadcrumbs.SequenceEqual(_breadcrumbs))
        {
            ResetActiveBreadcrumb(breadcrumbs);
            BreadcrumbsInit(breadcrumbs);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // @Watch('maxItems'), @Watch('itemsBeforeCollapse'), @Watch('itemsAfterCollapse').
        if (_loaded && (change.Property == MaxItemsProperty || change.Property == ItemsBeforeCollapseProperty
                || change.Property == ItemsAfterCollapseProperty))
        {
            var breadcrumbs = GetBreadcrumbs();
            ResetActiveBreadcrumb(breadcrumbs);
            BreadcrumbsInit(breadcrumbs);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateClasses();

        // componentWillLoad, once. Then slotchange and the watchers come whether or not it's in the document, as in a browser.
        if (!_loaded)
        {
            _loaded = true;
            BreadcrumbsInit(GetBreadcrumbs());
        }
    }


    /// <summary>hostContext('ion-toolbar'): any ancestor.</summary>
    private void UpdateClasses()
    {
        // :host(.in-toolbar-color) only sets the toolbar's own contrast again, which the row inherits anyway, and each breadcrumb
        // sets its own colour: so it isn't a pseudoclass here.
        PseudoClasses.Set(":in-toolbar", this.GetVisualAncestors().OfType<AvonicToolbar>().Any());
    }

    private void BreadcrumbsInit(List<AvonicBreadcrumb> breadcrumbs)
    {
        _breadcrumbs = breadcrumbs;
        SetBreadcrumbSeparator(breadcrumbs);
        SetMaxItems(breadcrumbs);
    }

    /// <summary>Only undoes an active breadcrumb if this made one active; otherwise the one set on the breadcrumb stays.</summary>
    private void ResetActiveBreadcrumb(List<AvonicBreadcrumb> breadcrumbs)
    {
        if (_activeChanged && breadcrumbs.FirstOrDefault(breadcrumb => breadcrumb.Active) is { } active)
        {
            active.SetCurrentValue(AvonicBreadcrumb.ActiveProperty, false);
        }
    }

    private void SetMaxItems(List<AvonicBreadcrumb> breadcrumbs)
    {
        var (maxItems, before, after) = (MaxItems, ItemsBeforeCollapse, ItemsAfterCollapse);
        var shouldCollapse = maxItems is { } max && breadcrumbs.Count > max && before + after <= max;

        for (var index = 0; index < breadcrumbs.Count; index++)
        {
            var breadcrumb = breadcrumbs[index];
            breadcrumb.ShowCollapsedIndicator = shouldCollapse && index == before;
            breadcrumb.Collapsed = shouldCollapse && index >= before && index < breadcrumbs.Count - after;
        }
    }

    private void SetBreadcrumbSeparator(List<AvonicBreadcrumb> breadcrumbs)
    {
        var (maxItems, before, after) = (MaxItems, ItemsBeforeCollapse, ItemsAfterCollapse);
        var active = breadcrumbs.Any(breadcrumb => breadcrumb.Active);

        for (var index = 0; index < breadcrumbs.Count; index++)
        {
            var breadcrumb = breadcrumbs[index];

            // The last changes only when itemsAfterCollapse is 0: then it's the collapsed indicator's breadcrumb. An index past the
            // end is undefined in Ionic, and no breadcrumb is last.
            var last = maxItems is not null && after == 0 ? index == before : index == breadcrumbs.Count - 1;
            breadcrumb.Last = last;

            // A separator set on the breadcrumb stays. Otherwise every one but the last gets one, assigned as Ionic assigns it.
            if (breadcrumb.Separator is null && !last)
            {
                breadcrumb.SetCurrentValue(AvonicBreadcrumb.SeparatorProperty, true);
            }

            if (!active && last)
            {
                breadcrumb.SetCurrentValue(AvonicBreadcrumb.ActiveProperty, true);
                _activeChanged = true;
            }
        }
    }

    /// <summary>Every breadcrumb inside, in order, as <c>querySelectorAll('ion-breadcrumb')</c> finds them.</summary>
    private List<AvonicBreadcrumb> GetBreadcrumbs()
    {
        var found = new List<AvonicBreadcrumb>();
        for (var index = 0; index < ItemsView.Count; index++)
        {
            if ((ContainerFromIndex(index) ?? ItemsView[index] as Control) is { } root)
            {
                if (root is AvonicBreadcrumb breadcrumb)
                {
                    found.Add(breadcrumb);
                }

                found.AddRange(root.GetLogicalDescendants().OfType<AvonicBreadcrumb>());
            }
        }

        return found;
    }

    /// <summary><c>onCollapsedClick</c>: the collapsed breadcrumbs, and what the breadcrumb's event carried.</summary>
    private void OnCollapsedClick(RoutedEventArgs e)
    {
        if (e.Source is not AvonicBreadcrumb source)
        {
            return;
        }

        var collapsed = GetBreadcrumbs().Where(breadcrumb => breadcrumb.Collapsed).ToList();
        RaiseEvent(new BreadcrumbCollapsedClickEventArgs(IonCollapsedClickEvent, collapsed, source.CollapsedIndicator));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new BreadcrumbsAutomationPeer(this);

    /// <summary>No role in Ionic, where an <see cref="ItemsControl"/>'s own peer would call it a list.</summary>
    private sealed class BreadcrumbsAutomationPeer(AvonicBreadcrumbs owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override bool IsContentElementCore() => false;

        protected override bool IsControlElementCore() => true;
    }
}
