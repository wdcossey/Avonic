using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Ionic 9's <c>utils/attribute-controller.js</c> (9.0.5, 9.0.11, #207): <c>ion-button</c>, <c>ion-card</c>, <c>ion-item</c>,
/// <c>ion-breadcrumb</c>, <c>ion-fab-button</c>, <c>ion-item-option</c>, <c>ion-segment-button</c> and <c>ion-tab-button</c> move
/// the host's <c>tabindex</c> to their native element, a negative one staying on the host, where it takes the control out of the
/// tab order; and the button, the card and the item keep aria attributes written after load in step on it. Here the host is the
/// focusable element and its peer reads <see cref="AutomationProperties"/> live, so this already held before Ionic 9: these pin
/// it, and fail if a template ever puts a tab stop of its own inside the host, or a peer stops reading the host's name.
/// </summary>
public class AttributeForwardingTests
{
    public static TheoryData<string> Controls =>
    [
        "button", "card", "item", "breadcrumb", "fab-button", "item-option", "segment-button", "tab-button",
        "card not clickable", "item not clickable",
    ];

    private static Control Make(string name) => name switch
    {
        "button" => new AvonicButton { Content = "Button" },
        "card" => new AvonicCard { Button = true, Content = "Card" },
        "item" => new AvonicItem { Button = true, Content = "Item" },
        "breadcrumb" => new AvonicBreadcrumb { Href = "#", Content = "Breadcrumb" },
        "fab-button" => new AvonicFabButton { Content = "Fab" },
        "item-option" => new AvonicItemOption { Content = "Option" },
        "segment-button" => new AvonicSegmentButton { Items = { new AvonicLabel { Content = "Segment" } } },
        // An <a> with no href, which takes focus only with a tabindex: here, Focusable.
        "tab-button" => new AvonicTabButton { Items = { new AvonicLabel { Content = "Tab" } }, Focusable = true },
        // tabindex stays on a host with no native element, which it makes a tab stop: here, Focusable on the host.
        "card not clickable" => new AvonicCard { Content = "Card", Focusable = true },
        "item not clickable" => new AvonicItem { Content = "Item", Focusable = true },
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void TabIndex_Orders_The_Tab_Stops_And_IsTabStop_False_Takes_The_Control_Out(string name) => InstalledTheme.Run(_ =>
    {
        var before = new Button { Content = "Before" };
        var later = Make(name);
        var sooner = Make(name);
        KeyboardNavigation.SetTabIndex(before, 0);
        KeyboardNavigation.SetTabIndex(later, 2);
        KeyboardNavigation.SetTabIndex(sooner, 1);
        using var scene = new Scene(new StackPanel { Children = { before, later, sooner } });
        var focus = scene.Window.FocusManager!;

        // tabindex="2" and tabindex="1": the lower first, whatever the order in the tree, and the host itself, not a part in it.
        before.Focus();
        Tab(scene);
        Assert.Same(sooner, focus.GetFocusedElement());
        Tab(scene);
        Assert.Same(later, focus.GetFocusedElement());

        // tabindex="-1" stays on the host in Ionic, and the control leaves the tab order, but can still be focused.
        KeyboardNavigation.SetIsTabStop(sooner, false);
        before.Focus();
        Tab(scene);
        Assert.Same(later, focus.GetFocusedElement());
        Assert.True(sooner.Focus());
        Assert.Same(sooner, focus.GetFocusedElement());
    });

    private static void Tab(Scene scene)
    {
        scene.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void A_Name_And_Help_Text_Set_After_Load_Reach_The_Peer(string name) => InstalledTheme.Run(_ =>
    {
        var control = Make(name);
        using var scene = new Scene(new StackPanel { Children = { control } });
        var peer = ControlAutomationPeer.CreatePeerForElement(control);
        Assert.NotEqual("Renamed", peer.GetName());

        // aria-label and aria-description written to the host after load. The peer reads them when asked; like a stock Button's,
        // it raises no change of the name (docs/infra/avalonia-issues.md).
        AutomationProperties.SetName(control, "Renamed");
        AutomationProperties.SetHelpText(control, "Described");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Renamed", peer.GetName());
        Assert.Equal("Described", peer.GetHelpText());
    });
}
