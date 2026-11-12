using Avalonia.Interactivity;

namespace Avonic.Components.Controls;

/// <summary>Ionic <c>ReorderMoveEventDetail</c>: <c>ionReorderMove</c>'s <c>from</c> and <c>to</c>.</summary>
public sealed class ReorderMoveEventArgs(RoutedEvent routedEvent, object source, int from, int to) : RoutedEventArgs(routedEvent, source)
{
    /// <summary>The index the item was over at the last move, which isn't where the drag began.</summary>
    public int From { get; } = from;

    /// <summary>The index the item is over now.</summary>
    public int To { get; } = to;
}

/// <summary>
/// Ionic <c>ReorderEndEventDetail</c>, and <c>ItemReorderEventDetail</c>, which has the same members: where the item began and
/// where it was dropped, and <c>complete</c>, which finishes the reorder. Until it's called, the item stays lifted and the group takes
/// no new drag.
/// </summary>
public class ReorderEndEventArgs(RoutedEvent routedEvent, AvonicReorderGroup source, int from, int to) : RoutedEventArgs(routedEvent, source)
{
    private readonly AvonicReorderGroup _group = source;

    /// <summary>The item's index when the drag began.</summary>
    public int From { get; } = from;

    /// <summary>The index it was dropped at. The same as <see cref="From"/> if it didn't move.</summary>
    public int To { get; } = to;

    /// <summary>Ionic <c>complete()</c>: see <see cref="AvonicReorderGroup.Complete()"/>.</summary>
    public void Complete() => _group.Complete();

    /// <summary>Ionic <c>complete(boolean)</c>: see <see cref="AvonicReorderGroup.Complete(bool)"/>.</summary>
    public void Complete(bool reorder) => _group.Complete(reorder);

    /// <summary>Ionic <c>complete(array)</c>: see <see cref="AvonicReorderGroup.Complete{T}(IList{T})"/>.</summary>
    public IList<T> Complete<T>(IList<T> list) => _group.Complete(list);
}

/// <summary>Ionic <c>ItemReorderEventDetail</c>, of the deprecated <c>ionItemReorder</c>.</summary>
public sealed class ItemReorderEventArgs(RoutedEvent routedEvent, AvonicReorderGroup source, int from, int to)
    : ReorderEndEventArgs(routedEvent, source, from, to);
