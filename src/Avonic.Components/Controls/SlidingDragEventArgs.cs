using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Event data for <see cref="AvonicItemSliding.Drag"/>.</summary>
public class SlidingDragEventArgs : RoutedEventArgs
{
    /// <summary>Current horizontal translation of the sliding content, in device-independent pixels.</summary>
    public double TranslateX { get; }

    public SlidingDragEventArgs(RoutedEvent routedEvent, object source, double translateX)
        : base(routedEvent, source)
    {
        TranslateX = translateX;
    }
}
