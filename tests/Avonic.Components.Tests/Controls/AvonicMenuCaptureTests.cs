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
using Avonic.Sample.Pages;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames for the compare step, beside the Chromium frames in docs/components/ion-menu/ref/ (<c>frames.js</c>, named in
/// COMPARISON.md). With <c>AVONIC_CAPTURE_DIR</c> set they're saved. The page is the reference's: a menu with a header and a padded
/// content holding a paragraph and a button, then the main page with the menu button in its toolbar's start buttons.
/// </summary>
public class AvonicMenuCaptureTests
{
    /// <summary>A header with <paramref name="title"/> and a padded content with a paragraph and a button, as an <c>ion-page</c>.</summary>
    private static DockPanel PageOf(string title, string text, string button, Control? start = null)
    {
        var header = new AvonicHeader
        {
            Items =
            {
                new AvonicToolbar
                {
                    StartContent = start is null ? null : new AvonicButtons { Items = { start } },
                    Content = new AvonicTitle { Content = title },
                },
            },
        };
        DockPanel.SetDock(header, Dock.Top);
        return new DockPanel
        {
            Background = Brushes.White,
            Children =
            {
                header,
                new AvonicContent
                {
                    Padding = new Thickness(16),
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = text, Margin = new Thickness(0, 16) },
                            new AvonicButton { Content = button, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left },
                        },
                    },
                },
            },
        };
    }

    private sealed record Page(Scene Scene, AvonicMenu Menu, DockPanel Main, AvonicMenuButton Button);

    private static void Run(Action<AvonicMenu>? set, Action<Page, TestClock> test)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                var button = new AvonicMenuButton();
                var main = PageOf("Page", "Main content", "Main", button);
                var menu = new AvonicMenu { ContentId = "Main", Content = PageOf("Menu", "Menu content", "In menu") };
                set?.Invoke(menu);
                var root = new Grid { Children = { menu, main } };
                var scope = new NameScope();
                NameScope.SetNameScope(root, scope);
                scope.Register("Main", main);
                TextElement.SetFontSize(root, 16);
                using var scene = new Scene(root);
                scene.Window.Background = Brushes.White;
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();
                test(new Page(scene, menu, main, button), clock);
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

    private static Rect InWindow(Visual visual, Scene scene) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(scene.Window)!.Value);

    private static Border Container(AvonicMenu menu) => menu.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Container" && b.TemplatedParent == menu);

    private static void OpenByClick(Page page, TestClock clock)
    {
        var at = InWindow(page.Button, page.Scene).Center;
        page.Scene.Window.MouseDown(at, MouseButton.Left);
        page.Scene.Window.MouseUp(at, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Advance(clock, 400);
    }

    [AvaloniaFact]
    public void Reference_Closed() => Run(null, (page, _) =>
    {
        Save(page.Scene, "avonic-ion-menu-page.png");

        // M1: the button 48 square at 4, 4; the menu hidden.
        Assert.Equal(new Rect(4, 4, 48, 48), InWindow(page.Button, page.Scene));
        Assert.False(page.Menu.IsVisible);
    });

    [AvaloniaFact]
    public void Reference_Overlay() => Run(null, (page, clock) =>
    {
        OpenByClick(page, clock);
        Save(page.Scene, "avonic-ion-menu-overlay.png");

        // M2: the container at 0, 0, 304 by 844; the backdrop at 0.25.
        Assert.Equal(new Rect(0, 0, 304, 844), InWindow(Container(page.Menu), page.Scene));
        Assert.Equal(0.25, page.Menu.GetVisualDescendants().OfType<AvonicBackdrop>().Single().Opacity, 6);
    });

    [AvaloniaFact]
    public void Reference_Push() => Run(m => m.Type = MenuType.Push, (page, clock) =>
    {
        OpenByClick(page, clock);
        Save(page.Scene, "avonic-ion-menu-push.png");

        // M3: the content at 304.
        Assert.Equal(new Rect(304, 0, 390, 844), InWindow(page.Main, page.Scene));
    });

    [AvaloniaFact]
    public void Reference_Reveal() => Run(m => m.Type = MenuType.Reveal, (page, clock) =>
    {
        OpenByClick(page, clock);
        Save(page.Scene, "avonic-ion-menu-reveal.png");

        // M4: the content at 304, the container at 0.
        Assert.Equal(new Rect(304, 0, 390, 844), InWindow(page.Main, page.Scene));
        Assert.Equal(new Rect(0, 0, 304, 844), InWindow(Container(page.Menu), page.Scene));
    });

    [AvaloniaFact]
    public void Reference_End() => Run(m => m.Side = MenuSide.End, (page, clock) =>
    {
        _ = page.Menu.Open();
        Advance(clock, 400);
        Save(page.Scene, "avonic-ion-menu-end.png");

        // M5: the container at 86.
        Assert.Equal(new Rect(86, 0, 304, 844), InWindow(Container(page.Menu), page.Scene));
    });

    [AvaloniaFact]
    public void Sample_Page_Opens_Its_Start_Menu_From_The_Button_And_A_Toggle_In_It_Closes_It()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                var page = new MenuPage();
                using var scene = new Scene(page);
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();
                Save(scene, "avonic-ion-menu-sample.png");

                void Click(Visual target)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        Dispatcher.UIThread.RunJobs();
                        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                    }

                    var point = InWindow(target, scene).Center;
                    scene.Window.MouseDown(point, MouseButton.Left);
                    scene.Window.MouseUp(point, MouseButton.Left);
                    Dispatcher.UIThread.RunJobs();
                }

                var start = page.FindControl<AvonicMenu>("StartMenu")!;
                Click(page.FindControl<AvonicMenuButton>("StartMenuButton")!);
                Advance(clock, 400);
                Assert.True(start.IsOpen());
                Assert.Equal("StartMenu open", page.FindControl<TextBlock>("Status")!.Text);
                Save(scene, "avonic-ion-menu-sample-open.png");

                Click(page.FindControl<AvonicItem>("StartOutbox")!);
                Advance(clock, 800);
                Assert.False(start.IsOpen());
                Assert.Equal("StartMenu closed, role none", page.FindControl<TextBlock>("Status")!.Text);

                // The end menu, from its own button.
                Click(page.FindControl<AvonicMenuButton>("EndMenuButton")!);
                Advance(clock, 1200);
                Assert.True(page.FindControl<AvonicMenu>("EndMenu")!.IsOpen());
                Save(scene, "avonic-ion-menu-sample-end.png");
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }
}
