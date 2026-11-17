using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace Avonic.Components.Controls;

/// <summary>
/// The modal's <c>.modal-wrapper</c>, its <c>content</c> part: <c>role="dialog"</c>, <c>aria-modal="true"</c> and
/// <c>tabindex="-1"</c>, so it can take focus when the modal is presented and isn't a tab stop. To automation it's a
/// <see cref="AutomationControlType.Window"/>, Avalonia's nearest type to a dialog, named by its own
/// <see cref="AutomationProperties.NameProperty"/> or else the modal's, as Ionic moves the host's <c>aria-label</c> onto it.
/// </summary>
public class ModalWrapper : Border
{
    /// <summary>
    /// <c>border-width: var(--border-width); border-style: var(--border-style)</c>: the width, or 0 when the style is <c>none</c> or
    /// <c>hidden</c>. Avalonia's <see cref="Border"/> draws a solid line, so any other style is solid.
    /// </summary>
    public static readonly IMultiValueConverter BorderThicknessConverter = new FuncMultiValueConverter<object?, Thickness>(values =>
    {
        var list = values.ToList();
        var width = list.Count > 0 && list[0] is double d ? d : 0;
        var style = list.Count > 1 ? (list[1] as string)?.Trim().ToLowerInvariant() : null;
        return style is null or "" or "none" or "hidden" ? default : new Thickness(width);
    });

    static ModalWrapper()
    {
        FocusableProperty.OverrideDefaultValue<ModalWrapper>(true);
        KeyboardNavigation.IsTabStopProperty.OverrideDefaultValue<ModalWrapper>(false);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ModalWrapperAutomationPeer(this);

    private sealed class ModalWrapperAutomationPeer(ModalWrapper owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

        protected override string? GetNameCore() =>
            base.GetNameCore() is { Length: > 0 } name ? name
            : Owner.TemplatedParent is Control host ? AutomationProperties.GetName(host)
            : null;

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;
    }
}
