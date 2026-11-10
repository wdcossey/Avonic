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
public class AvonicInputCaptureTests
{
    /// <summary>The same content as the snippet at the top of docs/components/ion-input/COMPARISON.md.</summary>
    internal static StackPanel Page()
    {
        static AvonicItem Item(AvonicInput input) => new() { Content = input };

        var invalid = new AvonicInput
        {
            Fill = InputFill.Solid, Label = "Invalid", LabelPlacement = InputLabelPlacement.Floating, HelperText = "Helper",
            ErrorText = "Error text", Value = "x",
        };
        invalid.Classes.Add(AvonicInput.TouchedClass);
        invalid.Classes.Add(AvonicInput.InvalidClass);

        var standalone = new StackPanel
        {
            Margin = new Thickness(16, 0),
            Spacing = 10,
            [TextElement.FontSizeProperty] = 16.0,
            Children =
            {
                new AvonicInput { Fill = InputFill.Solid, Label = "Solid", LabelPlacement = InputLabelPlacement.Floating, HelperText = "Helper text", Counter = true, Maxlength = 20 },
                new AvonicInput { Fill = InputFill.Outline, Label = "Outline", LabelPlacement = InputLabelPlacement.Floating, Value = "Value" },
                new AvonicInput { Fill = InputFill.Outline, Shape = InputShape.Round, Label = "Round", LabelPlacement = InputLabelPlacement.Stacked, Placeholder = "Placeholder" },
                new AvonicInput { Label = "No fill", LabelPlacement = InputLabelPlacement.Stacked, HelperText = "Helper", Value = "Value" },
                invalid,
                new AvonicInput { Label = "Disabled", IsEnabled = false, Value = "Value" },
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
                        Item(new AvonicInput { Label = "Start", Placeholder = "Placeholder" }),
                        Item(new AvonicInput { Label = "Floating", LabelPlacement = InputLabelPlacement.Floating }),
                        Item(new AvonicInput { Label = "Floating", LabelPlacement = InputLabelPlacement.Floating, Value = "Value" }),
                        Item(new AvonicInput { Label = "Stacked", LabelPlacement = InputLabelPlacement.Stacked, Placeholder = "Placeholder" }),
                        Item(new AvonicInput { Label = "Fixed label that is long", LabelPlacement = InputLabelPlacement.Fixed, Value = "Value" }),
                        Item(new AvonicInput { Label = "Clear", ClearInput = true, Value = "Value" }),
                        Item(new AvonicInput { Label = "Password", Type = InputType.Password, Value = "secret", EndContent = new AvonicInputPasswordToggle() }),
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
        Save(scene, "avonic-input-page.png");

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
