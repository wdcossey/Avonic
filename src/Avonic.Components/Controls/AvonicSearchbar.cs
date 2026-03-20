using Avalonia.Threading;

namespace Avonic.Components.Controls;

/// <summary>
/// A search input with integrated clear and optional cancel actions.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-searchbar</c>
/// </remarks>
[PseudoClasses(":focused", ":has-value", ":show-cancel")]
public class AvonicSearchbar : TemplatedControl
{
    // ── Backing fields ───────────────────────────────────────────────────────

    private string? _text;
    private DispatcherTimer? _debounceTimer;
    private TextBox?         _input;
    private Button?          _clearButton;
    private Button?          _cancelButton;

    // ── DirectProperty ───────────────────────────────────────────────────────

    public static readonly DirectProperty<AvonicSearchbar, string?> TextProperty =
        AvaloniaProperty.RegisterDirect<AvonicSearchbar, string?>(
            nameof(Text),
            o => o._text,
            (o, v) => o.Text = v);

    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<AvonicSearchbar, string?>(nameof(Placeholder), defaultValue: "Search");

    public static readonly StyledProperty<bool> AnimatedProperty =
        AvaloniaProperty.Register<AvonicSearchbar, bool>(nameof(Animated), defaultValue: false);

    public static readonly StyledProperty<bool> ShowCancelButtonProperty =
        AvaloniaProperty.Register<AvonicSearchbar, bool>(nameof(ShowCancelButton), defaultValue: false);

    public static readonly StyledProperty<string> CancelButtonTextProperty =
        AvaloniaProperty.Register<AvonicSearchbar, string>(nameof(CancelButtonText), defaultValue: "Cancel");

    public static readonly StyledProperty<int> DebounceProperty =
        AvaloniaProperty.Register<AvonicSearchbar, int>(nameof(Debounce), defaultValue: 250);

    // ── Routed Events ────────────────────────────────────────────────────────

    public static readonly RoutedEvent<SearchTextChangedEventArgs> TextChangedEvent =
        RoutedEvent.Register<AvonicSearchbar, SearchTextChangedEventArgs>(
            nameof(TextChanged), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> ClearedEvent =
        RoutedEvent.Register<AvonicSearchbar, RoutedEventArgs>(nameof(Cleared), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> CancelledEvent =
        RoutedEvent.Register<AvonicSearchbar, RoutedEventArgs>(nameof(Cancelled), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> SearchFocusedEvent =
        RoutedEvent.Register<AvonicSearchbar, RoutedEventArgs>(nameof(SearchFocused), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> SearchBlurredEvent =
        RoutedEvent.Register<AvonicSearchbar, RoutedEventArgs>(nameof(SearchBlurred), RoutingStrategies.Bubble);

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Current search text. Two-way bindable.</summary>
    public string? Text
    {
        get => _text;
        set
        {
            if (!SetAndRaise(TextProperty, ref _text, value))
                return;

            // Sync the internal TextBox without re-triggering the changed handler
            if (_input != null && _input.Text != value)
                _input.Text = value;

            PseudoClasses.Set(":has-value", !string.IsNullOrEmpty(value));
        }
    }

    /// <summary>Placeholder text shown when the searchbar is empty.</summary>
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Enables an animated slide-in for the search icon.</summary>
    public bool Animated
    {
        get => GetValue(AnimatedProperty);
        set => SetValue(AnimatedProperty, value);
    }

    /// <summary>Shows a "Cancel" button to the right of the input.</summary>
    public bool ShowCancelButton
    {
        get => GetValue(ShowCancelButtonProperty);
        set => SetValue(ShowCancelButtonProperty, value);
    }

    /// <summary>Text label for the cancel button.</summary>
    public string CancelButtonText
    {
        get => GetValue(CancelButtonTextProperty);
        set => SetValue(CancelButtonTextProperty, value);
    }

    /// <summary>Debounce delay in milliseconds applied to <see cref="TextChanged"/>.</summary>
    public int Debounce
    {
        get => GetValue(DebounceProperty);
        set => SetValue(DebounceProperty, value);
    }

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Raised (after debounce) when the search text changes.</summary>
    public event EventHandler<SearchTextChangedEventArgs>? TextChanged
    {
        add    => AddHandler(TextChangedEvent, value);
        remove => RemoveHandler(TextChangedEvent, value);
    }

    /// <summary>Raised when the clear button is tapped.</summary>
    public event EventHandler<RoutedEventArgs>? Cleared
    {
        add    => AddHandler(ClearedEvent, value);
        remove => RemoveHandler(ClearedEvent, value);
    }

    /// <summary>Raised when the cancel button is tapped.</summary>
    public event EventHandler<RoutedEventArgs>? Cancelled
    {
        add    => AddHandler(CancelledEvent, value);
        remove => RemoveHandler(CancelledEvent, value);
    }

    /// <summary>Raised when the inner TextBox receives focus.</summary>
    public event EventHandler<RoutedEventArgs>? SearchFocused
    {
        add    => AddHandler(SearchFocusedEvent, value);
        remove => RemoveHandler(SearchFocusedEvent, value);
    }

    /// <summary>Raised when the inner TextBox loses focus.</summary>
    public event EventHandler<RoutedEventArgs>? SearchBlurred
    {
        add    => AddHandler(SearchBlurredEvent, value);
        remove => RemoveHandler(SearchBlurredEvent, value);
    }

    // ── Template ─────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Detach from old parts
        if (_input != null)
        {
            _input.TextChanged -= OnInputTextChanged;
            _input.GotFocus    -= OnInputGotFocus;
            _input.LostFocus   -= OnInputLostFocus;
        }

        if (_clearButton  != null) _clearButton.Click  -= OnClearClicked;
        if (_cancelButton != null) _cancelButton.Click -= OnCancelClicked;

        _input        = e.NameScope.Find<TextBox>("PART_Input");
        _clearButton  = e.NameScope.Find<Button>("PART_ClearButton");
        _cancelButton = e.NameScope.Find<Button>("PART_CancelButton");

        if (_input != null)
        {
            _input.Text = _text;
            _input.TextChanged += OnInputTextChanged;
            _input.GotFocus    += OnInputGotFocus;
            _input.LostFocus   += OnInputLostFocus;
        }

        if (_clearButton  != null) _clearButton.Click  += OnClearClicked;
        if (_cancelButton != null) _cancelButton.Click += OnCancelClicked;

        PseudoClasses.Set(":has-value",    !string.IsNullOrEmpty(_text));
        PseudoClasses.Set(":show-cancel",  ShowCancelButton);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShowCancelButtonProperty)
            PseudoClasses.Set(":show-cancel", change.GetNewValue<bool>());
    }

    // ── Private Handlers ─────────────────────────────────────────────────────

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        var newText = _input?.Text;
        SetAndRaise(TextProperty, ref _text, newText);
        PseudoClasses.Set(":has-value", !string.IsNullOrEmpty(newText));

        _debounceTimer?.Stop();

        if (Debounce <= 0)
        {
            RaiseEvent(new SearchTextChangedEventArgs(TextChangedEvent, this, newText));
            return;
        }

        _debounceTimer          = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Debounce) };
        _debounceTimer.Tick    += (_, _) =>
        {
            _debounceTimer.Stop();
            RaiseEvent(new SearchTextChangedEventArgs(TextChangedEvent, this, _text));
        };
        _debounceTimer.Start();
    }

    private void OnInputGotFocus(object? sender, GotFocusEventArgs e)
    {
        PseudoClasses.Set(":focused", true);
        RaiseEvent(new RoutedEventArgs(SearchFocusedEvent, this));
    }

    private void OnInputLostFocus(object? sender, RoutedEventArgs e)
    {
        PseudoClasses.Set(":focused", false);
        RaiseEvent(new RoutedEventArgs(SearchBlurredEvent, this));
    }

    private void OnClearClicked(object? sender, RoutedEventArgs e)
    {
        Text = string.Empty;
        _input?.Focus();
        RaiseEvent(new RoutedEventArgs(ClearedEvent, this));
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Text = string.Empty;

        // Move focus away from the input
        if (_input != null)
            TopLevel.GetTopLevel(this)?.Focus();

        RaiseEvent(new RoutedEventArgs(CancelledEvent, this));
    }
}
