using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-input</c>: a one-line text field with a label, start and end slots, a clear button, helper and error text and a
/// counter. The native <c>&lt;input&gt;</c> is a <see cref="TextBox"/>, <c>PART_NativeInput</c>. The look is in the theme. See
/// <c>docs/components/ion-input/README.md</c>.
/// </summary>
/// <remarks>
/// <see cref="Value"/> and the text box's <c>Text</c> are kept in step with a guard, so that only the user's edits raise
/// <see cref="IonInput"/>, as only the native <c>input</c> event does in Ionic. A click on the wrapper is the <c>&lt;label for&gt;</c>'s
/// activation, through <see cref="NativeButton"/> with no keys; the clear button is a native <c>&lt;button&gt;</c> of its own.
/// </remarks>
[TemplatePart("PART_NativeInput", typeof(TextBox))]
[TemplatePart("PART_Wrapper", typeof(Control))]
[TemplatePart("PART_ClearButton", typeof(Control))]
[TemplatePart("PART_LabelContent", typeof(ContentPresenter))]
[PseudoClasses(":has-value", ":has-focus", ":label-floating", ":in-item", ":in-item-color", ":input-disabled", ":readonly")]
[PseudoClasses(":fill-outline", ":fill-solid", ":shape-round", ":label-text-wrapper-hidden", ":has-bottom", ":ion-hover")]
[PseudoClasses(":label-placement-start", ":label-placement-end", ":label-placement-fixed", ":label-placement-floating", ":label-placement-stacked")]
public class AvonicInput : TemplatedControl, IItemControl, IInputHost
{
    /// <summary>The style class an app sets once the user has been in the field, read with <see cref="InvalidClass"/>.</summary>
    public const string TouchedClass = LabelledTextField.TouchedClass;

    /// <summary>The style class an app sets when the value is invalid.</summary>
    public const string InvalidClass = LabelledTextField.InvalidClass;

    /// <summary>The style class an app sets when the value is valid.</summary>
    public const string ValidClass = LabelledTextField.ValidClass;

    /// <summary><c>checkClearOnEdit</c>'s <c>IGNORED_KEYS</c>: these don't edit, so they don't clear.</summary>
    private static readonly HashSet<Key> IgnoredKeys =
    [
        Key.Enter, Key.Tab, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin, Key.LeftAlt, Key.RightAlt, Key.LeftCtrl,
        Key.RightCtrl,
    ];

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>value</c>. Two-way by default, as Ionic's is mutable and the input sets it.</summary>
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<AvonicInput, string?>(nameof(Value), "", defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Ionic <c>type</c>. Two-way by default, as the password toggle sets it.</summary>
    public static readonly StyledProperty<InputType> TypeProperty =
        AvaloniaProperty.Register<AvonicInput, InputType>(nameof(Type), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Ionic <c>label</c>. Beats <see cref="LabelContent"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AvonicInput, string?>(nameof(Label));

    /// <summary>Ionic <c>labelPlacement</c>.</summary>
    public static readonly StyledProperty<InputLabelPlacement> LabelPlacementProperty =
        InputWrapperPanel.LabelPlacementProperty.AddOwner<AvonicInput>();

    /// <summary>Ionic <c>fill</c>. Null is none.</summary>
    public static readonly StyledProperty<InputFill?> FillProperty =
        AvaloniaProperty.Register<AvonicInput, InputFill?>(nameof(Fill));

    /// <summary>Ionic <c>shape</c>. Null is the default.</summary>
    public static readonly StyledProperty<InputShape?> ShapeProperty =
        AvaloniaProperty.Register<AvonicInput, InputShape?>(nameof(Shape));

    /// <summary>Ionic <c>color</c>: the highlight when focused.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicInput>();

    /// <summary>Ionic <c>placeholder</c>.</summary>
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<AvonicInput, string?>(nameof(Placeholder));

    /// <summary>Ionic <c>readonly</c>: focusable, but not editable.</summary>
    public static readonly StyledProperty<bool> ReadonlyProperty =
        AvaloniaProperty.Register<AvonicInput, bool>(nameof(Readonly));

    /// <summary>Ionic <c>required</c>: the native input says it's required to a screen reader.</summary>
    public static readonly StyledProperty<bool> RequiredProperty =
        AvaloniaProperty.Register<AvonicInput, bool>(nameof(Required));

    /// <summary>Ionic <c>maxlength</c>: typing stops there, and the counter counts to it.</summary>
    public static readonly StyledProperty<int?> MaxlengthProperty =
        AvaloniaProperty.Register<AvonicInput, int?>(nameof(Maxlength));

    /// <summary>Ionic <c>clearInput</c>: a button that clears the value, while there's one.</summary>
    public static readonly StyledProperty<bool> ClearInputProperty =
        AvaloniaProperty.Register<AvonicInput, bool>(nameof(ClearInput));

    /// <summary>Ionic <c>clearInputIcon</c>: an <see cref="AvonicIcon.Icon"/>. Null is the theme's.</summary>
    public static readonly StyledProperty<string?> ClearInputIconProperty =
        AvaloniaProperty.Register<AvonicInput, string?>(nameof(ClearInputIcon));

    /// <summary>Ionic <c>clearOnEdit</c>. Null is <c>Type == Password</c>.</summary>
    public static readonly StyledProperty<bool?> ClearOnEditProperty =
        AvaloniaProperty.Register<AvonicInput, bool?>(nameof(ClearOnEdit));

    /// <summary>Ionic <c>counter</c>: <c>length / maxlength</c> under the field, with <see cref="Maxlength"/>.</summary>
    public static readonly StyledProperty<bool> CounterProperty =
        AvaloniaProperty.Register<AvonicInput, bool>(nameof(Counter));

    /// <summary>Ionic <c>counterFormatter</c>: the counter's text from the length and the limit.</summary>
    public static readonly StyledProperty<Func<int, int, string>?> CounterFormatterProperty =
        AvaloniaProperty.Register<AvonicInput, Func<int, int, string>?>(nameof(CounterFormatter));

    /// <summary>Ionic <c>debounce</c>, in milliseconds: how long <see cref="IonInput"/> waits after the last edit.</summary>
    public static readonly StyledProperty<int?> DebounceProperty =
        AvaloniaProperty.Register<AvonicInput, int?>(nameof(Debounce));

    /// <summary>Ionic <c>helperText</c>: under the field, unless the input is invalid.</summary>
    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<AvonicInput, string?>(nameof(HelperText));

    /// <summary>Ionic <c>errorText</c>: under the field while it has the style classes <c>ion-touched</c> and <c>ion-invalid</c>.</summary>
    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<AvonicInput, string?>(nameof(ErrorText));

    /// <summary>Ionic <c>autofocus</c>: the field takes focus when it's loaded.</summary>
    public static readonly StyledProperty<bool> AutofocusProperty =
        AvaloniaProperty.Register<AvonicInput, bool>(nameof(Autofocus));

    /// <summary>Ionic <c>autocapitalize</c>: <c>on</c>, <c>sentences</c>, <c>words</c>, <c>characters</c>, or off.</summary>
    public static readonly StyledProperty<string> AutocapitalizeProperty =
        AvaloniaProperty.Register<AvonicInput, string>(nameof(Autocapitalize), "off");

    /// <summary>Ionic <c>autocorrect</c>: the soft keyboard's suggestions.</summary>
    public static readonly StyledProperty<InputAutocorrect> AutocorrectProperty =
        AvaloniaProperty.Register<AvonicInput, InputAutocorrect>(nameof(Autocorrect));

    /// <summary>Ionic <c>enterkeyhint</c>: the soft keyboard's Enter key.</summary>
    public static readonly StyledProperty<InputEnterkeyhint?> EnterkeyhintProperty =
        AvaloniaProperty.Register<AvonicInput, InputEnterkeyhint?>(nameof(Enterkeyhint));

    /// <summary>Ionic <c>inputmode</c>: which soft keyboard, beating <see cref="Type"/>'s.</summary>
    public static readonly StyledProperty<InputInputmode?> InputmodeProperty =
        AvaloniaProperty.Register<AvonicInput, InputInputmode?>(nameof(Inputmode));

    /// <summary>Ionic's <c>label</c> slot. Shown only when <see cref="Label"/> is null.</summary>
    public static readonly StyledProperty<object?> LabelContentProperty =
        AvaloniaProperty.Register<AvonicInput, object?>(nameof(LabelContent));

    public static readonly StyledProperty<IDataTemplate?> LabelContentTemplateProperty =
        AvaloniaProperty.Register<AvonicInput, IDataTemplate?>(nameof(LabelContentTemplate));

    /// <summary>Ionic's <c>start</c> slot, before the field.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicInput, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicInput, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>end</c> slot, after the field: where an <see cref="AvonicInputPasswordToggle"/> goes.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicInput, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicInput, IDataTemplate?>(nameof(EndContentTemplate));

    /// <summary>
    /// Ionic's internal <c>--highlight-color</c>: the focused, valid or invalid colour, which the theme works out per state, so
    /// that the highlight, the caret, the label and the borders read one value.
    /// </summary>
    public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.Register<AvonicInput, IBrush?>(nameof(HighlightBrush));

    /// <summary><c>renderCounter</c>'s text, or null when there's no counter. For the template.</summary>
    public static readonly DirectProperty<AvonicInput, string?> CounterTextProperty =
        AvaloniaProperty.RegisterDirect<AvonicInput, string?>(nameof(CounterText), input => input.CounterText);

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionInput</c>: each edit by the user, after <see cref="Debounce"/>. Not when <see cref="Value"/> is set in code.</summary>
    public static readonly RoutedEvent<InputInputEventArgs> IonInputEvent =
        RoutedEvent.Register<AvonicInput, InputInputEventArgs>(nameof(IonInput), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionChange</c>: the user committed a change, with Enter or by leaving the field.</summary>
    public static readonly RoutedEvent<InputChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicInput, InputChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionFocus</c>: the field has focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicInput, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>: the field lost focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicInput, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    private readonly NativeTextField _field;
    private readonly LabelledTextField _label;
    private Control? _clearButton;
    private AvonicItem? _item;
    private bool _hasFocus;
    private bool _didInputClearOnEdit;
    private string? _focusedValue;
    private string? _counterText;

    static AvonicInput()
    {
        // The host isn't a tab stop: the native input inside it is.
        FocusableProperty.OverrideDefaultValue<AvonicInput>(false);
    }

    public AvonicInput()
    {
        _field = new NativeTextField(this, ValueProperty, MaxlengthProperty)
        {
            WaitsForComposition = true,
            StaysOutOfView = true,
            KeyDownFirst = true,
            Edited = OnNativeEdited,
            Focused = OnNativeGotFocus,
            Blurred = OnNativeLostFocus,
            KeyDown = OnNativeKeyDown,
            Typing = () => CheckClearOnEdit(Key.None),
        };
        _label = new LabelledTextField(this, _field, SetFocus, UpdateAutomation);

        // The <label for> around the field: a click on it focuses the input, on release, with no keys.
        _ = new NativeButton(this, _label.OnLabelClick, () => false, UpdateIsEffectivelyEnabled);

        // The clear button's onPointerDown preventDefault: a press on it doesn't take focus from the field. Here, on the way
        // down, before the press reaches the button.
        AddHandler(PointerPressedEvent, OnPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);

        _label.FollowClasses();
        UpdateClasses();
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public InputType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public InputLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public InputFill? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public InputShape? Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool Readonly
    {
        get => GetValue(ReadonlyProperty);
        set => SetValue(ReadonlyProperty, value);
    }

    public bool Required
    {
        get => GetValue(RequiredProperty);
        set => SetValue(RequiredProperty, value);
    }

    public int? Maxlength
    {
        get => GetValue(MaxlengthProperty);
        set => SetValue(MaxlengthProperty, value);
    }

    public bool ClearInput
    {
        get => GetValue(ClearInputProperty);
        set => SetValue(ClearInputProperty, value);
    }

    public string? ClearInputIcon
    {
        get => GetValue(ClearInputIconProperty);
        set => SetValue(ClearInputIconProperty, value);
    }

    public bool? ClearOnEdit
    {
        get => GetValue(ClearOnEditProperty);
        set => SetValue(ClearOnEditProperty, value);
    }

    public bool Counter
    {
        get => GetValue(CounterProperty);
        set => SetValue(CounterProperty, value);
    }

    public Func<int, int, string>? CounterFormatter
    {
        get => GetValue(CounterFormatterProperty);
        set => SetValue(CounterFormatterProperty, value);
    }

    public int? Debounce
    {
        get => GetValue(DebounceProperty);
        set => SetValue(DebounceProperty, value);
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

    public bool Autofocus
    {
        get => GetValue(AutofocusProperty);
        set => SetValue(AutofocusProperty, value);
    }

    public string Autocapitalize
    {
        get => GetValue(AutocapitalizeProperty);
        set => SetValue(AutocapitalizeProperty, value);
    }

    public InputAutocorrect Autocorrect
    {
        get => GetValue(AutocorrectProperty);
        set => SetValue(AutocorrectProperty, value);
    }

    public InputEnterkeyhint? Enterkeyhint
    {
        get => GetValue(EnterkeyhintProperty);
        set => SetValue(EnterkeyhintProperty, value);
    }

    public InputInputmode? Inputmode
    {
        get => GetValue(InputmodeProperty);
        set => SetValue(InputmodeProperty, value);
    }

    public object? LabelContent
    {
        get => GetValue(LabelContentProperty);
        set => SetValue(LabelContentProperty, value);
    }

    public IDataTemplate? LabelContentTemplate
    {
        get => GetValue(LabelContentTemplateProperty);
        set => SetValue(LabelContentTemplateProperty, value);
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

    public IBrush? HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public string? CounterText
    {
        get => _counterText;
        private set => SetAndRaise(CounterTextProperty, ref _counterText, value);
    }

    public event EventHandler<InputInputEventArgs>? IonInput
    {
        add => AddHandler(IonInputEvent, value);
        remove => RemoveHandler(IonInputEvent, value);
    }

    public event EventHandler<InputChangeEventArgs>? IonChange
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

    // ── Methods ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>setFocus()</c>: focuses the native input.</summary>
    public void SetFocus() => GetInputElement()?.Focus();

    /// <summary>
    /// Ionic <c>getInputElement()</c>: the native input, a <see cref="TextBox"/>. Ionic waits for the component to load; this
    /// applies the template if it hasn't been, which needs the input in a tree that has its theme. Null before then, or if the
    /// template has no <c>PART_NativeInput</c>.
    /// </summary>
    public TextBox? GetInputElement()
    {
        if (_field.Native is null)
        {
            ApplyTemplate();
        }

        return _field.Native;
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>getValue</c>: null is <c>""</c>.</summary>
    private string CurrentValue => Value ?? "";

    /// <summary><c>hasValue</c>.</summary>
    private bool HasValue => CurrentValue.Length > 0;

    /// <summary><c>hasLabel</c>: a <c>label</c>, even <c>""</c>, or anything in the <c>label</c> slot.</summary>
    private bool HasLabel => Label is not null || LabelContent is not null;

    /// <summary><c>shouldClearOnEdit</c>.</summary>
    private bool ShouldClearOnEdit => ClearOnEdit ?? Type == InputType.Password;

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _field.Attach(e.NameScope.Find<TextBox>("PART_NativeInput"));
        _label.Attach(e.NameScope.Find<Control>("PART_Wrapper"), e.NameScope.Find<ContentPresenter>("PART_LabelContent"));
        _clearButton = e.NameScope.Find<Control>("PART_ClearButton");

        if (_field.Native is not null)
        {
            UpdateNative();
            UpdateWidth();
            _field.Sync();
        }

        if (_clearButton is not null)
        {
            // A native <button>: a click on release, Enter and Space.
            _ = new NativeButton(_clearButton, _ => ClearTextInput(), () => true, static () => { });
        }

        UpdateAutomation();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // hostContext('ion-item') and hostContext('ion-item.ion-color'), at render. Read again here, as a moved input is
        // attached again, and kept, as only an attach can change it.
        _item = this.FindAncestorOfType<AvonicItem>();
        UpdateClasses();
        UpdateWidth();
    }

    /// <summary>
    /// <c>:host-context(ion-item)[slot=start]</c> and <c>[slot=end]</c>: <c>width: auto</c>, as wide as what's in it, where the
    /// native input is as wide as Chromium makes 20 characters. Elsewhere <c>width: 100%</c>, which stretching gives.
    /// </summary>
    private void UpdateWidth()
    {
        if (_field.Native is InputNativeInput native)
        {
            native.SizesToCharacters = LabelledTextField.InItemStartOrEndSlot(this, _item);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _label.OnLoaded(Autofocus);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Avalonia doesn't always raise an exit for a pointer over an input that leaves the tree: measured, it does when the
        // test runs alone, and not after the rest of the suite has used the mouse.
        PseudoClasses.Set(Activatable.HoverPseudoClass, false);
        _item = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            // valueChanged: the native input shows the value, unless it's what it already shows.
            _field.Sync();
            UpdateCounter();
            UpdateClasses();
        }
        else if (change.Property == TypeProperty || change.Property == ReadonlyProperty || change.Property == MaxlengthProperty
                 || change.Property == PlaceholderProperty || change.Property == AutocapitalizeProperty
                 || change.Property == AutocorrectProperty || change.Property == EnterkeyhintProperty
                 || change.Property == InputmodeProperty)
        {
            UpdateNative();
            UpdateCounter();
            UpdateClasses();
        }
        else if (change.Property == CounterProperty || change.Property == CounterFormatterProperty)
        {
            UpdateCounter();
            UpdateClasses();
        }
        else if (change.Property == DebounceProperty)
        {
            _field.ForgetDebounce();
        }
        else if (change.Property == LabelProperty || change.Property == LabelContentProperty || change.Property == HelperTextProperty
                 || change.Property == ErrorTextProperty || change.Property == AutomationProperties.NameProperty)
        {
            UpdateClasses();
            UpdateAutomation();
        }
        else if (change.Property == RequiredProperty)
        {
            UpdateAutomation();
        }
        else if (change.Property == LabelPlacementProperty || change.Property == FillProperty || change.Property == ShapeProperty
                 || change.Property == IsEnabledProperty || change.Property == StartContentProperty
                 || change.Property == EndContentProperty)
        {
            UpdateClasses();
        }
    }

    /// <summary><c>render</c>'s host classes.</summary>
    private void UpdateClasses()
    {
        var item = _item;
        LabelledTextField.SetClasses(PseudoClasses, LabelPlacement, hasValue: HasValue, hasFocus: _hasFocus,
            hasStartEnd: StartContent is not null || EndContent is not null, outline: Fill == InputFill.Outline,
            solid: Fill == InputFill.Solid, round: Shape == InputShape.Round, inItem: item is not null);
        PseudoClasses.Set(":in-item-color", item?.Classes.Contains(":ion-color") == true);
        PseudoClasses.Set(":input-disabled", !IsEnabled);
        PseudoClasses.Set(":readonly", Readonly);
        PseudoClasses.Set(":label-text-wrapper-hidden", !HasLabel);

        // renderBottomContent: only with hint text or a counter.
        PseudoClasses.Set(":has-bottom", !string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText) || CounterText is not null);
    }

    /// <summary>What <c>render</c> puts on the native input.</summary>
    private void UpdateNative()
    {
        if (_field.Native is not { } native)
        {
            return;
        }

        var password = Type == InputType.Password;
        native.PasswordChar = password ? NativeTextField.PasswordMask : default;
        native.IsReadOnly = Readonly;
        NativeTextField.Apply(native, Maxlength, Placeholder, NativeTextField.ContentType(Inputmode, Type), Enterkeyhint, Autocapitalize);
        TextInputOptions.SetIsSensitive(native, password);
        TextInputOptions.SetShowSuggestions(native, Autocorrect == InputAutocorrect.On);
    }

    /// <summary><c>renderCounter</c> and <c>getCounterText</c>.</summary>
    private void UpdateCounter() => CounterText = LabelledTextField.CounterText(Counter, Maxlength, CurrentValue, CounterFormatter);

    // ── The native input ─────────────────────────────────────────────────────────────────────

    /// <summary><c>onInput</c>: the user edited the text.</summary>
    private void OnNativeEdited(string text) => _field.UserEdit(text, EmitInputChange);

    private void OnPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        if (_clearButton is not null && e.Source is Visual source && (source == _clearButton || _clearButton.IsVisualAncestorOf(source)))
        {
            KeepFocus.During(e);
        }
    }

    private void OnNativeGotFocus()
    {
        // onFocus
        _hasFocus = true;
        _focusedValue = Value;
        UpdateClasses();
        RaiseEvent(new RoutedEventArgs(IonFocusEvent, this));
    }

    private void OnNativeLostFocus()
    {
        // onBlur: ionChange if the value isn't what it was at focus, or at the last change.
        _hasFocus = false;
        if (_focusedValue != Value)
        {
            EmitValueChange();
        }

        _didInputClearOnEdit = false;
        UpdateClasses();
        RaiseEvent(new RoutedEventArgs(IonBlurEvent, this));
    }

    private void OnNativeKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _focusedValue != Value)
        {
            // The native change event, which Enter raises when the value has changed since focus or the last change.
            EmitValueChange();
        }

        CheckClearOnEdit(e.Key);
    }

    /// <summary><c>checkClearOnEdit</c>, on key down, before the key is typed. <see cref="Key.None"/> is text with no key.</summary>
    private void CheckClearOnEdit(Key key)
    {
        if (!ShouldClearOnEdit)
        {
            return;
        }

        var ignored = IgnoredKeys.Contains(key);
        if (!_didInputClearOnEdit && HasValue && !ignored)
        {
            _field.SetValueFromUser("");
            EmitInputChange();
        }

        if (!ignored)
        {
            _didInputClearOnEdit = true;
        }
    }

    /// <summary><c>clearTextInput</c>: the clear button's click.</summary>
    private void ClearTextInput()
    {
        if (ClearInput && !Readonly && IsEffectivelyEnabled)
        {
            SetFocus();
        }

        _field.SetValueFromUser("");
        EmitInputChange();
    }

    /// <summary><c>emitValueChange</c>.</summary>
    private void EmitValueChange()
    {
        var value = Value;
        _focusedValue = value;
        RaiseEvent(new InputChangeEventArgs(IonChangeEvent, this, value));
    }

    /// <summary><c>emitInputChange</c>, through <c>debounceEvent</c> when <see cref="Debounce"/> is set.</summary>
    private void EmitInputChange()
    {
        var value = Value;
        _field.Debounced(Debounce, () => RaiseEvent(new InputInputEventArgs(IonInputEvent, this, value)));
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        // @media (any-hover: hover): a mouse or a pen, not a finger.
        PseudoClasses.Set(Activatable.HoverPseudoClass, e.Pointer.Type != PointerType.Touch);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        PseudoClasses.Set(Activatable.HoverPseudoClass, false);
    }

    // ── Item ─────────────────────────────────────────────────────────────────────────────────

    ItemControlType IItemControl.ItemControlType => ItemControlType.Input;

    /// <summary>Ionic's item calls <c>setFocus()</c>, then <c>click()</c>, which does nothing more here.</summary>
    void IItemControl.OnItemTap() => SetFocus();

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>The native input's <c>aria-labelledby</c>, <c>aria-describedby</c> and <c>aria-required</c>.</summary>
    private void UpdateAutomation()
    {
        if (_field.Native is { } native)
        {
            _label.UpdateAutomation(native, Label, LabelContent, HelperText, ErrorText, Required);
        }
    }
}
