using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicItemSlidingCaptureTests
{
    /// <summary>The same content as the snippet at the top of docs/components/ion-item-sliding/COMPARISON.md.</summary>
    internal static (AvonicList List, AvonicItemSliding A, AvonicItemSliding B) Page()
    {
        static AvonicItem Item(string text) => new() { Content = new AvonicLabel { Content = text } };

        var a = new AvonicItemSliding
        {
            Items =
            {
                new AvonicItemOptions { Side = ItemOptionsSide.Start, Items = { new AvonicItemOption { Color = "success", Content = "Archive" } } },
                Item("Options on both sides"),
                new AvonicItemOptions
                {
                    Items =
                    {
                        new AvonicItemOption { Content = "Favorite" },
                        new AvonicItemOption { Color = "danger", Expandable = true, Content = "Delete" },
                    },
                },
            },
        };
        var b = new AvonicItemSliding
        {
            Items =
            {
                Item("Icons"),
                new AvonicItemOptions
                {
                    Items =
                    {
                        new AvonicItemOption { Color = "tertiary", IconOnlyContent = new AvonicIcon { IconName = "pin" } },
                        new AvonicItemOption { Color = "warning", TopContent = new AvonicIcon { IconName = "star" }, Content = "Star" },
                        new AvonicItemOption { Color = "medium", IsEnabled = false, StartContent = new AvonicIcon { IconName = "trash" }, Content = "Off" },
                    },
                },
            },
        };
        var c = new AvonicItemSliding { Items = { Item("Plain item") } };
        return (new AvonicList { Items = { a, b, c } }, a, b);
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (list, a, b) = Page();
        using var scene = new Scene(new StackPanel { Children = { list } }, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-item-sliding-page.png");

        a.Open(ItemOptionsSide.End);
        clock.At(1000);
        Save(scene, "avonic-item-sliding-open-end.png");
        Assert.InRange(a.GetOpenAmount(), 149, 154);

        a.Open(ItemOptionsSide.Start);
        clock.At(2000);
        Save(scene, "avonic-item-sliding-open-start.png");

        b.Open();
        clock.At(3000);
        Save(scene, "avonic-item-sliding-open-b.png");
        Assert.True(b.GetOpenAmount() > 0);
    });

    private static void Save(Scene scene, string name)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Frames.Save(frame!, name);
    }
}
