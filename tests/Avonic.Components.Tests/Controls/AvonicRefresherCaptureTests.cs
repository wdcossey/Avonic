using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Gestures.GestureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames for the compare step, the scene of docs/components/ion-refresher/COMPARISON.md: a header, and a content with
/// the refresher and twenty items. With <c>AVONIC_CAPTURE_DIR</c> set they're saved.
/// </summary>
public class AvonicRefresherCaptureTests
{
    private sealed class Page : IDisposable
    {
        private Finger? _finger;
        private double _y = 200;

        public Page()
        {
            var list = new AvonicList();
            for (var i = 1; i <= 20; i++)
            {
                list.Items.Add(new AvonicItem { Content = new AvonicLabel { Content = $"Item {i}" } });
            }

            Refresher = new AvonicRefresher { Content = new AvonicRefresherContent() };
            Content = new AvonicContent { Content = list, FixedContent = Refresher };
            var header = new AvonicHeader { Items = { new AvonicToolbar { Content = new AvonicTitle { Content = "Refresher" } } } };
            var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
            grid.Children.Add(header);
            Grid.SetRow(Content, 1);
            grid.Children.Add(Content);
            Scene = new Scene(grid, 390, 844);
            Scene.Window.FontSize = 16;
            Scene.Window.Background = Brushes.White;
            Scene.Window.SetRenderScaling(2);
            Dispatcher.UIThread.RunJobs();
        }

        public TestClock Clock { get; } = new();
        public AvonicContent Content { get; }
        public AvonicRefresher Refresher { get; }
        public Scene Scene { get; }
        public double At { get; private set; } = 1000;

        public Page Drag(double dy)
        {
            _finger ??= new Finger(Scene, Clock).Down(195, _y, At);
            var target = _y + dy;
            while (_y < target)
            {
                _y = Math.Min(target, _y + 8);
                At += 16;
                _finger.Move(195, _y, At);
            }

            return this;
        }

        public Page Lift()
        {
            At += 16;
            _finger!.Up(195, _y, At);
            _finger = null;
            return this;
        }

        public Page Wait(double ms)
        {
            At += ms;
            Clock.At(At);
            Settle();
            return this;
        }

        public Rect Box(string part)
        {
            var visual = Refresher.GetVisualDescendants().OfType<Control>().First(c => c.Name == part);
            return AvonicRefresherTests.BoundsInWindow(visual, Scene.Window);
        }

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    private static void Save(Scene scene, string name)
    {
        var frame = scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, $"avonic-ion-refresher-{name}.png");
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();

        // Chromium's pull-0.5 frame: the icon 50.49 below its place, at (174, 115.49)
        page.Drag(8 + 180).Wait(16);
        Assert.Equal(115.49, page.Box("PART_PullingIcon").Y, 0);
        Assert.Equal(174, page.Box("PART_PullingIcon").X, 9);
        Render();
        Save(page.Scene, "page");

        // It draws below the refresher's 60 px, where Avalonia would clip a templated control: the primary circle shows
        var frame = page.Scene.Window.CaptureRenderedFrame()!;
        var spinner = page.Box("PART_PullingSpinner");
        var blue = 0;
        for (var y = (int)(spinner.Y * 2); y < (int)(spinner.Bottom * 2); y++)
        {
            for (var x = (int)(spinner.X * 2); x < (int)(spinner.Right * 2); x++)
            {
                var pixel = Frames.PixelAt(frame, x, y);
                if (pixel is { B: > 200, R: < 60 })
                {
                    blue++;
                }
            }
        }

        Assert.True(blue > 100, $"{blue} primary pixels");

        // Chromium's refreshing frame: the refreshing icon at (174, 65)
        page.Lift().Wait(200);
        Assert.Equal(new Rect(174, 65, 42, 42), page.Box("PART_RefreshingIcon"));
        Render();
        Save(page.Scene, "refreshing");
    });
}
