using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames for the compare step, beside the Chromium frames in docs/components/ion-split-pane/ref/ (S3, S4, S11 and S17 of
/// <c>measure.js</c>, named in COMPARISON.md). With <c>AVONIC_CAPTURE_DIR</c> set they're saved. The page is the reference's: a menu
/// with a primary toolbar and a padded content, then the main page with the menu button in its toolbar's start buttons.
/// </summary>
public class AvonicSplitPaneCaptureTests
{
    private static DockPanel PageOf(string title, string text, string? toolbarColor = null, Control? start = null)
    {
        var header = new AvonicHeader
        {
            Items =
            {
                new AvonicToolbar
                {
                    Color = toolbarColor,
                    StartContent = start is null ? null : new AvonicButtons { Items = { start } },
                    Content = new AvonicTitle { Content = title },
                },
            },
        };
        DockPanel.SetDock(header, Dock.Top);
        return new DockPanel
        {
            Background = Brushes.White,
            Children = { header, new AvonicContent { Padding = new Thickness(16), Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap } } },
        };
    }

    private sealed record Page(Scene Scene, AvonicSplitPane Pane, AvonicMenu Menu, DockPanel Main, AvonicMenuButton Button);

    private static void Run(double width, double height, Action<Page> set, Action<Page> test)
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
                var main = PageOf("Main", "Main content", start: button);
                main.Name = "Main";
                var menu = new AvonicMenu { ContentId = "Main", Content = PageOf("Menu", "Menu content", "primary") };
                var pane = new AvonicSplitPane { ContentId = "Main", When = "xs", Items = { menu, main } };
                var root = new Panel { Children = { pane } };
                var scope = new NameScope();
                NameScope.SetNameScope(root, scope);
                scope.Register("Main", main);
                TextElement.SetFontSize(root, 16);
                var page = new Page(null!, pane, menu, main, button);
                set(page);
                using var scene = new Scene(root, width, height);
                scene.Window.Background = Brushes.White;
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();
                test(page with { Scene = scene });
            });
        }
        finally
        {
            styles.Remove(baseTheme);
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

    [AvaloniaFact]
    public void Reference_Page() => Run(390, 844, _ => { }, page =>
    {
        Save(page.Scene, "avonic-ion-split-pane-page.png");

        // S3: the menu 270 with its border, the main 120 from 270, the menu button hidden.
        Assert.Equal(new Rect(0, 0, 269, 844), InWindow(page.Menu, page.Scene));
        Assert.Equal(new Rect(270, 0, 120, 844), InWindow(page.Main, page.Scene));
        Assert.False(page.Button.IsVisible);
    });

    [AvaloniaFact]
    public void Reference_End() => Run(390, 844, page =>
    {
        page.Menu.Side = MenuSide.End;
        page.Pane.Items.Remove(page.Menu);
        page.Pane.Items.Add(page.Menu);
    }, page =>
    {
        Save(page.Scene, "avonic-ion-split-pane-end.png");

        // S4: the main 0 to 120, the menu from 121.
        Assert.Equal(new Rect(0, 0, 120, 844), InWindow(page.Main, page.Scene));
        Assert.Equal(121, InWindow(page.Menu, page.Scene).X);
    });

    [AvaloniaFact]
    public void Reference_Rtl() => Run(390, 844, page => page.Pane.FlowDirection = FlowDirection.RightToLeft, page =>
    {
        Save(page.Scene, "avonic-ion-split-pane-rtl.png");

        // S11: the menu at the right, from 121; the main 0 to 120.
        Assert.Equal(121, InWindow(page.Menu, page.Scene).X);
        Assert.Equal(new Rect(0, 0, 120, 844), InWindow(page.Main, page.Scene));
    });

    [AvaloniaFact]
    public void Reference_Wide() => Run(1024, 768, page => page.Pane.When = SplitPaneWhen.Default, page =>
    {
        Save(page.Scene, "avonic-ion-split-pane-wide.png");

        // S2: 28% of 1024, the main from 286.72, here 287 (whole pixels at 2x: 286.5).
        Assert.InRange(InWindow(page.Main, page.Scene).X, 286.5, 287);
    });

    [AvaloniaFact]
    public void Reference_Hidden() => Run(390, 844, page => page.Pane.When = SplitPaneWhen.Default, page =>
    {
        Save(page.Scene, "avonic-ion-split-pane-hidden.png");

        // S1: hidden at 390; the main fills it and the menu button shows.
        Assert.Equal(new Rect(0, 0, 390, 844), InWindow(page.Main, page.Scene));
        Assert.True(page.Button.IsVisible);
    });
}
