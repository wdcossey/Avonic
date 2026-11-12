using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Animation;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-checkbox in Chromium, md, at 390 px wide, and the tests are named after the behaviours in
/// docs/components/ion-checkbox/PLAN.md. Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub.
/// </summary>
public class AvonicCheckboxTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Danger = Color.Parse("#C5000F");
    private static readonly Color Step300 = Color.Parse("#4D4D4D");

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static Border Box(AvonicCheckbox box) => Part<Border>(box, "PART_Container");
    private static Control Label(AvonicCheckbox box) => Part<Control>(box, "PART_Label");
    private static CheckboxMark Mark(AvonicCheckbox box) => Part<CheckboxMark>(box, "PART_Mark");
    private static TextBlock Helper(AvonicCheckbox box) => Part<TextBlock>(box, "PART_HelperText");
    private static TextBlock Error(AvonicCheckbox box) => Part<TextBlock>(box, "PART_ErrorText");

    private static Rect Bounds(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void AssertBox(double x, double y, double width, double height, Rect actual)
    {
        // Layout rounds to half pixels at a scale of 2, and Chromium keeps fractions.
        Assert.InRange(actual.X, x - 0.5, x + 0.5);
        Assert.InRange(actual.Y, y - 0.5, y + 0.5);
        Assert.InRange(actual.Width, width - 0.5, width + 0.5);
        Assert.InRange(actual.Height, height - 0.5, height + 0.5);
    }

    private static Color Solid(IBrush? brush)
    {
        var solid = Assert.IsAssignableFrom<ISolidColorBrush>(brush);
        return Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B);
    }

    private static void AssertClose(Color expected, Color actual, int tolerance = 2)
    {
        Assert.InRange(actual.A, expected.A - tolerance, expected.A + tolerance);
        Assert.InRange(actual.R, expected.R - tolerance, expected.R + tolerance);
        Assert.InRange(actual.G, expected.G - tolerance, expected.G + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
    }

    /// <summary>Controls in a column on white, at a scale of 2, as the reference is.</summary>
    private static Scene Themed(params Control[] controls)
    {
        var holder = new StackPanel();
        holder.Children.AddRange(controls);
        var scene = new Scene(holder);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        return scene;
    }

    private static Scene Plain(params Control[] controls)
    {
        var holder = new StackPanel();
        holder.Children.AddRange(controls);
        return new Scene(holder);
    }

    private static Point Centre(Visual visual, Scene scene) => Bounds(visual, scene.Window).Center;

    private static void Click(Scene scene, Point point)
    {
        scene.Window.MouseMove(point, RawInputModifiers.None);
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Key(Scene scene, PhysicalKey key, bool release = true)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        if (release)
        {
            scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        }

        Dispatcher.UIThread.RunJobs();
    }

    private static List<CheckboxChangeEventArgs> Changes(AvonicCheckbox box)
    {
        var changes = new List<CheckboxChangeEventArgs>();
        box.IonChange += (_, e) => changes.Add(e);
        return changes;
    }

    private static Color Pixel(Scene scene, Visual visual, double x, double y)
    {
        Dispatcher.UIThread.RunJobs();
        var at = visual.TranslatePoint(new Point(x, y), scene.Window)!.Value * 2;
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, (int)at.X, (int)at.Y);
    }

    // ── B1: a click anywhere toggles once ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_A_Click_On_The_Box_Toggles_And_Raises_IonChange() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Themed(box);
        var changes = Changes(box);

        Click(scene, Centre(Box(box), scene));
        Assert.True(box.Checked);
        var change = Assert.Single(changes);
        Assert.True(change.Checked);
        Assert.Equal("on", change.Value);
        Assert.Same(box, change.Source);

        Click(scene, Centre(Box(box), scene));
        Assert.False(box.Checked);
        Assert.Equal(2, changes.Count);
        Assert.False(changes[1].Checked);
    });

    [AvaloniaFact]
    public void B1_A_Click_On_The_Label_Toggles_Once()
    {
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Plain(box);
        var changes = Changes(box);

        Click(scene, Centre(Label(box), scene));

        Assert.True(box.Checked);
        Assert.Single(changes);
    }

    [AvaloniaFact]
    public void B1_A_Click_In_The_Gap_Of_A_Space_Between_Row_Toggles() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Space between", Justify = CheckboxJustify.SpaceBetween };
        using var scene = Themed(box);
        var changes = Changes(box);

        // Between the label's end and the box, measured c7: a click at x 250 toggles it.
        Click(scene, new Point(250, Centre(box, scene).Y));

        Assert.True(box.Checked);
        Assert.Single(changes);
    });

    [AvaloniaFact]
    public void B1_IonChange_Carries_The_Value()
    {
        var box = new AvonicCheckbox { Content = "Label", Value = 42 };
        using var scene = Plain(box);
        var changes = Changes(box);

        Click(scene, Centre(box, scene));

        Assert.Equal(42, Assert.Single(changes).Value);
    }

    // ── B2: indeterminate ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_A_Click_On_An_Indeterminate_Checkbox_Checks_It_And_Clears_Indeterminate_After_IonChange()
    {
        var box = new AvonicCheckbox { Content = "Label", Indeterminate = true };
        using var scene = Plain(box);
        bool? indeterminateDuring = null;
        box.IonChange += (_, _) => indeterminateDuring = box.Indeterminate;

        Click(scene, Centre(box, scene));

        Assert.True(box.Checked);
        Assert.False(box.Indeterminate);
        Assert.True(indeterminateDuring);
    }

    // ── B3: code ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Setting_Checked_In_Code_Raises_No_IonChange()
    {
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Plain(box);
        var changes = Changes(box);

        box.Checked = true;
        box.Checked = false;

        Assert.Empty(changes);
    }

    private sealed class Model : INotifyPropertyChanged
    {
        private bool _accepted;

        public bool Accepted
        {
            get => _accepted;
            set
            {
                _accepted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Accepted)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    [AvaloniaFact]
    public void B3_A_Click_Writes_Back_Through_A_Binding_And_Keeps_It()
    {
        var model = new Model();
        var box = new AvonicCheckbox { Content = "Label" };
        box.Bind(AvonicCheckbox.CheckedProperty, new Binding(nameof(Model.Accepted)) { Source = model });
        using var scene = Plain(box);

        Click(scene, Centre(box, scene));
        Assert.True(model.Accepted);

        model.Accepted = false;
        Assert.False(box.Checked);
    }

    [AvaloniaFact]
    public void B3_A_Click_Keeps_A_One_Way_Binding()
    {
        var model = new Model();
        var box = new AvonicCheckbox { Content = "Label" };
        box.Bind(AvonicCheckbox.CheckedProperty, new Binding(nameof(Model.Accepted)) { Source = model, Mode = BindingMode.OneWay });
        using var scene = Plain(box);

        Click(scene, Centre(box, scene));
        Assert.True(box.Checked);
        Assert.False(model.Accepted);

        // The binding is still there, as a click sets the current value and not a local one.
        model.Accepted = true;
        model.Accepted = false;
        Assert.False(box.Checked);
    }

    // ── B4: keys ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Space_Toggles_On_Key_Down_And_Enter_Does_Nothing()
    {
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Plain(box);
        var changes = Changes(box);
        box.Focus(NavigationMethod.Tab);

        Key(scene, PhysicalKey.Enter);
        Assert.False(box.Checked);

        // On key down, before any release.
        Key(scene, PhysicalKey.Space, release: false);
        Assert.True(box.Checked);

        // A repeat is another key down, and toggles again.
        Key(scene, PhysicalKey.Space, release: false);
        Assert.False(box.Checked);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.False(box.Checked);
        Assert.Equal(2, changes.Count);
    }

    // ── B5: focus ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_A_Click_Focuses_It_And_Raises_IonFocus_Then_IonBlur()
    {
        var box = new AvonicCheckbox { Content = "Label" };
        var other = new Button { Content = "Other" };
        using var scene = Plain(box, other);
        var events = new List<string>();
        box.IonFocus += (_, _) => events.Add("focus");
        box.IonBlur += (_, _) => events.Add("blur");

        Assert.True(box.Focusable);
        Click(scene, Centre(box, scene));
        Assert.True(box.IsFocused);

        other.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["focus", "blur"], events);
    }

    // ── B6: disabled ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Disabled_Takes_No_Click_No_Space_And_No_Focus()
    {
        var box = new AvonicCheckbox { Content = "Label", IsEnabled = false };
        using var scene = Plain(box);
        var changes = Changes(box);

        Click(scene, Centre(box, scene));
        Assert.False(box.Focus(NavigationMethod.Tab));
        scene.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Space, Source = box });
        Dispatcher.UIThread.RunJobs();

        Assert.False(box.Checked);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void B6_Disabled_Fades_The_Label_And_The_Box() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Label", IsEnabled = false };
        using var scene = Themed(box);

        Assert.Equal(0.38, Label(box).Opacity);
        Assert.Equal(0.63, Part<Control>(box, "PART_NativeWrapper").Opacity);
    });

    // ── B7 to B12: layout ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_Label_Start_Is_The_Label_Then_16_Then_The_Box_Centred_On_It() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Label start" };
        using var scene = Themed(box);

        // Measured c1: 109.89 by 19, the box at 91.89, 0.5 down.
        AssertBox(0, 0, 109.89, 19, Bounds(box, scene.Window));
        AssertBox(91.89, 0.5, 18, 18, Bounds(Box(box), scene.Window));
        AssertBox(0, 0, 75.89, 19, Bounds(Label(box), scene.Window));
    });

    [AvaloniaFact]
    public void B7_In_RTL_The_Label_Is_On_The_Right() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Label start", FlowDirection = FlowDirection.RightToLeft, Justify = CheckboxJustify.Start };
        using var scene = Themed(box);

        Assert.Equal(390 - 75.89, Bounds(Label(box), scene.Window).X, 0.6);
        Assert.Equal(390 - 109.89, Bounds(Box(box), scene.Window).X, 0.6);
    });

    [AvaloniaFact]
    public void B8_Label_End_Is_The_Box_Then_16_Then_The_Label() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Label end", LabelPlacement = CheckboxLabelPlacement.End };
        using var scene = Themed(box);

        // Measured c4: 103 wide, the box at 0, the label at 34.
        AssertBox(0, 0, 103, 19, Bounds(box, scene.Window));
        AssertBox(0, 0.5, 18, 18, Bounds(Box(box), scene.Window));
        Assert.Equal(34, Bounds(Label(box), scene.Window).X, 0.5);
    });

    [AvaloniaFact]
    public void B8_Label_End_Stays_Packed_At_The_Start_Of_A_Full_Width_Host() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Label end", LabelPlacement = CheckboxLabelPlacement.End, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        using var scene = Themed(box);

        // justify-content: start, so not space-between, even with room.
        Assert.Equal(390, box.Bounds.Width);
        Assert.Equal(34, Bounds(Label(box), scene.Window).X, 0.5);
    });

    [AvaloniaFact]
    public void B9_Label_Fixed_Is_100_Wide_And_Ends_In_An_Ellipsis() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Fixed label that is long", LabelPlacement = CheckboxLabelPlacement.Fixed };
        using var scene = Themed(box);

        // Measured c5: 134 wide, the box at 116.
        AssertBox(0, 0, 134, 19, Bounds(box, scene.Window));
        AssertBox(116, 0.5, 18, 18, Bounds(Box(box), scene.Window));
        var text = box.GetVisualDescendants().OfType<TextBlock>().First(block => block.Text == "Fixed label that is long");
        Assert.Equal(TextTrimming.CharacterEllipsis, text.TextTrimming);
        Assert.Equal(TextWrapping.NoWrap, text.TextWrapping);
    });

    [AvaloniaFact]
    public void B10_Label_Stacked_Is_Above_At_Three_Quarters_And_Centred() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Stacked", LabelPlacement = CheckboxLabelPlacement.Stacked };
        using var scene = Themed(box);

        // Measured c6: 57.28 by 53, the label 42.96 by 14.25 at 7.16, 2.38, the box at 19.64, 35.
        AssertBox(0, 0, 57.28, 53, Bounds(box, scene.Window));
        AssertBox(7.16, 2.38, 42.96, 14.25, Bounds(Label(box), scene.Window));
        AssertBox(19.64, 35, 18, 18, Bounds(Box(box), scene.Window));
    });

    [AvaloniaFact]
    public void B10_A_Stacked_Label_Can_Be_Wider_Than_The_Host_By_The_Scale() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox
        {
            Content = "A stacked label much longer than its host",
            LabelPlacement = CheckboxLabelPlacement.Stacked,
            Width = 150,
        };
        using var scene = Themed(box);

        // max-width: calc(100% / 0.75): 200 wide before the scale, so 150 after it.
        Assert.Equal(200, Label(box).Bounds.Width, 0.5);
        var text = box.GetVisualDescendants().OfType<TextBlock>().First(block => block.Text == "A stacked label much longer than its host");
        Assert.InRange(text.TextLayout.Width, 150.5, 200.5);
        AssertBox(0, 2.38, 150, 14.25, Bounds(Label(box), scene.Window));
    });

    [AvaloniaFact]
    public void B11_Justify_Makes_It_A_Block_And_Space_Between_Puts_The_Box_At_The_End() => InstalledTheme.Run(_ =>
    {
        var spaced = new AvonicCheckbox { Content = "Space between", Justify = CheckboxJustify.SpaceBetween };
        var start = new AvonicCheckbox { Content = "Start", Justify = CheckboxJustify.Start };
        var end = new AvonicCheckbox { Content = "End", Justify = CheckboxJustify.End };
        var aligned = new AvonicCheckbox { Content = "Aligned", Alignment = CheckboxAlignment.Center };
        using var scene = Themed(spaced, start, end, aligned);

        // Measured c7: 390 wide, the box at 372.
        Assert.Equal(390, spaced.Bounds.Width);
        Assert.Equal(372, Bounds(Box(spaced), scene.Window).X, 0.5);
        Assert.Equal(Label(start).Bounds.Width + 16, Bounds(Box(start), scene.Window).X, 0.5);
        Assert.Equal(372, Bounds(Box(end), scene.Window).X, 0.5);
        Assert.Equal(390 - 18 - 16 - Label(end).Bounds.Width, Bounds(Label(end), scene.Window).X, 0.5);
        Assert.Equal(390, aligned.Bounds.Width);
    });

    [AvaloniaFact]
    public void B12_Alignment_Start_Puts_Them_At_The_Top_And_A_Stacked_Label_At_The_Left() => InstalledTheme.Run(_ =>
    {
        var row = new AvonicCheckbox { Content = "Row", Alignment = CheckboxAlignment.Start, HelperText = "Helper" };
        var stacked = new AvonicCheckbox { Content = "Stacked", LabelPlacement = CheckboxLabelPlacement.Stacked, Alignment = CheckboxAlignment.Start };
        using var scene = Themed(row, stacked);

        // A label two lines high: centred, the box would be 9.5 down; at the start it's at the top.
        Assert.Equal(0, Box(row).TranslatePoint(default, row)!.Value.Y, 0.5);
        var label = Bounds(Label(stacked), stacked);
        Assert.Equal(0, label.X, 0.5);
        Assert.Equal(0, label.Y, 0.5);
        Assert.Equal(0, Box(stacked).TranslatePoint(default, stacked)!.Value.X, 0.5);
    });

    // ── B13 to B17: the box and its mark ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_Unchecked_Is_The_Item_Background_With_A_Grey_Border_And_Checked_Is_Primary() => InstalledTheme.Run(_ =>
    {
        var off = new AvonicCheckbox { Content = "Off" };
        var on = new AvonicCheckbox { Content = "On", Checked = true };
        var mixed = new AvonicCheckbox { Content = "Mixed", Indeterminate = true };
        using var scene = Themed(off, on, mixed);

        AssertClose(Colors.White, Solid(Box(off).Background));
        AssertClose(Color.FromArgb(153, 0, 0, 0), Solid(Box(off).BorderBrush));
        Assert.Equal(new Thickness(2), Box(off).BorderThickness);
        Assert.Equal(new CornerRadius(2.25), Box(off).CornerRadius);
        foreach (var box in new[] { on, mixed })
        {
            AssertClose(Primary, Solid(Box(box).Background));
            AssertClose(Primary, Solid(Box(box).BorderBrush));
            AssertClose(Colors.White, Solid(Mark(box).Stroke));
        }
    });

    [AvaloniaFact]
    public void B13_The_Keys_Restyle_The_Box() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Keys" };
        box.Resources["Avonic.Checkbox.Size"] = 24d;
        box.Resources["Avonic.Checkbox.BorderWidth"] = new Thickness(1);
        box.Resources["Avonic.Checkbox.BorderColor"] = Brushes.Green;
        box.Resources["Avonic.Checkbox.CheckboxBackground"] = Brushes.Yellow;
        box.Resources["Avonic.Checkbox.CheckmarkWidth"] = 2d;
        using var scene = Themed(box);

        Assert.Equal(new Size(24, 24), Box(box).Bounds.Size);
        Assert.Equal(new CornerRadius(3), Box(box).CornerRadius);
        Assert.Equal(new Thickness(1), Box(box).BorderThickness);
        Assert.Equal(Brushes.Green, Box(box).BorderBrush);
        Assert.Equal(Brushes.Yellow, Box(box).Background);
        Assert.Equal(2, Mark(box).StrokeThickness);

        // Transitions run on real time.
        Box(box).Transitions = null;
        box.Resources["Avonic.Checkbox.BorderRadius"] = 6d;
        box.Checked = true;
        box.Resources["Avonic.Checkbox.CheckboxBackgroundChecked"] = Brushes.Red;
        box.Resources["Avonic.Checkbox.BorderColorChecked"] = Brushes.Blue;
        box.Resources["Avonic.Checkbox.CheckmarkColor"] = Brushes.Black;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new CornerRadius(6), Box(box).CornerRadius);
        Assert.Equal(Brushes.Red, Box(box).Background);
        Assert.Equal(Brushes.Blue, Box(box).BorderBrush);
        Assert.Equal(Brushes.Black, Mark(box).Stroke);
    });

    [AvaloniaFact]
    public void B14_The_Mark_Is_Mds_Tick_Or_Bar_In_The_Box() => InstalledTheme.Run(_ =>
    {
        var tick = new AvonicCheckbox { Checked = true };
        var bar = new AvonicCheckbox { Indeterminate = true };
        using var scene = Themed(tick, bar);
        var tickBox = Box(tick);
        var barBox = Box(bar);

        // The view box fills the 14 by 14 inside the border: a unit is 14 / 24. The tick's long stroke passes through
        // (15.45, 11.94), and the bar is at y 12; (12, 20) is on neither.
        static Point In(double x, double y) => new(2 + x * 14 / 24, 2 + y * 14 / 24);
        AssertClose(Colors.White, Pixel(scene, tickBox, In(15.45, 11.94).X, In(15.45, 11.94).Y), 40);
        AssertClose(Colors.White, Pixel(scene, tickBox, In(5, 16.2).X, In(5, 16.2).Y), 40);
        AssertClose(Primary, Pixel(scene, tickBox, In(12, 20).X, In(12, 20).Y), 10);

        // 3 units wide is 1.75: 1.2 off the line's centre is outside it.
        var off = In(15.45, 11.94) + new Vector(1.2 * Math.Sqrt(0.5), 1.2 * Math.Sqrt(0.5));
        AssertClose(Primary, Pixel(scene, tickBox, off.X, off.Y), 30);

        AssertClose(Colors.White, Pixel(scene, barBox, In(12, 12).X, In(12, 12).Y), 40);
        AssertClose(Colors.White, Pixel(scene, barBox, In(4, 12).X, In(4, 12).Y), 40);
        AssertClose(Primary, Pixel(scene, barBox, In(15.45, 11.94).X, In(15.45, 11.94).Y - 3), 10);
        AssertClose(Primary, Pixel(scene, barBox, In(5, 16.2).X, In(5, 16.2).Y), 10);
    });

    [AvaloniaFact]
    public void B14_Unchecked_Draws_No_Mark() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox();
        box.Resources["Avonic.Checkbox.CheckmarkColor"] = Brushes.Red;
        using var scene = Themed(box);

        AssertClose(Colors.White, Pixel(scene, Box(box), 2 + 15.45 * 14 / 24, 2 + 11.94 * 14 / 24), 2);
    });

    [AvaloniaFact]
    public void B15_Checking_Draws_The_Mark_After_90ms_Over_90ms_And_Unchecking_Removes_It_At_Once()
    {
        using var clock = new TestClock();
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Plain(box);
        var mark = Mark(box);
        clock.At(16);

        Click(scene, Centre(box, scene));
        var start = clock.Now.TotalMilliseconds;
        Assert.Equal(30, mark.DashOffset);
        clock.At(start + 80);
        Assert.Equal(30, mark.DashOffset);
        clock.At(start + 135);
        Assert.InRange(mark.DashOffset, 10, 20);
        clock.At(start + 200);
        Assert.Equal(0, mark.DashOffset);

        Click(scene, Centre(box, scene));
        Assert.Equal(30, mark.DashOffset);
    }

    [AvaloniaFact]
    public void B15_Checked_From_The_Start_Is_Drawn_At_Once_And_Code_Animates_Later()
    {
        using var clock = new TestClock();
        var first = new AvonicCheckbox { Content = "First", Checked = true };
        var later = new AvonicCheckbox { Content = "Later" };
        using var scene = Plain(first, later);

        Assert.Equal(0, Mark(first).DashOffset);

        clock.At(16);
        later.Checked = true;
        Assert.Equal(30, Mark(later).DashOffset);
        clock.At(16 + 200);
        Assert.Equal(0, Mark(later).DashOffset);
    }

    [AvaloniaFact]
    public void B15_From_Indeterminate_To_Checked_The_Mark_Stays_Drawn()
    {
        using var clock = new TestClock();
        var box = new AvonicCheckbox { Content = "Label", Indeterminate = true };
        using var scene = Plain(box);
        clock.At(16);

        Click(scene, Centre(box, scene));

        Assert.True(box.Checked);
        Assert.Equal(0, Mark(box).DashOffset);
    }

    [AvaloniaFact]
    public void B16_The_Background_Eases_From_The_First_Frame_And_The_Border_Does_Not() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var box = new AvonicCheckbox { Content = "Label", Checked = true };
        using var scene = Themed(box);

        // Not before the first frame, so checked at first is drawn checked.
        Assert.Null(Box(box).Transitions);
        AssertClose(Primary, Solid(Box(box).Background));

        clock.At(16);
        Dispatcher.UIThread.RunJobs();
        var transition = Assert.IsType<BrushTransition>(Assert.Single(Box(box).Transitions!));
        Assert.Equal(Border.BackgroundProperty, transition.Property);
        Assert.Equal(TimeSpan.FromMilliseconds(180), transition.Duration);
        var easing = Assert.IsType<CubicBezierEasing>(transition.Easing);
        Assert.Equal((0.4, 0, 0.2, 1), (easing.X1, easing.Y1, easing.X2, easing.Y2));

        // A change now starts at the old colour.
        box.Checked = false;
        Dispatcher.UIThread.RunJobs();
        AssertClose(Color.FromArgb(153, 0, 0, 0), Solid(Box(box).BorderBrush));
        Assert.NotEqual(Colors.White, Solid(Box(box).Background));
    });

    [AvaloniaFact]
    public void B17_A_Colour_Colours_The_Checked_Box_And_An_Apps_Key_Beats_It() => InstalledTheme.Run(_ =>
    {
        var danger = new AvonicCheckbox { Content = "Danger", Color = "danger", Checked = true };
        var keyed = new AvonicCheckbox { Content = "Keyed", Color = "danger", Checked = true };
        keyed.Resources["Avonic.Checkbox.CheckboxBackgroundChecked"] = Brushes.Green;
        var unknown = new AvonicCheckbox { Content = "Unknown", Color = "nope", Checked = true };
        var warning = new AvonicCheckbox { Content = "Warning", Color = "warning", Checked = true };
        using var scene = Themed(danger, keyed, unknown, warning);

        AssertClose(Danger, Solid(Box(danger).Background));
        AssertClose(Danger, Solid(Box(danger).BorderBrush));
        AssertClose(Colors.White, Solid(Mark(danger).Stroke));
        Assert.Equal(Brushes.Green, Box(keyed).Background);

        // The mark is the colour's contrast, which for warning is black.
        AssertClose(Colors.Black, Solid(Mark(warning).Stroke));

        // var(ion-color-base) is invalid: no background, and no stroke.
        Assert.Null(Box(unknown).Background);
        Assert.Null(Mark(unknown).Stroke);
    });

    // ── B18: no label ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B18_No_Text_Hides_The_Label_And_The_Host_Is_The_Box() => InstalledTheme.Run(_ =>
    {
        var none = new AvonicCheckbox { HelperText = "Helper" };
        var iconOnly = new AvonicCheckbox { Content = new AvonicIcon { IconName = "heart" } };
        var text = new AvonicCheckbox { Content = new StackPanel { Children = { new TextBlock { Text = "Nested" } } } };
        using var scene = Themed(none, iconOnly, text);

        // Measured c11: 18 by 18, and the helper goes with the label.
        Assert.Equal(new Size(18, 18), none.Bounds.Size);
        Assert.False(Label(iconOnly).IsVisible);
        Assert.True(Label(text).IsVisible);

        none.Content = "Now";
        Dispatcher.UIThread.RunJobs();
        Assert.True(Label(none).IsVisible);
    });

    // ── B19, B20: helper and error ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_Helper_Text_Is_Under_The_Label_And_Error_Text_Replaces_It_While_Touched_And_Invalid() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "With helper", HelperText = "Helper text", ErrorText = "Error text" };
        using var scene = Themed(box);

        // Measured c9: 37 high, the helper 12 px in rgb(77, 77, 77), 4 below the label.
        Assert.Equal(37, box.Bounds.Height, 0.6);
        Assert.True(Helper(box).IsVisible);
        Assert.Equal("Helper text", Helper(box).Text);
        Assert.Equal(12, Helper(box).FontSize);
        AssertClose(Step300, Solid(Helper(box).Foreground));
        Assert.Equal(23, Helper(box).TranslatePoint(default, box)!.Value.Y, 0.6);
        Assert.False(Error(box).IsVisible);

        box.Classes.Add("ion-invalid");
        Dispatcher.UIThread.RunJobs();
        Assert.True(Helper(box).IsVisible);
        Assert.False(Error(box).IsVisible);

        box.Classes.Add("ion-touched");
        Dispatcher.UIThread.RunJobs();
        Assert.False(Helper(box).IsVisible);
        Assert.True(Error(box).IsVisible);
        Assert.Equal("Error text", Error(box).Text);
        AssertClose(Danger, Solid(Error(box).Foreground));

        box.Classes.Remove("ion-invalid");
        Dispatcher.UIThread.RunJobs();
        Assert.True(Helper(box).IsVisible);
        Assert.False(Error(box).IsVisible);
    });

    [AvaloniaFact]
    public void B19_Invalid_With_Only_Helper_Text_Shows_Neither()
    {
        var box = new AvonicCheckbox { Content = "Label", HelperText = "Helper" };
        box.Classes.Add("ion-touched");
        box.Classes.Add("ion-invalid");
        using var scene = Plain(box);

        // The bottom is there, with the helper hidden and the error's div empty, so no line of its own.
        Assert.True(Part<Control>(box, "PART_Bottom").IsVisible);
        Assert.False(Helper(box).IsVisible);
        Assert.False(Error(box).IsVisible);
    }

    [AvaloniaFact]
    public void B15_Checked_Before_The_First_Frame_Is_Drawn_At_Once()
    {
        using var clock = new TestClock();
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Plain(box);

        // As Stencil's first render is at the next frame: a change before it isn't a transition.
        box.Checked = true;

        Assert.Equal(0, Mark(box).DashOffset);
    }

    [AvaloniaFact]
    public void B21_Beside_An_End_Slot_It_Takes_What_Is_Left() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "With a note" };
        var note = new AvonicNote { Content = "Note" };
        var item = new AvonicItem { Content = box, EndContent = note };
        using var scene = Themed(item);

        // Measured: the checkbox 318.44 wide at 16, its box ending at 334.44, and the note after it.
        Assert.Equal(16, Bounds(box, item).X, 0.5);
        Assert.Equal(334.44, Bounds(box, item).Right, 0.6);
        Assert.Equal(Bounds(note, item).X - 16, Bounds(box, item).Right, 0.6);
        Assert.Equal(Bounds(box, item).Right, Bounds(Box(box), item).Right, 0.5);
    });

    [AvaloniaFact]
    public void B7_A_Long_Standalone_Label_Shrinks_To_The_Width() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "A standalone label that is far too long to fit on one line beside its box" };
        using var scene = Themed(box);

        // Ionic's inline-block overflows to 519.2 with its box off the page; here it's as wide as the page, the box at the end.
        Assert.Equal(390, box.Bounds.Width, 0.5);
        Assert.Equal(372, Bounds(Box(box), scene.Window).X, 0.5);
    });

    [AvaloniaFact]
    public void B19_With_Neither_There_Is_No_Bottom()
    {
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Plain(box);

        Assert.False(Part<Control>(box, "PART_Bottom").IsVisible);
        box.ErrorText = "Error";
        Assert.True(Part<Control>(box, "PART_Bottom").IsVisible);
        Assert.False(Helper(box).IsVisible);
        Assert.False(Error(box).IsVisible);
    }

    [AvaloniaFact]
    public void B20_Stacked_Supporting_Text_Is_16() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Stacked", LabelPlacement = CheckboxLabelPlacement.Stacked, HelperText = "Helper" };
        using var scene = Themed(box);

        Assert.Equal(16, Helper(box).FontSize);
    });

    // ── B21 to B24: in an item ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B21_In_An_Item_It_Fills_The_Row() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "In an item" };
        var item = new AvonicItem { Content = box };
        using var scene = Themed(item);

        // Measured i1: 358 by 47 at 16, the label 14 down, the box at 356, 14.5 down.
        AssertBox(16, 0, 358, 47, Bounds(box, item));
        Assert.Equal(14, Bounds(Label(box), item).Y, 0.5);
        AssertBox(356, 14.5, 18, 18, Bounds(Box(box), item));
    });

    [AvaloniaFact]
    public void B22_In_A_Start_Slot_It_Is_Its_Own_Size_With_No_Margins() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox();
        var item = new AvonicItem { StartContent = box, Content = new AvonicLabel { Content = "Start slot" } };
        using var scene = Themed(item);

        // Measured i3: 18 by 18 at 16, 15 down.
        AssertBox(16, 15, 18, 18, Bounds(box, item));
        Assert.Equal(default, Part<Control>(box, "PART_NativeWrapper").Margin);
    });

    [AvaloniaFact]
    public void B23_Stacked_In_An_Item_Has_Its_Own_Margins() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Stacked, start", LabelPlacement = CheckboxLabelPlacement.Stacked, Alignment = CheckboxAlignment.Start };
        var item = new AvonicItem { Content = box };
        using var scene = Themed(item);

        // Measured i4: 73 high, the label 10 down at 16, the box 45 down.
        Assert.Equal(73, box.Bounds.Height, 0.5);
        AssertBox(16, 10, 73.23, 14.25, Bounds(Label(box), item));
        AssertBox(16, 45, 18, 18, Bounds(Box(box), item));
    });

    [AvaloniaFact]
    public void B24_An_Item_Forwards_A_Tap_On_Its_Padding() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "In an item" };
        var item = new AvonicItem { Content = box };
        using var scene = Themed(item);
        var changes = Changes(box);

        Assert.Equal(ItemControlType.Checkbox, ((IItemControl)box).ItemControlType);
        Click(scene, new Point(5, Centre(item, scene).Y));

        Assert.True(box.Checked);
        Assert.Single(changes);
    });

    [AvaloniaFact]
    public void B24_A_Disabled_Checkbox_Takes_No_Forwarded_Tap()
    {
        var box = new AvonicCheckbox { Content = "In an item" };
        using var scene = Plain(box);

        box.IsEnabled = false;
        ((IItemControl)box).OnItemTap();

        Assert.False(box.Checked);
    }

    [AvaloniaFact]
    public void B15_Half_Way_Only_The_Start_Of_The_Tick_Is_Drawn() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var box = new AvonicCheckbox();
        using var scene = Themed(box);
        clock.At(16);
        Dispatcher.UIThread.RunJobs();
        Box(box).Transitions = null;

        box.Checked = true;
        clock.At(160);
        Dispatcher.UIThread.RunJobs();

        // 54 ms into the 90 after the delay: an offset of 12 leaves 18 units, the short stroke, 9, and 9 of the long one, which
        // ends at (22.79, 4.59).
        Assert.Equal(12, Mark(box).DashOffset, 6);
        static Point In(double x, double y) => new(2 + x * 14 / 24, 2 + y * 14 / 24);
        AssertClose(Colors.White, Pixel(scene, Box(box), In(5, 16.2).X, In(5, 16.2).Y), 40);
        AssertClose(Primary, Pixel(scene, Box(box), In(19.5, 7.9).X, In(19.5, 7.9).Y), 10);

        // And at the end, the whole tick.
        clock.At(16 + 200);
        AssertClose(Colors.White, Pixel(scene, Box(box), In(19.5, 7.9).X, In(19.5, 7.9).Y), 40);
    });

    [AvaloniaFact]
    public void B7_The_Host_Shows_A_Pointer() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Themed(box);

        Assert.Equal(new Cursor(StandardCursorType.Hand).ToString(), box.Cursor?.ToString());
    });

    [AvaloniaFact]
    public void B7_A_Change_Of_Placement_Lays_It_Out_Again() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Label start" };
        using var scene = Themed(box);

        box.LabelPlacement = CheckboxLabelPlacement.End;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, Bounds(Box(box), scene.Window).X, 0.5);
    });

    [AvaloniaFact]
    public void B10_A_Taller_Stacked_Host_Puts_Them_By_Justify() => InstalledTheme.Run(_ =>
    {
        var between = new AvonicCheckbox { Content = "Between", LabelPlacement = CheckboxLabelPlacement.Stacked, Height = 100 };
        var start = new AvonicCheckbox { Content = "Start", LabelPlacement = CheckboxLabelPlacement.Stacked, Justify = CheckboxJustify.Start, Height = 100 };
        var end = new AvonicCheckbox { Content = "End", LabelPlacement = CheckboxLabelPlacement.Stacked, Justify = CheckboxJustify.End, Height = 100 };
        using var scene = Themed(between, start, end);

        // justify-content on the column: space-between by default.
        Assert.Equal(82, Box(between).TranslatePoint(default, between)!.Value.Y, 0.5);
        Assert.Equal(35, Box(start).TranslatePoint(default, start)!.Value.Y, 0.5);
        Assert.Equal(82, Box(end).TranslatePoint(default, end)!.Value.Y, 0.5);
        Assert.Equal(100 - 18 - 16 - 19, Label(end).Bounds.Y, 0.5);
    });

    [AvaloniaFact]
    public void B10_Stacked_Text_Is_Centred() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "A long stacked label", LabelPlacement = CheckboxLabelPlacement.Stacked, HelperText = "Short" };
        using var scene = Themed(box);

        Assert.Equal(TextAlignment.Center, Helper(box).TextAlignment);
    });

    [AvaloniaFact]
    public void B12_Alignment_Center_Scales_A_Stacked_Label_From_Its_Top() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Stacked", LabelPlacement = CheckboxLabelPlacement.Stacked, Alignment = CheckboxAlignment.Center };
        using var scene = Themed(box);

        // transform-origin: center top: 7.16 in, and at the top.
        AssertBox(7.16 + (390 - 57.28) / 2, 0, 42.96, 14.25, Bounds(Label(box), scene.Window));
    });

    [AvaloniaFact]
    public void B18_Text_In_A_Content_Control_Or_In_Inlines_Counts() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicCheckbox { Content = new AvonicLabel { Content = "In a label" } };
        var inlines = new AvonicCheckbox { Content = new TextBlock { Inlines = [new Avalonia.Controls.Documents.Run("In a run")] } };
        using var scene = Themed(label, inlines);

        Assert.True(Label(label).IsVisible);
        Assert.True(Label(inlines).IsVisible);
    });

    [AvaloniaFact]
    public void B21_A_Long_Label_In_An_Item_Leaves_The_Box_Its_Place() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "A label in an item that is far too long to fit on one line beside its box" };
        var item = new AvonicItem { Content = box };
        using var scene = Themed(item);

        AssertBox(356, 14.5, 18, 18, Bounds(Box(box), item));
        Assert.Equal(358 - 18 - 16, Label(box).Bounds.Width, 0.5);

        // Trimmed to it, with an ellipsis.
        var text = box.GetVisualDescendants().OfType<TextBlock>().First(block => block.Text is { Length: > 40 });
        Assert.InRange(text.TextLayout.Width, 1, 324.5);
    });

    [AvaloniaFact]
    public void B21_The_Margins_In_An_Item_Make_A_Taller_Row() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicCheckbox { Content = "In an item, helper", HelperText = "Helper text" };
        var end = new AvonicCheckbox { Content = "In an item, end", HelperText = "Helper text", LabelPlacement = CheckboxLabelPlacement.End };
        var top = new AvonicCheckbox { Content = "At the top", Alignment = CheckboxAlignment.Start };
        using var scene = Themed(new AvonicItem { Content = start }, new AvonicItem { Content = end }, new AvonicItem { Content = top });

        // Measured i5: 57 high, the label 10 down. At the start of the cross axis, the box keeps its 10 above.
        Assert.Equal(57, start.Bounds.Height, 0.6);
        Assert.Equal(57, end.Bounds.Height, 0.6);
        Assert.Equal(10, Bounds(Label(start), start).Y, 0.5);
        Assert.Equal(10, Box(top).TranslatePoint(default, top)!.Value.Y, 0.5);
        Assert.Equal(10, Bounds(Label(top), top).Y, 0.5);
    });

    [AvaloniaFact]
    public void B22_In_An_End_Slot_It_Is_Its_Own_Size_Too() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox();
        var item = new AvonicItem { Content = new AvonicLabel { Content = "End slot" }, EndContent = box };
        using var scene = Themed(item);

        Assert.Equal(new Size(18, 18), box.Bounds.Size);
        Assert.Equal(default, Part<Control>(box, "PART_NativeWrapper").Margin);
    });

    [AvaloniaFact]
    public void B18_Text_That_Comes_Later_Shows_The_Label() => InstalledTheme.Run(_ =>
    {
        var text = new TextBlock();
        var box = new AvonicCheckbox { Content = text };
        using var scene = Themed(box);
        Assert.False(Label(box).IsVisible);

        // Ionic reads textContent at its next render, which a change of a prop causes.
        text.Text = "Loaded";
        box.Checked = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(Label(box).IsVisible);

        // A shown label's text changing measures it again.
        text.Text = "";
        Dispatcher.UIThread.RunJobs();
        Assert.False(Label(box).IsVisible);
    });

    private sealed record Choice(string Name);

    [AvaloniaFact]
    public void B18_Data_Counts_As_What_Its_Template_Makes() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicCheckbox
        {
            Content = new Choice("Heart"),
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Choice>((_, _) => new AvonicIcon { IconName = "heart" }),
        };
        var named = new AvonicCheckbox
        {
            Content = new Choice("Heart"),
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Choice>((choice, _) => new TextBlock { Text = choice.Name }),
        };
        using var scene = Themed(icon, named);

        Assert.False(Label(icon).IsVisible);
        Assert.True(Label(named).IsVisible);
    });

    /// <summary>Measures its child with all the room in the world, and then arranges it narrower.</summary>
    private sealed class Squeeze : Decorator
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            Child!.Measure(Size.Infinity);
            return new Size(150, Child.DesiredSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Child!.Arrange(new Rect(0, 0, 150, finalSize.Height));
            return finalSize;
        }
    }

    [AvaloniaFact]
    public void B10_Arranged_Narrower_Than_Measured_The_Label_Is_Clipped_And_Layout_Settles() => InstalledTheme.Run(_ =>
    {
        var stacked = new AvonicCheckbox
        {
            Content = "A stacked label much longer than its host",
            LabelPlacement = CheckboxLabelPlacement.Stacked,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
        };
        var row = new AvonicCheckbox
        {
            Content = "A label much longer than its host",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
        };
        using var scene = Themed(new Squeeze { Child = stacked }, new Squeeze { Child = row });

        // No measure in arrange, which would ask for another layout pass after every one.
        Assert.Equal(200, Label(stacked).Bounds.Width, 0.5);
        Assert.Equal(150 - 18 - 16, Label(row).Bounds.Width, 0.5);
        Assert.Equal(132, Bounds(Box(row), row).X, 0.5);
        foreach (var wrapper in scene.Window.GetVisualDescendants().OfType<LabelledControlPanel>())
        {
            Assert.True(wrapper.IsMeasureValid && wrapper.IsArrangeValid);
        }
    });

    [AvaloniaFact]
    public void B9_A_Fixed_Label_Does_Not_Shrink() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "Fixed label that is long", LabelPlacement = CheckboxLabelPlacement.Fixed, Width = 110 };
        using var scene = Themed(box);

        // flex: 0 0 100px: the row overflows, and the box is still at 116.
        Assert.Equal(100, Label(box).Bounds.Width, 0.5);
        Assert.Equal(116, Bounds(Box(box), box).X, 0.5);
    });

    [AvaloniaFact]
    public void B6_In_A_Disabled_Item_It_Is_Not_Faded_Again() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox { Content = "In a disabled item" };
        using var scene = Themed(new AvonicItem { Content = box, IsEnabled = false });

        Assert.Equal(1, Label(box).Opacity);
        Assert.Equal(1, Part<Control>(box, "PART_NativeWrapper").Opacity);
    });

    [AvaloniaFact]
    public void B18_New_Data_Is_Read_From_What_Its_Template_Makes() => InstalledTheme.Run(_ =>
    {
        var box = new AvonicCheckbox
        {
            Content = 1,
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<int>((value, _) => value == 1 ? new AvonicIcon { IconName = "heart" } : new TextBlock { Text = "Two" }),
        };
        using var scene = Themed(box);
        Assert.False(Label(box).IsVisible);

        box.Content = 2;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Label(box).IsVisible);
    });

    [AvaloniaFact]
    public void B25_Invalid_With_No_Error_Text_Is_Described_By_Nothing()
    {
        var box = new AvonicCheckbox { Content = "Label", HelperText = "Pick one" };
        using var scene = Plain(box);
        var peer = ControlAutomationPeer.CreatePeerForElement(box);

        box.Classes.Add("ion-touched");
        box.Classes.Add("ion-invalid");

        // aria-describedby is the helper's div, which is empty while invalid.
        Assert.True(string.IsNullOrEmpty(peer.GetHelpText()));
    }

    [AvaloniaFact]
    public void B15_Hidden_Half_Way_The_Mark_Ends_Its_Transition()
    {
        using var clock = new TestClock();
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Plain(box);
        clock.At(16);

        box.Checked = true;
        clock.At(48);
        box.IsVisible = false;
        clock.At(64);

        Assert.Equal(0, Mark(box).DashOffset);
    }

    [AvaloniaFact]
    public void B25_Becoming_Invalid_Tells_Automation_The_New_Description()
    {
        var box = new AvonicCheckbox { Content = "Label", HelperText = "Helper", ErrorText = "Error" };
        using var scene = Plain(box);
        var peer = ControlAutomationPeer.CreatePeerForElement(box);
        var raised = new List<object?>();
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == AutomationElementIdentifiers.HelpTextProperty)
            {
                raised.Add(e.NewValue);
            }
        };

        box.Classes.Add("ion-touched");
        box.Classes.Add("ion-invalid");
        box.Classes.Remove("ion-invalid");

        Assert.Equal(["Error", "Helper"], raised);
    }

    // ── B25, B26: automation ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B25_It_Is_A_Check_Box_Named_By_Its_Label_With_Its_State()
    {
        var box = new AvonicCheckbox { Content = "With helper", HelperText = "Helper text", ErrorText = "Error text" };
        using var scene = Plain(box);
        var peer = ControlAutomationPeer.CreatePeerForElement(box);
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(peer.GetProvider<IToggleProvider>());

        Assert.Equal(AutomationControlType.CheckBox, peer.GetAutomationControlType());
        Assert.True(peer.IsControlElement());
        Assert.True(peer.IsContentElement());

        // Measured: aria-labelledby the label, which holds the helper.
        Assert.Equal("With helper Helper text", peer.GetName());
        Assert.Equal("Helper text", peer.GetHelpText());
        Assert.Equal(ToggleState.Off, toggle.ToggleState);

        box.Checked = true;
        Assert.Equal(ToggleState.On, toggle.ToggleState);
        box.Indeterminate = true;
        Assert.Equal(ToggleState.Indeterminate, toggle.ToggleState);

        // Invalid: the error describes it, and isn't in the name, measured.
        box.Classes.Add("ion-touched");
        box.Classes.Add("ion-invalid");
        Assert.Equal("With helper", peer.GetName());
        Assert.Equal("Error text", peer.GetHelpText());

        AutomationProperties.SetName(box, "Own name");
        Assert.Equal("Own name", peer.GetName());
    }

    [AvaloniaFact]
    public void B25_Toggle_Is_A_Click_And_Raises_The_State_Change()
    {
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Plain(box);
        var changes = Changes(box);
        var peer = ControlAutomationPeer.CreatePeerForElement(box);
        var raised = new List<object?>();
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == TogglePatternIdentifiers.ToggleStateProperty)
            {
                raised.Add(e.NewValue);
            }
        };

        peer.GetProvider<IToggleProvider>()!.Toggle();

        Assert.True(box.Checked);
        Assert.Single(changes);
        Assert.Equal([ToggleState.On], raised);

        box.IsEnabled = false;
        peer.GetProvider<IToggleProvider>()!.Toggle();
        Assert.True(box.Checked);
    }

    [AvaloniaFact]
    public void B25_Required_Is_Required_For_The_Form()
    {
        var box = new AvonicCheckbox { Content = "Label" };
        Assert.False(AutomationProperties.GetIsRequiredForForm(box));

        box.Required = true;
        Assert.True(AutomationProperties.GetIsRequiredForForm(box));

        box.Required = false;
        Assert.False(AutomationProperties.GetIsRequiredForForm(box));
    }

    // An app's own style for it wins, whenever required turns on after it. A value given at style priority when it turns on
    // would come after the app's style, and win.
    [AvaloniaFact]
    public void B25_An_Apps_Style_Survives_Required_Turning_On_After_It()
    {
        var box = new AvonicCheckbox { Content = "Label" };
        using var scene = Scene.Styled(box, Scene.AppStyle<AvonicCheckbox>(AutomationProperties.IsRequiredForFormProperty, false));

        box.Required = true;

        Assert.False(AutomationProperties.GetIsRequiredForForm(box));
    }

    [AvaloniaFact]
    public void B26_The_Helper_Is_Polite_And_The_Error_Assertive()
    {
        var box = new AvonicCheckbox { Content = "Label", HelperText = "Helper" };
        using var scene = Plain(box);

        Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(Helper(box)));
        Assert.Equal(AutomationLiveSetting.Assertive, AutomationProperties.GetLiveSetting(Error(box)));
    }
}
