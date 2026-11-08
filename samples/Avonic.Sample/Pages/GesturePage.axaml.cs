using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avonic.Components.Controls;
using Avonic.Components.Gestures;

namespace Avonic.Sample.Pages;

[SamplePage("gesture", Order = 4)]
public partial class GesturePage : UserControl
{
    private Gesture? _gesture;
    private readonly TranslateTransform _knobOffset = new();
    private double _offset;
    private double _startOffset;

    public GesturePage()
    {
        InitializeComponent();
        Knob.RenderTransform = _knobOffset;
        for (var i = 1; i <= 30; i++)
        {
            Rows.Children.Add(new AvonicItem { Content = new AvonicLabel { Content = $"Row {i}" } });
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _gesture = Gesture.Create(new GestureConfig
        {
            Element = Track,
            GestureName = "sample-knob",
            GesturePriority = 100,
            Threshold = 10,
            OnStart = detail =>
            {
                _startOffset = _offset;
                Show("Captured", detail);
            },
            OnMove = detail =>
            {
                var max = Math.Max(0, Track.Bounds.Width - Knob.Bounds.Width);
                _offset = Math.Clamp(_startOffset + detail.DeltaX, 0, max);
                _knobOffset.X = _offset;
                Show("Captured", detail);
            },
            OnEnd = detail => Show("Ended", detail),
            NotCaptured = detail => Show("Not captured", detail),
        });
        _gesture.Enable();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _gesture?.Destroy();
        _gesture = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void Show(string state, GestureDetail detail)
    {
        Status.Text = state;
        Detail.Text = string.Create(CultureInfo.InvariantCulture,
            $"delta {detail.DeltaX:0}, {detail.DeltaY:0}  velocity {detail.VelocityX:0.00}, {detail.VelocityY:0.00} px/ms");
    }
}
