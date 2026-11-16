using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Logging;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>utils/hardware-back-button</c>: on a back request, the <see cref="IonBackButtonEvent"/> goes out on the
/// <see cref="TopLevel"/>, everything that wants the back press registers a handler at a priority, and the highest runs. It's
/// started by <see cref="AvonicApp"/>, as Ionic's is by <c>ion-app</c>. See <c>docs/components/ion-app/README.md</c>.
/// </summary>
/// <remarks>
/// Ionic listens to the document's <c>backbutton</c>, which Capacitor and Cordova raise for Android's back. Here it's
/// <see cref="TopLevel.BackRequested"/>, which Avalonia raises for Android's back, and on a desktop for the mouse's back button
/// and its back key gesture (Alt+Left on Windows). A request an app's own handler has already handled doesn't come.
/// </remarks>
public static class HardwareBackButton
{
    /// <summary>Ionic <c>OVERLAY_BACK_BUTTON_PRIORITY</c>: an overlay with <c>backdropDismiss</c> on top dismisses.</summary>
    public const int OverlayBackButtonPriority = 100;

    /// <summary>
    /// Ionic <c>MENU_BACK_BUTTON_PRIORITY</c>: an open menu closes. One less than an overlay's, as a menu is behind overlays.
    /// </summary>
    public const int MenuBackButtonPriority = 99;

    /// <summary>
    /// Ionic <c>ionBackButton</c>: raised on the <see cref="TopLevel"/> for each back request, while the registry is started for it,
    /// for handlers to <see cref="BackButtonEventArgs.Register(int, Action{Action})"/>. It doesn't bubble, as Ionic's
    /// <c>bubbles: false</c> on the document: listen on the top level.
    /// </summary>
    public static readonly RoutedEvent<BackButtonEventArgs> IonBackButtonEvent =
        RoutedEvent.Register<BackButtonEventArgs>("IonBackButton", RoutingStrategies.Direct, typeof(HardwareBackButton));

    private static readonly ConditionalWeakTable<TopLevel, Registry> s_started = new();

    /// <summary>Whether the registry is started for <paramref name="topLevel"/>.</summary>
    public static bool IsStarted(TopLevel topLevel) => s_started.TryGetValue(topLevel, out var registry) && registry.Count > 0;

    /// <summary>
    /// Ionic <c>startHardwareBackButton</c>, for one top level, until the result is disposed. Starting it again counts, and it stops
    /// when every start has been disposed.
    /// </summary>
    internal static IDisposable Start(TopLevel topLevel)
    {
        var registry = s_started.GetValue(topLevel, top => new Registry(top));
        registry.Count++;
        if (registry.Count == 1)
        {
            topLevel.AddHandler(TopLevel.BackRequestedEvent, registry.OnBackRequested);
        }

        return new Stop(registry);
    }

    private sealed class Stop(Registry registry) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (--registry.Count == 0)
            {
                registry.TopLevel.RemoveHandler(TopLevel.BackRequestedEvent, registry.OnBackRequested);
            }
        }
    }

    /// <summary>Ionic's <c>backButtonCallback</c> and its <c>busy</c> flag, for one top level.</summary>
    private sealed class Registry(TopLevel topLevel)
    {
        private bool _busy;

        public TopLevel TopLevel { get; } = topLevel;

        public int Count { get; set; }

        /// <summary>
        /// Ionic's <c>backButtonCallback</c>. Handled when something registered, so Android stays in the app; left alone when
        /// nothing did, so Android leaves it, as an app with no handler would. While a handler's task is still running, a back
        /// request does nothing, and is handled, as Ionic's <c>busy</c> ignores it.
        /// </summary>
        public void OnBackRequested(object? sender, RoutedEventArgs e)
        {
            if (_busy)
            {
                e.Handled = true;
                return;
            }

            var ev = new BackButtonEventArgs(IonBackButtonEvent, TopLevel);
            TopLevel.RaiseEvent(ev);
            var handlers = ev.Handlers;
            if (handlers.Count == 0)
            {
                return;
            }

            e.Handled = true;

            void ProcessHandlers()
            {
                if (handlers.Count == 0)
                {
                    return;
                }

                // The highest priority, and of equal ones the last registered, as Ionic's >= keeps the later.
                var selected = handlers[0];
                foreach (var handler in handlers)
                {
                    if (handler.Priority >= selected.Priority)
                    {
                        selected = handler;
                    }
                }

                _busy = true;
                handlers.Remove(selected);
                _ = ExecuteAction(selected);
            }

            async Task ExecuteAction(BackButtonEventArgs.Entry entry)
            {
                try
                {
                    if (entry.Handler(ProcessHandlers) is { } task)
                    {
                        await task;
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Error, "Avonic")?.Log(TopLevel, "[AvonicApp] - Exception in a back button handler: {Exception}", ex);
                }

                _busy = false;
            }

            ProcessHandlers();
        }
    }
}

/// <summary>
/// The detail of <see cref="HardwareBackButton.IonBackButtonEvent"/>, Ionic's <c>BackButtonEventDetail</c>: what to call to take
/// the back press.
/// </summary>
public sealed class BackButtonEventArgs : RoutedEventArgs
{
    internal BackButtonEventArgs(RoutedEvent routedEvent, object source)
        : base(routedEvent, source)
    {
    }

    internal readonly record struct Entry(int Priority, Func<Action, Task?> Handler);

    internal List<Entry> Handlers { get; } = [];

    /// <summary>
    /// Ionic <c>register(priority, handler)</c>. Of everything registered for one back press, the handler with the highest
    /// priority runs, and of equal ones the last registered. It's given <c>processNextHandler</c>: calling it runs the next
    /// highest, and each call runs one more. Until the task it returns finishes, a back press does nothing.
    /// </summary>
    /// <param name="priority">Ionic's own: 100 for an overlay, 99 for a menu, 0 for its router's back.</param>
    /// <param name="handler">Given <c>processNextHandler</c>; returns what to wait for, or null.</param>
    public void Register(int priority, Func<Action, Task?> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Handlers.Add(new Entry(priority, handler));
    }

    /// <inheritdoc cref="Register(int, Func{Action, Task?})"/>
    public void Register(int priority, Action<Action> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Handlers.Add(new Entry(priority, next =>
        {
            handler(next);
            return null;
        }));
    }
}
