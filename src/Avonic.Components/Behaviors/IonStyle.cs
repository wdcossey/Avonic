using Avalonia.Interactivity;

namespace Avonic.Components.Behaviors;

/// <summary>
/// Ionic's internal <c>ionStyle</c> event: how a child tells a host control about itself. A label or
/// select raises it, and an <c>ion-item</c> turns each true key into a pseudoclass. It stops at the
/// first control that handles it.
/// </summary>
public static class IonStyle
{
    public static readonly RoutedEvent<IonStyleEventArgs> IonStyleEvent =
        RoutedEvent.Register<IonStyleEventArgs>("IonStyle", RoutingStrategies.Bubble, typeof(IonStyle));
}

/// <summary>The event's detail, such as <c>{ "label": true, "label-stacked": false }</c>.</summary>
public sealed class IonStyleEventArgs(IReadOnlyDictionary<string, bool> styles) : RoutedEventArgs(IonStyle.IonStyleEvent)
{
    public IReadOnlyDictionary<string, bool> Styles { get; } = styles;
}
