using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>InputOtpInputEventDetail</c>: what <see cref="AvonicInputOtp.IonInput"/> carries.</summary>
public sealed class InputOtpInputEventArgs(RoutedEvent routedEvent, object source, string? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>value</c>: the value after the edit.</summary>
    public string? Value { get; } = value;
}

/// <summary>Ionic <c>InputOtpChangeEventDetail</c>: what <see cref="AvonicInputOtp.IonChange"/> carries.</summary>
public sealed class InputOtpChangeEventArgs(RoutedEvent routedEvent, object source, string? value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>value</c>: the value the user committed.</summary>
    public string? Value { get; } = value;
}

/// <summary>Ionic <c>InputOtpCompleteEventDetail</c>: what <see cref="AvonicInputOtp.IonComplete"/> carries.</summary>
public sealed class InputOtpCompleteEventArgs(RoutedEvent routedEvent, object source, string value)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>Ionic <c>value</c>: the value, with every box filled.</summary>
    public string Value { get; } = value;
}
