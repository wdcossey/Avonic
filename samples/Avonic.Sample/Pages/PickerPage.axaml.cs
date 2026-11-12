using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-picker", Order = 37)]
public partial class PickerPage : UserControl
{
    public PickerPage()
    {
        InitializeComponent();

        // What an Ionic app's ionChange listener would do.
        Colors.IonChange += (_, e) => Changed.Text = $"ionChange: {e.Value}";
        Numbers.IonChange += (_, e) => Changed.Text = $"ionChange: {e.Value}";
    }
}
