using Avalonia.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-datetime-button", Order = 79)]
public partial class DatetimeButtonPage : UserControl
{
    public DatetimeButtonPage()
    {
        InitializeComponent();

        // Three dates in October 2023, set in script as Ionic's demos set an array.
        SeveralDatetime.Value = new[] { "2023-10-02", "2023-10-09", "2023-10-16" };

        // What an Ionic app's ionChange listener would do.
        foreach (var datetime in new[] { ModalDatetime, PopoverDatetime, TimeDatetime, WheelDatetime, SlotDatetime, SeveralDatetime, ItemDatetime, InlineDatetime })
        {
            datetime.IonChange += (_, e) => DatetimeButtonChanged.Text = $"{datetime.Name} ionChange: {e.Value?.ToString() ?? "no value"}";
        }
    }
}
