using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// The pulling circle of md's native refresher, drawn from the same config as <see cref="AvonicSpinner"/>'s <c>circular</c>:
/// <c>viewBox="24 24 48 48"</c>, a circle at 48, 48 of radius 20, a stroke of 5.6, its dash from 3 o'clock, clockwise. A part of
/// <see cref="AvonicRefresherContent"/>'s template: the pulling icon's circle, whose animation is set to <c>none</c>, so the pull
/// sets its dash. The refreshing icon's circle runs, so it's an <see cref="AvonicSpinner"/>, with an <c>animation-delay</c>.
/// </summary>
public sealed class RefresherCircle : Control
{
    /// <summary>The stroke's colour: the spinner's <c>currentColor</c>.</summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<RefresherCircle>();

    private double _dash = RefresherPullFrame.BaseCircleDash;
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

    /// <summary>The <c>circle</c>'s <c>stroke-dasharray</c> dash, in the <c>viewBox</c>'s units.</summary>
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

    /// <summary>The stretch of the circle drawn: the one dash from 3 o'clock.</summary>
    internal IReadOnlyList<SpinnerArc> CurrentArcs()
    {
        var config = SpinnerConfigs.Get(SpinnerName.Circular);
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
