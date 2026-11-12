using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-picker</c>: <see cref="AvonicPickerColumn"/>s side by side, left to right whatever the direction, with a highlight
/// behind the centre row and fades over the rows above and below it. Its columns are <see cref="ItemsControl.Items"/>, as
/// <c>core.json</c> lists no slot. Its numeric input mode, internal as in Ionic, lets <c>ion-datetime</c>'s user type the hour and
/// minute. See <c>docs/components/ion-picker/README.md</c>.
/// </summary>
[TemplatePart("PART_Highlight", typeof(Control))]
[TemplatePart("PART_Input", typeof(TextBox))]
public class AvonicPicker : ItemsControl
{
    /// <summary>How long typed digits wait for the next one in a single column, as Ionic's search timeout.</summary>
    internal static readonly TimeSpan SingleColumnSearchDelay = TimeSpan.FromSeconds(1);

    private Control? _highlight;
    private TextBox? _input;
    private bool _useInputMode;
    private AvonicPickerColumn? _inputModeColumn;
    private Action? _actionOnClick;
    private IDisposable? _singleColumnSearchTimeout;
    private bool _keypressListening;
    private bool _settingInput;

    static AvonicPicker()
    {
        // The host's touchstart listener, preventTouchStartPropagation: a touch on the picker starts no gesture around it, so that
        // scrolling a wheel doesn't swipe a sliding item or a card modal.
        Gesture.StopsTouchStartProperty.OverrideDefaultValue<AvonicPicker>(true);
    }

    public AvonicPicker()
    {
        // onPointerDown, ahead of the focus a press gives, as Chromium's pointerdown comes before its focusin.
        AddHandler(PointerPressedEvent, OnPointerDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        // onClick: a DOM click on release, from anything in the picker.
        _ = new NativeButton(this, _ => OnClick(), () => false, () => { });
        AddHandler(GotFocusEvent, OnFocusIn, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(LostFocusEvent, OnFocusOut, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(TextInputEvent, OnKeyPress, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>Ionic's internal <c>ionInputModeChange</c>, which each column listens to.</summary>
    internal event EventHandler<PickerInputModeChange>? InputModeChanged;

    /// <summary>Whether it takes typed digits, as Ionic's <c>useInputMode</c>.</summary>
    internal bool UseInputMode => _useInputMode;

    /// <summary>The one column taking digits, or null for every numeric column.</summary>
    internal AvonicPickerColumn? InputModeColumn => _inputModeColumn;

    /// <summary>The hidden input's text, as Ionic's <c>inputEl.value</c>.</summary>
    internal string InputValue => _input?.Text ?? string.Empty;

    /// <summary><c>querySelectorAll('ion-picker-column')</c>, without looking inside a column, where its options are.</summary>
    private IEnumerable<AvonicPickerColumn> Columns => FindColumns(this);

    private static IEnumerable<AvonicPickerColumn> FindColumns(ILogical parent)
    {
        foreach (var child in parent.LogicalChildren)
        {
            if (child is AvonicPickerColumn column)
            {
                yield return column;
                continue;
            }

            foreach (var inner in FindColumns(child))
            {
                yield return inner;
            }
        }
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<Control>(item, out recycleKey);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_input is not null)
        {
            _input.PropertyChanged -= OnInputPropertyChanged;
            _input.RemoveHandler(KeyDownEvent, OnInputKeyDown);
            _input.RemoveHandler(LostFocusEvent, OnInputBlur);
            _input.RemoveHandler(TextInputEvent, OnInputTextInput);
        }

        _highlight = e.NameScope.Find<Control>("PART_Highlight");
        _input = e.NameScope.Find<TextBox>("PART_Input");
        if (_input is not null)
        {
            _input.PropertyChanged += OnInputPropertyChanged;
            _input.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
            _input.AddHandler(LostFocusEvent, OnInputBlur);
            // type="number": only digits get in.
            _input.AddHandler(TextInputEvent, OnInputTextInput, RoutingStrategies.Tunnel);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // The search's pause ends now, so the digits typed so far don't carry over.
        if (_singleColumnSearchTimeout is not null)
        {
            _singleColumnSearchTimeout.Dispose();
            _singleColumnSearchTimeout = null;
            SetInput(string.Empty);
        }

        base.OnDetachedFromVisualTree(e);
    }

    // ── Pointer and focus ────────────────────────────────────────────────────────────────────

    private bool IsInHighlightBounds(PointerEventArgs e)
    {
        if (_highlight is null || !_highlight.IsAttachedToVisualTree())
        {
            return false;
        }

        // Inside the box or on its edge, as the check is against left/right/top/bottom.
        var point = e.GetPosition(_highlight);
        var size = _highlight.Bounds.Size;
        return point.X >= 0 && point.X <= size.Width && point.Y >= 0 && point.Y <= size.Height;
    }

    /// <summary>
    /// What the press landed on, as the DOM sees it: a column when it's on the column's own parts, which its shadow root
    /// retargets to the host, and not when it's on an option, or a slotted element, which are the light DOM's own.
    /// </summary>
    private static AvonicPickerColumn? ColumnTarget(object? source)
    {
        for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
        {
            switch (visual)
            {
                case AvonicPickerColumnOption:
                    return null;
                case AvonicPickerColumn column:
                    return column;
            }

            if (visual is ContentPresenter { Name: "PART_Prefix" or "PART_Suffix" } presenter && presenter.Content is Visual)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>The column the press is in, whether on an option or on the column's own parts.</summary>
    private static AvonicPickerColumn? OwningColumn(object? source) =>
        (source as Visual)?.GetSelfAndVisualAncestors().OfType<AvonicPickerColumn>().FirstOrDefault();

    /// <summary>Ionic's <c>onPointerDown</c>: what the click that follows does to input mode.</summary>
    private void OnPointerDown(object? sender, PointerPressedEventArgs e)
    {
        if (IsInHighlightBounds(e))
        {
            var target = ColumnTarget(e.Source);
            if (_useInputMode)
            {
                if (target is not null)
                {
                    // The same column again: every numeric column; another one: that one.
                    _actionOnClick = _inputModeColumn is not null && _inputModeColumn == target
                        ? () => EnterInputMode()
                        : () => EnterInputMode(target);
                }
                else
                {
                    _actionOnClick = () => ExitInputMode();
                }
            }
            else
            {
                // With one numeric column, the column tapped, which Ionic's ev.target is (see "Decided" for a tap on an option);
                // with more, every numeric column.
                var numeric = Columns.Count(column => column.NumericInput);
                var columnEl = numeric == 1 ? OwningColumn(e.Source) : null;
                _actionOnClick = () => EnterInputMode(columnEl);
            }

            return;
        }

        _actionOnClick = () => ExitInputMode();
    }

    /// <summary>Ionic's <c>onClick</c>: the action the press chose.</summary>
    private void OnClick()
    {
        var action = _actionOnClick;
        if (action is not null)
        {
            action();
            _actionOnClick = null;
        }
    }

    /// <summary>
    /// Ionic's <c>onFocusIn</c>, for a column only: one focused by the keyboard enters input mode if it's numeric and leaves it
    /// if not. A press waits for its click.
    /// </summary>
    private void OnFocusIn(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is not AvonicPickerColumn column || _actionOnClick is not null)
        {
            return;
        }

        if (column.NumericInput)
        {
            EnterInputMode(column, focusInput: false);
        }
        else
        {
            ExitInputMode();
        }
    }

    /// <summary>Ionic's <c>onFocusOut</c>: focus going anywhere but a column or the hidden input leaves input mode.</summary>
    private void OnFocusOut(object? sender, FocusChangedEventArgs e)
    {
        var related = e.NewFocusedElement;
        if (related is not AvonicPickerColumn && (related is null || related != _input))
        {
            ExitInputMode();
        }
    }

    // ── Input mode ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>enterInputMode</c>: digits go to <paramref name="columnEl"/>, or to every numeric column when it's null. The hidden
    /// input takes focus, which brings up a keyboard, unless a column was focused by the keyboard, which keeps its focus and
    /// listens for digits instead.
    /// </summary>
    internal void EnterInputMode(AvonicPickerColumn? columnEl = null, bool focusInput = true)
    {
        if (_input is null || !Columns.Any(column => column.NumericInput))
        {
            return;
        }

        _useInputMode = true;
        _inputModeColumn = columnEl;
        if (focusInput)
        {
            _keypressListening = false;
            _input.Focus();
        }
        else
        {
            _keypressListening = true;
        }

        EmitInputModeChange();
    }

    /// <summary>Ionic's internal <c>exitInputMode()</c>: blurs the hidden input, which dismisses a keyboard, and clears it.</summary>
    internal void ExitInputMode()
    {
        if (!_useInputMode || _input is null)
        {
            return;
        }

        _useInputMode = false;
        _inputModeColumn = null;
        if (_input.IsFocused)
        {
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
        }

        SetInput(string.Empty);
        _keypressListening = false;
        EmitInputModeChange();
    }

    private void EmitInputModeChange() => InputModeChanged?.Invoke(this, new PickerInputModeChange(_useInputMode, _inputModeColumn));

    /// <summary>Ionic's <c>onKeyPress</c>, while a column focused by the keyboard takes digits: each digit goes into the input.</summary>
    private void OnKeyPress(object? sender, TextInputEventArgs e)
    {
        if (!_keypressListening || _input is null || e.Source == _input || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        // parseInt(ev.key, 10): a key whose text starts with a digit.
        if (char.IsAsciiDigit(e.Text[0]))
        {
            SetInput(InputValue + e.Text);
            OnInputChange();
        }
    }

    private void OnInputTextInput(object? sender, TextInputEventArgs e)
    {
        if (e.Text is { } text && !text.All(char.IsAsciiDigit))
        {
            e.Handled = true;
        }
    }

    /// <summary>Enter is the user done typing: blur the input, which leaves input mode.</summary>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _input is { IsFocused: true })
        {
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
        }
    }

    private void OnInputBlur(object? sender, RoutedEventArgs e) => ExitInputMode();

    /// <summary>The input event: only the user's edits, not the picker's own.</summary>
    private void OnInputPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty && !_settingInput)
        {
            OnInputChange();
        }
    }

    private void SetInput(string value)
    {
        if (_input is null)
        {
            return;
        }

        _settingInput = true;
        try
        {
            _input.Text = value;
        }
        finally
        {
            _settingInput = false;
        }
    }

    /// <summary>Ionic's <c>onInputChange</c>.</summary>
    private void OnInputChange()
    {
        if (!_useInputMode || _input is null)
        {
            return;
        }

        if (_inputModeColumn is not null)
        {
            SelectSingleColumn();
        }
        else
        {
            SelectMultiColumn();
        }
    }

    /// <summary>The text of an option, as Ionic's <c>textContent</c>.</summary>
    internal static string OptionText(AvonicPickerColumnOption option) => TextContent(option.Content);

    private static string TextContent(object? content) => content switch
    {
        null => string.Empty,
        string text => text,
        TextBlock { Inlines: { Count: > 0 } inlines } => string.Concat(inlines.Select(InlineText)),
        TextBlock block => block.Text ?? string.Empty,
        Control control => string.Concat(((ILogical)control).LogicalChildren.Select(TextContent)),
        var other => other.ToString() ?? string.Empty,
    };

    private static string InlineText(Avalonia.Controls.Documents.Inline inline) => inline switch
    {
        Avalonia.Controls.Documents.Run run => run.Text ?? string.Empty,
        Avalonia.Controls.Documents.Span span => string.Concat(span.Inlines.Select(InlineText)),
        Avalonia.Controls.Documents.InlineUIContainer { Child: { } child } => TextContent(child),
        _ => string.Empty,
    };

    /// <summary>
    /// Ionic's <c>selectSingleColumn</c>: the typed digits, leading zeros ignored, pick the option with that text; more than two keep
    /// the last two; two that match nothing start again from the last; a second's pause clears them.
    /// </summary>
    private void SelectSingleColumn()
    {
        if (_input is null || _inputModeColumn is not { } column)
        {
            return;
        }

        var options = column.Options.Where(option => option.IsEnabled).ToList();
        _singleColumnSearchTimeout?.Dispose();
        _singleColumnSearchTimeout = AvonicConfig.Schedule(() =>
        {
            SetInput(string.Empty);
            _singleColumnSearchTimeout = null;
        }, SingleColumnSearchDelay);

        var value = InputValue;
        if (value.Length >= 3)
        {
            SetInput(value[^2..]);
            SelectSingleColumn();
            return;
        }

        var found = options.FirstOrDefault(option => TrimLeadingZeros(OptionText(option)) == value);
        if (found is not null)
        {
            column.SetValue(found.Value);
            return;
        }

        if (value.Length == 2)
        {
            SetInput(value[^1..]);
            SelectSingleColumn();
        }
    }

    private static readonly System.Text.RegularExpressions.Regex LeadingZeros = new("^0+(?=[1-9])|0+(?=0$)");
    private static readonly System.Text.RegularExpressions.Regex ZerosAtStart = new("^0+");
    private static readonly System.Text.RegularExpressions.Regex ZeroAtEnd = new("0$");

    /// <summary>
    /// <c>/^0+(?=[1-9])|0+(?=0$)/</c>, with no <c>g</c>, so only the first match: the zeros before a digit, or all but the last of a
    /// string of zeros.
    /// </summary>
    private static string TrimLeadingZeros(string text) => LeadingZeros.Replace(text, string.Empty, 1);

    /// <summary>
    /// Ionic's <c>searchColumn</c>: the enabled option whose text, without its zeros at the start (or the end), is
    /// <paramref name="value"/>'s, becomes the column's value.
    /// </summary>
    private static bool SearchColumn(AvonicPickerColumn? column, string value, bool zerosAtStart = true)
    {
        if (column is null || string.IsNullOrEmpty(value))
        {
            return false;
        }

        var pattern = zerosAtStart ? ZerosAtStart : ZeroAtEnd;
        string Strip(string text) => pattern.Replace(text, string.Empty, 1);
        value = Strip(value);
        var option = column.Options.FirstOrDefault(o => o.IsEnabled && Strip(OptionText(o)) == value);
        if (option is not null)
        {
            column.SetValue(option.Value);
        }

        return option is not null;
    }

    /// <summary>Ionic's <c>multiColumnSearch</c>: the first two digits are the first column's, the next two the second's.</summary>
    private static void MultiColumnSearch(AvonicPickerColumn? firstColumn, AvonicPickerColumn? secondColumn, string input)
    {
        if (input.Length == 0)
        {
            return;
        }

        var digits = input.Select(c => c.ToString()).ToList();
        var hourValue = string.Concat(digits.Take(2));
        var foundHour = SearchColumn(firstColumn, hourValue);
        if (digits.Count > 2 && foundHour)
        {
            SearchColumn(secondColumn, string.Concat(digits.Skip(2).Take(2)));
        }
        else if (!foundHour && digits.Count >= 1)
        {
            var singleDigitFound = SearchColumn(firstColumn, digits[0]);
            if (!singleDigitFound)
            {
                digits.RemoveAt(0);
                singleDigitFound = digits.Count > 0 && SearchColumn(firstColumn, digits[0]);
            }

            if (singleDigitFound && digits.Count > 1)
            {
                SearchColumn(secondColumn, string.Concat(digits.Skip(1).Take(2)));
            }
        }
    }

    /// <summary>Ionic's <c>selectMultiColumn</c>: the last four digits, over the first two numeric columns.</summary>
    private void SelectMultiColumn()
    {
        if (_input is null)
        {
            return;
        }

        var numeric = Columns.Where(column => column.NumericInput).ToList();
        var value = InputValue;
        if (value.Length > 4)
        {
            value = value[^4..];
            SetInput(value);
        }

        MultiColumnSearch(numeric.ElementAtOrDefault(0), numeric.ElementAtOrDefault(1), value);
    }
}
