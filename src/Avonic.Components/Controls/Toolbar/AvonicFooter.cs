using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.VisualTree;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-footer</c>: the toolbars at the bottom of a page, with md's shadow, which <c>Classes="ion-no-border"</c>
/// removes. The last toolbar's bottom padding is <c>Avonic.SafeAreaBottom</c>, except above a bottom tab bar, which has its own.
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

    public AvonicFooter()
    {
        PseudoClasses.Set(FooterToolbarPadding, true);
    }

    /// <summary>
    /// <c>render()</c> reads the tabs around it, and the first render is at the attach. A bar a slot's template makes is there by
    /// then, as the tabs' template fills every slot before it lays any out.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateToolbarPadding();
    }

    /// <summary>
    /// <c>'footer-toolbar-padding': !keyboardVisible &amp;&amp; (!tabBar || tabBar.slot !== 'bottom')</c>, with the closest tabs'
    /// tab bar. Avonic has no keyboard controller, so the keyboard half isn't here. Read when Ionic renders, so not again when the
    /// tabs' slots change later. The tabs are the visual ancestor, as <see cref="AvonicContent"/>'s page is, since an overlay's
    /// content is in its layer, as Ionic moves an overlay out to <c>ion-app</c>.
    /// </summary>
    private void UpdateToolbarPadding() =>
        PseudoClasses.Set(FooterToolbarPadding, this.FindAncestorOfType<AvonicTabs>() is not { } tabs || !tabs.HasBottomTabBar());
}
