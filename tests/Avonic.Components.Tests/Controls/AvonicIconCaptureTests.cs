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
/// Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for
/// <c>tools/ionic-ref/compare-icons.js</c> and <c>COMPARISON.md</c>.
/// </summary>
[Collection(IconStoreCollection.Name)]
public class AvonicIconCaptureTests
{
    private static unsafe bool HasInk(WriteableBitmap bitmap, int x0, int y0, int size)
    {
        using var fb = bitmap.Lock();
        for (var y = y0; y < y0 + size; y++)
        {
            var row = (byte*)fb.Address + y * fb.RowBytes;
            for (var x = x0; x < x0 + size; x++)
            {
                if (row[x * 4] < 128)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // The layout has to match tools/ionic-ref/compare-icons.js.
    [AvaloniaFact]
    public void Every_Icon_Paints()
    {
        const int Cols = 40, Cell = 40, Size = 32, Pad = 4, Scale = 2;

        var names = IconStore.BundledNames.Order(StringComparer.Ordinal).ToList();
        var rows = (int)Math.Ceiling(names.Count / (double)Cols);
        var canvas = new Canvas { Width = Cols * Cell, Height = rows * Cell, Background = Brushes.White };
        for (var i = 0; i < names.Count; i++)
        {
            var icon = new AvonicIcon { IconName = names[i], FontSize = Size, Foreground = Brushes.Black };
            Canvas.SetLeft(icon, i % Cols * Cell + Pad);
            Canvas.SetTop(icon, i / Cols * Cell + Pad);
            canvas.Children.Add(icon);
        }

        using var scene = new Scene(canvas, Cols * Cell, rows * Cell);
        scene.Window.SetRenderScaling(Scale);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "icons-avonic.png");

        var blank = names.Where((_, i) => !HasInk(frame, i % Cols * Cell * Scale, i / Cols * Cell * Scale, Cell * Scale)).ToList();
        Assert.Empty(blank);
    }

    // The same content as DEFAULT_HTML['ion-icon'] in tools/ionic-ref/render.js.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        static StackPanel Row(params AvonicIcon[] icons)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 0, 0, 4) };
            row.Children.AddRange(icons);
            return row;
        }

        static AvonicIcon Named(string name, double size = 24) => new() { IconName = name, FontSize = size };

        var colors = new[] { "primary", "secondary", "tertiary", "success", "warning", "danger", "light", "medium", "dark" };
        var rtl = Row(
            Named("arrow-back"),
            Named("chevron-forward"),
            new AvonicIcon { IconName = "chevron-forward", FlipRtl = false, FontSize = 24 },
            Named("trending-up"),
            new AvonicIcon { IconName = "trending-up", FlipRtl = true, FontSize = 24 });
        rtl.FlowDirection = FlowDirection.RightToLeft;
        rtl.HorizontalAlignment = HorizontalAlignment.Left;

        var page = new StackPanel
        {
            Margin = new Thickness(16),
            Children =
            {
                Row(new[] { "heart", "heart-outline", "heart-sharp", "search", "search-outline", "search-sharp", "logo-github" }
                    .Select(name => Named(name)).ToArray()),
                Row(
                    new AvonicIcon { IconName = "star", Size = IconSize.Small },
                    Named("star"),
                    new AvonicIcon { IconName = "star", Size = IconSize.Large },
                    Named("star", 48)),
                Row(colors.Select(color => new AvonicIcon { IconName = "ellipse", Color = color, FontSize = 24 }).ToArray()),
                rtl,
            },
        };

        using var scene = new Scene(page, 390, 240);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "avonic-page.png");

        Assert.True(HasInk(frame, 0, 0, 200));
    });
}
