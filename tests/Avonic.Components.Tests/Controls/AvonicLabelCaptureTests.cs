using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicLabelCaptureTests
{
    // The same content as DEFAULT_HTML['ion-label'] in tools/ionic-ref/render.js.
    // With Css/Typography.axaml merged, as render.js loads ionic.bundle.css, which includes typography.css.
    [AvaloniaFact]
    public void Reference_Page() => Typography.TypographyTests.WithFile(() =>
    {
        static AvonicLabel Text(string text, string @class)
        {
            var label = new AvonicLabel { Content = text };
            label.Classes.Add(@class);
            return label;
        }

        static AvonicItem Focused(AvonicItem item)
        {
            ((IPseudoClasses)item.Classes).Set(":ion-focused", true);
            return item;
        }

        var page = new StackPanel
        {
            Children =
            {
                new AvonicItem { Content = new AvonicLabel { Content = "Default" } },
                new AvonicItem { Content = new AvonicLabel { Content = "Danger", Color = "danger" } },
                new AvonicItem
                {
                    Color = "primary",
                    Content = new AvonicLabel
                    {
                        Content = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock { Text = "In a primary item" },
                                new TextBlock { Text = "Paragraph", Classes = { "p" } },
                            },
                        },
                    },
                },
                new AvonicItem
                {
                    Content = new AvonicLabel
                    {
                        Content = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock { Text = "Heading one", Classes = { "h1" } },
                                new TextBlock { Text = "Heading two", Classes = { "h2" } },
                                new TextBlock { Text = "Heading three", Classes = { "h3" } },
                                new TextBlock { Text = "Paragraph", Classes = { "p" } },
                            },
                        },
                    },
                },
                new AvonicItem { Content = Text("Wrapped text that certainly has to wrap around because it does not fit on one line", "ion-text-wrap") },
                new AvonicItem { Content = Text("Text that does not wrap, so it certainly has to end with an ellipsis instead", "ion-text-nowrap") },
                new AvonicItem { Content = new AvonicLabel { Content = "Fixed", Position = LabelPosition.Fixed } },
                new AvonicItem { Content = new AvonicLabel { Content = "Stacked", Position = LabelPosition.Stacked } },
                Focused(new AvonicItem { Content = new AvonicLabel { Content = "Stacked and focused", Position = LabelPosition.Stacked, Transitions = [] } }),
                new AvonicItem { Content = new AvonicLabel { Content = "Floating, pushed out of the row", Position = LabelPosition.Floating } },
                Focused(new AvonicItem { Content = new AvonicLabel { Content = "Floating and focused", Position = LabelPosition.Floating, Transitions = [] } }),
                new AvonicItem
                {
                    Content = new AvonicLabel { Content = "RTL stacked", Position = LabelPosition.Stacked },
                    FlowDirection = FlowDirection.RightToLeft,
                },
            },
        };

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-label-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        // The rows the reference has fixed heights for: the stacked and floating ones are 55.
        Assert.All(new[] { 7, 8, 9, 10, 11 }, row => Assert.Equal(55, page.Children[row].Bounds.Height));
        Assert.Equal(48, page.Children[0].Bounds.Height);
    });
}
