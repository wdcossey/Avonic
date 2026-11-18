using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;

namespace Avonic.Components.Overlays;

/// <summary>
/// Ionic's <c>createTriggerController</c>: a click on the control named by an overlay's <c>trigger</c> presents it. The name is
/// looked up in the overlay's name scope, as Ionic's <c>getElementById</c> looks in the document. The listeners are the
/// overlay's <see cref="AvonicOverlay.AddTriggerListeners"/>: by default a click, which is the control's own <c>Click</c> routed
/// event, whatever its type (a stock <see cref="Button"/>'s, an <c>AvonicButton</c>'s, a chip's), or
/// <see cref="InputElement.TappedEvent"/> for a control with none, as a DOM <c>click</c> comes on any element. Handled ones too,
/// as an <c>addEventListener</c> hears every click. The popover adds its own, for its <c>triggerAction</c>.
/// </summary>
internal sealed class OverlayTrigger(AvonicOverlay overlay)
{
    private Action? _remove;

    /// <summary>Ionic's <c>triggerEl</c>: the control found, while its listeners are on it.</summary>
    public Interactive? Target { get; private set; }

    /// <summary>Ionic's <c>addClickListener</c>: the old listeners go, and the control named <paramref name="name"/> gets them.</summary>
    public void Attach(string? name)
    {
        Remove();
        if (name is null)
        {
            return;
        }

        if (FindNameScope(overlay)?.Find(name) is not Interactive target)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(overlay,
                "A trigger element with the name \"{Name}\" was not found. The trigger element must be in the overlay's name scope when the Trigger property is set on an overlay.",
                name);
            return;
        }

        Target = target;
        _remove = overlay.AddTriggerListeners(target);
    }

    /// <summary>
    /// The nearest name scope, as <c>FindNameScope</c> finds it, going on from an overlay around that's presented to its
    /// placeholder in its page, as it would from where it was: an overlay inside another's content (a nested popover) still finds
    /// its trigger in the page while the one around it is in the overlay host, as <c>getElementById</c> looks in the whole document.
    /// </summary>
    private static INameScope? FindNameScope(StyledElement from)
    {
        for (var element = from; element is not null;)
        {
            if (NameScope.GetNameScope(element) is { } scope)
            {
                return scope;
            }

            element = element is AvonicOverlay { Parent: OverlayHost, Placeholder: { } placeholder } ? placeholder : element.Parent;
        }

        return null;
    }

    /// <summary>Ionic's <c>removeClickListener</c>.</summary>
    public void Remove()
    {
        _remove?.Invoke();
        _remove = null;
        Target = null;
    }

    /// <summary>
    /// A DOM <c>click</c> on <paramref name="target"/> calls <paramref name="handler"/>: its type's own <c>Click</c>, and a tap. A
    /// type's own <c>Click</c> may come only in some of its modes (an item's or a card's, as a button), so a tap does too; the
    /// overlay's present while presented does nothing, so the two needn't be told apart. Returns what removes it.
    /// </summary>
    internal static Action AddClickListener(Interactive target, EventHandler<RoutedEventArgs> handler)
    {
        var click = ClickEventOf(target.GetType());
        target.AddHandler(click, handler, RoutingStrategies.Bubble, handledEventsToo: true);
        if (click != InputElement.TappedEvent)
        {
            target.AddHandler(InputElement.TappedEvent, handler, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        return () =>
        {
            target.RemoveHandler(click, handler);
            target.RemoveHandler(InputElement.TappedEvent, handler);
        };
    }

    /// <summary>The first routed event called <c>Click</c> registered by the type or a base of it; <c>Tapped</c> if none is.</summary>
    internal static RoutedEvent ClickEventOf(Type type)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (RoutedEventRegistry.Instance.GetRegistered(t).FirstOrDefault(e => e.Name == "Click") is { } click)
            {
                return click;
            }
        }

        return InputElement.TappedEvent;
    }
}
