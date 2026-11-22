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
    /// Ionic's <c>backButtonIcon</c> config, an <see cref="AvonicIcon.Icon"/>: the icon of an <see cref="AvonicBackButton"/> whose
    /// <see cref="AvonicBackButton.Icon"/> is null, mirrored in RTL as the default is; and of an <see cref="AvonicSearchbar"/>'s md
    /// cancel button when its <see cref="AvonicSearchbar.CancelButtonIcon"/> isn't set. Null is md's <c>arrow-back-sharp</c>: the
    /// back button mirrors it in RTL (<c>flip-rtl</c>); the searchbar, which Ionic gives it as SVG, doesn't, and a name given here
    /// is mirrored there as an arrow's is.
    /// </summary>
    /// <remarks>
    /// The back button reads it when it renders: its first attach and each change of one of its props, as Ionic reads it in
    /// <c>render()</c>. The searchbar reads it when it's made, as Ionic reads it for <c>cancelButtonIcon</c>'s default in the
    /// constructor: a change of this reaches only searchbars made after it. The value is the searchbar's at style priority, so an
    /// app's own wins.
    /// </remarks>
    public static string? BackButtonIcon { get; set; }

    /// <summary>
    /// Ionic's <c>backButtonText</c> config: the text of an <see cref="AvonicBackButton"/> whose <see cref="AvonicBackButton.Text"/>
    /// is null. Null is md's: no text. Read when the button renders: its first attach and each change of one of its props.
    /// </summary>
    public static string? BackButtonText { get; set; }

    /// <summary>
    /// Ionic's <c>backButtonDefaultHref</c> config: the <see cref="AvonicBackButton.DefaultHref"/> of a back button that has none
    /// when it's first attached, as Ionic reads it in <c>componentWillLoad</c>. Clearing the button's later doesn't read it again.
    /// </summary>
    public static string? BackButtonDefaultHref { get; set; }

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
    /// Ionic's <c>loadingSpinner</c> config: the <see cref="AvonicLoading.Spinner"/> of a loading that has none, before
    /// <see cref="Spinner"/>. Null, not set, falls back to <see cref="Spinner"/>, then md's <see cref="SpinnerName.Crescent"/>;
    /// <see cref="ConfigSpinner.None"/>, Ionic's <c>null</c>, is no spinner.
    /// </summary>
    /// <remarks>
    /// Read when the loading is first attached, as Ionic reads it in <c>componentWillLoad</c>: for one made in code, at its first
    /// present. A change of this reaches only loadings attached for the first time after it.
    /// </remarks>
    public static ConfigSpinner? LoadingSpinner { get; set; }

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

    /// <summary>
    /// Ionic's <c>menuIcon</c> config: the icon of an <see cref="AvonicMenuButton"/>, an <see cref="AvonicIcon.Icon"/>. Null is md's
    /// <c>menu-sharp</c>.
    /// </summary>
    /// <remarks>
    /// Read when the button renders, as Ionic reads it in <c>render()</c>: when it's first attached, and at each change of one of
    /// its properties. A change of this alone redraws nothing.
    /// </remarks>
    public static string? MenuIcon { get; set; }

    /// <summary>
    /// Ionic's <c>menuType</c> config: the <see cref="AvonicMenu.Type"/> of a menu that has none. Null is Ionic's fallback,
    /// <see cref="Controls.MenuType.Overlay"/>.
    /// </summary>
    /// <remarks>
    /// Read when the menu is first attached, as Ionic reads it in <c>connectedCallback</c> while <c>type</c> is undefined: a change of
    /// this reaches only menus attached for the first time after it, and only if their type isn't set then.
    /// </remarks>
    public static MenuType? MenuType { get; set; }

    /// <summary>
    /// Ionic's <c>toastDuration</c> config: the <see cref="AvonicToast.Duration"/> of a toast, in milliseconds. 0, Ionic's fallback,
    /// keeps a toast until it's dismissed.
    /// </summary>
    /// <remarks>
    /// Read when the toast is made, as Ionic reads it as the prop's default: a change of this reaches only toasts made after it. It's
    /// the toast's value at style priority, so an app's own, set or in a style, wins.
    /// </remarks>
    public static double ToastDuration { get; set; }

    /// <summary>
    /// Ionic's <c>swipeBackEnabled</c> config: the <see cref="AvonicNav.NavSwipeGesture"/> of a nav that has none, whether a drag from
    /// the start edge goes back. False, Ionic's fallback in md.
    /// </summary>
    /// <remarks>
    /// Read when the nav is first attached, as Ionic reads it in <c>componentWillLoad</c> while <c>swipeGesture</c> is undefined: a
    /// change of this reaches only navs attached for the first time after it, and only if theirs isn't set then. On Android, a drag
    /// from the left edge is also the system's back gesture, which the system takes first where it's on.
    /// </remarks>
    public static bool SwipeBackEnabled { get; set; }

    /// <summary>
    /// Ionic's <c>navAnimation</c> config: the page transition of an <see cref="AvonicNav"/> whose call and own
    /// <see cref="AvonicNav.Animation"/> give none. Null is md's.
    /// </summary>
    /// <remarks>Read at each transition, as Ionic reads it in <c>transition()</c>.</remarks>
    public static Animation.AnimationBuilder? NavAnimation { get; set; }

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

    /// <summary>
    /// Not Ionic's: the default of <see cref="AvonicDatetime.CalendarRendering"/>, how a datetime's calendar draws its months.
    /// <see cref="Controls.DatetimeCalendarRendering.ScrolledMonths"/> by default, which is Ionic's.
    /// </summary>
    /// <remarks>
    /// Read when the datetime is made, as a prop's default is: a change of this reaches only datetimes made after it, so set it at
    /// start-up. The value is the datetime's at style priority, so an app's own, set or in a style, wins.
    /// </remarks>
    public static DatetimeCalendarRendering DatetimeCalendarRendering { get; set; }

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

    /// <summary>
    /// Ionic's <c>inputShims</c> config: whether an <see cref="AvonicApp"/> starts the scroll assist and scroll padding for its
    /// fields. Default on Android and iOS, where the on-screen keyboard covers the app instead of resizing it, as Ionic's
    /// <c>needInputShims()</c> is for iOS and for Chrome on Android; and on Windows, where the touch keyboard covers the window
    /// the same way; off elsewhere. Read when the app is attached.
    /// </summary>
    /// <remarks>
    /// On Windows, a deviation: Ionic's is off in a desktop browser. There they follow the touch keyboard: a field's focus waits
    /// for the window's input pane (<see cref="TopLevel.InputPane"/>) to open over the window, and reckons with the height it
    /// covers, as most Windows PCs type on a keyboard of their own and the touch keyboard comes only in tablet use. With no
    /// keyboard ever shown, nothing happens, and the padding goes when the keyboard does. On Android and iOS, and on macOS and
    /// Linux where an app turns them on, they're Ionic's: a field's focus reckons with <see cref="KeyboardHeight"/> at once. See
    /// <c>docs/components/ion-app/README.md</c>.
    /// </remarks>
    public static bool InputShims { get; set; } = InputShimsByDefault(Controls.InputShims.IsPlatform);

    /// <summary><see cref="InputShims"/>'s default for the platform <paramref name="isPlatform"/> says it is.</summary>
    internal static bool InputShimsByDefault(Func<string, bool> isPlatform) =>
        KeyboardIsTheOnlyOne(isPlatform) || isPlatform("windows");

    /// <summary>
    /// Whether the on-screen keyboard is the only keyboard on the platform <paramref name="isPlatform"/> says it is, so that a
    /// field's focus brings it, as Ionic reckons: Android and iOS.
    /// </summary>
    internal static bool KeyboardIsTheOnlyOne(Func<string, bool> isPlatform) => isPlatform("android") || isPlatform("ios");

    /// <summary>
    /// Ionic's <c>scrollAssist</c> config: whether a field that takes focus where the keyboard would cover it scrolls its content
    /// to bring it above the keyboard. Default <see langword="true"/>. Read when the app is attached, as Ionic reads it when
    /// <c>ion-app</c> loads.
    /// </summary>
    public static bool ScrollAssist { get; set; } = true;

    /// <summary>
    /// Ionic's <c>scrollPadding</c> config: whether the scroll assist pads the end of the content by the keyboard's height
    /// (<see cref="AvonicContent.KeyboardOffset"/>), so that a field near the end can scroll above it. Default
    /// <see langword="true"/>. Read when the app is attached.
    /// </summary>
    public static bool ScrollPadding { get; set; } = true;

    /// <summary>
    /// Ionic's <c>keyboardHeight</c> config: the keyboard's height the scroll assist reckons with when a field takes focus,
    /// before the keyboard has said its own. Default 290. Read when the app is attached.
    /// </summary>
    public static double KeyboardHeight { get; set; } = 290;

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
