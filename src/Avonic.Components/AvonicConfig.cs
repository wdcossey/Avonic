using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components;

/// <summary>
/// Process-wide switches, the equivalent of Ionic's global config
/// (<c>IonicConfig</c>). Read at the moment a behaviour runs, so they may be changed at runtime.
/// </summary>
public static class AvonicConfig
{
    /// <summary>
    /// Ionic <c>animated</c>: whether the ripple shows. Default <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Not a master switch. In Ionic it turns off the ripple (<c>tap-click</c>) and the transitions of the accordion, the menu,
    /// <c>ion-nav</c>, <c>ion-router-outlet</c>, overlays and the searchbar, and the skeleton text's shimmer. A spinner, a progress
    /// bar and a scroll of <c>ion-content</c> don't read it, so <see cref="AvonicSpinner"/>, <see cref="AvonicProgressBar"/> and
    /// <see cref="AvonicContent"/> keep moving with it off: they're the user's only sign that something is happening.
    /// </remarks>
    public static bool Animated { get; set; } = true;

    /// <summary>
    /// Ionic <c>rippleEffect</c>: whether activatable surfaces show a ripple on press.
    /// Ripples are shown only when both this and <see cref="Animated"/> are <see langword="true"/>.
    /// </summary>
    public static bool RippleEffect { get; set; } = true;

    /// <summary>
    /// Ionic's <c>spinner</c> config: the name of an <see cref="AvonicSpinner"/> that has none. Null is md's default,
    /// <see cref="SpinnerName.Circular"/>.
    /// </summary>
    /// <remarks>
    /// Read when the spinner renders, as Ionic's <c>getName()</c> reads it in <c>render()</c>: when it's first attached, and at each
    /// change of its <see cref="AvonicSpinner.SpinnerName"/>, <see cref="AvonicSpinner.Paused"/>,
    /// <see cref="AvonicSpinner.Duration"/> or <see cref="AvonicSpinner.Color"/>. A change of this alone redraws nothing, and
    /// neither does a spinner leaving the tree and coming back, or being hidden and shown, as measured in Chromium.
    /// </remarks>
    public static SpinnerName? Spinner { get; set; }

    private static readonly Stopwatch s_clock = Stopwatch.StartNew();

    /// <summary>Monotonic time source for every Avonic timeline. Tests substitute a manual clock.</summary>
    internal static Func<TimeSpan> Now { get; set; } = () => s_clock.Elapsed;

    /// <summary>
    /// Ionic's <c>now(ev)</c>, <c>ev.timeStamp || Date.now()</c>, for <see cref="Gestures.Gesture"/>: the event's own time, in
    /// milliseconds, or <see cref="Now"/>'s if it has none. Only differences are used, except by a control that compares
    /// <see cref="Gestures.GestureDetail.CurrentTime"/> with a later event's <see cref="PointerEventArgs.Timestamp"/>, as
    /// <c>ion-menu</c> does. Tests substitute <see cref="Now"/>, as headless input's timestamps can't be set.
    /// </summary>
    internal static Func<PointerEventArgs, double> EventTime { get; set; } =
        static ev => ev.Timestamp > 0 ? ev.Timestamp : Now().TotalMilliseconds;

    /// <summary>
    /// One-shot scheduler for every Avonic defer (Ionic's <c>setTimeout</c>). Runs on the UI thread.
    /// Tests substitute a scheduler driven by the same manual clock as <see cref="Now"/>.
    /// </summary>
    internal static Func<Action, TimeSpan, IDisposable> Schedule { get; set; } =
        static (callback, delay) => DispatcherTimer.RunOnce(callback, delay);

    /// <summary>
    /// Ionic's <c>requestAnimationFrame</c> and <c>readTask</c>: before the next frame the visual's window draws. Outside a window
    /// there's no frame to wait for, so it's a timer. Tests substitute one driven by the same manual clock as <see cref="Now"/>.
    /// </summary>
    internal static Func<Visual, Action, IDisposable> RequestFrame { get; set; } = static (visual, callback) =>
    {
        if (TopLevel.GetTopLevel(visual) is not { } top)
        {
            return Schedule(callback, FrameInterval);
        }

        var frame = new CancellableFrame();
        top.RequestAnimationFrame(_ =>
        {
            if (!frame.IsCancelled)
            {
                callback();
            }
        });
        return frame;
    };

    /// <summary>A frame, for what doesn't have one: a test's clock, and no window.</summary>
    internal static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);

    private sealed class CancellableFrame : IDisposable
    {
        public bool IsCancelled { get; private set; }

        public void Dispose() => IsCancelled = true;
    }
}
