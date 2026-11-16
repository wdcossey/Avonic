using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>startKeyboardAssist</c> (<c>utils/keyboard/keyboard</c>): <see cref="IonKeyboardDidShowEvent"/> and
/// <see cref="IonKeyboardDidHideEvent"/> on the <see cref="TopLevel"/> when the on-screen keyboard has opened or closed. It's started
/// by <see cref="AvonicApp"/>, as Ionic's is by <c>ion-app</c>.
/// </summary>
/// <remarks>
/// Ionic takes them from a native keyboard plugin's <c>keyboardDidShow</c> and <c>keyboardDidHide</c> when there is one (Capacitor,
/// Cordova), and passes each on as it comes, with the plugin's height: that's the path here, as an Avalonia app is native. The
/// events are <see cref="TopLevel.InputPane"/> changing state, which Avalonia reports when the pane's animation starts, with its
/// duration, so each is raised once that has passed, as a plugin's <c>Did</c> events come at its end. A newer change before then
/// replaces it. A desktop has no pane, and nothing is raised, as a desktop browser's viewport doesn't change for a keyboard.
/// </remarks>
public static class KeyboardAssist
{
    /// <summary>Ionic <c>ionKeyboardDidShow</c>, with the keyboard's height. Raised on the top level; it doesn't bubble.</summary>
    public static readonly RoutedEvent<KeyboardDidShowEventArgs> IonKeyboardDidShowEvent =
        RoutedEvent.Register<KeyboardDidShowEventArgs>("IonKeyboardDidShow", RoutingStrategies.Direct, typeof(KeyboardAssist));

    /// <summary>Ionic <c>ionKeyboardDidHide</c>. Raised on the top level; it doesn't bubble.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonKeyboardDidHideEvent =
        RoutedEvent.Register<RoutedEventArgs>("IonKeyboardDidHide", RoutingStrategies.Direct, typeof(KeyboardAssist));

    private static readonly ConditionalWeakTable<TopLevel, Watch> s_started = new();

    /// <summary>
    /// Whether the keyboard is open for <paramref name="topLevel"/>, as the last event raised says: Ionic's module-level
    /// <c>keyboardOpen</c>. False while nothing is started.
    /// </summary>
    internal static bool IsOpen(TopLevel topLevel) => s_started.TryGetValue(topLevel, out var watch) && watch.Open;

    /// <summary>Starts it for one top level, until the result is disposed. Starts count, as the back button's do.</summary>
    internal static IDisposable Start(TopLevel topLevel)
    {
        var watch = s_started.GetValue(topLevel, top => new Watch(top));
        watch.Count++;
        if (watch.Count == 1)
        {
            watch.Attach();
        }

        return new Stop(watch);
    }

    private sealed class Stop(Watch watch) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (--watch.Count == 0)
                {
                    watch.Detach();
                }
            }
        }
    }

    private sealed class Watch(TopLevel topLevel)
    {
        private IInputPane? _pane;
        private IDisposable? _pending;

        public int Count { get; set; }

        public bool Open { get; private set; }

        public void Attach()
        {
            _pane = KeyboardController.InputPaneOf(topLevel);
            if (_pane is not null)
            {
                _pane.StateChanged += OnStateChanged;
            }
        }

        public void Detach()
        {
            if (_pane is not null)
            {
                _pane.StateChanged -= OnStateChanged;
                _pane = null;
            }

            _pending?.Dispose();
            _pending = null;
        }

        private void OnStateChanged(object? sender, InputPaneStateEventArgs e)
        {
            _pending?.Dispose();
            var open = e.NewState == InputPaneState.Open;
            var height = e.EndRect.Height;
            _pending = AvonicConfig.Schedule(() =>
            {
                _pending = null;
                Open = open;
                if (open)
                {
                    topLevel.RaiseEvent(new KeyboardDidShowEventArgs(IonKeyboardDidShowEvent, topLevel, height));
                }
                else
                {
                    topLevel.RaiseEvent(new RoutedEventArgs(IonKeyboardDidHideEvent, topLevel));
                }
            }, e.AnimationDuration);
        }
    }
}

/// <summary>The detail of <see cref="KeyboardAssist.IonKeyboardDidShowEvent"/>: Ionic's <c>{ keyboardHeight }</c>.</summary>
public sealed class KeyboardDidShowEventArgs : RoutedEventArgs
{
    internal KeyboardDidShowEventArgs(RoutedEvent routedEvent, object source, double keyboardHeight)
        : base(routedEvent, source) => KeyboardHeight = keyboardHeight;

    /// <summary>
    /// Ionic <c>keyboardHeight</c>: how much of the top level the keyboard covers, in device-independent pixels, above the
    /// navigation bar, as Avalonia's <see cref="IInputPane.OccludedRect"/> gives it.
    /// </summary>
    public double KeyboardHeight { get; }
}
