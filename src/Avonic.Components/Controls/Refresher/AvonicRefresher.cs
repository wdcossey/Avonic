using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Logging;
using Avalonia.VisualTree;
using Avonic.Components.Gestures;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-refresher</c>: pull-to-refresh on the <see cref="AvonicContent"/> whose <see cref="AvonicContent.FixedContent"/>
/// it is. Holding an <see cref="AvonicRefresherContent"/> with a spinner in both places, it's md's native refresher: a round icon
/// that follows the finger down from the top of the content, and spins while the app refreshes, until it calls
/// <see cref="Complete"/>. See <c>docs/components/ion-refresher/README.md</c>.
/// </summary>
/// <remarks>
/// Ionic's legacy refresher, which moves the content itself and which md uses only with no spinner in either place, isn't here:
/// with no spinner the refresher does nothing. Its props, <c>pullMin</c>, <c>pullMax</c>, <c>pullFactor</c>,
/// <c>closeDuration</c> and <c>snapbackDuration</c>, don't apply to the native refresher, so they aren't declared.
/// </remarks>
[PseudoClasses(":native", ":active", ":pulling", ":ready", ":refreshing", ":cancelling", ":completing")]
public class AvonicRefresher : ContentControl
{
    /// <summary>Ionic <c>ionPull</c>: on each move of the finger once it's pulling.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonPullEvent =
        RoutedEvent.Register<AvonicRefresher, RoutedEventArgs>(nameof(IonPull), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionPullEnd</c>: the refresher is back to inactive after a pull, whether it refreshed or not.</summary>
    public static readonly RoutedEvent<RefresherPullEndEventArgs> IonPullEndEvent =
        RoutedEvent.Register<AvonicRefresher, RefresherPullEndEventArgs>(nameof(IonPullEnd), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionPullStart</c>: the finger has started pulling.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonPullStartEvent =
        RoutedEvent.Register<AvonicRefresher, RoutedEventArgs>(nameof(IonPullStart), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionRefresh</c>: the refresher is refreshing. Call <see cref="RefresherEventArgs.Complete"/> when done.</summary>
    public static readonly RoutedEvent<RefresherEventArgs> IonRefreshEvent =
        RoutedEvent.Register<AvonicRefresher, RefresherEventArgs>(nameof(IonRefresh), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionStart</c>: the same moment as <see cref="IonPullStartEvent"/>, which Ionic keeps as well.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonStartEvent =
        RoutedEvent.Register<AvonicRefresher, RoutedEventArgs>(nameof(IonStart), RoutingStrategies.Bubble);

    /// <summary>The gesture's <c>threshold</c>.</summary>
    internal const double Threshold = 5;

    /// <summary><c>transitionEndAsync</c>'s fallback, past the expected 200 ms, when no transition ends.</summary>
    internal static readonly TimeSpan TransitionFallback = TimeSpan.FromMilliseconds(200 + 500);

    private RefresherState _state = RefresherState.Inactive;
    private double _progress;
    private bool _nativeRefresher;

    private AvonicContent? _contentEl;
    private ScrollViewer? _scrollEl;
    private Gesture? _gesture;
    private AvonicRefresherContent? _refresherContent;

    // ev.data, for the press being pulled
    private PullData? _data;

    // The press the gesture is deciding about, for handing an upward drag to the scroller
    private IPointer? _deciding;
    private Point _pressPoint;

    private readonly List<IDisposable> _animations = [];
    private readonly List<PendingReset> _resets = [];

    static AvonicRefresher()
    {
        // pointer-events: none
        IsHitTestVisibleProperty.OverrideDefaultValue<AvonicRefresher>(false);

        // Its icon starts above it, and overflow is visible. Avalonia clips by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicRefresher>(false);
    }

    /// <inheritdoc cref="IonPullEvent"/>
    public event EventHandler<RoutedEventArgs>? IonPull
    {
        add => AddHandler(IonPullEvent, value);
        remove => RemoveHandler(IonPullEvent, value);
    }

    /// <inheritdoc cref="IonPullEndEvent"/>
    public event EventHandler<RefresherPullEndEventArgs>? IonPullEnd
    {
        add => AddHandler(IonPullEndEvent, value);
        remove => RemoveHandler(IonPullEndEvent, value);
    }

    /// <inheritdoc cref="IonPullStartEvent"/>
    public event EventHandler<RoutedEventArgs>? IonPullStart
    {
        add => AddHandler(IonPullStartEvent, value);
        remove => RemoveHandler(IonPullStartEvent, value);
    }

    /// <inheritdoc cref="IonRefreshEvent"/>
    public event EventHandler<RefresherEventArgs>? IonRefresh
    {
        add => AddHandler(IonRefreshEvent, value);
        remove => RemoveHandler(IonRefreshEvent, value);
    }

    /// <inheritdoc cref="IonStartEvent"/>
    public event EventHandler<RoutedEventArgs>? IonStart
    {
        add => AddHandler(IonStartEvent, value);
        remove => RemoveHandler(IonStartEvent, value);
    }

    /// <summary>Ionic's <c>state</c>.</summary>
    internal RefresherState State
    {
        get => _state;
        private set
        {
            if (_state == value)
            {
                return;
            }

            _state = value;
            UpdatePseudoClasses();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Whether it's md's native refresher (<c>nativeRefresher</c>).</summary>
    internal bool IsNative => _nativeRefresher;

    /// <summary>Raised when <see cref="State"/> changes, for <see cref="AvonicRefresherContent"/>.</summary>
    internal event EventHandler? StateChanged;

    /// <summary>
    /// Ionic <c>complete()</c>: call it when the refresh is done. Two frames later the refreshing icon shrinks away, and once it's
    /// gone the refresher is inactive again and raises <see cref="IonPullEnd"/>.
    /// </summary>
    public void Complete()
    {
        if (_nativeRefresher)
        {

            // needsCompletion and pointerDown are only read by iOS's native refresher, so md's always goes on
            AfterTwoFrames(() => ResetNativeRefresher(RefresherState.Completing));
        }
    }

    /// <summary>
    /// Ionic <c>cancel()</c>: the refresher closes as cancelled. Two frames later it's cancelling, and it's inactive once the
    /// refreshing icon's transition would have ended: 700 ms, Ionic's fallback, as nothing transitions.
    /// </summary>
    public void Cancel()
    {
        if (_nativeRefresher)
        {
            AfterTwoFrames(() => ResetNativeRefresher(RefresherState.Cancelling));
        }
    }

    /// <summary>
    /// Ionic <c>getProgress()</c>: how far it's been pulled, from 0. The native refresher refreshes on letting go past 0.4, and
    /// stops at 1. Synchronous, as Ionic's promise resolves at once.
    /// </summary>
    public double GetProgress() => _progress;

    /// <summary>
    /// <c>width: 100%</c> of the content, while the fixed slot places its child at the top left, at its own size: it's as wide as
    /// it's given.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return double.IsInfinity(availableSize.Width) ? size : size.WithWidth(availableSize.Width);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Connect();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Disconnect();
    }

    /// <summary>
    /// <c>disconnectedCallback</c>. Destroying the gesture mid-pull calls nothing back, as in Ionic, so the pull's own bookkeeping is
    /// dropped here, and the next pull starts afresh.
    /// </summary>
    private void Disconnect()
    {
        _nativeRefresher = false;
        _scrollEl?.RemoveHandler(PointerMovedEvent, OnScrollPreviewMoved);
        _scrollEl?.RemoveHandler(PointerReleasedEvent, OnScrollReleased);
        _scrollEl = null;
        _contentEl = null;
        _gesture?.Destroy();
        _gesture = null;
        _deciding = null;
        _data = null;
        UpdatePseudoClasses();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            // disabledChanged
            _gesture?.Enable(IsEnabled);
        }
        else if (change.Property == ContentProperty && VisualRoot is not null)
        {
            // Ionic reads its ion-refresher-content when it connects, and again after each render (checkNativeRefresher). A new
            // content is looked at again, from the start.
            Disconnect();
            Connect();
        }
    }

    /// <summary>
    /// <c>connectedCallback</c>: only in an <see cref="AvonicContent"/>'s fixed slot, as Ionic only works with <c>slot="fixed"</c>
    /// in an <c>ion-content</c>, and logs otherwise. Ionic waits for the content to be ready; its scroll element comes with its
    /// template, which is applied on demand.
    /// </summary>
    private void Connect()
    {
        if (FindFixedSlot() is not { } content)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(
                this, "AvonicRefresher must be the FixedContent of an AvonicContent, as ion-refresher needs slot=\"fixed\".");
            return;
        }

        _contentEl = content;
        _scrollEl = content.GetScrollElement();
        if (_scrollEl is null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "AvonicRefresher found no ScrollViewer in {Host}.", content);
            return;
        }

        if (ShouldUseNativeRefresher())
        {
            SetupNativeRefresher();
        }
    }

    /// <summary>
    /// The content whose fixed slot it's in, through any panel there, as an <c>ion-fab</c> can share the slot: the nearest
    /// <see cref="AvonicContent"/>, if the way up to it is through its <c>PART_Fixed</c>.
    /// </summary>
    private AvonicContent? FindFixedSlot()
    {
        for (var visual = this.GetVisualParent(); visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is ContentPresenter { Name: "PART_Fixed", TemplatedParent: AvonicContent content })
            {
                return content;
            }

            if (visual is AvonicContent)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// <c>shouldUseNativeRefresher</c> in md: a refresher content with a pulling spinner and a refreshing spinner, once it's loaded.
    /// </summary>
    private bool ShouldUseNativeRefresher()
    {
        _refresherContent = Content as AvonicRefresherContent
                            ?? this.GetLogicalDescendants().OfType<AvonicRefresherContent>().FirstOrDefault();
        if (_refresherContent is null)
        {
            return false;
        }

        _refresherContent.WillLoad();
        return _refresherContent.HasSpinners;
    }

    /// <summary><c>setupNativeRefresher</c> and <c>setupMDNativeRefresher</c>.</summary>
    private void SetupNativeRefresher()
    {
        if (_nativeRefresher || _scrollEl is not { } scrollEl)
        {
            return;
        }

        _nativeRefresher = true;
        UpdatePseudoClasses();

        // Before the gesture's own handlers, so that an upward drag is handed to the scroller before the gesture decides.
        scrollEl.AddHandler(PointerMovedEvent, OnScrollPreviewMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        scrollEl.AddHandler(PointerReleasedEvent, OnScrollReleased, RoutingStrategies.Tunnel, handledEventsToo: true);

        _gesture = Gesture.Create(new GestureConfig
        {
            Element = scrollEl,
            GestureName = "refresher",
            GesturePriority = 31,
            Direction = GestureDirection.Y,
            Threshold = Threshold,
            CanStart = CanStart,
            OnStart = OnStart,
            OnMove = OnMove,
            OnEnd = OnEnd,
            NotCaptured = _ => _deciding = null,
        });

        // disabledChanged
        _gesture.Enable(IsEnabled);
    }

    private bool CanStart(GestureDetail detail)
    {
        var canStart = State != RefresherState.Refreshing
                       && State != RefresherState.Completing
                       && _scrollEl?.Offset.Y == 0;
        if (canStart)
        {
            _deciding = detail.Event?.Pointer;
            _pressPoint = new Point(detail.StartX, detail.StartY);
        }

        return canStart;
    }

    /// <summary>
    /// In Chromium an upward drag from the top scrolls the content, and Ionic's passive gesture, which captures it too, cancels at
    /// its first move. Avalonia gives a pointer to one owner, so the gesture lets go of an upward drag at its threshold, before it
    /// would capture it, and the scroller, which has followed the press, scrolls from there.
    /// </summary>
    private void OnScrollPreviewMoved(object? sender, PointerEventArgs e)
    {
        if (_deciding is null || !ReferenceEquals(e.Pointer, _deciding) || _data is not null || _gesture is null)
        {
            return;
        }

        var point = e.GetPosition(null);
        var delta = point - _pressPoint;
        if (delta.Y < 0 && Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) >= Threshold)
        {
            _deciding = null;
            _gesture.Enable(false);
            _gesture.Enable(IsEnabled);
        }
    }

    /// <summary>A press the controller refused calls nothing back, so the press is forgotten when it ends.</summary>
    private void OnScrollReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ReferenceEquals(e.Pointer, _deciding))
        {
            _deciding = null;
        }
    }

    private void OnStart(GestureDetail detail)
    {
        _deciding = null;
        _progress = 0;
        _data = new PullData();
        detail.Data = _data;
    }

    private void OnMove(GestureDetail detail)
    {
        if (detail.Data is not PullData data)
        {
            return;
        }

        if ((detail.VelocityY < 0 && _progress == 0 && !data.DidStart) || data.Cancelled)
        {
            data.Cancelled = true;
            return;
        }

        if (!data.DidStart)
        {
            data.DidStart = true;
            State = RefresherState.Pulling;

            // overflow: hidden on the scroll element: the captured gesture already keeps the scroller off the pointer.
            data.Type = GetRefresherAnimationType();
            data.Height = Bounds.Height;

            // progressStart(false, 0)
            var pulling = new PullingAnimation(this, data);
            data.Animation = pulling;
            pulling.Step(0);
            RaiseEvent(new RoutedEventArgs(IonStartEvent));
            RaiseEvent(new RoutedEventArgs(IonPullStartEvent));
            _animations.Add(pulling);
            return;
        }

        // Since we are using an easing curve, slow the gesture tracking down a bit
        _progress = Math.Clamp(detail.DeltaY / 180 * 0.5, 0, 1);
        data.Animation!.Step(_progress);
        RaiseEvent(new RoutedEventArgs(IonPullEvent));
    }

    private void OnEnd(GestureDetail detail)
    {
        var data = detail.Data as PullData;
        _data = null;
        _deciding = null;
        if (data is null || !data.DidStart || _gesture is not { } gesture)
        {
            return;
        }

        gesture.Enable(false);
        var animation = data.Animation!;
        if (_progress <= RefresherPullFrame.RefreshThreshold)
        {
            // progressEnd(0, progress, 500): back to 0, over what's left of 500 ms in reverse
            animation.PlayBack(_progress, () =>
            {
                DestroyAnimations();
                gesture.Enable(IsEnabled);
                State = RefresherState.Inactive;
                RaiseEvent(new RefresherPullEndEventArgs(IonPullEndEvent, RefresherPullEndReason.Cancel));
            });
            return;
        }

        // getTimeGivenProgression on a linear curve is the progress itself
        var progress = _progress;
        var snapBack = new SnapBackAnimation(this, progress * RefresherPullFrame.SnapBackDistance);
        _animations.Add(snapBack);

        // writeTask
        _animations.Add(AvonicConfig.RequestFrame(this, () =>
        {
            snapBack.Play(() =>
            {
                BeginRefresh();
                animation.Dispose();
                _animations.Remove(animation);
                gesture.Enable(IsEnabled);
            });
        }));
    }

    /// <summary><c>getRefresherAnimationType</c>: translate when an <see cref="AvonicHeader"/> is the content's previous sibling.</summary>
    private RefresherAnimationType GetRefresherAnimationType()
    {
        if (_contentEl?.GetVisualParent() is Panel panel)
        {
            var index = panel.Children.IndexOf(_contentEl);
            if (index > 0 && panel.Children[index - 1] is AvonicHeader)
            {
                return RefresherAnimationType.Translate;
            }
        }

        return RefresherAnimationType.Scale;
    }

    /// <summary><c>beginRefresh</c>.</summary>
    private void BeginRefresh()
    {
        State = RefresherState.Refreshing;

        // Clear focus from any active element to prevent scroll jumps after the refresh
        TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);

        RaiseEvent(new RefresherEventArgs(IonRefreshEvent, Complete));
    }

    /// <summary>
    /// <c>resetNativeRefresher</c> in md: the state, then the refreshing icon's transition, or Ionic's fallback when none runs,
    /// then inactive.
    /// </summary>
    private void ResetNativeRefresher(RefresherState state)
    {
        State = state;

        // transitionEndAsync(.refresher-refreshing-icon, 200): the icon's transitionend, which ends every wait on it, or the
        // fallback. The completing state's scale runs one when the icon was showing; the content says when it ends.
        var now = AvonicConfig.Now();
        var reset = new PendingReset(state, now + TransitionFallback);
        _resets.Add(reset);
        if (_refresherContent?.IconTransitionEndsAt is { } endsAt && endsAt >= now)
        {
            foreach (var pending in _resets.Where(pending => pending.Due > endsAt).ToArray())
            {
                pending.Due = endsAt;
                pending.Timer?.Dispose();
                pending.Timer = null;
            }
        }

        foreach (var pending in _resets.Where(pending => pending.Timer is null).ToArray())
        {
            pending.Timer = AvonicConfig.Schedule(() => FinishReset(pending), pending.Due - now);
        }
    }

    /// <summary>The rest of <c>resetNativeRefresher</c>, once the wait is over: inactive, and <c>ionPullEnd</c>.</summary>
    private void FinishReset(PendingReset reset)
    {
        _resets.Remove(reset);
        DestroyAnimations();
        _progress = 0;
        State = RefresherState.Inactive;
        RaiseEvent(new RefresherPullEndEventArgs(
            IonPullEndEvent,
            reset.State == RefresherState.Completing ? RefresherPullEndReason.Complete : RefresherPullEndReason.Cancel));
    }

    /// <summary>A <c>resetNativeRefresher</c> waiting for the icon's transition.</summary>
    private sealed class PendingReset(RefresherState state, TimeSpan due)
    {
        public RefresherState State { get; } = state;

        public TimeSpan Due { get; set; } = due;

        public IDisposable? Timer { get; set; }
    }

    private void AfterTwoFrames(Action action)
    {
        // raf(() => raf(...))
        AvonicConfig.RequestFrame(this, () => AvonicConfig.RequestFrame(this, action));
    }

    private void DestroyAnimations()
    {
        foreach (var animation in _animations.ToArray())
        {
            animation.Dispose();
        }

        _animations.Clear();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":native", _nativeRefresher);
        PseudoClasses.Set(":active", _state != RefresherState.Inactive);
        PseudoClasses.Set(":pulling", _state == RefresherState.Pulling);
        PseudoClasses.Set(":ready", _state == RefresherState.Ready);
        PseudoClasses.Set(":refreshing", _state == RefresherState.Refreshing);
        PseudoClasses.Set(":cancelling", _state == RefresherState.Cancelling);
        PseudoClasses.Set(":completing", _state == RefresherState.Completing);
    }

    /// <summary>Ionic's <c>ev.data</c> for a pull.</summary>
    private sealed class PullData
    {
        public bool DidStart;
        public bool Cancelled;
        public RefresherAnimationType Type;
        public double Height;
        public PullingAnimation? Animation;
    }

    /// <summary>
    /// <c>createPullingAnimation</c>, held at a step as Ionic's <c>progressStep</c> does, or played back to 0 as
    /// <c>progressEnd(0, step, 500)</c> does, on <see cref="AvonicConfig"/>'s clock.
    /// </summary>
    private sealed class PullingAnimation(AvonicRefresher owner, PullData data) : IDisposable
    {
        private IDisposable? _frame;
        private bool _disposed;

        public void Step(double step)
        {
            if (!_disposed)
            {
                owner._refresherContent?.ApplyPull(RefresherPullFrame.At(step, data.Type, data.Height));
            }
        }

        /// <summary>
        /// Plays in reverse from <paramref name="step"/>: the directed progress falls from it to 0 over <paramref name="step"/> ×
        /// 500 ms, and the ease-out reads it, as Web Animations apply the easing after the direction.
        /// </summary>
        public void PlayBack(double step, Action onFinish)
        {
            var from = Math.Clamp(step, 0, RefresherPullFrame.MaxStep);
            var start = AvonicConfig.Now();
            void Frame()
            {
                _frame = null;
                if (_disposed)
                {
                    return;
                }

                var elapsed = (AvonicConfig.Now() - start).TotalMilliseconds;
                var at = Math.Max(0, from - elapsed / RefresherPullFrame.CancelDuration);
                Step(at);
                if (at <= 0)
                {
                    onFinish();
                }
                else
                {
                    _frame = AvonicConfig.RequestFrame(owner, Frame);
                }
            }

            _frame = AvonicConfig.RequestFrame(owner, Frame);
        }

        public void Dispose()
        {
            _disposed = true;
            _frame?.Dispose();
            _frame = null;
            owner._refresherContent?.ClearPull();
        }
    }

    /// <summary><c>createSnapBackAnimation</c>: the pulling icon from <c>translateY(progress × 100px)</c> to 0 in 125 ms, linear.</summary>
    private sealed class SnapBackAnimation(AvonicRefresher owner, double from) : IDisposable
    {
        private IDisposable? _frame;
        private bool _disposed;

        public void Play(Action onFinish)
        {
            var start = AvonicConfig.Now();
            void Frame()
            {
                _frame = null;
                if (_disposed)
                {
                    return;
                }

                var t = Math.Min(1, (AvonicConfig.Now() - start).TotalMilliseconds / RefresherPullFrame.SnapBackDuration);
                owner._refresherContent?.ApplySnapBack(from * (1 - t));
                if (t >= 1)
                {
                    // fill: both holds the end until it's destroyed
                    onFinish();
                }
                else
                {
                    _frame = AvonicConfig.RequestFrame(owner, Frame);
                }
            }

            Frame();
        }

        public void Dispose()
        {
            _disposed = true;
            _frame?.Dispose();
            _frame = null;
            owner._refresherContent?.ClearSnapBack();
        }
    }
}
