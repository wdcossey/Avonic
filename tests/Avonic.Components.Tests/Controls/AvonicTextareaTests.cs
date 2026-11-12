using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-textarea in Chromium, at 390 px wide, and the tests are named after the behaviours in
/// docs/components/ion-textarea/PLAN.md. Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub.
/// </summary>
public class AvonicTextareaTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Danger = Color.Parse("#C5000F");
    private static readonly Color Success = Color.Parse("#2DD55B");

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────

    private sealed class Log
    {
        public List<string> All { get; } = [];

        public static Log Of(AvonicTextarea textarea)
        {
            var log = new Log();
            textarea.IonInput += (_, e) => log.All.Add($"input:{e.Value}");
            textarea.IonChange += (_, e) => log.All.Add($"change:{e.Value}");
            textarea.IonFocus += (_, _) => log.All.Add("focus");
            textarea.IonBlur += (_, _) => log.All.Add("blur");
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

    private static TextareaNativeInput Native(AvonicTextarea textarea) => Part<TextareaNativeInput>(textarea, "PART_NativeInput");

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

    /// <summary>A textarea on the stub, with a text box after it to take focus away.</summary>
    private static (AvonicTextarea Textarea, TextBox Other, Scene Scene) Stub(AvonicTextarea? textarea = null)
    {
        textarea ??= new AvonicTextarea { Label = "Label" };
        var other = new TextBox();
        var scene = new Scene(new StackPanel { Children = { textarea, other } });
        return (textarea, other, scene);
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

    private static AvonicItem Item(AvonicTextarea textarea) => new() { Content = textarea };

    private static Color BrushColor(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    // ── 1 to 6: the value and its events ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B01_Typing_Sets_The_Value_And_Raises_IonInput_With_Each_Edit_After_IonFocus()
    {
        var (textarea, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(textarea);

        textarea.SetFocus();
        Settle();
        Type(scene, PhysicalKey.H, "h");
        Type(scene, PhysicalKey.I, "i");

        Assert.Equal(["focus", "input:h", "input:hi"], log.Take());
        Assert.Equal("hi", textarea.Value);
    }

    [AvaloniaFact]
    public void B02_Enter_Types_A_Line_Feed_And_Raises_IonInput_Not_IonChange()
    {
        var (textarea, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(textarea);
        textarea.SetFocus();
        Settle();
        Type(scene, PhysicalKey.H, "h");
        log.Take();

        // Measured "enter": ionInput "hi\n", and nothing else, whatever the platform's line break.
        Key(scene, PhysicalKey.Enter);
        Assert.Equal(["input:h\n"], log.Take());
        Assert.Equal("h\n", textarea.Value);

        Type(scene, PhysicalKey.X, "x");
        Assert.Equal(["input:h\nx"], log.Take());
    }

    [AvaloniaFact]
    public void B03_Blur_Raises_IonChange_When_The_Value_Differs_From_Focus_Then_IonBlur()
    {
        var (textarea, other, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(textarea);

        textarea.SetFocus();
        Settle();
        Type(scene, PhysicalKey.X, "x");
        Key(scene, PhysicalKey.Enter);
        other.Focus();
        Settle();
        Assert.Equal(["focus", "input:x", "input:x\n", "change:x\n", "blur"], log.Take());

        // A focus and blur with no edit: no change.
        textarea.SetFocus();
        Settle();
        other.Focus();
        Settle();
        Assert.Equal(["focus", "blur"], log.Take());

        // Measured "grow-code": a value set in code while focused is a change on blur.
        textarea.SetFocus();
        Settle();
        textarea.Value = "code";
        other.Focus();
        Settle();
        Assert.Equal(["focus", "change:code", "blur"], log.Take());
    }

    [AvaloniaFact]
    public void B04_A_Value_Set_In_Code_Raises_Nothing_Shows_And_Can_Replace_An_Edit()
    {
        var (textarea, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(textarea);

        textarea.Value = "one\ntwo";
        Settle();
        Assert.Empty(log.Take());
        Assert.Equal("one\ntwo", Native(textarea).Text);

        textarea.Value = null;
        Settle();
        Assert.Equal("", Native(textarea).Text);

        textarea.IonInput += (_, e) => textarea.Value = e.Value?.ToUpperInvariant();
        textarea.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        Assert.Equal("A", textarea.Value);
        Assert.Equal("A", Native(textarea).Text);
    }

    [AvaloniaFact]
    public void B05_Debounce_Raises_IonInput_Once_With_The_Last_Value_After_The_Wait()
    {
        using var clock = new TestClock();
        var (textarea, _, scene) = Stub(new AvonicTextarea { Debounce = 100 });
        using var _ = scene;
        var log = Log.Of(textarea);
        textarea.SetFocus();
        Settle();
        log.Take();

        clock.At(0);
        Type(scene, PhysicalKey.A, "a");
        clock.At(30);
        Type(scene, PhysicalKey.B, "b");
        clock.At(129);
        Assert.Empty(log.Take());
        Assert.Equal("ab", textarea.Value);

        clock.At(130);
        Assert.Equal(["input:ab"], log.Take());

        textarea.Debounce = null;
        Type(scene, PhysicalKey.C, "c");
        Assert.Equal(["input:abc"], log.Take());
    }

    [AvaloniaFact]
    public void B06_ClearOnEdit_Clears_On_The_First_Key_That_Edits_Enter_Included_Once_Per_Focus()
    {
        var (textarea, other, scene) = Stub(new AvonicTextarea { ClearOnEdit = true, Value = "abc" });
        using var _ = scene;
        var log = Log.Of(textarea);
        textarea.SetFocus();
        Settle();
        log.Take();

        // Measured "coeShift": Shift doesn't edit, and doesn't use it up.
        Key(scene, PhysicalKey.ShiftLeft);
        Assert.Empty(log.Take());

        // Measured "coeEnter": ionInput "" and then the line break, unlike the input, which ignores Enter.
        Key(scene, PhysicalKey.Enter);
        Assert.Equal(["input:", "input:\n"], log.Take());

        // Once per focus.
        Type(scene, PhysicalKey.Z, "z");
        Assert.Equal(["input:\nz"], log.Take());

        // Measured "coeArrow": an arrow clears too.
        other.Focus();
        Settle();
        textarea.Value = "abc";
        textarea.SetFocus();
        Settle();
        log.Take();
        Key(scene, PhysicalKey.ArrowLeft);
        Assert.Equal(["input:"], log.Take());
        Assert.Equal("", textarea.Value);
    }

    [AvaloniaFact]
    public void B06_ClearOnEdit_Is_Off_By_Default_And_Clears_Text_With_No_Key()
    {
        var (textarea, other, scene) = Stub(new AvonicTextarea { Value = "abc" });
        using var _ = scene;
        textarea.SetFocus();
        Settle();
        Native(textarea).CaretIndex = 3;
        Type(scene, PhysicalKey.D, "d");
        Assert.Equal("abcd", textarea.Value);

        // A soft keyboard's text, with no key down.
        other.Focus();
        Settle();
        textarea.ClearOnEdit = true;
        textarea.SetFocus();
        Settle();
        scene.Window.KeyTextInput("e");
        Settle();
        Assert.Equal("e", textarea.Value);
    }

    // ── 7 to 9: focus ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B07_A_Click_On_The_Label_Focuses_The_Field_But_Not_On_The_Bottom_Or_A_Slotted_Button()
    {
        var clicked = 0;
        var button = new Button { Content = "B" };
        button.Click += (_, _) => clicked++;
        var (textarea, other, scene) = Stub(new AvonicTextarea { Label = "Label", HelperText = "Helper", EndContent = button });
        using var _ = scene;
        Render();

        Click(scene, Centre(Part<Control>(textarea, "PART_Label"), scene));
        Assert.Same(Native(textarea), Focused(textarea));

        // On the bottom's text itself, which is what takes a click on a TextBlock.
        other.Focus();
        Settle();
        var helper = Bounds(Part<Control>(textarea, "PART_HelperText"), scene.Window);
        Click(scene, new Point(helper.X + 4, helper.Center.Y));
        Assert.Same(other, Focused(textarea));

        Click(scene, Centre(button, scene));
        Assert.Equal(1, clicked);
        Assert.NotSame(Native(textarea), Focused(textarea));
    }

    [AvaloniaFact]
    public void B08_SetFocus_Focuses_The_Native_Textarea_That_GetInputElement_Returns()
    {
        var (textarea, _, scene) = Stub();
        using var _ = scene;
        var native = textarea.GetInputElement();
        Assert.Same(Native(textarea), native);

        textarea.SetFocus();
        Settle();
        Assert.Same(native, Focused(textarea));
        Assert.False(textarea.Focusable);
    }

    [AvaloniaFact]
    public void B09_A_Tap_On_An_Items_Padding_Focuses_The_Textarea() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Label = "Label" };
        var log = Log.Of(textarea);
        var item = Item(textarea);
        using var scene = new Scene(new StackPanel { Children = { item, new TextBox() } });
        Render();

        var box = Bounds(item, scene.Window);
        var at = new Point(box.X + 4, box.Center.Y);
        Assert.False(Bounds(textarea, scene.Window).Contains(at));
        Click(scene, at);

        Assert.Same(Native(textarea), Focused(textarea));
        Assert.Equal(["focus"], log.Take());
    });

    // ── 10 to 13: the native textarea's size ─────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_The_Field_Is_Rows_Lines_High_19px_A_Line_And_Scrolls_Past_Them() => InstalledTheme.Run(_ =>
    {
        var two = new AvonicTextarea { Label = "Start" };
        var four = new AvonicTextarea { Label = "Rows 4", Rows = 4, Value = "Value" };
        var one = new AvonicTextarea { Rows = 1 };
        var zero = new AvonicTextarea { Rows = 0 };
        var full = new AvonicTextarea { Label = "Fixed", LabelPlacement = TextareaLabelPlacement.Fixed, Value = "One\ntwo\nthree\nfour" };
        using var scene = new Scene(new StackPanel { Children = { Item(two), Item(four), Item(one), Item(zero), Item(full) } });
        Render();

        // Measured a, h, r1: 18 + 2 × 19 + 8; 18 + 4 × 19 + 8; 18 + 19 + 8. rows="0" isn't valid, so 2.
        Assert.Equal(19, Native(two).LineHeight);
        Assert.Equal(64, two.Bounds.Height, 0.5);
        Assert.Equal(102, four.Bounds.Height, 0.5);
        Assert.Equal(45, one.Bounds.Height, 0.5);
        Assert.Equal(64, zero.Bounds.Height, 0.5);

        // Measured e: four lines in a 64 px field, which scrolls: scrollHeight 102.
        Assert.Equal(64, full.Bounds.Height, 0.5);
        var viewer = Part<ScrollViewer>(Native(full), "PART_ScrollViewer");
        Assert.Equal(38, viewer.Viewport.Height, 0.5);
        Assert.Equal(76, viewer.Extent.Height, 0.5);

        // Not focused, it shows its first lines, as Chromium does, though the caret is at the end of the value.
        full.Value = "Five\nsix\nseven\neight";
        Render();
        Assert.Equal(0, viewer.Offset.Y);
    });

    [AvaloniaFact]
    public void B10_The_Lines_Are_Chromiums_Normal_Line_Height_Whatever_Is_Inherited() => InstalledTheme.Run(_ =>
    {
        var big = new AvonicTextarea { FontSize = 32 };
        var page = Page(new AvonicTextarea(), big);
        page.SetValue(TextBlock.LineHeightProperty, 40.0);
        using var scene = new Scene(page);
        Render();

        // line-height: normal on .textarea-wrapper: Roboto's ascent and descent rounded, 15 + 4 at 16 px, 30 + 8 at 32 px.
        Assert.Equal(19, Native((AvonicTextarea)page.Children[0]).LineHeight);
        Assert.Equal(38, Native(big).LineHeight);
    });

    [AvaloniaFact]
    public void B11_AutoGrow_Is_As_Many_Lines_As_The_Value_And_Never_Fewer_Than_Rows() => InstalledTheme.Run(_ =>
    {
        var grow = new AvonicTextarea { AutoGrow = true, Rows = 1 };
        var fixedRows = new AvonicTextarea { Rows = 1 };
        using var scene = new Scene(Page(grow, fixedRows, new TextBox()));
        Render();

        // Measured grow0 to shrink: 45, 64, 83, 64; the same value without autoGrow stays 45.
        Assert.Equal(45, grow.Bounds.Height, 0.5);
        grow.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        Render();
        Assert.Equal(45, grow.Bounds.Height, 0.5);

        Key(scene, PhysicalKey.Enter);
        Type(scene, PhysicalKey.B, "b");
        Render();
        Assert.Equal(64, grow.Bounds.Height, 0.5);

        Key(scene, PhysicalKey.Enter);
        Type(scene, PhysicalKey.C, "c");
        Render();
        Assert.Equal(83, grow.Bounds.Height, 0.5);

        Key(scene, PhysicalKey.Backspace);
        Key(scene, PhysicalKey.Backspace);
        Render();
        Assert.Equal(64, grow.Bounds.Height, 0.5);

        // Set in code: grow-code, 4 lines.
        grow.Value = "x\ny\nz\nw";
        fixedRows.Value = "x\ny\nz\nw";
        Render();
        Assert.Equal(102, grow.Bounds.Height, 0.5);
        Assert.Equal(45, fixedRows.Bounds.Height, 0.5);

        // Default rows: never fewer than 2.
        grow.Rows = null;
        grow.Value = "";
        Render();
        Assert.Equal(64, grow.Bounds.Height, 0.5);
    });

    [AvaloniaFact]
    public void B12_Cols_Makes_The_Host_As_Wide_As_Its_Content_Unless_It_Grows() => InstalledTheme.Run(_ =>
    {
        var ten = new AvonicTextarea { Cols = 10 };
        var twenty = new AvonicTextarea { Cols = 20 };
        var forty = new AvonicTextarea { Cols = 40 };
        var growing = new AvonicTextarea { Cols = 20, AutoGrow = true };
        using var scene = new Scene(Page(ten, twenty, forty, growing), 358, 600);
        Render();

        // Measured c10, c20, c40: cols × 8 + 4; c20g: 100%.
        Assert.Equal(84, ten.Bounds.Width, 0.5);
        Assert.Equal(164, twenty.Bounds.Width, 0.5);
        Assert.Equal(324, forty.Bounds.Width, 0.5);
        Assert.Equal(0, ten.Bounds.X, 0.5);
        Assert.Equal(358, growing.Bounds.Width, 0.5);

        twenty.Cols = null;
        Render();
        Assert.Equal(358, twenty.Bounds.Width, 0.5);
    });

    [AvaloniaFact]
    public void B12_Cols_Fits_The_Host_To_Its_Content_On_The_Stub_Too()
    {
        var (textarea, other, scene) = Stub(new AvonicTextarea { Cols = 10 });
        using var _ = scene;
        Assert.True(textarea.Bounds.Width < other.Bounds.Width / 2, $"{textarea.Bounds.Width}");

        textarea.AutoGrow = true;
        Settle();
        Assert.Equal(other.Bounds.Width, textarea.Bounds.Width, 0.5);
    }

    // Chromium's columns: ceil(cols × c) + 4, where c is the x glyph's advance, or that rounded when it rounds up. 13 px's 6.45
    // stays, where rounding it would give 124. Each measured in Chromium.
    [AvaloniaFact]
    public void B12_Cols_Follows_Chromiums_Rounding_At_Each_Size() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Cols = 20 };
        using var scene = new Scene(Page(textarea), 358, 600);

        foreach (var (size, cols, width) in new[]
                 {
                     (13.0, 20, 133.0), (15.0, 20, 153.0), (17.0, 20, 173.0), (21.0, 20, 213.0), (12.5, 33, 209.0), (13.5, 7, 53.0),
                     (14.3, 10, 75.0), (16.6, 3, 29.0), (19.2, 10, 104.0), (12.0, 20, 124.0), (16.0, 20, 164.0),
                     // Rounded up where the fraction is under a half: 52.09, 59.04 and 45.14.
                     (15.0, 7, 57.0), (17.0, 7, 64.0), (13.0, 7, 50.0),
                 })
        {
            textarea.FontSize = size;
            textarea.Cols = cols;
            Render();
            Assert.True(Math.Abs(width - textarea.Bounds.Width) < 0.5, $"{size} px, {cols} columns: {textarea.Bounds.Width}, not {width}");
        }
    });

    // :host-context(ion-item)[slot=start] and [slot=end]: width: auto, as wide as 20 columns, or cols, whatever the value or the
    // placeholder. autoGrow's overflow: hidden leaves no room for a scroll bar, and a long line widens it to what's left. Each
    // measured in Chromium.
    [AvaloniaFact]
    public void B12_In_An_Items_Start_Or_End_Slot_The_Textarea_Is_As_Wide_As_Its_Columns() => InstalledTheme.Run(_ =>
    {
        static AvonicItem Start(AvonicTextarea textarea) => new() { StartContent = textarea, Content = new AvonicLabel { Content = "L" } };
        var bare = new AvonicTextarea { [AutomationProperties.NameProperty] = "x" };
        var valued = new AvonicTextarea { Value = "hello", [AutomationProperties.NameProperty] = "x" };
        var labelled = new AvonicTextarea { Label = "Notes" };
        var end = new AvonicTextarea { Placeholder = "A placeholder that is long enough to matter here", [AutomationProperties.NameProperty] = "x" };
        var ten = new AvonicTextarea { Cols = 10, [AutomationProperties.NameProperty] = "x" };
        var growing = new AvonicTextarea { AutoGrow = true, Value = "hi", [AutomationProperties.NameProperty] = "x" };
        var growingTen = new AvonicTextarea { AutoGrow = true, Cols = 10, Value = "hi", [AutomationProperties.NameProperty] = "x" };
        var growingLong = new AvonicTextarea { AutoGrow = true, Value = "a rather long line of text that goes on and on and on", [AutomationProperties.NameProperty] = "x" };
        var stacked = new AvonicTextarea { Label = "Notes", LabelPlacement = TextareaLabelPlacement.Stacked };
        var full = new AvonicTextarea { [AutomationProperties.NameProperty] = "x" };
        var sized = new AvonicTextarea { FontSize = 13, [AutomationProperties.NameProperty] = "x" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                Start(bare), Start(valued), Start(labelled),
                new AvonicItem { Content = new AvonicLabel { Content = "L" }, EndContent = end },
                Start(ten), Start(growing), Start(growingTen), Start(growingLong), Start(stacked),
                new AvonicItem { Content = full },
                Start(sized),
            },
        });
        Render();

        Assert.Equal(16, Bounds(bare, scene.Window).X, 0.6);
        Assert.Equal(164, bare.Bounds.Width, 0.6);
        Assert.Equal(164, Native(bare).Bounds.Width, 0.6);
        Assert.Equal(164, valued.Bounds.Width, 0.6);
        Assert.Equal(222.52, labelled.Bounds.Width, 0.6);
        Assert.Equal(164, Native(labelled).Bounds.Width, 0.6);
        Assert.Equal(164, end.Bounds.Width, 0.6);
        Assert.Equal(374, Bounds(end, scene.Window).Right, 0.6);
        Assert.Equal(84, ten.Bounds.Width, 0.6);
        Assert.Equal(160, growing.Bounds.Width, 0.6);
        Assert.Equal(80, growingTen.Bounds.Width, 0.6);
        Assert.Equal(342, growingLong.Bounds.Width, 0.6);
        Assert.Equal(164, stacked.Bounds.Width, 0.6);
        Assert.Equal(133, sized.Bounds.Width, 0.6);

        // In the default slot it's width: 100%.
        Assert.Equal(358, full.Bounds.Width, 0.6);
    });

    // The native textarea is width: 100%, which makes it compressible: a textarea in the end slot gives way to the start slot's,
    // where a label beside it, which is flex: 1, wraps instead. Measured in Chromium.
    [AvaloniaFact]
    public void B12_In_A_Slot_With_Too_Little_Room_The_Textarea_Gives_Way() => InstalledTheme.Run(_ =>
    {
        var beside = new AvonicTextarea { [AutomationProperties.NameProperty] = "x" };
        var start = new AvonicTextarea { [AutomationProperties.NameProperty] = "x" };
        var end = new AvonicTextarea { [AutomationProperties.NameProperty] = "x" };
        // Even growing, with a word wider than what's left: its copy of the text doesn't hold it open.
        var growing = new AvonicTextarea { AutoGrow = true, Value = "supercalifragilisticexpialidocious and more", [AutomationProperties.NameProperty] = "x" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItem { StartContent = beside, Content = new AvonicLabel { Content = "A label that is long enough to want the whole row for itself" } },
                new AvonicItem { StartContent = start, EndContent = end },
                new AvonicItem { StartContent = new AvonicTextarea { [AutomationProperties.NameProperty] = "x" }, EndContent = growing },
            },
        });
        Render();

        Assert.Equal(164, beside.Bounds.Width, 0.6);
        Assert.Equal(164, start.Bounds.Width, 0.6);
        Assert.Equal(162, end.Bounds.Width, 0.6);
        Assert.Equal(374, Bounds(end, scene.Window).Right, 0.6);
        Assert.Equal(162, growing.Bounds.Width, 0.6);
    });

    // The slot is read again when the textarea is moved, as hostContext and the slot attribute are at render.
    [AvaloniaFact]
    public void B12_A_Textarea_Moved_Into_A_Start_Slot_Becomes_As_Wide_As_Its_Columns() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { [AutomationProperties.NameProperty] = "x" };
        var first = new AvonicItem { Content = textarea };
        var second = new AvonicItem { Content = new AvonicLabel { Content = "L" } };
        using var scene = new Scene(new StackPanel { Children = { first, second } });
        Render();
        Assert.Equal(358, textarea.Bounds.Width, 0.6);

        first.Content = null;
        second.StartContent = textarea;
        Render();
        Assert.Equal(164, textarea.Bounds.Width, 0.6);
    });

    [AvaloniaFact]
    public void B13_Wrap_Off_Scrolls_Sideways_On_The_Stub_Too()
    {
        var textarea = new AvonicTextarea { Wrap = TextareaWrap.Off, Value = "A long line that will not wrap because wrap is off here", Width = 120 };
        var (_, _, scene) = Stub(textarea);
        using var _ = scene;
        var viewer = Part<ScrollViewer>(Native(textarea), "PART_ScrollViewer");
        Assert.True(viewer.Extent.Width > viewer.Viewport.Width, $"{viewer.Extent.Width}");
    }

    [AvaloniaFact]
    public void B16_An_Apps_Padding_Keys_Win() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Label = "L" };
        textarea.Resources["Avonic.Textarea.PaddingTop"] = 30.0;
        textarea.Resources["Avonic.Textarea.PaddingBottom"] = 2.0;
        using var scene = new Scene(Page(textarea));
        Render();

        // 30 + 2 × 19 + 2, and the label's text 30 down, as its padding is the field's.
        Assert.Equal(70, textarea.Bounds.Height, 0.5);
        Assert.Equal(30, Bounds(Part<Control>(textarea, "PART_LabelText"), textarea).Y, 0.5);
    });

    [AvaloniaFact]
    public void B13_Wrap_Off_Keeps_A_Line_On_One_Line() => InstalledTheme.Run(_ =>
    {
        const string Long = "A long line that will not wrap because wrap is off here";
        var off = new AvonicTextarea { Wrap = TextareaWrap.Off, Value = Long };
        var soft = new AvonicTextarea { Wrap = TextareaWrap.Soft, Value = Long };
        var hard = new AvonicTextarea { Wrap = TextareaWrap.Hard, Value = Long };
        var none = new AvonicTextarea { Value = Long, AutoGrow = true, Rows = 1 };
        using var scene = new Scene(Page(off, soft, hard, none), 200, 600);
        Render();

        Assert.Equal(TextWrapping.NoWrap, Native(off).TextWrapping);
        Assert.Equal(TextWrapping.Wrap, Native(soft).TextWrapping);
        Assert.Equal(TextWrapping.Wrap, Native(hard).TextWrapping);
        Assert.Equal(TextWrapping.Wrap, Native(none).TextWrapping);

        // Wrapped, it's several lines; off, one, which scrolls sideways.
        Assert.True(none.Bounds.Height > 45, $"{none.Bounds.Height}");
        Assert.True(Part<ScrollViewer>(Native(soft), "PART_ScrollViewer").Extent.Height > 19);
        var viewer = Part<ScrollViewer>(Native(off), "PART_ScrollViewer");
        Assert.Equal(19, viewer.Extent.Height, 0.5);
        Assert.True(viewer.Extent.Width > 200, $"{viewer.Extent.Width}");
    });

    // ── 14 to 16: layout ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_Row_Placements_Measured_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicTextarea { Label = "Start", Placeholder = "Placeholder" };
        var end = new AvonicTextarea { Label = "End", LabelPlacement = TextareaLabelPlacement.End, Value = "Value" };
        var fixedLabel = new AvonicTextarea { Label = "Fixed", LabelPlacement = TextareaLabelPlacement.Fixed, Value = "Value" };
        var longLabel = new AvonicTextarea { Label = "A start label that is far too long to fit", Value = "Value" };
        using var scene = new Scene(new StackPanel { Children = { Item(start), Item(end), Item(fixedLabel), Item(longLabel) } });
        Render();

        // Measured a: the label at the top, 18 above and 8 below its 19 px line, 16 from the field.
        var label = Part<Control>(start, "PART_Label");
        AssertBox(0, 0, 34.48, 45, Bounds(label, start), 0.8);
        Assert.Equal(18, Bounds(Part<Control>(start, "PART_LabelText"), start).Y, 0.5);
        Assert.Equal(label.Bounds.Width + 16, Bounds(Native(start), start).X, 0.5);
        Assert.Equal(64, Native(start).Bounds.Height, 0.5);

        // f: mirrored, the label's right edge at the host's.
        var endLabel = Bounds(Part<Control>(end, "PART_Label"), end);
        Assert.Equal(358, endLabel.Right, 0.5);
        Assert.Equal(0, Bounds(Native(end), end).X, 0.5);
        Assert.Equal(358 - endLabel.Width - 16, Native(end).Bounds.Width, 0.5);

        // e: 100 px.
        Assert.Equal(100, Part<Control>(fixedLabel, "PART_Label").Bounds.Width, 0.5);
        Assert.Equal(116, Bounds(Native(fixedLabel), fixedLabel).X, 0.5);

        Assert.Equal(200, Part<Control>(longLabel, "PART_Label").Bounds.Width, 0.5);
    });

    [AvaloniaFact]
    public void B15_Column_Placements_Measured() => InstalledTheme.Run(_ =>
    {
        var floating = new AvonicTextarea { Label = "Floating", LabelPlacement = TextareaLabelPlacement.Floating };
        var raised = new AvonicTextarea { Label = "Floating", LabelPlacement = TextareaLabelPlacement.Floating, Value = "Value" };
        var stacked = new AvonicTextarea { Label = "Auto grow", LabelPlacement = TextareaLabelPlacement.Stacked, AutoGrow = true, Value = "One\ntwo\nthree\nfour" };
        var outline = new AvonicTextarea { Label = "Outline", LabelPlacement = TextareaLabelPlacement.Floating, Fill = TextareaFill.Outline, Value = "Value" };
        using var scene = new Scene(new StackPanel { Children = { Item(floating), Item(raised), Item(stacked), Page(outline) } });
        foreach (var textarea in new[] { floating, raised, stacked, outline })
        {
            Part<Control>(textarea, "PART_Label").Transitions = [];
        }

        Render();

        // Measured b: the label 19 high, on the field (translateY(100%)); the field 8 under the label and 48 high, as
        // .textarea-wrapper-inner is at least 56 too: 75 in all.
        Assert.Equal(75, floating.Bounds.Height, 0.5);
        AssertBox(0, 19, 57.52, 19, Bounds(Part<Control>(floating, "PART_Label"), floating), 0.8);
        AssertBox(0, 27, 358, 48, Bounds(Native(floating), floating));

        // c: raised, translateY(50%) scale(0.75).
        AssertBox(0, 9.5, 43.14, 14.25, Bounds(Part<Control>(raised, "PART_Label"), raised), 0.8);
        Assert.Equal(27, Bounds(Native(raised), raised).Y, 0.5);

        // g: auto-grow with four lines: 19 + 8 + 4 × 19 + 8.
        Assert.Equal(111, stacked.Bounds.Height, 0.5);

        // j: the outline's label sits on the border, out of the flow; the field 12 down, 46 high, 58 in all.
        Assert.Equal(58, outline.Bounds.Height, 0.5);
        Assert.Equal(-6.08, Bounds(Part<Control>(outline, "PART_Label"), outline).Y, 0.6);
        AssertBox(16, 12, 358, 46, Bounds(Native(outline), outline));
    });

    [AvaloniaFact]
    public void B15_A_Floating_Fields_Text_Is_Hidden_Until_It_Has_Focus_Or_A_Value() => InstalledTheme.Run(_ =>
    {
        var (textarea, other, scene) = Stub(new AvonicTextarea { Label = "L", LabelPlacement = TextareaLabelPlacement.Floating });
        using var owned = scene;
        Native(textarea).Transitions = [];
        Part<Control>(textarea, "PART_Label").Transitions = [];
        Assert.Equal(0, Native(textarea).Opacity);
        Assert.Equal(1, Part<Control>(textarea, "PART_Label").GetValue(Css.ScaleProperty));

        textarea.SetFocus();
        Settle();
        Assert.Equal(1, Native(textarea).Opacity);
        Assert.Equal(0.75, Part<Control>(textarea, "PART_Label").GetValue(Css.ScaleProperty));

        other.Focus();
        Settle();
        Assert.Equal(0, Native(textarea).Opacity);

        textarea.Value = "v";
        Settle();
        Assert.Equal(1, Native(textarea).Opacity);

        // Start content floats the label, but the field stays hidden: the rule reads focus and value only.
        textarea.Value = "";
        textarea.StartContent = new Border { Width = 4, Height = 4 };
        Settle();
        Assert.Equal(0, Native(textarea).Opacity);
        Assert.Equal(0.75, Part<Control>(textarea, "PART_Label").GetValue(Css.ScaleProperty));
    });

    [AvaloniaFact]
    public void B16_The_Wrapper_Is_At_Least_44_And_56_When_Floating_Stacked_Or_Filled() => InstalledTheme.Run(_ =>
    {
        static AvonicTextarea Short(AvonicTextarea textarea)
        {
            textarea.Rows = 1;
            textarea.Resources["Avonic.Textarea.PaddingTop"] = 0.0;
            textarea.Resources["Avonic.Textarea.PaddingBottom"] = 0.0;
            return textarea;
        }

        var plain = Short(new AvonicTextarea { Label = "L" });
        var solid = Short(new AvonicTextarea { Label = "L", Fill = TextareaFill.Solid });
        var outline = Short(new AvonicTextarea { Label = "L", Fill = TextareaFill.Outline });
        var stacked = Short(new AvonicTextarea { Label = "L", LabelPlacement = TextareaLabelPlacement.Stacked });
        using var scene = new Scene(Page(plain, solid, outline, stacked));
        Render();

        // A one-line field with no padding is 19: the minimums show.
        Assert.Equal(44, plain.Bounds.Height, 0.5);
        Assert.Equal(57, solid.Bounds.Height, 0.5);
        Assert.Equal(56, outline.Bounds.Height, 0.5);

        // In a column, .textarea-wrapper-inner is at least 56 under the label.
        Assert.Equal(56, Part<Control>(stacked, "PART_NativeWrapper").Bounds.Height, 0.5);
    });

    // ── 17, 18: the highlight ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_The_Highlight_Grows_On_Focus_Outside_An_Item_And_Not_With_The_Outline() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicTextarea { Label = "Plain" };
        var outline = new AvonicTextarea { Label = "Outline", Fill = TextareaFill.Outline };
        var inItem = new AvonicTextarea { Label = "Item" };
        using var scene = new Scene(Page(plain, outline, Item(inItem)));
        Render();

        var highlight = Part<Control>(plain, "PART_Highlight");
        Assert.True(highlight.IsVisible);
        Assert.False(Part<Control>(outline, "PART_Highlight").IsVisible);
        Assert.False(Part<Control>(inItem, "PART_Highlight").IsVisible);
        Assert.Equal(0, Bounds(highlight, plain).Width, 0.01);

        var transition = Assert.Single(highlight.Transitions!.OfType<Avalonia.Animation.DoubleTransition>());
        Assert.Equal(Css.ScaleProperty, transition.Property);
        Assert.Equal(TimeSpan.FromMilliseconds(200), transition.Duration);

        // Focused: 2 px, 1 px below the 64 px wrapper.
        highlight.Transitions = [];
        plain.SetFocus();
        Settle();
        Render();
        AssertBox(0, 63, plain.Bounds.Width, 2, Bounds(highlight, plain));
    });

    [AvaloniaFact]
    public void B18_The_Highlight_Colour_Is_Primary_A_Colours_Base_Then_Valid_Then_Invalid() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Label = "L", LabelPlacement = TextareaLabelPlacement.Stacked, HelperText = "Helper" };
        using var scene = new Scene(Page(textarea, new TextBox()));
        var label = Part<Control>(textarea, "PART_Label");
        label.Transitions = [];
        var bottom = Part<Border>(textarea, "PART_Bottom");
        Assert.Equal(Color.FromArgb(0x21, 0, 0, 0), BrushColor(bottom.BorderBrush));
        textarea.SetFocus();
        Settle();

        Assert.Equal(Primary, BrushColor(Part<Control>(textarea, "PART_Highlight").GetValue(Border.BackgroundProperty)));
        Assert.Equal(Primary, BrushColor(Native(textarea).CaretBrush));
        Assert.Equal(Primary, BrushColor(label.GetValue(TextElement.ForegroundProperty)));

        textarea.Color = "danger";
        Settle();
        Assert.Equal(Danger, BrushColor(textarea.HighlightBrush));

        textarea.Color = null;
        textarea.Classes.Add(AvonicTextarea.ValidClass);
        Settle();
        Assert.Equal(Success, BrushColor(textarea.HighlightBrush));
        Assert.Equal(Success, BrushColor(bottom.BorderBrush));

        textarea.Classes.Add(AvonicTextarea.TouchedClass);
        textarea.Classes.Add(AvonicTextarea.InvalidClass);
        Settle();
        Assert.Equal(Danger, BrushColor(textarea.HighlightBrush));

        // Invalid and blurred: the label and the bottom border keep it.
        scene.Window.GetVisualDescendants().OfType<TextBox>().Last().Focus();
        Settle();
        Assert.Equal(Danger, BrushColor(label.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(Danger, BrushColor(bottom.BorderBrush));
    });

    // ── 19, 20: fills and shapes ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_Solid_Background_And_Border_At_Rest_Hovered_And_Focused() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Label = "Solid", Fill = TextareaFill.Solid, LabelPlacement = TextareaLabelPlacement.Floating, HelperText = "Helper text" };
        using var scene = new Scene(Page(textarea, new TextBox()));
        var wrapper = Part<Border>(textarea, "PART_Wrapper");
        wrapper.Transitions = [];
        Render();

        // Measured i: 19 + 56 + the 1 px border.
        Assert.Equal(76, wrapper.Bounds.Height, 0.5);
        Assert.Equal(Color.Parse("#F2F2F2"), BrushColor(wrapper.Background));
        Assert.Equal(Color.Parse("#808080"), BrushColor(wrapper.BorderBrush));
        Assert.Equal(new Thickness(0, 0, 0, 1), wrapper.BorderThickness);
        Assert.Equal(new CornerRadius(4, 4, 0, 0), wrapper.CornerRadius);
        Assert.Equal(new Thickness(16, 0, 16, 0), wrapper.Padding);
        Assert.Equal(0, Part<Border>(textarea, "PART_Bottom").BorderThickness.Top);

        scene.Window.MouseMove(Centre(wrapper, scene), RawInputModifiers.None);
        Settle();
        Assert.Equal(Color.Parse("#E6E6E6"), BrushColor(wrapper.Background));
        Assert.Equal(Color.Parse("#404040"), BrushColor(wrapper.BorderBrush));

        scene.Window.MouseMove(new Point(380, 830), RawInputModifiers.None);
        textarea.SetFocus();
        Settle();
        Assert.Equal(Color.Parse("#D9D9D9"), BrushColor(wrapper.Background));
        Assert.Equal(Color.Parse("#404040"), BrushColor(wrapper.BorderBrush));
    });

    [AvaloniaFact]
    public void B19_A_Finger_Doesnt_Hover() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Label = "Solid", Fill = TextareaFill.Solid };
        using var scene = new Scene(Page(textarea));
        var wrapper = Part<Border>(textarea, "PART_Wrapper");
        wrapper.Transitions = [];
        Render();

        void Enter(PointerType type) => textarea.RaiseEvent(new PointerEventArgs(
            InputElement.PointerEnteredEvent, textarea, new Pointer(Pointer.GetNextFreeId(), type, true), scene.Window,
            Centre(wrapper, scene), 0, new PointerPointProperties(), KeyModifiers.None));

        Enter(PointerType.Touch);
        Settle();
        Assert.Equal(Color.Parse("#F2F2F2"), BrushColor(wrapper.Background));

        Enter(PointerType.Pen);
        Settle();
        Assert.Equal(Color.Parse("#E6E6E6"), BrushColor(wrapper.Background));

        // Leaving the tree ends it.
        ((StackPanel)textarea.Parent!).Children.Remove(textarea);
        Assert.DoesNotContain(":ion-hover", textarea.Classes);
    });

    [AvaloniaFact]
    public void B20_The_Outline_Notch_Opens_For_A_Floating_Label_And_Round_Shapes() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Label = "Outline", Fill = TextareaFill.Outline, LabelPlacement = TextareaLabelPlacement.Floating, Value = "Value" };
        var resting = new AvonicTextarea { Label = "Outline start", Fill = TextareaFill.Outline, Value = "v" };
        var round = new AvonicTextarea { Label = "Round", Fill = TextareaFill.Outline, Shape = TextareaShape.Round, LabelPlacement = TextareaLabelPlacement.Stacked };
        var solidRound = new AvonicTextarea { Label = "Round", Fill = TextareaFill.Solid, Shape = TextareaShape.Round };
        var plainRound = new AvonicTextarea { Label = "Round", Shape = TextareaShape.Round };
        using var scene = new Scene(Page(textarea, resting, round, solidRound, plainRound, new TextBox()));
        Part<Control>(textarea, "PART_Label").Transitions = [];
        Render();

        var start = Part<Border>(textarea, "PART_OutlineStart");
        var notch = Part<Border>(textarea, "PART_OutlineNotch");
        var end = Part<Border>(textarea, "PART_OutlineEnd");

        // Measured j: start 12 wide, the notch the label at 12 px plus 8.
        AssertBox(0, 0, 12, 58, Bounds(start, textarea));
        Assert.Equal(45.63, notch.Bounds.Width, 1.0);
        Assert.Equal(new Thickness(1, 1, 0, 1), start.BorderThickness);
        Assert.Equal(new CornerRadius(4, 0, 0, 4), start.CornerRadius);
        Assert.Equal(Color.Parse("#B3B3B3"), BrushColor(start.BorderBrush));
        Assert.Equal(new Thickness(0, 0, 0, 1), notch.BorderThickness);
        Assert.Equal(new Thickness(0, 1, 0, 1), Part<Border>(resting, "PART_OutlineNotch").BorderThickness);

        // Measured k: 28 px corners, padding 32. Solid round: top corners 16; no fill: 16.
        Assert.Equal(new CornerRadius(28, 0, 0, 28), Part<Border>(round, "PART_OutlineStart").CornerRadius);
        Assert.Equal(new Thickness(32, 0, 32, 0), Part<Border>(round, "PART_Wrapper").Padding);
        Assert.Equal(new CornerRadius(16, 16, 0, 0), Part<Border>(solidRound, "PART_Wrapper").CornerRadius);
        Assert.Equal(new CornerRadius(16), Part<Border>(plainRound, "PART_Wrapper").CornerRadius);

        // Focused: 2 px in the highlight colour.
        textarea.SetFocus();
        Settle();
        Assert.Equal(new Thickness(2, 2, 0, 2), start.BorderThickness);
        Assert.Equal(Primary, BrushColor(end.BorderBrush));
    });

    // ── 21: the bottom ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B21_The_Bottom_Shows_Only_With_Hint_Text_Or_A_Counter_And_Errors_Replace_The_Helper()
    {
        var (textarea, _, scene) = Stub(new AvonicTextarea { Counter = true });
        using var _ = scene;
        var bottom = Part<Control>(textarea, "PART_Bottom");

        Assert.False(bottom.IsVisible);
        Assert.Null(textarea.CounterText);

        textarea.Maxlength = 100;
        Settle();
        Assert.True(bottom.IsVisible);
        Assert.Equal("0 / 100", textarea.CounterText);

        textarea.Value = "ab\nc";
        Assert.Equal("4 / 100", textarea.CounterText);
        textarea.CounterFormatter = (length, max) => $"{max - length} left";
        Assert.Equal("96 left", textarea.CounterText);
        textarea.CounterFormatter = (_, _) => throw new InvalidOperationException();
        Assert.Equal("4 / 100", textarea.CounterText);

        // renderBottomContent reads counter and maxlength, not the text: a formatter's null keeps the bottom.
        textarea.CounterFormatter = (_, _) => null!;
        Settle();
        Assert.Null(textarea.CounterText);
        Assert.True(bottom.IsVisible);
        textarea.CounterFormatter = null;

        textarea.Counter = false;
        textarea.HelperText = "Helper";
        textarea.ErrorText = "Error";
        Settle();
        Assert.True(bottom.IsVisible);
        Assert.True(Part<Control>(textarea, "PART_HelperText").IsVisible);
        Assert.False(Part<Control>(textarea, "PART_ErrorText").IsVisible);

        textarea.Classes.Add(AvonicTextarea.TouchedClass);
        textarea.Classes.Add(AvonicTextarea.InvalidClass);
        Settle();
        Assert.False(Part<Control>(textarea, "PART_HelperText").IsVisible);
        Assert.True(Part<Control>(textarea, "PART_ErrorText").IsVisible);
    }

    [AvaloniaFact]
    public void B21_The_Bottom_Text_Measured() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Label = "No fill", HelperText = "Helper", Value = "Value" };
        using var scene = new Scene(Page(textarea));
        Render();

        // Measured l: the bottom under the 64 px wrapper, 5 px above 12 px text, a 1 px top border, the helper step 700.
        var bottom = Part<Border>(textarea, "PART_Bottom");
        Assert.True(bottom.IsVisible);
        Assert.Equal(64, bottom.Bounds.Y, 0.5);
        Assert.Equal(new Thickness(0, 1, 0, 0), bottom.BorderThickness);
        Assert.Equal(new Thickness(0, 5, 0, 0), bottom.Padding);
        Assert.Equal(12, Part<TextBlock>(textarea, "PART_HelperText").FontSize);
        Assert.Equal(Color.Parse("#4D4D4D"), BrushColor(Part<TextBlock>(textarea, "PART_HelperText").Foreground));
    });

    // ── 22: slots ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B22_Slots_Sit_At_The_Top_With_The_Fields_Padding_16_From_It() => InstalledTheme.Run(_ =>
    {
        var icon = new Border { Width = 20, Height = 20 };
        var button = new AvonicButton { Fill = ButtonFill.Clear, Content = "B" };
        var row = new AvonicTextarea { Label = "Outline start", Fill = TextareaFill.Outline, Value = "Value", StartContent = icon, EndContent = button };
        var startIcon = new Border { Width = 20, Height = 20 };
        var stacked = new AvonicTextarea { Label = "Stacked", LabelPlacement = TextareaLabelPlacement.Stacked, StartContent = startIcon };
        var endIcon = new Border { Width = 20, Height = 20 };
        stacked.EndContent = endIcon;
        var outlineIcon = new Border { Width = 20, Height = 20 };
        var outlined = new AvonicTextarea { Label = "Outline", LabelPlacement = TextareaLabelPlacement.Floating, Fill = TextareaFill.Outline, StartContent = outlineIcon };
        using var scene = new Scene(Page(row, stacked, outlined));
        Render();

        // Measured q: the end slot at the top, 18 down; the button 0 0 4 16 around it.
        Assert.Equal(18, Bounds(icon, row).Y, 0.5);
        Assert.Equal(18, Bounds(button, row).Y, 0.5);
        Assert.Equal(new Thickness(16, 0, 0, 4), button.Margin);
        Assert.Equal(new Thickness(0, 0, 16, 0), icon.Margin);
        Assert.Equal(Bounds(icon, row).Right + 16, Bounds(Native(row), row).X, 0.5);
        Assert.Equal(Bounds(Native(row), row).Right + 16, Bounds(button, row).X, 0.5);

        // In a column, 8 px lower than the field's padding of 0, as the field is: 19 + 8; with the outline, 12.
        Assert.Equal(27, Bounds(startIcon, stacked).Y, 0.5);
        Assert.Equal(Bounds(Native(stacked), stacked).Y, Bounds(startIcon, stacked).Y, 0.5);
        Assert.Equal(12, Bounds(outlineIcon, outlined).Y, 0.5);
        Assert.Equal(new Thickness(16, 0, 0, 0), endIcon.Margin);
        Assert.Equal(Bounds(Native(stacked), stacked).Right + 16, Bounds(endIcon, stacked).X, 0.5);
    });

    [AvaloniaFact]
    public void B22_An_Apps_Plain_Margin_On_A_Slotted_Child_Wins() => InstalledTheme.Run(_ =>
    {
        var app = new Avalonia.Styling.Style(x => x.OfType<AvonicButton>())
        {
            Setters = { new Avalonia.Styling.Setter(Layoutable.MarginProperty, new Thickness(3)) },
        };
        Application.Current!.Styles.Add(app);
        try
        {
            var button = new AvonicButton { Content = "B" };
            using var scene = new Scene(Page(new AvonicTextarea { EndContent = button }));
            Render();
            Assert.Equal(new Thickness(3), button.Margin);
        }
        finally
        {
            Application.Current.Styles.Remove(app);
        }
    });

    // ── 23 to 26 ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B23_Disabled_Cant_Be_Focused_And_Read_Only_Cant_Be_Edited()
    {
        var (textarea, _, scene) = Stub(new AvonicTextarea { Value = "v", IsEnabled = false });
        using var _ = scene;
        textarea.SetFocus();
        Settle();
        Assert.NotSame(Native(textarea), Focused(textarea));

        textarea.IsEnabled = true;
        textarea.Readonly = true;
        textarea.SetFocus();
        Settle();
        Assert.Same(Native(textarea), Focused(textarea));
        Type(scene, PhysicalKey.A, "a");
        Key(scene, PhysicalKey.Enter);
        Assert.Equal("v", textarea.Value);
    }

    [AvaloniaFact]
    public void B23_Disabled_Is_038_Opacity() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Label = "Disabled", Value = "Value" };
        using var scene = new Scene(Page(textarea));
        Assert.Null(textarea.OpacityMask);

        // As one: a mask, not Opacity, which fades each overlapping part on its own.
        textarea.IsEnabled = false;
        Settle();
        Assert.Equal(1, textarea.Opacity);
        var mask = Assert.IsAssignableFrom<ISolidColorBrush>(textarea.OpacityMask);
        Assert.Equal(0.38, mask.Opacity * mask.Color.A / 255.0, 3);
    });

    [AvaloniaFact]
    public void B24_The_Placeholder_Is_The_Fields_Colour_At_06() => InstalledTheme.Run(_ =>
    {
        var textarea = new AvonicTextarea { Label = "L", Placeholder = "Placeholder" };
        using var scene = new Scene(Page(textarea));
        Render();

        var placeholder = Part<TextBlock>(Native(textarea), "PART_Placeholder");
        Assert.Equal("Placeholder", placeholder.Text);
        Assert.Equal(0.6, placeholder.Opacity, 3);

        // The field's line height, which the placeholder inherits.
        Assert.Equal(19, placeholder.LineHeight);
        Assert.Equal(BrushColor(textarea.Foreground), BrushColor(placeholder.Foreground));

        textarea.Value = "v";
        Settle();
        Assert.False(placeholder.IsVisible);

        textarea.Resources["Avonic.Textarea.PlaceholderColor"] = new SolidColorBrush(Colors.Red);
        textarea.Value = "";
        Settle();
        Assert.Equal(Colors.Red, BrushColor(placeholder.Foreground));
    });

    [AvaloniaFact]
    public void B25_The_Hints_Reach_The_Native_Textarea()
    {
        var (textarea, _, scene) = Stub(new AvonicTextarea());
        using var _ = scene;
        var native = Native(textarea);
        Assert.True(native.AcceptsReturn);
        Assert.True(TextInputOptions.GetMultiline(native));
        Assert.Equal(TextInputContentType.Normal, TextInputOptions.GetContentType(native));

        textarea.Inputmode = TextareaInputmode.Email;
        Assert.Equal(TextInputContentType.Email, TextInputOptions.GetContentType(native));
        textarea.Inputmode = TextareaInputmode.Decimal;
        Assert.Equal(TextInputContentType.Number, TextInputOptions.GetContentType(native));
        textarea.Inputmode = TextareaInputmode.Numeric;
        Assert.Equal(TextInputContentType.Digits, TextInputOptions.GetContentType(native));
        textarea.Inputmode = TextareaInputmode.Tel;
        Assert.Equal(TextInputContentType.Digits, TextInputOptions.GetContentType(native));
        textarea.Inputmode = TextareaInputmode.Url;
        Assert.Equal(TextInputContentType.Url, TextInputOptions.GetContentType(native));
        textarea.Inputmode = TextareaInputmode.Search;
        Assert.Equal(TextInputContentType.Search, TextInputOptions.GetContentType(native));
        textarea.Inputmode = TextareaInputmode.Text;
        Assert.Equal(TextInputContentType.Normal, TextInputOptions.GetContentType(native));

        textarea.Enterkeyhint = TextareaEnterkeyhint.Send;
        Assert.Equal(TextInputReturnKeyType.Send, TextInputOptions.GetReturnKeyType(native));
        textarea.Enterkeyhint = TextareaEnterkeyhint.Enter;
        Assert.Equal(TextInputReturnKeyType.Return, TextInputOptions.GetReturnKeyType(native));
        textarea.Enterkeyhint = null;
        Assert.Equal(TextInputReturnKeyType.Default, TextInputOptions.GetReturnKeyType(native));

        Assert.False(TextInputOptions.GetAutoCapitalization(native));
        textarea.Autocapitalize = "words";
        Assert.True(TextInputOptions.GetAutoCapitalization(native));
        textarea.Autocapitalize = "characters";
        Assert.False(TextInputOptions.GetAutoCapitalization(native));
        Assert.True(TextInputOptions.GetUppercase(native));

        textarea.Placeholder = "P";
        Assert.Equal("P", native.PlaceholderText);
    }

    [AvaloniaFact]
    public void B25_Maxlength_Stops_Typing_And_0_Allows_Nothing()
    {
        var (textarea, _, scene) = Stub(new AvonicTextarea { Maxlength = 2 });
        using var _ = scene;
        textarea.SetFocus();
        Settle();
        Type(scene, PhysicalKey.A, "a");
        Type(scene, PhysicalKey.B, "b");
        Type(scene, PhysicalKey.C, "c");
        Assert.Equal("ab", textarea.Value);

        // A line break counts, at the limit.
        Key(scene, PhysicalKey.Enter);
        Assert.Equal("ab", textarea.Value);

        textarea.Value = "";
        textarea.Maxlength = 0;
        var log = Log.Of(textarea);
        Type(scene, PhysicalKey.A, "a");
        Key(scene, PhysicalKey.Enter);
        Assert.Equal("", textarea.Value);
        Assert.Empty(log.Take());
    }

    [AvaloniaFact]
    public void B26_Autofocus_Focuses_The_Field_The_First_Time_It_Loads_Only()
    {
        var textarea = new AvonicTextarea { Autofocus = true };
        var other = new TextBox();
        var host = new Border { Child = textarea };
        using var scene = new Scene(new StackPanel { Children = { host, other } });
        Assert.Same(Native(textarea), Focused(textarea));

        other.Focus();
        Settle();
        host.Child = null;
        Settle();
        scene.Window.FocusManager?.Focus(null);
        Settle();
        host.Child = textarea;
        Settle();
        Assert.NotSame(Native(textarea), Focused(textarea));

        // Not when something else has focus.
        other.Focus();
        Settle();
        var late = new AvonicTextarea { Autofocus = true };
        host.Child = late;
        Settle();
        Assert.Same(other, Focused(other));
    }

    // ── 27: automation ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B27_The_Native_Textarea_Is_Named_By_The_Label_And_Described_By_The_Hint()
    {
        var (textarea, _, scene) = Stub(new AvonicTextarea { Label = "Notes", HelperText = "Helper", ErrorText = "Error" });
        using var _ = scene;
        var native = Native(textarea);
        Assert.Equal("Notes", AutomationProperties.GetName(native));
        Assert.Equal("Helper", AutomationProperties.GetHelpText(native));

        textarea.Classes.Add(AvonicTextarea.TouchedClass);
        textarea.Classes.Add(AvonicTextarea.InvalidClass);
        Assert.Equal("Error", AutomationProperties.GetHelpText(native));

        textarea.ErrorText = null;
        Assert.Equal("Helper", AutomationProperties.GetHelpText(native));

        Assert.False(AutomationProperties.GetIsRequiredForForm(native));
        textarea.Required = true;
        Assert.True(AutomationProperties.GetIsRequiredForForm(native));

        AutomationProperties.SetName(textarea, "Your notes");
        Assert.Equal("Your notes", AutomationProperties.GetName(native));

        AutomationProperties.SetName(textarea, null);
        textarea.Label = null;
        textarea.LabelContent = new TextBlock { Text = "Slotted" };
        Settle();
        Assert.Equal("Slotted", AutomationProperties.GetName(native));

        textarea.LabelContent = new Uri("https://example.com/");
        textarea.LabelContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Uri>((uri, _) => new TextBlock { Text = uri.Host });
        Settle();
        Assert.Equal("example.com", AutomationProperties.GetName(native));
    }

    // aria-required, aria-labelledby and aria-describedby on the native textarea are at style priority and bound before the
    // field's styles, so that an app's plain style for the field, there before the window is shown, wins whenever they change
    // after it.
    [AvaloniaFact]
    public void B27_An_Apps_Style_For_The_Native_Textareas_Automation_Survives_Changes_After_It()
    {
        var textarea = new AvonicTextarea { Label = "Notes", HelperText = "Helper" };
        using var scene = Scene.Styled(new StackPanel { Children = { textarea } },
            Scene.AppStyle<TextBox>(AutomationProperties.IsRequiredForFormProperty, false),
            Scene.AppStyle<TextBox>(AutomationProperties.NameProperty, "The app's name"),
            Scene.AppStyle<TextBox>(AutomationProperties.HelpTextProperty, "The app's hint"));
        var native = Native(textarea);

        textarea.Required = true;
        textarea.Label = "Comments";
        textarea.HelperText = "Another helper";

        Assert.False(AutomationProperties.GetIsRequiredForForm(native));
        Assert.Equal("The app's name", AutomationProperties.GetName(native));
        Assert.Equal("The app's hint", AutomationProperties.GetHelpText(native));
    }

    // ── 28: RTL ──────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B28_Right_To_Left_Mirrors_The_Row_And_The_Outline() => InstalledTheme.Run(_ =>
    {
        var row = new AvonicTextarea { Label = "End", Value = "v", FlowDirection = FlowDirection.RightToLeft };
        var outline = new AvonicTextarea { Label = "RTL", Fill = TextareaFill.Outline, LabelPlacement = TextareaLabelPlacement.Floating, Value = "v", FlowDirection = FlowDirection.RightToLeft };
        using var scene = new Scene(Page(row, outline));
        Part<Control>(outline, "PART_Label").Transitions = [];
        Render();

        var window = scene.Window;
        Assert.Equal(390, Bounds(Part<Control>(row, "PART_Label"), window).Right, 0.5);
        Assert.True(Bounds(Native(row), window).Right < Bounds(Part<Control>(row, "PART_Label"), window).X);

        var top = Bounds(outline, window).Y;
        AssertBox(378, top, 12, 58, Bounds(Part<Control>(outline, "PART_OutlineStart"), window));
        Assert.Equal(374, Bounds(Part<Control>(outline, "PART_Label"), window).Right, 0.6);
    });

    // ── Regressions ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R01_A_Value_Replaced_During_An_Edit_Leaves_The_Caret_At_The_End()
    {
        var (textarea, _, scene) = Stub(new AvonicTextarea { Value = "abc" });
        using var _ = scene;
        textarea.IonInput += (_, e) => textarea.Value = e.Value?.ToUpperInvariant();
        textarea.SetFocus();
        Settle();
        Native(textarea).CaretIndex = 1;

        Type(scene, PhysicalKey.X, "x");

        Assert.Equal("AXBC", Native(textarea).Text);
        Assert.Equal(4, Native(textarea).CaretIndex);
    }

    [AvaloniaFact]
    public void R02_A_Value_Set_During_Composition_Is_Shown_At_Once()
    {
        // textarea.js's valueChanged, unlike input.js's, doesn't check isComposing.
        var (textarea, _, scene) = Stub(new AvonicTextarea { Value = "a" });
        using var _ = scene;
        var presenter = Native(textarea).GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().First();
        presenter.PreeditText = "ka";

        textarea.Value = "b";

        Assert.Equal("b", Native(textarea).Text);
    }

    [AvaloniaFact]
    public void R05_A_Pasted_Line_Break_Is_A_Line_Feed_With_The_Caret_After_It()
    {
        var (textarea, _, scene) = Stub(new AvonicTextarea { Value = "ab" });
        using var _ = scene;
        var log = Log.Of(textarea);
        textarea.SetFocus();
        Settle();
        log.Take();
        var native = Native(textarea);

        // What a paste does: the text, then the caret after what was pasted, in the text's own terms.
        native.Text = "a1\r\n2\rb";
        native.CaretIndex = 6;
        Settle();

        Assert.Equal("a1\n2\nb", textarea.Value);
        Assert.Equal("a1\n2\nb", native.Text);
        Assert.Equal(5, native.CaretIndex);
        Assert.Equal(["input:a1\n2\nb"], log.Take());
    }

    [AvaloniaFact]
    public void R06_The_Start_And_End_Slots_Take_Templates()
    {
        var textarea = new AvonicTextarea
        {
            StartContent = "s",
            StartContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => new TextBlock { Text = "start " + text }),
            EndContent = "e",
            EndContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) => new TextBlock { Text = "end " + text }),
        };
        var (_, _, scene) = Stub(textarea);
        using var _ = scene;

        Assert.Equal("start s", Assert.IsType<TextBlock>(Part<Avalonia.Controls.Presenters.ContentPresenter>(textarea, "PART_Start").Child).Text);
        Assert.Equal("end e", Assert.IsType<TextBlock>(Part<Avalonia.Controls.Presenters.ContentPresenter>(textarea, "PART_End").Child).Text);
    }

    [AvaloniaFact]
    public void R03_A_Pending_Debounced_IonInput_Is_Still_Raised_After_Debounce_Changes()
    {
        using var clock = new TestClock();
        var (textarea, _, scene) = Stub(new AvonicTextarea { Debounce = 100 });
        using var _ = scene;
        var log = Log.Of(textarea);
        textarea.SetFocus();
        Settle();
        log.Take();

        clock.At(0);
        Type(scene, PhysicalKey.A, "a");
        textarea.Debounce = 50;
        clock.At(100);
        Assert.Equal(["input:a"], log.Take());
    }

    [AvaloniaFact]
    public void R04_A_Value_On_A_Textarea_That_Isnt_Focused_Doesnt_Scroll_The_Page() => WithScroller((viewer, textarea) =>
    {
        Assert.Equal(0, viewer.Offset.Y);
        textarea.Value = "changed\nand longer";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, viewer.Offset.Y);

        // Focused, its caret still comes into view.
        Native(textarea).Focus();
        Dispatcher.UIThread.RunJobs();
        viewer.Offset = default;
        Dispatcher.UIThread.RunJobs();
        textarea.Value = "typed";
        Dispatcher.UIThread.RunJobs();
        Assert.True(viewer.Offset.Y > 0, $"offset {viewer.Offset.Y}");
    });

    /// <summary>A textarea below the fold of a 300-high ScrollViewer, over the base theme.</summary>
    private static void WithScroller(Action<ScrollViewer, AvonicTextarea> test)
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var textarea = new AvonicTextarea { Label = "Below" };
                var viewer = new ScrollViewer
                {
                    Height = 300,
                    Content = new StackPanel { Children = { new Border { Height = 400 }, textarea, new Border { Height = 50 } } },
                };
                using var scene = new Scene(new StackPanel { Children = { viewer, new Button() } });
                Dispatcher.UIThread.RunJobs();
                Assert.True(viewer.Extent.Height > 300);
                test(viewer, textarea);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }
}
