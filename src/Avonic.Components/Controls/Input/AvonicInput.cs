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
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
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
public class AvonicInput : TemplatedControl, IItemControl
{
    /// <summary>The style class an app sets once the user has been in the field, read with <see cref="InvalidClass"/>.</summary>
    public const string TouchedClass = "ion-touched";

    /// <summary>The style class an app sets when the value is invalid.</summary>
    public const string InvalidClass = "ion-invalid";

    /// <summary>The style class an app sets when the value is valid.</summary>
    public const string ValidClass = "ion-valid";

    /// <summary>What Chromium draws for each character of a password.</summary>
    private const char PasswordMask = '•';

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

    // ── For the theme ────────────────────────────────────────────────────────────────────────

    /// <summary>The value, or else the <c>ConverterParameter</c>: a default such as md's clear icon.</summary>
    public static readonly IValueConverter OrConverter =
        new FuncValueConverter<object?, object?, object?>((value, parameter) => value is null or "" ? parameter : value);

    /// <summary>
    /// The wrapper's padding: the values are <c>--padding-start</c>, <c>-top</c>, <c>-end</c> and <c>-bottom</c>, then the fill's
    /// own start and end default for an unset start or end. An unset top or bottom is 0.
    /// </summary>
    public static readonly IMultiValueConverter PaddingConverter =
        new FuncMultiValueConverter<object?, Thickness>(values =>
        {
            var list = values.ToList();
            var fallback = Number(list.ElementAtOrDefault(4));
            return new Thickness(
                NumberOr(list.ElementAtOrDefault(0), fallback),
                Number(list.ElementAtOrDefault(1)),
                NumberOr(list.ElementAtOrDefault(2), fallback),
                Number(list.ElementAtOrDefault(3)));
        });

    /// <summary>
    /// <c>border-radius</c>: the first value that's set, a number or a <see cref="CornerRadius"/>, on the corners the
    /// <c>ConverterParameter</c> names: <c>Top</c> (solid's), <c>Start</c> or <c>End</c> (the outline's ends), or all of them.
    /// </summary>
    public static readonly IMultiValueConverter RadiusConverter = new RadiusConverterImpl();

    /// <summary>
    /// A border of the width in the value on the sides the <c>ConverterParameter</c> names, from <c>s</c>tart, <c>t</c>op,
    /// <c>e</c>nd and <c>b</c>ottom, such as <c>stb</c> for the outline's start.
    /// </summary>
    public static readonly IMultiValueConverter SidesConverter = new SidesConverterImpl();

    /// <summary>
    /// <c>.input-bottom</c>'s padding: the values are <c>--padding-start</c> and <c>--padding-end</c>, then the fill's own default
    /// for either that's unset. The <c>ConverterParameter</c> is the padding above; there's none below.
    /// </summary>
    public static readonly IMultiValueConverter BottomPaddingConverter = new BottomPaddingConverterImpl();

    /// <summary><c>calc(var(--padding-start) - 4px)</c>: the outline's start. The values are the variable and the fill's default.</summary>
    public static readonly IMultiValueConverter OutlineStartWidthConverter =
        new FuncMultiValueConverter<object?, double>(values =>
        {
            var list = values.ToList();
            return Math.Max(0, NumberOr(list.ElementAtOrDefault(0), Number(list.ElementAtOrDefault(1))) - 4);
        });

    /// <summary>
    /// The notch for a slotted label: <c>createNotchController</c> sets the spacer's width to the label's width at 0.75. The
    /// values are <see cref="Label"/>, which leaves the spacer to its own text when it's set, and the slotted label's width.
    /// </summary>
    public static readonly IMultiValueConverter NotchWidthConverter =
        new FuncMultiValueConverter<object?, double>(values =>
        {
            var list = values.ToList();
            return list.ElementAtOrDefault(0) is string ? double.NaN : Number(list.ElementAtOrDefault(1)) * 0.75;
        });

    private static double Number(object? value) => value is double number && double.IsFinite(number) ? number : 0;

    private static double NumberOr(object? value, double fallback) =>
        value is double number && double.IsFinite(number) ? number : fallback;

    private sealed class SidesConverterImpl : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            var width = Number(values.FirstOrDefault());
            var sides = parameter as string ?? "";
            return new Thickness(
                sides.Contains('s') ? width : 0, sides.Contains('t') ? width : 0,
                sides.Contains('e') ? width : 0, sides.Contains('b') ? width : 0);
        }
    }

    private sealed class BottomPaddingConverterImpl : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            var fallback = Number(values.ElementAtOrDefault(2));
            var top = double.TryParse(parameter as string, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : 0;
            return new Thickness(NumberOr(values.ElementAtOrDefault(0), fallback), top, NumberOr(values.ElementAtOrDefault(1), fallback), 0);
        }
    }

    private sealed class RadiusConverterImpl : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            // The extras: the element's Bounds and FlowDirection, for a CSS string's percentages.
            var extras = values.Where(value => value is Rect or Size or Avalonia.Media.FlowDirection).ToList();
            var r = values.FirstOrDefault(value => value is double or Avalonia.CornerRadius or string) switch
            {
                double number => new Avalonia.CornerRadius(number),
                Avalonia.CornerRadius radius => radius,
                string css => Behaviors.BorderRadius.ToCornerRadius.Convert([css, .. extras], typeof(Avalonia.CornerRadius), null, culture)
                    is Avalonia.CornerRadius parsed ? parsed : default,
                _ => default,
            };
            return parameter switch
            {
                "Top" => new Avalonia.CornerRadius(r.TopLeft, r.TopRight, 0, 0),
                "Start" => new Avalonia.CornerRadius(r.TopLeft, 0, 0, r.BottomLeft),
                "End" => new Avalonia.CornerRadius(0, r.TopRight, r.BottomRight, 0),
                _ => r,
            };
        }
    }

    private TextBox? _native;
    private Control? _wrapper;
    private Control? _clearButton;
    private ContentPresenter? _labelContent;
    private bool _syncing;
    private bool _inUserEdit;
    private bool _autofocused;
    private AvonicItem? _item;
    private bool _hasFocus;
    private bool _didInputClearOnEdit;
    private string? _focusedValue;
    private string? _counterText;
    private IDisposable? _debounced;

    static AvonicInput()
    {
        // The host isn't a tab stop: the native input inside it is.
        FocusableProperty.OverrideDefaultValue<AvonicInput>(false);
    }

    public AvonicInput()
    {
        // The <label for> around the field: a click on it focuses the input, on release, with no keys.
        _ = new NativeButton(this, OnLabelClick, () => false, UpdateIsEffectivelyEnabled);

        // The clear button's onPointerDown preventDefault: a press on it doesn't take focus from the field. Here, on the way
        // down, before the press reaches the button.
        AddHandler(PointerPressedEvent, OnPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);

        Classes.CollectionChanged += OnClassesChanged;
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

    /// <summary><c>shouldClearOnEdit</c>.</summary>
    private bool ShouldClearOnEdit => ClearOnEdit ?? Type == InputType.Password;

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
        }

        _native = e.NameScope.Find<TextBox>("PART_NativeInput");
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
        _clearButton = e.NameScope.Find<Control>("PART_ClearButton");

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
            UpdateNative();
            SyncNativeText();
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
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // autoFocus on the native input: Chromium focuses it once, when it's first in the document, unless something else
        // already has focus. Not again when it's loaded again.
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
        _item = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            // valueChanged: the native input shows the value, unless it's what it already shows.
            SyncNativeText();
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
        // The MutationObserver on class: a new aria-describedby. Only the two classes checkInvalidState reads, as the list
        // also holds every pseudoclass.
        if (Mentions(e.NewItems) || Mentions(e.OldItems) || e.Action == NotifyCollectionChangedAction.Reset)
        {
            UpdateAutomation();
        }
    }

    private static bool Mentions(System.Collections.IList? classes) =>
        classes?.OfType<string>().Any(name => name is TouchedClass or InvalidClass) == true;

    /// <summary><c>render</c>'s host classes.</summary>
    private void UpdateClasses()
    {
        var placement = LabelPlacement;
        var hasStartEnd = StartContent is not null || EndContent is not null;
        var item = _item;

        PseudoClasses.Set(":has-value", HasValue);
        PseudoClasses.Set(":has-focus", _hasFocus);

        // The label floats when stacked, or floating with a value, focus, or anything in a slot.
        PseudoClasses.Set(":label-floating",
            placement == InputLabelPlacement.Stacked
            || (placement == InputLabelPlacement.Floating && (HasValue || _hasFocus || hasStartEnd)));

        PseudoClasses.Set(":fill-outline", Fill == InputFill.Outline);
        PseudoClasses.Set(":fill-solid", Fill == InputFill.Solid);
        PseudoClasses.Set(":shape-round", Shape == InputShape.Round);
        PseudoClasses.Set(":label-placement-start", placement == InputLabelPlacement.Start);
        PseudoClasses.Set(":label-placement-end", placement == InputLabelPlacement.End);
        PseudoClasses.Set(":label-placement-fixed", placement == InputLabelPlacement.Fixed);
        PseudoClasses.Set(":label-placement-floating", placement == InputLabelPlacement.Floating);
        PseudoClasses.Set(":label-placement-stacked", placement == InputLabelPlacement.Stacked);
        PseudoClasses.Set(":in-item", item is not null);
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
        if (_native is not { } native)
        {
            return;
        }

        var password = Type == InputType.Password;
        native.PasswordChar = password ? PasswordMask : default;
        native.IsReadOnly = Readonly;
        // A negative maxlength is invalid in HTML, so no limit; 0 allows nothing, which OnNativeTextInput keeps out.
        native.MaxLength = Maxlength is > 0 and var max ? max : 0;
        native.PlaceholderText = Placeholder ?? "";

        TextInputOptions.SetContentType(native, ContentType());
        TextInputOptions.SetIsSensitive(native, password);
        TextInputOptions.SetReturnKeyType(native, Enterkeyhint switch
        {
            InputEnterkeyhint.Done => TextInputReturnKeyType.Done,
            InputEnterkeyhint.Enter => TextInputReturnKeyType.Return,
            InputEnterkeyhint.Go => TextInputReturnKeyType.Go,
            InputEnterkeyhint.Next => TextInputReturnKeyType.Next,
            InputEnterkeyhint.Previous => TextInputReturnKeyType.Previous,
            InputEnterkeyhint.Search => TextInputReturnKeyType.Search,
            InputEnterkeyhint.Send => TextInputReturnKeyType.Send,
            _ => TextInputReturnKeyType.Default,
        });

        var capitalize = Autocapitalize?.Trim().ToLowerInvariant();
        TextInputOptions.SetAutoCapitalization(native, capitalize is "on" or "sentences" or "words");
        TextInputOptions.SetUppercase(native, capitalize == "characters");
        TextInputOptions.SetShowSuggestions(native, Autocorrect == InputAutocorrect.On);
    }

    /// <summary><c>inputmode</c> if it's set, as it beats the type's keyboard, else <c>type</c>'s.</summary>
    private TextInputContentType ContentType() => Inputmode switch
    {
        InputInputmode.Decimal => TextInputContentType.Number,
        InputInputmode.Email => TextInputContentType.Email,
        InputInputmode.Numeric or InputInputmode.Tel => TextInputContentType.Digits,
        InputInputmode.Search => TextInputContentType.Search,
        InputInputmode.Text => TextInputContentType.Normal,
        InputInputmode.Url => TextInputContentType.Url,
        _ => Type switch
        {
            InputType.Email => TextInputContentType.Email,
            InputType.Number => TextInputContentType.Number,
            InputType.Password => TextInputContentType.Password,
            InputType.Search => TextInputContentType.Search,
            InputType.Tel => TextInputContentType.Digits,
            InputType.Url => TextInputContentType.Url,
            _ => TextInputContentType.Normal,
        },
    };

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

    // ── The native input ─────────────────────────────────────────────────────────────────────

    /// <summary><c>valueChanged</c>: assigning the native input's value, which lets an <c>ionInput</c> handler replace it.</summary>
    private void SyncNativeText()
    {
        // Not during an IME's composition, as Ionic checks isComposing.
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

    private void OnPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        if (_clearButton is not null && e.Source is Visual source && (source == _clearButton || _clearButton.IsVisualAncestorOf(source)))
        {
            KeepFocus.During(e);
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

        // A handler of the value's change can have replaced it, which the native input then shows.
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

    private void OnNativeKeyDown(object? sender, KeyEventArgs e)
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
            SetValueFromUser("");
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

        SetValueFromUser("");
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
        if (Debounce is not { } wait)
        {
            RaiseEvent(new InputInputEventArgs(IonInputEvent, this, value));
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

            RaiseEvent(new InputInputEventArgs(IonInputEvent, this, value));
        }, TimeSpan.FromMilliseconds(Math.Max(0, wait)));
        _debounced = handle;
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The <c>&lt;label for&gt;</c>'s activation: a click in the wrapper that isn't on the native input focuses it. A slotted
    /// button's click, or the clear button's, is its own, as <see cref="NativeButton"/> arranges. The bottom text is outside
    /// the label.
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

    ItemControlType IItemControl.ItemControlType => ItemControlType.Input;

    /// <summary>Ionic's item calls <c>setFocus()</c>, then <c>click()</c>, which does nothing more here.</summary>
    void IItemControl.OnItemTap() => SetFocus();

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The native input's <c>aria-labelledby</c>: the label, or the slotted label's text, unless the host has an
    /// <c>aria-label</c>, which <c>inheritAriaAttributes</c> moves to the input. <c>aria-describedby</c>: <c>getHintTextID</c>.
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
