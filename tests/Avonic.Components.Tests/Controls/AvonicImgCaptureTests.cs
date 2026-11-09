using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicImgCaptureTests
{
    // The same page as the --html at the top of docs/components/ion-img/COMPARISON.md, with the picture Chromium was given
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        using var loader = new FakeLoader();
        AvonicImg Picture() => new() { Src = "picture.png" };
        var first = new AvonicImg { Src = "picture.png", Alt = "Four quarters" };
        var noSrc = new AvonicImg { Alt = "No src yet", Margin = new Thickness(0, 16, 0, 0) };
        var failed = new AvonicImg { Src = "missing.png", Alt = "A picture that failed", Margin = new Thickness(0, 16, 0, 0) };
        var avatar = new AvonicAvatar { Content = Picture(), VerticalAlignment = VerticalAlignment.Top };
        var thumbnail = new AvonicThumbnail { Content = Picture(), VerticalAlignment = VerticalAlignment.Top };
        var box = new AvonicImg
        {
            Src = "picture.png", Width = 120, Height = 120, Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
        };
        var inItem = Picture();
        var padding = new StackPanel
        {
            Margin = new Thickness(16),
            Children =
            {
                first, noSrc, failed,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 16, Margin = new Thickness(0, 16, 0, 0),
                    Children = { avatar, thumbnail, box },
                },
            },
        };
        var list = new AvonicList
        {
            Items =
            {
                new AvonicItem
                {
                    StartContent = new AvonicThumbnail { Content = inItem },
                    Content = new AvonicLabel { Content = "In an item" },
                },
            },
        };
        var page = new StackPanel { Background = Brushes.White, Children = { padding, list } };
        TextElement.SetFontSize(page, 16);

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        scene.Window.UpdateLayout();
        for (var i = 0; i < loader.Requests.Count; i++)
        {
            if (loader.Requests[i].Src == "missing.png")
            {
                loader.Fail(i);
            }
            else
            {
                loader.Finish(i, 300, 200);
            }
        }

        scene.Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "avonic-img-page.png");

        // Measured in Ionic: 358 × 238.66, then the alt texts 19 high, 16 apart, then the media 64, 48 and 120, and 56 in an
        // item.
        Assert.Equal(6, loader.Requests.Count);
        Assert.Equal(238.66, first.Bounds.Height, 0.5);
        Assert.Equal(new Size(64, 64), avatar.Bounds.Size);
        Assert.Equal(new Size(48, 48), thumbnail.Bounds.Size);
        Assert.Equal(new Size(56, 56), inItem.Bounds.Size);
    });

    // The same page as docs/components/ion-img/ref-broken: what a browser shows where there's no picture
    [AvaloniaFact]
    public void Broken_Image_Page() => InstalledTheme.Run(_ =>
    {
        using var loader = new FakeLoader();
        static AvonicImg Box(string? alt, string? src) => new() { Alt = alt, Src = src, Width = 120, Height = 60 };
        var images = new[]
        {
            new AvonicImg { Alt = "Missing picture", Src = "missing.png" },
            new AvonicImg { Alt = "No src yet" },
            new AvonicImg { Src = "missing.png" },
            new AvonicImg { Alt = "", Src = "missing.png" },
            Box("Missing picture", "missing.png"),
            Box(null, "missing.png"),
            Box("No src yet", null),
            Box(null, null),
            Box("A longer alt text that wraps in its box", "missing.png"),
        };
        var padding = new StackPanel { Margin = new Thickness(16) };
        foreach (var image in images.Take(4))
        {
            padding.Children.Add(new Panel { Margin = new Thickness(0, 0, 0, 12), Children = { image } });
        }

        var wrap = new WrapPanel { ItemSpacing = 12, LineSpacing = 12 };
        wrap.Children.AddRange(images.Skip(4));
        padding.Children.Add(wrap);
        var page = new StackPanel { Background = Brushes.White, Children = { padding } };
        TextElement.SetFontSize(page, 16);

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        scene.Window.UpdateLayout();
        for (var i = 0; i < loader.Requests.Count; i++)
        {
            loader.Fail(i);
        }

        scene.Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "avonic-img-broken.png");

        // Measured in Chromium: 358 × 19 with an alt text, 358 × 24 without, and every box 120 × 60
        Assert.Equal(358, images[0].Bounds.Width);
        Assert.Equal(19, images[0].Bounds.Height, 1);
        Assert.Equal(new Size(358, 24), images[2].Bounds.Size);
        Assert.Equal(new Size(358, 24), images[3].Bounds.Size);
        Assert.All(images.Skip(4), image => Assert.Equal(new Size(120, 60), image.Bounds.Size));
    });
}
