using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicRangeCaptureTests
{
    // The same content as the --html at the top of docs/components/ion-range/COMPARISON.md.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var list = new AvonicList
        {
            Items =
            {
                new AvonicItem { Content = new AvonicRange { Label = "Label", Value = 40 } },
                new AvonicItem { Content = new AvonicRange { Value = 0 } },
                new AvonicItem { Content = new AvonicRange { Label = "Pin", Pin = true, Value = 60 } },
                new AvonicItem { Content = new AvonicRange { Label = "Stacked", LabelPlacement = RangeLabelPlacement.Stacked, Value = 30 } },
                new AvonicItem { Content = new AvonicRange { Label = "A fixed label that wraps", LabelPlacement = RangeLabelPlacement.Fixed, Value = 50 } },
                new AvonicItem { Content = new AvonicRange { Label = "End", LabelPlacement = RangeLabelPlacement.End, Value = 70 } },
                new AvonicItem { Content = new AvonicRange { DualKnobs = true, Value = new RangeValue(20, 80) } },
                new AvonicItem
                {
                    Content = new AvonicRange
                    {
                        Snaps = true,
                        Step = 10,
                        Value = 50,
                        StartContent = new AvonicIcon { IconName = "sunny-outline" },
                        EndContent = new AvonicIcon { IconName = "sunny" },
                    },
                },
                new AvonicItem { Content = new AvonicRange { ActiveBarStart = 50, Value = 20, Color = "danger" } },
                new AvonicItem { Content = new AvonicRange { IsEnabled = false, Value = 40 } },
                new AvonicItem { Content = new AvonicRange { Value = 40, StartContent = "Low", EndContent = "High" } },
            },
        };
        var page = new StackPanel
        {
            Children =
            {
                list,
                new AvonicRange { Value = 25 },
                new AvonicItem { Content = new AvonicRange { Label = "RTL", Value = 25 }, FlowDirection = FlowDirection.RightToLeft },
            },
        };

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        foreach (var animatable in page.GetVisualDescendants().OfType<Animatable>())
        {
            animatable.Transitions = null;
        }

        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-range-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        // Measured in Ionic: rows of 48, then 71 with a pin, 88 stacked and 58 with the fixed label's three lines.
        Assert.Equal(48, list.Items.OfType<AvonicItem>().ElementAt(0).Bounds.Height);
        Assert.Equal(71, list.Items.OfType<AvonicItem>().ElementAt(2).Bounds.Height, 0.5);
        Assert.Equal(88, list.Items.OfType<AvonicItem>().ElementAt(3).Bounds.Height, 0.5);
    });

    // A keyboard-focused knob with a pin, as the second frame in COMPARISON.md: Tab to the first range.
    [AvaloniaFact]
    public void Focused_Pin() => InstalledTheme.Run(_ =>
    {
        var pin = new AvonicRange { Label = "Pin", Pin = true, Value = 60 };
        var page = new AvonicList
        {
            Items =
            {
                new AvonicItem { Content = pin },
                new AvonicItem { Content = new AvonicRange { Value = 30 } },
            },
        };

        using var scene = new Scene(page, 390, 80);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        foreach (var animatable in page.GetVisualDescendants().OfType<Animatable>())
        {
            animatable.Transitions = null;
        }

        pin.GetVisualDescendants().OfType<RangeKnobHandle>().First().Focus(Avalonia.Input.NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-range-pin.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        Assert.Contains(":ion-focused", pin.GetVisualDescendants().OfType<RangeKnobHandle>().First().Classes);
    });
}
