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

    static AvonicProgressBar()
    {
        FocusableProperty.OverrideDefaultValue<AvonicProgressBar>(false);
    }

    public AvonicProgressBar()
    {
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
    private readonly CssTransition _lead = new();

    /// <summary>The track and the stream's window, when determinate. New elements on a change of type, so they don't ease then.</summary>
    private readonly CssTransition _buffer = new();

    private bool _attached;
    private TimeSpan _renderedAt;
    private IDisposable? _frame;
    private IDisposable? _reversedFrame;
    private ProgressBarAutomationPeer? _peer;

    /// <summary>Whether a frame is requested: only while something moves.</summary>
    internal bool IsFrameLoopRunning => _frame is not null;

    /// <summary>Whether the bar is drawn mirrored: Ionic's <c>progress-bar-reversed</c>.</summary>
    internal bool IsEffectivelyReversed => FlowDirection == FlowDirection.RightToLeft ? !Reversed : Reversed;

    /// <summary>
    /// <c>clamp(0, value, 1)</c>. NaN stays NaN in Ionic, which makes <c>scaleX(NaN)</c> an invalid transform, so the
    /// part is drawn untransformed: at 1.
    /// </summary>
    private static double Display(double value) => double.IsNaN(value) ? 1 : Math.Clamp(value, 0, 1);

    private double LeadTarget => Type == ProgressBarType.Determinate ? Display(Value) : 1;

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
        _root = this.GetVisualChildren().OfType<Control>().FirstOrDefault();
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
        _renderedAt = AvonicConfig.Now();
        _lead.Snap(LeadTarget);
        _buffer.Snap(Display(Buffer));
        Update();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        _frame?.Dispose();
        _frame = null;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Update();
    }

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
            if (Type == ProgressBarType.Determinate)
            {
                Ease(_buffer, Display(Buffer));
            }
            else
            {
                _buffer.Snap(Display(Buffer));
            }

            UpdatePseudoClasses();
            Update();
        }
        else if (change.Property == ReversedProperty || change.Property == FlowDirectionProperty)
        {
            UpdatePseudoClasses();
        }
    }

    private void Ease(CssTransition transition, double target)
    {
        if (_attached && AvonicConfig.Animated)
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

        // :host(.progress-bar-reversed) { transform: scaleX(-1) }. At style priority, so that an app's transform replaces it,
        // as an outer stylesheet's does in CSS.
        if (reversed && _reversedFrame is null)
        {
            _reversedFrame = SetValue(RenderTransformProperty, new ScaleTransform(-1, 1), BindingPriority.Style);
        }
        else if (!reversed && _reversedFrame is not null)
        {
            _reversedFrame.Dispose();
            _reversedFrame = null;
        }
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
            return;
        }

        var now = AvonicConfig.Now();
        var animated = AvonicConfig.Animated && _attached;

        // AvonicConfig.Animated off stands the animations at time 0, as Ionic's progress-paused does.
        var elapsed = animated ? now - _renderedAt : TimeSpan.Zero;
        var width = _root.Bounds.Width;
        var moving = _attached && (_lead.IsRunning(now) || _buffer.IsRunning(now));
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
            moving |= animated && buffer < 1;
        }
        else
        {
            SetScale(_track, _lead.At(now));
            var t = Fraction(elapsed, IndeterminateDuration);
            var (primaryTranslate, primaryScale) = IndeterminateKeyframes.Primary(t);
            var (secondaryTranslate, secondaryScale) = IndeterminateKeyframes.Secondary(t);
            SetSpan(_primary, IndeterminateKeyframes.PrimaryLeft + primaryTranslate, primaryScale, width);
            SetSpan(_secondary, IndeterminateKeyframes.SecondaryLeft + secondaryTranslate, secondaryScale, width);
            moving |= animated;
        }

        if (moving)
        {
            _frame ??= AvonicConfig.RequestFrame(this, OnFrame);
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
    /// that <c>scaleX</c> scales about its centre. As one matrix about the top left: x′ = s·x + (1 − s)·W/2 + offset·W.
    /// </summary>
    private static void SetSpan(Control? part, double offset, double scale, double width)
    {
        if (part?.RenderTransform is MatrixTransform transform)
        {
            transform.Matrix = new Matrix(scale, 0, 0, 1, (1 - scale) * width / 2 + offset * width, 0);
        }
    }

    // ── Automation ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>role="progressbar"</c>, <c>aria-valuemin="0"</c>, <c>aria-valuemax="1"</c>, and <c>aria-valuenow</c> the raw value
    /// when determinate. Indeterminate has no value, so no range.
    /// </summary>
    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new ProgressBarAutomationPeer(this);

    private sealed class ProgressBarAutomationPeer(AvonicProgressBar owner) : ControlAutomationPeer(owner), IRangeValueProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

        protected override bool IsContentElementCore() => true;

        protected override bool IsControlElementCore() => true;

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

/// <summary>
/// A CSS transition of one number, on <see cref="AvonicConfig"/>'s clock, with CSS's rules for an interrupted one: it starts
/// from the value shown, and one that goes back to where the running one began is shortened by how far that one got.
/// </summary>
internal sealed class CssTransition
{
    private double _from;
    private double _to;
    private TimeSpan _start;
    private TimeSpan _duration;
    private double _reversingStart;
    private double _shortening = 1;

    public void Snap(double value)
    {
        _from = _to = _reversingStart = value;
        _duration = TimeSpan.Zero;
        _shortening = 1;
    }

    public bool IsRunning(TimeSpan now) => _duration > TimeSpan.Zero && now - _start < _duration;

    /// <summary>The value shown. Exactly the target once done, so that a later change compares against it as CSS does.</summary>
    public double At(TimeSpan now) => Progress(now) is var progress && progress >= 1 ? _to : _from + (_to - _from) * progress;

    private double Progress(TimeSpan now) =>
        _duration <= TimeSpan.Zero ? 1 : Math.Clamp((now - _start).Ticks / (double)_duration.Ticks, 0, 1);

    /// <summary>CSS Transitions §3, "starting of transitions", for a linear timing function and no delay.</summary>
    public void Start(double to, TimeSpan now, TimeSpan duration)
    {
        var running = IsRunning(now);
        var current = At(now);
        if (running ? to == _to : to == current)
        {
            return;
        }

        if (running && to == current)
        {
            Snap(to);
            return;
        }

        if (running && to == _reversingStart)
        {
            // The reversing shortening factor: how far the running one got, scaled by its own factor
            _shortening = Math.Clamp(Math.Abs(Progress(now) * _shortening) + 1 - _shortening, 0, 1);
            _reversingStart = _to;
        }
        else
        {
            _shortening = 1;
            _reversingStart = current;
        }

        _from = current;
        _to = to;
        _start = now;
        _duration = TimeSpan.FromTicks((long)(duration.Ticks * _shortening));
        if (_duration <= TimeSpan.Zero)
        {
            Snap(to);
        }
    }
}
