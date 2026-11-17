using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames and boxes for the compare step, beside Ionic's in docs/components/ion-action-sheet/ref/, which are measured on the
/// same phone (390 x 844) and a 1024 x 768 tablet: <c>ref/layout.json</c>. With <c>AVONIC_CAPTURE_DIR</c> set the frames are
/// saved. The base theme is there for the overlay layer.
/// </summary>
public class AvonicActionSheetCaptureTests
{
    /// <summary>Ionic's page under the sheet: a heading, a paragraph and a button.</summary>
    private static Control Page() => new StackPanel
    {
        Margin = new Thickness(16),
        Children =
        {
            new TextBlock { Text = "Page", FontSize = 32, FontWeight = FontWeight.Medium, Margin = new Thickness(0, 21, 0, 10) },
            new TextBlock { Text = "The page under the action sheet.", FontSize = 16, Margin = new Thickness(0, 0, 0, 16) },
            new AvonicButton { Content = "Open", HorizontalAlignment = HorizontalAlignment.Left },
        },
    };

    private static void Run(Action<Scene, TestClock> test, double width = 390, double height = 844) =>
        AvonicAlertCaptureTests.Run((scene, clock) =>
        {
            scene.Window.Content = Page();
            Dispatcher.UIThread.RunJobs();
            test(scene, clock);
        }, width, height);

    private static void Save(Scene scene, string name)
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        Frames.Save(scene.Window.CaptureRenderedFrame()!, name);
    }

    private static AvonicActionSheet Present(Scene scene, TestClock clock, AvonicActionSheet sheet)
    {
        sheet.Present(scene.Window);
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 500);
        Dispatcher.UIThread.RunJobs();
        return sheet;
    }

    private static Rect Box(Visual visual, Scene scene) => new(visual.TranslatePoint(default, scene.Window)!.Value, visual.Bounds.Size);

    private static void Near(double[] expected, Rect actual, double tolerance = 0.5)
    {
        var got = new[] { actual.X, actual.Y, actual.Width, actual.Height };
        for (var i = 0; i < 4; i++)
        {
            Assert.True(Math.Abs(expected[i] - got[i]) <= tolerance,
                $"Expected [{string.Join(", ", expected)}], got [{string.Join(", ", got.Select(v => Math.Round(v, 2)))}]");
        }
    }

    private static T Part<T>(AvonicActionSheet sheet, string name) where T : Control =>
        sheet.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static ActionSheetButton[] Icons() =>
    [
        new() { Text = "Delete", Role = "destructive", Icon = "trash" },
        new() { Text = "Share", Icon = "share" },
        new() { Text = "Play (open modal)", Icon = "caret-forward-circle" },
        new() { Text = "Favorite", Icon = "heart" },
        new() { Text = "Cancel", Role = "cancel", Icon = "close" },
    ];

    private static List<ActionSheetButton> Long() =>
    [
        .. Enumerable.Range(1, 30).Select(i => new ActionSheetButton { Text = $"Option {i}" }),
        new ActionSheetButton { Text = "Cancel", Role = "cancel" },
    ];

    [AvaloniaFact]
    public void Reference_Basic() => Run((scene, clock) =>
    {
        var sheet = Present(scene, clock, new AvonicActionSheet
        {
            Header = "Albums",
            Buttons = [new ActionSheetButton { Text = "Delete", Role = "destructive" }, "Share", new ActionSheetButton { Text = "Cancel", Role = "cancel" }],
        });
        Save(scene, "avonic-action-sheet-page.png");

        // M4, ref/layout.json "basic".
        Near([0, 628, 390, 216], Box(sheet.Wrapper!, scene));
        Near([0, 628, 390, 164], Box(Part<Border>(sheet, "PART_Group"), scene));
        Near([0, 628, 390, 60], Box(Part<Border>(sheet, "PART_Title"), scene));
        Near([0, 688, 390, 52], Box(sheet.ButtonControls[0], scene));
        Near([0, 792, 390, 52], Box(sheet.CancelControl!, scene));
    });

    [AvaloniaFact]
    public void Reference_Icons() => Run((scene, clock) =>
    {
        var sheet = Present(scene, clock, new AvonicActionSheet { Header = "Albums", SubHeader = "Choose an action", Buttons = Icons() });
        Save(scene, "avonic-action-sheet-icons.png");

        // M5.
        Near([0, 496, 390, 348], Box(sheet.Wrapper!, scene));
        Near([0, 496, 390, 88], Box(Part<Border>(sheet, "PART_Title"), scene));
        Near([16, 551, 358, 16], Box(Part<TextBlock>(sheet, "PART_SubTitle"), scene));
        Near([0, 584, 390, 52], Box(sheet.ButtonControls[0], scene));
        Near([16, 598, 24, 24], Box(sheet.ButtonControls[0].GetVisualDescendants().OfType<AvonicIcon>().Single(), scene));
        Near([16, 806, 24, 24], Box(sheet.CancelControl!.GetVisualDescendants().OfType<AvonicIcon>().Single(), scene));
        Assert.Equal(72, Box(sheet.CancelControl!.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Label"), scene).X, 1);
    });

    [AvaloniaFact]
    public void Reference_No_Header() => Run((scene, clock) =>
    {
        var sheet = Present(scene, clock, new AvonicActionSheet { Buttons = ["Share", "Copy link", "Cancel"] });
        Save(scene, "avonic-action-sheet-no-header.png");

        // M6.
        Near([0, 688, 390, 156], Box(sheet.Wrapper!, scene));
    });

    [AvaloniaFact]
    public void Reference_Long() => Run((scene, clock) =>
    {
        var sheet = Present(scene, clock, new AvonicActionSheet { Header = "Pick one", Buttons = Long() });
        Save(scene, "avonic-action-sheet-long.png");

        // M7.
        Near([0, 0, 390, 844], Box(sheet.Wrapper!, scene));
        Near([0, 0, 390, 792], Box(Part<Border>(sheet, "PART_Group"), scene));
        Near([0, 792, 390, 52], Box(sheet.CancelControl!, scene));
    });

    [AvaloniaFact]
    public void Reference_Radios() => Run((scene, clock) =>
    {
        ActionSheetButton Radio(string text, bool isChecked, bool disabled = false) => new()
        {
            Text = text,
            Disabled = disabled,
            HtmlAttributes = new Dictionary<string, object?> { ["role"] = "radio", ["aria-checked"] = isChecked ? "true" : "false" },
        };
        var sheet = Present(scene, clock, new AvonicActionSheet
        {
            Header = "Pick a fruit",
            Buttons = [Radio("Apple", false), Radio("Banana", true), Radio("Cherry", false), Radio("Durian", false, true), new ActionSheetButton { Text = "Cancel", Role = "cancel" }],
        });
        Save(scene, "avonic-action-sheet-radios.png");

        // M9: as the select's sheet, from Ionic's controller: Banana bold; Durian at 0.4.
        Near([0, 524, 390, 320], Box(sheet.Wrapper!, scene));
        Assert.Equal(FontWeight.Bold, sheet.ButtonControls[1].FontWeight);
    });

    [AvaloniaFact]
    public void Reference_Disabled() => Run((scene, clock) =>
    {
        var sheet = Present(scene, clock, new AvonicActionSheet
        {
            Buttons = ["Enabled", new ActionSheetButton { Text = "Disabled", Disabled = true }, new ActionSheetButton { Text = "Cancel", Role = "cancel", Disabled = true }],
        });
        Save(scene, "avonic-action-sheet-disabled.png");
        Near([0, 688, 390, 156], Box(sheet.Wrapper!, scene));
    });

    [AvaloniaFact]
    public void Reference_Long_Text() => Run((scene, clock) =>
    {
        var sheet = Present(scene, clock, new AvonicActionSheet
        {
            Header = "A header long enough that it has to wrap onto a second line in the sheet",
            Buttons = [new ActionSheetButton { Text = "A button whose text is long enough that it has to wrap onto another line", Icon = "heart" }, new ActionSheetButton { Text = "Cancel", Role = "cancel" }],
        });
        Save(scene, "avonic-action-sheet-long-text.png");

        // M11: the title 75; the button 56, its icon 14.78 wide, the label 32 after it.
        Near([0, 661, 390, 183], Box(sheet.Wrapper!, scene));
        Near([0, 661, 390, 75], Box(Part<Border>(sheet, "PART_Title"), scene));
        Near([0, 736, 390, 56], Box(sheet.ButtonControls[0], scene));
        var icon = sheet.ButtonControls[0].GetVisualDescendants().OfType<AvonicIcon>().Single();
        Near([16, 752, 14.78, 24], Box(icon, scene), 1);
    });

    [AvaloniaFact]
    public void Reference_Tablet() => Run((scene, clock) =>
    {
        var sheet = Present(scene, clock, new AvonicActionSheet { Header = "Albums", SubHeader = "Choose an action", Buttons = Icons() });
        Save(scene, "avonic-action-sheet-tablet.png");

        // M8.
        Near([262, 420, 500, 348], Box(sheet.Wrapper!, scene));
        Near([278, 522, 24, 24], Box(sheet.ButtonControls[0].GetVisualDescendants().OfType<AvonicIcon>().Single(), scene));
    }, 1024, 768);

    [AvaloniaFact]
    public void Reference_Long_Tablet() => Run((scene, clock) =>
    {
        var sheet = Present(scene, clock, new AvonicActionSheet { Header = "Pick one", Buttons = Long() });
        Save(scene, "avonic-action-sheet-long-tablet.png");

        // M8: 500 x 768, the group 716.
        Near([262, 0, 500, 768], Box(sheet.Wrapper!, scene));
        Near([262, 0, 500, 716], Box(Part<Border>(sheet, "PART_Group"), scene));
    }, 1024, 768);

    [AvaloniaFact]
    public void Sample_Page_Its_Xaml_Buttons_And_Trigger() => AvonicAlertCaptureTests.Run((_, clock) =>
    {
        var page = new Avonic.Sample.Pages.ActionSheetPage();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();
        var opener = page.FindControl<AvonicButton>("OpenSheet")!;
        opener.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(AvonicButton.ClickEvent));
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 500);
        Save(scene, "avonic-action-sheet-sample.png");

        var sheet = page.FindControl<AvonicActionSheet>("BasicSheet")!;
        Assert.Equal(["Delete", "Share", "Play (open modal)", "Favorite"], sheet.ButtonControls.Select(b => b.Text));
        Assert.Equal("Cancel", sheet.CancelControl!.Text);
        var share = sheet.ButtonControls[1];
        var centre = share.TranslatePoint(new Point(share.Bounds.Width / 2, share.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(centre, Avalonia.Input.MouseButton.Left);
        scene.Window.MouseUp(centre, Avalonia.Input.MouseButton.Left);
        AvonicAlertCaptureTests.Advance(clock, clock.Now.TotalMilliseconds + 500);
        Assert.Equal("BasicSheet dismissed with share, role none", page.FindControl<TextBlock>("Status")!.Text);
    });
}
