using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-item</c>: a row with start, default and end content, and optional detail icon, that
/// can be tappable. It's also what the form controls inside it report to.
/// </summary>
[TemplatePart("PART_Start", typeof(ContentPresenter))]
[TemplatePart("PART_Container", typeof(ContentPresenter))]
[TemplatePart("PART_End", typeof(ContentPresenter))]
[TemplatePart("PART_DetailIcon", typeof(AvonicIcon))]
[TemplatePart("PART_Ripple", typeof(AvonicRippleEffect))]
[PseudoClasses(
    ":lines-default", ":lines-full", ":lines-inset", ":lines-none", ":button", ":ion-activatable",
    ":item-multiple-inputs", ":item-control-needs-pointer-cursor", ":item-focus-indicator-room")]
public class AvonicItem : TemplatedControl, IItemHost
{
    /// <summary><c>margin-inline-end: -6px</c> on <c>.item-detail-icon</c>. There's no variable for it.</summary>
    private const double DetailIconMarginEnd = -6;

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>button</c>: the item is tappable.</summary>
    public static readonly StyledProperty<bool> ButtonProperty =
        AvaloniaProperty.Register<AvonicItem, bool>(nameof(Button));

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicItem>();

    /// <summary>Ionic <c>detail</c>. In md, only <c>true</c> shows the icon.</summary>
    public static readonly StyledProperty<bool?> DetailProperty =
        AvaloniaProperty.Register<AvonicItem, bool?>(nameof(Detail));

    /// <summary>Ionic <c>detailIcon</c>: a name or a source, as <see cref="AvonicIcon.Icon"/> takes.</summary>
    public static readonly StyledProperty<string> DetailIconProperty =
        AvaloniaProperty.Register<AvonicItem, string>(nameof(DetailIcon), "chevron-forward");

    /// <summary>Ionic <c>lines</c>. Null leaves it to the item's default, or an <c>ion-list</c>'s.</summary>
    public static readonly StyledProperty<ItemLines?> LinesProperty =
        AvaloniaProperty.Register<AvonicItem, ItemLines?>(nameof(Lines));

    /// <summary>Ionic's default slot.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<AvonicItem, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItem, IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>Ionic's <c>start</c> slot.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicItem, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItem, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>end</c> slot.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicItem, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicItem, IDataTemplate?>(nameof(EndContentTemplate));

    public bool Button
    {
        get => GetValue(ButtonProperty);
        set => SetValue(ButtonProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool? Detail
    {
        get => GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public string DetailIcon
    {
        get => GetValue(DetailIconProperty);
        set => SetValue(DetailIconProperty, value);
    }

    public ItemLines? Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public object? StartContent
    {
        get => GetValue(StartContentProperty);
        set => SetValue(StartContentProperty, value);
    }

    public IDataTemplate? StartContentTemplate
    {
        get => GetValue(StartContentTemplateProperty);
        set => SetValue(StartContentTemplateProperty, value);
    }

    public object? EndContent
    {
        get => GetValue(EndContentProperty);
        set => SetValue(EndContentProperty, value);
    }

    public IDataTemplate? EndContentTemplate
    {
        get => GetValue(EndContentTemplateProperty);
        set => SetValue(EndContentTemplateProperty, value);
    }

    // ── Extensions ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Avonic extension: a <see cref="Button"/> item was tapped or activated by keyboard. Ionic gets
    /// this from its native <c>&lt;button&gt;</c>.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AvonicItem, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>Avonic extension: runs when a <see cref="Button"/> item clicks. It does nothing on a plain item, which has no native button.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvonicButton.CommandProperty.AddOwner<AvonicItem>();

    /// <summary>Avonic extension: the parameter for <see cref="Command"/>.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvonicButton.CommandParameterProperty.AddOwner<AvonicItem>();

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>
    /// For the theme: <c>margin-inline-start: calc(var(--inner-padding-end) / 2)</c> and
    /// <c>margin-inline-end: -6px</c> on the detail icon.
    /// </summary>
    public static readonly IMultiValueConverter DetailIconMarginConverter =
        new FuncMultiValueConverter<object?, Thickness>(values =>
            new Thickness(
                values.FirstOrDefault() is double innerPaddingEnd ? innerPaddingEnd / 2 : 0,
                0,
                DetailIconMarginEnd,
                0));

    /// <summary>
    /// For the theme: <c>overflow: hidden</c> on <c>.item-inner</c>, which clips at its padding box, as a clip for the row inside
    /// it. The values are the row's <c>Bounds</c>, then the inner part's <c>Padding</c>.
    /// </summary>
    public static readonly IMultiValueConverter PaddingBoxClipConverter =
        new FuncMultiValueConverter<object?, Geometry?>(values =>
        {
            if (values.ElementAtOrDefault(0) is not Rect bounds)
            {
                return null;
            }

            var padding = values.ElementAtOrDefault(1) is Thickness thickness ? thickness : default;
            return new RectangleGeometry(new Rect(
                -padding.Left,
                -padding.Top,
                bounds.Width + padding.Left + padding.Right,
                bounds.Height + padding.Top + padding.Bottom));
        });

    /// <summary><c>item-multiple-inputs</c>, which the controls inside read.</summary>
    internal const string MultipleInputsPseudoClass = ":item-multiple-inputs";

    /// <summary><c>ion-activatable</c>, which a toggle inside reads.</summary>
    internal const string ActivatablePseudoClass = ":ion-activatable";

    // ── State ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>getFirstInteractive</c>'s selector.</summary>
    private static readonly HashSet<ItemControlType> FirstInteractiveTypes =
    [
        ItemControlType.Toggle,
        ItemControlType.Checkbox,
        ItemControlType.Radio,
        ItemControlType.Select,
        ItemControlType.Input,
        ItemControlType.Textarea,
    ];

    private NativeButton? _native;
    private readonly IonStyleClasses _itemStyles = new(":item-");
    private ContentPresenter?[] _slots = [];
    private AvonicIcon? _detailIcon;
    private AvonicRippleEffect? _ripple;
    private bool _multipleInputs;

    /// <summary>
    /// <c>getFirstInteractive</c>'s control as the item last rendered: <c>render()</c> keeps it for the click, so one enabled
    /// since isn't forwarded to, and one disabled since ignores it.
    /// </summary>
    private IItemControl? _firstInteractive;
    private bool _forwarded;
    private readonly StyleValue<bool> _focusable = new();
    private readonly StyleValue<bool> _noInput = new();
    private readonly StyleValue<bool> _activatable = new();
    private readonly StyleValue<bool> _focusVisible = new();

    public AvonicItem()
    {
        // stopImmediatePropagation, for what's left of a tap that was forwarded. Tapped comes after the release, if it comes at all,
        // so the next press ends it.
        AddHandler(TappedEvent, (_, e) => e.Handled = _forwarded);
        AddHandler(PointerPressedEvent, (_, _) => _forwarded = false, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(IonStyle.IonStyleEvent, OnIonStyle);

        // Behaviour, so it doesn't depend on the theme, which an app can replace. At style priority, as CSS is, so an app's own
        // value wins, bound once so that it still wins when the state turns on after it.
        Bind(FocusableProperty, _focusable, BindingPriority.Style);
        Bind(IsHitTestVisibleProperty, _noInput, BindingPriority.Style);
        Bind(Activatable.IsEnabledProperty, _activatable, BindingPriority.Style);
        Bind(FocusVisible.IsEnabledProperty, _focusVisible, BindingPriority.Style);
        UpdateLines();
        UpdateInteractivity();
    }

    /// <summary>A command only disables a button item. The base class's constructor reads this before there's a <see cref="NativeButton"/>.</summary>
    protected override bool IsEnabledCore => base.IsEnabledCore && (!Button || _native?.CanExecute != false);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LinesProperty)
        {
            UpdateLines();
        }
        else if (change.Property == ButtonProperty)
        {
            _native?.Reset();
            UpdateIsEffectivelyEnabled();
            UpdateInteractivity();
        }
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            // Later, because this runs before the controls inside find out.
            Dispatcher.UIThread.Post(UpdateInteractivity, DispatcherPriority.Loaded);
        }
        else if (change.Property == DetailProperty)
        {
            UpdateDetail();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        foreach (var slot in _slots)
        {
            if (slot is not null)
            {
                slot.PropertyChanged -= OnSlotPropertyChanged;
            }
        }

        _slots =
        [
            e.NameScope.Find<ContentPresenter>("PART_Start"),
            e.NameScope.Find<ContentPresenter>("PART_Container"),
            e.NameScope.Find<ContentPresenter>("PART_End"),
        ];

        foreach (var slot in _slots)
        {
            if (slot is not null)
            {
                slot.PropertyChanged += OnSlotPropertyChanged;
            }
        }

        _detailIcon = e.NameScope.Find<AvonicIcon>("PART_DetailIcon");
        _ripple = e.NameScope.Find<AvonicRippleEffect>("PART_Ripple");

        UpdateDetail();
        UpdateInteractivity();
    }

    /// <summary>
    /// <c>updateInteractivityOnSlotChange</c>: a presenter's child changing is <c>slotchange</c>. The child
    /// isn't attached yet, so whether it's disabled through an ancestor isn't known until later, which is
    /// why Ionic also does this in a <c>raf</c> on load.
    /// </summary>
    private void OnSlotPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ChildProperty)
        {
            Dispatcher.UIThread.Post(UpdateInteractivity, DispatcherPriority.Loaded);
        }
    }

    private bool _indicatorChangePosted;

    /// <summary>
    /// <c>watchForIndicatorControls</c>: a checkbox, a radio or a toggle came or went inside a wrapper in a slot, which the slot
    /// doesn't see, so it runs <c>updateInteractivityOnSlotChange</c>. Ionic's <c>MutationObserver</c> reports later, once for any
    /// number of changes, as this does. One directly in a slot is the slot's change, and a detached item reads its slots again
    /// when its controls come back.
    /// </summary>
    internal void IndicatorControlChanged(Control control)
    {
        if (_indicatorChangePosted || !this.IsAttachedToVisualTree() || _slots.Any(slot => slot is not null && slot.Child == control))
        {
            return;
        }

        _indicatorChangePosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _indicatorChangePosted = false;
            UpdateInteractivity();
        }, DispatcherPriority.Loaded);
    }

    private void UpdateLines()
    {
        var lines = Lines;
        PseudoClasses.Set(":lines-default", lines is null);
        PseudoClasses.Set(":lines-full", lines == ItemLines.Full);
        PseudoClasses.Set(":lines-inset", lines == ItemLines.Inset);
        PseudoClasses.Set(":lines-none", lines == ItemLines.None);
    }

    /// <summary><c>showDetail</c>, in md.</summary>
    private void UpdateDetail()
    {
        if (_detailIcon is not null)
        {
            _detailIcon.IsVisible = Detail == true;
        }
    }

    // ── The controls inside ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Everything slotted, in slot order. A presenter is its child's logical parent, so the host's own
    /// logical descendants don't include these.
    /// </summary>
    private IEnumerable<Control> SlottedControls() =>
        _slots.Select(slot => slot?.Child)
            .OfType<Control>()
            .SelectMany(child => child.GetSelfAndLogicalDescendants().OfType<Control>());

    private static ItemControlType? TypeOf(Control control) => control switch
    {
        IItemControl itemControl => itemControl.ItemControlType,
        Avalonia.Controls.Button => ItemControlType.Button,
        _ => null,
    };

    /// <summary><c>getFirstInteractive</c>.</summary>
    private IItemControl? FirstInteractive() =>
        SlottedControls()
            .Where(control => control.IsEffectivelyEnabled)
            .OfType<IItemControl>()
            .FirstOrDefault(control => FirstInteractiveTypes.Contains(control.ItemControlType));

    /// <summary><c>setMultipleInputs</c>, <c>canActivate</c> and <c>isFocusable</c>.</summary>
    private void UpdateInteractivity()
    {
        _firstInteractive = FirstInteractive();

        // Most rows are plain, and don't pay for this until they have something to press or to forward to.
        if (_native is null && (Button || _firstInteractive is not null))
        {
            _native = new NativeButton(this, Tap, () => Button, UpdateIsEffectivelyEnabled);
            UpdateIsEffectivelyEnabled();
        }

        int covers = 0, inputs = 0, clickables = 0;
        var focusableChild = false;
        var slottedIndicatorControl = false;
        for (var slot = 0; slot < _slots.Length; slot++)
        {
            if (_slots[slot]?.Child is not Control slotted)
            {
                continue;
            }

            foreach (var control in slotted.GetSelfAndLogicalDescendants().OfType<Control>())
            {
                var type = TypeOf(control);

                // isFocusable reads .ion-focusable once, in the frame after load, before a checkbox or a radio in a multi-input item
                // has rendered again with its own ion-focusable, which item-multiple-inputs gives it. So theirs never makes the item
                // focusable: measured, an item with two checkboxes, or a radio and a checkbox, isn't ion-focusable. A toggle's is
                // there from its first render.
                focusableChild |= FocusVisible.GetIsEnabled(control) && type is not (ItemControlType.Checkbox or ItemControlType.Radio);

                // setHasSlottedIndicatorControl: INDICATOR_CONTROL_SELECTOR in the default slot, directly or inside what's in it.
                // Only the default slot clips, so one in the start or end slot needs no room.
                slottedIndicatorControl |= slot == 1 && type is (ItemControlType.Checkbox or ItemControlType.Radio or ItemControlType.Toggle);

                switch (type)
                {
                    case ItemControlType.Checkbox or ItemControlType.Datetime or ItemControlType.Select or ItemControlType.Radio:
                        covers++;
                        break;
                    case ItemControlType.Input or ItemControlType.Range or ItemControlType.Searchbar
                        or ItemControlType.Segment or ItemControlType.Textarea or ItemControlType.Toggle:
                        inputs++;
                        break;
                    case ItemControlType.Button:
                        clickables++;
                        break;
                }
            }
        }

        var clickable = Button;
        _multipleInputs = covers + inputs > 1 || covers + clickables > 1 || (covers > 0 && clickable);
        var hasCover = covers == 1 && !_multipleInputs;
        var canActivate = clickable || hasCover;

        PseudoClasses.Set(":button", clickable);
        PseudoClasses.Set(ActivatablePseudoClass, canActivate);
        PseudoClasses.Set(MultipleInputsPseudoClass, _multipleInputs);

        // slottedIndicatorNeedsRoom: a control in a single-input item leaves its indicator to the item, so only a multi-input or a
        // clickable item has one to make room for.
        PseudoClasses.Set(":item-focus-indicator-room", (_multipleInputs || clickable) && slottedIndicatorControl);
        _activatable.SetWhen(canActivate, true);
        _focusVisible.SetWhen(canActivate || focusableChild, true);

        // A <button> takes focus, and a cover's control takes it itself.
        _focusable.SetWhen(clickable, true);

        // :host(.item-interactive-disabled:not(.item-multiple-inputs)) { pointer-events: none }
        _noInput.SetWhen(PseudoClasses.Contains(":item-interactive-disabled") && !_multipleInputs, false);

        if (_ripple is not null)
        {
            _ripple.IsVisible = canActivate;
        }

        // firstInteractiveNeedsPointerCursor
        PseudoClasses.Set(
            ":item-control-needs-pointer-cursor",
            _firstInteractive is { ItemControlType: not (ItemControlType.Input or ItemControlType.Textarea) });
    }

    /// <summary><c>clickedWithinShadowRoot</c>, inverted. Plain text in a slot has no element of its own, so it's padding.</summary>
    private bool IsOnSlottedControl(object? source)
    {
        for (var visual = source as Visual; visual is not null && visual != this; visual = visual.GetVisualParent())
        {
            foreach (var slot in _slots)
            {
                if (slot?.Child == visual)
                {
                    return !(visual is TextBlock && slot.Content is not Control && slot.ContentTemplate is null);
                }
            }
        }

        return false;
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>onClick</c>.</summary>
    private void Tap(object? source)
    {
        if (Button)
        {
            RaiseClick();
        }

        // firstInteractive.click(): a control disabled since ignores it itself, as each of Ionic's does in its onClick.
        if (!_multipleInputs && _firstInteractive is { } first && !IsOnSlottedControl(source))
        {
            first.OnItemTap();
            _forwarded = true;
        }
    }

    private void RaiseClick() => _native?.Click(ClickEvent);

    // ── ionStyle ─────────────────────────────────────────────────────────────────────────────

    /// <summary><c>itemStyle</c>: each true key becomes <c>:item-key</c>, kept per type of child, as Ionic's by tag name.</summary>
    private void OnIonStyle(object? sender, IonStyleEventArgs e)
    {
        if (_itemStyles.Apply(PseudoClasses, e))
        {
            // forceUpdate: Ionic re-renders, which also works out the first interactive control again.
            UpdateInteractivity();
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    private ItemAutomationPeer? _peer;

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new ItemAutomationPeer(this);

    /// <summary>The accordion whose header this item is: <c>getSlottedHeaderIonItem</c>, the other way round.</summary>
    private AvonicAccordion? HeaderOf() =>
        this.GetLogicalAncestors().OfType<AvonicAccordion>().FirstOrDefault(accordion => accordion.HeaderItem() == this);

    /// <summary>The accordion's <c>setAria</c>: its state changed, which this item's peer announces.</summary>
    internal void RaiseExpandCollapseChanged() => _peer?.RaiseExpandCollapseChanged();

    private sealed class ItemAutomationPeer(AvonicItem owner) : ControlAutomationPeer(owner), IInvokeProvider, IExpandCollapseProvider
    {
        /// <summary>What a screen reader knows: the state when the peer was made, or when it was last read or announced.</summary>
        private ExpandCollapseState _last = StateOf(owner);

        private static ExpandCollapseState StateOf(AvonicItem item) =>
            item.HeaderOf()?.IsExpandedState == true ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        /// <summary>
        /// Ionic only sets onClick when clickable, "to prevent screen readers from reading all items as clickable". And
        /// <c>aria-expanded</c> only on an accordion's header item, which the accordion sets on its button.
        /// </summary>
        protected override object? GetProviderCore(Type providerType) =>
            providerType == typeof(IInvokeProvider) && !owner.Button ? null
            : providerType == typeof(IExpandCollapseProvider) && owner.HeaderOf() is null ? null
            : base.GetProviderCore(providerType);

        public ExpandCollapseState ExpandCollapseState => _last = StateOf(owner);

        public bool ShowsMenu => false;

        /// <summary>Asks the group, as a tap does.</summary>
        public void Expand()
        {
            if (owner.HeaderOf() is { IsExpandedState: false } accordion)
            {
                accordion.ToggleExpanded();
            }
        }

        public void Collapse()
        {
            if (owner.HeaderOf() is { IsExpandedState: true } accordion)
            {
                accordion.ToggleExpanded();
            }
        }

        /// <summary>Only a change: an accordion that renders again in the same state says nothing.</summary>
        public void RaiseExpandCollapseChanged()
        {
            var last = _last;
            var state = ExpandCollapseState;
            if (state != last)
            {
                RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, last, state);
            }
        }

        /// <summary><c>aria-disabled</c>: <c>disabled || childStyles['item-interactive-disabled']</c>.</summary>
        protected override bool IsEnabledCore() =>
            base.IsEnabledCore() && !owner.PseudoClasses.Contains(":item-interactive-disabled");

        /// <summary>
        /// <c>role="listitem"</c>, where <c>inList = hostContext('ion-list') &amp;&amp; !hostContext('ion-radio-group')</c>. A button
        /// item stays a button: Ionic has a button inside the listitem, and Avalonia has one peer for both.
        /// </summary>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            if (owner.Button)
            {
                return AutomationControlType.Button;
            }

            var inList = false;
            foreach (var ancestor in owner.GetLogicalAncestors())
            {
                if (ancestor is AvonicRadioGroup)
                {
                    return AutomationControlType.Group;
                }

                inList |= ancestor is AvonicList;
            }

            return inList ? AutomationControlType.ListItem : AutomationControlType.Group;
        }

        /// <summary>A native button names itself from what's inside it.</summary>
        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            // A plain item has no native button to name, measured, and its children speak for themselves.
            return owner.Button ? AccessibleText.Of(owner) : name;
        }

        protected override bool IsContentElementCore() => owner.Button;

        protected override bool IsControlElementCore() => true;

        public void Invoke()
        {
            // A native button's click is what the host's onClick forwards.
            if (owner.Button && IsEnabled())
            {
                owner.Tap(owner);
            }
        }
    }
}
