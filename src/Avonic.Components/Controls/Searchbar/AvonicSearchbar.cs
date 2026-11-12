using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-searchbar</c>: a search field with a search icon, a clear button and, in md, a cancel button. The native
/// <c>&lt;input&gt;</c> is a <see cref="TextBox"/>, <c>PART_NativeInput</c>. The look is in the theme. See
/// <c>docs/components/ion-searchbar/README.md</c>.
/// </summary>
/// <remarks>
/// <see cref="Value"/> and the text box's <c>Text</c> are kept in step with a guard, so that only the user's edits raise
/// <see cref="IonInput"/>. The native <c>change</c> event, which comes on Enter or blur after the user's edit, is kept apart
/// from Ionic's own check on blur, as the two come in a different order around <see cref="IonBlur"/>.
/// </remarks>
[TemplatePart("PART_NativeInput", typeof(TextBox))]
[TemplatePart("PART_ClearButton", typeof(Control))]
[TemplatePart("PART_CancelButton", typeof(Control))]
[PseudoClasses(":has-value", ":has-focus", ":should-show-clear", ":should-show-cancel", ":searchbar-disabled")]
[PseudoClasses(":animated", ":no-animate", ":left-aligned")]
public class AvonicSearchbar : TemplatedControl, IItemControl
{
    /// <summary><c>onClearInput</c>'s <c>setTimeout</c> of <c>16 * 4</c>: four frames.</summary>
    internal static readonly TimeSpan ClearDelay = TimeSpan.FromMilliseconds(64);

    /// <summary><c>componentDidLoad</c>'s <c>setTimeout</c> before <c>noAnimate</c> turns off.</summary>
    internal static readonly TimeSpan NoAnimateTime = TimeSpan.FromMilliseconds(300);

    /// <summary>What Chromium draws for each character of a password.</summary>
    private const char PasswordMask = '•';

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>value</c>. Two-way by default, as the input sets it.</summary>
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<AvonicSearchbar, string?>(nameof(Value), "", defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Ionic <c>animated</c>. md has no rule on what it changes; see <c>:animated</c>.</summary>
    public static readonly StyledProperty<bool> AnimatedProperty =
        AvaloniaProperty.Register<AvonicSearchbar, bool>(nameof(Animated));

    /// <summary>Ionic <c>autocapitalize</c>.</summary>
    public static readonly StyledProperty<string> AutocapitalizeProperty =
        AvaloniaProperty.Register<AvonicSearchbar, string>(nameof(Autocapitalize), "off");

    /// <summary>Ionic <c>autocorrect</c>.</summary>
    public static readonly StyledProperty<SearchbarAutocorrect> AutocorrectProperty =
        AvaloniaProperty.Register<AvonicSearchbar, SearchbarAutocorrect>(nameof(Autocorrect));

    /// <summary>
    /// Ionic <c>cancelButtonIcon</c>: an <see cref="AvonicIcon.Icon"/>, a name or a source. Null is md's <c>arrow-back-sharp</c>,
    /// which Ionic gives as the icon's SVG, not its name, so it isn't mirrored in RTL; a name given here is, as an arrow's is.
    /// </summary>
    public static readonly StyledProperty<string?> CancelButtonIconProperty =
        AvaloniaProperty.Register<AvonicSearchbar, string?>(nameof(CancelButtonIcon));

    /// <summary>Ionic <c>cancelButtonText</c>: in md, the cancel button's automation name.</summary>
    public static readonly StyledProperty<string?> CancelButtonTextProperty =
        AvaloniaProperty.Register<AvonicSearchbar, string?>(nameof(CancelButtonText), "Cancel");

    /// <summary>Ionic <c>clearIcon</c>. Null is md's <c>close-sharp</c>.</summary>
    public static readonly StyledProperty<string?> ClearIconProperty =
        AvaloniaProperty.Register<AvonicSearchbar, string?>(nameof(ClearIcon));

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicSearchbar>();

    /// <summary>Ionic <c>debounce</c>, in milliseconds: how long <see cref="IonInput"/> waits after each edit.</summary>
    public static readonly StyledProperty<int?> DebounceProperty =
        AvaloniaProperty.Register<AvonicSearchbar, int?>(nameof(Debounce));

    /// <summary>Ionic <c>enterkeyhint</c>.</summary>
    public static readonly StyledProperty<SearchbarEnterkeyhint?> EnterkeyhintProperty =
        AvaloniaProperty.Register<AvonicSearchbar, SearchbarEnterkeyhint?>(nameof(Enterkeyhint));

    /// <summary>Ionic <c>inputmode</c>.</summary>
    public static readonly StyledProperty<SearchbarInputmode?> InputmodeProperty =
        AvaloniaProperty.Register<AvonicSearchbar, SearchbarInputmode?>(nameof(Inputmode));

    /// <summary>Ionic <c>maxlength</c>.</summary>
    public static readonly StyledProperty<int?> MaxlengthProperty =
        AvaloniaProperty.Register<AvonicSearchbar, int?>(nameof(Maxlength));

    /// <summary>Ionic <c>placeholder</c>.</summary>
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<AvonicSearchbar, string?>(nameof(Placeholder), "Search");

    /// <summary>Ionic <c>searchIcon</c>. Null is md's <c>search-sharp</c>.</summary>
    public static readonly StyledProperty<string?> SearchIconProperty =
        AvaloniaProperty.Register<AvonicSearchbar, string?>(nameof(SearchIcon));

    /// <summary>Ionic <c>showCancelButton</c>.</summary>
    public static readonly StyledProperty<SearchbarShowCancelButton> ShowCancelButtonProperty =
        AvaloniaProperty.Register<AvonicSearchbar, SearchbarShowCancelButton>(nameof(ShowCancelButton), SearchbarShowCancelButton.Never);

    /// <summary>Ionic <c>showClearButton</c>.</summary>
    public static readonly StyledProperty<SearchbarShowClearButton> ShowClearButtonProperty =
        AvaloniaProperty.Register<AvonicSearchbar, SearchbarShowClearButton>(nameof(ShowClearButton));

    /// <summary>Ionic <c>type</c>.</summary>
    public static readonly StyledProperty<SearchbarType> TypeProperty =
        AvaloniaProperty.Register<AvonicSearchbar, SearchbarType>(nameof(Type), SearchbarType.Search);

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionInput</c>.</summary>
    public static readonly RoutedEvent<SearchbarInputEventArgs> IonInputEvent =
        RoutedEvent.Register<AvonicSearchbar, SearchbarInputEventArgs>(nameof(IonInput), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionChange</c>.</summary>
    public static readonly RoutedEvent<SearchbarChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicSearchbar, SearchbarChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionFocus</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicSearchbar, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicSearchbar, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionClear</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonClearEvent =
        RoutedEvent.Register<AvonicSearchbar, RoutedEventArgs>(nameof(IonClear), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionCancel</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonCancelEvent =
        RoutedEvent.Register<AvonicSearchbar, RoutedEventArgs>(nameof(IonCancel), RoutingStrategies.Bubble);

    private TextBox? _native;
    private Control? _clearButton;
    private Control? _cancelButton;
    private bool _syncing;
    private bool _inUserEdit;
    private bool _focused;
    private string? _focusedValue;
    private bool _userEdited;
    private string _textAtLastChange = "";
    private bool _leftAligned = true;
    private bool _noAnimate = true;
    private bool _loaded;
    private IDisposable? _debounced;
    private IDisposable? _clearTimeout;
    private IDisposable? _loadTimeout;
    private IDisposable? _positionFrame;

    static AvonicSearchbar()
    {
        // The host isn't a tab stop: the native input inside it is.
        FocusableProperty.OverrideDefaultValue<AvonicSearchbar>(false);

        // role="search"
        AutomationProperties.LandmarkTypeProperty.OverrideDefaultValue<AvonicSearchbar>(AutomationLandmarkType.Search);
    }

    public AvonicSearchbar()
    {
        // The clear button's onPointerDown preventDefault, and the cancel button's onMouseDown and onTouchStart: on the way
        // down, before the press reaches either.
        AddHandler(PointerPressedEvent, OnPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        UpdateClasses();
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool Animated
    {
        get => GetValue(AnimatedProperty);
        set => SetValue(AnimatedProperty, value);
    }

    public string Autocapitalize
    {
        get => GetValue(AutocapitalizeProperty);
        set => SetValue(AutocapitalizeProperty, value);
    }

    public SearchbarAutocorrect Autocorrect
    {
        get => GetValue(AutocorrectProperty);
        set => SetValue(AutocorrectProperty, value);
    }

    public string? CancelButtonIcon
    {
        get => GetValue(CancelButtonIconProperty);
        set => SetValue(CancelButtonIconProperty, value);
    }

    public string? CancelButtonText
    {
        get => GetValue(CancelButtonTextProperty);
        set => SetValue(CancelButtonTextProperty, value);
    }

    public string? ClearIcon
    {
        get => GetValue(ClearIconProperty);
        set => SetValue(ClearIconProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public int? Debounce
    {
        get => GetValue(DebounceProperty);
        set => SetValue(DebounceProperty, value);
    }

    public SearchbarEnterkeyhint? Enterkeyhint
    {
        get => GetValue(EnterkeyhintProperty);
        set => SetValue(EnterkeyhintProperty, value);
    }

    public SearchbarInputmode? Inputmode
    {
        get => GetValue(InputmodeProperty);
        set => SetValue(InputmodeProperty, value);
    }

    public int? Maxlength
    {
        get => GetValue(MaxlengthProperty);
        set => SetValue(MaxlengthProperty, value);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string? SearchIcon
    {
        get => GetValue(SearchIconProperty);
        set => SetValue(SearchIconProperty, value);
    }

    public SearchbarShowCancelButton ShowCancelButton
    {
        get => GetValue(ShowCancelButtonProperty);
        set => SetValue(ShowCancelButtonProperty, value);
    }

    public SearchbarShowClearButton ShowClearButton
    {
        get => GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }

    public SearchbarType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public event EventHandler<SearchbarInputEventArgs>? IonInput
    {
        add => AddHandler(IonInputEvent, value);
        remove => RemoveHandler(IonInputEvent, value);
    }

    public event EventHandler<SearchbarChangeEventArgs>? IonChange
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

    public event EventHandler<RoutedEventArgs>? IonClear
    {
        add => AddHandler(IonClearEvent, value);
        remove => RemoveHandler(IonClearEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonCancel
    {
        add => AddHandler(IonCancelEvent, value);
        remove => RemoveHandler(IonCancelEvent, value);
    }

    // ── Methods ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>setFocus()</c>: focuses the native input.</summary>
    public void SetFocus() => GetInputElement()?.Focus();

    /// <summary>
    /// Ionic <c>getInputElement()</c>: the native input, a <see cref="TextBox"/>. Ionic waits for the component to load; this
    /// applies the template if it hasn't been. Null before it can be, or if the template has no <c>PART_NativeInput</c>.
    /// </summary>
    public TextBox? GetInputElement()
    {
        if (_native is null)
        {
            ApplyTemplate();
        }

        return _native;
    }

    // ── For the theme ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The cancel icon's <c>flipRtl</c>: false for the default, which Ionic imports as SVG with no name to flip by, else the
    /// icon's own rule.
    /// </summary>
    public static readonly Avalonia.Data.Converters.IValueConverter FlipUnlessDefault =
        new Avalonia.Data.Converters.FuncValueConverter<object?, bool?>(value => value is null or "" ? false : null);

    // ── State ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>getValue</c>: null is <c>""</c>.</summary>
    private string CurrentValue => Value ?? "";

    /// <summary><c>shouldShowCancelButton</c>.</summary>
    private bool ShouldShowCancel =>
        ShowCancelButton != SearchbarShowCancelButton.Never && (ShowCancelButton != SearchbarShowCancelButton.Focus || _focused);

    /// <summary><c>shouldShowClearButton</c>.</summary>
    private bool ShouldShowClear =>
        ShowClearButton != SearchbarShowClearButton.Never && (ShowClearButton != SearchbarShowClearButton.Focus || _focused);

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_native is not null)
        {
            _native.PropertyChanged -= OnNativePropertyChanged;
            _native.GotFocus -= OnNativeGotFocus;
            _native.LostFocus -= OnNativeLostFocus;
            _native.RemoveHandler(KeyDownEvent, OnNativeKeyDown);
            _native.RemoveHandler(TextInputEvent, OnNativeTextInput);
            _native.PastingFromClipboard -= OnNativePasting;
            _native.RemoveHandler(RequestBringIntoViewEvent, OnNativeRequestBringIntoView);
        }

        _native = e.NameScope.Find<TextBox>("PART_NativeInput");
        _clearButton = e.NameScope.Find<Control>("PART_ClearButton");
        _cancelButton = e.NameScope.Find<Control>("PART_CancelButton");

        if (_native is not null)
        {
            // Text's own change, which is synchronous: TextChanged is posted, after the guard has gone.
            _native.PropertyChanged += OnNativePropertyChanged;
            _native.GotFocus += OnNativeGotFocus;
            _native.LostFocus += OnNativeLostFocus;

            // Enter's change and Escape's clear are default actions, after the key's listeners, and not if one handled it, as
            // preventDefault() stops them. The text box itself handles neither.
            _native.AddHandler(KeyDownEvent, OnNativeKeyDown, RoutingStrategies.Bubble);
            _native.AddHandler(TextInputEvent, OnNativeTextInput, RoutingStrategies.Tunnel, handledEventsToo: true);
            _native.PastingFromClipboard += OnNativePasting;

            // Chromium never scrolls to an input that isn't focused; Avalonia's presenter brings its caret into view anyway.
            _native.AddHandler(RequestBringIntoViewEvent, OnNativeRequestBringIntoView);
            UpdateNative();
            SyncNativeText();
        }

        if (_clearButton is not null)
        {
            // A native <button>: a click on release, Enter and Space. onClick: onClearInput(true).
            _ = new NativeButton(_clearButton, _ => OnClearInput(shouldFocus: true, then: null), () => true, static () => { });
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // connectedCallback: emitStyle, which an item or a toolbar takes as a class.
        RaiseEvent(new IonStyleEventArgs(new Dictionary<string, bool> { ["searchbar"] = true }));
        UpdateClasses();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // componentDidLoad, once: positionElements, and noAnimate off after 300 ms.
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        PositionElements();
        _loadTimeout = AvonicConfig.Schedule(() =>
        {
            _loadTimeout = null;
            _noAnimate = false;
            UpdateClasses();
        }, NoAnimateTime);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // disconnectedCallback: the timeouts are cleared. A pending clear never happens, and a cancel waiting on it never ends.
        // A requested frame isn't, so positionElements still runs.
        _loadTimeout?.Dispose();
        _loadTimeout = null;
        _clearTimeout?.Dispose();
        _clearTimeout = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            // valueChanged: the native input shows the value, unless it's what it already shows.
            SyncNativeText();
            UpdateClasses();
        }
        else if (change.Property == TypeProperty || change.Property == MaxlengthProperty || change.Property == PlaceholderProperty
                 || change.Property == AutocapitalizeProperty || change.Property == AutocorrectProperty
                 || change.Property == EnterkeyhintProperty || change.Property == InputmodeProperty)
        {
            UpdateNative();
        }
        else if (change.Property == DebounceProperty)
        {
            // debounceChanged replaces the emitter, and the old one's timer still runs: a pending ionInput is still raised.
            _debounced = null;
        }
        else if (change.Property == ShowCancelButtonProperty)
        {
            // showCancelButtonChanged: positionElements on the next frame.
            UpdateClasses();
            _positionFrame?.Dispose();
            _positionFrame = AvonicConfig.RequestFrame(this, () =>
            {
                _positionFrame = null;
                PositionElements();
            });
        }
        else if (change.Property == ShowClearButtonProperty || change.Property == AnimatedProperty
                 || change.Property == IsEnabledProperty)
        {
            UpdateClasses();
        }
    }

    /// <summary><c>render</c>'s host classes.</summary>
    private void UpdateClasses()
    {
        var animated = Animated && AvonicConfig.Animated;
        PseudoClasses.Set(":animated", animated);
        PseudoClasses.Set(":no-animate", animated && _noAnimate);
        PseudoClasses.Set(":has-value", CurrentValue.Length > 0);
        PseudoClasses.Set(":left-aligned", _leftAligned);
        PseudoClasses.Set(":has-focus", _focused);
        PseudoClasses.Set(":should-show-clear", ShouldShowClear);
        PseudoClasses.Set(":should-show-cancel", ShouldShowCancel);
        PseudoClasses.Set(":searchbar-disabled", !IsEnabled);
    }

    /// <summary><c>positionElements</c>: in md, only <c>shouldAlignLeft</c>, which the next render shows.</summary>
    private void PositionElements()
    {
        _leftAligned = !Animated || CurrentValue.Trim().Length > 0 || _focused;
        UpdateClasses();
    }

    /// <summary>What <c>render</c> puts on the native input.</summary>
    private void UpdateNative()
    {
        if (_native is not { } native)
        {
            return;
        }

        var password = Type == SearchbarType.Password;
        native.PasswordChar = password ? PasswordMask : default;
        // A negative maxlength is invalid in HTML, so no limit; 0 allows nothing, which OnNativeTextInput keeps out.
        native.MaxLength = Maxlength is > 0 and var max ? max : 0;
        native.PlaceholderText = Placeholder ?? "";

        TextInputOptions.SetContentType(native, ContentType());
        TextInputOptions.SetIsSensitive(native, password);
        TextInputOptions.SetReturnKeyType(native, Enterkeyhint switch
        {
            SearchbarEnterkeyhint.Done => TextInputReturnKeyType.Done,
            SearchbarEnterkeyhint.Enter => TextInputReturnKeyType.Return,
            SearchbarEnterkeyhint.Go => TextInputReturnKeyType.Go,
            SearchbarEnterkeyhint.Next => TextInputReturnKeyType.Next,
            SearchbarEnterkeyhint.Previous => TextInputReturnKeyType.Previous,
            SearchbarEnterkeyhint.Search => TextInputReturnKeyType.Search,
            SearchbarEnterkeyhint.Send => TextInputReturnKeyType.Send,
            _ => TextInputReturnKeyType.Default,
        });

        // autocapitalize === 'default' is no attribute, which is off.
        var capitalize = Autocapitalize?.Trim().ToLowerInvariant();
        TextInputOptions.SetAutoCapitalization(native, capitalize is "on" or "sentences" or "words");
        TextInputOptions.SetUppercase(native, capitalize == "characters");
        TextInputOptions.SetShowSuggestions(native, Autocorrect == SearchbarAutocorrect.On);
    }

    /// <summary><c>inputmode</c> if it's set, as it beats the type's keyboard, else <c>type</c>'s.</summary>
    private TextInputContentType ContentType() => Inputmode switch
    {
        SearchbarInputmode.Decimal => TextInputContentType.Number,
        SearchbarInputmode.Email => TextInputContentType.Email,
        SearchbarInputmode.Numeric or SearchbarInputmode.Tel => TextInputContentType.Digits,
        SearchbarInputmode.Search => TextInputContentType.Search,
        SearchbarInputmode.Text => TextInputContentType.Normal,
        SearchbarInputmode.Url => TextInputContentType.Url,
        _ => Type switch
        {
            SearchbarType.Email => TextInputContentType.Email,
            SearchbarType.Number => TextInputContentType.Number,
            SearchbarType.Password => TextInputContentType.Password,
            SearchbarType.Search => TextInputContentType.Search,
            SearchbarType.Tel => TextInputContentType.Digits,
            SearchbarType.Url => TextInputContentType.Url,
            _ => TextInputContentType.Normal,
        },
    };

    // ── The native input ─────────────────────────────────────────────────────────────────────

    /// <summary><c>valueChanged</c>: assigning the native input's value, which lets an <c>ionInput</c> handler replace it.</summary>
    private void SyncNativeText()
    {
        // During an IME's composition too: searchbar.js's valueChanged, unlike ion-input's, doesn't check isComposing.
        if (_native is not { } native || _syncing || native.Text == CurrentValue)
        {
            return;
        }

        _syncing = true;
        try
        {
            native.Text = CurrentValue;
            native.CaretIndex = CurrentValue.Length;
        }
        finally
        {
            _syncing = false;
        }

        if (_inUserEdit)
        {
            // Assigned during the user's edit, which the text box finishes by placing its caret after what was typed.
            // Chromium leaves the caret at the end of an assigned value.
            Dispatcher.UIThread.Post(() => native.CaretIndex = native.Text?.Length ?? 0, DispatcherPriority.Send);
        }
    }

    private void OnNativeRequestBringIntoView(object? sender, RequestBringIntoViewEventArgs e)
    {
        if (_native is { IsKeyboardFocusWithin: false })
        {
            e.Handled = true;
        }
    }

    /// <summary><c>onInput</c>: the user edited the text.</summary>
    private void OnNativePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.TextProperty || _syncing || _native is not { } native)
        {
            return;
        }

        OnUserEdit(native.Text ?? "");
    }

    private void OnUserEdit(string text)
    {
        _userEdited = true;
        _inUserEdit = true;
        try
        {
            _syncing = true;
            try
            {
                SetCurrentValue(ValueProperty, text);
            }
            finally
            {
                _syncing = false;
            }

            // A handler of the value's change can have replaced it, which the native input then shows.
            SyncNativeText();
            EmitInputChange();
        }
        finally
        {
            _inUserEdit = false;
        }
    }

    private void OnNativeTextInput(object? sender, TextInputEventArgs e)
    {
        // maxlength="0": nothing can be typed, which Avalonia's MaxLength of 0, no limit, doesn't say.
        if (Maxlength == 0)
        {
            e.Handled = true;
        }
    }

    private void OnNativePasting(object? sender, RoutedEventArgs e)
    {
        if (Maxlength == 0)
        {
            e.Handled = true;
        }
    }

    private void OnNativeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // The native change event, which Enter raises after the user's edit.
            NativeChange();
        }
        else if (e.Key == Key.Escape && Type == SearchbarType.Search && CurrentValue.Length > 0 && _native is { IsReadOnly: false })
        {
            // Chromium's search field: Escape clears it, as an edit. The key goes on, as it does in the page.
            OnUserEdit("");
        }
    }

    private void OnNativeGotFocus(object? sender, FocusChangedEventArgs e)
    {
        // The native input's own record for its change event.
        _userEdited = false;
        _textAtLastChange = CurrentValue;

        // onFocus
        _focused = true;
        _focusedValue = Value;
        RaiseEvent(new RoutedEventArgs(IonFocusEvent, this));
        PositionElements();
    }

    private void OnNativeLostFocus(object? sender, FocusChangedEventArgs e)
    {
        // Chromium raises the native change before blur.
        NativeChange();

        // onBlur: ionBlur, then ionChange if the value isn't what it was at focus, or at the last change.
        _focused = false;
        RaiseEvent(new RoutedEventArgs(IonBlurEvent, this));
        PositionElements();
        if (_focusedValue != Value)
        {
            EmitValueChange();
        }

        _focusedValue = null;
    }

    /// <summary>
    /// The native <c>change</c> event, then <c>onChange</c>: on Enter or blur, once the user has edited, if the text isn't what
    /// it was at focus or at the last change. Measured: a value set in code with no edit raises none, and an edit undone none.
    /// </summary>
    private void NativeChange()
    {
        if (!_userEdited || CurrentValue == _textAtLastChange)
        {
            return;
        }

        _userEdited = false;
        _textAtLastChange = CurrentValue;
        EmitValueChange();
    }

    /// <summary><c>emitValueChange</c>.</summary>
    private void EmitValueChange()
    {
        var value = Value;
        _focusedValue = value;
        RaiseEvent(new SearchbarChangeEventArgs(IonChangeEvent, this, value));
    }

    /// <summary><c>emitInputChange</c>, through <c>debounceEvent</c> when <see cref="Debounce"/> is set.</summary>
    private void EmitInputChange()
    {
        var value = Value;
        if (Debounce is not { } wait)
        {
            RaiseEvent(new SearchbarInputEventArgs(IonInputEvent, this, value));
            return;
        }

        // debounce: each call restarts the timer, and the last call's detail is what's raised.
        _debounced?.Dispose();
        IDisposable? handle = null;
        handle = AvonicConfig.Schedule(() =>
        {
            if (_debounced == handle)
            {
                _debounced = null;
            }

            RaiseEvent(new SearchbarInputEventArgs(IonInputEvent, this, value));
        }, TimeSpan.FromMilliseconds(Math.Max(0, wait)));
        _debounced = handle;
    }

    // ── The buttons ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>onClearInput</c>: <c>ionClear</c> at once, then, four frames later, the value cleared with an <c>ionInput</c>, and the
    /// input focused if <paramref name="shouldFocus"/>. A second call restarts the wait. <paramref name="then"/> is the promise's
    /// continuation, which a detach drops with the timeout.
    /// </summary>
    private void OnClearInput(bool shouldFocus, Action? then)
    {
        _clearTimeout?.Dispose();
        _clearTimeout = null;
        RaiseEvent(new RoutedEventArgs(IonClearEvent, this));

        _clearTimeout = AvonicConfig.Schedule(() =>
        {
            _clearTimeout = null;
            var value = CurrentValue;
            if (value != "")
            {
                SetCurrentValue(ValueProperty, "");
                EmitInputChange();
                if (shouldFocus && !_focused)
                {
                    SetFocus();
                    _focusedValue = value;
                }
            }

            then?.Invoke();
        }, ClearDelay);
    }

    /// <summary><c>onCancelSearchbar</c>, on a press of the cancel button.</summary>
    private void OnCancelSearchbar()
    {
        RaiseEvent(new RoutedEventArgs(IonCancelEvent, this));

        // The values before the input is cleared.
        var value = CurrentValue;
        var focused = _focused;
        OnClearInput(shouldFocus: false, then: () =>
        {
            if (value != "" && !focused)
            {
                EmitValueChange();
            }

            // nativeInput.blur()
            if (_native is { IsFocused: true } && TopLevel.GetTopLevel(this)?.FocusManager is { } focusManager)
            {
                focusManager.Focus(null);
            }
        });
    }

    private void OnPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual source)
        {
            return;
        }

        if (_clearButton is not null && (source == _clearButton || _clearButton.IsVisualAncestorOf(source)))
        {
            // onPointerDown: preventDefault, so the field keeps focus, or doesn't take it.
            KeepFocus.During(e);
        }
        else if (!e.Handled && _cancelButton is not null && (source == _cancelButton || _cancelButton.IsVisualAncestorOf(source)))
        {
            // onMouseDown, onTouchStart: preventDefault and stopPropagation, then cancel. Any button, as mousedown is. A handled
            // press moves no focus, which is preventDefault's part. Not if an ancestor stopped the press on its way down.
            e.Handled = true;
            OnCancelSearchbar();
        }
    }

    // ── Item ─────────────────────────────────────────────────────────────────────────────────

    ItemControlType IItemControl.ItemControlType => ItemControlType.Searchbar;

    /// <summary>Never called: Ionic's <c>getFirstInteractive</c> doesn't look for a searchbar.</summary>
    void IItemControl.OnItemTap()
    {
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary><c>role="search"</c>: a landmark around the input and its buttons, where a control's default peer is none.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new SearchbarAutomationPeer(this);

    private sealed class SearchbarAutomationPeer(AvonicSearchbar owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}
