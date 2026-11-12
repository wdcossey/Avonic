using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
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
    private readonly List<(int Index, double Top)> _realisedDividers = [];

    /// <summary>The last row <see cref="SectionDividerBefore"/> was asked about, or -1, and its answer.</summary>
    private int _sectionRow = -1;
    private int _section = -1;
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

        ForgetStickyRows();
        RemoveStuckDivider();
    }

    private void ForgetStickyRows()
    {
        _stickyRows.Clear();
        _knownRows.Clear();
        _sectionRow = -1;
        _stuckIndex = -1;
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
        _sectionRow = -1;
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
    /// made from its template to see, once, and not shown, as the DOM has every row. The last answer is kept, so that
    /// a scroll looks only at the rows it moved past.
    /// </summary>
    private int SectionDividerBefore(int row)
    {
        int section;
        if (_sectionRow < 0)
        {
            section = LastStickyBetween(0, row - 1);
        }
        else if (row >= _sectionRow)
        {
            section = LastStickyBetween(_sectionRow, row - 1) is var found and >= 0 ? found : _section;
        }
        else
        {
            // Nothing between the last answer's divider and its row was sticky.
            section = _section < row ? _section : LastStickyBetween(0, row - 1);
        }

        _sectionRow = row;
        _section = section;
        return section;
    }

    private int LastStickyBetween(int from, int to)
    {
        for (var index = to; index >= from; index--)
        {
            if (_stickyRows.Contains(index))
            {
                return index;
            }

            if (!_knownRows.Add(index))
            {
                continue;
            }

            // Made as the panel would make it, with the item as its data, so that a binding of Sticky is read.
            var item = ItemsView[index];
            var made = item as Control;
            if (made is null && this.FindDataTemplate(item, ItemTemplate)?.Build(item) is { } built)
            {
                built.DataContext = item;
                made = built;
            }

            if (made is AvonicItemDivider { Sticky: true })
            {
                _stickyRows.Add(index);
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// An inset list has <c>overflow: hidden</c>, so it's the divider's scrollport, and it never scrolls, as the divider
    /// measures it.
    /// </summary>
    private bool IsInAnInsetList(ScrollViewer scrollViewer)
    {
        for (Visual? visual = this; visual is not null && visual != scrollViewer; visual = visual.GetVisualParent())
        {
            if (visual is AvonicList { Inset: true })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A scroll is a change of arrangement, so a layout pass is the one thing that follows every cause.</summary>
    private void OnLayoutUpdatedForStickyRows(object? sender, EventArgs e)
    {
        // A horizontal panel's rows are side by side, and position: sticky here is top: 0.
        if (ItemsPanelRoot is not VirtualizingPanel panel
            || panel is VirtualizingStackPanel { Orientation: Orientation.Horizontal }
            || this.FindAncestorOfType<ScrollViewer>() is not { } scrollViewer
            || scrollViewer.TranslatePoint(new Point(0, scrollViewer.Padding.Top), this) is not { } scrollport
            || IsInAnInsetList(scrollViewer))
        {
            HideStuckDivider();
            return;
        }

        // The first row on screen, and the natural tops of the sticky dividers realised. A row the panel keeps above
        // the top, as it does a focused one, isn't on screen.
        var first = int.MaxValue;
        _realisedDividers.Clear();
        foreach (var child in panel.Children)
        {
            var index = child.IsVisible ? IndexFromContainer(child) : -1;
            if (index < 0 || panel.TranslatePoint(new Point(0, child.Bounds.Top), this) is not { } at)
            {
                continue;
            }

            _knownRows.Add(index);
            if (at.Y + child.Bounds.Height > scrollport.Y)
            {
                first = Math.Min(first, index);
            }

            if (((child as ContentPresenter)?.Child ?? child) is AvonicItemDivider { Sticky: true } divider)
            {
                _realisedDividers.Add((index, child == divider ? at.Y : at.Y + divider.Bounds.Top));
                // One newly sticky is found by the next look from the last answer, as that looks at every row since.
                _stickyRows.Add(index);
            }
            else if (_stickyRows.Remove(index))
            {
                _sectionRow = -1;
            }
        }

        // The section's divider is the last sticky one before the first row on screen. One that's realised sticks by
        // itself. An item that's a control is its own row, and can be in one place only.
        var section = first == int.MaxValue ? -1 : SectionDividerBefore(Math.Min(first, ItemsView.Count));
        if (section < 0 || ItemsView[section] is Control || ContainerFromIndex(section) is { IsVisible: true })
        {
            HideStuckDivider();
            return;
        }

        // A later sticky divider is drawn above it, as it comes later in the DOM.
        var nextTop = double.PositiveInfinity;
        foreach (var (index, top) in _realisedDividers)
        {
            if (index > section)
            {
                nextTop = Math.Min(nextTop, top);
            }
        }

        ShowStuckDivider(section, panel, scrollport.Y, nextTop);
    }

    private void ShowStuckDivider(int index, Panel panel, double scrollportTop, double nextTop)
    {
        if (_stuckDivider is null)
        {
            _stuckDivider = new ContentPresenter { RenderTransform = _stuckOffset };
            LogicalChildren.Add(_stuckDivider);
        }

        // Applying a template again clears the visual children.
        if (_stuckDivider.GetVisualParent() != this)
        {
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
        if (_stuckDivider.Child is not AvonicItemDivider divider || panel.TranslatePoint(default, this) is not { } panelTop)
        {
            HideStuckDivider();
            return;
        }

        // Its item may have stopped it sticking since its row was last realised.
        if (!divider.Sticky)
        {
            _stickyRows.Remove(index);
            _sectionRow = -1;
            HideStuckDivider();
            return;
        }

        _stuckDivider.IsVisible = true;

        // As the divider's own offset: the border box at the top of the scrollport, and never out of its parent.
        var height = divider.Bounds.Height;
        var top = Math.Min(scrollportTop, panelTop.Y + panel.Bounds.Height - height - divider.Margin.Bottom);
        _stuckOffset.Y = top - divider.Margin.Top;

        // It shows only above a later sticky divider, which is drawn above it.
        var shown = Math.Clamp(nextTop - top, 0, height);
        _stuckDivider.IsHitTestVisible = shown > 0;
        if (shown >= height)
        {
            _stuckDivider.Clip = null;
            return;
        }

        // A new geometry, as the property changing is what redraws it.
        var clip = new Rect(0, 0, _stuckDivider.Bounds.Width, divider.Margin.Top + shown);
        if (_stuckDivider.Clip is not RectangleGeometry { } current || current.Rect != clip)
        {
            _stuckDivider.Clip = new RectangleGeometry(clip);
        }
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

    // As a templated control lays out its template, but with the copy beside it at its own size, which leaves the
    // list's size alone. Arranged at a place that doesn't move as the list scrolls: its transform puts it where it sticks.
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_stuckDivider is null)
        {
            return base.MeasureOverride(availableSize);
        }

        var size = default(Size);
        foreach (var child in VisualChildren)
        {
            if (child is Layoutable layoutable && child != _stuckDivider)
            {
                layoutable.Measure(availableSize);
                size = new Size(
                    Math.Max(size.Width, layoutable.DesiredSize.Width),
                    Math.Max(size.Height, layoutable.DesiredSize.Height));
            }
        }

        // At the panel's width, at which it's arranged.
        var width = ItemsPanelRoot is { Bounds.Width: > 0 } panel ? panel.Bounds.Width : availableSize.Width;
        _stuckDivider.Measure(new Size(width, double.PositiveInfinity));
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_stuckDivider is null)
        {
            return base.ArrangeOverride(finalSize);
        }

        foreach (var child in VisualChildren)
        {
            if (child is Layoutable layoutable && child != _stuckDivider)
            {
                layoutable.Arrange(new Rect(finalSize));
            }
        }

        if (ItemsPanelRoot is { } panel && panel.TranslatePoint(default, this) is { } at)
        {
            // The panel's width may have changed since the measure, which read the last one.
            if (_stuckDivider.IsVisible && _stuckDivider.Bounds.Width != panel.Bounds.Width)
            {
                _stuckDivider.Measure(new Size(panel.Bounds.Width, double.PositiveInfinity));
            }

            _stuckDivider.Arrange(new Rect(at.X, 0, panel.Bounds.Width, _stuckDivider.DesiredSize.Height));
        }

        return finalSize;
    }
}
