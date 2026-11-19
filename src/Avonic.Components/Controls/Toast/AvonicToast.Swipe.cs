using Avalonia;
using Avalonia.Input;
using Avonic.Components.Animation;
using Avonic.Components.Gestures;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>gestures/swipe-to-dismiss.js</c>: the toast's swipe, on its wrapper.</summary>
public partial class AvonicToast
{
    /// <summary>Ionic's <c>OVERLAY_GESTURE_PRIORITY</c>.</summary>
    internal const int SwipeGesturePriority = 39;

    /// <summary><c>DISMISS_THRESHOLD</c>: the step at which a swipe dismisses; half of it either way in the middle.</summary>
    internal const double DismissThreshold = 0.5;

    /// <summary>The longest the release's animation takes, in ms.</summary>
    internal const double MaxReleaseDuration = 200;

    private Gesture? _gesture;
    private AvonicAnimation? _swipeAnimation;

    /// <summary>The gesture, while there's one.</summary>
    internal Gesture? SwipeGestureInstance => _gesture;

    /// <summary><c>prefersSwipeGesture</c>.</summary>
    private bool PrefersSwipeGesture => ToastSwipeGesture == ToastSwipeGestureDirection.Vertical;

    /// <summary><c>swipeGestureChanged</c>: the gesture goes, and a new one comes if the toast is presented and still wants one.</summary>
    private void SwipeGestureChanged()
    {
        DestroySwipeGesture();
        if (_presented && PrefersSwipeGesture && _lastPosition is { } position)
        {
            CreateSwipeGesture(position);
        }
    }

    /// <summary>
    /// <c>destroySwipeGesture</c>, and its animation, which Ionic leaves held on the wrapper at the place the present put it: here it
    /// goes too, so that it can't hold an old place over a later present's.
    /// </summary>
    private void DestroySwipeGesture()
    {
        _gesture?.Destroy();
        _gesture = null;
        _swipeAnimation?.Destroy();
        _swipeAnimation = null;
    }

    /// <summary>
    /// <c>createSwipeToDismissGesture</c>: an animation of the wrapper's transform, held, which the drag moves 1:1, from where the
    /// present put it to off the edge it's at; and the gesture on the wrapper.
    /// </summary>
    private void CreateSwipeGesture(ToastPresentOptions toastPosition)
    {
        if (_wrapper is not { } wrapper)
        {
            return;
        }

        var position = Position;
        var hostHeight = Math.Round(Bounds.Height);
        var box = WrapperBox(wrapper);
        double maxSwipeDistance;
        var stepOffset = position == ToastPosition.Middle ? 0.5 : 0;
        var inversionFactor = position == ToastPosition.Top ? -1 : 1;
        var topPosition = OffsetForMiddle(hostHeight, box.Height);
        AnimationKeyframe[] upDown =
        [
            new(0) { ["transform"] = Px(-(topPosition + box.Height)) },
            new(0.5) { ["transform"] = Px(0) },
            new(1) { ["transform"] = Px(topPosition + box.Height) },
        ];

        // The value doesn't matter, as long as it's above 0, so that the animation doesn't jump to its end when the drag starts.
        var swipeAnimation = AvonicAnimation.Create("toast-swipe-to-dismiss-animation").AddElement(wrapper).Duration(100);
        _swipeAnimation?.Destroy();
        _swipeAnimation = swipeAnimation;
        switch (position)
        {
            case ToastPosition.Middle:
                maxSwipeDistance = hostHeight + box.Height;
                swipeAnimation.Keyframes(upDown);
                swipeAnimation.ProgressStart(true, 0.5);
                break;
            case ToastPosition.Top:
                maxSwipeDistance = box.Bottom;
                swipeAnimation.Keyframes([
                    new AnimationKeyframe(0) { ["transform"] = Px(toastPosition.Top) },
                    new AnimationKeyframe(1) { ["transform"] = "translateY(-100%)" },
                ]);
                swipeAnimation.ProgressStart(true, 0);
                break;
            default:
                maxSwipeDistance = hostHeight - box.Top;
                swipeAnimation.Keyframes([
                    new AnimationKeyframe(0) { ["transform"] = Px(toastPosition.Bottom) },
                    new AnimationKeyframe(1) { ["transform"] = "translateY(100%)" },
                ]);
                swipeAnimation.ProgressStart(true, 0);
                break;
        }

        double ComputeStep(double delta) => delta * inversionFactor / maxSwipeDistance;

        Gesture? gesture = null;
        void OnMove(GestureDetail detail) => swipeAnimation.ProgressStep(stepOffset + ComputeStep(detail.DeltaY));

        void OnEnd(GestureDetail detail)
        {
            var velocity = detail.VelocityY;
            var threshold = (detail.DeltaY + velocity * 1000) / maxSwipeDistance * inversionFactor;

            // Off for the rest of the animation; back on if the toast goes back to where it was.
            gesture!.Enable(false);
            bool shouldDismiss;
            int playTo;
            double step;
            double remainingDistance;
            if (position == ToastPosition.Middle)
            {
                shouldDismiss = threshold >= DismissThreshold / 2 || threshold <= -DismissThreshold / 2;
                playTo = 1;
                step = 0;
                var startOffset = WrapperBox(wrapper).Top - topPosition;
                var offsetFactor = detail.DeltaY <= 0 ? -1 : 1;
                var endOffset = (topPosition + box.Height) * offsetFactor;
                swipeAnimation.Keyframes([
                    new AnimationKeyframe(0) { ["transform"] = Px(startOffset) },
                    new AnimationKeyframe(1) { ["transform"] = Px(shouldDismiss ? endOffset : 0) },
                ]);
                remainingDistance = endOffset - startOffset;
            }
            else
            {
                shouldDismiss = threshold >= DismissThreshold;
                playTo = shouldDismiss ? 1 : 0;
                step = ComputeStep(detail.DeltaY);
                remainingDistance = (shouldDismiss ? 1 - step : step) * maxSwipeDistance;
            }

            // As quick as the flick, but no slower than 200 ms.
            var duration = Math.Min(Math.Abs(remainingDistance) / Math.Abs(velocity), MaxReleaseDuration);
            if (double.IsNaN(duration))
            {
                duration = 0;
            }

            swipeAnimation
                .OnFinish((_, _) =>
                {
                    if (shouldDismiss)
                    {
                        // The dismiss destroys the gesture and this animation (OnDismissed).
                        DismissFromInput(OverlayRoles.Gesture);
                    }
                    else
                    {
                        if (position == ToastPosition.Middle)
                        {
                            swipeAnimation.Keyframes(upDown).ProgressStart(true, 0.5);
                        }
                        else
                        {
                            swipeAnimation.ProgressStart(true, 0);
                        }

                        gesture.Enable(true);
                    }
                }, oneTimeCallback: true)
                .ProgressEnd(playTo, step, duration);
        }

        gesture = Gesture.Create(new GestureConfig
        {
            Element = (InputElement)wrapper,
            GestureName = "toast-swipe-to-dismiss",
            GesturePriority = SwipeGesturePriority,
            Direction = GestureDirection.Y,
            OnMove = OnMove,
            OnEnd = OnEnd,
        });
        _gesture = gesture;
        gesture.Enable(true);
    }

    /// <summary>The wrapper's <c>getBoundingClientRect()</c>, in the toast, which covers the window: its box with its transform.</summary>
    private Rect WrapperBox(Visual wrapper) =>
        wrapper.TransformToVisual(this) is { } transform ? new Rect(wrapper.Bounds.Size).TransformToAABB(transform) : wrapper.Bounds;

    private static string Px(double value) =>
        "translateY(" + value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "px)";
}
