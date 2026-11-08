namespace Avonic.Components.Gestures;

/// <summary>The detail of Ionic's <c>ionGestureCaptured</c>.</summary>
public sealed class GestureCapturedEventArgs(string gestureName) : EventArgs
{
    /// <summary>The <see cref="GestureConfig.GestureName"/> of the gesture that captured.</summary>
    public string GestureName { get; } = gestureName;
}
