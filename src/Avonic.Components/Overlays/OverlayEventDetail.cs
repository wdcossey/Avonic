using Avalonia.Interactivity;

namespace Avonic.Components.Overlays;

/// <summary>Ionic's <c>OverlayEventDetail</c>: what an overlay was dismissed with.</summary>
/// <param name="Data">Ionic <c>data</c>.</param>
/// <param name="Role">Ionic <c>role</c>, such as <see cref="OverlayRoles.Backdrop"/>.</param>
public sealed record OverlayEventDetail(object? Data, string? Role);

/// <summary>The arguments of <see cref="AvonicOverlay.WillDismissEvent"/> and <see cref="AvonicOverlay.DidDismissEvent"/>: Ionic's <c>detail</c>.</summary>
public sealed class OverlayEventArgs : RoutedEventArgs
{
    public OverlayEventArgs(RoutedEvent routedEvent, object? data, string? role)
        : base(routedEvent)
    {
        Data = data;
        Role = role;
    }

    /// <summary>Ionic <c>detail.data</c>.</summary>
    public object? Data { get; }

    /// <summary>Ionic <c>detail.role</c>.</summary>
    public string? Role { get; }

    /// <summary>The detail, as <see cref="AvonicOverlay.OnDidDismiss"/> gives it.</summary>
    public OverlayEventDetail Detail => new(Data, Role);
}

/// <summary>Ionic's roles that <c>utils/overlays</c> gives a dismiss.</summary>
public static class OverlayRoles
{
    /// <summary>Ionic <c>BACKDROP</c>: a tap on the backdrop, Escape, or the hardware back button.</summary>
    public const string Backdrop = "backdrop";

    /// <summary>Ionic <c>GESTURE</c>: a swipe has already moved it off, so the leave animation doesn't play.</summary>
    public const string Gesture = "gesture";

    /// <summary>Ionic <c>isCancel</c>: <c>cancel</c> or <see cref="Backdrop"/>.</summary>
    public static bool IsCancel(string? role) => role is "cancel" or Backdrop;
}
