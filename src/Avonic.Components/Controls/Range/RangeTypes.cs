using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ion-range</c> <c>labelPlacement</c>.</summary>
public enum RangeLabelPlacement
{
    /// <summary>Before the slider: left in LTR, right in RTL. The default.</summary>
    Start,

    /// <summary>After the slider.</summary>
    End,

    /// <summary>As <see cref="Start"/>, 100 px wide.</summary>
    Fixed,

    /// <summary>Above the slider, at 0.75.</summary>
    Stacked,
}

/// <summary>
/// Ionic's <c>KnobName</c>, <c>'A' | 'B'</c>: a knob's identity, which stays as the knobs cross. A is the only knob with one.
/// </summary>
public enum RangeKnob
{
    A,
    B,
}

/// <summary><c>RangeChangeEventDetail</c>, for <see cref="AvonicRange.IonChange"/> and <see cref="AvonicRange.IonInput"/>.</summary>
public sealed class RangeChangeEventArgs(RoutedEvent routedEvent, object source, RangeValue value) : RoutedEventArgs(routedEvent, source)
{
    public RangeValue Value { get; } = value;
}

/// <summary>
/// <c>RangeKnobMoveStartEventDetail</c> and <c>RangeKnobMoveEndEventDetail</c>, which are the same, for
/// <see cref="AvonicRange.IonKnobMoveStart"/> and <see cref="AvonicRange.IonKnobMoveEnd"/>.
/// </summary>
public sealed class RangeKnobMoveEventArgs(RoutedEvent routedEvent, object source, RangeValue value) : RoutedEventArgs(routedEvent, source)
{
    public RangeValue Value { get; } = value;
}
