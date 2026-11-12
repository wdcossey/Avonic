using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// The circle of md's native refresher, drawn from the same config as <see cref="AvonicSpinner"/>'s <c>circular</c>:
/// <c>viewBox="24 24 48 48"</c>, a circle at 48, 48 of radius 20, a stroke of 5.6, its dash from 3 o'clock, clockwise. A part of
/// <see cref="AvonicRefresherContent"/>'s template, in two places. The pulling icon's has its animation set to <c>none</c>, so
/// the pull sets its dash. The refreshing icon's runs <c>spinner-circular-inner</c> with <c>animation-delay: -655ms</c>, which
/// lines it up with where the pull left off, and which <see cref="AvonicSpinner"/> has no way to take.
/// </summary>
public sealed class RefresherCircle : Control
{
    /// <summary>The stroke's colour: the spinner's <c>currentColor</c>.</summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<RefresherCircle>();

    private double _dash = RefresherPullFrame.BaseCircleDash;
    private TimeSpan? _startedAt;
    private IDisposable? _frame;
    private Pen? _pen;

    static RefresherCircle()
    {
        AffectsRender<RefresherCircle>(ForegroundProperty);
    }

    /// <inheritdoc cref="ForegroundProperty"/>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>The <c>circle</c>'s <c>stroke-dasharray</c> dash while it isn't running, in the <c>viewBox</c>'s units.</summary>
    internal double Dash
    {
        get => _dash;
        set
        {
            if (_dash != value)
            {
                _dash = value;
                InvalidateVisual();
            }
        }
    }

    /// <summary>Whether <c>spinner-circular-inner</c> is running.</summary>
    internal bool IsRunning => _startedAt is not null;

    /// <summary>The time on <see cref="AvonicConfig.Now"/> its animation began, delay included, while it's running.</summary>
    internal TimeSpan? StartedAt => _startedAt;

    /// <summary>Runs <c>spinner-circular-inner</c>, as if it had begun at <paramref name="startedAt"/>.</summary>
    internal void Run(TimeSpan startedAt)
    {
        _startedAt = startedAt;
        RequestFrame();
        InvalidateVisual();
    }

    /// <summary>Stops it: <c>display: none</c> ends a CSS animation, and the next show starts it again.</summary>
    internal void Stop()
    {
        _startedAt = null;
        _frame?.Dispose();
        _frame = null;
        InvalidateVisual();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_startedAt is not null)
        {
            RequestFrame();
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _frame?.Dispose();
        _frame = null;
    }

    private void RequestFrame() => _frame ??= AvonicConfig.RequestFrame(this, () =>
    {
        _frame = null;
        if (_startedAt is not null && VisualRoot is not null)
        {
            InvalidateVisual();
            RequestFrame();
        }
    });

    /// <summary>The stretches of the circle drawn now: the running animation's, or the one dash from 3 o'clock.</summary>
    internal IReadOnlyList<SpinnerArc> CurrentArcs()
    {
        var config = SpinnerConfigs.Get(SpinnerName.Circular);
        if (_startedAt is { } startedAt)
        {
            var frame = SpinnerFrame.Compute(config, config.Dur, 0, (AvonicConfig.Now() - startedAt).TotalMilliseconds);
            return frame.Arcs ?? [];
        }

        return _dash > 0 ? [new SpinnerArc(0, Math.Min(_dash, 2 * Math.PI * config.Radius))] : [];
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);

    /// <summary>The <c>svg</c> spinner draws by position, so a right-to-left ancestor doesn't mirror it.</summary>
    protected override bool BypassFlowDirectionPolicies => true;

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0 || Foreground is not { } brush)
        {
            return;
        }

        var config = SpinnerConfigs.Get(SpinnerName.Circular);
        if (_pen is null || !ReferenceEquals(_pen.Brush, brush))
        {
            _pen = new Pen(brush, config.StrokeWidth);
        }

        using (context.PushTransform(SpinnerFrame.Transform(config, new SpinnerElementFrame(0, 0, 1, 1, 0, null), size)))
        {
            foreach (var arc in CurrentArcs())
            {
                context.DrawGeometry(null, _pen, Arc(config.Center, config.Radius, arc));
            }
        }
    }

    /// <summary>A stretch of the circle, as distances along it from 3 o'clock, clockwise, as an SVG <c>circle</c>'s path runs.</summary>
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
            var halfTurn = Math.PI * radius;
            for (var from = arc.Start; from < arc.End; from += halfTurn)
            {
                g.ArcTo(At(Math.Min(arc.End, from + halfTurn)), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            }

            g.EndFigure(false);
        }

        return geometry;
    }
}
