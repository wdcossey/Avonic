using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages.Showcase;

/// <summary>
/// The showcase's form: inputs, a textarea, a segment, radios, a checkbox and a block button, which says so in an alert made in
/// code, as Ionic's <c>alertController</c> makes one.
/// </summary>
public partial class ProfileScreen : UserControl
{
    private bool _alertOpen;

    public ProfileScreen() => InitializeComponent();

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        // One alert at a time: a second tap while one is on its way in or open does nothing.
        if (_alertOpen || TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        var alert = new AvonicAlert
        {
            Header = "Profile saved",
            Message = "Your changes have been saved.",
            Buttons = ["OK"],
        };

        _alertOpen = true;
        try
        {
            await alert.Present(top);
            await alert.OnDidDismiss();
        }
        catch (InvalidOperationException)
        {
            // Nowhere to present it: nothing to show.
        }
        finally
        {
            _alertOpen = false;
        }
    }
}
