using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;

namespace Avonic.Components.Overlays;

/// <summary>
/// Ionic's <c>createTriggerController</c>: a click on the control named by an overlay's <c>trigger</c> presents it. The name is
/// looked up in the overlay's name scope, as Ionic's <c>getElementById</c> looks in the document. The click is the control's own
/// <c>Click</c> routed event, whatever its type (a stock <see cref="Button"/>'s, an <c>AvonicButton</c>'s, a chip's), or
/// <see cref="InputElement.TappedEvent"/> for a control with none, as a DOM <c>click</c> comes on any element. Handled ones too,
/// as an <c>addEventListener</c> hears every click.
/// </summary>
internal sealed class OverlayTrigger(AvonicOverlay overlay)
{
    private Action? _remove;

    /// <summary>Ionic's <c>addClickListener</c>: the old listener goes, and the control named <paramref name="name"/> gets one.</summary>
    public void Attach(string? name)
    {
        Remove();
        if (name is null)
        {
            return;
        }

        if (overlay.FindNameScope()?.Find(name) is not Interactive target)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(overlay,
                "A trigger element with the name \"{Name}\" was not found. The trigger element must be in the overlay's name scope when the Trigger property is set on an overlay.",
                name);
            return;
        }

        var click = ClickEventOf(target.GetType());
        EventHandler<RoutedEventArgs> handler = (_, _) => overlay.PresentFromInput();
        target.AddHandler(click, handler, RoutingStrategies.Bubble, handledEventsToo: true);
        _remove = () => target.RemoveHandler(click, handler);
    }

    /// <summary>Ionic's <c>removeClickListener</c>.</summary>
    public void Remove()
    {
        _remove?.Invoke();
        _remove = null;
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
