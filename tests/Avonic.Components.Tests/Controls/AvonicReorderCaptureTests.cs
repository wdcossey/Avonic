using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Gestures.GestureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The look, with the theme, and real frames for the compare step: the same scene as the --html at the top of
/// docs/components/ion-reorder/COMPARISON.md. With <c>AVONIC_CAPTURE_DIR</c> set they're saved. The boxes are Chromium's, in
/// docs/components/ion-reorder/ref/measurements.json.
/// </summary>
public class AvonicReorderCaptureTests
{
    private sealed class Page : IDisposable
    {
        public TestClock Clock { get; } = new();

        public AvonicReorderGroup Group { get; } = new() { Disabled = false };

        public Scene Scene { get; }

        public Page()
        {
            for (var i = 1; i <= 5; i++)
            {
                Group.Items.Add(new AvonicItem
                {
                    Content = new AvonicLabel { Content = $"Item {i}" },
                    EndContent = new AvonicReorder(),
                });
            }

            Scene = new Scene(new AvonicContent { Content = new AvonicList { Items = { Group } } });
            Scene.Window.Background = Brushes.White;
            Scene.Window.FontSize = 16;
            Render();
        }

        public Rect Box(Visual visual) => new(visual.TranslatePoint(default, Scene.Window)!.Value, visual.Bounds.Size);

        public AvonicReorder Handle(int index) => Group.ItemsPanelRoot!.Children[index].GetVisualDescendants().OfType<AvonicReorder>().First();

        public void Dispose()
        {
            Scene.Dispose();
            Clock.Dispose();
        }
    }

    [AvaloniaFact]
    public void B2_The_Handle_Is_31_Px_At_0_3_After_The_Items_End_Margin() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        var handle = page.Handle(0);

        // Measured: the item at 8, 48 high; the handle 31 x 31 at (343, 16), 16 px from the end
        Assert.Equal(new Rect(0, 8, 390, 48), page.Box(page.Group.ItemsPanelRoot!.Children[0]));
        Assert.Equal(new Rect(343, 16, 31, 31), page.Box(handle));
        var icon = handle.GetVisualDescendants().OfType<AvonicIcon>().Single();
        Assert.Equal(0.3, icon.Opacity);
        Assert.Equal(Colors.Black, ((ISolidColorBrush)icon.GetValue(TextElement.ForegroundProperty)!).Color);
    });

    [AvaloniaFact]
    public void B14_The_Lifted_Item_Is_Translucent_With_A_Shadow_Around_It() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        var item = (AvonicItem)page.Group.ItemsPanelRoot!.Children[1];
        var native = item.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Native");
        Assert.Equal(default, native.BoxShadow);

        var at = page.Box(page.Handle(1)).Center;
        new Finger(page.Scene, page.Clock).Down(at.X, at.Y, 1000);
        Assert.Equal(0.8, item.Opacity);
        Assert.Equal(BoxShadows.Parse("0 0 15.6 0 #66000000"), native.BoxShadow);
        Assert.False(item.ClipToBounds);
        Assert.False(native.ClipToBounds);
    });

    [AvaloniaFact]
    public void B14_A_Handle_That_Wraps_Its_Item_Is_Lifted_The_Same_Way() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var handle = new AvonicReorder { Content = new AvonicItem { Content = new AvonicLabel { Content = "Wrapped" } } };
        var group = new AvonicReorderGroup { Disabled = false, Items = { handle } };
        using var scene = new Scene(new AvonicList { Items = { group } });
        Render();
        var host = handle.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Host");
        Assert.False(handle.ClipToBounds);
        Assert.Equal(default, host.BoxShadow);

        var at = handle.TranslatePoint(new Point(100, 20), scene.Window)!.Value;
        new Finger(scene, clock).Down(at.X, at.Y, 1000);
        Assert.Equal(0.8, handle.Opacity);
        Assert.Equal(BoxShadows.Parse("0 0 15.6 0 #66000000"), host.BoxShadow);
    });

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        page.Scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = page.Scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "avonic-ion-reorder-page.png");

        // The icon's bar: black at 0.3 on white
        Assert.NotEqual(Colors.White, Frames.PixelAt(frame, 2 * 358, 2 * 28));
    });

    [AvaloniaFact]
    public void Reference_Lifted() => InstalledTheme.Run(_ =>
    {
        using var page = new Page();
        page.Scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        // As measure.js: the second handle, 80 px down in 8 px steps, then still until the siblings settle
        var at = page.Box(page.Handle(1)).Center;
        var finger = new Finger(page.Scene, page.Clock).Down(at.X, at.Y, 1000);
        var time = 1000.0;
        for (var d = 8; d <= 80; d += 8)
        {
            time += 16;
            finger.Move(at.X, at.Y + d, time);
        }

        page.Clock.At(time + 500);
        Render();
        var frame = page.Scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "avonic-ion-reorder-lifted.png");

        // Measured: the shadow darkens the row above the lifted item, which is over item 4's place
        Assert.NotEqual(Colors.White, Frames.PixelAt(frame, 2 * 200, 2 * (56 + 80 - 3)));
    });
}
