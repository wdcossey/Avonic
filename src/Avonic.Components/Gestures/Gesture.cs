using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avonic.Components.Gestures;

/// <summary>
/// Ionic's <c>createGesture</c> (<c>utils/gesture/index.js</c>): follows one pointer pressed on
/// <see cref="GestureConfig.Element"/>, decides at the threshold whether it's a pan along the gesture's axis, captures it if the
/// <see cref="GestureController"/> allows, and reports it through the config's callbacks. It starts disabled: call
/// <see cref="Enable"/>.
/// </summary>
/// <remarks>
/// Capturing takes Avalonia's pointer capture to the element, which cancels whatever held the press (a <c>Button</c>, or an
/// <c>Activatable</c> surface), and keeps every gesture recogniser, a <c>ScrollViewer</c>'s included, off the
/// pointer until it's released. Losing the capture to anything outside the element, such as a scroller taking the pointer or the
/// platform cancelling the touch, is Ionic's <c>touchcancel</c>. See <c>docs/infra/gesture/README.md</c>.
/// </remarks>
public sealed class Gesture : IDisposable
{
    private readonly GestureConfig _config;
    private readonly InputElement _el;
    private readonly GestureDetail _detail = new();
    private readonly PanRecognizer _pan;
    private readonly GestureDelegate _gesture;
    private readonly Action _fireOnMove;

    private bool _hasCapturedPan;
    private bool _hasStartedPan;
    private bool _hasFiredStart = true;
    private bool _isMoveQueued;

    // createPointerEvents: whether presses are listened to, and the pointer whose moves and release are.
    private bool _attached;
    private bool _listening;
    private IPointer? _tracking;
    private IPointer? _held;
    private IDisposable? _frame;
    private bool _destroyed;

    // Counts the presses that started, so that an OnWillStart settling after a later press has started doesn't start that one.
    private int _press;

    // The pointer whose release is on its way, from the tunnel phase: a child letting go of the capture while it handles the
    // release isn't a cancel, as the release is still to reach the element.
    private IPointer? _releasing;

    // Where the tracked mouse or pen last was, so that a move that doesn't move it is dropped: see IsStill.
    private Point _mouseAt;

    // Inside PointerDown or PointerMove, whose callbacks may move the capture. A cancel then waits until they return, as a browser's
    // touchcancel can't arrive in the middle of a callback.
    private bool _deciding;
    private bool _cancelPending;

    /// <summary>
    /// While a gesture takes the pointer, the gestures that heard the capture lost on the way. Each is checked once the capture has
    /// arrived: a gesture around the one that captured still gets the pointer, and one inside it never will again.
    /// </summary>
    private static List<Gesture>? s_handoff;

    private Gesture(GestureConfig config)
    {
        _config = config;
        _el = config.Element ?? throw new ArgumentNullException(nameof(config), "GestureConfig.Element is required.");
        _pan = new PanRecognizer(config.Direction, config.Threshold, config.MaxAngle);
        _gesture = GestureController.Instance.CreateGesture(config.GestureName, config.GesturePriority, config.DisableScroll);
        _fireOnMove = FireOnMove;
    }

    /// <summary>Ionic <c>createGesture(config)</c>. The gesture starts disabled.</summary>
    public static Gesture Create(GestureConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new Gesture(config);
    }

    /// <summary>
    /// Ionic <c>enable(enable = true)</c>: listen for presses, or stop. Disabling a captured gesture ends it with
    /// <see cref="GestureConfig.OnEnd"/>, and the rest of that press is ignored.
    /// </summary>
    public void Enable(bool enable = true)
    {
        if (_destroyed)
        {
            return;
        }

        if (!enable)
        {
            if (_hasCapturedPan)
            {
                PointerUp(null);
            }

            Reset();
        }

        EnablePointerEvents(enable);
    }

    /// <summary>
    /// Ionic <c>destroy()</c>: let go of the controller and stop listening, with no callback. Unlike Ionic's, a move queued for the
    /// next frame and an <see cref="GestureConfig.OnWillStart"/> still pending don't call back after it.
    /// </summary>
    public void Destroy()
    {
        if (_destroyed)
        {
            return;
        }

        _destroyed = true;
        _gesture.Destroy();
        if (_attached)
        {
            _el.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
            _el.RemoveHandler(InputElement.PointerMovedEvent, OnPreviewMoved);
            _el.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
            _el.RemoveHandler(InputElement.PointerReleasedEvent, OnPreviewReleased);
            _el.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
            _el.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
            _attached = false;
        }

        _hasCapturedPan = false;
        _hasStartedPan = false;
        CancelFrame();
        ReleaseHold();
    }

    /// <summary>The same as <see cref="Destroy"/>.</summary>
    public void Dispose() => Destroy();

    // ── index.js ─────────────────────────────────────────────────────────────────────────────

    private bool PointerDown(PointerPressedEventArgs ev)
    {
        var timeStamp = AvonicConfig.EventTime(ev);
        if (_hasStartedPan || !_hasFiredStart)
        {
            return false;
        }

        UpdateDetail(ev, _detail);
        _detail.StartX = _detail.CurrentX;
        _detail.StartY = _detail.CurrentY;
        _detail.StartTime = _detail.CurrentTime = timeStamp;
        _detail.VelocityX = _detail.VelocityY = _detail.DeltaX = _detail.DeltaY = 0;
        _detail.Event = ev;

        // Check if gesture can start
        if (_config.CanStart is { } canStart && !canStart(_detail))
        {
            return false;
        }

        // Release fallback
        _gesture.Release();

        // Start gesture
        if (!_gesture.Start())
        {
            return false;
        }

        _hasStartedPan = true;
        _press++;
        if (_config.Threshold == 0)
        {
            if (TryToCapturePan(ev))
            {
                return true;
            }

            // Ionic returns false here with hasStartedPan still true, and the gesture never starts again until it's disabled.
            // A deviation: let go, as a refused press does.
            Reset();
            return false;
        }

        _pan.Start(_detail.StartX, _detail.StartY);
        return true;
    }

    private void PointerMove(PointerEventArgs ev)
    {
        // fast path, if gesture is currently captured
        if (_hasCapturedPan)
        {
            if (!_isMoveQueued && _hasFiredStart)
            {
                _isMoveQueued = true;
                CalcGestureData(_detail, ev);
                _frame = AvonicConfig.RequestFrame(_el, _fireOnMove);
            }

            return;
        }

        // gesture is currently being detected
        CalcGestureData(_detail, ev);
        if (_pan.Detect(_detail.CurrentX, _detail.CurrentY))
        {
            if (!_pan.IsGesture() || !TryToCapturePan(ev))
            {
                AbortGesture();
            }
        }
    }

    private void FireOnMove()
    {
        _frame = null;

        // onEnd may have come first
        if (!_hasCapturedPan)
        {
            return;
        }

        _isMoveQueued = false;
        _config.OnMove?.Invoke(_detail);
    }

    private bool TryToCapturePan(PointerEventArgs ev)
    {
        if (!_gesture.Capture())
        {
            return false;
        }

        _hasCapturedPan = true;
        _hasFiredStart = false;

        // reset start position since the real user-land event starts here
        _detail.StartX = _detail.CurrentX;
        _detail.StartY = _detail.CurrentY;
        _detail.StartTime = _detail.CurrentTime;

        // The pointer is the element's now. It's let go first, so that every element that held the press hears it lost, as Ionic's
        // ionGestureCaptured reaches tap-click on the document: a Button's press and an Activatable surface's are cancelled, inside
        // the element, around it or the element itself. Then no recogniser gets the pointer until the release.
        // A handler on the way may start another handoff, so this one's list is put back after it.
        var outer = s_handoff;
        var heard = s_handoff = [];
        try
        {
            ev.Pointer.Capture(null);
            ev.Pointer.Capture(_el);
        }
        finally
        {
            s_handoff = outer;
        }

        foreach (var other in heard)
        {
            if (!ReferenceEquals(other, this))
            {
                other.CaptureMoved(ev.Pointer);
            }
        }

        RecognitionHold.Captured(ev);
        _held = null;

        if (_config.OnWillStart is { } onWillStart && onWillStart(_detail) is { } willStart)
        {
            AwaitWillStart(willStart, _press);
        }
        else
        {
            FireOnStart();
        }

        return true;
    }

    /// <summary>
    /// Ionic's <c>onWillStart(detail).then(fireOnStart)</c>. A faulted task is rethrown on the UI thread, and doesn't start. Unlike
    /// Ionic's, it doesn't start after <see cref="Destroy"/>, or once another press has started: Ionic's would fire that press's
    /// <c>onStart</c> early, and let its moves through before its own <c>onWillStart</c> settles.
    /// </summary>
    private async void AwaitWillStart(Task willStart, int press)
    {
        await willStart;
        if (!_destroyed && press == _press)
        {
            FireOnStart();
        }
    }

    private void FireOnStart()
    {
        if (_config.BlurOnStart)
        {
            // blurActiveElement: Focus(null) clears the focus, as Avalonia has no blur.
            TopLevel.GetTopLevel(_el)?.FocusManager?.Focus(null);
        }

        _config.OnStart?.Invoke(_detail);
        _hasFiredStart = true;
    }

    private void Reset()
    {
        _hasCapturedPan = false;
        _hasStartedPan = false;
        _isMoveQueued = false;
        _hasFiredStart = true;
        _gesture.Release();
        CancelFrame();
        ReleaseHold();
    }

    private void CancelFrame()
    {
        _frame?.Dispose();
        _frame = null;
    }

    private void PointerUp(PointerEventArgs? ev)
    {
        var tmpHasCaptured = _hasCapturedPan;
        var tmpHasFiredStart = _hasFiredStart;
        Reset();
        if (!tmpHasFiredStart)
        {
            return;
        }

        CalcGestureData(_detail, ev);

        if (tmpHasCaptured)
        {
            _config.OnEnd?.Invoke(_detail);
            return;
        }

        // Not captured any event
        _config.NotCaptured?.Invoke(_detail);
    }

    private void AbortGesture()
    {
        Reset();
        StopPointerEvents();
        _config.NotCaptured?.Invoke(_detail);
    }

    private static void CalcGestureData(GestureDetail detail, PointerEventArgs? ev)
    {
        if (ev is null)
        {
            return;
        }

        var prevX = detail.CurrentX;
        var prevY = detail.CurrentY;
        var prevT = detail.CurrentTime;
        UpdateDetail(ev, detail);
        var currentX = detail.CurrentX;
        var currentY = detail.CurrentY;
        var timestamp = detail.CurrentTime = AvonicConfig.EventTime(ev);
        var timeDelta = timestamp - prevT;
        if (timeDelta > 0 && timeDelta < 100)
        {
            var velocityX = (currentX - prevX) / timeDelta;
            var velocityY = (currentY - prevY) / timeDelta;
            detail.VelocityX = velocityX * 0.7 + detail.VelocityX * 0.3;
            detail.VelocityY = velocityY * 0.7 + detail.VelocityY * 0.3;
        }

        detail.DeltaX = currentX - detail.StartX;
        detail.DeltaY = currentY - detail.StartY;
        detail.Event = ev;
    }

    private static void UpdateDetail(PointerEventArgs ev, GestureDetail detail)
    {
        var point = ev.GetPosition(null);
        detail.CurrentX = point.X;
        detail.CurrentY = point.Y;
    }

    private void ReleaseHold()
    {
        if (_held is { } held)
        {
            _held = null;
            RecognitionHold.Release(this, held);
        }
    }

    // ── pointer-events.js ────────────────────────────────────────────────────────────────────

    private void EnablePointerEvents(bool enable)
    {
        _listening = enable;
        if (!enable)
        {
            StopPointerEvents();
            return;
        }

        if (_attached)
        {
            return;
        }

        // Ionic listens in the bubble phase, whatever a child did with the event: the DOM has no "handled". The handlers stay on
        // until Destroy, and a disabled gesture ignores presses, so a press that started stays followed, as Ionic's listeners do.
        _el.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        _el.AddHandler(InputElement.PointerMovedEvent, OnPreviewMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        _el.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        _el.AddHandler(InputElement.PointerReleasedEvent, OnPreviewReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        _el.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        _el.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, handledEventsToo: true);
        _attached = true;
    }

    /// <summary>Ionic <c>stop()</c>: stop listening to this press's moves and release.</summary>
    private void StopPointerEvents() => _tracking = null;

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_listening)
        {
            return;
        }

        // Followed during PointerDown, so that the capture moving away during a capture at the press, in OnStart, is a cancel. One
        // pointer at a time: a second press is refused by hasStartedPan, and the first stays followed.
        var previous = _tracking;
        var previousMouseAt = _mouseAt;
        _tracking = e.Pointer;
        _mouseAt = e.GetPosition(null);
        _cancelPending = false;
        bool started;
        _deciding = true;
        try
        {
            started = PointerDown(e);
        }
        finally
        {
            _deciding = false;
        }

        if (!started)
        {
            _cancelPending = false;
            if (ReferenceEquals(_tracking, e.Pointer))
            {
                _tracking = previous;
                _mouseAt = previousMouseAt;
            }

            return;
        }

        // Ionic adds the press's listeners once pointerDown returns true, so a callback that disabled the gesture during it doesn't
        // stop the press being followed.
        _tracking = e.Pointer;
        ApplyPendingCancel(e.Pointer);
    }

    /// <summary>Before the move reaches any recogniser, inside the element or around it, while this gesture is deciding.</summary>
    private void OnPreviewMoved(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(e.Pointer, _tracking) || !_hasStartedPan || _hasCapturedPan)
        {
            return;
        }

        if (ReferenceEquals(_held, e.Pointer) || IsStill(e))
        {
            return;
        }

        _held = e.Pointer;
        RecognitionHold.Hold(this, e.Pointer);
    }

    /// <summary>
    /// A mouse or pen move with the pointer where it was. Windows sends a mouse move when the capture changes, as this gesture's own
    /// capture does, and a pen may send one when only its pressure changes. Ionic hears a mouse or a pen through the DOM's mousemove,
    /// which is for a pointer that moved, so it never sees one: here it reached OnMove with no delta, and ion-item-sliding, at 0,
    /// closed and turned its gesture off (found in a real window). It's dropped before the tunnel holds the scroller for it, too.
    /// Touch is left as it comes.
    /// </summary>
    private bool IsStill(PointerEventArgs e) => e.Pointer.Type != PointerType.Touch && e.GetPosition(null) == _mouseAt;

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(e.Pointer, _tracking))
        {
            return;
        }

        if (IsStill(e))
        {
            return;
        }

        _mouseAt = e.GetPosition(null);

        _deciding = true;
        try
        {
            PointerMove(e);
        }
        finally
        {
            _deciding = false;
        }

        ApplyPendingCancel(e.Pointer);
    }

    private void ApplyPendingCancel(IPointer pointer)
    {
        if (_cancelPending)
        {
            _cancelPending = false;
            CaptureMoved(pointer);
        }
    }

    private void OnPreviewReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ReferenceEquals(e.Pointer, _tracking))
        {
            _releasing = e.Pointer;
        }
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        _releasing = null;
        if (!ReferenceEquals(e.Pointer, _tracking))
        {
            return;
        }

        StopPointerEvents();
        PointerUp(e);
    }

    /// <summary>
    /// Ionic's <c>touchcancel</c>: the capture went somewhere outside the element, to a recogniser, or nowhere, as when the platform
    /// cancels the touch or the element leaves the tree. There's no position, so the detail is the last move's.
    /// </summary>
    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!ReferenceEquals(e.Pointer, _tracking))
        {
            return;
        }

        if (s_handoff is { } handoff)
        {
            if (!handoff.Contains(this))
            {
                handoff.Add(this);
            }

            return;
        }

        CaptureMoved(e.Pointer);
    }

    /// <summary>The capture went somewhere: still this press if it's inside the element, Ionic's <c>touchcancel</c> if not.</summary>
    private void CaptureMoved(IPointer pointer)
    {
        if (!ReferenceEquals(pointer, _tracking))
        {
            return;
        }

        if (pointer.Captured is Visual holder && (ReferenceEquals(holder, _el) || _el.IsVisualAncestorOf(holder)))
        {
            return;
        }

        // A child letting go as it handles the release: the release itself is still to arrive.
        if (pointer.Captured is null && ReferenceEquals(pointer, _releasing))
        {
            return;
        }

        if (_deciding)
        {
            _cancelPending = true;
            return;
        }

        StopPointerEvents();
        PointerUp(null);
    }
}
