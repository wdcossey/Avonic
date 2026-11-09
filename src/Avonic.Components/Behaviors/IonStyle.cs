using Avalonia.Controls;
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

/// <summary>
/// What a host has been told by <c>ionStyle</c>, per type of child, as Ionic keeps it by tag name: each true key becomes
/// <c>:&lt;prefix&gt;-key</c>, and a key that no child sends any more is removed. <c>AvonicItem</c>'s <c>itemStyle</c> and
/// <c>AvonicToolbar</c>'s <c>childrenStyle</c>.
/// </summary>
internal sealed class IonStyleClasses(string prefix)
{
    private readonly Dictionary<Type, HashSet<string>> _byChild = [];
    private readonly HashSet<string> _applied = [];

    /// <summary>Handles the event, as Ionic's host stops it, and applies it. False if it has no source to keep it by.</summary>
    public bool Apply(IPseudoClasses pseudoClasses, IonStyleEventArgs e)
    {
        e.Handled = true;
        if (e.Source is null)
        {
            return false;
        }

        _byChild[e.Source.GetType()] = e.Styles.Where(style => style.Value).Select(style => style.Key).ToHashSet();

        var styles = _byChild.Values.SelectMany(keys => keys).ToHashSet();
        foreach (var removed in _applied.Except(styles))
        {
            pseudoClasses.Remove(prefix + removed);
        }

        foreach (var added in styles.Except(_applied))
        {
            pseudoClasses.Add(prefix + added);
        }

        _applied.Clear();
        _applied.UnionWith(styles);
        return true;
    }
}
