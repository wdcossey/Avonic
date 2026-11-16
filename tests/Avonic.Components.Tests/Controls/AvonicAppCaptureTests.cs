using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>Real frames for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set they're also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicAppCaptureTests
{
    // The command at the top of docs/components/ion-app/COMPARISON.md renders the same scene: plain text in the app is 16, as a
    // page's is, beside an item, under a header.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var header = new AvonicHeader { Items = { new AvonicToolbar { Content = new AvonicTitle { Content = "ion-app" } } } };
        DockPanel.SetDock(header, Dock.Top);
        var text = new TextBlock { Text = "Plain text in ion-app is 16px Roboto, in the text colour.", TextWrapping = TextWrapping.Wrap };
        var content = new AvonicContent
        {
            Content = new StackPanel
            {
                Children =
                {
                    new Border { Padding = new(16), Child = text },
                    new AvonicItem { Content = new AvonicLabel { Content = "An item" } },
                },
            },
        };
        var app = new AvonicApp { Content = new DockPanel { Children = { header, content } } };

        using var scene = new Scene(app, 390, 844);
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;
        Frames.Save(frame, "avonic-app-page.png");

        Assert.Equal(16, text.FontSize);
        Assert.Equal(390, app.Bounds.Width);
    });
}
