using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Behaviors;

/// <summary>
/// What a native <c>&lt;button&gt;</c> does, for the controls that have one in Ionic's shadow root: a click on release, Enter
/// and Space, and <c>Command</c>, which follows Avalonia's own <c>Button</c>. See <c>docs/components/ion-item/PLAN.md</c>. The
/// chip, which has none, uses it with no keys for the plain DOM <c>click</c>, which a press raises the same way.
/// </summary>
/// <remarks>
/// It tracks the press and release itself. <c>Tapped</c> has a small movement limit even for a mouse, and isn't raised for a
/// second quick press. It handles nothing, so that a control around this one still sees the same press.
/// </remarks>
internal sealed class NativeButton
{
    /// <summary>Measured in Chromium: a finger that moved 14 px clicks, and one that moved 16 doesn't. A mouse has no limit.</summary>
    private const double TouchSlop = 15;

    /// <summary>
    /// A <see cref="NativeButton"/> clicked, after its control's own <c>Click</c> has been through, whichever control it is: what a
    /// control around needs of the DOM <c>click</c> bubbling to it, such as the accordion's header. <see cref="ClickInsideArgs.Stopped"/>
    /// is a <c>Click</c> handler's <c>Handled</c>, <c>stopPropagation</c>.
    /// </summary>
    internal static readonly RoutedEvent<ClickInsideArgs> ClickInsideEvent =
        RoutedEvent.Register<Control, ClickInsideArgs>("NativeButtonClickInside", RoutingStrategies.Bubble);

    private readonly Control _owner;
    private readonly Action<object?> _activate;
    private readonly Func<bool> _takesKeys;
    private readonly Action _canExecuteChanged;
    private IPointer? _pointer;
    private object? _source;
    private Point _start;
    private bool _spacePressed;
    private bool _subscribed;
    private bool _stoppedDuringPress;
    private RoutedEvent? _nestedClick;
    private bool _routingPointer;
    private Button? _stockButtonClicked;
    private RoutedEventArgs? _stockClick;
    private readonly List<Delegate> _clickHandlers = [];
    private TopLevel? _watchedForFingers;

    /// <param name="owner">The control.</param>
    /// <param name="activate">A click, or Enter or Space. It's given what the press landed on, or the control for a key.</param>
    /// <param name="takesKeys">Whether Enter and Space do anything. A plain item has no native button to take them.</param>
    /// <param name="canExecuteChanged">Calls <c>UpdateIsEffectivelyEnabled</c>, which only the control can.</param>
    public NativeButton(Control owner, Action<object?> activate, Func<bool> takesKeys, Action canExecuteChanged)
    {
        _owner = owner;
        _activate = activate;
        _takesKeys = takesKeys;
        _canExecuteChanged = canExecuteChanged;

        owner.AddHandler(InputElement.PointerPressedEvent, OnPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        owner.AddHandler(InputElement.PointerPressedEvent, OnPressedBubble, RoutingStrategies.Bubble, handledEventsToo: true);
        owner.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        owner.AddHandler(InputElement.PointerReleasedEvent, OnReleasedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        owner.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        owner.AddHandler(Button.ClickEvent, OnStockButtonClick, RoutingStrategies.Bubble, handledEventsToo: true);
        owner.AddHandler(ClickInsideEvent, OnNativeButtonClickInside);
        owner.AddHandler(Control.ContextRequestedEvent, OnContextRequested, RoutingStrategies.Bubble, handledEventsToo: true);
        owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
        owner.AddHandler(InputElement.KeyUpEvent, OnKeyUp);
        owner.AddHandler(InputElement.LostFocusEvent, OnLostFocus);
        owner.AttachedToLogicalTree += (_, _) => Subscribe();
        owner.DetachedFromLogicalTree += (_, _) => Unsubscribe();
        owner.PropertyChanged += OnOwnerPropertyChanged;

        // Made when it's first needed, which can be after the control's in the tree.
        if (((ILogical)owner).IsAttachedToLogicalTree)
        {
            Subscribe();
        }
    }

    private ICommand? Command => _owner.GetValue(AvonicButton.CommandProperty);

    private object? CommandParameter => _owner.GetValue(AvonicButton.CommandParameterProperty);

    /// <summary>A command that can't execute disables the control, as it does a <c>Button</c>.</summary>
    public bool CanExecute { get; private set; } = true;

    /// <summary>
    /// A stock <see cref="Button"/> inside that clicks clicks the control too, as the DOM <c>click</c> of a <c>&lt;button&gt;</c>
    /// inside bubbles to the host: from a key or automation, with no press, at once; from a press, with the press's own click on
    /// release, once, so that the two aren't counted twice. Not a <see cref="RepeatButton"/>, a scrollbar's or a spinner's, nor a
    /// click that a handler stopped. For a control whose click is the plain DOM <c>click</c> on its host.
    /// </summary>
    public bool StockButtonsClick { get; init; }

    /// <summary>
    /// With <see cref="StockButtonsClick"/>, called with the stock <see cref="Button"/> just before its click activates the control:
    /// the button's own <c>onClick</c>, before the DOM click reaches the host.
    /// </summary>
    public Action<Button>? StockButtonClicked { get; init; }

    /// <summary>
    /// Whether anything listens to the control's click: a handler added through <see cref="AddClickHandler"/>, or a command. Chromium
    /// offers a screen reader the DOM <c>click</c> only on an element something listens to, so this is when <c>Invoke</c> is offered.
    /// </summary>
    public bool HasClickListeners => _clickHandlers.Count > 0 || Command is not null;

    /// <summary>
    /// The control's <c>Click</c> event's <c>add</c>, kept for <see cref="HasClickListeners"/>. A null handler does nothing, as with
    /// a field-like event.
    /// </summary>
    public void AddClickHandler(RoutedEvent<RoutedEventArgs> click, EventHandler<RoutedEventArgs>? handler)
    {
        if (handler is not null)
        {
            _owner.AddHandler(click, handler);
            _clickHandlers.Add(handler);
        }
    }

    /// <summary>The control's <c>Click</c> event's <c>remove</c>. A handler that wasn't added does nothing.</summary>
    public void RemoveClickHandler(RoutedEvent<RoutedEventArgs> click, EventHandler<RoutedEventArgs>? handler)
    {
        if (handler is not null && _clickHandlers.Remove(handler))
        {
            _owner.RemoveHandler(click, handler);
        }
    }

    /// <summary>A control that stops being a button mid-press doesn't finish it.</summary>
    public void Reset()
    {
        EndPress();
        _spacePressed = false;
    }

    /// <summary>
    /// The control's <c>Click</c>, then its command, as Avalonia's own <c>Button</c>: setting <c>Handled</c> stops the command. A
    /// browser has one click, which reaches each control on its way up. So if one of this type inside has clicked, its
    /// <c>Click</c> has already been through this control's handlers, and raising another would run them twice.
    /// </summary>
    public void Click(RoutedEvent<RoutedEventArgs> click)
    {
        var handled = false;
        if (!ReferenceEquals(_nestedClick, click))
        {
            var args = new RoutedEventArgs(click, _owner);
            _owner.RaiseEvent(args);
            handled = args.Handled;
        }

        _nestedClick = null;
        _owner.RaiseEvent(new ClickInsideArgs(click, handled, _stockClick));

        if (!handled && Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }

    private void EndPress()
    {
        _pointer = null;
        _source = null;

        if (_watchedForFingers is { } top)
        {
            _watchedForFingers = null;
            top.RemoveHandler(InputElement.PointerPressedEvent, OnAnyPress);
        }
    }

    internal sealed class ClickInsideArgs(RoutedEvent click, bool stopped, RoutedEventArgs? stockClick) : RoutedEventArgs(ClickInsideEvent)
    {
        /// <summary>The control's own <c>Click</c> event.</summary>
        public RoutedEvent Click { get; } = click;

        /// <summary>A <c>Click</c> handler set <c>Handled</c>.</summary>
        public bool Stopped { get; } = stopped;

        /// <summary>
        /// The stock <see cref="Button"/>'s <c>Click</c> that this click is, from a key or automation, which goes on bubbling after
        /// it: the DOM's one click, which a control around counts once.
        /// </summary>
        public RoutedEventArgs? StockClick { get; } = stockClick;
    }

    // ── A press ──────────────────────────────────────────────────────────────────────────────

    /// <summary>A new press ends the last one, which a gesture may have taken, so its release never came here.</summary>
    private void OnPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        _stoppedDuringPress = false;
        _nestedClick = null;
        _stockButtonClicked = null;
        _routingPointer = true;
        if (e.Pointer == _pointer)
        {
            EndPress();
        }
    }

    /// <summary>
    /// A child that handles the press keeps it, as <c>stopPropagation</c> does, as measured. Not a stock <c>Button</c>, which
    /// always handles its press, as a native <c>&lt;button&gt;</c> inside clicks both: measured too. One that clicks on press
    /// has already had its say.
    /// </summary>
    private void OnPressedBubble(object? sender, PointerPressedEventArgs e)
    {
        _routingPointer = false;
        if (e.Pointer.IsPrimary && !_stoppedDuringPress && (!e.Handled || IsInStockButton(e.Source)))
        {
            _pointer = e.Pointer;
            _source = e.Source;
            _start = e.GetPosition(_owner);

            // A second finger anywhere means no click, as measured, and this control only sees the ones that land on it.
            if (_watchedForFingers is null && TopLevel.GetTopLevel(_owner) is { } top)
            {
                _watchedForFingers = top;
                top.AddHandler(InputElement.PointerPressedEvent, OnAnyPress, RoutingStrategies.Tunnel, handledEventsToo: true);
            }
        }
    }

    private void OnAnyPress(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer != _pointer)
        {
            EndPress();
        }
    }

    /// <summary>A context menu took the press. A browser that opens one on a long press doesn't click as well. Without one it does, measured.</summary>
    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (e.Handled)
        {
            EndPress();
        }
    }

    private bool IsInStockButton(object? source)
    {
        for (var visual = source as Visual; visual is not null && visual != _owner; visual = visual.GetVisualParent())
        {
            // Not a RepeatButton, which is a scrollbar's or a spinner's. A browser doesn't click on those.
            if (visual is Button and not RepeatButton)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A finger that's moved too far has ended its press for good, even if it comes back.</summary>
    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (e.Pointer == _pointer && e.Pointer.Type == PointerType.Touch)
        {
            var moved = e.GetPosition(_owner) - _start;
            if ((moved.X * moved.X) + (moved.Y * moved.Y) > TouchSlop * TouchSlop)
            {
                EndPress();
            }
        }
    }

    /// <summary>
    /// Measured with two mouse buttons: left, then right down, then left up clicks. Left, then a right tap, then left up
    /// doesn't, and nor does right, then left, then right up, then left up. So releasing any other button ends the press.
    /// </summary>
    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        var stockButton = _stockButtonClicked;
        _stockButtonClicked = null;
        _routingPointer = false;
        if (e.Pointer != _pointer)
        {
            return;
        }

        var source = _source;
        EndPress();

        // Not InitialPressMouseButton, which is the last button pressed, not the one released.
        var isLeft = e.GetCurrentPoint(_owner).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased;
        if (isLeft && _owner.IsEffectivelyVisible && HoldsPointer(e.Pointer) && new Rect(_owner.Bounds.Size).Contains(e.GetPosition(_owner)))
        {
            // Chromium cancels the whole click, the button's with it, when the press isn't one: so the button's onClick only now.
            if (stockButton is not null)
            {
                StockButtonClicked?.Invoke(stockButton);
            }

            _activate(source);
        }
    }

    /// <summary>A release on its way through, during which a stock button inside clicks, before <see cref="OnReleased"/>.</summary>
    private void OnReleasedTunnel(object? sender, PointerReleasedEventArgs e) => _routingPointer = true;

    /// <summary>A press that lost the pointer was cancelled, as <c>pointercancel</c> does, even if it's released over the control.</summary>
    private bool HoldsPointer(IPointer pointer) =>
        pointer.Captured is Visual holder && (holder == _owner || _owner.IsVisualAncestorOf(holder));

    /// <summary>
    /// <c>click.stopPropagation()</c>, in a button inside. A child's release comes before this one's, so this is in time. A
    /// <c>Button</c> with <c>ClickMode.Press</c> clicks before this has seen the press, so that's remembered until the next.
    /// </summary>
    private void OnStockButtonClick(object? sender, RoutedEventArgs e)
    {
        if (e.Handled)
        {
            Stop();
            return;
        }

        if (!StockButtonsClick || e.Source is not Button button || button is RepeatButton)
        {
            return;
        }

        // A press's or a release's own click: the release decides whether the press clicks at all.
        if (_routingPointer)
        {
            _stockButtonClicked = button;
            return;
        }

        StockButtonClicked?.Invoke(button);
        _stockClick = e;
        try
        {
            _activate(button);
        }
        finally
        {
            _stockClick = null;
        }
    }

    private void OnNativeButtonClickInside(object? sender, ClickInsideArgs e)
    {
        if (ReferenceEquals(e.Source, _owner))
        {
            return;
        }

        if (e.Stopped)
        {
            Stop();
        }
        else if (_pointer is not null)
        {
            _nestedClick = e.Click;
        }
    }

    private void Stop()
    {
        EndPress();
        _stoppedDuringPress = true;
    }

    // ── Keys ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>Enter activates on press, and Space on release, with focus on the control itself and not something in it.</summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_takesKeys() || !ReferenceEquals(e.Source, _owner))
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            _activate(_owner);
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            _spacePressed = true;
            e.Handled = true;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && _spacePressed)
        {
            _spacePressed = false;
            _activate(_owner);
            e.Handled = true;
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.Source, _owner))
        {
            _spacePressed = false;
        }
    }

    // ── The control and its command ──────────────────────────────────────────────────────────

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.IsEffectivelyEnabledProperty && !_owner.IsEffectivelyEnabled)
        {
            // A disabled <button> doesn't click, even if it was disabled during the press. Space doesn't need this:
            // disabling takes focus away, which ends it.
            EndPress();
        }
        else if (e.Property == AvonicButton.CommandProperty)
        {
            if (_subscribed)
            {
                if (e.OldValue is ICommand old)
                {
                    old.CanExecuteChanged -= OnCanExecuteChanged;
                }

                if (e.NewValue is ICommand command)
                {
                    command.CanExecuteChanged += OnCanExecuteChanged;
                }
            }

            UpdateCanExecute();
        }
        else if (e.Property == AvonicButton.CommandParameterProperty)
        {
            UpdateCanExecute();
        }
    }

    private void Subscribe()
    {
        if (!_subscribed)
        {
            _subscribed = true;
            if (Command is { } command)
            {
                command.CanExecuteChanged += OnCanExecuteChanged;
            }

            UpdateCanExecute();
        }
    }

    private void Unsubscribe()
    {
        if (_subscribed)
        {
            _subscribed = false;
            if (Command is { } command)
            {
                command.CanExecuteChanged -= OnCanExecuteChanged;
            }
        }
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e) => UpdateCanExecute();

    private void UpdateCanExecute()
    {
        var canExecute = Command is not { } command || command.CanExecute(CommandParameter);
        if (canExecute != CanExecute)
        {
            CanExecute = canExecute;
            _canExecuteChanged();
        }
    }
}
