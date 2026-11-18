using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Sample.Pages.Showcase;

namespace Avonic.Components.Tests.Sample;

/// <summary>
/// The overlays the showcase uses as an app would: an alert from Save changes, an action sheet from a message's More, and a
/// select for the language, a popover.
/// </summary>
public class ShowcaseOverlayTests
{
    [AvaloniaFact]
    public void Save_Changes_Opens_An_Alert_Once() => Run(new ProfileScreen(), (screen, scene, clock) =>
    {
        var save = Assert.Single(screen.GetVisualDescendants().OfType<AvonicButton>(), b => b.Content as string == "Save changes");
        save.RaiseEvent(new RoutedEventArgs(AvonicButton.ClickEvent));
        save.RaiseEvent(new RoutedEventArgs(AvonicButton.ClickEvent));
        Advance(clock, 500);

        var alert = Assert.Single(scene.Window.GetVisualDescendants().OfType<AvonicAlert>());
        Assert.Equal("Profile saved", alert.Header);
        Assert.Equal("Your changes have been saved.", alert.Message);
        Assert.Equal(["OK"], alert.ButtonControls.Select(b => b.Text));
        Assert.True(alert.IsVisible);
    });

    [AvaloniaFact]
    public void More_Opens_An_Action_Sheet_Once_And_Closes_The_Message() => Run(new InboxScreen(), (screen, scene, clock) =>
    {
        var sliding = screen.GetVisualDescendants().OfType<AvonicItemSliding>().ElementAt(1);
        sliding.Open(ItemOptionsSide.End);
        Advance(clock, 500);
        Assert.True(sliding.GetOpenAmount() > 0);

        var more = Assert.Single(sliding.GetVisualDescendants().OfType<AvonicItemOption>(), o => o.Content as string == "More");
        more.RaiseEvent(new RoutedEventArgs(AvonicItemOption.ClickEvent));
        more.RaiseEvent(new RoutedEventArgs(AvonicItemOption.ClickEvent));
        Advance(clock, 500);

        var sheet = Assert.Single(scene.Window.GetVisualDescendants().OfType<AvonicActionSheet>());
        Assert.Equal(["Reply", "Forward", "Mark as unread"], sheet.ButtonControls.Select(b => b.Text));
        Assert.Equal("Cancel", sheet.CancelControl!.Text);
        Assert.Equal(0, sliding.GetOpenAmount());
    });

    [AvaloniaFact]
    public void The_Language_Is_A_Select_Showing_English_In_A_Popover() => Run(new SettingsScreen(), (screen, scene, clock) =>
    {
        var select = Assert.Single(screen.GetVisualDescendants().OfType<AvonicSelect>());
        Assert.Equal("Language", select.Label);
        var text = Assert.Single(select.GetVisualDescendants().OfType<SelectTextBlock>());
        Assert.Equal("English", text.Text);

        // With an event, as a tap gives it: with none, Ionic's popover falls back to the alert.
        _ = select.Open(new PopoverEvent(select));
        Advance(clock, 500);

        var popover = Assert.IsType<AvonicPopover>(select.Overlay);
        Assert.True(popover.IsVisible);
        Assert.Same(scene.Window, TopLevel.GetTopLevel(popover));
        var options = popover.GetVisualDescendants().OfType<AvonicRadio>().Select(r => (r.Content as TextBlock)?.Text).ToList();
        Assert.Equal(["English", "Français", "Deutsch", "Español"], options);
    });

    /// <summary>The screen alone in a phone-sized window, with the sample's base theme, Avonic's theme and a test clock.</summary>
    private static void Run<T>(T screen, Action<T, Scene, TestClock> test) where T : Control
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                using var scene = new Scene(screen, 390, 844);
                Dispatcher.UIThread.RunJobs();
                test(screen, scene, clock);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    private static void Advance(TestClock clock, double by)
    {
        var to = clock.Now.TotalMilliseconds + by;
        while (clock.Now.TotalMilliseconds < to)
        {
            clock.At(clock.Now.TotalMilliseconds + 1);
            Dispatcher.UIThread.RunJobs();
        }
    }
}
