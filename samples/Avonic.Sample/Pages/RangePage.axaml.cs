using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-range", Order = 23)]
public partial class RangePage : UserControl
{
    private int _inputs;

    public RangePage() => InitializeComponent();

    private void OnInput(object? sender, RangeChangeEventArgs e) => Report.Text = $"ionInput {e.Value} ({++_inputs})";

    private void OnChange(object? sender, RangeChangeEventArgs e) => Report.Text = $"ionChange {e.Value} after {_inputs} ionInput";
}
