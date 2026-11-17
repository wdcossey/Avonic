using Avalonia.Controls;
using Avalonia.Interactivity;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;

namespace Avonic.Sample.Pages;

[SamplePage("ion-modal", Order = 42)]
public partial class ModalPage : UserControl
{
    public ModalPage()
    {
        InitializeComponent();
        foreach (var modal in new[] { TriggerModal, GuardedModal, SheetModal, PageSheetModal })
        {
            modal.DidDismiss += (_, e) => Status.Text = $"{modal.Name} dismissed, role {e.Role ?? "none"}";
        }

        // The nested modal has its own line, as it goes with its parent, at the same time.
        NestedModal.DidDismiss += (_, e) => NestedStatus.Text = $"NestedModal dismissed, role {e.Role ?? "none"}";

        // The sheet says where it is.
        for (var i = 1; i <= 30; i++)
        {
            SheetRows.Children.Add(new AvonicItem { Content = new AvonicLabel { Content = $"Row {i}" } });
        }

        SheetModal.DidPresent += (_, _) => SheetStatus.Text = $"The sheet is at {SheetModal.GetCurrentBreakpoint()}";
        SheetModal.IonBreakpointDidChange += (_, e) => SheetStatus.Text = $"The sheet is at {e.Breakpoint}";
        SheetModal.DidDismiss += (_, _) => SheetStatus.Text = "The sheet is closed";

        // isOpen isn't set back by a dismiss, so the page does it, as Ionic's docs say an app has to.
        IsOpenToggle.IonChange += (_, _) => GuardedModal.IsOpen = IsOpenToggle.Checked;
        GuardedModal.DidDismiss += (_, _) =>
        {
            GuardedModal.IsOpen = false;
            IsOpenToggle.Checked = false;
        };
        GuardedModal.CanDismiss = new ModalCanDismiss((_, _) => Task.FromResult(AllowClose.IsChecked == true));
        OpenInCode.Click += (_, _) => PresentInCode();
    }

    private void OnCloseTrigger(object? sender, RoutedEventArgs e) => TriggerModal.Dismiss(null, "cancel");

    private void OnOpenNested(object? sender, RoutedEventArgs e) => NestedModal.Present();

    private void OnClosePageSheet(object? sender, RoutedEventArgs e) => PageSheetModal.Dismiss(null, "cancel");

    private void OnCloseGuarded(object? sender, RoutedEventArgs e) => GuardedModal.Dismiss(null, OverlayRoles.Handler);

    private async void PresentInCode()
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        var close = new AvonicButton { Content = "Close" };
        var modal = new AvonicModal
        {
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children = { new TextBlock { Text = "A modal made in code, presented with Present()." }, close },
            },
        };
        close.Click += (_, _) => modal.Dismiss("closed", "cancel");
        await modal.Present(top);
        var detail = await modal.OnDidDismiss();
        Status.Text = $"The modal from code was dismissed with {detail.Data}, role {detail.Role}";
    }
}
