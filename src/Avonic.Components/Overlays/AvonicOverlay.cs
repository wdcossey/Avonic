using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;

namespace Avonic.Components.Overlays;

/// <summary>
/// The base of every overlay control: Ionic's <c>utils/overlays</c>, <c>present</c> and <c>dismiss</c> and what they share,
/// with each overlay's <c>lockController</c>. What every overlay in <c>core.json</c> has is declared here; what only some have
/// is a protected member the control overrides. See <c>docs/infra/overlays/README.md</c>.
/// </summary>
/// <remarks>
/// <see cref="Present()"/> attaches it to its <see cref="TopLevel"/>'s overlay host, in the <c>OverlayLayer</c>, hidden until its
/// enter animation starts, as <c>overlay-hidden</c> is; <see cref="Dismiss"/> removes it. It's focusable and not a tab stop, as
/// an overlay's <c>tabindex="-1"</c>.
/// </remarks>
public abstract class AvonicOverlay : TemplatedControl
{
    /// <summary>Ionic <c>animated</c>.</summary>
    public static readonly StyledProperty<bool> AnimatedProperty =
        AvaloniaProperty.Register<AvonicOverlay, bool>(nameof(Animated), defaultValue: true);

    /// <summary>Ionic <c>keyboardClose</c>. True, as on every overlay but the toast, which overrides it.</summary>
    public static readonly StyledProperty<bool> KeyboardCloseProperty =
        AvaloniaProperty.Register<AvonicOverlay, bool>(nameof(KeyboardClose), defaultValue: true);

    /// <summary>Ionic <c>enterAnimation</c>.</summary>
    public static readonly StyledProperty<AnimationBuilder?> EnterAnimationProperty =
        AvaloniaProperty.Register<AvonicOverlay, AnimationBuilder?>(nameof(EnterAnimation));

    /// <summary>Ionic <c>leaveAnimation</c>.</summary>
    public static readonly StyledProperty<AnimationBuilder?> LeaveAnimationProperty =
        AvaloniaProperty.Register<AvonicOverlay, AnimationBuilder?>(nameof(LeaveAnimation));

    /// <summary>Ionic <c>willPresent</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonWillPresentEvent =
        RoutedEvent.Register<AvonicOverlay, RoutedEventArgs>(nameof(IonWillPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>didPresent</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonDidPresentEvent =
        RoutedEvent.Register<AvonicOverlay, RoutedEventArgs>(nameof(IonDidPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>willDismiss</c>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonWillDismissEvent =
        RoutedEvent.Register<AvonicOverlay, OverlayEventArgs>(nameof(IonWillDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>didDismiss</c>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonDidDismissEvent =
        RoutedEvent.Register<AvonicOverlay, OverlayEventArgs>(nameof(IonDidDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic's <c>lastOverlayIndex</c>, for the whole process, as Ionic's is for the page.</summary>
    private static int s_lastOverlayIndex;

    private readonly OverlayLock _lock = new();
    private readonly List<AvonicAnimation> _activeAnimations = [];
    private IDisposable? _hidden;
    private IDisposable? _noPointer;
    private OverlayHost? _host;
    private (TopLevel TopLevel, IInputElement Element)? _focusToRestore;
    private bool _presented;

    static AvonicOverlay()
    {
        FocusableProperty.OverrideDefaultValue<AvonicOverlay>(true);
        KeyboardNavigation.IsTabStopProperty.OverrideDefaultValue<AvonicOverlay>(false);
    }

    protected AvonicOverlay()
    {
        // overlay-hidden is display: none !important, so it beats any value.
        _hidden = SetValue(IsVisibleProperty, false, BindingPriority.Animation);
        AddHandler(AvonicBackdrop.IonBackdropTapEvent, (_, _) => OnBackdropTap());
    }

    public bool Animated
    {
        get => GetValue(AnimatedProperty);
        set => SetValue(AnimatedProperty, value);
    }

    public bool KeyboardClose
    {
        get => GetValue(KeyboardCloseProperty);
        set => SetValue(KeyboardCloseProperty, value);
    }

    public AnimationBuilder? EnterAnimation
    {
        get => GetValue(EnterAnimationProperty);
        set => SetValue(EnterAnimationProperty, value);
    }

    public AnimationBuilder? LeaveAnimation
    {
        get => GetValue(LeaveAnimationProperty);
        set => SetValue(LeaveAnimationProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? IonWillPresent
    {
        add => AddHandler(IonWillPresentEvent, value);
        remove => RemoveHandler(IonWillPresentEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonDidPresent
    {
        add => AddHandler(IonDidPresentEvent, value);
        remove => RemoveHandler(IonDidPresentEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonWillDismiss
    {
        add => AddHandler(IonWillDismissEvent, value);
        remove => RemoveHandler(IonWillDismissEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonDidDismiss
    {
        add => AddHandler(IonDidDismissEvent, value);
        remove => RemoveHandler(IonDidDismissEvent, value);
    }

    /// <summary>Ionic <c>overlayIndex</c>: given when it's attached, from 1. Its <c>ZIndex</c> is <see cref="ZIndexBase"/> plus this.</summary>
    internal int OverlayIndex { get; private set; }

    /// <summary>Not <c>overlay-hidden</c>: from the start of its enter animation to the end of its dismiss.</summary>
    internal bool IsShown => _hidden is null;

    /// <summary>The focus trap's <c>lastFocus</c>.</summary>
    internal IInputElement? LastFocus { get; set; }

    /// <summary>Ionic <c>backdropDismiss</c>, which every overlay but the toast declares: whether Escape, the back button and a backdrop tap dismiss it.</summary>
    protected internal virtual bool BackdropDismissEnabled => false;

    /// <summary>Not a toast: it blurs the focused element at present and gives focus back after, and it can trap focus.</summary>
    protected internal virtual bool StealsFocus => true;

    /// <summary>Ionic's <c>ion-disable-focus-trap</c> is the opposite: a modal or popover with <c>focusTrap</c> false.</summary>
    protected internal virtual bool FocusTrapEnabled => true;

    /// <summary>
    /// Ionic's <c>.ion-overlay-wrapper</c> in an overlay with no shadow root (alert, action-sheet, loading, picker): focus outside
    /// the overlay is left alone, and focus on a focusable part outside the wrapper, a sentinel, wraps. Null traps every focus
    /// outside the overlay, as the modal and popover do.
    /// </summary>
    protected internal virtual Control? FocusTrapWrapper => null;

    /// <summary>The z-index base: 20000, 40000 for loading, 60000 for toast.</summary>
    protected virtual int ZIndexBase => 20000;

    /// <summary>md's enter animation, used when <see cref="EnterAnimation"/> is null.</summary>
    protected abstract AnimationBuilder MdEnterAnimation { get; }

    /// <summary>md's leave animation, used when <see cref="LeaveAnimation"/> is null.</summary>
    protected abstract AnimationBuilder MdLeaveAnimation { get; }

    /// <summary>What the overlay gives its enter and leave animations, as a toast gives its position.</summary>
    protected virtual object? AnimationOptions => null;

    /// <summary>Ionic <c>present()</c>, in the application's top level: a desktop's active window, else its main one; a single view's.</summary>
    /// <exception cref="InvalidOperationException">There's no top level to present in.</exception>
    public Task Present() => OverlayHost.TopLevelOf(Application.Current?.ApplicationLifetime) is { } topLevel
        ? Present(topLevel)
        : Task.FromException(new InvalidOperationException(
            "There's no TopLevel to present the overlay in: the application has no desktop or single-view lifetime. Use Present(TopLevel)."));

    /// <summary>Ionic <c>present()</c>, in <paramref name="topLevel"/>. An Avonic extension, for an app with several windows.</summary>
    public async Task Present(TopLevel topLevel)
    {
        using var unlock = await _lock.Lock();
        if (_presented)
        {
            return;
        }

        // A dismiss always detaches it, so this is always a new attach, and a new index, as connectedCallback gives.
        var host = OverlayHost.For(topLevel);
        _host = host;
        OverlayIndex = ++s_lastOverlayIndex;
        ZIndex = ZIndexBase + OverlayIndex;
        host.Attach(this);
        await PresentCore(host.TopLevel);
    }

    /// <summary>
    /// Ionic <c>dismiss(data, role)</c>: true when it dismissed, false when it wasn't presented. It waits for a present that's
    /// still running.
    /// </summary>
    public async Task<bool> Dismiss(object? data = null, string? role = null)
    {
        using var unlock = await _lock.Lock();
        return await DismissCore(data, role);
    }

    /// <summary>Ionic <c>onWillDismiss()</c>: the detail of the next <see cref="IonWillDismiss"/>.</summary>
    public Task<OverlayEventDetail> OnWillDismiss() => NextEvent(IonWillDismissEvent);

    /// <summary>Ionic <c>onDidDismiss()</c>: the detail of the next <see cref="IonDidDismiss"/>, once the overlay has gone.</summary>
    public Task<OverlayEventDetail> OnDidDismiss() => NextEvent(IonDidDismissEvent);

    /// <summary>Ionic's <c>onIonBackdropTap</c>: an <see cref="AvonicBackdrop"/> in it was tapped.</summary>
    protected virtual void OnBackdropTap() => DismissFromInput(OverlayRoles.Backdrop);

    /// <summary>
    /// A dismiss that input started, which nothing awaits: a backdrop tap, Escape, the back button. An exception in it is logged,
    /// as Ionic's <c>dismiss</c> logs one with <c>printIonError</c>, rather than lost in a task no one reads.
    /// </summary>
    internal void DismissFromInput(string role) =>
        Dismiss(null, role).ContinueWith(
            task => Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "The overlay's dismiss failed: {Exception}", task.Exception!.InnerException),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

    private async Task PresentCore(TopLevel topLevel)
    {
        if (StealsFocus)
        {
            BlurForRestore(topLevel);
        }

        _presented = true;
        RaiseEvent(new RoutedEventArgs(IonWillPresentEvent, this));
        await OverlayAnimation(EnterAnimation ?? MdEnterAnimation);
        RaiseEvent(new RoutedEventArgs(IonDidPresentEvent, this));

        // Focus is left where it is if it's already inside.
        if (KeyboardClose && (Focused(topLevel) is not { } focused || !OverlayHost.Contains(this, focused)))
        {
            Focus();
        }
    }

    private async Task<bool> DismissCore(object? data, string? role)
    {
        if (!_presented)
        {
            return false;
        }

        _presented = false;
        var didDismiss = false;
        try
        {
            // Not clickable during the dismiss: pointer-events: none.
            _noPointer = SetValue(IsHitTestVisibleProperty, false, BindingPriority.Animation);
            RaiseEvent(new OverlayEventArgs(IonWillDismissEvent, data, role));
            if (role != OverlayRoles.Gesture)
            {
                await OverlayAnimation(LeaveAnimation ?? MdLeaveAnimation);
            }

            RaiseEvent(new OverlayEventArgs(IonDidDismissEvent, data, role));
            didDismiss = true;
        }
        finally
        {
            // Every animation it played goes, with the values they held; then overlay-hidden, and the element is removed.
            foreach (var animation in _activeAnimations)
            {
                animation.Destroy();
            }

            _activeAnimations.Clear();
            _hidden ??= SetValue(IsVisibleProperty, false, BindingPriority.Animation);
            _noPointer?.Dispose();
            _noPointer = null;
            LastFocus = null;
            _host?.Detach(this);
            _host = null;
            RestoreElementFocus(didDismiss);
        }

        return true;
    }

    /// <summary>Ionic <c>overlayAnimation</c>.</summary>
    private Task OverlayAnimation(AnimationBuilder builder)
    {
        _hidden?.Dispose();
        _hidden = null;
        ApplyTemplate();

        var animation = builder(this, AnimationOptions);
        if (!Animated || !AvonicConfig.Animated)
        {
            animation.Duration(0);
        }

        if (KeyboardClose)
        {
            animation.BeforeAddWrite(() =>
            {
                // input, ion-input, ion-textarea: what raises a keyboard.
                if (TopLevel.GetTopLevel(this) is { } top && Focused(top) is TextBox)
                {
                    Blur(top);
                }
            });
        }

        _activeAnimations.Add(animation);
        return animation.Play();
    }

    /// <summary>
    /// The first half of Ionic's <c>restoreElementFocus</c>: blur what has focus, and remember it for the dismiss.
    /// </summary>
    private void BlurForRestore(TopLevel topLevel)
    {
        _focusToRestore = null;
        if (Focused(topLevel) is { } previous)
        {
            _focusToRestore = (topLevel, previous);
            Blur(topLevel);
        }
    }

    /// <summary>
    /// The second half: after <see cref="IonDidDismiss"/>, give focus back unless something has it by then. Ionic awaits
    /// <c>onDidDismiss()</c>, so it runs once the overlay has gone, as a continuation, and never if the dismiss failed before the
    /// event, whose promise then never resolves. Forgotten either way, so a later dismiss can't use it.
    /// </summary>
    private void RestoreElementFocus(bool didDismiss)
    {
        if (_focusToRestore is not var (topLevel, previous))
        {
            return;
        }

        _focusToRestore = null;
        if (didDismiss)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (Focused(topLevel) is null || ReferenceEquals(Focused(topLevel), topLevel))
                {
                    previous.Focus();
                }
            });
        }
    }

    internal static IInputElement? Focused(TopLevel topLevel) => topLevel.FocusManager?.GetFocusedElement();

    private static void Blur(TopLevel topLevel) => (topLevel.FocusManager as FocusManager)?.Focus(null);

    private Task<OverlayEventDetail> NextEvent(RoutedEvent<OverlayEventArgs> routedEvent)
    {
        var next = new TaskCompletionSource<OverlayEventDetail>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<OverlayEventArgs>? handler = null;
        handler = (_, e) =>
        {
            RemoveHandler(routedEvent, handler!);
            next.TrySetResult(e.Detail);
        };
        // Every handled one too: Ionic's listener hears the event whatever another did with it.
        AddHandler(routedEvent, handler, RoutingStrategies.Bubble, handledEventsToo: true);
        return next.Task;
    }
}
