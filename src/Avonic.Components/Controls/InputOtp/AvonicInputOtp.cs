using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Media;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-input-otp</c>: a row of one-character boxes for a one-time code, with separators between them and a description,
/// the content, under them. Each box is an <see cref="InputOtpBox"/>, a <see cref="TextBox"/>, made in code in <c>PART_Group</c>.
/// The look is in the theme. See <c>docs/components/ion-input-otp/README.md</c>.
/// </summary>
/// <remarks>
/// The boxes' input reaches the control as Chromium's reaches Ionic's handlers: text input, before the box types it, as
/// <c>onInput</c> with what the box would then hold; Backspace, the arrows and Tab as <c>onKeyDown</c>; a paste as
/// <c>onPaste</c>; and any other edit the box makes itself as an <c>input</c> event. The control keeps Ionic's
/// <c>inputValues</c>, writes each box as Ionic writes <c>input.value</c>, and works out the value from them.
/// </remarks>
[TemplatePart("PART_Group", typeof(Panel))]
[PseudoClasses(":has-focus", ":readonly", ":fill-outline", ":fill-solid", ":shape-rectangular", ":shape-round", ":shape-soft")]
[PseudoClasses(":size-large", ":size-medium", ":size-small", ":has-description")]
public class AvonicInputOtp : ContentControl
{
    /// <summary>The style class an app sets once the user has been in the field, read with <see cref="InvalidClass"/>.</summary>
    public const string TouchedClass = "ion-touched";

    /// <summary>The style class an app sets when the value is invalid.</summary>
    public const string InvalidClass = "ion-invalid";

    /// <summary>The style class an app sets when the value is valid.</summary>
    public const string ValidClass = "ion-valid";

    /// <summary>The group's <c>aria-label</c>, unless the host has a name of its own.</summary>
    internal const string GroupName = "One-time password input";

    /// <summary>The delay before an autofill moves focus, Ionic's <c>setTimeout(…, 20)</c>.</summary>
    internal static readonly TimeSpan AutofillFocusDelay = TimeSpan.FromMilliseconds(20);

    /// <summary><c>onKeyDown</c>'s <c>metaShortcuts</c>: with Control or Meta, these copy, paste, select and undo.</summary>
    private static readonly HashSet<Key> MetaShortcuts = [Key.A, Key.C, Key.V, Key.X, Key.R, Key.Z, Key.Y];

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>value</c>. Two-way by default, as Ionic's is mutable and the boxes set it.</summary>
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<AvonicInputOtp, string?>(nameof(Value), "",
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Ionic <c>length</c>: the number of boxes.</summary>
    public static readonly StyledProperty<int> LengthProperty =
        AvaloniaProperty.Register<AvonicInputOtp, int>(nameof(Length), 4);

    /// <summary>Ionic <c>type</c>: the characters allowed, unless <see cref="Pattern"/> says, and the keyboard.</summary>
    public static readonly StyledProperty<InputOtpType> TypeProperty =
        AvaloniaProperty.Register<AvonicInputOtp, InputOtpType>(nameof(Type));

    /// <summary>Ionic <c>pattern</c>: a regular expression one character has to match. Null is the type's.</summary>
    public static readonly StyledProperty<string?> PatternProperty =
        AvaloniaProperty.Register<AvonicInputOtp, string?>(nameof(Pattern));

    /// <summary>Ionic <c>separators</c>: <c>"all"</c>, or the boxes to put a separator after, such as <c>"1,3"</c>.</summary>
    public static readonly StyledProperty<string?> SeparatorsProperty =
        AvaloniaProperty.Register<AvonicInputOtp, string?>(nameof(Separators));

    /// <summary>Ionic <c>fill</c>.</summary>
    public static readonly StyledProperty<InputOtpFill> FillProperty =
        AvaloniaProperty.Register<AvonicInputOtp, InputOtpFill>(nameof(Fill));

    /// <summary>Ionic <c>shape</c>.</summary>
    public static readonly StyledProperty<InputOtpShape> ShapeProperty =
        AvaloniaProperty.Register<AvonicInputOtp, InputOtpShape>(nameof(Shape), InputOtpShape.Round);

    /// <summary>Ionic <c>size</c>.</summary>
    public static readonly StyledProperty<InputOtpSize> SizeProperty =
        AvaloniaProperty.Register<AvonicInputOtp, InputOtpSize>(nameof(Size), InputOtpSize.Medium);

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicInputOtp>();

    /// <summary>Ionic <c>readonly</c>.</summary>
    public static readonly StyledProperty<bool> ReadonlyProperty =
        AvaloniaProperty.Register<AvonicInputOtp, bool>(nameof(Readonly));

    /// <summary>Ionic <c>autocapitalize</c>.</summary>
    public static readonly StyledProperty<string> AutocapitalizeProperty =
        AvaloniaProperty.Register<AvonicInputOtp, string>(nameof(Autocapitalize), "off");

    /// <summary>Ionic <c>inputmode</c>. Null is the type's: numeric for numbers, text for text.</summary>
    public static readonly StyledProperty<InputOtpInputmode?> InputmodeProperty =
        AvaloniaProperty.Register<AvonicInputOtp, InputOtpInputmode?>(nameof(Inputmode));

    /// <summary>
    /// Ionic's internal <c>--highlight-color</c>: the focused, valid or invalid colour, which the theme works out per state, so
    /// that each box reads one value.
    /// </summary>
    public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.Register<AvonicInputOtp, IBrush?>(nameof(HighlightBrush));

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    public static readonly RoutedEvent<InputOtpInputEventArgs> IonInputEvent =
        RoutedEvent.Register<AvonicInputOtp, InputOtpInputEventArgs>(nameof(IonInput), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<InputOtpChangeEventArgs> IonChangeEvent =
        RoutedEvent.Register<AvonicInputOtp, InputOtpChangeEventArgs>(nameof(IonChange), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<InputOtpCompleteEventArgs> IonCompleteEvent =
        RoutedEvent.Register<AvonicInputOtp, InputOtpCompleteEventArgs>(nameof(IonComplete), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> IonFocusEvent =
        RoutedEvent.Register<AvonicInputOtp, RoutedEventArgs>(nameof(IonFocus), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> IonBlurEvent =
        RoutedEvent.Register<AvonicInputOtp, RoutedEventArgs>(nameof(IonBlur), RoutingStrategies.Bubble);

    // ── State: Ionic's fields ────────────────────────────────────────────────────────────────

    /// <summary><c>inputRefs</c>, and the separator after each.</summary>
    private readonly List<InputOtpBox> _boxes = [];
    private readonly List<InputOtpSeparator> _separators = [];

    /// <summary><c>inputValues</c>: what each box holds. It can be longer than <see cref="Length"/>, as Ionic's array can.</summary>
    private List<string> _inputValues = [];

    /// <summary><c>previousInputValues</c>.</summary>
    private List<string> _previousInputValues = [];

    /// <summary><c>parsedSeparators</c>.</summary>
    private List<int> _parsedSeparators = [];

    private bool _isKeyboardNavigation;
    private bool _hasFocus;
    private string? _focusedValue;

    private Panel? _group;
    private Regex? _validKeyPattern;
    private bool _syncing;
    private bool _updatingClasses;

    /// <summary>Past <c>componentWillLoad</c>: the watchers on <c>value</c>, <c>length</c> and <c>separators</c> run from then on.</summary>
    private bool _loaded;
    private IDisposable? _autofillFocus;

    static AvonicInputOtp()
    {
        // The host isn't a tab stop: a box inside it is.
        FocusableProperty.OverrideDefaultValue<AvonicInputOtp>(false);
    }

    public AvonicInputOtp()
    {
        Classes.CollectionChanged += OnClassesChanged;
        UpdateClasses();
    }

    // ── CLR accessors ────────────────────────────────────────────────────────────────────────

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Length
    {
        get => GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    public InputOtpType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public string? Pattern
    {
        get => GetValue(PatternProperty);
        set => SetValue(PatternProperty, value);
    }

    public string? Separators
    {
        get => GetValue(SeparatorsProperty);
        set => SetValue(SeparatorsProperty, value);
    }

    public InputOtpFill Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public InputOtpShape Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public InputOtpSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public bool Readonly
    {
        get => GetValue(ReadonlyProperty);
        set => SetValue(ReadonlyProperty, value);
    }

    public string Autocapitalize
    {
        get => GetValue(AutocapitalizeProperty);
        set => SetValue(AutocapitalizeProperty, value);
    }

    public InputOtpInputmode? Inputmode
    {
        get => GetValue(InputmodeProperty);
        set => SetValue(InputmodeProperty, value);
    }

    public IBrush? HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public event EventHandler<InputOtpInputEventArgs>? IonInput
    {
        add => AddHandler(IonInputEvent, value);
        remove => RemoveHandler(IonInputEvent, value);
    }

    public event EventHandler<InputOtpChangeEventArgs>? IonChange
    {
        add => AddHandler(IonChangeEvent, value);
        remove => RemoveHandler(IonChangeEvent, value);
    }

    public event EventHandler<InputOtpCompleteEventArgs>? IonComplete
    {
        add => AddHandler(IonCompleteEvent, value);
        remove => RemoveHandler(IonCompleteEvent, value);
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

    /// <summary>
    /// Ionic <c>setFocus(index?)</c>: with an index, that box, clamped to the row; with none, the first empty box, or the last if
    /// all are filled. The box's focus then lands as a click's would, on the box if it's filled, else on the first empty one.
    /// </summary>
    public void SetFocus(int? index = null)
    {
        if (_group is null)
        {
            ApplyTemplate();
        }

        var target = index is { } i ? Math.Max(0, Math.Min(i, Length - 1)) : GetTabbableIndex();
        if (target >= 0 && target < _boxes.Count)
        {
            _boxes[target].Focus();
        }
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _group?.Children.Clear();
        _group = e.NameScope.Find<Panel>("PART_Group");
        if (_group is not null)
        {
            foreach (var (box, separator) in _boxes.Zip(_separators))
            {
                _group.Children.Add(box);
                _group.Children.Add(separator);
            }
        }

        // render: the boxes, then componentDidLoad's updateTabIndexes.
        Render();
        UpdateTabIndexes();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // componentWillLoad, once, with every property set, whatever order they were set in: the separators, then the values.
        if (!_loaded)
        {
            _loaded = true;
            Render();
            ProcessSeparators();
            UpdateSeparators();
            InitializeValues();
            UpdateTabIndexes();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // A pending autofill focus would take focus back to a control that has left the page.
        _autofillFocus?.Dispose();
        _autofillFocus = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (!_loaded && (change.Property == ValueProperty || change.Property == LengthProperty || change.Property == SeparatorsProperty))
        {
            // Stencil doesn't run a watcher before the component loads, which does what they would.
            return;
        }

        if (change.Property == ValueProperty)
        {
            ValueChanged();
        }
        else if (change.Property == LengthProperty)
        {
            // The watcher on length processes the separators, and the render makes the boxes. The values stay as they are.
            ProcessSeparators();
            Render();
            UpdateTabIndexes();
        }
        else if (change.Property == SeparatorsProperty)
        {
            ProcessSeparators();
            UpdateSeparators();
        }
        else if (change.Property == TypeProperty || change.Property == PatternProperty)
        {
            _validKeyPattern = null;
            UpdateNative();
        }
        else if (change.Property == InputmodeProperty || change.Property == AutocapitalizeProperty
                 || change.Property == ReadonlyProperty)
        {
            UpdateNative();
            UpdateClasses();
        }
        else if (change.Property == FillProperty || change.Property == ShapeProperty || change.Property == SizeProperty
                 || change.Property == ColorProperty || change.Property == ContentProperty)
        {
            UpdateClasses();
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new InputOtpAutomationPeer(this);

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Only the classes the boxes copy: the list also holds every pseudoclass, which changes on each hover and focus.
        if (e.Action == NotifyCollectionChangedAction.Reset || Mentions(e.NewItems) || Mentions(e.OldItems))
        {
            UpdateClasses();
        }
    }

    private static bool Mentions(System.Collections.IList? classes) =>
        classes?.OfType<string>().Any(name => name is ValidClass or InvalidClass or TouchedClass or ColorPalette.IonColorPseudoClass) == true;

    // ── The value ────────────────────────────────────────────────────────────────────────────

    /// <summary><c>valueChanged</c>, the watcher on <c>value</c>, which an edit sets too.</summary>
    private void ValueChanged()
    {
        InitializeValues();
        UpdateTabIndexes();
    }

    /// <summary>
    /// <c>initializeValues</c>: each box's character from the value, cut to the length, with what the pattern refuses left
    /// empty; then the value is what the boxes hold, which runs this again once, and the boxes close up.
    /// </summary>
    private void InitializeValues()
    {
        var length = Math.Max(0, Length);
        _inputValues = Enumerable.Repeat("", length).ToList();
        if (string.IsNullOrEmpty(Value))
        {
            ShowValues();
            return;
        }

        var chars = Value[..Math.Min(Value.Length, length)];
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i].ToString();
            if (IsValidKey(c))
            {
                _inputValues[i] = c;
            }
        }

        ShowValues();

        // Without events: the watcher runs again for a value that changes.
        SetCurrentValue(ValueProperty, string.Concat(_inputValues));
        _previousInputValues = [.. _inputValues];
    }

    /// <summary><c>updateValue</c>: the value from the boxes, then <c>ionInput</c>, and <c>ionComplete</c> when they're all filled.</summary>
    private void UpdateValue()
    {
        var newValue = string.Concat(_inputValues);
        SetCurrentValue(ValueProperty, newValue);
        EmitIonInput();
        if (newValue.Length == Length)
        {
            RaiseEvent(new InputOtpCompleteEventArgs(IonCompleteEvent, this, newValue));
        }
    }

    private void EmitIonInput() => RaiseEvent(new InputOtpInputEventArgs(IonInputEvent, this, Value));

    private void EmitIonChange() => RaiseEvent(new InputOtpChangeEventArgs(IonChangeEvent, this, Value));

    /// <summary><c>validKeyPattern</c>: <c>^pattern$</c>, with the type's pattern when there's none.</summary>
    private bool IsValidKey(string key)
    {
        if (_validKeyPattern is null)
        {
            var pattern = !string.IsNullOrEmpty(Pattern) ? Pattern : Type == InputOtpType.Number ? @"[\p{N}]" : @"[\p{L}\p{N}]";
            try
            {
                // JavaScript's $ without the m flag is the end of the text only; .NET's also matches before a last newline.
                _validKeyPattern = new Regex("^" + pattern + @"\z", RegexOptions.CultureInvariant);
            }
            catch (ArgumentException exception)
            {
                // Ionic's handlers throw on a pattern JavaScript can't read. Here nothing matches.
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this,
                    "[AvonicInputOtp] Pattern {Pattern} isn't a regular expression: {Message}", pattern, exception.Message);
                _validKeyPattern = new Regex("(?!)");
            }
        }

        return _validKeyPattern.IsMatch(key);
    }

    private static string At(List<string> values, int index) => index >= 0 && index < values.Count ? values[index] : "";

    /// <summary>Writes <c>inputValues[index]</c>, growing the list as a JavaScript array grows.</summary>
    private static void Put(List<string> values, int index, string value)
    {
        while (values.Count <= index)
        {
            values.Add("");
        }

        values[index] = value;
    }

    // ── Boxes ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>render</c>: <c>length</c> boxes with a separator after each, each showing <c>inputValues[index]</c>.</summary>
    private void Render()
    {
        var length = Math.Max(0, Length);
        var changed = false;
        while (_boxes.Count > length)
        {
            var last = _boxes.Count - 1;
            _group?.Children.Remove(_boxes[last]);
            _group?.Children.Remove(_separators[last]);
            Unhook(_boxes[last]);
            _boxes.RemoveAt(last);
            _separators.RemoveAt(last);
            changed = true;
        }

        while (_boxes.Count < length)
        {
            var box = new InputOtpBox(_boxes.Count);
            Hook(box);
            var separator = new InputOtpSeparator();
            _boxes.Add(box);
            _separators.Add(separator);
            _group?.Children.Add(box);
            _group?.Children.Add(separator);
            changed = true;
        }

        ShowValues();
        UpdateSeparators();
        if (changed)
        {
            // New boxes, or a new length in each box's name.
            UpdateNative();
            UpdateClasses();
        }
    }

    /// <summary>The part of <c>render</c> that an edit changes: each box shows <c>inputValues[index]</c>.</summary>
    private void ShowValues()
    {
        foreach (var box in _boxes)
        {
            SetBoxText(box, At(_inputValues, box.Index));
        }
    }

    private void Hook(InputOtpBox box)
    {
        box.AddHandler(KeyDownEvent, OnBoxKeyDown, RoutingStrategies.Tunnel);
        box.AddHandler(TextInputEvent, OnBoxTextInput, RoutingStrategies.Tunnel);
        box.PastingFromClipboard += OnBoxPasting;
        box.GotFocus += OnBoxGotFocus;
        box.LostFocus += OnBoxLostFocus;
        box.PropertyChanged += OnBoxPropertyChanged;
    }

    private void Unhook(InputOtpBox box)
    {
        box.RemoveHandler(KeyDownEvent, OnBoxKeyDown);
        box.RemoveHandler(TextInputEvent, OnBoxTextInput);
        box.PastingFromClipboard -= OnBoxPasting;
        box.GotFocus -= OnBoxGotFocus;
        box.LostFocus -= OnBoxLostFocus;
        box.PropertyChanged -= OnBoxPropertyChanged;
    }

    /// <summary><c>input.value = text</c>: Chromium puts the caret at the end of a new value.</summary>
    private void SetBoxText(InputOtpBox? box, string text)
    {
        if (box is null || box.Text == text)
        {
            return;
        }

        _syncing = true;
        try
        {
            box.Text = text;
            box.CaretIndex = text.Length;
            box.ClearSelection();
        }
        finally
        {
            _syncing = false;
        }
    }

    private InputOtpBox? Box(int index) => index >= 0 && index < _boxes.Count ? _boxes[index] : null;

    /// <summary>What <c>render</c> puts on each native input.</summary>
    private void UpdateNative()
    {
        var contentType = (Inputmode ?? (Type == InputOtpType.Number ? InputOtpInputmode.Numeric : InputOtpInputmode.Text)) switch
        {
            InputOtpInputmode.Decimal => TextInputContentType.Number,
            InputOtpInputmode.Email => TextInputContentType.Email,
            InputOtpInputmode.Numeric or InputOtpInputmode.Tel => TextInputContentType.Digits,
            InputOtpInputmode.Search => TextInputContentType.Search,
            InputOtpInputmode.Url => TextInputContentType.Url,
            _ => TextInputContentType.Normal,
        };
        var capitalize = Autocapitalize?.Trim().ToLowerInvariant();

        foreach (var box in _boxes)
        {
            box.IsReadOnly = Readonly;
            TextInputOptions.SetContentType(box, contentType);
            TextInputOptions.SetAutoCapitalization(box, capitalize is "on" or "sentences" or "words");
            TextInputOptions.SetUppercase(box, capitalize == "characters");
            AutomationProperties.SetName(box, $"Input {box.Index + 1} of {Length}");
        }
    }

    /// <summary><c>render</c>'s host classes, which each box carries too, as its theme can't see the host's.</summary>
    private void UpdateClasses()
    {
        if (_updatingClasses)
        {
            return;
        }

        _updatingClasses = true;
        try
        {
            UpdateClassesCore();
        }
        finally
        {
            _updatingClasses = false;
        }
    }

    private void UpdateClassesCore()
    {
        void Set(string name, bool on)
        {
            PseudoClasses.Set(name, on);
            foreach (var box in _boxes)
            {
                box.SetState(name, on);
            }
        }

        Set(":has-focus", _hasFocus);
        Set(":readonly", Readonly);
        Set(":fill-outline", Fill == InputOtpFill.Outline);
        Set(":fill-solid", Fill == InputOtpFill.Solid);
        Set(":shape-rectangular", Shape == InputOtpShape.Rectangular);
        Set(":shape-round", Shape == InputOtpShape.Round);
        Set(":shape-soft", Shape == InputOtpShape.Soft);
        Set(":size-large", Size == InputOtpSize.Large);
        Set(":size-medium", Size == InputOtpSize.Medium);
        Set(":size-small", Size == InputOtpSize.Small);

        // The host's own: the boxes only copy them.
        foreach (var box in _boxes)
        {
            box.SetState(ColorPalette.IonColorPseudoClass, Classes.Contains(ColorPalette.IonColorPseudoClass));
            box.SetState(":ion-valid", Classes.Contains(ValidClass));
            box.SetState(":ion-invalid", Classes.Contains(InvalidClass));
            box.SetState(":ion-touched", Classes.Contains(TouchedClass));
        }

        // input-otp-description-hidden: no text in the slot.
        PseudoClasses.Set(":has-description", Content is not null && (Content is not string text || !string.IsNullOrWhiteSpace(text)));
    }

    // ── Separators ───────────────────────────────────────────────────────────────────────────

    /// <summary><c>processSeparators</c>.</summary>
    private void ProcessSeparators()
    {
        var separators = Separators;
        var length = Length;
        if (separators is null)
        {
            _parsedSeparators = [];
            return;
        }

        List<int> values;
        if (separators == "all")
        {
            values = Enumerable.Range(1, Math.Max(0, length - 1)).ToList();
        }
        else if (!Regex.IsMatch(separators, @"^([0-9]+)(,[0-9]+)*\z"))
        {
            Warn($"Invalid separators format. Expected a comma-separated list of numbers, an array of numbers, or \"all\". Received: {separators}");
            _parsedSeparators = [];
            return;
        }
        else
        {
            values = separators.Split(',').Select(part => int.TryParse(part, out var n) ? n : int.MaxValue).ToList();
        }

        if (values.Where((position, index) => values.IndexOf(position) != index).Any())
        {
            Warn($"Duplicate separator positions are not allowed. Received: {separators}");
        }

        var invalid = values.Where(position => position > length).ToList();
        if (invalid.Count > 0)
        {
            Warn($"The following separator positions are greater than the input length ({length}): {string.Join(", ", invalid)}. These separators will be ignored.");
        }

        _parsedSeparators = values.Where(position => position <= length).ToList();
    }

    private void Warn(string message) =>
        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "[AvonicInputOtp] {Message}", message);

    /// <summary><c>showSeparator</c>: after a listed box, but not after the last.</summary>
    private void UpdateSeparators()
    {
        for (var i = 0; i < _separators.Count; i++)
        {
            _separators[i].IsVisible = _parsedSeparators.Contains(i + 1) && i < Length - 1;
        }
    }

    // ── Focus ────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>hasFocus</c>, and its <c>has-focus</c> class on the host and every box, which is all a focus change touches.</summary>
    private void SetHasFocus(bool on)
    {
        if (_hasFocus == on)
        {
            return;
        }

        _hasFocus = on;
        PseudoClasses.Set(":has-focus", on);
        foreach (var box in _boxes)
        {
            box.SetState(":has-focus", on);
        }
    }

    /// <summary><c>getFirstEmptyIndex</c>, or -1.</summary>
    private int GetFirstEmptyIndex()
    {
        for (var i = 0; i < Length; i++)
        {
            if (At(_inputValues, i).Length == 0)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary><c>getTabbableIndex</c>: the first empty box, or the last.</summary>
    private int GetTabbableIndex()
    {
        var firstEmpty = GetFirstEmptyIndex();
        return firstEmpty == -1 ? Length - 1 : firstEmpty;
    }

    /// <summary><c>updateTabIndexes</c>: the first empty box, or the last, is the tab stop; an empty box that isn't is hidden.</summary>
    private void UpdateTabIndexes()
    {
        var firstEmpty = GetFirstEmptyIndex();
        foreach (var box in _boxes)
        {
            var tabbable = firstEmpty == -1 ? box.Index == Length - 1 : firstEmpty == box.Index;
            box.IsTabStop = tabbable;
            var empty = At(_inputValues, box.Index).Length == 0;
            AutomationProperties.SetAccessibilityView(box, empty && !tabbable ? AccessibilityView.Raw : AccessibilityView.Content);
        }
    }

    private void FocusNext(int index)
    {
        if (index < Length - 1)
        {
            Box(index + 1)?.Focus();
        }
    }

    private void FocusPrevious(int index)
    {
        if (index > 0)
        {
            Box(index - 1)?.Focus();
        }
    }

    /// <summary>
    /// <c>onFocus</c>: <c>ionFocus</c> when focus enters the group. A focus that isn't from the keyboard lands on the box if it's
    /// filled, else on the first empty box, or the last. The focused box is the tab stop.
    /// </summary>
    private void OnBoxGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is not InputOtpBox box)
        {
            return;
        }

        if (!_hasFocus)
        {
            RaiseEvent(new RoutedEventArgs(IonFocusEvent, this));
            _focusedValue = Value;
        }

        SetHasFocus(true);

        var finalIndex = box.Index;
        if (!_isKeyboardNavigation)
        {
            var target = At(_inputValues, box.Index).Length > 0 ? box.Index : GetFirstEmptyIndex();
            finalIndex = target == -1 ? Length - 1 : target;
            Box(finalIndex)?.Focus();
        }

        foreach (var other in _boxes)
        {
            other.IsTabStop = other.Index == finalIndex;
        }

        _isKeyboardNavigation = false;
    }

    /// <summary><c>onBlur</c>: unless focus moves to another box, <c>ionBlur</c>, then <c>ionChange</c> if the value isn't what it was at focus.</summary>
    private void OnBoxLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.NewFocusedElement is InputOtpBox next && _boxes.Contains(next))
        {
            return;
        }

        SetHasFocus(false);
        UpdateTabIndexes();
        RaiseEvent(new RoutedEventArgs(IonBlurEvent, this));
        if (_focusedValue != Value)
        {
            EmitIonChange();
        }
    }

    // ── Keys ─────────────────────────────────────────────────────────────────────────────────

    /// <summary><c>onKeyDown</c>: Backspace, the arrows and Tab.</summary>
    private void OnBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not InputOtpBox box)
        {
            return;
        }

        // A read-only text box keeps its paste event to itself, and Chromium still raises it, which onPaste takes.
        if (box.IsReadOnly && Application.Current?.PlatformSettings?.HotkeyConfiguration.Paste.Any(gesture => gesture.Matches(e)) == true)
        {
            e.Handled = true;
            PasteFromClipboard();
            return;
        }

        var index = box.Index;
        var length = Length;
        var isTextSelection = box.SelectionStart != box.SelectionEnd;
        var meta = (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0 && MetaShortcuts.Contains(e.Key);
        if (isTextSelection || meta)
        {
            // The box's own handling, and onPaste or onInput after it.
            return;
        }

        if (e.Key == Key.Back)
        {
            if (At(_inputValues, index).Length > 0)
            {
                // Shift everything after this box left by one, and clear the last.
                for (var i = index; i < length - 1; i++)
                {
                    Put(_inputValues, i, At(_inputValues, i + 1));
                }

                Put(_inputValues, length - 1, "");
                for (var i = 0; i < length; i++)
                {
                    SetBoxText(Box(i), At(_inputValues, i));
                }

                UpdateValue();
                e.Handled = true;
            }
            else if (index > 0)
            {
                // Focus moves back during the keydown, and Chromium's default action, the deletion, follows focus.
                FocusPrevious(index);
                e.Handled = true;
                if (_boxes.FirstOrDefault(other => other.IsFocused) is { } focused)
                {
                    NativeDelete(focused);
                }
            }
        }
        else if (e.Key is Key.Left or Key.Right)
        {
            _isKeyboardNavigation = true;
            e.Handled = true;
            var isLeft = e.Key == Key.Left;
            var rtl = FlowDirection == FlowDirection.RightToLeft;
            if ((isLeft && rtl) || (!isLeft && !rtl))
            {
                // Only on from a box with a value.
                if (At(_inputValues, index).Length > 0 && index < length - 1)
                {
                    FocusNext(index);
                }
            }
            else
            {
                FocusPrevious(index);
            }
        }
        else if (e.Key == Key.Tab)
        {
            _isKeyboardNavigation = true;
        }
    }

    /// <summary>Backspace's default action in a box: the selection, or the character before the caret, then an <c>input</c> event.</summary>
    private void NativeDelete(InputOtpBox box)
    {
        if (box.IsReadOnly || !box.IsEffectivelyEnabled)
        {
            return;
        }

        var text = box.Text ?? "";
        var start = Math.Clamp(Math.Min(box.SelectionStart, box.SelectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(box.SelectionStart, box.SelectionEnd), 0, text.Length);
        if (start == end)
        {
            start = Math.Clamp(box.CaretIndex, 0, text.Length);
            end = start;
            if (start == 0)
            {
                return;
            }

            start--;
        }

        OnInput(box, text.Remove(start, end - start), start, start, null);
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Text the box is about to type: a key, a soft keyboard's word or an autofill, or an IME's commit. What the native input would
    /// then hold, with the selection replaced and the caret after it, goes to <c>onInput</c>, which writes the box itself.
    /// </summary>
    private void OnBoxTextInput(object? sender, TextInputEventArgs e)
    {
        if (sender is not InputOtpBox box || string.IsNullOrEmpty(e.Text) || box.IsReadOnly)
        {
            // A read-only input takes nothing, and raises no input event.
            return;
        }

        e.Handled = true;
        var text = box.Text ?? "";
        var start = Math.Clamp(Math.Min(box.SelectionStart, box.SelectionEnd), 0, text.Length);
        var end = Math.Clamp(Math.Max(box.SelectionStart, box.SelectionEnd), 0, text.Length);
        if (start == end)
        {
            start = end = Math.Clamp(box.CaretIndex, 0, text.Length);
        }

        var value = text[..start] + e.Text + text[end..];
        var caret = start + e.Text.Length;
        OnInput(box, value, caret, caret, e.Text);
    }

    /// <summary>Any other edit the box makes itself, such as a deleted selection or a cut: an <c>input</c> event.</summary>
    private void OnBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.TextProperty || _syncing || sender is not InputOtpBox box)
        {
            return;
        }

        var value = box.Text ?? "";
        var caret = Math.Min(Math.Min(box.SelectionStart, box.SelectionEnd), value.Length);
        OnInput(box, value, caret, Math.Min(Math.Max(box.SelectionStart, box.SelectionEnd), value.Length), null);
    }

    /// <summary><c>onInput</c>: an autofill, a refused character, a box filled, a character inserted, or one replaced.</summary>
    private void OnInput(InputOtpBox input, string value, int selectionStart, int selectionEnd, string? data)
    {
        var index = input.Index;
        var length = Length;
        var previousValue = At(_previousInputValues, index);

        // 1. Autofill: more than one character more than before. The valid ones fill the boxes from the first.
        if (value.Length - previousValue.Length > 1)
        {
            var validChars = value.Select(c => c.ToString()).Where(IsValidKey).Take(Math.Max(0, length)).ToList();
            for (var i = 0; i < length; i++)
            {
                var c = i < validChars.Count ? validChars[i] : "";
                Put(_inputValues, i, c);
                SetBoxText(Box(i), c);
            }

            UpdateValue();

            // The first empty box, or the last, a little later, once the boxes have been written.
            _autofillFocus?.Dispose();
            var nextIndex = validChars.Count < length ? validChars.Count : length - 1;
            _autofillFocus = AvonicConfig.Schedule(() => Box(nextIndex)?.Focus(), AutofillFocusDelay);
            _previousInputValues = [.. _inputValues];
            return;
        }

        // 2. A character the pattern refuses: the box goes back to what it held.
        if (value.Length > 0 && !IsValidKey(value[^1].ToString()))
        {
            SetBoxText(input, At(_inputValues, index));
            _previousInputValues = [.. _inputValues];
            return;
        }

        // 3. The whole box replaced, or an empty box: this box only, then on to the next.
        var isAllSelected = selectionStart == 0 && selectionEnd == value.Length;
        var isEmpty = At(_inputValues, index).Length == 0;
        if (isAllSelected || isEmpty)
        {
            Put(_inputValues, index, value);
            SetBoxText(input, value);
            UpdateValue();
            FocusNext(index);
            _previousInputValues = [.. _inputValues];
            return;
        }

        // 4. Typed into a filled box while the last is empty: the rest shift right, and it goes in here.
        var hasAvailableBoxAtEnd = _inputValues.Count > 0 && _inputValues[^1].Length == 0;
        if (hasAvailableBoxAtEnd && value.Length == 2)
        {
            var newChar = data;
            if (string.IsNullOrEmpty(newChar))
            {
                var differs = Enumerable.Range(0, value.Length).FirstOrDefault(i => i >= previousValue.Length || value[i] != previousValue[i], -1);
                newChar = (differs >= 0 ? value[differs] : value[^1]).ToString();
            }

            if (!IsValidKey(newChar))
            {
                SetBoxText(input, At(_inputValues, index));
                _previousInputValues = [.. _inputValues];
                return;
            }

            for (var i = _inputValues.Count - 1; i > index; i--)
            {
                _inputValues[i] = _inputValues[i - 1];
                SetBoxText(Box(i), _inputValues[i]);
            }

            Put(_inputValues, index, newChar);
            SetBoxText(input, newChar);
            UpdateValue();
            _previousInputValues = [.. _inputValues];
            return;
        }

        // 5. A filled box's character replaced by the one typed, which is the one before the caret.
        var newCharIndex = selectionStart - 1;
        var replacement = (newCharIndex >= 0 && newCharIndex < value.Length ? value[newCharIndex] : value[0]).ToString();
        if (!IsValidKey(replacement))
        {
            SetBoxText(input, At(_inputValues, index));
            _previousInputValues = [.. _inputValues];
            return;
        }

        Put(_inputValues, index, replacement);
        SetBoxText(input, replacement);
        UpdateValue();
        _previousInputValues = [.. _inputValues];
    }

    // ── Paste ────────────────────────────────────────────────────────────────────────────────

    private void OnBoxPasting(object? sender, RoutedEventArgs e)
    {
        // onPaste's preventDefault: Ionic writes the boxes itself.
        e.Handled = true;
        PasteFromClipboard();
    }

    private async void PasteFromClipboard()
    {
        // The clipboard is read asynchronously, where Chromium hands onPaste its text with the event.
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        string? text;
        try
        {
            text = clipboard is null ? null : await clipboard.TryGetTextAsync();
        }
        catch (Exception exception)
        {
            // A platform that can't read its clipboard now: nothing is pasted, rather than an unhandled exception.
            Warn($"The clipboard couldn't be read: {exception.Message}");
            return;
        }

        OnPaste(text);
    }

    /// <summary><c>onPaste</c>: the valid characters from the first box, then the next empty box, or the last, focused.</summary>
    internal void OnPaste(string? pastedText)
    {
        var length = Length;
        if (string.IsNullOrEmpty(pastedText))
        {
            // As a native input does, an input event with nothing pasted.
            EmitIonInput();
            return;
        }

        var validChars = pastedText.Select(c => c.ToString()).Where(IsValidKey).Take(Math.Max(0, length)).ToList();
        for (var i = 0; i < validChars.Count; i++)
        {
            SetBoxText(Box(i), validChars[i]);
            Put(_inputValues, i, validChars[i]);
        }

        // The watcher on value writes every box from it.
        SetCurrentValue(ValueProperty, string.Concat(validChars));
        UpdateValue();

        var nextEmptyIndex = validChars.Count < length ? validChars.Count : length - 1;
        Box(nextEmptyIndex)?.Focus();
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>The <c>role="group"</c> div, named "One-time password input", unless the host has a name, which Ionic moves to it.</summary>
    private sealed class InputOtpAutomationPeer(AvonicInputOtp owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string? GetNameCore()
        {
            // The host's own name only, as inheritAriaAttributes moves aria-label: not the description's text, which a content
            // control's name could come from.
            var name = AutomationProperties.GetName(Owner);
            return string.IsNullOrEmpty(name) ? GroupName : name;
        }
    }
}
