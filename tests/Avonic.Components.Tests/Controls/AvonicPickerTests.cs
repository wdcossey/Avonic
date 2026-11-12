using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;
using static Avonic.Components.Tests.Controls.AvonicPickerColumnTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-picker's behaviours, from <c>picker.js</c> and <c>picker.md.css</c>, measured in Chromium: see
/// <c>docs/components/ion-picker/PLAN.md</c>.
/// </summary>
public class AvonicPickerTests
{
    private static AvonicPickerColumn Column(object? value, IEnumerable<string> values, bool numeric = false)
    {
        var column = new AvonicPickerColumn { Value = value, NumericInput = numeric };
        foreach (var v in values)
        {
            column.Items.Add(new AvonicPickerColumnOption { Value = v, Content = v });
        }

        return column;
    }

    private static Rect BoundsIn(Visual visual, Visual root) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root)!.Value);

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    // ── P1: layout ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void P1_The_Highlight_And_Fades_Sit_Where_Chromium_Puts_Them() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var first = Column("Green", AvonicPickerColumnTests.Colors);
        var second = Column("5", Numbers);
        var picker = new AvonicPicker { Items = { first, second }, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(picker);
        Show(clock);

        // Measured: picker 390 × 200; highlight 8, 83, 374 × 34, radius 8, transparent; fades 0–83 and 116–200.
        Assert.Equal(new Size(390, 200), picker.Bounds.Size);
        var highlight = Part<Border>(picker, "PART_Highlight");
        Assert.Equal(new Rect(8, 83, 374, 34), BoundsIn(highlight, picker));
        Assert.Equal(new CornerRadius(8), highlight.CornerRadius);
        Assert.Null(highlight.Background);
        Assert.Equal(new Rect(0, 0, 390, 83), BoundsIn(Part<Border>(picker, "PART_Before"), picker));
        Assert.Equal(new Rect(0, 116, 390, 84), BoundsIn(Part<Border>(picker, "PART_After"), picker));

        // linear-gradient(to bottom, white 20%, transparent 90%), and to top, 30% and 90%.
        var before = (LinearGradientBrush)Part<Border>(picker, "PART_Before").Background!;
        Assert.Equal([(0.2, Avalonia.Media.Colors.White), (0.9, Color.FromArgb(0, 255, 255, 255))], before.GradientStops.Select(s => (s.Offset, s.Color)));
        Assert.True(before.EndPoint.Point.Y > before.StartPoint.Point.Y);
        var after = (LinearGradientBrush)Part<Border>(picker, "PART_After").Background!;
        Assert.Equal([(0.3, Avalonia.Media.Colors.White), (0.9, Color.FromArgb(0, 255, 255, 255))], after.GradientStops.Select(s => (s.Offset, s.Color)));
        Assert.True(after.EndPoint.Point.Y < after.StartPoint.Point.Y);

        // The columns side by side in the middle. Measured: 102.3 and 58 wide, from 114.84; text measures a little differently.
        var a = BoundsIn(first, picker);
        var b = BoundsIn(second, picker);
        Assert.Equal(58, b.Width, 0.5);
        Assert.Equal(102.3, a.Width, 1.5);
        Assert.Equal(a.Right, b.X, 0.01);
        Assert.Equal(390 - b.Right, a.X, 1.0);
        Assert.Equal(new Rect(0, 0, a.Width, 200), new Rect(0, a.Y, a.Width, a.Height));
    });

    [AvaloniaFact]
    public void P1_The_Fades_Follow_FadeBackgroundRgb_Then_The_Background_Colour() => InstalledTheme.Run(_ =>
    {
        var picker = new AvonicPicker { Items = { Column("a", ["a"]) } };
        picker.Resources["Avonic.BackgroundColor"] = Color.Parse("#123456");
        using var scene = new Scene(picker);
        Dispatcher.UIThread.RunJobs();
        var before = (LinearGradientBrush)Part<Border>(picker, "PART_Before").Background!;
        Assert.Equal(Color.Parse("#123456"), before.GradientStops[0].Color);

        picker.Resources["Avonic.Picker.FadeBackgroundRgb"] = Color.Parse("#ABCDEF");
        Dispatcher.UIThread.RunJobs();
        before = (LinearGradientBrush)Part<Border>(picker, "PART_Before").Background!;
        Assert.Equal(Color.Parse("#ABCDEF"), before.GradientStops[0].Color);
        Assert.Equal(Color.FromArgb(0, 0xAB, 0xCD, 0xEF), before.GradientStops[1].Color);

        picker.Resources["Avonic.Picker.HighlightBackground"] = Brushes.Red;
        picker.Resources["Avonic.Picker.HighlightBorderRadius"] = new CornerRadius(3);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Brushes.Red, Part<Border>(picker, "PART_Highlight").Background);
        Assert.Equal(new CornerRadius(3), Part<Border>(picker, "PART_Highlight").CornerRadius);
    });

    [AvaloniaFact]
    public void P1_Columns_Go_Left_To_Right_Whatever_The_Direction() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var first = Column("a", ["a"]);
        var second = Column("b", ["b"]);
        var picker = new AvonicPicker { Items = { first, second } };
        using var scene = new Scene(new Border { FlowDirection = FlowDirection.RightToLeft, Child = picker });
        Show(clock);

        Assert.Equal(FlowDirection.LeftToRight, first.FlowDirection);
        Assert.True(BoundsIn(first, scene.Window).X < BoundsIn(second, scene.Window).X);
    });

    // ── P2: text alignment ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void P2_The_First_Columns_Text_Is_At_The_Start_The_Lasts_At_The_End_And_A_Lone_One_Centred() => InstalledTheme.Run(_ =>
    {
        var first = Column("a", ["a", "longer"]);
        var middle = Column("a", ["a"]);
        var last = Column("a", ["a", "longer"]);
        var alone = Column("a", ["a"]);
        var outside = Column("a", ["a"]);
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicPicker { Items = { first, middle, last } },
                new AvonicPicker { Items = { alone } },
                outside,
            },
        });
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(HorizontalAlignment.Left, first.HorizontalContentAlignment);
        Assert.Equal(HorizontalAlignment.Center, middle.HorizontalContentAlignment);
        Assert.Equal(HorizontalAlignment.Right, last.HorizontalContentAlignment);
        Assert.Equal(HorizontalAlignment.Center, alone.HorizontalContentAlignment);
        Assert.Equal(HorizontalAlignment.Center, outside.HorizontalContentAlignment);

        // text-align: inherit: the short option's text sits at the start of the first column, and at the end of the last.
        GestureTests.Render();
        TextBlock Text(AvonicPickerColumn column) => column.Items.OfType<AvonicPickerColumnOption>().First().GetVisualDescendants().OfType<TextBlock>().First();
        var firstOption = first.Items.OfType<AvonicPickerColumnOption>().First();
        var lastOption = last.Items.OfType<AvonicPickerColumnOption>().First();
        Assert.Equal(0, BoundsIn(Text(first), firstOption).X, 0.5);
        Assert.Equal(lastOption.Bounds.Width, BoundsIn(Text(last), lastOption).Right, 0.5);
    });

    // ── P3: numeric input ────────────────────────────────────────────────────────────────────

    private static readonly string[] Hours = Enumerable.Range(1, 12).Select(i => i.ToString()).ToArray();
    private static readonly string[] Minutes = Enumerable.Range(0, 60).Select(i => i.ToString("00")).ToArray();

    private sealed record Time(AvonicPicker Picker, AvonicPickerColumn Hour, AvonicPickerColumn Minute, AvonicPickerColumn Other, TextBox Input);

    private static Time ShowTime(TestClock clock, out Scene scene)
    {
        var hour = Column("1", Hours, numeric: true);
        var minute = Column("00", Minutes, numeric: true);
        var other = Column("AM", ["AM", "PM"]);
        var picker = new AvonicPicker { Items = { hour, minute, other }, VerticalAlignment = VerticalAlignment.Top };
        scene = new Scene(picker);
        Show(clock);
        return new Time(picker, hour, minute, other, Part<TextBox>(picker, "PART_Input"));
    }

    private static bool Active(AvonicPickerColumn column) => column.Classes.Contains(":column-active");

    [AvaloniaFact]
    public void P3_A_Tap_In_The_Highlight_Takes_Digits_For_Every_Numeric_Column() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var time = ShowTime(clock, out var scene);
        using var _scene = scene;
        var changes = new List<object?>();
        time.Hour.IonChange += (_, e) => changes.Add(e.Value);
        time.Minute.IonChange += (_, e) => changes.Add(e.Value);

        Click(scene, Centre(time.Hour.Scroller!, scene));
        Later(clock, 16);
        Assert.True(time.Picker.UseInputMode);
        Assert.Null(time.Picker.InputModeColumn);
        Assert.True(time.Input.IsFocused);
        Assert.True(Active(time.Hour));
        Assert.True(Active(time.Minute));
        Assert.False(Active(time.Other));

        // "1230": the first two digits are the hour, the next two the minute.
        foreach (var digit in "1230")
        {
            scene.Window.KeyTextInput(digit.ToString());
            Dispatcher.UIThread.RunJobs();
        }

        Assert.Equal("12", time.Hour.Value);
        Assert.Equal("30", time.Minute.Value);
        Assert.Contains("12", changes);
        Assert.Contains("30", changes);

        // A fifth digit keeps the last four, 2304: 23 is no hour, so 2, and the next two are the minute.
        scene.Window.KeyTextInput("4");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("2304", time.Picker.InputValue);
        Assert.Equal("2", time.Hour.Value);
        Assert.Equal("30", time.Minute.Value);
    });

    [AvaloniaFact]
    public void P3_A_Tap_Outside_The_Highlight_Leaves_Input_Mode() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var time = ShowTime(clock, out var scene);
        using var _scene = scene;
        Click(scene, Centre(time.Hour.Scroller!, scene));
        Later(clock, 16);
        Assert.True(time.Picker.UseInputMode);

        Click(scene, Centre(time.Hour.Scroller!, scene) + new Point(0, -68));
        Later(clock, 600);
        Assert.False(time.Picker.UseInputMode);
        Assert.False(time.Input.IsFocused);
        Assert.False(Active(time.Hour));
        Assert.Equal("", time.Picker.InputValue);
    });

    [AvaloniaFact]
    public void P3_Enter_In_The_Input_Leaves_Input_Mode() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var time = ShowTime(clock, out var scene);
        using var _scene = scene;
        Click(scene, Centre(time.Hour.Scroller!, scene));
        Later(clock, 16);
        scene.Window.KeyTextInput("7");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("7", time.Hour.Value);
        // "75": no hour 75, so the 7, and the rest is the minute, 05.
        scene.Window.KeyTextInput("5");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("7", time.Hour.Value);
        Assert.Equal("05", time.Minute.Value);

        Key(scene, PhysicalKey.Enter);
        Later(clock, 16);
        Assert.False(time.Picker.UseInputMode);
        Assert.False(time.Input.IsFocused);
    });

    [AvaloniaFact]
    public void P3_A_Column_Focused_By_The_Keyboard_Takes_Digits_For_Itself_And_A_Pause_Starts_Again() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var time = ShowTime(clock, out var scene);
        using var _scene = scene;

        time.Hour.Focus(NavigationMethod.Tab);
        Later(clock, 16);
        Assert.True(time.Picker.UseInputMode);
        Assert.Same(time.Hour, time.Picker.InputModeColumn);
        Assert.True(time.Hour.IsFocused);
        Assert.True(Active(time.Hour));
        Assert.False(Active(time.Minute));

        // "1" then "2": 12.
        // A key that isn't a digit doesn't go in.
        scene.Window.KeyTextInput("a");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("", time.Picker.InputValue);
        scene.Window.KeyTextInput("1");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("1", time.Hour.Value);
        scene.Window.KeyTextInput("2");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("12", time.Hour.Value);
        // After a second's pause "1" starts again, and after another "0" is alone, no hour, where without the pauses it would
        // have made 10.
        Later(clock, 1001);
        scene.Window.KeyTextInput("1");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("1", time.Hour.Value);
        Later(clock, 1001);
        scene.Window.KeyTextInput("0");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("1", time.Hour.Value);
        // "07" is no option's text, so the last digit alone: 7.
        scene.Window.KeyTextInput("7");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("7", time.Hour.Value);
        // "7", "5": 75 is no hour, so the last digit alone: 5.
        scene.Window.KeyTextInput("5");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("5", time.Hour.Value);

        // A column that isn't numeric leaves input mode when focused.
        time.Other.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.False(time.Picker.UseInputMode);
    });

    [AvaloniaFact]
    public void P3_A_Users_Scroll_Leaves_Input_Mode_Only_Once_A_Scroll_Has_Ended_Since_The_Column_Last_Centred_Itself() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var time = ShowTime(clock, out var scene);
        using var _scene = scene;
        Click(scene, Centre(time.Hour.Scroller!, scene));
        Later(clock, 600);
        Assert.True(time.Picker.UseInputMode);

        // Measured in Chromium: wheels over the minute column in input mode leave it on. Centring the value (at load, and after
        // each change) asks for 2 px off the snap point, which clears canExitInputMode until a scroll ends; the next scroll's
        // own change then centres again. So a wheel never leaves it, at md's 200 px.
        scene.Window.MouseWheel(Centre(time.Minute.Scroller!, scene), new Vector(0, -1), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Later(clock, 600);
        Assert.True(time.Picker.UseInputMode);
        Assert.True(Active(time.Minute));
        Assert.Equal("03", time.Minute.Value);

        // A scroll that comes back to where it was ends with no change, so nothing centres the column again, and the flag stays
        // set: the next scroll that changes the selection leaves input mode, and drops picker-column-active at once.
        var scroller = time.Minute.Scroller!;
        scroller.Offset = new Vector(0, scroller.Offset.Y + 34);
        Later(clock, 50);
        scroller.Offset = new Vector(0, scroller.Offset.Y - 34);
        Later(clock, 600);
        Assert.True(time.Picker.UseInputMode);
        Assert.Equal("03", time.Minute.Value);

        scroller.Offset = new Vector(0, scroller.Offset.Y + 34);
        Later(clock, 16);
        Assert.False(time.Picker.UseInputMode);
        Assert.False(Active(time.Minute));
        Assert.False(time.Input.IsFocused);
    });

    [AvaloniaFact]
    public void P3_With_One_Numeric_Column_A_Tap_On_Its_Option_Takes_Digits_For_That_Column() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var minute = Column("00", Minutes, numeric: true);
        var other = Column("AM", ["AM", "PM"]);
        var picker = new AvonicPicker { Items = { minute, other }, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(picker);
        Show(clock);

        Click(scene, Centre(minute.Scroller!, scene));
        Later(clock, 16);
        Assert.Same(minute, picker.InputModeColumn);
        Assert.True(Active(minute));

        // One column's search: "56", then "7", which makes 567, no minute, so the last two, 67, then the last one: 07.
        foreach (var digit in "567")
        {
            scene.Window.KeyTextInput(digit.ToString());
            Dispatcher.UIThread.RunJobs();
        }

        Assert.Equal("07", minute.Value);
    });

    [AvaloniaFact]
    public void P3_Leaving_Input_Mode_Mid_Scroll_Waits_For_The_Scroll_To_End() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var time = ShowTime(clock, out var scene);
        using var _scene = scene;
        Click(scene, Centre(time.Hour.Scroller!, scene));
        Later(clock, 600);
        Assert.True(Active(time.Minute));

        time.Minute.Scroller!.Offset = new Vector(0, time.Minute.Scroller.Offset.Y + 34);
        Later(clock, 16);
        Assert.True(time.Minute.IsScrolling);
        time.Picker.ExitInputMode();
        Later(clock, 16);
        Assert.False(Active(time.Hour));
        Assert.True(Active(time.Minute));
        Later(clock, 300);
        Assert.False(Active(time.Minute));
    });

    [AvaloniaFact]
    public void P3_Focus_Leaving_The_Picker_Leaves_Input_Mode() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var hour = Column("1", Hours, numeric: true);
        var minute = Column("00", Minutes, numeric: true);
        var picker = new AvonicPicker { Items = { hour, minute } };
        var outside = new Button { Content = "Outside" };
        using var scene = new Scene(new StackPanel { VerticalAlignment = VerticalAlignment.Top, Children = { picker, outside } });
        Show(clock);

        hour.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.True(picker.UseInputMode);
        minute.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.True(picker.UseInputMode);
        Assert.Same(minute, picker.InputModeColumn);

        outside.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.False(picker.UseInputMode);
    });

    [AvaloniaFact]
    public void P3_The_Inputs_Blur_Clears_What_Was_Typed() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var time = ShowTime(clock, out var scene);
        using var _scene = scene;
        Click(scene, Centre(time.Hour.Scroller!, scene));
        Later(clock, 16);
        scene.Window.KeyTextInput("1");
        Dispatcher.UIThread.RunJobs();

        // Focus goes from the input to a column: the blur leaves input mode, emptying the input, and the column's focus enters
        // it again for itself, so "2" is 2 and not 12.
        time.Hour.Focus(NavigationMethod.Tab);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(time.Hour, time.Picker.InputModeColumn);
        scene.Window.KeyTextInput("2");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("2", time.Hour.Value);
    });

    [AvaloniaFact]
    public void P3_With_No_Numeric_Column_A_Tap_Doesnt_Enter_Input_Mode() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var column = Column("1", Hours);
        var picker = new AvonicPicker { Items = { column }, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(picker);
        Show(clock);

        Click(scene, Centre(column.Scroller!, scene));
        Later(clock, 16);
        Assert.False(picker.UseInputMode);
    });

    [AvaloniaFact]
    public void P3_In_Input_Mode_A_Tap_On_A_Columns_Own_Part_Takes_It_And_A_Second_Takes_Every_Column_Again() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var time = ShowTime(clock, out var scene);
        using var _scene = scene;
        Click(scene, Centre(time.Hour.Scroller!, scene));
        Later(clock, 16);
        Assert.Null(time.Picker.InputModeColumn);

        // The column's padding, beside its options, in the highlight's row: the column itself, as its shadow root retargets.
        var padding = new Point(BoundsIn(time.Hour.Scroller!, scene.Window).X + 4, Centre(time.Hour.Scroller!, scene).Y);
        Click(scene, padding);
        Later(clock, 16);
        Assert.Same(time.Hour, time.Picker.InputModeColumn);
        Assert.False(Active(time.Minute));
        Click(scene, padding);
        Later(clock, 16);
        Assert.Null(time.Picker.InputModeColumn);
        Assert.True(Active(time.Minute));
    });

    // ── The compare frame ────────────────────────────────────────────────────────────────────

    /// <summary>The same content as the --html at the top of docs/components/ion-picker/COMPARISON.md.</summary>
    internal static StackPanel ReferencePage()
    {
        var colors = Column("Green", AvonicPickerColumnTests.Colors);
        ((AvonicPickerColumnOption)colors.Items[2]!).IsEnabled = false;
        var disabled = Column("b", ["a", "b", "c"]);
        disabled.IsEnabled = false;
        ((AvonicPickerColumnOption)disabled.Items[1]!).IsEnabled = false;
        disabled.PrefixContent = "Pre";
        disabled.SuffixContent = "Suffix";
        return new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Top,
            Children =
            {
                new AvonicPicker { Items = { colors, Column("5", Numbers) } },
                new AvonicPicker { Items = { disabled } },
            },
        };
    }

    [AvaloniaFact]
    public void Reference_Page() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = ReferencePage();
        using var scene = new Scene(page, 390, 844);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Show(clock);
        Dispatcher.UIThread.RunJobs();
        Avonic.Components.Tests.Frames.Save(scene.Window.CaptureRenderedFrame()!, "avonic-picker-page.png");

        Assert.Equal(400, page.Bounds.Height, 0.5);
    });
}
