using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic's <c>createItemMultipleInputsObserver</c> (<c>utils/forms/item-multiple-inputs.js</c>): the item around a checkbox, a
/// radio or a toggle, and a call back when one of the item's states that the control's focus indicator reads turns on or off,
/// as <c>item-multiple-inputs</c> comes after the control has rendered. Here those states are the item's pseudoclasses, which
/// are in its <see cref="StyledElement.Classes"/>.
/// </summary>
/// <remarks>
/// It also tells the item that one of these controls has come or gone, which is the item's own <c>MutationObserver</c> for
/// them (<c>watchForIndicatorControls</c>): a control inside a wrapper in a slot changes no slot.
/// </remarks>
internal sealed class ItemMultipleInputsObserver : IDisposable
{
    private readonly AvonicItem _item;
    private readonly Action _onChange;
    private readonly string[] _pseudoClasses;
    private string _previous;

    private ItemMultipleInputsObserver(AvonicItem item, Action onChange, string[] pseudoClasses)
    {
        _item = item;
        _onChange = onChange;
        _pseudoClasses = pseudoClasses;
        _previous = Read();
        item.Classes.CollectionChanged += OnClassesChanged;
        item.IndicatorControlChanged();
    }

    /// <summary>
    /// Watches the nearest item around <paramref name="control"/>, which is attached, for <paramref name="pseudoClasses"/>, or
    /// null when there's none.
    /// </summary>
    public static ItemMultipleInputsObserver? Create(Visual control, Action onChange, params string[] pseudoClasses) =>
        control.FindAncestorOfType<AvonicItem>() is { } item ? new ItemMultipleInputsObserver(item, onChange, pseudoClasses) : null;

    /// <summary>Whether the item has <paramref name="pseudoClass"/> now.</summary>
    public bool Has(string pseudoClass) => _item.Classes.Contains(pseudoClass);

    private string Read() => string.Join(",", _pseudoClasses.Select(name => _item.Classes.Contains(name)));

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var now = Read();
        if (now != _previous)
        {
            _previous = now;
            _onChange();
        }
    }

    public void Dispose()
    {
        _item.Classes.CollectionChanged -= OnClassesChanged;
        _item.IndicatorControlChanged();
    }
}
