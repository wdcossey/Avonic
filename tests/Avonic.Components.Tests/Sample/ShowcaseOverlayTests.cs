using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Sample.Pages;
using Avonic.Sample.Pages.Showcase;

namespace Avonic.Components.Tests.Sample;

/// <summary>
/// What the showcase does as an app would: an alert from Save changes, an action sheet from a message's More, a toast from its
/// Archive, a select for the language (a popover), a message pushed on the inbox's nav with a back button, the side menu from the
/// inbox's menu button, and a birthday's datetime button.
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

    [AvaloniaFact]
    public void Archive_Takes_The_Message_Out_With_A_Toast_Above_The_Tab_Bar_Whose_Undo_Puts_It_Back() =>
        Run(new ShowcasePage(), (page, scene, clock) =>
        {
            Advance(clock, 100);
            var inbox = Assert.IsType<InboxScreen>(page.Inbox.GetActive()?.Element);
            var list = Assert.Single(inbox.GetVisualDescendants().OfType<AvonicList>());
            var count = list.Items.Count;
            var sliding = Assert.IsType<AvonicItemSliding>(list.Items[0]);

            Archive(sliding, clock, twice: true);
            var toast = Assert.Single(scene.Window.GetVisualDescendants().OfType<AvonicToast>(), t => t.IsVisible);
            Assert.Equal("Conversation archived", toast.Message);
            Assert.Equal(["Undo"], toast.Buttons!.Select(b => b.Text));
            Assert.Equal(count - 1, list.Items.Count);
            Assert.DoesNotContain(sliding, list.Items.Cast<object>());

            // Above the tab bar, anchored to it.
            Render();
            var bar = Assert.Single(page.GetVisualDescendants().OfType<AvonicTabBar>());
            var barTop = bar.TranslatePoint(default, scene.Window)!.Value.Y;
            var wrapper = toast.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "PART_Wrapper");
            var toastBottom = wrapper.TranslatePoint(new Point(0, wrapper.Bounds.Height), scene.Window)!.Value.Y;
            Assert.True(toastBottom <= barTop + 0.5, $"The toast ends at {toastBottom}, the tab bar starts at {barTop}");

            // Undo puts it back where it was.
            _ = toast.Dismiss(null, "cancel");
            Advance(clock, 600);
            Assert.Equal(count, list.Items.Count);
            Assert.Same(sliding, list.Items[0]);
            Assert.Equal(0, sliding.GetOpenAmount());

            // Archived again, once the first toast has gone: another toast. Left to its 3 s, it goes, and the message stays out.
            Archive(sliding, clock, twice: false);
            Assert.Single(scene.Window.GetVisualDescendants().OfType<AvonicToast>(), t => t.IsVisible && !ReferenceEquals(t, toast));
            Assert.Equal(count - 1, list.Items.Count);
            Advance(clock, 3600);
            Assert.DoesNotContain(scene.Window.GetVisualDescendants().OfType<AvonicToast>(), t => t.IsVisible);
            Assert.Equal(count - 1, list.Items.Count);
            Assert.DoesNotContain(sliding, list.Items.Cast<object>());
        });

    /// <summary>The first message, the one with an Archive option, swiped open and its Archive tapped.</summary>
    private static void Archive(AvonicItemSliding sliding, TestClock clock, bool twice)
    {
        Render();
        sliding.Open(ItemOptionsSide.Start);
        Advance(clock, 500);
        Render(); // A message put back by Undo has its options in the visual tree again only after a layout.
        var archive = Assert.Single(sliding.GetVisualDescendants().OfType<AvonicItemOption>(), o => o.Content as string == "Archive");
        archive.RaiseEvent(new RoutedEventArgs(AvonicItemOption.ClickEvent));
        if (twice)
        {
            archive.RaiseEvent(new RoutedEventArgs(AvonicItemOption.ClickEvent));
        }

        Advance(clock, 500);
    }

    [AvaloniaFact]
    public void A_Tapped_Message_Is_Pushed_With_A_Back_Button_That_Pops_It() =>
        Run(new AvonicNav { Root = typeof(InboxScreen) }, (nav, scene, clock) =>
        {
            Advance(clock, 100);
            var inbox = Assert.IsType<InboxScreen>(nav.GetActive()?.Element);
            var item = inbox.GetVisualDescendants().OfType<AvonicItem>()
                .First(i => i.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Marcus Webb"));
            Click(scene, clock, item);
            Advance(clock, 600);

            Assert.Equal(2, nav.GetLength());
            var message = Assert.IsType<MessageScreen>(nav.GetActive()?.Element);
            Assert.Equal("Marcus Webb", message.Sender);
            Assert.Equal("Lunch tomorrow", message.Subject);
            Assert.Equal("Yesterday", message.Time);
            Assert.Equal("MW", message.Initials);
            Assert.Equal("Success", message.AvatarColor);
            Assert.StartsWith("There's a new ramen place", message.Body);
            var avatar = Assert.Single(message.GetVisualDescendants().OfType<AvonicAvatar>());
            Assert.Same(message.FindResource("Avonic.Color.SuccessBrush"), avatar.Background);

            var back = Assert.Single(message.GetVisualDescendants().OfType<AvonicBackButton>());
            Assert.True(back.IsEffectivelyVisible, "The pushed message's back button is hidden");
            Click(scene, clock, back);
            Advance(clock, 600);

            Assert.Equal(1, nav.GetLength());
            Assert.Same(inbox, nav.GetActive()?.Element);
        });

    [AvaloniaFact]
    public void The_Inbox_Menu_Button_Opens_The_Side_Menu_And_An_Item_Closes_It() => Run(new ShowcasePage(), (page, scene, clock) =>
    {
        Advance(clock, 100);
        var button = Assert.Single(page.GetVisualDescendants().OfType<AvonicMenuButton>());
        Assert.True(button.IsEffectivelyVisible, "The inbox's menu button is hidden");
        Click(scene, clock, button);
        Advance(clock, 600);
        Assert.True(page.Menu.IsOpen());

        var sent = page.Menu.GetVisualDescendants().OfType<AvonicItem>()
            .Single(i => i.GetVisualDescendants().OfType<AvonicLabel>().Any(l => l.Content as string == "Sent"));
        Click(scene, clock, sent);
        Advance(clock, 600);
        Assert.False(page.Menu.IsOpen());
    });

    [AvaloniaFact]
    public void The_Birthday_Is_A_Datetime_Button_That_Opens_Its_Datetime() => Run(new ProfileScreen(), (screen, scene, clock) =>
    {
        Advance(clock, 100);
        var button = Assert.Single(screen.GetVisualDescendants().OfType<AvonicDatetimeButton>());
        Assert.Equal("Apr 12, 1990", button.DateText);
        Assert.Null(button.TimeText);

        var date = button.GetVisualDescendants().OfType<DatetimeButtonNative>().First(b => b.IsEffectivelyVisible);
        Click(scene, clock, date);
        Advance(clock, 600);

        var modal = Assert.Single(scene.Window.GetVisualDescendants().OfType<AvonicModal>());
        Assert.True(modal.IsVisible);
        Assert.Single(modal.GetVisualDescendants().OfType<AvonicDatetime>(), d => d.Name == "BirthdayDatetime");
    });

    /// <summary>A real click at the control's centre, after a frame, as headless hit tests read the last one.</summary>
    private static void Click(Scene scene, TestClock clock, Control control)
    {
        Render();
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Advance(clock, 50);
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Render()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

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
