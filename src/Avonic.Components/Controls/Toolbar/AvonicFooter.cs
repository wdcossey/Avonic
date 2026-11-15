using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-footer</c>: the toolbars at the bottom of a page, with md's shadow, which <c>Classes="ion-no-border"</c>
/// removes. The last toolbar's bottom padding is <c>Avonic.SafeAreaBottom</c>, except above a bottom tab bar, which has its own,
/// and while the on-screen keyboard is open, which covers the safe area.
/// It holds many children and Ionic lists no slot, so it's an <see cref="ItemsControl"/>. See
/// <c>docs/components/ion-toolbar/README.md</c>.
/// </summary>
[PseudoClasses(FooterToolbarPadding)]
public class AvonicFooter : ItemsControl
{
    /// <summary>Ionic's <c>footer-toolbar-padding</c> class: the last toolbar takes the bottom safe area.</summary>
    internal const string FooterToolbarPadding = ":footer-toolbar-padding";

    static AvonicFooter()
    {
        // role="contentinfo"
        AutomationProperties.LandmarkTypeProperty.OverrideDefaultValue<AvonicFooter>(AutomationLandmarkType.ContentInfo);

        // A TemplatedControl clips by default, which would cut off the shadow.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicFooter>(false);
    }

    /// <summary>Ionic's <c>keyboardCtrl</c>, from the attach to the detach.</summary>
    private KeyboardController? _keyboard;

    /// <summary>
    /// Ionic's <c>keyboardVisible</c> state. Not reset at a detach, as Ionic doesn't reset it: a footer moved while the keyboard is
    /// open keeps it until the keyboard's next change.
    /// </summary>
    private bool _keyboardVisible;

    public AvonicFooter()
    {
        PseudoClasses.Set(FooterToolbarPadding, true);
    }

    /// <summary>
    /// <c>connectedCallback</c> makes the keyboard controller, and <c>render()</c> reads the tabs around it, the first render being
    /// at the attach. A bar a slot's template makes is there by then, as the tabs' template fills every slot before it lays any out.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _keyboard?.Dispose();
        _keyboard = TopLevel.GetTopLevel(this) is { } top ? KeyboardController.Create(top, OnKeyboardChanged) : null;
        UpdateToolbarPadding();
    }

    /// <summary><c>disconnectedCallback</c>: the keyboard controller is destroyed.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _keyboard?.Dispose();
        _keyboard = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// The keyboard controller's callback: when the keyboard closes, Ionic waits for the app to resize back first, so that the
    /// footer doesn't flicker before it does; then the state changes, and the footer renders again.
    /// </summary>
    private async void OnKeyboardChanged(bool open, Task? waitForResize)
    {
        if (!open && waitForResize is not null)
        {
            await waitForResize;
        }

        _keyboardVisible = open;
        UpdateToolbarPadding();
    }

    /// <summary>
    /// <c>'footer-toolbar-padding': !keyboardVisible &amp;&amp; (!tabBar || tabBar.slot !== 'bottom')</c>, with the closest tabs'
    /// tab bar. Read when Ionic renders, at the attach and when the keyboard changes, so not again when the tabs' slots change
    /// later. The tabs are the visual ancestor, as <see cref="AvonicContent"/>'s page is, since an overlay's content is in its
    /// layer, as Ionic moves an overlay out to <c>ion-app</c>.
    /// </summary>
    private void UpdateToolbarPadding() =>
        PseudoClasses.Set(
            FooterToolbarPadding,
            !_keyboardVisible && (this.FindAncestorOfType<AvonicTabs>() is not { } tabs || !tabs.HasBottomTabBar()));
}
