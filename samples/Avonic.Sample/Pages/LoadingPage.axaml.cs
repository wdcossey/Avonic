using Avalonia.Controls;
using Avalonia.Media;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;

namespace Avonic.Sample.Pages;

[SamplePage("ion-loading", Order = 41)]
public partial class LoadingPage : UserControl
{
    public LoadingPage()
    {
        InitializeComponent();
        TimedLoading.DidDismiss += (_, e) => Report("TimedLoading", e.Detail);

        // isOpen isn't set back by a dismiss, so the page does it, as Ionic's docs say an app has to.
        LoadingOpenToggle.IonChange += (_, _) => IsOpenLoading.IsOpen = LoadingOpenToggle.Checked;
        IsOpenLoading.DidDismiss += (_, e) =>
        {
            IsOpenLoading.IsOpen = false;
            LoadingOpenToggle.Checked = false;
            Report("IsOpenLoading", e.Detail);
        };

        ShowDismissable.Click += (_, _) => Show("Dismissable", new AvonicLoading
        {
            Message = "Tap outside to close",
            Spinner = SpinnerName.Circles,
            BackdropDismiss = true,
        });

        ShowNoSpinner.Click += (_, _) => Show("NoSpinner", new AvonicLoading
        {
            Message = "Please wait...",
            Spinner = null,
            BackdropDismiss = true,
        });

        // The backdrop is transparent, and still covers the page: a tap on it still dismisses.
        ShowNoBackdrop.Click += (_, _) => Show("NoBackdrop", new AvonicLoading
        {
            Message = "The page shows through",
            ShowBackdrop = false,
            BackdropDismiss = true,
        });

        // What a cssClass with --background, --spinner-color and color does in Ionic, as the loading's own keys.
        ShowCustom.Click += (_, _) =>
        {
            var custom = new AvonicLoading
            {
                Message = "Custom colours",
                Spinner = SpinnerName.Lines,
                CssClass = "custom-loading",
                BackdropDismiss = true,
                Foreground = Brushes.White,
            };
            custom.Resources["Avonic.Loading.Background"] = new SolidColorBrush(Color.Parse("#222428"));
            custom.Resources["Avonic.Loading.SpinnerColor"] = Brushes.White;
            Show("Custom", custom);
        };

        ShowSpinnerOnly.Click += (_, _) => Show("SpinnerOnly", new AvonicLoading
        {
            Spinner = SpinnerName.Crescent,
            BackdropDismiss = true,
            HtmlAttributes = new Dictionary<string, object?> { ["aria-label"] = "Loading, spinner alone" },
        });
    }

    private async void Show(string name, AvonicLoading loading)
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        // An async void handler: an exception here would reach the dispatcher and end the app, so the status line shows it.
        try
        {
            var dismissed = loading.OnDidDismiss();
            await loading.Present(top);
            Report(name, await dismissed);
        }
        catch (Exception exception)
        {
            LoadingStatus.Text = $"{name} failed: {exception.Message}";
        }
    }

    private void Report(string name, OverlayEventDetail detail) =>
        LoadingStatus.Text = $"{name} dismissed with role {detail.Role ?? "none"}";
}
