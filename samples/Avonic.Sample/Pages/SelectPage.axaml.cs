using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-select", Order = 45)]
public partial class SelectPage : UserControl
{
    public SelectPage()
    {
        InitializeComponent();

        var selects = new[]
        {
            AlertSelect, SheetSelect, PopoverSelect, ModalSelect, MultipleSelect, MultiplePopoverSelect, DescriptionSelect,
            DisabledSelect, FloatingSelect, StackedSelect, SolidSelect, OutlineSelect, RoundSelect, ErrorSelect,
        };
        foreach (var select in selects)
        {
            if (select.Items.Count == 0)
            {
                select.Items.AddRange(Fruits(withDescriptions: select == DescriptionSelect));
            }

            select.IonChange += (_, e) => Status.Text = $"{select.Name}: {Show(e.Value)}";
            select.IonCancel += (_, _) => Status.Text = $"{select.Name}: cancelled";
        }

        ModalSelect.InterfaceOptions = new Dictionary<string, object?> { ["header"] = "Fruits" };
        ErrorSelect.IonChange += (_, _) => ErrorSelect.Classes.Remove("ion-invalid");
    }

    private static IEnumerable<AvonicSelectOption> Fruits(bool withDescriptions) =>
    [
        new() { Value = "apple", Content = "Apple", Description = withDescriptions ? "Crisp, red or green" : null },
        new() { Value = "banana", Content = "Banana", Description = withDescriptions ? "Yellow and long" : null },
        new() { Value = "cherry", Content = "Cherry", IsEnabled = false },
        new() { Value = "grape", Content = "Grape", Description = withDescriptions ? "In bunches" : null },
    ];

    private static string Show(object? value) => value switch
    {
        null => "none",
        object?[] values => string.Join(", ", values),
        _ => value.ToString() ?? "",
    };
}
