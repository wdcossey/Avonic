using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>PickerColumnChangeEventDetail</c>: the column's new value.</summary>
public class PickerColumnChangeEventArgs(RoutedEvent routedEvent, object? value) : RoutedEventArgs(routedEvent)
{
    public object? Value { get; } = value;
}

/// <summary>
/// Ionic's internal <c>PickerChangeEventDetail</c>, the detail of <c>ionInputModeChange</c>: whether the picker takes typed digits,
/// and for which column, or every numeric column when it's null.
/// </summary>
internal sealed class PickerInputModeChange(bool useInputMode, AvonicPickerColumn? inputModeColumn) : EventArgs
{
    public bool UseInputMode { get; } = useInputMode;

    public AvonicPickerColumn? InputModeColumn { get; } = inputModeColumn;
}
