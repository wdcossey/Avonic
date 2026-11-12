using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-input in Chromium, at 390 px wide, and the tests are named after the behaviours in
/// docs/components/ion-input/PLAN.md. Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub.
/// </summary>
public class AvonicInputTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Danger = Color.Parse("#C5000F");
    private static readonly Color Success = Color.Parse("#2DD55B");

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────

    private sealed class Log
    {
        public List<string> All { get; } = [];

        public static Log Of(AvonicInput input)
        {
            var log = new Log();
            input.IonInput += (_, e) => log.All.Add($"input:{e.Value}");
            input.IonChange += (_, e) => log.All.Add($"change:{e.Value}");
            input.IonFocus += (_, _) => log.All.Add("focus");
            input.IonBlur += (_, _) => log.All.Add("blur");
            return log;
        }

        public string[] Take()
        {
            var all = All.ToArray();
            All.Clear();
            return all;
        }
    }

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static TextBox Native(AvonicInput input) => Part<TextBox>(input, "PART_NativeInput");

    private static Rect Bounds(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static Point Centre(Visual visual, Scene scene) => Bounds(visual, scene.Window).Center;

    private static void AssertBox(double x, double y, double width, double height, Rect actual, double tolerance = 0.6)
    {
        Assert.InRange(actual.X, x - tolerance, x + tolerance);
        Assert.InRange(actual.Y, y - tolerance, y + tolerance);
        Assert.InRange(actual.Width, width - tolerance, width + tolerance);
        Assert.InRange(actual.Height, height - tolerance, height + tolerance);
    }

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static IInputElement? Focused(Visual visual) => TopLevel.GetTopLevel(visual)?.FocusManager?.GetFocusedElement();

    private static void Click(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        Settle();
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Settle();
    }

    private static void Key(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        Settle();
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Settle();
    }

    /// <summary>A key and the character it types, as a keyboard sends them.</summary>
    private static void Type(Scene scene, PhysicalKey key, string text)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyTextInput(text);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Settle();
    }

    private static void Render()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();
    }

    /// <summary>An input on the stub, with another text box after it to take focus away.</summary>
    private static (AvonicInput Input, TextBox Other, Scene Scene) Stub(AvonicInput? input = null)
    {
        input ??= new AvonicInput { Label = "Label" };
        var other = new TextBox();
        var scene = new Scene(new StackPanel { Children = { input, other } });
        return (input, other, scene);
    }

    /// <summary>The page's 16 px Roboto, which an item or the app would give.</summary>
    private static StackPanel Page(params Control[] children)
    {
        var page = new StackPanel { [TextElement.FontSizeProperty] = 16.0 };
        page.Bind(TextElement.FontFamilyProperty, page.GetResourceObservable("Avonic.FontFamily"));
        foreach (var child in children)
        {
            page.Children.Add(child);
        }

        return page;
    }

    private static Color BrushColor(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    // ── 1 to 4: the value and its events ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B01_Typing_Sets_The_Value_And_Raises_IonInput_With_Each_Edit_After_IonFocus()
    {
        var (input, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(input);

        input.SetFocus();
        Settle();
        Type(scene, PhysicalKey.H, "h");
        Type(scene, PhysicalKey.I, "i");

        Assert.Equal(["focus", "input:h", "input:hi"], log.Take());
        Assert.Equal("hi", input.Value);
    }

    [AvaloniaFact]
    public void B02_Enter_Raises_IonChange_Once_When_The_Value_Changed()
    {
        var (input, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(input);
        input.SetFocus();
        Settle();
        Type(scene, PhysicalKey.H, "h");
        log.Take();

        Key(scene, PhysicalKey.Enter);
        Assert.Equal(["change:h"], log.Take());

        Key(scene, PhysicalKey.Enter);
        Assert.Empty(log.Take());
    }

    [AvaloniaFact]
    public void B03_Blur_Raises_IonChange_When_Edited_Then_IonBlur()
    {
        var (input, other, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(input);

        input.SetFocus();
        Settle();
        Type(scene, PhysicalKey.X, "x");
        other.Focus();
        Settle();
        Assert.Equal(["focus", "input:x", "change:x", "blur"], log.Take());

        // A focus and blur with no edit: no change.
        input.SetFocus();
        Settle();
        other.Focus();
        Settle();
        Assert.Equal(["focus", "blur"], log.Take());

        // Committed with Enter: the blur doesn't commit it again.
        input.SetFocus();
        Settle();
        Type(scene, PhysicalKey.Y, "y");
        Key(scene, PhysicalKey.Enter);
        other.Focus();
        Settle();
        Assert.Equal(["focus", "input:xy", "change:xy", "blur"], log.Take());
    }

    [AvaloniaFact]
    public void B04_A_Value_Set_In_Code_Raises_Nothing_Shows_And_Can_Replace_An_Edit()
    {
        var (input, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(input);

        input.Value = "prog";
        Settle();
        Assert.Empty(log.Take());
        Assert.Equal("prog", Native(input).Text);

        input.Value = null;
        Settle();
        Assert.Equal("", Native(input).Text);

        // Input masking: an ionInput handler that replaces the value replaces what's shown.
        input.IonInput += (_, e) => input.Value = e.Value?.ToUpperInvariant();
        input.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        Assert.Equal("A", input.Value);
        Assert.Equal("A", Native(input).Text);
    }

    // ── 5: debounce ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B05_Debounce_Raises_IonInput_Once_With_The_Last_Value_After_The_Wait()
    {
        using var clock = new TestClock();
        var (input, _, scene) = Stub(new AvonicInput { Debounce = 100 });
        using var _ = scene;
        var log = Log.Of(input);
        input.SetFocus();
        Settle();
        log.Take();

        clock.At(0);
        Type(scene, PhysicalKey.A, "a");
        clock.At(30);
        Type(scene, PhysicalKey.B, "b");
        clock.At(129);
        Assert.Empty(log.Take());
        Assert.Equal("ab", input.Value);

        clock.At(130);
        Assert.Equal(["input:ab"], log.Take());

        // Back to undefined: at once again.
        input.Debounce = null;
        Type(scene, PhysicalKey.C, "c");
        Assert.Equal(["input:abc"], log.Take());
    }

    // ── 6: clear on edit ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B06_ClearOnEdit_Clears_On_The_First_Key_That_Edits_Once_Per_Focus_For_A_Password()
    {
        var (input, other, scene) = Stub(new AvonicInput { Type = InputType.Password, Value = "old" });
        using var _ = scene;
        var log = Log.Of(input);
        input.SetFocus();
        Settle();
        log.Take();

        // Shift and Tab are ignored, and don't use it up.
        Key(scene, PhysicalKey.ShiftLeft);
        Assert.Equal("old", input.Value);

        // An arrow edits nothing, but isn't ignored: it clears.
        Key(scene, PhysicalKey.ArrowLeft);
        Assert.Equal("", input.Value);
        Assert.Equal(["input:"], log.Take());

        // Once per focus.
        Type(scene, PhysicalKey.Z, "z");
        Type(scene, PhysicalKey.Z, "z");
        Assert.Equal("zz", input.Value);
        Assert.Equal(["input:z", "input:zz"], log.Take());

        // Tab away: no clear.
        other.Focus();
        Settle();
        input.SetFocus();
        Settle();
        Key(scene, PhysicalKey.Tab);
        Assert.Equal("zz", input.Value);

        // Tab left the field, which resets it: the next focus clears again.
        input.SetFocus();
        Settle();
        Type(scene, PhysicalKey.Q, "q");
        Assert.Equal("q", input.Value);
    }

    [AvaloniaFact]
    public void B06_ClearOnEdit_Follows_The_Property_And_Text_With_No_Key()
    {
        // Off for a password.
        var (input, _, scene) = Stub(new AvonicInput { Type = InputType.Password, Value = "old", ClearOnEdit = false });
        using (scene)
        {
            input.SetFocus();
            Settle();
            Type(scene, PhysicalKey.A, "a");
            Assert.Equal("olda", input.Value);
        }

        // On for text, and text typed with no key down, as a soft keyboard does.
        (input, _, scene) = Stub(new AvonicInput { Value = "old", ClearOnEdit = true });
        using (scene)
        {
            input.SetFocus();
            Settle();
            scene.Window.KeyTextInput("b");
            Settle();
            Assert.Equal("b", input.Value);
        }

        // Off by default for text.
        (input, _, scene) = Stub(new AvonicInput { Value = "old" });
        using (scene)
        {
            input.SetFocus();
            Settle();
            Type(scene, PhysicalKey.C, "c");
            Assert.Equal("oldc", input.Value);
        }
    }

    // ── 7: the clear button ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B07_The_Clear_Button_Shows_With_A_Value_And_Clears_Keeping_Focus()
    {
        var (input, _, scene) = Stub(new AvonicInput { ClearInput = true, Value = "" });
        using var _ = scene;
        var button = Part<Control>(input, "PART_ClearButton");

        // visibility: hidden with no value: there, but not seen and not hit.
        Assert.True(button.IsVisible);
        Assert.Equal(0, button.Opacity);
        Assert.False(button.IsHitTestVisible);

        input.Value = "abc";
        Settle();
        Assert.Equal(1, button.Opacity);
        Assert.True(button.IsHitTestVisible);

        var log = Log.Of(input);
        input.SetFocus();
        Settle();
        log.Take();
        Render();

        // A press doesn't take focus from the field; the click clears and keeps it.
        var at = Centre(button, scene);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Settle();
        Assert.Same(Native(input), Focused(input));
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Settle();

        Assert.Equal("", input.Value);
        Assert.Equal(["input:"], log.Take());
        Assert.Same(Native(input), Focused(input));
    }

    [AvaloniaFact]
    public void B07_The_Clear_Button_Focuses_The_Field_First_When_It_Wasnt()
    {
        var (input, other, scene) = Stub(new AvonicInput { ClearInput = true, Value = "q" });
        using var _ = scene;
        other.Focus();
        Settle();
        var log = Log.Of(input);
        Render();

        Click(scene, Centre(Part<Control>(input, "PART_ClearButton"), scene));

        Assert.Equal(["focus", "input:"], log.Take());
        Assert.Same(Native(input), Focused(input));
    }

    [AvaloniaFact]
    public void B07_Enter_On_The_Focused_Clear_Button_Clears()
    {
        var (input, _, scene) = Stub(new AvonicInput { ClearInput = true, Value = "abc" });
        using var _ = scene;
        var button = Part<Control>(input, "PART_ClearButton");
        input.SetFocus();
        Settle();

        // Tab from the field: the clear button is next.
        Key(scene, PhysicalKey.Tab);
        Assert.Same(button, Focused(input));

        Key(scene, PhysicalKey.Enter);
        Assert.Equal("", input.Value);
    }

    [AvaloniaFact]
    public void B07_The_Clear_Button_Isnt_There_When_Read_Only_Or_Disabled()
    {
        var (input, _, scene) = Stub(new AvonicInput { ClearInput = true, Value = "abc", Readonly = true });
        using var _ = scene;
        var button = Part<Control>(input, "PART_ClearButton");
        Assert.False(button.IsVisible);

        input.Readonly = false;
        Settle();
        Assert.True(button.IsVisible);

        input.IsEnabled = false;
        Settle();
        Assert.False(button.IsVisible);

        input.IsEnabled = true;
        input.ClearInput = false;
        Settle();
        Assert.False(button.IsVisible);
    }

    // ── 8 to 10: focus ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B08_A_Click_On_The_Label_Focuses_The_Field_But_Not_On_The_Bottom_Or_A_Slotted_Button()
    {
        var clicked = 0;
        var button = new Button { Content = "B" };
        button.Click += (_, _) => clicked++;
        var (input, other, scene) = Stub(new AvonicInput { Label = "Label", HelperText = "Helper", EndContent = button });
        using var _ = scene;
        Render();

        Click(scene, Centre(Part<Control>(input, "PART_Label"), scene));
        Assert.Same(Native(input), Focused(input));

        other.Focus();
        Settle();
        // On the text itself, which is what takes a click on a TextBlock.
        var helper = Bounds(Part<Control>(input, "PART_HelperText"), scene.Window);
        Click(scene, new Point(helper.X + 4, helper.Center.Y));
        Assert.Same(other, Focused(input));

        Click(scene, Centre(button, scene));
        Assert.Equal(1, clicked);
        Assert.NotSame(Native(input), Focused(input));
    }

    [AvaloniaFact]
    public void B09_SetFocus_Focuses_The_Native_Input_That_GetInputElement_Returns()
    {
        var (input, _, scene) = Stub(new AvonicInput());
        using var _ = scene;
        var native = input.GetInputElement();
        Assert.NotNull(native);
        Assert.Equal("PART_NativeInput", native!.Name);
        Assert.Same(native, input.GetInputElement());
        input.SetFocus();
        Settle();
        Assert.Same(native, Focused(input));
    }

    [AvaloniaFact]
    public void B10_A_Tap_On_An_Items_Padding_Focuses_The_Input() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "Label" };
        var log = Log.Of(input);
        var item = new AvonicItem { Content = input };
        using var scene = new Scene(new StackPanel { Children = { item, new TextBox() } });
        Render();

        // The item's 16 px padding, outside the input.
        var box = Bounds(item, scene.Window);
        var at = new Point(box.X + 4, box.Center.Y);
        Assert.False(Bounds(input, scene.Window).Contains(at));
        Click(scene, at);

        Assert.Same(Native(input), Focused(input));
        Assert.Equal(["focus"], log.Take());
    });

    // ── 11 to 13: layout ─────────────────────────────────────────────────────────────────────

    // :host-context(ion-item)[slot=start|end] { width: auto }: as wide as its content, where the native input is as wide as
    // Chromium makes an <input> of 20 characters. Measured in Chromium at 16 px Roboto: 199, and the label's 34.48 and 16 more;
    // a long value doesn't widen it; 298 at 24 px.
    [AvaloniaFact]
    public void B11_In_An_Items_Start_Or_End_Slot_The_Input_Is_As_Wide_As_Its_Content() => InstalledTheme.Run(_ =>
    {
        var bare = new AvonicInput { [AutomationProperties.NameProperty] = "x" };
        var labelled = new AvonicInput { Label = "Start", Value = "abc" };
        var end = new AvonicInput { Placeholder = "End" };
        var full = new AvonicInput { Label = "Default" };
        var longValue = new AvonicInput { Value = "a very long value that is wider than twenty characters" };
        var big = new AvonicInput { FontSize = 24, [AutomationProperties.NameProperty] = "x" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItem { StartContent = bare, Content = new AvonicLabel { Content = "Label" } },
                new AvonicItem { StartContent = labelled, Content = new AvonicLabel { Content = "Label" } },
                new AvonicItem { Content = new AvonicLabel { Content = "Label" }, EndContent = end },
                new AvonicItem { Content = full },
                new AvonicItem { StartContent = longValue, Content = new AvonicLabel { Content = "Label" } },
                new AvonicItem { StartContent = big, Content = new AvonicLabel { Content = "Label" } },
            },
        });
        Render();

        Assert.Equal(199, longValue.Bounds.Width, 0.6);
        Assert.Equal(298, big.Bounds.Width, 0.6);
        AssertBox(16, 2, 199, 44, Bounds(bare, scene.Window));
        Assert.Equal(199, Native(bare).Bounds.Width, 0.6);
        Assert.Equal(249.48, labelled.Bounds.Width, 0.6);
        Assert.Equal(199, Native(labelled).Bounds.Width, 0.6);
        Assert.Equal(199, end.Bounds.Width, 0.6);
        Assert.Equal(374, Bounds(end, scene.Window).Right, 0.6);

        // In the default slot it's width: 100%.
        Assert.Equal(358, full.Bounds.Width, 0.6);

        // A new font size is a new width: each measured in Chromium.
        foreach (var (size, width) in new[] { (13.0, 161.0), (15.0, 186.0), (17.0, 211.0), (18.5, 229.0), (20.0, 249.0), (16.0, 199.0) })
        {
            big.FontSize = size;
            Render();
            Assert.Equal(width, big.Bounds.Width, 0.6);
        }
    });

    // The native input is width: 100%, which makes it compressible: an input in the end slot gives way to the start slot's, where
    // a label beside it, which is flex: 1, wraps instead. Measured in Chromium.
    [AvaloniaFact]
    public void B11_In_A_Slot_With_Too_Little_Room_The_Input_Gives_Way() => InstalledTheme.Run(_ =>
    {
        var beside = new AvonicInput { [AutomationProperties.NameProperty] = "x" };
        var start = new AvonicInput { Value = "a very long value that is wider than twenty characters and more", [AutomationProperties.NameProperty] = "x" };
        var end = new AvonicInput { [AutomationProperties.NameProperty] = "x" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItem { StartContent = beside, Content = new AvonicLabel { Content = "A label that is long enough to want the whole row for itself" } },
                new AvonicItem { StartContent = start, EndContent = end },
            },
        });
        Render();

        Assert.Equal(199, beside.Bounds.Width, 0.6);
        Assert.Equal(199, start.Bounds.Width, 0.6);
        Assert.Equal(127, end.Bounds.Width, 0.6);
        Assert.Equal(374, Bounds(end, scene.Window).Right, 0.6);

        // A plain TextBox in an app's own template is the native field too, and gives way with its text rather than push the
        // row off the screen.
        var theme = (ControlTheme)Application.Current!.FindResource("Avonic.TextField.NativeInput")!;
        start.Template = new FuncControlTemplate<AvonicInput>((_, scope) =>
            new TextBox { Name = "PART_NativeInput", Theme = theme }.RegisterInNameScope(scope));
        start.Value = "a very long value that is wider than what's left of the row by far";
        Render();
        Assert.True(Bounds(start, scene.Window).Right <= 374.6, $"{Bounds(start, scene.Window)}");
    });

    [AvaloniaFact]
    public void B11_Row_Placements_Measured_In_An_Item() => InstalledTheme.Run(_ =>
    {
        static AvonicItem Item(AvonicInput input) => new() { Content = input };
        var start = new AvonicInput { Label = "A start label that is far too long to fit", Value = "Value" };
        var end = new AvonicInput { Label = "End", LabelPlacement = InputLabelPlacement.End, Value = "Value" };
        var fixedLabel = new AvonicInput { Label = "Fix", LabelPlacement = InputLabelPlacement.Fixed, Value = "Value" };
        var none = new AvonicInput { Value = "No label" };
        using var scene = new Scene(new StackPanel { Children = { Item(start), Item(end), Item(fixedLabel), Item(none) } });
        Render();

        // scene3 a: the label at its 200 px most, the field the rest of 358; b: mirrored; d: 100 px; f: no label at all.
        AssertBox(16, 0, 200, 44, Bounds(Part<Control>(start, "PART_Label"), start), 16.6);
        Assert.Equal(200, Part<Control>(start, "PART_Label").Bounds.Width, 0.5);
        Assert.Equal(142, Native(start).Bounds.Width, 0.5);
        Assert.Equal(44, start.Bounds.Height, 0.5);

        var endLabel = Bounds(Part<Control>(end, "PART_Label"), end);
        Assert.Equal(358, endLabel.Right, 0.5);
        Assert.Equal(26.95, endLabel.Width, 0.6);
        Assert.Equal(0, Bounds(Native(end), end).X, 0.5);
        Assert.Equal(315.05, Native(end).Bounds.Width, 0.6);

        Assert.Equal(100, Part<Control>(fixedLabel, "PART_Label").Bounds.Width, 0.5);
        Assert.Equal(116, Bounds(Native(fixedLabel), fixedLabel).X, 0.5);

        Assert.False(Part<Control>(none, "PART_Label").IsVisible);
        Assert.Equal(358, Native(none).Bounds.Width, 0.5);
    });

    [AvaloniaFact]
    public void B11_Column_Placements_Measured_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var floating = new AvonicInput { Label = "Floating", LabelPlacement = InputLabelPlacement.Floating };
        var raised = new AvonicInput { Label = "Floating", LabelPlacement = InputLabelPlacement.Floating, Value = "Value" };
        var stacked = new AvonicInput { Label = "Stacked label that is far too long to fit in the row at all", LabelPlacement = InputLabelPlacement.Stacked, Value = "Value" };
        var slots = new AvonicInput { Label = "Slots", LabelPlacement = InputLabelPlacement.Floating, StartContent = new AvonicIcon { IconName = "heart" } };
        using var scene = new Scene(new StackPanel
        {
            Children = { new AvonicItem { Content = floating }, new AvonicItem { Content = raised }, new AvonicItem { Content = stacked }, new AvonicItem { Content = slots } },
        });
        floating.Transitions = [];
        Part<Control>(floating, "PART_Label").Transitions = [];
        Part<Control>(raised, "PART_Label").Transitions = [];
        Part<Control>(stacked, "PART_Label").Transitions = [];
        Part<Control>(slots, "PART_Label").Transitions = [];
        Render();

        // scene1 b: on the field, translateY(100%): 19 down from the top of a 56 px host.
        Assert.Equal(56, floating.Bounds.Height, 0.5);
        AssertBox(0, 19, 57.52, 18.75, Bounds(Part<Control>(floating, "PART_Label"), floating), 0.8);

        // scene1 c: raised, translateY(50%) scale(0.75): 9.5 down, 0.75 the size; the field 1 px below the label's box,
        // in the middle of what's left.
        AssertBox(0, 9.5, 43.14, 14.25, Bounds(Part<Control>(raised, "PART_Label"), raised), 0.8);
        Assert.Equal(28.5, Bounds(Native(raised), raised).Y, 0.8);

        // scene3 e: the label's room is 358 / 0.75, so 377 wide before the scale.
        var label = Part<Control>(stacked, "PART_Label");
        Assert.InRange(label.Bounds.Width, 358.5, 477.4);
        Assert.Equal(282.9, Bounds(label, stacked).Width, 1.5);

        // scene3 g: anything in a slot floats the label.
        Assert.Equal(0.75, Bounds(Part<Control>(slots, "PART_Label"), slots).Height / Part<Control>(slots, "PART_Label").Bounds.Height, 0.01);
    });

    [AvaloniaFact]
    public void B12_A_Floating_Fields_Text_Is_Hidden_Until_It_Has_Focus_Or_A_Value() => InstalledTheme.Run(_ =>
    {
        var (input, other, scene) = Stub(new AvonicInput { Label = "L", LabelPlacement = InputLabelPlacement.Floating });
        using var owned = scene;
        Native(input).Transitions = [];
        Part<Control>(input, "PART_Label").Transitions = [];
        Assert.Equal(0, Native(input).Opacity);

        input.SetFocus();
        Settle();
        Assert.Equal(1, Native(input).Opacity);

        // Focus floats the label too.
        Assert.Equal(0.75, Part<Control>(input, "PART_Label").GetValue(Css.ScaleProperty));

        other.Focus();
        Settle();
        Assert.Equal(0, Native(input).Opacity);

        input.Value = "v";
        Settle();
        Assert.Equal(1, Native(input).Opacity);

        // Start content floats the label, but the field stays hidden: the rule reads focus and value only.
        input.Value = "";
        input.StartContent = new Border { Width = 4, Height = 4 };
        Settle();
        Assert.Equal(0, Native(input).Opacity);
    });

    [AvaloniaFact]
    public void B13_The_Wrapper_Is_44_Tall_And_56_When_Floating_Stacked_Or_Filled() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicInput { Label = "L" };
        var stacked = new AvonicInput { Label = "L", LabelPlacement = InputLabelPlacement.Stacked };
        var solid = new AvonicInput { Label = "L", Fill = InputFill.Solid, HelperText = "Helper" };
        var outline = new AvonicInput { Label = "L", Fill = InputFill.Outline };
        using var scene = new Scene(Page(plain, stacked, solid, outline));
        Render();

        Assert.Equal(44, plain.Bounds.Height, 0.5);
        Assert.Equal(56, stacked.Bounds.Height, 0.5);
        Assert.Equal(56, outline.Bounds.Height, 0.5);

        // scene1 f: the wrapper is 56 and the bottom 19 under it, 75 in all.
        Assert.Equal(56, Part<Control>(solid, "PART_Wrapper").Bounds.Height, 0.5);
        // Avalonia's 12 px line is 15 high, Chromium's 14.
        Assert.Equal(75, solid.Bounds.Height, 1.1);
    });

    [AvaloniaFact]
    public void B13_A_Large_Field_Has_1px_Above_It_Stacked_And_6px_Around_It_In_An_Outline() => InstalledTheme.Run(_ =>
    {
        // At 40 px the text, not the 56 px minimum, sets the height, so the margins show.
        var stacked = new AvonicInput { Label = "L", LabelPlacement = InputLabelPlacement.Stacked, FontSize = 40 };
        var outline = new AvonicInput { Label = "L", LabelPlacement = InputLabelPlacement.Stacked, Fill = InputFill.Outline, FontSize = 40 };
        using var scene = new Scene(Page(stacked, outline));
        Render();

        var label = Part<Control>(stacked, "PART_Label").Bounds.Height;
        Assert.Equal(label + 1 + Native(stacked).Bounds.Height, stacked.Bounds.Height, 0.5);
        Assert.Equal(6 + Native(outline).Bounds.Height + 6, outline.Bounds.Height, 0.5);
    });

    // ── 14, 15: the highlight ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_The_Highlight_Grows_On_Focus_Outside_An_Item_And_Not_With_The_Outline() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicInput { Label = "Plain" };
        var solid = new AvonicInput { Label = "Solid", Fill = InputFill.Solid };
        var outline = new AvonicInput { Label = "Outline", Fill = InputFill.Outline };
        var inItem = new AvonicInput { Label = "Item" };
        using var scene = new Scene(Page(plain, solid, outline, new AvonicItem { Content = inItem }));
        Render();

        var highlight = Part<Control>(plain, "PART_Highlight");
        Assert.True(highlight.IsVisible);
        Assert.False(Part<Control>(outline, "PART_Highlight").IsVisible);
        Assert.False(Part<Control>(inItem, "PART_Highlight").IsVisible);

        // Unfocused: scale(0).
        Assert.Equal(0, Bounds(highlight, plain).Width, 0.01);

        // Focused: it eases to scale(1) over 200 ms, 2 px tall, 1 px below the wrapper with no fill, over solid's border.
        highlight.Transitions = [];
        Part<Control>(solid, "PART_Highlight").Transitions = [];
        plain.SetFocus();
        Settle();
        Render();
        var box = Bounds(highlight, plain);
        AssertBox(0, 43, plain.Bounds.Width, 2, box);

        solid.SetFocus();
        Settle();
        Render();
        Assert.Equal(56, Bounds(Part<Control>(solid, "PART_Highlight"), solid).Bottom, 0.5);
    });

    [AvaloniaFact]
    public void B14_The_Highlight_Eases_Over_200ms() => InstalledTheme.Run(_ =>
    {
        var (input, _, scene) = Stub(new AvonicInput { Label = "L" });
        using var owned = scene;
        var highlight = Part<Control>(input, "PART_Highlight");
        var transition = Assert.Single(highlight.Transitions!.OfType<Avalonia.Animation.DoubleTransition>());
        Assert.Equal(Css.ScaleProperty, transition.Property);
        Assert.Equal(TimeSpan.FromMilliseconds(200), transition.Duration);
    });

    [AvaloniaFact]
    public void B15_The_Highlight_Colour_Is_Primary_A_Colours_Base_Then_Valid_Then_Invalid() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "L", LabelPlacement = InputLabelPlacement.Floating };
        using var scene = new Scene(Page(input, new TextBox()));
        var label = Part<Control>(input, "PART_Label");
        label.Transitions = [];
        input.SetFocus();
        Settle();

        Assert.Equal(Primary, BrushColor(Part<Control>(input, "PART_Highlight").GetValue(Border.BackgroundProperty)));
        Assert.Equal(Primary, BrushColor(Native(input).CaretBrush));

        // A focused floating label takes it.
        Assert.Equal(Primary, BrushColor(label.GetValue(TextElement.ForegroundProperty)));

        input.Color = "danger";
        Settle();
        Assert.Equal(Danger, BrushColor(input.HighlightBrush));

        input.Color = null;
        input.Classes.Add(AvonicInput.ValidClass);
        Settle();
        Assert.Equal(Success, BrushColor(input.HighlightBrush));

        // touched and invalid beat valid.
        input.Classes.Add(AvonicInput.TouchedClass);
        input.Classes.Add(AvonicInput.InvalidClass);
        Settle();
        Assert.Equal(Danger, BrushColor(input.HighlightBrush));

        // Invalid and blurred: the label keeps it.
        scene.Window.GetVisualDescendants().OfType<TextBox>().Last().Focus();
        Settle();
        Assert.Equal(Danger, BrushColor(label.GetValue(TextElement.ForegroundProperty)));
    });

    [AvaloniaFact]
    public void B15_The_Bottom_Border_Takes_The_Highlight_When_Focused_And_Valid_Or_Invalid() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "L", HelperText = "Helper" };
        using var scene = new Scene(Page(input, new TextBox()));
        var bottom = Part<Border>(input, "PART_Bottom");

        // rgba(0, 0, 0, 0.13): the item border colour.
        Assert.Equal(Color.FromArgb(0x21, 0, 0, 0), BrushColor(bottom.BorderBrush));
        Assert.Equal(new Thickness(0, 1, 0, 0), bottom.BorderThickness);

        input.Classes.Add(AvonicInput.ValidClass);
        input.SetFocus();
        Settle();
        Assert.Equal(Success, BrushColor(bottom.BorderBrush));

        input.Classes.Remove(AvonicInput.ValidClass);
        Settle();
        Assert.Equal(Color.FromArgb(0x21, 0, 0, 0), BrushColor(bottom.BorderBrush));

        input.Classes.Add(AvonicInput.TouchedClass);
        input.Classes.Add(AvonicInput.InvalidClass);
        Settle();
        Assert.Equal(Danger, BrushColor(bottom.BorderBrush));
    });

    // ── 16 to 18: fills and shapes ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B16_Solid_Background_And_Border_At_Rest_Hovered_And_Focused() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "Solid", Fill = InputFill.Solid };
        using var scene = new Scene(Page(input, new TextBox()));
        var wrapper = Part<Border>(input, "PART_Wrapper");
        wrapper.Transitions = [];
        Render();

        Assert.Equal(Color.Parse("#F2F2F2"), BrushColor(wrapper.Background));
        Assert.Equal(Color.Parse("#808080"), BrushColor(wrapper.BorderBrush));
        Assert.Equal(new Thickness(0, 0, 0, 1), wrapper.BorderThickness);
        Assert.Equal(new CornerRadius(4, 4, 0, 0), wrapper.CornerRadius);
        Assert.Equal(new Thickness(16, 0, 16, 0), wrapper.Padding);

        scene.Window.MouseMove(Centre(wrapper, scene), RawInputModifiers.None);
        Settle();
        Assert.Equal(Color.Parse("#E6E6E6"), BrushColor(wrapper.Background));
        Assert.Equal(Color.Parse("#404040"), BrushColor(wrapper.BorderBrush));

        scene.Window.MouseMove(new Point(380, 830), RawInputModifiers.None);
        input.SetFocus();
        Settle();
        Assert.Equal(Color.Parse("#D9D9D9"), BrushColor(wrapper.Background));
        Assert.Equal(Color.Parse("#404040"), BrushColor(wrapper.BorderBrush));
    });

    [AvaloniaFact]
    public void B16_A_Finger_Doesnt_Hover() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "Solid", Fill = InputFill.Solid };
        using var scene = new Scene(Page(input));
        var wrapper = Part<Border>(input, "PART_Wrapper");
        wrapper.Transitions = [];
        Render();

        // Avalonia raises an enter for a finger as it lands, which headless touch doesn't, so it's raised here.
        void Enter(PointerType type) => input.RaiseEvent(new PointerEventArgs(
            InputElement.PointerEnteredEvent, input, new Pointer(Pointer.GetNextFreeId(), type, true), scene.Window,
            Centre(wrapper, scene), 0, new PointerPointProperties(), KeyModifiers.None));

        Enter(PointerType.Touch);
        Settle();
        Assert.Equal(Color.Parse("#F2F2F2"), BrushColor(wrapper.Background));

        Enter(PointerType.Pen);
        Settle();
        Assert.Equal(Color.Parse("#E6E6E6"), BrushColor(wrapper.Background));
    });

    [AvaloniaFact]
    public void B17_The_Outline_Notch_Opens_For_A_Floating_Label() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "Outline", Fill = InputFill.Outline, LabelPlacement = InputLabelPlacement.Floating, Value = "Value" };
        var resting = new AvonicInput { Label = "Outline start", Fill = InputFill.Outline, Value = "v" };
        using var scene = new Scene(Page(input, resting, new TextBox()));
        Part<Control>(input, "PART_Label").Transitions = [];
        Render();

        var start = Part<Border>(input, "PART_OutlineStart");
        var notch = Part<Border>(input, "PART_OutlineNotch");
        var end = Part<Border>(input, "PART_OutlineEnd");

        // scene1 g: start 12 wide with its corners, the notch the label at 12 px plus 8, the end the rest, all #b3b3b3.
        AssertBox(0, 0, 12, 56, Bounds(start, input));
        Assert.Equal(45.63, notch.Bounds.Width, 1.0);
        Assert.Equal(new Thickness(1, 1, 0, 1), start.BorderThickness);
        Assert.Equal(new CornerRadius(4, 0, 0, 4), start.CornerRadius);
        Assert.Equal(new Thickness(0, 1, 1, 1), end.BorderThickness);
        Assert.Equal(new CornerRadius(0, 4, 4, 0), end.CornerRadius);
        Assert.Equal(Color.Parse("#B3B3B3"), BrushColor(start.BorderBrush));

        // The floating label opens the notch, and sits on the border: translateY(-32%), out of the flow.
        Assert.Equal(new Thickness(0, 0, 0, 1), notch.BorderThickness);
        Assert.Equal(-6.08, Bounds(Part<Control>(input, "PART_Label"), input).Y, 0.6);
        Assert.Equal(18.5, Bounds(Native(input), input).Y, 0.8);

        // A label that isn't floating closes it: scene3 l.
        Assert.Equal(new Thickness(0, 1, 0, 1), Part<Border>(resting, "PART_OutlineNotch").BorderThickness);

        // Focused: 2 px in the highlight colour.
        input.SetFocus();
        Settle();
        Assert.Equal(new Thickness(2, 2, 0, 2), start.BorderThickness);
        Assert.Equal(Primary, BrushColor(end.BorderBrush));
    });

    [AvaloniaFact]
    public void B17_A_Slotted_Labels_Notch_Is_Its_Width_At_075_Plus_8() => InstalledTheme.Run(_ =>
    {
        var label = new TextBlock { Text = "Slotted label" };
        var input = new AvonicInput { LabelContent = label, Fill = InputFill.Outline, LabelPlacement = InputLabelPlacement.Floating };
        using var scene = new Scene(Page(input));
        Render();

        Assert.Equal(label.Bounds.Width * 0.75 + 8, Part<Control>(input, "PART_OutlineNotch").Bounds.Width, 0.5);
    });

    [AvaloniaFact]
    public void B17_A_Floating_Outline_Label_Has_8px_Less_Room() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput
        {
            LabelContent = new Border { Width = 1000, Height = 10 },
            Fill = InputFill.Outline, LabelPlacement = InputLabelPlacement.Floating, Value = "v",
        };

        // The label takes no room: a 60 px slot makes the input 60, not 60 and the label's 10.
        var tall = new AvonicInput
        {
            Label = "Tall", Fill = InputFill.Outline, LabelPlacement = InputLabelPlacement.Stacked,
            EndContent = new Border { Width = 10, Height = 60 },
        };
        using var scene = new Scene(Page(input, tall));
        Render();

        // max-width: calc((100% - padding - 8px) / 0.75), of what's inside the padding.
        var room = (input.Bounds.Width - 32 - 8) / 0.75;
        Assert.Equal(room, Part<Control>(input, "PART_Label").Bounds.Width, 1.0);
        Assert.Equal(60, tall.Bounds.Height, 0.5);
    });

    [AvaloniaFact]
    public void B17_No_Label_No_Notch()
    {
        var (input, _, scene) = Stub(new AvonicInput { Fill = InputFill.Outline });
        using var _ = scene;
        Assert.True(Part<Control>(input, "PART_OutlineContainer").IsVisible);
        Assert.False(Part<Control>(input, "PART_OutlineNotch").IsVisible);
        Assert.False(Part<Control>(input, "PART_Label").IsVisible);

        input.Label = "L";
        Settle();
        Assert.True(Part<Control>(input, "PART_OutlineNotch").IsVisible);
        Assert.True(Part<Control>(input, "PART_Label").IsVisible);
    }

    [AvaloniaFact]
    public void B17_No_Label_No_Notch_In_The_Theme() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Fill = InputFill.Outline };
        using var scene = new Scene(Page(input));
        Assert.False(Part<Control>(input, "PART_OutlineNotch").IsVisible);
    });

    [AvaloniaFact]
    public void B18_Round_Shapes() => InstalledTheme.Run(_ =>
    {
        var outline = new AvonicInput { Label = "Round", Fill = InputFill.Outline, Shape = InputShape.Round, LabelPlacement = InputLabelPlacement.Stacked };
        var solid = new AvonicInput { Label = "Round", Fill = InputFill.Solid, Shape = InputShape.Round };
        var plain = new AvonicInput { Label = "Round", Shape = InputShape.Round };
        using var scene = new Scene(Page(outline, solid, plain));
        Render();

        // scene1 h: 28 px corners, padding 32, the start 28 wide; scene3 n: solid's top corners 16.
        Assert.Equal(new CornerRadius(28, 0, 0, 28), Part<Border>(outline, "PART_OutlineStart").CornerRadius);
        Assert.Equal(28, Part<Control>(outline, "PART_OutlineStart").Bounds.Width, 0.5);
        Assert.Equal(new Thickness(32, 0, 32, 0), Part<Border>(outline, "PART_Wrapper").Padding);
        Assert.Equal(new CornerRadius(16, 16, 0, 0), Part<Border>(solid, "PART_Wrapper").CornerRadius);
        Assert.Equal(new CornerRadius(16), Part<Border>(plain, "PART_Wrapper").CornerRadius);
    });

    // ── 19: the bottom ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_The_Bottom_Shows_Only_With_Hint_Text_Or_A_Counter()
    {
        var (input, _, scene) = Stub(new AvonicInput { Counter = true });
        using var _ = scene;
        var bottom = Part<Control>(input, "PART_Bottom");

        // A counter needs maxlength.
        Assert.False(bottom.IsVisible);
        Assert.Null(input.CounterText);

        input.Maxlength = 20;
        Settle();
        Assert.True(bottom.IsVisible);
        Assert.Equal("0 / 20", Part<TextBlock>(input, "PART_Counter").Text);

        input.Counter = false;
        Settle();
        Assert.False(bottom.IsVisible);

        input.HelperText = "Help";
        Settle();
        Assert.True(bottom.IsVisible);
        input.HelperText = null;
        input.ErrorText = "Error";
        Settle();
        Assert.True(bottom.IsVisible);
    }

    [AvaloniaFact]
    public void B19_Touched_And_Invalid_Shows_The_Error_Text_Instead_Of_The_Helper()
    {
        var (input, _, scene) = Stub(new AvonicInput { HelperText = "Helper", ErrorText = "Error" });
        using var _ = scene;
        var helper = Part<Control>(input, "PART_HelperText");
        var error = Part<Control>(input, "PART_ErrorText");
        Assert.True(helper.IsVisible);
        Assert.False(error.IsVisible);

        // One class isn't enough.
        input.Classes.Add(AvonicInput.InvalidClass);
        Settle();
        Assert.True(helper.IsVisible);

        input.Classes.Add(AvonicInput.TouchedClass);
        Settle();
        Assert.False(helper.IsVisible);
        Assert.True(error.IsVisible);
    }

    [AvaloniaFact]
    public void B19_The_Counter_Counts_The_Value_With_The_Formatter_Or_The_Default()
    {
        var (input, _, scene) = Stub(new AvonicInput { Counter = true, Maxlength = 5, Value = "abc" });
        using var _ = scene;
        Assert.Equal("3 / 5", input.CounterText);

        input.CounterFormatter = (length, max) => $"{max - length} left";
        Assert.Equal("2 left", input.CounterText);

        input.SetFocus();
        Settle();
        Type(scene, PhysicalKey.D, "d");
        Assert.Equal("1 left", input.CounterText);

        // A formatter that throws: the default text.
        input.CounterFormatter = (_, _) => throw new InvalidOperationException();
        Assert.Equal("4 / 5", input.CounterText);
    }

    [AvaloniaFact]
    public void B19_The_Bottom_Text_Measured() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "Solid", Fill = InputFill.Solid, LabelPlacement = InputLabelPlacement.Floating, HelperText = "Helper text", Counter = true, Maxlength = 20 };
        var plain = new AvonicInput { Label = "No fill", LabelPlacement = InputLabelPlacement.Stacked, HelperText = "Helper", Value = "Value" };
        using var scene = new Scene(Page(input, plain));
        Render();

        // scene1 f: the helper 16 in and 5 below the wrapper, 12 px in #4d4d4d; the counter at the end, 16 in.
        var helper = Part<TextBlock>(input, "PART_HelperText");
        Assert.Equal(16, Bounds(helper, input).X, 0.5);
        Assert.Equal(61, Bounds(helper, input).Y, 0.6);
        Assert.Equal(12, helper.FontSize);
        Assert.Equal(Color.Parse("#4D4D4D"), BrushColor(helper.Foreground));
        Assert.Equal(input.Bounds.Width - 16, Bounds(Part<Control>(input, "PART_Counter"), input).Right, 0.6);
        Assert.Equal(Bounds(Part<Control>(input, "PART_Counter"), input).X - 16, Bounds(helper, input).Right, 0.6);
        Assert.Equal(new Thickness(0), Part<Border>(input, "PART_Bottom").BorderThickness);

        // scene1 i: no fill, no padding, and a top border, then 5 px.
        Assert.Equal(0, Bounds(Part<Control>(plain, "PART_HelperText"), plain).X, 0.5);
        Assert.Equal(62, Bounds(Part<Control>(plain, "PART_HelperText"), plain).Y, 0.6);
        Assert.Equal(new Thickness(0, 1, 0, 0), Part<Border>(plain, "PART_Bottom").BorderThickness);
    });

    // ── 20 to 23: the parts ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_The_Clear_Button_Is_30_Square_At_The_End_With_A_22px_Icon() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "Clear", ClearInput = true, Value = "Value" };
        var coloured = new AvonicInput { Label = "Clear", ClearInput = true, Value = "Value" };
        using var scene = new Scene(new StackPanel
        {
            Children = { new AvonicItem { Content = input }, new AvonicItem { Content = coloured, Color = "danger" } },
        });
        Render();

        // scene1 j: 30 x 30, flush with the end of the 358 px input, in the middle of 44.
        var button = Part<Control>(input, "PART_ClearButton");
        AssertBox(328, 7, 30, 30, Bounds(button, input));
        Assert.Equal(22, Part<Control>(input, "PART_ClearIcon").Bounds.Width, 0.5);
        Assert.Equal(Color.Parse("#666666"), BrushColor(button.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal("close-sharp", Part<AvonicIcon>(input, "PART_ClearIcon").Icon);

        // In a coloured item it inherits.
        Assert.Equal(coloured.Foreground, Part<Control>(coloured, "PART_ClearButton").GetValue(TextElement.ForegroundProperty));

        input.ClearInputIcon = "trash";
        Settle();
        Assert.Equal("trash", Part<AvonicIcon>(input, "PART_ClearIcon").Icon);
    });

    [AvaloniaFact]
    public void B21_Slotted_Content_Sits_16_From_The_Field() => InstalledTheme.Run(_ =>
    {
        var start = new Border { Width = 10, Height = 10 };
        var end = new Border { Width = 10, Height = 10 };
        var input = new AvonicInput { Label = "Slots", StartContent = start, EndContent = end };
        using var scene = new Scene(Page(input));
        Render();

        Assert.Equal(16, Bounds(Native(input), input).X - Bounds(start, input).Right, 0.5);
        Assert.Equal(16, Bounds(end, input).X - Bounds(Native(input), input).Right, 0.5);
    });

    [AvaloniaFact]
    public void B22_Disabled_Fades_And_Cant_Be_Focused_And_Hides_The_Toggle()
    {
        var toggle = new AvonicInputPasswordToggle();
        var (input, _, scene) = Stub(new AvonicInput { Type = InputType.Password, Value = "pw", EndContent = toggle, IsEnabled = false });
        using var _ = scene;

        input.SetFocus();
        Settle();
        Assert.NotSame(Native(input), Focused(input));
        Assert.Equal(0, toggle.Opacity);
        Assert.False(toggle.IsHitTestVisible);

        input.IsEnabled = true;
        Settle();
        Assert.Equal(1, toggle.Opacity);

    }

    [AvaloniaFact]
    public void B22_Disabled_Is_038_Opacity() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "Disabled", Value = "Value" };
        using var scene = new Scene(Page(input));
        Assert.Null(input.OpacityMask);

        // As one: a mask, not Opacity, which fades each overlapping part on its own.
        input.IsEnabled = false;
        Settle();
        Assert.Equal(1, input.Opacity);
        var mask = Assert.IsAssignableFrom<ISolidColorBrush>(input.OpacityMask);
        Assert.Equal(0.38, mask.Opacity * mask.Color.A / 255.0, 3);

        input.IsEnabled = true;
        Settle();
        Assert.Null(input.OpacityMask);
    });

    [AvaloniaFact]
    public void B22_Read_Only_Can_Be_Focused_But_Not_Edited_And_Hides_The_Toggle()
    {
        var toggle = new AvonicInputPasswordToggle();
        var (input, _, scene) = Stub(new AvonicInput { Value = "ro", Readonly = true, EndContent = toggle });
        using var _ = scene;

        input.SetFocus();
        Settle();
        Assert.Same(Native(input), Focused(input));
        Type(scene, PhysicalKey.A, "a");
        Assert.Equal("ro", input.Value);
        Assert.Equal(0, toggle.Opacity);
    }

    [AvaloniaFact]
    public void B23_The_Placeholder_Is_The_Fields_Colour_At_06() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "L", Placeholder = "Placeholder" };
        using var scene = new Scene(Page(input));
        Render();

        var placeholder = Part<TextBlock>(Native(input), "PART_Placeholder");
        Assert.Equal("Placeholder", placeholder.Text);
        Assert.Equal(0.6, placeholder.Opacity, 3);
        Assert.Equal(BrushColor(input.Foreground), BrushColor(placeholder.Foreground));

        input.Value = "v";
        Settle();
        Assert.False(placeholder.IsVisible);

        // An app's colour.
        input.Resources["Avonic.Input.PlaceholderColor"] = new SolidColorBrush(Colors.Red);
        input.Value = "";
        Settle();
        Assert.Equal(Colors.Red, BrushColor(placeholder.Foreground));
    });

    // ── 24, 25: the platform ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B24_Type_And_The_Hints_Reach_The_Native_Input()
    {
        var (input, _, scene) = Stub(new AvonicInput());
        using var _ = scene;
        var native = Native(input);
        Assert.Equal(TextInputContentType.Normal, TextInputOptions.GetContentType(native));
        Assert.Equal(default, native.PasswordChar);

        input.Type = InputType.Password;
        Assert.Equal('•', native.PasswordChar);
        Assert.Equal(TextInputContentType.Password, TextInputOptions.GetContentType(native));
        Assert.True(TextInputOptions.GetIsSensitive(native));

        input.Type = InputType.Email;
        Assert.Equal(TextInputContentType.Email, TextInputOptions.GetContentType(native));
        Assert.False(TextInputOptions.GetIsSensitive(native));
        input.Type = InputType.Tel;
        Assert.Equal(TextInputContentType.Digits, TextInputOptions.GetContentType(native));
        input.Type = InputType.Number;
        Assert.Equal(TextInputContentType.Number, TextInputOptions.GetContentType(native));
        input.Type = InputType.Url;
        Assert.Equal(TextInputContentType.Url, TextInputOptions.GetContentType(native));
        input.Type = InputType.Search;
        Assert.Equal(TextInputContentType.Search, TextInputOptions.GetContentType(native));

        // inputmode beats the type's keyboard.
        input.Inputmode = InputInputmode.Decimal;
        Assert.Equal(TextInputContentType.Number, TextInputOptions.GetContentType(native));
        input.Inputmode = InputInputmode.Numeric;
        Assert.Equal(TextInputContentType.Digits, TextInputOptions.GetContentType(native));
        input.Inputmode = InputInputmode.Text;
        Assert.Equal(TextInputContentType.Normal, TextInputOptions.GetContentType(native));

        input.Enterkeyhint = InputEnterkeyhint.Go;
        Assert.Equal(TextInputReturnKeyType.Go, TextInputOptions.GetReturnKeyType(native));
        input.Enterkeyhint = InputEnterkeyhint.Enter;
        Assert.Equal(TextInputReturnKeyType.Return, TextInputOptions.GetReturnKeyType(native));

        Assert.False(TextInputOptions.GetAutoCapitalization(native));
        input.Autocapitalize = "sentences";
        Assert.True(TextInputOptions.GetAutoCapitalization(native));
        input.Autocapitalize = "characters";
        Assert.False(TextInputOptions.GetAutoCapitalization(native));
        Assert.True(TextInputOptions.GetUppercase(native));

        Assert.False(TextInputOptions.GetShowSuggestions(native));
        input.Autocorrect = InputAutocorrect.On;
        Assert.True(TextInputOptions.GetShowSuggestions(native));

        input.Placeholder = "P";
        Assert.Equal("P", native.PlaceholderText);
    }

    [AvaloniaFact]
    public void B24_Maxlength_Stops_Typing()
    {
        var (input, _, scene) = Stub(new AvonicInput { Maxlength = 2 });
        using var _ = scene;
        input.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        Type(scene, PhysicalKey.B, "b");
        Type(scene, PhysicalKey.C, "c");

        Assert.Equal("ab", input.Value);
    }

    [AvaloniaFact]
    public void B25_Autofocus_Focuses_The_Field_When_Its_Loaded()
    {
        var (input, _, scene) = Stub(new AvonicInput { Autofocus = true });
        using var _ = scene;
        Assert.Same(Native(input), Focused(input));

        var (plain, _, other) = Stub(new AvonicInput());
        using var __ = other;
        Assert.NotSame(Native(plain), Focused(plain));
    }

    // ── 26: automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B26_The_Native_Input_Is_Named_By_The_Label_And_Described_By_The_Hint()
    {
        var (input, _, scene) = Stub(new AvonicInput { Label = "Email", HelperText = "Helper", ErrorText = "Error" });
        using var _ = scene;
        var native = Native(input);
        Assert.Equal("Email", AutomationProperties.GetName(native));
        Assert.Equal("Helper", AutomationProperties.GetHelpText(native));

        // aria-describedby follows the classes.
        input.Classes.Add(AvonicInput.TouchedClass);
        input.Classes.Add(AvonicInput.InvalidClass);
        Assert.Equal("Error", AutomationProperties.GetHelpText(native));

        // Invalid with no error text: the helper's.
        input.ErrorText = null;
        Assert.Equal("Helper", AutomationProperties.GetHelpText(native));

        // aria-label on the host beats the label.
        AutomationProperties.SetName(input, "Your email");
        Assert.Equal("Your email", AutomationProperties.GetName(native));

        // A slotted label's text.
        AutomationProperties.SetName(input, null);
        input.Label = null;
        input.LabelContent = new TextBlock { Text = "Slotted" };
        Settle();
        Assert.Equal("Slotted", AutomationProperties.GetName(native));
    }

    [AvaloniaFact]
    public void B26_Required_Says_The_Native_Input_Is_Required()
    {
        // required on the <input>: aria-required, from the first render and on each change.
        var (input, _, scene) = Stub(new AvonicInput { Label = "Email", Required = true });
        using var _ = scene;
        var native = Native(input);
        Assert.True(AutomationProperties.GetIsRequiredForForm(native));

        input.Required = false;
        Assert.False(AutomationProperties.GetIsRequiredForForm(native));

        input.Required = true;
        Assert.True(AutomationProperties.GetIsRequiredForForm(native));
    }

    [AvaloniaFact]
    public void B26_An_Apps_Own_Required_On_The_Native_Input_Wins()
    {
        // The mapping rule's style priority: an app's style on the native input beats what Required gives it.
        var (input, _, scene) = Stub(new AvonicInput { Label = "Email" });
        using var _ = scene;
        scene.Window.Styles.Add(new Style(x => x.OfType<AvonicInput>().Template().OfType<TextBox>())
        {
            Setters = { new Setter(AutomationProperties.IsRequiredForFormProperty, true) },
        });
        Settle();
        var native = Native(input);
        Assert.True(AutomationProperties.GetIsRequiredForForm(native));

        input.Required = true;
        input.Required = false;
        Assert.True(AutomationProperties.GetIsRequiredForForm(native));
    }

    [AvaloniaFact]
    public void B26_An_Apps_Not_Required_On_The_Native_Input_Wins_Over_Required()
    {
        var (input, _, scene) = Stub(new AvonicInput { Label = "Email", Required = true });
        using var _ = scene;
        scene.Window.Styles.Add(new Style(x => x.OfType<AvonicInput>().Template().OfType<TextBox>())
        {
            Setters = { new Setter(AutomationProperties.IsRequiredForFormProperty, false) },
        });
        Settle();
        var native = Native(input);
        Assert.False(AutomationProperties.GetIsRequiredForForm(native));

        input.Required = false;
        input.Required = true;
        Assert.False(AutomationProperties.GetIsRequiredForForm(native));
    }

    // aria-required, aria-labelledby and aria-describedby on the native input are at style priority and bound before the field's
    // styles, so that an app's plain style for the field, there before the window is shown, wins whenever they change after it.
    [AvaloniaFact]
    public void B26_An_Apps_Style_For_The_Native_Inputs_Automation_Survives_Changes_After_It()
    {
        var input = new AvonicInput { Label = "Email", HelperText = "Helper" };
        using var scene = Scene.Styled(new StackPanel { Children = { input } },
            Scene.AppStyle<TextBox>(AutomationProperties.IsRequiredForFormProperty, false),
            Scene.AppStyle<TextBox>(AutomationProperties.NameProperty, "The app's name"),
            Scene.AppStyle<TextBox>(AutomationProperties.HelpTextProperty, "The app's hint"));
        var native = Native(input);

        input.Required = true;
        input.Label = "Address";
        input.HelperText = "Another helper";

        Assert.False(AutomationProperties.GetIsRequiredForForm(native));
        Assert.Equal("The app's name", AutomationProperties.GetName(native));
        Assert.Equal("The app's hint", AutomationProperties.GetHelpText(native));
    }

    // A plain TextBox in an app's own template has no automation of its own: it's given the name, the hint and required when it's
    // found, and they're taken from it when another field takes its place.
    [AvaloniaFact]
    public void B26_A_Plain_Native_Input_In_An_Apps_Template_Is_Named_Too_And_Let_Go_When_Replaced()
    {
        var (input, _, scene) = Stub(new AvonicInput { Label = "Email", HelperText = "Helper", Required = true });
        using var _ = scene;
        static FuncControlTemplate<AvonicInput> Plain() =>
            new((_, scope) => new TextBox { Name = "PART_NativeInput" }.RegisterInNameScope(scope));

        input.Template = Plain();
        input.ApplyTemplate();
        Settle();
        var first = Native(input);
        Assert.IsNotType<InputNativeInput>(first);
        Assert.Equal("Email", AutomationProperties.GetName(first));
        Assert.Equal("Helper", AutomationProperties.GetHelpText(first));
        Assert.True(AutomationProperties.GetIsRequiredForForm(first));

        input.Template = Plain();
        input.ApplyTemplate();
        Settle();
        input.Label = "Address";
        Assert.NotSame(first, Native(input));
        Assert.Equal("Address", AutomationProperties.GetName(Native(input)));
        Assert.True(AutomationProperties.GetIsRequiredForForm(Native(input)));
        Assert.Null(AutomationProperties.GetName(first));
        Assert.False(AutomationProperties.GetIsRequiredForForm(first));

        // A template with no field lets go of the last one too.
        var second = Native(input);
        input.Template = new FuncControlTemplate<AvonicInput>((_, _) => new Border());
        input.ApplyTemplate();
        Settle();
        Assert.Null(input.GetInputElement());
        Assert.Null(AutomationProperties.GetName(second));
        Assert.False(AutomationProperties.GetIsRequiredForForm(second));
    }

    // ── 27: the password toggle ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B27_The_Password_Toggle_Flips_The_Type_And_Icon_Without_Taking_Focus()
    {
        var toggle = new AvonicInputPasswordToggle();
        var (input, _, scene) = Stub(new AvonicInput { Label = "P", Type = InputType.Password, Value = "pw", EndContent = toggle });
        using var _ = scene;
        var button = Part<AvonicButton>(toggle, "PART_Button");
        var icon = Part<AvonicIcon>(toggle, "PART_Icon");
        Assert.Equal("eye", icon.Icon);
        Assert.Equal("Show password", AutomationProperties.GetName(button));

        input.SetFocus();
        Settle();
        Render();
        var at = Centre(button, scene);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Settle();
        Assert.Same(Native(input), Focused(input));
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Settle();

        Assert.Equal(InputType.Text, input.Type);
        Assert.Equal(default, Native(input).PasswordChar);
        Assert.Equal("eye-off", icon.Icon);
        Assert.Equal("Hide password", AutomationProperties.GetName(button));
        Assert.Same(Native(input), Focused(input));

        // A second click hides it again.
        Click(scene, at);
        Assert.Equal(InputType.Password, input.Type);

        // The type set in code updates the toggle.
        input.Type = InputType.Text;
        Assert.Equal("eye-off", icon.Icon);
    }

    [AvaloniaFact]
    public void B27_The_Password_Toggles_Icons_And_Colour_Are_Its_Own()
    {
        var toggle = new AvonicInputPasswordToggle { ShowIcon = "lock-open", HideIcon = "lock-closed", Color = "danger" };
        var (input, _, scene) = Stub(new AvonicInput { Type = InputType.Text, EndContent = toggle });
        using var _ = scene;
        var icon = Part<AvonicIcon>(toggle, "PART_Icon");

        Assert.Equal("lock-closed", icon.Icon);
        input.Type = InputType.Password;
        Assert.Equal("lock-open", icon.Icon);
        Assert.Equal("danger", Part<AvonicButton>(toggle, "PART_Button").Color);
    }

    [AvaloniaFact]
    public void B27_The_Toggle_Is_A_Round_Clear_Button_Of_Default_Size_Even_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var toggle = new AvonicInputPasswordToggle();
        var input = new AvonicInput { Label = "Password", Type = InputType.Password, Value = "secret", EndContent = toggle };
        using var scene = new Scene(new StackPanel { Children = { new AvonicItem { Content = input } } });
        Render();

        // scene1 k: 44 x 48 at the end, 16 from the field.
        var button = Part<AvonicButton>(toggle, "PART_Button");
        Assert.Equal(ButtonFill.Clear, button.Fill);
        Assert.Equal(ButtonShape.Round, button.Shape);
        AssertBox(313.97, 0, 44.03, 48.03, Bounds(toggle, input), 1);
    });

    // ── Found in review ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R01_A_Press_On_The_Clear_Buttons_Edge_Keeps_Focus_Too() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "Clear", ClearInput = true, Value = "abc" };
        var log = Log.Of(input);
        using var scene = new Scene(Page(input));
        input.SetFocus();
        Settle();
        log.Take();
        Render();

        // 2 px in from the corner: on the 30 px button, off its 22 px icon.
        var box = Bounds(Part<Control>(input, "PART_ClearButton"), scene.Window);
        Click(scene, new Point(box.X + 2, box.Y + 2));

        Assert.Equal("", input.Value);
        Assert.Equal(["input:"], log.Take());
        Assert.Same(Native(input), Focused(input));
    });

    [AvaloniaFact]
    public void R02_A_Focusable_Item_Around_The_Input_Doesnt_Take_Focus_From_A_Press_On_The_Toggle_Or_Clear_Button()
    {
        var toggle = new AvonicInputPasswordToggle();
        var input = new AvonicInput { Label = "P", Type = InputType.Password, Value = "pw", ClearInput = true, EndContent = toggle };
        var log = Log.Of(input);
        var item = new AvonicItem { Button = true, Content = input };
        using var scene = new Scene(new StackPanel { Children = { item } });
        Assert.True(item.Focusable);
        input.SetFocus();
        Settle();
        log.Take();
        Render();

        Click(scene, Centre(Part<AvonicButton>(toggle, "PART_Button"), scene));
        Assert.Equal(InputType.Text, input.Type);
        Assert.Same(Native(input), Focused(input));

        Click(scene, Centre(Part<Control>(input, "PART_ClearButton"), scene));
        Assert.Equal("", input.Value);
        Assert.Same(Native(input), Focused(input));
        Assert.Equal(["input:"], log.Take());
    }

    [AvaloniaFact]
    public void R03_A_Pending_Debounced_IonInput_Is_Still_Raised_After_Debounce_Changes_Or_The_Input_Leaves()
    {
        using var clock = new TestClock();
        var (input, _, scene) = Stub(new AvonicInput { Debounce = 100 });
        var log = Log.Of(input);
        input.SetFocus();
        Settle();
        log.Take();

        clock.At(0);
        Type(scene, PhysicalKey.A, "a");
        input.Debounce = null;
        clock.At(100);
        Assert.Equal(["input:a"], log.Take());

        input.Debounce = 100;
        clock.At(200);
        Type(scene, PhysicalKey.B, "b");
        scene.Dispose();
        Assert.DoesNotContain("input:ab", log.Take());
        clock.At(300);
        Assert.Equal(["input:ab"], log.Take());
    }

    [AvaloniaFact]
    public void R04_A_Chip_In_A_Slot_Keeps_Its_Own_4px_Above_And_Below() => InstalledTheme.Run(_ =>
    {
        var chip = new AvonicChip { Items = { "Chip" } };
        var input = new AvonicInput { Label = "Chip", StartContent = chip };
        using var scene = new Scene(Page(input));
        Render();

        Assert.Equal(new Thickness(0, 4, 16, 4), chip.Margin);
    });

    [AvaloniaFact]
    public void R05_Hover_Ends_When_The_Input_Leaves_The_Tree() => InstalledTheme.Run(_ =>
    {
        // Avalonia raises an exit here when this test runs alone, and not after the suite has used the mouse.
        var input = new AvonicInput { Label = "Solid", Fill = InputFill.Solid };
        var host = new Border { Child = input };
        using var scene = new Scene(Page(host));
        var wrapper = Part<Border>(input, "PART_Wrapper");
        wrapper.Transitions = [];
        Render();
        scene.Window.MouseMove(Centre(wrapper, scene), RawInputModifiers.None);
        Settle();
        Assert.Equal(Color.Parse("#E6E6E6"), BrushColor(wrapper.Background));

        host.Child = null;
        Settle();
        host.Child = input;
        Settle();
        Assert.Equal(Color.Parse("#F2F2F2"), BrushColor(wrapper.Background));
    });

    [AvaloniaFact]
    public void R06_Maxlength_0_Allows_Nothing()
    {
        var (input, _, scene) = Stub(new AvonicInput { Maxlength = 0, Counter = true });
        using var _ = scene;
        var log = Log.Of(input);
        input.SetFocus();
        Settle();
        log.Take();
        Type(scene, PhysicalKey.A, "a");

        Assert.Equal("", input.Value);
        Assert.Empty(log.Take());
        Assert.Equal("0 / 0", input.CounterText);

        // Nor a paste, which Avalonia's text box puts in with no text input.
        var clipboard = scene.Window.Clipboard!;
        clipboard.SetTextAsync("b").GetAwaiter().GetResult();
        Settle();
        scene.Window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
        scene.Window.KeyReleaseQwerty(PhysicalKey.V, RawInputModifiers.Control);
        Settle();
        clipboard.ClearAsync().GetAwaiter().GetResult();

        Assert.Equal("", input.Value);
        Assert.Empty(log.Take());
    }

    [AvaloniaFact]
    public void R07_A_Templated_Slotted_Label_Names_The_Field()
    {
        var input = new AvonicInput
        {
            LabelContent = new Uri("https://example.com/"),
            LabelContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Uri>((uri, _) => new TextBlock { Text = uri.Host }),
        };
        var (_, _, scene) = Stub(input);
        using var _ = scene;

        Assert.Equal("example.com", AutomationProperties.GetName(Native(input)));
    }

    [AvaloniaFact]
    public void R08_A_Radius_Key_Can_Be_A_CSS_String() => InstalledTheme.Run(_ =>
    {
        var input = new AvonicInput { Label = "Solid", Fill = InputFill.Solid, Resources = { ["Avonic.Input.BorderRadius"] = "8px" } };
        using var scene = new Scene(Page(input));
        Render();

        Assert.Equal(new CornerRadius(8, 8, 0, 0), Part<Border>(input, "PART_Wrapper").CornerRadius);
    });

    [AvaloniaFact]
    public void R09_A_Value_Set_In_Code_Before_Focus_Isnt_A_Change_On_Blur()
    {
        var (input, other, scene) = Stub();
        using var _ = scene;
        input.Value = "abc";
        var log = Log.Of(input);

        input.SetFocus();
        Settle();
        other.Focus();
        Settle();

        Assert.Equal(["focus", "blur"], log.Take());
    }

    [AvaloniaFact]
    public void R10_A_Null_Value_Focused_And_Left_Isnt_A_Change()
    {
        var (input, other, scene) = Stub(new AvonicInput { Value = null });
        using var _ = scene;
        var log = Log.Of(input);

        input.SetFocus();
        Settle();
        Key(scene, PhysicalKey.Enter);
        other.Focus();
        Settle();

        Assert.Equal(["focus", "blur"], log.Take());
    }

    [AvaloniaFact]
    public void R11_Autofocus_Only_The_First_Time_It_Loads()
    {
        var input = new AvonicInput { Autofocus = true };
        var other = new TextBox();
        var host = new Border { Child = input };
        using var scene = new Scene(new StackPanel { Children = { host, other } });
        Assert.Same(Native(input), Focused(input));

        other.Focus();
        Settle();
        host.Child = null;
        Settle();
        host.Child = input;
        Settle();

        Assert.Same(other, Focused(other));

        // Loaded again with nothing focused: still not again.
        host.Child = null;
        Settle();
        scene.Window.FocusManager?.Focus(null);
        Settle();
        host.Child = input;
        Settle();
        Assert.NotSame(Native(input), Focused(input));
    }

    [AvaloniaFact]
    public void R11_Autofocus_Doesnt_Take_Focus_From_Another_Element()
    {
        var other = new TextBox();
        var host = new Border();
        using var scene = new Scene(new StackPanel { Children = { host, other } });
        other.Focus();
        Settle();

        var input = new AvonicInput { Autofocus = true };
        host.Child = input;
        Settle();

        Assert.Same(other, Focused(other));
    }

    [AvaloniaFact]
    public void R12_A_Value_Replaced_During_An_Edit_Leaves_The_Caret_At_The_End()
    {
        var (input, _, scene) = Stub(new AvonicInput { Value = "abc" });
        using var _ = scene;
        input.IonInput += (_, e) => input.Value = e.Value?.ToUpperInvariant();
        input.SetFocus();
        Settle();
        Native(input).CaretIndex = 1;

        Type(scene, PhysicalKey.X, "x");

        Assert.Equal("AXBC", Native(input).Text);
        Assert.Equal(4, Native(input).CaretIndex);
    }

    // A new template's native input is the one that's followed: the last one's edits no longer reach the value.
    [AvaloniaFact]
    public void R12_A_New_Template_Leaves_The_Last_Native_Input()
    {
        var (input, _, scene) = Stub(new AvonicInput { Value = "a" });
        using var _ = scene;
        var log = Log.Of(input);
        var old = Native(input);

        input.Template = new FuncControlTemplate<AvonicInput>((_, scope) => new TextBox { Name = "PART_NativeInput" }.RegisterInNameScope(scope));
        input.ApplyTemplate();
        Settle();
        Assert.NotSame(old, Native(input));
        Assert.Equal("a", Native(input).Text);

        old.Text = "stale";
        Settle();
        Assert.Equal("a", input.Value);
        Assert.Empty(log.Take());

        Native(input).Text = "b";
        Settle();
        Assert.Equal("b", input.Value);
        Assert.Equal(["input:b"], log.Take());
    }

    [AvaloniaFact]
    public void R13_A_Value_Set_During_Composition_Waits()
    {
        var (input, _, scene) = Stub(new AvonicInput { Value = "a" });
        using var _ = scene;
        var presenter = Native(input).GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().First();
        presenter.PreeditText = "ka";

        input.Value = "b";

        Assert.Equal("a", Native(input).Text);
    }

    // A value puts the caret at its end, and Avalonia's text presenter brings its caret into view whenever it moves. Chromium
    // never scrolls to an input that isn't focused. The base theme gives the ScrollViewer and the TextBox their templates.
    [AvaloniaFact]
    public void R12_A_Value_On_An_Input_That_Isnt_Focused_Doesnt_Scroll_The_Page() => WithScroller(input => input.Value = "x", (viewer, input) =>
    {
        Assert.Equal(0, viewer.Offset.Y);

        input.Value = "changed";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, viewer.Offset.Y);
    });

    [AvaloniaFact]
    public void R12_A_Focused_Input_Still_Brings_Its_Caret_Into_View() => WithScroller(_ => { }, (viewer, input) =>
    {
        Native(input).Focus();
        Dispatcher.UIThread.RunJobs();
        viewer.Offset = default;
        Dispatcher.UIThread.RunJobs();

        input.Value = "typed";
        Dispatcher.UIThread.RunJobs();
        Assert.True(viewer.Offset.Y > 0, $"offset {viewer.Offset.Y}");
    });

    /// <summary>An input below the fold of a 300-high ScrollViewer, over the base theme.</summary>
    private static void WithScroller(Action<AvonicInput> before, Action<ScrollViewer, AvonicInput> test)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var input = new AvonicInput { Label = "Below" };
                before(input);
                var viewer = new ScrollViewer
                {
                    Height = 300,
                    Content = new StackPanel { Children = { new Border { Height = 400 }, input, new Border { Height = 50 } } },
                };
                using var scene = new Scene(new StackPanel { Children = { viewer, new Button() } });
                Dispatcher.UIThread.RunJobs();
                Assert.True(viewer.Extent.Height > 300);
                test(viewer, input);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    // ── 28: RTL ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B28_Right_To_Left_Mirrors_The_Row_And_The_Outline() => InstalledTheme.Run(_ =>
    {
        var row = new AvonicInput { Label = "End", Value = "v", FlowDirection = FlowDirection.RightToLeft };
        var outline = new AvonicInput { Label = "RTL", Fill = InputFill.Outline, LabelPlacement = InputLabelPlacement.Floating, Value = "v", FlowDirection = FlowDirection.RightToLeft };
        using var scene = new Scene(Page(row, outline));
        Part<Control>(outline, "PART_Label").Transitions = [];
        Render();

        // The label at the right, the field to its left.
        var window = scene.Window;
        Assert.Equal(390, Bounds(Part<Control>(row, "PART_Label"), window).Right, 0.5);
        Assert.True(Bounds(Native(row), window).Right < Bounds(Part<Control>(row, "PART_Label"), window).X);

        // scene3 q: the outline's start at the right, 12 wide, and the label scaled from its top right, 16 in.
        var top = Bounds(outline, window).Y;
        AssertBox(378, top, 12, 56, Bounds(Part<Control>(outline, "PART_OutlineStart"), window));
        Assert.Equal(374, Bounds(Part<Control>(outline, "PART_Label"), window).Right, 0.6);
    });
}
