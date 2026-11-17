using Avalonia.Controls;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;

namespace Avonic.Sample.Pages;

[SamplePage("ion-alert", Order = 39)]
public partial class AlertPage : UserControl
{
    public AlertPage()
    {
        InitializeComponent();
        BasicAlert.DidDismiss += (_, e) => Report("BasicAlert", e.Detail);

        // isOpen isn't set back by a dismiss, so the page does it, as Ionic's docs say an app has to.
        IsOpenToggle.IonChange += (_, _) => ThreeAlert.IsOpen = IsOpenToggle.Checked;
        ThreeAlert.DidDismiss += (_, e) =>
        {
            ThreeAlert.IsOpen = false;
            IsOpenToggle.Checked = false;
            Report("ThreeAlert", e.Detail);
        };

        PickColor.Click += (_, _) => Show("Color", new AvonicAlert
        {
            Header = "Select a color",
            Buttons = ["Cancel", "OK"],
            Inputs =
            [
                new AlertInput { Type = AlertInputType.Radio, Label = "Red", Value = "red" },
                new AlertInput { Type = AlertInputType.Radio, Label = "Blue", Value = "blue", Checked = true },
                new AlertInput { Type = AlertInputType.Radio, Label = "Green", Value = "green" },
                new AlertInput { Type = AlertInputType.Radio, Label = "Disabled", Value = "disabled", Disabled = true },
            ],
        });

        PickToppings.Click += (_, _) => Show("Toppings", new AvonicAlert
        {
            Header = "Toppings",
            Buttons = ["Cancel", "OK"],
            Inputs =
            [
                new AlertInput { Type = AlertInputType.Checkbox, Label = "Cheese", Value = "cheese", Checked = true },
                new AlertInput { Type = AlertInputType.Checkbox, Label = "Olives", Value = "olives" },
                new AlertInput { Type = AlertInputType.Checkbox, Label = "A topping with a really long name that wraps around", Value = "long" },
                new AlertInput { Type = AlertInputType.Checkbox, Label = "Disabled", Value = "disabled", Disabled = true },
            ],
        });

        // Save's handler returns false while the name is empty, which keeps the alert open.
        EnterName.Click += (_, _) => Show("Name", new AvonicAlert
        {
            Header = "Please enter your info",
            Buttons =
            [
                new AlertButton { Text = "Cancel", Role = "cancel" },
                new AlertButton
                {
                    Text = "Save",
                    Handler = values =>
                    {
                        if (values is IReadOnlyDictionary<string, object?> fields && fields["name"] is string { Length: > 0 })
                        {
                            return new Dictionary<string, object?> { ["saved"] = true };
                        }

                        Status.Text = "Save kept it open: there's no name";
                        return false;
                    },
                },
            ],
            Inputs =
            [
                new AlertInput { Name = "name", Placeholder = "Name" },
                new AlertInput { Name = "nickname", Placeholder = "Nickname (max 8 characters)", Attributes = new Dictionary<string, object?> { ["maxlength"] = 8 } },
                new AlertInput { Name = "age", Type = AlertInputType.Number, Placeholder = "Age" },
                new AlertInput { Name = "about", Type = AlertInputType.Textarea, Placeholder = "A little about yourself" },
            ],
        });

        PickMany.Click += (_, _) => Show("Many", new AvonicAlert
        {
            Header = "Pick one of twenty",
            Buttons = ["Cancel", "OK"],
            Inputs = Enumerable.Range(1, 20)
                .Select(i => new AlertInput { Type = AlertInputType.Radio, Label = $"Option {i}", Value = i, Checked = i == 1 })
                .ToList(),
        });
    }

    private async void Show(string name, AvonicAlert alert)
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        await alert.Present(top);
        Report(name, await alert.OnDidDismiss());
    }

    private void Report(string name, OverlayEventDetail detail) =>
        Status.Text = $"{name} dismissed with {detail.Data?.ToString() ?? "nothing"}, role {detail.Role ?? "none"}";
}
