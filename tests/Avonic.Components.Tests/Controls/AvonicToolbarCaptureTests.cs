using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicToolbarCaptureTests
{
    internal static AvonicButton IconButton(string icon) => new() { IconOnlyContent = new AvonicIcon { IconName = icon } };

    internal static AvonicButtons Buttons(params Control[] buttons)
    {
        var row = new AvonicButtons();
        foreach (var button in buttons)
        {
            row.Items.Add(button);
        }

        return row;
    }

    // The same content as the command at the top of docs/components/ion-toolbar/COMPARISON.md.
    internal static StackPanel ReferencePage() => new()
    {
        Children =
        {
            new AvonicHeader
            {
                Items =
                {
                    new AvonicToolbar
                    {
                        StartContent = Buttons(IconButton("menu")),
                        Content = new AvonicTitle { Content = "Header" },
                        EndContent = Buttons(new AvonicButton { Content = "Edit" }, IconButton("search")),
                    },
                },
            },
            new AvonicToolbar
            {
                Color = "primary",
                SecondaryContent = Buttons(IconButton("star")),
                PrimaryContent = Buttons(new AvonicButton { Content = "Primary" }),
                Content = new AvonicTitle { Content = "Primary" },
            },
            new AvonicToolbar
            {
                Color = "danger",
                StartContent = Buttons(
                    new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid },
                    new AvonicButton { Content = "Outline", Fill = ButtonFill.Outline },
                    new AvonicButton { Content = "Clear" }),
            },
            new AvonicToolbar
            {
                StartContent = Buttons(
                    new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid },
                    new AvonicButton { Content = "Outline", Fill = ButtonFill.Outline },
                    new AvonicButton { Content = "Clear", Color = "success" }),
            },
            new AvonicToolbar { Content = new AvonicTitle { Content = "Small title", Size = TitleSize.Small } },
            new AvonicToolbar { Content = new AvonicTitle { Content = "Large title, coloured", Size = TitleSize.Large, Color = "tertiary" } },
            new AvonicToolbar { Content = new AvonicTitle { Content = "A title that is far too long to fit in the toolbar on one line" } },
            new AvonicToolbar { Content = "Default slot text" },
            new AvonicToolbar
            {
                FlowDirection = FlowDirection.RightToLeft,
                StartContent = Buttons(new AvonicButton { Content = "Start" }),
                Content = new AvonicTitle { Content = "RTL" },
                EndContent = Buttons(new AvonicButton { Content = "End" }),
            },
            new AvonicFooter { Items = { new AvonicToolbar { Content = new AvonicTitle { Content = "Footer" } } } },
        },
    };

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var page = ReferencePage();

        // A web page's text is 16, which the toolbar inherits. Avalonia's default is the app's (ADR 0002).
        TextElement.SetFontSize(page, 16);

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-toolbar-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        // The header, eight toolbars and the footer, each 56 high, as measured.
        Assert.Equal(Enumerable.Repeat(56d, 10), page.Children.Select(child => child.Bounds.Height));
    });
}
