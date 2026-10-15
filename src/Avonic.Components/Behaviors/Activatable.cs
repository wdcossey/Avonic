using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Behaviors;

/// <summary>
/// Ionic's <c>tap-click</c> utility as an attached behaviour. A surface with
/// <c>Activatable.IsEnabled="True"</c> (Ionic: class <c>ion-activatable</c>) gets the
/// <c>:activated</c> pseudoclass while pressed and, if it contains an <see cref="AvonicRippleEffect"/>,
/// a ripple at the press position.
/// </summary>
/// <remarks>
/// Timeline (from <c>utils/tap-click/index.js</c>):
/// <list type="bullet">
///   <item>Press (any button but the secondary one) → wait <c>ADD_ACTIVATED_DEFERS</c> (100 ms),
///   unless <see cref="IsInstantProperty"/> → activate. Only the innermost activatable under the
///   pointer activates, and only one surface in the whole application is active at a time.</item>
///   <item>Release before that → activate now, then release ("a quick tap still ripples").</item>
///   <item>Release after → release the ripple; if the release comes within <c>CLEAR_STATE_DEFERS</c>
///   (150 ms) of activation, <c>:activated</c> is cleared 150 ms after the release, otherwise
///   immediately. Instant surfaces always clear immediately.</item>
///   <item>Cancel (the pressed element loses pointer capture, e.g. to a scroll gesture, or the
///   surface leaves the tree) → everything cleared immediately; no ripple if not yet activated.</item>
/// </list>
/// Press and release are observed on the surface in the tunnel phase (Ionic listens on
/// <c>document</c> in the capture phase). Cancellation cannot be: Avalonia routes
/// <c>PointerCaptureLost</c> directly to the element that holds capture, so for the duration of a
/// press the behaviour watches whichever element holds the pointer.
/// </remarks>
public static class Activatable
{
    // ── Ionic constants ──────────────────────────────────────────────────────────────────────

    internal static readonly TimeSpan AddActivatedDefer = TimeSpan.FromMilliseconds(100);
    internal static readonly TimeSpan ClearStateDefer   = TimeSpan.FromMilliseconds(150);

    public const string ActivatedPseudoClass = ":activated";

    // ── Attached properties ──────────────────────────────────────────────────────────────────

    /// <summary>Ionic class <c>ion-activatable</c>.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(Activatable));

    /// <summary>Ionic class <c>ion-activatable-instant</c>: activate on press with no defer.</summary>
    public static readonly AttachedProperty<bool> IsInstantProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsInstant", typeof(Activatable));

    private static readonly AttachedProperty<State?> StateProperty =
        AvaloniaProperty.RegisterAttached<Control, State?>("State", typeof(Activatable));

    public static bool GetIsEnabled(Control element) => element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsInstant(Control element) => element.GetValue(IsInstantProperty);
    public static void SetIsInstant(Control element, bool value) => element.SetValue(IsInstantProperty, value);

    /// <summary>Ionic <c>activatableEle</c>: the one surface currently pressed, application-wide.</summary>
    private static State? s_active;

    static Activatable()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>((surface, e) =>
        {
            if (e.GetNewValue<bool>()) Attach(surface);
            else Detach(surface);
        });
    }

    // ── Wiring ───────────────────────────────────────────────────────────────────────────────

    private static void Attach(Control surface)
    {
        if (surface.GetValue(StateProperty) is not null) return;
        surface.SetValue(StateProperty, new State(surface));

        surface.AddHandler(InputElement.PointerPressedEvent,  OnPointerPressed,  RoutingStrategies.Tunnel, handledEventsToo: true);
        surface.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        surface.DetachedFromVisualTree += OnSurfaceDetached;
    }

    private static void Detach(Control surface)
    {
        if (surface.GetValue(StateProperty) is not { } state) return;

        Reset(state);
        surface.RemoveHandler(InputElement.PointerPressedEvent,  OnPointerPressed);
        surface.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        surface.DetachedFromVisualTree -= OnSurfaceDetached;
        surface.SetValue(StateProperty, null);
    }

    private static void OnSurfaceDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control surface && surface.GetValue(StateProperty) is { } state)
            Reset(state);
    }

    /// <summary>Cancel any press and drop any pending or current activation — the surface is going away.</summary>
    private static void Reset(State state)
    {
        Cancel(state);
        state.CancelClear();
        ClearActivated(state);
    }

    // ── tap-click ────────────────────────────────────────────────────────────────────────────

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control surface || surface.GetValue(StateProperty) is not { } state) return;

        // `if (activatableEle || ev.button === 2) return;` — the button that made this press, not button state
        if (s_active is not null) return;
        if (e.GetCurrentPoint(surface).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed) return;

        // getActivatableTarget: the innermost activatable under the pointer wins.
        if (e.Source is not InputElement pressed || HasCloserActivatable(pressed, surface)) return;

        s_active            = state;
        state.IsPressed     = true;
        state.PressPosition = e.GetPosition(surface);

        // setActivatedElement: a new press drops any lingering activated state and its clear defer.
        state.CancelClear();
        ClearActivated(state);

        // Watch whichever element holds the pointer for release and loss of capture.
        var holder = e.Pointer.Captured as InputElement;
        if (holder is null)
        {
            e.Pointer.Capture(pressed);
            holder = pressed;
        }
        state.Watch(holder);

        if (GetIsInstant(surface))
        {
            Activate(state);
        }
        else
        {
            state.ActivateDefer = AvonicConfig.Schedule(() =>
            {
                state.ActivateDefer = null;
                Activate(state);
            }, AddActivatedDefer);
        }
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control surface && surface.GetValue(StateProperty) is { } state)
            Released(state, e.GetPosition(surface));
    }

    /// <summary><c>setActivatedElement(undefined, pointerUpEvent)</c>.</summary>
    private static void Released(State state, Point releasePosition)
    {
        if (!state.IsPressed) return;

        state.CancelActivateDefer();
        if (!state.IsActivated)
        {
            // A quick tap still activates and ripples — from the pointer-up position, as Ionic
            // passes `pointerCoord(ev)` of the release event to addActivated.
            state.PressPosition = releasePosition;
            Activate(state);
        }

        RemoveActivated(state, smooth: true);
        state.EndPress();
    }

    /// <summary><c>cancelActive</c>: a gesture took the pointer, or the surface is going away.</summary>
    private static void Cancel(State state)
    {
        if (!state.IsPressed) return;

        state.CancelActivateDefer();
        if (state.IsActivated)
            RemoveActivated(state, smooth: false);
        state.EndPress();
    }

    /// <summary><c>addActivated</c>.</summary>
    private static void Activate(State state)
    {
        var surface = state.Surface;

        state.IsActivated   = true;
        state.LastActivated = AvonicConfig.Now();
        ((IPseudoClasses)surface.Classes).Add(ActivatedPseudoClass);

        // useRippleEffect = config.animated && config.rippleEffect
        if (!AvonicConfig.Animated || !AvonicConfig.RippleEffect)
            return;

        if (FindRippleEffect(surface) is not { } ripple)
            return;

        state.ReleaseRipple();
        var point = surface.TranslatePoint(state.PressPosition, ripple) ?? state.PressPosition;
        state.ActiveRipple = ripple.AddRipple(point.X, point.Y);
    }

    /// <summary><c>removeActivated(smooth)</c>.</summary>
    private static void RemoveActivated(State state, bool smooth)
    {
        state.ReleaseRipple();

        var sinceActivation = AvonicConfig.Now() - state.LastActivated;
        if (smooth && sinceActivation < ClearStateDefer && !GetIsInstant(state.Surface))
        {
            state.ClearDefer = AvonicConfig.Schedule(() =>
            {
                state.ClearDefer = null;
                ClearActivated(state);
            }, ClearStateDefer);
        }
        else
        {
            ClearActivated(state);
        }
    }

    private static void ClearActivated(State state)
    {
        state.IsActivated = false;
        ((IPseudoClasses)state.Surface.Classes).Remove(ActivatedPseudoClass);
    }

    /// <summary>
    /// <c>getRippleEffect</c>: the first ripple below the surface in tree order. Ionic's
    /// <c>querySelector</c> cannot pierce a nested component's shadow root, so the search does not
    /// descend into another activatable surface.
    /// </summary>
    private static AvonicRippleEffect? FindRippleEffect(Visual root)
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is AvonicRippleEffect ripple)
                return ripple;
            if (child is Control c && GetIsEnabled(c))
                continue;
            if (FindRippleEffect(child) is { } nested)
                return nested;
        }
        return null;
    }

    /// <summary>True when an activatable surface sits between the pressed element and <paramref name="surface"/>.</summary>
    private static bool HasCloserActivatable(Visual pressed, Control surface)
    {
        for (var v = pressed; v is not null && v != surface; v = v.GetVisualParent())
            if (v is Control c && GetIsEnabled(c))
                return true;
        return false;
    }

    // ── Per-surface state ────────────────────────────────────────────────────────────────────

    private sealed class State(Control surface)
    {
        public readonly Control Surface = surface;

        public bool         IsPressed;
        public bool         IsActivated;
        public Point        PressPosition;
        public TimeSpan     LastActivated;
        public IDisposable? ActivateDefer;
        public IDisposable? ClearDefer;
        public Action?      ActiveRipple;

        private InputElement? _holder;

        /// <summary>Watch the element holding the pointer: its release ends the press, losing capture cancels it.</summary>
        public void Watch(InputElement holder)
        {
            Unwatch();
            _holder = holder;
            _holder.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, handledEventsToo: true);
            _holder.AddHandler(InputElement.PointerReleasedEvent,    OnReleased,    handledEventsToo: true);
        }

        private void Unwatch()
        {
            if (_holder is null) return;
            _holder.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            _holder.RemoveHandler(InputElement.PointerReleasedEvent,    OnReleased);
            _holder = null;
        }

        private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            // Capture moving to another element inside the surface (a child capturing itself on
            // press) is the same press, so follow it; capture leaving the surface (a scroll or
            // other gesture taking the pointer) is Ionic's pointercancel.
            if (e.Pointer.Captured is InputElement next && !ReferenceEquals(next, _holder)
                && (ReferenceEquals(next, Surface) || Surface.IsVisualAncestorOf(next)))
            {
                Watch(next);
                return;
            }
            Cancel(this);
        }

        private void OnReleased(object? sender, PointerReleasedEventArgs e) => Released(this, e.GetPosition(Surface));

        /// <summary>The press is over, by release or cancel.</summary>
        public void EndPress()
        {
            IsPressed = false;
            Unwatch();
            if (ReferenceEquals(s_active, this))
                s_active = null;
        }

        public void CancelActivateDefer() { ActivateDefer?.Dispose(); ActivateDefer = null; }
        public void CancelClear()         { ClearDefer?.Dispose();    ClearDefer    = null; }
        public void ReleaseRipple()       { ActiveRipple?.Invoke();   ActiveRipple  = null; }
    }
}
