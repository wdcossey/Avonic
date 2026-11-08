using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, as <c>avonic-ion-spinner-page.png</c>,
/// for <c>docs/components/ion-spinner/COMPARISON.md</c>.
/// </summary>
public class AvonicSpinnerCaptureTests
{
    // The same scene as the command at the top of COMPARISON.md, which seeks every animation to 350 ms.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var frame = Render();
        Save(frame, "avonic-ion-spinner-page.png");

        // The large circular spinner's arc, at 350 ms, is over the bottom: dash 50.5 from 7.5 along, 21° to 166°.
        // Its centre is at (16 + 40, 16 + 28 + 16 + 28 + 16 + 40) px, r 33.3 px, at 2×.
        var centre = new Point(56, 144);
        Assert.True(Frames.PixelAt(frame, (int)(centre.X * 2), (int)((centre.Y + 33) * 2)).R < 64);
        Assert.True(Frames.PixelAt(frame, (int)(centre.X * 2), (int)((centre.Y - 33) * 2)).R > 192);
    });

    // Ionic's spinner doesn't read the animated config, so with it off the page at 350 ms is the same frame, every byte.
    [AvaloniaFact]
    public void Reference_Page_With_Animated_Off_Is_The_Same_Frame() => InstalledTheme.Run(_ =>
    {
        var expected = Render();
        AvonicConfig.Animated = false;
        try
        {
            var frame = Render();
            Save(frame, "avonic-ion-spinner-page-animated-off.png");
            AssertSameFrame(expected, frame);
        }
        finally
        {
            AvonicConfig.Animated = true;
        }
    });

    private static void Save(WriteableBitmap frame, string name)
    {
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, name), PngBitmapEncoderOptions.Default);
        }
    }

    /// <summary>Every byte of two frames, row by row, as a frame's rows may be padded.</summary>
    private static unsafe void AssertSameFrame(WriteableBitmap expected, WriteableBitmap actual)
    {
        using var a = expected.Lock();
        using var b = actual.Lock();
        Assert.Equal(a.Size, b.Size);
        var differing = 0;
        for (var y = 0; y < a.Size.Height; y++)
        {
            var rowA = new ReadOnlySpan<byte>((byte*)a.Address + y * a.RowBytes, a.Size.Width * 4);
            var rowB = new ReadOnlySpan<byte>((byte*)b.Address + y * b.RowBytes, b.Size.Width * 4);
            if (!rowA.SequenceEqual(rowB))
            {
                differing++;
            }
        }

        Assert.True(differing == 0, $"{differing} rows differ");
    }

    private static WriteableBitmap Render()
    {
        using var clock = new TestClock();

        static StackPanel Row(params AvonicSpinner[] spinners)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            row.Children.AddRange(spinners);
            return row;
        }

        static AvonicSpinner Spinner(SpinnerName? name = null, string? color = null, double? size = null)
        {
            var spinner = new AvonicSpinner { SpinnerName = name, Color = color };
            if (size is { } s)
            {
                spinner.Width = s;
                spinner.Height = s;
            }

            return spinner;
        }

        var page = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 16,
            Children =
            {
                Row(Enum.GetValues<SpinnerName>().Select(name => Spinner(name)).ToArray()),
                Row(Spinner(), Spinner(SpinnerName.Crescent, "danger"), Spinner(SpinnerName.Lines, "primary"), Spinner(SpinnerName.Dots, "success")),
                Row(Spinner(size: 80), Spinner(SpinnerName.Lines, size: 80), Spinner(SpinnerName.Crescent, size: 80), Spinner(SpinnerName.Dots, size: 80)),
            },
        };
        Avalonia.Controls.Documents.TextElement.SetForeground(page, Brushes.Black);

        using var scene = new Scene(page);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        clock.At(350);
        Dispatcher.UIThread.RunJobs();
        return scene.Window.CaptureRenderedFrame()!;
    }
}
