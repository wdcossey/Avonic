using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using System.Collections.Specialized;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-list</c>: a block of items that can share their lines and sit inset from the page. It doesn't
/// scroll or select, so it's an <see cref="ItemsControl"/>, and what it does to its items is in the theme's
/// <c>Styles/List.axaml</c>. To virtualise, put it in a <see cref="ScrollViewer"/> and set
/// <see cref="ItemsControl.ItemsPanel"/> to a <see cref="VirtualizingStackPanel"/>.
/// </summary>
[PseudoClasses(":lines-full", ":lines-inset", ":lines-none", ":inset", ":after-inset")]
public partial class AvonicList : ItemsControl
{
    /// <summary>Ionic <c>lines</c>: how the bottom border is displayed on the items. Null leaves it to each item.</summary>
    public static readonly StyledProperty<ListLines?> LinesProperty =
        AvaloniaProperty.Register<AvonicList, ListLines?>(nameof(Lines));

    /// <summary>Ionic <c>inset</c>: margin around the list, and rounded corners.</summary>
    public static readonly StyledProperty<bool> InsetProperty =
        AvaloniaProperty.Register<AvonicList, bool>(nameof(Inset));

    private AvaloniaList<Control>? _siblings;
    private Control? _inParent;

    /// <summary>The items control whose virtualising panel this list is a row of, if it is.</summary>
    private ItemsControl? _rowsOwner;

    /// <summary>Waiting for the panel to finish realising this row, so that it has an index.</summary>
    private bool _awaitingRowIndex;

    public ListLines? Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    public bool Inset
    {
        get => GetValue(InsetProperty);
        set => SetValue(InsetProperty, value);
    }

    /// <summary>
    /// Ionic <c>closeSlidingItems()</c>: the first <see cref="AvonicItemSliding"/> in the list, at any depth, closes the open one,
    /// as <see cref="AvonicItemSliding.CloseOpened"/> does, wherever that is. True if one closed; false, too, with no sliding item
    /// in the list. Synchronous, where Ionic's returns a promise.
    /// </summary>
    public bool CloseSlidingItems() =>
        this.GetLogicalDescendants().OfType<AvonicItemSliding>().FirstOrDefault()?.CloseOpened() ?? false;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LinesProperty)
        {
            var lines = Lines;
            PseudoClasses.Set(":lines-full", lines == ListLines.Full);
            PseudoClasses.Set(":lines-inset", lines == ListLines.Inset);
            PseudoClasses.Set(":lines-none", lines == ListLines.None);
        }
        else if (change.Property == ItemTemplateProperty)
        {
            // What the template makes may stick differently, so the list looks again.
            ForgetStickyRows();
        }
        else if (change.Property == InsetProperty)
        {
            PseudoClasses.Set(":inset", Inset);
            UpdateNextList();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _inParent = ContainerOf(this);

        // A virtualising panel's children are only what's realised, in no set order, so there the row before is the
        // item before, through its owner.
        if (_inParent.GetVisualParent() is VirtualizingPanel panelOfRows)
        {
            // The panel is in its owner's template, so the owner is the first items control above it.
            if (panelOfRows.FindAncestorOfType<ItemsControl>() is { } owner)
            {
                _rowsOwner = owner;
                owner.ContainerPrepared += OnRowPrepared;
                owner.ContainerIndexChanged += OnRowIndexChanged;
            }
        }
        else if (_inParent.GetVisualParent() is Panel panel)
        {
            _siblings = panel.Children;
            _siblings.CollectionChanged += OnSiblingsChanged;
        }

        UpdateAfterInset();

        // A presenter's new list wasn't there to read when the presenter was added.
        UpdateNextList();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_siblings is not null)
        {
            _siblings.CollectionChanged -= OnSiblingsChanged;
            _siblings = null;
        }

        if (_rowsOwner is not null)
        {
            _rowsOwner.ContainerPrepared -= OnRowPrepared;
            _rowsOwner.ContainerIndexChanged -= OnRowIndexChanged;
            _rowsOwner = null;
        }

        LayoutUpdated -= OnRowRealised;
        _awaitingRowIndex = false;
        StopWatchingStickyRows();

        _inParent = null;
    }

    /// <summary>An <see cref="ItemsControl"/> moves focus with the arrow keys. ion-list doesn't: only a popover's does.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
    }

    private void OnSiblingsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateAfterInset();

    private void OnRowPrepared(object? sender, ContainerPreparedEventArgs e) => OnRowChanged(e.Container);

    private void OnRowIndexChanged(object? sender, ContainerIndexChangedEventArgs e) => OnRowChanged(e.Container);

    /// <summary>
    /// This row given another item, as a template that recycles its content keeps the list, or another index. A row
    /// realised or moved before it tells it, once its own check is done.
    /// </summary>
    private void OnRowChanged(Control container)
    {
        if (container == _inParent)
        {
            ScheduleRowCheck();
        }
    }

    private int RowIndex() => _rowsOwner is null || _inParent is null ? -1 : _rowsOwner.IndexFromContainer(_inParent);

    /// <summary>
    /// What's beside a control in a panel. An <see cref="ItemsControl"/> wraps templated content in a presenter
    /// as tall as the content, and there's no such box in the DOM. A presenter with a <c>TemplatedParent</c> is part of
    /// a template, such as a <see cref="ScrollViewer"/>'s, so this stops there.
    /// </summary>
    internal static Control ContainerOf(Control control)
    {
        while (control.GetVisualParent() is ContentPresenter { TemplatedParent: null } presenter)
        {
            control = presenter;
        }

        return control;
    }

    private int IndexInSiblings() => _inParent is null ? -1 : _siblings?.IndexOf(_inParent) ?? -1;

    private static AvonicList? ListIn(Control sibling) =>
        (sibling as ContentPresenter)?.Child as AvonicList ?? sibling as AvonicList;

    private void UpdateNextList()
    {
        if (_rowsOwner is not null)
        {
            ScheduleRowCheck();
            return;
        }

        var index = IndexInSiblings();
        if (index >= 0 && index + 1 < _siblings!.Count)
        {
            ListIn(_siblings[index + 1])?.UpdateAfterInset();
        }
    }

    /// <summary>
    /// <c>.list-inset + ion-list</c>: the list before this one is inset. Avalonia's selectors have no sibling
    /// combinator, so it's a pseudoclass, and the theme has the rest of the rule.
    /// </summary>
    private void UpdateAfterInset()
    {
        if (_rowsOwner is not null)
        {
            ScheduleRowCheck();
            return;
        }

        var index = IndexInSiblings();
        PseudoClasses.Set(":after-inset", index > 0 && ListIn(_siblings![index - 1]) is { Inset: true });
    }

    /// <summary>
    /// A virtualising panel makes a row's content while it measures the row, before it has the row's index, and
    /// what it has realised only settles at the end of the pass. So a row looks once the pass is over, which is still
    /// before it's drawn, and then tells the row after it.
    /// </summary>
    private void ScheduleRowCheck()
    {
        if (_awaitingRowIndex)
        {
            return;
        }

        _awaitingRowIndex = true;
        LayoutUpdated += OnRowRealised;
    }

    private void OnRowRealised(object? sender, EventArgs e)
    {
        LayoutUpdated -= OnRowRealised;
        _awaitingRowIndex = false;

        var index = RowIndex();
        if (index >= 0)
        {
            UpdateAfterInsetRow(index);
            if (_rowsOwner!.ContainerFromIndex(index + 1) is { } next && ListIn(next) is { _rowsOwner: not null } list)
            {
                list.UpdateAfterInsetRow(index + 1);
            }
        }
    }

    /// <summary>
    /// <c>.list-inset + ion-list</c>, for a row of a virtualising panel: the row before is the item before. If that
    /// isn't realised, as at the top while scrolling up, it's made from its template to see, with the item as its data,
    /// and not shown, as the DOM has every row. A recycled row is hidden, with no index, and the next item it's given
    /// decides.
    /// </summary>
    private void UpdateAfterInsetRow(int index)
    {
        bool after;
        if (index == 0)
        {
            after = false;
        }
        else if (_rowsOwner!.ContainerFromIndex(index - 1) is { } before && before is not ContentPresenter { Child: null })
        {
            after = ListIn(before) is { Inset: true };
        }
        else
        {
            var item = _rowsOwner.ItemsView[index - 1];
            var made = item as Control;
            if (made is null && _rowsOwner.FindDataTemplate(item, _rowsOwner.ItemTemplate)?.Build(item) is { } built)
            {
                built.DataContext = item;
                made = built;
            }

            after = made is AvonicList { Inset: true };
        }

        PseudoClasses.Set(":after-inset", after);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ListAutomationPeer(this);

    /// <summary><c>role="list"</c>.</summary>
    private sealed class ListAutomationPeer(AvonicList owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

        protected override bool IsControlElementCore() => true;
    }
}
