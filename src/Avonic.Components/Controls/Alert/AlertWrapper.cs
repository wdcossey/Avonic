using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avonic.Components.Overlays;

namespace Avonic.Components.Controls;

/// <summary>
/// The alert's <c>.alert-wrapper</c>: <c>role="alertdialog"</c> (<c>alert</c> with no buttons or inputs), <c>aria-modal</c>,
/// <c>tabindex="0"</c>, so it's a tab stop, and present focuses it. To automation it's a <see cref="AutomationControlType.Window"/>,
/// Avalonia's nearest type to a dialog, named by the header and sub header (<c>aria-labelledby</c>), else <c>aria-label</c> from
/// <see cref="AvonicAlert.HtmlAttributes"/>, else the alert's own name; its help text is a text message
/// (<c>aria-describedby</c>), and its <c>AutomationId</c> <c>htmlAttributes.id</c>.
/// </summary>
public class AlertWrapper : Border
{
    static AlertWrapper()
    {
        FocusableProperty.OverrideDefaultValue<AlertWrapper>(true);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new AlertWrapperAutomationPeer(this);

    private sealed class AlertWrapperAutomationPeer(AlertWrapper owner) : ControlAutomationPeer(owner)
    {
        private AvonicAlert? Alert => Owner.TemplatedParent as AvonicAlert;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

        protected override string? GetNameCore() =>
            base.GetNameCore() is { Length: > 0 } name ? name : Alert?.AccessibleName;

        protected override string? GetHelpTextCore() =>
            base.GetHelpTextCore() is { Length: > 0 } help ? help : Alert?.Message as string;

        // htmlAttributes is spread after the wrapper's own attributes; Avalonia's own id would be the part's name.
        protected override string? GetAutomationIdCore() => OverlayAttributes.Id(Alert?.HtmlAttributes) ?? base.GetAutomationIdCore();

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;
    }
}
