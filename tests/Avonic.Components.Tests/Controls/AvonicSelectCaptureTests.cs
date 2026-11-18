using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Overlays;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames for the compare step, beside <c>docs/components/ion-select/ref/</c>, each asserting the measured boxes. With
/// <c>AVONIC_CAPTURE_DIR</c> set they're also saved.
/// </summary>
public class AvonicSelectCaptureTests
{
    /// <summary>The options of every select on the page: Apple, Banana, Cherry (disabled), Grape.</summary>
    internal static AvonicSelectOption[] Options() =>
    [
        new() { Value = "apple", Content = "Apple" },
        new() { Value = "banana", Content = "Banana" },
        new() { Value = "cherry", Content = "Cherry", IsEnabled = false },
        new() { Value = "grape", Content = "Grape" },
    ];

    internal static AvonicSelect Select(string name, Action<AvonicSelect>? set = null)
    {
        var select = new AvonicSelect { Name = name };
        select.Items.AddRange(Options());
        set?.Invoke(select);
        return select;
    }

    /// <summary>The page <c>measure.js</c> renders, in docs/components/ion-select/COMPARISON.md.</summary>
    internal static StackPanel Page(Action<AvonicSelect>? first = null)
    {
        var error = Select("s9", s =>
        {
            s.Label = "Error";
            s.ErrorText = "Required";
            s.HelperText = "Helper";
        });
        error.Classes.Add(AvonicSelect.TouchedClass);
        error.Classes.Add(AvonicSelect.InvalidClass);

        var standalone = new StackPanel
        {
            Margin = new Thickness(16, 0),
            [TextElement.FontSizeProperty] = 16.0,
            Children =
            {
                Select("s5", s => { s.Label = "Floating"; s.LabelPlacement = SelectLabelPlacement.Floating; }),
                Select("s6", s => { s.Label = "Stacked"; s.LabelPlacement = SelectLabelPlacement.Stacked; s.Placeholder = "Pick one"; }),
                Select("s7", s =>
                {
                    s.Label = "Solid";
                    s.LabelPlacement = SelectLabelPlacement.Floating;
                    s.Fill = SelectFill.Solid;
                    s.HelperText = "Helper text";
                    s.Value = "apple";
                }),
                new Border { Height = 8 },
                Select("s8", s => { s.Label = "Outline"; s.LabelPlacement = SelectLabelPlacement.Floating; s.Fill = SelectFill.Outline; s.Value = "banana"; }),
                new Border { Height = 8 },
                error,
            },
        };
        standalone.Bind(TextElement.FontFamilyProperty, standalone.GetResourceObservable("Avonic.FontFamily"));

        static AvonicItem Item(AvonicSelect select) => new() { Content = select };

        return new StackPanel
        {
            Children =
            {
                new AvonicList
                {
                    Items =
                    {
                        Item(Select("s1", s => { s.Label = "Fruit"; s.Placeholder = "Pick one"; s.Value = "apple"; first?.Invoke(s); })),
                        Item(Select("s2", s => { s.Label = "Placeholder"; s.Placeholder = "Pick one"; })),
                        Item(Select("s3", s => { s.Label = "Multiple"; s.Multiple = true; })),
                        Item(Select("s4", s => { s.Label = "Disabled"; s.IsEnabled = false; s.Value = "apple"; })),
                    },
                },
                standalone,
            },
        };
    }

    internal static AvonicSelect Find(Control page, string name) =>
        page.GetLogicalDescendants().Select(element => element is AvonicItem { Content: AvonicSelect inItem } ? inItem : element)
            .OfType<AvonicSelect>().Distinct().Single(select => select.Name == name);

    internal static Rect Box(Visual visual, Visual relativeTo) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(relativeTo)!.Value);

    internal static T Part<T>(AvonicSelect select, string name) where T : Control =>
        select.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        var page = Page();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        Save(scene, "avonic-select-page.png");

        // ref/page.json: each in an item 358 x 44 at 9.5 down its row; the label at 16, the text at its end, the icon 13 x 16.
        var s1 = Find(page, "s1");
        AssertBox(16, 9.5, 358, 44, Box(s1, page));
        AssertBox(361, 23.5, 13, 16, Box(Part<AvonicIcon>(s1, "PART_Icon"), page));
        Assert.Equal(316.22, Box(Part<Control>(s1, "PART_Container"), page).X, 0.6);

        // An empty text keeps 2ch: the container 34.98 wide, from 339.02.
        Assert.Equal(339.02, Box(Part<Control>(Find(page, "s3"), "PART_Container"), page).X, 0.6);

        // Floating, stacked, solid with helper text, outline, error: 56, 56, 75, 56, 64 high.
        AssertBox(16, 208, 358, 56, Box(Find(page, "s5"), page));
        AssertBox(16, 264, 358, 56, Box(Find(page, "s6"), page));
        AssertBox(16, 320, 358, 75, Box(Find(page, "s7"), page));
        AssertBox(16, 403, 358, 56, Box(Find(page, "s8"), page));
        AssertBox(16, 467, 358, 64, Box(Find(page, "s9"), page));

        // The floating label at rest: 19 down, on the field; stacked: scaled to 14.25 at 9.5 down.
        AssertBox(16, 227, 57.52, 19, Box(Part<Control>(Find(page, "s5"), "PART_Label"), page), 1);
        AssertBox(16, 273.5, 42.96, 14.25, Box(Part<Control>(Find(page, "s6"), "PART_Label"), page), 1);
        // The end icon of a floating label: 361, 228.
        AssertBox(361, 228, 13, 16, Box(Part<AvonicIcon>(Find(page, "s5"), "PART_EndIcon"), page));
    });

    /// <summary>
    /// Opens <paramref name="name"/> on the page by a tap, waits for its interface, and saves the frame, as <c>measure.js</c> does.
    /// </summary>
    private static (AvonicPopoverTests.Stage Stage, AvonicOverlay Overlay) Opened(string file, string name, Action<AvonicSelect> set)
    {
        var stage = new AvonicPopoverTests.Stage();
        stage.Window.Background = Brushes.White;
        stage.Window.SetRenderScaling(2);
        var page = Page();
        page.Width = 390;
        set(Find(page, name));
        // In an ion-app, as measure.js's page, so that the chosen option shows focused (focusVisibleElement).
        stage.Page.Children.Add(new AvonicApp { Content = page });
        stage.Render();
        var select = Find(page, name);
        var centre = select.TranslatePoint(new Point(select.Bounds.Width / 2, select.Bounds.Height / 2), stage.Window)!.Value;
        stage.Click(centre);
        stage.After(600);

        // A finger leaves no pointer over what opened, as measure.js's tap doesn't; the mouse here would hover it.
        stage.Move(new Point(389, 843));
        stage.Render();
        Save(stage.Scene, file);
        return (stage, select.Overlay!);
    }

    [AvaloniaFact]
    public void Interface_Popover()
    {
        var (stage, overlay) = Opened("avonic-select-popover.png", "s1", s => s.Interface = SelectInterface.Popover);
        using (stage)
        {
            // ref/popover.json: 250 x 208 at 128, 41; the checked item's background primary at 0.08.
            AssertBox(128, 41, 250, 208, AvonicPopoverTests.Box((AvonicPopover)overlay, stage.Window), 1);
        }
    }

    [AvaloniaFact]
    public void Interface_Popover_Multiple()
    {
        var (stage, overlay) = Opened("avonic-select-popover-multiple.png", "s3", s =>
        {
            s.Interface = SelectInterface.Popover;
            s.Value = new object?[] { "apple" };
        });
        using (stage)
        {
            AssertBox(128, 137, 250, 208, AvonicPopoverTests.Box((AvonicPopover)overlay, stage.Window), 1);
            // The label after the checkbox, at 178 (ref/popoverMultiple.json).
            var label = overlay.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Apple");
            Assert.Equal(178, Box(label, stage.Window).X, 1);
        }
    }

    [AvaloniaFact]
    public void Interface_Popover_Header()
    {
        var (stage, overlay) = Opened("avonic-select-popover-header.png", "s1", s =>
        {
            s.Interface = SelectInterface.Popover;
            s.InterfaceOptions = new Dictionary<string, object?> { ["header"] = "Fruits", ["subHeader"] = "Pick one", ["message"] = "Only one" };
        });
        using (stage)
        {
            // ref/popoverHeader.json: 301 high; the header 45, the message's item 48.
            AssertBox(128, 41, 250, 301, AvonicPopoverTests.Box((AvonicPopover)overlay, stage.Window), 1.5);
        }
    }

    [AvaloniaFact]
    public void Interface_Popover_Description()
    {
        var (stage, overlay) = Opened("avonic-select-popover-description.png", "s1", s =>
        {
            s.Interface = SelectInterface.Popover;
            s.Items[1].Description = "Yellow and long";
        });
        using (stage)
        {
            AssertBox(28, 41, 350, 208, AvonicPopoverTests.Box((AvonicPopover)overlay, stage.Window), 1);
            var description = overlay.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Yellow and long");
            Assert.Equal(44, Box(description, stage.Window).X, 1);
            // Its box at 121 with padding-top 5, so its text at 126.
            Assert.Equal(126, Box(description, stage.Window).Y, 1);
        }
    }

    [AvaloniaFact]
    public void Interface_Popover_Outline()
    {
        var (stage, overlay) = Opened("avonic-select-popover-outline.png", "s8", s => s.Interface = SelectInterface.Popover);
        using (stage)
        {
            // Cover: the select's width, under it (ref/popoverOutline.json: 16, 459, 358 x 208).
            AssertBox(16, 459, 358, 208, AvonicPopoverTests.Box((AvonicPopover)overlay, stage.Window), 1);
        }
    }

    [AvaloniaFact]
    public void Interface_Modal()
    {
        var (stage, overlay) = Opened("avonic-select-modal.png", "s1", s => s.Interface = SelectInterface.Modal);
        using (stage)
        {
            // ref/modal.json: the toolbar 56 high with Cancel at 309.98, 12; the items from 64.
            var cancel = overlay.GetVisualDescendants().OfType<AvonicButton>().Single(b => b.Name == "PART_Cancel");
            AssertBox(309.98, 12, 74.02, 32, Box(cancel, stage.Window), 1.5);
            var first = overlay.GetVisualDescendants().OfType<AvonicItem>().First(i => i.Classes.Contains(AvonicSelect.OptionClass));
            AssertBox(0, 64, 390, 48, Box(first, stage.Window), 1);
        }
    }

    [AvaloniaFact]
    public void Interface_Modal_Multiple()
    {
        var (stage, overlay) = Opened("avonic-select-modal-multiple.png", "s3", s =>
        {
            s.Interface = SelectInterface.Modal;
            s.Value = new object?[] { "apple" };
        });
        using (stage)
        {
            var label = overlay.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Apple");
            Assert.Equal(50, Box(label, stage.Window).X, 1);
            Assert.Equal(Color.FromRgb(0, 84, 233), ((ISolidColorBrush)label.Foreground!).Color);
        }
    }

    [AvaloniaFact]
    public void Interface_Alert()
    {
        var (stage, overlay) = Opened("avonic-select-alert.png", "s1", _ => { });
        using (stage)
        {
            Assert.IsType<AvonicAlert>(overlay);
        }
    }

    [AvaloniaFact]
    public void Interface_Action_Sheet()
    {
        var (stage, overlay) = Opened("avonic-select-action-sheet.png", "s1", s => s.Interface = SelectInterface.ActionSheet);
        using (stage)
        {
            Assert.IsType<AvonicActionSheet>(overlay);
        }
    }

    internal static void AssertBox(double x, double y, double width, double height, Rect actual, double tolerance = 0.6)
    {
        Assert.True(Math.Abs(actual.X - x) <= tolerance && Math.Abs(actual.Y - y) <= tolerance
                    && Math.Abs(actual.Width - width) <= tolerance && Math.Abs(actual.Height - height) <= tolerance,
            $"expected {x}, {y}, {width} x {height}; was {actual.X:0.##}, {actual.Y:0.##}, {actual.Width:0.##} x {actual.Height:0.##}");
    }

    internal static void Save(Scene scene, string name)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Frames.Save(frame!, name);
    }
}
