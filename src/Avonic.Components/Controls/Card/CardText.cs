using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace Avonic.Components.Controls;

/// <summary>What automation reads from a card's controls: a browser names a button or a heading from the text inside it, leaving out what's hidden.</summary>
internal static class CardText
{
    public static string Of(Control owner) =>
        string.Join(
            " ",
            owner.GetLogicalDescendants()
                .OfType<TextBlock>()
                .Where(block => block.IsEffectivelyVisible)
                .Select(block => block.Inlines is { Count: > 0 } inlines ? inlines.Text : block.Text)
                .Where(text => !string.IsNullOrEmpty(text)));
}

/// <summary><c>role="heading"</c>, for <see cref="AvonicCardTitle"/> and <see cref="AvonicCardSubtitle"/>.</summary>
internal sealed class CardHeadingAutomationPeer(Control owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? CardText.Of(Owner) : name;
    }

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;
}
