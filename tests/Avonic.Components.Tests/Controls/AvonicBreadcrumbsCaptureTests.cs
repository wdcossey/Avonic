using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicBreadcrumbsCaptureTests
{
    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var (page, focused) = Page();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        // class="ion-focused" in the reference: focus by keyboard.
        focused.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-breadcrumbs-page.png");

        Assert.True(focused.IsFocused);
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

    internal static AvonicBreadcrumb Crumb(string text, string? href = null) => new() { Content = text, Href = href };

    internal static AvonicIcon Icon(string name) => new() { IconName = name };

    internal static AvonicBreadcrumbs Trail(params AvonicBreadcrumb[] breadcrumbs)
    {
        var trail = new AvonicBreadcrumbs();
        foreach (var breadcrumb in breadcrumbs)
        {
            trail.Items.Add(breadcrumb);
        }

        return trail;
    }

    internal static AvonicBreadcrumbs Trail(params string[] texts) => Trail(texts.Select(text => Crumb(text)).ToArray());

    // The same content as the --html at the top of docs/components/ion-breadcrumbs/COMPARISON.md.
    private static (StackPanel Page, AvonicBreadcrumb Focused) Page()
    {
        var focused = Crumb("Focused", "#focus");

        var icons = Trail(
            new AvonicBreadcrumb { StartContent = Icon("home"), Content = "Home" },
            new AvonicBreadcrumb { Content = "Files", EndContent = Icon("folder") },
            new AvonicBreadcrumb { StartContent = Icon("document"), Content = "Report" });

        var collapsed = Trail("Home", "Electronics", "Photography", "Cameras", "Film", "35 mm");
        collapsed.MaxItems = 4;

        var afterZero = Trail("Home", "Electronics", "Photography", "Cameras");
        afterZero.MaxItems = 2;
        afterZero.ItemsAfterCollapse = 0;

        var primary = Trail("Home", "Electronics", "Primary");
        primary.Color = "primary";

        var disabled = Crumb("Disabled");
        disabled.IsEnabled = false;
        var states = Trail(new AvonicBreadcrumb { Color = "danger", Content = "Danger" }, disabled, new AvonicBreadcrumb { Active = true, Content = "Active" }, Crumb("Last"));

        var separators = Trail(
            new AvonicBreadcrumb { Content = "Custom", SeparatorContent = Icon("arrow-forward") },
            new AvonicBreadcrumb { Content = "Separator", SeparatorContent = Icon("arrow-forward") },
            Crumb("End"));

        var inPrimaryToolbar = Trail("Home", "Hidden", "Primary toolbar");
        inPrimaryToolbar.MaxItems = 2;

        var rtl = Trail(new AvonicBreadcrumb { StartContent = Icon("home"), Content = "Home" }, Crumb("Hidden"), Crumb("RTL"), Crumb("Last"));
        rtl.MaxItems = 3;
        rtl.FlowDirection = FlowDirection.RightToLeft;

        var page = new StackPanel
        {
            Margin = new Thickness(0, 8, 0, 0),
            Children =
            {
                Trail("Home", "Electronics", "Cameras"),
                Trail(Crumb("Home", "#home"), focused, Crumb("Photography", "#photo")),
                icons,
                collapsed,
                afterZero,
                primary,
                states,
                separators,
                new AvonicToolbar { Content = Trail("Home", "In a toolbar") },
                new AvonicToolbar { Color = "primary", Content = inPrimaryToolbar },
                rtl,
            },
        };

        return (page, focused);
    }
}
