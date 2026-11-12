using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-segment-view</c>: <see cref="AvonicSegmentContent"/>s side by side in a horizontal scroller, one in view, which
/// a finger pages one at a time, and an <see cref="AvonicSegment"/> whose buttons name them pages in code. Its children are
/// <see cref="ItemsControl.Items"/>, as <c>core.json</c> lists no slot. See <c>docs/components/ion-segment/README.md</c>.
/// </summary>
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
public class AvonicSegmentView : ItemsControl
{
    /// <summary>The time after the last scroll that a scroll by <see cref="SetContent"/> counts as over, as Ionic's timeout.</summary>
    internal static readonly TimeSpan ScrollEndDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>Ionic <c>swipeGesture</c>: a finger pages it. True by default. Code still pages it when false.</summary>
    public static readonly StyledProperty<bool> SegmentViewSwipeGestureProperty =
        AvaloniaProperty.Register<AvonicSegmentView, bool>(nameof(SegmentViewSwipeGesture), true);

    /// <summary>Ionic <c>ionSegmentViewScroll</c>: on every scroll, by a finger or by <see cref="SetContent"/>.</summary>
    public static readonly RoutedEvent<SegmentViewScrollEventArgs> IonSegmentViewScrollEvent =
        RoutedEvent.Register<AvonicSegmentView, SegmentViewScrollEventArgs>(nameof(IonSegmentViewScroll), RoutingStrategies.Bubble);

    public event EventHandler<SegmentViewScrollEventArgs>? IonSegmentViewScroll
    {
        add => AddHandler(IonSegmentViewScrollEvent, value);
        remove => RemoveHandler(IonSegmentViewScrollEvent, value);
    }

    private ScrollViewer? _scroll;
    private SmoothScroll? _smooth;
    private IDisposable? _scrollEndTimeout;
    private bool _isTouching;
    private bool? _isManualScroll;
    private int _page;
    private readonly StyleValue<bool> _scrollDisabled = new();

    static AvonicSegmentView()
    {
        SegmentViewSwipeGestureProperty.Changed.AddClassHandler<AvonicSegmentView>((view, _) => view.UpdateSwipe());
        IsEnabledProperty.Changed.AddClassHandler<AvonicSegmentView>((view, _) => view.UpdateSwipe());
    }

    public AvonicSegmentView()
    {
        // touchstart and touchend: whether a finger is on it.
        AddHandler(PointerPressedEvent, (_, e) => { if (e.Pointer.Type == PointerType.Touch) HandleScrollStart(); }, RoutingStrategies.Tunnel, true);
        AddHandler(PointerReleasedEvent, (_, e) => { if (e.Pointer.Type == PointerType.Touch) _isTouching = false; }, RoutingStrategies.Tunnel, true);
        AddHandler(PointerCaptureLostEvent, (_, e) => { if (e.Pointer.Type == PointerType.Touch) _isTouching = false; }, RoutingStrategies.Bubble, true);

        // At style priority, as CSS is, bound once, before any style, so that an app's style still wins when it turns off after it.
        Bind(IsHitTestVisibleProperty, _scrollDisabled, BindingPriority.Style);
    }

    public bool SegmentViewSwipeGesture
    {
        get => GetValue(SegmentViewSwipeGestureProperty);
        set => SetValue(SegmentViewSwipeGestureProperty, value);
    }

    /// <summary>Whether it's scrolling for <see cref="SetContent"/>: false then, as Ionic's <c>isManualScroll</c>, and null after.</summary>
    internal bool? IsManualScroll => _isManualScroll;

    /// <summary><c>segment-view-disabled</c> and <c>segment-view-swipe-disabled</c> are <c>overflow-x: hidden</c>.</summary>
    internal bool CanSwipe => IsEnabled && SegmentViewSwipeGesture;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _smooth?.Stop();
        if (_scroll is not null)
        {
            _scroll.ScrollChanged -= HandleScroll;
            _scroll.TemplateApplied -= OnScrollTemplateApplied;
        }

        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        _smooth = _scroll is null ? null : new SmoothScroll(_scroll);
        if (_scroll is not null)
        {
            _scroll.ScrollChanged += HandleScroll;
            _scroll.TemplateApplied += OnScrollTemplateApplied;
        }
    }

    private void OnScrollTemplateApplied(object? sender, TemplateAppliedEventArgs e) => UpdateSwipe();

    /// <summary>The width the view was given, which each content is, as the scroller inside doesn't pass it on.</summary>
    internal double PageWidth { get; private set; } = double.PositiveInfinity;

    protected override Size MeasureOverride(Size availableSize)
    {
        // The scroller measures the row with an infinite width whatever the view's, so it has to be told.
        if (PageWidth != availableSize.Width)
        {
            PageWidth = availableSize.Width;
            ItemsPanelRoot?.InvalidateMeasure();
        }

        return base.MeasureOverride(availableSize);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        // A new width keeps the same content in view, as scroll snapping does after a layout change, or goes on to the one it was
        // scrolling to. The page is the one kept from the last scroll, as the scroller has clamped its offset by now.
        if (_scroll is not null && _smooth is not null && e.WidthChanged && e.PreviousSize.Width > 0)
        {
            var page = _page;
            var smooth = _smooth.IsRunning;
            Dispatcher.UIThread.Post(() => _smooth.To(page * Bounds.Width, smooth), DispatcherPriority.Loaded);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _smooth?.Stop();
        _scrollEndTimeout?.Dispose();
        _scrollEndTimeout = null;
        _isTouching = false;

        // Ionic's timeout still fires after the view leaves, so it comes back taking input.
        SetManualScroll(null);
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>The touch scroll follows <see cref="CanSwipe"/>.</summary>
    private void UpdateSwipe()
    {
        var presenter = _scroll?.Presenter as ScrollContentPresenter;
        foreach (var recognizer in presenter?.GestureRecognizers.OfType<ScrollGestureRecognizer>() ?? [])
        {
            recognizer.CanHorizontallyScroll = CanSwipe;
        }
    }

    /// <summary><c>handleScroll</c>: <c>ionSegmentViewScroll</c>, then the scroll end's timeout again.</summary>
    private void HandleScroll(object? sender, ScrollChangedEventArgs e)
    {
        if (_scroll is null || e.OffsetDelta.X == 0)
        {
            return;
        }

        var max = _scroll.Extent.Width - _scroll.Viewport.Width;
        var scrollRatio = max > 0 ? _scroll.Offset.X / max : 0;

        // Where it is, unless SetContent is taking it somewhere: for a resize.
        if (_smooth?.IsRunning != true && Bounds.Width > 0)
        {
            _page = (int)Math.Round(_scroll.Offset.X / Bounds.Width, MidpointRounding.AwayFromZero);
        }

        RaiseEvent(new SegmentViewScrollEventArgs(IonSegmentViewScrollEvent, scrollRatio, _isManualScroll ?? true));
        ResetScrollEndTimeout();
    }

    /// <summary><c>handleScrollStart</c>: a finger is on it.</summary>
    private void HandleScrollStart()
    {
        _scrollEndTimeout?.Dispose();
        _scrollEndTimeout = null;
        _isTouching = true;
    }

    private void ResetScrollEndTimeout()
    {
        _scrollEndTimeout?.Dispose();
        _scrollEndTimeout = AvonicConfig.Schedule(CheckForScrollEnd, ScrollEndDelay);
    }

    /// <summary><c>checkForScrollEnd</c>: with no finger on it, a scroll from now on is a person's.</summary>
    private void CheckForScrollEnd()
    {
        _scrollEndTimeout = null;
        if (!_isTouching)
        {
            SetManualScroll(null);
        }
    }

    /// <summary><c>isManualScroll</c>, and <c>segment-view-scroll-disabled</c>'s <c>pointer-events: none</c> while it's false.</summary>
    private void SetManualScroll(bool? value)
    {
        _isManualScroll = value;
        _scrollDisabled.SetWhen(value == false, false);
    }

    /// <summary>
    /// Ionic's internal <c>setContent</c>: show the content with this <c>Name</c>, scrolling to it smoothly or at once. The
    /// segment calls it.
    /// </summary>
    internal void SetContent(string id, bool smoothScroll = true)
    {
        var contents = this.GetLogicalDescendants().OfType<AvonicSegmentContent>().ToList();
        var index = contents.FindIndex(content => content.Name == id);
        if (index == -1 || _smooth is null)
        {
            return;
        }

        SetManualScroll(false);
        ResetScrollEndTimeout();
        _page = index;
        _smooth.To(index * Bounds.Width, smoothScroll);
    }
}
