using Avalonia;
using Avonic.Components.Animation;

namespace Avonic.Components.Controls;

/// <summary>
/// What md's pulling animation (<c>createPullingAnimation</c> in <c>refresher.utils.js</c>) makes of the pulling icon at one step:
/// a pure function, so that tests can pin it to what Chromium computes and <see cref="AvonicRefresherContent"/> can apply it.
/// </summary>
/// <param name="IconTransform">The <c>.refresher-pulling-icon</c>'s transform, about its centre.</param>
/// <param name="SpinnerArrowOpacity">The <c>.spinner-arrow-container</c>'s opacity.</param>
/// <param name="CircleDash">The pulling spinner's <c>circle</c>'s <c>stroke-dasharray</c> dash, in the <c>viewBox</c>'s units.</param>
/// <param name="SpinnerRotation">The pulling <c>ion-spinner</c>'s rotation, in degrees, clockwise.</param>
/// <param name="ArrowContainerRotation">The <c>.arrow-container</c>'s rotation, in degrees, clockwise.</param>
/// <param name="ArrowTransform">The arrow <c>ion-icon</c>'s transform, about its centre.</param>
internal readonly record struct RefresherPullFrame(
    Matrix IconTransform, double SpinnerArrowOpacity, double CircleDash, double SpinnerRotation, double ArrowContainerRotation,
    Matrix ArrowTransform)
{
    /// <summary>The parent animation's <c>duration(1000)</c>, in milliseconds.</summary>
    internal const double Duration = 1000;

    /// <summary><c>progressEnd(0, progress, 500)</c>: the time the whole way back would take, in milliseconds.</summary>
    internal const double CancelDuration = 500;

    /// <summary><c>createSnapBackAnimation</c>'s <c>duration(125)</c>, in milliseconds.</summary>
    internal const double SnapBackDuration = 125;

    /// <summary>Past this progress, letting go refreshes; at it or below, the pull is cancelled.</summary>
    internal const double RefreshThreshold = 0.4;

    /// <summary><c>(deltaY / 180) * 0.5</c>: how far the finger goes for the whole animation.</summary>
    internal const double PullDistance = 360;

    /// <summary><c>translateY(var(--ion-pulling-refresher-translate, 100px))</c>: the snap back starts at progress × this.</summary>
    internal const double SnapBackDistance = 100;

    /// <summary><c>setAnimationStep</c> clamps a step below 1, so the end keyframe is never quite reached.</summary>
    internal const double MaxStep = 0.9999;

    /// <summary><c>spinner.css</c>'s <c>stroke-dasharray: 80px, 200px</c>, which the pulling circle keeps once its animation is gone.</summary>
    internal const double BaseCircleDash = 80;

    /// <summary><c>easing('ease-out')</c>, which the child animations inherit, as Chromium solves it.</summary>
    private static readonly CubicBezierEasing EaseOut = new(0, 0, 0.58, 1);

    /// <summary>
    /// The frame at <paramref name="step"/>, a time of the 1,000 ms animation from 0 to 1, eased by <c>ease-out</c> and then read
    /// off each keyframe list, which is linear between its offsets. <paramref name="refresherHeight"/> is the <c>ion-refresher</c>'s
    /// <c>clientHeight</c>, which the icon starts that far above its place.
    /// </summary>
    public static RefresherPullFrame At(double step, RefresherAnimationType type, double refresherHeight)
    {
        var t = EaseOut.Ease(Math.Clamp(step, 0, MaxStep));

        // translateY(-height) to translateY(100px); with scale(0) to scale(1) before it for the scale type.
        var translateY = -refresherHeight + (SnapBackDistance + refresherHeight) * t;
        var icon = type == RefresherAnimationType.Scale
            ? Matrix.CreateTranslation(0, translateY) * Matrix.CreateScale(t, t)
            : Matrix.CreateTranslation(0, translateY);

        // .spinner-arrow-container: opacity 0.3 at 0 and 0.45, 1 at 0.55 and 1
        var opacity = Keyframes(t, (0, 0.3), (0.45, 0.3), (0.55, 1), (1, 1));

        // circle: stroke-dasharray 1px at 0 and 0.2, 100px at 0.55 and 1
        var dash = Keyframes(t, (0, 1), (0.2, 1), (0.55, 100), (1, 100));

        // ion-spinner: rotate(-90deg) to rotate(210deg)
        var spinner = -90 + 300 * t;

        // .arrow-container: rotate 0 at 0 and 0.3, 280deg at 0.55, 400deg at 1
        var arrowContainer = Keyframes(t, (0, 0), (0.3, 0), (0.55, 280), (1, 400));

        // the arrow: translateX(2px) scale(0) at 0 and 0.3, translateX(-1.5px) scale(1) at 0.55 and 1
        var arrowX = Keyframes(t, (0, 2), (0.3, 2), (0.55, -1.5), (1, -1.5));
        var arrowScale = Keyframes(t, (0, 0), (0.3, 0), (0.55, 1), (1, 1));
        var arrow = Matrix.CreateScale(arrowScale, arrowScale) * Matrix.CreateTranslation(arrowX, 0);

        return new RefresherPullFrame(icon, opacity, dash, spinner, arrowContainer, arrow);
    }

    /// <summary>The value at <paramref name="t"/> of keyframes at the given offsets, linear between them.</summary>
    private static double Keyframes(double t, params (double Offset, double Value)[] frames)
    {
        for (var i = 1; i < frames.Length; i++)
        {
            var (offset, value) = frames[i];
            if (t <= offset)
            {
                var (previousOffset, previousValue) = frames[i - 1];
                var span = offset - previousOffset;
                return span <= 0 ? value : previousValue + (value - previousValue) * (t - previousOffset) / span;
            }
        }

        return frames[^1].Value;
    }
}
