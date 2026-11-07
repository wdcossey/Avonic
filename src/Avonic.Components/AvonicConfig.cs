using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Avonic.Components;

/// <summary>
/// Process-wide switches, the equivalent of Ionic's global config
/// (<c>IonicConfig</c>). Read at the moment a behaviour runs, so they may be changed at runtime.
/// </summary>
public static class AvonicConfig
{
    /// <summary>Ionic <c>animated</c>: master switch for animations. Default <see langword="true"/>.</summary>
    public static bool Animated { get; set; } = true;

    /// <summary>
    /// Ionic <c>rippleEffect</c>: whether activatable surfaces show a ripple on press.
    /// Ripples are shown only when both this and <see cref="Animated"/> are <see langword="true"/>.
    /// </summary>
    public static bool RippleEffect { get; set; } = true;

    private static readonly Stopwatch s_clock = Stopwatch.StartNew();

    /// <summary>Monotonic time source for every Avonic timeline. Tests substitute a manual clock.</summary>
    internal static Func<TimeSpan> Now { get; set; } = () => s_clock.Elapsed;

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
