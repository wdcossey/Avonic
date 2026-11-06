using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Behaviors;

/// <summary>
/// What a native <c>&lt;button&gt;</c> does, for the controls that have one in Ionic's shadow root: a click on release, Enter
/// and Space, and <c>Command</c>, which follows Avalonia's own <c>Button</c>. See <c>docs/components/ion-item/PLAN.md</c>.
/// </summary>
/// <remarks>
/// It tracks the press and release itself. <c>Tapped</c> has a small movement limit even for a mouse, and isn't raised for a
/// second quick press. It handles nothing, so that a control around this one still sees the same press.
/// </remarks>
internal sealed class NativeButton
{
    /// <summary>Measured in Chromium: a finger that moved 14 px clicks, and one that moved 16 doesn't. A mouse has no limit.</summary>
    private const double TouchSlop = 15;

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
        owner.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        owner.AddHandler(Button.ClickEvent, OnClickInside, RoutingStrategies.Bubble, handledEventsToo: true);
        owner.AddHandler(AvonicButton.ClickEvent, OnClickInside, RoutingStrategies.Bubble, handledEventsToo: true);
        owner.AddHandler(AvonicItem.ClickEvent, OnClickInside, RoutingStrategies.Bubble, handledEventsToo: true);
        owner.AddHandler(AvonicCard.ClickEvent, OnClickInside, RoutingStrategies.Bubble, handledEventsToo: true);
        owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
        owner.AddHandler(InputElement.KeyUpEvent, OnKeyUp);
        owner.AddHandler(InputElement.LostFocusEvent, OnLostFocus);
        owner.AttachedToLogicalTree += (_, _) => Subscribe();
        owner.DetachedFromLogicalTree += (_, _) => Unsubscribe();
        owner.PropertyChanged += OnOwnerPropertyChanged;
    }

    private ICommand? Command => _owner.GetValue(AvonicButton.CommandProperty);

    private object? CommandParameter => _owner.GetValue(AvonicButton.CommandParameterProperty);

    /// <summary>A command that can't execute disables the control, as it does a <c>Button</c>.</summary>
    public bool CanExecute { get; private set; } = true;

    /// <summary>A control that stops being a button mid-press doesn't finish it.</summary>
    public void Reset()
    {
        EndPress();
        _spacePressed = false;
    }

    /// <summary>The control's <c>Click</c>, then its command, as Avalonia's own <c>Button</c>: setting <c>Handled</c> stops the command.</summary>
    public void Click(RoutedEvent<RoutedEventArgs> click)
    {
        var args = new RoutedEventArgs(click, _owner);
        _owner.RaiseEvent(args);

        if (!args.Handled && Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }

    private void EndPress()
    {
        _pointer = null;
        _source = null;
    }

    // ── A press ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A new press ends the last one, which a gesture may have taken, so its release never came here. A second finger
    /// means no click, whichever lifts first, as measured.
    /// </summary>
    private void OnPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        _stoppedDuringPress = false;
        if (e.Pointer == _pointer || !e.Pointer.IsPrimary)
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
        if (e.Pointer.IsPrimary && !_stoppedDuringPress && (!e.Handled || IsInStockButton(e.Source)))
        {
            _pointer = e.Pointer;
            _source = e.Source;
            _start = e.GetPosition(_owner);
        }
    }

    private bool IsInStockButton(object? source)
    {
        for (var visual = source as Visual; visual is not null && visual != _owner; visual = visual.GetVisualParent())
        {
            if (visual is Button)
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
        if (e.Pointer != _pointer)
        {
            return;
        }

        var source = _source;
        EndPress();

        // Not InitialPressMouseButton, which is the last button pressed, not the one released.
        var isLeft = e.GetCurrentPoint(_owner).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased;
        if (isLeft && HoldsPointer(e.Pointer) && new Rect(_owner.Bounds.Size).Contains(e.GetPosition(_owner)))
        {
            _activate(source);
        }
    }

    /// <summary>A press that lost the pointer was cancelled, as <c>pointercancel</c> does, even if it's released over the control.</summary>
    private bool HoldsPointer(IPointer pointer) =>
        pointer.Captured is Visual holder && (holder == _owner || _owner.IsVisualAncestorOf(holder));

    /// <summary>
    /// <c>click.stopPropagation()</c>, in a button inside. A child's release comes before this one's, so this is in time. A
    /// <c>Button</c> with <c>ClickMode.Press</c> clicks before this has seen the press, so that's remembered until the next.
    /// </summary>
    private void OnClickInside(object? sender, RoutedEventArgs e)
    {
        if (e.Handled && !ReferenceEquals(e.Source, _owner))
        {
            EndPress();
            _stoppedDuringPress = true;
        }
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
