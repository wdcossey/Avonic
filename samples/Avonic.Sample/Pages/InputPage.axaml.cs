using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-input", Order = 24)]
public partial class InputPage : UserControl
{
    public InputPage() => InitializeComponent();

    /// <summary>Ionic's validation example: the app marks it valid or invalid on change, and touched on blur.</summary>
    private void OnValidatedChange(object? sender, InputChangeEventArgs e)
    {
        var valid = Regex.IsMatch(e.Value ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        Validated.Classes.Set(AvonicInput.ValidClass, valid);
        Validated.Classes.Set(AvonicInput.InvalidClass, !valid);
    }

    private void OnValidatedBlur(object? sender, RoutedEventArgs e) => Validated.Classes.Add(AvonicInput.TouchedClass);

    private void OnDebouncedInput(object? sender, InputInputEventArgs e) => DebouncedText.Text = $"ionInput: {e.Value}";
}
