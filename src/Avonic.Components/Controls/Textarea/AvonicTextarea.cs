using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-textarea</c>: a multi-line text field with a label, start and end slots, helper and error text and a counter.
/// The native <c>&lt;textarea&gt;</c> is a <see cref="TextareaNativeInput"/>, <c>PART_NativeInput</c>. The look is in the theme.
/// See <c>docs/components/ion-textarea/README.md</c>.
/// </summary>
/// <remarks>
/// <see cref="Value"/> and the field's <c>Text</c> are kept in step with a guard, so that only the user's edits raise
/// <see cref="IonInput"/>, as only the native <c>input</c> event does in Ionic. A click on the wrapper is the <c>&lt;label for&gt;</c>'s
/// activation, through <see cref="NativeButton"/> with no keys.
/// </remarks>
[TemplatePart("PART_NativeInput", typeof(TextBox))]
[TemplatePart("PART_Wrapper", typeof(Control))]
[TemplatePart("PART_LabelContent", typeof(ContentPresenter))]
[PseudoClasses(":has-value", ":has-focus", ":label-floating", ":in-item", ":textarea-disabled", ":readonly", ":auto-grow", ":cols")]
[PseudoClasses(":fill-outline", ":fill-solid", ":shape-round", ":label-text-wrapper-hidden", ":has-bottom", ":ion-hover")]
[PseudoClasses(":label-placement-start", ":label-placement-end", ":label-placement-fixed", ":label-placement-floating", ":label-placement-stacked")]
public class AvonicTextarea : TemplatedControl, IItemControl
{
    /// <summary>The style class an app sets once the user has been in the field, read with <see cref="InvalidClass"/>.</summary>
    public const string TouchedClass = "ion-touched";

    /// <summary>The style class an app sets when the value is invalid.</summary>
    public const string InvalidClass = "ion-invalid";

    /// <summary>The style class an app sets when the value is valid.</summary>
    public const string ValidClass = "ion-valid";

    /// <summary>
    /// <c>checkClearOnEdit</c>'s <c>IGNORED_KEYS</c>: these don't edit, so they don't clear. Unlike the input's, Enter isn't one,
    /// as it types a line break.
    /// </summary>
    private static readonly HashSet<Key> IgnoredKeys =
    [
        Key.Tab, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin, Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl,
    ];

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>value</c>. Two-way by default, as Ionic's is mutable and the textarea sets it.</summary>
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<AvonicTextarea, string?>(nameof(Value), "", defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Ionic <c>label</c>. Beats <see cref="LabelContent"/>.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<AvonicTextarea, string?>(nameof(Label));

    /// <summary>Ionic <c>labelPlacement</c>.</summary>
    public static readonly StyledProperty<TextareaLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<AvonicTextarea, TextareaLabelPlacement>(nameof(LabelPlacement));

    /// <summary>Ionic <c>fill</c>. Null is none.</summary>
    public static readonly StyledProperty<TextareaFill?> FillProperty =
        AvaloniaProperty.Register<AvonicTextarea, TextareaFill?>(nameof(Fill));

    /// <summary>Ionic <c>shape</c>. Null is the default.</summary>
    public static readonly StyledProperty<TextareaShape?> ShapeProperty =
        AvaloniaProperty.Register<AvonicTextarea, TextareaShape?>(nameof(Shape));

    /// <summary>Ionic <c>color</c>: the highlight when focused.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicTextarea>();

    /// <summary>Ionic <c>placeholder</c>.</summary>
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<AvonicTextarea, string?>(nameof(Placeholder));

    /// <summary>Ionic <c>readonly</c>: focusable, but not editable.</summary>
    public static readonly StyledProperty<bool> ReadonlyProperty =
        AvaloniaProperty.Register<AvonicTextarea, bool>(nameof(Readonly));

    /// <summary>Ionic <c>maxlength</c>: typing stops there, and the counter counts to it.</summary>
    public static readonly StyledProperty<int?> MaxlengthProperty =
        AvaloniaProperty.Register<AvonicTextarea, int?>(nameof(Maxlength));

    /// <summary>Ionic <c>clearOnEdit</c>: the first edit after focus clears the value.</summary>
    public static readonly StyledProperty<bool> ClearOnEditProperty =
        AvaloniaProperty.Register<AvonicTextarea, bool>(nameof(ClearOnEdit));

    /// <summary>Ionic <c>counter</c>: <c>length / maxlength</c> under the field, with <see cref="Maxlength"/>.</summary>
    public static readonly StyledProperty<bool> CounterProperty =
        AvaloniaProperty.Register<AvonicTextarea, bool>(nameof(Counter));

    /// <summary>Ionic <c>counterFormatter</c>: the counter's text from the length and the limit.</summary>
    public static readonly StyledProperty<Func<int, int, string>?> CounterFormatterProperty =
        AvaloniaProperty.Register<AvonicTextarea, Func<int, int, string>?>(nameof(CounterFormatter));

    /// <summary>Ionic <c>debounce</c>, in milliseconds: how long <see cref="IonInput"/> waits after the last edit.</summary>
    public static readonly StyledProperty<int?> DebounceProperty =
        AvaloniaProperty.Register<AvonicTextarea, int?>(nameof(Debounce));

    /// <summary>Ionic <c>helperText</c>: under the field, unless it's invalid.</summary>
    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<AvonicTextarea, string?>(nameof(HelperText));

    /// <summary>Ionic <c>errorText</c>: under the field while it has the style classes <c>ion-touched</c> and <c>ion-invalid</c>.</summary>
    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<AvonicTextarea, string?>(nameof(ErrorText));

    /// <summary>Ionic <c>autofocus</c>: the field takes focus when it's loaded.</summary>
    public static readonly StyledProperty<bool> AutofocusProperty =
        AvaloniaProperty.Register<AvonicTextarea, bool>(nameof(Autofocus));

    /// <summary>Ionic <c>autocapitalize</c>: <c>on</c>, <c>sentences</c>, <c>words</c>, <c>characters</c>, or none.</summary>
    public static readonly StyledProperty<string> AutocapitalizeProperty =
        AvaloniaProperty.Register<AvonicTextarea, string>(nameof(Autocapitalize), "none");

    /// <summary>Ionic <c>enterkeyhint</c>: the soft keyboard's Enter key.</summary>
    public static readonly StyledProperty<TextareaEnterkeyhint?> EnterkeyhintProperty =
        AvaloniaProperty.Register<AvonicTextarea, TextareaEnterkeyhint?>(nameof(Enterkeyhint));

    /// <summary>Ionic <c>inputmode</c>: which soft keyboard.</summary>
    public static readonly StyledProperty<TextareaInputmode?> InputmodeProperty =
        AvaloniaProperty.Register<AvonicTextarea, TextareaInputmode?>(nameof(Inputmode));

    /// <summary>Ionic <c>rows</c>: how many lines high the field is. Null is HTML's 2.</summary>
    public static readonly StyledProperty<int?> RowsProperty =
        AvaloniaProperty.Register<AvonicTextarea, int?>(nameof(Rows));

    /// <summary>Ionic <c>cols</c>: without <see cref="AutoGrow"/>, the host is as wide as its content, the field this many columns.</summary>
    public static readonly StyledProperty<int?> ColsProperty =
        AvaloniaProperty.Register<AvonicTextarea, int?>(nameof(Cols));

    /// <summary>Ionic <c>autoGrow</c>: the field grows and shrinks with its lines.</summary>
    public static readonly StyledProperty<bool> AutoGrowProperty =
        AvaloniaProperty.Register<AvonicTextarea, bool>(nameof(AutoGrow));

    /// <summary>Ionic <c>wrap</c>. Null wraps, as <c>Soft</c> does.</summary>
    public static readonly StyledProperty<TextareaWrap?> WrapProperty =
        AvaloniaProperty.Register<AvonicTextarea, TextareaWrap?>(nameof(Wrap));

    /// <summary>Ionic's <c>label</c> slot. Shown only when <see cref="Label"/> is null.</summary>
    public static readonly StyledProperty<object?> LabelContentProperty =
        AvaloniaProperty.Register<AvonicTextarea, object?>(nameof(LabelContent));

    public static readonly StyledProperty<IDataTemplate?> LabelContentTemplateProperty =
        AvaloniaProperty.Register<AvonicTextarea, IDataTemplate?>(nameof(LabelContentTemplate));

    /// <summary>Ionic's <c>start</c> slot, before the field.</summary>
    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicTextarea, object?>(nameof(StartContent));

    public static readonly StyledProperty<IDataTemplate?> StartContentTemplateProperty =
        AvaloniaProperty.Register<AvonicTextarea, IDataTemplate?>(nameof(StartContentTemplate));

    /// <summary>Ionic's <c>end</c> slot, after the field.</summary>
    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicTextarea, object?>(nameof(EndContent));

    public static readonly StyledProperty<IDataTemplate?> EndContentTemplateProperty =
        AvaloniaProperty.Register<AvonicTextarea, IDataTemplate?>(nameof(EndContentTemplate));

    /// <summary>
    /// Ionic's internal <c>--highlight-color</c>: the focused, valid or invalid colour, which the theme works out per state, so
    /// that the highlight, the caret, the label and the borders read one value.
    /// </summary>
    public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.Register<AvonicTextarea, IBrush?>(nameof(HighlightBrush));

    /// <summary><c>renderCounter</c>'s text, or null when there's no counter. For the template.</summary>
    public static readonly DirectProperty<AvonicTextarea, string?> CounterTextProperty =
        AvaloniaProperty.RegisterDirect<AvonicTextarea, string?>(nameof(CounterText), textarea => textarea.CounterText);

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionInput</c>: each edit by the user, after <see cref="Debounce"/>. Not when <see cref="Value"/> is set in code.</summary>
    public static readonly RoutedEvent<TextareaInputEventArgs> IonInputEvent =
        RoutedEvent.Register<AvonicTextarea, TextareaInputEventArgs>(nameof(IonInput), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionChange</c>: the user changed the value and left the field.</summary>
    public static readonly RoutedEvent<TextareaChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicTextarea, TextareaChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionFocus</c>: the field has focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicTextarea, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionBlur</c>: the field lost focus.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicTextarea, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    // ── For the theme ────────────────────────────────────────────────────────────────────────

    /// <summary>The placement as <see cref="InputWrapperPanel"/> takes it, which lays out the label the same five ways.</summary>
    public static readonly IValueConverter PlacementConverter =
        new FuncValueConverter<TextareaLabelPlacement, InputLabelPlacement>(placement => placement switch
        {
            TextareaLabelPlacement.End => InputLabelPlacement.End,
            TextareaLabelPlacement.Fixed => InputLabelPlacement.Fixed,
            TextareaLabelPlacement.Floating => InputLabelPlacement.Floating,
            TextareaLabelPlacement.Stacked => InputLabelPlacement.Stacked,
            _ => InputLabelPlacement.Start,
        });

    /// <summary>
    /// <c>padding-top: var(--padding-top)</c> and <c>padding-bottom: var(--padding-bottom)</c>, on the field, a row's label and the
    /// slots. The values are the two variables, then the default for an unset bottom, then for an unset top (0 if there's none,
    /// as in a column). The <c>ConverterParameter</c> is what's added above: a slotted child's <c>margin-top</c> in a column.
    /// </summary>
    public static readonly IMultiValueConverter VerticalPaddingConverter = new VerticalPaddingConverterImpl();

    private static double Number(object? value) => value is double number && double.IsFinite(number) ? number : 0;

    private static double NumberOr(object? value, double fallback) =>
        value is double number && double.IsFinite(number) ? number : fallback;

    private sealed class VerticalPaddingConverterImpl : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            var extra = double.TryParse(parameter as string, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : 0;
            var top = NumberOr(values.ElementAtOrDefault(0), Number(values.ElementAtOrDefault(3)));
            var bottom = NumberOr(values.ElementAtOrDefault(1), Number(values.ElementAtOrDefault(2)));
            return new Thickness(0, top + extra, 0, bottom);
        }
    }

    private TextareaNativeInput? _native;
    private Control? _wrapper;
    private ContentPresenter? _labelContent;
    private bool _syncing;
    private bool _inUserEdit;
    private bool _autofocused;
    private AvonicItem? _item;
    private bool _hasFocus;
    private bool _didTextareaClearOnEdit;
    private string? _focusedValue;
    private string? _counterText;
    private IDisposable? _debounced;

    static AvonicTextarea()
    {
        // The host isn't a tab stop: the native textarea inside it is.
        FocusableProperty.OverrideDefaultValue<AvonicTextarea>(false);
    }

    public AvonicTextarea()
    {
        // The <label for> around the field: a click on it focuses the textarea, on release, with no keys.
        _ = new NativeButton(this, OnLabelClick, () => false, UpdateIsEffectivelyEnabled);

        Classes.CollectionChanged += OnClassesChanged;
        UpdateClasses();
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public TextareaLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    public TextareaFill? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public TextareaShape? Shape
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

    public int? Maxlength
    {
        get => GetValue(MaxlengthProperty);
        set => SetValue(MaxlengthProperty, value);
    }

    public bool ClearOnEdit
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

    public TextareaEnterkeyhint? Enterkeyhint
    {
        get => GetValue(EnterkeyhintProperty);
        set => SetValue(EnterkeyhintProperty, value);
    }

    public TextareaInputmode? Inputmode
    {
        get => GetValue(InputmodeProperty);
        set => SetValue(InputmodeProperty, value);
    }

    public int? Rows
    {
        get => GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public int? Cols
    {
        get => GetValue(ColsProperty);
        set => SetValue(ColsProperty, value);
    }

    public bool AutoGrow
    {
        get => GetValue(AutoGrowProperty);
        set => SetValue(AutoGrowProperty, value);
    }

    public TextareaWrap? Wrap
    {
        get => GetValue(WrapProperty);
        set => SetValue(WrapProperty, value);
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

    public event EventHandler<TextareaInputEventArgs>? IonInput
    {
        add => AddHandler(IonInputEvent, value);
        remove => RemoveHandler(IonInputEvent, value);
    }

    public event EventHandler<TextareaChangeEventArgs>? IonChange
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

    /// <summary>Ionic <c>setFocus()</c>: focuses the native textarea.</summary>
    public void SetFocus() => GetInputElement()?.Focus();

    /// <summary>
    /// Ionic <c>getInputElement()</c>: the native textarea. Ionic waits for the component to load; this applies the template if it
    /// hasn't been, which needs the textarea in a tree that has its theme. Null before then, or if the template has no
    /// <c>PART_NativeInput</c>.
    /// </summary>
    public TextBox? GetInputElement()
    {
        if (_native is null)
        {
            ApplyTemplate();
        }

        return _native;
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>getValue</c>: null is <c>""</c>.</summary>
    private string CurrentValue => Value ?? "";

    /// <summary><c>hasValue</c>.</summary>
    private bool HasValue => CurrentValue.Length > 0;

    /// <summary><c>hasLabel</c>: a <c>label</c>, even <c>""</c>, or anything in the <c>label</c> slot.</summary>
    private bool HasLabel => Label is not null || LabelContent is not null;

    /// <summary><c>checkInvalidState</c>: the app has marked it both touched and invalid.</summary>
    private bool IsInvalid => Classes.Contains(TouchedClass) && Classes.Contains(InvalidClass);

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_native is not null)
        {
            _native.PropertyChanged -= OnNativePropertyChanged;
            _native.RemoveHandler(TextInputEvent, OnNativeTextInput);
            _native.PastingFromClipboard -= OnNativePasting;
            _native.GotFocus -= OnNativeGotFocus;
            _native.LostFocus -= OnNativeLostFocus;
            _native.RemoveHandler(KeyDownEvent, OnNativeKeyDown);
            _native.RemoveHandler(RequestBringIntoViewEvent, OnNativeRequestBringIntoView);
        }

        _native = e.NameScope.Find<TextareaNativeInput>("PART_NativeInput");
        _wrapper = e.NameScope.Find<Control>("PART_Wrapper");
        if (_labelContent is not null)
        {
            _labelContent.PropertyChanged -= OnLabelContentPropertyChanged;
        }

        _labelContent = e.NameScope.Find<ContentPresenter>("PART_LabelContent");
        if (_labelContent is not null)
        {
            // A templated label's element is made after the template's applied: name the field again then.
            _labelContent.PropertyChanged += OnLabelContentPropertyChanged;
        }

        if (_native is not null)
        {
            // Text's own change, which is synchronous: TextChanged is posted, after the guard has gone.
            _native.PropertyChanged += OnNativePropertyChanged;
            _native.GotFocus += OnNativeGotFocus;
            _native.LostFocus += OnNativeLostFocus;

            // Before the text box types the key: checkClearOnEdit runs on keydown, before the character is in.
            _native.AddHandler(KeyDownEvent, OnNativeKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);

            // A soft keyboard can type with no key down, where Chromium's keydown says "Unidentified": that clears too.
            _native.AddHandler(TextInputEvent, OnNativeTextInput, RoutingStrategies.Tunnel, handledEventsToo: true);
            _native.PastingFromClipboard += OnNativePasting;

            // Avalonia's text presenter brings its caret into view whenever the caret moves, focused or not. Chromium never
            // scrolls the page to a textarea that isn't focused.
            _native.AddHandler(RequestBringIntoViewEvent, OnNativeRequestBringIntoView);
            UpdateNative();
            SyncNativeText();
        }

        UpdateAutomation();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // hostContext('ion-item'), at render: read again on each attach, as only an attach can change it.
        _item = this.FindAncestorOfType<AvonicItem>();
        UpdateClasses();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // autoFocus: Chromium focuses it once, when it's first in the document, unless something else already has focus.
        if (Autofocus && !_autofocused)
        {
            _autofocused = true;
            if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is null)
            {
                SetFocus();
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Avalonia doesn't always raise an exit for a pointer over a control that leaves the tree.
        PseudoClasses.Set(Activatable.HoverPseudoClass, false);
        _item = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            // valueChanged: the native textarea shows the value, unless it's what it already shows. runAutoGrow is the field's
            // own measure.
            SyncNativeText();
            UpdateCounter();
            UpdateClasses();
        }
        else if (change.Property == ReadonlyProperty || change.Property == MaxlengthProperty || change.Property == PlaceholderProperty
                 || change.Property == AutocapitalizeProperty || change.Property == EnterkeyhintProperty
                 || change.Property == InputmodeProperty || change.Property == RowsProperty || change.Property == ColsProperty
                 || change.Property == AutoGrowProperty || change.Property == WrapProperty)
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
            // debounceChanged replaces the emitter, and the old one's timer still runs: a pending ionInput is still raised.
            _debounced = null;
        }
        else if (change.Property == LabelProperty || change.Property == LabelContentProperty || change.Property == HelperTextProperty
                 || change.Property == ErrorTextProperty || change.Property == AutomationProperties.NameProperty)
        {
            UpdateClasses();
            UpdateAutomation();
        }
        else if (change.Property == LabelPlacementProperty || change.Property == FillProperty || change.Property == ShapeProperty
                 || change.Property == IsEnabledProperty || change.Property == StartContentProperty
                 || change.Property == EndContentProperty)
        {
            UpdateClasses();
        }
    }

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // The MutationObserver on class: a new aria-describedby. Only the two classes checkInvalidState reads.
        if (Mentions(e.NewItems) || Mentions(e.OldItems) || e.Action == NotifyCollectionChangedAction.Reset)
        {
            UpdateAutomation();
        }
    }

    private static bool Mentions(System.Collections.IList? classes) =>
        classes?.OfType<string>().Any(name => name is TouchedClass or InvalidClass) == true;

    /// <summary><c>render</c>'s host classes, and the reflected <c>cols</c> and <c>auto-grow</c> attributes.</summary>
    private void UpdateClasses()
    {
        var placement = LabelPlacement;
        var hasStartEnd = StartContent is not null || EndContent is not null;

        PseudoClasses.Set(":has-value", HasValue);
        PseudoClasses.Set(":has-focus", _hasFocus);

        // The label floats when stacked, or floating with a value, focus, or anything in a slot.
        PseudoClasses.Set(":label-floating",
            placement == TextareaLabelPlacement.Stacked
            || (placement == TextareaLabelPlacement.Floating && (HasValue || _hasFocus || hasStartEnd)));

        PseudoClasses.Set(":fill-outline", Fill == TextareaFill.Outline);
        PseudoClasses.Set(":fill-solid", Fill == TextareaFill.Solid);
        PseudoClasses.Set(":shape-round", Shape == TextareaShape.Round);
        PseudoClasses.Set(":label-placement-start", placement == TextareaLabelPlacement.Start);
        PseudoClasses.Set(":label-placement-end", placement == TextareaLabelPlacement.End);
        PseudoClasses.Set(":label-placement-fixed", placement == TextareaLabelPlacement.Fixed);
        PseudoClasses.Set(":label-placement-floating", placement == TextareaLabelPlacement.Floating);
        PseudoClasses.Set(":label-placement-stacked", placement == TextareaLabelPlacement.Stacked);
        PseudoClasses.Set(":in-item", _item is not null);
        PseudoClasses.Set(":textarea-disabled", !IsEnabled);
        PseudoClasses.Set(":readonly", Readonly);
        PseudoClasses.Set(":auto-grow", AutoGrow);
        PseudoClasses.Set(":cols", Cols is not null);
        PseudoClasses.Set(":label-text-wrapper-hidden", !HasLabel);

        // renderBottomContent: only with hint text or a counter.
        PseudoClasses.Set(":has-bottom", !string.IsNullOrEmpty(HelperText) || !string.IsNullOrEmpty(ErrorText) || CounterText is not null);
    }

    /// <summary>What <c>render</c> puts on the native textarea.</summary>
    private void UpdateNative()
    {
        if (_native is not { } native)
        {
            return;
        }

        native.IsReadOnly = Readonly;
        // A negative maxlength is invalid in HTML, so no limit; 0 allows nothing, which OnNativeTextInput keeps out.
        native.MaxLength = Maxlength is > 0 and var max ? max : 0;
        native.PlaceholderText = Placeholder ?? "";
        native.Rows = Rows;
        native.Cols = Cols;
        native.AutoGrow = AutoGrow;
        native.TextWrapping = Wrap == TextareaWrap.Off ? TextWrapping.NoWrap : TextWrapping.Wrap;

        TextInputOptions.SetMultiline(native, true);
        TextInputOptions.SetContentType(native, Inputmode switch
        {
            TextareaInputmode.Decimal => TextInputContentType.Number,
            TextareaInputmode.Email => TextInputContentType.Email,
            TextareaInputmode.Numeric or TextareaInputmode.Tel => TextInputContentType.Digits,
            TextareaInputmode.Search => TextInputContentType.Search,
            TextareaInputmode.Url => TextInputContentType.Url,
            _ => TextInputContentType.Normal,
        });
        TextInputOptions.SetReturnKeyType(native, Enterkeyhint switch
        {
            TextareaEnterkeyhint.Done => TextInputReturnKeyType.Done,
            TextareaEnterkeyhint.Enter => TextInputReturnKeyType.Return,
            TextareaEnterkeyhint.Go => TextInputReturnKeyType.Go,
            TextareaEnterkeyhint.Next => TextInputReturnKeyType.Next,
            TextareaEnterkeyhint.Previous => TextInputReturnKeyType.Previous,
            TextareaEnterkeyhint.Search => TextInputReturnKeyType.Search,
            TextareaEnterkeyhint.Send => TextInputReturnKeyType.Send,
            _ => TextInputReturnKeyType.Default,
        });

        var capitalize = Autocapitalize?.Trim().ToLowerInvariant();
        TextInputOptions.SetAutoCapitalization(native, capitalize is "on" or "sentences" or "words");
        TextInputOptions.SetUppercase(native, capitalize == "characters");
    }

    /// <summary><c>renderCounter</c> and <c>getCounterText</c>.</summary>
    private void UpdateCounter()
    {
        if (!Counter || Maxlength is not { } max)
        {
            CounterText = null;
            return;
        }

        var length = CurrentValue.Length;
        var fallback = $"{length} / {max}";
        if (CounterFormatter is not { } formatter)
        {
            CounterText = fallback;
            return;
        }

        try
        {
            CounterText = formatter(length, max);
        }
        catch (Exception)
        {
            // printIonError, then the default text.
            CounterText = fallback;
        }
    }

    // ── The native textarea ──────────────────────────────────────────────────────────────────

    /// <summary><c>valueChanged</c>: assigning the native textarea's value, which lets an <c>ionInput</c> handler replace it.</summary>
    private void SyncNativeText()
    {
        // Not during an IME's composition.
        if (_native is not { } native || _syncing || native.Text == CurrentValue
            || native.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault() is { PreeditText.Length: > 0 })
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

    /// <summary>Keeps a value assigned to a textarea that isn't focused from scrolling the page to it.</summary>
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

        _inUserEdit = true;
        try
        {
            SetValueFromUser(native.Text ?? "");
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
            return;
        }

        CheckClearOnEdit(Key.None);
    }

    private void OnNativePasting(object? sender, RoutedEventArgs e)
    {
        if (Maxlength == 0)
        {
            e.Handled = true;
        }
    }

    private void SetValueFromUser(string value)
    {
        _syncing = true;
        try
        {
            SetCurrentValue(ValueProperty, value);
        }
        finally
        {
            _syncing = false;
        }

        // A handler of the value's change can have replaced it, which the native textarea then shows.
        SyncNativeText();
    }

    private void OnNativeGotFocus(object? sender, FocusChangedEventArgs e)
    {
        // onFocus
        _hasFocus = true;
        _focusedValue = Value;
        UpdateClasses();
        RaiseEvent(new RoutedEventArgs(IonFocusEvent, this));
    }

    private void OnNativeLostFocus(object? sender, FocusChangedEventArgs e)
    {
        // The native change event, then onBlur, which finds the value already committed: one ionChange, if the value isn't what
        // it was at focus.
        _hasFocus = false;
        if (_focusedValue != Value)
        {
            EmitValueChange();
        }

        _didTextareaClearOnEdit = false;
        UpdateClasses();
        RaiseEvent(new RoutedEventArgs(IonBlurEvent, this));
    }

    private void OnNativeKeyDown(object? sender, KeyEventArgs e) => CheckClearOnEdit(e.Key);

    /// <summary><c>checkClearOnEdit</c>, on key down, before the key is typed. <see cref="Key.None"/> is text with no key.</summary>
    private void CheckClearOnEdit(Key key)
    {
        if (!ClearOnEdit)
        {
            return;
        }

        var ignored = IgnoredKeys.Contains(key);
        if (!_didTextareaClearOnEdit && HasValue && !ignored)
        {
            SetValueFromUser("");
            EmitInputChange();
        }

        if (!ignored)
        {
            _didTextareaClearOnEdit = true;
        }
    }

    /// <summary><c>emitValueChange</c>. Ionic's also sets the value at focus, which only the blur after it reads; here the blur raises it.</summary>
    private void EmitValueChange() => RaiseEvent(new TextareaChangeEventArgs(IonChangeEvent, this, Value));

    /// <summary><c>emitInputChange</c>, through <c>debounceEvent</c> when <see cref="Debounce"/> is set.</summary>
    private void EmitInputChange()
    {
        var value = Value;
        if (Debounce is not { } wait)
        {
            RaiseEvent(new TextareaInputEventArgs(IonInputEvent, this, value));
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

            RaiseEvent(new TextareaInputEventArgs(IonInputEvent, this, value));
        }, TimeSpan.FromMilliseconds(Math.Max(0, wait)));
        _debounced = handle;
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The <c>&lt;label for&gt;</c>'s activation: a click in the wrapper that isn't on the native textarea focuses it. A slotted
    /// button's click is its own, as <see cref="NativeButton"/> arranges. The bottom text is outside the label.
    /// </summary>
    private void OnLabelClick(object? source)
    {
        if (source is not Visual visual || _wrapper is null || (visual != _wrapper && !_wrapper.IsVisualAncestorOf(visual)))
        {
            return;
        }

        // A label does nothing when the click is on interactive content inside it, other than its own control.
        for (var at = visual; at is not null && at != _wrapper && at != this; at = at.GetVisualParent())
        {
            if (at is InputElement { Focusable: true } element && element != _native && !(_native?.IsVisualAncestorOf(element) ?? false))
            {
                return;
            }
        }

        SetFocus();
    }

    private void OnLabelContentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ChildProperty)
        {
            UpdateAutomation();
        }
    }

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

    ItemControlType IItemControl.ItemControlType => ItemControlType.Textarea;

    /// <summary>Ionic's item calls <c>setFocus()</c>, then <c>click()</c>, which does nothing more here.</summary>
    void IItemControl.OnItemTap() => SetFocus();

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The native textarea's <c>aria-labelledby</c>: the label, or the slotted label's text, unless the host has an
    /// <c>aria-label</c>, which <c>inheritAriaAttributes</c> moves to it. <c>aria-describedby</c>: <c>getHintTextID</c>.
    /// </summary>
    private void UpdateAutomation()
    {
        if (_native is not { } native)
        {
            return;
        }

        var name = AutomationProperties.GetName(this);
        if (string.IsNullOrEmpty(name))
        {
            name = Label ?? LabelContent switch
            {
                Control control => AccessibleText.Of(control),
                string text => text,
                _ => _labelContent?.Child is { } child ? AccessibleText.Of(child) : null,
            };
        }

        native.SetValue(AutomationProperties.NameProperty, name);

        var hint = IsInvalid && !string.IsNullOrEmpty(ErrorText) ? ErrorText
            : !string.IsNullOrEmpty(HelperText) ? HelperText
            : null;
        native.SetValue(AutomationProperties.HelpTextProperty, hint);
    }
}
