using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// <c>ion-select</c>'s <c>&lt;button aria-haspopup="dialog" aria-expanded&gt;</c>: a button that opens the interface (invoke),
/// with its expanded state (<c>aria-expanded</c>), named by <c>ariaLabel</c> (the label, then the text), described by the helper or
/// error text (<c>aria-describedby</c>). <c>aria-haspopup</c> and <c>aria-invalid</c> have no counterpart in Avalonia.
/// </summary>
internal sealed class SelectAutomationPeer(AvonicSelect owner) : ControlAutomationPeer(owner), IInvokeProvider, IExpandCollapseProvider
{
    // What a screen reader was last told: as it is when the peer is made, then each change raised.
    private string? _name = owner.AccessibleName(AutomationProperties.GetName(owner));
    private string? _helpText = owner.HintText;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override string? GetNameCore() => owner.AccessibleName(AutomationProperties.GetName(owner));

    protected override string? GetHelpTextCore() => owner.HintText;

    protected override bool IsControlElementCore() => true;

    protected override bool IsContentElementCore() => true;

    public ExpandCollapseState ExpandCollapseState => owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public bool ShowsMenu => false;

    /// <summary>A click on the button: the select opens, as from a key.</summary>
    public void Invoke()
    {
        if (owner.IsEffectivelyEnabled)
        {
            owner.Click();
        }
    }

    public void Expand() => Invoke();

    public void Collapse() => _ = owner.Close();

    /// <summary>The name and help text again, raising a change for each that changed since the last it raised.</summary>
    public void Update()
    {
        var oldName = _name;
        var oldHelp = _helpText;
        var name = _name = GetNameCore();
        var help = _helpText = GetHelpTextCore();
        if (oldName != name)
        {
            RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, oldName, name);
        }

        if (oldHelp != help)
        {
            RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, oldHelp, help);
        }
    }

    public void RaiseExpanded(bool old, bool now) =>
        RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            old ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
            now ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
}
