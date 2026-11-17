using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avonic.Components.Animation;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// The sheet: a modal with <see cref="Breakpoints"/> and <see cref="InitialBreakpoint"/> sits at the bottom at a fraction of its
/// height, and is dragged between those fractions (<c>modal.js</c>, <c>gestures/sheet.js</c>, <c>animations/sheet.js</c>). See
/// <c>docs/components/ion-modal/PLAN.md</c>, "The sheet".
/// </summary>
public partial class AvonicModal
{
    /// <summary>Ionic's <c>modal-sheet</c> host class: <see cref="Breakpoints"/> and <see cref="InitialBreakpoint"/> are both set.</summary>
    public const string Sheet = ":sheet";

    /// <summary>
    /// Ionic <c>breakpoints</c>: the fractions of its height, from 0 (closed) to 1 (fully open), a sheet stops at. XAML reads
    /// <c>"0, 0.25, 0.5, 1"</c>. They're kept sorted, as Ionic sorts its array when it's set.
    /// </summary>
    public static readonly StyledProperty<AvaloniaList<double>?> BreakpointsProperty =
        AvaloniaProperty.Register<AvonicModal, AvaloniaList<double>?>(nameof(Breakpoints));

    /// <summary>Ionic <c>initialBreakpoint</c>: where a sheet opens, one of <see cref="Breakpoints"/>.</summary>
    public static readonly StyledProperty<double?> InitialBreakpointProperty =
        AvaloniaProperty.Register<AvonicModal, double?>(nameof(InitialBreakpoint));

    /// <summary>
    /// Ionic <c>backdropBreakpoint</c>: at or below this breakpoint the backdrop is hidden and the page behind takes input; above it,
    /// it fades in.
    /// </summary>
    public static readonly StyledProperty<double> BackdropBreakpointProperty =
        AvaloniaProperty.Register<AvonicModal, double>(nameof(BackdropBreakpoint));

    /// <summary>Ionic <c>handle</c>: the bar at the top of a sheet. Null, Ionic's <c>undefined</c>, shows it on a sheet, as true does.</summary>
    public static readonly StyledProperty<bool?> HandleProperty =
        AvaloniaProperty.Register<AvonicModal, bool?>(nameof(Handle));

    /// <summary>Ionic <c>handleBehavior</c>: whether a press on the handle cycles through the breakpoints.</summary>
    public static readonly StyledProperty<ModalHandleBehavior> HandleBehaviorProperty =
        AvaloniaProperty.Register<AvonicModal, ModalHandleBehavior>(nameof(HandleBehavior), ModalHandleBehavior.Cycle);

    /// <summary>
    /// Ionic <c>expandToScroll</c>: a drag on the content of a sheet below its top breakpoint expands it, and the content scrolls
    /// only at the top one. False, the content scrolls at every breakpoint, and the sheet moves by its header or handle.
    /// </summary>
    public static readonly StyledProperty<bool> ExpandToScrollProperty =
        AvaloniaProperty.Register<AvonicModal, bool>(nameof(ExpandToScroll), defaultValue: true);

    /// <summary>Ionic <c>ionBreakpointDidChange</c>: a sheet has settled at another breakpoint.</summary>
    public static readonly RoutedEvent<ModalBreakpointChangeEventArgs> IonBreakpointDidChangeEvent =
        RoutedEvent.Register<AvonicModal, ModalBreakpointChangeEventArgs>(nameof(IonBreakpointDidChange), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionDragStart</c>: a drag of the sheet has started.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonDragStartEvent =
        RoutedEvent.Register<AvonicModal, RoutedEventArgs>(nameof(IonDragStart), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionDragMove</c>: the sheet moved with the drag.</summary>
    public static readonly RoutedEvent<ModalDragEventArgs> IonDragMoveEvent =
        RoutedEvent.Register<AvonicModal, ModalDragEventArgs>(nameof(IonDragMove), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionDragEnd</c>: the drag ended, and the sheet snaps to <see cref="ModalDragEventArgs.SnapBreakpoint"/>.</summary>
    public static readonly RoutedEvent<ModalDragEventArgs> IonDragEndEvent =
        RoutedEvent.Register<AvonicModal, ModalDragEventArgs>(nameof(IonDragEnd), RoutingStrategies.Bubble);

    /// <summary>
    /// Ionic's <c>--ion-modal-offset-top</c>: the root's <c>Avonic.SafeAreaTop</c>, which a sheet's height leaves out, as the modal's
    /// own is 0. Set by the modal in its resources.
    /// </summary>
    internal const string OffsetTopKey = "Avonic.Modal.OffsetTop";

    /// <summary>Ionic's sorted <c>breakpoints</c> (<c>breakpointsChanged</c>).</summary>
    private double[] _sortedBreakpoints = [];
    private double? _currentBreakpoint;
    private bool _isSheetModal;
    private bool _sheetLoaded;
    private ModalSheet? _sheet;
    private AvonicAnimation? _sheetAnimation;
    private Task? _sheetTransition;
    private bool _gestureAnimationDismissing;
    private ModalHandle? _handle;
    private IDisposable? _tabStop;
    private IDisposable? _offsetTopWatch;
    private TopLevel? _keyboardTopLevel;
    private bool _focusEnteredFromOutside;

    public AvaloniaList<double>? Breakpoints
    {
        get => GetValue(BreakpointsProperty);
        set => SetValue(BreakpointsProperty, value);
    }

    public double? InitialBreakpoint
    {
        get => GetValue(InitialBreakpointProperty);
        set => SetValue(InitialBreakpointProperty, value);
    }

    public double BackdropBreakpoint
    {
        get => GetValue(BackdropBreakpointProperty);
        set => SetValue(BackdropBreakpointProperty, value);
    }

    public bool? Handle
    {
        get => GetValue(HandleProperty);
        set => SetValue(HandleProperty, value);
    }

    public ModalHandleBehavior HandleBehavior
    {
        get => GetValue(HandleBehaviorProperty);
        set => SetValue(HandleBehaviorProperty, value);
    }

    public bool ExpandToScroll
    {
        get => GetValue(ExpandToScrollProperty);
        set => SetValue(ExpandToScrollProperty, value);
    }

    public event EventHandler<ModalBreakpointChangeEventArgs>? IonBreakpointDidChange
    {
        add => AddHandler(IonBreakpointDidChangeEvent, value);
        remove => RemoveHandler(IonBreakpointDidChangeEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonDragStart
    {
        add => AddHandler(IonDragStartEvent, value);
        remove => RemoveHandler(IonDragStartEvent, value);
    }

    public event EventHandler<ModalDragEventArgs>? IonDragMove
    {
        add => AddHandler(IonDragMoveEvent, value);
        remove => RemoveHandler(IonDragMoveEvent, value);
    }

    public event EventHandler<ModalDragEventArgs>? IonDragEnd
    {
        add => AddHandler(IonDragEndEvent, value);
        remove => RemoveHandler(IonDragEndEvent, value);
    }

    /// <summary>
    /// A sheet's <c>disableBackdrop</c> is on: at or below <see cref="BackdropBreakpoint"/>, or with <see cref="FocusTrap"/> or
    /// <see cref="ShowBackdrop"/> off. It adds <c>ion-disable-focus-trap</c>.
    /// </summary>
    internal bool SheetBackdropOff { get; set; }

    /// <summary>The handle part.</summary>
    internal ModalHandle? HandlePart => _handle;

    /// <summary>The sheet's gesture and snapping, from its present to its dismiss.</summary>
    internal ModalSheet? SheetGesture => _sheet;

    /// <summary>The breakpoint after a sheet's present, as its enter and leave animations are given it.</summary>
    protected override object? AnimationOptions =>
        new ModalAnimationOptions(_currentBreakpoint ?? InitialBreakpoint, BackdropBreakpoint, ExpandToScroll);

    /// <summary>Ionic <c>getCurrentBreakpoint()</c>: where the sheet is, or null when it isn't presented.</summary>
    public double? GetCurrentBreakpoint() => _currentBreakpoint;

    /// <summary>
    /// Ionic <c>setCurrentBreakpoint(breakpoint)</c>: moves the sheet there, animated with <see cref="AvonicOverlay.Animated"/>, and
    /// completes when it's there. Nothing, with a warning in the log, for a modal that isn't a sheet or a breakpoint not in
    /// <see cref="Breakpoints"/>; nothing for the breakpoint it's at. 0 dismisses it, with the role <c>gesture</c>.
    /// </summary>
    public async Task SetCurrentBreakpoint(double breakpoint)
    {
        if (!_isSheetModal)
        {
            Warn("setCurrentBreakpoint is only supported on sheet modals.");
            return;
        }

        if (!_sortedBreakpoints.Contains(breakpoint))
        {
            Warn(string.Create(CultureInfo.InvariantCulture,
                $"Attempted to set invalid breakpoint value {breakpoint}. Please double check that the breakpoint value is part of your defined breakpoints."));
            return;
        }

        if (_currentBreakpoint == breakpoint || _currentBreakpoint is not { } current || _sheet is not { } sheet)
        {
            return;
        }

        _sheetTransition = sheet.MoveSheetToBreakpoint(
            breakpoint, 1 - current, !CanDismiss.IsTrue && _sortedBreakpoints[0] == 0, Animated);
        await _sheetTransition;
        _sheetTransition = null;
    }

    /// <summary>Ionic <c>moveToNextBreakpoint</c>: the next breakpoint that isn't 0, from the last back to the first.</summary>
    private async Task MoveToNextBreakpoint()
    {
        if (_sortedBreakpoints.Length == 0 || _currentBreakpoint is not { } current)
        {
            return;
        }

        var allowed = _sortedBreakpoints.Where(b => b != 0).ToArray();
        var next = allowed[(Array.IndexOf(allowed, current) + 1) % allowed.Length];
        await SetCurrentBreakpoint(next);
    }

    /// <summary>Ionic's <c>onHandleClick</c>: with <see cref="ModalHandleBehavior.Cycle"/>, and the sheet still.</summary>
    private void OnHandleClick(object? sender, EventArgs e)
    {
        if (HandleBehavior != ModalHandleBehavior.Cycle || _sheetTransition is not null)
        {
            return;
        }

        LogTaskFault(MoveToNextBreakpoint());
    }

    /// <summary>Ionic's <c>onBackdropTap</c>: not while the sheet moves between breakpoints, as a double click on the handle would.</summary>
    protected override void OnBackdropTap()
    {
        if (_sheetTransition is not null)
        {
            return;
        }

        base.OnBackdropTap();
    }

    /// <summary>Ionic's own <c>present()</c> after the shared one: a sheet's gesture, and the keyboard's listener.</summary>
    protected override void OnPresented()
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            _keyboardTopLevel = topLevel;
            topLevel.AddHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnKeyboardDidShow);
        }

        if (_isSheetModal)
        {
            InitSheetGesture();
        }
    }

    /// <summary>
    /// Ionic's <c>keyboardOpenCallback</c>: the gesture is turned off for a frame when the keyboard opens, as the resize would leave
    /// it unresponsive in a browser.
    /// </summary>
    private void OnKeyboardDidShow(object? sender, KeyboardDidShowEventArgs e)
    {
        if (_sheet is not { } sheet)
        {
            return;
        }

        sheet.Gesture.Enable(false);
        AvonicConfig.RequestFrame(this, () =>
        {
            if (ReferenceEquals(_sheet, sheet))
            {
                sheet.Gesture.Enable(true);
            }
        });
    }

    /// <summary>
    /// Ionic <c>initSheetGesture</c>: <see cref="AvonicOverlay.EnterAnimation"/>, or the ios enter animation as md's gesture uses it,
    /// held fully open, and the gesture over it.
    /// </summary>
    private void InitSheetGesture()
    {
        if (Wrapper is not { } wrapper || Backdrop is not { } backdrop || InitialBreakpoint is not { } initialBreakpoint)
        {
            return;
        }

        var options = new ModalAnimationOptions(initialBreakpoint, BackdropBreakpoint, ExpandToScroll);
        var animation = (EnterAnimation ?? SheetGestureAnimation)(this, options);
        _sheetAnimation = animation;
        animation.ProgressStart(true, 1);
        _sheet = new ModalSheet(
            this,
            backdrop,
            wrapper,
            initialBreakpoint,
            BackdropBreakpoint,
            animation,
            _sortedBreakpoints,
            ExpandToScroll,
            () => _currentBreakpoint ?? 0,
            SheetOnDismiss,
            breakpoint =>
            {
                if (_currentBreakpoint != breakpoint)
                {
                    _currentBreakpoint = breakpoint;
                    RaiseEvent(new ModalBreakpointChangeEventArgs(IonBreakpointDidChangeEvent, breakpoint));
                }
            });
        _sheet.Gesture.Enable(true);
    }

    /// <summary>
    /// Ionic <c>sheetOnDismiss</c>: once the snap to 0 has finished, the breakpoint is 0 and it dismisses with <c>gesture</c>; until
    /// then, other dismisses are refused, so that a backdrop tap doesn't play the leave animation again.
    /// </summary>
    private void SheetOnDismiss()
    {
        _gestureAnimationDismissing = true;
        _sheetAnimation?.OnFinish(async (_, _) =>
        {
            _currentBreakpoint = 0;
            RaiseEvent(new ModalBreakpointChangeEventArgs(IonBreakpointDidChangeEvent, 0));
            try
            {
                await Dismiss(null, OverlayRoles.Gesture);
            }
            finally
            {
                _gestureAnimationDismissing = false;
            }
        });
    }

    /// <summary>After a dismiss: the gesture's animation and the gesture go, and the content scrolls as it did.</summary>
    private void TearDownSheet()
    {
        _sheetAnimation?.Destroy();
        _sheetAnimation = null;
        _sheet?.Gesture.Destroy();
        _sheet?.ResetContentScroll();
        _sheet = null;
        _sheetTransition = null;
        SheetBackdropOff = false;
        _backdrop?.ClearValue(IsHitTestVisibleProperty);
        _keyboardTopLevel?.RemoveHandler(KeyboardAssist.IonKeyboardDidShowEvent, OnKeyboardDidShow);
        _keyboardTopLevel = null;
        _offsetTopWatch?.Dispose();
        _offsetTopWatch = null;
    }

    /// <summary>
    /// <c>createSheetEnterAnimation</c> in md's timing (<paramref name="easing"/>, <paramref name="duration"/>): the wrapper up from
    /// below to the breakpoint, the backdrop in to its share of <c>--backdrop-opacity</c> if the breakpoint is above
    /// <see cref="BackdropBreakpoint"/>, and with <see cref="ExpandToScroll"/> off, the content as tall as what shows.
    /// </summary>
    private AvonicAnimation SheetEnterAnimation(ModalAnimationOptions options, string easing, double duration)
    {
        var (wrapper, backdrop, content) = SheetEnterParts(options);
        var baseAnimation = AvonicAnimation.Create()
            .AddElement(this)
            .Easing(easing)
            .Duration(duration)
            .AddAnimation(backdrop, wrapper);
        return content is null ? baseAnimation : baseAnimation.AddAnimation(content);
    }

    /// <summary>
    /// <c>iosEnterAnimation</c> with no presenting element, which md's sheet gesture drives: the same parts, 500 ms,
    /// <c>cubic-bezier(0.32,0.72,0,1)</c>. Its wrapper's inline <c>opacity: 1</c> is left out: md's own enter animation, which holds
    /// the wrapper at 1 until the dismiss, is there the whole time.
    /// </summary>
    private AnimationBuilder SheetGestureAnimation => (baseElement, options) =>
    {
        var modal = (AvonicModal)baseElement;
        var (wrapper, backdrop, content) = modal.SheetEnterParts((ModalAnimationOptions)options!);
        var baseAnimation = AvonicAnimation.Create("entering-base")
            .AddElement(modal)
            .Easing("cubic-bezier(0.32,0.72,0,1)")
            .Duration(ModalSheet.SnapDuration)
            .AddAnimation(wrapper);
        if (content is not null)
        {
            baseAnimation.AddAnimation(content);
        }

        return baseAnimation.AddAnimation(backdrop);
    };

    private (AvonicAnimation Wrapper, AvonicAnimation Backdrop, AvonicAnimation? Content) SheetEnterParts(ModalAnimationOptions options)
    {
        var currentBreakpoint = options.CurrentBreakpoint ?? 0;

        // The backdrop fades in only when the breakpoint is above backdropBreakpoint.
        var shouldShowBackdrop = options.BackdropBreakpoint < currentBreakpoint;
        var backdrop = AvonicAnimation.Create("backdropAnimation")
            .AddElement(_backdrop)
            .FromTo("opacity", 0, shouldShowBackdrop ? ModalSheet.BackdropOpacity(this, currentBreakpoint) : 0);
        if (shouldShowBackdrop)
        {
            backdrop.BeforeStyles(new() { ["pointer-events"] = "none" }).AfterClearStyles(["pointer-events"]);
        }

        var wrapper = AvonicAnimation.Create("wrapperAnimation")
            .AddElement(_wrapper)
            .Keyframes([
                new AnimationKeyframe(0) { ["opacity"] = 1, ["transform"] = "translateY(100%)" },
                new AnimationKeyframe(1) { ["opacity"] = 1, ["transform"] = $"translateY({ModalSheet.Percent(100 - currentBreakpoint * 100)})" },
            ]);

        // The content scrolls at any breakpoint with expandToScroll off.
        var content = options.ExpandToScroll
            ? null
            : AvonicAnimation.Create("contentAnimation")
                .AddElement(_presenter)
                .Keyframes([
                    new AnimationKeyframe(0) { ["opacity"] = 1, ["maxHeight"] = ModalSheet.Percent((1 - currentBreakpoint) * 100) },
                    new AnimationKeyframe(1) { ["opacity"] = 1, ["maxHeight"] = ModalSheet.Percent(currentBreakpoint * 100) },
                ]);
        return (wrapper, backdrop, content);
    }

    /// <summary>
    /// <c>createSheetLeaveAnimation</c>, md's: 200 ms, the wrapper down from the breakpoint, the backdrop out from its share, which
    /// reaches 0 at <see cref="BackdropBreakpoint"/> when that's set.
    /// </summary>
    private AvonicAnimation SheetLeaveAnimation(double currentBreakpoint, double backdropBreakpoint)
    {
        var backdropValue = ModalSheet.BackdropOpacity(this, ModalSheet.GetBackdropValueForSheet(currentBreakpoint, backdropBreakpoint));
        var backdrop = AvonicAnimation.Create("backdropAnimation")
            .AddElement(_backdrop)
            .Keyframes(backdropBreakpoint != 0
                ?
                [
                    new AnimationKeyframe(0) { ["opacity"] = backdropValue },
                    new AnimationKeyframe(backdropBreakpoint) { ["opacity"] = 0 },
                    new AnimationKeyframe(1) { ["opacity"] = 0 },
                ]
                :
                [
                    new AnimationKeyframe(0) { ["opacity"] = backdropValue },
                    new AnimationKeyframe(1) { ["opacity"] = 0 },
                ]);
        var wrapper = AvonicAnimation.Create("wrapperAnimation")
            .AddElement(_wrapper)
            .Keyframes([
                new AnimationKeyframe(0) { ["opacity"] = 1, ["transform"] = $"translateY({ModalSheet.Percent(100 - currentBreakpoint * 100)})" },
                new AnimationKeyframe(1) { ["opacity"] = 1, ["transform"] = "translateY(100%)" },
            ]);
        return AvonicAnimation.Create()
            .Easing("cubic-bezier(0.47,0,0.745,0.715)")
            .Duration(200)
            .AddAnimation(backdrop, wrapper);
    }

    /// <summary>
    /// <c>isSheetModal</c>, which Ionic works out at load and again at each present: <see cref="Breakpoints"/> and
    /// <see cref="InitialBreakpoint"/> both set. It's <c>modal-sheet</c>, and shows the handle.
    /// </summary>
    private void UpdateIsSheetModal()
    {
        _isSheetModal = Breakpoints is not null && InitialBreakpoint is not null;
        PseudoClasses.Set(Sheet, _isSheetModal);
        UpdateHandle();
    }

    /// <summary>An <see cref="InitialBreakpoint"/> that isn't in <see cref="Breakpoints"/>: Ionic's warning at load.</summary>
    private void WarnIfInitialBreakpointMissing()
    {
        if (Breakpoints is { } breakpoints && InitialBreakpoint is { } initial && !breakpoints.Contains(initial))
        {
            Warn("Your breakpoints array must include the initialBreakpoint value.");
        }
    }

    /// <summary>
    /// <c>render</c>'s handle: shown on a sheet unless <see cref="Handle"/> is false, a tab stop when it cycles; and the host, a tab
    /// stop then too (<c>tabindex</c> 0), so that it can pass focus on to it.
    /// </summary>
    private void UpdateHandle()
    {
        var showHandle = Handle != false && _isSheetModal;
        var cycle = HandleBehavior == ModalHandleBehavior.Cycle;
        if (_handle is not null)
        {
            _handle.SetValue(IsVisibleProperty, showHandle, BindingPriority.Style);
            _handle.SetValue(KeyboardNavigation.IsTabStopProperty, cycle, BindingPriority.Style);
        }

        if (cycle && showHandle)
        {
            _tabStop ??= SetValue(KeyboardNavigation.IsTabStopProperty, true, BindingPriority.Style);
        }
        else
        {
            _tabStop?.Dispose();
            _tabStop = null;
        }
    }

    private void SetHandle(ModalHandle? handle)
    {
        if (_handle is not null)
        {
            _handle.Click -= OnHandleClick;
        }

        _handle = handle;
        if (_handle is not null)
        {
            _handle.Click += OnHandleClick;
        }

        UpdateHandle();
    }

    private void OnSheetPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == BreakpointsProperty)
        {
            // breakpointsChanged: sorted when set.
            _sortedBreakpoints = Breakpoints is { } breakpoints ? [.. breakpoints.Order()] : [];
        }
        else if (change.Property == HandleProperty || change.Property == HandleBehaviorProperty)
        {
            UpdateHandle();
        }
        else if (change.Property == IsKeyboardFocusWithinProperty && change.GetNewValue<bool>())
        {
            _focusEnteredFromOutside = true;
        }
    }

    /// <summary>
    /// Ionic's <c>onModalFocus</c>: a focus on the host itself, from outside it, goes on to the handle when it's a tab stop. Focus
    /// that moves to the host from inside, as a drag's start does, stays there.
    /// </summary>
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        var fromOutside = _focusEnteredFromOutside;
        _focusEnteredFromOutside = false;
        if (fromOutside && ReferenceEquals(e.Source, this) && _handle is { IsEffectivelyVisible: true } handle
            && KeyboardNavigation.GetIsTabStop(handle))
        {
            handle.Focus(NavigationMethod.Unspecified);
        }
    }

    /// <summary>
    /// A sheet's safe area, fixed at present (<c>getInitialSafeAreaConfig</c>): 0 at the top, left and right, the app's at the bottom;
    /// and <c>--ion-modal-offset-top</c>, the root's top, which its height leaves out, kept up to date while it's presented.
    /// </summary>
    private void UpdateSheetSafeArea()
    {
        _offsetTopWatch?.Dispose();
        _offsetTopWatch = null;
        if (!_isSheetModal)
        {
            _safeArea.Remove(OffsetTopKey);
            UpdateSafeArea();
            return;
        }

        SetZeroEdges((Top: true, Bottom: false, Left: true, Right: true));
        if (Parent is IResourceHost root)
        {
            _offsetTopWatch = root.GetResourceObservable("Avonic.SafeAreaTop").Subscribe(new OffsetTopObserver(this));
        }
    }

    private sealed class OffsetTopObserver(AvonicModal modal) : IObserver<object?>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(object? value) => modal._safeArea[OffsetTopKey] = value is double top ? top : 0d;
    }

    internal void RaiseDragStart() => RaiseEvent(new RoutedEventArgs(IonDragStartEvent, this));

    internal void RaiseDragMove(ModalDragEventArgs e) => RaiseEvent(e);

    internal void RaiseDragEnd(ModalDragEventArgs e) => RaiseEvent(e);

    private void Warn(string message) =>
        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "[ion-modal] - {Message}", message);

    private void LogTaskFault(Task task) =>
        task.ContinueWith(
            t => Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "The sheet's move failed: {Exception}", t.Exception!.InnerException),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
}
