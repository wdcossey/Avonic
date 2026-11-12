using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
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
[PseudoClasses(
    ":name-bubbles", ":name-circles", ":name-circular", ":name-crescent", ":name-dots",
    ":name-lines", ":name-lines-small", ":name-lines-sharp", ":name-lines-sharp-small", ":paused")]
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
    /// Ionic's <c>name</c>, renamed because <see cref="StyledElement.Name"/> already exists. Null is
    /// <see cref="AvonicConfig.Spinner"/>, and if that's null too, md's default, <see cref="Controls.SpinnerName.Circular"/>.
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

    /// <summary>The name <see cref="GetName"/> gave at the last render, which a re-render compares with. <see cref="_config"/> is its config.</summary>
    internal SpinnerName ResolvedName { get; private set; }

    private SpinnerConfig _config;

    /// <summary>When each element's animation began, moved on by the time spent paused, as a CSS animation's start time is.</summary>
    private TimeSpan[] _starts = [];

    /// <summary>When the animations were paused, or null while they run.</summary>
    private TimeSpan? _pausedAt;

    private TimeSpan _animationDelay;

    private IDisposable? _frame;
    private bool _attached;

    /// <summary>Whether it's been attached, which is Ionic's first render. Attaching again isn't a render, measured.</summary>
    private bool _rendered;

    /// <summary>
    /// Whether the animations exist: attached and shown. CSS's <c>display: none</c>, on the spinner or an ancestor, cancels
    /// them, as leaving the document does, and showing it again starts new ones.
    /// </summary>
    private bool _live;

    private Pen? _pen;
    private SpinnerConfig? _penConfig;
    private StreamGeometry[]? _fixedArcs;

    /// <summary>
    /// Ionic's host classes <c>spinner-&lt;name&gt;</c>, as <c>:name-&lt;name&gt;</c>, by the rule for a host class from a
    /// prop, indexed by <see cref="Controls.SpinnerName"/>.
    /// </summary>
    private static readonly string[] s_nameClasses =
    [
        ":name-bubbles", ":name-circles", ":name-circular", ":name-crescent", ":name-dots",
        ":name-lines", ":name-lines-small", ":name-lines-sharp", ":name-lines-sharp-small",
    ];

    /// <summary>The spinners in a visual tree, which a change of <see cref="Visual.IsVisible"/> anywhere may show or hide.</summary>
    private static readonly HashSet<AvonicSpinner> s_attached = [];

    static AvonicSpinner()
    {
        AffectsRender<AvonicSpinner>(ForegroundProperty, DurationProperty);

        // display: inline-block. At the start and top of its parent, rather than stretching to fill it.
        HorizontalAlignmentProperty.OverrideDefaultValue<AvonicSpinner>(HorizontalAlignment.Left);
        VerticalAlignmentProperty.OverrideDefaultValue<AvonicSpinner>(VerticalAlignment.Top);

        // A change of a prop re-renders it in Ionic, which reads config.spinner again in getName()
        SpinnerNameProperty.Changed.AddClassHandler<AvonicSpinner>((spinner, _) => spinner.Rerender());
        PausedProperty.Changed.AddClassHandler<AvonicSpinner>((spinner, _) => spinner.Rerender());
        DurationProperty.Changed.AddClassHandler<AvonicSpinner>((spinner, _) => spinner.Rerender());
        ColorProperty.Changed.AddClassHandler<AvonicSpinner>((spinner, _) => spinner.Rerender());

        // Hiding the spinner or an ancestor is CSS's display: none, which cancels its animations.
        // A window that's hidden isn't: a page that's hidden doesn't cancel its animations. A visual outside a tree can't
        // be an attached spinner's ancestor. Something shown can only start a spinner that's stopped, and something hidden
        // only stop one that runs, so the rest aren't walked. UpdateLive changes nothing in the set, so it's walked as it is.
        IsVisibleProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs<bool>>(e =>
        {
            if (s_attached.Count == 0 || e.Sender is TopLevel || e.Sender is not Visual sender || !sender.IsAttachedToVisualTree())
            {
                return;
            }

            var shown = e.NewValue.GetValueOrDefault();
            foreach (var spinner in s_attached)
            {
                if (spinner._live != shown)
                {
                    spinner.UpdateLive();
                }
            }
        }));
    }

    public AvonicSpinner()
    {
        ResolvedName = GetName();
        _config = SpinnerConfigs.Get(ResolvedName);
        UpdatePseudoClasses();
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

    /// <summary>
    /// CSS <c>animation-delay</c> on each element's animation, on top of the delay its config gives it: a negative one starts the
    /// animations that far in. A change moves the running animations, as a change of <c>animation-delay</c> does. Internal, for md's
    /// native refresher, which sets <c>-655ms</c> on its circular refreshing spinner.
    /// </summary>
    internal TimeSpan AnimationDelay
    {
        get => _animationDelay;
        set
        {
            if (_animationDelay == value)
            {
                return;
            }

            var shift = value - _animationDelay;
            _animationDelay = value;
            for (var i = 0; i < _starts.Length; i++)
            {
                _starts[i] += shift;
            }

            InvalidateVisual();
        }
    }

    // ── For tests ────────────────────────────────────────────────────────────────────────────

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
    /// Ionic's <c>getName()</c>: <c>this.name || config.get('spinner')</c>, or md's default. Read at each render, as there.
    /// </summary>
    private SpinnerName GetName() => SpinnerName ?? AvonicConfig.Spinner ?? Controls.SpinnerName.Circular;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        s_attached.Add(this);

        // Ionic's first render is when the element is first connected, so the name is read again then
        if (!_rendered)
        {
            _rendered = true;
            Rerender();
        }

        UpdateLive();
    }

    /// <summary>
    /// What a re-render reads: the name, from <c>getName()</c>, and <c>paused</c>. A name that has changed since the last render,
    /// from <see cref="SpinnerName"/> or <see cref="AvonicConfig.Spinner"/>, is a change of name. At once, where Stencil renders at
    /// the next frame, as every change of a prop here applies at once.
    /// </summary>
    private void Rerender()
    {
        if (GetName() != ResolvedName)
        {
            ChangeName();
        }
        else
        {
            UpdatePlayState();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        s_attached.Remove(this);
        UpdateLive();
    }

    /// <summary>
    /// Whether it and every ancestor below the window are visible. Not <see cref="Visual.IsEffectivelyVisible"/>, which
    /// counts the window too.
    /// </summary>
    private bool IsShown()
    {
        for (Visual? visual = this; visual is not null and not TopLevel; visual = visual.GetVisualParent())
        {
            if (!visual.IsVisible)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateLive()
    {
        var live = _attached && IsShown();
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

        // An element that's inserted, or shown again, gets new animations, which start from the beginning. An old pause goes
        // with the old animations, and UpdatePlayState pauses the new ones if it's paused.
        var now = AvonicConfig.Now();
        _starts = Enumerable.Repeat(now + _animationDelay, _config.Count).ToArray();
        _pausedAt = null;
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
        ResolvedName = GetName();
        _config = SpinnerConfigs.Get(ResolvedName);
        UpdatePseudoClasses();
        if (_live)
        {
            var now = _pausedAt ?? AvonicConfig.Now();
            var starts = new TimeSpan[_config.Count];
            for (var i = 0; i < starts.Length; i++)
            {
                starts[i] = i < _starts.Length && previous.Animation == _config.Animation ? _starts[i] : now + _animationDelay;
            }

            _starts = starts;
            UpdatePlayState();
        }

        InvalidateVisual();
    }

    /// <summary>Pauses or resumes, as Ionic's <c>render</c> reads <c>paused</c>.</summary>
    private void UpdatePlayState()
    {
        UpdatePseudoClasses();
        if (!_live)
        {
            return;
        }

        var now = AvonicConfig.Now();
        if (Paused)
        {
            if (_pausedAt is null)
            {
                _pausedAt = now;
                InvalidateVisual();
            }

            StopFrames();
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
            InvalidateVisual();
        }
    }

    /// <summary>
    /// <c>spinner-&lt;name&gt;</c> for the name <c>getName()</c> gave, and <c>spinner-paused</c> for <c>paused</c>. Ionic's
    /// <c>_testing</c>, a private config for its own screenshot tests, has no equivalent.
    /// </summary>
    private void UpdatePseudoClasses()
    {
        var name = (int)ResolvedName;
        for (var i = 0; i < s_nameClasses.Length; i++)
        {
            PseudoClasses.Set(s_nameClasses[i], i == name);
        }

        PseudoClasses.Set(":paused", Paused);
    }

    private void RequestFrame() => _frame ??= AvonicConfig.RequestFrame(this, OnFrame);

    private void OnFrame()
    {
        _frame = null;
        if (!_live)
        {
            return;
        }

        InvalidateVisual();
        RequestFrame();
    }

    private void StopFrames()
    {
        _frame?.Dispose();
        _frame = null;
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
                    // crescent's dash is fixed, and only its turn changes, so its arc is built once. It's the only fixed
                    // one, so the geometry needs no key: one would be a rule that nothing can break.
                    if (config.Dash is not null)
                    {
                        _fixedArcs ??= arcs.Select(arc => Arc(config.Center, config.Radius, arc)).ToArray();

                        foreach (var geometry in _fixedArcs)
                        {
                            context.DrawGeometry(null, pen, geometry);
                        }
                    }
                    else
                    {
                        foreach (var arc in arcs)
                        {
                            context.DrawGeometry(null, pen, Arc(config.Center, config.Radius, arc));
                        }
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
