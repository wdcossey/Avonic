using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Collections.Specialized;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>position: sticky</c> on an <see cref="AvonicItemDivider"/> that's a row of this list's virtualising panel. CSS
/// keeps a stuck divider stuck while its section scrolls, but the panel recycles its row once the row's own place has
/// gone above the top. So the list draws the divider there itself, in a second container for the same item, made as
/// the panel makes one, until the row is realised again.
/// </summary>
public partial class AvonicList
{
    /// <summary>The rows the panel has realised as sticky dividers, by index, kept as the items change.</summary>
    private readonly SortedSet<int> _stickyRows = [];

    /// <summary>The rows known to be sticky dividers or not, realised or looked at, kept as the items change.</summary>
    private readonly HashSet<int> _knownRows = [];

    private readonly TranslateTransform _stuckOffset = new();
    private bool _watchingStickyRows;
    private ContentPresenter? _stuckDivider;
    private int _stuckIndex = -1;
    private ItemsSourceView? _watchedItems;

    /// <summary>The list's own copy of a divider, which the list sticks, not the divider.</summary>
    internal static bool IsStuckCopy(Visual container) =>
        container.GetVisualParent() is AvonicList list && list._stuckDivider == container;

    /// <summary>A sticky divider is a row of a list's virtualising panel, so the list watches its rows.</summary>
    internal static void WatchStickyRow(Visual container)
    {
        if (container.GetVisualParent() is VirtualizingPanel panel
            && panel.FindAncestorOfType<AvonicList>() is { } list
            && list.ItemsPanelRoot == panel
            && !list._watchingStickyRows)
        {
            list._watchingStickyRows = true;
            list._watchedItems = list.ItemsView;
            list._watchedItems.CollectionChanged += list.OnItemsChangedForStickyRows;
            list.LayoutUpdated += list.OnLayoutUpdatedForStickyRows;
        }
    }

    private void StopWatchingStickyRows()
    {
        if (!_watchingStickyRows)
        {
            return;
        }

        _watchingStickyRows = false;
        LayoutUpdated -= OnLayoutUpdatedForStickyRows;
        if (_watchedItems is not null)
        {
            _watchedItems.CollectionChanged -= OnItemsChangedForStickyRows;
            _watchedItems = null;
        }

        _stickyRows.Clear();
        _knownRows.Clear();
        RemoveStuckDivider();
    }

    /// <summary>The indexes after a change move with their items, as the DOM's elements would.</summary>
    private void OnItemsChangedForStickyRows(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                Shift(e.NewStartingIndex, e.NewItems.Count);
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                Forget(e.OldStartingIndex, e.OldItems.Count);
                Shift(e.OldStartingIndex + e.OldItems.Count, -e.OldItems.Count);
                break;
            case NotifyCollectionChangedAction.Replace when e.OldItems is not null:
                Forget(e.OldStartingIndex, e.OldItems.Count);
                break;
            default:
                _stickyRows.Clear();
                _knownRows.Clear();
                break;
        }

        // The copy may be of an item that's gone or moved, so it's made again at the next pass.
        _stuckIndex = -1;
    }

    private void Forget(int from, int count)
    {
        _stickyRows.RemoveWhere(row => row >= from && row < from + count);
        _knownRows.RemoveWhere(row => row >= from && row < from + count);
    }

    private void Shift(int from, int by)
    {
        Shift(_stickyRows, from, by);
        Shift(_knownRows, from, by);
    }

    private static void Shift(ISet<int> rows, int from, int by)
    {
        var moved = rows.Where(row => row >= from).ToList();
        foreach (var row in moved)
        {
            rows.Remove(row);
        }

        foreach (var row in moved)
        {
            rows.Add(row + by);
        }
    }

    /// <summary>
    /// The last sticky divider before a row: the section it's in. A row the panel hasn't realised, as after a jump, is
    /// made from its template to see, once, and not shown, as the DOM has every row.
    /// </summary>
    private int SectionDividerBefore(int row)
    {
        for (var index = row - 1; index >= 0; index--)
        {
            if (_stickyRows.Contains(index))
            {
                return index;
            }

            if (!_knownRows.Add(index))
            {
                continue;
            }

            var item = ItemsView[index];
            var made = item as Control ?? this.FindDataTemplate(item, ItemTemplate)?.Build(item);
            if (made is AvonicItemDivider { Sticky: true })
            {
                _stickyRows.Add(index);
                return index;
            }
        }

        return -1;
    }

    /// <summary>A scroll is a change of arrangement, so a layout pass is the one thing that follows every cause.</summary>
    private void OnLayoutUpdatedForStickyRows(object? sender, EventArgs e)
    {
        if (ItemsPanelRoot is not VirtualizingPanel panel || this.FindAncestorOfType<ScrollViewer>() is not { } scrollViewer)
        {
            HideStuckDivider();
            return;
        }

        // What's realised now, and the natural top of the first sticky divider in it, which is drawn above the copy.
        var first = int.MaxValue;
        var nextTop = double.PositiveInfinity;
        foreach (var child in panel.Children)
        {
            var index = child.IsVisible ? IndexFromContainer(child) : -1;
            if (index < 0)
            {
                continue;
            }

            first = Math.Min(first, index);
            _knownRows.Add(index);
            if (((child as ContentPresenter)?.Child ?? child) is AvonicItemDivider { Sticky: true } divider)
            {
                _stickyRows.Add(index);
                var top = child == divider ? child.Bounds.Top : child.Bounds.Top + divider.Bounds.Top;
                if (panel.TranslatePoint(new Point(0, top), this) is { } natural)
                {
                    nextTop = Math.Min(nextTop, natural.Y);
                }
            }
            else
            {
                _stickyRows.Remove(index);
            }
        }

        // The section's divider is the last sticky one before what's realised. One that's realised sticks by itself.
        // An item that's a control is its own row, and can be in one place only.
        var section = first == int.MaxValue ? -1 : SectionDividerBefore(Math.Min(first, ItemsView.Count));
        if (section < 0 || ItemsView[section] is Control)
        {
            HideStuckDivider();
            return;
        }

        ShowStuckDivider(section, panel, scrollViewer, nextTop);
    }

    private void ShowStuckDivider(int index, Panel panel, ScrollViewer scrollViewer, double nextTop)
    {
        if (_stuckDivider is null)
        {
            _stuckDivider = new ContentPresenter { RenderTransform = _stuckOffset };
            LogicalChildren.Add(_stuckDivider);
            VisualChildren.Add(_stuckDivider);
        }

        if (_stuckIndex != index)
        {
            // As the panel reuses a container: what was set for the last item is cleared first.
            if (_stuckIndex >= 0 || _stuckDivider.Content is not null)
            {
                ClearContainerForItemOverride(_stuckDivider);
            }

            _stuckIndex = index;
            PrepareContainerForItemOverride(_stuckDivider, ItemsView[index], index);
            _stuckDivider.UpdateChild();
            InvalidateMeasure();
        }

        // Only whether there's a divider to show changes the layout. Being covered is drawing, so that a pass doesn't
        // undo the last one's choice.
        if (_stuckDivider.Child is not AvonicItemDivider divider
            || scrollViewer.TranslatePoint(new Point(0, scrollViewer.Padding.Top), this) is not { } scrollport
            || panel.TranslatePoint(default, this) is not { } panelTop)
        {
            HideStuckDivider();
            return;
        }

        _stuckDivider.IsVisible = true;

        // As the divider's own offset: the border box at the top of the scrollport, and never out of its parent.
        var height = divider.Bounds.Height;
        var top = Math.Min(scrollport.Y, panelTop.Y + panel.Bounds.Height - height - divider.Margin.Bottom);
        _stuckOffset.Y = top - divider.Margin.Top;

        // A later sticky divider is drawn above it, as it comes later in the DOM, so it shows only above that one.
        var shown = Math.Clamp(nextTop - top, 0, height);
        _stuckDivider.IsHitTestVisible = shown > 0;
        _stuckDivider.Clip = shown >= height
            ? null
            : new RectangleGeometry(new Rect(0, 0, _stuckDivider.Bounds.Width, divider.Margin.Top + shown));
    }

    private void HideStuckDivider()
    {
        if (_stuckDivider is not null)
        {
            _stuckDivider.IsVisible = false;
        }
    }

    private void RemoveStuckDivider()
    {
        if (_stuckDivider is not null)
        {
            ClearContainerForItemOverride(_stuckDivider);
            VisualChildren.Remove(_stuckDivider);
            LogicalChildren.Remove(_stuckDivider);
            _stuckDivider = null;
            _stuckIndex = -1;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        _stuckDivider?.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (_stuckDivider is not null && ItemsPanelRoot is { } panel && panel.TranslatePoint(default, this) is { } at)
        {
            _stuckDivider.Arrange(new Rect(at.X, 0, panel.Bounds.Width, _stuckDivider.DesiredSize.Height));
        }

        return size;
    }
}
