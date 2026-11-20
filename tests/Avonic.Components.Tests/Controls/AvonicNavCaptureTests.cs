using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames for the compare step, beside the Chromium frames in docs/components/ion-nav/ref/ (<c>render.js</c> and
/// <c>frames.js</c>, named in COMPARISON.md). With <c>AVONIC_CAPTURE_DIR</c> set they're saved. The pages are the reference's: Page
/// One with a paragraph, three items and a link that pushes Page Two, whose toolbar is primary. The transition's frames are at the
/// local times Chromium's were paused at: the push plays from the frame at 32, the pop from 16.
/// </summary>
public class AvonicNavCaptureTests
{
    private static DockPanel PageOf(string title, string? color, string text, string[] items, Control link)
    {
        var header = new AvonicHeader { Items = { new AvonicToolbar { Color = color, Content = new AvonicTitle { Content = title } } } };
        DockPanel.SetDock(header, Dock.Top);
        var list = new AvonicList();
        foreach (var item in items)
        {
            list.Items.Add(new AvonicItem { Content = new AvonicLabel { Content = item } });
        }

        return new DockPanel
        {
            Name = title,
            Children =
            {
                header,
                new AvonicContent
                {
                    Padding = new Thickness(16),
                    Content = new StackPanel { Children = { new TextBlock { Text = text, Margin = new Thickness(0, 16) }, list, link } },
                },
            },
        };
    }

    internal static DockPanel PageOne() => PageOf(
        "Page One", null, "The root page of the nav.", ["Inbox", "Starred", "Sent"],
        new AvonicNavLink { HorizontalAlignment = HorizontalAlignment.Left, Content = new AvonicButton { Content = "Go to Page Two" } });

    internal static DockPanel PageTwo() => PageOf(
        "Page Two", "primary", "Pushed on top of Page One.", ["Details", "Settings"],
        new AvonicNavLink
        {
            RouterDirection = NavLinkRouterDirection.Back,
            HorizontalAlignment = HorizontalAlignment.Left,
            Content = new AvonicButton { Content = "Back to Page One", Fill = ButtonFill.Outline },
        });

    private static void Run(Action<Scene, AvonicNav, TestClock> test)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                var nav = new AvonicNav { Root = PageOne() };
                TextElement.SetFontSize(nav, 16);
                using var scene = new Scene(nav);
                scene.Window.Background = Brushes.White;
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();
                Advance(clock, 100);
                test(scene, nav, clock);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    private static void Advance(TestClock clock, double to)
    {
        while (clock.Now.TotalMilliseconds < to)
        {
            clock.At(clock.Now.TotalMilliseconds + 1);
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void Save(Scene scene, string name)
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        Frames.Save(scene.Window.CaptureRenderedFrame()!, name);
    }

    [AvaloniaFact]
    public void Reference_Page() => Run((scene, nav, _) =>
    {
        Assert.Equal(1, nav.GetLength());
        Save(scene, "avonic-nav-page.png");
    });

    [AvaloniaFact]
    public void Push_Frames() => Run((scene, nav, clock) =>
    {
        foreach (var t in new[] { 0, 64, 144, 208, 280 })
        {
            _ = nav.SetRoot(PageOne());
            Advance(clock, clock.Now.TotalMilliseconds + 100);
            var start = clock.Now.TotalMilliseconds;
            var push = nav.Push(PageTwo());
            Advance(clock, start + 32 + t);
            Save(scene, $"avonic-nav-push-{t:000}ms.png");
            Advance(clock, start + 600);
            Assert.True(AvonicNavTests.Ended(push));
        }

        Save(scene, "avonic-nav-pushed.png");
    });

    [AvaloniaFact]
    public void Pop_Frames() => Run((scene, nav, clock) =>
    {
        foreach (var t in new[] { 0, 48, 96, 144, 192 })
        {
            _ = nav.SetRoot(PageOne());
            _ = nav.Push(PageTwo(), opts: new NavOptions { Animated = false });
            Advance(clock, clock.Now.TotalMilliseconds + 100);
            var start = clock.Now.TotalMilliseconds;
            var pop = nav.Pop();
            Advance(clock, start + 16 + t);
            Save(scene, $"avonic-nav-pop-{t:000}ms.png");
            Advance(clock, start + 600);
            Assert.True(AvonicNavTests.Ended(pop));
        }
    });
}
