using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Gestures;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-menu</c>: a side menu that comes in over the main content, or pushes it, or is revealed under it, opened by
/// <see cref="Open"/>, an <see cref="AvonicMenuButton"/>, an <see cref="AvonicMenuToggle"/>, or a swipe from the window's edge. It
/// fills its parent, as Ionic's <c>position: absolute; inset: 0</c>, so put it in a panel over the main content, before it, and name
/// that content in <see cref="ContentId"/>. <see cref="MenuController"/> keeps one menu open at a time. The look is in the theme. See
/// <c>docs/components/ion-menu/README.md</c>.
/// </summary>
[TemplatePart("PART_Container", typeof(Border))]
[TemplatePart("PART_Backdrop", typeof(AvonicBackdrop))]
[TemplatePart("PART_ContentShadow", typeof(Border))]
[TemplatePart("PART_Panel", typeof(MenuPanel))]
[PseudoClasses(TypeOverlay, TypePush, TypeReveal, SideStart, SideEnd, ShowMenu, SmallWidth, MenuPaneVisible, SplitPaneSide)]
public partial class AvonicMenu : ContentControl
{
    /// <summary><c>menu-type-overlay</c>.</summary>
    public const string TypeOverlay = ":type-overlay";

    /// <summary><c>menu-type-push</c>.</summary>
    public const string TypePush = ":type-push";

    /// <summary><c>menu-type-reveal</c>.</summary>
    public const string TypeReveal = ":type-reveal";

    /// <summary><c>menu-side-start</c>.</summary>
    public const string SideStart = ":side-start";

    /// <summary><c>menu-side-end</c>.</summary>
    public const string SideEnd = ":side-end";

    /// <summary><c>show-menu</c>: from the start of its opening to the end of its closing.</summary>
    public const string ShowMenu = ":show-menu";

    /// <summary><c>@media (max-width: 340px)</c>, on the top level's width: <c>.menu-inner</c>'s <c>--width</c> is md's small one.</summary>
    public const string SmallWidth = ":small-width";

    /// <summary><c>menu-pane-visible</c>: its <see cref="AvonicSplitPane"/> shows it beside the main content.</summary>
    public const string MenuPaneVisible = ":menu-pane-visible";

    /// <summary><c>split-pane-side</c>: it's the side of an <see cref="AvonicSplitPane"/>, not a menu inside its main content.</summary>
    public const string SplitPaneSide = ":split-pane-side";

    /// <summary>The widest top level <see cref="SmallWidth"/> is on for: the media query's 340 px.</summary>
    internal const double SmallMaxWidth = 340;

    /// <summary>The role <c>BACKDROP</c>: a click outside the menu, or Escape.</summary>
    public const string BackdropRole = "backdrop";

    /// <summary>The role <c>GESTURE</c>: a swipe, or a menu that was disabled.</summary>
    public const string GestureRole = "gesture";

    /// <summary>md's easing to open: <c>cubic-bezier(0.0, 0.0, 0.2, 1)</c>.</summary>
    internal const string MdEasing = "cubic-bezier(0.0,0.0,0.2,1)";

    /// <summary>md's easing to close, the animation playing in reverse: <c>cubic-bezier(0.4, 0, 0.6, 1)</c>.</summary>
    internal const string MdEasingReverse = "cubic-bezier(0.4, 0, 0.6, 1)";

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic <c>contentId</c>: the <c>Name</c> of the main content, the view the menu goes over, which moves for push and reveal and
    /// takes no input while the menu is open. Looked up in the menu's name scope when it's attached. Without one found the menu
    /// isn't registered with <see cref="MenuController"/> and has no swipe gesture, as Ionic's.
    /// </summary>
    public static readonly StyledProperty<string?> ContentIdProperty =
        AvaloniaProperty.Register<AvonicMenu, string?>(nameof(ContentId));

    /// <summary>Ionic <c>menuId</c>: what <see cref="MenuController"/>, a menu button and a toggle find it by.</summary>
    public static readonly StyledProperty<string?> MenuIdProperty =
        AvaloniaProperty.Register<AvonicMenu, string?>(nameof(MenuId));

    /// <summary>
    /// Ionic <c>type</c>. Null, Ionic's <c>undefined</c>, takes <see cref="AvonicConfig.MenuType"/>, or md's
    /// <see cref="MenuType.Overlay"/>, when it's first attached.
    /// </summary>
    public static readonly StyledProperty<MenuType?> TypeProperty =
        AvaloniaProperty.Register<AvonicMenu, MenuType?>(nameof(Type));

    /// <summary>Ionic <c>side</c>: <see cref="MenuSide.Start"/> by default.</summary>
    public static readonly StyledProperty<MenuSide> SideProperty =
        AvaloniaProperty.Register<AvonicMenu, MenuSide>(nameof(Side));

    /// <summary>Ionic <c>swipeGesture</c>: whether a swipe opens and closes it. Not <c>SwipeGesture</c>, which <see cref="InputElement"/> has.</summary>
    public static readonly StyledProperty<bool> MenuSwipeGestureProperty =
        AvaloniaProperty.Register<AvonicMenu, bool>(nameof(MenuSwipeGesture), defaultValue: true);

    /// <summary>Ionic <c>maxEdgeStart</c>: how far from the window's edge, in pixels, a swipe can start to open it. 50.</summary>
    public static readonly StyledProperty<double> MaxEdgeStartProperty =
        AvaloniaProperty.Register<AvonicMenu, double>(nameof(MaxEdgeStart), defaultValue: 50);

    // ── Events ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>ionWillOpen</c>: it's about to open.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonWillOpenEvent =
        RoutedEvent.Register<AvonicMenu, RoutedEventArgs>(nameof(IonWillOpen), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionDidOpen</c>: it's open.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonDidOpenEvent =
        RoutedEvent.Register<AvonicMenu, RoutedEventArgs>(nameof(IonDidOpen), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionWillClose</c>: it's about to close, with the role of what closed it.</summary>
    public static readonly RoutedEvent<MenuCloseEventArgs> IonWillCloseEvent =
        RoutedEvent.Register<AvonicMenu, MenuCloseEventArgs>(nameof(IonWillClose), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionDidClose</c>: it's closed, with the role of what closed it.</summary>
    public static readonly RoutedEvent<MenuCloseEventArgs> IonDidCloseEvent =
        RoutedEvent.Register<AvonicMenu, MenuCloseEventArgs>(nameof(IonDidClose), RoutingStrategies.Bubble);

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private readonly ConfigOnLoad<MenuType?> _typeConfig;
    private readonly StyleValue<bool> _shown = new();
    private readonly StyleValue<bool> _focusable = new();
    private readonly StyleValue<int> _zIndex = new();

    private Border? _container;
    private AvonicBackdrop? _backdrop;
    private Border? _contentShadow;
    private MenuPanel? _panel;
    private Panel? _root;

    // Made again after each detach, which destroys it, as Ionic's disconnectedCallback does: an element here can come back.
    private GestureBlocker _blocker = GestureController.Instance.CreateBlocker(disableScroll: true);
    private Control? _contentEl;
    private TopLevel? _topLevel;
    private Gesture? _gesture;
    private AvonicAnimation? _animation;

    // Ionic's typeChanged and sideChanged drop the animation; one that's playing or holds the open menu in place is kept until the
    // next one is made, at the next open or close, so that its play ends and its values hold until then.
    private bool _animationStale;
    private Rect _contentShadowRect;
    private Control? _watchedContent;
    private bool _didLoad;
    private bool _connected;
    private bool _operationCancelled;
    private double? _width;
    private bool _isEndSide;
    private int _contentSign = 1;
    private double _lastOnEnd;
    private double _lastRelease;
    private IInputElement? _lastFocus;
    private IDisposable? _contentNoHit;
    private IDisposable? _contentOffscreen;
    private bool _trapping;

    // Its split pane, from getSplitPane() at each attach, and Ionic's isPaneVisible.
    private AvonicSplitPane? _splitPane;
    private bool _isPaneVisible;

    static AvonicMenu()
    {
        // role="navigation"
        AutomationProperties.LandmarkTypeProperty.OverrideDefaultValue<AvonicMenu>(AutomationLandmarkType.Navigation);
    }

    public AvonicMenu()
    {
        // display: none until show-menu; tabindex="0" while it's shown; z-index by type. At style priority, through bindings made
        // here, before any style, so that an app's style wins.
        Bind(IsVisibleProperty, _shown, BindingPriority.Style);
        Bind(FocusableProperty, _focusable, BindingPriority.Style);
        Bind(ZIndexProperty, _zIndex, BindingPriority.Style);
        _shown.Set(false);
        _focusable.Set(false);
        _typeConfig = ConfigOnLoad<MenuType?>.IfUnset(this, TypeProperty);
        UpdateTypeClasses();
        UpdateSideClasses();

        // The click listener on the host, Ionic's backdrop click: the DOM click, on release, with no keys.
        _ = new NativeButton(this, OnHostClick, static () => false, static () => { });
        AddHandler(PointerReleasedEvent, (_, e) => _lastRelease = AvonicConfig.EventTime(e), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyDownEvent, OnHostKeyDown);
    }

    public string? ContentId
    {
        get => GetValue(ContentIdProperty);
        set => SetValue(ContentIdProperty, value);
    }

    public string? MenuId
    {
        get => GetValue(MenuIdProperty);
        set => SetValue(MenuIdProperty, value);
    }

    public MenuType? Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public MenuSide Side
    {
        get => GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    public bool MenuSwipeGesture
    {
        get => GetValue(MenuSwipeGestureProperty);
        set => SetValue(MenuSwipeGestureProperty, value);
    }

    public double MaxEdgeStart
    {
        get => GetValue(MaxEdgeStartProperty);
        set => SetValue(MaxEdgeStartProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? IonWillOpen
    {
        add => AddHandler(IonWillOpenEvent, value);
        remove => RemoveHandler(IonWillOpenEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonDidOpen
    {
        add => AddHandler(IonDidOpenEvent, value);
        remove => RemoveHandler(IonDidOpenEvent, value);
    }

    public event EventHandler<MenuCloseEventArgs>? IonWillClose
    {
        add => AddHandler(IonWillCloseEvent, value);
        remove => RemoveHandler(IonWillCloseEvent, value);
    }

    public event EventHandler<MenuCloseEventArgs>? IonDidClose
    {
        add => AddHandler(IonDidCloseEvent, value);
        remove => RemoveHandler(IonDidCloseEvent, value);
    }

    /// <summary>Ionic's <c>_isOpen</c>: open, as of the end of the last animation.</summary>
    internal bool IsOpenState { get; private set; }

    /// <summary>Ionic's <c>isAnimating</c>: from the start of an opening or closing, or of a swipe, to its end.</summary>
    internal bool IsMenuAnimating { get; private set; }

    /// <summary>The top level it's registered with the controller in, while it is.</summary>
    internal TopLevel? RegisteredIn { get; private set; }

    /// <summary>The main content <see cref="ContentId"/> found, while it's attached.</summary>
    internal Control? MainContent => _contentEl;

    /// <summary>
    /// Shown beside the main content: <c>.menu-pane-visible.split-pane-side.menu-enabled</c>, which is <c>display: flex</c>. What
    /// <see cref="SplitPanePanel"/> lays out as a side.
    /// </summary>
    internal bool IsPaneShown => _isPaneVisible && _splitPane is not null && IsEnabled;

    /// <summary>The animation it plays, Ionic's <c>this.animation</c>, for tests.</summary>
    internal AvonicAnimation? CurrentAnimation => _animation;

    // ── Methods ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>isOpen()</c>.</summary>
    public bool IsOpen() => IsOpenState;

    /// <summary>
    /// Ionic <c>isActive()</c>: whether it can open or close, which is whether it's enabled and not shown in an
    /// <see cref="AvonicSplitPane"/>.
    /// </summary>
    public bool IsActive() => IsActiveCore;

    /// <summary>Ionic <c>open(animated)</c>: false if it's open already, or can't open.</summary>
    public Task<bool> Open(bool animated = true) => SetOpen(true, animated);

    /// <summary>
    /// Ionic <c>close(animated, role)</c>: false if it's closed already, or can't close. <paramref name="role"/> is what
    /// <see cref="IonWillClose"/> and <see cref="IonDidClose"/> carry.
    /// </summary>
    public Task<bool> Close(bool animated = true, string? role = null) => SetOpen(false, animated, role);

    /// <summary>Ionic <c>toggle(animated)</c>: closes it if it's open, opens it if not.</summary>
    public Task<bool> Toggle(bool animated = true) => SetOpen(!IsOpenState, animated);

    /// <summary>
    /// Ionic <c>setOpen(shouldOpen, animated, role)</c>. The focus is cleared first, so that nothing stays focused in what's about to
    /// be hidden. False if it couldn't: another menu is animating, this one is, it isn't active, it's already that way, or it was
    /// disabled before it finished.
    /// </summary>
    public Task<bool> SetOpen(bool shouldOpen, bool animated = true, string? role = null)
    {
        if (TopLevel.GetTopLevel(this) is { } top)
        {
            AvonicOverlay.Blur(top);
        }

        return MenuController.SetOpen(this, shouldOpen, animated, role);
    }

    /// <summary>Ionic's <c>_isActive()</c>: <c>!disabled &amp;&amp; !isPaneVisible</c>.</summary>
    private bool IsActiveCore => IsEnabled && !_isPaneVisible;

    /// <summary>Ionic's own <c>_setOpen</c>, which the controller calls.</summary>
    internal async Task<bool> SetOpenCore(bool shouldOpen, bool animated, string? role)
    {
        // If the menu is disabled or it is currently being animated, let's do nothing
        if (!IsActiveCore || IsMenuAnimating || shouldOpen == IsOpenState)
        {
            return false;
        }

        BeforeAnimation(shouldOpen, role);
        try
        {
            LoadAnimation();
        }
        catch
        {
            // No animation for its type (Ionic's "animation not registered"), or a builder that threw: the task faults, as Ionic's
            // promise rejects, but the menu isn't left animating, which would refuse every menu from then on.
            UndoBeforeAnimation(role);
            throw;
        }

        await StartAnimation(shouldOpen, animated);

        // An operation the menu was disabled during didn't succeed.
        if (_operationCancelled)
        {
            _operationCancelled = false;
            return false;
        }

        AfterAnimation(shouldOpen, role);
        return true;
    }

    // ── Template ─────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _container = e.NameScope.Find<Border>("PART_Container");
        _backdrop = e.NameScope.Find<AvonicBackdrop>("PART_Backdrop");
        _contentShadow = e.NameScope.Find<Border>("PART_ContentShadow");
        _panel = e.NameScope.Find<MenuPanel>("PART_Panel");
        _root = _panel?.GetVisualParent() as Panel;
        DropAnimation();
        UpdateSideParts();
        ShowContentShadowPart();
    }

    /// <summary>
    /// The main content's box, without its transform, in <paramref name="root"/>'s left to right coordinates, where
    /// <see cref="MenuRootPanel"/> puts the part that draws its shadow; empty without one.
    /// </summary>
    internal Rect ContentShadowRect(Visual root)
    {
        _contentShadowRect = _contentEl?.GetVisualParent() is Visual parent && parent.TransformToVisual(root) is { } toRoot
            ? _contentEl.Bounds.TransformToAABB(toRoot)
            : default;
        return _contentShadowRect;
    }

    /// <summary>The part that draws the content's shadow is shown while there's a content to draw it for.</summary>
    private void ShowContentShadowPart()
    {
        if (_contentShadow is not null)
        {
            _contentShadow.IsVisible = _contentEl is not null;
        }
    }

    /// <summary>
    /// While the menu is shown, the content can move or change size where the menu isn't arranged again (a margin, a row, the
    /// keyboard): the shadow follows it at the next pass.
    /// </summary>
    private void OnContentLayoutUpdated(object? sender, EventArgs e)
    {
        var before = _contentShadowRect;
        if (_root is not null && ContentShadowRect(_root) != before)
        {
            _root.InvalidateArrange();
        }
    }

    private void WatchContentLayout(bool watch)
    {
        if (_watchedContent is { } watched)
        {
            watched.LayoutUpdated -= OnContentLayoutUpdated;
            _watchedContent = null;
        }

        if (watch && _contentEl is { } content)
        {
            content.LayoutUpdated += OnContentLayoutUpdated;
            _watchedContent = content;
        }
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
        {
            _topLevel.PropertyChanged += OnTopLevelPropertyChanged;
        }

        UpdateSmallWidth();
        AttachToSplitPane();
        Connect();
        if (!_didLoad)
        {
            // componentDidLoad
            _didLoad = true;
            MenuChanged();
            UpdateState();
        }
    }

    /// <summary>Ionic's <c>connectedCallback</c>.</summary>
    private void Connect()
    {
        _typeConfig.Load(() => AvonicConfig.MenuType ?? MenuType.Overlay);
        _connected = true;
        // The nearest name scope, as Ionic's getElementById looks in the document.
        var content = ContentId is { } id ? this.FindNameScope()?.Find<Control>(id) : null;
        if (content is null || _topLevel is null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this,
                "[AvonicMenu] - Must have a \"content\" element to listen for drag events on: no control named \"{ContentId}\" in the menu's name scope.",
                ContentId);
            return;
        }

        if (this.IsVisualAncestorOf(content) || this.IsLogicalAncestorOf(content))
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this,
                "[AvonicMenu] - The ContentId should refer to the main view's content, not the content inside of the menu.");
        }

        _contentEl = content;
        ShowContentShadowPart();
        TypeChanged();
        SideChanged();
        MenuController.Register(this, _topLevel);
        RegisteredIn = _topLevel;
        MenuChanged();
        _gesture = Gesture.Create(new GestureConfig
        {
            Element = _topLevel,
            GestureName = "menu-swipe",
            GesturePriority = 30,
            Threshold = 10,
            BlurOnStart = true,
            CanStart = CanStart,
            OnWillStart = OnWillStart,
            OnStart = OnStart,
            OnMove = OnMove,
            OnEnd = OnEnd,
        });
        UpdateState();
        InvalidateArrange();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_topLevel is not null)
        {
            _topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
        }

        // disconnectedCallback: closed at once, so the content's state goes. One that's animating, or that the controller won't
        // close while another animates, is closed by force: its play ends, and a call awaiting it returns false. In Ionic it would
        // stay animating, which here, where a page is detached and attached again far more often than an element is removed from a
        // document, would refuse every menu once it was back.
        if (IsOpenState && !IsMenuAnimating)
        {
            _ = Close(false);
        }

        if (IsOpenState || IsMenuAnimating)
        {
            _operationCancelled = IsMenuAnimating;
            AfterAnimation(false, null);
        }

        DetachFromSplitPane();
        _connected = false;
        _blocker.Destroy();
        _blocker = GestureController.Instance.CreateBlocker(disableScroll: true);
        MenuController.Unregister(this);
        RegisteredIn = null;
        _animation?.Destroy();
        _gesture?.Destroy();
        _gesture = null;
        _animation = null;
        _animationStale = false;
        WatchContentLayout(false);
        _contentEl = null;
        ShowContentShadowPart();
        StopTrap();
        _topLevel = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TypeProperty)
        {
            UpdateTypeClasses();
            if (_connected)
            {
                TypeChanged();
            }
        }
        else if (change.Property == SideProperty)
        {
            UpdateSideClasses();
            _splitPane?.SidesChanged();
            if (_connected)
            {
                SideChanged();
            }
        }
        else if (change.Property == IsEnabledProperty)
        {
            // disabledChanged, once it has loaded, as a Stencil watcher.
            // menu-enabled: a disabled side isn't shown in the pane.
            UpdatePaneState();
            if (_didLoad)
            {
                UpdateState();
                MenuController.RaiseMenuChanged(this);
            }
        }
        else if (change.Property == MenuSwipeGestureProperty)
        {
            if (_didLoad)
            {
                UpdateState();
            }
        }
        else if (change.Property == FlowDirectionProperty)
        {
            // isEndSide reads the direction: the swipe's edge follows it at once, as the parts do.
            if (_connected)
            {
                SideChanged();
            }
            else
            {
                UpdateSideParts();
            }
        }
    }

    /// <summary>Ionic's <c>typeChanged</c>: the next open or close makes the type's animation.</summary>
    private void TypeChanged() => DropAnimation();

    /// <summary>Ionic's <c>sideChanged</c>: which edge it's at, and a new animation.</summary>
    private void SideChanged()
    {
        _isEndSide = ComputeIsEndSide();
        DropAnimation();
        UpdateSideParts();
    }

    /// <summary>
    /// Ionic's <c>this.animation = undefined</c>. While the menu is shown, the animation is playing or holds the open menu's values,
    /// so it's kept, marked stale, until <see cref="LoadAnimation"/> makes the next one: destroyed mid-play, its play would never end,
    /// and the menu would stay animating, refusing every menu. Ionic's is dropped and left on its elements, with <c>fill: both</c>,
    /// even after the next close.
    /// </summary>
    private void DropAnimation()
    {
        if (IsOpenState || IsMenuAnimating)
        {
            _animationStale = _animation is not null;
            return;
        }

        _animation?.Destroy();
        _animation = null;
        _animationStale = false;
    }

    /// <summary>Ionic's <c>isEndSide(side, el)</c>: physical, as the start is the right in right-to-left.</summary>
    private bool ComputeIsEndSide() => (Side == MenuSide.End) != (FlowDirection == FlowDirection.RightToLeft);

    /// <summary>Ionic's <c>menuChanged</c>: <c>ionMenuChange</c>, once it has loaded.</summary>
    private void MenuChanged()
    {
        if (_didLoad)
        {
            MenuController.RaiseMenuChanged(this);
        }
    }

    /// <summary>
    /// Ionic's <c>updateState</c>: the gesture follows whether it's active and swipeable, and a menu that isn't active is closed at
    /// once, with <see cref="IonDidClose"/> and the <c>gesture</c> role, whether it was open or not.
    /// </summary>
    private void UpdateState()
    {
        var isActive = IsActiveCore;
        _gesture?.Enable(isActive && MenuSwipeGesture);
        if (!isActive)
        {
            var wasAnimating = IsMenuAnimating;
            if (wasAnimating)
            {
                _operationCancelled = true;
            }

            AfterAnimation(false, GestureRole);

            // A swipe's release left its finish callback on the animation, which a stop keeps for the next play: it's made again, so
            // that the next open doesn't run it.
            if (wasAnimating)
            {
                _animation?.Destroy();
                _animation = null;
                _animationStale = false;
            }
        }
    }

    private void UpdateTypeClasses()
    {
        var type = Type;
        PseudoClasses.Set(TypeOverlay, type == MenuType.Overlay);
        PseudoClasses.Set(TypePush, type == MenuType.Push);
        PseudoClasses.Set(TypeReveal, type == MenuType.Reveal);

        UpdateZIndex();
    }

    /// <summary>
    /// <c>:host(.menu-type-overlay) { z-index: 1000 }</c>, reveal's 0, push's auto; and <c>z-index: 0</c> beside the main content,
    /// <c>.menu-pane-visible.split-pane-side</c>.
    /// </summary>
    private void UpdateZIndex()
    {
        if (_isPaneVisible && _splitPane is not null)
        {
            _zIndex.Set(0);
        }
        else
        {
            _zIndex.SetWhen(Type == MenuType.Overlay, 1000);
        }
    }

    private void UpdateSideClasses()
    {
        PseudoClasses.Set(SideStart, Side == MenuSide.Start);
        PseudoClasses.Set(SideEnd, Side == MenuSide.End);
    }

    /// <summary>
    /// The parts follow the side, physically: the panel puts the container at the right edge for the end side, and the side away
    /// from the screen's edge has no safe area (<c>--ion-safe-area-right: 0px</c> at the start in left to right).
    /// </summary>
    private void UpdateSideParts()
    {
        var isEndSide = ComputeIsEndSide();
        if (_panel is not null)
        {
            _panel.IsEndSide = isEndSide;
        }

        // Only when the side moves: each change of the container's resources resolves every resource in the menu's content again.
        var zeroed = isEndSide ? "Avonic.SafeAreaLeft" : "Avonic.SafeAreaRight";
        if (_container is not null && !_container.Resources.ContainsKey(zeroed))
        {
            _container.Resources.Remove(isEndSide ? "Avonic.SafeAreaRight" : "Avonic.SafeAreaLeft");
            _container.Resources[zeroed] = 0d;
        }
    }

    private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty)
        {
            UpdateSmallWidth();
        }
    }

    private void UpdateSmallWidth() =>
        PseudoClasses.Set(SmallWidth, _topLevel is { } top && top.ClientSize.Width <= SmallMaxWidth);

    // ── Split pane ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>getSplitPane()</c> and <c>split-pane-side</c>, then <c>componentDidLoad</c>'s <c>isPaneVisible = await
    /// splitPane.isVisible()</c>, and the <c>ionSplitPaneVisible</c> listener on <c>body</c>, here the top level's. Ionic reads the
    /// state once, at its load; here it's read at every attach, as a menu here is detached and attached again with its page far more
    /// often, and hears no event while it's away.
    /// </summary>
    private void AttachToSplitPane()
    {
        _splitPane = FindSplitPane();
        PseudoClasses.Set(SplitPaneSide, _splitPane is not null);
        _isPaneVisible = _splitPane?.Visible ?? false;
        _splitPane?.AddSide(this);

        // Handled too: an app's Handled isn't stopPropagation() to most code that sets it, and a menu left in the wrong state is
        // worse than one that hears an event an app meant to stop. Only a side listens: Ionic's every menu does, and drops the
        // events of any split pane but its own, which a menu with none would drop all of.
        if (_splitPane is not null)
        {
            _topLevel?.AddHandler(AvonicSplitPane.IonSplitPaneVisibleEvent, OnSplitPaneVisible, RoutingStrategies.Bubble, handledEventsToo: true);
        }
        UpdatePaneState();
    }

    private void DetachFromSplitPane()
    {
        _topLevel?.RemoveHandler(AvonicSplitPane.IonSplitPaneVisibleEvent, OnSplitPaneVisible);
        _splitPane?.RemoveSide(this);
        _splitPane = null;
    }

    /// <summary>
    /// The nearest <see cref="AvonicSplitPane"/> it's in, unless the nearest main content (<see cref="AvonicSplitPane.SplitPaneMain"/>)
    /// comes first: a menu in the main content, such as one in a page, is an ordinary menu. Not only a direct child, as a menu can be
    /// wrapped in another control.
    /// </summary>
    private AvonicSplitPane? FindSplitPane()
    {
        for (Visual? visual = this; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is AvonicSplitPane pane)
            {
                return pane;
            }

            if (visual is StyledElement element && element.Classes.Contains(AvonicSplitPane.SplitPaneMain))
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Ionic's <c>onSplitPaneChanged</c>: from its own split pane only, the state, then <c>updateState</c>, which closes it at once
    /// once it's in the pane. <c>ion-menu-button</c> and <c>ion-menu-toggle</c> listen to the same event to check their menu again;
    /// Avonic's listen to the controller's <c>ionMenuChange</c>, so it's raised for them here.
    /// </summary>
    private void OnSplitPaneVisible(object? sender, SplitPaneVisibleEventArgs e)
    {
        if (_splitPane is null || !ReferenceEquals(e.Source, _splitPane))
        {
            return;
        }

        _isPaneVisible = e.Visible;
        UpdatePaneState();
        UpdateState();
        MenuController.RaiseMenuChanged(this);
    }

    /// <summary>
    /// <c>menu-pane-visible</c>, and what follows from it: shown (<c>display: flex</c>) while it's an enabled side in a visible pane,
    /// and <c>z-index: 0</c>. The pane lays out its sides again.
    /// </summary>
    private void UpdatePaneState()
    {
        PseudoClasses.Set(MenuPaneVisible, _isPaneVisible);
        _shown.Set(PseudoClasses.Contains(ShowMenu) || IsPaneShown);
        UpdateZIndex();
        _splitPane?.SidesChanged();
    }

    // ── Animation ────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic's <c>beforeAnimation</c>: shown, the content covered, and the will event.</summary>
    private void BeforeAnimation(bool shouldOpen, string? role)
    {
        // A cancel is of this operation only. Ionic clears it only in setOpen, so one from a swipe, which never gets there, would
        // make the next open return false and leave the menu animating.
        _operationCancelled = false;

        // show-menu, tabindex="0" and show-backdrop
        _shown.Set(true);
        _focusable.Set(true);
        PseudoClasses.Set(ShowMenu, true);

        // menu-content-open: pointer-events: none, and aria-hidden.
        if (_contentEl is { } content)
        {
            _contentNoHit ??= content.SetValue(IsHitTestVisibleProperty, false, BindingPriority.Style);
            _contentOffscreen ??= content.SetValue(AutomationProperties.IsOffscreenBehaviorProperty, IsOffscreenBehavior.Offscreen, BindingPriority.Style);
        }

        WatchContentLayout(true);
        _blocker.Block();
        IsMenuAnimating = true;
        if (shouldOpen)
        {
            RaiseEvent(new RoutedEventArgs(IonWillOpenEvent, this));
        }
        else
        {
            RaiseEvent(new MenuCloseEventArgs(IonWillCloseEvent, role));
        }
    }

    /// <summary>
    /// What <see cref="BeforeAnimation"/> did, undone when no animation could be made: it ends as it was, with the did event of that
    /// state after the will event already raised, as a swipe that goes back does.
    /// </summary>
    private void UndoBeforeAnimation(string? role) => AfterAnimation(IsOpenState, role);

    /// <summary>The closed state: hidden, the content back, the blocker off.</summary>
    private void HideClosed()
    {
        _blocker.Unblock();
        _shown.Set(IsPaneShown);
        _focusable.Set(false);
        PseudoClasses.Set(ShowMenu, false);
        _contentNoHit?.Dispose();
        _contentNoHit = null;
        _contentOffscreen?.Dispose();
        _contentOffscreen = null;
        WatchContentLayout(false);
    }

    /// <summary>
    /// Ionic's <c>loadAnimation</c>: the container's width, after a layout pass now that it's shown, as <c>offsetWidth</c> forces
    /// one; a new animation if the width or the side changed.
    /// </summary>
    private void LoadAnimation()
    {
        ApplyTemplate();
        UpdateLayout();
        var width = _container?.Bounds.Width ?? 0;
        var isEndSide = ComputeIsEndSide();
        var contentSign = ContentSign();
        if (width == _width && _animation is not null && !_animationStale && isEndSide == _isEndSide && contentSign == _contentSign)
        {
            return;
        }

        _width = width;
        _isEndSide = isEndSide;
        _contentSign = contentSign;
        _animation?.Destroy();
        _animation = null;
        _animationStale = false;
        var animation = MenuController.CreateAnimation(Type, new MenuAnimationContext(
            this, (Control?)_container ?? this, (Control?)_backdrop ?? this, _contentEl,
            _contentEl is null ? null : _contentShadow, width, isEndSide, contentSign));
        _animation = animation;
        if (!AvonicConfig.Animated)
        {
            animation.Duration(0);
        }

        animation.Fill(AnimationFill.Both);
    }

    /// <summary>-1 when the content's parent is mirrored to the top level, so a physical distance is negated for it.</summary>
    private int ContentSign() =>
        _contentEl?.GetVisualParent() is Visual parent && _topLevel is { } top && parent.TransformToVisual(top) is { M11: < 0 } ? -1 : 1;

    /// <summary>Ionic's <c>startAnimation</c>.</summary>
    private async Task StartAnimation(bool shouldOpen, bool animated)
    {
        var isReversed = !shouldOpen;
        var ani = _animation!
            .Direction(isReversed ? AnimationDirection.Reverse : AnimationDirection.Normal)
            .Easing(isReversed ? MdEasingReverse : MdEasing);
        if (animated)
        {
            await ani.Play();
        }
        else
        {
            _ = ani.Play(sync: true);
        }

        // After the play, not in an onFinish, so that a play, stop and play again doesn't run several.
        if (ani.GetDirection() == AnimationDirection.Reverse)
        {
            ani.Direction(AnimationDirection.Normal);
        }
    }

    /// <summary>Ionic's <c>afterAnimation</c>: open or closed, and the did event.</summary>
    private void AfterAnimation(bool isOpen, string? role)
    {
        IsOpenState = isOpen;
        IsMenuAnimating = false;
        if (isOpen)
        {
            RaiseEvent(new RoutedEventArgs(IonDidOpenEvent, this));

            // Focus moves to the menu itself, unless it's already in it.
            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            if (focused is not Visual visual || !(ReferenceEquals(visual, this) || this.IsVisualAncestorOf(visual)))
            {
                Focus();
            }

            StartTrap();
        }
        else
        {
            HideClosed();
            _animation?.Stop();
            RaiseEvent(new MenuCloseEventArgs(IonDidCloseEvent, role));
            StopTrap();
        }
    }

    // ── Input ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>onBackdropClick</c>: a click in the menu but outside its container, while it's open and at least 100 ms after a
    /// swipe ended, closes it with the <c>backdrop</c> role.
    /// </summary>
    private void OnHostClick(object? source)
    {
        if (!IsOpenState || _lastOnEnd >= _lastRelease - 100)
        {
            return;
        }

        if (source is Visual visual && _container is { } container && (ReferenceEquals(visual, container) || container.IsVisualAncestorOf(visual)))
        {
            return;
        }

        _ = Close(true, BackdropRole);
    }

    /// <summary>Ionic's <c>onKeydown</c> on the host: Escape closes it with the <c>backdrop</c> role.</summary>
    private void OnHostKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _ = Close(true, BackdropRole);
        }
    }

    // ── Focus trap ───────────────────────────────────────────────────────────────────────────

    private void StartTrap()
    {
        if (!_trapping && TopLevel.GetTopLevel(this) is { } top)
        {
            _trapping = true;
            top.AddHandler(GotFocusEvent, OnTopLevelGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
        }
    }

    private void StopTrap()
    {
        if (_trapping)
        {
            _trapping = false;
            (_topLevel ?? TopLevel.GetTopLevel(this))?.RemoveHandler(GotFocusEvent, OnTopLevelGotFocus);
        }

        _lastFocus = null;
    }

    /// <summary>
    /// Ionic's <c>handleFocus</c>: an overlay on top that doesn't hold the menu keeps its own trap; otherwise
    /// <see cref="TrapKeyboardFocus"/>.
    /// </summary>
    private void OnTopLevelGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is TopLevel top && OverlayHost.PresentedOverlayIn(top) is { } last && !OverlayHost.Contains(last, this))
        {
            return;
        }

        TrapKeyboardFocus(e);
    }

    /// <summary>
    /// Ionic's <c>trapKeyboardFocus</c>: focus inside is remembered; focus outside goes to the first focusable element in the menu,
    /// then to the last if the remembered one is where focus now is. As Ionic's, the remembered one isn't kept aside before the first
    /// is focused, and that focus updates it, so focus that leaves always ends on the last (measured: Tab past the last element, or
    /// focus given to the content, lands on the last).
    /// </summary>
    private void TrapKeyboardFocus(FocusChangedEventArgs e)
    {
        if (e.Source is not IInputElement target)
        {
            return;
        }

        if (OverlayHost.Contains(this, target))
        {
            _lastFocus = target;
        }
        else
        {
            OverlayHost.FocusFirstDescendant(this, this, e.NavigationMethod);
            if (ReferenceEquals(_lastFocus, TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement()))
            {
                OverlayHost.FocusLastDescendant(this, this, e.NavigationMethod);
            }
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new MenuPeer(this);

    /// <summary><c>role="navigation"</c>, named <c>aria-label</c>: the app's name, or <c>menu</c>.</summary>
    private sealed class MenuPeer(AvonicMenu owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : "menu";

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;
    }
}
