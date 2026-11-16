using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>startStatusTap</c> (<c>utils/status-tap</c>): a tap on the status bar scrolls the content under the middle of the
/// window to its top, in 300 ms. It's started by <see cref="AvonicApp"/>, as Ionic's is by <c>ion-app</c>.
/// </summary>
/// <remarks>
/// Ionic listens for <c>statusTap</c> on the window, which Capacitor's status bar raises on iOS when the bar is tapped. Avalonia
/// doesn't report a tap on the status bar, and Android has no such thing, so the app raises <see cref="StatusTapEvent"/> on its
/// top level, from its iOS head, as Capacitor's native side raises <c>statusTap</c>.
/// </remarks>
public static class StatusTap
{
    /// <summary>Ionic's <c>statusTap</c>, for the app to raise on the <see cref="TopLevel"/>. It doesn't bubble.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> StatusTapEvent =
        RoutedEvent.Register<RoutedEventArgs>("StatusTap", RoutingStrategies.Direct, typeof(StatusTap));

    /// <summary>Ionic's <c>scrollToTop(contentEl, 300)</c>.</summary>
    internal const double ScrollDuration = 300;

    private static readonly ConditionalWeakTable<TopLevel, Counter> s_started = new();

    internal static IDisposable Start(TopLevel topLevel)
    {
        var counter = s_started.GetValue(topLevel, _ => new Counter());
        if (counter.Count++ == 0)
        {
            topLevel.AddHandler(StatusTapEvent, OnStatusTap);
        }

        return new Stop(topLevel, counter);
    }

    private sealed class Counter
    {
        public int Count { get; set; }
    }

    private sealed class Stop(TopLevel topLevel, Counter counter) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (--counter.Count == 0)
                {
                    topLevel.RemoveHandler(StatusTapEvent, OnStatusTap);
                }
            }
        }
    }

    /// <summary>
    /// In a <c>readTask</c>, the element at the middle of the window, <c>document.elementFromPoint(innerWidth / 2,
    /// innerHeight / 2)</c>, and its closest content; then, in the same frame's write, the content's scrolling held off
    /// (<c>--overflow: hidden</c>), its scroll to the top, and the scrolling back. Nothing if there's no content there.
    /// </summary>
    private static void OnStatusTap(object? sender, RoutedEventArgs e)
    {
        if (sender is not TopLevel topLevel)
        {
            return;
        }

        AvonicConfig.RequestFrame(topLevel, () =>
        {
            var size = topLevel.ClientSize;
            if (topLevel.InputHitTest(new Point(size.Width / 2, size.Height / 2)) is not Visual el
                || ContentUtils.FindClosestIonContent(el) is not { } host)
            {
                return;
            }

            _ = ScrollToTop(host, el);
        });
    }

    private static async Task ScrollToTop(Control host, Visual from)
    {
        if (host is AvonicContent content)
        {
            // The inline style beats the content's own: Animation priority beats a local value, and comes off when disposed.
            using (content.SetValue(AvonicContent.ScrollYProperty, false, BindingPriority.Animation))
            {
                await content.ScrollToTop(ScrollDuration);
            }
        }
        else if (ContentUtils.GetScrollElement(host, from) is { } scroller)
        {
            // el.scrollTo({ top: 0, behavior: 'smooth' }), as a browser scrolls smoothly.
            new SmoothScroll(scroller, Orientation.Vertical).To(0, smooth: true);
        }
    }
}
