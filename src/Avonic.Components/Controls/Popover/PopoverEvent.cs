using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's popover <c>event</c>, what <c>present(event)</c> positions by: any DOM event, of which the popover reads the target
/// (<c>event.target</c>, or <c>event.detail.ionShadowTarget</c>, a part inside a control) and the point (<c>clientX</c>,
/// <c>clientY</c>). With <see cref="PopoverReference.Trigger"/> it's beside <see cref="Target"/>, and with
/// <see cref="PopoverSize.Cover"/> as wide; with <see cref="PopoverReference.Event"/> it's at <see cref="ClientPosition"/>.
/// </summary>
/// <param name="target">The element: the event's target, or the part of it to align with.</param>
/// <param name="clientPosition">
/// The point, in the coordinates of the target's <see cref="TopLevel"/> (the popover's, with no target), as <c>clientX</c> and
/// <c>clientY</c> are the viewport's. Null for an event with none, such as a key's click, which Chromium gives at 0, 0.
/// </param>
public sealed class PopoverEvent(Visual? target, Point? clientPosition = null)
{
    /// <summary>The element: <c>event.detail.ionShadowTarget</c> if there's one, else <c>event.target</c>.</summary>
    public Visual? Target { get; } = target;

    /// <summary><c>clientX</c>, <c>clientY</c>: the point in the target's top level, or null.</summary>
    public Point? ClientPosition { get; } = clientPosition;

    /// <summary>
    /// The event an Avalonia input event is: its source as the target (a breadcrumb's collapsed click gives its indicator, as
    /// Ionic's <c>ionShadowTarget</c>), and the point of a pointer, a tap, or a context request that has one.
    /// </summary>
    public static PopoverEvent From(RoutedEventArgs e)
    {
        var target = e is BreadcrumbCollapsedClickEventArgs { IonShadowTarget: { } shadow } ? shadow : e.Source as Visual;
        var top = target is null ? null : TopLevel.GetTopLevel(target);
        Point? position = null;
        if (top is not null)
        {
            switch (e)
            {
                case PointerEventArgs pointer:
                    position = pointer.GetPosition(top);
                    break;
                case TappedEventArgs tapped:
                    position = tapped.GetPosition(top);
                    break;
                case ContextRequestedEventArgs context when context.TryGetPosition(top, out var point):
                    position = point;
                    break;
            }
        }

        return new PopoverEvent(target, position);
    }
}
