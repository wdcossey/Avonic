using Avalonia.Controls;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;

namespace Avonic.Sample.Pages;

[SamplePage("ion-toast", Order = 38)]
public partial class ToastPage : UserControl
{
    private int _retries;

    public ToastPage()
    {
        InitializeComponent();
        BottomToast.DidDismiss += (_, e) => Report("BottomToast", e.Detail);

        // isOpen isn't set back by a dismiss, so the page does it, as Ionic's docs say an app has to.
        SwipeToastToggle.IonChange += (_, _) => SwipeToast.IsOpen = SwipeToastToggle.Checked;
        SwipeToast.DidDismiss += (_, e) =>
        {
            SwipeToast.IsOpen = false;
            SwipeToastToggle.Checked = false;
            Report("SwipeToast", e.Detail);
        };

        ShowTopToast.Click += (_, _) => Show("TopToast", new AvonicToast
        {
            Position = ToastPosition.Top,
            Header = "Heads up",
            Message = "A toast at the top, with an icon-only button.",
            Icon = "information-circle",
            Buttons =
            [
                new ToastButton
                {
                    Icon = "close",
                    Role = "cancel",
                    HtmlAttributes = new Dictionary<string, object?> { ["aria-label"] = "Close the top toast" },
                },
            ],
        });

        ShowMiddleToast.Click += (_, _) => Show("MiddleToast", new AvonicToast
        {
            Position = ToastPosition.Middle,
            Message = "In the middle: swipe it up or down.",
            ToastSwipeGesture = ToastSwipeGestureDirection.Vertical,
            Buttons = [new ToastButton { Text = "Middle done", Role = "done" }],
        });

        // Its first Retry's handler returns false, which keeps the toast open.
        ShowDangerToast.Click += (_, _) =>
        {
            _retries = 0;
            Show("DangerToast", new AvonicToast
            {
                Color = "danger",
                Icon = "alert-circle",
                Message = "The upload failed.",
                Buttons =
                [
                    new ToastButton
                    {
                        Text = "Retry upload",
                        Role = "retry",
                        Handler = () =>
                        {
                            _retries++;
                            ToastStatus.Text = $"Retry upload pressed {_retries} time(s)";
                            return _retries < 2 ? false : null;
                        },
                    },
                ],
            });
        };

        ShowTimedToast.Click += (_, _) => Show("TimedToast", new AvonicToast { Message = "This one goes by itself.", Duration = 2000 });
    }

    private async void Show(string name, AvonicToast toast)
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        await toast.Present(top);
        Report(name, await toast.OnDidDismiss());
    }

    private void Report(string name, OverlayEventDetail detail) =>
        ToastStatus.Text = $"{name} dismissed with role {detail.Role ?? "none"}";
}
