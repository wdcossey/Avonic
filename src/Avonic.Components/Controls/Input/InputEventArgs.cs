using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>InputInputEventDetail</c>: what <see cref="AvonicInput.IonInput"/> carries.</summary>
public sealed class InputInputEventArgs(RoutedEvent routedEvent, object source, string? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>value</c>: the input's value after the edit.</summary>
    public string? Value { get; } = value;
}

/// <summary>Ionic <c>InputChangeEventDetail</c>: what <see cref="AvonicInput.IonChange"/> carries.</summary>
public sealed class InputChangeEventArgs(RoutedEvent routedEvent, object source, string? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>value</c>: the value the user committed.</summary>
    public string? Value { get; } = value;
}
