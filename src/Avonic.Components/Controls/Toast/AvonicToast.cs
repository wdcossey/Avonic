using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Gestures;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>ion-toast</c>, md: a short message at the bottom, the top or the middle of the window, over everything, with no
/// backdrop, which doesn't take focus or block the page. Present it in code (<c>new AvonicToast { Message = …, Duration = 2000
/// }.Present()</c>), or declare it in a page and open it with <see cref="AvonicOverlay.IsOpen"/> or <see cref="AvonicOverlay.Trigger"/>.
/// See <c>docs/components/ion-toast/README.md</c>.
/// </summary>
[TemplatePart("PART_Panel", typeof(ToastPanel))]
[TemplatePart("PART_Wrapper", typeof(Control))]
[TemplatePart("PART_Content", typeof(ToastContent))]
[TemplatePart("PART_Header", typeof(TextBlock))]
[TemplatePart("PART_Message", typeof(TextBlock))]
[TemplatePart("PART_StartButtons", typeof(Panel))]
[TemplatePart("PART_EndButtons", typeof(Panel))]
[PseudoClasses(PositionTop, PositionBottom, PositionMiddle, LayoutBaseline, LayoutStacked)]
public partial class AvonicToast : AvonicOverlay
{
    /// <summary><c>.toast-top</c> on the wrapper.</summary>
    public const string PositionTop = ":position-top";

    /// <summary><c>.toast-bottom</c> on the wrapper.</summary>
    public const string PositionBottom = ":position-bottom";

    /// <summary><c>.toast-middle</c> on the wrapper.</summary>
    public const string PositionMiddle = ":position-middle";

    /// <summary><c>.toast-layout-baseline</c> on the wrapper.</summary>
    public const string LayoutBaseline = ":layout-baseline";

    /// <summary><c>.toast-layout-stacked</c> on the wrapper.</summary>
    public const string LayoutStacked = ":layout-stacked";

    /// <summary>md's enter: 400 ms.</summary>
    internal const double EnterDuration = 400;

    /// <summary>md's leave: 300 ms.</summary>
    internal const double LeaveDuration = 300;

    /// <summary>md's easing, both ways.</summary>
    internal const string Easing = "cubic-bezier(.36,.66,.04,1)";

    /// <summary>md's offset from the edge, or the anchor: 8 px (<c>getAnimationPosition</c>).</summary>
    internal const double EdgeOffset = 8;

    /// <summary>The toast's z-index base: 60000, over every other overlay.</summary>
    private const int ToastZIndex = 60000;

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty = ColorPalette.ColorProperty.AddOwner<AvonicToast>();

    /// <summary>Ionic <c>buttons</c>.</summary>
    public static readonly StyledProperty<IList<ToastButton>?> ButtonsProperty =
        AvaloniaProperty.Register<AvonicToast, IList<ToastButton>?>(nameof(Buttons));

    /// <summary>Ionic <c>cssClass</c>: style classes on the toast, space-separated.</summary>
    public static readonly StyledProperty<string?> CssClassProperty =
        AvaloniaProperty.Register<AvonicToast, string?>(nameof(CssClass));

    /// <summary>
    /// Ionic <c>duration</c>, in milliseconds: how long the toast stays once presented, before it dismisses itself with the role
    /// <c>timeout</c>. 0 keeps it until it's dismissed. Its default is <see cref="AvonicConfig.ToastDuration"/>, at style priority.
    /// </summary>
    public static readonly StyledProperty<double> DurationProperty =
        AvaloniaProperty.Register<AvonicToast, double>(nameof(Duration));

    /// <summary>Ionic <c>header</c>: shown above the message when it isn't null.</summary>
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<AvonicToast, string?>(nameof(Header));

    /// <summary>
    /// Ionic <c>htmlAttributes</c>, spread on the host: <c>id</c> is its <c>AutomationId</c>, <c>aria-label</c> its name. Others have
    /// nothing to do in Avalonia, and are ignored.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyDictionary<string, object?>?> HtmlAttributesProperty =
        AvaloniaProperty.Register<AvonicToast, IReadOnlyDictionary<string, object?>?>(nameof(HtmlAttributes));

    /// <summary>Ionic <c>icon</c>: an ionicons name, or what <see cref="AvonicIcon.Icon"/> takes, before the content.</summary>
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<AvonicToast, string?>(nameof(Icon));

    /// <summary>Ionic <c>layout</c>.</summary>
    public static readonly StyledProperty<ToastLayout> LayoutProperty =
        AvaloniaProperty.Register<AvonicToast, ToastLayout>(nameof(Layout));

    /// <summary>Ionic <c>message</c>, as plain text: shown when it isn't null.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<AvonicToast, string?>(nameof(Message));

    /// <summary>Ionic <c>position</c>.</summary>
    public static readonly StyledProperty<ToastPosition> PositionProperty =
        AvaloniaProperty.Register<AvonicToast, ToastPosition>(nameof(Position));

    /// <summary>
    /// Ionic <c>positionAnchor</c>: a <see cref="Control"/>, or the <see cref="StyledElement.Name"/> of one, looked up at each present in
    /// the toast's name scope, else in its top level, as <c>getElementById</c> searches the document. At the bottom the toast sits 8
    /// above the anchor's top; at the top, 8 below its bottom. Ignored in the middle.
    /// </summary>
    public static readonly StyledProperty<object?> PositionAnchorProperty =
        AvaloniaProperty.Register<AvonicToast, object?>(nameof(PositionAnchor));

    /// <summary>
    /// Ionic <c>swipeGesture</c>: <see cref="ToastSwipeGestureDirection.Vertical"/> lets a swipe dismiss it, up at the top, down at the
    /// bottom, either way in the middle. Renamed, as <see cref="Avalonia.Input.InputElement.SwipeGestureEvent"/> is an event.
    /// </summary>
    public static readonly StyledProperty<ToastSwipeGestureDirection?> ToastSwipeGestureProperty =
        AvaloniaProperty.Register<AvonicToast, ToastSwipeGestureDirection?>(nameof(ToastSwipeGesture));

    /// <summary>Ionic <c>ionToastWillPresent</c>, raised before <see cref="AvonicOverlay.WillPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonToastWillPresentEvent =
        RoutedEvent.Register<AvonicToast, RoutedEventArgs>(nameof(IonToastWillPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionToastDidPresent</c>, raised before <see cref="AvonicOverlay.DidPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonToastDidPresentEvent =
        RoutedEvent.Register<AvonicToast, RoutedEventArgs>(nameof(IonToastDidPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionToastWillDismiss</c>, raised before <see cref="AvonicOverlay.WillDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonToastWillDismissEvent =
        RoutedEvent.Register<AvonicToast, OverlayEventArgs>(nameof(IonToastWillDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionToastDidDismiss</c>, raised before <see cref="AvonicOverlay.DidDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonToastDidDismissEvent =
        RoutedEvent.Register<AvonicToast, OverlayEventArgs>(nameof(IonToastDidDismiss), RoutingStrategies.Bubble);

    private readonly List<ToastNativeButton> _startControls = [];
    private readonly List<ToastNativeButton> _endControls = [];
    private string[] _cssClasses = [];
    private INotifyCollectionChanged? _watchedButtons;
    private bool _buttonsPending;
    private bool _inTree;
    private ToastButton[] _seen = [];

    /// <summary>Ionic's <c>presented</c>, for the swipe's watcher: from the end of a present to the start of a dismiss.</summary>
    private bool _presented;

    /// <summary>Ionic's <c>lastPresentedPosition</c>, worked out at present for both animations.</summary>
    private ToastPresentOptions? _lastPosition;

    /// <summary>Ionic's <c>durationTimeout</c>.</summary>
    private IDisposable? _durationTimeout;

    private ToastPanel? _panel;
    private Control? _wrapper;
    private ToastContent? _content;
    private TextBlock? _header;
    private TextBlock? _message;
    private Panel? _startPanel;
    private Panel? _endPanel;

    static AvonicToast()
    {
        KeyboardCloseProperty.OverrideDefaultValue<AvonicToast>(false);
    }

    public AvonicToast()
    {
        SetCurrentValue(ButtonsProperty, new AvaloniaList<ToastButton>());

        // config.getNumber('toastDuration', 0), the prop's default.
        SetValue(DurationProperty, AvonicConfig.ToastDuration, BindingPriority.Style);

        // The host's onIonToastWillDismiss, Ionic's dispatchCancelHandler.
        AddHandler(IonToastWillDismissEvent, OnWillDismissCancel);
        UpdatePseudoClasses();
    }

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public IList<ToastButton>? Buttons
    {
        get => GetValue(ButtonsProperty);
        set => SetValue(ButtonsProperty, value);
    }

    public string? CssClass
    {
        get => GetValue(CssClassProperty);
        set => SetValue(CssClassProperty, value);
    }

    public double Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public IReadOnlyDictionary<string, object?>? HtmlAttributes
    {
        get => GetValue(HtmlAttributesProperty);
        set => SetValue(HtmlAttributesProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ToastLayout Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ToastPosition Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public object? PositionAnchor
    {
        get => GetValue(PositionAnchorProperty);
        set => SetValue(PositionAnchorProperty, value);
    }

    public ToastSwipeGestureDirection? ToastSwipeGesture
    {
        get => GetValue(ToastSwipeGestureProperty);
        set => SetValue(ToastSwipeGestureProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? IonToastWillPresent
    {
        add => AddHandler(IonToastWillPresentEvent, value);
        remove => RemoveHandler(IonToastWillPresentEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonToastDidPresent
    {
        add => AddHandler(IonToastDidPresentEvent, value);
        remove => RemoveHandler(IonToastDidPresentEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonToastWillDismiss
    {
        add => AddHandler(IonToastWillDismissEvent, value);
        remove => RemoveHandler(IonToastWillDismissEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonToastDidDismiss
    {
        add => AddHandler(IonToastDidDismissEvent, value);
        remove => RemoveHandler(IonToastDidDismissEvent, value);
    }

    /// <summary>The wrapper, <c>.toast-wrapper</c>.</summary>
    internal Control? Wrapper => _wrapper;

    /// <summary>The panel that places the wrapper.</summary>
    internal ToastPanel? Panel => _panel;

    /// <summary>The content, <c>.toast-content</c>.</summary>
    internal ToastContent? Content => _content;

    /// <summary>The start buttons' controls, in order.</summary>
    internal IReadOnlyList<ToastNativeButton> StartButtonControls => _startControls;

    /// <summary>The end buttons' controls, in order.</summary>
    internal IReadOnlyList<ToastNativeButton> EndButtonControls => _endControls;

    /// <summary>Every button's control, as they're shown: the start group's, then the end group's.</summary>
    internal IEnumerable<ToastNativeButton> ButtonControls => _startControls.Concat(_endControls);

    /// <summary>The position the last present worked out, while it's presented.</summary>
    internal ToastPresentOptions? LastPresentedPosition => _lastPosition;

    /// <summary>Not a toast's: it neither blurs nor gives focus back, and has no focus trap.</summary>
    protected internal override bool StealsFocus => false;

    protected override int ZIndexBase => ToastZIndex;

    protected override object? AnimationOptions => _lastPosition;

    protected override RoutedEvent<RoutedEventArgs> ControlWillPresentEvent => IonToastWillPresentEvent;

    protected override RoutedEvent<RoutedEventArgs> ControlDidPresentEvent => IonToastDidPresentEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlWillDismissEvent => IonToastWillDismissEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlDidDismissEvent => IonToastDidDismissEvent;

    /// <summary>
    /// <c>md.enter.js</c>: the wrapper's inline <c>transform: translateY(…)</c> at the top and the bottom, or its <c>top</c> in the
    /// middle, set at once, not animated; then 400 ms <c>cubic-bezier(.36,.66,.04,1)</c> of the wrapper's opacity from 0.01 to 1.
    /// </summary>
    protected override AnimationBuilder MdEnterAnimation => (baseElement, options) =>
    {
        var toast = (AvonicToast)baseElement;
        var position = options as ToastPresentOptions ?? new ToastPresentOptions(toast.Position, EdgeOffset, -EdgeOffset);
        if (toast._wrapper is { } wrapper)
        {
            switch (position.Position)
            {
                case ToastPosition.Top:
                    wrapper.RenderTransform = new TranslateTransform(0, position.Top);
                    break;
                case ToastPosition.Middle:
                    // Ionic sets only top here; the transform of an earlier present at an edge is cleared (PLAN.md, not copied).
                    wrapper.ClearValue(RenderTransformProperty);
                    toast.UpdateLayout();
                    if (toast._panel is { } panel)
                    {
                        // clientHeight, which leaves the border out.
                        var border = wrapper is Border { BorderThickness: var thickness } ? thickness.Top + thickness.Bottom : 0;
                        panel.MiddleTop = OffsetForMiddle(toast.Bounds.Height, wrapper.Bounds.Height - border);
                    }

                    break;
                default:
                    wrapper.RenderTransform = new TranslateTransform(0, position.Bottom);
                    break;
            }
        }

        var wrapperAnimation = AvonicAnimation.Create().AddElement(toast._wrapper).FromTo("opacity", 0.01, 1);
        return AvonicAnimation.Create()
            .Easing(Easing)
            .Duration(EnterDuration)
            .AddAnimation(wrapperAnimation);
    };

    /// <summary><c>md.leave.js</c>: 300 ms, the same curve, the wrapper's opacity from 0.99 to 0.</summary>
    protected override AnimationBuilder MdLeaveAnimation => (baseElement, _) =>
    {
        var toast = (AvonicToast)baseElement;
        var wrapperAnimation = AvonicAnimation.Create().AddElement(toast._wrapper).FromTo("opacity", 0.99, 0);
        return AvonicAnimation.Create()
            .Easing(Easing)
            .Duration(LeaveDuration)
            .AddAnimation(wrapperAnimation);
    };

    /// <summary>
    /// Ionic's <c>getOffsetForMiddlePosition</c>: <c>floor(toast.clientHeight / 2 - wrapper.clientHeight / 2)</c>, each height a
    /// whole number, as <c>clientHeight</c> is.
    /// </summary>
    internal static double OffsetForMiddle(double toastHeight, double wrapperHeight) =>
        Math.Floor(Math.Round(toastHeight) / 2 - Math.Round(wrapperHeight) / 2);

    /// <summary>
    /// The start of <c>present()</c>, once it's in the host: the anchor and the animation's position, cached for the leave, and the
    /// render.
    /// </summary>
    protected override void OnPresenting()
    {
        _lastPosition = AnimationPosition(Position, AnchorElement());
        RenderButtons();
    }

    /// <summary>
    /// The end of <c>present()</c>: the content shown to screen readers, the duration's timer, and the swipe gesture.
    /// </summary>
    protected override void OnPresented()
    {
        _presented = true;
        Reveal(true);
        // setTimeout takes any number; a TimeSpan doesn't, so one too long for it (infinity, say) never comes.
        if (Duration > 0 && Duration < TimeSpan.MaxValue.TotalMilliseconds)
        {
            _durationTimeout = AvonicConfig.Schedule(() => DismissFromInput("timeout"), TimeSpan.FromMilliseconds(Duration));
        }

        if (PrefersSwipeGesture)
        {
            CreateSwipeGesture(_lastPosition!);
        }
    }

    /// <summary>The start of <c>dismiss()</c>, under the lock: the duration's timer goes.</summary>
    protected override Task<bool> OnDismissing(object? data, string? role)
    {
        _durationTimeout?.Dispose();
        _durationTimeout = null;
        return Task.FromResult(true);
    }

    /// <summary>The end of <c>dismiss()</c>: hidden from screen readers again, the position forgotten, the swipe gesture destroyed.</summary>
    protected override void OnDismissed(bool dismissed)
    {
        if (dismissed)
        {
            Reveal(false);
        }

        _lastPosition = null;
        DestroySwipeGesture();
    }

    // ── The position ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>getAnchorElement</c>: the anchor, or null with none, in the middle (a warning), or when a name isn't found (a
    /// warning). A name is looked up at each present, so the toast follows a page that's changed since.
    /// </summary>
    private Control? AnchorElement()
    {
        if (PositionAnchor is not { } anchor)
        {
            return null;
        }

        if (Position == ToastPosition.Middle)
        {
            Warn("The PositionAnchor property is ignored when using Position Middle.");
            return null;
        }

        if (anchor is string name)
        {
            if (FindByName(name) is { } found)
            {
                return found;
            }

            Warn($"An anchor element with the name \"{name}\" was not found.");
            return null;
        }

        if (anchor is Control control)
        {
            return control;
        }

        Warn($"Invalid PositionAnchor value: {anchor}.");
        return null;
    }

    /// <summary>
    /// The control called <paramref name="name"/>: in the name scope the toast's place is in (its placeholder's, when it's inline and
    /// presented), else the first in its top level, in tree order, as <c>getElementById</c> finds the first in the document.
    /// </summary>
    private Control? FindByName(string name)
    {
        for (StyledElement? element = Placeholder ?? (Parent is OverlayHost ? null : this); element is not null; element = element.Parent)
        {
            if (NameScope.GetNameScope(element) is { } scope)
            {
                if (scope.Find(name) is Control found)
                {
                    return found;
                }

                break;
            }
        }

        return TopLevel.GetTopLevel(this) is { } top
            ? top.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == name && !OverlayHost.Contains(this, c))
            : null;
    }

    private void Warn(string message) =>
        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "[AvonicToast] {Message}", message);

    /// <summary>
    /// Ionic's <c>getAnimationPosition</c>, md: 8 from the edge and the safe area; with an anchor, from the anchor's edge, with no safe
    /// area. At the bottom it's negative, from the bottom of the window.
    /// </summary>
    private ToastPresentOptions AnimationPosition(ToastPosition position, Control? anchor)
    {
        var offset = position == ToastPosition.Top ? EdgeOffset : -EdgeOffset;

        // The anchor's box and the height in the toast's host, which the toast covers, as Ionic's fixed host covers the window that
        // getBoundingClientRect and innerHeight measure. A host not laid out yet is its top level's size.
        if (anchor is not null && this.GetVisualParent() is Visual host)
        {
            if (anchor.TransformToVisual(host) is not { } transform)
            {
                Warn("The PositionAnchor element isn't in the toast's window, so it's ignored.");
            }
            else
            {
                if (!anchor.IsEffectivelyVisible)
                {
                    Warn("The PositionAnchor element was found, but appears to be hidden. This may lead to unexpected positioning of the toast.");
                }

                var height = host.Bounds.Height > 0 ? host.Bounds.Height : TopLevel.GetTopLevel(this)?.ClientSize.Height ?? 0;
                var box = new Rect(anchor.Bounds.Size).TransformToAABB(transform);
                if (position == ToastPosition.Top)
                {
                    offset += box.Bottom;
                }
                else if (position == ToastPosition.Bottom)
                {
                    offset -= height - box.Top;
                }

                return new ToastPresentOptions(position, offset, offset);
            }
        }

        return new ToastPresentOptions(position, EdgeOffset + SafeArea(AppSafeArea.TopKey), -EdgeOffset - SafeArea(AppSafeArea.BottomKey));
    }

    private double SafeArea(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) && value is double d ? d : 0;

    // ── The template and the render ─────────────────────────────────────────────────────────

    protected override AutomationPeer OnCreateAutomationPeer() => new ToastAutomationPeer(this);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        DropAll();
        _panel = e.NameScope.Find<ToastPanel>("PART_Panel");
        _wrapper = e.NameScope.Find<Control>("PART_Wrapper");
        _content = e.NameScope.Find<ToastContent>("PART_Content");
        _header = e.NameScope.Find<TextBlock>("PART_Header");
        _message = e.NameScope.Find<TextBlock>("PART_Message");
        _startPanel = e.NameScope.Find<Panel>("PART_StartButtons");
        _endPanel = e.NameScope.Find<Panel>("PART_EndButtons");
        Reveal(_presented);
        RenderButtons();
    }

    /// <summary>
    /// Ionic's <c>revealContentToScreenReader</c>: until the enter has finished, the header and the message are <c>aria-hidden</c>;
    /// then they're shown, as new elements, so that the live region announces them.
    /// </summary>
    private void Reveal(bool revealed)
    {
        var view = revealed ? AccessibilityView.Content : AccessibilityView.Raw;
        foreach (var part in new Control?[] { _header, _message })
        {
            part?.SetValue(AutomationProperties.AccessibilityViewProperty, view, BindingPriority.Template);
        }

        if (revealed)
        {
            _content?.Revealed();
        }
    }

    /// <summary>The list is followed only while the toast is in a tree; a change while it was out is read when it's back.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _inTree = true;
        WatchButtons(Buttons as INotifyCollectionChanged);
        if (!(Buttons ?? []).SequenceEqual(_seen))
        {
            RenderButtons();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _inTree = false;
        WatchButtons(null);
    }

    private void WatchButtons(INotifyCollectionChanged? next)
    {
        if (_watchedButtons is not null)
        {
            _watchedButtons.CollectionChanged -= OnButtonsCollectionChanged;
        }

        _watchedButtons = next;
        if (_watchedButtons is not null)
        {
            _watchedButtons.CollectionChanged += OnButtonsCollectionChanged;
        }
    }

    // Changes to the list in place come in a batch, as one render after them, as Stencil renders once for many changes.
    private void OnButtonsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_buttonsPending)
        {
            return;
        }

        _buttonsPending = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(RenderButtons);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ButtonsProperty)
        {
            if (_inTree)
            {
                WatchButtons(change.NewValue as INotifyCollectionChanged);
            }

            RenderButtons();
        }
        else if (change.Property == PositionProperty || change.Property == LayoutProperty)
        {
            UpdatePseudoClasses();
        }
        else if (change.Property == ColorProperty)
        {
            var colored = !string.IsNullOrEmpty(Color);
            foreach (var native in ButtonControls)
            {
                native.IsInColor = colored;
            }
        }
        else if (change.Property == ToastSwipeGestureProperty)
        {
            SwipeGestureChanged();
        }
        else if (change.Property == CssClassProperty)
        {
            foreach (var name in _cssClasses)
            {
                Classes.Remove(name);
            }

            _cssClasses = AlertClasses.Apply(this, change.GetNewValue<string?>());
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(PositionTop, Position == ToastPosition.Top);
        PseudoClasses.Set(PositionBottom, Position == ToastPosition.Bottom);
        PseudoClasses.Set(PositionMiddle, Position == ToastPosition.Middle);
        PseudoClasses.Set(LayoutBaseline, Layout == ToastLayout.Baseline);
        PseudoClasses.Set(LayoutStacked, Layout == ToastLayout.Stacked);
    }

    /// <summary>
    /// <c>render</c>'s buttons: the <c>start</c> ones in the start group, the others in the end group, each in order. As Stencil's render
    /// patches the elements that are there, the controls are kept and updated in place, by their place in each group, so that a button
    /// keeps its focus and its press through a render; only the ones past the end go.
    /// </summary>
    private void RenderButtons()
    {
        _buttonsPending = false;
        _seen = Buttons?.ToArray() ?? [];
        if (_startPanel is null || _endPanel is null)
        {
            return;
        }

        var buttons = _seen.Where(b => b is not null).ToList();
        Render(_startPanel, _startControls, buttons.Where(b => b.Side == ToastButtonSide.Start).ToList());
        Render(_endPanel, _endControls, buttons.Where(b => b.Side != ToastButtonSide.Start).ToList());
    }

    private void Render(Panel panel, List<ToastNativeButton> controls, List<ToastButton> buttons)
    {
        while (controls.Count > buttons.Count)
        {
            var gone = controls[^1];
            gone.Clicked -= OnButtonClicked;
            controls.RemoveAt(controls.Count - 1);
            panel.Children.Remove(gone);
        }

        for (var i = 0; i < buttons.Count; i++)
        {
            if (i == controls.Count)
            {
                var control = new ToastNativeButton();
                control.Clicked += OnButtonClicked;
                controls.Add(control);
                panel.Children.Add(control);
            }

            var button = buttons[i];
            var native = controls[i];
            native.Button = button;
            native.Text = button.Text;
            native.Icon = button.Icon;
            native.Role = button.Role;
            native.IsInColor = !string.IsNullOrEmpty(Color);
            if (button.HtmlAttributes is { } attributes && attributes.TryGetValue("aria-label", out var label) && label is not null)
            {
                AutomationProperties.SetName(native, label.ToString());
            }
            else
            {
                native.ClearValue(AutomationProperties.NameProperty);
            }
        }

        // A group with no buttons isn't rendered.
        panel.IsVisible = buttons.Count > 0;
    }

    /// <summary>A new template: the controls go, to be made again in its parts.</summary>
    private void DropAll()
    {
        foreach (var control in ButtonControls)
        {
            control.Clicked -= OnButtonClicked;
        }

        _startControls.Clear();
        _endControls.Clear();
        _startPanel?.Children.Clear();
        _endPanel?.Children.Clear();
    }

    // ── Buttons ─────────────────────────────────────────────────────────────────────────────

    private void OnButtonClicked(object? sender, EventArgs e)
    {
        if (sender is ToastNativeButton { Button: { } button })
        {
            LogFault(ButtonClick(button), "button's handler");
        }
    }

    /// <summary>
    /// <c>buttonClick</c>: a <c>cancel</c> or <c>backdrop</c> role dismisses with it at once (a cancel button's handler then runs from
    /// the will-dismiss listener); otherwise the handler, awaited: false keeps the toast open, anything else dismisses with the button's
    /// role, and no data.
    /// </summary>
    internal async Task ButtonClick(ToastButton button)
    {
        if (OverlayRoles.IsCancel(button.Role))
        {
            await Dismiss(null, button.Role);
            return;
        }

        if (await CallButtonHandler(button))
        {
            await Dismiss(null, button.Role);
        }
    }

    /// <summary><c>callButtonHandler</c>: false when the handler said false; an exception is logged, as <c>printIonError</c>, and is true.</summary>
    private async Task<bool> CallButtonHandler(ToastButton? button)
    {
        if (button?.Handler is not { } handler)
        {
            return true;
        }

        try
        {
            return await OverlayHandler.Run(handler) is not false;
        }
        catch (Exception exception)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Control)?.Log(this, "[AvonicToast] Exception in the button's handler: {Exception}", exception);
            return true;
        }
    }

    /// <summary>
    /// <c>dispatchCancelHandler</c>: a dismiss with a cancel role (a cancel button, or <c>Dismiss</c> with <c>cancel</c> or
    /// <c>backdrop</c>) calls the first <c>cancel</c> button's handler, and doesn't wait for it or read what it returns.
    /// </summary>
    private void OnWillDismissCancel(object? sender, OverlayEventArgs e)
    {
        if (!ReferenceEquals(e.Source, this))
        {
            return;
        }

        _presented = false;
        if (OverlayRoles.IsCancel(e.Role) && Buttons?.FirstOrDefault(b => b?.Role == "cancel") is { } cancel)
        {
            LogFault(CallButtonHandler(cancel), "cancel button's handler");
        }
    }

    /// <summary>
    /// The host: Ionic's has no role, so to automation it's a group, named by <c>htmlAttributes['aria-label']</c> or its own name; its
    /// <c>AutomationId</c> <c>htmlAttributes.id</c>.
    /// </summary>
    private sealed class ToastAutomationPeer(AvonicToast owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override string? GetNameCore() =>
            owner.HtmlAttributes is { } attributes && attributes.TryGetValue("aria-label", out var label) && label?.ToString() is { Length: > 0 } text
                ? text
                : base.GetNameCore();

        protected override string? GetAutomationIdCore() =>
            owner.HtmlAttributes is { } attributes && attributes.TryGetValue("id", out var value) && value?.ToString() is { Length: > 0 } id
                ? id
                : base.GetAutomationIdCore();

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;
    }
}
