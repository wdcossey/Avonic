using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicTabsCaptureTests
{
    // ── Builders the other tests share ──────────────────────────────────────────────────────

    /// <summary>A tab button with an icon, a label and a badge, each left out when null; "" is an empty badge.</summary>
    internal static AvonicTabButton Button(string tab, string? label, string? icon = null, string? badge = null, TabButtonLayout layout = TabButtonLayout.IconTop)
    {
        var button = new AvonicTabButton { Tab = tab, Layout = layout };
        if (icon is not null)
        {
            button.Items.Add(new AvonicIcon { IconName = icon });
        }

        if (label is not null)
        {
            button.Items.Add(new AvonicLabel { Content = label });
        }

        if (badge is not null)
        {
            button.Items.Add(new AvonicBadge { Content = badge == "" ? null : badge });
        }

        return button;
    }

    internal static AvonicTabBar Bar(string? selected, params AvonicTabButton[] buttons)
    {
        var bar = new AvonicTabBar { SelectedTab = selected };
        foreach (var button in buttons)
        {
            bar.Items.Add(button);
        }

        return bar;
    }

    internal static AvonicTab Page(string tab, string text) => new() { Tab = tab, Content = new TextBlock { Text = text, Margin = new Thickness(16), FontSize = 16 } };

    internal static AvonicTabs Tabs(AvonicTabBar? bottom, params AvonicTab[] tabs)
    {
        var panel = new Panel();
        foreach (var tab in tabs)
        {
            panel.Children.Add(tab);
        }

        return new AvonicTabs { Content = panel, BottomContent = bottom };
    }

    // The same content as the --html at the top of docs/components/ion-tabs/COMPARISON.md.
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var disabled = Button("c", "Map", "map");
        disabled.IsEnabled = false;
        var primary = Bar("a", Button("a", "Primary", "calendar"), Button("b", "Speakers", "person-circle"), Button("c", "Map", "map"));
        primary.Color = "primary";
        var rtl = Bar("a", Button("a", "RTL", "calendar", "6"), Button("b", "Map", "map"));
        rtl.FlowDirection = FlowDirection.RightToLeft;
        var top = new AvonicTabs
        {
            TopContent = Bar(null, Button("one", "One"), Button("two", "Two")),
            Content = new Panel { Children = { Page("one", "A top tab bar"), Page("two", "Two") } },
        };

        var page = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0, 8),
            Children =
            {
                Bar("a", Button("a", "Schedule", "calendar", "6"), Button("b", "Speakers", "person-circle", ""), disabled),
                Bar("a", Button("a", "Start", "calendar", "6", TabButtonLayout.IconStart), Button("b", "End", "person-circle", "6", TabButtonLayout.IconEnd),
                    Button("c", "Bottom", "map", "6", TabButtonLayout.IconBottom)),
                Bar("b", Button("a", "Icon hide", "calendar", "6", TabButtonLayout.IconHide), Button("b", "Label hide", "person-circle", "6", TabButtonLayout.LabelHide),
                    Button("c", "Label only, long enough to wrap"), Button("d", null, "map", "6")),
                primary,
                rtl,
                new Border
                {
                    Height = 200,
                    Child = Tabs(Bar(null, Button("home", "Listen now", "play-circle"), Button("radio", "Radio", "radio")),
                        Page("home", "Home, the first tab, selected at load"), Page("radio", "Radio")),
                },
                new Border { Height = 120, Child = top },
            },
        };
        foreach (var child in page.Children.OfType<Control>())
        {
            child.VerticalAlignment = VerticalAlignment.Top;
        }

        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, "avonic-tabs-page.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        // Measured in Ionic: each bar 57 high, 56 inside a 1 px border.
        Assert.Equal(57, page.Children[0].Bounds.Height);
        Assert.Equal(57, page.Children[3].Bounds.Height);
    });
}
