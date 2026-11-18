using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Controls;
using Avonic.Sample.Pages.Showcase;

namespace Avonic.Components.Tests.Sample;

/// <summary>
/// The overlays the showcase uses as an app would: an alert from Save changes, an action sheet from a message's More, and a
/// select for the language.
/// </summary>
public class ShowcaseOverlayTests
{
    [AvaloniaFact]
    public void Save_Changes_Opens_An_Alert() => AvonicAlertCaptureTests.Run((_, clock) =>
    {
        var screen = new ProfileScreen();
        using var scene = new Scene(screen, 390, 844);
        Dispatcher.UIThread.RunJobs();

        var save = Assert.Single(screen.GetVisualDescendants().OfType<AvonicButton>(), b => b.Content as string == "Save changes");
        save.RaiseEvent(new RoutedEventArgs(AvonicButton.ClickEvent));
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 500);

        var alert = Assert.Single(scene.Window.GetVisualDescendants().OfType<AvonicAlert>());
        Assert.Equal("Profile saved", alert.Header);
        Assert.True(alert.IsVisible);
    });

    [AvaloniaFact]
    public void More_Opens_An_Action_Sheet_And_Closes_The_Message() => AvonicAlertCaptureTests.Run((_, clock) =>
    {
        var screen = new InboxScreen();
        using var scene = new Scene(screen, 390, 844);
        Dispatcher.UIThread.RunJobs();

        var sliding = screen.GetVisualDescendants().OfType<AvonicItemSliding>().ElementAt(1);
        sliding.Open(ItemOptionsSide.End);
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 500);
        Assert.True(sliding.GetOpenAmount() > 0);

        var more = Assert.Single(sliding.GetVisualDescendants().OfType<AvonicItemOption>(), o => o.Content as string == "More");
        more.RaiseEvent(new RoutedEventArgs(AvonicItemOption.ClickEvent));
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 500);

        var sheet = Assert.Single(scene.Window.GetVisualDescendants().OfType<AvonicActionSheet>());
        Assert.Equal(["Reply", "Forward", "Mark as unread"], sheet.ButtonControls.Select(b => b.Text));
        Assert.Equal("Cancel", sheet.CancelControl!.Text);
        Assert.Equal(0, sliding.GetOpenAmount());
    });

    [AvaloniaFact]
    public void The_Language_Is_A_Select_Showing_English() => AvonicAlertCaptureTests.Run((_, _) =>
    {
        var screen = new SettingsScreen();
        using var scene = new Scene(screen, 390, 844);
        Dispatcher.UIThread.RunJobs();

        var select = Assert.Single(screen.GetVisualDescendants().OfType<AvonicSelect>());
        Assert.Equal("Language", select.Label);
        Assert.Equal(SelectInterface.Popover, select.Interface);
        var text = Assert.Single(select.GetVisualDescendants().OfType<SelectTextBlock>());
        Assert.Equal("English", text.Text);
    });
}
