using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-segment-button</c>'s <c>layout</c>: where the icon goes, or what's hidden.</summary>
public enum SegmentButtonLayout
{
    IconTop,
    IconBottom,
    IconStart,
    IconEnd,
    IconHide,
    LabelHide,
}

/// <summary>Ionic <c>SegmentChangeEventDetail</c>: the segment's new value.</summary>
public class SegmentChangeEventArgs(RoutedEvent routedEvent, object? value) : RoutedEventArgs(routedEvent)
{
    public object? Value { get; } = value;
}

/// <summary>Ionic <c>SegmentViewScrollEvent</c>: how far the view has scrolled, and whether a person did it.</summary>
public class SegmentViewScrollEventArgs(RoutedEvent routedEvent, double scrollRatio, bool isManualScroll) : RoutedEventArgs(routedEvent)
{
    /// <summary>The offset over the most it can scroll: 0 at the first content, 1 at the last.</summary>
    public double ScrollRatio { get; } = scrollRatio;

    /// <summary>False while <c>SetContent</c> is scrolling it, and for 100 ms after.</summary>
    public bool IsManualScroll { get; } = isManualScroll;
}
