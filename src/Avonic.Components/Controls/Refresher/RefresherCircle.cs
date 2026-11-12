using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// The pulling icon's circle, when its spinner is <see cref="SpinnerName.Circular"/>: <c>ion-spinner name="circular"</c> with the
/// <c>circle</c>'s animation set to <c>none</c>, as md's native refresher does, so that the pull sets its dash instead. Drawn from
/// the same config as <see cref="AvonicSpinner"/>: <c>viewBox="24 24 48 48"</c>, a circle at 48, 48 of radius 20, a stroke of 5.6,
/// with a dash from 3 o'clock, clockwise. A part of <see cref="AvonicRefresherContent"/>'s template.
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

    /// <summary>The <c>circle</c>'s <c>stroke-dasharray</c> dash, in the <c>viewBox</c>'s units. The gap, 200, is longer than the circle.</summary>
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

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);

    /// <summary>The <c>svg</c> spinner draws by position, so a right-to-left ancestor doesn't mirror it.</summary>
    protected override bool BypassFlowDirectionPolicies => true;

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0 || Foreground is not { } brush || _dash <= 0)
        {
            return;
        }

        var config = SpinnerConfigs.Get(SpinnerName.Circular);
        var frame = new SpinnerElementFrame(0, 0, 1, 1, 0, null);
        if (_pen is null || !ReferenceEquals(_pen.Brush, brush))
        {
            _pen = new Pen(brush, config.StrokeWidth);
        }

        var radius = config.Radius;
        var center = config.Center;
        var length = Math.Min(_dash, 2 * Math.PI * radius);
        using (context.PushTransform(SpinnerFrame.Transform(config, frame, size)))
        {
            context.DrawGeometry(null, _pen, Arc(center, radius, length));
        }
    }

    /// <summary>An arc of <paramref name="length"/> along the circle from 3 o'clock, clockwise, as an SVG <c>circle</c>'s path runs.</summary>
    private static StreamGeometry Arc(Point center, double radius, double length)
    {
        Point At(double distance)
        {
            var angle = distance / radius;
            return new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
        }

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(At(0), false);
            var halfTurn = Math.PI * radius;
            for (var from = 0d; from < length; from += halfTurn)
            {
                g.ArcTo(At(Math.Min(length, from + halfTurn)), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            }

            g.EndFigure(false);
        }

        return geometry;
    }
}
