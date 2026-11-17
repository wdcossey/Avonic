using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>startInputShims</c> (<c>utils/input-shims</c>), its scroll assist and scroll padding: when an
/// <see cref="AvonicInput"/> or <see cref="AvonicTextarea"/> in an <see cref="AvonicContent"/> takes focus where the on-screen
/// keyboard would cover it, the content is padded at its end by the keyboard's height and scrolled to bring its item above the
/// keyboard. Started by <see cref="AvonicApp"/> when <see cref="AvonicConfig.InputShims"/> is on.
/// </summary>
/// <remarks>
/// Ionic needs it where the keyboard covers the page instead of resizing it: on iOS, and in Chrome on Android. Avalonia is like
/// that on Android, where it draws edge to edge and leaves the keyboard over the view (measured on a Pixel 8; see
/// <c>docs/components/ion-app/COMPARISON.md</c>). What Ionic does to stop a browser scrolling the page itself to the field, moving
/// the native input away and focusing a clone, has nothing to stop here, as Avalonia doesn't, so it isn't done. Nor are hide caret
/// on scroll (WebKit's caret drawn over a scrolling page) or input blurring (off since Ionic 8, and on its way out).
/// <para>
/// On Windows, where the touch keyboard covers the window in the same way but most PCs have a keyboard of their own, they follow
/// the window's input pane instead, a deviation: a field's focus does nothing until the pane opens over the window, and then
/// it runs with the height the pane covers; a field focused while it's open runs at once with that height. Where there's no
/// pane, or the keyboard is the only one (Android, iOS), it's Ionic's: focus reckons with the config's height at once.
/// </para>
/// </remarks>
internal static class InputShims
{
    /// <summary>Ionic <c>SCROLL_AMOUNT_PADDING</c>: room for a bar the keyboard may show above itself later.</summary>
    internal const double ScrollAmountPadding = 50;

    /// <summary>Ionic <c>SCROLL_ASSIST_SPEED</c>, pixels a millisecond.</summary>
    internal const double ScrollAssistSpeed = 0.3;

    /// <summary>Ionic's 120 ms before scroll padding goes, so that focus moving to another field keeps it.</summary>
    internal static readonly TimeSpan PaddingClearDelay = TimeSpan.FromMilliseconds(120);

    /// <summary>Ionic's 1000 ms wait for the keyboard before scrolling anyway.</summary>
    internal static readonly TimeSpan KeyboardWait = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether a field's focus means the on-screen keyboard is coming, as Ionic reckons on a phone: Android and iOS, where it's the
    /// only keyboard. Tests replace it.
    /// </summary>
    internal static Func<bool> KeyboardComesWithFocus { get; set; } = () => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    private static readonly TopLevelStarts<Assist> s_started = new(top => new Assist(top), a => a.Attach(), a => a.Detach());

    internal static IDisposable Start(TopLevel topLevel) => s_started.Start(topLevel);

    /// <summary>
    /// Ionic's <c>calcScrollData</c>: how far to scroll so that the item is between 15 below the content's top and 50 above the
    /// keyboard, no further than its top, and how long, from the item's and the content's boxes in the top level.
    /// </summary>
    internal static (double ScrollAmount, double ScrollDuration, double ScrollPadding) ScrollData(
        Rect input, Rect content, double keyboardHeight, double platformHeight)
    {
        var visibleAreaTop = content.Top;
        var visibleAreaBottom = Math.Min(content.Bottom, platformHeight - keyboardHeight);
        var safeAreaTop = visibleAreaTop + 15;
        var safeAreaBottom = visibleAreaBottom - ScrollAmountPadding;
        var distanceToBottom = safeAreaBottom - input.Bottom;
        var distanceToTop = safeAreaTop - input.Top;
        // Math.round: halves go up, toward positive infinity.
        var desired = Math.Floor((distanceToBottom < 0 ? -distanceToBottom : distanceToTop > 0 ? -distanceToTop : 0) + 0.5);
        var scrollAmount = Math.Min(desired, input.Top - visibleAreaTop);
        var duration = Math.Min(400, Math.Max(150, Math.Abs(scrollAmount) / ScrollAssistSpeed));
        return (scrollAmount, duration, keyboardHeight);
    }

    /// <summary>One top level's assist: Ionic's per-input <c>enableScrollAssist</c>, listening once for all of them.</summary>
    private sealed class Assist(TopLevel topLevel)
    {
        // Ionic's config, read when the shims start, as startInputShims reads it when ion-app loads.
        private bool _scrollAssist;
        private bool _scrollPadding;
        private double _configKeyboardHeight;

        /// <summary>
        /// Whether it follows the input pane, rather than reckon a keyboard comes with focus: where the top level has a pane and the
        /// keyboard isn't the only one (Windows). Decided when it starts.
        /// </summary>
        private bool _followsPane;

        /// <summary>Ionic's module-level <c>currentPadding</c>.</summary>
        private double _currentPadding;

        private Control? _component;

        /// <summary>Ionic's <c>hasKeyboardBeenPresentedForTextField</c>.</summary>
        private bool _keyboardPresented;

        /// <summary>
        /// The keyboard's own height, from its last show, while it's open; null when it's closed. Kept from field to field, as the
        /// keyboard stays open when focus moves between them.
        /// </summary>
        private double? _keyboardHeight;

        /// <summary>The keyboard height the last run reckoned with, so that it runs again only for a new one.</summary>
        private double _usedHeight;

        /// <summary>
        /// <c>win.innerHeight</c> as the run reads it: taken while the keyboard is closed, and kept while it's open, as Ionic takes it
        /// once, so that a view the keyboard has already shrunk doesn't have the keyboard taken off twice.
        /// </summary>
        private double _platformHeight;

        /// <summary>Whether a run's scroll is waiting or running.</summary>
        private bool _scrollPending;

        /// <summary>Takes away a run's wait for the keyboard: its timeout and its handlers.</summary>
        private Action? _cancelWait;

        private IDisposable? _paddingTimer;
        private IDisposable? _padding;
        private AvonicContent? _padded;

        public void Attach()
        {
            _scrollAssist = AvonicConfig.ScrollAssist;
            _scrollPadding = AvonicConfig.ScrollPadding;
            _configKeyboardHeight = AvonicConfig.KeyboardHeight;
            _followsPane = !KeyboardComesWithFocus() && KeyboardController.InputPaneOf(topLevel) is not null;
            topLevel.AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
            topLevel.AddHandler(InputElement.LostFocusEvent, OnLostFocus, RoutingStrategies.Bubble, handledEventsToo: true);
            topLevel.AddHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnKeyboardShow);
            topLevel.AddHandler(KeyboardAssist.IonKeyboardDidHideEvent, OnKeyboardHide);
        }

        public void Detach()
        {
            topLevel.RemoveHandler(InputElement.GotFocusEvent, OnGotFocus);
            topLevel.RemoveHandler(InputElement.LostFocusEvent, OnLostFocus);
            topLevel.RemoveHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnKeyboardShow);
            topLevel.RemoveHandler(KeyboardAssist.IonKeyboardDidHideEvent, OnKeyboardHide);
            _cancelWait?.Invoke();
            _cancelWait = null;
            _paddingTimer?.Dispose();
            _paddingTimer = null;
            _padding?.Dispose();
            _padding = null;
            _padded = null;
            _currentPadding = 0;
            _component = null;
            _keyboardHeight = null;
            _scrollPending = false;
        }

        /// <summary>The input or textarea around what has focus: Ionic registers <c>ion-input</c> and <c>ion-textarea</c>.</summary>
        private static Control? ComponentOf(object? source) =>
            source is Visual visual
                ? visual.GetSelfAndVisualAncestors().FirstOrDefault(v => v is AvonicInput or AvonicTextarea) as Control
                : null;

        /// <summary>Ionic's <c>focusIn</c> on the component.</summary>
        private void OnGotFocus(object? sender, FocusChangedEventArgs e)
        {
            if (!_scrollAssist || ComponentOf(e.Source) is not { } component || ReferenceEquals(component, _component))
            {
                return;
            }

            _cancelWait?.Invoke();
            _cancelWait = null;
            _component = component;
            _keyboardPresented = false;
            _scrollPending = false;

            // Also with the keyboard open before any field had focus, as Windows' touch keyboard can be, opened from the taskbar.
            if (_keyboardHeight is null || _platformHeight <= 0)
            {
                _platformHeight = PlatformHeight();
            }

            if (_keyboardHeight is null && _followsPane)
            {
                // No keyboard yet, and none may come: the pane opening runs it, with its own height (OnKeyboardShow).
                _usedHeight = double.NaN;
                return;
            }

            // With the keyboard already open, its own height: a deviation, as I8 is.
            SetFocus(component, _keyboardHeight ?? _configKeyboardHeight, waitForResize: true);
        }

        /// <summary>Ionic's <c>focusOut</c>, and the padding's <c>setClearScrollPaddingListener</c>.</summary>
        private void OnLostFocus(object? sender, RoutedEventArgs e)
        {
            if (_component is not { } component || ComponentOf(e.Source) != component)
            {
                return;
            }

            // Focus moving inside the component, as from the field to its clear button, isn't leaving it.
            if (topLevel.FocusManager?.GetFocusedElement() is Visual now && component.IsVisualAncestorOf(now))
            {
                return;
            }

            _component = null;
            _keyboardPresented = false;
            if (_scrollPadding && _padded is { } content)
            {
                SetScrollPadding(content, 0, () => _currentPadding = 0);
            }
        }

        private void OnKeyboardHide(object? sender, RoutedEventArgs e) => _keyboardHeight = null;

        /// <summary>
        /// Ionic's <c>keyboardShow</c>: the first one after focus is the keyboard opening for it, which focus has already dealt with;
        /// a later one is the keyboard changing size, so it runs again with the keyboard's real height, without waiting.
        /// </summary>
        /// <remarks>
        /// A deviation: Ionic reckons with its estimate, 290, until the keyboard changes, as a browser then scrolls the focused field
        /// into the visual viewport itself. Avalonia doesn't, so once the keyboard has said its real height, it runs again with it
        /// when its scroll ends, or at once if there's none, once for each height: on a Pixel 8 the keyboard is about 355, and 290
        /// left the field under it.
        /// </remarks>
        private void OnKeyboardShow(object? sender, KeyboardDidShowEventArgs e)
        {
            if (_followsPane && e.KeyboardHeight <= 0)
            {
                // A touch keyboard that doesn't reach the window: nothing to keep clear of, as when it's closed.
                _keyboardHeight = null;
                return;
            }

            _keyboardHeight = e.KeyboardHeight;
            if (_component is not { } component)
            {
                return;
            }

            if (!_keyboardPresented)
            {
                _keyboardPresented = true;
                if (!_scrollPending)
                {
                    RunAgainWithTheKeyboard(component);
                }

                return;
            }

            SetFocus(component, e.KeyboardHeight, waitForResize: false);
        }

        /// <summary>Runs again with the keyboard's own height if the last run didn't, so a field that can't reach its place stops.</summary>
        private void RunAgainWithTheKeyboard(Control component)
        {
            if (ReferenceEquals(component, _component) && _keyboardHeight is { } height && height != _usedHeight)
            {
                SetFocus(component, height, waitForResize: false);
            }
        }

        /// <summary>Ionic's <c>jsSetFocus</c>, with nothing to relocate or refocus.</summary>
        private void SetFocus(Control component, double keyboardHeight, bool waitForResize)
        {
            if (ContentUtils.FindClosestIonContent(component) is not AvonicContent content || content.GetScrollElement() is not { } scroll)
            {
                // A footer's input: Ionic only relocates its native input there, which has nothing to do here.
                return;
            }

            // getBoundingClientRect lays the page out, so the boxes are the latest offset's.
            topLevel.UpdateLayout();
            var item = (Visual?)component.GetSelfAndVisualAncestors().OfType<AvonicItem>().FirstOrDefault() ?? component;
            if (BoxOf(item) is not { } input || BoxOf(content) is not { } box)
            {
                return;
            }

            _usedHeight = keyboardHeight;
            var (scrollAmount, scrollDuration, scrollPadding) = ScrollData(input, box, keyboardHeight, _platformHeight);
            if (Math.Abs(scrollAmount) < 4)
            {
                // Safe where it is: the padding stays as it was, so that moving from a field that needed it doesn't jump.
                if (_scrollPadding)
                {
                    SetScrollPadding(content, _currentPadding);
                }

                return;
            }

            if (_scrollPadding)
            {
                _currentPadding = scrollPadding;
                SetScrollPadding(content, _currentPadding);
            }

            IDisposable? timeout = null;
            EventHandler<KeyboardDidShowEventArgs>? onFirstShow = null;
            var done = false;

            void StopWaiting()
            {
                done = true;
                timeout?.Dispose();
                topLevel.RemoveHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnShowForScroll);
                if (onFirstShow is not null)
                {
                    topLevel.RemoveHandler(KeyboardAssist.IonKeyboardDidShowEvent, onFirstShow);
                }
            }

            async void ScrollContent()
            {
                if (done)
                {
                    return;
                }

                StopWaiting();
                _cancelWait = null;
                await content.ScrollByPoint(0, scrollAmount, scrollDuration);
                _scrollPending = false;
                RunAgainWithTheKeyboard(component);
            }

            void OnShowForScroll(object? s, KeyboardDidShowEventArgs args) => ScrollContent();

            _scrollPending = true;

            // Reading scrollHeight lays the page out, with the padding just set.
            scroll.UpdateLayout();
            var total = scroll.Extent.Height - scroll.Viewport.Height;
            if (waitForResize && scrollAmount > total - scroll.Offset.Y)
            {
                // Not enough room yet: wait for the keyboard, or a second at most. A password field waits for a second show, as
                // iOS shows a passwords bar after the keyboard, and scrolls 50 more for it.
                if (component is AvonicInput { Type: InputType.Password })
                {
                    scrollAmount += ScrollAmountPadding;
                    onFirstShow = (_, _) =>
                    {
                        topLevel.RemoveHandler(KeyboardAssist.IonKeyboardDidShowEvent, onFirstShow!);
                        topLevel.AddHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnShowForScroll);
                    };
                    topLevel.AddHandler(KeyboardAssist.IonKeyboardDidShowEvent, onFirstShow);
                }
                else
                {
                    topLevel.AddHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnShowForScroll);
                }

                timeout = AvonicConfig.Schedule(ScrollContent, KeyboardWait);
                _cancelWait = StopWaiting;
                return;
            }

            ScrollContent();
        }

        /// <summary>
        /// Ionic's <c>setScrollPadding</c>: <c>--keyboard-offset</c> inline on the content at once, or back to 0 after 120 ms, which a
        /// new padding before then cancels. Inline beats the theme, so it's set at <see cref="BindingPriority.Animation"/>; at 0 it's
        /// taken off, so that the app's own value comes back, where Ionic leaves an inline <c>0px</c>.
        /// </summary>
        private void SetScrollPadding(AvonicContent content, double amount, Action? cleared = null)
        {
            _paddingTimer?.Dispose();
            _paddingTimer = null;
            if (amount > 0)
            {
                _padding?.Dispose();
                _padded = content;
                _padding = content.SetValue(AvonicContent.KeyboardOffsetProperty, amount, BindingPriority.Animation);
            }
            else
            {
                _paddingTimer = AvonicConfig.Schedule(() =>
                {
                    _paddingTimer = null;
                    _padding?.Dispose();
                    _padding = null;
                    _padded = null;
                    cleared?.Invoke();
                }, PaddingClearDelay);
            }
        }

        /// <summary>
        /// <c>win.innerHeight</c>: the height the page has, above the navigation bar. Edge to edge, the top level's client area reaches
        /// under the bar, and the keyboard's height is measured above it, as Avalonia's <c>OccludedRect</c> is, so the bottom inset
        /// comes off (measured on a Pixel 8: without it, the item ended 26 above the keyboard, where it's 50). Not edge to edge, the
        /// inset is 0 and the client area ends at the bar.
        /// </summary>
        private double PlatformHeight() =>
            topLevel.ClientSize.Height - (AppSafeArea.InsetsOf(topLevel)?.SafeAreaPadding.Bottom ?? 0);

        /// <summary><c>getBoundingClientRect()</c>: the visual's box in the top level.</summary>
        private Rect? BoxOf(Visual visual) =>
            visual.TranslatePoint(default, topLevel) is { } origin ? new Rect(origin, visual.Bounds.Size) : null;
    }
}
