using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicTextCaptureTests
{
    private static TextBlock Block(params Inline[] inlines)
    {
        var block = new TextBlock();
        block.Inlines!.AddRange(inlines);
        return block;
    }

    private static AvonicTextSpan Span(string? color, params Inline[] inlines)
    {
        var span = new AvonicTextSpan { Color = color };
        span.Inlines!.AddRange(inlines);
        return span;
    }

    // The same content as the --html at the top of docs/components/ion-text/COMPARISON.md. The page is
    // 16 px with the theme's font, which is what the web page's body is. It's what this control inherits.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(theme =>
    {
        var heading = Block(new Run("Heading with "), Span("danger", new Run("red")));
        heading.Classes.Add("h2");
        var paragraph = Block(new Run("Paragraph with "), Span("primary", new Run("blue")), new Run(" in it"));
        paragraph.Classes.Add("p");

        var page = new StackPanel
        {
            Children =
            {
                Block(
                    new Run("Some words and "),
                    Span("primary", new Run("some coloured ones which wrap around with the words either side of them")),
                    new Run(" and then some more words after.")),
                Block(
                    new Run("Nested: "),
                    Span("danger", new Run("red, "), Span("success", new Run("green")), new Run(", "), Span(null, new Run("still red"))),
                    new Run(".")),
                new AvonicText
                {
                    Color = "secondary",
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "A block" },
                            new TextBlock { Text = "Another block" },
                        },
                    },
                },
                new AvonicText { Color = "danger", Content = new AvonicIcon { IconName = "heart" } },
                new AvonicItem { Content = new AvonicText { Content = "In an item", Color = "danger" } },
                new AvonicItem { Color = "primary", Content = new AvonicText { Content = "Follows a primary item" } },
                new AvonicItem { Color = "primary", Content = new AvonicText { Content = "Light in a primary item", Color = "light" } },
                new AvonicItem
                {
                    Content = new AvonicLabel { Content = "In the end slot" },
                    EndContent = new AvonicText { Content = "End", Color = "danger" },
                },
                new AvonicItem
                {
                    Content = new AvonicLabel { Content = new StackPanel { Children = { heading, paragraph } } },
                },
            },
        };
        Assert.True(theme.TryGetResource("Avonic.FontFamily", null, out var font));
        TextElement.SetFontFamily(page, (FontFamily)font!);
        TextElement.SetFontSize(page, 16);
        TextBlock.SetTextWrapping(page, TextWrapping.Wrap);

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-text-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        Assert.Equal(48, page.Children[4].Bounds.Height);
    });
}
