using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-radio</c>: a radio with its label. Checked is internal, as Ionic's <c>checked</c> is <c>@State</c>: in an
/// <see cref="AvonicRadioGroup"/> it follows the group's <see cref="AvonicRadioGroup.Value"/>, and alone a click checks it for
/// good. The look is in the theme. See <c>docs/components/ion-radio/README.md</c>.
/// </summary>
/// <remarks>
/// Checked is the pseudoclass <c>:checked</c> (Ionic's <c>radio-checked</c>), and the automation peer's <c>IsSelected</c>
/// (<c>aria-checked</c>).
/// </remarks>
[TemplatePart("PART_Label", typeof(Control))]
[TemplatePart("PART_Container", typeof(Control))]
[TemplatePart("PART_Mark", typeof(Control))]
[PseudoClasses(":checked", ":in-item", ":slot-start", ":slot-end",
    ":label-placement-start", ":label-placement-end", ":label-placement-fixed", ":label-placement-stacked",
    ":justify-start", ":justify-end", ":justify-space-between", ":alignment-start", ":alignment-center")]
public class AvonicRadio : ContentControl, IItemControl
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>alignment</c>: <c>align-items</c> of the label and the radio. Set, the radio fills its line.</summary>
    public static readonly StyledProperty<RadioAlignment?> AlignmentProperty =
        AvaloniaProperty.Register<AvonicRadio, RadioAlignment?>(nameof(Alignment));

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicRadio>();

    /// <summary>Ionic <c>justify</c>: <c>justify-content</c> of the label and the radio. Set, the radio fills its line.</summary>
    public static readonly StyledProperty<RadioJustify?> JustifyProperty =
        AvaloniaProperty.Register<AvonicRadio, RadioJustify?>(nameof(Justify));

    /// <summary>Ionic <c>labelPlacement</c>.</summary>
    public static readonly StyledProperty<RadioLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<AvonicRadio, RadioLabelPlacement>(nameof(LabelPlacement));

    /// <summary>Ionic <c>value</c>. Null gets a unique one when the radio is attached, as <c>connectedCallback</c> gives it.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicRadio, object?>(nameof(Value));

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionFocus</c>: the radio itself got focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicRadio, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>: the radio itself lost focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicRadio, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? IonFocus
    {
        add => AddHandler(IonFocusEvent, value);
        remove => RemoveHandler(IonFocusEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonBlur
    {
        add => AddHandler(IonBlurEvent, value);
        remove => RemoveHandler(IonBlurEvent, value);
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>radioButtonIds</c>, for <c>ion-rb-N</c>.</summary>
    private static int s_ids;

    private AvonicRadioGroup? _group;
    private bool _checked;
    private Control? _label;
    private readonly StyleValue<bool> _tabStop = new();
    private readonly StyleValue<bool> _activatable = new();
    private readonly StyleValue<bool> _focusVisible = new();

    static AvonicRadio()
    {
        // tabindex="-1": focusable, and not a tab stop until a group makes it one.
        FocusableProperty.OverrideDefaultValue<AvonicRadio>(true);
        IsTabStopProperty.OverrideDefaultValue<AvonicRadio>(false);

        // display: inline-block, as wide as its content. The theme stretches it where Ionic makes it a block.
        HorizontalAlignmentProperty.OverrideDefaultValue<AvonicRadio>(HorizontalAlignment.Left);

        // overflow: visible, for the focus circle around the ring. A TemplatedControl clips by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicRadio>(false);

        LabelPlacementProperty.Changed.AddClassHandler<AvonicRadio>((radio, _) => radio.UpdateClasses());
        JustifyProperty.Changed.AddClassHandler<AvonicRadio>((radio, _) => radio.UpdateClasses());
        AlignmentProperty.Changed.AddClassHandler<AvonicRadio>((radio, _) => radio.UpdateClasses());
        ValueProperty.Changed.AddClassHandler<AvonicRadio>((radio, _) => radio.UpdateState());
        ContentProperty.Changed.AddClassHandler<AvonicRadio>((radio, _) => radio.UpdateLabel());
    }

    public AvonicRadio()
    {
        // onClick, on the host: a DOM click on release, with no keys, as the radio is a div.
        // It lives as long as the radio, through the handlers it adds to it.
        _ = new NativeButton(this, _ => Click(), () => false, UpdateIsEffectivelyEnabled);

        AddHandler(GotFocusEvent, (_, e) =>
        {
            if (e.Source == this)
            {
                RaiseEvent(new RoutedEventArgs(IonFocusEvent));
            }
        });
        AddHandler(LostFocusEvent, (_, e) =>
        {
            if (e.Source == this)
            {
                RaiseEvent(new RoutedEventArgs(IonBlurEvent));
            }
        });

        // At style priority, as the classes and tabindex are, so an app's own value wins. Bound once, before any style, so that an
        // app's style still wins when the group or an item changes them after it.
        Bind(IsTabStopProperty, _tabStop, BindingPriority.Style);
        Bind(Activatable.IsEnabledProperty, _activatable, BindingPriority.Style);
        Bind(FocusVisible.IsEnabledProperty, _focusVisible, BindingPriority.Style);

        UpdateClasses();
        UpdateHostContext(inItem: false);
    }

    public RadioAlignment? Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public RadioJustify? Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    public RadioLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    ItemControlType IItemControl.ItemControlType => ItemControlType.Radio;

    /// <summary>Ionic's <c>checked</c>: <c>radio-checked</c>, <c>aria-checked</c>.</summary>
    internal bool IsChecked => _checked;

    /// <summary>The group around it, which Ionic finds with <c>closest('ion-radio-group')</c>.</summary>
    internal AvonicRadioGroup? Group => _group;

    /// <summary><c>firstInteractive.click()</c>, from an item whose padding was tapped.</summary>
    void IItemControl.OnItemTap() => Click();

    /// <summary>
    /// A DOM <c>click</c> on the radio: its own <c>onClick</c>, then the group's, which the click bubbles to. A disabled radio
    /// takes no click, from a press or from automation.
    /// </summary>
    internal void Click()
    {
        if (!IsEffectivelyEnabled)
        {
            return;
        }

        if (_group is { } group)
        {
            group.OnRadioClick(this);
        }
        else
        {
            // Alone, a radio can't be unchecked: there's no group to allow an empty selection.
            SetChecked(true);
        }
    }

    // ── The group ────────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        // connectedCallback: a unique value, and the group.
        if (Value is null)
        {
            SetCurrentValue(ValueProperty, $"ion-rb-{s_ids++}");
        }

        _group = this.GetLogicalAncestors().OfType<AvonicRadioGroup>().FirstOrDefault();
        if (_group is not null)
        {
            _group.ValueChanged += UpdateState;
            UpdateState();
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);

        if (_group is not null)
        {
            _group.ValueChanged -= UpdateState;
            _group = null;
        }
    }

    /// <summary><c>updateState</c>: checked when the group's value is this one's.</summary>
    private void UpdateState()
    {
        if (_group is { } group)
        {
            SetChecked(group.Selects(Value));
        }
    }

    private void SetChecked(bool value)
    {
        if (_checked == value)
        {
            return;
        }

        _checked = value;
        PseudoClasses.Set(":checked", value);
        if (ControlAutomationPeer.FromElement(this) is { } peer)
        {
            peer.RaisePropertyChangedEvent(Avalonia.Automation.SelectionItemPatternIdentifiers.IsSelectedProperty, !value, value);
        }
    }

    /// <summary><c>setButtonTabindex</c>: 0 makes it the group's tab stop, at style priority, so an app's own value wins.</summary>
    internal void SetTabStop(bool value) => _tabStop.SetWhen(value, true);

    // ── Host context ─────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // hostContext('ion-item'), read at render, which is when it's attached here.
        UpdateHostContext(this.GetVisualAncestors().OfType<AvonicItem>().Any());
    }

    /// <summary>
    /// <c>in-item</c>, and <c>ion-activatable</c> and <c>ion-focusable</c> only when not in one: the item is, as the radio is its
    /// cover. <c>[slot=start]</c> and <c>[slot=end]</c> are a host's start and end slots, an item's or a toolbar's.
    /// </summary>
    private void UpdateHostContext(bool inItem)
    {
        PseudoClasses.Set(":in-item", inItem);
        var slot = (Parent as ContentPresenter)?.Name;
        PseudoClasses.Set(":slot-start", slot == "PART_Start");
        PseudoClasses.Set(":slot-end", slot == "PART_End");

        // At style priority, as the classes are, so an app's own value wins. A value it already has raises nothing, so a move
        // doesn't either.
        _activatable.Set(!inItem);
        _focusVisible.Set(!inItem);
    }

    private void UpdateClasses()
    {
        var placement = LabelPlacement;
        PseudoClasses.Set(":label-placement-start", placement == RadioLabelPlacement.Start);
        PseudoClasses.Set(":label-placement-end", placement == RadioLabelPlacement.End);
        PseudoClasses.Set(":label-placement-fixed", placement == RadioLabelPlacement.Fixed);
        PseudoClasses.Set(":label-placement-stacked", placement == RadioLabelPlacement.Stacked);

        var justify = Justify;
        PseudoClasses.Set(":justify-start", justify == RadioJustify.Start);
        PseudoClasses.Set(":justify-end", justify == RadioJustify.End);
        PseudoClasses.Set(":justify-space-between", justify == RadioJustify.SpaceBetween);

        var alignment = Alignment;
        PseudoClasses.Set(":alignment-start", alignment == RadioAlignment.Start);
        PseudoClasses.Set(":alignment-center", alignment == RadioAlignment.Center);
    }

    // ── Template ─────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _label = e.NameScope.Find<Control>("PART_Label");
        UpdateLabel();
    }

    /// <summary>
    /// <c>label-text-wrapper-hidden</c>, when <c>textContent</c> is empty: no content, or an empty string. A control counts as
    /// text, as most have some.
    /// </summary>
    private void UpdateLabel()
    {
        if (_label is not null)
        {
            _label.IsVisible = Content is not (null or "");
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new RadioAutomationPeer(this);

    /// <summary>
    /// <c>role="radio"</c>, <c>aria-checked</c> and <c>aria-disabled</c>, named from the label, as a browser names a radio from
    /// its content. Selecting an unchecked radio is a screen reader's activation, which is a DOM <c>click</c>.
    /// </summary>
    private sealed class RadioAutomationPeer(AvonicRadio owner) : ControlAutomationPeer(owner), ISelectionItemProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.RadioButton;

        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? AccessibleText.Of(owner) : name;
        }

        public bool IsSelected => owner.IsChecked;

        public ISelectionProvider? SelectionContainer => null;

        /// <summary>Selecting a selected radio leaves it so, where a click on it could clear the group's value.</summary>
        public void Select()
        {
            if (!owner.IsChecked)
            {
                owner.Click();
            }
        }

        public void AddToSelection() => Select();

        public void RemoveFromSelection()
        {
        }
    }
}
