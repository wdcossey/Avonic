using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicAccordionCaptureTests
{
    /// <summary>The same content as the <c>--html</c> in docs/components/ion-accordion/COMPARISON.md.</summary>
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        static AvonicAccordion Accordion(string value, string header, string content, bool enabled = true) => new()
        {
            Value = value,
            IsEnabled = enabled,
            HeaderContent = new AvonicItem { Content = new AvonicLabel { Content = header } },
            // <div style="padding:16px">, whose line of text is 19 px tall in Chromium
            ContentContent = new TextBlock { Margin = new Thickness(16), Text = content, FontSize = 16 },
        };

        var compact = new AvonicAccordionGroup { Value = "first" };
        compact.Items.Add(Accordion("first", "First Accordion", "First Content"));
        compact.Items.Add(Accordion("second", "Second Accordion", "Second Content"));
        compact.Items.Add(Accordion("third", "Disabled Accordion", "Third Content", enabled: false));

        var inset = new AvonicAccordionGroup { Value = "b", Expand = AccordionGroupExpand.Inset };
        inset.Items.Add(Accordion("a", "Inset A", "A Content"));
        inset.Items.Add(Accordion("b", "Inset B", "B Content"));
        inset.Items.Add(Accordion("c", "Inset C", "C Content"));

        var page = new StackPanel { Children = { compact, inset } };
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-accordion-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        // Measured in Chromium: the compact group 196 tall, and the inset one at 212, 358 wide.
        Assert.Equal(196, compact.Bounds.Height, 0);
        Assert.Equal(new Rect(16, 212, 358, 227), inset.Bounds);
    });
}
