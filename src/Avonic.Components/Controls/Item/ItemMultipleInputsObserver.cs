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
    private readonly Control _control;
    private readonly Action _onChange;
    private readonly string[] _pseudoClasses;
    private int _previous;

    private ItemMultipleInputsObserver(AvonicItem item, Control control, Action onChange, string[] pseudoClasses)
    {
        _item = item;
        _control = control;
        _onChange = onChange;
        _pseudoClasses = pseudoClasses;
        _previous = Read();
        item.Classes.CollectionChanged += OnClassesChanged;
        item.IndicatorControlChanged(control);
    }

    /// <summary>
    /// Watches the nearest item around <paramref name="control"/>, which is attached, for <paramref name="pseudoClasses"/>, or
    /// null when there's none.
    /// </summary>
    public static ItemMultipleInputsObserver? Create(Control control, Action onChange, params string[] pseudoClasses) =>
        control.FindAncestorOfType<AvonicItem>() is { } item ? new ItemMultipleInputsObserver(item, control, onChange, pseudoClasses) : null;

    /// <summary>Whether the item has <paramref name="pseudoClass"/> now.</summary>
    public bool Has(string pseudoClass) => _item.Classes.Contains(pseudoClass);

    /// <summary>One bit per watched pseudoclass, so that a change of any other class costs no allocation.</summary>
    private int Read()
    {
        var bits = 0;
        for (var i = 0; i < _pseudoClasses.Length; i++)
        {
            if (_item.Classes.Contains(_pseudoClasses[i]))
            {
                bits |= 1 << i;
            }
        }

        return bits;
    }

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
        _item.IndicatorControlChanged(_control);
    }
}
