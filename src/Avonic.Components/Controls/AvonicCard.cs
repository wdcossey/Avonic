using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>
/// A content container with elevation/shadow, optionally tappable as a button.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-card</c>
/// </remarks>
[PseudoClasses(":button")]
public class AvonicCard : ContentControl
{
    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<bool> ButtonProperty =
        AvaloniaProperty.Register<AvonicCard, bool>(nameof(Button), defaultValue: false);

    /// <summary>
    /// When <see langword="true"/>, the card is interactive — it responds to press/release
    /// and raises the <see cref="Click"/> event.
    /// </summary>
    public bool Button
    {
        get => GetValue(ButtonProperty);
        set => SetValue(ButtonProperty, value);
    }

    // ── Routed Events ────────────────────────────────────────────────────────

    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<AvonicCard, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    /// <summary>Raised when the card is tapped (only when <see cref="Button"/> is <see langword="true"/>).</summary>
    public event EventHandler<RoutedEventArgs>? Click
    {
        add    => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    // ── State ────────────────────────────────────────────────────────────────

    private bool _isPointerCaptured;
    private bool _pointerLeftBounds;

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ButtonProperty)
        {
            var isButton = change.GetNewValue<bool>();
            PseudoClasses.Set(":button", isButton);
            Cursor = isButton ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
        }
    }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        PseudoClasses.Set(":button", Button);
        Cursor = Button ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
    }

    // ── Input Handling ───────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!Button || !IsEffectivelyEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        e.Pointer.Capture(this);
        _isPointerCaptured = true;
        _pointerLeftBounds = false;

        PseudoClasses.Set(":pressed", true);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPointerCaptured)
            _pointerLeftBounds = !new Rect(Bounds.Size).Contains(e.GetPosition(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isPointerCaptured)
            return;

        _isPointerCaptured = false;
        e.Pointer.Capture(null);

        PseudoClasses.Set(":pressed", false);

        if (!_pointerLeftBounds)
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));

        _pointerLeftBounds = false;
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (!_isPointerCaptured)
            return;

        _isPointerCaptured = false;
        PseudoClasses.Set(":pressed", false);
    }
}
