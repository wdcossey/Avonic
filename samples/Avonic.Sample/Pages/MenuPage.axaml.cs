using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-menu", Order = 44)]
public partial class MenuPage : UserControl
{
    public MenuPage()
    {
        InitializeComponent();
        foreach (var menu in new[] { StartMenu, EndMenu })
        {
            menu.IonDidOpen += (_, _) => Status.Text = $"{menu.Name} open";
            menu.IonDidClose += (_, e) => Status.Text = $"{menu.Name} closed, role {e.Role ?? "none"}";
        }

        foreach (var item in new[] { StartInbox, StartOutbox, StartFavourites, StartArchived })
        {
            item.Click += (_, _) => Status.Text = $"{item.Name} picked";
        }

        TypeGroup.IonChange += (_, e) => StartMenu.Type = e.Value switch
        {
            "reveal" => MenuType.Reveal,
            "push" => MenuType.Push,
            _ => MenuType.Overlay,
        };
        SwipeToggle.IonChange += (_, e) => StartMenu.MenuSwipeGesture = e.Checked;
        EnabledToggle.IonChange += (_, e) => MenuController.Enable(e.Checked, "start-menu");
        OpenStart.Click += (_, _) => _ = MenuController.Open("start-menu");
        OpenEnd.Click += (_, _) => _ = EndMenu.Open();
    }
}
