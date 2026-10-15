using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-ripple-effect</c>: a presentation-only overlay that fills its parent and draws
/// expanding "ink" circles on request. It does not listen for pointer input itself —
/// <see cref="Behaviors.Activatable"/> (Ionic's <c>tap-click</c> utility) calls
/// <see cref="AddRipple"/> on behalf of an activatable surface.
/// </summary>
/// <remarks>
/// Place it in the same <see cref="Panel"/> as the surface's content, beneath it, and set its
/// <see cref="TemplatedControl.Foreground"/> to the surface's ripple colour (Ionic: <c>currentColor</c>).
/// Clipping to rounded corners is the parent's responsibility, exactly as in Ionic.
/// </remarks>
[PseudoClasses(":type-unbounded")]
public class AvonicRippleEffect : TemplatedControl
{
    // ── Ionic constants (ripple-effect.js) ───────────────────────────────────────────────────

    /// <summary><c>PADDING</c>: a bounded ripple's final radius is the host's diagonal plus this.</summary>
    internal const double RipplePadding = 10;

    /// <summary><c>INITIAL_ORIGIN_SCALE</c>: the circle starts at half the host's larger dimension.</summary>
    internal const double InitialOriginScale = 0.5;

    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16);

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>type</c>.</summary>
    public static readonly StyledProperty<RippleEffectType> TypeProperty =
        AvaloniaProperty.Register<AvonicRippleEffect, RippleEffectType>(nameof(Type), RippleEffectType.Bounded);

    /// <summary>
    /// <see cref="RippleEffectType.Bounded"/> (default): the ripple expands from the press position
    /// and is clipped to this control's bounds. <see cref="RippleEffectType.Unbounded"/>: it expands
    /// from the centre and may overflow.
    /// </summary>
    public RippleEffectType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    static AvonicRippleEffect()
    {
        // pointer-events: none
        IsHitTestVisibleProperty.OverrideDefaultValue<AvonicRippleEffect>(false);
        FocusableProperty.OverrideDefaultValue<AvonicRippleEffect>(false);
    }

    public AvonicRippleEffect()
    {
        // role="presentation"
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
        ApplyType(Type);
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private Canvas?          _container;
    private DispatcherTimer? _timer;

    /// <summary>Live ripple circles, oldest first.</summary>
    internal IReadOnlyList<RippleVisual> Ripples =>
        _container?.Children.OfType<RippleVisual>().ToList() ?? (IReadOnlyList<RippleVisual>)Array.Empty<RippleVisual>();

    // ── Ionic addRipple(x, y) ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a ripple starting at (<paramref name="x"/>, <paramref name="y"/>) in this control's
    /// coordinate space. Returns the <b>release</b> action: invoke it when the press ends to
    /// fade the ripple out. As in Ionic, fade-out never begins before 325 ms after the ripple
    /// started, however early the release is invoked.
    /// </summary>
    public Action AddRipple(double x, double y)
    {
        if (_container is null)
            ApplyTemplate();

        var width  = Bounds.Width;
        var height = Bounds.Height;
        var maxDim = Math.Max(width, height);
        var initialSize = Math.Floor(maxDim * InitialOriginScale);

        if (_container is null || initialSize <= 0)
            return static () => { };

        var unbounded  = Type == RippleEffectType.Unbounded;
        var hypotenuse = Math.Sqrt(width * width + height * height);
        var maxRadius  = unbounded ? maxDim : hypotenuse + RipplePadding;
        var finalScale = maxRadius / initialSize;

        var posX = unbounded ? width  * 0.5 : x;
        var posY = unbounded ? height * 0.5 : y;

        var ripple = new RippleVisual
        {
            Width        = initialSize,
            Height       = initialSize,
            InitialSize  = initialSize,
            FinalScale   = finalScale,
            TranslateEnd = new Vector(width * 0.5 - posX, height * 0.5 - posY),
            StartedAt    = AvonicConfig.Now(),
        };
        Canvas.SetLeft(ripple, posX - initialSize * 0.5);
        Canvas.SetTop(ripple,  posY - initialSize * 0.5);
        ripple[!RippleVisual.FillProperty] = this[!ForegroundProperty];   // currentColor

        _container.Children.Add(ripple);
        ripple.Update(ripple.StartedAt);
        EnsureTimer();

        var released = false;
        return () =>
        {
            if (released) return;
            released = true;
            ripple.ReleaseRequestedAt = AvonicConfig.Now();
            EnsureTimer();
            Tick();
        };
    }

    /// <summary>Whether the frame timer is running — only while some ripple is still changing.</summary>
    internal bool IsFrameTimerRunning => _timer is not null;

    /// <summary>Advance every ripple to the current clock, removing finished ones.</summary>
    internal void Tick()
    {
        if (_container is null) return;

        var now = AvonicConfig.Now();
        var allAtRest = true;
        for (var i = _container.Children.Count - 1; i >= 0; i--)
        {
            if (_container.Children[i] is not RippleVisual ripple) continue;
            ripple.Update(now);
            if (ripple.IsRemoved)
                _container.Children.RemoveAt(i);
            else if (!ripple.IsAtRest)
                allAtRest = false;
        }

        // A held press idles at the end state, as Ionic's `forwards` animations do; the release
        // action restarts the timer.
        if (allAtRest)
            StopTimer();
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _container = e.NameScope.Find<Canvas>("PART_Container");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TypeProperty)
            ApplyType(change.GetNewValue<RippleEffectType>());
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopTimer();
        _container?.Children.Clear();
    }

    private void ApplyType(RippleEffectType type)
    {
        // contain: strict (bounded) vs contain: layout size style (unbounded) — paint containment
        // is what clips a bounded ripple to the host.
        ClipToBounds = type == RippleEffectType.Bounded;
        PseudoClasses.Set(":type-unbounded", type == RippleEffectType.Unbounded);   // Ionic host class `unbounded`
    }

    private void EnsureTimer()
    {
        if (_timer is not null) return;
        _timer = new DispatcherTimer(FrameInterval, DispatcherPriority.Render, (_, _) => Tick());
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }
}
