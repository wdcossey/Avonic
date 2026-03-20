namespace Avonic.Components.Controls;

/// <summary>
/// Event args for <see cref="AvonicRange.ValueChangedEvent"/>.
/// Carries the old and new values produced by a user interaction.
/// </summary>
public class RangeValueChangedEventArgs : RoutedEventArgs
{
    /// <summary>The value before the change.</summary>
    public double OldValue { get; }

    /// <summary>The value after the change.</summary>
    public double NewValue { get; }

    public RangeValueChangedEventArgs(
        RoutedEvent routedEvent,
        object      source,
        double      oldValue,
        double      newValue)
        : base(routedEvent, source)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
