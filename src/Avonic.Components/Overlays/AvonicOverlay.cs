using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
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

    /// <summary>
    /// Ionic <c>isOpen</c>: false to true presents it, true to false dismisses it; true when it's first attached presents it at
    /// the next frame. A dismiss doesn't set it back.
    /// </summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<AvonicOverlay, bool>(nameof(IsOpen));

    /// <summary>Ionic <c>trigger</c>: the name of a control in the overlay's name scope whose click presents it.</summary>
    public static readonly StyledProperty<string?> TriggerProperty =
        AvaloniaProperty.Register<AvonicOverlay, string?>(nameof(Trigger));

    /// <summary>Ionic <c>willPresent</c>, the generic event of every overlay. Each control adds its own <c>Ion&lt;Name&gt;WillPresent</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> WillPresentEvent =
        RoutedEvent.Register<AvonicOverlay, RoutedEventArgs>(nameof(WillPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>didPresent</c>, the generic event of every overlay. Each control adds its own <c>Ion&lt;Name&gt;DidPresent</c>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> DidPresentEvent =
        RoutedEvent.Register<AvonicOverlay, RoutedEventArgs>(nameof(DidPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>willDismiss</c>, the generic event of every overlay. Each control adds its own <c>Ion&lt;Name&gt;WillDismiss</c>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> WillDismissEvent =
        RoutedEvent.Register<AvonicOverlay, OverlayEventArgs>(nameof(WillDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>didDismiss</c>, the generic event of every overlay. Each control adds its own <c>Ion&lt;Name&gt;DidDismiss</c>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> DidDismissEvent =
        RoutedEvent.Register<AvonicOverlay, OverlayEventArgs>(nameof(DidDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic's <c>lastOverlayIndex</c>, for the whole process, as Ionic's is for the page.</summary>
    private static int s_lastOverlayIndex;

    private readonly OverlayLock _lock = new();
    private readonly List<AvonicAnimation> _activeAnimations = [];
    private IDisposable? _hidden;
    private IDisposable? _noPointer;
    private OverlayHost? _host;
    private (TopLevel TopLevel, IInputElement Element)? _focusToRestore;
    private bool _presented;
    private readonly OverlayTrigger _trigger;
    private InlinePlacement? _placement;
    private bool _loaded;
    private bool _moving;
    private int _dismissesPending;

    static AvonicOverlay()
    {
        FocusableProperty.OverrideDefaultValue<AvonicOverlay>(true);
        KeyboardNavigation.IsTabStopProperty.OverrideDefaultValue<AvonicOverlay>(false);
    }

    protected AvonicOverlay()
    {
        // overlay-hidden is display: none !important, so it beats any value.
        _hidden = SetValue(IsVisibleProperty, false, BindingPriority.Animation);
        _trigger = new OverlayTrigger(this);
        AddHandler(AvonicBackdrop.IonBackdropTapEvent, (_, _) => OnBackdropTap());
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string? Trigger
    {
        get => GetValue(TriggerProperty);
        set => SetValue(TriggerProperty, value);
    }

    /// <summary>
    /// The placeholder an inline overlay left in its place while it's presented: it had a parent of the app's when it was
    /// presented, and has been moved to the overlay host until its dismiss puts it back (Ionic's <c>CoreDelegate</c>).
    /// </summary>
    internal Control? Placeholder => _placement?.Placeholder;

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

    public event EventHandler<RoutedEventArgs>? WillPresent
    {
        add => AddHandler(WillPresentEvent, value);
        remove => RemoveHandler(WillPresentEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? DidPresent
    {
        add => AddHandler(DidPresentEvent, value);
        remove => RemoveHandler(DidPresentEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? WillDismiss
    {
        add => AddHandler(WillDismissEvent, value);
        remove => RemoveHandler(WillDismissEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? DidDismiss
    {
        add => AddHandler(DidDismissEvent, value);
        remove => RemoveHandler(DidDismissEvent, value);
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

    /// <summary>The control's own <c>ion&lt;Name&gt;WillPresent</c>, raised just before <see cref="WillPresentEvent"/>, as Ionic does.</summary>
    protected virtual RoutedEvent<RoutedEventArgs>? ControlWillPresentEvent => null;

    /// <summary>The control's own <c>ion&lt;Name&gt;DidPresent</c>, raised just before <see cref="DidPresentEvent"/>.</summary>
    protected virtual RoutedEvent<RoutedEventArgs>? ControlDidPresentEvent => null;

    /// <summary>The control's own <c>ion&lt;Name&gt;WillDismiss</c>, raised just before <see cref="WillDismissEvent"/>.</summary>
    protected virtual RoutedEvent<OverlayEventArgs>? ControlWillDismissEvent => null;

    /// <summary>The control's own <c>ion&lt;Name&gt;DidDismiss</c>, raised just before <see cref="DidDismissEvent"/>.</summary>
    protected virtual RoutedEvent<OverlayEventArgs>? ControlDidDismissEvent => null;

    /// <summary>
    /// What present focuses with <see cref="KeyboardClose"/>: Ionic's <c>[role="dialog"][tabindex]</c> in the overlay's root (the
    /// modal's wrapper), or the overlay itself when null.
    /// </summary>
    protected virtual IInputElement? PresentFocusTarget => null;

    /// <summary>
    /// What an overlay's own <c>present()</c> does before the shared one: under the lock, when it isn't presented yet, once it's in
    /// the overlay host (an inline one's place in its page is <see cref="Placeholder"/>).
    /// </summary>
    protected virtual void OnPresenting()
    {
    }

    /// <summary>
    /// What an overlay's own <c>present()</c> does after the shared one, still under the lock: after its enter animation, its
    /// <c>didPresent</c> events and the focus, as the modal starts its sheet's gesture.
    /// </summary>
    protected virtual void OnPresented()
    {
    }

    /// <summary>
    /// What an overlay's own <c>dismiss()</c> does before the shared one, under the lock: false stops the dismiss, which then
    /// returns false, as the modal's <c>canDismiss</c> does.
    /// </summary>
    protected virtual Task<bool> OnDismissing(object? data, string? role) => Task.FromResult(true);

    /// <summary>What an overlay's own <c>dismiss()</c> does after the shared one, under the lock; <paramref name="dismissed"/> is its result.</summary>
    protected virtual void OnDismissed(bool dismissed)
    {
    }

    /// <summary>Ionic's <c>triggerEl</c>: the control <see cref="Trigger"/> names, while its listeners are on it.</summary>
    protected Interactive? TriggerElement => _trigger.Target;

    /// <summary>
    /// The listeners that make <paramref name="trigger"/>, the control <see cref="Trigger"/> names, present the overlay; returns
    /// what removes them. A click by default (<c>createTriggerController</c>); the popover's depend on its <c>triggerAction</c>.
    /// </summary>
    protected internal virtual Action AddTriggerListeners(Interactive trigger) =>
        OverlayTrigger.AddClickListener(trigger, (_, _) => PresentFromInput());

    /// <summary>
    /// Looks <see cref="Trigger"/> up again and gives it its listeners, as a watcher on a prop they depend on does (the popover's
    /// <c>triggerAction</c>). Nothing while the overlay is presented in the overlay host, out of its name scope.
    /// </summary>
    protected void ConfigureTrigger()
    {
        if (!IsInHost && ((ILogical)this).IsAttachedToLogicalTree)
        {
            _trigger.Attach(Trigger);
        }
    }

    /// <summary>
    /// Ionic <c>present()</c>. An inline overlay, one with a parent of the app's, presents in its own top level, as Ionic's
    /// teleports to its document's <c>ion-app</c>. Otherwise the application's top level: a desktop's active window, else its main
    /// one; a single view's; under Android's <c>MainViewFactory</c>, the current activity's.
    /// </summary>
    /// <exception cref="InvalidOperationException">There's no top level to present in.</exception>
    public Task Present()
    {
        // Inline, or still in a host during its leave animation: its own top level, where an inline one goes back to. In a
        // popup, the top level the popup belongs to, as a browser has one document.
        if (Parent is not null && TopLevel.GetTopLevel(this) is { } own)
        {
            while (own is PopupRoot { ParentTopLevel: { } parent })
            {
                own = parent;
            }

            return Present(own);
        }

        return OverlayHost.TopLevelOf(Application.Current?.ApplicationLifetime) is { } topLevel
            ? Present(topLevel)
            : Task.FromException(new InvalidOperationException(
                "There's no TopLevel to present the overlay in: the application has no desktop, single-view or activity lifetime, or none of its " +
                "top levels is open. Use Present(TopLevel)."));
    }

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
        if (Parent is not (null or OverlayHost) || this.GetVisualParent() is not (null or OverlayHost))
        {
            _moving = true;
            try
            {
                _placement = InlinePlacement.Teleport(this, OnPlaceRemoved);
            }
            finally
            {
                _moving = false;
            }
        }

        _host = host;
        OverlayIndex = ++s_lastOverlayIndex;
        ZIndex = ZIndexBase + OverlayIndex;
        host.Attach(this);
        OnPresenting();
        await PresentCore(host.TopLevel);
        OnPresented();
    }

    /// <summary>
    /// An inline overlay's place left the tree while it was presented: <c>parent-removed</c>, unless a dismiss is already on its
    /// way, as when the modal it's inside unmounts its content after dismissing it with <c>parent-dismissed</c>.
    /// </summary>
    private void OnPlaceRemoved()
    {
        if (_dismissesPending == 0)
        {
            DismissFromInput(OverlayRoles.ParentRemoved);
        }
    }

    /// <summary>
    /// Ionic <c>dismiss(data, role)</c>: true when it dismissed, false when it wasn't presented, or the overlay stopped it. It
    /// waits for a present that's still running.
    /// </summary>
    public async Task<bool> Dismiss(object? data = null, string? role = null)
    {
        _dismissesPending++;
        try
        {
            using var unlock = await _lock.Lock();
            if (!await OnDismissing(data, role))
            {
                return false;
            }

            bool dismissed;
            try
            {
                dismissed = await DismissCore(data, role);
            }
            catch
            {
                OnDismissed(true);
                throw;
            }

            OnDismissed(dismissed);
            return dismissed;
        }
        finally
        {
            _dismissesPending--;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // Stencil calls a watcher only once the component has loaded; before that, the first attach reads isOpen.
        if (change.Property == IsOpenProperty && (_loaded || _presented))
        {
            var (oldValue, newValue) = change.GetOldAndNewValue<bool>();
            if (newValue && !oldValue)
            {
                PresentFromInput();
            }
            else if (!newValue && oldValue)
            {
                DismissFromInput(null);
            }
        }
        else if (change.Property == TriggerProperty)
        {
            ConfigureTrigger();
        }
    }

    /// <summary>Ionic's <c>connectedCallback</c> and <c>componentDidLoad</c>: the trigger is looked up; <c>isOpen</c> at load presents.</summary>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (!IsInHost)
        {
            _trigger.Attach(Trigger);
        }
    }

    /// <summary>Ionic's <c>disconnectedCallback</c>: the trigger's listener goes. Not when it's moved for its presentation.</summary>
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        if (!_moving && !_presented && _placement is null)
        {
            _trigger.Remove();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_loaded)
        {
            return;
        }

        // Loaded at its first attach, which for one made in code is its present, as Ionic's controller attaches it at create.
        _loaded = true;
        if (IsOpen && !IsInHost)
        {
            AvonicConfig.RequestFrame(this, PresentFromInput);
        }
    }

    private bool IsInHost => Parent is OverlayHost;

    /// <summary>A present that nothing awaits (<c>isOpen</c>, a trigger), its exception logged as a dismiss's from input is.</summary>
    internal void PresentFromInput() => LogFault(Present(), "present");

    /// <summary>Ionic <c>onWillDismiss()</c>: the detail of the next <see cref="WillDismiss"/>.</summary>
    public Task<OverlayEventDetail> OnWillDismiss() => NextEvent(WillDismissEvent);

    /// <summary>Ionic <c>onDidDismiss()</c>: the detail of the next <see cref="DidDismiss"/>, once the overlay has gone.</summary>
    public Task<OverlayEventDetail> OnDidDismiss() => NextEvent(DidDismissEvent);

    /// <summary>Ionic's <c>onIonBackdropTap</c>: an <see cref="AvonicBackdrop"/> in it was tapped.</summary>
    protected virtual void OnBackdropTap() => DismissFromInput(OverlayRoles.Backdrop);

    /// <summary>
    /// A dismiss that input started, which nothing awaits: a backdrop tap, Escape, the back button. An exception in it is logged,
    /// as Ionic's <c>dismiss</c> logs one with <c>printIonError</c>, rather than lost in a task no one reads.
    /// </summary>
    internal void DismissFromInput(string? role) => LogFault(Dismiss(null, role), "dismiss");

    private protected void LogFault(Task task, string what) =>
        task.ContinueWith(
            t => Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "The overlay's {What} failed: {Exception}", what, t.Exception!.InnerException),
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
        if (ControlWillPresentEvent is { } controlWillPresent)
        {
            RaiseEvent(new RoutedEventArgs(controlWillPresent, this));
        }

        RaiseEvent(new RoutedEventArgs(WillPresentEvent, this));
        await OverlayAnimation(EnterAnimation ?? MdEnterAnimation);
        if (ControlDidPresentEvent is { } controlDidPresent)
        {
            RaiseEvent(new RoutedEventArgs(controlDidPresent, this));
        }

        RaiseEvent(new RoutedEventArgs(DidPresentEvent, this));

        // Focus is left where it is if it's already inside.
        if (KeyboardClose && (Focused(topLevel) is not { } focused || !OverlayHost.Contains(this, focused)))
        {
            if (PresentFocusTarget is not { } target || !target.Focus())
            {
                Focus();
            }
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
            if (ControlWillDismissEvent is { } controlWillDismiss)
            {
                RaiseEvent(new OverlayEventArgs(controlWillDismiss, data, role));
            }

            RaiseEvent(new OverlayEventArgs(WillDismissEvent, data, role));
            if (role != OverlayRoles.Gesture)
            {
                await OverlayAnimation(LeaveAnimation ?? MdLeaveAnimation);
            }

            if (ControlDidDismissEvent is { } controlDidDismiss)
            {
                RaiseEvent(new OverlayEventArgs(controlDidDismiss, data, role));
            }

            RaiseEvent(new OverlayEventArgs(DidDismissEvent, data, role));
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

            // An inline overlay goes back where it was: detachComponent's removeViewFromDom. If its page has left the tree, it
            // goes back there, detached, and nothing will detach it again: its trigger's listener goes now.
            _placement?.Restore();
            _placement = null;
            if (!((ILogical)this).IsAttachedToLogicalTree)
            {
                _trigger.Remove();
            }
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
    /// The second half: after <see cref="DidDismiss"/>, give focus back unless something has it by then. Ionic awaits
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
