using Avalonia.Input;

namespace Avonic.Components.Gestures;

/// <summary>
/// Ionic's <c>GestureConfig</c>, for <see cref="Gesture.Create"/>. Read once, when the gesture is created, as Ionic reads it.
/// </summary>
public sealed class GestureConfig
{
    /// <summary>Ionic <c>el</c>: the element whose presses start the gesture, including presses on anything inside it.</summary>
    public required InputElement Element { get; init; }

    /// <summary>
    /// Ionic <c>gestureName</c>: what a <see cref="GestureBlocker"/> disables it by, and what
    /// <see cref="GestureController.IonGestureCaptured"/> reports.
    /// </summary>
    public required string GestureName { get; init; }

    /// <summary>
    /// Ionic <c>gesturePriority</c>, 0 by default. Of the gestures a press has started, only the one with the highest priority can
    /// capture, and on a tie the one created last.
    /// </summary>
    public int GesturePriority { get; init; }

    /// <summary>Ionic <c>disableScroll</c>: <see cref="GestureController.IsScrollDisabled"/> from the capture to the release.</summary>
    public bool DisableScroll { get; init; }

    /// <summary>Ionic <c>direction</c>, <see cref="GestureDirection.X"/> by default.</summary>
    public GestureDirection Direction { get; init; } = GestureDirection.X;

    /// <summary>
    /// Ionic <c>threshold</c>: how far, in pixels, the pointer moves before the gesture decides. 10 by default, and 0 captures at
    /// the press.
    /// </summary>
    public double Threshold { get; init; } = 10;

    /// <summary>Ionic <c>maxAngle</c>: how far from <see cref="Direction"/>, in degrees, a movement can be and still capture. 40 by default.</summary>
    public double MaxAngle { get; init; } = 40;

    /// <summary>Ionic <c>blurOnStart</c>: clear the focus before <see cref="OnStart"/>.</summary>
    public bool BlurOnStart { get; init; }

    /// <summary>Ionic <c>canStart</c>: asked at every press. <see langword="false"/> ignores the press, with no <see cref="NotCaptured"/>.</summary>
    public Func<GestureDetail, bool>? CanStart { get; init; }

    /// <summary>
    /// Ionic <c>onWillStart</c>: awaited after the capture, before <see cref="OnStart"/>. Moves are dropped until it completes, and
    /// a release before then fires nothing.
    /// </summary>
    public Func<GestureDetail, Task>? OnWillStart { get; init; }

    /// <summary>Ionic <c>onStart</c>: the gesture captured.</summary>
    public Action<GestureDetail>? OnStart { get; init; }

    /// <summary>Ionic <c>onMove</c>: once a frame while captured, with the first move of that frame.</summary>
    public Action<GestureDetail>? OnMove { get; init; }

    /// <summary>Ionic <c>onEnd</c>: a captured gesture ended, by release, cancel or <c>Enable(false)</c>.</summary>
    public Action<GestureDetail>? OnEnd { get; init; }

    /// <summary>
    /// Ionic <c>notCaptured</c>: a started gesture ended without capturing, by going the wrong way, losing to a higher priority, or
    /// a release or cancel before the threshold.
    /// </summary>
    public Action<GestureDetail>? NotCaptured { get; init; }
}
