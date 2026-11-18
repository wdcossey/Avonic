using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages.Showcase;

/// <summary>
/// The showcase's inbox: a searchbar, and messages that slide to show their options. More opens an action sheet, made in code
/// as Ionic's <c>actionSheetController</c> makes one.
/// </summary>
public partial class InboxScreen : UserControl
{
    public InboxScreen() => InitializeComponent();

    private async void OnMore(object? sender, RoutedEventArgs e)
    {
        if (sender is not AvonicItemOption option || TopLevel.GetTopLevel(this) is not { } top)
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
        await sheet.Present(top);
    }
}
