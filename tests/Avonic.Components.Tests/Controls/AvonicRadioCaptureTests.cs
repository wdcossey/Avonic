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
public class AvonicRadioCaptureTests
{
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(theme =>
    {
        var page = Page();
        Assert.True(theme.TryGetResource("Avonic.FontFamily", null, out var font));
        TextElement.SetFontFamily(page, (FontFamily)font!);
        // A browser's 16, which the hint and a radio outside an item inherit, as nothing in Ionic sets it.
        TextElement.SetFontSize(page, 16);
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-radio-page.png");

        Assert.True(page.Bounds.Height > 0);
    });

    private static void Save(Scene scene, string name)
    {
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, name), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }
    }

    private static AvonicItem Item(AvonicRadio radio) => new() { Content = radio };

    // The same content as the --html at the top of docs/components/ion-radio/COMPARISON.md.
    private static StackPanel Page()
    {
        var first = new AvonicRadioGroup
        {
            Value = "b",
            HelperText = "Helper text",
            Items =
            {
                Item(new AvonicRadio { Value = "a", Content = "Apple" }),
                Item(new AvonicRadio { Value = "b", Content = "Banana" }),
                Item(new AvonicRadio { Value = "c", Content = "Cherry", IsEnabled = false }),
                Item(new AvonicRadio { Value = "d", Content = "Label at the end", LabelPlacement = RadioLabelPlacement.End }),
                Item(new AvonicRadio { Value = "e", Content = "A fixed label that is far too long", LabelPlacement = RadioLabelPlacement.Fixed }),
                Item(new AvonicRadio { Value = "f", Content = "Stacked", LabelPlacement = RadioLabelPlacement.Stacked }),
                Item(new AvonicRadio { Value = "g", Content = "Stacked, centred", LabelPlacement = RadioLabelPlacement.Stacked, Alignment = RadioAlignment.Center }),
                Item(new AvonicRadio { Value = "h", Content = "Justify start", Justify = RadioJustify.Start }),
                Item(new AvonicRadio { Value = "i", Content = "Justify end", Justify = RadioJustify.End }),
            },
        };

        var invalid = new AvonicRadioGroup
        {
            Value = "x",
            HelperText = "Helper text",
            ErrorText = "Error text",
            Classes = { "ion-touched", "ion-invalid" },
            Items = { Item(new AvonicRadio { Value = "x", Content = "Danger", Color = "danger" }) },
        };

        var focused = new AvonicRadio { Value = "x" };
        ((IPseudoClasses)focused.Classes).Add(":ion-focused");
        var row = new AvonicRadioGroup
        {
            Value = "x",
            Margin = new Thickness(16),
            Items =
            {
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 16,
                    Children =
                    {
                        new AvonicRadio { Value = "y", VerticalAlignment = VerticalAlignment.Center },
                        new AvonicRadio { Value = "x", VerticalAlignment = VerticalAlignment.Center },
                        new AvonicRadio { Value = "y", IsEnabled = false, VerticalAlignment = VerticalAlignment.Center },
                        new AvonicRadio { Value = "x", IsEnabled = false, VerticalAlignment = VerticalAlignment.Center },
                        new AvonicRadio { Value = "x", Color = "success", VerticalAlignment = VerticalAlignment.Center },
                        focused,
                        new AvonicRadio { Value = "y", Content = "Label", VerticalAlignment = VerticalAlignment.Center },
                    },
                },
            },
        };
        focused.VerticalAlignment = VerticalAlignment.Center;

        var rtl = new AvonicList
        {
            FlowDirection = FlowDirection.RightToLeft,
            Items =
            {
                new AvonicRadioGroup
                {
                    Value = "r",
                    Items =
                    {
                        Item(new AvonicRadio { Value = "r", Content = "RTL" }),
                        Item(new AvonicRadio { Value = "s", Content = "RTL, at the end", LabelPlacement = RadioLabelPlacement.End }),
                    },
                },
            },
        };

        return new StackPanel
        {
            Children =
            {
                new AvonicList { Items = { first } },
                new AvonicList { Items = { invalid } },
                row,
                rtl,
            },
        };
    }
}
