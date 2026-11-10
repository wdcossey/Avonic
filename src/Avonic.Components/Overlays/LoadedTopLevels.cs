using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Avonic.Components.Overlays;

/// <summary>
/// The top levels that are loaded, in the order they loaded. It's how <see cref="AvonicOverlay.Present()"/> finds
/// the current activity's top level under Android's <c>IActivityApplicationLifetime.MainViewFactory</c>, which gives each activity a
/// view of its own and leaves <c>MainView</c> null: Avalonia 12 keeps the current activity internal to <c>Avalonia.Android</c>, and
/// has no public list of top levels and no focused element without one. See <c>docs/infra/overlays/README.md</c>.
/// </summary>
internal static class LoadedTopLevels
{
    private static readonly List<WeakReference<TopLevel>> s_loaded = [];

    /// <summary>
    /// Listens from the moment this assembly loads. An app that uses Avonic loads it with <c>AvonicTheme</c>, from its XAML in
    /// <c>Application.Initialize</c>, which on Android runs in the application's <c>OnCreate</c>, before the first activity and its
    /// top level. A static constructor would run only at the first overlay, too late to hear the top level it's presented in.
    /// </summary>
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "It only adds two class handlers, and has to run before the app's first top level loads.")]
    internal static void Start()
    {
        Control.LoadedEvent.AddClassHandler<TopLevel>((topLevel, _) => OnLoaded(topLevel));
        Control.UnloadedEvent.AddClassHandler<TopLevel>((topLevel, _) => OnUnloaded(topLevel));
    }

    /// <summary>The last top level to load that's still loaded: the activity on top, as the one Android created last.</summary>
    internal static TopLevel? Last
    {
        get
        {
            lock (s_loaded)
            {
                for (var i = s_loaded.Count - 1; i >= 0; i--)
                {
                    if (s_loaded[i].TryGetTarget(out var topLevel))
                    {
                        return topLevel;
                    }
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Every top level, popups' too: Android, the one platform with activities, shows popups in the overlay layer, where they have
    /// none of their own.
    /// </summary>
    private static void OnLoaded(TopLevel topLevel)
    {
        lock (s_loaded)
        {
            s_loaded.Add(new WeakReference<TopLevel>(topLevel));
        }
    }

    /// <summary>Weak, in case a top level is let go of without unloading; those are cleared at the next unload.</summary>
    private static void OnUnloaded(TopLevel topLevel)
    {
        lock (s_loaded)
        {
            s_loaded.RemoveAll(r => !r.TryGetTarget(out var loaded) || ReferenceEquals(loaded, topLevel));
        }
    }
}
