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
    /// Ionic's <c>spinner</c> config: the name of an <see cref="AvonicSpinner"/> that has none. Null, not set, and
    /// <see cref="ConfigSpinner.None"/> are md's default there, <see cref="SpinnerName.Circular"/>, as Ionic's <c>getName()</c>
    /// reads <c>this.name || config.get('spinner')</c>. The infinite scroll's and the refresher's contents fall back to it when
    /// their own keys aren't set, and there <see cref="ConfigSpinner.None"/>, Ionic's <c>null</c>, is no spinner.
    /// </summary>
    /// <remarks>
    /// Read when the spinner renders, as Ionic's <c>getName()</c> reads it in <c>render()</c>: when it's first attached, and at each
    /// change of its <see cref="AvonicSpinner.SpinnerName"/>, <see cref="AvonicSpinner.Paused"/>,
    /// <see cref="AvonicSpinner.Duration"/> or <see cref="AvonicSpinner.Color"/>. A change of this alone redraws nothing, and
    /// neither does a spinner leaving the tree and coming back, or being hidden and shown, as measured in Chromium. Ionic's own type
    /// for it has no <c>null</c>, though its code passes one on: <see cref="ConfigSpinner.None"/> is that.
    /// </remarks>
    public static ConfigSpinner? Spinner { get; set; }

    /// <summary>
    /// Ionic's <c>backButtonIcon</c> config: the icon of an <see cref="AvonicSearchbar"/>'s md cancel button, an
    /// <see cref="AvonicIcon.Icon"/>, when its <see cref="AvonicSearchbar.CancelButtonIcon"/> isn't set. Null is md's
    /// <c>arrow-back-sharp</c>, which Ionic gives as SVG, so it isn't mirrored in RTL; a name given here is, as an arrow's is.
    /// </summary>
    /// <remarks>
    /// Read when the searchbar is made, as Ionic reads it for <c>cancelButtonIcon</c>'s default in the constructor: a change of
    /// this reaches only searchbars made after it. The value is the searchbar's at style priority, so an app's own wins.
    /// </remarks>
    public static string? BackButtonIcon { get; set; }

    /// <summary>
    /// Ionic's <c>toggleOnOffLabels</c> config: the default of <see cref="AvonicToggle.EnableOnOffLabels"/>. Ionic leaves it
    /// undefined, which is false.
    /// </summary>
    /// <remarks>
    /// Read when the toggle is made, as Ionic reads it for <c>enableOnOffLabels</c>'s default in the constructor: a change of this
    /// reaches only toggles made after it. The value is the toggle's at style priority, so an app's own wins.
    /// </remarks>
    public static bool ToggleOnOffLabels { get; set; }

    /// <summary>
    /// Ionic's <c>infiniteLoadingSpinner</c> config: the <see cref="AvonicInfiniteScrollContent.LoadingSpinner"/> of a content that
    /// has none, before <see cref="Spinner"/>. Null, not set, falls back to <see cref="Spinner"/>, then md's
    /// <see cref="SpinnerName.Crescent"/>; <see cref="ConfigSpinner.None"/>, Ionic's <c>null</c>, is no spinner.
    /// </summary>
    /// <remarks>
    /// Read when the content is first attached, as Ionic reads it in <c>componentDidLoad</c>: a change of this reaches only contents
    /// attached for the first time after it.
    /// </remarks>
    public static ConfigSpinner? InfiniteLoadingSpinner { get; set; }

    /// <summary>
    /// Ionic's <c>refreshingIcon</c> config: the <see cref="AvonicRefresherContent.PullingIcon"/> of a content that has none, which
    /// Ionic names after the icon its legacy refresher pulls with. Null, not set, is md's <see cref="SpinnerName.Circular"/>, which
    /// doesn't fall back to <see cref="Spinner"/> in md; <see cref="ConfigSpinner.None"/>, Ionic's <c>null</c>, is none, and with
    /// no pulling spinner the refresher isn't md's native one, so it does nothing here.
    /// </summary>
    /// <remarks>
    /// Read when the content first loads, as Ionic reads it in <c>componentWillLoad</c>: when it's first attached, or when its
    /// refresher first looks at it. A change of this reaches only contents loaded for the first time after it. Ionic's is any icon's
    /// name; only spinners are here, as an icon is the legacy refresher's.
    /// </remarks>
    public static ConfigSpinner? RefreshingIcon { get; set; }

    /// <summary>
    /// Ionic's <c>refreshingSpinner</c> config: the <see cref="AvonicRefresherContent.RefreshingSpinner"/> of a content that has
    /// none, before <see cref="Spinner"/>. Null, not set, falls back to <see cref="Spinner"/>, then md's
    /// <see cref="SpinnerName.Circular"/>; <see cref="ConfigSpinner.None"/>, Ionic's <c>null</c>, is none, and the refresher then
    /// isn't md's native one, so it does nothing here.
    /// </summary>
    /// <remarks>Read when the content first loads, as <see cref="RefreshingIcon"/> is.</remarks>
    public static ConfigSpinner? RefreshingSpinner { get; set; }

    /// <summary>
    /// Ionic's <c>tabButtonLayout</c> config: the <see cref="AvonicTabButton.Layout"/> of a tab button that has none. Null is
    /// Ionic's fallback, <see cref="Controls.TabButtonLayout.IconTop"/>.
    /// </summary>
    /// <remarks>
    /// Read when the tab button is first attached, as Ionic reads it in <c>componentWillLoad</c>: a change of this reaches only tab
    /// buttons attached for the first time after it.
    /// </remarks>
    public static TabButtonLayout? TabButtonLayout { get; set; }

    // ── Avonic's own: no Ionic config has these ─────────────────────────────────────────────────

    /// <summary>
    /// Not Ionic's: the default of <see cref="AvonicContent.ScrollBars"/>, whether an <see cref="AvonicContent"/> has a scroll bar to
    /// drag, a desktop browser's, in place of md's phone thumb. False by default, which is Ionic's md, as a phone draws it.
    /// </summary>
    /// <remarks>
    /// Read when the content is made, as a prop's default is: a change of this reaches only contents made after it, so set it at
    /// start-up. The value is the content's at style priority, so an app's own, set or in a style, wins.
    /// </remarks>
    public static bool ContentScrollBars { get; set; }

    // ── ion-app (#65): read by AvonicApp when it's attached ─────────────────────────────────────

    /// <summary>
    /// Ionic's <c>hardwareBackButton</c> config: whether Avonic handles a back request (<see cref="TopLevel.BackRequested"/>):
    /// <see cref="AvonicApp"/> raises <see cref="Controls.HardwareBackButton.IonBackButtonEvent"/> for it, and without one an overlay still
    /// dismisses on it. Default <see langword="true"/>, as in a Capacitor or Cordova app, which an Avalonia app is like: Ionic's is
    /// false only in a browser.
    /// </summary>
    /// <remarks>
    /// Read by an <see cref="AvonicApp"/> when it's attached, as Ionic reads it when <c>ion-app</c> loads, and by an overlay at each
    /// back request when there's no app. False leaves the request to the app and the platform, where Ionic's
    /// <c>blockHardwareBackButton</c> takes it and does nothing: here that would also hide it from the app's own handlers.
    /// </remarks>
    public static bool HardwareBackButton { get; set; } = true;

    /// <summary>
    /// An Avonic extension: whether a back request that nothing registers a handler for, while an <see cref="AvonicApp"/> has
    /// started the back button, is still handled, so that Android stays in the app. Default <see langword="true"/>, as in an
    /// Ionic app on Capacitor, where the App plugin takes every back press and "nothing will happen" when nothing can go back:
    /// such an app exits through a handler of its own at -1. False leaves that request to the platform, so Android leaves the
    /// app. Read at each back request.
    /// </summary>
    public static bool HandleUnclaimedBack { get; set; } = true;

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
