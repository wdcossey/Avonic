using Avalonia.Input;

namespace Avonic.Components.Gestures;

/// <summary>
/// Ionic's <c>GestureDetail</c>. One object per gesture, updated in place and passed to every callback, as Ionic's is, so copy
/// what you need to keep. Positions are in the top level's coordinates, as <c>pageX</c> is the page's. Times are milliseconds on
/// Avonic's clock, and velocities pixels per millisecond.
/// </summary>
public sealed class GestureDetail
{
    /// <summary>Always <c>"pan"</c>.</summary>
    public string Type { get; } = "pan";

    /// <summary>Where the gesture started: the press, then the move that captured.</summary>
    public double StartX { get; internal set; }

    /// <inheritdoc cref="StartX" />
    public double StartY { get; internal set; }

    /// <summary>When the gesture started: the press, then the move that captured.</summary>
    public double StartTime { get; internal set; }

    /// <summary>Where the pointer is.</summary>
    public double CurrentX { get; internal set; }

    /// <inheritdoc cref="CurrentX" />
    public double CurrentY { get; internal set; }

    /// <summary>An exponential average of the speed: 0.7 of the last move's and 0.3 of what it was.</summary>
    public double VelocityX { get; internal set; }

    /// <inheritdoc cref="VelocityX" />
    public double VelocityY { get; internal set; }

    /// <summary><see cref="CurrentX"/> less <see cref="StartX"/>, as of the last recalculation.</summary>
    public double DeltaX { get; internal set; }

    /// <inheritdoc cref="DeltaX" />
    public double DeltaY { get; internal set; }

    /// <summary>When the last event was.</summary>
    public double CurrentTime { get; internal set; }

    /// <summary>The last pointer event, or <see langword="null"/> before the first.</summary>
    public PointerEventArgs? Event { get; internal set; }

    /// <summary>Anything a control keeps here, from <c>CanStart</c> to <c>OnEnd</c>. Not cleared between presses, as Ionic's isn't.</summary>
    public object? Data { get; set; }
}
