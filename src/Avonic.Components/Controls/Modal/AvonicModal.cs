using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>ion-modal</c>, md: full screen on a phone and a centred dialog on a tablet, over a backdrop. Present it in code
/// (<c>new AvonicModal { Content = … }.Present()</c>), or declare it in a page and open it with <see cref="AvonicOverlay.IsOpen"/> or
/// <see cref="AvonicOverlay.Trigger"/>: it's then moved to the overlay layer while it's presented, as Ionic teleports an inline
/// modal. See <c>docs/components/ion-modal/README.md</c>.
/// </summary>
[TemplatePart("PART_Backdrop", typeof(AvonicBackdrop))]
[TemplatePart("PART_Content", typeof(ModalWrapper))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[PseudoClasses(InsetSmall, InsetLarge)]
public class AvonicModal : AvonicOverlay
{
    /// <summary>
    /// <c>@media (min-width: 768px) and (min-height: 600px)</c>, on the top level's client size, named after Ionic's
    /// <c>$modal-inset-min-height-small</c>.
    /// </summary>
    public const string InsetSmall = ":inset-small";

    /// <summary><c>@media (min-width: 768px) and (min-height: 768px)</c>, after <c>$modal-inset-min-height-large</c>.</summary>
    public const string InsetLarge = ":inset-large";

    /// <summary>Ionic's <c>$modal-inset-min-width</c>.</summary>
    internal const double InsetMinWidth = 768;

    /// <summary>Ionic's <c>$modal-inset-min-height-small</c>.</summary>
    internal const double InsetMinHeightSmall = 600;

    /// <summary>Ionic's <c>$modal-inset-min-height-large</c>.</summary>
    internal const double InsetMinHeightLarge = 768;

    /// <summary><c>EDGE_THRESHOLD</c> in <c>safe-area-utils.js</c>: how near an edge the wrapper has to be to reach it.</summary>
    internal const double EdgeThreshold = 5;

    /// <summary><c>--backdrop-opacity</c>'s md fallback, <c>var(--ion-backdrop-opacity, 0.32)</c>.</summary>
    internal const double DefaultBackdropOpacity = 0.32;

    /// <summary>Ionic <c>backdropDismiss</c>: a tap on the backdrop, Escape and back dismiss it.</summary>
    public static readonly StyledProperty<bool> BackdropDismissProperty =
        AvaloniaProperty.Register<AvonicModal, bool>(nameof(BackdropDismiss), defaultValue: true);

    /// <summary>Ionic <c>showBackdrop</c>: whether the backdrop darkens the screen. It's there and takes a tap either way.</summary>
    public static readonly StyledProperty<bool> ShowBackdropProperty =
        AvaloniaProperty.Register<AvonicModal, bool>(nameof(ShowBackdrop), defaultValue: true);

    /// <summary>Ionic <c>focusTrap</c>.</summary>
    public static readonly StyledProperty<bool> FocusTrapProperty =
        AvaloniaProperty.Register<AvonicModal, bool>(nameof(FocusTrap), defaultValue: true);

    /// <summary>Ionic <c>canDismiss</c>: a <see cref="bool"/>, or a function of the dismiss's data and role.</summary>
    public static readonly StyledProperty<ModalCanDismiss> CanDismissProperty =
        AvaloniaProperty.Register<AvonicModal, ModalCanDismiss>(nameof(CanDismiss), defaultValue: ModalCanDismiss.True);

    /// <summary>
    /// Ionic <c>keepContentsMounted</c>: the content is in the visual tree from when the modal is attached, and stays after a
    /// dismiss. Otherwise it's there only while the modal is presented, as a framework mounts an inline modal's content.
    /// </summary>
    public static readonly StyledProperty<bool> KeepContentsMountedProperty =
        AvaloniaProperty.Register<AvonicModal, bool>(nameof(KeepContentsMounted));

    /// <summary>The default slot.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        ContentControl.ContentProperty.AddOwner<AvonicModal>();

    /// <summary>The template for <see cref="Content"/> when it isn't a control.</summary>
    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        ContentControl.ContentTemplateProperty.AddOwner<AvonicModal>();

    /// <summary>Ionic <c>ionModalWillPresent</c>, raised before <see cref="AvonicOverlay.WillPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonModalWillPresentEvent =
        RoutedEvent.Register<AvonicModal, RoutedEventArgs>(nameof(IonModalWillPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionModalDidPresent</c>, raised before <see cref="AvonicOverlay.DidPresent"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IonModalDidPresentEvent =
        RoutedEvent.Register<AvonicModal, RoutedEventArgs>(nameof(IonModalDidPresent), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionModalWillDismiss</c>, raised before <see cref="AvonicOverlay.WillDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonModalWillDismissEvent =
        RoutedEvent.Register<AvonicModal, OverlayEventArgs>(nameof(IonModalWillDismiss), RoutingStrategies.Bubble);

    /// <summary>Ionic <c>ionModalDidDismiss</c>, raised before <see cref="AvonicOverlay.DidDismiss"/>.</summary>
    public static readonly RoutedEvent<OverlayEventArgs> IonModalDidDismissEvent =
        RoutedEvent.Register<AvonicModal, OverlayEventArgs>(nameof(IonModalDidDismiss), RoutingStrategies.Bubble);


    private static readonly string[] SafeAreaKeys = ["Avonic.SafeAreaTop", "Avonic.SafeAreaBottom", "Avonic.SafeAreaLeft", "Avonic.SafeAreaRight"];

    /// <summary>The safe-area overrides, merged into <see cref="StyledElement.Resources"/>, so an app's own key there wins.</summary>
    private readonly ResourceDictionary _safeArea = new();

    private AvonicBackdrop? _backdrop;
    private ModalWrapper? _wrapper;
    private ContentPresenter? _presenter;
    private bool _mounted;
    private TopLevel? _topLevel;

    /// <summary>The modal this one was inside when it was presented: Ionic's <c>data-parent-ion-modal</c>.</summary>
    private AvonicModal? _parentModal;

    /// <summary>Which edges the wrapper doesn't reach, top, bottom, left, right: their safe area is 0.</summary>
    private (bool Top, bool Bottom, bool Left, bool Right) _zeroEdges;

    public AvonicModal()
    {
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

    public ModalCanDismiss CanDismiss
    {
        get => GetValue(CanDismissProperty);
        set => SetValue(CanDismissProperty, value);
    }

    public bool KeepContentsMounted
    {
        get => GetValue(KeepContentsMountedProperty);
        set => SetValue(KeepContentsMountedProperty, value);
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

    public event EventHandler<RoutedEventArgs>? IonModalWillPresent
    {
        add => AddHandler(IonModalWillPresentEvent, value);
        remove => RemoveHandler(IonModalWillPresentEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? IonModalDidPresent
    {
        add => AddHandler(IonModalDidPresentEvent, value);
        remove => RemoveHandler(IonModalDidPresentEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonModalWillDismiss
    {
        add => AddHandler(IonModalWillDismissEvent, value);
        remove => RemoveHandler(IonModalWillDismissEvent, value);
    }

    public event EventHandler<OverlayEventArgs>? IonModalDidDismiss
    {
        add => AddHandler(IonModalDidDismissEvent, value);
        remove => RemoveHandler(IonModalDidDismissEvent, value);
    }

    /// <summary>Whether the content is in the visual tree: presented, or kept mounted.</summary>
    internal bool IsContentMounted => _mounted;

    /// <summary>The wrapper, the <c>content</c> part.</summary>
    internal ModalWrapper? Wrapper => _wrapper;

    /// <summary>The backdrop part.</summary>
    internal AvonicBackdrop? Backdrop => _backdrop;

    protected internal override bool BackdropDismissEnabled => BackdropDismiss;

    protected internal override bool FocusTrapEnabled => FocusTrap;

    protected override RoutedEvent<RoutedEventArgs> ControlWillPresentEvent => IonModalWillPresentEvent;

    protected override RoutedEvent<RoutedEventArgs> ControlDidPresentEvent => IonModalDidPresentEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlWillDismissEvent => IonModalWillDismissEvent;

    protected override RoutedEvent<OverlayEventArgs> ControlDidDismissEvent => IonModalDidDismissEvent;

    /// <summary>The wrapper, which has <c>role="dialog"</c> and <c>tabindex="-1"</c>, as Ionic's present focuses it.</summary>
    protected override IInputElement? PresentFocusTarget => _wrapper;

    /// <summary><c>md.enter.js</c>: 280 ms, the backdrop to <c>--backdrop-opacity</c>, the wrapper up 40 px as it appears.</summary>
    protected override AnimationBuilder MdEnterAnimation => (baseElement, _) =>
    {
        var modal = (AvonicModal)baseElement;
        var backdrop = AvonicAnimation.Create()
            .AddElement(modal._backdrop)
            .FromTo("opacity", 0.01, modal.BackdropOpacity())
            .BeforeStyles(new() { ["pointer-events"] = "none" })
            .AfterClearStyles(["pointer-events"]);
        var wrapper = AvonicAnimation.Create()
            .AddElement(modal._wrapper)
            .Keyframes([
                new AnimationKeyframe(0) { ["opacity"] = 0.01, ["transform"] = "translateY(40px)" },
                new AnimationKeyframe(1) { ["opacity"] = 1, ["transform"] = "translateY(0px)" },
            ]);
        return AvonicAnimation.Create()
            .AddElement(baseElement)
            .Easing("cubic-bezier(0.36,0.66,0.04,1)")
            .Duration(280)
            .AddAnimation(backdrop, wrapper);
    };

    /// <summary><c>md.leave.js</c>: 200 ms, the backdrop to 0, the wrapper down 40 px as it fades.</summary>
    protected override AnimationBuilder MdLeaveAnimation => (baseElement, _) =>
    {
        var modal = (AvonicModal)baseElement;
        var backdrop = AvonicAnimation.Create()
            .AddElement(modal._backdrop)
            .FromTo("opacity", modal.BackdropOpacity(), 0);
        var wrapper = AvonicAnimation.Create()
            .AddElement(modal._wrapper)
            .Keyframes([
                new AnimationKeyframe(0) { ["opacity"] = 0.99, ["transform"] = "translateY(0px)" },
                new AnimationKeyframe(1) { ["opacity"] = 0, ["transform"] = "translateY(40px)" },
            ]);
        return AvonicAnimation.Create()
            .Easing("cubic-bezier(0.47,0,0.745,0.715)")
            .Duration(200)
            .AddAnimation(backdrop, wrapper);
    };

    /// <summary>
    /// <c>var(--backdrop-opacity)</c>: <c>Avonic.Modal.BackdropOpacity</c>, falling back to <c>Avonic.BackdropOpacity</c>
    /// (<c>--ion-backdrop-opacity</c>), then 0.32.
    /// </summary>
    internal double BackdropOpacity()
    {
        foreach (var key in (string[])["Avonic.Modal.BackdropOpacity", "Avonic.BackdropOpacity"])
        {
            if (this.TryFindResource(key, ActualThemeVariant, out var value) && Css.IsSet(value) && value is double or float or int)
            {
                return Convert.ToDouble(value);
            }
        }

        return DefaultBackdropOpacity;
    }

    /// <summary>
    /// Ionic's own <c>present()</c> before the shared one: the modal it's inside is noted (<c>data-parent-ion-modal</c>), and its
    /// content is mounted (<c>attachComponent</c>).
    /// </summary>
    protected override void OnPresenting()
    {
        _parentModal = this.GetLogicalAncestors().OfType<AvonicModal>().FirstOrDefault();
        Mount(true);
    }

    /// <summary>
    /// Ionic's own <c>dismiss()</c> before the shared one: the modals inside it are dismissed, not awaited, then
    /// <see cref="CanDismiss"/> decides, unless its handler is dismissing it.
    /// </summary>
    protected override async Task<bool> OnDismissing(object? data, string? role)
    {
        // Ionic queries the document; a nested inline modal is presented in the same top level, so in the same host.
        var presented = Parent is Panel host ? host.Children.OfType<AvonicModal>() : [];
        foreach (var nested in presented.Where(m => m._parentModal == this).ToList())
        {
            nested.DismissFromInput(OverlayRoles.ParentDismissed);
        }

        return role == OverlayRoles.Handler || await CanDismiss.Check(data, role);
    }

    /// <summary>Ionic's own <c>dismiss()</c> after the shared one: the content is unmounted (<c>detachComponent</c>).</summary>
    protected override void OnDismissed(bool dismissed)
    {
        if (!dismissed)
        {
            return;
        }

        _parentModal = null;
        if (!KeepContentsMounted)
        {
            Mount(false);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _backdrop = e.NameScope.Find<AvonicBackdrop>("PART_Backdrop");
        if (_wrapper is not null)
        {
            _wrapper.PropertyChanged -= OnWrapperPropertyChanged;
        }

        _wrapper = e.NameScope.Find<ModalWrapper>("PART_Content");
        if (_wrapper is not null)
        {
            _wrapper.PropertyChanged += OnWrapperPropertyChanged;
        }

        if (_presenter is not null)
        {
            _presenter.Content = null;
        }

        _presenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        UpdatePresenter();
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
        else if (change.Property == BoundsProperty)
        {
            UpdateSafeArea();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The media queries matter only while it's shown, which is while it's in the overlay host.
        if (Parent is OverlayHost)
        {
            _topLevel = TopLevel.GetTopLevel(this);
            _topLevel?.PropertyChanged += OnTopLevelPropertyChanged;
            UpdateInset();
        }

        // keepContentsMounted: mounted from creation, which here is the first attach to a page.
        if (KeepContentsMounted)
        {
            ApplyTemplate();
            Mount(true);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel?.PropertyChanged -= OnTopLevelPropertyChanged;
        _topLevel = null;
    }

    /// <summary>The wrapper's box changed, or the host's: which edges it reaches may have too.</summary>
    private void OnWrapperPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
        {
            UpdateSafeArea();
        }
    }

    /// <summary>
    /// <c>getPositionBasedSafeAreaConfig</c>: an edge the wrapper's box (untransformed, as Ionic's prediction and its read after
    /// the animation agree on) doesn't reach within 5 px gets <c>0px</c>; one it reaches inherits. Measured in its panel,
    /// which covers the window, as Ionic measures against the viewport.
    /// </summary>
    private void UpdateSafeArea()
    {
        if (_wrapper is not { Parent: Visual panel })
        {
            return;
        }

        var wrapper = _wrapper.Bounds;
        var viewport = panel.Bounds.Size;
        var zero = (
            Top: wrapper.Top > EdgeThreshold,
            Bottom: wrapper.Bottom < viewport.Height - EdgeThreshold,
            Left: wrapper.Left > EdgeThreshold,
            Right: wrapper.Right < viewport.Width - EdgeThreshold);
        if (zero == _zeroEdges)
        {
            return;
        }

        _zeroEdges = zero;
        bool[] edges = [zero.Top, zero.Bottom, zero.Left, zero.Right];
        for (var i = 0; i < SafeAreaKeys.Length; i++)
        {
            if (edges[i])
            {
                _safeArea[SafeAreaKeys[i]] = 0d;
            }
            else
            {
                _safeArea.Remove(SafeAreaKeys[i]);
            }
        }
    }

    private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty)
        {
            UpdateInset();
        }
    }

    /// <summary>The two media queries of <c>modal.md.css</c>, on the window's size.</summary>
    private void UpdateInset()
    {
        var size = _topLevel?.ClientSize ?? default;
        PseudoClasses.Set(InsetSmall, size.Width >= InsetMinWidth && size.Height >= InsetMinHeightSmall);
        PseudoClasses.Set(InsetLarge, size.Width >= InsetMinWidth && size.Height >= InsetMinHeightLarge);
    }

    private void Mount(bool mounted)
    {
        _mounted = mounted;
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
    }
}
