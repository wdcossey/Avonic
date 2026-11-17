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
/// Real frames and boxes for the compare step, beside Ionic's in docs/components/ion-alert/ref/, which are measured on the same
/// phone (390 x 844) and a 1024 x 768 tablet: <c>ref/layout.json</c>. With <c>AVONIC_CAPTURE_DIR</c> set the frames are saved.
/// The base theme is there for the overlay layer.
/// </summary>
public class AvonicAlertCaptureTests
{
    /// <summary>Ionic's page under the alert: a header, a paragraph and a button.</summary>
    private static Control Page()
    {
        var page = AvonicModalCaptureTests.PageOf("Page", "Under the alert");
        TextElement.SetFontSize(page, 16);
        return page;
    }

    internal static void Run(Action<Scene, TestClock> test, double width = 390, double height = 844)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                using var clock = new TestClock();
                using var scene = new Scene(Page(), width, height);
                scene.Window.Background = Brushes.White;
                scene.Window.SetRenderScaling(2);
                Dispatcher.UIThread.RunJobs();
                test(scene, clock);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    internal static void Advance(TestClock clock, double to)
    {
        while (clock.Now.TotalMilliseconds < to)
        {
            clock.At(clock.Now.TotalMilliseconds + 1);
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void Save(Scene scene, string name)
    {
        for (var i = 0; i < 2; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        Frames.Save(scene.Window.CaptureRenderedFrame()!, name);
    }

    private static AvonicAlert Present(Scene scene, TestClock clock, AvonicAlert alert)
    {
        alert.Present(scene.Window);
        // The enter animation, then a frame for the button group's wrap check.
        Advance(clock, 250);
        Dispatcher.UIThread.RunJobs();
        return alert;
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

    private static T Part<T>(AvonicAlert alert, string name) where T : Control =>
        alert.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    [AvaloniaFact]
    public void Reference_Basic() => Run((scene, clock) =>
    {
        var alert = Present(scene, clock, new AvonicAlert
        {
            Header = "Alert",
            SubHeader = "Important message",
            Message = "This is an alert!",
            Buttons = ["Cancel", "OK"],
        });
        Save(scene, "avonic-alert-page.png");

        // M4, ref/layout.json "basic": the subtitle line is 19 here, 19.2 with Ionic's typography.css.
        Near([70, 333.01, 250, 178], Box(alert.Wrapper!, scene), 0.5);
        Near([94, 352.91, 202, 24], Box(Part<TextBlock>(alert, "PART_Title"), scene), 0.5);
        Near([94, 381.91, 202, 19.19], Box(Part<TextBlock>(alert, "PART_SubTitle"), scene), 0.5);
        Near([70, 416.09, 250, 39], Box(Part<ScrollViewer>(alert, "PART_Message"), scene), 0.5);
        Near([70, 455.09, 250, 56], Box(alert.ButtonGroup!, scene), 0.5);
        Near([184.53, 463.09, 72.97, 40], Box(alert.ButtonControls[0], scene), 1);
        Near([265.5, 463.09, 38.5, 40], Box(alert.ButtonControls[1], scene), 1);
    });

    [AvaloniaFact]
    public void Reference_Radios() => Run((scene, clock) =>
    {
        var alert = Present(scene, clock, new AvonicAlert
        {
            Header = "Select a color",
            Buttons = ["Cancel", "OK"],
            Inputs =
            [
                new AlertInput { Type = AlertInputType.Radio, Label = "Red", Value = "red" },
                new AlertInput { Type = AlertInputType.Radio, Label = "Blue", Value = "blue", Checked = true },
                new AlertInput { Type = AlertInputType.Radio, Label = "Green", Value = "green" },
                new AlertInput { Type = AlertInputType.Radio, Label = "Disabled", Value = "x", Disabled = true },
            ],
        });
        Save(scene, "avonic-alert-radios.png");

        // M9, ref/layout.json "radios".
        Near([70, 267.5, 250, 309], Box(alert.Wrapper!, scene));
        Near([70, 326.5, 250, 194], Box(Part<Border>(alert, "PART_InputGroup"), scene));
        var options = alert.InputControls;
        Near([70, 327.5, 250, 48], Box(options[0], scene));
        Near([94, 341.5, 20, 20], Box(Part<Border>((AvonicAlert)alert, "PART_Icon"), scene));
        Near([70, 471.5, 250, 48], Box(options[3], scene));
        Near([70, 520.5, 250, 56], Box(alert.ButtonGroup!, scene));
    });

    [AvaloniaFact]
    public void Reference_Checkboxes() => Run((scene, clock) =>
    {
        var alert = Present(scene, clock, new AvonicAlert
        {
            Header = "Toppings",
            Buttons = ["Cancel", "OK"],
            Inputs =
            [
                new AlertInput { Type = AlertInputType.Checkbox, Label = "Cheese", Value = "cheese", Checked = true },
                new AlertInput { Type = AlertInputType.Checkbox, Label = "Olives", Value = "olives" },
                new AlertInput { Type = AlertInputType.Checkbox, Label = "A topping with a really long name that wraps around", Value = "long" },
                new AlertInput { Type = AlertInputType.Checkbox, Label = "Disabled", Value = "x", Disabled = true },
            ],
        });
        Save(scene, "avonic-alert-checkboxes.png");

        // M10, ref/layout.json "checkboxes": a long label makes the wrapper 280.
        Near([55, 250, 280, 344], Box(alert.Wrapper!, scene));
        var options = alert.InputControls;
        Near([55, 310, 280, 48], Box(options[0], scene));
        Near([55, 406, 280, 83], Box(options[2], scene));
        Near([79, 326, 16, 16], Box(Part<Border>(alert, "PART_Icon"), scene));
    });

    [AvaloniaFact]
    public void Reference_Inputs() => Run((scene, clock) =>
    {
        var alert = Present(scene, clock, new AvonicAlert
        {
            Header = "Please enter your info",
            Buttons = ["OK"],
            Inputs =
            [
                new AlertInput { Placeholder = "Name" },
                new AlertInput { Placeholder = "Nickname (max 8 characters)", Attributes = new Dictionary<string, object?> { ["maxlength"] = 8 } },
                new AlertInput { Type = AlertInputType.Number, Placeholder = "Age" },
                new AlertInput { Type = AlertInputType.Textarea, Placeholder = "A little about yourself" },
            ],
        });
        Save(scene, "avonic-alert-inputs.png");

        // M8, ref/layout.json "inputs".
        Near([70, 241, 250, 362], Box(alert.Wrapper!, scene));
        Near([70, 300, 250, 247], Box(Part<Border>(alert, "PART_InputGroup"), scene));
        var fields = alert.InputControls.Cast<AlertTextField>().ToList();
        Near([94, 325, 202, 37], Box(fields[0].Native!.Parent as Border ?? (Visual)fields[0].Native!, scene));
        Near([94, 466, 202, 53], Box((Visual)fields[3].Native!.Parent!, scene));
    });

    [AvaloniaFact]
    public void Reference_Three_Buttons() => Run((scene, clock) =>
    {
        var alert = Present(scene, clock, new AvonicAlert { Header = "Three", Message = "Three buttons", Buttons = ["Cancel", "Maybe", "OK"] });
        Save(scene, "avonic-alert-three.png");

        // M7: a column, each button 226 wide.
        Near([70, 305, 250, 234], Box(alert.Wrapper!, scene));
        Near([78, 411, 226, 40], Box(alert.ButtonControls[0], scene));
        Near([78, 491, 226, 40], Box(alert.ButtonControls[2], scene));
    });

    [AvaloniaFact]
    public void Reference_Wrapped_Buttons() => Run((scene, clock) =>
    {
        var alert = Present(scene, clock, new AvonicAlert
        {
            Header = "Wrap",
            Message = "Two long buttons",
            Buttons = ["A rather long cancel button", "Confirm the long thing"],
        });
        Save(scene, "avonic-alert-wrap.png");

        // M5, M7: they wrapped, so a column, and the wrapper the column's width.
        Assert.True(alert.ButtonGroup!.Vertical);
        Near([63.31, 325, 263.38, 194], Box(alert.Wrapper!, scene), 3);
    });

    [AvaloniaFact]
    public void Reference_Long_Radios() => Run((scene, clock) =>
    {
        var alert = Present(scene, clock, new AvonicAlert
        {
            Header = "Many",
            Buttons = ["Cancel", "OK"],
            Inputs = Enumerable.Range(1, 20).Select(i => new AlertInput
            {
                Type = AlertInputType.Radio, Label = $"Option {i}", Value = i, Checked = i == 1,
            }).ToList(),
        });
        Save(scene, "avonic-alert-long-radios.png");

        // M6: the group capped at 266.
        Near([70, 231.5, 250, 381], Box(alert.Wrapper!, scene));
        Near([70, 290.5, 250, 266], Box(Part<Border>(alert, "PART_InputGroup"), scene));
    });

    [AvaloniaFact]
    public void Reference_Long_Message() => Run((scene, clock) =>
    {
        var text = string.Join(" ", Enumerable.Range(1, 30).Select(i => $"Line {i} of a long message that scrolls."));
        var alert = Present(scene, clock, new AvonicAlert { Header = "Long", Message = text, Buttons = ["OK"] });
        Save(scene, "avonic-alert-long-message.png");

        // M6: the message capped at 266, the wrapper 280.
        Near([55, 231.5, 280, 381], Box(alert.Wrapper!, scene));
        Near([55, 290.5, 280, 266], Box(Part<ScrollViewer>(alert, "PART_Message"), scene));
    });

    [AvaloniaFact]
    public void Reference_Tablet() => Run((scene, clock) =>
    {
        var alert = Present(scene, clock, new AvonicAlert
        {
            Header = "Many",
            Buttons = ["Cancel", "OK"],
            Inputs = Enumerable.Range(1, 20).Select(i => new AlertInput
            {
                Type = AlertInputType.Radio, Label = $"Option {i}", Value = i, Checked = i == 1,
            }).ToList(),
        });
        Save(scene, "avonic-alert-tablet.png");

        // M6: no 266 cap from 768 up; the wrapper's 560 cap shrinks the group to 445.
        Near([387, 104, 250, 560], Box(alert.Wrapper!, scene));
        Near([387, 163, 250, 445], Box(Part<Border>(alert, "PART_InputGroup"), scene));
    }, 1024, 768);
}
