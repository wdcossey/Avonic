using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-nav</c>: a stack of pages, the top one shown, with md's transition between them. Every call is queued and run one
/// at a time, as Ionic's <c>queueTrns</c> does. See <c>docs/components/ion-nav/README.md</c>.
/// </summary>
/// <remarks>
/// A page (Ionic's <c>NavComponent</c>, the <c>component</c> of <see cref="Push"/>, <see cref="Root"/> and the rest) is one of:
/// a <see cref="Control"/>, used as it is; a <see cref="Type"/> of control with a parameterless constructor, made when it's first
/// entered; a <c>Func&lt;Control&gt;</c>, called then; a <see cref="ViewController"/>; or any other object, a view model, whose page
/// the nav's data templates build (its own, its ancestors', the application's, where an app's view locator is), with the object as
/// the page's <c>DataContext</c>. <c>componentProps</c> are set on the page by name before it's attached (<see cref="NavProps"/>).
/// </remarks>
public partial class AvonicNav : TemplatedControl
{
    /// <summary>The pseudoclass the entering page gets when a page is under it: Ionic's <c>can-go-back</c> class.</summary>
    public const string CanGoBackPseudoClass = ":can-go-back";

    /// <summary>Ionic <c>animated</c>: false turns the transitions off.</summary>
    public static readonly StyledProperty<bool> AnimatedProperty =
        AvaloniaProperty.Register<AvonicNav, bool>(nameof(Animated), true);

    /// <summary>Ionic <c>animation</c>: the transition, when a call gives none. Null is md's.</summary>
    public static readonly StyledProperty<AnimationBuilder?> AnimationProperty =
        AvaloniaProperty.Register<AvonicNav, AnimationBuilder?>(nameof(Animation));

    /// <summary>Ionic <c>root</c>: the page set as the root when the nav loads, and again whenever it changes after.</summary>
    public static readonly StyledProperty<object?> RootProperty =
        AvaloniaProperty.Register<AvonicNav, object?>(nameof(Root));

    /// <summary>Ionic <c>rootParams</c>: <see cref="Root"/>'s props.</summary>
    public static readonly StyledProperty<IReadOnlyDictionary<string, object?>?> RootParamsProperty =
        AvaloniaProperty.Register<AvonicNav, IReadOnlyDictionary<string, object?>?>(nameof(RootParams));

    /// <summary>Ionic <c>ionNavWillChange</c>: a transition starts. It doesn't bubble.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonNavWillChangeEvent =
        RoutedEvent.Register<AvonicNav, RoutedEventArgs>(nameof(IonNavWillChange), RoutingStrategies.Direct);

    /// <summary>Ionic <c>ionNavDidChange</c>: a transition has ended. It doesn't bubble.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonNavDidChangeEvent =
        RoutedEvent.Register<AvonicNav, RoutedEventArgs>(nameof(IonNavDidChange), RoutingStrategies.Direct);

    /// <summary>Ionic <c>ionViewWillEnter</c>, raised on a page that's about to become the top. It doesn't bubble.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonViewWillEnterEvent =
        RoutedEvent.Register<AvonicNav, RoutedEventArgs>("IonViewWillEnter", RoutingStrategies.Direct);

    /// <summary>Ionic <c>ionViewDidEnter</c>, raised on a page that has become the top.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonViewDidEnterEvent =
        RoutedEvent.Register<AvonicNav, RoutedEventArgs>("IonViewDidEnter", RoutingStrategies.Direct);

    /// <summary>Ionic <c>ionViewWillLeave</c>, raised on a page that's about to stop being the top, or to be removed.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonViewWillLeaveEvent =
        RoutedEvent.Register<AvonicNav, RoutedEventArgs>("IonViewWillLeave", RoutingStrategies.Direct);

    /// <summary>Ionic <c>ionViewDidLeave</c>, raised on a page that has stopped being the top, or is being removed.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonViewDidLeaveEvent =
        RoutedEvent.Register<AvonicNav, RoutedEventArgs>("IonViewDidLeave", RoutingStrategies.Direct);

    /// <summary>Ionic <c>ionViewWillUnload</c>, raised on a page that's about to leave the nav.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonViewWillUnloadEvent =
        RoutedEvent.Register<AvonicNav, RoutedEventArgs>("IonViewWillUnload", RoutingStrategies.Direct);

    private readonly List<ViewController> _views = [];
    private readonly List<Instruction> _transInstr = [];
    private readonly List<Instruction> _beforeLoad = [];
    private readonly List<Control> _pages = [];
    private NavPagesHost? _host;
    private bool _isTransitioning;
    private bool _didLoad;

    static AvonicNav()
    {
        RootProperty.Changed.AddClassHandler<AvonicNav>((nav, _) => nav.RootChanged());

        // .ion-page has no overflow rule, and nor has the nav's host.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicNav>(false);
    }

    public bool Animated
    {
        get => GetValue(AnimatedProperty);
        set => SetValue(AnimatedProperty, value);
    }

    public AnimationBuilder? Animation
    {
        get => GetValue(AnimationProperty);
        set => SetValue(AnimationProperty, value);
    }

    public object? Root
    {
        get => GetValue(RootProperty);
        set => SetValue(RootProperty, value);
    }

    public IReadOnlyDictionary<string, object?>? RootParams
    {
        get => GetValue(RootParamsProperty);
        set => SetValue(RootParamsProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? IonNavWillChange
    {
        add => AddHandler(IonNavWillChangeEvent, value);
        remove => RemoveHandler(IonNavWillChangeEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonNavDidChange
    {
        add => AddHandler(IonNavDidChangeEvent, value);
        remove => RemoveHandler(IonNavDidChangeEvent, value);
    }

    /// <summary>Listens to <see cref="IonViewWillEnterEvent"/> on a page.</summary>
    public static void AddIonViewWillEnterHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.AddHandler(IonViewWillEnterEvent, handler);

    /// <summary>Stops listening to <see cref="IonViewWillEnterEvent"/> on a page.</summary>
    public static void RemoveIonViewWillEnterHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.RemoveHandler(IonViewWillEnterEvent, handler);

    /// <summary>Listens to <see cref="IonViewDidEnterEvent"/> on a page.</summary>
    public static void AddIonViewDidEnterHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.AddHandler(IonViewDidEnterEvent, handler);

    /// <summary>Stops listening to <see cref="IonViewDidEnterEvent"/> on a page.</summary>
    public static void RemoveIonViewDidEnterHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.RemoveHandler(IonViewDidEnterEvent, handler);

    /// <summary>Listens to <see cref="IonViewWillLeaveEvent"/> on a page.</summary>
    public static void AddIonViewWillLeaveHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.AddHandler(IonViewWillLeaveEvent, handler);

    /// <summary>Stops listening to <see cref="IonViewWillLeaveEvent"/> on a page.</summary>
    public static void RemoveIonViewWillLeaveHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.RemoveHandler(IonViewWillLeaveEvent, handler);

    /// <summary>Listens to <see cref="IonViewDidLeaveEvent"/> on a page.</summary>
    public static void AddIonViewDidLeaveHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.AddHandler(IonViewDidLeaveEvent, handler);

    /// <summary>Stops listening to <see cref="IonViewDidLeaveEvent"/> on a page.</summary>
    public static void RemoveIonViewDidLeaveHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.RemoveHandler(IonViewDidLeaveEvent, handler);

    /// <summary>Listens to <see cref="IonViewWillUnloadEvent"/> on a page.</summary>
    public static void AddIonViewWillUnloadHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.AddHandler(IonViewWillUnloadEvent, handler);

    /// <summary>Stops listening to <see cref="IonViewWillUnloadEvent"/> on a page.</summary>
    public static void RemoveIonViewWillUnloadHandler(Interactive page, EventHandler<RoutedEventArgs> handler) =>
        page.RemoveHandler(IonViewWillUnloadEvent, handler);

    /// <summary>The pages built and in the nav, in the order they were added.</summary>
    internal IReadOnlyList<Control> Pages => _pages;

    // ── Methods ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>push</c>: <paramref name="component"/> becomes the top, over the current one.</summary>
    /// <returns>Whether the transition completed. Faults when the stack can't be changed that way.</returns>
    public Task<bool> Push(object component, IReadOnlyDictionary<string, object?>? componentProps = null, NavOptions? opts = null, TransitionDoneFn? done = null) =>
        Insert(-1, component, componentProps, opts, done);

    /// <summary>Ionic <c>insert</c>: <paramref name="component"/> at <paramref name="insertIndex"/>; -1, or past the end, is the top.</summary>
    public Task<bool> Insert(int insertIndex, object component, IReadOnlyDictionary<string, object?>? componentProps = null, NavOptions? opts = null, TransitionDoneFn? done = null) =>
        InsertPages(insertIndex, [new NavComponentWithProps(component, componentProps)], opts, done);

    /// <summary>
    /// Ionic <c>insertPages</c>: each of <paramref name="insertComponents"/> (a page form, or a <see cref="NavComponentWithProps"/>)
    /// at <paramref name="insertIndex"/>. Only the last is built now, and becomes the top if they go at the end.
    /// </summary>
    public Task<bool> InsertPages(int insertIndex, IEnumerable<object> insertComponents, NavOptions? opts = null, TransitionDoneFn? done = null) =>
        QueueTrns(new Instruction(opts) { InsertStart = insertIndex, InsertViews = insertComponents.ToList() }, done);

    /// <summary>Ionic <c>pop</c>: the top page goes, back to the one under it.</summary>
    public Task<bool> Pop(NavOptions? opts = null, TransitionDoneFn? done = null) => RemoveIndex(-1, 1, opts, done);

    /// <summary>Ionic <c>popTo(index)</c>: back to the page at <paramref name="index"/>, every page above it going.</summary>
    public Task<bool> PopTo(int index, NavOptions? opts = null, TransitionDoneFn? done = null) =>
        QueueTrns(new Instruction(opts) { RemoveStart = index + 1, RemoveCount = -1 }, done);

    /// <summary>Ionic <c>popTo(viewController)</c>: back to <paramref name="view"/>, every page above it going.</summary>
    public Task<bool> PopTo(ViewController view, NavOptions? opts = null, TransitionDoneFn? done = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        return QueueTrns(new Instruction(opts) { RemoveView = view, RemoveStart = 1, RemoveCount = -1 }, done);
    }

    /// <summary>Ionic <c>popToRoot</c>: back to the first page.</summary>
    public Task<bool> PopToRoot(NavOptions? opts = null, TransitionDoneFn? done = null) => RemoveIndex(1, -1, opts, done);

    /// <summary>
    /// Ionic <c>removeIndex</c>: <paramref name="removeCount"/> pages (-1: all) from <paramref name="startIndex"/> (-1: the top)
    /// go. Removing the top transitions back to the page under them.
    /// </summary>
    public Task<bool> RemoveIndex(int startIndex, int removeCount = 1, NavOptions? opts = null, TransitionDoneFn? done = null) =>
        QueueTrns(new Instruction(opts) { RemoveStart = startIndex, RemoveCount = removeCount }, done);

    /// <summary>Ionic <c>setRoot</c>: the stack becomes <paramref name="component"/> alone. Unanimated unless the options say.</summary>
    public Task<bool> SetRoot(object component, IReadOnlyDictionary<string, object?>? componentProps = null, NavOptions? opts = null, TransitionDoneFn? done = null) =>
        SetPages([new NavComponentWithProps(component, componentProps)], opts, done);

    /// <summary>
    /// Ionic <c>setPages</c>: the stack becomes <paramref name="views"/>, the last on top. Unanimated unless
    /// <see cref="NavOptions.Animated"/> is true.
    /// </summary>
    public Task<bool> SetPages(IEnumerable<object> views, NavOptions? opts = null, TransitionDoneFn? done = null)
    {
        var options = opts?.Clone() ?? new NavOptions();
        if (options.Animated != true)
        {
            options.Animated = false;
        }

        return QueueTrns(new Instruction(options, clone: false)
        {
            InsertStart = 0,
            InsertViews = views.ToList(),
            RemoveStart = 0,
            RemoveCount = -1,
        }, done);
    }

    /// <summary>Ionic <c>getActive</c>: the top view, or null in an empty nav.</summary>
    public ViewController? GetActive() => _views.Count > 0 ? _views[^1] : null;

    /// <summary>Ionic <c>getByIndex</c>: the view at <paramref name="index"/>, or null.</summary>
    public ViewController? GetByIndex(int index) => index >= 0 && index < _views.Count ? _views[index] : null;

    /// <summary>Ionic <c>canGoBack</c>: whether a view is under <paramref name="view"/>, by default the top one.</summary>
    public bool CanGoBack(ViewController? view = null)
    {
        view ??= GetActive();
        return view is not null && GetPrevious(view) is not null;
    }

    /// <summary>Ionic <c>getPrevious</c>: the view under <paramref name="view"/>, by default the top one, or null.</summary>
    public ViewController? GetPrevious(ViewController? view = null)
    {
        view ??= GetActive();
        if (view is null)
        {
            return null;
        }

        var index = _views.IndexOf(view);
        return index > 0 ? _views[index - 1] : null;
    }

    /// <summary>Ionic <c>getLength</c>: how many views the stack has.</summary>
    public int GetLength() => _views.Count;

    // ── Load and pages ───────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _host?.Detach();
        _host = e.NameScope.Find<NavPagesHost>("PART_Pages");
        _host?.Attach(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachBack(TopLevel.GetTopLevel(this));
        if (!_didLoad)
        {
            Load();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Ionic's disconnectedCallback unloads every view, and the root isn't set again, so a nav that's moved, as an inline
        // modal's content is, comes back empty. The stack is kept here (ADR 0005, docs/infra/ionic-issues.md).
        DetachBack();
    }

    /// <summary>
    /// Ionic's <c>componentWillLoad</c> and <c>componentDidLoad</c>, at the first attach: the swipe's default, then the root, then
    /// the calls made before the load, in order, as Stencil holds a method until the component has loaded.
    /// </summary>
    private void Load()
    {
        _didLoad = true;
        LoadSwipeGesture();
        RootChanged();
        _transInstr.AddRange(_beforeLoad);
        _beforeLoad.Clear();
        NextTrns();
    }

    /// <summary>Ionic <c>rootChanged</c>: a root after load is set at once, unanimated.</summary>
    private void RootChanged()
    {
        if (Root is null || !_didLoad)
        {
            return;
        }

        _ = SetRoot(Root, RootParams);
    }

    internal void AddPage(Control page)
    {
        _pages.Add(page);
        LogicalChildren.Add(page);
        _host?.Add(page);
    }

    internal void RemovePage(Control page)
    {
        _pages.Remove(page);
        _host?.Remove(page);
        LogicalChildren.Remove(page);
    }

    private static void Lifecycle(Control? page, RoutedEvent<RoutedEventArgs> routedEvent) =>
        page?.RaiseEvent(new RoutedEventArgs(routedEvent, page));

    private static void Warn(Control control, string message) =>
        Logger.TryGet(LogEventLevel.Warning, "Avonic")?.Log(control, message);
}
