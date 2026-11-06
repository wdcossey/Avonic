using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ScrollBaseDetail</c>: the detail of <c>ionScrollStart</c> and <c>ionScrollEnd</c>.</summary>
public class ScrollBaseDetailEventArgs(RoutedEvent routedEvent, bool isScrolling) : RoutedEventArgs(routedEvent)
{
    /// <summary>Whether the content is scrolling.</summary>
    public bool IsScrolling { get; } = isScrolling;
}

/// <summary>
/// Ionic <c>ScrollDetail</c>: the detail of <c>ionScroll</c>. Times are in milliseconds on
/// <see cref="AvonicConfig.Now"/>, and velocities in pixels per millisecond, as in Ionic.
/// </summary>
public sealed class ScrollDetailEventArgs(RoutedEvent routedEvent) : ScrollBaseDetailEventArgs(routedEvent, true)
{
    /// <summary>Always <c>scroll</c>.</summary>
    public string Type => "scroll";

    public double ScrollTop { get; init; }
    public double ScrollLeft { get; init; }
    public double StartX { get; init; }
    public double StartY { get; init; }
    public double StartTime { get; init; }
    public double CurrentX { get; init; }
    public double CurrentY { get; init; }
    public double VelocityX { get; init; }
    public double VelocityY { get; init; }
    public double DeltaX { get; init; }
    public double DeltaY { get; init; }
    public double CurrentTime { get; init; }
}
