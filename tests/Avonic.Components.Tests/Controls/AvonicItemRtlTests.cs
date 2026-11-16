using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// #90: in right to left, the inner border of an item, a divider and a list header ends where Chromium's does, with and without
/// safe areas. The page is docs/components/ion-item/ref/rtl/page.html, and Chromium's numbers are its boxes.txt and the lines in its
/// page.png, at a scale of 2.
/// </summary>
public class AvonicItemRtlTests
{
    /// <summary>A row of the page, with Chromium's inner part and container (x and width), and its lowest line's first and last pixel.</summary>
    private sealed record Row(string Id, Control Control, double InnerX, double InnerWidth, double ContainerX, double ContainerWidth, int LineFirst, int LineLast)
    {
        public bool Safe => Id.StartsWith("safe", StringComparison.Ordinal);
    }

    private static Row[] Rows()
    {
        static AvonicIcon Icon(string name) => new() { IconName = name };
        const FlowDirection rtl = FlowDirection.RightToLeft;
        return
        [
            new("ltr-detail", new AvonicItem { Detail = true, StartContent = Icon("heart"), Content = "LTR", EndContent = Icon("star") }, 72, 318, 72, 240, 144, 779),
            new("rtl-detail", new AvonicItem { FlowDirection = rtl, Detail = true, StartContent = Icon("heart"), Content = "RTL", EndContent = Icon("star") }, 0, 318, 78, 240, 0, 635),
            new("rtl-badge", new AvonicItem
            {
                FlowDirection = rtl,
                StartContent = new AvonicAvatar { Content = new Border { Background = Brushes.Blue } },
                Content = new AvonicLabel { Content = "RTL" },
                EndContent = new AvonicBadge { Content = "7" },
            }, 0, 318, 47.5, 270.5, 0, 635),
            new("rtl-plain", new AvonicItem { FlowDirection = rtl, Content = "Plain" }, 0, 374, 16, 358, 0, 747),
            new("rtl-full", new AvonicItem { FlowDirection = rtl, Content = "Full", Lines = ItemLines.Full }, 0, 374, 16, 358, 0, 779),
            new("rtl-divider", new AvonicItemDivider { FlowDirection = rtl, Content = new AvonicLabel { Content = "Divider" } }, 0, 374, 16, 358, 0, 779),
            new("rtl-divider-slots", new AvonicItemDivider
            {
                FlowDirection = rtl, StartContent = Icon("heart"), Content = new AvonicLabel { Content = "Divider slots" }, EndContent = Icon("star"),
            }, 0, 318, 56, 262, 0, 779),
            new("rtl-header-inset", new AvonicListHeader { FlowDirection = rtl, Content = new AvonicLabel { Content = "Header inset" }, Lines = ListHeaderLines.Inset }, 0, 374, double.NaN, double.NaN, 0, 747),
            new("rtl-header-full", new AvonicListHeader { FlowDirection = rtl, Content = new AvonicLabel { Content = "Header full" }, Lines = ListHeaderLines.Full }, 0, 374, double.NaN, double.NaN, 0, 779),

            // --ion-safe-area-left: 20px; --ion-safe-area-right: 10px
            new("safe-detail", new AvonicItem { FlowDirection = rtl, Detail = true, StartContent = Icon("heart"), Content = "RTL", EndContent = Icon("star") }, 0, 308, 98, 210, 0, 615),
            new("safe-plain", new AvonicItem { FlowDirection = rtl, Content = "Plain" }, 0, 364, 36, 328, 0, 727),
            new("safe-divider", new AvonicItemDivider { FlowDirection = rtl, Content = new AvonicLabel { Content = "Divider" } }, 0, 364, 36, 328, 0, 779),
            new("safe-header", new AvonicListHeader { FlowDirection = rtl, Content = new AvonicLabel { Content = "Header inset" }, Lines = ListHeaderLines.Inset }, 20, 344, double.NaN, double.NaN, 40, 727),
            new("safe-ltr-detail", new AvonicItem { Detail = true, StartContent = Icon("heart"), Content = "LTR", EndContent = Icon("star") }, 92, 298, 92, 210, 184, 779),
        ];
    }

    /// <summary>The page at 390 wide and a scale of 2, as Chromium's; the safe rows with the safe areas set on them.</summary>
    private static (Scene Scene, Row[] Rows) Page()
    {
        var rows = Rows();
        var page = new StackPanel();
        foreach (var row in rows)
        {
            if (row.Safe)
            {
                row.Control.Resources["Avonic.SafeAreaLeft"] = 20d;
                row.Control.Resources["Avonic.SafeAreaRight"] = 10d;
            }

            page.Children.Add(row.Control);
        }

        var scene = new Scene(page, 390, 1400);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        return (scene, rows);
    }

    private static Rect Box(Row row, string part, Visual window) =>
        row.Control.GetVisualDescendants().OfType<Control>().First(c => c.Name == part) is var v
            ? new Rect(v.Bounds.Size).TransformToAABB(v.TransformToVisual(window)!.Value)
            : default;

    [AvaloniaFact]
    public void R1_The_Inner_Part_And_The_Container_Are_Where_Chromiums_Are() => InstalledTheme.Run(_ =>
    {
        var (scene, rows) = Page();
        using var shown = scene;
        var wrong = new List<string>();
        foreach (var row in rows)
        {
            var inner = Box(row, "PART_Inner", scene.Window);
            if (inner.X != row.InnerX || inner.Width != row.InnerWidth)
            {
                wrong.Add($"{row.Id}: inner {inner.X} {inner.Width}, Chromium {row.InnerX} {row.InnerWidth}");
            }

            if (!double.IsNaN(row.ContainerX))
            {
                var container = Box(row, "PART_Container", scene.Window);
                if (container.X != row.ContainerX || container.Width != row.ContainerWidth)
                {
                    wrong.Add($"{row.Id}: container {container.X} {container.Width}, Chromium {row.ContainerX} {row.ContainerWidth}");
                }
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    });

    [AvaloniaFact]
    public void R2_Each_Rows_Line_Starts_And_Ends_Where_Chromiums_Does() => InstalledTheme.Run(_ =>
    {
        var (scene, rows) = Page();
        using var shown = scene;
        var frame = scene.Window.CaptureRenderedFrame()!;
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-item-rtl-page.png"), PngBitmapEncoderOptions.Default);
        }

        var wrong = new List<string>();
        foreach (var row in rows)
        {
            var (first, last) = LowestLine(frame, row.Control, scene.Window);
            if (first != row.LineFirst || last != row.LineLast)
            {
                wrong.Add($"{row.Id}: {first}..{last}, Chromium {row.LineFirst}..{row.LineLast}");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    });

    /// <summary>The first and last grey pixel of the lowest line in a row's last four pixel rows, or -1 when there's none.</summary>
    private static (int First, int Last) LowestLine(WriteableBitmap frame, Control row, Visual window)
    {
        var top = row.TransformToVisual(window)!.Value.M32;
        var bottom = (int)((top + row.Bounds.Height) * 2) - 1;
        for (var y = bottom; y > bottom - 4; y--)
        {
            int first = -1, last = -1;
            for (var x = 0; x < frame.PixelSize.Width; x++)
            {
                var pixel = Frames.PixelAt(frame, x, y);
                if (pixel.R < 245 && Math.Abs(pixel.R - pixel.B) < 6 && Math.Abs(pixel.R - pixel.G) < 6)
                {
                    if (first < 0)
                    {
                        first = x;
                    }

                    last = x;
                }
            }

            if (first >= 0)
            {
                return (first, last);
            }
        }

        return (-1, -1);
    }
}
