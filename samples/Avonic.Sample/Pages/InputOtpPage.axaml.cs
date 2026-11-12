using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-input-otp", Order = 27)]
public partial class InputOtpPage : UserControl
{
    public InputOtpPage() => InitializeComponent();

    private void OnComplete(object? sender, InputOtpCompleteEventArgs e) => Status.Text = $"ionComplete: {e.Value}";

    private void OnChange(object? sender, InputOtpChangeEventArgs e) => Status.Text = $"ionChange: {e.Value}";

    /// <summary>Ionic's validation pattern: the app marks it valid or invalid as it's typed, and touched on blur.</summary>
    private void OnValidatedInput(object? sender, InputOtpInputEventArgs e)
    {
        var valid = e.Value == "1234";
        Validated.Classes.Set(AvonicInputOtp.ValidClass, valid);
        Validated.Classes.Set(AvonicInputOtp.InvalidClass, !valid);
    }

    private void OnValidatedBlur(object? sender, RoutedEventArgs e) => Validated.Classes.Add(AvonicInputOtp.TouchedClass);
}
