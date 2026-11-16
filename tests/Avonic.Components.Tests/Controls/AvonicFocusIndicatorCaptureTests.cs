using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Real frames of Ionic 8.8.17's keyboard focus indicators, for the compare step, with <c>AVONIC_CAPTURE_DIR</c> set. Each page is
/// the snippet at the top of the focus section of the control's <c>COMPARISON.md</c>, where <c>class="ion-focused"</c> is on
/// what keyboard focus marks, measured: the control, when it's <c>ion-focusable</c>, and the item, when it is.
/// </summary>
public class AvonicFocusIndicatorCaptureTests
{
    private static T Focused<T>(T control) where T : Control
    {
        control.Tag = "focused";
        return control;
    }

    private static AvonicItem Item(object content, object? start = null, object? end = null, bool button = false, bool focused = false)
    {
        var item = new AvonicItem { Content = content, StartContent = start, EndContent = end, Button = button };
        return focused ? Focused(item) : item;
    }

    private static StackPanel Alone(params Control[] controls)
    {
        var column = new StackPanel { Margin = new Thickness(16), Spacing = 24 };
        foreach (var control in controls)
        {
            control.HorizontalAlignment = HorizontalAlignment.Left;
            column.Children.Add(control);
        }

        return column;
    }

    private static void Capture(StackPanel page, string name)
    {
        TextElement.SetFontSize(page, 16);
        page.Bind(TextElement.FontFamilyProperty, page.GetResourceObservable("Avonic.FontFamily"));
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        // ion-focused as keyboard focus gives it, after the first frame and with no transitions, so the frame is its end.
        foreach (var visual in page.GetVisualDescendants().OfType<Animatable>())
        {
            visual.Transitions = null;
        }

        foreach (var control in page.GetVisualDescendants().OfType<Control>().Where(c => Equals(c.Tag, "focused")))
        {
            ((IPseudoClasses)control.Classes).Add(FocusVisible.FocusedPseudoClass);
        }

        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Frames.Save(frame!, name);
        Assert.True(page.Bounds.Height > 0);
    }

    [AvaloniaFact]
    public void Checkbox_Focus_Page() => InstalledTheme.Run(_ => Capture(
        new StackPanel
        {
            Children =
            {
                Alone(
                    Focused(new AvonicCheckbox { Content = "Unchecked" }),
                    Focused(new AvonicCheckbox { Content = "Checked", Checked = true }),
                    Focused(new AvonicCheckbox { Content = "Danger", Checked = true, Color = "danger" }),
                    Focused(new AvonicCheckbox { Content = "Label at the end", LabelPlacement = CheckboxLabelPlacement.End }),
                    new Panel { FlowDirection = FlowDirection.RightToLeft, Children = { Focused(new AvonicCheckbox { Content = "RTL", HorizontalAlignment = HorizontalAlignment.Left }) } }),
                new AvonicList
                {
                    Items =
                    {
                        Item(new AvonicCheckbox { Content = "Single input: the item draws it" }, focused: true),
                        Item(Focused(new AvonicCheckbox { Content = "Multi-input" }), start: new AvonicCheckbox()),
                        Item(Focused(new AvonicCheckbox { Content = "Clickable item" }), button: true, focused: true),
                        Item(Focused(new AvonicCheckbox { Content = "Label at the end, clipped", LabelPlacement = CheckboxLabelPlacement.End }), end: new AvonicCheckbox()),
                        Item(new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Children = { Focused(new AvonicCheckbox { Content = "Wrapped" }) } }, end: new AvonicCheckbox()),
                        Item(new AvonicLabel { Content = "Start slot" }, start: Focused(new AvonicCheckbox()), end: new AvonicCheckbox()),
                    },
                },
            },
        },
        "avonic-checkbox-focus.png"));

    [AvaloniaFact]
    public void Toggle_Focus_Page() => InstalledTheme.Run(_ => Capture(
        new StackPanel
        {
            Children =
            {
                Alone(
                    Focused(new AvonicToggle { Content = "Unchecked" }),
                    Focused(new AvonicToggle { Content = "Checked", Checked = true }),
                    Focused(new AvonicToggle { Content = "Danger, checked", Checked = true, Color = "danger" }),
                    Focused(new AvonicToggle { Content = "Danger", Color = "danger" }),
                    new Panel { FlowDirection = FlowDirection.RightToLeft, Children = { Focused(new AvonicToggle { Content = "RTL", HorizontalAlignment = HorizontalAlignment.Left }) } },
                    new Panel { FlowDirection = FlowDirection.RightToLeft, Children = { Focused(new AvonicToggle { Content = "RTL, checked", Checked = true, HorizontalAlignment = HorizontalAlignment.Left }) } }),
                new AvonicList
                {
                    Items =
                    {
                        Item(Focused(new AvonicToggle { Content = "Single input: the item draws it" }), focused: true),
                        Item(Focused(new AvonicToggle { Content = "Multi-input" }), start: new AvonicCheckbox(), focused: true),
                        Item(Focused(new AvonicToggle { Content = "Clickable item, checked", Checked = true }), button: true, focused: true),
                        Item(Focused(new AvonicToggle { Content = "Label at the end", LabelPlacement = ToggleLabelPlacement.End }), end: new AvonicToggle(), focused: true),
                        Item(Focused(new AvonicToggle { Content = "With a button: single input" }), end: new AvonicButton { Content = "B" }, focused: true),
                    },
                },
            },
        },
        "avonic-toggle-focus.png"));

    [AvaloniaFact]
    public void Radio_Focus_Page() => InstalledTheme.Run(_ =>
    {
        var alone = new AvonicRadioGroup
        {
            Value = "x",
            Items =
            {
                new StackPanel
                {
                    Margin = new Thickness(16),
                    Spacing = 24,
                    Children =
                    {
                        Focused(new AvonicRadio { Value = "y", Content = "Unchecked" }),
                        Focused(new AvonicRadio { Value = "x", Content = "Checked, success", Color = "success" }),
                        new Panel { FlowDirection = FlowDirection.RightToLeft, HorizontalAlignment = HorizontalAlignment.Left, Children = { Focused(new AvonicRadio { Value = "z", Content = "RTL" }) } },
                    },
                },
            },
        };
        var items = new AvonicRadioGroup
        {
            Value = "a",
            Items =
            {
                Item(new AvonicRadio { Value = "a", Content = "Single input: the item draws it" }, focused: true),
                Item(Focused(new AvonicRadio { Value = "b", Content = "Multi-input" }), end: new AvonicCheckbox()),
                Item(Focused(new AvonicRadio { Value = "c", Content = "Clickable item" }), button: true, focused: true),
                Item(Focused(new AvonicRadio { Value = "d", Content = "Label at the end", LabelPlacement = RadioLabelPlacement.End }), end: new AvonicButton { Content = "B" }, focused: true),
            },
        };
        Capture(new StackPanel { Children = { alone, new AvonicList { Items = { items } } } }, "avonic-radio-focus.png");
    });

    [AvaloniaFact]
    public void Item_Focus_Page() => InstalledTheme.Run(_ => Capture(
        new StackPanel
        {
            Children =
            {
                new AvonicList
                {
                    Items =
                    {
                        Item(Focused(new AvonicCheckbox { Content = "Room: clipped at the start", LabelPlacement = CheckboxLabelPlacement.End }), end: new AvonicToggle(), focused: true),
                        Item(Focused(new AvonicCheckbox { Content = "Room: shown at the end" }), start: new AvonicCheckbox()),
                        Item(new AvonicLabel { Content = "No room: slots only" }, start: new AvonicCheckbox(), end: Focused(new AvonicToggle()), focused: true),
                        Item(new AvonicLabel { Content = "No room: clickable, end slot" }, end: Focused(new AvonicCheckbox()), button: true, focused: true),
                        Item(Focused(new AvonicToggle { Content = "Room: clickable", Checked = true }), button: true, focused: true),
                    },
                },
                new AvonicList
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    Items =
                    {
                        Item(Focused(new AvonicCheckbox { Content = "RTL multi" }), end: new AvonicCheckbox()),
                        Item(Focused(new AvonicToggle { Content = "RTL multi", Checked = true }), end: new AvonicToggle(), focused: true),
                    },
                },
            },
        },
        "avonic-item-focus.png"));
}
