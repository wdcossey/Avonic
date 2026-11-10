using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-tab-button</c>: one button of an <see cref="AvonicTabBar"/>. Its click asks the <see cref="AvonicTabs"/> around it to
/// select its <see cref="Tab"/>, and the bar marks it <see cref="Selected"/>. Its children, an <see cref="AvonicIcon"/>, an
/// <see cref="AvonicLabel"/> and an <see cref="AvonicBadge"/>, are <see cref="ItemsControl.Items"/>, as <c>core.json</c> lists no
/// slot, laid out by <see cref="Layout"/>. See <c>docs/components/ion-tabs/README.md</c>.
/// </summary>
[TemplatePart("PART_Native", typeof(Border))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(
    ":selected", ":has-label", ":has-icon", ":has-label-only", ":has-icon-only", ":layout-icon-top", ":layout-icon-bottom",
    ":layout-icon-start", ":layout-icon-end", ":layout-icon-hide", ":layout-label-hide", ":in-tab-bar-color",
    ":in-tab-bar-translucent")]
public class AvonicTabButton : ItemsControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>tab</c>: the <see cref="AvonicTab.Tab"/> it selects.</summary>
    public static readonly StyledProperty<string?> TabProperty =
        AvaloniaProperty.Register<AvonicTabButton, string?>(nameof(Tab));

    /// <summary>
    /// Ionic <c>layout</c>: where the icon goes, or what's hidden. <see cref="TabButtonLayout.IconTop"/> by default, Ionic's fallback
    /// for its <c>tabButtonLayout</c> config.
    /// </summary>
    public static readonly StyledProperty<TabButtonLayout> LayoutProperty =
        AvaloniaProperty.Register<AvonicTabButton, TabButtonLayout>(nameof(Layout));

    /// <summary>Ionic <c>selected</c>: its bar's <see cref="AvonicTabBar.SelectedTab"/> is its <see cref="Tab"/>. The bar writes it.</summary>
    public static readonly StyledProperty<bool> SelectedProperty =
        AvaloniaProperty.Register<AvonicTabButton, bool>(nameof(Selected));

    /// <summary>
    /// Ionic's internal <c>ionTabButtonClick</c>, which bubbles to the <see cref="AvonicTabs"/>. Internal, as it is in Ionic: an app
    /// that wants to know about a click handles the button's pointer events or the tabs' events.
    /// </summary>
    internal static readonly RoutedEvent<TabButtonClickEventArgs> IonTabButtonClickEvent =
        RoutedEvent.Register<AvonicTabButton, TabButtonClickEventArgs>("IonTabButtonClick", RoutingStrategies.Bubble);

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private AvonicTabBar? _bar;

    static AvonicTabButton()
    {
        // An <a> with no href, which a browser doesn't focus: Tab never reached one, measured. An app can make it focusable.
        FocusableProperty.OverrideDefaultValue<AvonicTabButton>(false);

        LayoutProperty.Changed.AddClassHandler<AvonicTabButton>((button, _) => button.UpdateChildClasses());
        SelectedProperty.Changed.AddClassHandler<AvonicTabButton>((button, e) => button.OnSelectedChanged(e));
    }

    public AvonicTabButton()
    {
        // ion-activatable and ion-focusable, always.
        Activatable.SetIsEnabled(this, true);
        FocusVisible.SetIsEnabled(this, true);

        // The host's onClick, which the anchor's click reaches. No keys: the host's onKeyUp takes Enter and Space.
        _ = new NativeButton(this, _ => SelectTab(), () => false, () => { });
        AddHandler(KeyUpEvent, OnKeyUp);

        Items.CollectionChanged += (_, _) => UpdateChildClasses();
        UpdateChildClasses();
        UpdateClasses();
    }

    public string? Tab
    {
        get => GetValue(TabProperty);
        set => SetValue(TabProperty, value);
    }

    public TabButtonLayout Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public bool Selected
    {
        get => GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    /// <summary>A control is its own container, so that its logical parent is the button, as a slotted child's host is.</summary>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    // ── The click ────────────────────────────────────────────────────────────────────────────

    /// <summary><c>onKeyUp</c>: Enter or Space released on it, which it hears only once something has focused it.</summary>
    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            SelectTab();
        }
    }

    /// <summary>
    /// <c>selectTab</c>: with a tab, and enabled, the internal click, which the tabs select its tab from. A disabled one takes no
    /// input at all here, as <c>pointer-events: none</c> gives it none.
    /// </summary>
    private void SelectTab()
    {
        if (Tab is { } tab && IsEffectivelyEnabled)
        {
            RaiseEvent(new TabButtonClickEventArgs(IonTabButtonClickEvent, tab, Selected));
        }
    }

    // ── Classes ──────────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        // ::slotted(ion-tab-button) in the bar's CSS: only its own children.
        _bar = Parent as AvonicTabBar;
        if (_bar is not null)
        {
            _bar.StyleChanged += UpdateClasses;
        }

        UpdateChildClasses();
        UpdateClasses();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (_bar is not null)
        {
            _bar.StyleChanged -= UpdateClasses;
            _bar = null;
        }

        base.OnDetachedFromLogicalTree(e);
        UpdateClasses();
    }

    /// <summary>The classes Ionic's <c>render</c> works out from the selection, and the bar's, which its <c>::slotted</c> rules read.</summary>
    private void UpdateClasses()
    {
        PseudoClasses.Set(":selected", Selected);
        PseudoClasses.Set(":in-tab-bar-color", !string.IsNullOrEmpty(_bar?.Color));
        PseudoClasses.Set(":in-tab-bar-translucent", _bar?.Translucent == true);
    }

    /// <summary>The classes Ionic's <c>render</c> works out from its children and its layout.</summary>
    private void UpdateChildClasses()
    {
        // hasLabel, hasIcon: querySelector, anywhere inside.
        var descendants = this.GetLogicalDescendants().ToList();
        var hasLabel = descendants.OfType<AvonicLabel>().Any();
        var hasIcon = descendants.OfType<AvonicIcon>().Any();
        PseudoClasses.Set(":has-label", hasLabel);
        PseudoClasses.Set(":has-icon", hasIcon);
        PseudoClasses.Set(":has-label-only", hasLabel && !hasIcon);
        PseudoClasses.Set(":has-icon-only", hasIcon && !hasLabel);

        PseudoClasses.Set(":layout-icon-top", Layout == TabButtonLayout.IconTop);
        PseudoClasses.Set(":layout-icon-bottom", Layout == TabButtonLayout.IconBottom);
        PseudoClasses.Set(":layout-icon-start", Layout == TabButtonLayout.IconStart);
        PseudoClasses.Set(":layout-icon-end", Layout == TabButtonLayout.IconEnd);
        PseudoClasses.Set(":layout-icon-hide", Layout == TabButtonLayout.IconHide);
        PseudoClasses.Set(":layout-label-hide", Layout == TabButtonLayout.LabelHide);
    }

    private void OnSelectedChanged(AvaloniaPropertyChangedEventArgs e)
    {
        UpdateClasses();

        // aria-selected changes, which assistive technology hears.
        if (ControlAutomationPeer.FromElement(this) is TabButtonPeer peer)
        {
            peer.RaiseSelectedChanged((bool)e.OldValue!, (bool)e.NewValue!);
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new TabButtonPeer(this);

    /// <summary><c>role="tab"</c> and <c>aria-selected</c>, named by its text.</summary>
    private sealed class TabButtonPeer(AvonicTabButton owner) : ControlAutomationPeer(owner), ISelectionItemProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

        protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : AccessibleText.Of(owner);

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        public bool IsSelected => owner.Selected;

        public ISelectionProvider? SelectionContainer => null;

        public void Select() => owner.SelectTab();

        public void AddToSelection() => Select();

        public void RemoveFromSelection()
        {
        }

        public void RaiseSelectedChanged(bool oldValue, bool newValue) =>
            RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, oldValue, newValue);
    }
}
