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
    public ProfileScreen() => InitializeComponent();

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        var alert = new AvonicAlert
        {
            Header = "Profile saved",
            Message = "Your changes have been saved.",
            Buttons = ["OK"],
        };
        await alert.Present(top);
    }
}
