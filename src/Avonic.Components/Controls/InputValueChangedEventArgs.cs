using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>
/// Event data for <see cref="AvonicInput.ValueChangedEvent"/> and
/// <see cref="AvonicTextarea.ValueChangedEvent"/>.
/// </summary>
public class InputValueChangedEventArgs : RoutedEventArgs
{
    /// <summary>The value before the change.</summary>
    public string? OldValue { get; }

    /// <summary>The value after the change.</summary>
    public string? NewValue { get; }

    public InputValueChangedEventArgs(
        RoutedEvent routedEvent,
        object? source,
        string? oldValue,
        string? newValue)
        : base(routedEvent, source)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
