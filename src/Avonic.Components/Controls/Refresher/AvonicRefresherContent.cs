using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-refresher-content</c>: what an <see cref="AvonicRefresher"/> shows, the pulling icon while it's pulled and the
/// refreshing spinner while it refreshes. md's native refresher draws both as a round icon with a spinner in it, and hides
/// Ionic's texts, so they aren't here. See <c>docs/components/ion-refresher/README.md</c>.
/// </summary>
/// <remarks>
/// The refresher's state reaches the template as pseudoclasses on this control, <c>:pulling</c>, <c>:ready</c>,
/// <c>:refreshing</c>, <c>:cancelling</c>, <c>:completing</c> and <c>:native</c>, as Ionic's rules are descendant rules of the
/// refresher's classes. The pulling animation's values are set on the parts at animation priority, as a Web Animation's beat
/// every style.
/// </remarks>
[TemplatePart("PART_Pulling", typeof(Control))]
[TemplatePart("PART_PullingIcon", typeof(Control))]
[TemplatePart("PART_SpinnerArrowContainer", typeof(Control))]
[TemplatePart("PART_PullingSpinner", typeof(Control))]
[TemplatePart("PART_PullingCircle", typeof(RefresherCircle))]
[TemplatePart("PART_ArrowContainer", typeof(Control))]
[TemplatePart("PART_Arrow", typeof(Control))]
[TemplatePart("PART_Refreshing", typeof(Control))]
[TemplatePart("PART_RefreshingIcon", typeof(Control))]
[TemplatePart("PART_RefreshingSpinner", typeof(AvonicSpinner))]
[PseudoClasses(":pulling", ":ready", ":refreshing", ":cancelling", ":completing", ":native", ":pulling-circular")]
public class AvonicRefresherContent : TemplatedControl
{
    /// <summary>
    /// Ionic <c>pullingIcon</c>, as a spinner: md's native refresher shows a spinner, and Ionic's icon names are its legacy
    /// refresher's, which isn't here. Left unset it becomes <see cref="SpinnerName.Circular"/> when it's first attached, as Ionic
    /// sets md's in <c>componentWillLoad</c>. <see cref="SpinnerName.Circular"/> also shows md's arrow. Null: none, which in Ionic
    /// is the legacy refresher.
    /// </summary>
    public static readonly StyledProperty<SpinnerName?> PullingIconProperty =
        AvaloniaProperty.Register<AvonicRefresherContent, SpinnerName?>(nameof(PullingIcon));

    /// <summary>
    /// Ionic <c>refreshingSpinner</c>. Left unset it becomes <see cref="AvonicConfig.Spinner"/>, or md's
    /// <see cref="SpinnerName.Circular"/>, when it's first attached, as Ionic sets it in <c>componentWillLoad</c>. Null: none,
    /// which in Ionic is the legacy refresher.
    /// </summary>
    public static readonly StyledProperty<SpinnerName?> RefreshingSpinnerProperty =
        AvaloniaProperty.Register<AvonicRefresherContent, SpinnerName?>(nameof(RefreshingSpinner));

    /// <summary><c>.refresher-refreshing-icon</c>'s <c>transition: 200ms</c>, which md's completing state runs on its scale.</summary>
    internal static readonly TimeSpan IconTransition = TimeSpan.FromMilliseconds(200);

    /// <summary>The transition's timing function, CSS's default <c>ease</c>.</summary>
    private static readonly CubicBezierEasing Ease = CubicBezierEasing.CssEase;

    static AvonicRefresherContent()
    {
        // The pulling icon comes down from above the refresher and goes on below it, and nothing clips it in Ionic. Avalonia
        // clips a templated control by default.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicRefresherContent>(false);
    }

    private bool _didLoad;
    private AvonicRefresher? _refresher;
    private RefresherState _state = RefresherState.Inactive;

    private Control? _pullingIcon;
    private Control? _spinnerArrowContainer;
    private Control? _pullingSpinner;
    private RefresherCircle? _pullingCircle;
    private Control? _arrowContainer;
    private Control? _arrow;
    private Control? _refreshingIcon;
    private AvonicSpinner? _refreshingSpinner;

    private readonly List<IDisposable> _animated = [];
    private IDisposable? _snapBack;
    private IDisposable? _iconScale;
    private IDisposable? _transitionFrame;

    /// <inheritdoc cref="PullingIconProperty"/>
    public SpinnerName? PullingIcon
    {
        get => GetValue(PullingIconProperty);
        set => SetValue(PullingIconProperty, value);
    }

    /// <inheritdoc cref="RefreshingSpinnerProperty"/>
    public SpinnerName? RefreshingSpinner
    {
        get => GetValue(RefreshingSpinnerProperty);
        set => SetValue(RefreshingSpinnerProperty, value);
    }

    /// <summary>
    /// <c>componentWillLoad</c>, once: md's defaults for what's unset. The refresher asks for it too, as Ionic's waits for this
    /// control to be ready before it reads the spinners.
    /// </summary>
    internal void WillLoad()
    {
        if (_didLoad)
        {
            return;
        }

        _didLoad = true;
        if (!IsSet(PullingIconProperty))
        {
            // config.get('refreshingIcon', 'circular') in md, which has no rubber band scrolling
            SetCurrentValue(PullingIconProperty, SpinnerName.Circular);
        }

        if (!IsSet(RefreshingSpinnerProperty))
        {
            SetCurrentValue(RefreshingSpinnerProperty, AvonicConfig.Spinner ?? SpinnerName.Circular);
        }
    }

    /// <summary>Whether the refresher can use md's native refresher: a pulling spinner and a refreshing spinner (<c>shouldUseNativeRefresher</c>).</summary>
    internal bool HasSpinners => PullingIcon is not null && RefreshingSpinner is not null;

    /// <summary>
    /// When the refreshing icon's running transition ends, on <see cref="AvonicConfig.Now"/>, which <see cref="AvonicRefresher"/> waits
    /// for as Ionic's <c>transitionEndAsync</c> does; null when none has run.
    /// </summary>
    internal TimeSpan? IconTransitionEndsAt { get; private set; }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        WillLoad();

        _refresher = this.FindAncestorOfType<AvonicRefresher>();
        if (_refresher is not null)
        {
            _refresher.StateChanged += OnRefresherStateChanged;
        }

        UpdateState();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_refresher is not null)
        {
            _refresher.StateChanged -= OnRefresherStateChanged;
            _refresher = null;
        }

        UpdateState();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        ClearPull();
        ClearSnapBack();
        StopIconScale();

        _pullingIcon = e.NameScope.Find<Control>("PART_PullingIcon");
        _spinnerArrowContainer = e.NameScope.Find<Control>("PART_SpinnerArrowContainer");
        _pullingSpinner = e.NameScope.Find<Control>("PART_PullingSpinner");
        _pullingCircle = e.NameScope.Find<RefresherCircle>("PART_PullingCircle");
        _arrowContainer = e.NameScope.Find<Control>("PART_ArrowContainer");
        _arrow = e.NameScope.Find<Control>("PART_Arrow");
        _refreshingIcon = e.NameScope.Find<Control>("PART_RefreshingIcon");
        _refreshingSpinner = e.NameScope.Find<AvonicSpinner>("PART_RefreshingSpinner");
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PullingIconProperty)
        {
            // mode === 'md' && pullingIcon === 'circular': the circle without its animation, and the arrow
            PseudoClasses.Set(":pulling-circular", PullingIcon == SpinnerName.Circular);
        }
    }

    /// <summary>
    /// <c>animation-delay: -655ms</c> on the refreshing spinner's circle, which md's native refresher sets so that the arc it
    /// starts with is about the one the pull left: its animation begins 655 ms in, from when <c>.refresher-refreshing</c> shows.
    /// </summary>
    internal static readonly TimeSpan RefreshingDelay = TimeSpan.FromMilliseconds(-655);

    /// <summary>
    /// <c>setupMDNativeRefresher</c>'s <c>animation-delay</c> on the refreshing spinner, set only if both spinners have a
    /// <c>circle</c> (the lines have <c>line</c>s), and which moves only <c>circular</c>'s animation, as the other names animate
    /// their <c>svg</c>s, not the <c>circle</c> it's set on. Worked out as <c>.refresher-refreshing</c> is about to show, where the
    /// spinner starts its animation, as CSS does after <c>display: none</c>, and left alone while it shows: Ionic sets it once, so a
    /// spinner changed mid-refresh doesn't jump.
    /// </summary>
    private void UpdateRefreshingDelay(RefresherState previous)
    {
        if (_refreshingSpinner is { } spinner && ShowsRefreshing(_state) && !ShowsRefreshing(previous))
        {
            spinner.AnimationDelay = RefreshingSpinner == SpinnerName.Circular
                                     && PullingIcon is { } pulling
                                     && !SpinnerConfigs.Get(pulling).IsLine
                ? RefreshingDelay
                : TimeSpan.Zero;
        }
    }

    private void OnRefresherStateChanged(object? sender, EventArgs e) => UpdateState();

    private void UpdateState()
    {
        var previous = _state;
        // Ionic's rules for these states that show anything are md's native refresher's; the legacy refresher, which md would use
        // otherwise, isn't here, and it sits behind the content.
        _state = _refresher is { IsNative: true } refresher ? refresher.State : RefresherState.Inactive;
        UpdateRefreshingDelay(previous);

        // The new state's class on before the old one's off, as the class list changes at once in a browser: from refreshing to
        // completing, .refresher-refreshing never has display: none in between, so its spinner carries on rather than start again.
        SetStateClass(RefresherState.Pulling, ":pulling", on: true);
        SetStateClass(RefresherState.Ready, ":ready", on: true);
        SetStateClass(RefresherState.Refreshing, ":refreshing", on: true);
        SetStateClass(RefresherState.Cancelling, ":cancelling", on: true);
        SetStateClass(RefresherState.Completing, ":completing", on: true);
        SetStateClass(RefresherState.Pulling, ":pulling", on: false);
        SetStateClass(RefresherState.Ready, ":ready", on: false);
        SetStateClass(RefresherState.Refreshing, ":refreshing", on: false);
        SetStateClass(RefresherState.Cancelling, ":cancelling", on: false);
        SetStateClass(RefresherState.Completing, ":completing", on: false);
        PseudoClasses.Set(":native", _refresher?.IsNative == true);

        // .refresher-completing .refresher-refreshing-icon { transform: scale(0) }, with transition: 200ms. It transitions only if
        // the icon was showing already: from display: none, there's no style to start from, and it's simply scale(0).
        if (_state == RefresherState.Completing && previous != RefresherState.Completing)
        {
            StartIconScale(animate: ShowsRefreshing(previous));
        }
        else if (_state != RefresherState.Completing)
        {
            StopIconScale();
        }
    }

    /// <summary>The pseudoclass for <paramref name="state"/>, if it's the one <paramref name="on"/> asks to set.</summary>
    private void SetStateClass(RefresherState state, string pseudoClass, bool on)
    {
        if ((_state == state) == on)
        {
            PseudoClasses.Set(pseudoClass, on);
        }
    }

    /// <summary>The states in which md's native refresher shows <c>.refresher-refreshing</c>.</summary>
    internal static bool ShowsRefreshing(RefresherState state) =>
        state is RefresherState.Refreshing or RefresherState.Completing or RefresherState.Cancelling;

    // ── The pulling animation ─────────────────────────────────────────────────────────────────

    /// <summary>Sets the parts to the pulling animation's <paramref name="frame"/>, at animation priority.</summary>
    internal void ApplyPull(RefresherPullFrame frame)
    {
        ClearAnimated();
        if (_snapBack is null)
        {
            Animate(_pullingIcon, RenderTransformProperty, new MatrixTransform(frame.IconTransform));
        }

        Animate(_spinnerArrowContainer, OpacityProperty, frame.SpinnerArrowOpacity);
        Animate(_pullingSpinner, RenderTransformProperty, new RotateTransform(frame.SpinnerRotation));
        Animate(_arrowContainer, RenderTransformProperty, new RotateTransform(frame.ArrowContainerRotation));
        Animate(_arrow, RenderTransformProperty, new MatrixTransform(frame.ArrowTransform));
        if (_pullingCircle is not null)
        {
            _pullingCircle.Dash = frame.CircleDash;
        }
    }

    /// <summary>The pulling animation's <c>destroy()</c>: its values go, and the parts are as the styles leave them.</summary>
    internal void ClearPull()
    {
        ClearAnimated();
        if (_pullingCircle is not null)
        {
            _pullingCircle.Dash = RefresherPullFrame.BaseCircleDash;
        }
    }

    /// <summary>
    /// The snap back's <c>translateY</c> on the pulling icon, which replaces the pulling animation's transform there, as the later
    /// of two animations on one property wins.
    /// </summary>
    internal void ApplySnapBack(double translateY)
    {
        ClearSnapBack();
        _snapBack = _pullingIcon?.SetValue(RenderTransformProperty, new TranslateTransform(0, translateY), BindingPriority.Animation)
                    ?? Disposable.Empty;
    }

    /// <summary>The snap back's <c>destroy()</c>.</summary>
    internal void ClearSnapBack()
    {
        _snapBack?.Dispose();
        _snapBack = null;
    }

    private void ClearAnimated()
    {
        foreach (var value in _animated)
        {
            value.Dispose();
        }

        _animated.Clear();
    }

    private void Animate(Control? part, AvaloniaProperty property, object value)
    {
        if (part?.SetValue(property, value, BindingPriority.Animation) is { } frame)
        {
            _animated.Add(frame);
        }
    }

    // ── The completing transition ─────────────────────────────────────────────────────────────

    private void StartIconScale(bool animate)
    {
        StopIconScale();
        if (_refreshingIcon is not { } icon)
        {
            return;
        }

        if (!animate)
        {
            _iconScale = icon.SetValue(RenderTransformProperty, new ScaleTransform(0, 0), BindingPriority.Animation);
            return;
        }

        var start = AvonicConfig.Now();
        IconTransitionEndsAt = start + IconTransition;
        void Step()
        {
            var t = Math.Min(1, (AvonicConfig.Now() - start) / IconTransition);
            var scale = 1 - Ease.Ease(t);
            _iconScale?.Dispose();
            _iconScale = icon.SetValue(RenderTransformProperty, new ScaleTransform(scale, scale), BindingPriority.Animation);
            _transitionFrame = t < 1 ? AvonicConfig.RequestFrame(icon, Step) : null;
        }

        Step();
    }

    private void StopIconScale()
    {
        IconTransitionEndsAt = null;
        _transitionFrame?.Dispose();
        _transitionFrame = null;
        _iconScale?.Dispose();
        _iconScale = null;
    }

    private sealed class Disposable : IDisposable
    {
        public static readonly Disposable Empty = new();

        public void Dispose()
        {
        }
    }
}
