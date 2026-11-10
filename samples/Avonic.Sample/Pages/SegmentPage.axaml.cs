using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-segment", Order = 28)]
public partial class SegmentPage : UserControl
{
    public SegmentPage()
    {
        InitializeComponent();

        // What an Ionic app's ionChange listener would do.
        Basic.IonChange += (_, e) => Changed.Text = $"ionChange: {e.Value}";
    }
}
