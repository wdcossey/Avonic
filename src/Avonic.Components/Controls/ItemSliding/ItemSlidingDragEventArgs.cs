using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>What <see cref="AvonicItemSliding.IonDrag"/> carries: Ionic's <c>{ amount, ratio }</c>.</summary>
public sealed class ItemSlidingDragEventArgs(RoutedEvent routedEvent, object source, double amount, double ratio)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>amount</c>: how far the item is open, in pixels, positive with the right options showing.</summary>
    public double Amount { get; } = amount;

    /// <summary>Ionic <c>ratio</c>: <see cref="Amount"/> over the width of the options it shows, negative for the left.</summary>
    public double Ratio { get; } = ratio;
}
