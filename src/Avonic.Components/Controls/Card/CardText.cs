using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avonic.Components.Behaviors;

namespace Avonic.Components.Controls;

/// <summary><c>role="heading"</c>, for <see cref="AvonicCardTitle"/> and <see cref="AvonicCardSubtitle"/>.</summary>
internal sealed class CardHeadingAutomationPeer(Control owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? AccessibleText.Of(Owner) : name;
    }

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;
}
