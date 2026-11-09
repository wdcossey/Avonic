using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-radio and ion-radio-group in Chromium, at 390 px wide, and the tests are named after the
/// behaviours in docs/components/ion-radio/PLAN.md. Tests with <c>InstalledTheme</c> check the look; the others run on the
/// functional stub. Text is a different font here, so a label that has to be a known size is a box of that size.
/// </summary>
public class AvonicRadioTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Danger = Color.Parse("#C5000F");

    /// <summary>A label of a known size: a line of 16 px Roboto is 19 high.</summary>
    private static Border Box(double width = 40, double height = 19) => new() { Width = width, Height = height };

    private static AvonicRadio Radio(object? value, bool enabled = true) => new() { Value = value, Content = Box(), IsEnabled = enabled };

    private static AvonicRadioGroup Group(params Control[] children)
    {
        var group = new AvonicRadioGroup();
        foreach (var child in children)
        {
            group.Items.Add(child);
        }

        return group;
    }

    private static AvonicItem Item(AvonicRadio radio) => new() { Content = radio };

    private static ISelectionItemProvider Selection(AvonicRadio radio) =>
        (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(radio);

    /// <summary><c>aria-checked</c>.</summary>
    private static bool Checked(AvonicRadio radio) => Selection(radio).IsSelected;

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static Rect Bounds(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void AssertBox(double x, double y, double width, double height, Rect actual)
    {
        // Layout rounds to whole pixels, and Chromium keeps fractions.
        Assert.InRange(actual.X, x - 0.5, x + 0.5);
        Assert.InRange(actual.Y, y - 0.5, y + 0.5);
        Assert.InRange(actual.Width, width - 0.5, width + 0.5);
        Assert.InRange(actual.Height, height - 0.5, height + 0.5);
    }

    private static Scene Show(Control content, bool white = false)
    {
        var scene = new Scene(new StackPanel { Children = { content } });
        if (white)
        {
            scene.Window.Background = Brushes.White;
        }

        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    private static void Render(Scene scene)
    {
        Dispatcher.UIThread.RunJobs();
        scene.Window.CaptureRenderedFrame();
    }

    private static void Click(Scene scene, Control control) => Click(scene, Bounds(control, scene.Window).Center);

    private static void Click(Scene scene, Point point)
    {
        Render(scene);
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Press(Scene scene, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        scene.Window.KeyPressQwerty(key, modifiers);
        scene.Window.KeyReleaseQwerty(key, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    private static List<object?> Changes(AvonicRadioGroup group)
    {
        var changes = new List<object?>();
        group.IonChange += (_, e) => changes.Add(e.Value);
        return changes;
    }

    private static Color Pixel(Scene scene, int x, int y)
    {
        Dispatcher.UIThread.RunJobs();
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, x, y);
    }

    private static void AssertClose(Color expected, Color actual, int tolerance = 3)
    {
        Assert.InRange(actual.R, expected.R - tolerance, expected.R + tolerance);
        Assert.InRange(actual.G, expected.G - tolerance, expected.G + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
    }

    /// <summary>A brush as CSS's <c>rgba()</c>: its colour, with its opacity folded into the alpha.</summary>
    private static Color Rgba(IBrush? brush)
    {
        var solid = Assert.IsAssignableFrom<ISolidColorBrush>(brush);
        return Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B);
    }

    // ── R1: alone, a click checks it for good ────────────────────────────────────────────────

    [AvaloniaFact]
    public void R1_Alone_A_Click_Checks_It_And_A_Second_Leaves_It_Checked()
    {
        var radio = Radio("a");
        using var scene = Show(radio);
        Assert.False(Checked(radio));

        Click(scene, radio);
        Assert.True(Checked(radio));

        Click(scene, radio);
        Assert.True(Checked(radio));
    }

    // ── R2: in a group, it follows the group's value ────────────────────────────────────────

    [AvaloniaFact]
    public void R2_In_A_Group_It_Is_Checked_When_Its_Value_Is_The_Groups()
    {
        var a = Radio("a");
        var b = Radio("b");
        var group = Group(a, b);
        group.Value = "b";
        using var scene = Show(group);

        Assert.False(Checked(a));
        Assert.True(Checked(b));

        group.Value = "a";
        Assert.True(Checked(a));
        Assert.False(Checked(b));

        group.Value = null;
        Assert.False(Checked(a));
        Assert.False(Checked(b));
    }

    [AvaloniaFact]
    public void R2_A_Radio_Follows_Its_Own_Value_Too()
    {
        var a = Radio("a");
        var b = Radio("b");
        var group = Group(a, b);
        group.Value = "a";
        using var scene = Show(group);

        b.Value = "a";
        Assert.True(Checked(b));

        a.Value = "c";
        Assert.False(Checked(a));
    }

    [AvaloniaFact]
    public void R2_A_Radio_Added_Later_Reads_The_Groups_Value()
    {
        var group = Group(Radio("a"));
        group.Value = "b";
        using var scene = Show(group);

        var b = Radio("b");
        group.Items.Add(b);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Checked(b));
    }

    [AvaloniaFact]
    public void R2_A_Radio_Inside_An_Item_Inside_A_Panel_Finds_Its_Group()
    {
        var b = Radio("b");
        var group = Group(new StackPanel { Children = { Item(Radio("a")), Item(b) } });
        group.Value = "b";
        using var scene = Show(group);

        Assert.True(Checked(b));
    }

    // ── R3: compareWith ──────────────────────────────────────────────────────────────────────

    private sealed record Fruit(int Id, string Name);

    [AvaloniaFact]
    public void R3_CompareWith_A_Function_Matches_Through_It()
    {
        var apple = Radio(new Fruit(1, "Apple"));
        var pear = Radio(new Fruit(2, "Pear"));
        var group = Group(apple, pear);
        group.CompareWith = (Func<object?, object?, bool>)((current, compare) => ((Fruit)current!).Id == ((Fruit)compare!).Id);
        group.Value = new Fruit(2, "Another pear");
        using var scene = Show(group);

        Assert.False(Checked(apple));
        Assert.True(Checked(pear));
    }

    [AvaloniaFact]
    public void R3_CompareWith_A_Property_Name_Compares_That_Property()
    {
        var apple = Radio(new Fruit(1, "Apple"));
        var pear = Radio(new Fruit(2, "Pear"));
        var group = Group(apple, pear);
        group.Value = new Fruit(1, "Another apple");
        using var scene = Show(group);
        Assert.False(Checked(apple));

        group.CompareWith = "Id";
        group.Value = new Fruit(1, "Yet another apple");
        Assert.True(Checked(apple));
        Assert.False(Checked(pear));
    }

    [AvaloniaFact]
    public void R3_CompareWith_A_Property_Name_Reads_A_Dictionary_Entry()
    {
        var apple = Radio(new Dictionary<string, object> { ["id"] = 1 });
        var group = Group(apple, Radio(new Dictionary<string, object> { ["id"] = 2 }));
        group.CompareWith = "id";
        group.Value = new Dictionary<string, object> { ["id"] = 1 };
        using var scene = Show(group);

        Assert.True(Checked(apple));
    }

    [AvaloniaFact]
    public void R3_An_Array_Value_Checks_Every_Radio_In_It()
    {
        var a = Radio("a");
        var b = Radio("b");
        var c = Radio("c");
        var group = Group(a, b, c);
        group.Value = new[] { "a", "c" };
        using var scene = Show(group);

        Assert.True(Checked(a));
        Assert.False(Checked(b));
        Assert.True(Checked(c));
    }

    [AvaloniaFact]
    public void R3_A_String_Value_Is_Not_An_Array_Of_Characters()
    {
        var a = Radio("a");
        var group = Group(a);
        group.Value = "ab";
        using var scene = Show(group);

        Assert.False(Checked(a));
    }

    [AvaloniaFact]
    public void R3_A_Radio_Whose_Value_Is_An_Array_Is_Checked_When_It_Holds_The_Groups()
    {
        var both = Radio(new[] { "a", "b" });
        var other = Radio(new[] { "c" });
        var group = Group(both, other);
        group.Value = "b";
        using var scene = Show(group);

        Assert.True(Checked(both));
        Assert.False(Checked(other));
    }

    // ── R4: a unique value ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R4_Radios_Without_A_Value_Are_Two_Choices()
    {
        var first = new AvonicRadio { Content = Box() };
        var second = new AvonicRadio { Content = Box() };
        var group = Group(first, second);
        using var scene = Show(group);

        Assert.StartsWith("ion-rb-", Assert.IsType<string>(first.Value));
        Assert.NotEqual(first.Value, second.Value);

        Click(scene, first);
        Assert.Equal(first.Value, group.Value);
        Assert.True(Checked(first));
        Assert.False(Checked(second));
    }

    // ── R5: disabled ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R5_A_Disabled_Radio_Takes_No_Click()
    {
        var a = Radio("a", enabled: false);
        var group = Group(a, Radio("b"));
        var changes = Changes(group);
        using var scene = Show(group);

        Click(scene, a);
        Assert.Null(group.Value);
        Assert.Empty(changes);

    }

    [AvaloniaFact]
    public void R5_A_Disabled_Radio_Alone_Takes_No_Click()
    {
        var radio = Radio("a", enabled: false);
        using var scene = Show(radio);

        Click(scene, radio);
        Assert.False(Checked(radio));
    }

    [AvaloniaFact]
    public void R5_A_Disabled_Radio_Is_Not_Selected_From_Automation()
    {
        var a = Radio("a", enabled: false);
        var group = Group(a);
        using var scene = Show(group);

        Selection(a).Select();
        Assert.Null(group.Value);
    }

    // ── R6: ionFocus and ionBlur ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R6_A_Click_Focuses_It_With_IonFocus_And_The_Last_One_Blurs()
    {
        var a = Radio("a");
        var b = Radio("b");
        var group = Group(a, b);
        var log = new List<string>();
        a.IonFocus += (_, _) => log.Add("focus:a");
        a.IonBlur += (_, _) => log.Add("blur:a");
        b.IonFocus += (_, _) => log.Add("focus:b");
        using var scene = Show(group);

        Click(scene, a);
        Assert.True(a.IsFocused);
        Click(scene, b);

        Assert.Equal(["focus:a", "blur:a", "focus:b"], log);
    }

    [AvaloniaFact]
    public void R6_Focus_On_Something_Inside_The_Label_Is_Not_The_Radios()
    {
        var button = new Button { Content = "Inside" };
        var radio = new AvonicRadio { Value = "a", Content = button };
        var log = new List<string>();
        radio.IonFocus += (_, _) => log.Add("focus");
        radio.IonBlur += (_, _) => log.Add("blur");
        using var scene = Show(radio);

        button.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(log);
    }

    // ── R7: the tab stop ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R7_Alone_It_Is_Not_A_Tab_Stop()
    {
        var radio = Radio("a");
        using var scene = Show(radio);

        Assert.True(radio.Focusable);
        Assert.False(radio.IsTabStop);
    }

    [AvaloniaFact]
    public void R7_In_A_Group_The_Checked_Radio_Is_The_One_Tab_Stop_And_It_Follows_The_Value()
    {
        var a = Radio("a");
        var b = Radio("b");
        var c = Radio("c");
        var group = Group(a, b, c);
        group.Value = "b";
        using var scene = Show(group);

        Assert.Equal([false, true, false], new[] { a.IsTabStop, b.IsTabStop, c.IsTabStop });

        group.Value = "c";
        Assert.Equal([false, false, true], new[] { a.IsTabStop, b.IsTabStop, c.IsTabStop });
    }

    [AvaloniaFact]
    public void R7_With_Nothing_Checked_The_First_Enabled_Radio_Is_The_Tab_Stop()
    {
        var a = Radio("a", enabled: false);
        var b = Radio("b");
        var c = Radio("c", enabled: false);
        var group = Group(a, b, c);
        group.Value = "c";
        using var scene = Show(group);

        Assert.Equal([false, true, false], new[] { a.IsTabStop, b.IsTabStop, c.IsTabStop });
    }

    [AvaloniaFact]
    public void R7_Tab_Goes_To_The_Checked_Radio_And_Then_Out_Of_The_Group()
    {
        var before = new Button { Content = "Before" };
        var a = Radio("a");
        var b = Radio("b");
        var after = new Button { Content = "After" };
        var group = Group(a, b);
        group.Value = "b";
        using var scene = Show(new StackPanel { Children = { before, group, after } });

        before.Focus();
        Press(scene, PhysicalKey.Tab);
        Assert.True(b.IsFocused);

        Press(scene, PhysicalKey.Tab);
        Assert.True(after.IsFocused);
    }

    [AvaloniaFact]
    public void R7_An_Apps_Own_IsTabStop_Wins()
    {
        var a = Radio("a");
        a.IsTabStop = true;
        var b = Radio("b");
        var group = Group(a, b);
        group.Value = "b";
        using var scene = Show(group);

        Assert.True(a.IsTabStop);
    }

    [AvaloniaFact]
    public void R7_Tab_Goes_On_From_The_Checked_Radio_To_A_Button_In_The_Group()
    {
        var before = new Button { Content = "Before" };
        var a = Radio("a");
        var inside = new Button { Content = "Inside" };
        var after = new Button { Content = "After" };
        var group = Group(a, Radio("b"), inside);
        group.Value = "a";
        using var scene = Show(new StackPanel { Children = { before, group, after } });

        before.Focus();
        Press(scene, PhysicalKey.Tab);
        Assert.True(a.IsFocused);
        Press(scene, PhysicalKey.Tab);
        Assert.True(inside.IsFocused);
    }

    [AvaloniaFact]
    public void R7_Tab_Comes_Back_To_The_Choice_Not_To_The_Radio_Last_Focused()
    {
        var before = new Button { Content = "Before" };
        var a = Radio("a");
        var c = Radio("c");
        var group = Group(a, Radio("b"), c);
        group.Value = "a";
        using var scene = Show(new StackPanel { Children = { before, group } });

        before.Focus();
        Press(scene, PhysicalKey.Tab);
        Assert.True(a.IsFocused);
        group.Value = "c";
        Press(scene, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.True(before.IsFocused);

        Press(scene, PhysicalKey.Tab);
        Assert.True(c.IsFocused);
    }

    // ── R8: in an item ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R8_In_An_Item_The_Item_Is_Activatable_And_Focusable_Not_The_Radio()
    {
        var alone = Radio("a");
        var inside = Radio("b");
        using var scene = Show(new StackPanel { Children = { alone, Item(inside) } });

        Assert.True(Activatable.GetIsEnabled(alone));
        Assert.True(FocusVisible.GetIsEnabled(alone));
        Assert.False(Activatable.GetIsEnabled(inside));
        Assert.False(FocusVisible.GetIsEnabled(inside));
    }

    [AvaloniaFact]
    public void R8_An_Apps_Own_Activatable_And_FocusVisible_Win()
    {
        var radio = Radio("a");
        Activatable.SetIsEnabled(radio, false);
        FocusVisible.SetIsEnabled(radio, false);
        using var scene = Show(radio);

        Assert.False(Activatable.GetIsEnabled(radio));
        Assert.False(FocusVisible.GetIsEnabled(radio));
    }

    [AvaloniaFact]
    public void R8_A_Tap_On_The_Items_Padding_Clicks_The_Radio() => InstalledTheme.Run(_ =>
    {
        var a = Radio("a");
        var b = Radio("b");
        var item = Item(b);
        var group = Group(Item(a), item);
        group.Value = "a";
        var changes = Changes(group);
        using var scene = Show(group);

        var box = Bounds(item, scene.Window);
        Click(scene, new Point(box.X + 6, box.Center.Y));

        Assert.Equal("b", group.Value);
        Assert.Equal(["b"], changes);
        Assert.False(b.IsFocused);
    });

    [AvaloniaFact]
    public void R8_In_An_Item_It_Fills_The_Row_47_High() => InstalledTheme.Run(_ =>
    {
        var radio = Radio("a");
        using var scene = Show(Item(radio));

        // measured a: 16, 0 (in its item), 358 by 47
        AssertBox(16, 0, 358, 47, Bounds(radio, scene.Window));
    });

    [AvaloniaFact]
    public void R8_In_An_Items_Start_Slot_It_Is_As_Wide_As_Its_Content_And_Centred() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "a", Content = Box(9.5) };
        using var scene = Show(new AvonicItem { StartContent = radio, Content = new AvonicLabel { Content = "Label" } });

        // measured n5: 45.5 by 39, label 10 below the top
        var box = Bounds(radio, scene.Window);
        Assert.Equal(45.5, box.Width, 0.5);
        Assert.Equal(39, box.Height, 0.5);
        Assert.Equal(10, Bounds(Part<ContentPresenter>(radio, "PART_Label"), radio).Y, 0.5);
    });

    // ── R9: focus by keyboard ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R9_Focus_By_Keyboard_Shows_A_36_Circle_Of_The_Primary_Tint_At_0_2() => InstalledTheme.Run(_ =>
    {
        var before = new Button { Content = "Before" };
        var radio = Radio("a");
        var group = Group(radio);
        group.Value = "a";
        using var scene = Show(new StackPanel { Children = { before, group } }, white: true);
        var focus = Part<Border>(radio, "PART_Focus");
        Assert.False(focus.IsEffectivelyVisible);

        before.Focus();
        Press(scene, PhysicalKey.Tab);
        Assert.True(radio.IsFocused);
        Assert.True(focus.IsEffectivelyVisible);

        var ring = Bounds(Part<Border>(radio, "PART_Container"), scene.Window);
        AssertBox(ring.X - 8, ring.Y - 8, 36, 36, Bounds(focus, scene.Window));

        // rgb(26, 101, 235) at 0.2 on white, just inside the circle, outside the ring: (209, 224, 251)
        AssertClose(Color.FromRgb(209, 224, 251), Pixel(scene, (int)(ring.Center.X), (int)(ring.Y - 5)));
    });

    [AvaloniaFact]
    public void R9_A_Click_Shows_No_Circle() => InstalledTheme.Run(_ =>
    {
        var radio = Radio("a");
        using var scene = Show(radio);

        Click(scene, radio);
        Assert.True(radio.IsFocused);
        Assert.False(Part<Border>(radio, "PART_Focus").IsEffectivelyVisible);
    });

    [AvaloniaFact]
    public void R9_The_Circle_Is_Not_Clipped_By_The_Radio_Or_Its_Group() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "a" };
        var group = Group(radio);
        using var scene = Show(group);

        Assert.False(radio.ClipToBounds);
        Assert.False(group.ClipToBounds);
    });

    // outline: none. The adorner comes from the app's base theme, which TestApp doesn't load, so this adds it underneath, as
    // the sample has it. FocusAdorner reads null either way, so only the layer shows what's drawn.
    [AvaloniaFact]
    public void R9_There_Is_No_Focus_Adorner_Over_A_Base_Theme()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var before = new Button { Content = "Before" };
                var radio = Radio("a");
                var group = Group(radio);
                var other = new Button { Content = "After" };
                using var scene = Show(new StackPanel { Children = { before, group, other } });
                var layer = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(radio)!;
                before.Focus();
                Dispatcher.UIThread.RunJobs();

                Press(scene, PhysicalKey.Tab);
                Assert.True(radio.IsFocused);
                Assert.Empty(layer.Children);

                // Proves the base theme really does give a focused control one.
                Press(scene, PhysicalKey.Tab);
                Assert.True(other.IsFocused);
                Assert.Single(layer.Children);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    // ── R10: automation ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R10_It_Is_A_Radio_Button_Named_From_Its_Label_Selected_When_Checked()
    {
        var a = new AvonicRadio { Value = "a", Content = "Apple" };
        var b = new AvonicRadio { Value = "b", Content = "Banana", IsEnabled = false };
        var group = Group(a, b);
        group.Value = "a";
        using var scene = Show(group);

        var peer = ControlAutomationPeer.CreatePeerForElement(a);
        Assert.Equal(AutomationControlType.RadioButton, peer.GetAutomationControlType());
        Assert.Equal("Apple", peer.GetName());
        Assert.True(peer.IsEnabled());
        Assert.True(Checked(a));
        Assert.False(ControlAutomationPeer.CreatePeerForElement(b).IsEnabled());
    }

    [AvaloniaFact]
    public void R10_Selecting_It_From_Automation_Is_A_Click()
    {
        var a = Radio("a");
        var b = Radio("b");
        var group = Group(a, b);
        group.Value = "a";
        var changes = Changes(group);
        using var scene = Show(group);

        Selection(b).Select();
        Assert.Equal("b", group.Value);
        Assert.Equal(["b"], changes);
    }

    [AvaloniaFact]
    public void R10_Selecting_The_Checked_Radio_Leaves_It_Checked()
    {
        var a = Radio("a");
        var group = Group(a);
        group.AllowEmptySelection = true;
        group.Value = "a";
        var changes = Changes(group);
        using var scene = Show(group);

        Selection(a).Select();
        Selection(a).AddToSelection();
        Assert.Equal("a", group.Value);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void R10_A_Change_Of_Checked_Is_Announced()
    {
        var a = Radio("a");
        var group = Group(a);
        using var scene = Show(group);
        var peer = ControlAutomationPeer.CreatePeerForElement(a);
        var changed = new List<AutomationProperty>();
        peer.PropertyChanged += (_, e) => changed.Add(e.Property);

        group.Value = "a";
        Assert.Contains(SelectionItemPatternIdentifiers.IsSelectedProperty, changed);
    }

    // ── R11: no label ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void R11_With_No_Content_It_Is_Just_The_Ring() => InstalledTheme.Run(_ =>
    {
        var none = new AvonicRadio { Value = "a" };
        var empty = new AvonicRadio { Value = "b", Content = "" };
        var label = new AvonicRadio { Value = "c", Content = Box() };
        using var scene = Show(new StackPanel { Children = { none, empty, label } });

        Assert.Equal(new Size(20, 20), none.Bounds.Size);
        Assert.Equal(new Size(20, 20), empty.Bounds.Size);

        // measured 16 (Label): 38.69 + 16 + 20 wide, 20 high
        Assert.Equal(76, label.Bounds.Width, 0.5);
        label.Content = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(20, label.Bounds.Width, 0.5);
    });

    // ── V1–V4: the ring and the mark ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void V1_A_20_Ring_With_A_2_Border_At_60_Percent_Text_And_A_Centred_10_Mark() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "a" };
        using var scene = Show(radio);
        var ring = Part<Border>(radio, "PART_Container");
        var mark = Part<Border>(radio, "PART_Mark");

        AssertBox(0, 0, 20, 20, Bounds(ring, radio));
        Assert.Equal(new Thickness(2), ring.BorderThickness);
        Assert.Equal(new CornerRadius(10), ring.CornerRadius);
        Assert.Equal(Color.FromArgb(153, 0, 0, 0), Rgba(ring.BorderBrush));

        // In the ring, inside its border: (16 - 10) / 2 + 2
        Assert.Equal(new Rect(5, 5, 10, 10), mark.Bounds);
        Assert.Equal(new CornerRadius(5), mark.CornerRadius);
        Assert.Equal(Primary, Rgba(mark.Background));

        // scale3d(0, 0, 0) unchecked
        Assert.Equal(0, mark.GetValue(Css.ScaleProperty));
    });

    [AvaloniaFact]
    public void V1_The_Radius_Keys_Take_A_Number() => InstalledTheme.Run(theme =>
    {
        var radio = new AvonicRadio { Value = "a" };
        radio.Resources["Avonic.Radio.BorderRadius"] = 4.0;
        radio.Resources["Avonic.Radio.InnerBorderRadius"] = "0";
        using var scene = Show(radio);

        Assert.Equal(new CornerRadius(4), Part<Border>(radio, "PART_Container").CornerRadius);
        Assert.Equal(new CornerRadius(0), Part<Border>(radio, "PART_Mark").CornerRadius);
    });

    [AvaloniaFact]
    public void V2_Checked_Is_A_Primary_Ring_And_A_Full_Mark_With_No_Transition_On_The_First_Frame() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "a" };
        var group = Group(radio);
        group.Value = "a";
        using var scene = Show(group, white: true);

        var mark = Part<Border>(radio, "PART_Mark");
        Assert.Equal(1, mark.GetValue(Css.ScaleProperty));
        Assert.Equal(Primary, Rgba(Part<Border>(radio, "PART_Container").BorderBrush));
        AssertClose(Primary, Pixel(scene, 10, 10));
    });

    [AvaloniaFact]
    public void V2_The_Mark_Eases_In_Over_280_ms_And_Out_Again() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "a" };
        var group = Group(radio);
        using var scene = Show(group);
        var mark = Part<Border>(radio, "PART_Mark");

        var transition = Assert.IsType<DoubleTransition>(Assert.Single(mark.Transitions!));
        Assert.Equal(Css.ScaleProperty, transition.Property);
        Assert.Equal(TimeSpan.FromMilliseconds(280), transition.Duration);
        var easing = Assert.IsType<CubicBezierEasing>(transition.Easing);
        Assert.Equal((0.4, 0, 0.2, 1), (easing.X1, easing.Y1, easing.X2, easing.Y2));

        // Checked now, it starts from 0: the transition, not the end value.
        group.Value = "a";
        Assert.True(mark.GetValue(Css.ScaleProperty) < 0.5);

        mark.Transitions = [];
        group.Value = null;
        Assert.Equal(0, mark.GetValue(Css.ScaleProperty));
    });

    [AvaloniaFact]
    public void V3_Color_Paints_The_Mark_And_The_Checked_Ring_In_Its_Base() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicRadio { Value = "x", Color = "danger" };
        var checkedOne = new AvonicRadio { Value = "a", Color = "danger" };
        var group = Group(plain, checkedOne);
        group.Value = "a";
        using var scene = Show(group);

        Assert.Equal(Danger, Rgba(Part<Border>(plain, "PART_Mark").Background));
        Assert.Equal(Color.FromArgb(153, 0, 0, 0), Rgba(Part<Border>(plain, "PART_Container").BorderBrush));
        Assert.Equal(Danger, Rgba(Part<Border>(checkedOne, "PART_Container").BorderBrush));
    });

    [AvaloniaFact]
    public void V4_Disabled_Fades_The_Label_To_0_38_And_The_Ring_To_0_63() => InstalledTheme.Run(_ =>
    {
        var radio = Radio("a", enabled: false);
        using var scene = Show(radio);

        Assert.Equal(0.38, Part<ContentPresenter>(radio, "PART_Label").Opacity);
        Assert.Equal(0.63, Part<Panel>(radio, "PART_Native").Opacity);
    });

    // ── V5–V9: the layout ────────────────────────────────────────────────────────────────────

    private static (Rect Label, Rect Ring) Layout(AvonicRadio radio) =>
        (Bounds(Part<ContentPresenter>(radio, "PART_Label"), radio), Bounds(Part<Border>(radio, "PART_Container"), radio));

    [AvaloniaFact]
    public void V5_Start_In_An_Item_Puts_The_Label_First_And_The_Ring_At_The_End() => InstalledTheme.Run(_ =>
    {
        var radio = Radio("a");
        using var scene = Show(Item(radio));

        // measured a, from the radio's top left: label 0, 14; ring 338, 13.5
        var (label, ring) = Layout(radio);
        AssertBox(0, 14, 40, 19, label);
        AssertBox(338, 13.5, 20, 20, ring);
    });

    [AvaloniaFact]
    public void V5_End_Puts_The_Ring_First_And_The_Label_At_The_End() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "d", Content = Box(113), LabelPlacement = RadioLabelPlacement.End };
        using var scene = Show(Item(radio));

        // measured d: the label at the end of the line, 14 down; the ring at the start, 13.5 down
        var (label, ring) = Layout(radio);
        AssertBox(358 - 113, 14, 113, 19, label);
        AssertBox(0, 13.5, 20, 20, ring);
    });

    [AvaloniaFact]
    public void V5_Fixed_Makes_The_Label_100_Wide_With_An_Ellipsis() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "e", Content = "A fixed label that is far too long", LabelPlacement = RadioLabelPlacement.Fixed };
        using var scene = Show(Item(radio));

        var (label, ring) = Layout(radio);
        Assert.Equal(100, label.Width, 0.5);
        AssertBox(338, 13.5, 20, 20, ring);
        Assert.Equal(TextTrimming.CharacterEllipsis, radio.GetVisualDescendants().OfType<TextBlock>().Single().TextTrimming);
    });

    [AvaloniaFact]
    public void V5_A_Fixed_Label_Keeps_Its_100_In_A_Narrow_Line() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "e", Content = Box(200), LabelPlacement = RadioLabelPlacement.Fixed, Justify = RadioJustify.Start };
        using var scene = Show(new Border { Width = 90, HorizontalAlignment = HorizontalAlignment.Left, Child = radio });

        // flex: 0 0 100px: the label stays 100, and the ring overflows after it
        var (label, ring) = Layout(radio);
        Assert.Equal(100, label.Width, 0.5);
        Assert.Equal(116, ring.X, 0.5);
    });

    [AvaloniaFact]
    public void V5_A_Long_Label_Shrinks_To_What_Is_Left() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "a", Content = Box(500) };
        using var scene = Show(Item(radio));

        var (label, ring) = Layout(radio);
        Assert.Equal(358 - 20 - 16, label.Width, 0.5);
        AssertBox(338, 13.5, 20, 20, ring);
    });

    [AvaloniaFact]
    public void V5_Stacked_Puts_The_Label_Above_At_0_75_From_Its_Centre() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "f", Content = Box(57), LabelPlacement = RadioLabelPlacement.Stacked };
        using var scene = Show(Item(radio));

        // measured f: 75 high; the label centred, 12.38 down, 0.75 of 19 high as drawn; ring 169, 45
        Assert.Equal(75, radio.Bounds.Height, 0.5);
        var (label, ring) = Layout(radio);
        AssertBox((358 - 42.75) / 2, 12.38, 42.75, 14.25, label);
        AssertBox(169, 45, 20, 20, ring);
    });

    [AvaloniaFact]
    public void V5_Stacked_With_Alignment_Center_Scales_From_The_Top() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "g", Content = Box(118), LabelPlacement = RadioLabelPlacement.Stacked, Alignment = RadioAlignment.Center };
        using var scene = Show(Item(radio));

        // measured g: the label centred, 10 down, 14.25 high; ring 169, 45
        var (label, ring) = Layout(radio);
        AssertBox((358 - 88.5) / 2, 10, 88.5, 14.25, label);
        AssertBox(169, 45, 20, 20, ring);
    });

    [AvaloniaFact]
    public void V5_Stacked_With_Alignment_Start_Is_At_The_Start_From_The_Top_Left() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "n4", Content = Box(94), LabelPlacement = RadioLabelPlacement.Stacked, Alignment = RadioAlignment.Start };
        using var scene = Show(Item(radio));

        // measured n4: label 0, 10, 14.25 high; ring 0, 45
        var (label, ring) = Layout(radio);
        AssertBox(0, 10, 70.5, 14.25, label);
        AssertBox(0, 45, 20, 20, ring);
    });

    [AvaloniaFact]
    public void V5_A_Stacked_Label_Can_Be_A_Third_Wider_Than_The_Line() => InstalledTheme.Run(_ =>
    {
        var radio = new AvonicRadio { Value = "a", Content = Box(1000), LabelPlacement = RadioLabelPlacement.Stacked };
        using var scene = Show(Item(radio));

        // max-width: calc(100% / 0.75), then scale(0.75): as wide as the line, drawn
        var (label, _) = Layout(radio);
        AssertBox(0, 12.38, 358, 14.25, label);
    });

    [AvaloniaFact]
    public void V6_Justify_Start_Packs_Them_At_The_Start_And_End_At_The_End() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicRadio { Value = "h", Content = Box(85), Justify = RadioJustify.Start };
        var end = new AvonicRadio { Value = "i", Content = Box(78), Justify = RadioJustify.End };
        using var scene = Show(new StackPanel { Children = { Item(start), Item(end) } });

        // measured h: the ring 16 after the label; i: the label 16 before the ring, which is at the end
        Assert.Equal(85 + 16, Layout(start).Ring.X, 0.5);
        Assert.Equal(358 - 20 - 16 - 78, Layout(end).Label.X, 0.5);
        Assert.Equal(338, Layout(end).Ring.X, 0.5);
    });

    [AvaloniaFact]
    public void V6_Alone_It_Is_As_Wide_As_Its_Content_And_Justify_Or_Alignment_Make_It_Fill_Its_Line() => InstalledTheme.Run(_ =>
    {
        var plain = Radio("a");
        var justified = Radio("b");
        justified.Justify = RadioJustify.Start;
        var aligned = Radio("c");
        aligned.Alignment = RadioAlignment.Center;
        using var scene = Show(new StackPanel { Children = { plain, justified, aligned } });

        Assert.Equal(76, plain.Bounds.Width, 0.5);
        // cursor: pointer
        Assert.Equal(StandardCursorType.Hand.ToString(), plain.Cursor?.ToString());
        // measured n6: 390 wide, the ring right after the label
        Assert.Equal(390, justified.Bounds.Width, 0.5);
        Assert.Equal(56, Layout(justified).Ring.X, 0.5);
        Assert.Equal(390, aligned.Bounds.Width, 0.5);
    });

    [AvaloniaFact]
    public void V5_Alone_End_Puts_16_Between_The_Ring_And_The_Label_And_Stacked_16_Below_The_Label() => InstalledTheme.Run(_ =>
    {
        var end = Radio("a");
        end.LabelPlacement = RadioLabelPlacement.End;
        var stacked = Radio("b");
        stacked.LabelPlacement = RadioLabelPlacement.Stacked;
        using var scene = Show(new StackPanel { Children = { end, stacked } });

        Assert.Equal(76, end.Bounds.Width, 0.5);
        Assert.Equal(36, Layout(end).Label.X, 0.5);
        Assert.Equal(new Size(40, 19 + 16 + 20), stacked.Bounds.Size);
        Assert.Equal(35, Layout(stacked).Ring.Y, 0.5);
    });

    [AvaloniaFact]
    public void V6_With_No_Label_In_An_Item_The_Ring_Is_At_The_Start_Or_At_The_End_For_End() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicRadio { Value = "n1" };
        var end = new AvonicRadio { Value = "n2", LabelPlacement = RadioLabelPlacement.End };
        using var scene = Show(new StackPanel { Children = { Item(start), Item(end) } });

        // measured n1: ring 0, 13.5; n2: ring 338, 13.5
        AssertBox(0, 13.5, 20, 20, Layout(start).Ring);
        AssertBox(338, 13.5, 20, 20, Layout(end).Ring);
    });

    [AvaloniaFact]
    public void V7_Alignment_Start_In_A_Row_Puts_Both_At_The_Top() => InstalledTheme.Run(_ =>
    {
        var radio = Radio("n3");
        radio.Alignment = RadioAlignment.Start;
        using var scene = Show(Item(radio));

        // measured n3: label 0, 10; ring 338, 0
        var (label, ring) = Layout(radio);
        Assert.Equal(10, label.Y, 0.5);
        AssertBox(338, 0, 20, 20, ring);
    });

    [AvaloniaFact]
    public void V9_RTL_Mirrors_It() => InstalledTheme.Run(_ =>
    {
        var start = Radio("r");
        var end = new AvonicRadio { Value = "s", Content = Box(105), LabelPlacement = RadioLabelPlacement.End };
        var list = new StackPanel { FlowDirection = FlowDirection.RightToLeft, Children = { Item(start), Item(end) } };
        using var scene = Show(list);

        // measured r: ring at 16, label ending at 374; s: label at 16, ring at 354
        Assert.Equal(16, Bounds(Part<Border>(start, "PART_Container"), scene.Window).X, 0.5);
        Assert.Equal(374, Bounds(Part<ContentPresenter>(start, "PART_Label"), scene.Window).Right, 0.5);
        Assert.Equal(16, Bounds(Part<ContentPresenter>(end, "PART_Label"), scene.Window).X, 0.5);
        Assert.Equal(354, Bounds(Part<Border>(end, "PART_Container"), scene.Window).X, 0.5);
    });

    // ── G1–G2: clicks and the value ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G1_A_Click_Sets_The_Value_And_Raises_IonChange_Once()
    {
        var a = Radio("a");
        var b = Radio("b");
        var group = Group(a, b);
        group.Value = "b";
        var changes = Changes(group);
        using var scene = Show(group);

        Click(scene, a);
        Assert.Equal("a", group.Value);
        Assert.Equal(["a"], changes);
        Assert.True(Checked(a));
        Assert.False(Checked(b));
    }

    [AvaloniaFact]
    public void G1_Clicking_The_Checked_Radio_Does_Nothing()
    {
        var a = Radio("a");
        var group = Group(a);
        group.Value = "a";
        var changes = Changes(group);
        using var scene = Show(group);

        Click(scene, a);
        Assert.Equal("a", group.Value);
        Assert.Empty(changes);
        Assert.True(Checked(a));
    }

    [AvaloniaFact]
    public void G1_With_AllowEmptySelection_Clicking_The_Checked_Radio_Clears_The_Value()
    {
        var a = Radio("a");
        var group = Group(a);
        group.AllowEmptySelection = true;
        group.Value = "a";
        var changes = Changes(group);
        using var scene = Show(group);

        Click(scene, a);
        Assert.Null(group.Value);
        Assert.Equal([null], changes);
        Assert.False(Checked(a));
    }

    private sealed class Choice : System.ComponentModel.INotifyPropertyChanged
    {
        private object? _value;

        public object? Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    [AvaloniaFact]
    public void G1_A_Bound_Value_Hears_The_Users_Choice()
    {
        var model = new Choice { Value = "a" };
        var b = Radio("b");
        var group = Group(Radio("a"), b);
        group.Bind(AvonicRadioGroup.ValueProperty, new Avalonia.Data.Binding(nameof(Choice.Value)) { Source = model });
        using var scene = Show(group);

        Click(scene, b);
        Assert.Equal("b", model.Value);

        model.Value = "a";
        Assert.False(Checked(b));
    }

    [AvaloniaFact]
    public void G1_IonChange_Bubbles_From_The_Group()
    {
        var a = Radio("a");
        var group = Group(a);
        var outer = new StackPanel { Children = { group } };
        var heard = new List<object?>();
        outer.AddHandler(AvonicRadioGroup.IonChangeEvent, (_, e) => heard.Add(e.Value));
        using var scene = Show(outer);

        Click(scene, a);
        Assert.Equal(["a"], heard);
    }

    [AvaloniaFact]
    public void G2_Setting_The_Value_In_Code_Raises_No_IonChange()
    {
        var a = Radio("a");
        var group = Group(a, Radio("b"));
        var changes = Changes(group);
        using var scene = Show(group);

        group.Value = "a";
        group.Value = "b";
        Assert.Empty(changes);
    }

    // ── G3–G5: keys ──────────────────────────────────────────────────────────────────────────

    private static (AvonicRadioGroup Group, AvonicRadio[] Radios) Keyed(params AvonicRadio[] radios)
    {
        var group = Group(radios);
        return (group, radios);
    }

    [AvaloniaFact]
    public void G3_Down_And_Right_Move_To_The_Next_Enabled_Radio_And_Check_It()
    {
        var (group, r) = Keyed(Radio("a"), Radio("b"), Radio("c", enabled: false), Radio("d"));
        group.Value = "b";
        var changes = Changes(group);
        using var scene = Show(group);
        r[1].Focus(NavigationMethod.Tab);

        // measured down: Cherry, disabled, skipped
        Press(scene, PhysicalKey.ArrowDown);
        Assert.True(r[3].IsFocused);
        Assert.Equal("d", group.Value);

        // wraps to the first
        Press(scene, PhysicalKey.ArrowRight);
        Assert.True(r[0].IsFocused);
        Assert.Equal("a", group.Value);
        Assert.Equal(["d", "a"], changes);
    }

    [AvaloniaFact]
    public void G3_Up_And_Left_Move_To_The_Previous_Enabled_Radio_And_Check_It()
    {
        var (group, r) = Keyed(Radio("a"), Radio("b", enabled: false), Radio("c"));
        group.Value = "c";
        var changes = Changes(group);
        using var scene = Show(group);
        r[2].Focus(NavigationMethod.Tab);

        Press(scene, PhysicalKey.ArrowUp);
        Assert.True(r[0].IsFocused);
        Assert.Equal("a", group.Value);

        // wraps to the last
        Press(scene, PhysicalKey.ArrowLeft);
        Assert.True(r[2].IsFocused);
        Assert.Equal("c", group.Value);
        Assert.Equal(["a", "c"], changes);
    }

    [AvaloniaFact]
    public void G3_Focus_By_Arrow_Shows_The_Focus_Ring_On_The_Item_Around_It() => InstalledTheme.Run(_ =>
    {
        var a = Radio("a");
        var b = Radio("b");
        var itemB = Item(b);
        var group = Group(Item(a), itemB);
        group.Value = "a";
        using var scene = Show(group);
        a.Focus(NavigationMethod.Tab);

        // measured up: the item has ion-focused
        Press(scene, PhysicalKey.ArrowDown);
        Assert.True(b.IsFocused);
        Assert.Contains(FocusVisible.FocusedPseudoClass, itemB.Classes);
        Assert.False(Part<Border>(b, "PART_Focus").IsEffectivelyVisible);
    });

    [AvaloniaFact]
    public void G3_Focus_By_Arrow_Shows_The_Radios_Own_Circle_Outside_An_Item() => InstalledTheme.Run(_ =>
    {
        var (group, r) = Keyed(Radio("a"), Radio("b"));
        group.Value = "a";
        using var scene = Show(group);
        r[0].Focus(NavigationMethod.Tab);

        Press(scene, PhysicalKey.ArrowDown);
        Assert.True(Part<Border>(r[1], "PART_Focus").IsEffectivelyVisible);
        Assert.False(Part<Border>(r[0], "PART_Focus").IsEffectivelyVisible);
    });

    [AvaloniaFact]
    public void G4_Space_Checks_The_Focused_Radio_And_Raises_IonChange_Only_If_It_Changed()
    {
        var (group, r) = Keyed(Radio("a"), Radio("b"));
        group.Value = "a";
        var changes = Changes(group);
        using var scene = Show(group);
        r[0].Focus(NavigationMethod.Tab);

        // measured space on the checked one: nothing
        Press(scene, PhysicalKey.Space);
        Assert.Equal("a", group.Value);
        Assert.Empty(changes);

        r[1].Focus(NavigationMethod.Tab);
        Press(scene, PhysicalKey.Space);
        Assert.Equal("b", group.Value);
        Assert.Equal(["b"], changes);
    }

    [AvaloniaFact]
    public void G4_With_AllowEmptySelection_Space_Clears_A_Value_Whichever_Radio_Is_Focused()
    {
        var (group, r) = Keyed(Radio("a"), Radio("b"));
        group.AllowEmptySelection = true;
        group.Value = "a";
        var changes = Changes(group);
        using var scene = Show(group);
        r[1].Focus(NavigationMethod.Tab);

        Press(scene, PhysicalKey.Space);
        Assert.Null(group.Value);

        Press(scene, PhysicalKey.Space);
        Assert.Equal("b", group.Value);
        Assert.Equal([null, "b"], changes);
    }

    [AvaloniaFact]
    public void G4_Enter_Does_Nothing()
    {
        var (group, r) = Keyed(Radio("a"), Radio("b"));
        group.Value = "a";
        var changes = Changes(group);
        using var scene = Show(group);
        r[1].Focus(NavigationMethod.Tab);

        // measured enter: nothing
        Press(scene, PhysicalKey.Enter);
        Assert.Equal("a", group.Value);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void G5_Only_A_Key_On_One_Of_Its_Radios_Counts()
    {
        var button = new Button { Content = "Inside" };
        var (group, _) = Keyed(Radio("a"), Radio("b"));
        group.Items.Add(button);
        group.Value = "a";
        var changes = Changes(group);
        using var scene = Show(group);
        button.Focus(NavigationMethod.Tab);

        // An ItemsControl would move focus between its containers: Ionic's group doesn't.
        Press(scene, PhysicalKey.ArrowDown);
        Assert.True(button.IsFocused);
        Press(scene, PhysicalKey.Space);
        Assert.Equal("a", group.Value);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void G5_A_Radio_Alone_Takes_No_Keys()
    {
        var radio = Radio("a");
        using var scene = Show(radio);
        radio.Focus(NavigationMethod.Tab);

        Press(scene, PhysicalKey.Space);
        Assert.False(Checked(radio));
    }

    // ── G6: helper and error text ────────────────────────────────────────────────────────────

    private static TextBlock Text(AvonicRadioGroup group, string name) => Part<TextBlock>(group, name);

    [AvaloniaFact]
    public void G6_Helper_Text_At_The_Top_On_A_24_Line_In_The_Step_300_Text_Colour() => InstalledTheme.Run(_ =>
    {
        var group = Group(Item(Radio("a")));
        group.HelperText = "Helper text";
        group.ErrorText = "Error text";
        using var scene = Show(new AvonicList { Items = { group } });

        var helper = Text(group, "PART_HelperText");
        Assert.True(helper.IsEffectivelyVisible);
        Assert.False(Text(group, "PART_ErrorText").IsEffectivelyVisible);

        // measured helper: 16, 8 (in the list's padding), 358 by 24, rgb(77, 77, 77)
        AssertBox(16, 8, 358, 24, Bounds(helper, scene.Window));
        Assert.Equal(Color.Parse("#4D4D4D"), Rgba(helper.Foreground));
    });

    [AvaloniaFact]
    public void G6_Error_Text_Instead_When_Touched_And_Invalid() => InstalledTheme.Run(_ =>
    {
        var group = Group(Item(Radio("a")));
        group.HelperText = "Helper text";
        group.ErrorText = "Error text";
        using var scene = Show(group);

        group.Classes.Add("ion-invalid");
        Dispatcher.UIThread.RunJobs();
        Assert.True(Text(group, "PART_HelperText").IsEffectivelyVisible);

        group.Classes.Add("ion-touched");
        Dispatcher.UIThread.RunJobs();
        var error = Text(group, "PART_ErrorText");
        Assert.False(Text(group, "PART_HelperText").IsEffectivelyVisible);
        Assert.True(error.IsEffectivelyVisible);
        Assert.Equal(Danger, Rgba(error.Foreground));

        // Outside a list, at the edge
        Assert.Equal(0, Bounds(error, scene.Window).X, 0.5);
    });

    [AvaloniaFact]
    public void G6_No_Text_No_Top() => InstalledTheme.Run(_ =>
    {
        var group = Group(Item(Radio("a")));
        group.ErrorText = "Error text";
        using var scene = Show(group);

        // Error text alone, not invalid: the top is there, with nothing in it.
        Assert.Equal(0, Part<StackPanel>(group, "PART_Top").Bounds.Height);

        group.ErrorText = null;
        Dispatcher.UIThread.RunJobs();
        Assert.False(Part<StackPanel>(group, "PART_Top").IsEffectivelyVisible);
    });

    // ── G7–G8: automation ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G7_A_Group_Described_By_Its_Hint_And_Labelled_By_Its_Header()
    {
        var label = new AvonicLabel { Content = "Fruit" };
        var group = Group(new AvonicListHeader { Content = label }, Item(Radio("a")));
        group.HelperText = "Helper text";
        group.ErrorText = "Error text";
        using var scene = Show(group);

        var peer = ControlAutomationPeer.CreatePeerForElement(group);
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Equal("Helper text", peer.GetHelpText());
        Assert.Same(ControlAutomationPeer.CreatePeerForElement(label), peer.GetLabeledBy());

        group.Classes.Add("ion-touched");
        group.Classes.Add("ion-invalid");
        Assert.Equal("Error text", peer.GetHelpText());
    }

    [AvaloniaFact]
    public void G7_A_Divider_Labels_It_When_There_Is_No_Header()
    {
        var label = new AvonicLabel { Content = "Fruit" };
        var group = Group(new AvonicItemDivider { Content = label }, Item(Radio("a")));
        using var scene = Show(group);

        Assert.Same(ControlAutomationPeer.CreatePeerForElement(label), ControlAutomationPeer.CreatePeerForElement(group).GetLabeledBy());
    }

    [AvaloniaFact]
    public void G8_An_Item_In_A_Group_Is_Not_A_List_Item_Even_In_A_List() => InstalledTheme.Run(_ =>
    {
        var inGroup = Item(Radio("a"));
        var button = new AvonicItem { Button = true, Content = "Button" };
        var outside = new AvonicItem { Content = "Outside" };
        using var scene = Show(new AvonicList { Items = { Group(inGroup, button), outside } });

        Assert.Equal(AutomationControlType.Group, ControlAutomationPeer.CreatePeerForElement(inGroup).GetAutomationControlType());
        Assert.Equal(AutomationControlType.Button, ControlAutomationPeer.CreatePeerForElement(button).GetAutomationControlType());
        Assert.Equal(AutomationControlType.ListItem, ControlAutomationPeer.CreatePeerForElement(outside).GetAutomationControlType());
    });

    // ── The list's padding ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void G6_In_A_List_The_Top_Has_16_At_Each_Side() => InstalledTheme.Run(_ =>
    {
        var inList = Group(Item(Radio("a")));
        inList.HelperText = "In a list";
        var alone = Group(Item(Radio("b")));
        alone.HelperText = "Alone";
        using var scene = Show(new StackPanel { Children = { new AvonicList { Items = { inList } }, alone } });

        Assert.Equal(16, Bounds(Text(inList, "PART_HelperText"), inList).X, 0.5);
        Assert.Equal(0, Bounds(Text(alone, "PART_HelperText"), alone).X, 0.5);
    });
}
