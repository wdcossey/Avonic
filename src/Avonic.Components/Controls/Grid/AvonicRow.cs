using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;

namespace Avonic.Components.Controls;

/// <summary>
/// Ionic <c>ion-row</c>: <c>display: flex; flex-wrap: wrap</c> around <see cref="AvonicCol"/>s. It has no props and no look of
/// its own, so it's an <see cref="ItemsControl"/> whose panel is <see cref="RowPanel"/>.
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

    /// <summary>An <see cref="ItemsControl"/> moves focus with the arrow keys. A row doesn't.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
    }

    /// <summary>No <c>role</c>: a <c>div</c>, not a list.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
}
