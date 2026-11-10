using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>The detail of Ionic's <c>ionTabsWillChange</c> and <c>ionTabsDidChange</c>: <c>{ tab: string }</c>.</summary>
public sealed class TabsChangeEventArgs(RoutedEvent routedEvent, string? tab) : RoutedEventArgs(routedEvent)
{
    /// <summary>The <see cref="AvonicTab.Tab"/> of the tab being selected.</summary>
    public string? Tab { get; } = tab;
}

/// <summary>
/// The detail of Ionic's internal <c>ionTabButtonClick</c>: <c>{ tab, href, selected }</c>. There's no <c>href</c> without a router.
/// </summary>
internal sealed class TabButtonClickEventArgs(RoutedEvent routedEvent, string tab, bool selected) : RoutedEventArgs(routedEvent)
{
    public string Tab { get; } = tab;

    public bool Selected { get; } = selected;
}
