namespace Avonic.Components.Animation;

/// <summary>
/// A CSS <c>cubic-bezier(x1, y1, x2, y2)</c> in XAML, as a <see cref="CubicBezierEasing"/>:
/// <c>Easing="{a:CubicBezier 0.4, 0, 0.2, 1}"</c>. Avalonia reads <c>Easing="0.4,0,0.2,1"</c> as its own <c>SplineEasing</c>,
/// which is about 2e-3 out.
/// </summary>
public sealed class CubicBezierExtension(double x1, double y1, double x2, double y2)
{
    public CubicBezierEasing ProvideValue(IServiceProvider serviceProvider) => new(x1, y1, x2, y2);
}
