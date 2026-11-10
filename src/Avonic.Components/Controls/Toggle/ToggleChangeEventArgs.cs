using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ToggleChangeEventDetail</c>: what <see cref="AvonicToggle.IonChange"/> carries.</summary>
public sealed class ToggleChangeEventArgs(RoutedEvent routedEvent, object source, bool @checked, string? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>checked</c>: the state the toggle has just turned to.</summary>
    public bool Checked { get; } = @checked;

    /// <summary>Ionic <c>value</c>: the toggle's <see cref="AvonicToggle.Value"/>, as it is, even if it's null.</summary>
    public string? Value { get; } = value;
}
