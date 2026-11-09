using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-toggle", Order = 21)]
public partial class TogglePage : UserControl
{
    private int _changes;

    public TogglePage() => InitializeComponent();

    /// <summary>What an Ionic app's ionChange listener sees: the new state and the value, once per flip, drags included.</summary>
    private void OnIonChange(object? sender, ToggleChangeEventArgs e)
    {
        _changes++;
        var name = (sender as AvonicToggle)?.Content as string ?? "A toggle";
        Changes.Text = $"{_changes}: {name} is {(e.Checked ? "on" : "off")}, value {e.Value ?? "null"}";
    }

    /// <summary>An app's own validation: invalid while it's off, as ion-invalid is the app's to set.</summary>
    private void OnRequiredChange(object? sender, ToggleChangeEventArgs e)
    {
        OnIonChange(sender, e);
        RequiredToggle.Classes.Set("ion-invalid", !e.Checked);
    }
}
