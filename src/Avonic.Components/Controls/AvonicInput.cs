using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Controls;

/// <summary>
/// A touch-first single-line text input with label, fill, and slot support.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-input</c><br/>
/// <c>Content</c> is the label slot — set it to a string or any control.<br/>
/// <c>StartContent</c> / <c>EndContent</c> map to Ionic's <c>start</c> / <c>end</c> slots.
/// </remarks>
[PseudoClasses(
    ":focused",
    ":has-value",
    ":has-error",
    ":readonly",
    ":label-start", ":label-end", ":label-fixed", ":label-stacked", ":label-floating",
    ":fill-solid", ":fill-outline",
    ":shape-round")]
public class AvonicInput : ContentControl
{
    // ── Backing fields ────────────────────────────────────────────────────────

    private string? _value;

    // ── DirectProperty ────────────────────────────────────────────────────────

    public static readonly DirectProperty<AvonicInput, string?> ValueProperty =
        AvaloniaProperty.RegisterDirect<AvonicInput, string?>(
            nameof(Value),
            o => o._value,
            (o, v) => o.Value = v);

    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<AvonicInput, string?>(nameof(Placeholder));

    public static readonly StyledProperty<InputLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<AvonicInput, InputLabelPlacement>(
            nameof(LabelPlacement), defaultValue: InputLabelPlacement.Start);

    public static readonly StyledProperty<InputFill> FillProperty =
        AvaloniaProperty.Register<AvonicInput, InputFill>(nameof(Fill), defaultValue: InputFill.Default);

    public static readonly StyledProperty<InputShape> ShapeProperty =
        AvaloniaProperty.Register<AvonicInput, InputShape>(nameof(Shape), defaultValue: InputShape.Default);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<AvonicInput, bool>(nameof(IsReadOnly), defaultValue: false);

    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<AvonicInput, int>(nameof(MaxLength), defaultValue: 0);

    public static readonly StyledProperty<bool> ClearInputProperty =
        AvaloniaProperty.Register<AvonicInput, bool>(nameof(ClearInput), defaultValue: false);

    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<AvonicInput, string?>(nameof(HelperText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<AvonicInput, string?>(nameof(ErrorText));

    public static readonly StyledProperty<bool> CounterProperty =
        AvaloniaProperty.Register<AvonicInput, bool>(nameof(Counter), defaultValue: false);

    public static readonly StyledProperty<object?> StartContentProperty =
        AvaloniaProperty.Register<AvonicInput, object?>(nameof(StartContent));

    public static readonly StyledProperty<object?> EndContentProperty =
        AvaloniaProperty.Register<AvonicInput, object?>(nameof(EndContent));

    // ── Routed Events ────────────────────────────────────────────────────────

    public static readonly RoutedEvent<InputValueChangedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<AvonicInput, InputValueChangedEventArgs>(
            nameof(ValueChanged), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> InputFocusedEvent =
        RoutedEvent.Register<AvonicInput, RoutedEventArgs>(
            nameof(InputFocused), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> InputBlurredEvent =
        RoutedEvent.Register<AvonicInput, RoutedEventArgs>(
            nameof(InputBlurred), RoutingStrategies.Bubble);

    /// <summary>Raised when the user changes the text value.</summary>
    public event EventHandler<InputValueChangedEventArgs>? ValueChanged
    {
        add    => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>Raised when the inner TextBox receives focus.</summary>
    public event EventHandler<RoutedEventArgs>? InputFocused
    {
        add    => AddHandler(InputFocusedEvent, value);
        remove => RemoveHandler(InputFocusedEvent, value);
    }

    /// <summary>Raised when the inner TextBox loses focus.</summary>
    public event EventHandler<RoutedEventArgs>? InputBlurred
    {
        add    => AddHandler(InputBlurredEvent, value);
        remove => RemoveHandler(InputBlurredEvent, value);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>The current text value.</summary>
    public string? Value
    {
        get => _value;
        set => SetAndRaise(ValueProperty, ref _value, value);
    }

    /// <summary>Placeholder text shown when the input is empty.</summary>
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Where the label is rendered relative to the input field.</summary>
    public InputLabelPlacement LabelPlacement
    {
        get => GetValue(LabelPlacementProperty);
        set => SetValue(LabelPlacementProperty, value);
    }

    /// <summary>Visual fill style of the input container.</summary>
    public InputFill Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>Corner radius shape of the input container.</summary>
    public InputShape Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    /// <summary>When <see langword="true"/> the input text cannot be edited.</summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// Maximum number of characters allowed. 0 means unlimited.
    /// Also controls the counter display when <see cref="Counter"/> is <see langword="true"/>.
    /// </summary>
    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    /// <summary>
    /// When <see langword="true"/>, a clear button appears while the input has a value.
    /// </summary>
    public bool ClearInput
    {
        get => GetValue(ClearInputProperty);
        set => SetValue(ClearInputProperty, value);
    }

    /// <summary>Helper text shown below the input. Hidden when <see cref="ErrorText"/> is set.</summary>
    public string? HelperText
    {
        get => GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    /// <summary>Error message shown below the input. Takes priority over <see cref="HelperText"/>.</summary>
    public string? ErrorText
    {
        get => GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    /// <summary>When <see langword="true"/>, shows a character count below the input.</summary>
    public bool Counter
    {
        get => GetValue(CounterProperty);
        set => SetValue(CounterProperty, value);
    }

    /// <summary>Content placed at the leading edge of the input. Ionic <c>start</c> slot.</summary>
    public object? StartContent
    {
        get => GetValue(StartContentProperty);
        set => SetValue(StartContentProperty, value);
    }

    /// <summary>Content placed at the trailing edge of the input. Ionic <c>end</c> slot.</summary>
    public object? EndContent
    {
        get => GetValue(EndContentProperty);
        set => SetValue(EndContentProperty, value);
    }

    // ── Template parts ───────────────────────────────────────────────────────

    private TextBox? _textBox;
    private Button?  _clearButton;

    // ── Template ─────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_textBox != null)
        {
            _textBox.TextChanged -= OnTextBoxTextChanged;
            _textBox.GotFocus    -= OnTextBoxGotFocus;
            _textBox.LostFocus   -= OnTextBoxLostFocus;
        }

        if (_clearButton != null)
            _clearButton.Click -= OnClearButtonClick;

        _textBox     = e.NameScope.Find<TextBox>("PART_TextBox");
        _clearButton = e.NameScope.Find<Button>("PART_ClearButton");

        if (_textBox != null)
        {
            // Sync template state from current property values
            SyncTextBoxFromValue();
            SyncTextBoxReadOnly();
            SyncTextBoxMaxLength();

            _textBox.TextChanged += OnTextBoxTextChanged;
            _textBox.GotFocus    += OnTextBoxGotFocus;
            _textBox.LostFocus   += OnTextBoxLostFocus;
        }

        if (_clearButton != null)
            _clearButton.Click += OnClearButtonClick;

        UpdateAllPseudoClasses();
    }

    // ── Property change ───────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            SyncTextBoxFromValue();
            PseudoClasses.Set(":has-value", !string.IsNullOrEmpty(_value));
            UpdateCounterText();
        }

        if (change.Property == LabelPlacementProperty)
            UpdateLabelPlacementPseudoClasses(change.GetNewValue<InputLabelPlacement>());

        if (change.Property == FillProperty)
            UpdateFillPseudoClasses(change.GetNewValue<InputFill>());

        if (change.Property == ShapeProperty)
            PseudoClasses.Set(":shape-round", change.GetNewValue<InputShape>() == InputShape.Round);

        if (change.Property == IsReadOnlyProperty)
        {
            SyncTextBoxReadOnly();
            PseudoClasses.Set(":readonly", change.GetNewValue<bool>());
        }

        if (change.Property == MaxLengthProperty)
            SyncTextBoxMaxLength();

        if (change.Property == ErrorTextProperty)
            PseudoClasses.Set(":has-error", !string.IsNullOrEmpty(change.GetNewValue<string?>()));
    }

    // ── TextBox event handlers ─────────────────────────────────────────────────

    private void OnTextBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_textBox == null) return;

        var newText = _textBox.Text;
        if (newText == _value) return;

        var oldValue = _value;
        Value = newText;
        RaiseEvent(new InputValueChangedEventArgs(ValueChangedEvent, this, oldValue, newText));
    }

    private void OnTextBoxGotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        PseudoClasses.Set(":focused", true);
        RaiseEvent(new RoutedEventArgs(InputFocusedEvent, this));
    }

    private void OnTextBoxLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PseudoClasses.Set(":focused", false);
        RaiseEvent(new RoutedEventArgs(InputBlurredEvent, this));
    }

    private void OnClearButtonClick(object? sender, RoutedEventArgs e)
    {
        var oldValue = _value;
        Value = string.Empty;

        if (_textBox != null)
            _textBox.Text = string.Empty;

        RaiseEvent(new InputValueChangedEventArgs(ValueChangedEvent, this, oldValue, string.Empty));
        _textBox?.Focus();
    }

    // ── Sync helpers ──────────────────────────────────────────────────────────

    private void SyncTextBoxFromValue()
    {
        if (_textBox == null || _textBox.Text == _value) return;
        _textBox.Text = _value;
    }

    private void SyncTextBoxReadOnly()
    {
        if (_textBox != null)
            _textBox.IsReadOnly = IsReadOnly;
    }

    private void SyncTextBoxMaxLength()
    {
        if (_textBox != null)
            _textBox.MaxLength = MaxLength > 0 ? MaxLength : int.MaxValue;
    }

    private void UpdateCounterText()
    {
        // Counter TextBlock text is updated via binding in the template.
        // The PART_Counter TextBlock binds to Value.Length / MaxLength directly.
    }

    // ── Pseudoclass helpers ───────────────────────────────────────────────────

    private void UpdateAllPseudoClasses()
    {
        UpdateLabelPlacementPseudoClasses(LabelPlacement);
        UpdateFillPseudoClasses(Fill);
        PseudoClasses.Set(":shape-round",  Shape == InputShape.Round);
        PseudoClasses.Set(":has-value",    !string.IsNullOrEmpty(_value));
        PseudoClasses.Set(":has-error",    !string.IsNullOrEmpty(ErrorText));
        PseudoClasses.Set(":readonly",     IsReadOnly);
    }

    private void UpdateLabelPlacementPseudoClasses(InputLabelPlacement placement)
    {
        PseudoClasses.Set(":label-start",    placement == InputLabelPlacement.Start);
        PseudoClasses.Set(":label-end",      placement == InputLabelPlacement.End);
        PseudoClasses.Set(":label-fixed",    placement == InputLabelPlacement.Fixed);
        PseudoClasses.Set(":label-stacked",  placement == InputLabelPlacement.Stacked);
        PseudoClasses.Set(":label-floating", placement == InputLabelPlacement.Floating);
    }

    private void UpdateFillPseudoClasses(InputFill fill)
    {
        PseudoClasses.Set(":fill-solid",   fill == InputFill.Solid);
        PseudoClasses.Set(":fill-outline", fill == InputFill.Outline);
    }
}
