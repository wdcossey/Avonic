using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-input-otp in Chromium, at 390 px wide, and the tests are named after the behaviours in
/// docs/components/ion-input-otp/PLAN.md. Tests with <c>InstalledTheme</c> check the look, or need boxes with a size to click. The
/// others run on the functional stub.
/// </summary>
public class AvonicInputOtpTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Danger = Color.Parse("#C5000F");
    private static readonly Color Success = Color.Parse("#2DD55B");

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────

    private sealed class Log
    {
        private readonly List<string> _all = [];

        public static Log Of(AvonicInputOtp otp)
        {
            var log = new Log();
            otp.IonInput += (_, e) => log._all.Add($"input:{e.Value}");
            otp.IonChange += (_, e) => log._all.Add($"change:{e.Value}");
            otp.IonComplete += (_, e) => log._all.Add($"complete:{e.Value}");
            otp.IonFocus += (_, _) => log._all.Add("focus");
            otp.IonBlur += (_, _) => log._all.Add("blur");
            return log;
        }

        public string[] Take()
        {
            var all = _all.ToArray();
            _all.Clear();
            return all;
        }
    }

    private static InputOtpBox[] Boxes(AvonicInputOtp otp) =>
        otp.GetVisualDescendants().OfType<InputOtpBox>().OrderBy(box => box.Index).ToArray();

    private static InputOtpSeparator[] Separators(AvonicInputOtp otp) =>
        otp.GetVisualDescendants().OfType<InputOtpSeparator>().ToArray();

    /// <summary>What each box shows, as the Chromium traces print it.</summary>
    private static string Shown(AvonicInputOtp otp) => string.Join("|", Boxes(otp).Select(box => box.Text));

    /// <summary>The focused box's index, or -1.</summary>
    private static int FocusedBox(AvonicInputOtp otp) => Boxes(otp).FirstOrDefault(box => box.IsFocused)?.Index ?? -1;

    private static string TabStops(AvonicInputOtp otp) => string.Join(",", Boxes(otp).Select(box => box.IsTabStop ? 0 : -1));

    private static Rect Bounds(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void AssertBox(double x, double y, double width, double height, Rect actual, double tolerance = 0.6)
    {
        Assert.InRange(actual.X, x - tolerance, x + tolerance);
        Assert.InRange(actual.Y, y - tolerance, y + tolerance);
        Assert.InRange(actual.Width, width - tolerance, width + tolerance);
        Assert.InRange(actual.Height, height - tolerance, height + tolerance);
    }

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Click(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        Settle();
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Settle();
    }

    private static void ClickBox(Scene scene, AvonicInputOtp otp, int index, double dx = 0)
    {
        var box = Bounds(Boxes(otp)[index], scene.Window);
        Click(scene, box.Center + new Point(dx, 0));
    }

    private static void Key(Scene scene, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        scene.Window.KeyPressQwerty(key, modifiers);
        Settle();
        scene.Window.KeyReleaseQwerty(key, modifiers);
        Settle();
    }

    /// <summary>A key and the character it types, as a keyboard sends them.</summary>
    private static void Type(Scene scene, string text)
    {
        foreach (var c in text)
        {
            var key = char.IsDigit(c) ? PhysicalKey.Digit0 + (c - '0') : char.IsLetter(c) && c < 128 ? PhysicalKey.A + (char.ToUpperInvariant(c) - 'A') : PhysicalKey.Minus;
            scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
            scene.Window.KeyTextInput(c.ToString());
            scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
            Settle();
        }
    }

    private static void Render()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();
    }

    /// <summary>An OTP on the stub, with a text box after it to take focus away.</summary>
    private static (AvonicInputOtp Otp, TextBox Other, Scene Scene) Stub(AvonicInputOtp? otp = null)
    {
        otp ??= new AvonicInputOtp();
        var other = new TextBox();
        var scene = new Scene(new StackPanel { Children = { otp, other } });
        return (otp, other, scene);
    }

    /// <summary>An OTP in the theme, 358 wide at x = 16 as in the reference, with a text box after it.</summary>
    private static (AvonicInputOtp Otp, TextBox Other, Scene Scene) Themed(AvonicInputOtp? otp = null)
    {
        otp ??= new AvonicInputOtp();
        var other = new TextBox();
        var page = new StackPanel { Margin = new Thickness(16, 0), Children = { otp, other } };
        page.Bind(TextElement.FontFamilyProperty, page.GetResourceObservable("Avonic.FontFamily"));
        var scene = new Scene(page);
        Render();
        return (otp, other, scene);
    }

    /// <summary>A solid brush's colour, with its opacity as the alpha, as rgba() and a brush at an opacity draw the same.</summary>
    private static Color BrushColor(IBrush? brush)
    {
        var solid = Assert.IsAssignableFrom<ISolidColorBrush>(brush);
        return Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B);
    }

    private static Color Rgba(Color color, double alpha) => Color.FromArgb((byte)Math.Round(alpha * 255), color.R, color.G, color.B);

    // ── 1: the value and the boxes ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B01_The_Boxes_Show_The_Value_Cut_To_Length_With_Refused_Characters_Dropped()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "1a2" });
        using var _ = scene;

        Assert.Equal(4, Boxes(otp).Length);
        Assert.Equal("1|2||", Shown(otp));
        Assert.Equal("12", otp.Value);

        // Cut to the length first, then filtered: "1a234" is "1a23", which is "123".
        otp.Value = "1a234";
        Assert.Equal("123", otp.Value);
        Assert.Equal("1|2|3|", Shown(otp));

        otp.Length = 6;
        Assert.Equal(6, Boxes(otp).Length);
        Assert.Equal("1|2|3|||", Shown(otp));
    }

    [AvaloniaFact]
    public void B01_The_Value_Is_Read_Once_Every_Property_Is_Set_Whatever_Their_Order()
    {
        // As componentWillLoad: the value isn't cut by the default length, or filtered by the default type, set after it.
        var six = new AvonicInputOtp { Value = "123456", Length = 6, Separators = "5" };
        var text = new AvonicInputOtp { Value = "AB", Type = InputOtpType.Text };
        var scene = new Scene(new StackPanel { Children = { six, text } });
        using var _ = scene;

        Assert.Equal("123456", six.Value);
        Assert.Equal("1|2|3|4|5|6", Shown(six));
        Assert.True(Separators(six)[4].IsVisible);
        Assert.Equal("AB", text.Value);
        Assert.Equal("A|B||", Shown(text));
    }

    private sealed class Model : System.ComponentModel.INotifyPropertyChanged
    {
        private string? _code;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public string? Code
        {
            get => _code;
            set
            {
                _code = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Code)));
            }
        }
    }

    [AvaloniaFact]
    public void B01_A_Template_Applied_Before_Load_Still_Gets_The_Final_Length()
    {
        // An app's own template, applied before the control is on the page, then a length.
        var otp = new AvonicInputOtp
        {
            Template = new FuncControlTemplate<AvonicInputOtp>((_, scope) =>
                new FlexRow { Name = "PART_Group" }.RegisterInNameScope(scope)),
        };
        otp.ApplyTemplate();
        otp.Length = 6;
        otp.Value = "123456";

        var (_, _, scene) = Stub(otp);
        using var _ = scene;
        Assert.Equal("1|2|3|4|5|6", Shown(otp));
    }

    [AvaloniaFact]
    public void B01_A_Bound_Value_Gets_The_Rewritten_Value_Back()
    {
        var model = new Model { Code = "1a2" };
        var otp = new AvonicInputOtp { DataContext = model };
        otp.Bind(AvonicInputOtp.ValueProperty, new Avalonia.Data.Binding(nameof(Model.Code)));
        var (_, _, scene) = Stub(otp);
        using var _ = scene;
        Settle();

        Assert.Equal("12", otp.Value);
        Assert.Equal("12", model.Code);

        model.Code = "1b34567";
        Settle();
        Assert.Equal("134", otp.Value);
        Assert.Equal("1|3|4|", Shown(otp));
        Assert.Equal("134", model.Code);
    }

    // ── 2 to 5: typing ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B02_Typing_In_An_Empty_Box_Fills_It_Raises_IonInput_And_Moves_On()
    {
        var (otp, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(otp);

        otp.SetFocus();
        Settle();
        Assert.Equal(["focus"], log.Take());
        Assert.Equal(0, FocusedBox(otp));

        Type(scene, "1");
        Assert.Equal(["input:1"], log.Take());
        Assert.Equal("1|||", Shown(otp));
        Assert.Equal(1, FocusedBox(otp));
        Assert.Equal("-1,0,-1,-1", TabStops(otp));

        Type(scene, "2");
        Assert.Equal("12", otp.Value);
        Assert.Equal(2, FocusedBox(otp));
    }

    [AvaloniaFact]
    public void B03_A_Refused_Character_Changes_Nothing()
    {
        var (otp, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(otp);
        otp.SetFocus();
        Settle();
        log.Take();

        Type(scene, "a");
        Assert.Empty(log.Take());
        Assert.Equal("|||", Shown(otp));
        Assert.Equal(0, FocusedBox(otp));

        // Text takes letters, including accented ones, and digits, but not a hyphen.
        otp.Type = InputOtpType.Text;
        Type(scene, "b");
        scene.Window.KeyTextInput("é");
        Settle();
        Type(scene, "-");
        Assert.Equal("bé", otp.Value);

        // A pattern replaces the type's. Nothing re-reads the value when it changes, but the next edit sets the value, and
        // its watcher checks every box again: "é" goes, as it does in Ionic.
        otp.Pattern = "[a-c]";
        Assert.Equal("bé", otp.Value);
        Type(scene, "d");
        Assert.Equal("bé", otp.Value);
        Type(scene, "c");
        Assert.Equal("bc", otp.Value);
    }

    [AvaloniaFact]
    public void B04_Filling_The_Last_Box_Raises_IonComplete_After_IonInput_Each_Time()
    {
        var (otp, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(otp);
        otp.SetFocus();
        Settle();
        Type(scene, "123");
        log.Take();

        Type(scene, "4");
        Assert.Equal(["input:1234", "complete:1234"], log.Take());
        Assert.Equal(3, FocusedBox(otp));

        // Typed again in the full last box, with the caret at its end: it replaces it, and completes again.
        Type(scene, "5");
        Assert.Equal(["input:1235", "complete:1235"], log.Take());
        Assert.Equal("1|2|3|5", Shown(otp));
    }

    [AvaloniaFact]
    public void B05_Typing_In_A_Filled_Box_Inserts_And_Shifts_While_The_Last_Is_Empty_Else_Replaces()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "1" });
        using var _ = scene;
        var log = Log.Of(otp);
        otp.SetFocus(0);
        Settle();
        log.Take();

        // The caret at the end of "1": "9" goes in here, and "1" moves on. Focus stays.
        Type(scene, "9");
        Assert.Equal(["input:91"], log.Take());
        Assert.Equal("9|1||", Shown(otp));
        Assert.Equal(0, FocusedBox(otp));

        // The caret at the start: still inserted here.
        Boxes(otp)[0].CaretIndex = 0;
        Type(scene, "7");
        Assert.Equal("7|9|1|", Shown(otp));

        // The last box filled: the character before the caret replaces the box's.
        otp.Value = "1234";
        otp.SetFocus(2);
        Settle();
        Boxes(otp)[2].CaretIndex = 0;
        Type(scene, "7");
        Assert.Equal("1274", otp.Value);
        Assert.Equal(2, FocusedBox(otp));
    }

    // ── 6, 7: Backspace and the arrows ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B06_Backspace_Removes_A_Filled_Boxs_Character_And_In_An_Empty_Box_Walks_Back_Deleting()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "1234" });
        using var _ = scene;
        var log = Log.Of(otp);
        otp.SetFocus(3);
        Settle();
        log.Take();

        Key(scene, PhysicalKey.Backspace);
        Assert.Equal(["input:123"], log.Take());
        Assert.Equal(3, FocusedBox(otp));

        // The box is empty: focus moves back, and the key deletes there.
        Key(scene, PhysicalKey.Backspace);
        Assert.Equal(["input:12"], log.Take());
        Assert.Equal("1|2||", Shown(otp));
        Assert.Equal(2, FocusedBox(otp));

        Key(scene, PhysicalKey.Backspace);
        Assert.Equal("1", otp.Value);
        Assert.Equal(1, FocusedBox(otp));

        // In a filled box, whatever the caret, its character goes and the rest shift left.
        otp.Value = "179";
        otp.SetFocus(1);
        Settle();
        Boxes(otp)[1].CaretIndex = 0;
        Key(scene, PhysicalKey.Backspace);
        Assert.Equal("19", otp.Value);
        Assert.Equal("1|9||", Shown(otp));
        Assert.Equal(1, FocusedBox(otp));
    }

    [AvaloniaFact]
    public void B06_Delete_Edits_Natively_And_The_Emptied_Box_Keeps_Focus()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "179" });
        using var _ = scene;
        otp.SetFocus(0);
        Settle();
        Boxes(otp)[0].CaretIndex = 0;

        Key(scene, PhysicalKey.Delete);

        // Measured: the box empties, the value closes up, focus moves on and comes back to the box that's now filled.
        Assert.Equal("79", otp.Value);
        Assert.Equal("7|9||", Shown(otp));
        Assert.Equal(1, FocusedBox(otp));
    }

    [AvaloniaFact]
    public void B07_Right_Moves_On_Only_From_A_Filled_Box_And_Left_Moves_Back_Swapped_In_RTL()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "1" });
        using var _ = scene;
        otp.SetFocus(1);
        Settle();
        Assert.Equal(1, FocusedBox(otp));

        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal(1, FocusedBox(otp));
        Key(scene, PhysicalKey.ArrowLeft);
        Assert.Equal(0, FocusedBox(otp));
        Key(scene, PhysicalKey.ArrowLeft);
        Assert.Equal(0, FocusedBox(otp));
        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal(1, FocusedBox(otp));

        otp.FlowDirection = FlowDirection.RightToLeft;
        Key(scene, PhysicalKey.ArrowRight);
        Assert.Equal(0, FocusedBox(otp));
        Key(scene, PhysicalKey.ArrowLeft);
        Assert.Equal(1, FocusedBox(otp));
    }

    // ── 8 to 10: focus ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B08_A_Click_Lands_On_A_Filled_Box_Else_The_First_Empty() => InstalledTheme.Run(_ =>
    {
        var (otp, _, scene) = Themed(new AvonicInputOtp { Value = "1" });
        using var __ = scene;

        ClickBox(scene, otp, 2);
        Assert.Equal(1, FocusedBox(otp));

        ClickBox(scene, otp, 0);
        Assert.Equal(0, FocusedBox(otp));

        // Typed after the click: into the box focus landed on.
        ClickBox(scene, otp, 3);
        Type(scene, "2");
        Assert.Equal("12", otp.Value);
    });

    [AvaloniaFact]
    public void B08_After_Tab_In_A_Box_The_Next_Focus_Isnt_Moved() => InstalledTheme.Run(_ =>
    {
        var (otp, other, scene) = Themed(new AvonicInputOtp { Value = "1" });
        using var __ = scene;
        ClickBox(scene, otp, 0);
        Assert.Equal(0, FocusedBox(otp));

        Key(scene, PhysicalKey.Tab);
        Assert.True(other.IsFocused);

        // Measured: the empty last box keeps focus, as Tab left Ionic's flag set.
        ClickBox(scene, otp, 3);
        Assert.Equal(3, FocusedBox(otp));

        // And only once.
        other.Focus();
        Settle();
        ClickBox(scene, otp, 3);
        Assert.Equal(1, FocusedBox(otp));
    });

    [AvaloniaFact]
    public void B09_One_Box_Is_A_Tab_Stop_And_Empty_Ones_Past_It_Are_Hidden_From_Automation()
    {
        var (otp, other, scene) = Stub(new AvonicInputOtp { Value = "12" });
        using var _ = scene;

        Assert.Equal("-1,-1,0,-1", TabStops(otp));
        AccessibilityView[] Views() => Boxes(otp).Select(AutomationProperties.GetAccessibilityView).ToArray();
        Assert.Equal([AccessibilityView.Content, AccessibilityView.Content, AccessibilityView.Content, AccessibilityView.Raw], Views());

        // The focused box is the tab stop.
        otp.SetFocus(0);
        Settle();
        Assert.Equal("0,-1,-1,-1", TabStops(otp));

        // A blur puts it back on the first empty box.
        other.Focus();
        Settle();
        Assert.Equal("-1,-1,0,-1", TabStops(otp));

        // Tab from before the group lands there.
        var before = new TextBox();
        ((StackPanel)otp.Parent!).Children.Insert(0, before);
        before.Focus();
        Settle();
        Key(scene, PhysicalKey.Tab);
        Assert.Equal(2, FocusedBox(otp));

        // All filled: the last.
        otp.Value = "1234";
        Assert.Equal("-1,-1,-1,0", TabStops(otp));
    }

    [AvaloniaFact]
    public void B10_IonFocus_And_IonBlur_Are_The_Groups_And_IonChange_Follows_The_Blur()
    {
        var (otp, other, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(otp);

        otp.SetFocus();
        Settle();
        Type(scene, "12");
        Key(scene, PhysicalKey.ArrowLeft);
        other.Focus();
        Settle();
        Assert.Equal(["focus", "input:1", "input:12", "blur", "change:12"], log.Take());

        // No edit: no change.
        otp.SetFocus();
        Settle();
        other.Focus();
        Settle();
        Assert.Equal(["focus", "blur"], log.Take());

        // A value set in code while focused counts, as Ionic compares with the value at focus.
        otp.SetFocus();
        Settle();
        otp.Value = "43";
        other.Focus();
        Settle();
        Assert.Equal(["focus", "blur", "change:43"], log.Take());
    }

    // ── 11, 12: paste and autofill ───────────────────────────────────────────────────────────

    private static void Paste(Scene scene, string text)
    {
        var clipboard = scene.Window.Clipboard!;
        clipboard.SetTextAsync(text).GetAwaiter().GetResult();
        Settle();
        Key(scene, PhysicalKey.V, RawInputModifiers.Control);
        Settle();
    }

    [AvaloniaFact]
    public void B11_Paste_Fills_From_The_First_Box_And_Focuses_The_Next_Empty()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "43" });
        using var _ = scene;
        var log = Log.Of(otp);
        otp.SetFocus();
        Settle();
        log.Take();

        // With the box's character selected: input.value collapses the selection, so Backspace afterwards is Ionic's own.
        Boxes(otp)[0].SelectAll();
        Paste(scene, "x1 2-3");
        Assert.Equal(["input:123"], log.Take());
        Assert.All(Boxes(otp), box => Assert.Equal(box.SelectionStart, box.SelectionEnd));
        Assert.Equal("1|2|3|", Shown(otp));
        Assert.Equal(3, FocusedBox(otp));

        Paste(scene, "987654");
        Assert.Equal(["input:9876", "complete:9876"], log.Take());
        Assert.Equal(3, FocusedBox(otp));

        // Nothing valid: the value is cleared.
        Paste(scene, "ab");
        Assert.Equal(["input:"], log.Take());
        Assert.Equal("|||", Shown(otp));
        Assert.Equal(0, FocusedBox(otp));

        // An empty clipboard: only the input event, as a native input raises one.
        otp.Value = "12";
        scene.Window.Clipboard!.ClearAsync().GetAwaiter().GetResult();
        Key(scene, PhysicalKey.V, RawInputModifiers.Control);
        Settle();
        Assert.Equal(["input:12"], log.Take());
        Assert.Equal("12", otp.Value);
    }

    [AvaloniaFact]
    public void B12_Autofill_Spreads_From_The_First_Box_And_Focuses_The_Next_Empty_20ms_Later()
    {
        using var clock = new TestClock();
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "567" });
        using var _ = scene;
        var log = Log.Of(otp);
        otp.SetFocus();
        Settle();
        log.Take();

        // A soft keyboard's suggested code, in one go, into the last box.
        scene.Window.KeyTextInput("8a9");
        Settle();
        Assert.Equal(["input:89"], log.Take());
        Assert.Equal("8|9||", Shown(otp));
        Assert.Equal(3, FocusedBox(otp));

        clock.At(19);
        Settle();
        Assert.Equal(3, FocusedBox(otp));
        clock.At(20);
        Settle();
        Assert.Equal(2, FocusedBox(otp));

        // Taken off the page and put back before the 20 ms: the focus it was waiting for doesn't come.
        scene.Window.KeyTextInput("12");
        Settle();
        var panel = (StackPanel)otp.Parent!;
        panel.Children.Remove(otp);
        panel.Children.Insert(0, otp);
        var other = panel.Children.OfType<TextBox>().Last();
        other.Focus();
        Settle();
        clock.At(60);
        Settle();
        Assert.True(other.IsFocused);
    }

    // ── 13, 14: methods and the value in code ────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_SetFocus_Clamps_And_Lands_As_A_Click_Does()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "4321" });
        using var _ = scene;

        otp.SetFocus(1);
        Settle();
        Assert.Equal(1, FocusedBox(otp));

        otp.SetFocus(9);
        Settle();
        Assert.Equal(3, FocusedBox(otp));

        // An empty box: the first empty one. A focused box isn't focused again, as in Chromium, so from outside.
        otp.Value = "43";
        scene.Window.Focus();
        ((StackPanel)otp.Parent!).Children.OfType<TextBox>().Last().Focus();
        Settle();
        otp.SetFocus(3);
        Settle();
        Assert.Equal(2, FocusedBox(otp));

        otp.SetFocus(-2);
        Settle();
        Assert.Equal(0, FocusedBox(otp));

        otp.SetFocus();
        Settle();
        Assert.Equal(2, FocusedBox(otp));
    }

    [AvaloniaFact]
    public void B14_A_Value_Set_In_Code_Raises_Nothing_And_Shows()
    {
        var (otp, _, scene) = Stub();
        using var _ = scene;
        var log = Log.Of(otp);

        otp.Value = "4321";
        Assert.Empty(log.Take());
        Assert.Equal("4|3|2|1", Shown(otp));
        Assert.Equal("-1,-1,-1,0", TabStops(otp));

        otp.Value = null;
        Assert.Equal("|||", Shown(otp));
        Assert.Null(otp.Value);
    }

    // ── 15: separators ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_Separators_Show_After_The_Listed_Boxes_But_Not_The_Last()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Length = 6 });
        using var _ = scene;
        bool[] Shown() => Separators(otp).Select(separator => separator.IsVisible).ToArray();

        Assert.Equal([false, false, false, false, false, false], Shown());

        otp.Separators = "3";
        Assert.Equal([false, false, true, false, false, false], Shown());

        otp.Separators = "1,3";
        Assert.Equal([true, false, true, false, false, false], Shown());

        otp.Separators = "all";
        Assert.Equal([true, true, true, true, true, false], Shown());

        // Past the length, ignored; at the length, after the last box, not shown.
        otp.Separators = "6,9,2,2";
        Assert.Equal([false, true, false, false, false, false], Shown());

        // Not a list of numbers: none.
        otp.Separators = "1, 3";
        Assert.Equal([false, false, false, false, false, false], Shown());

        // A shorter row keeps them where they still fit.
        otp.Separators = "1,4";
        otp.Length = 4;
        Assert.Equal([true, false, false, false], Shown());
    }

    private sealed class WarningSink : Avalonia.Logging.ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(Avalonia.Logging.LogEventLevel level, string area) => level >= Avalonia.Logging.LogEventLevel.Warning;

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate) => Messages.Add(messageTemplate);

        public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            Messages.Add(string.Join(" ", propertyValues));
    }

    [AvaloniaFact]
    public void B15_Separators_Warn_As_Ionic_Does_With_The_Final_Length()
    {
        var sink = new WarningSink();
        var previous = Avalonia.Logging.Logger.Sink;
        Avalonia.Logging.Logger.Sink = sink;
        try
        {
            // Set before the length: read once, at load, with 6. No warning.
            var (otp, _, scene) = Stub(new AvonicInputOtp { Separators = "5", Length = 6 });
            using var _ = scene;
            Assert.DoesNotContain(sink.Messages, m => m.Contains("separator"));

            otp.Separators = "2,2";
            Assert.Contains(sink.Messages, m => m.Contains("Duplicate separator positions"));
            otp.Separators = "9";
            Assert.Contains(sink.Messages, m => m.Contains("greater than the input length (6): 9"));

            // JavaScript's \d is ASCII: an Arabic-Indic three isn't a number to Ionic.
            sink.Messages.Clear();
            otp.Separators = "\u0663";
            Assert.Contains(sink.Messages, m => m.Contains("Invalid separators format"));
            Assert.All(Separators(otp), separator => Assert.False(separator.IsVisible));
        }
        finally
        {
            Avalonia.Logging.Logger.Sink = previous;
        }
    }

    // ── 16, 17: disabled, read-only, the native inputs ───────────────────────────────────────

    [AvaloniaFact]
    public void B16_Disabled_Boxes_Cant_Be_Focused()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "1", IsEnabled = false });
        using var _ = scene;

        otp.SetFocus();
        Settle();
        Assert.Equal(-1, FocusedBox(otp));
        Assert.All(Boxes(otp), box => Assert.False(box.IsEffectivelyEnabled));
    }

    [AvaloniaFact]
    public void B16_Read_Only_Takes_Focus_Not_Typing_But_Backspace_And_Paste_Still_Edit_As_In_Ionic()
    {
        var (otp, _, scene) = Stub(new AvonicInputOtp { Value = "12", Readonly = true });
        using var _ = scene;
        var log = Log.Of(otp);
        otp.SetFocus(1);
        Settle();
        Assert.Equal(1, FocusedBox(otp));
        log.Take();

        Type(scene, "9");
        Assert.Empty(log.Take());
        Assert.Equal("12", otp.Value);

        // Measured in Chromium: Ionic's own handlers don't check readonly.
        Key(scene, PhysicalKey.Backspace);
        Assert.Equal(["input:1"], log.Take());

        // In the now empty box, focus moves back, and the native deletion does nothing in a read-only input.
        Key(scene, PhysicalKey.Backspace);
        Assert.Empty(log.Take());
        Assert.Equal(0, FocusedBox(otp));
        Assert.Equal("1", otp.Value);

        Paste(scene, "55");
        Assert.Equal(["input:55"], log.Take());
    }

    [AvaloniaFact]
    public void B17_Type_Inputmode_And_Autocapitalize_Reach_Each_Box()
    {
        var (otp, _, scene) = Stub();
        using var _ = scene;
        var box = Boxes(otp)[2];

        Assert.Equal(TextInputContentType.Digits, TextInputOptions.GetContentType(box));
        otp.Type = InputOtpType.Text;
        Assert.Equal(TextInputContentType.Normal, TextInputOptions.GetContentType(box));
        otp.Inputmode = InputOtpInputmode.Email;
        Assert.Equal(TextInputContentType.Email, TextInputOptions.GetContentType(box));
        otp.Inputmode = InputOtpInputmode.Decimal;
        Assert.Equal(TextInputContentType.Number, TextInputOptions.GetContentType(box));

        Assert.False(TextInputOptions.GetAutoCapitalization(box));
        otp.Autocapitalize = "words";
        Assert.True(TextInputOptions.GetAutoCapitalization(box));
        otp.Autocapitalize = "characters";
        Assert.False(TextInputOptions.GetAutoCapitalization(box));
        Assert.True(TextInputOptions.GetUppercase(box));

        otp.Readonly = true;
        Assert.True(box.IsReadOnly);
    }

    // ── 18, 19: automation and the description ───────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_The_Group_And_Each_Box_Are_Named()
    {
        var (otp, _, scene) = Stub();
        using var _ = scene;

        var peer = ControlAutomationPeer.CreatePeerForElement(otp);
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Equal("One-time password input", peer.GetName());
        Assert.Equal("Input 2 of 4", AutomationProperties.GetName(Boxes(otp)[1]));

        // Not the description's text.
        otp.Content = "Didn't get a code?";
        Assert.Equal("One-time password input", peer.GetName());

        AutomationProperties.SetName(otp, "Verification code");
        Assert.Equal("Verification code", peer.GetName());

        otp.Length = 6;
        Assert.Equal("Input 2 of 6", AutomationProperties.GetName(Boxes(otp)[1]));
    }

    [AvaloniaFact]
    public void B19_The_Description_Shows_Only_With_Content()
    {
        var (otp, _, scene) = Stub();
        using var _ = scene;
        var description = otp.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_Description");

        Assert.False(description.IsVisible);
        otp.Content = "Didn't get a code?";
        Settle();
        Assert.True(description.IsVisible);
        otp.Content = "  ";
        Settle();
        Assert.False(description.IsVisible);
        otp.Content = new Border();
        Settle();
        Assert.True(description.IsVisible);
    }

    [AvaloniaFact]
    public void B19_The_Description_Is_12px_On_20_Centred_In_Text_Step_300() => InstalledTheme.Run(_ =>
    {
        var (otp, _, scene) = Themed(new AvonicInputOtp { Content = "Didn't get a code? Resend" });
        using var __ = scene;
        var text = otp.GetVisualDescendants().OfType<TextBlock>().Single();

        Assert.Equal(12, text.FontSize);
        Assert.Equal(TextAlignment.Center, text.TextAlignment);
        Assert.Equal(Color.Parse("#4D4D4D"), BrushColor(text.Foreground));

        // Measured: the host 100 high, the description 20 of it, under the 80 of the group.
        AssertBox(16, 80, 358, 20, Bounds(text.GetVisualAncestors().OfType<ContentPresenter>().First(), scene.Window));
        Assert.Equal(100, otp.Bounds.Height, 0.6);
    });

    // ── 20, 21: the look ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_Boxes_By_Size_Centred_With_The_Gap() => InstalledTheme.Run(_ =>
    {
        var medium = new AvonicInputOtp();
        var small = new AvonicInputOtp { Size = InputOtpSize.Small, Length = 6, Separators = "3" };
        var large = new AvonicInputOtp { Size = InputOtpSize.Large };
        var page = new StackPanel { Margin = new Thickness(16, 0), Children = { medium, small, large } };
        using var scene = new Scene(page);
        Render();

        Assert.Equal(80, medium.Bounds.Height, 0.6);
        var boxes = Boxes(medium);
        AssertBox(81, 16, 48, 48, Bounds(boxes[0], scene.Window));
        AssertBox(261, 16, 48, 48, Bounds(boxes[3], scene.Window));

        // Measured: 47, 95, 143, a separator at 191, then 207, 255, 303; 8 apart.
        boxes = Boxes(small);
        AssertBox(47, 96, 40, 40, Bounds(boxes[0], scene.Window));
        AssertBox(143, 96, 40, 40, Bounds(boxes[2], scene.Window));
        AssertBox(191, 112, 8, 8, Bounds(Separators(small)[2], scene.Window));
        AssertBox(207, 96, 40, 40, Bounds(boxes[3], scene.Window));

        AssertBox(65, 168, 56, 56, Bounds(Boxes(large)[0], scene.Window));
        AssertBox(269, 168, 56, 56, Bounds(Boxes(large)[3], scene.Window));
    });

    [AvaloniaFact]
    public void B20_Too_Narrow_The_Boxes_Shrink_To_40_And_Overflow_Both_Sides() => InstalledTheme.Run(_ =>
    {
        var eight = new AvonicInputOtp { Length = 8 };
        var separated = new AvonicInputOtp { Length = 6, Separators = "all", Value = "1234" };
        var page = new StackPanel { Margin = new Thickness(16, 0), Children = { eight, separated } };
        using var scene = new Scene(page);
        Render();

        // Measured: 8 × 40 + 7 × 12 is 404 in 358, from x = -7.
        AssertBox(-7, 16, 40, 48, Bounds(Boxes(eight)[0], scene.Window));
        AssertBox(357, 16, 40, 48, Bounds(Boxes(eight)[7], scene.Window));

        // Separators don't shrink: 6 × 40 + 5 × 8 + 10 × 12 is 400, from x = -5.
        AssertBox(-5, 96, 40, 48, Bounds(Boxes(separated)[0], scene.Window));
        AssertBox(47, 116, 8, 8, Bounds(Separators(separated)[0], scene.Window));
        AssertBox(355, 96, 40, 48, Bounds(Boxes(separated)[5], scene.Window));

        // overflow: visible. Left of the host, the first box still takes a click.
        Render();
        Assert.Same(Boxes(eight)[0], (scene.Window.InputHitTest(new Point(5, 40)) as Visual)?.GetSelfAndVisualAncestors().OfType<InputOtpBox>().FirstOrDefault());
    });

    // align-items: center. A box with no height of its own is as tall as its text, in the middle of the row, not stretched to it.
    [AvaloniaFact]
    public void B20_A_Box_With_No_Height_Is_Centred_Down_The_Row() => InstalledTheme.Run(_ =>
    {
        var (otp, _, scene) = Themed();
        using var __ = scene;
        var box = Boxes(otp)[1];
        box.Height = double.NaN;
        Render();

        var bounds = Bounds(box, scene.Window);
        var row = Bounds(Boxes(otp)[0], scene.Window);
        Assert.True(bounds.Height < row.Height, $"{bounds.Height}");
        Assert.Equal(row.Center.Y, bounds.Center.Y, tolerance: 1);
    });

    [AvaloniaFact]
    public void B20_Separators_Are_Round_Step_150_Dots() => InstalledTheme.Run(_ =>
    {
        var (otp, _, scene) = Themed(new AvonicInputOtp { Separators = "2" });
        using var __ = scene;
        var separator = Separators(otp)[1];

        Assert.Equal(Color.Parse("#D9D9D9"), BrushColor(separator.Background));
        Assert.Equal(new CornerRadius(4), separator.CornerRadius);
        AssertBox(191, 36, 8, 8, Bounds(separator, scene.Window));
    });

    [AvaloniaFact]
    public void B21_Shapes() => InstalledTheme.Run(_ =>
    {
        var (otp, _, scene) = Themed();
        using var __ = scene;
        var box = Boxes(otp)[0];

        Assert.Equal(new CornerRadius(16), box.CornerRadius);
        otp.Shape = InputOtpShape.Soft;
        Assert.Equal(new CornerRadius(8), box.CornerRadius);
        otp.Shape = InputOtpShape.Rectangular;
        Assert.Equal(new CornerRadius(0), box.CornerRadius);
    });

    [AvaloniaFact]
    public void B21_Fill_Disabled_And_Read_Only_Colours() => InstalledTheme.Run(_ =>
    {
        var (otp, _, scene) = Themed(new AvonicInputOtp { Value = "1" });
        using var __ = scene;
        var box = Boxes(otp)[0];
        void Assert3(string border, string background, string text)
        {
            Assert.Equal(Color.Parse(border), BrushColor(box.BorderBrush));
            Assert.Equal(Color.Parse(background), BrushColor(box.Background));
            Assert.Equal(Color.Parse(text), BrushColor(box.Foreground));
        }

        Assert.Equal(new Thickness(1), box.BorderThickness);
        Assert.Equal(14, box.FontSize);

        // Chromium's 1px 2px for an <input>, and text-align: center.
        Assert.Equal(new Thickness(2, 1), box.Padding);
        Assert.Equal(TextAlignment.Center, box.TextAlignment);
        Assert3("#B3B3B3", "#00FFFFFF", "#000000");
        otp.Readonly = true;
        Assert3("#B3B3B3", "#F2F2F2", "#000000");
        otp.Readonly = false;
        otp.IsEnabled = false;
        Assert3("#E6E6E6", "#F2F2F2", "#A6A6A6");

        otp.Fill = InputOtpFill.Solid;
        Assert3("#E6E6E6", "#E6E6E6", "#A6A6A6");
        otp.IsEnabled = true;
        Assert3("#F2F2F2", "#F2F2F2", "#000000");
        otp.Readonly = true;
        Assert3("#E6E6E6", "#E6E6E6", "#000000");
    });

    [AvaloniaFact]
    public void B21_The_Focused_Box_Is_2px_In_The_Highlight_As_Is_The_Caret() => InstalledTheme.Run(_ =>
    {
        var (otp, other, scene) = Themed();
        using var __ = scene;
        otp.SetFocus();
        Settle();
        var boxes = Boxes(otp);

        Assert.Equal(new Thickness(2), boxes[0].BorderThickness);
        Assert.Equal(Primary, BrushColor(boxes[0].BorderBrush));
        Assert.Equal(Primary, BrushColor(boxes[0].CaretBrush));
        Assert.Equal(new Thickness(1), boxes[1].BorderThickness);
        Assert.Equal(Color.Parse("#B3B3B3"), BrushColor(boxes[1].BorderBrush));

        // Solid: the same on its step 50.
        otp.Fill = InputOtpFill.Solid;
        Assert.Equal(Primary, BrushColor(boxes[0].BorderBrush));
        Assert.Equal(Color.Parse("#F2F2F2"), BrushColor(boxes[1].BorderBrush));

        other.Focus();
        Settle();
        Assert.Equal(new Thickness(1), boxes[0].BorderThickness);
        Assert.Equal(Color.Parse("#F2F2F2"), BrushColor(boxes[0].BorderBrush));
    });

    [AvaloniaFact]
    public void B21_Valid_While_Focused_And_Touched_And_Invalid_Border_Every_Box() => InstalledTheme.Run(_ =>
    {
        var (otp, other, scene) = Themed();
        using var __ = scene;
        var boxes = Boxes(otp);

        otp.Classes.Add(AvonicInputOtp.ValidClass);
        Assert.Equal(Color.Parse("#B3B3B3"), BrushColor(boxes[1].BorderBrush));
        otp.SetFocus();
        Settle();
        Assert.Equal(Success, BrushColor(boxes[1].BorderBrush));
        Assert.Equal(Success, BrushColor(boxes[0].CaretBrush));
        other.Focus();
        Settle();

        otp.Classes.Remove(AvonicInputOtp.ValidClass);
        otp.Classes.Add(AvonicInputOtp.InvalidClass);
        Assert.Equal(Color.Parse("#B3B3B3"), BrushColor(boxes[1].BorderBrush));
        otp.Classes.Add(AvonicInputOtp.TouchedClass);
        Assert.Equal(Danger, BrushColor(boxes[1].BorderBrush));
        Assert.Equal(new Thickness(1), boxes[1].BorderThickness);
    });

    [AvaloniaFact]
    public void B21_A_Colour_Borders_Outline_Boxes_In_Its_Base() => InstalledTheme.Run(_ =>
    {
        var (otp, other, scene) = Themed(new AvonicInputOtp { Color = "danger" });
        using var __ = scene;
        var boxes = Boxes(otp);

        // Measured: rgba(197, 0, 15, 0.6), focused or not, 2 px when focused; the caret in the base.
        Assert.Equal(Rgba(Danger, 0.6), BrushColor(boxes[1].BorderBrush));
        otp.SetFocus();
        Settle();
        Assert.Equal(Rgba(Danger, 0.6), BrushColor(boxes[0].BorderBrush));
        Assert.Equal(new Thickness(2), boxes[0].BorderThickness);
        Assert.Equal(Danger, BrushColor(boxes[0].CaretBrush));

        // Solid: only the focused box.
        otp.Fill = InputOtpFill.Solid;
        Assert.Equal(Rgba(Danger, 0.6), BrushColor(boxes[0].BorderBrush));
        Assert.Equal(Color.Parse("#F2F2F2"), BrushColor(boxes[1].BorderBrush));
        other.Focus();
        Settle();

        // With ion-invalid, touched or not: danger; ion-valid: success; disabled outline: the base at 0.3.
        otp.Color = "primary";
        otp.Fill = InputOtpFill.Outline;
        otp.Classes.Add(AvonicInputOtp.InvalidClass);
        Assert.Equal(Danger, BrushColor(boxes[1].BorderBrush));
        otp.Classes.Remove(AvonicInputOtp.InvalidClass);
        otp.Classes.Add(AvonicInputOtp.ValidClass);
        Assert.Equal(Success, BrushColor(boxes[1].BorderBrush));
        otp.Classes.Remove(AvonicInputOtp.ValidClass);
        otp.IsEnabled = false;
        Assert.Equal(Rgba(Primary, 0.3), BrushColor(boxes[1].BorderBrush));
    });

    [AvaloniaFact]
    public void B21_An_Apps_Keys_Restyle_The_Boxes() => InstalledTheme.Run(_ =>
    {
        var otp = new AvonicInputOtp
        {
            Resources =
            {
                ["Avonic.InputOtp.Width"] = 30.0,
                ["Avonic.InputOtp.Height"] = 36.0,
                ["Avonic.InputOtp.MinWidth"] = 20.0,
                ["Avonic.InputOtp.BorderRadius"] = "25%",
                ["Avonic.InputOtp.Background"] = Brushes.Yellow,
                ["Avonic.InputOtp.PaddingTop"] = 4.0,
            },
        };
        var (_, _, scene) = Themed(otp);
        using var __ = scene;
        var box = Boxes(otp)[0];

        Assert.Equal(30, box.Bounds.Width, 0.6);
        Assert.Equal(36, box.Bounds.Height, 0.6);
        // 25% of 30 across and 36 down: the smaller, 7.5.
        Assert.Equal(new CornerRadius(7.5), box.CornerRadius);
        Assert.Equal(Colors.Yellow, BrushColor(box.Background));
        Assert.Equal(4 + 36 + 16, otp.Bounds.Height, 0.6);

        // The box sits under the 4 px padding, not centred in the whole group.
        Assert.Equal(4, Bounds(box, otp).Y, 0.6);
    });
}
