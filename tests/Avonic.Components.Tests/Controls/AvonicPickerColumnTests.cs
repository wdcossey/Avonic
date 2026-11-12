using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-picker-column's and ion-picker-column-option's behaviours, from <c>picker-column.js</c> and
/// <c>picker-column-option.js</c>, measured in Chromium: see <c>docs/components/ion-picker/PLAN.md</c>. Most run with the theme,
/// as where an option sits is the theme's.
/// </summary>
public class AvonicPickerColumnTests
{
    internal static readonly Color Primary = Color.Parse("#0054E9");

    internal sealed record Page(AvonicPicker Picker, AvonicPickerColumn Column, List<object?> Changes)
    {
        public ScrollViewer Scroll => Column.Scroller!;

        public AvonicPickerColumnOption Option(object value) => Column.Items.OfType<AvonicPickerColumnOption>().First(o => Equals(o.Value, value));

        public string Active => string.Join(",", Column.Options.Where(o => o.IsActive).Select(o => o.Value));
    }

    internal static readonly string[] Colors = ["Red", "Orange", "Yellow", "Green", "Blue", "Indigo", "Violet"];

    internal static readonly string[] Numbers = Enumerable.Range(1, 12).Select(i => i.ToString()).ToArray();

    internal static Page Build(object? value = null, string[]? values = null, string? disabled = null)
    {
        var column = new AvonicPickerColumn { Value = value };
        foreach (var v in values ?? Colors)
        {
            column.Items.Add(new AvonicPickerColumnOption { Value = v, Content = v, IsEnabled = v != disabled });
        }

        var changes = new List<object?>();
        column.IonChange += (_, e) => changes.Add(e.Value);
        var picker = new AvonicPicker { Items = { column }, VerticalAlignment = VerticalAlignment.Top };
        return new Page(picker, column, changes);
    }

    /// <summary>Shows it, lets the layout and the listener's frame run.</summary>
    internal static void Show(TestClock clock)
    {
        Dispatcher.UIThread.RunJobs();
        GestureTests.Render();
        Frames(clock, clock.Now.TotalMilliseconds + 32);
    }

    /// <summary>Frame by frame, with the layout pass between.</summary>
    internal static void Frames(TestClock clock, double until)
    {
        while (clock.Now.TotalMilliseconds + 16 <= until)
        {
            clock.At(clock.Now.TotalMilliseconds + 16);
            Dispatcher.UIThread.RunJobs();
        }

        clock.At(until);
        Dispatcher.UIThread.RunJobs();
    }

    internal static void Later(TestClock clock, double milliseconds) => Frames(clock, clock.Now.TotalMilliseconds + milliseconds);

    internal static Point Centre(Visual visual, Scene scene) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(scene.Window)!.Value).Center;

    internal static void Click(Scene scene, Point at)
    {
        GestureTests.Render();
        scene.Window.MouseMove(at, RawInputModifiers.None);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    internal static void Key(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>A finger dragging from the column's centre by <paramref name="dy"/>, in steps of 5, then lifted.</summary>
    internal static void Drag(Scene scene, Page page, double dy, bool lift = true)
    {
        GestureTests.Render();
        var at = Centre(page.Scroll, scene);
        var finger = scene.Window.TouchBegin(at, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        var steps = (int)Math.Abs(dy / 5);
        for (var i = 1; i <= steps; i++)
        {
            scene.Window.TouchMove(finger, at + new Point(0, Math.Sign(dy) * 5 * i), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }

        if (lift)
        {
            scene.Window.TouchEnd(finger, at + new Point(0, dy), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static IBrush? Foreground(AvonicPickerColumnOption option) => option.Foreground;

    private static Color? ColorOf(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    // ── C1: at load ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void C1_At_Load_It_Centres_Its_Value_Which_Is_Active_With_No_IonChange() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("Green");
        using var scene = new Scene(page.Picker);
        Show(clock);

        // Measured in Chromium: scrollTop 121 for the 4th option, 34 * 3 + 19.
        Assert.Equal(121, page.Scroll.Offset.Y, 0.5);
        Assert.Equal("Green", page.Active);
        Assert.Equal(Primary, ColorOf(Foreground(page.Option("Green"))));
        Assert.NotEqual(Primary, ColorOf(Foreground(page.Option("Blue"))));
        Later(clock, 600);
        Assert.Empty(page.Changes);
        Assert.Equal("Green", page.Column.Value);
    });

    // ── C2: a value from code ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void C2_A_Value_From_Code_Scrolls_Smoothly_With_No_IonChange() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("5", Numbers);
        using var scene = new Scene(page.Picker);
        Show(clock);
        Assert.Equal(155, page.Scroll.Offset.Y, 0.5);

        page.Column.Value = "9";
        var start = clock.Now.TotalMilliseconds;
        Assert.Equal(155, page.Scroll.Offset.Y, 0.5);
        // Measured: 136 px in 200 ms; √136 / 60 s = 194 ms, eased in and out, so slow at first and halfway at half time.
        Frames(clock, start + 97);
        Assert.InRange(page.Scroll.Offset.Y, 210, 236);
        Frames(clock, start + 200);
        Assert.Equal(291, page.Scroll.Offset.Y, 0.5);
        Later(clock, 600);
        Assert.Empty(page.Changes);
        Assert.Equal("9", page.Active);
    });

    // ── C3: a scroll by the user ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void C3_A_Wheel_Moves_The_Active_Option_And_Sets_The_Value_250ms_After_The_Last_Scroll() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("Green");
        using var scene = new Scene(page.Picker);
        Show(clock);

        scene.Window.MouseWheel(Centre(page.Scroll, scene), new Vector(0, -1), RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        var scrolled = clock.Now.TotalMilliseconds;
        // Measured: a notch from 121 rests at 223, the nearest snap to 221.
        Assert.Equal(223, page.Scroll.Offset.Y, 0.5);
        Frames(clock, scrolled + 16);
        Assert.Equal("Violet", page.Active);
        Assert.Equal("Green", page.Column.Value);

        Frames(clock, scrolled + 16 + 240);
        Assert.Empty(page.Changes);
        Frames(clock, scrolled + 16 + 260);
        Assert.Equal("Violet", page.Column.Value);
        Assert.Equal(["Violet"], page.Changes);
        Later(clock, 500);
        Assert.Single(page.Changes);
    });

    [AvaloniaFact]
    public void C3_Each_Scroll_Puts_Off_The_Value_Until_250ms_After_The_Last() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("Red");
        using var scene = new Scene(page.Picker);
        Show(clock);

        for (var i = 1; i <= 4; i++)
        {
            page.Scroll.Offset = new Vector(0, page.Scroll.Offset.Y + 34);
            Later(clock, 200);
            Assert.Empty(page.Changes);
        }

        Later(clock, 100);
        Assert.Equal(["Blue"], page.Changes);
    });

    [AvaloniaFact]
    public void C3_A_Disabled_Option_At_The_Centre_Is_Never_Active_Nor_The_Value() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("Orange", disabled: "Yellow");
        using var scene = new Scene(page.Picker);
        Show(clock);
        Assert.Equal("Orange", page.Active);

        // A scroll that stops with Yellow, disabled, in the middle.
        page.Scroll.Offset = new Vector(0, 87);
        Later(clock, 600);
        Assert.Equal("", page.Active);
        Assert.Empty(page.Changes);
        Assert.Equal("Orange", page.Column.Value);
    });

    // ── C4: snapping ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void C4_A_Drag_Comes_To_Rest_On_An_Enabled_Option_And_Not_On_A_Disabled_One() => InstalledTheme.Run(_ =>
    {
        foreach (var disabled in new[] { (string?)null, "Yellow" })
        {
            using var clock = new TestClock(timesEvents: false);
            var page = Build("Orange", disabled: disabled);
            using var scene = new Scene(page.Picker);
            Show(clock);
            Assert.Equal(53, page.Scroll.Offset.Y, 0.5);
            // No fling: this is about where it rests.
            page.Scroll.IsScrollInertiaEnabled = false;

            // Up by 40, less the recognizer's start distance: Yellow at the centre, which rests there when it's enabled.
            Drag(scene, page, -40);
            Later(clock, 600);
            if (disabled is null)
            {
                Assert.Equal(87, page.Scroll.Offset.Y, 0.5);
                Assert.Equal("Yellow", page.Column.Value);
            }
            else
            {
                // Orange's or Green's: Yellow has no snap point.
                Assert.Contains(Math.Round(page.Scroll.Offset.Y), new[] { 53.0, 121.0 });
                Assert.NotEqual("Yellow", page.Column.Value);
            }
        }
    });

    // ── C5: a click ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void C5_A_Click_On_An_Option_Sets_The_Value_At_Once_Then_Scrolls_It_To_The_Centre() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("9", Numbers);
        using var scene = new Scene(page.Picker);
        Show(clock);

        Click(scene, Centre(page.Scroll, scene) + new Point(0, 68));
        Assert.Equal(["11"], page.Changes);
        Assert.Equal("11", page.Column.Value);
        var offset = page.Scroll.Offset.Y;
        Later(clock, 600);
        Assert.True(page.Scroll.Offset.Y > offset);
        Assert.Equal(19 + 34 * 10, page.Scroll.Offset.Y, 0.5);
        // The scroll it makes comes to rest on the same value: no second event.
        Assert.Single(page.Changes);
    });

    // ── C6, C7: disabled ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void C6_A_Disabled_Column_Takes_No_Wheel_Click_Key_Or_Focus_And_Shows_Its_Disabled_Value_Active() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("Yellow", disabled: "Yellow");
        page.Column.IsEnabled = false;
        using var scene = new Scene(page.Picker);
        Show(clock);

        // Measured: the disabled column's disabled option with its value is active, at the centre, and every option is at 0.4.
        Assert.Equal("Yellow", page.Active);
        Assert.Equal(87, page.Scroll.Offset.Y, 0.5);
        Assert.All(page.Column.Options, o => Assert.Equal(0.4, o.Opacity));

        scene.Window.MouseWheel(Centre(page.Scroll, scene), new Vector(0, -1), RawInputModifiers.None);
        Click(scene, Centre(page.Scroll, scene) + new Point(0, 34));
        page.Column.Focus();
        Key(scene, PhysicalKey.ArrowDown);
        Later(clock, 600);

        Assert.Equal(87, page.Scroll.Offset.Y, 0.5);
        Assert.False(page.Column.IsFocused);
        Assert.Empty(page.Changes);
        Assert.Equal("Yellow", page.Column.Value);
    });

    [AvaloniaFact]
    public void C7_A_Disabled_Option_Is_Faded_Skipped_By_The_Keys_And_Not_Active_For_Its_Value() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("Orange", disabled: "Yellow");
        using var scene = new Scene(page.Picker);
        Show(clock);
        Assert.Equal(0.4, page.Option("Yellow").Opacity);
        Assert.Equal(1, page.Option("Orange").Opacity);

        page.Column.Focus();
        Key(scene, PhysicalKey.ArrowDown);
        Assert.Equal(["Green"], page.Changes);
    });

    // ── C8: keys ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void C8_The_Keys_Move_One_Five_Or_To_Either_End_As_Measured() => InstalledTheme.Run(_ =>
    {
        var mobile = AvonicPickerColumn.IsMobile;
        try
        {
            AvonicPickerColumn.IsMobile = () => false;
            using var clock = new TestClock();
            var page = Build("11", Numbers);
            using var scene = new Scene(page.Picker);
            Show(clock);
            page.Column.Focus();

            // Measured in Chromium on a desktop: 11 → ↓ 12 → ↑ 11 → PgUp 6 → Home 1 → End 12 → PgUp 7, each with ionChange.
            foreach (var key in new[] { PhysicalKey.ArrowDown, PhysicalKey.ArrowUp, PhysicalKey.PageUp, PhysicalKey.Home, PhysicalKey.End, PhysicalKey.PageUp })
            {
                Key(scene, key);
                Later(clock, 400);
            }

            Assert.Equal(["12", "11", "6", "1", "12", "7"], page.Changes);
            Key(scene, PhysicalKey.PageDown);
            Assert.Equal("12", page.Column.Value);
        }
        finally
        {
            AvonicPickerColumn.IsMobile = mobile;
        }
    });

    [AvaloniaFact]
    public void C8_On_A_Mobile_Platform_The_Arrows_And_Pages_Are_Reversed() => InstalledTheme.Run(_ =>
    {
        var mobile = AvonicPickerColumn.IsMobile;
        try
        {
            AvonicPickerColumn.IsMobile = () => true;
            using var clock = new TestClock();
            var page = Build("6", Numbers);
            using var scene = new Scene(page.Picker);
            Show(clock);
            page.Column.Focus();

            Key(scene, PhysicalKey.ArrowDown);
            Key(scene, PhysicalKey.ArrowUp);
            Key(scene, PhysicalKey.ArrowUp);
            Key(scene, PhysicalKey.PageDown);
            Key(scene, PhysicalKey.PageUp);
            Assert.Equal(["5", "6", "7", "2", "7"], page.Changes);
        }
        finally
        {
            AvonicPickerColumn.IsMobile = mobile;
        }
    });

    // ── C9: a value with no option ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void C9_A_Value_With_No_Option_Becomes_The_First_Options_When_Visible_But_Not_At_Load() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("zzz", ["a", "b", "c"]);
        using var scene = new Scene(page.Picker);
        Show(clock);
        Later(clock, 400);

        // Measured: at load it stays, with the first option at the centre and none active.
        Assert.Equal("zzz", page.Column.Value);
        Assert.Equal(19, page.Scroll.Offset.Y, 0.5);
        Assert.Equal("", page.Active);
        Assert.Empty(page.Changes);

        page.Column.Value = "qqq";
        Assert.Equal("qqq", page.Column.Value);
        Later(clock, 16);
        Assert.Equal("a", page.Column.Value);
        Assert.Equal(["a"], page.Changes);
    });

    // ── C10, C11: focus ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void C10_The_Focused_Column_Has_Its_Colour_At_02_Behind_Its_Options_Where_A_Pointer_Hovers() => InstalledTheme.Run(_ =>
    {
        var mobile = AvonicPickerColumn.IsMobile;
        try
        {
            foreach (var isMobile in new[] { false, true })
            {
                AvonicPickerColumn.IsMobile = () => isMobile;
                using var clock = new TestClock();
                var page = Build("Green");
                using var scene = new Scene(page.Picker);
                Show(clock);
                Assert.Null(page.Scroll.Background);

                page.Column.SetFocus();
                Dispatcher.UIThread.RunJobs();
                Assert.True(page.Column.IsFocused);
                // Measured: rgba(0, 84, 233, 0.2).
                var brush = page.Scroll.Background as ISolidColorBrush;
                Assert.Equal(isMobile ? null : Primary, brush?.Color);
                Assert.Equal(isMobile ? null : 0.2, brush?.Opacity);
            }
        }
        finally
        {
            AvonicPickerColumn.IsMobile = mobile;
        }
    });

    [AvaloniaFact]
    public void C10_The_Focused_Background_Follows_The_Columns_Color() => InstalledTheme.Run(_ =>
    {
        var mobile = AvonicPickerColumn.IsMobile;
        try
        {
            AvonicPickerColumn.IsMobile = () => false;
            using var clock = new TestClock();
            var page = Build("Green");
            page.Column.Color = "danger";
            using var scene = new Scene(page.Picker);
            Show(clock);
            page.Column.SetFocus();
            Dispatcher.UIThread.RunJobs();
            var danger = (Color)page.Column.FindResource("Avonic.Color.Danger")!;
            Assert.Equal(danger, ColorOf(page.Scroll.Background));
            Assert.Equal(0.2, page.Scroll.Background!.Opacity);
        }
        finally
        {
            AvonicPickerColumn.IsMobile = mobile;
        }
    });

    // ── C12, C13: options that come later, a column that isn't shown ─────────────────────────

    [AvaloniaFact]
    public void C12_An_Option_Added_Later_Centres_The_Column_On_Its_Value() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("late", ["a", "b"]);
        using var scene = new Scene(page.Picker);
        Show(clock);
        Assert.Equal(19, page.Scroll.Offset.Y, 0.5);

        page.Column.Items.Add(new AvonicPickerColumnOption { Value = "late", Content = "late" });
        Dispatcher.UIThread.RunJobs();
        GestureTests.Render();
        Assert.Equal(19 + 34 * 2, page.Scroll.Offset.Y, 0.5);
    });

    [AvaloniaFact]
    public void C13_A_Hidden_Column_Doesnt_Scroll_For_A_Value_And_Centres_On_It_When_Shown() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("Red");
        page.Picker.IsVisible = false;
        using var scene = new Scene(page.Picker);
        Show(clock);
        Assert.False(page.Column.IsColumnVisible);

        page.Column.Value = "Blue";
        Later(clock, 400);
        Assert.Equal(0, page.Column.Scroller?.Offset.Y ?? 0, 0.5);

        page.Picker.IsVisible = true;
        Dispatcher.UIThread.RunJobs();
        GestureTests.Render();
        Assert.True(page.Column.IsColumnVisible);
        Assert.Equal(19 + 34 * 4, page.Scroll.Offset.Y, 0.5);
        Assert.Equal("Blue", page.Active);
        // The jump into view isn't a user's scroll.
        Later(clock, 400);
        Assert.Empty(page.Changes);
    });

    // ── Color, options' color ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Color_Is_Primary_By_Default_And_The_Active_Options_Text_Is_Its_Own_Colour() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("Green");
        Assert.Equal("primary", page.Column.Color);
        Assert.Equal("primary", page.Option("Green").Color);
        page.Option("Blue").Color = "danger";
        using var scene = new Scene(page.Picker);
        Show(clock);

        page.Column.Value = "Blue";
        Later(clock, 400);
        var danger = (Color)page.Column.FindResource("Avonic.Color.Danger")!;
        Assert.Equal(danger, ColorOf(Foreground(page.Option("Blue"))));
        Assert.NotEqual(Primary, ColorOf(Foreground(page.Option("Green"))));
    });

    [AvaloniaFact]
    public void An_Apps_Style_Beats_The_Default_Color() => InstalledTheme.Run(_ =>
    {
        var page = Build("Green");
        page.Picker.Styles.Add(new Style(x => x.OfType<AvonicPickerColumnOption>())
        {
            Setters = { new Setter(AvonicPickerColumnOption.ColorProperty, "danger") },
        });
        using var scene = new Scene(page.Picker);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("danger", page.Option("Green").Color);
    });

    // ── The peers ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void The_Column_Is_A_Slider_Named_Select_A_Value_Whose_Value_Text_Is_The_Active_Options() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var page = Build("Green");
        using var scene = new Scene(page.Picker);
        Show(clock);

        var peer = ControlAutomationPeer.CreatePeerForElement(page.Column);
        Assert.Equal(AutomationControlType.Slider, peer.GetAutomationControlType());
        Assert.Equal("Select a value", peer.GetName());
        Assert.Equal("Green", ((IValueProvider)peer).Value);
        Assert.Equal(AutomationControlType.Button, ControlAutomationPeer.CreatePeerForElement(page.Option("Red")).GetAutomationControlType());
    });
}
