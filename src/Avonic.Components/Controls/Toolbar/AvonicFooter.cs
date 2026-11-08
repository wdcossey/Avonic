using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-footer</c>: the toolbars at the bottom of a page, with md's shadow, which <c>Classes="ion-no-border"</c>
/// removes. The last toolbar's bottom padding is <c>Avonic.SafeAreaBottom</c>. It holds many children and Ionic lists no
/// slot, so it's an <see cref="ItemsControl"/>. See <c>docs/components/ion-toolbar/README.md</c>.
/// </summary>
public class AvonicFooter : ItemsControl
{
    static AvonicFooter()
    {
        // role="contentinfo"
        AutomationProperties.LandmarkTypeProperty.OverrideDefaultValue<AvonicFooter>(AutomationLandmarkType.ContentInfo);

        // A TemplatedControl clips by default, which would cut off the shadow.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicFooter>(false);
    }
}
