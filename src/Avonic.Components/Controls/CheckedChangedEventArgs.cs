namespace Avonic.Components.Controls;

/// <summary>
/// Event data for the <c>CheckedChanged</c> routed event raised by checkable Avonic controls.
/// </summary>
public sealed class CheckedChangedEventArgs : RoutedEventArgs
{
    /// <summary>Whether the control is now checked.</summary>
    public bool IsChecked { get; }

    /// <summary>
    /// The value associated with the control at the time of the change.
    /// Used by <c>AvonicRadioGroup</c> to propagate group selection.
    /// </summary>
    public object? Value { get; }

    public CheckedChangedEventArgs(RoutedEvent routedEvent, object source, bool isChecked, object? value)
        : base(routedEvent, source)
    {
        IsChecked = isChecked;
        Value     = value;
    }
}
