using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-header</c>: the toolbars at the top of a page, with md's shadow below them, which <c>Classes="ion-no-border"</c>
/// removes. The first toolbar's top padding is <c>Avonic.SafeAreaTop</c>. It holds many children and Ionic lists no slot, so
/// it's an <see cref="ItemsControl"/>. See <c>docs/components/ion-toolbar/README.md</c>.
/// </summary>
public class AvonicHeader : ItemsControl
{
    static AvonicHeader()
    {
        // role="banner"
        AutomationProperties.LandmarkTypeProperty.OverrideDefaultValue<AvonicHeader>(AutomationLandmarkType.Banner);

        // A TemplatedControl clips by default, which would cut off the shadow.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicHeader>(false);
    }
}
