using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>TextareaInputEventDetail</c>: what <see cref="AvonicTextarea.IonInput"/> carries.</summary>
public sealed class TextareaInputEventArgs(RoutedEvent routedEvent, object source, string? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>value</c>: the textarea's value after the edit.</summary>
    public string? Value { get; } = value;
}

/// <summary>Ionic <c>TextareaChangeEventDetail</c>: what <see cref="AvonicTextarea.IonChange"/> carries.</summary>
public sealed class TextareaChangeEventArgs(RoutedEvent routedEvent, object source, string? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>value</c>: the value the user committed.</summary>
    public string? Value { get; } = value;
}
