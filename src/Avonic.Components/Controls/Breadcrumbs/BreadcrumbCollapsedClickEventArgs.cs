using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic's <c>BreadcrumbCollapsedClickEventDetail</c>, for <see cref="AvonicBreadcrumbs.IonCollapsedClick"/>.</summary>
public sealed class BreadcrumbCollapsedClickEventArgs(
    RoutedEvent routedEvent,
    IReadOnlyList<AvonicBreadcrumb> collapsedBreadcrumbs,
    Control? ionShadowTarget) : RoutedEventArgs(routedEvent)
{
    /// <summary>Ionic <c>collapsedBreadcrumbs</c>: the breadcrumbs behind the indicator, in order.</summary>
    public IReadOnlyList<AvonicBreadcrumb> CollapsedBreadcrumbs { get; } = collapsedBreadcrumbs;

    /// <summary>Ionic <c>ionShadowTarget</c>: the collapsed indicator that was clicked, to place a popover by.</summary>
    public Control? IonShadowTarget { get; } = ionShadowTarget;
}
