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
public class AvonicTextareaCaptureTests
{
    /// <summary>The same content as the snippet at the top of docs/components/ion-textarea/COMPARISON.md.</summary>
    internal static StackPanel Page()
    {
        static AvonicItem Item(AvonicTextarea textarea) => new() { Content = textarea };

        var invalid = new AvonicTextarea
        {
            Fill = TextareaFill.Solid, Label = "Invalid", LabelPlacement = TextareaLabelPlacement.Floating, HelperText = "Helper",
            ErrorText = "Error text", Value = "x",
        };
        invalid.Classes.Add(AvonicTextarea.TouchedClass);
        invalid.Classes.Add(AvonicTextarea.InvalidClass);

        var standalone = new StackPanel
        {
            Margin = new Thickness(16, 0),
            Spacing = 10,
            [TextElement.FontSizeProperty] = 16.0,
            Children =
            {
                new AvonicTextarea { Fill = TextareaFill.Solid, Label = "Solid", LabelPlacement = TextareaLabelPlacement.Floating, HelperText = "Helper text", Counter = true, Maxlength = 100 },
                new AvonicTextarea { Fill = TextareaFill.Outline, Label = "Outline", LabelPlacement = TextareaLabelPlacement.Floating, Value = "Value" },
                new AvonicTextarea { Fill = TextareaFill.Outline, Shape = TextareaShape.Round, Label = "Round", LabelPlacement = TextareaLabelPlacement.Stacked, Placeholder = "Placeholder" },
                invalid,
                new AvonicTextarea { Label = "Disabled", IsEnabled = false, Value = "Value" },
                new AvonicTextarea { Placeholder = "Cols 20", Cols = 20 },
            },
        };
        standalone.Bind(TextElement.FontFamilyProperty, standalone.GetResourceObservable("Avonic.FontFamily"));

        return new StackPanel
        {
            Children =
            {
                new AvonicList
                {
                    Items =
                    {
                        Item(new AvonicTextarea { Label = "Start", Placeholder = "Placeholder" }),
                        Item(new AvonicTextarea { Label = "Floating", LabelPlacement = TextareaLabelPlacement.Floating }),
                        Item(new AvonicTextarea { Label = "Stacked", LabelPlacement = TextareaLabelPlacement.Stacked, Value = "Two\nlines" }),
                        Item(new AvonicTextarea { Label = "Fixed", LabelPlacement = TextareaLabelPlacement.Fixed, Rows = 3, Value = "A fixed label, three rows, and a value that wraps onto the next line" }),
                        Item(new AvonicTextarea { Label = "Auto grow", LabelPlacement = TextareaLabelPlacement.Floating, AutoGrow = true, Value = "One\ntwo\nthree" }),
                    },
                },
                standalone,
            },
        };
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var page = Page();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-textarea-page.png");

        Assert.True(page.Bounds.Height > 0);
    });

    private static void Save(Scene scene, string name)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Frames.Save(frame!, name);
    }
}
