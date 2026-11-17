using Avalonia.Controls;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;

namespace Avonic.Sample.Pages;

[SamplePage("ion-action-sheet", Order = 40)]
public partial class ActionSheetPage : UserControl
{
    private string _fruit = "banana";

    public ActionSheetPage()
    {
        InitializeComponent();
        BasicSheet.DidDismiss += (_, e) => Report("BasicSheet", e.Detail);

        // isOpen isn't set back by a dismiss, so the page does it, as Ionic's docs say an app has to.
        IsOpenToggle.IonChange += (_, _) => IsOpenSheet.IsOpen = IsOpenToggle.Checked;
        IsOpenSheet.DidDismiss += (_, e) =>
        {
            IsOpenSheet.IsOpen = false;
            IsOpenToggle.Checked = false;
            Report("IsOpenSheet", e.Detail);
        };

        // As ion-select's action sheet: a radio per option, the selected one checked, and a cancel button.
        PickFruit.Click += (_, _) => Show("Fruit", new AvonicActionSheet
        {
            Header = "Pick a fruit",
            Buttons =
            [
                .. new[] { "Apple", "Banana", "Cherry" }.Select(name => new ActionSheetButton
                {
                    Text = name,
                    Data = name.ToLowerInvariant(),
                    Handler = () =>
                    {
                        _fruit = name.ToLowerInvariant();
                        return null;
                    },
                    HtmlAttributes = new Dictionary<string, object?>
                    {
                        ["role"] = "radio",
                        ["aria-checked"] = name.ToLowerInvariant() == _fruit ? "true" : "false",
                    },
                }),
                new ActionSheetButton { Text = "Durian", Disabled = true, HtmlAttributes = new Dictionary<string, object?> { ["role"] = "radio" } },
                new ActionSheetButton { Text = "Cancel", Role = "cancel" },
            ],
        });

        // Not yet's handler returns false, which keeps the sheet open.
        KeepOpen.Click += (_, _) => Show("KeepOpen", new AvonicActionSheet
        {
            Buttons =
            [
                new ActionSheetButton
                {
                    Text = "Not yet",
                    Handler = () =>
                    {
                        Status.Text = "Not yet kept it open";
                        return false;
                    },
                },
                new ActionSheetButton { Text = "Done", Role = "confirm", Data = 1 },
                new ActionSheetButton { Text = "Cancel", Role = "cancel" },
            ],
        });

        PickMany.Click += (_, _) => Show("Many", new AvonicActionSheet
        {
            Header = "Pick one of thirty",
            Buttons =
            [
                .. Enumerable.Range(1, 30).Select(i => new ActionSheetButton { Text = $"Option {i}", Data = i }),
                new ActionSheetButton { Text = "Cancel", Role = "cancel" },
            ],
        });
    }

    private async void Show(string name, AvonicActionSheet sheet)
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        await sheet.Present(top);
        Report(name, await sheet.OnDidDismiss());
    }

    private void Report(string name, OverlayEventDetail detail) =>
        Status.Text = $"{name} dismissed with {detail.Data?.ToString() ?? "nothing"}, role {detail.Role ?? "none"}";
}
