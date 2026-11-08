using Avalonia;
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

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicChipCaptureTests
{
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var (page, _) = Page();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-chip-page.png");

        Assert.True(page.Bounds.Height > 0);
    });

    // render.js --tap: the first chip, pressed with a mouse at its centre, 160 ms in. The mouse hovers, so the background is
    // hover's, with the ripple over it.
    [AvaloniaFact]
    public void Press_Frame() => InstalledTheme.Run(_ =>
    {
        var (page, first) = Page();
        using var scene = new Scene(page, 390, 844);
        using var clock = new TestClock();
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        var centre = first.TranslatePoint(new Point(first.Bounds.Width / 2, first.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseMove(centre, RawInputModifiers.None);
        scene.Window.MouseDown(centre, MouseButton.Left, RawInputModifiers.None);
        clock.At(160);
        var ripple = first.GetVisualDescendants().OfType<AvonicRippleEffect>().First();
        ripple.Tick();
        Save(scene, "avonic-chip-press-160ms.png");
        scene.Window.MouseUp(centre, MouseButton.Left, RawInputModifiers.None);

        Assert.Single(ripple.Ripples);
    });

    // The same content as the --html under "In a slot" in docs/components/ion-chip/COMPARISON.md: a chip, an avatar and a
    // thumbnail in an item's start and end slots, chips in a divider's, and a chip beside a stacked label.
    [AvaloniaFact]
    public void Slots_Page() => InstalledTheme.Run(_ =>
    {
        static AvonicChip Chip(string text) => new() { Items = { text } };
        static AvonicLabel Label(string text) => new() { Content = text };

        var chipStart = Chip("Start");
        var chipEnd = Chip("End");
        var page = new StackPanel
        {
            Children =
            {
                new AvonicItem { StartContent = chipStart, Content = Label("Chip at the start") },
                new AvonicItem { Content = Label("Chip at the end"), EndContent = chipEnd },
                new AvonicItem { StartContent = new AvonicAvatar { Content = AvonicAvatarTests.Picture() }, Content = Label("Avatar at the start") },
                new AvonicItem { Content = Label("Avatar at the end"), EndContent = new AvonicAvatar { Content = AvonicAvatarTests.Picture() } },
                new AvonicItem { StartContent = new AvonicThumbnail { Content = AvonicAvatarTests.Picture() }, Content = Label("Thumbnail at the start") },
                new AvonicItem { Content = Label("Thumbnail at the end"), EndContent = new AvonicThumbnail { Content = AvonicAvatarTests.Picture() } },
                new AvonicItemDivider { StartContent = Chip("Start"), Content = Label("A divider"), EndContent = Chip("End") },
                new AvonicItem { Content = new AvonicLabel { Content = "Stacked", Position = LabelPosition.Stacked }, EndContent = Chip("End") },
            },
        };
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-chip-slots.png");

        // Measured: 4px 16px 4px 4px at the start, at x = 20, and 4px 4px 4px 16px at the end, whose right edge is at 370.
        Assert.Equal(new Thickness(4, 4, 16, 4), chipStart.Margin);
        Assert.Equal(new Thickness(16, 4, 4, 4), chipEnd.Margin);
        Assert.Equal(20, chipStart.TranslatePoint(default, page)!.Value.X);
        Assert.Equal(370, chipEnd.TranslatePoint(new Point(chipEnd.Bounds.Width, 0), page)!.Value.X, 0.5);
    });

    private static void Save(Scene scene, string name)
    {
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }
    }

    // The same content as the --html at the top of docs/components/ion-chip/COMPARISON.md: rows are flex rows, with align-items: center.
    private static (StackPanel Page, AvonicChip First) Page()
    {
        static AvonicLabel Label(string text) => new() { Content = text };
        static AvonicIcon Icon(string name) => new() { IconName = name };
        static AvonicChip Chip(params object[] items)
        {
            var chip = new AvonicChip();
            foreach (var item in items)
            {
                chip.Items.Add(item);
            }

            return chip;
        }

        static StackPanel Row(params Control[] chips)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.AddRange(chips);
            return row;
        }

        var outline = Chip(Label("Outline"));
        outline.Outline = true;
        var disabled = Chip(Label("Disabled"));
        disabled.IsEnabled = false;
        var primary = Chip(Label("Primary"));
        primary.Color = "primary";
        var primaryOutline = Chip(Label("Outline"));
        primaryOutline.Color = "primary";
        primaryOutline.Outline = true;
        var danger = Chip(Icon("heart"), Label("Danger"));
        danger.Color = "danger";
        var rtl = Row(Chip(Icon("pin"), Label("RTL"), Icon("close-circle")));
        rtl.FlowDirection = FlowDirection.RightToLeft;

        var first = Chip(Label("Default"));
        var page = new StackPanel
        {
            Margin = new Thickness(8),
            Children =
            {
                Row(first, outline, disabled),
                Row(primary, primaryOutline, danger),
                Row(Chip(Icon("pin"), Label("Icon start")), Chip(Label("Icon end"), Icon("close-circle")), Chip(Icon("star"))),
                Chip(Label("A long label that has to wrap because it does not fit on one line of a phone")),
                rtl,
            },
        };

        return (page, first);
    }
}
