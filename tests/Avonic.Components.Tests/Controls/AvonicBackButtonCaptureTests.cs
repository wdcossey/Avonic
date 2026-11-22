using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The frame for the compare step, beside the Chromium frame in docs/components/ion-back-button/ref/ (its command in COMPARISON.md),
/// and its boxes against <c>ref/dump.json</c>. With <c>AVONIC_CAPTURE_DIR</c> set it's saved.
/// </summary>
public class AvonicBackButtonCaptureTests
{
    private static AvonicToolbar Toolbar(string title, string? color, params AvonicBackButton[] buttons)
    {
        var start = new AvonicButtons();
        foreach (var button in buttons)
        {
            start.Items.Add(button);
        }

        return new AvonicToolbar { Color = color, StartContent = start, Content = new AvonicTitle { Content = title } };
    }

    private static Rect InWindow(Visual visual, Window window) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(window)!.Value);

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            var plain = new AvonicBackButton { DefaultHref = "/home" };
            var text = new AvonicBackButton { DefaultHref = "/inbox", Text = "Inbox" };
            var danger = new AvonicBackButton { DefaultHref = "/home", Color = "danger" };
            var close = new AvonicBackButton { DefaultHref = "/home", Icon = "close" };
            var disabled = new AvonicBackButton { DefaultHref = "/home", IsEnabled = false };
            var primary = new AvonicBackButton { DefaultHref = "/home" };
            var rtl = new AvonicBackButton { DefaultHref = "/home" };
            var rtlText = new AvonicBackButton { DefaultHref = "/home", Text = "Inbox" };
            var root = new StackPanel
            {
                Background = Brushes.White,
                Children =
                {
                    Toolbar("Default href", null, plain),
                    Toolbar("With text", null, text),
                    Toolbar("Danger, close, disabled", null, danger, close, disabled),
                    Toolbar("Primary toolbar", "primary", primary),
                    new Panel { FlowDirection = FlowDirection.RightToLeft, Children = { Toolbar("RTL", null, rtl, rtlText) } },
                },
            };
            TextElement.SetFontSize(root, 16);
            using var scene = new Scene(root);
            scene.Window.Background = Brushes.White;
            scene.Window.SetRenderScaling(2);
            for (var i = 0; i < 2; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            Frames.Save(scene.Window.CaptureRenderedFrame()!, "avonic-ion-back-button-page.png");

            // ref/dump.json: the boxes of the eight back buttons, and the text's.
            var w = scene.Window;
            Assert.Equal(new Rect(4, 4, 48, 48), InWindow(plain, w));
            Assert.Equal(new Rect(16, 16, 24, 24), InWindow(plain.GetVisualDescendants().OfType<AvonicIcon>().Single(), w));
            var textBox = InWindow(text, w);
            Assert.Equal(new Point(4, 68), textBox.Position);
            Assert.Equal(32, textBox.Height);
            Assert.InRange(textBox.Width, 96, 98);
            Assert.Equal(new Rect(4, 116, 48, 48), InWindow(danger, w));
            Assert.Equal(new Rect(52, 116, 48, 48), InWindow(close, w));
            Assert.Equal(new Rect(100, 116, 48, 48), InWindow(disabled, w));
            Assert.Equal(new Rect(4, 172, 48, 48), InWindow(primary, w));
            Assert.Equal(new Rect(338, 228, 48, 48), InWindow(rtl, w));
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    });
}
