using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>What <see cref="AvonicItemOptions.IonSwipe"/> carries: Ionic's <c>{ side }</c>.</summary>
public sealed class ItemOptionsSwipeEventArgs(RoutedEvent routedEvent, object source, ItemOptionsSide side)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>side</c>: the options' <see cref="AvonicItemOptions.Side"/>.</summary>
    public ItemOptionsSide Side { get; } = side;
}
