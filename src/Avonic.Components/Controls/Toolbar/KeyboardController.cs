using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>createKeyboardController</c> (<c>utils/keyboard/keyboard-controller</c>), for the footer: it calls back when the
/// on-screen keyboard is about to open or close, with what to wait on for the app to have resized back when it closes.
/// </summary>
/// <remarks>
/// Ionic listens to <c>keyboardWillShow</c> and <c>keyboardWillHide</c> on the window, which only a native app's keyboard plugin
/// (Capacitor, Cordova) raises, so a web page never hears them. Here they're the <see cref="TopLevel"/>'s
/// <see cref="TopLevel.InputPane"/> changing state, which Avalonia raises when the pane's animation starts, on Android and iOS; on a
/// desktop there's no pane, and nothing is heard, as on the web. The resize container, <c>ion-app</c>, is the
/// <see cref="TopLevel"/>, whose client height is read as <c>clientHeight</c>. Ionic's resize mode isn't known here: with none, the
/// height doesn't change, and there's nothing to wait on, as there.
/// </remarks>
internal sealed class KeyboardController : IDisposable
{
    /// <summary>Where the input pane comes from. Tests replace it, as a headless window has none.</summary>
    internal static Func<TopLevel, IInputPane?> InputPaneOf { get; set; } = top => top.InputPane;

    private readonly TopLevel _top;
    private readonly Action<bool, Task?> _callback;
    private IInputPane? _pane;

    /// <summary>
    /// <c>initialResizeContainerHeight</c>: the app's height just before the keyboard first opens, read once, as Ionic reads it at
    /// the first <c>keyboardWillShow</c>.
    /// </summary>
    private double? _initialHeight;

    /// <summary>
    /// The wait for the app's height, while one is pending. Ionic makes an observer for each change; one here serves them all, as
    /// they end at the same moment, so they don't pile up on the top level.
    /// </summary>
    private TaskCompletionSource? _resized;

    private KeyboardController(TopLevel top, IInputPane pane, Action<bool, Task?> callback)
    {
        _top = top;
        _pane = pane;
        _callback = callback;
        pane.StateChanged += OnStateChanged;
    }

    /// <summary>
    /// <c>createKeyboardController(callback)</c>: <paramref name="callback"/> gets whether the keyboard is opening, and a task that
    /// completes once the app is back to its height from before the keyboard, or null when there's nothing to wait for. Null when
    /// the top level has no input pane, which means no events, as on the web.
    /// </summary>
    public static KeyboardController? Create(TopLevel top, Action<bool, Task?> callback) =>
        InputPaneOf(top) is { } pane ? new KeyboardController(top, pane, callback) : null;

    private void OnStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        if (_pane is null)
        {
            return;
        }

        var open = e.NewState == InputPaneState.Open;
        if (open)
        {
            // keyboardWillShowHandler
            _initialHeight ??= _top.ClientSize.Height;
        }

        _callback(open, WaitForResize());
    }

    /// <summary>
    /// <c>createResizePromiseIfNeeded</c>: nothing to wait on before the keyboard has first opened, or if the app is already at its
    /// height from before; or else a task that completes when the app's height is back to it, as Ionic's <c>ResizeObserver</c> does.
    /// </summary>
    private Task? WaitForResize()
    {
        if (_initialHeight is not { } initial || initial == 0 || initial == _top.ClientSize.Height)
        {
            return null;
        }

        if (_resized is { } pending)
        {
            return pending.Task;
        }

        // Its continuations run later, as a promise's do, not inside the top level's notification.
        var resized = _resized = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnTopChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TopLevel.ClientSizeProperty && _top.ClientSize.Height == initial)
            {
                _top.PropertyChanged -= OnTopChanged;
                _resized = null;
                resized.TrySetResult();
            }
        }

        _top.PropertyChanged += OnTopChanged;
        return resized.Task;
    }

    /// <summary>
    /// <c>destroy</c>: no more events. A wait already handed out still completes when the app resizes, as Ionic's observer isn't
    /// disconnected either.
    /// </summary>
    public void Dispose()
    {
        if (_pane is { } pane)
        {
            pane.StateChanged -= OnStateChanged;
            _pane = null;
        }
    }
}
