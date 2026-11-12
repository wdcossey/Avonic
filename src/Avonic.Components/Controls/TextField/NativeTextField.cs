using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// The native <c>&lt;input&gt;</c> or <c>&lt;textarea&gt;</c> of <see cref="AvonicInput"/>, <see cref="AvonicTextarea"/> and
/// <see cref="AvonicSearchbar"/>: a <see cref="TextBox"/> in the host's template, <c>PART_NativeInput</c>. The host's value and
/// the text box's <c>Text</c> are kept in step with a guard, so that only the user's edits reach <see cref="Edited"/>, as only
/// the native <c>input</c> event does in Ionic. What the three do differently, from their own <c>.js</c>, is the host's.
/// </summary>
internal sealed class NativeTextField
{
    /// <summary>What Chromium draws for each character of a password.</summary>
    public const char PasswordMask = '•';

    private readonly TemplatedControl _host;
    private readonly StyledProperty<string?> _value;
    private readonly StyledProperty<int?> _maxlength;
    private bool _syncing;
    private bool _inUserEdit;
    private IDisposable? _debounced;

    public NativeTextField(TemplatedControl host, StyledProperty<string?> value, StyledProperty<int?> maxlength)
    {
        _host = host;
        _value = value;
        _maxlength = maxlength;
    }

    /// <summary>The text box, once the template has been applied, or null if it has no <c>PART_NativeInput</c>.</summary>
    public TextBox? Native { get; private set; }

    /// <summary>
    /// <c>valueChanged</c> waits for an IME's composition to end, as <c>input.js</c> checks <c>isComposing</c>. The textarea's
    /// and the searchbar's don't.
    /// </summary>
    public bool WaitsForComposition { get; init; }

    /// <summary>
    /// The text box's own requests to bring its caret into view are dropped while it isn't focused: Avalonia's text presenter
    /// makes one whenever the caret moves, focused or not, and the caret moves to the end of an assigned value, where Chromium
    /// never scrolls to an input that isn't focused.
    /// </summary>
    public bool StaysOutOfView { get; init; }

    /// <summary>
    /// <see cref="KeyDown"/> comes on the way down, handled or not, before the text box types the key, as <c>checkClearOnEdit</c>
    /// needs. Otherwise it comes on the way up, after the key's listeners, and not if one handled it, as a default action does.
    /// </summary>
    public bool KeyDownFirst { get; init; }

    /// <summary>The user edited the text, which is this: <c>onInput</c>.</summary>
    public Action<string>? Edited { get; init; }

    /// <summary>The text box has focus: <c>onFocus</c>.</summary>
    public Action? Focused { get; init; }

    /// <summary>The text box lost focus: <c>onBlur</c>.</summary>
    public Action? Blurred { get; init; }

    public Action<KeyEventArgs>? KeyDown { get; init; }

    /// <summary>Text typed with no key down, such as by a soft keyboard, where Chromium's <c>keydown</c> says "Unidentified".</summary>
    public Action? Typing { get; init; }

    /// <summary>While true, the value isn't shown in the text box: the textarea's text is being normalised.</summary>
    public bool IsHeld { get; set; }

    /// <summary><c>getValue</c>: null is <c>""</c>.</summary>
    private string CurrentValue => _host.GetValue(_value) ?? "";

    /// <summary>Follows the template's text box, and leaves the last one. The host then shows its options and calls <see cref="Sync"/>.</summary>
    public void Attach(TextBox? native)
    {
        if (Native is { } old)
        {
            old.PropertyChanged -= OnPropertyChanged;
            old.GotFocus -= OnGotFocus;
            old.LostFocus -= OnLostFocus;
            old.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            old.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
            old.PastingFromClipboard -= OnPasting;
            old.RemoveHandler(Control.RequestBringIntoViewEvent, OnRequestBringIntoView);
        }

        Native = native;
        if (native is null)
        {
            return;
        }

        // Text's own change, which is synchronous: TextChanged is posted, after the guard has gone.
        native.PropertyChanged += OnPropertyChanged;
        native.GotFocus += OnGotFocus;
        native.LostFocus += OnLostFocus;
        if (KeyDownFirst)
        {
            native.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        else
        {
            native.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
        }

        native.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel, handledEventsToo: true);
        native.PastingFromClipboard += OnPasting;
        if (StaysOutOfView)
        {
            native.AddHandler(Control.RequestBringIntoViewEvent, OnRequestBringIntoView);
        }
    }

    /// <summary><c>valueChanged</c>: assigning the native field's value, which lets an <c>ionInput</c> handler replace it.</summary>
    public void Sync()
    {
        if (Native is not { } native || _syncing || IsHeld || native.Text == CurrentValue
            || (WaitsForComposition && native.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault() is { PreeditText.Length: > 0 }))
        {
            return;
        }

        SetText(CurrentValue, CurrentValue.Length);
        if (_inUserEdit)
        {
            // Assigned during the user's edit, which the text box finishes by placing its caret after what was typed.
            // Chromium leaves the caret at the end of an assigned value.
            Dispatcher.UIThread.Post(() => native.CaretIndex = native.Text?.Length ?? 0, DispatcherPriority.Send);
        }
    }

    /// <summary>Puts text and a caret in the text box, which isn't the user's edit.</summary>
    public void SetText(string text, int caret)
    {
        if (Native is not { } native)
        {
            return;
        }

        _syncing = true;
        try
        {
            native.Text = text;
            native.CaretIndex = caret;
        }
        finally
        {
            _syncing = false;
        }
    }

    /// <summary>The value from the user, which the text box then shows, as a handler of the value's change can have replaced it.</summary>
    public void SetValueFromUser(string value)
    {
        _syncing = true;
        try
        {
            _host.SetCurrentValue(_value, value);
        }
        finally
        {
            _syncing = false;
        }

        Sync();
    }

    /// <summary><c>onInput</c>'s work: the value from the user's edit, then <paramref name="emitInput"/>.</summary>
    public void UserEdit(string value, Action emitInput)
    {
        _inUserEdit = true;
        try
        {
            SetValueFromUser(value);
            emitInput();
        }
        finally
        {
            _inUserEdit = false;
        }
    }

    /// <summary>
    /// <c>emitInputChange</c> through <c>debounceEvent</c>: <paramref name="raise"/> at once with no <paramref name="debounce"/>,
    /// else that many milliseconds after the last call, which is the one that's raised.
    /// </summary>
    public void Debounced(int? debounce, Action raise)
    {
        if (debounce is not { } wait)
        {
            raise();
            return;
        }

        _debounced?.Dispose();
        IDisposable? handle = null;
        handle = AvonicConfig.Schedule(() =>
        {
            if (_debounced == handle)
            {
                _debounced = null;
            }

            raise();
        }, TimeSpan.FromMilliseconds(Math.Max(0, wait)));
        _debounced = handle;
    }

    /// <summary><c>debounceChanged</c> replaces the emitter, and the old one's timer still runs: a pending event is still raised.</summary>
    public void ForgetDebounce() => _debounced = null;

    /// <summary>
    /// What <c>render</c> puts on the native field that the three share: <c>maxlength</c>, <c>placeholder</c>,
    /// <c>enterkeyhint</c>, <c>autocapitalize</c>, and the keyboard, from <see cref="ContentType"/>.
    /// </summary>
    public static void Apply(TextBox native, int? maxlength, string? placeholder, TextInputContentType contentType,
        Enum? enterkeyhint, string? autocapitalize)
    {
        // A negative maxlength is invalid in HTML, so no limit; 0 allows nothing, which OnTextInput keeps out.
        native.MaxLength = maxlength is > 0 and var max ? max : 0;
        native.PlaceholderText = placeholder ?? "";

        TextInputOptions.SetContentType(native, contentType);
        TextInputOptions.SetReturnKeyType(native, enterkeyhint?.ToString() switch
        {
            "Done" => TextInputReturnKeyType.Done,
            "Enter" => TextInputReturnKeyType.Return,
            "Go" => TextInputReturnKeyType.Go,
            "Next" => TextInputReturnKeyType.Next,
            "Previous" => TextInputReturnKeyType.Previous,
            "Search" => TextInputReturnKeyType.Search,
            "Send" => TextInputReturnKeyType.Send,
            _ => TextInputReturnKeyType.Default,
        });

        var capitalize = autocapitalize?.Trim().ToLowerInvariant();
        TextInputOptions.SetAutoCapitalization(native, capitalize is "on" or "sentences" or "words");
        TextInputOptions.SetUppercase(native, capitalize == "characters");
    }

    /// <summary>
    /// The keyboard: <c>inputmode</c> if it's set, as it beats the type's keyboard, else <c>type</c>'s. Each control's enums,
    /// by name, as they name Ionic's values.
    /// </summary>
    public static TextInputContentType ContentType(Enum? inputmode, Enum? type) => inputmode?.ToString() switch
    {
        "Decimal" => TextInputContentType.Number,
        "Email" => TextInputContentType.Email,
        "Numeric" or "Tel" => TextInputContentType.Digits,
        "Search" => TextInputContentType.Search,
        "Text" => TextInputContentType.Normal,
        "Url" => TextInputContentType.Url,
        _ => type?.ToString() switch
        {
            "Email" => TextInputContentType.Email,
            "Number" => TextInputContentType.Number,
            "Password" => TextInputContentType.Password,
            "Search" => TextInputContentType.Search,
            "Tel" => TextInputContentType.Digits,
            "Url" => TextInputContentType.Url,
            _ => TextInputContentType.Normal,
        },
    };

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.TextProperty || _syncing || Native is not { } native)
        {
            return;
        }

        Edited?.Invoke(native.Text ?? "");
    }

    private void OnGotFocus(object? sender, FocusChangedEventArgs e) => Focused?.Invoke();

    private void OnLostFocus(object? sender, FocusChangedEventArgs e) => Blurred?.Invoke();

    private void OnKeyDown(object? sender, KeyEventArgs e) => KeyDown?.Invoke(e);

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        // maxlength="0": nothing can be typed, which Avalonia's MaxLength of 0, no limit, doesn't say.
        if (_host.GetValue(_maxlength) == 0)
        {
            e.Handled = true;
            return;
        }

        Typing?.Invoke();
    }

    private void OnPasting(object? sender, RoutedEventArgs e)
    {
        if (_host.GetValue(_maxlength) == 0)
        {
            e.Handled = true;
        }
    }

    private void OnRequestBringIntoView(object? sender, RequestBringIntoViewEventArgs e)
    {
        if (Native is { IsKeyboardFocusWithin: false })
        {
            e.Handled = true;
        }
    }
}
