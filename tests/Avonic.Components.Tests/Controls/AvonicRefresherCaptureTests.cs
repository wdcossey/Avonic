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
        var blue = Primary(frame, page.Box("PART_PullingSpinner"));
        Assert.True(blue > 100, $"{blue} primary pixels");

        // Chromium's refreshing frame: the refreshing icon at (174, 65)
        page.Lift().Wait(200);
        Assert.Equal(new Rect(174, 65, 42, 42), page.Box("PART_RefreshingIcon"));
        Render();
        Save(page.Scene, "refreshing");
    });

    /// <summary>The primary-coloured pixels, the spinners' and the arrow's, in <paramref name="box"/> of a frame at a scale of 2, within the frame.</summary>
    private static int Primary(Avalonia.Media.Imaging.WriteableBitmap frame, Rect box)
    {
        var count = 0;
        var bottom = Math.Min(frame.PixelSize.Height, (int)(box.Bottom * 2));
        var right = Math.Min(frame.PixelSize.Width, (int)(box.Right * 2));
        for (var y = Math.Max(0, (int)(box.Y * 2)); y < bottom; y++)
        {
            for (var x = Math.Max(0, (int)(box.X * 2)); x < right; x++)
            {
                if (Frames.PixelAt(frame, x, y) is { B: > 200, G: < 120, R: < 60 })
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// #167: after <c>cancel()</c> during a refresh, Chromium shows both icons for the cancel's 700 ms, the pulling one 390 wide
    /// with its spinner at the start (docs/components/ion-refresher/ref/cancel-*.png); Avonic shows only the refreshing one, a
    /// deviation the user chose. With no pull, only the refreshing one; and none once it's over.
    /// </summary>
    [AvaloniaFact]
    public void Cancel_Frames() => InstalledTheme.Run(_ =>
    {
        // Below the header: the refresher and the overflow of its column. Its text can have a few fringe pixels of the primary
        // colour, which depend on the platform's text rendering, so it's compared with the page before anything happened.
        var strip = new Rect(0, 56, 390, 100);
        using (var page = new Page())
        {
            Render();
            var plain = page.Scene.Window.CaptureRenderedFrame()!;
            var before = Primary(plain, strip);
            page.Drag(8 + 180).Wait(16).Lift().Wait(400);
            page.Refresher.Cancel();
            page.Wait(100);
            Render();
            Save(page.Scene, "cancelling");
            var frame = page.Scene.Window.CaptureRenderedFrame()!;
            var refreshing = page.Box("PART_RefreshingSpinner");
            Assert.True(Primary(frame, refreshing) > 50, "the refreshing spinner shows");
            // Only the refreshing icon: outside it the strip is as it was before the pull, with no pulling spinner at the start
            var icon = page.Box("PART_RefreshingIcon");
            Assert.Equal(before - Primary(plain, icon), Primary(frame, strip) - Primary(frame, icon));

            page.Wait(700);
            Render();
            Save(page.Scene, "cancelled");
            Assert.Equal(before, Primary(page.Scene.Window.CaptureRenderedFrame()!, strip));
        }

        using (var page = new Page())
        {
            page.Wait(300);
            Render();
            var plain = page.Scene.Window.CaptureRenderedFrame()!;
            var before = Primary(plain, strip);
            page.Refresher.Cancel();
            page.Wait(100);
            Render();
            Save(page.Scene, "cancel-inactive");
            var frame = page.Scene.Window.CaptureRenderedFrame()!;
            var refreshing = page.Box("PART_RefreshingSpinner");
            Assert.True(Primary(frame, refreshing) > 50, "the refreshing spinner shows");
            // Only the refreshing icon: outside it the strip is as it was
            var icon = page.Box("PART_RefreshingIcon");
            Assert.Equal(before - Primary(plain, icon), Primary(frame, strip) - Primary(frame, icon));

            page.Wait(700);
            Render();
            Assert.Equal(before, Primary(page.Scene.Window.CaptureRenderedFrame()!, strip));
        }
    });
}
