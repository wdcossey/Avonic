using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicSegmentCaptureTests
{
    internal static AvonicSegmentButton Button(string value, string label, string? icon = null, SegmentButtonLayout layout = SegmentButtonLayout.IconTop)
    {
        var button = new AvonicSegmentButton { Value = value, Layout = layout, Items = { new AvonicLabel { Content = label } } };
        if (icon is not null)
        {
            button.Items.Add(new AvonicIcon { IconName = icon });
        }

        return button;
    }

    internal static AvonicSegment Segment(string value, params AvonicSegmentButton[] buttons)
    {
        var segment = new AvonicSegment { Value = value };
        foreach (var button in buttons)
        {
            segment.Items.Add(button);
        }

        return segment;
    }

    /// <summary>The same content as the --html at the top of docs/components/ion-segment/COMPARISON.md.</summary>
    internal static StackPanel Page()
    {
        var disabled = Button("2", "Two");
        disabled.IsEnabled = false;
        var colored = Segment("call", Button("call", "Call", "call"), Button("heart", "Heart", "heart"), Button("pin", "Pin", "pin"));
        colored.Color = "danger";
        var scrollable = Segment("s4", Button("s1", "First"), Button("s2", "Second"), Button("s3", "Third"), Button("s4", "Fourth"), Button("s5", "Fifth"), Button("s6", "Sixth"));
        scrollable.Scrollable = true;
        var withView = Segment("v1", Button("v1", "Photos"), Button("v2", "Music"), Button("v3", "Files"));
        foreach (var button in withView.Items.OfType<AvonicSegmentButton>())
        {
            button.ContentId = (string)button.Value!;
        }

        static AvonicSegmentContent Content(string name, string text) =>
            new() { Name = name, Content = new TextBlock { Text = text, Margin = new Avalonia.Thickness(16), FontSize = 16 } };

        return new StackPanel
        {
            Spacing = 8,
            Children =
            {
                Segment("a", Button("a", "Friends"), Button("b", "Enemies")),
                colored,
                Segment("wifi", Button("wifi", "Wi-Fi", "wifi", SegmentButtonLayout.IconStart), Button("bt", "Bluetooth", "bluetooth", SegmentButtonLayout.IconStart)),
                Segment("1", Button("1", "One"), disabled, Button("3", "Three")),
                scrollable,
                new AvonicToolbar { Color = "primary", Content = Segment("t1", Button("t1", "Inbox"), Button("t2", "Sent")) },
                new AvonicToolbar { Content = Segment("t2", Button("t1", "Inbox"), Button("t2", "Sent")) },
                withView,
                new AvonicSegmentView { Height = 64, Items = { Content("v1", "Photos content"), Content("v2", "Music content"), Content("v3", "Files content") } },
            },
        };
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var page = Page();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-segment-page.png");

        Assert.True(page.Bounds.Height > 0);
    });

    private static void Save(Scene scene, string name)
    {
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }
    }
}
