using Avalonia;
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
/// replaces it. On Windows 10 and later every window has a pane, which reports the touch keyboard docking and undocking (Avalonia's
/// <c>WindowsInputPane</c>, through <c>IFrameworkInputPane</c>), with no animation; macOS and Linux have none, and nothing is
/// raised, as a desktop browser's viewport doesn't change for a keyboard.
/// </remarks>
public static class KeyboardAssist
{
    /// <summary>Ionic <c>ionKeyboardDidShow</c>, with the keyboard's height. Raised on the top level; it doesn't bubble.</summary>
    public static readonly RoutedEvent<KeyboardDidShowEventArgs> IonKeyboardDidShowEvent =
        RoutedEvent.Register<KeyboardDidShowEventArgs>("IonKeyboardDidShow", RoutingStrategies.Direct, typeof(KeyboardAssist));

    /// <summary>Ionic <c>ionKeyboardDidHide</c>. Raised on the top level; it doesn't bubble.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonKeyboardDidHideEvent =
        RoutedEvent.Register<RoutedEventArgs>("IonKeyboardDidHide", RoutingStrategies.Direct, typeof(KeyboardAssist));

    private static readonly TopLevelStarts<Watch> s_started = new(top => new Watch(top), watch => watch.Attach(), watch => watch.Detach());

    /// <summary>Starts it for one top level, until the result is disposed. Starts count, as the back button's do.</summary>
    internal static IDisposable Start(TopLevel topLevel) => s_started.Start(topLevel);

    /// <summary>
    /// How much of a top level of <paramref name="clientSize"/> the pane at <paramref name="occluded"/> covers. Where the keyboard
    /// is the only one (Android, iOS), the pane's own height, as before: Android's rect is always inside the view, and iOS's is
    /// UIKit's keyboard frame in the screen's coordinates, not the view's, which an iPad's window in Split View or Stage Manager
    /// doesn't share. Elsewhere (Windows), the height of their overlap: Windows' rect is the touch keyboard's place on the screen, in
    /// the window's coordinates, and a window that doesn't reach the bottom of the screen is covered by part of it, or none, when
    /// the keyboard is reported as hidden. The rect is the one Windows gave when the keyboard showed: Avalonia doesn't report it
    /// again when the window moves.
    /// </summary>
    internal static double CoveredHeight(Rect occluded, Size clientSize) =>
        AvonicConfig.KeyboardIsTheOnlyOne(InputShims.IsPlatform) ? occluded.Height : occluded.Intersect(new Rect(clientSize)).Height;

    private sealed class Watch(TopLevel topLevel)
    {
        private IInputPane? _pane;
        private IDisposable? _pending;

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
            _pending?.Dispose(); // A newer change replaces one not raised yet.
            // A pane that's somewhere, but over none of the top level, is no keyboard to it: Windows' touch keyboard under a window
            // that ends higher on the screen. An empty rect is left as it was, as Android's can be at the start of an opening.
            var height = CoveredHeight(e.EndRect, topLevel.ClientSize);
            var open = e.NewState == InputPaneState.Open && !(e.EndRect.Height > 0 && height <= 0);
            _pending = AvonicConfig.Schedule(() =>
            {
                _pending = null;
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
