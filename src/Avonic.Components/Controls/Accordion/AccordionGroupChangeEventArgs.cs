using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>AccordionGroupChangeEventDetail</c>: the group's new value.</summary>
public sealed class AccordionGroupChangeEventArgs(RoutedEvent routedEvent, object? value) : RoutedEventArgs(routedEvent)
{
    /// <summary>A <see cref="string"/>, a <see cref="string"/> array with <see cref="AvonicAccordionGroup.Multiple"/>, or null.</summary>
    public object? Value { get; } = value;
}
