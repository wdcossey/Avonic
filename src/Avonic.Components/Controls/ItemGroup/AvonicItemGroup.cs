using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-item-group</c>: a run of items and dividers that belong together. It has no look of its own. A
/// sticky <see cref="AvonicItemDivider"/> stays inside its group, and a label in a group acts as it does in an item.
/// </summary>
public class AvonicItemGroup : ItemsControl
{
    /// <summary>An <see cref="ItemsControl"/> moves focus with the arrow keys. ion-item-group doesn't.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ItemGroupAutomationPeer(this);

    /// <summary><c>role="group"</c>.</summary>
    private sealed class ItemGroupAutomationPeer(AvonicItemGroup owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        protected override bool IsControlElementCore() => true;
    }
}
