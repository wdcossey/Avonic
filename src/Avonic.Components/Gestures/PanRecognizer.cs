namespace Avonic.Components.Gestures;

/// <summary>
/// Ionic's <c>createPanRecognizer</c> (<c>utils/gesture/recognizers.js</c>): decides once, when the pointer has moved
/// <c>threshold</c> from where it started, whether the movement is within <c>maxAngle</c> of the axis, either way along it.
/// </summary>
internal sealed class PanRecognizer(GestureDirection direction, double thresh, double maxAngle)
{
    private readonly bool _isDirX = direction == GestureDirection.X;
    private readonly double _maxCosine = Math.Cos(maxAngle * (Math.PI / 180));
    private readonly double _threshold = thresh * thresh;
    private double _startX;
    private double _startY;
    private bool _dirty;
    private int _isPan;

    public void Start(double x, double y)
    {
        _startX = x;
        _startY = y;
        _isPan = 0;
        _dirty = true;
    }

    /// <summary>True once, at the move that reaches the threshold; <see cref="IsGesture"/> then says whether it was along the axis.</summary>
    public bool Detect(double x, double y)
    {
        if (!_dirty)
        {
            return false;
        }

        var deltaX = x - _startX;
        var deltaY = y - _startY;
        var distance = deltaX * deltaX + deltaY * deltaY;
        if (distance < _threshold)
        {
            return false;
        }

        var hypotenuse = Math.Sqrt(distance);
        var cosine = (_isDirX ? deltaX : deltaY) / hypotenuse;
        if (cosine > _maxCosine)
        {
            _isPan = 1;
        }
        else if (cosine < -_maxCosine)
        {
            _isPan = -1;
        }
        else
        {
            _isPan = 0;
        }

        _dirty = false;
        return true;
    }

    public bool IsGesture() => _isPan != 0;
}
