using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Avonic.Components.Controls;

/// <summary>
/// The checkbox's <c>part="mark"</c>: md's path from <c>getSVGPath</c>, in the SVG's 24-unit view box, fitted to this control's
/// size and centred, as <c>viewBox="0 0 24 24"</c> is in the box's content. A tick when checked, a bar when indeterminate.
/// </summary>
/// <remarks>
/// md draws it with <c>stroke-dasharray: 30</c>: hidden with <c>stroke-dashoffset: 30</c> and <c>opacity: 0</c>, and shown with
/// 0 and 1. Only the shown rule has <c>transition: stroke-dashoffset 90ms linear 90ms</c>, so showing draws the stroke from its
/// start, after 90 ms, over 90 ms, and hiding is at once. That runs here, on <see cref="AvonicConfig"/>'s clock, from the visible
/// length a dash offset leaves, which is <c>30 - offset</c>. Not before the first frame after it's attached, as CSS doesn't
/// transition a first style.
/// </remarks>
public sealed class CheckboxMark : Control
{
    /// <summary><c>stroke-dasharray</c> and the hidden <c>stroke-dashoffset</c>, in view box units.</summary>
    internal const double DashLength = 30;

    /// <summary>The delay in <c>transition: stroke-dashoffset 90ms linear 90ms</c>.</summary>
    internal static readonly TimeSpan TransitionDelay = TimeSpan.FromMilliseconds(90);

    /// <summary>The duration in <c>transition: stroke-dashoffset 90ms linear 90ms</c>.</summary>
    internal static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(90);

    /// <summary><c>M1.73,12.91 8.1,19.28 22.79,4.59</c>.</summary>
    private static readonly Point[] Tick = [new(1.73, 12.91), new(8.1, 19.28), new(22.79, 4.59)];

    /// <summary><c>M2 12H22</c>.</summary>
    private static readonly Point[] Bar = [new(2, 12), new(22, 12)];

    /// <summary>The checkbox's <c>checked</c>.</summary>
    public static readonly StyledProperty<bool> CheckedProperty =
        AvonicCheckbox.CheckedProperty.AddOwner<CheckboxMark>(new StyledPropertyMetadata<bool>(defaultBindingMode: Avalonia.Data.BindingMode.OneWay));

    /// <summary>The checkbox's <c>indeterminate</c>: the bar, and shown.</summary>
    public static readonly StyledProperty<bool> IndeterminateProperty =
        AvonicCheckbox.IndeterminateProperty.AddOwner<CheckboxMark>(new StyledPropertyMetadata<bool>(defaultBindingMode: Avalonia.Data.BindingMode.OneWay));

    /// <summary><c>stroke</c>: <c>--checkmark-color</c>.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<CheckboxMark, IBrush?>(nameof(Stroke));

    /// <summary><c>stroke-width</c>: <c>--checkmark-width</c>, in view box units.</summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<CheckboxMark, double>(nameof(StrokeThickness), 3);

    private bool _shown;
    private TimeSpan? _start;
    private IDisposable? _frame;
    private IDisposable? _firstFrame;
    private bool _attached;

    static CheckboxMark()
    {
        AffectsRender<CheckboxMark>(IndeterminateProperty, StrokeProperty, StrokeThicknessProperty);
    }

    public bool Checked
    {
        get => GetValue(CheckedProperty);
        set => SetValue(CheckedProperty, value);
    }

    public bool Indeterminate
    {
        get => GetValue(IndeterminateProperty);
        set => SetValue(IndeterminateProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>What <c>stroke-dashoffset</c> is now: 30 hidden, 0 drawn.</summary>
    internal double DashOffset { get; private set; } = DashLength;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;

        // A first style: no transition until the next frame.
        _firstFrame?.Dispose();
        _firstFrame = AvonicConfig.RequestFrame(this, () => _firstFrame = null);
        Snap();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        _firstFrame?.Dispose();
        _firstFrame = null;
        Snap();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CheckedProperty || change.Property == IndeterminateProperty)
        {
            var shown = Checked || Indeterminate;
            if (shown == _shown)
            {
                return;
            }

            _shown = shown;
            if (shown && _attached && _firstFrame is null && IsEffectivelyVisible)
            {
                _start = AvonicConfig.Now() + TransitionDelay;
                DashOffset = DashLength;
                _frame ??= AvonicConfig.RequestFrame(this, OnFrame);
            }
            else
            {
                Snap();
            }

            InvalidateVisual();
        }
    }

    /// <summary>The end of any transition: the shown or hidden offset.</summary>
    private void Snap()
    {
        _shown = Checked || Indeterminate;
        _frame?.Dispose();
        _frame = null;
        _start = null;
        DashOffset = _shown ? 0 : DashLength;
        InvalidateVisual();
    }

    private void OnFrame()
    {
        _frame = null;
        if (_start is not { } start)
        {
            return;
        }

        var progress = (AvonicConfig.Now() - start).TotalMilliseconds / TransitionDuration.TotalMilliseconds;
        DashOffset = DashLength * (1 - Math.Clamp(progress, 0, 1));
        if (progress >= 1)
        {
            _start = null;
        }
        else
        {
            _frame = AvonicConfig.RequestFrame(this, OnFrame);
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        // opacity: 0 while hidden, and nothing of the dash is left at an offset of 30.
        var visible = DashLength - DashOffset;
        if (!_shown || visible <= 0 || Stroke is not { } stroke || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        // preserveAspectRatio="xMidYMid meet"
        var scale = Math.Min(Bounds.Width, Bounds.Height) / 24;
        var offset = new Vector((Bounds.Width - 24 * scale) / 2, (Bounds.Height - 24 * scale) / 2);
        var points = Indeterminate ? Bar : Tick;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0] * scale + offset, isFilled: false);
            var left = visible;
            for (var i = 1; i < points.Length && left > 0; i++)
            {
                var from = points[i - 1];
                var to = points[i];
                var length = Point.Distance(from, to);
                var end = left >= length ? to : from + (to - from) * (left / length);
                g.LineTo(end * scale + offset);
                left -= length;
            }

            g.EndFigure(isClosed: false);
        }

        // The SVG defaults: butt caps and miter joins.
        context.DrawGeometry(null, new Pen(stroke, StrokeThickness * scale, lineCap: PenLineCap.Flat, lineJoin: PenLineJoin.Miter), geometry);
    }
}
