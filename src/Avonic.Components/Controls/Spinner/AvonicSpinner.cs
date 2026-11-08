using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-spinner</c>: an indeterminate progress indicator, one of nine sets of shapes, each animated.
/// </summary>
/// <remarks>
/// Ionic draws it as an <c>svg</c> per shape with a CSS keyframe animation on each. Here it draws itself, from the same
/// configs, on <see cref="AvonicConfig"/>'s clock, so it has no template. Its size, 28 px square, and its colour are in
/// the theme. See <c>docs/components/ion-spinner/README.md</c>.
/// </remarks>
public class AvonicSpinner : Control
{
    // ── Properties ───────────────────────────────────────────────────────────────────────────

    /// <summary>Ionic's <c>color</c>. The theme paints the palette's base colour, as <c>:host(.ion-color)</c> does.</summary>
    public static readonly StyledProperty<string?> ColorProperty =
        ColorPalette.ColorProperty.AddOwner<AvonicSpinner>();

    /// <summary>
    /// Ionic's <c>duration</c>, in milliseconds. Only a duration over 10 counts: otherwise, and by default, each name has
    /// its own.
    /// </summary>
    public static readonly StyledProperty<double?> DurationProperty =
        AvaloniaProperty.Register<AvonicSpinner, double?>(nameof(Duration));

    /// <summary>
    /// Ionic's <c>name</c>, renamed because <see cref="StyledElement.Name"/> already exists. Null is md's default,
    /// <see cref="Controls.SpinnerName.Circular"/>.
    /// </summary>
    public static readonly StyledProperty<SpinnerName?> SpinnerNameProperty =
        AvaloniaProperty.Register<AvonicSpinner, SpinnerName?>(nameof(SpinnerName));

    /// <summary>Ionic's <c>paused</c>: stops the animation where it is. Clearing it carries on from there.</summary>
    public static readonly StyledProperty<bool> PausedProperty =
        AvaloniaProperty.Register<AvonicSpinner, bool>(nameof(Paused));

    /// <summary>Ionic's <c>currentColor</c>. Inherited.</summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<AvonicSpinner>();

    // ── State ────────────────────────────────────────────────────────────────────────────────

    private SpinnerConfig _config = SpinnerConfigs.Get(Controls.SpinnerName.Circular);

    /// <summary>When each element's animation began, moved on by the time spent paused, as a CSS animation's start time is.</summary>
    private TimeSpan[] _starts = [];

    /// <summary>When the animations were paused, or null while they run.</summary>
    private TimeSpan? _pausedAt;

    private IDisposable? _frame;

    /// <summary>While paused only by <see cref="AvonicConfig.Animated"/>, the next look at it.</summary>
    private IDisposable? _animatedCheck;

    /// <summary>How often a spinner paused by <see cref="AvonicConfig.Animated"/> looks to see if it's back on.</summary>
    internal static readonly TimeSpan AnimatedCheckInterval = TimeSpan.FromMilliseconds(250);
    private bool _attached;

    /// <summary>
    /// Whether the animations exist: attached and shown. CSS's <c>display: none</c>, on the spinner or an ancestor, cancels
    /// them, as leaving the document does, and showing it again starts new ones.
    /// </summary>
    private bool _live;

    private Pen? _pen;
    private SpinnerConfig? _penConfig;

    /// <summary>The spinners in a visual tree, which a change of <see cref="Visual.IsVisible"/> anywhere may show or hide.</summary>
    private static readonly HashSet<AvonicSpinner> s_attached = [];

    static AvonicSpinner()
    {
        AffectsRender<AvonicSpinner>(ForegroundProperty, DurationProperty);

        // display: inline-block. At the start and top of its parent, rather than stretching to fill it.
        HorizontalAlignmentProperty.OverrideDefaultValue<AvonicSpinner>(HorizontalAlignment.Left);
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicSpinner>(VerticalAlignment.Top);

        SpinnerNameProperty.Changed.AddClassHandler<AvonicSpinner>((spinner, _) => spinner.ChangeName());
        PausedProperty.Changed.AddClassHandler<AvonicSpinner>((spinner, _) => spinner.UpdatePlayState());
        DurationProperty.Changed.AddClassHandler<AvonicSpinner>((spinner, _) => spinner.UpdatePlayState());

        // Hiding the spinner or an ancestor is CSS's display: none, which cancels its animations.
        IsVisibleProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs<bool>>(_ =>
        {
            if (s_attached.Count == 0)
            {
                return;
            }

            foreach (var spinner in s_attached.ToArray())
            {
                spinner.UpdateLive();
            }
        }));
    }

    /// <inheritdoc cref="ColorProperty"/>
    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <inheritdoc cref="DurationProperty"/>
    public double? Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <inheritdoc cref="SpinnerNameProperty"/>
    public SpinnerName? SpinnerName
    {
        get => GetValue(SpinnerNameProperty);
        set => SetValue(SpinnerNameProperty, value);
    }

    /// <inheritdoc cref="PausedProperty"/>
    public bool Paused
    {
        get => GetValue(PausedProperty);
        set => SetValue(PausedProperty, value);
    }

    /// <inheritdoc cref="ForegroundProperty"/>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    // ── For tests ────────────────────────────────────────────────────────────────────────────

    /// <summary><c>getName()</c>: the name, or md's default.</summary>
    internal SpinnerName ResolvedName => SpinnerName ?? Controls.SpinnerName.Circular;

    /// <summary>The config being drawn.</summary>
    internal SpinnerConfig Config => _config;

    /// <summary>The duration being used, in milliseconds.</summary>
    internal double EffectiveDuration => SpinnerFrame.EffectiveDuration(_config, Duration);

    /// <summary>Whether a frame has been asked for: only while attached, shown and running, as a CSS animation costs nothing otherwise.</summary>
    internal bool IsRequestingFrames => _frame is not null;

    /// <summary>Every element at this instant on <see cref="AvonicConfig.Now"/>.</summary>
    internal IReadOnlyList<SpinnerElementFrame> CurrentFrame()
    {
        var now = _pausedAt ?? AvonicConfig.Now();
        var duration = EffectiveDuration;
        var frames = new SpinnerElementFrame[_config.Count];
        for (var i = 0; i < frames.Length; i++)
        {
            var start = i < _starts.Length ? _starts[i] : now;
            frames[i] = SpinnerFrame.Compute(_config, duration, i, (now - start).TotalMilliseconds);
        }

        return frames;
    }

    // ── Timeline ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ionic's <c>paused</c>, or Avonic's <see cref="AvonicConfig.Animated"/> off, which stands in for Ionic's
    /// <c>_testing</c> config: <c>'spinner-paused': self.paused || config.getBoolean('_testing')</c>.
    /// </summary>
    private bool ShouldPause => Paused || !AvonicConfig.Animated;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        s_attached.Add(this);
        UpdateLive();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        s_attached.Remove(this);
        UpdateLive();
    }

    private void UpdateLive()
    {
        var live = _attached && IsVisible && this.GetVisualAncestors().All(ancestor => ancestor.IsVisible);
        if (live == _live)
        {
            return;
        }

        _live = live;
        if (!live)
        {
            StopFrames();
            return;
        }

        // An element that's inserted, or shown again, gets new animations, which start from the beginning.
        var now = AvonicConfig.Now();
        _config = SpinnerConfigs.Get(ResolvedName);
        _starts = Enumerable.Repeat(now, _config.Count).ToArray();
        _pausedAt = ShouldPause ? now : null;
        UpdatePlayState();
    }

    /// <summary>
    /// A new name re-renders the same <c>svg</c>s, in order. One that's kept, with the same keyframes, carries on, as a
    /// CSS animation whose name doesn't change does. A new one, or one whose keyframes change, starts from the beginning,
    /// and paused if the spinner is.
    /// </summary>
    private void ChangeName()
    {
        var previous = _config;
        _config = SpinnerConfigs.Get(ResolvedName);
        if (_live)
        {
            var now = _pausedAt ?? AvonicConfig.Now();
            var starts = new TimeSpan[_config.Count];
            for (var i = 0; i < starts.Length; i++)
            {
                starts[i] = i < _starts.Length && previous.Animation == _config.Animation ? _starts[i] : now;
            }

            _starts = starts;
            UpdatePlayState();
        }

        InvalidateVisual();
    }

    /// <summary>
    /// Pauses or resumes. Read when the spinner renders, as Ionic's <c>render</c> reads <c>paused</c> and
    /// <c>_testing</c>, and on each frame.
    /// </summary>
    private void UpdatePlayState()
    {
        if (!_live)
        {
            return;
        }

        var now = AvonicConfig.Now();
        if (ShouldPause)
        {
            _pausedAt ??= now;
            StopFrames();

            // Paused only because animations are off: look again now and then, without drawing, so that turning them back
            // on carries on, as AvonicConfig's switches may change at runtime.
            if (!Paused)
            {
                _animatedCheck ??= AvonicConfig.Schedule(OnAnimatedCheck, AnimatedCheckInterval);
            }
        }
        else
        {
            if (_pausedAt is { } pausedAt)
            {
                // animation-play-state: running carries on from where it stopped.
                var paused = now - pausedAt;
                for (var i = 0; i < _starts.Length; i++)
                {
                    _starts[i] += paused;
                }

                _pausedAt = null;
            }

            RequestFrame();
        }

        InvalidateVisual();
    }

    private void RequestFrame() => _frame ??= AvonicConfig.RequestFrame(this, OnFrame);

    private void OnFrame()
    {
        _frame = null;
        if (!_live)
        {
            return;
        }

        if (ShouldPause)
        {
            UpdatePlayState();
            return;
        }

        InvalidateVisual();
        RequestFrame();
    }

    private void OnAnimatedCheck()
    {
        _animatedCheck = null;
        UpdatePlayState();
    }

    private void StopFrames()
    {
        _frame?.Dispose();
        _frame = null;
        _animatedCheck?.Dispose();
        _animatedCheck = null;
    }

    // ── Drawing ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Avalonia mirrors drawing under a right-to-left ancestor. Ionic's spinner positions with <c>top</c> and <c>left</c> on purpose, so opt out.</summary>
    protected override bool BypassFlowDirectionPolicies => true;

    /// <summary><c>role="progressbar"</c>.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new SpinnerAutomationPeer(this);

    private sealed class SpinnerAutomationPeer(AvonicSpinner owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;
        protected override string GetClassNameCore() => nameof(AvonicSpinner);
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        // The host is a box, so all of it takes input. A transparent brush hit-tests, and no brush wouldn't.
        context.FillRectangle(Brushes.Transparent, new Rect(size));

        if (Foreground is not { } color)
        {
            return;
        }

        var config = _config;
        var fill = config.Fill ? color : null;
        var pen = PenFor(color, config);

        foreach (var frame in CurrentFrame())
        {
            if (frame.Opacity <= 0 || frame.Scale <= 0)
            {
                continue;
            }

            using (context.PushOpacity(frame.Opacity))
            using (context.PushTransform(SpinnerFrame.Transform(config, frame, size)))
            {
                if (config.IsLine)
                {
                    context.DrawLine(pen!, config.Center + new Vector(0, config.Y1), config.Center + new Vector(0, config.Y2));
                }
                else if (frame.Arcs is { } arcs)
                {
                    foreach (var arc in arcs)
                    {
                        context.DrawGeometry(null, pen, Arc(config.Center, config.Radius, arc));
                    }
                }
                else
                {
                    context.DrawEllipse(fill, pen, config.Center, config.Radius, config.Radius);
                }
            }
        }
    }

    /// <summary>The stroke, kept while the colour and the name stay the same, as it's drawn every frame.</summary>
    private Pen? PenFor(IBrush color, SpinnerConfig config)
    {
        if (config.StrokeWidth <= 0)
        {
            return null;
        }

        if (_pen is null || !ReferenceEquals(_pen.Brush, color) || _penConfig != config)
        {
            _pen = new Pen(color, config.StrokeWidth, lineCap: config.LineCap);
            _penConfig = config;
        }

        return _pen;
    }

    /// <summary>
    /// A stretch of a circle's outline. Its path starts at 3 o'clock and runs clockwise, as an SVG <c>circle</c>'s does,
    /// with flat ends, the default <c>stroke-linecap: butt</c>.
    /// </summary>
    private static StreamGeometry Arc(Point center, double radius, SpinnerArc arc)
    {
        Point At(double distance)
        {
            var angle = distance / radius;
            return new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
        }

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(At(arc.Start), false);

            // An arc segment can't be a whole turn, so draw at most half a turn at a time.
            var halfTurn = Math.PI * radius;
            for (var from = arc.Start; from < arc.End; from += halfTurn)
            {
                var to = Math.Min(arc.End, from + halfTurn);
                g.ArcTo(At(to), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            }

            g.EndFigure(false);
        }

        return geometry;
    }
}
