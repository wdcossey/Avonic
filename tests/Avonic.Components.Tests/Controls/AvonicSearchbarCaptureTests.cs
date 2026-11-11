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
public class AvonicSearchbarCaptureTests
{
    /// <summary>The same content as the snippet at the top of docs/components/ion-searchbar/COMPARISON.md.</summary>
    internal static StackPanel Page()
    {
        var page = new StackPanel
        {
            [TextElement.FontSizeProperty] = 16.0,
            Children =
            {
                new AvonicSearchbar(),
                new AvonicSearchbar { Value = "Value" },
                new AvonicSearchbar { ShowCancelButton = SearchbarShowCancelButton.Always, Value = "Cancel always" },
                new AvonicSearchbar { ShowClearButton = SearchbarShowClearButton.Never, Value = "Clear never" },
                new AvonicSearchbar { Color = "primary", Value = "Primary" },
                new AvonicSearchbar { Color = "dark", Placeholder = "Dark" },
                new AvonicSearchbar { IsEnabled = false, Value = "Disabled" },
                new AvonicToolbar { Content = new AvonicSearchbar { Placeholder = "In a toolbar" } },
                new AvonicSearchbar { FlowDirection = FlowDirection.RightToLeft, ShowCancelButton = SearchbarShowCancelButton.Always, Value = "RTL" },
            },
        };
        page.Bind(TextElement.FontFamilyProperty, page.GetResourceObservable("Avonic.FontFamily"));
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
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Frames.Save(frame!, "avonic-searchbar-page.png");

        Assert.True(page.Bounds.Height > 0);
    });
}
