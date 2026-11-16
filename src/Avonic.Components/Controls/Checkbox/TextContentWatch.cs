using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;

namespace Avonic.Components.Controls;

/// <summary>
/// The checkbox's <c>MutationObserver</c> with <c>characterData</c>, <c>childList</c> and <c>subtree</c> (Ionic 8.8.2, 8.8.4):
/// text or children changing anywhere inside what it watches, which <c>textContent</c> reads. A hidden label isn't measured, so
/// nothing else reports its text arriving.
/// </summary>
internal sealed class TextContentWatch(Action changed) : IDisposable
{
    private readonly List<Action> _unsubscribe = [];
    private object? _root;
    private bool _watching;
    private bool _changed;

    /// <summary>
    /// Watches <paramref name="root"/> and everything logically inside it, instead of what it watched before. The same root again
    /// costs nothing, unless something inside it has changed since, which may have added children to watch.
    /// </summary>
    public void Watch(object? root)
    {
        if (_watching && ReferenceEquals(root, _root) && !_changed)
        {
            return;
        }

        Dispose();
        _root = root;
        _watching = true;
        _changed = false;
        Add(root);
    }

    private void Add(object? node)
    {
        if (node is AvaloniaObject element)
        {
            element.PropertyChanged += OnPropertyChanged;
            _unsubscribe.Add(() => element.PropertyChanged -= OnPropertyChanged);
        }

        if (node is ILogical logical)
        {
            if (logical.LogicalChildren is INotifyCollectionChanged children)
            {
                children.CollectionChanged += OnChildrenChanged;
                _unsubscribe.Add(() => children.CollectionChanged -= OnChildrenChanged);
            }

            foreach (var child in logical.LogicalChildren)
            {
                Add(child);
            }
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBlock.TextProperty || e.Property == TextBlock.InlinesProperty || e.Property == Run.TextProperty
            || e.Property == ContentControl.ContentProperty || e.Property == ContentPresenter.ChildProperty)
        {
            Changed();
        }
    }

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e) => Changed();

    private void Changed()
    {
        _changed = true;
        changed();
    }

    public void Dispose()
    {
        _watching = false;
        _root = null;
        foreach (var unsubscribe in _unsubscribe)
        {
            unsubscribe();
        }

        _unsubscribe.Clear();
    }
}
