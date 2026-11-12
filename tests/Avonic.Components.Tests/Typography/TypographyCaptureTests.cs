using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;
using Avonic.Sample.Pages;

namespace Avonic.Components.Tests.Typography;

/// <summary>
/// Real frames for the compare step: the sample's page, with the file and without. With <c>AVONIC_CAPTURE_DIR</c> set they're also
/// saved, for docs/infra/typography/COMPARISON.md.
/// </summary>
public class TypographyCaptureTests
{
    private static (Scene Scene, TypographyPage Page) Show(bool withFile)
    {
        var page = new TypographyPage();
        if (!withFile)
        {
            page.Resources.MergedDictionaries.Clear();
        }

        var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        return (scene, page);
    }

    private static void Save(Scene scene, string name)
    {
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            scene.Window.CaptureRenderedFrame()!.Save(Path.Combine(dir, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);

            // Each text's top and height in the window, beside Chromium's in COMPARISON.md.
            var boxes = scene.Window.GetVisualDescendants().OfType<TextBlock>()
                .Select(t => (t.Text, Box: t.TransformToVisual(scene.Window) is { } m ? new Rect(t.Bounds.Size).TransformToAABB(m) : default))
                .Select(b => $"{b.Text}	{b.Box.Top:0.#}	{b.Box.Height:0.#}");
            File.WriteAllLines(Path.Combine(dir, Path.ChangeExtension(name, ".txt")), boxes);
        }
    }

    private static TextBlock Find(TypographyPage page, string text) =>
        page.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == text);

    [AvaloniaFact]
    public void Reference_Page_With_The_File() => InstalledTheme.Run(_ =>
    {
        var (scene, page) = Show(withFile: true);
        using var shown = scene;
        Save(scene, "avonic-typography-on-page.png");

        // Measured bh1, dh1, lh1, ch1 in Chromium: the page merges the file for itself, and each host's rules win where they do.
        Assert.Equal(26, Find(page, "Heading 1").FontSize);
        Assert.Equal(26, Find(page, "h1 in a divider").FontSize);
        Assert.Equal(24, Find(page, "h1 in a label").FontSize);
        Assert.Equal(28.8, Find(page, "h1 in a card").LineHeight, 6);
    });

    [AvaloniaFact]
    public void Reference_Page_Without_The_File() => InstalledTheme.Run(_ =>
    {
        var (scene, page) = Show(withFile: false);
        using var shown = scene;
        Save(scene, "avonic-typography-off-page.png");

        Assert.Equal(default, Find(page, "Heading 1").Margin);
        Assert.True(double.IsNaN(Find(page, "h1 in a label").LineHeight));
        Assert.Equal(36, Find(page, "h1 in a card").LineHeight, 6);
    });
}
