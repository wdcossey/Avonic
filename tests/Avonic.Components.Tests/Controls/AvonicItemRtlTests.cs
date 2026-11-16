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
/// safe areas, and in a list. The page is docs/components/ion-item/ref/rtl/page.html, where the direction comes from a
/// <c>div dir="rtl"</c> around the rows, as it does here from a panel around them. Chromium's numbers are its boxes.txt and the
/// lines in its page.png, at a scale of 2.
/// </summary>
public class AvonicItemRtlTests
{
    private enum Group
    {
        Ltr,
        Rtl,
        RtlList,
        SafeRtl,
        SafeLtr,
    }

    /// <summary>A row of the page, with Chromium's inner part and container (x and width), and its lowest line's first and last pixel.</summary>
    private sealed record Row(string Id, Group Group, Control Control, double InnerX, double InnerWidth, double ContainerX, double ContainerWidth, int LineFirst, int LineLast);

    private static Row[] Rows()
    {
        static AvonicIcon Icon(string name) => new() { IconName = name };
        return
        [
            new("ltr-detail", Group.Ltr, new AvonicItem { Detail = true, StartContent = Icon("heart"), Content = "LTR", EndContent = Icon("star") }, 72, 318, 72, 240, 144, 779),
            new("rtl-detail", Group.Rtl, new AvonicItem { Detail = true, StartContent = Icon("heart"), Content = "RTL", EndContent = Icon("star") }, 0, 318, 78, 240, 0, 635),
            new("rtl-badge", Group.Rtl, new AvonicItem
            {
                StartContent = new AvonicAvatar { Content = new Border { Background = Brushes.Blue } },
                Content = new AvonicLabel { Content = "RTL" },
                EndContent = new AvonicBadge { Content = "7" },
            }, 0, 318, 47.5, 270.5, 0, 635),
            new("rtl-plain", Group.Rtl, new AvonicItem { Content = "Plain" }, 0, 374, 16, 358, 0, 747),
            new("rtl-full", Group.Rtl, new AvonicItem { Content = "Full", Lines = ItemLines.Full }, 0, 374, 16, 358, 0, 779),
            new("rtl-divider", Group.Rtl, new AvonicItemDivider { Content = new AvonicLabel { Content = "Divider" } }, 0, 374, 16, 358, 0, 779),
            new("rtl-divider-slots", Group.Rtl, new AvonicItemDivider
            {
                StartContent = Icon("heart"), Content = new AvonicLabel { Content = "Divider slots" }, EndContent = Icon("star"),
            }, 0, 318, 56, 262, 0, 779),
            new("rtl-header-inset", Group.Rtl, new AvonicListHeader { Content = new AvonicLabel { Content = "Header inset" }, Lines = ListHeaderLines.Inset }, 0, 374, double.NaN, double.NaN, 0, 747),
            new("rtl-header-full", Group.Rtl, new AvonicListHeader { Content = new AvonicLabel { Content = "Header full" }, Lines = ListHeaderLines.Full }, 0, 374, double.NaN, double.NaN, 0, 779),
            new("list-first", Group.RtlList, new AvonicItem { Content = "In a list" }, 0, 374, 16, 358, 0, 747),
            new("list-last", Group.RtlList, new AvonicItem { Content = "Last in a list" }, 0, 374, 16, 358, 0, 747),

            // --ion-safe-area-left: 20px; --ion-safe-area-right: 10px
            new("safe-detail", Group.SafeRtl, new AvonicItem { Detail = true, StartContent = Icon("heart"), Content = "RTL", EndContent = Icon("star") }, 0, 308, 98, 210, 0, 615),
            new("safe-plain", Group.SafeRtl, new AvonicItem { Content = "Plain" }, 0, 364, 36, 328, 0, 727),
            new("safe-divider", Group.SafeRtl, new AvonicItemDivider { Content = new AvonicLabel { Content = "Divider" } }, 0, 364, 36, 328, 0, 779),
            new("safe-header", Group.SafeRtl, new AvonicListHeader { Content = new AvonicLabel { Content = "Header inset" }, Lines = ListHeaderLines.Inset }, 20, 344, double.NaN, double.NaN, 40, 727),
            new("safe-ltr-detail", Group.SafeLtr, new AvonicItem { Detail = true, StartContent = Icon("heart"), Content = "LTR", EndContent = Icon("star") }, 92, 298, 92, 210, 184, 779),
        ];
    }

    /// <summary>The page at 390 wide and a scale of 2, as Chromium's, each group of rows in a panel that gives it its direction and safe areas.</summary>
    private static (Scene Scene, Row[] Rows) Page()
    {
        var rows = Rows();
        StackPanel Panel(Group group, bool rtl, bool safe)
        {
            var panel = new StackPanel { FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            if (safe)
            {
                panel.Resources["Avonic.SafeAreaLeft"] = 20d;
                panel.Resources["Avonic.SafeAreaRight"] = 10d;
            }

            foreach (var row in rows.Where(r => r.Group == group))
            {
                panel.Children.Add(row.Control);
            }

            return panel;
        }

        var rtlPanel = Panel(Group.Rtl, rtl: true, safe: false);
        var list = new AvonicList();
        foreach (var row in rows.Where(r => r.Group == Group.RtlList))
        {
            list.Items.Add(row.Control);
        }

        rtlPanel.Children.Add(list);
        var page = new StackPanel
        {
            Children =
            {
                Panel(Group.Ltr, rtl: false, safe: false),
                rtlPanel,
                Panel(Group.SafeRtl, rtl: true, safe: true),
                Panel(Group.SafeLtr, rtl: false, safe: true),
            },
        };

        var scene = new Scene(page, 390, 1400);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        return (scene, rows);
    }

    private static Rect? Box(Row row, string part, Visual window) =>
        row.Control.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == part) is { } v
            ? new Rect(v.Bounds.Size).TransformToAABB(v.TransformToVisual(window)!.Value)
            : null;

    private static bool Near(double expected, double actual) => Math.Abs(expected - actual) < 0.05;

    [AvaloniaFact]
    public void R1_The_Inner_Part_And_The_Container_Are_Where_Chromiums_Are() => InstalledTheme.Run(_ =>
    {
        var (scene, rows) = Page();
        using var shown = scene;
        var wrong = new List<string>();
        foreach (var row in rows)
        {
            var inner = Box(row, "PART_Inner", scene.Window);
            if (inner is not { } i || !Near(row.InnerX, i.X) || !Near(row.InnerWidth, i.Width))
            {
                wrong.Add($"{row.Id}: inner {inner}, Chromium x {row.InnerX} width {row.InnerWidth}");
            }

            if (!double.IsNaN(row.ContainerX))
            {
                var container = Box(row, "PART_Container", scene.Window);
                if (container is not { } c || !Near(row.ContainerX, c.X) || !Near(row.ContainerWidth, c.Width))
                {
                    wrong.Add($"{row.Id}: container {container}, Chromium x {row.ContainerX} width {row.ContainerWidth}");
                }
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    });

    [AvaloniaFact]
    public void R2_Each_Rows_Line_Is_Unbroken_And_Starts_And_Ends_Where_Chromiums_Does() => InstalledTheme.Run(_ =>
    {
        var (scene, rows) = Page();
        using var shown = scene;
        using var frame = scene.Window.CaptureRenderedFrame()!;
        // compare/avonic-rtl-page.png is this, cut to Chromium's 844 high
        Frames.Save(frame, "avonic-item-rtl-page.png");

        var wrong = new List<string>();
        foreach (var row in rows)
        {
            var (first, last, count) = LowestLine(frame, row.Control, scene.Window);
            if (first != row.LineFirst || last != row.LineLast || count != last - first + 1)
            {
                wrong.Add($"{row.Id}: {first}..{last} with {count} grey, Chromium {row.LineFirst}..{row.LineLast} unbroken");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    });

    /// <summary>
    /// The first and last grey pixel of the lowest line in a row's last four pixel rows, and how many of the pixels are grey, or -1
    /// when there's none.
    /// </summary>
    private static (int First, int Last, int Count) LowestLine(WriteableBitmap frame, Control row, Visual window)
    {
        var top = row.TransformToVisual(window)!.Value.M32;
        var bottom = (int)Math.Round((top + row.Bounds.Height) * 2) - 1;
        for (var y = bottom; y > bottom - 4; y--)
        {
            int first = -1, last = -1, count = 0;
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
                    count++;
                }
            }

            if (first >= 0)
            {
                return (first, last, count);
            }
        }

        return (-1, -1, 0);
    }
}
