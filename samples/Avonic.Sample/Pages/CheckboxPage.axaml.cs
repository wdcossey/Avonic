using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-checkbox", Order = 20)]
public partial class CheckboxPage : UserControl
{
    public CheckboxPage() => InitializeComponent();

    /// <summary>
    /// What an Ionic app's form does with <c>ionChange</c>: marks the field touched, and invalid while it's unchecked, so the
    /// error text replaces the helper text.
    /// </summary>
    private void OnTermsChange(object? sender, CheckboxChangeEventArgs e)
    {
        Terms.Classes.Set("ion-touched", true);
        Terms.Classes.Set("ion-invalid", !e.Checked);
        Changes.Text = $"ionChange: checked {e.Checked}, value {e.Value}";
    }
}
