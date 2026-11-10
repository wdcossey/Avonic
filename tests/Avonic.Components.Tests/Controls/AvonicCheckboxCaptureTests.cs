using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>A real frame for the compare step. With <c>AVONIC_CAPTURE_DIR</c> set it's also saved, for <c>COMPARISON.md</c>.</summary>
public class AvonicCheckboxCaptureTests
{
    /// <summary>
    /// The same content as the scene at the top of <c>docs/components/ion-checkbox/COMPARISON.md</c>: each standalone checkbox
    /// in a block of its own, then a list of items.
    /// </summary>
    internal static (StackPanel Page, Dictionary<string, AvonicCheckbox> Boxes) Page()
    {
        var boxes = new Dictionary<string, AvonicCheckbox>();
        AvonicCheckbox Box(string id, object? content, Action<AvonicCheckbox>? set = null)
        {
            var box = new AvonicCheckbox { Content = content };
            set?.Invoke(box);
            boxes[id] = box;
            return box;
        }

        var page = new StackPanel
        {
            Children =
            {
                Box("c1", "Label start"),
                Box("c2", "Checked", b => b.Checked = true),
                Box("c3", "Indeterminate", b => b.Indeterminate = true),
                Box("c4", "Label end", b => b.LabelPlacement = CheckboxLabelPlacement.End),
                Box("c5", "Fixed label that is long", b => b.LabelPlacement = CheckboxLabelPlacement.Fixed),
                Box("c6", "Stacked", b => { b.LabelPlacement = CheckboxLabelPlacement.Stacked; b.Checked = true; }),
                Box("c7", "Space between, danger", b => { b.Justify = CheckboxJustify.SpaceBetween; b.Checked = true; b.Color = "danger"; }),
                Box("c8", "Disabled", b => { b.IsEnabled = false; b.Checked = true; }),
                Box("c9", "With helper", b => b.HelperText = "Helper text"),
                Box("c10", "Invalid", b => { b.Classes.Add("ion-touched"); b.Classes.Add("ion-invalid"); b.HelperText = "Helper text"; b.ErrorText = "Error text"; }),
                Box("c11", null),
                new AvonicList
                {
                    Items =
                    {
                        new AvonicItem { Content = Box("i1", "In an item") },
                        new AvonicItem { Content = Box("i2", "In an item, end", b => { b.Checked = true; b.LabelPlacement = CheckboxLabelPlacement.End; }) },
                        new AvonicItem { StartContent = Box("i3", null), Content = new AvonicLabel { Content = "Start slot" } },
                        new AvonicItem { Content = Box("i4", "Stacked, start", b => { b.LabelPlacement = CheckboxLabelPlacement.Stacked; b.Alignment = CheckboxAlignment.Start; }) },
                        new AvonicItem { Content = Box("i5", "In an item, helper", b => b.HelperText = "Helper text") },
                    },
                },
            },
        };
        return (page, boxes);
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var (page, boxes) = Page();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-checkbox-page.png");

        if (Environment.GetEnvironmentVariable("AVONIC_CAPTURE_DIR") is { } dir)
        {
            var lines = boxes.Select(pair =>
            {
                var box = pair.Value;
                var at = box.TranslatePoint(default, page)!.Value;
                return $"{pair.Key}: host {at.X:0.##} {at.Y:0.##} {box.Bounds.Width:0.##} {box.Bounds.Height:0.##}";
            });
            File.WriteAllLines(Path.Combine(dir, "avonic-checkbox-boxes.txt"), lines);
        }

        Assert.True(page.Bounds.Height > 0);
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
}
