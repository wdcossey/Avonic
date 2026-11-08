using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-progress-bar", Order = 18)]
public partial class ProgressBarPage : UserControl
{
    public ProgressBarPage() => InitializeComponent();

    /// <summary>Steps by 0.2, and back to 0 after 1, so a change is seen to ease.</summary>
    private static double Step(double value) => value >= 1 ? 0 : Math.Round(value + 0.2, 1);

    private void OnValue(object? sender, RoutedEventArgs e) => Playground.Value = Step(Playground.Value);

    private void OnBuffer(object? sender, RoutedEventArgs e) => Playground.Buffer = Step(Playground.Buffer);

    private void OnType(object? sender, RoutedEventArgs e) =>
        Playground.Type = Playground.Type == ProgressBarType.Determinate ? ProgressBarType.Indeterminate : ProgressBarType.Determinate;

    private void OnReversed(object? sender, RoutedEventArgs e) => Playground.Reversed = !Playground.Reversed;
}
