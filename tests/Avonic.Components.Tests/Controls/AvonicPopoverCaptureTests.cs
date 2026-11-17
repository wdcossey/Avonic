using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames for the compare step, beside Chromium's <c>ref/menu.png</c> and <c>ref/submenu.png</c> in
/// docs/components/ion-popover/COMPARISON.md: the sample's menu, below its button, and its submenu. With <c>AVONIC_CAPTURE_DIR</c>
/// set they're saved. The base theme is there for the overlay layer.
/// </summary>
public class AvonicPopoverCaptureTests
{
    private static AvonicItem Item(string text, bool detail = false, bool enabled = true) =>
        new() { Button = true, Detail = detail, Content = text, IsEnabled = enabled };

    private static void Run(Action<Scene, TestClock, AvonicButton, AvonicPopover, AvonicItem, AvonicPopover> test)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                var more = Item("More", detail: true);
                var sub = new AvonicPopover
                {
                    Trigger = "More", Side = PopoverSide.End, DismissOnSelect = true,
                    Content = new AvonicList { Items = { Item("Share"), Item("Print") } },
                };
                var menu = new AvonicPopover
                {
                    Trigger = "Open", DismissOnSelect = true,
                    Content = new StackPanel
                    {
                        Children = { new AvonicList { Items = { Item("Copy"), Item("Paste"), more, Item("Delete", enabled: false) } }, sub },
                    },
                };
                var open = new AvonicButton { Content = "Open the menu", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
                var page = new AvonicContent { Content = new StackPanel { Margin = new Thickness(16, 16, 16, 0), Children = { open, menu } } };
                var names = new NameScope();
                names.Register("Open", open);
                names.Register("More", more);
                NameScope.SetNameScope(page, names);
                TextElement.SetFontSize(page, 16);
                using var scene = new Scene(page);
                scene.Window.Background = Brushes.White;
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();
                test(scene, clock, open, menu, more, sub);
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

    private static void Render()
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    private static void Click(Scene scene, Visual visual)
    {
        Render();
        var centre = new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(scene.Window)!.Value).Center;
        scene.Window.MouseDown(centre, MouseButton.Left);
        scene.Window.MouseUp(centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The mouse off to the backdrop's corner: Chromium's phone has no hover to shade an item with.</summary>
    private static void Away(Scene scene)
    {
        scene.Window.MouseMove(new Point(380, 830));
        Render();
    }

    private static Rect InWindow(Visual visual, Scene scene) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(scene.Window)!.Value);

    [AvaloniaFact]
    public void Reference_Menu_And_Submenu() => Run((scene, clock, open, menu, more, sub) =>
    {
        Click(scene, open);
        Advance(clock, AvonicPopoverTests.Entered);
        Away(scene);
        Frames.Save(scene.Window.CaptureRenderedFrame()!, "avonic-popover-page.png");

        // Chromium: the menu at the button's left, below it, 250 by 208 (four items of 48 and the list's 8 above and below).
        var button = InWindow(open, scene);
        var box = InWindow(menu.ContentPart!, scene);
        Assert.Equal(new Rect(button.X, button.Bottom, 250, 208), box);

        // Presented in code, as a click's ripple fades on a real-time transition in headless tests (the click is B13's).
        sub.Present(new PopoverEvent(more));
        Advance(clock, AvonicPopoverTests.Entered);
        Away(scene);
        Frames.Save(scene.Window.CaptureRenderedFrame()!, "avonic-popover-submenu.png");

        // Chromium: beside More, clamped to 12 from the right, then 5 back over it; level with More.
        Assert.Equal(new Rect(123, InWindow(more, scene).Y, 250, 112), InWindow(sub.ContentPart!, scene));
    });
}
