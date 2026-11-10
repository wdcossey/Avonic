using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

/// <summary>ion-radio and ion-radio-group, which are one issue.</summary>
[SamplePage("ion-radio", Order = 22)]
public partial class RadioPage : UserControl
{
    public RadioPage() => InitializeComponent();

    private void OnFruitChange(object? sender, RadioGroupChangeEventArgs e) =>
        FruitChoice.Text = $"ionChange: {e.Value ?? "nothing"}";

    /// <summary>What an app's validation does: the classes that show the error text until a size is chosen.</summary>
    private void OnValidate(object? sender, RoutedEventArgs e)
    {
        var invalid = Validated.Value is null;
        Validated.Classes.Set("ion-touched", true);
        Validated.Classes.Set("ion-invalid", invalid);
    }
}
