using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avonic.Components.Primitives;

/// <summary>
/// A composable touch ripple effect element. Place inside any control template to add
/// Ionic-style touch feedback. Wires automatically to the nearest templated parent's
/// pointer events.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-ripple-effect</c><br/>
/// Rendering approach attributed to Material.Avalonia (MIT) — uses Avalonia Composition API.
/// <para>
/// Usage: drop <c>&lt;AvonicRipple /&gt;</c> inside a <c>ControlTemplate</c>. The element
/// fills its parent, is transparent to hit testing, and self-wires to pointer events.
/// </para>
/// </remarks>
public class AvonicRipple : Control
{
    // ── Styled Properties ─────────────────────────────────────────────────────

    public static readonly StyledProperty<RippleType> RippleTypeProperty =
        AvaloniaProperty.Register<AvonicRipple, RippleType>(
            nameof(RippleType), defaultValue: RippleType.Bounded);

    public static readonly StyledProperty<IBrush> RippleFillProperty =
        AvaloniaProperty.Register<AvonicRipple, IBrush>(
            nameof(RippleFill), inherits: true, defaultValue: Brushes.White);

    public static readonly StyledProperty<double> RippleOpacityProperty =
        AvaloniaProperty.Register<AvonicRipple, double>(
            nameof(RippleOpacity), inherits: true, defaultValue: 0.16);

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<AvonicRipple, CornerRadius>(nameof(CornerRadius));

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Bounded (clips to control, expands from touch origin) or Unbounded (from centre, overflows).</summary>
    public RippleType RippleType
    {
        get => GetValue(RippleTypeProperty);
        set => SetValue(RippleTypeProperty, value);
    }

    /// <summary>Brush used to draw the ripple circle. Inheritable — set on a parent to apply to all children.</summary>
    public IBrush RippleFill
    {
        get => GetValue(RippleFillProperty);
        set => SetValue(RippleFillProperty, value);
    }

    /// <summary>Peak opacity of the ripple at its brightest point. Default is 0.16 (Ionic spec).</summary>
    public double RippleOpacity
    {
        get => GetValue(RippleOpacityProperty);
        set => SetValue(RippleOpacityProperty, value);
    }

    /// <summary>Corner radius used to clip the ripple. Bind to the host control's CornerRadius.</summary>
    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    // ── Static constructor ────────────────────────────────────────────────────

    static AvonicRipple()
    {
        IsHitTestVisibleProperty.OverrideDefaultValue<AvonicRipple>(false);
        HorizontalAlignmentProperty.OverrideDefaultValue<AvonicRipple>(HorizontalAlignment.Stretch);
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicRipple>(VerticalAlignment.Stretch);
    }

    // ── Animation timing (must match AvonicRippleHandler constants) ──────────

    private static readonly TimeSpan ExpandDuration  = TimeSpan.FromMilliseconds(225);
    private static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan CleanupBuffer   = TimeSpan.FromMilliseconds(50);

    // ── Composition state ─────────────────────────────────────────────────────

    private CompositionContainerVisual? _container;
    private CompositionCustomVisual?    _active;
    private int                         _pointers;
    private DateTime                    _pressStart;

    // ── Parent event subscription ─────────────────────────────────────────────

    private InputElement? _subscribedParent;

    // ── Visual tree lifecycle ─────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var thisVisual = ElementComposition.GetElementVisual(this)!;
        _container      = thisVisual.Compositor.CreateContainerVisual();
        _container.Size = new Vector(Bounds.Width, Bounds.Height);
        ElementComposition.SetElementChildVisual(this, _container);

        SubscribeToParent();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        UnsubscribeFromParent();

        _container = null;
        _active    = null;
        ElementComposition.SetElementChildVisual(this, null);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        if (_container is { } c)
        {
            var newSize = new Vector(e.NewSize.Width, e.NewSize.Height);
            if (newSize != default)
            {
                c.Size = newSize;
                foreach (var child in c.Children)
                    child.Size = newSize;
            }
        }
    }

    // ── Parent wiring ─────────────────────────────────────────────────────────

    private void SubscribeToParent()
    {
        var parent = (TemplatedParent ?? Parent) as InputElement;
        if (parent is null) return;

        _subscribedParent = parent;

        parent.AddHandler(InputElement.PointerPressedEvent,     OnParentPointerPressed,
            routes: Avalonia.Interactivity.RoutingStrategies.Direct | Avalonia.Interactivity.RoutingStrategies.Bubble,
            handledEventsToo: true);

        parent.AddHandler(InputElement.PointerReleasedEvent,    OnParentPointerReleased,
            routes: Avalonia.Interactivity.RoutingStrategies.Direct | Avalonia.Interactivity.RoutingStrategies.Bubble,
            handledEventsToo: true);

        parent.AddHandler(InputElement.PointerCaptureLostEvent, OnParentPointerCaptureLost,
            routes: Avalonia.Interactivity.RoutingStrategies.Direct);
    }

    private void UnsubscribeFromParent()
    {
        if (_subscribedParent is null) return;

        _subscribedParent.RemoveHandler(InputElement.PointerPressedEvent,     OnParentPointerPressed);
        _subscribedParent.RemoveHandler(InputElement.PointerReleasedEvent,    OnParentPointerReleased);
        _subscribedParent.RemoveHandler(InputElement.PointerCaptureLostEvent, OnParentPointerCaptureLost);
        _subscribedParent = null;
    }

    // ── Pointer handlers ──────────────────────────────────────────────────────

    private void OnParentPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_container is not { } container) return;

        // Only left/primary button; only first simultaneous pointer
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (Interlocked.CompareExchange(ref _pointers, 1, 0) != 0) return;

        _pressStart = DateTime.UtcNow;
        var pos = e.GetPosition(this);

        // Clamp to bounds — pointer may be slightly outside due to capture
        var x = Math.Clamp(pos.X, 0, Bounds.Width);
        var y = Math.Clamp(pos.Y, 0, Bounds.Height);

        var handler = new AvonicRippleHandler(
            brush:        RippleFill.ToImmutable(),
            opacity:      RippleOpacity,
            cornerRadius: CornerRadius,
            pressX:       x,
            pressY:       y,
            width:        Bounds.Width,
            height:       Bounds.Height,
            unbounded:    RippleType == RippleType.Unbounded);

        var visual = ElementComposition.GetElementVisual(this)!.Compositor
            .CreateCustomVisual(handler);
        visual.Size = new Vector(Bounds.Width, Bounds.Height);

        _active = visual;
        container.Children.Add(visual);
        visual.SendHandlerMessage(AvonicRippleHandler.FirstStepMessage);
    }

    private void OnParentPointerReleased(object? sender, PointerReleasedEventArgs e)
        => EndRipple();

    private void OnParentPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        => EndRipple();

    private void EndRipple()
    {
        if (_active is not { } visual) return;

        Interlocked.Decrement(ref _pointers);
        _active = null;

        visual.SendHandlerMessage(AvonicRippleHandler.SecondStepMessage);

        // Compute how much animation time remains from this moment.
        // A fast tap defers fade-out until the expand finishes (225ms), then fades
        // for 150ms — so worst-case the animation runs 375ms from press start.
        // Removing the composition visual before the fade completes causes a hard cut.
        var elapsed     = DateTime.UtcNow - _pressStart;
        var remaining   = ExpandDuration + FadeOutDuration - elapsed + CleanupBuffer;
        var cleanupDelay = remaining > CleanupBuffer ? remaining : CleanupBuffer;

        var container = _container;
        DispatcherTimer.RunOnce(() =>
        {
            container?.Children.Remove(visual);
        }, cleanupDelay, DispatcherPriority.Render);
    }
}
