using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
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
public class AvonicList : ItemsControl
{
    /// <summary>Ionic <c>lines</c>: how the bottom border is displayed on the items. Null leaves it to each item.</summary>
    public static readonly StyledProperty<ListLines?> LinesProperty =
        AvaloniaProperty.Register<AvonicList, ListLines?>(nameof(Lines));

    /// <summary>Ionic <c>inset</c>: margin around the list, and rounded corners.</summary>
    public static readonly StyledProperty<bool> InsetProperty =
        AvaloniaProperty.Register<AvonicList, bool>(nameof(Inset));

    private AvaloniaList<Control>? _siblings;
    private Control? _inParent;

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
        else if (change.Property == InsetProperty)
        {
            PseudoClasses.Set(":inset", Inset);
            var index = IndexInSiblings();
            if (index >= 0 && index + 1 < _siblings!.Count)
            {
                ListIn(_siblings[index + 1])?.UpdateAfterInset();
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // An ItemsControl wraps templated content in a presenter. In the DOM there's no such box.
        _inParent = this;
        while (_inParent.GetVisualParent() is ContentPresenter { TemplatedParent: null } presenter)
        {
            _inParent = presenter;
        }

        if (_inParent.GetVisualParent() is Panel panel)
        {
            _siblings = panel.Children;
            _siblings.CollectionChanged += OnSiblingsChanged;
        }

        UpdateAfterInset();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_siblings is not null)
        {
            _siblings.CollectionChanged -= OnSiblingsChanged;
            _siblings = null;
        }

        _inParent = null;
        UpdateAfterInset();
    }

    /// <summary>An <see cref="ItemsControl"/> moves focus with the arrow keys. ion-list doesn't: only a popover's does.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
    }

    private void OnSiblingsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateAfterInset();

    private int IndexInSiblings() => _inParent is null ? -1 : _siblings?.IndexOf(_inParent) ?? -1;

    private static AvonicList? ListIn(Control sibling) =>
        (sibling as ContentPresenter)?.Child as AvonicList ?? sibling as AvonicList;

    /// <summary>
    /// <c>.list-inset + ion-list</c>: the list before this one is inset. Avalonia's selectors have no sibling
    /// combinator, so it's a pseudoclass, and the theme has the rest of the rule.
    /// </summary>
    private void UpdateAfterInset()
    {
        var index = IndexInSiblings();
        PseudoClasses.Set(":after-inset", index > 0 && ListIn(_siblings![index - 1]) is { Inset: true });
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ListAutomationPeer(this);

    /// <summary><c>role="list"</c>.</summary>
    private sealed class ListAutomationPeer(AvonicList owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

        protected override bool IsControlElementCore() => true;
    }
}
