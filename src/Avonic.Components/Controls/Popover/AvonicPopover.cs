using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>ion-popover</c>, md: content in a box beside a reference, over a backdrop. Present it in code
/// (<c>new AvonicPopover { Content = … }.Present(PopoverEvent.From(e))</c>), or declare it in a page and open it with
/// <see cref="AvonicOverlay.Trigger"/> and <see cref="TriggerAction"/>, or <see cref="AvonicOverlay.IsOpen"/>: it's then moved to
/// the overlay layer while it's presented, as Ionic teleports an inline popover. A popover declared in another's content is nested:
/// a submenu. See <c>docs/components/ion-popover/README.md</c>.
/// </summary>
[TemplatePart("PART_Backdrop", typeof(AvonicBackdrop))]
[TemplatePart("PART_Wrapper", typeof(PopoverPanel))]
[TemplatePart("PART_Content", typeof(Border))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_Scroll", typeof(ScrollViewer))]
[PseudoClasses(Nested)]
public partial class AvonicPopover : AvonicOverlay
{
    /// <summary>Ionic's <c>popover-nested</c>: it's in another popover's content.</summary>
    public const string Nested = ":nested";

    /// <summary><c>--backdrop-opacity</c>'s md fallback, <c>var(--ion-backdrop-opacity, 0.32)</c>.</summary>
    internal const double DefaultBackdropOpacity = 0.32;

    /// <summary>Ionic <c>backdropDismiss</c>: a tap on the backdrop, Escape and back dismiss it.</summary>
    public static readonly StyledProperty<bool> BackdropDismissProperty =
        AvaloniaProperty.Register<AvonicPopover, bool>(nameof(BackdropDismiss), defaultValue: true);

    /// <summary>Ionic <c>showBackdrop</c>: whether the backdrop darkens the screen. It's there and takes a tap either way.</summary>
    public static readonly StyledProperty<bool> ShowBackdropProperty =
        AvaloniaProperty.Register<AvonicPopover, bool>(nameof(ShowBackdrop), defaultValue: true);

    /// <summary>Ionic <c>focusTrap</c>.</summary>
    public static readonly StyledProperty<bool> FocusTrapProperty =
        AvaloniaProperty.Register<AvonicPopover, bool>(nameof(FocusTrap), defaultValue: true);

    /// <summary>
    /// Ionic <c>keepContentsMounted</c>: the content is in the visual tree from when the popover is attached, and stays after a
    /// dismiss. Otherwise it's there only while the popover is presented, as a framework mounts an inline popover's content.
    /// </summary>
    public static readonly StyledProperty<bool> KeepContentsMountedProperty =
        AvaloniaProperty.Register<AvonicPopover, bool>(nameof(KeepContentsMounted));

    /// <summary>Ionic <c>dismissOnSelect</c>: a click in the content dismisses it.</summary>
    public static readonly StyledProperty<bool> DismissOnSelectProperty =
        AvaloniaProperty.Register<AvonicPopover, bool>(nameof(DismissOnSelect));

    /// <summary>Ionic <c>side</c>: which side of the reference it's on.</summary>
    public static readonly StyledProperty<PopoverSide> SideProperty =
        AvaloniaProperty.Register<AvonicPopover, PopoverSide>(nameof(Side), PopoverSide.Bottom);

    /// <summary>Ionic <c>alignment</c>: <c>start</c> in md, which <c>componentWillLoad</c> gives it when it's unset.</summary>
    public static readonly StyledProperty<PopoverAlignment> AlignmentProperty =
        AvaloniaProperty.Register<AvonicPopover, PopoverAlignment>(nameof(Alignment), PopoverAlignment.Start);

    /// <summary>Ionic <c>reference</c>: the trigger's box, or the event's point.</summary>
    public static readonly StyledProperty<PopoverReference> ReferenceProperty =
        AvaloniaProperty.Register<AvonicPopover, PopoverReference>(nameof(Reference), PopoverReference.Trigger);

    /// <summary>Ionic <c>size</c>: <c>--width</c>, or the reference's width.</summary>
    public static readonly StyledProperty<PopoverSize> SizeProperty =
        AvaloniaProperty.Register<AvonicPopover, PopoverSize>(nameof(Size), PopoverSize.Auto);

    /// <summary>Ionic <c>triggerAction</c>: what on the <see cref="AvonicOverlay.Trigger"/> presents it.</summary>
    public static readonly StyledProperty<PopoverTriggerAction> TriggerActionProperty =
        AvaloniaProperty.Register<AvonicPopover, PopoverTriggerAction>(nameof(TriggerAction), PopoverTriggerAction.Click);

    /// <summary>Ionic <c>event</c>: what <see cref="AvonicOverlay.Present()"/> positions it by when it's given none.</summary>
    public static readonly StyledProperty<PopoverEvent?> EventProperty =
        AvaloniaProperty.Register<AvonicPopover, PopoverEvent?>(nameof(Event));

    /// <summary>The default slot.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        ContentControl.ContentProperty.AddOwner<AvonicPopover>();

    /// <summary>The template for <see cref="Content"/> when it isn't a control.</summary>
    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        ContentControl.ContentTemplateProperty.AddOwner<AvonicPopover>();

    /// <summary>Ionic <c>ionPopoverWillPresent</c>, raised before <see cref="AvonicOverlay.WillPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonPopoverWillPresentEvent =
        RoutedEvent.Register<AvonicPopover, RoutedEventArgs>(nameof(IonPopoverWillPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionPopoverDidPresent</c>, raised before <see cref="AvonicOverlay.DidPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonPopoverDidPresentEvent =
        RoutedEvent.Register<AvonicPopover, RoutedEventArgs>(nameof(IonPopoverDidPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionPopoverWillDismiss</c>, raised before <see cref="AvonicOverlay.WillDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonPopoverWillDismissEvent =
        RoutedEvent.Register<AvonicPopover, OverlayEventArgs>(nameof(IonPopoverWillDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionPopoverDidDismiss</c>, raised before <see cref="AvonicOverlay.DidDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonPopoverDidDismissEvent =
        RoutedEvent.Register<AvonicPopover, OverlayEventArgs>(nameof(IonPopoverDidDismiss), RoutingStrategies.Bubble);

    private static readonly string[] SafeAreaKeys = ["Avonic.SafeAreaTop", "Avonic.SafeAreaBottom", "Avonic.SafeAreaLeft", "Avonic.SafeAreaRight"];

    /// <summary>
    /// What a call to <see cref="Present(PopoverEvent?)"/> or <c>presentFromTrigger</c> gave, for <see cref="OnPresenting"/>, which
    /// runs in the same asynchronous flow under the lock: Ionic's arguments to <c>present()</c>. Kept with the popover it's for, so
    /// another popover presented from inside the flow doesn't read it.
    /// </summary>
    private static readonly AsyncLocal<PresentCall?> s_presentCall = new();

    /// <summary>A call to <see cref="Dismiss(object?, string?, bool)"/> with <c>dismissParentPopover</c> false, for <see cref="OnDismissing"/>.</summary>
    private static readonly AsyncLocal<AvonicPopover?> s_keepParent = new();

    /// <summary>The safe areas inside, 0: <c>::slotted(.popover-viewport)</c>. The popover's position reads the host's.</summary>
    private readonly ResourceDictionary _safeArea = new();

    private AvonicBackdrop? _backdrop;
    private PopoverPanel? _wrapper;
    private Border? _content;
    private ContentPresenter? _presenter;
    private ScrollViewer? _scroll;
    private bool _mounted;
    private bool _loaded;
    private AvonicPopover? _parentPopover;
    private IDisposable? _direction;
    private IDisposable? _origin;

    /// <summary>The transform's origin the enter animation worked out, in the content's own coordinates, given with its place.</summary>
    private RelativePoint _pendingOrigin;
    private PopoverEvent? _presentEvent;
    private bool _focusDescendantOnPresent;

    public AvonicPopover()
    {
        foreach (var key in SafeAreaKeys)
        {
            _safeArea[key] = 0d;
        }

        Resources.MergedDictionaries.Add(_safeArea);
    }

    public bool BackdropDismiss
    {
        get => GetValue(BackdropDismissProperty);
        set => SetValue(BackdropDismissProperty, value);
    }

    public bool ShowBackdrop
    {
        get => GetValue(ShowBackdropProperty);
        set => SetValue(ShowBackdropProperty, value);
    }

    public bool FocusTrap
    {
        get => GetValue(FocusTrapProperty);
        set => SetValue(FocusTrapProperty, value);
    }

    public bool KeepContentsMounted
    {
        get => GetValue(KeepContentsMountedProperty);
        set => SetValue(KeepContentsMountedProperty, value);
    }

    public bool DismissOnSelect
    {
        get => GetValue(DismissOnSelectProperty);
        set => SetValue(DismissOnSelectProperty, value);
    }

    public PopoverSide Side
    {
        get => GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    public PopoverAlignment Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    public PopoverReference Reference
    {
        get => GetValue(ReferenceProperty);
        set => SetValue(ReferenceProperty, value);
    }

    public PopoverSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public PopoverTriggerAction TriggerAction
    {
        get => GetValue(TriggerActionProperty);
        set => SetValue(TriggerActionProperty, value);
    }

    public PopoverEvent? Event
    {
        get => GetValue(EventProperty);
        set => SetValue(EventProperty, value);
    }

    [Content]
    [DependsOn(nameof(ContentTemplate))]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IDataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? IonPopoverWillPresent
    {
        add => AddHandler(IonPopoverWillPresentEvent, value);
        remove => RemoveHandler(IonPopoverWillPresentEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonPopoverDidPresent
    {
        add => AddHandler(IonPopoverDidPresentEvent, value);
        remove => RemoveHandler(IonPopoverDidPresentEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonPopoverWillDismiss
    {
        add => AddHandler(IonPopoverWillDismissEvent, value);
        remove => RemoveHandler(IonPopoverWillDismissEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonPopoverDidDismiss
    {
        add => AddHandler(IonPopoverDidDismissEvent, value);
        remove => RemoveHandler(IonPopoverDidDismissEvent, value);
    }

    /// <summary>Whether the content is in the visual tree: presented, or kept mounted.</summary>
    internal bool IsContentMounted => _mounted;

    /// <summary>The backdrop part.</summary>
    internal AvonicBackdrop? Backdrop => _backdrop;

    /// <summary>The <c>wrapper</c>, <c>.popover-wrapper</c>.</summary>
    internal PopoverPanel? Wrapper => _wrapper;

    /// <summary>The <c>content</c> part, <c>.popover-content</c>.</summary>
    internal Border? ContentPart => _content;

    /// <summary>Ionic's <c>parentPopover</c>: the popover whose content this one was in when it was first attached.</summary>
    internal AvonicPopover? ParentPopover => _parentPopover;

    protected internal override bool BackdropDismissEnabled => BackdropDismiss;

    protected internal override bool FocusTrapEnabled => FocusTrap;

    protected override RoutedEvent<RoutedEventArgs> ControlWillPresentEvent => IonPopoverWillPresentEvent;

    protected override RoutedEvent<RoutedEventArgs> ControlDidPresentEvent => IonPopoverDidPresentEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlWillDismissEvent => IonPopoverWillDismissEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlDidDismissEvent => IonPopoverDidDismissEvent;

    /// <summary>The options <c>present()</c> gives the enter animation.</summary>
    protected override object? AnimationOptions => new PopoverAnimationOptions(_presentEvent, Size, TriggerElement, Reference, Side, Alignment);

    /// <summary>
    /// <c>md.enter.js</c>: the place worked out from the layout, then 300 ms on <c>cubic-bezier(0.36,0.66,0.04,1)</c>: the backdrop to
    /// <c>--backdrop-opacity</c>, the wrapper 0.01 to 1 over its own 150 ms, the content <c>scale(0.8)</c> to 1 from its origin.
    /// </summary>
    protected override AnimationBuilder MdEnterAnimation => (baseElement, options) =>
    {
        var popover = (AvonicPopover)baseElement;
        var place = popover.WorkOutPlace(options as PopoverAnimationOptions ?? (PopoverAnimationOptions)popover.AnimationOptions!);
        var backdrop = AvonicAnimation.Create()
            .AddElement(popover._backdrop)
            .FromTo("opacity", 0.01, popover.BackdropOpacity())
            .BeforeStyles(new() { ["pointer-events"] = "none" })
            .AfterClearStyles(["pointer-events"]);
        var wrapper = AvonicAnimation.Create()
            .AddElement(popover._wrapper)
            .Duration(150)
            .FromTo("opacity", 0.01, 1);
        var content = AvonicAnimation.Create()
            .AddElement(popover._content)
            .BeforeAddWrite(() => popover.ApplyPlace(place))
            .FromTo("transform", "scale(0.8)", "scale(1)");
        return AvonicAnimation.Create()
            .Easing("cubic-bezier(0.36,0.66,0.04,1)")
            .Duration(300)
            .AddAnimation(backdrop, wrapper, content);
    };

    /// <summary><c>md.leave.js</c>: 150 ms, <c>ease</c>, the backdrop to 0 and the wrapper 0.99 to 0; then the place is taken away.</summary>
    protected override AnimationBuilder MdLeaveAnimation => (baseElement, _) =>
    {
        var popover = (AvonicPopover)baseElement;
        var backdrop = AvonicAnimation.Create()
            .AddElement(popover._backdrop)
            .FromTo("opacity", popover.BackdropOpacity(), 0);
        var wrapper = AvonicAnimation.Create()
            .AddElement(popover._wrapper)
            .FromTo("opacity", 0.99, 0);
        return AvonicAnimation.Create()
            .Easing("ease")
            .AfterAddWrite(popover.ClearPlace)
            .Duration(150)
            .AddAnimation(backdrop, wrapper);
    };

    /// <summary>
    /// Ionic <c>present(event)</c>: positioned by <paramref name="ev"/>, or by <see cref="Event"/> when it's null. The select presents
    /// its popover this way, with itself as the target.
    /// </summary>
    public Task Present(PopoverEvent? ev) => PresentWith(ev, focusDescendant: false);

    /// <summary>
    /// Ionic <c>dismiss(data, role, dismissParentPopover)</c>: with <paramref name="dismissParentPopover"/> false, a nested popover
    /// is dismissed alone. <see cref="AvonicOverlay.Dismiss(object?, string?)"/> is this with true.
    /// </summary>
    public Task<bool> Dismiss(object? data, string? role, bool dismissParentPopover)
    {
        if (dismissParentPopover)
        {
            return Dismiss(data, role);
        }

        var previous = s_keepParent.Value;
        s_keepParent.Value = this;
        try
        {
            return Dismiss(data, role);
        }
        finally
        {
            s_keepParent.Value = previous;
        }
    }

    /// <summary>Ionic's <c>presentFromTrigger(event, focusDescendant)</c>.</summary>
    internal Task PresentWith(PopoverEvent? ev, bool focusDescendant)
    {
        var previous = s_presentCall.Value;
        s_presentCall.Value = new PresentCall(this, ev, focusDescendant);
        try
        {
            // One made in code presents in its event's window, as Ionic's has one document; an inline one in its own.
            if (Parent is null && (ev ?? Event)?.Target is { } target && TopLevel.GetTopLevel(target) is { } top)
            {
                while (top is PopupRoot { ParentTopLevel: { } parent })
                {
                    top = parent;
                }

                return Present(top);
            }

            return Present();
        }
        finally
        {
            s_presentCall.Value = previous;
        }
    }

    /// <summary>A present from input, which nothing awaits, its fault logged.</summary>
    internal void PresentFromTrigger(PopoverEvent? ev, bool focusDescendant = false) =>
        LogFault(PresentWith(ev, focusDescendant), "present");

    /// <summary>A dismiss from input of this popover alone, which nothing awaits, its fault logged.</summary>
    internal void DismissAlone() => LogFault(Dismiss(null, null, dismissParentPopover: false), "dismiss");

    /// <summary>
    /// Ionic's own <c>present()</c> before the shared one: the event and whether to focus inside after, the direction it has in its
    /// page, the content mounted, the keyboard and the dismiss interactions.
    /// </summary>
    protected override void OnPresenting()
    {
        var call = s_presentCall.Value is { } c && ReferenceEquals(c.Popover, this) ? c : null;
        _presentEvent = call?.Event ?? Event;
        _focusDescendantOnPresent = call?.FocusDescendant ?? false;

        // dir copied from where it is, unless it has its own, as it's hoisted out of its page.
        _direction?.Dispose();
        _direction = null;
        if (!IsSet(FlowDirectionProperty))
        {
            var direction = ((Visual?)Placeholder ?? this).FlowDirection;
            _direction = SetValue(FlowDirectionProperty, direction, BindingPriority.Style);
        }

        Mount(true);
        ConfigureKeyboardInteraction();
        ConfigureDismissInteraction();
    }

    /// <summary>Ionic's own <c>present()</c> after the shared one: the first focusable inside, after a keyboard activation.</summary>
    protected override void OnPresented()
    {
        if (_focusDescendantOnPresent)
        {
            _focusDescendantOnPresent = false;
            FocusFirstDescendant();
        }
    }

    /// <summary>Ionic's own <c>dismiss()</c> before the shared one: the parent popover first, not awaited, unless it's to stay.</summary>
    protected override Task<bool> OnDismissing(object? data, string? role)
    {
        var keepParent = ReferenceEquals(s_keepParent.Value, this);
        if (!keepParent && _parentPopover is { } parent)
        {
            // Not awaited, and its own lock first, a microtask: its events come after this one's willDismiss (M7).
            Dispatcher.UIThread.Post(() => LogFault(parent.Dismiss(data, role), "dismiss"));
        }

        return Task.FromResult(true);
    }

    /// <summary>Ionic's own <c>dismiss()</c> after the shared one: the interactions go, the content is unmounted, the direction goes.</summary>
    protected override void OnDismissed(bool dismissed)
    {
        if (!dismissed)
        {
            return;
        }

        RemoveKeyboardInteraction();
        RemoveDismissInteraction();
        if (!KeepContentsMounted)
        {
            Mount(false);
        }

        _direction?.Dispose();
        _direction = null;
        _presentEvent = null;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _backdrop = e.NameScope.Find<AvonicBackdrop>("PART_Backdrop");
        _wrapper?.RemoveHandlers(OnWrapperClick);
        _wrapper = e.NameScope.Find<PopoverPanel>("PART_Wrapper");
        _wrapper?.AddClickHandlers(OnWrapperClick);
        _content = e.NameScope.Find<Border>("PART_Content");
        if (_presenter is not null)
        {
            _presenter.Content = null;
        }

        _presenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _scroll = e.NameScope.Find<ScrollViewer>("PART_Scroll");
        UpdatePresenter();
        UpdateNested();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
        {
            if (change.OldValue is ILogical oldChild)
            {
                LogicalChildren.Remove(oldChild);
            }

            if (change.NewValue is ILogical newChild && newChild.LogicalParent is null)
            {
                LogicalChildren.Add(newChild);
            }

            UpdatePresenter();
        }
        else if (change.Property == ContentTemplateProperty)
        {
            UpdatePresenter();
        }
        else if (change.Property == TriggerActionProperty)
        {
            ConfigureTrigger();
        }
        else if (change.Property == KeepContentsMountedProperty && !IsShown)
        {
            if (change.GetNewValue<bool>() && TopLevel.GetTopLevel(this) is not null)
            {
                ApplyTemplate();
                Mount(true);
            }
            else if (!change.GetNewValue<bool>())
            {
                Mount(false);
            }
        }
    }

    /// <summary>
    /// Ionic's <c>componentWillLoad</c>: the parent popover, the closest one around it, found once; and <c>componentDidLoad</c>'s
    /// listener to the parent's <c>ionPopoverWillDismiss</c>, which dismisses this one alone.
    /// </summary>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (_loaded || Parent is OverlayHost)
        {
            return;
        }

        _loaded = true;
        _parentPopover = this.GetLogicalAncestors().OfType<AvonicPopover>().FirstOrDefault();
        _parentPopover?.AddHandler(IonPopoverWillDismissEvent, OnParentWillDismiss);
        UpdateNested();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // keepContentsMounted: mounted from creation, which here is the first attach to a page.
        if (KeepContentsMounted)
        {
            ApplyTemplate();
            Mount(true);
        }
    }

    private void OnParentWillDismiss(object? sender, OverlayEventArgs e)
    {
        // Its own lock first, a microtask: after the parent's willDismiss events (M7).
        if (ReferenceEquals(e.Source, _parentPopover))
        {
            Dispatcher.UIThread.Post(DismissAlone);
        }
    }

    /// <summary><c>popover-nested</c>: no backdrop, and the offsets of <c>popover.md.css</c>.</summary>
    private void UpdateNested()
    {
        var nested = _parentPopover is not null;
        PseudoClasses.Set(Nested, nested);
        _backdrop?.IsVisible = !nested;
    }

    /// <summary>
    /// <c>var(--backdrop-opacity)</c>: <c>Avonic.Popover.BackdropOpacity</c>, falling back to <c>Avonic.BackdropOpacity</c>
    /// (<c>--ion-backdrop-opacity</c>), then 0.32.
    /// </summary>
    internal double BackdropOpacity()
    {
        foreach (var key in (string[])["Avonic.Popover.BackdropOpacity", "Avonic.BackdropOpacity"])
        {
            if (this.TryFindResource(key, ActualThemeVariant, out var value) && Css.IsSet(value) && value is double or float or int)
            {
                return Convert.ToDouble(value);
            }
        }

        return DefaultBackdropOpacity;
    }

    // ── Positioning: md.enter.js ───────────────────────────────────────────────────────────

    /// <summary>
    /// What <c>md.enter.js</c> works out before it plays: the content's box read from the layout, as <c>getBoundingClientRect</c>
    /// forces one, then <see cref="PopoverPosition"/>, in the top level's coordinates, which are the viewport's, left to right
    /// whatever the direction, as the client's are. The overlay host covers it; in a right-to-left window it's mirrored.
    /// </summary>
    private PopoverPanel.Placement? WorkOutPlace(PopoverAnimationOptions options)
    {
        if (_content is null || _wrapper is null || Parent is not Visual host || TopLevel.GetTopLevel(this) is not { } root)
        {
            return null;
        }

        UpdateLayout();
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        var bodyWidth = host.Bounds.Width;
        var bodyHeight = host.Bounds.Height;
        var cover = options.Size == PopoverSize.Cover;
        var referenceElement = options.Trigger ?? options.Event?.Target;
        var contentWidth = _content.Bounds.Width;
        var contentHeight = _content.Bounds.Height;
        if (cover && referenceElement is not null && BoxIn(referenceElement, root) is { } referenceBox)
        {
            contentWidth = referenceBox.Width;
        }

        var defaultPosition = new PopoverPosition.Result(
            bodyHeight / 2 - contentHeight / 2, bodyWidth / 2 - contentWidth / 2, null, rtl ? 1 : 0, 0);
        var reference = referenceElement is null ? null : BoxIn(referenceElement, root);
        Point? point = options.Event is { } ev ? PointIn(ev, root) ?? default(Point) : null;
        var results = PopoverPosition.GetPopoverPosition(rtl, contentWidth, contentHeight, options.Reference, options.Side,
            options.Alignment, defaultPosition, reference, point);

        // md's padding and the safe areas, none for cover; the CSS adds the real safe area wherever it's checked.
        var safe = SafeArea(host);
        var adjusted = PopoverPosition.CalculateWindowAdjustment(options.Side, results.Top, results.Left,
            cover ? 0 : PopoverPosition.MdBodyPadding, bodyWidth, bodyHeight, contentWidth, contentHeight, cover ? default : safe,
            results.OriginX, results.OriginY, results.Reference);
        var left = adjusted.Left + (adjusted.CheckSafeAreaLeft ? safe.Left : 0) - (adjusted.CheckSafeAreaRight ? safe.Right : 0);
        var top = adjusted.Top + (adjusted.CheckSafeAreaTop ? safe.Top : 0);
        double? bottom = adjusted.Bottom is { } b ? b + (adjusted.CheckSafeAreaBottom ? safe.Bottom : 0) : null;
        var mirrored = _wrapper.TransformToVisual(root) is { } m && m.M11 < 0;
        _pendingOrigin = new RelativePoint(mirrored ? 1 - adjusted.OriginX : adjusted.OriginX, adjusted.OriginY, RelativeUnit.Relative);
        return new PopoverPanel.Placement(top, left, bottom, cover ? contentWidth : null, mirrored);
    }

    /// <summary>The content's <c>beforeStyles</c> and the base's <c>beforeAddWrite</c>: the place, the origin, cover's width.</summary>
    private void ApplyPlace(PopoverPanel.Placement? place)
    {
        if (_wrapper is null || _content is null || place is null)
        {
            return;
        }

        _wrapper.Place = place;
        _origin?.Dispose();
        _origin = _content.SetValue(RenderTransformOriginProperty, _pendingOrigin, BindingPriority.Animation);
    }

    /// <summary>The leave's <c>afterAddWrite</c>: <c>top</c>, <c>left</c>, <c>bottom</c>, <c>transform-origin</c> and <c>--width</c> removed.</summary>
    private void ClearPlace()
    {
        _wrapper?.Place = null;
        _origin?.Dispose();
        _origin = null;
    }

    /// <summary>An element's box in the top level's coordinates, as <c>getBoundingClientRect</c>; null if it isn't in that window.</summary>
    private static Rect? BoxIn(Visual element, Visual root) =>
        element.TransformToVisual(root) is { } matrix ? new Rect(element.Bounds.Size).TransformToAABB(matrix) : null;

    /// <summary>The event's point in the popover's top level; null with none, or out of its window.</summary>
    private Point? PointIn(PopoverEvent ev, Visual root)
    {
        if (ev.ClientPosition is not { } point)
        {
            return null;
        }

        var top = ev.Target is { } target ? TopLevel.GetTopLevel(target) : TopLevel.GetTopLevel(this);
        return top is null ? null : top.TranslatePoint(point, root);
    }

    /// <summary><c>getSafeAreaInsets</c>: the <c>Avonic.SafeArea*</c> keys as the host reads them, outside the popover's own zeros.</summary>
    private static Thickness SafeArea(Visual host)
    {
        double Read(string key) => host is IResourceHost resources && resources.TryFindResource(key, out var value) && value is double d ? d : 0;
        return new Thickness(Read("Avonic.SafeAreaLeft"), Read("Avonic.SafeAreaTop"), Read("Avonic.SafeAreaRight"), Read("Avonic.SafeAreaBottom"));
    }

    // ── Content ───────────────────────────────────────────────────────────────────────────

    private void Mount(bool mounted)
    {
        _mounted = mounted;

        // The presenter is inside the scroller, whose own template waits for a measure that a hidden popover doesn't have.
        if (mounted)
        {
            _scroll?.ApplyTemplate();
        }

        UpdatePresenter();
    }

    private void UpdatePresenter()
    {
        if (_presenter is null)
        {
            return;
        }

        _presenter.ContentTemplate = ContentTemplate;
        _presenter.Content = _mounted ? Content : null;

        // .popover-content is overflow: auto, and the viewport a flex column: an ion-content in it has min-height: 0, so it
        // shrinks to the box and scrolls itself; anything else is as tall as it is, and the box scrolls it.
        _scroll?.VerticalScrollBarVisibility = Content is AvonicContent ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
    }

    /// <summary>Ionic's <c>focusFirstDescendant(el)</c>: the first focusable element in the content, else the popover.</summary>
    private void FocusFirstDescendant()
    {
        var first = (_presenter as Visual ?? this).GetVisualDescendants().OfType<InputElement>()
            .FirstOrDefault(e => e.Focusable && e.IsEffectivelyEnabled && e.IsEffectivelyVisible && KeyboardNavigation.GetIsTabStop(e));
        if (first is not null)
        {
            first.Focus(NavigationMethod.Directional);
        }
        else
        {
            Focus();
        }
    }

    /// <summary>What a call to <c>present</c> gave.</summary>
    private sealed record PresentCall(AvonicPopover Popover, PopoverEvent? Event, bool FocusDescendant);
}

/// <summary>The options <c>present()</c> gives the popover's enter animation, as Ionic's <c>opts</c>.</summary>
/// <param name="Event"><c>event || this.event</c>.</param>
/// <param name="Size"><c>size</c>.</param>
/// <param name="Trigger"><c>triggerEl</c>.</param>
/// <param name="Reference"><c>reference</c>.</param>
/// <param name="Side"><c>side</c>.</param>
/// <param name="Alignment"><c>align</c>.</param>
public sealed record PopoverAnimationOptions(
    PopoverEvent? Event,
    PopoverSize Size,
    Visual? Trigger,
    PopoverReference Reference,
    PopoverSide Side,
    PopoverAlignment Alignment);
