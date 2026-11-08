using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicButtonCaptureTests
{
    // The same content as the command at the top of docs/components/ion-button/COMPARISON.md.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        static AvonicIcon Icon(string name) => new() { IconName = name };
        static StackPanel Row(params Control[] buttons)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.AddRange(buttons);
            return row;
        }

        var long_ = "A long piece of text that has to wrap because it does not fit on one line of a phone";
        var page = new StackPanel
        {
            Children =
            {
                Row(new AvonicButton { Content = "Default" }, new AvonicButton { Content = "Outline", Fill = ButtonFill.Outline }, new AvonicButton { Content = "Clear", Fill = ButtonFill.Clear }),
                Row(new AvonicButton { Content = "Danger", Color = "danger" }, new AvonicButton { Content = "Outline", Color = "danger", Fill = ButtonFill.Outline }, new AvonicButton { Content = "Clear", Color = "danger", Fill = ButtonFill.Clear }),
                Row(new AvonicButton { Content = "Unknown", Color = "nope" }, new AvonicButton { Content = "Fill default", Fill = ButtonFill.Default }),
                Row(new AvonicButton { Content = "Round", Shape = ButtonShape.Round }, new AvonicButton { Content = "Outline", Shape = ButtonShape.Round, Fill = ButtonFill.Outline }),
                Row(new AvonicButton { Content = "Small", Size = ButtonSize.Small }, new AvonicButton { Content = "Default", Size = ButtonSize.Default }, new AvonicButton { Content = "Large", Size = ButtonSize.Large }),
                new AvonicButton { Content = "Block", Expand = ButtonExpand.Block },
                new AvonicButton { Content = "Full", Expand = ButtonExpand.Full },
                new AvonicButton { Content = "Full outline", Expand = ButtonExpand.Full, Fill = ButtonFill.Outline },
                new AvonicButton { Content = "Block icon", Expand = ButtonExpand.Block, StartContent = Icon("heart") },
                Row(new AvonicButton { Content = "Strong", Strong = true }, new AvonicButton { Content = "Disabled", IsEnabled = false }),
                Row(new AvonicButton { Content = "Start", StartContent = Icon("heart") }, new AvonicButton { Content = "End", EndContent = Icon("star") }, new AvonicButton { Content = "Both", StartContent = Icon("heart"), EndContent = Icon("star") }),
                Row(new AvonicButton { IconOnlyContent = Icon("heart") }, new AvonicButton { IconOnlyContent = Icon("heart"), Size = ButtonSize.Small }, new AvonicButton { IconOnlyContent = Icon("heart"), Size = ButtonSize.Large }, new AvonicButton { IconOnlyContent = Icon("heart"), Shape = ButtonShape.Round }),
                new AvonicButton { Content = long_ },
                new AvonicItem { Content = new AvonicButton { Content = "In item" } },
                new AvonicItem
                {
                    StartContent = new AvonicButton { Content = "Start" },
                    Content = new AvonicLabel { Content = "Label" },
                    EndContent = new AvonicButton { Content = "End" },
                },
                new AvonicItem { EndContent = new AvonicButton { Content = "Default", Size = ButtonSize.Default } },
                new AvonicItem { EndContent = new AvonicButton { IconOnlyContent = Icon("heart") } },
                new AvonicButton { Content = "RTL", StartContent = Icon("heart"), EndContent = Icon("star"), FlowDirection = FlowDirection.RightToLeft },
            },
        };
        page.Resources["Avonic.Button.Transition"] = new Transitions();

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-button-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        Assert.True(page.Bounds.Height > 0);
    });

    // The rows of the Chromium page measured for a button in a divider: an item with a start and an end, a divider with both,
    // one in a divider's default slot, and a large one at a divider's end.
    [AvaloniaFact]
    public void Capture_In_A_Divider() => InstalledTheme.Run(_ =>
    {
        var page = new StackPanel
        {
            Children =
            {
                new AvonicItem { StartContent = new AvonicButton { Content = "Text" }, Content = new AvonicLabel { Content = "x" }, EndContent = new AvonicButton { Content = "Text" } },
                new AvonicItemDivider { StartContent = new AvonicButton { Content = "Text" }, Content = new AvonicLabel { Content = "x" }, EndContent = new AvonicButton { Content = "Text" } },
                new AvonicItemDivider { Content = new AvonicButton { Content = "Text" } },
                new AvonicItemDivider { EndContent = new AvonicButton { Content = "Text", Size = ButtonSize.Large } },
            },
        };
        page.Resources["Avonic.Button.Transition"] = new Transitions();

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-button-divider.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        Assert.True(page.Bounds.Height > 0);
    });

    // The same scene and states as Ionic's, for docs/components/ion-button/compare/states.png.
    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid, 0)]
    [InlineData(ButtonFill.Outline, 1)]
    [InlineData(ButtonFill.Clear, 2)]
    public void State_Frames(ButtonFill fill, int index) => InstalledTheme.Run(_ =>
    {
        var buttons = new[]
        {
            new AvonicButton { Content = "Solid", Expand = ButtonExpand.Block },
            new AvonicButton { Content = "Outline", Expand = ButtonExpand.Block, Fill = ButtonFill.Outline },
            new AvonicButton { Content = "Clear", Expand = ButtonExpand.Block, Fill = ButtonFill.Clear },
        };
        var page = new StackPanel { Width = 300, Margin = new Thickness(10), HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.AddRange(buttons);
        page.Resources["Avonic.Button.Transition"] = new Transitions();

        using var scene = new Scene(page, 320, 150);
        using var clock = new TestClock();
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        var dir = Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR");
        var button = buttons[index];
        var name = fill.ToString().ToLowerInvariant();

        void Save(string state)
        {
            button.GetVisualDescendants().OfType<AvonicRippleEffect>().Single().Tick();
            Dispatcher.UIThread.RunJobs();
            var frame = scene.Window.CaptureRenderedFrame()!;
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
                frame.Save(Path.Combine(dir, $"avonic-button-{state}-{name}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
            }
        }

        var box = button.TranslatePoint(new Point(60, button.Bounds.Height / 2), scene.Window)!.Value;

        scene.Window.MouseMove(box, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Save("hover");

        // Press, and 300 ms later, as Ionic's frame is.
        scene.Window.MouseDown(box, MouseButton.Left, RawInputModifiers.None);
        clock.At(300);
        Save("press");
        Assert.Single(button.GetVisualDescendants().OfType<AvonicRippleEffect>().Single().Ripples);

        scene.Window.MouseUp(box, MouseButton.Left, RawInputModifiers.None);
        clock.At(1200);
        scene.Window.MouseMove(new Point(310, 145), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        // Tab from the button before it, so it arrives by keyboard. Focus sits on the one just pressed.
        scene.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Shift);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.Shift);
        scene.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.True(button.IsFocused);
        Save("focus");
    });
}
