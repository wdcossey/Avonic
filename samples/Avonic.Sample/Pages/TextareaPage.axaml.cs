using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-textarea", Order = 25)]
public partial class TextareaPage : UserControl
{
    public TextareaPage() => InitializeComponent();

    /// <summary>Ionic's validation pattern: the app marks it valid or invalid on change, and touched on blur.</summary>
    private void OnValidatedChange(object? sender, TextareaChangeEventArgs e)
    {
        var valid = (e.Value ?? "").Trim().Length >= 10;
        Validated.Classes.Set(AvonicTextarea.ValidClass, valid);
        Validated.Classes.Set(AvonicTextarea.InvalidClass, !valid);
    }

    private void OnValidatedBlur(object? sender, RoutedEventArgs e) => Validated.Classes.Add(AvonicTextarea.TouchedClass);

    private void OnDebouncedInput(object? sender, TextareaInputEventArgs e) => DebouncedText.Text = $"ionInput: {e.Value}";
}
