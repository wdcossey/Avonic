using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-content</c>: the scrolling area of a page, with a background and a slot for content that doesn't
/// scroll. Momentum and the touch gesture are Avalonia's. See <c>docs/components/ion-content/PLAN.md</c>.
/// </summary>
[TemplatePart("PART_Background", typeof(Control))]
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
[TemplatePart("PART_Container", typeof(ContentPresenter))]
[TemplatePart("PART_Fixed", typeof(ContentPresenter))]
public class AvonicContent : TemplatedControl
{
    /// <summary>
    /// Ionic's <c>.ion-content-scroll-host</c>: the class that marks a custom scroller, which the infinite scroll and the reorder
    /// group take as their content's, as they take an <see cref="AvonicContent"/>.
    /// </summary>
    public const string ScrollHostClass = "ion-content-scroll-host";

    /// <summary>The <c>setInterval</c> of <c>onScrollStart</c>'s watchdog.</summary>
    internal static readonly TimeSpan WatchDogInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>How long after the last scroll the watchdog calls it ended.</summary>
    internal static readonly TimeSpan WatchDogIdle = TimeSpan.FromMilliseconds(120);

    /// <summary><c>scrollToPoint</c> jumps below this duration, in milliseconds.</summary>
    internal const double MinAnimatedDuration = 32;

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicContent>();

    /// <summary>The default slot.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<AvonicContent, object?>(nameof(Content));

    /// <summary>The template for <see cref="Content"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<AvonicContent, Avalonia.Controls.Templates.IDataTemplate?>(nameof(ContentTemplate));

    /// <summary>The <c>fixed</c> slot: content that doesn't scroll.</summary>
    public static readonly StyledProperty<object?> FixedContentProperty =
        AvaloniaProperty.Register<AvonicContent, object?>(nameof(FixedContent));

    /// <summary>The template for <see cref="FixedContent"/>.</summary>
    public static readonly StyledProperty<Avalonia.Controls.Templates.IDataTemplate?> FixedContentTemplateProperty =
        AvaloniaProperty.Register<AvonicContent, Avalonia.Controls.Templates.IDataTemplate?>(nameof(FixedContentTemplate));

    /// <summary>Ionic <c>fixedSlotPlacement</c>.</summary>
    public static readonly StyledProperty<ContentFixedSlotPlacement> FixedSlotPlacementProperty =
        AvaloniaProperty.Register<AvonicContent, ContentFixedSlotPlacement>(nameof(FixedSlotPlacement));

    /// <summary>
    /// Ionic <c>forceOverscroll</c>: makes content that fits scrollable by one pixel. Null is the default, which is
    /// off in md.
    /// </summary>
    public static readonly StyledProperty<bool?> ForceOverscrollProperty =
        AvaloniaProperty.Register<AvonicContent, bool?>(nameof(ForceOverscroll));

    /// <summary>Ionic <c>fullscreen</c>: the background and scrolling area reach the edges of the page, behind what's around the content.</summary>
    public static readonly StyledProperty<bool> FullscreenProperty =
        AvaloniaProperty.Register<AvonicContent, bool>(nameof(Fullscreen));

    /// <summary>Ionic <c>scrollEvents</c>: whether the scroll events are raised.</summary>
    public static readonly StyledProperty<bool> ScrollEventsProperty =
        AvaloniaProperty.Register<AvonicContent, bool>(nameof(ScrollEvents));

    /// <summary>Ionic <c>scrollX</c>.</summary>
    public static readonly StyledProperty<bool> ScrollXProperty =
        AvaloniaProperty.Register<AvonicContent, bool>(nameof(ScrollX));

    /// <summary>Ionic <c>scrollY</c>.</summary>
    public static readonly StyledProperty<bool> ScrollYProperty =
        AvaloniaProperty.Register<AvonicContent, bool>(nameof(ScrollY), true);

    /// <summary>
    /// Ionic <c>--keyboard-offset</c>: added to the bottom padding, on top of whatever
    /// <see cref="TemplatedControl.Padding"/> is. The theme sets it from a token. An extension, because in Ionic it's
    /// only a variable.
    /// </summary>
    public static readonly StyledProperty<double> KeyboardOffsetProperty =
        AvaloniaProperty.Register<AvonicContent, double>(nameof(KeyboardOffset));

    /// <summary>Ionic <c>ionScrollStart</c>.</summary>
    public static readonly RoutedEvent<ScrollBaseDetailEventArgs> IonScrollStartEvent =
        RoutedEvent.Register<AvonicContent, ScrollBaseDetailEventArgs>(nameof(IonScrollStart), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionScroll</c>.</summary>
    public static readonly RoutedEvent<ScrollDetailEventArgs> IonScrollEvent =
        RoutedEvent.Register<AvonicContent, ScrollDetailEventArgs>(nameof(IonScroll), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionScrollEnd</c>.</summary>
    public static readonly RoutedEvent<ScrollBaseDetailEventArgs> IonScrollEndEvent =
        RoutedEvent.Register<AvonicContent, ScrollBaseDetailEventArgs>(nameof(IonScrollEnd), RoutingStrategies.Bubble);


    private Control? _background;
    private ScrollViewer? _scroll;
    private ContentPresenter? _container;
    private ContentPresenter? _fixed;
    private ScrollGestureRecognizer? _recognizer;
    private RoutedEventArgs? _passedOver;
    private readonly List<IDisposable> _containerChildDefaults = [];
    private readonly List<IDisposable> _fixedChildDefaults = [];

    // componentOnReady, for a scroll requested before the first layout.
    private bool _hasLoaded;
    private bool _wasDetached;
    private TaskCompletionSource? _loaded;

    private double _offsetTop;
    private double _offsetBottom;

    private bool _isScrolling;
    private TimeSpan _lastScroll;
    private bool _queued;
    private IDisposable? _frame;
    private IDisposable? _watchDog;
    private ScrollState _detail;

    static AvonicContent()
    {
        // role="main". An overlay's own PR turns this off for the content inside it.
        AutomationProperties.LandmarkTypeProperty.OverrideDefaultValue<AvonicContent>(AutomationLandmarkType.Main);

        // The host has no overflow rule, and Fullscreen and the fixed slot reach outside it. Avalonia clips by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicContent>(false);
    }

    /// <inheritdoc cref="ColorProperty"/>
    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <inheritdoc cref="ContentProperty"/>
    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <inheritdoc cref="ContentTemplateProperty"/>
    public Avalonia.Controls.Templates.IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    /// <inheritdoc cref="FixedContentProperty"/>
    public object? FixedContent
    {
        get => GetValue(FixedContentProperty);
        set => SetValue(FixedContentProperty, value);
    }

    /// <inheritdoc cref="FixedContentTemplateProperty"/>
    public Avalonia.Controls.Templates.IDataTemplate? FixedContentTemplate
    {
        get => GetValue(FixedContentTemplateProperty);
        set => SetValue(FixedContentTemplateProperty, value);
    }

    /// <inheritdoc cref="FixedSlotPlacementProperty"/>
    public ContentFixedSlotPlacement FixedSlotPlacement
    {
        get => GetValue(FixedSlotPlacementProperty);
        set => SetValue(FixedSlotPlacementProperty, value);
    }

    /// <inheritdoc cref="ForceOverscrollProperty"/>
    public bool? ForceOverscroll
    {
        get => GetValue(ForceOverscrollProperty);
        set => SetValue(ForceOverscrollProperty, value);
    }

    /// <inheritdoc cref="FullscreenProperty"/>
    public bool Fullscreen
    {
        get => GetValue(FullscreenProperty);
        set => SetValue(FullscreenProperty, value);
    }

    /// <inheritdoc cref="KeyboardOffsetProperty"/>
    public double KeyboardOffset
    {
        get => GetValue(KeyboardOffsetProperty);
        set => SetValue(KeyboardOffsetProperty, value);
    }

    /// <inheritdoc cref="ScrollEventsProperty"/>
    public bool ScrollEvents
    {
        get => GetValue(ScrollEventsProperty);
        set => SetValue(ScrollEventsProperty, value);
    }

    /// <inheritdoc cref="ScrollXProperty"/>
    public bool ScrollX
    {
        get => GetValue(ScrollXProperty);
        set => SetValue(ScrollXProperty, value);
    }

    /// <inheritdoc cref="ScrollYProperty"/>
    public bool ScrollY
    {
        get => GetValue(ScrollYProperty);
        set => SetValue(ScrollYProperty, value);
    }

    /// <inheritdoc cref="IonScrollStartEvent"/>
    public event EventHandler<ScrollBaseDetailEventArgs>? IonScrollStart
    {
        add => AddHandler(IonScrollStartEvent, value);
        remove => RemoveHandler(IonScrollStartEvent, value);
    }

    /// <inheritdoc cref="IonScrollEvent"/>
    public event EventHandler<ScrollDetailEventArgs>? IonScroll
    {
        add => AddHandler(IonScrollEvent, value);
        remove => RemoveHandler(IonScrollEvent, value);
    }

    /// <inheritdoc cref="IonScrollEndEvent"/>
    public event EventHandler<ScrollBaseDetailEventArgs>? IonScrollEnd
    {
        add => AddHandler(IonScrollEndEvent, value);
        remove => RemoveHandler(IonScrollEndEvent, value);
    }

    /// <summary>
    /// Ionic <c>getScrollElement</c>. It's synchronous, because once it's in a tree the template can be applied on
    /// demand, where Ionic has to wait for the component to load. Null before then, because the template comes from a
    /// theme that it finds through the tree, so not in a constructor, or if a replaced template has no
    /// <c>PART_Scroll</c>.
    /// </summary>
    public ScrollViewer? GetScrollElement()
    {
        ApplyTemplate();
        return _scroll;
    }

    /// <summary>Ionic <c>scrollToTop</c>.</summary>
    /// <param name="duration">In milliseconds.</param>
    /// <returns>A task that's finished when the scroll is, and already finished for a jump.</returns>
    public Task ScrollToTop(double duration = 0) => ScrollToPoint(null, 0, duration);

    /// <summary>Ionic <c>scrollToBottom</c>.</summary>
    /// <param name="duration">In milliseconds.</param>
    /// <returns>A task that's finished when the scroll is, and already finished for a jump.</returns>
    public async Task ScrollToBottom(double duration = 0)
    {
        if (await WaitForScrollElement() is not { } element)
        {
            return;
        }

        // Reading scrollHeight makes a browser lay out, so content added just now counts.
        element.UpdateLayout();
        await ScrollToPoint(null, element.Extent.Height - element.Viewport.Height, duration);
    }

    /// <summary>Ionic <c>scrollByPoint</c>.</summary>
    /// <param name="x">The distance on the horizontal axis.</param>
    /// <param name="y">The distance on the vertical axis.</param>
    /// <param name="duration">In milliseconds.</param>
    /// <returns>A task that's finished when the scroll is, and already finished for a jump.</returns>
    public async Task ScrollByPoint(double x, double y, double duration)
    {
        if (await WaitForScrollElement() is { } element)
        {
            // Reading scrollTop makes a browser lay out, so content that changed just now counts.
            element.UpdateLayout();
            await ScrollToPoint(x + element.Offset.X, y + element.Offset.Y, duration);
        }
    }

    /// <summary>
    /// Ionic <c>scrollToPoint</c>. A null coordinate stays where it is. Below 32 ms it jumps at once. Otherwise it
    /// waits a frame, then eases out, even with <see cref="AvonicConfig.Animated"/> off, as Ionic's ignores
    /// <c>animated</c>. As in Ionic, nothing cancels it: a second call or the user's finger doesn't stop it, and the
    /// last write of each frame wins.
    /// </summary>
    /// <param name="x">The point on the horizontal axis.</param>
    /// <param name="y">The point on the vertical axis.</param>
    /// <param name="duration">In milliseconds.</param>
    /// <returns>A task that's finished when the scroll is, and already finished for a jump.</returns>
    public async Task ScrollToPoint(double? x, double? y, double duration = 0)
    {
        if (await WaitForScrollElement() is not { } element)
        {
            return;
        }

        // Setting scrollTop makes a browser lay out, and Avalonia limits an offset by the last layout.
        element.UpdateLayout();

        if (duration < MinAnimatedDuration)
        {
            var offset = element.Offset;
            element.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(x ?? offset.X, y ?? offset.Y));
            return;
        }

        await Animate(element, x, y, duration);
    }

    private static Task Animate(ScrollViewer element, double? x, double? y, double duration)
    {
        var done = new TaskCompletionSource();
        var fromX = element.Offset.X;
        var fromY = element.Offset.Y;
        var deltaX = x is { } toX ? toX - fromX : 0;
        var deltaY = y is { } toY ? toY - fromY : 0;
        var startTime = 0d;

        void Step()
        {
            var linearTime = Math.Min(1, (AvonicConfig.Now().TotalMilliseconds - startTime) / duration) - 1;
            var easedT = Math.Pow(linearTime, 3) + 1;
            var offset = element.Offset;
            element.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(
                deltaX != 0 ? Math.Floor(easedT * deltaX + fromX) : offset.X,
                deltaY != 0 ? Math.Floor(easedT * deltaY + fromY) : offset.Y));

            if (easedT < 1)
            {
                AvonicConfig.RequestFrame(element, Step);
            }
            else
            {
                done.SetResult();
            }
        }

        // "chill out for a frame first"
        AvonicConfig.RequestFrame(element, () =>
        {
            startTime = AvonicConfig.Now().TotalMilliseconds;
            Step();
        });

        return done.Task;
    }

    /// <summary>
    /// Waits for the first layout, so that a scroll requested before it isn't lost to an empty extent. A control that was
    /// removed before it came has nothing to wait for, until it's back.
    /// </summary>
    private async Task<ScrollViewer?> WaitForScrollElement()
    {
        if (!_hasLoaded && (!_wasDetached || VisualRoot is not null))
        {
            _loaded ??= new TaskCompletionSource();
            await _loaded.Task;
        }

        return GetScrollElement();
    }

    /// <inheritdoc/>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _hasLoaded = true;
        _loaded?.TrySetResult();
        _loaded = null;
    }

    /// <summary>
    /// <c>width: 100%; height: 100%; contain: size</c>: it fills what it's given and never sizes to its content, so
    /// with no height to fill, in a <see cref="StackPanel"/> or an <c>Auto</c> row, it's zero high, as in Ionic.
    /// The children still get what's really available, because a parent can arrange it bigger than it measured, as a
    /// <see cref="ScrollViewer"/> does, and a child measured at zero stays at zero.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        base.MeasureOverride(availableSize);
        return new Size(
            double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_scroll is not null)
        {
            _scroll.PropertyChanged -= OnScrollPropertyChanged;
            _scroll.TemplateApplied -= OnScrollTemplateApplied;
            _scroll.RemoveHandler(PointerWheelChangedEvent, OnPassedOver);
            _scroll.RemoveHandler(KeyDownEvent, OnPassedOver);
        }

        if (_container is not null)
        {
            _container.PropertyChanged -= OnContainerPropertyChanged;
            _container.RemoveHandler(PointerWheelChangedEvent, OnContainerInput);
            _container.RemoveHandler(KeyDownEvent, OnContainerInput);
        }

        if (_fixed is not null)
        {
            _fixed.PropertyChanged -= OnFixedPropertyChanged;
        }

        _background = e.NameScope.Find<Control>("PART_Background");
        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        _container = e.NameScope.Find<ContentPresenter>("PART_Container");
        _fixed = e.NameScope.Find<ContentPresenter>("PART_Fixed");

        if (_scroll is not null)
        {
            _scroll.PropertyChanged += OnScrollPropertyChanged;
            _scroll.TemplateApplied += OnScrollTemplateApplied;
            _scroll.AddHandler(PointerWheelChangedEvent, OnPassedOver, RoutingStrategies.Bubble, handledEventsToo: true);
            _scroll.AddHandler(KeyDownEvent, OnPassedOver, RoutingStrategies.Bubble, handledEventsToo: true);
            // overscroll-behavior: contain
            _scroll.SetCurrentValue(ScrollViewer.IsScrollChainingEnabledProperty, false);
        }

        if (_container is not null)
        {
            _container.PropertyChanged += OnContainerPropertyChanged;
            _container.AddHandler(PointerWheelChangedEvent, OnContainerInput);
            _container.AddHandler(KeyDownEvent, OnContainerInput);
        }

        if (_fixed is not null)
        {
            _fixed.PropertyChanged += OnFixedPropertyChanged;
        }

        UpdateInsets();
        UpdateFixedPlacement();
        UpdateContainerChild();
        UpdateFixedChild();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ScrollXProperty || change.Property == ScrollYProperty)
        {
            UpdateAxes();
            UpdateOverscroll();
        }
        else if (change.Property == ForceOverscrollProperty)
        {
            UpdateOverscroll();
        }
        else if (change.Property == PaddingProperty || change.Property == KeyboardOffsetProperty)
        {
            UpdateInsets();
        }
        else if (change.Property == FixedSlotPlacementProperty)
        {
            UpdateFixedPlacement();
        }
        else if (change.Property == FullscreenProperty)
        {
            OnFullscreenChanged();
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // disconnectedCallback
        _frame?.Dispose();
        _frame = null;
        _queued = false;
        OnScrollEnd();

        _wasDetached = true;

        // Removed before its first layout: a scroll that was waiting for one has nothing left to wait for. This runs
        // after Loaded, so if it was a move and it's back by now, Loaded has already released it, with a real layout.
        if (_loaded is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _loaded?.TrySetResult();
                _loaded = null;
            }, DispatcherPriority.Background);
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new ContentAutomationPeer(this);

    private void OnScrollPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty)
        {
            // A browser's disconnected element doesn't scroll, and an animation can outlive its control.
            if (ScrollEvents && VisualRoot is not null)
            {
                OnScroll();
            }
        }
        else if (e.Property == ScrollViewer.ViewportProperty)
        {
            UpdateOverscroll();
        }
    }

    private void OnScrollTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        _recognizer = e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter")
            ?.GestureRecognizers.OfType<ScrollGestureRecognizer>().FirstOrDefault();
        UpdateAxes();
    }

    /// <summary>
    /// With no axis on, the wheel and the page keys belong to whatever's outside, as with <c>overflow: hidden</c>. Avalonia's
    /// presenter and viewer don't ask whether an axis is on, only whether there's somewhere to go, so input is marked handled
    /// to get past them, and <see cref="OnPassedOver"/> takes that back on the other side.
    /// </summary>
    private void OnContainerInput(object? sender, RoutedEventArgs e)
    {
        if (!ScrollX && !ScrollY)
        {
            _passedOver = e;
            e.Handled = true;
        }
    }

    private void OnPassedOver(object? sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(_passedOver, e))
        {
            _passedOver = null;
            e.Handled = false;
        }
    }

    private void OnContainerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ChildProperty)
        {
            UpdateContainerChild();
        }
    }

    private void OnFixedPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ContentPresenter.ChildProperty)
        {
            UpdateFixedChild();
        }
    }

    /// <summary>
    /// <c>.scroll-x</c> and <c>.scroll-y</c>. On is <see cref="ScrollBarVisibility.Hidden"/>, because a phone's scroll
    /// bars aren't Ionic's: the template has none. Horizontal off is <see cref="ScrollBarVisibility.Disabled"/>, so text
    /// wraps to the width, where hidden measures it as if the width had no end. Vertical off is still hidden, with no
    /// gesture, because <c>overflow: hidden</c> is a scroller to a script and keeps its position, where disabled would
    /// reset it. That takes blocking input by hand, and a drag at an angle can't be blocked with horizontal on, so
    /// that one combination is disabled.
    /// </summary>
    private void UpdateAxes()
    {
        if (_scroll is null)
        {
            return;
        }

        _scroll.SetCurrentValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollX ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled);
        _scroll.SetCurrentValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollY || !ScrollX ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled);
        _recognizer?.SetCurrentValue(ScrollGestureRecognizer.CanVerticallyScrollProperty, ScrollY);
    }

    /// <summary><c>.inner-scroll</c>'s and <c>#background-content</c>'s <c>top</c>, <c>bottom</c> and <c>padding</c>.</summary>
    private void UpdateInsets()
    {
        var outward = new Thickness(0, -_offsetTop, 0, -_offsetBottom);
        _background?.SetCurrentValue(MarginProperty, outward);
        _scroll?.SetCurrentValue(MarginProperty, outward);

        var padding = Padding;
        _container?.SetCurrentValue(
            ContentPresenter.PaddingProperty,
            new Thickness(padding.Left, padding.Top + _offsetTop, padding.Right, padding.Bottom + KeyboardOffset + _offsetBottom));
    }

    /// <summary>
    /// <c>.overscroll::before</c>, a pixel below the padding box: content that fits can scroll by one pixel, and longer
    /// content is unchanged.
    /// </summary>
    private void UpdateOverscroll()
    {
        if (_scroll is not null)
        {
            _container?.SetCurrentValue(
                MinHeightProperty,
                ForceOverscroll == true && ScrollY ? _scroll.Viewport.Height + 1 : 0d);
        }
    }

    /// <summary>The slot's position in the DOM decides its paint order and its place in the tab order, as here.</summary>
    private void UpdateFixedPlacement()
    {
        if (_fixed?.GetVisualParent() is not Panel panel || _scroll is null)
        {
            return;
        }

        var fixedIndex = panel.Children.IndexOf(_fixed);
        var scrollIndex = panel.Children.IndexOf(_scroll);
        if (scrollIndex < 0)
        {
            return;
        }

        var before = FixedSlotPlacement == ContentFixedSlotPlacement.Before;
        if (before == fixedIndex > scrollIndex)
        {
            panel.Children.Move(fixedIndex, scrollIndex);
        }
    }

    /// <summary>
    /// A block is as high as its content, where Avalonia would stretch it to the viewport. An app's own alignment wins, its style
    /// too: a presenter sets its child before it adds it to the logical tree, which styles it, so the frame here comes first.
    /// </summary>
    private void UpdateContainerChild()
    {
        Reset(_containerChildDefaults);
        if (_container?.Child is { } child)
        {
            Add(_containerChildDefaults, child.SetValue(VerticalAlignmentProperty, VerticalAlignment.Top, BindingPriority.Style));
        }
    }

    /// <summary>
    /// <c>position: absolute</c> with no inset: it's as big as its content, at the host's corner. An app's own alignment wins, its
    /// style too, as for <see cref="UpdateContainerChild"/>. A slot holds one child here, so a panel that overlaps its children (a
    /// plain <see cref="Panel"/>, a <see cref="Grid"/> or a <see cref="Canvas"/>) stands for the slot, as Ionic takes any number of
    /// <c>slot="fixed"</c> elements: it's left to fill the host, and its children, such as several <see cref="AvonicFab"/>s, place
    /// themselves in it. A panel that flows its children, a <see cref="StackPanel"/> say, is a <c>div</c> around them, so it's one
    /// block at the corner.
    /// </summary>
    private void UpdateFixedChild()
    {
        Reset(_fixedChildDefaults);
        if (_fixed?.Child is { } child && !StandsForTheSlot(child))
        {
            Add(_fixedChildDefaults, child.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Left, BindingPriority.Style));
            Add(_fixedChildDefaults, child.SetValue(VerticalAlignmentProperty, VerticalAlignment.Top, BindingPriority.Style));
        }
    }

    private static bool StandsForTheSlot(Control child) => child.GetType() == typeof(Panel) || child is Grid or Canvas;

    private static void Reset(List<IDisposable> frames)
    {
        foreach (var frame in frames)
        {
            frame.Dispose();
        }

        frames.Clear();
    }

    private static void Add(List<IDisposable> frames, IDisposable? frame)
    {
        if (frame is not null)
        {
            frames.Add(frame);
        }
    }

    private void OnFullscreenChanged()
    {
        if (Fullscreen)
        {
            // Ionic only looks again on a render or a window resize, 100 ms later. A page's layout is what can
            // change the answer, so this follows it, at once.
            LayoutUpdated += OnLayoutUpdated;
            ReadDimensions();
        }
        else
        {
            LayoutUpdated -= OnLayoutUpdated;
            SetOffsets(0, 0);
        }
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => ReadDimensions();

    /// <summary>
    /// <c>readDimensions</c>: <c>offsetTop</c>, and the page's height less that and the host's. <c>offsetTop</c> counts from the
    /// offset parent: the closest <c>.ion-page</c> above the host, or in tabs their <c>.tabs-inner</c> (<c>position: relative</c>),
    /// whichever is nearer, and the page itself otherwise. So in tabs the offsets reach the tabs' bottom (a bottom tab bar and a
    /// footer), and with a top tab bar the top is 0 and the bottom runs past the tabs by the bar, as measured in Chromium.
    /// </summary>
    private void ReadDimensions()
    {
        if (FindPage() is not { } page)
        {
            return;
        }

        var tabsInner = (page as AvonicTabs)?.Inner;
        var top = 0d;
        for (Visual? visual = this; visual is not null && visual != page; visual = visual.GetVisualParent())
        {
            if (visual != this && (IsIonPage(visual) || visual == tabsInner))
            {
                break;
            }

            top += visual.Bounds.Y;

            // offsetTop doesn't move with an ancestor's scroll.
            if (visual.GetVisualParent() is ScrollContentPresenter scroller)
            {
                top += scroller.Offset.Y;
            }
        }

        top = Math.Max(top, 0);
        SetOffsets(top, Math.Max(page.Bounds.Height - top - Bounds.Height, 0));
    }

    /// <summary>
    /// <c>getPageElement</c>: the closest <see cref="AvonicTabs"/>, whatever is between, else the closest <c>.ion-page</c>, or else
    /// the parent. Avonic has no <c>ion-app</c> or popover yet.
    /// </summary>
    private Visual? FindPage() =>
        this.FindAncestorOfType<AvonicTabs>()
        ?? this.GetVisualAncestors().FirstOrDefault(IsIonPage)
        ?? this.GetVisualParent();

    private static bool IsIonPage(Visual visual) => visual.Classes.Contains("ion-page");

    private void SetOffsets(double top, double bottom)
    {
        if (top != _offsetTop || bottom != _offsetBottom)
        {
            _offsetTop = top;
            _offsetBottom = bottom;
            UpdateInsets();
        }
    }

    private void OnScroll()
    {
        var shouldStart = !_isScrolling;
        _lastScroll = AvonicConfig.Now();
        if (shouldStart)
        {
            OnScrollStart();
        }

        if (!_queued)
        {
            _queued = true;
            _frame = AvonicConfig.RequestFrame(this, () =>
            {
                _queued = false;
                _frame = null;
                RaiseScroll(shouldStart);
            });
        }
    }

    private void OnScrollStart()
    {
        _isScrolling = true;
        RaiseEvent(new ScrollBaseDetailEventArgs(IonScrollStartEvent, true));
        _watchDog?.Dispose();
        _watchDog = AvonicConfig.Schedule(WatchDog, WatchDogInterval);
    }

    private void WatchDog()
    {
        if (_lastScroll < AvonicConfig.Now() - WatchDogIdle)
        {
            OnScrollEnd();
        }
        else
        {
            _watchDog = AvonicConfig.Schedule(WatchDog, WatchDogInterval);
        }
    }

    private void OnScrollEnd()
    {
        _watchDog?.Dispose();
        _watchDog = null;
        if (_isScrolling)
        {
            _isScrolling = false;
            RaiseEvent(new ScrollBaseDetailEventArgs(IonScrollEndEvent, false));
        }
    }

    /// <summary><c>updateScrollDetail</c>. The state lasts between scrolls, as Ionic's one detail object does.</summary>
    private void RaiseScroll(bool shouldStart)
    {
        if (_scroll is not { } scroll)
        {
            return;
        }

        var timestamp = AvonicConfig.Now().TotalMilliseconds;
        var previous = _detail;
        var current = previous;
        var currentX = scroll.Offset.X;
        var currentY = scroll.Offset.Y;
        var timeDelta = timestamp - previous.CurrentTime;

        if (shouldStart)
        {
            current.StartTime = timestamp;
            current.StartX = currentX;
            current.StartY = currentY;
            current.VelocityX = current.VelocityY = 0;
        }

        current.CurrentTime = timestamp;
        current.CurrentX = currentX;
        current.CurrentY = currentY;

        if (timeDelta is > 0 and < 100)
        {
            current.VelocityX = (currentX - previous.CurrentX) / timeDelta * 0.7 + current.VelocityX * 0.3;
            current.VelocityY = (currentY - previous.CurrentY) / timeDelta * 0.7 + current.VelocityY * 0.3;
        }

        _detail = current;
        RaiseEvent(new ScrollDetailEventArgs(IonScrollEvent)
        {
            ScrollTop = currentY,
            ScrollLeft = currentX,
            StartX = current.StartX,
            StartY = current.StartY,
            StartTime = current.StartTime,
            CurrentX = currentX,
            CurrentY = currentY,
            VelocityX = current.VelocityX,
            VelocityY = current.VelocityY,
            DeltaX = currentX - current.StartX,
            DeltaY = currentY - current.StartY,
            CurrentTime = timestamp,
        });
    }

    private sealed class ContentAutomationPeer(AvonicContent owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }

    private struct ScrollState
    {
        public double StartX, StartY, StartTime, CurrentX, CurrentY, VelocityX, VelocityY, CurrentTime;
    }


}
