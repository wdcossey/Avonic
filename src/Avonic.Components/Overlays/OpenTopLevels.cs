using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;

namespace Avonic.Components.Overlays;

/// <summary>
/// The top levels that are open, in the order they came: from when their template is applied, which gives them an overlay layer,
/// to when they close. It's how <see cref="AvonicOverlay.Present()"/> finds the current activity's top level under Android's
/// <c>IActivityApplicationLifetime.MainViewFactory</c>, which gives each activity a view of its own and leaves <c>MainView</c> null:
/// Avalonia 12 keeps the current activity internal to <c>Avalonia.Android</c>, and has no public list of top levels and no focused
/// element without one. See <c>docs/infra/overlays/PLAN.md</c>.
/// </summary>
internal static class OpenTopLevels
{
    private static readonly List<WeakReference<TopLevel>> s_open = [];
    private static bool s_listening;

    /// <summary>
    /// Listens, unless <paramref name="lifetime"/> is one that never reads the list: a desktop's or a single view's without
    /// activities, which then don't pay for a handler on every control's <c>TemplateApplied</c>. With no lifetime yet, as when an
    /// app touches Avonic before it builds Avalonia, it can't tell, so it listens. True if it listens. Called by each
    /// <see cref="AvonicComponentsStyles"/>, which an app's <c>Application.Initialize</c> makes, with <c>AvonicTheme</c>, and which on
    /// Android runs in the application's <c>OnCreate</c>, before the first activity and its top level; it listens once.
    /// </summary>
    internal static bool Start(IApplicationLifetime? lifetime)
    {
        if (lifetime is not (null or IActivityApplicationLifetime))
        {
            return false;
        }

        if (!s_listening)
        {
            s_listening = true;

            // Synchronous: in an EmbeddableControlRoot's Prepare(), before the view in it is attached; in a window's Show().
            TemplatedControl.TemplateAppliedEvent.AddClassHandler<TopLevel>((topLevel, _) => Add(topLevel));
        }

        return true;
    }

    /// <summary>The last top level to come that's still open: the activity on top, as the one Android created last.</summary>
    internal static TopLevel? Last
    {
        get
        {
            lock (s_open)
            {
                for (var i = s_open.Count - 1; i >= 0; i--)
                {
                    if (s_open[i].TryGetTarget(out var topLevel))
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
    /// none of their own. One that's already there keeps its place, as a template applied again, for a new theme, isn't a new one.
    /// </summary>
    private static void Add(TopLevel topLevel)
    {
        lock (s_open)
        {
            if (s_open.Exists(r => r.TryGetTarget(out var known) && ReferenceEquals(known, topLevel)))
            {
                return;
            }

            s_open.Add(new WeakReference<TopLevel>(topLevel));
        }

        topLevel.Closed += OnClosed;
    }

    /// <summary>A closed window, and a disposed root, as Android's activity disposes its view's in <c>OnDestroy</c>, loaded or not.</summary>
    private static void OnClosed(object? sender, EventArgs e)
    {
        var topLevel = (TopLevel)sender!;
        topLevel.Closed -= OnClosed;
        lock (s_open)
        {
            s_open.RemoveAll(r => !r.TryGetTarget(out var open) || ReferenceEquals(open, topLevel));
        }
    }
}
