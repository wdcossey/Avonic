using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Container that enforces mutually exclusive selection among child
/// <see cref="AvonicRadio"/> controls.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-radio-group</c><br/>
/// Subscribes to the bubbling <c>CheckedChangedEvent</c> from descendant radios.
/// When a radio checks itself the group sets its <see cref="Value"/> and unchecks
/// all sibling radios. Setting <see cref="Value"/> from code syncs children in turn.
/// </remarks>
public class AvonicRadioGroup : ContentControl
{
    // ── Direct Properties ────────────────────────────────────────────────────

    private object? _value;

    public static readonly DirectProperty<AvonicRadioGroup, object?> ValueProperty =
        AvaloniaProperty.RegisterDirect<AvonicRadioGroup, object?>(
            nameof(Value),
            o => o._value,
            (o, v) => o.Value = v);

    /// <summary>
    /// The value of the currently selected <see cref="AvonicRadio"/>.
    /// Setting this programmatically checks the matching radio and unchecks all others.
    /// </summary>
    public object? Value
    {
        get => _value;
        set => SetAndRaise(ValueProperty, ref _value, value);
    }

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Raised when the selected radio changes.</summary>
    public event EventHandler<CheckedChangedEventArgs>? CheckedChanged
    {
        add    => AddHandler(AvonicCheckableBase.CheckedChangedEvent, value);
        remove => RemoveHandler(AvonicCheckableBase.CheckedChangedEvent, value);
    }

    // ── State ────────────────────────────────────────────────────────────────

    private bool _isUpdating;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AddHandler(AvonicCheckableBase.CheckedChangedEvent, OnChildCheckedChanged);

        // If a value was set before attachment, sync children now that the tree exists.
        if (_value != null)
            SyncChildrenToValue(_value);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        RemoveHandler(AvonicCheckableBase.CheckedChangedEvent, OnChildCheckedChanged);
    }

    // ── Property Change ──────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
            SyncChildrenToValue(change.GetNewValue<object?>());
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void OnChildCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_isUpdating) return;
        if (e.Source is not AvonicRadio checkedRadio || !e.IsChecked) return;

        _isUpdating = true;
        try
        {
            Value = checkedRadio.Value;

            foreach (var radio in this.GetVisualDescendants().OfType<AvonicRadio>())
            {
                if (radio != checkedRadio)
                    radio.IsChecked = false;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SyncChildrenToValue(object? value)
    {
        if (_isUpdating) return;

        _isUpdating = true;
        try
        {
            foreach (var radio in this.GetVisualDescendants().OfType<AvonicRadio>())
                radio.IsChecked = Equals(radio.Value, value);
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
