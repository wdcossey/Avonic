using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-row</c>: <c>display: flex; flex-wrap: wrap</c> around <see cref="AvonicCol"/>s. It has no props and no look of
/// its own, so it's an <see cref="ItemsControl"/> whose panel is <see cref="RowPanel"/>. An <see cref="ItemsControl"/> only
/// moves focus with the arrow keys in a panel that's an <c>INavigableContainer</c>, which <see cref="RowPanel"/> isn't, so a
/// row doesn't, as in Ionic.
/// </summary>
public class AvonicRow : ItemsControl
{
    private static readonly FuncTemplate<Panel?> DefaultPanel = new(() => new RowPanel());

    static AvonicRow()
    {
        ItemsPanelProperty.OverrideDefaultValue<AvonicRow>(DefaultPanel);

        // overflow: visible, so a column pushed or sized past the row shows. A TemplatedControl clips otherwise.
        ClipToBoundsProperty.OverrideDefaultValue<AvonicRow>(false);
    }

    /// <summary>No <c>role</c>: a <c>div</c>, not a list.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
}
