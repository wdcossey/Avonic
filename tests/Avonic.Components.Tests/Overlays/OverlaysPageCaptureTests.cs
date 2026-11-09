using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;
using Avonic.Sample.Pages;

namespace Avonic.Components.Tests.Overlays;

/// <summary>
/// The sample's overlays page, with the theme and the base theme, driven by real headless clicks: its test overlay presents,
/// stacks and dismisses through the system. With <c>AVONIC_CAPTURE_DIR</c> set, the frame is saved for the PR.
/// </summary>
public class OverlaysPageCaptureTests
{
    [AvaloniaFact]
    public void The_Sample_Presents_Stacks_And_Dismisses_Its_Test_Overlay()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                var page = new OverlaysPage();
                using var scene = new Scene(page);
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();

                void Advance(double to)
                {
                    while (clock.Now.TotalMilliseconds < to)
                    {
                        clock.At(clock.Now.TotalMilliseconds + 1);
                        Dispatcher.UIThread.RunJobs();
                    }
                }

                void Click(Visual target, Point? at = null)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        Dispatcher.UIThread.RunJobs();
                        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                    }

                    var centre = target.TranslatePoint(at ?? new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), scene.Window)!.Value;
                    scene.Window.MouseDown(centre, MouseButton.Left);
                    scene.Window.MouseUp(centre, MouseButton.Left);
                    Dispatcher.UIThread.RunJobs();
                }

                AvonicOverlay[] Overlays() => scene.Window.GetVisualDescendants().OfType<AvonicOverlay>().ToArray();

                Click(page.FindControl<AvonicButton>("Present")!);
                Advance(200);
                var first = Assert.Single(Overlays());
                var backdrop = first.GetVisualDescendants().OfType<AvonicBackdrop>().Single();
                Assert.Equal(0.32, backdrop.Opacity, 6);
                Assert.Same(first, scene.Window.FocusManager!.GetFocusedElement());

                Click(first.GetVisualDescendants().OfType<AvonicButton>().First(b => Equals(b.Content, "Stack another")));
                Advance(400);
                Assert.Equal(2, Overlays().Length);

                var frame = scene.Window.CaptureRenderedFrame()!;
                Frames.Save(frame, "avonic-overlays-page.png");

                scene.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
                Advance(600);
                Assert.Same(first, Assert.Single(Overlays()));

                // Above the box, which is in the middle.
                Click(backdrop, new Point(20, 20));
                Advance(800);
                Assert.Empty(Overlays());
                Assert.Equal("Overlay 1 dismissed, role backdrop", page.FindControl<TextBlock>("Status")!.Text);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }
}
