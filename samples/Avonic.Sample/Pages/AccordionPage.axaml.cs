using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-accordion", Order = 32)]
public partial class AccordionPage : UserControl
{
    public AccordionPage() => InitializeComponent();

    /// <summary>What an Ionic app's <c>ionChange</c> listener would see: only taps and keys, not a value set in code.</summary>
    private void OnIonChange(object? sender, AccordionGroupChangeEventArgs e) =>
        Changes.Text = $"ionChange: {e.Value ?? "undefined"}";
}
