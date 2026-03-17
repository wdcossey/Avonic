namespace Avonic.Components.Controls;

/// <summary>
/// A touch-first radio button. Use inside an <see cref="AvonicRadioGroup"/> for
/// mutually exclusive selection.
/// </summary>
/// <remarks>
/// Ionic reference: <c>ion-radio</c><br/>
/// Radios can only be <em>checked</em> by user interaction, never unchecked.
/// Deselection is handled exclusively by the parent <see cref="AvonicRadioGroup"/>.
/// </remarks>
public class AvonicRadio : AvonicCheckableBase
{
    protected override void ToggleChecked()
    {
        if (IsChecked) return; // already selected — no-op, no event

        IsChecked = true;
        RaiseEvent(new CheckedChangedEventArgs(CheckedChangedEvent, this, true, Value));
    }
}
