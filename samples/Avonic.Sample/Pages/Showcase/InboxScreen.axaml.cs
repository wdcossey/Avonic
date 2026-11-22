using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages.Showcase;

/// <summary>
/// The showcase's inbox, the root of the inbox tab's <see cref="AvonicNav"/>: a searchbar, and messages that slide to show their
/// options. A tap on a message pushes it, as a <see cref="MessageScreen"/>; Archive shows a toast and More opens
/// an action sheet, both made in code as Ionic's <c>toastController</c> and <c>actionSheetController</c> make them.
/// </summary>
public partial class InboxScreen : UserControl
{
    private bool _sheetOpen;
    private bool _toastOpen;

    public InboxScreen()
    {
        InitializeComponent();
        Messages.AddHandler(AvonicItem.ClickEvent, OnOpen);
    }

    /// <summary>The message tapped, pushed on the nav around the inbox with what its row shows.</summary>
    private void OnOpen(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not AvonicItem item || this.FindAncestorOfType<AvonicNav>() is not { } nav)
        {
            return;
        }

        var lines = item.GetVisualDescendants().OfType<TextBlock>().ToList();
        string Line(string classes) => lines.FirstOrDefault(t => t.Classes.Contains(classes))?.Text ?? "";
        var avatar = item.GetVisualDescendants().OfType<AvonicAvatar>().FirstOrDefault();
        var initials = avatar?.Content as TextBlock;
        var message = new MessageScreen
        {
            Sender = Line("h2"),
            Subject = Line("h3"),
            Body = Line("p"),
            Time = item.GetVisualDescendants().OfType<AvonicNote>().FirstOrDefault()?.Content as string ?? "",
            Picture = (avatar?.Content as AvonicImg)?.Src,
            Initials = initials?.Text ?? "",
            AvatarBackground = avatar?.Background,
            AvatarForeground = initials?.Foreground,
        };

        // A factory, one of the nav's page forms: componentProps would set these by reflection, which a trimmed app can strip.
        _ = nav.Push(() => message);
    }

    private async void OnArchive(object? sender, RoutedEventArgs e)
    {
        // One toast at a time, as the sheet below.
        if (_toastOpen || sender is not AvonicItemOption option || TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        option.FindAncestorOfType<AvonicItemSliding>()?.Close();
        var toast = new AvonicToast
        {
            Message = "Conversation archived",
            Duration = 3000,
            // Above the tab bar, as Ionic's docs anchor a toast in a tabs app.
            PositionAnchor = "ShowcaseTabBar",
            Buttons = [new ToastButton { Text = "Undo", Role = "cancel" }],
        };

        _toastOpen = true;
        try
        {
            await toast.Present(top);
            await toast.OnDidDismiss();
        }
        catch (InvalidOperationException)
        {
            // Nowhere to present it: nothing to show.
        }
        finally
        {
            _toastOpen = false;
        }
    }

    private async void OnMore(object? sender, RoutedEventArgs e)
    {
        // One sheet at a time: a second tap while one is on its way in or open does nothing.
        if (_sheetOpen || sender is not AvonicItemOption option || TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        option.FindAncestorOfType<AvonicItemSliding>()?.Close();
        var sheet = new AvonicActionSheet
        {
            Header = "Message",
            Buttons =
            [
                new ActionSheetButton { Text = "Reply", Icon = "arrow-undo-outline" },
                new ActionSheetButton { Text = "Forward", Icon = "arrow-redo-outline" },
                new ActionSheetButton { Text = "Mark as unread", Icon = "mail-unread-outline" },
                new ActionSheetButton { Text = "Cancel", Role = "cancel", Icon = "close" },
            ],
        };

        _sheetOpen = true;
        try
        {
            await sheet.Present(top);
            await sheet.OnDidDismiss();
        }
        catch (InvalidOperationException)
        {
            // Nowhere to present it: nothing to show.
        }
        finally
        {
            _sheetOpen = false;
        }
    }
}
