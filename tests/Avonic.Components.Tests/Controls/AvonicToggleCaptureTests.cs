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
public class AvonicToggleCaptureTests
{
    /// <summary>The same content as the snippet at the top of docs/components/ion-toggle/COMPARISON.md.</summary>
    internal static StackPanel Page()
    {
        static AvonicItem Item(AvonicToggle toggle) => new() { Content = toggle };

        var invalid = new AvonicToggle { Content = "Invalid", HelperText = "Helper text", ErrorText = "Error text" };
        invalid.Classes.Add(AvonicToggle.TouchedClass);
        invalid.Classes.Add(AvonicToggle.InvalidClass);

        var page = new StackPanel
        {
            Children =
            {
                new AvonicList
                {
                    Items =
                    {
                        Item(new AvonicToggle { Content = "Default" }),
                        Item(new AvonicToggle { Content = "Checked", Checked = true }),
                        Item(new AvonicToggle { Content = "Danger", Checked = true, Color = "danger" }),
                        Item(new AvonicToggle { Content = "Disabled", IsEnabled = false }),
                        Item(new AvonicToggle { Content = "Checked and disabled", Checked = true, IsEnabled = false }),
                        Item(new AvonicToggle { Content = "Label at the end", LabelPlacement = ToggleLabelPlacement.End }),
                        Item(new AvonicToggle { Content = "A fixed label that is far too long", LabelPlacement = ToggleLabelPlacement.Fixed }),
                        Item(new AvonicToggle { Content = "Stacked", LabelPlacement = ToggleLabelPlacement.Stacked, Alignment = ToggleAlignment.Start }),
                        Item(new AvonicToggle { Content = "On/off labels", EnableOnOffLabels = true }),
                        Item(new AvonicToggle { Content = "On/off labels, checked", EnableOnOffLabels = true, Checked = true }),
                        Item(new AvonicToggle { Content = "Helper text", HelperText = "Helper text" }),
                        Item(invalid),
                    },
                },
                // Outside an item, the page's 16 px Roboto, which an item would give.
                new DockPanel
                {
                    Margin = new Thickness(16, 8),
                    [TextElement.FontSizeProperty] = 16.0,
                    Children =
                    {
                        new AvonicToggle { [DockPanel.DockProperty] = Dock.Left, VerticalAlignment = VerticalAlignment.Center },
                        new AvonicToggle { [DockPanel.DockProperty] = Dock.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0), Checked = true },
                        new AvonicToggle { Content = "Justify end", Justify = ToggleJustify.End, VerticalAlignment = VerticalAlignment.Center },
                    },
                },
                new AvonicItem { FlowDirection = FlowDirection.RightToLeft, Content = new AvonicToggle { Content = "RTL", Checked = true } },
            },
        };
        var row = page.Children[1];
        row.Bind(TextElement.FontFamilyProperty, row.GetResourceObservable("Avonic.FontFamily"));
        return page;
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var page = Page();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-toggle-page.png");

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
