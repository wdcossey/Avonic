using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>The detail of <see cref="AvonicCheckbox.IonChange"/>: Ionic's <c>CheckboxChangeEventDetail</c>.</summary>
public sealed class CheckboxChangeEventArgs(RoutedEvent routedEvent, object? source, bool isChecked, object? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>The new <see cref="AvonicCheckbox.Checked"/>.</summary>
    public bool Checked { get; } = isChecked;

    /// <summary>The checkbox's <see cref="AvonicCheckbox.Value"/>.</summary>
    public object? Value { get; } = value;
}

/// <summary>
/// Ionic <c>ion-checkbox</c>: a box with a drawn mark, and its own label, which is the content. The look is in the theme. See
/// <c>docs/components/ion-checkbox/README.md</c>.
/// </summary>
/// <remarks>
/// A click anywhere on it toggles it, as Ionic's host <c>onClick</c> does, through <see cref="NativeButton"/> with no keys, as
/// the DOM <c>click</c> of an element that isn't a button. Space toggles on key down, as Ionic's <c>onKeyDown</c> does.
/// </remarks>
[TemplatePart("PART_Wrapper", typeof(LabelledControlPanel))]
[TemplatePart("PART_Label", typeof(Control))]
[TemplatePart("PART_Content", typeof(ContentPresenter))]
[TemplatePart("PART_Bottom", typeof(Control))]
[TemplatePart("PART_HelperText", typeof(TextBlock))]
[TemplatePart("PART_ErrorText", typeof(TextBlock))]
[TemplatePart("PART_NativeWrapper", typeof(Control))]
[TemplatePart("PART_Container", typeof(Border))]
[TemplatePart("PART_Mark", typeof(CheckboxMark))]
[PseudoClasses(":checked", ":indeterminate", ":in-item", ":slot-start", ":slot-end", ":drawn")]
[PseudoClasses(":label-placement-start", ":label-placement-end", ":label-placement-fixed", ":label-placement-stacked")]
[PseudoClasses(":justify-start", ":justify-end", ":justify-space-between", ":alignment-start", ":alignment-center")]
public class AvonicCheckbox : ContentControl, IItemControl
{
    /// <summary>Ionic <c>color</c>: the checked background and border, and the mark.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicCheckbox>();

    /// <summary>Ionic <c>checked</c>. A click sets it, so it binds two-way by default, as a <c>CheckBox</c>'s does.</summary>
    public static readonly StyledProperty<bool> CheckedProperty =
        AvaloniaProperty.Register<AvonicCheckbox, bool>(nameof(Checked), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Ionic <c>indeterminate</c>. A click clears it, so it binds two-way by default.</summary>
    public static readonly StyledProperty<bool> IndeterminateProperty =
        AvaloniaProperty.Register<AvonicCheckbox, bool>(nameof(Indeterminate), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Ionic <c>labelPlacement</c>.</summary>
    public static readonly StyledProperty<CheckboxLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<AvonicCheckbox, CheckboxLabelPlacement>(nameof(LabelPlacement), CheckboxLabelPlacement.Start);

    /// <summary>Ionic <c>justify</c>. Unset, the label and the box are at either end, or packed at the start with the label at the end.</summary>
    public static readonly StyledProperty<CheckboxJustify?> JustifyProperty =
        AvaloniaProperty.Register<AvonicCheckbox, CheckboxJustify?>(nameof(Justify));

    /// <summary>Ionic <c>alignment</c>. Unset, they're centred.</summary>
    public static readonly StyledProperty<CheckboxAlignment?> AlignmentProperty =
        AvaloniaProperty.Register<AvonicCheckbox, CheckboxAlignment?>(nameof(Alignment));

    /// <summary>Ionic <c>helperText</c>: under the label, unless it's invalid.</summary>
    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<AvonicCheckbox, string?>(nameof(HelperText));

    /// <summary>Ionic <c>errorText</c>: under the label while it has the <c>ion-touched</c> and <c>ion-invalid</c> classes.</summary>
    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<AvonicCheckbox, string?>(nameof(ErrorText));

    /// <summary>Ionic <c>required</c>: for accessibility only, as <c>aria-required</c>.</summary>
    public static readonly StyledProperty<bool> RequiredProperty =
        AvaloniaProperty.Register<AvonicCheckbox, bool>(nameof(Required));

    /// <summary>Ionic <c>value</c>, which <see cref="IonChange"/> carries. It isn't whether it's checked.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<AvonicCheckbox, object?>(nameof(Value), "on");

    /// <summary>Ionic <c>ionChange</c>: <see cref="Checked"/> changed through the user, not through code.</summary>
    public static readonly RoutedEvent<CheckboxChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicCheckbox, CheckboxChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionFocus</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicCheckbox, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicCheckbox, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    private Control? _label;
    private ContentPresenter? _content;
    private Control? _bottom;
    private TextBlock? _helper;
    private TextBlock? _error;
    private IDisposable? _firstFrame;
    private bool _invalid;
    private string? _hint;
    private readonly StyleValue<bool> _required = new();
    private CheckboxAutomationPeer? _peer;

    static AvonicCheckbox()
    {
        // tabindex: 0, unless disabled, which a disabled control already isn't.
        FocusableProperty.OverrideDefaultValue<AvonicCheckbox>(true);
    }

    public AvonicCheckbox()
    {
        // onClick: the DOM click, on release, with no keys. Its own Space is OnKeyDown.
        _ = new NativeButton(this, _ => OnClick(), () => false, () => { });

        // The MutationObserver on class, for ion-touched and ion-invalid.
        // Pseudoclasses are in the same list, so only a change of the two classes counts.
        Classes.CollectionChanged += (_, _) =>
        {
            if (IsInvalid != _invalid)
            {
                UpdateHint();
            }
        };

        // aria-required, at style priority, so an app's own value wins. Bound once, before any style, so that an app's style still
        // wins when required turns on after it.
        Bind(AutomationProperties.IsRequiredForFormProperty, _required, BindingPriority.Style);

        UpdateChecked();
        UpdatePlacement();
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool Checked
    {
        get => GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public bool Indeterminate
    {
        get => GetValue(IndeterminateProperty);
        set => SetValue(IndeterminateProperty, value);
    }

    public CheckboxLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public CheckboxJustify? Justify
    {
        get => GetValue(JustifyProperty);
        set => SetValue(JustifyProperty, value);
    }

    public CheckboxAlignment? Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    public string? HelperText
    {
        get => GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    public string? ErrorText
    {
        get => GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public bool Required
    {
        get => GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public event EventHandler<CheckboxChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

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

    /// <summary><c>checkInvalidState</c>: the app's <c>ion-touched</c> and <c>ion-invalid</c> classes, both.</summary>
    private bool IsInvalid => Classes.Contains("ion-touched") && Classes.Contains("ion-invalid");

    // ── Item ─────────────────────────────────────────────────────────────────────────────────

    ItemControlType IItemControl.ItemControlType => ItemControlType.Checkbox;

    /// <summary>The item's <c>firstInteractive.click()</c>, which is this control's <c>onClick</c>.</summary>
    void IItemControl.OnItemTap() => OnClick();

    // ── State ────────────────────────────────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Ionic renders again when any of its props changes, and reads the label's textContent then. It reads hostContext then
        // too, but the ancestors and the slot can't change without a detach and an attach, where they're read.
        if (change.Property.OwnerType == typeof(AvonicCheckbox) || change.Property == ContentProperty
            || change.Property == ColorProperty || change.Property == IsEnabledProperty)
        {
            UpdateLabel();
        }

        if (change.Property == CheckedProperty || change.Property == IndeterminateProperty)
        {
            UpdateChecked();
            var old = change.Property == CheckedProperty
                ? ToggleStateOf(change.GetOldValue<bool>(), Indeterminate)
                : ToggleStateOf(Checked, change.GetOldValue<bool>());
            var now = ToggleStateOf(Checked, Indeterminate);
            if (old != now)
            {
                _peer?.RaiseToggleStateChanged(old, now);
            }
        }
        else if (change.Property == LabelPlacementProperty || change.Property == JustifyProperty || change.Property == AlignmentProperty)
        {
            UpdatePlacement();
        }
        else if (change.Property == HelperTextProperty || change.Property == ErrorTextProperty)
        {
            UpdateHint();
        }
        else if (change.Property == RequiredProperty)
        {
            _required.SetWhen(Required, true);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _label = e.NameScope.Find<Control>("PART_Label");
        _bottom = e.NameScope.Find<Control>("PART_Bottom");
        _helper = e.NameScope.Find<TextBlock>("PART_HelperText");
        _error = e.NameScope.Find<TextBlock>("PART_ErrorText");

        // What a template makes of data comes after Content changes, so the label is looked at again then.
        if (_content is not null)
        {
            _content.PropertyChanged -= OnContentPresenterChanged;
        }

        _content = e.NameScope.Find<ContentPresenter>("PART_Content");
        if (_content is not null)
        {
            _content.PropertyChanged += OnContentPresenterChanged;
        }

        // aria-live="polite" on the helper, and role="alert" on the error, which is assertive.
        _helper?.SetValue(AutomationProperties.LiveSettingProperty, AutomationLiveSetting.Polite);
        _error?.SetValue(AutomationProperties.LiveSettingProperty, AutomationLiveSetting.Assertive);

        UpdateLabel();
        UpdateHint();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        UpdateHostContext();
        HoldTransitions();
    }

    /// <summary><c>hostContext('ion-item')</c>, read at each render, and <c>[slot]</c>: the item slot holding it.</summary>
    private void UpdateHostContext()
    {
        PseudoClasses.Set(":in-item", this.FindAncestorOfType<AvonicItem>() is not null);
        var slot = (Parent as StyledElement)?.Name;
        PseudoClasses.Set(":slot-start", slot == "PART_Start");
        PseudoClasses.Set(":slot-end", slot == "PART_End");
    }

    private void OnContentPresenterChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ChildProperty)
        {
            UpdateLabel();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        ReleaseTransitions();
        PseudoClasses.Set(":drawn", false);
    }

    /// <summary>
    /// <c>:drawn</c>, from the first frame after it's attached: CSS doesn't transition a first style, so the theme's
    /// <c>--transition</c> waits for it, and a checkbox checked from the start is drawn checked. A transition can't be taken back
    /// once it's started, so it's the theme's rule for the transition that waits.
    /// </summary>
    private void HoldTransitions()
    {
        ReleaseTransitions();
        PseudoClasses.Set(":drawn", false);
        _firstFrame = AvonicConfig.RequestFrame(this, () =>
        {
            _firstFrame = null;
            PseudoClasses.Set(":drawn", true);
        });
    }

    private void ReleaseTransitions()
    {
        _firstFrame?.Dispose();
        _firstFrame = null;
    }

    private void UpdateChecked()
    {
        PseudoClasses.Set(":checked", Checked);
        PseudoClasses.Set(":indeterminate", Indeterminate);
    }

    private void UpdatePlacement()
    {
        var placement = LabelPlacement;
        PseudoClasses.Set(":label-placement-start", placement == CheckboxLabelPlacement.Start);
        PseudoClasses.Set(":label-placement-end", placement == CheckboxLabelPlacement.End);
        PseudoClasses.Set(":label-placement-fixed", placement == CheckboxLabelPlacement.Fixed);
        PseudoClasses.Set(":label-placement-stacked", placement == CheckboxLabelPlacement.Stacked);
        PseudoClasses.Set(":justify-start", Justify == CheckboxJustify.Start);
        PseudoClasses.Set(":justify-end", Justify == CheckboxJustify.End);
        PseudoClasses.Set(":justify-space-between", Justify == CheckboxJustify.SpaceBetween);
        PseudoClasses.Set(":alignment-start", Alignment == CheckboxAlignment.Start);
        PseudoClasses.Set(":alignment-center", Alignment == CheckboxAlignment.Center);
    }

    /// <summary>
    /// <c>label-text-wrapper-hidden</c>: <c>el.textContent === ''</c>, which Ionic reads at each render, so text that comes later
    /// shows at the next change of a prop, as there. Here at each measure too, which a change of a shown label's text causes.
    /// </summary>
    private void UpdateLabel()
    {
        if (_label is not null && TextContent.HasText(Content, _content) is var shown && _label.IsVisible != shown)
        {
            _label.IsVisible = shown;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        UpdateLabel();
        return base.MeasureOverride(availableSize);
    }

    /// <summary>
    /// <c>renderHintText</c>: the bottom only with helper or error text, and the helper's or the error's text, as
    /// <c>isInvalid</c> says. The CSS shows the error only on <c>.ion-touched.ion-invalid</c>, which is the same condition. An
    /// empty one has no box, as an empty block has no height.
    /// </summary>
    private void UpdateHint()
    {
        var invalid = _invalid = IsInvalid;

        // aria-describedby changes with it, which a screen reader announces.
        var hint = HintText();
        if (hint != _hint)
        {
            _peer?.RaiseHelpTextChanged(_hint, hint);
            _hint = hint;
        }

        if (_bottom is not null)
        {
            _bottom.IsVisible = !string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText);
        }

        if (_helper is not null)
        {
            _helper.Text = invalid ? null : HelperText;
            _helper.IsVisible = !invalid && !string.IsNullOrEmpty(HelperText);
        }

        if (_error is not null)
        {
            _error.Text = invalid ? ErrorText : null;
            _error.IsVisible = invalid && !string.IsNullOrEmpty(ErrorText);
        }
    }

    /// <summary><c>aria-checked</c>: <c>mixed</c> when indeterminate, whatever <c>checked</c> is.</summary>
    private static ToggleState ToggleStateOf(bool isChecked, bool indeterminate) =>
        indeterminate ? ToggleState.Indeterminate : isChecked ? ToggleState.On : ToggleState.Off;

    /// <summary>
    /// The <c>aria-describedby</c> target, <c>getHintTextId</c>: the error while invalid with one, else the helper. While invalid,
    /// the helper's div is empty, so it describes nothing.
    /// </summary>
    private string? HintText()
    {
        var invalid = IsInvalid;
        if (invalid && !string.IsNullOrEmpty(ErrorText))
        {
            return ErrorText;
        }

        return invalid || string.IsNullOrEmpty(HelperText) ? null : HelperText;
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>onClick</c>: nothing while disabled.</summary>
    private void OnClick()
    {
        if (IsEffectivelyEnabled)
        {
            ToggleChecked();
        }
    }

    /// <summary><c>toggleChecked</c>: <c>setChecked</c>, which raises <c>ionChange</c>, and then <c>indeterminate = false</c>.</summary>
    private void ToggleChecked()
    {
        var isChecked = !Checked;
        SetCurrentValue(CheckedProperty, isChecked);
        RaiseEvent(new CheckboxChangeEventArgs(IonChangeEvent, this, isChecked, Value));
        SetCurrentValue(IndeterminateProperty, false);
    }

    /// <summary><c>onKeyDown</c>: Space, on every key down, repeats too. Enter does nothing.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space && !e.Handled)
        {
            // preventDefault, so nothing scrolls.
            e.Handled = true;
            OnClick();
        }
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        if (ReferenceEquals(e.Source, this))
        {
            RaiseEvent(new RoutedEventArgs(IonFocusEvent, this));
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        if (ReferenceEquals(e.Source, this))
        {
            RaiseEvent(new RoutedEventArgs(IonBlurEvent, this));
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new CheckboxAutomationPeer(this);

    /// <summary>
    /// <c>role="checkbox"</c> with <c>aria-checked</c>, <c>mixed</c> when indeterminate. Named by the label, through
    /// <c>aria-labelledby</c>, which is the slot's text and the helper's while it's shown, not the error's, as measured.
    /// <c>aria-describedby</c> is the help text. <c>Toggle</c> is a screen reader's click.
    /// </summary>
    private sealed class CheckboxAutomationPeer(AvonicCheckbox owner) : ControlAutomationPeer(owner), IToggleProvider
    {
        public ToggleState ToggleState => ToggleStateOf(owner.Checked, owner.Indeterminate);

        public void Toggle() => owner.OnClick();

        public void RaiseToggleStateChanged(ToggleState old, ToggleState now) =>
            RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, old, now);

        public void RaiseHelpTextChanged(string? old, string? now) =>
            RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, old, now);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            if (!string.IsNullOrEmpty(name) || owner._label is not { IsVisible: true })
            {
                return name;
            }

            var parts = new List<string>();
            if (owner._content is not null && AccessibleText.Of(owner._content) is { Length: > 0 } label)
            {
                parts.Add(label);
            }

            if (owner._helper is { IsVisible: true, Text: { Length: > 0 } helper })
            {
                parts.Add(helper);
            }

            return string.Join(" ", parts);
        }

        protected override string? GetHelpTextCore()
        {
            var help = base.GetHelpTextCore();
            return string.IsNullOrEmpty(help) ? owner.HintText() : help;
        }
    }
}
