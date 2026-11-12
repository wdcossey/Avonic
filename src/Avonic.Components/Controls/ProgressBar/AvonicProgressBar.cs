using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-progress-bar</c>: a bar showing how far a process has got, or, when that isn't known, two bars sliding
/// across. The look is in the theme. See <c>docs/components/ion-progress-bar/README.md</c>.
/// </summary>
/// <remarks>
/// Every moving part is a <c>transform</c> in Ionic, so here every part is a full-size child of the template's panel, and
/// the control sets its <see cref="Visual.RenderTransform"/> from <see cref="AvonicConfig"/>'s clock. Nothing is
/// re-measured by an animation frame.
/// </remarks>
[TemplatePart("PART_Stream", typeof(ProgressBarStream))]
[TemplatePart("PART_Track", typeof(Control))]
[TemplatePart("PART_Progress", typeof(Control))]
[TemplatePart("PART_ProgressPrimary", typeof(Control))]
[TemplatePart("PART_ProgressSecondary", typeof(Control))]
[PseudoClasses(":type-determinate", ":type-indeterminate", ":reversed", ":solid")]
public class AvonicProgressBar : TemplatedControl
{
    // ── Ionic constants (progress-bar.md.css) ────────────────────────────────────────────────

    /// <summary><c>transition: transform 150ms linear</c> on the progress, the track and the stream's containers.</summary>
    internal static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(150);

    /// <summary>The four indeterminate <c>@keyframes</c>: <c>2s infinite linear</c>.</summary>
    internal static readonly TimeSpan IndeterminateDuration = TimeSpan.FromSeconds(2);

    /// <summary><c>animation: buffering 450ms infinite linear</c>, which moves the dots one <see cref="ProgressBarStream.Pitch"/>.</summary>
    internal static readonly TimeSpan BufferingDuration = TimeSpan.FromMilliseconds(450);

    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic <c>color</c>.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicProgressBar>();

    /// <summary>Ionic <c>type</c>.</summary>
    public static readonly StyledProperty<ProgressBarType> TypeProperty =
        AvaloniaProperty.Register<AvonicProgressBar, ProgressBarType>(nameof(Type));

    /// <summary>Ionic <c>reversed</c>.</summary>
    public static readonly StyledProperty<bool> ReversedProperty =
        AvaloniaProperty.Register<AvonicProgressBar, bool>(nameof(Reversed));

    /// <summary>Ionic <c>value</c>.</summary>
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<AvonicProgressBar, double>(nameof(Value));

    /// <summary>Ionic <c>buffer</c>.</summary>
    public static readonly StyledProperty<double> BufferProperty =
        AvaloniaProperty.Register<AvonicProgressBar, double>(nameof(Buffer), 1);

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    /// <see cref="ProgressBarType.Determinate"/> (default) shows <see cref="Value"/>; <see cref="ProgressBarType.Indeterminate"/>
    /// slides two bars across on a 2 s loop.
    /// </summary>
    public ProgressBarType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <summary>Mirrors the bar, so that it fills from the end. In right-to-left it fills from the left again, as in Ionic.</summary>
    public bool Reversed
    {
        get => GetValue(ReversedProperty);
        set => SetValue(ReversedProperty, value);
    }

    /// <summary>How much of the bar is filled, from 0 to 1. Drawn clamped to that range, and reported to accessibility as it is.</summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>How far the track goes, from 0 to 1. Under 1, a stream of dots fills the rest. Drawn clamped to that range.</summary>
    public double Buffer
    {
        get => GetValue(BufferProperty);
        set => SetValue(BufferProperty, value);
    }

    public AvonicProgressBar()
    {
        _hiddenChain = new HiddenChainWatcher(Restart);

        // :host(.progress-bar-reversed) { transform: scaleX(-1) }. At style priority, so that an app's transform replaces it,
        // as an outer stylesheet's does in CSS. Bound once, here, before any style applies: of two values at one priority the
        // later wins, so a frame made when the bar is reversed would beat an app's style that was already there.
        Bind(RenderTransformProperty, _reversal, BindingPriority.Style);
        UpdatePseudoClasses();
    }

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private Control? _root;
    private ProgressBarStream? _stream;
    private Control? _track;
    private Control? _progress;
    private Control? _primary;
    private Control? _secondary;

    /// <summary>
    /// The first element Ionic renders, which Stencil keeps across a change of <c>type</c>: the progress when determinate,
    /// the track when indeterminate. So a change of type eases from where that element was, as measured.
    /// </summary>
    private readonly CssTransition _lead = new(CssTransition.Linear, double.NaN);

    /// <summary>The track and the stream's window, when determinate. New elements on a change of type, so they don't ease then.</summary>
    private readonly CssTransition _buffer = new(CssTransition.Linear, double.NaN);

    private bool _attached;
    private TimeSpan _renderedAt;
    private IDisposable? _frame;
    private IDisposable? _firstFrame;

    /// <summary>
    /// <c>display: none</c>, on the bar or an ancestor, stops an element's CSS animations, and showing it starts them again
    /// from 0. So a frame that finds the bar hidden, or an attach while it's hidden, stops the frames and watches its chain
    /// until it shows. A hide and show between two frames isn't seen, as in Chromium, measured, where no style change sees it.
    /// </summary>
    private readonly HiddenChainWatcher _hiddenChain;
    // scaleX(-1) while the bar is reversed, and nothing otherwise.
    private readonly StyleValue<ITransform?> _reversal = new();
    private readonly ScaleTransform _mirror = new(-1, 1);
    private ProgressBarAutomationPeer? _peer;

    /// <summary>Whether a frame is requested: only while something moves.</summary>
    internal bool IsFrameLoopRunning => _frame is not null;

    /// <summary>Whether it waits for something hidden in its chain to show.</summary>
    internal bool IsWaitingUntilShown => _hiddenChain.IsWaiting;

    /// <summary>Whether the bar is drawn mirrored: Ionic's <c>progress-bar-reversed</c>.</summary>
    internal bool IsEffectivelyReversed => FlowDirection == FlowDirection.RightToLeft ? !Reversed : Reversed;

    /// <summary>
    /// <c>clamp(0, value, 1)</c>, for an element Ionic creates. NaN stays NaN in <c>clamp</c>, and <c>scaleX(NaN)</c> is an
    /// invalid transform, so a new element is drawn untransformed: at 1.
    /// </summary>
    private static double Display(double value) => double.IsNaN(value) ? 1 : Math.Clamp(value, 0, 1);

    /// <summary>
    /// The same for an element Ionic keeps: setting <c>style.transform</c> to an invalid value is ignored, so NaN keeps the
    /// scale it had, measured as the inline style staying.
    /// </summary>
    private static double Display(double value, CssTransition kept) =>
        double.IsNaN(value) && !double.IsNaN(kept.Target) ? kept.Target : Display(value);

    private double LeadTarget => Type == ProgressBarType.Determinate ? Display(Value, _lead) : 1;

    // ── Lifecycle ────────────────────────────────────────────────────────────────────────────

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _stream = e.NameScope.Find<ProgressBarStream>("PART_Stream");
        _track = e.NameScope.Find<Control>("PART_Track");
        _progress = e.NameScope.Find<Control>("PART_Progress");
        _primary = e.NameScope.Find<Control>("PART_ProgressPrimary");
        _secondary = e.NameScope.Find<Control>("PART_ProgressSecondary");

        // :host { direction: ltr }. The parts are laid out left to right whatever the app's direction, and right-to-left
        // reverses the bar through IsEffectivelyReversed instead, as Ionic's progress-bar-reversed does.
        if (_root is not null)
        {
            _root.SizeChanged -= OnRootSizeChanged;
        }

        _root = this.GetVisualChildren().OfType<Control>().FirstOrDefault();
        if (_root is not null)
        {
            // The parts are placed in px from its width, which is 0 until it's arranged, even when a template is applied
            // again at the bar's own size
            _root.SizeChanged += OnRootSizeChanged;
        }

        _root?.SetValue(FlowDirectionProperty, FlowDirection.LeftToRight, BindingPriority.Template);

        foreach (var part in new[] { _track, _progress, _primary, _secondary })
        {
            if (part is not null)
            {
                part.RenderTransformOrigin = RelativePoint.TopLeft;
                part.RenderTransform = new MatrixTransform();
            }
        }

        Update();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;

        // A first render, or a re-created element: nothing eases, and the animations start at 0.
        Restart();
    }

    private void Restart()
    {
        _hiddenChain.Dispose();
        _renderedAt = AvonicConfig.Now();

        // Stencil's first render is at the next frame, so a change before it is drawn without a transition, measured
        _firstFrame?.Dispose();
        _firstFrame = AvonicConfig.RequestFrame(this, () => _firstFrame = null);
        _lead.Snap(LeadTarget);
        _buffer.Snap(Display(Buffer, _buffer));
        Update();
    }

    private void StopFrames()
    {
        _frame?.Dispose();
        _frame = null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        StopFrames();
        _firstFrame?.Dispose();
        _firstFrame = null;
        _hiddenChain.Dispose();
    }

    private void OnRootSizeChanged(object? sender, SizeChangedEventArgs e) => Update();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TypeProperty)
        {
            // Stencil re-renders: the indeterminate bars, the track and the stream are new elements, so their animations start
            // at 0 and they don't ease. The first element is kept, so it eases to its new scale.
            _renderedAt = AvonicConfig.Now();
            _buffer.Snap(Display(Buffer));
            Ease(_lead, LeadTarget);
            UpdatePseudoClasses();
            Update();
        }
        else if (change.Property == ValueProperty)
        {
            Ease(_lead, LeadTarget);
            _peer?.RaiseValueChanged(change.GetOldValue<double>(), change.GetNewValue<double>());
            Update();
        }
        else if (change.Property == BufferProperty)
        {
            // While indeterminate it goes unseen, and the change of type snaps it
            Ease(_buffer, Display(Buffer, _buffer));
            UpdatePseudoClasses();
            Update();
        }
        else if (change.Property == ReversedProperty || change.Property == FlowDirectionProperty)
        {
            UpdatePseudoClasses();
        }
    }

    /// <summary>
    /// A transition, unless the bar hasn't been drawn since it attached or showed. One started while the bar is detached or
    /// hidden is snapped when it attaches or shows, as both restart it. <see cref="AvonicConfig.Animated"/> doesn't stop it, as
    /// Ionic's <c>animated</c> doesn't reach this control's CSS.
    /// </summary>
    private void Ease(CssTransition transition, double target)
    {
        if (_firstFrame is null)
        {
            transition.Start(target, AvonicConfig.Now(), TransitionDuration);
        }
        else
        {
            transition.Snap(target);
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":type-determinate", Type == ProgressBarType.Determinate);
        PseudoClasses.Set(":type-indeterminate", Type == ProgressBarType.Indeterminate);
        // progress-bar-solid: buffer === 1, before clamping
        PseudoClasses.Set(":solid", Buffer == 1);

        var reversed = IsEffectivelyReversed;
        PseudoClasses.Set(":reversed", reversed);

        _reversal.SetWhen(reversed, _mirror);
    }

    // ── Frames ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Draws the parts at the clock's current time. Tests call it to read an exact time between frames.</summary>
    internal void Tick() => Update();

    private void OnFrame()
    {
        _frame = null;
        Update();
    }

    private void Update()
    {
        if (_root is null)
        {
            // Attached while it or an ancestor is hidden: Avalonia doesn't measure it, so it has no template yet, and showing
            // it has to start the animations then
            if (_attached && !IsEffectivelyVisible)
            {
                _hiddenChain.Watch(this);
            }

            return;
        }

        var now = AvonicConfig.Now();
        var running = _attached && IsEffectivelyVisible;

        // The animations run whatever AvonicConfig.Animated says, as Ionic's animated config doesn't reach them: they're the
        // user's only sign that something is happening. Detached or hidden, nothing shows, and attaching or showing restarts them.
        var elapsed = now - _renderedAt;
        var width = _root.Bounds.Width;
        var moving = running && (_lead.IsRunning(now) || _buffer.IsRunning(now));
        var determinate = Type == ProgressBarType.Determinate;

        SetVisible(_progress, determinate);
        SetVisible(_stream, determinate);
        SetVisible(_primary, !determinate);
        SetVisible(_secondary, !determinate);

        if (determinate)
        {
            var buffer = _buffer.At(now);
            SetScale(_progress, _lead.At(now));
            SetScale(_track, buffer);
            if (_stream is not null)
            {
                _stream.WindowStart = buffer * width;
                _stream.Phase = Fraction(elapsed, BufferingDuration) * ProgressBarStream.Pitch;
            }

            // Only while the dots show. Their phase is measured from the render, so they come back where Ionic's would be.
            moving |= running && buffer < 1;
        }
        else
        {
            // The two bars are inside the track in Ionic, so the track's ease after a change of type scales them too
            var track = _lead.At(now);
            SetScale(_track, track);
            var t = Fraction(elapsed, IndeterminateDuration);
            var (primaryTranslate, primaryScale) = IndeterminateKeyframes.Primary(t);
            var (secondaryTranslate, secondaryScale) = IndeterminateKeyframes.Secondary(t);
            SetSpan(_primary, IndeterminateKeyframes.PrimaryLeft + primaryTranslate, primaryScale, width, track);
            SetSpan(_secondary, IndeterminateKeyframes.SecondaryLeft + secondaryTranslate, secondaryScale, width, track);
            moving |= running;
        }

        if (moving)
        {
            _frame ??= AvonicConfig.RequestFrame(this, OnFrame);
        }
        else if (_attached && !IsEffectivelyVisible)
        {
            _hiddenChain.Watch(this);
        }
    }

    private static double Fraction(TimeSpan elapsed, TimeSpan period) =>
        elapsed.Ticks % period.Ticks / (double)period.Ticks;

    private static void SetVisible(Control? part, bool visible)
    {
        if (part is not null)
        {
            part.IsVisible = visible;
        }
    }

    /// <summary><c>transform-origin: left top; transform: scaleX(scale)</c>.</summary>
    private static void SetScale(Control? part, double scale)
    {
        if (part?.RenderTransform is MatrixTransform transform)
        {
            transform.Matrix = new Matrix(scale, 0, 0, 1, 0, 0);
        }
    }

    /// <summary>
    /// An indeterminate bar: a div at <c>left: offset × W</c>, which <c>translateX</c> moves, holding a span of the same width
    /// that <c>scaleX</c> scales about its centre, all inside the track, scaled by <paramref name="track"/> from its left. As
    /// one matrix about the top left: x′ = k·(s·x + (1 − s)·W/2 + offset·W).
    /// </summary>
    private static void SetSpan(Control? part, double offset, double scale, double width, double track)
    {
        if (part?.RenderTransform is MatrixTransform transform)
        {
            transform.Matrix = new Matrix(track * scale, 0, 0, 1, track * ((1 - scale) * width / 2 + offset * width), 0);
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>role="progressbar"</c>, <c>aria-valuemin="0"</c>, <c>aria-valuemax="1"</c>, and <c>aria-valuenow</c> the raw value
    /// when determinate. Indeterminate has no value, so no range. A change of type raises no event, as WPF's progress bar
    /// raises none when it becomes indeterminate: the range appears or goes, and a client reads it when it next looks.
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new ProgressBarAutomationPeer(this);

    private sealed class ProgressBarAutomationPeer(AvonicProgressBar owner) : ControlAutomationPeer(owner), IRangeValueProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

        protected override object? GetProviderCore(Type providerType) =>
            providerType == typeof(IRangeValueProvider) && owner.Type == ProgressBarType.Indeterminate
                ? null
                : base.GetProviderCore(providerType);

        public bool IsReadOnly => true;

        public double Minimum => 0;

        public double Maximum => 1;

        public double Value => owner.Value;

        public double LargeChange => 0;

        public double SmallChange => 0;

        public void SetValue(double value) => throw new InvalidOperationException("A progress bar's value is read-only.");

        public void RaiseValueChanged(double oldValue, double newValue)
        {
            if (owner.Type == ProgressBarType.Determinate)
            {
                RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
            }
        }
    }
}
