using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Sample.Pages;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames for the compare step, beside the renders at the top of docs/components/ion-modal/COMPARISON.md. With
/// <c>AVONIC_CAPTURE_DIR</c> set they're saved. The base theme is there for the overlay layer.
/// </summary>
public class AvonicModalCaptureTests
{
    /// <summary>A header and a content with a paragraph, as an <c>ion-page</c> or a modal holds them.</summary>
    internal static Grid PageOf(string title, string text, Control? end = null) => new()
    {
        RowDefinitions = new RowDefinitions("Auto,*"),
        Children =
        {
            new AvonicHeader
            {
                Items =
                {
                    new AvonicToolbar
                    {
                        Content = new AvonicTitle { Content = title },
                        EndContent = end,
                    },
                },
            },
            new AvonicContent
            {
                [Grid.RowProperty] = 1,
                Padding = new Thickness(16),
                Content = new TextBlock { Text = text, Margin = new Thickness(0, 16), TextWrapping = TextWrapping.Wrap },
            },
        },
    };

    private static void Run(Action<Scene, TestClock> test)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                var page = PageOf("Page", "The page under the modal.");
                TextElement.SetFontSize(page, 16);
                using var scene = new Scene(page);
                scene.Window.Background = Brushes.White;
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();
                test(scene, clock);
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

        var frame = scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, name);
    }

    private static Rect InWindow(Visual visual, Scene scene) => new(visual.TranslatePoint(default, scene.Window)!.Value, visual.Bounds.Size);

    [AvaloniaFact]
    public void Reference_Full_Screen() => Run((scene, clock) =>
    {
        var modal = new AvonicModal
        {
            Content = PageOf("Modal", "A full-screen modal on a phone.", AvonicToolbarCaptureTests.Buttons(new AvonicButton { Content = "Close" })),
        };
        TextElement.SetFontSize(modal, 16);
        modal.Present(scene.Window);
        Advance(clock, 304);
        Save(scene, "avonic-modal-page.png");

        // M5: the wrapper covers the window.
        Assert.Equal(new Rect(0, 0, 390, 844), InWindow(modal.Wrapper!, scene));
    });

    [AvaloniaFact]
    public void Reference_Dialog() => Run((scene, clock) =>
    {
        var modal = new AvonicModal { Content = PageOf("Dialog", "A modal of its own size.") };
        modal.Resources["Avonic.Modal.Width"] = "320px";
        modal.Resources["Avonic.Modal.Height"] = "360px";
        modal.Resources["Avonic.Modal.BorderRadius"] = "8px";
        TextElement.SetFontSize(modal, 16);
        modal.Present(scene.Window);
        Advance(clock, 304);
        Save(scene, "avonic-modal-dialog.png");

        // Chromium: the wrapper at 35, 242, 320 by 360, the radius 8.
        Assert.Equal(new Rect(35, 242, 320, 360), InWindow(modal.Wrapper!, scene));
        Assert.Equal(new CornerRadius(8), modal.Wrapper!.CornerRadius);
    });

    [AvaloniaFact]
    public void Reference_Tablet() => Run((scene, clock) =>
    {
        scene.Window.Width = 1024;
        scene.Window.Height = 768;
        Dispatcher.UIThread.RunJobs();
        var modal = new AvonicModal { Content = PageOf("Modal", "Hello") };
        TextElement.SetFontSize(modal, 16);
        modal.Present(scene.Window);
        Advance(clock, 304);
        Save(scene, "avonic-modal-tablet.png");

        // M5: 600 by 600 at 212, 84, radius 2, md's shadow.
        Assert.Equal(new Rect(212, 84, 600, 600), InWindow(modal.Wrapper!, scene));
        Assert.Equal(new CornerRadius(2), modal.Wrapper!.CornerRadius);
        Assert.Equal(1, modal.Wrapper.BoxShadow.Count);
        Assert.Equal(28, modal.Wrapper.BoxShadow[0].OffsetY);
    });

    /// <summary>The sheets of ref/sheet/, which a script renders in Chromium, as render.js can't set an array prop.</summary>
    private static AvonicModal SheetOf(double initial, params double[] breakpoints) => new()
    {
        Content = PageOf("Sheet", "A sheet at its breakpoint."),
        Breakpoints = [.. breakpoints],
        InitialBreakpoint = initial,
    };

    private static void UnderTheSheet(Scene scene) =>
        ((TextBlock)((AvonicContent)((Grid)scene.Window.Content!).Children[1]).Content!).Text = "The page under the sheet.";

    [AvaloniaFact]
    public void Reference_Sheet() => Run((scene, clock) =>
    {
        UnderTheSheet(scene);
        var modal = SheetOf(0.5, 0, 0.25, 0.5, 0.75, 1);
        TextElement.SetFontSize(modal, 16);
        modal.Present(scene.Window);
        Advance(clock, 340);
        Save(scene, "avonic-modal-sheet.png");

        // Chromium: the wrapper at 0, 427, 390 by 834; the handle at 177, 432, 36 by 5; the backdrop 0.165.
        Assert.Equal(new Rect(0, 427, 390, 834), InWindow(modal.Wrapper!, scene));
        Assert.Equal(new Rect(177, 432, 36, 5), InWindow(modal.HandlePart!, scene));
        Assert.Equal(0.165, modal.Backdrop!.Opacity, 4);
    });

    [AvaloniaFact]
    public void Reference_Sheet_Tablet() => Run((scene, clock) =>
    {
        UnderTheSheet(scene);
        scene.Window.Width = 1024;
        scene.Window.Height = 768;
        Dispatcher.UIThread.RunJobs();
        var modal = SheetOf(0.5, 0, 0.25, 0.5, 0.75, 1);
        TextElement.SetFontSize(modal, 16);
        modal.Present(scene.Window);
        Advance(clock, 340);
        Save(scene, "avonic-modal-sheet-tablet.png");

        // Chromium: 212, 389, 600 by 758, the handle at 494, 394.
        Assert.Equal(new Rect(212, 389, 600, 758), InWindow(modal.Wrapper!, scene));
        Assert.Equal(new Rect(494, 394, 36, 5), InWindow(modal.HandlePart!, scene));
    });

    [AvaloniaFact]
    public void Reference_Sheet_Backdrop_Breakpoint() => Run((scene, clock) =>
    {
        UnderTheSheet(scene);
        var modal = SheetOf(0.25, 0.25, 0.5, 0.75);
        modal.BackdropBreakpoint = 0.5;
        TextElement.SetFontSize(modal, 16);
        modal.Present(scene.Window);
        Advance(clock, 340);
        Save(scene, "avonic-modal-sheet-backdrop-breakpoint.png");

        // Chromium: 0, 635.5, and no backdrop.
        Assert.Equal(new Rect(0, 635.5, 390, 834), InWindow(modal.Wrapper!, scene));
        Assert.Equal(0, modal.Backdrop!.Opacity, 6);
    });

    [AvaloniaFact]
    public void Sample_Page_Closes_Under_The_Nested_Modal_And_Opens_Its_Sheet_And_The_Handle_Cycles_It()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                var page = new ModalPage();
                using var scene = new Scene(page);
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();

                void Click(Visual target, Point at)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        Dispatcher.UIThread.RunJobs();
                        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                    }

                    var point = target.TranslatePoint(at, scene.Window)!.Value;
                    scene.Window.MouseDown(point, MouseButton.Left);
                    scene.Window.MouseUp(point, MouseButton.Left);
                    Dispatcher.UIThread.RunJobs();
                }

                // The nested modal covers its parent's Close, so its own button closes the parent: a click can reach it.
                var trigger = page.FindControl<AvonicModal>("TriggerModal")!;
                var nested = page.FindControl<AvonicModal>("NestedModal")!;
                Click(page.FindControl<AvonicButton>("OpenModal")!, new Point(10, 10));
                Advance(clock, 340);
                var openNested = trigger.GetVisualDescendants().OfType<AvonicButton>().First(b => Equals(b.Content, "Open a nested modal"));
                Click(openNested, new Point(10, 10));
                Advance(clock, 680);
                var closeUnder = nested.GetVisualDescendants().OfType<AvonicButton>().First(b => Equals(b.Content, "Close the modal under this one"));
                Click(closeUnder, new Point(10, 10));
                Advance(clock, 1000);
                Assert.Equal("TriggerModal dismissed, role cancel", page.FindControl<TextBlock>("Status")!.Text);
                Assert.Equal("NestedModal dismissed, role parent-dismissed", page.FindControl<TextBlock>("NestedStatus")!.Text);
                var start = clock.Now.TotalMilliseconds;

                var sheet = page.FindControl<AvonicModal>("SheetModal")!;
                var status = page.FindControl<TextBlock>("SheetStatus")!;
                Assert.Equal([0, 0.25, 0.5, 0.75, 1], sheet.Breakpoints!);
                var open = page.FindControl<AvonicButton>("OpenSheet")!;
                open.BringIntoView();
                Click(open, new Point(open.Bounds.Width / 2, open.Bounds.Height / 2));
                Advance(clock, start + 340);
                Assert.Equal("The sheet is at 0.5", status.Text);
                Save(scene, "avonic-modal-sample-sheet.png");

                Click(sheet.HandlePart!, new Point(18, 2.5));
                Advance(clock, start + 340 + 600);
                Assert.Equal("The sheet is at 0.75", status.Text);
                Assert.Equal(new Rect(0, 218.5, 390, 834), InWindow(sheet.Wrapper!, scene));
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    [AvaloniaFact]
    public void Sample_Page_Opens_Its_Modal_From_The_Trigger_And_Closes_It()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                var page = new ModalPage();
                using var scene = new Scene(page);
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();

                void Click(Visual target)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        Dispatcher.UIThread.RunJobs();
                        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                    }

                    var centre = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), scene.Window)!.Value;
                    scene.Window.MouseDown(centre, MouseButton.Left);
                    scene.Window.MouseUp(centre, MouseButton.Left);
                    Dispatcher.UIThread.RunJobs();
                }

                var modal = page.FindControl<AvonicModal>("TriggerModal")!;
                Click(page.FindControl<AvonicButton>("OpenModal")!);
                Advance(clock, 304);
                Assert.Equal(new Rect(0, 0, 390, 844), InWindow(modal.Wrapper!, scene));
                Save(scene, "avonic-modal-sample.png");

                var close = modal.GetVisualDescendants().OfType<AvonicButton>().First(b => Equals(b.Content, "Close"));
                Click(close);
                Advance(clock, 600);
                Assert.Same(page, modal.FindLogicalAncestorOfType<ModalPage>());
                Assert.Equal("TriggerModal dismissed, role cancel", page.FindControl<TextBlock>("Status")!.Text);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }
}
