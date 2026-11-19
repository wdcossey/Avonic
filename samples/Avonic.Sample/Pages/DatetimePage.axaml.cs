using Avalonia.Controls;
using Avonic.Components.Controls;

namespace Avonic.Sample.Pages;

[SamplePage("ion-datetime", Order = 46)]
public partial class DatetimePage : UserControl
{
    public DatetimePage()
    {
        InitializeComponent();

        // What an Ionic app's ionChange listener would do.
        foreach (var datetime in new[] { DateTimeGrid, WithButtons, MinMax, Several, WheelDate, WheelDateTime, TimeOnly, MonthYear })
        {
            datetime.IonChange += (_, e) => DatetimeChanged.Text = $"{datetime.Name} ionChange: {e.Value?.ToString() ?? "no value"}";
            datetime.IonCancel += (_, _) => DatetimeChanged.Text = $"{datetime.Name} ionCancel";
        }

        // Two dates in June 2023, so that the page shows the same month any day.
        Several.Value = new[] { "2023-06-05", "2023-06-09" };

        // Weekends off on the multiple calendar, and highlighted dates, as Ionic's demos set them in script.
        Several.IsDateEnabled = iso => DateTime.Parse(iso, System.Globalization.CultureInfo.InvariantCulture).DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
        MinMax.HighlightedDates = new DatetimeHighlight[]
        {
            new() { Date = "2023-06-05", TextColor = "#800080", BackgroundColor = "#ffc0cb" },
            new() { Date = "2023-06-20", Border = "1px solid green" },
        };
    }
}
