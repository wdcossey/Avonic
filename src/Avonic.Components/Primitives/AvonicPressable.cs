using Avalonia.Media;

namespace Avonic.Components.Primitives;

/// <summary>
/// A touch-first pressable primitive that provides correct press, release, and cancel
/// interaction states. All interactive Avonic components compose this control.
/// </summary>
/// <remarks>
/// Enforces a minimum touch target as a layout constraint (not a style), ensuring
/// 48dp minimum hit area on all platforms.
/// </remarks>
public class AvonicPressable : ContentControl
{
    // ── Pseudoclass constants ────────────────────────────────────────────────

    public const string PressedPseudoClass   = ":pressed";
    public const string CancelledPseudoClass = ":avonic-cancelled";

    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<double> MinimumTouchTargetProperty =
        AvaloniaProperty.Register<AvonicPressable, double>(
            nameof(MinimumTouchTarget),
            defaultValue: 48.0);

    /// <summary>
    /// Minimum touch target size in device-independent pixels.
    /// Enforced as a layout constraint on both width and height. Default is 48.
    /// </summary>
    public double MinimumTouchTarget
    {
        get => GetValue(MinimumTouchTargetProperty);
        set => SetValue(MinimumTouchTargetProperty, value);
    }

    // ── Routed Events ────────────────────────────────────────────────────────

    public static readonly RoutedEvent<RoutedEventArgs> PressedEvent =
        RoutedEvent.Register<AvonicPressable, RoutedEventArgs>(
            nameof(Pressed), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> ReleasedEvent =
        RoutedEvent.Register<AvonicPressable, RoutedEventArgs>(
            nameof(Released), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> CancelledEvent =
        RoutedEvent.Register<AvonicPressable, RoutedEventArgs>(
            nameof(Cancelled), RoutingStrategies.Bubble);

    /// <summary>Raised when the control is pressed.</summary>
    public event EventHandler<RoutedEventArgs>? Pressed
    {
        add    => AddHandler(PressedEvent, value);
        remove => RemoveHandler(PressedEvent, value);
    }

    /// <summary>Raised when the press is released within the control bounds.</summary>
    public event EventHandler<RoutedEventArgs>? Released
    {
        add    => AddHandler(ReleasedEvent, value);
        remove => RemoveHandler(ReleasedEvent, value);
    }

    /// <summary>
    /// Raised when a press is cancelled — pointer left the bounds while pressed,
    /// or the pointer was captured away.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? Cancelled
    {
        add    => AddHandler(CancelledEvent, value);
        remove => RemoveHandler(CancelledEvent, value);
    }

    // ── Static constructor ───────────────────────────────────────────────────

    static AvonicPressable()
    {
        // Transparent background ensures the control participates in hit testing
        // even when unstyled. Without this, pointer events fall through to the parent.
        BackgroundProperty.OverrideDefaultValue<AvonicPressable>(Brushes.Transparent);
    }

    // ── State ────────────────────────────────────────────────────────────────

    private bool _isPointerCaptured;
    private bool _pointerLeftBoundsWhilePressed;

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = base.MeasureOverride(availableSize);
        return new Size(
            Math.Max(desired.Width,  MinimumTouchTarget),
            Math.Max(desired.Height, MinimumTouchTarget));
    }

    // ── Input Handling ───────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEffectivelyEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        e.Pointer.Capture(this);
        _isPointerCaptured = true;
        _pointerLeftBoundsWhilePressed = false;

        PseudoClasses.Set(CancelledPseudoClass, false);
        PseudoClasses.Set(PressedPseudoClass,   true);

        RaiseEvent(new RoutedEventArgs(PressedEvent, this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isPointerCaptured)
            _pointerLeftBoundsWhilePressed = !new Rect(Bounds.Size).Contains(e.GetPosition(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isPointerCaptured)
            return;

        // Clear the flag before releasing capture — Avalonia fires PointerCaptureLost
        // synchronously from Capture(null), so the guard in OnPointerCaptureLost must
        // already see _isPointerCaptured = false to avoid a double-cancel.
        _isPointerCaptured = false;
        e.Pointer.Capture(null);

        PseudoClasses.Set(PressedPseudoClass, false);

        // If the pointer moved outside bounds during the press, treat as a cancel
        if (_pointerLeftBoundsWhilePressed)
        {
            _pointerLeftBoundsWhilePressed = false;
            PseudoClasses.Set(CancelledPseudoClass, true);
            RaiseEvent(new RoutedEventArgs(CancelledEvent, this));
        }
        else
        {
            RaiseEvent(new RoutedEventArgs(ReleasedEvent, this));
        }

        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (!_isPointerCaptured)
            return;

        _isPointerCaptured = false;

        PseudoClasses.Set(PressedPseudoClass,   false);
        PseudoClasses.Set(CancelledPseudoClass, true);

        RaiseEvent(new RoutedEventArgs(CancelledEvent, this));
    }
}
