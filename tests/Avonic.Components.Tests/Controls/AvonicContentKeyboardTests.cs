using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// A browser's keyboard scrolling, an extension: the numbers are from Chromium 141 in Android emulation, on ion-content in ion-app.
/// See "Keyboard scrolling and the scroll indicator" in docs/components/ion-content/PLAN.md, whose numbers these tests have.
/// </summary>
public class AvonicContentKeyboardTests
{
    private static readonly CubicBezierEase Ease = new();

    /// <summary>The curve the tests check against: CSS's ease-in-out, solved independently of the control's.</summary>
    private sealed class CubicBezierEase
    {
        public double At(double x)
        {
            // cubic-bezier(0.42, 0, 0.58, 1) by bisection on the parameter.
            double Bx(double t) => 3 * (1 - t) * (1 - t) * t * 0.42 + 3 * (1 - t) * t * t * 0.58 + t * t * t;
            double By(double t) => 3 * (1 - t) * t * t + t * t * t;
            if (x <= 0) return 0;
            if (x >= 1) return 1;
            double low = 0, high = 1;
            for (var i = 0; i < 60; i++)
            {
                var mid = (low + high) / 2;
                if (Bx(mid) < x) low = mid; else high = mid;
            }

            return By((low + high) / 2);
        }
    }

    private static ScrollViewer Scroll(AvonicContent content) =>
        content.GetVisualDescendants().OfType<ScrollViewer>().First(control => control.Name == "PART_Scroll");

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Press(Scene scene, PhysicalKey key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        scene.Window.KeyPressQwerty(key, modifiers);
        Settle();
    }

    private static Panel Box(Control child, double height = 300) =>
        new() { Height = height, VerticalAlignment = VerticalAlignment.Top, Children = { child } };

    private static AvonicContent Tall(double height = 3000, Control? first = null)
    {
        var stack = new StackPanel();
        if (first is not null)
        {
            stack.Children.Add(first);
        }

        stack.Children.Add(new Border { Height = height });
        return new AvonicContent { Content = stack };
    }

    /// <summary>A content in a 300 high box, then a button after it, with the scroller focused as Tab does.</summary>
    private static Scene Focused(AvonicContent content, out Button after, double height = 300)
    {
        after = new Button { Content = "after" };
        var scene = new Scene(new StackPanel { Children = { Box(content, height), after } });
        Scroll(content).Focus(NavigationMethod.Tab);
        Settle();
        return scene;
    }

    private static Border FocusableBlock(string name) => new() { Name = name, Focusable = true, Height = 20, Background = Brushes.Transparent };

    // ── 38 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B38_Down_And_Up_Scroll_40()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.ArrowDown);
        clock.At(400);
        Assert.Equal(40, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.ArrowDown);
        clock.At(800);
        Assert.Equal(80, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.ArrowUp);
        clock.At(1200);
        Assert.Equal(40, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B38_Left_And_Right_Scroll_40_With_ScrollX_On()
    {
        using var clock = new TestClock();
        var content = new AvonicContent { ScrollX = true, Content = new Border { Width = 3000, Height = 3000 } };
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.ArrowRight);
        clock.At(400);
        Assert.Equal(new Vector(40, 0), Scroll(content).Offset);

        Press(scene, PhysicalKey.ArrowRight);
        Press(scene, PhysicalKey.ArrowLeft);
        clock.At(800);
        Assert.Equal(new Vector(40, 0), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B38_Left_And_Right_Do_Nothing_With_ScrollX_Off()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.ArrowRight);
        clock.At(400);

        Assert.Equal(new Vector(0, 0), Scroll(content).Offset);
    }

    [AvaloniaFact]
    public void B38_Left_And_Right_Are_Physical_In_RTL()
    {
        // scrollLeft -40 on ArrowLeft in Chromium: towards the end, which is Offset.X 40 here.
        using var clock = new TestClock();
        var content = new AvonicContent { ScrollX = true, FlowDirection = FlowDirection.RightToLeft, Content = new Border { Width = 3000, Height = 3000 } };
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.ArrowLeft);
        clock.At(400);
        Assert.Equal(40, Scroll(content).Offset.X);

        Press(scene, PhysicalKey.ArrowRight);
        clock.At(800);
        Assert.Equal(0, Scroll(content).Offset.X);
    }

    // ── 39 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(100, 87)]
    [InlineData(200, 175)]
    [InlineData(300, 262)]
    [InlineData(400, 350)]
    [InlineData(600, 525)]
    [InlineData(844, 738)]
    public void B39_Page_Down_Goes_87_And_A_Half_Percent_Of_The_Viewport_Rounded_Down(double viewport, double page)
    {
        using var clock = new TestClock();
        var content = Tall(5000);
        using var scene = Focused(content, out _, viewport);

        Press(scene, PhysicalKey.PageDown);
        clock.At(400);
        Assert.Equal(page, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.Space);
        clock.At(800);
        Assert.Equal(2 * page, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B39_Page_Up_And_Shift_Space_Go_Up_A_Page_And_Alt_With_An_Arrow_Pages()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = Focused(content, out _);
        Scroll(content).Offset = new Vector(0, 1000);
        Settle();

        Press(scene, PhysicalKey.PageUp);
        clock.At(400);
        Assert.Equal(738, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.Space, RawInputModifiers.Shift);
        clock.At(800);
        Assert.Equal(476, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.ArrowDown, RawInputModifiers.Alt);
        clock.At(1200);
        Assert.Equal(738, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.ArrowUp, RawInputModifiers.Alt);
        clock.At(1600);
        Assert.Equal(476, Scroll(content).Offset.Y);
    }

    // ── 40 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B40_Home_And_End_Go_To_The_Top_And_The_Bottom_With_Or_Without_Ctrl()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.End);
        clock.At(400);
        Assert.Equal(2700, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.Home);
        clock.At(800);
        Assert.Equal(0, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.End, RawInputModifiers.Control);
        clock.At(1200);
        Assert.Equal(2700, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.Home, RawInputModifiers.Control);
        clock.At(1600);
        Assert.Equal(0, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B40_Home_And_End_And_The_Page_Keys_Are_Vertical_Only()
    {
        // scroll-x with scroll-y off: End, Page Down, Space and Down do nothing, and Right still goes 40.
        using var clock = new TestClock();
        var content = new AvonicContent { ScrollX = true, ScrollY = false, Content = new Border { Width = 3000, Height = 3000 } };
        using var scene = Focused(content, out _);

        foreach (var key in new[] { PhysicalKey.End, PhysicalKey.PageDown, PhysicalKey.Space, PhysicalKey.ArrowDown })
        {
            Press(scene, key);
        }

        clock.At(400);
        Assert.Equal(new Vector(0, 0), Scroll(content).Offset);

        Press(scene, PhysicalKey.ArrowRight);
        clock.At(800);
        Assert.Equal(new Vector(40, 0), Scroll(content).Offset);
    }

    // ── 41 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(PhysicalKey.ArrowDown, RawInputModifiers.Control)]
    [InlineData(PhysicalKey.ArrowDown, RawInputModifiers.Shift)]
    [InlineData(PhysicalKey.ArrowDown, RawInputModifiers.Meta)]
    [InlineData(PhysicalKey.PageDown, RawInputModifiers.Shift)]
    [InlineData(PhysicalKey.PageDown, RawInputModifiers.Control)]
    [InlineData(PhysicalKey.PageDown, RawInputModifiers.Alt)]
    [InlineData(PhysicalKey.PageUp, RawInputModifiers.Shift)]
    [InlineData(PhysicalKey.End, RawInputModifiers.Shift)]
    [InlineData(PhysicalKey.Home, RawInputModifiers.Shift)]
    [InlineData(PhysicalKey.Space, RawInputModifiers.Control)]
    [InlineData(PhysicalKey.Space, RawInputModifiers.Alt)]
    [InlineData(PhysicalKey.Enter, RawInputModifiers.None)]
    public void B41_Other_Modifiers_Do_Nothing_And_Go_On(PhysicalKey key, RawInputModifiers modifiers)
    {
        using var clock = new TestClock();
        var content = Tall();
        var outside = 0;
        var box = Box(content);
        box.AddHandler(InputElement.KeyDownEvent, (object? _, KeyEventArgs e) => outside += e.Handled ? 0 : 1);
        using var scene = new Scene(box);
        Scroll(content).Offset = new Vector(0, 1000);
        Scroll(content).Focus(NavigationMethod.Tab);
        Settle();

        Press(scene, key, modifiers);
        clock.At(400);

        // Avalonia's own ScrollViewer would take Shift+Page Down by a whole viewport.
        Assert.Equal(1000, Scroll(content).Offset.Y);
        Assert.Equal(1, outside);
    }

    // ── 42 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B42_A_Key_Scrolls_Over_150ms_Of_Ease_In_Out_From_The_Key()
    {
        using var clock = new TestClock();
        var content = Tall(5000);
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.PageDown);
        Assert.Equal(0, Scroll(content).Offset.Y);
        foreach (var at in new[] { 16, 48, 64, 96, 144 })
        {
            clock.At(at);
            Assert.Equal(262 * Ease.At(at / 150d), Scroll(content).Offset.Y, 3);
        }

        clock.At(160);
        Assert.Equal(262, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B42_The_Curve_Is_Chromiums()
    {
        // Full headless at 240 Hz, a Page Down from 40: 136 at 63.2 ms and 148 at 67.4; 221 at 92.4 and 232 at 96.6; 288 at 125.7 and 292 at 129.8.
        using var clock = new TestClock();
        var content = Tall(5000);
        using var scene = Focused(content, out _);
        Scroll(content).Offset = new Vector(0, 40);
        Settle();

        Press(scene, PhysicalKey.PageDown);
        clock.At(64);
        var at64 = Scroll(content).Offset.Y;
        clock.At(96);
        var at96 = Scroll(content).Offset.Y;
        clock.At(128);
        var at128 = Scroll(content).Offset.Y;

        // Interpolated between Chromium's frames either side.
        Assert.InRange(at64, 136 + (148 - 136) * (0.8 / 4.2) - 1.5, 136 + (148 - 136) * (0.8 / 4.2) + 1.5);
        Assert.InRange(at96, 221 + (232 - 221) * (3.6 / 4.2) - 1.5, 221 + (232 - 221) * (3.6 / 4.2) + 1.5);
        Assert.InRange(at128, 288 + (292 - 288) * (2.3 / 4.1) - 1.5, 288 + (292 - 288) * (2.3 / 4.1) + 1.5);
    }

    [AvaloniaFact]
    public void B42_End_Takes_The_Same_150ms_Over_Any_Distance()
    {
        using var clock = new TestClock();
        var content = Tall(5000);
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.End);
        clock.At(80);
        Assert.Equal(4700 * Ease.At(80 / 150d), Scroll(content).Offset.Y, 3);
        clock.At(160);
        Assert.Equal(4700, Scroll(content).Offset.Y);
    }

    // ── 43 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B43_A_Second_Key_Adds_To_The_Target_And_Ends_150ms_After_It()
    {
        // Two Downs 57 ms apart: 80, at about 207 ms.
        using var clock = new TestClock();
        var content = Tall();
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.ArrowDown);
        clock.At(57);
        Press(scene, PhysicalKey.ArrowDown);
        clock.At(192);
        Assert.InRange(Scroll(content).Offset.Y, 78, 79.9);
        clock.At(208);
        Assert.Equal(80, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B43_Held_And_Repeated_Keys_Each_Add()
    {
        // Ten Downs 47 ms apart: 400. Two Page Downs 50 ms apart: 524.
        using var clock = new TestClock();
        var content = Tall();
        using var scene = Focused(content, out _);

        for (var i = 0; i < 10; i++)
        {
            clock.At(i * 47);
            Press(scene, PhysicalKey.ArrowDown);
        }

        clock.At(1000);
        Assert.Equal(400, Scroll(content).Offset.Y);

        Scroll(content).Offset = new Vector(0, 0);
        Settle();
        Press(scene, PhysicalKey.PageDown);
        clock.At(1050);
        Press(scene, PhysicalKey.PageDown);
        clock.At(1500);
        Assert.Equal(524, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B43_A_Change_Of_Target_Keeps_The_Speed_It_Has()
    {
        // Page Down, then Up 70 ms later: Chromium's frames, at 240 Hz, are 211 at 114.2 ms and 220 at 126.7, settling on 222 by about
        // 135. Starting again from rest would be about 144 at 128 ms.
        using var clock = new TestClock();
        var content = Tall();
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.PageDown);
        clock.At(70);
        Press(scene, PhysicalKey.ArrowUp);
        clock.At(112);
        Assert.InRange(Scroll(content).Offset.Y, 207, 212);
        clock.At(128);
        Assert.InRange(Scroll(content).Offset.Y, 218.5, 221.5);
        clock.At(400);
        Assert.Equal(222, Scroll(content).Offset.Y);
    }

    // ── 44 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B44_Something_Else_Scrolling_It_Stops_It()
    {
        using var clock = new TestClock();
        var content = Tall();
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.End);
        clock.At(48);
        Scroll(content).Offset = new Vector(0, 1000);
        clock.At(400);

        Assert.Equal(1000, Scroll(content).Offset.Y);
    }

    // ── 45 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B45_At_Its_Limit_A_Key_Goes_To_The_Content_Around()
    {
        using var clock = new TestClock();
        var inner = new AvonicContent { Content = new Border { Height = 400 } };
        var outer = new AvonicContent
        {
            Content = new StackPanel { Children = { new Border { Height = 100 }, new Panel { Height = 150, Children = { inner } }, new Border { Height = 3000 } } },
        };
        using var scene = new Scene(Box(outer));
        Scroll(inner).Focus(NavigationMethod.Tab);
        Settle();

        Press(scene, PhysicalKey.End);
        clock.At(400);
        Assert.Equal(250, Scroll(inner).Offset.Y);
        Assert.Equal(0, Scroll(outer).Offset.Y);

        Press(scene, PhysicalKey.ArrowDown);
        clock.At(800);
        Assert.Equal(40, Scroll(outer).Offset.Y);

        Press(scene, PhysicalKey.PageDown);
        clock.At(1200);
        Assert.Equal(302, Scroll(outer).Offset.Y);
        Assert.Equal(250, Scroll(inner).Offset.Y);
    }

    [AvaloniaFact]
    public void B45_An_Axis_That_Is_Off_Sends_The_Key_On()
    {
        using var clock = new TestClock();
        var target = FocusableBlock("target");
        var inner = new AvonicContent { ScrollY = false, Content = new StackPanel { Children = { target, new Border { Height = 400 } } } };
        var outer = new AvonicContent
        {
            Content = new StackPanel { Children = { new Panel { Height = 150, Children = { inner } }, new Border { Height = 3000 } } },
        };
        using var scene = new Scene(Box(outer));
        target.Focus(NavigationMethod.Tab);
        Settle();

        Press(scene, PhysicalKey.ArrowDown);
        clock.At(400);

        Assert.Equal(0, Scroll(inner).Offset.Y);
        Assert.Equal(40, Scroll(outer).Offset.Y);
    }

    // ── 46, 47 ───────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B46_Tab_Stops_On_A_Content_That_Can_Scroll_And_Shift_Tab_Comes_Back()
    {
        var before = new Button { Content = "before" };
        var content = Tall();
        var after = new Button { Content = "after" };
        using var scene = new Scene(new StackPanel { Children = { before, Box(content), after } });
        before.Focus(NavigationMethod.Tab);
        Settle();

        Press(scene, PhysicalKey.Tab);
        Assert.True(Scroll(content).IsFocused);

        Press(scene, PhysicalKey.Tab);
        Assert.True(after.IsFocused);

        Press(scene, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.True(Scroll(content).IsFocused);
    }

    [AvaloniaTheory]
    [InlineData("fits")]
    [InlineData("scroll-y off")]
    [InlineData("overscroll")]
    public void B46_Not_On_One_That_Fits_Or_With_ScrollY_Off_But_On_ForceOverscroll(string which)
    {
        var before = new Button { Content = "before" };
        var content = which switch
        {
            "fits" => Tall(100),
            "scroll-y off" => Tall(),
            _ => Tall(100),
        };
        content.ScrollY = which != "scroll-y off";
        content.ForceOverscroll = which == "overscroll";
        var after = new Button { Content = "after" };
        using var scene = new Scene(new StackPanel { Children = { before, Box(content), after } });
        before.Focus(NavigationMethod.Tab);
        Settle();

        Press(scene, PhysicalKey.Tab);

        Assert.Equal(which == "overscroll", Scroll(content).IsFocused);
        Assert.Equal(which != "overscroll", after.IsFocused);
    }

    [AvaloniaFact]
    public void B47_With_Something_Focusable_Inside_Tab_Goes_Into_It_And_Shift_Tab_Past_It()
    {
        var before = new Button { Content = "before" };
        var inside = new Button { Content = "inside" };
        var content = Tall(3000, inside);
        using var scene = new Scene(new StackPanel { Children = { before, Box(content) } });
        before.Focus(NavigationMethod.Tab);
        Settle();

        Press(scene, PhysicalKey.Tab);
        Assert.True(inside.IsFocused);

        Press(scene, PhysicalKey.Tab, RawInputModifiers.Shift);
        Assert.True(before.IsFocused);
    }

    [AvaloniaFact]
    public void B47_Nested_The_Inner_One_Is_The_Stop()
    {
        var before = new Button { Content = "before" };
        var inner = new AvonicContent { Content = new Border { Height = 1000 } };
        var outer = new AvonicContent { Content = new StackPanel { Children = { new Panel { Height = 150, Children = { inner } }, new Border { Height = 3000 } } } };
        using var scene = new Scene(new StackPanel { Children = { before, Box(outer) } });
        before.Focus(NavigationMethod.Tab);
        Settle();

        Press(scene, PhysicalKey.Tab);

        Assert.True(Scroll(inner).IsFocused);
    }

    [AvaloniaFact]
    public void B47_Nested_An_Inner_One_That_Fits_Leaves_The_Outer_As_The_Stop()
    {
        var before = new Button { Content = "before" };
        var inner = new AvonicContent { Content = new Border { Height = 50 } };
        var outer = new AvonicContent { Content = new StackPanel { Children = { new Panel { Height = 150, Children = { inner } }, new Border { Height = 3000 } } } };
        using var scene = new Scene(new StackPanel { Children = { before, Box(outer) } });
        before.Focus(NavigationMethod.Tab);
        Settle();

        Press(scene, PhysicalKey.Tab);

        Assert.True(Scroll(outer).IsFocused);
    }

    // ── 48 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void B48_A_Press_On_The_Content_Lets_The_Keys_Scroll_It(bool touch)
    {
        using var clock = new TestClock();
        var focused = new Button { Content = "elsewhere" };
        var content = Tall();
        using var scene = new Scene(new StackPanel { Children = { focused, Box(content) } });
        focused.Focus();
        Settle();

        var point = new Point(100, 150);
        if (touch)
        {
            var finger = scene.Window.TouchBegin(point, RawInputModifiers.None);
            scene.Window.TouchEnd(finger, point, RawInputModifiers.None);
        }
        else
        {
            scene.Window.MouseDown(point, MouseButton.Left);
            scene.Window.MouseUp(point, MouseButton.Left);
        }

        Settle();
        Assert.False(focused.IsFocused);

        Press(scene, PhysicalKey.ArrowDown);
        clock.At(400);
        Assert.Equal(40, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B48_A_Press_On_A_Nested_Content_Part_Way_Out_Of_View_Doesnt_Scroll_The_Outer()
    {
        // Chromium moves no focus on a press, so nothing is brought into view. Here the inner one takes focus, by pointer.
        var inner = new AvonicContent { Content = new Border { Height = 1000, Background = Brushes.Transparent } };
        var outer = new AvonicContent
        {
            Content = new StackPanel { Children = { new Border { Height = 250 }, new Panel { Height = 200, Children = { inner } }, new Border { Height = 3000 } } },
        };
        using var scene = new Scene(Box(outer));
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Settle();

        // The inner one is from 250 to 450, so only 50 of it shows.
        scene.Window.MouseDown(new Point(100, 280), MouseButton.Left);
        scene.Window.MouseUp(new Point(100, 280), MouseButton.Left);
        Settle();

        Assert.True(Scroll(inner).IsFocused);
        Assert.Equal(0, Scroll(outer).Offset.Y);

        // Tab still brings it into view, as a browser's focus does, after a press.
        var before = new Button { Content = "before" };
        ((StackPanel)outer.Content!).Children.Insert(0, before);
        Settle();
        before.Focus(NavigationMethod.Tab);
        Settle();
        Scroll(outer).Offset = new Vector(0, 0);
        Settle();
        Press(scene, PhysicalKey.Tab);

        Assert.True(Scroll(inner).IsFocused);
        Assert.True(Scroll(outer).Offset.Y > 0);
    }

    // ── 49, 50 ───────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B49_A_Focused_Button_Keeps_Space_And_The_Arrows_And_Pages_Still_Scroll()
    {
        using var clock = new TestClock();
        var spaceTaken = false;
        var button = new Button { Content = "inside" };
        button.AddHandler(InputElement.KeyDownEvent, (object? _, KeyEventArgs e) => spaceTaken |= e.Key == Key.Space && e.Handled,
            Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
        var content = Tall(3000, button);
        using var scene = new Scene(Box(content));
        button.Focus(NavigationMethod.Tab);
        Settle();

        Press(scene, PhysicalKey.Space);
        clock.At(400);
        Assert.True(spaceTaken);
        Assert.Equal(0, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.ArrowDown);
        clock.At(800);
        Assert.Equal(40, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.PageDown);
        clock.At(1200);
        Assert.Equal(302, Scroll(content).Offset.Y);
        Assert.True(button.IsFocused);
    }

    [AvaloniaFact]
    public void B50_A_Text_Field_Keeps_The_Arrows_Home_End_And_Space_And_The_Page_Keys_Scroll()
    {
        using var clock = new TestClock();
        var field = new TextBox { Text = "abc" };
        var content = Tall(3000, field);
        var outside = 0;
        var box = Box(content);
        box.AddHandler(InputElement.KeyDownEvent, (object? _, KeyEventArgs e) => outside += e.Handled ? 0 : 1);
        using var scene = new Scene(box);
        field.Focus();
        Settle();

        foreach (var key in new[] { PhysicalKey.ArrowDown, PhysicalKey.ArrowUp, PhysicalKey.End, PhysicalKey.Home, PhysicalKey.Space, PhysicalKey.ArrowRight })
        {
            Press(scene, key);
        }

        clock.At(400);
        Assert.Equal(0, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.PageDown);
        clock.At(800);
        Assert.Equal(262, Scroll(content).Offset.Y);

        Press(scene, PhysicalKey.PageUp);
        clock.At(1200);
        Assert.Equal(0, Scroll(content).Offset.Y);
    }

    [AvaloniaFact]
    public void B50_The_Page_Keys_Scroll_From_A_Text_Field_Over_A_Base_Theme_Too()
    {
        // A base theme's one-line TextBox takes Page Up and Page Down, and does nothing with them.
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            using var clock = new TestClock();
            var field = new TextBox { Text = "abc" };
            var inner = Tall(400, field);
            var outer = new AvonicContent
            {
                Content = new StackPanel { Children = { new Panel { Height = 150, Children = { inner } }, new Border { Height = 3000 } } },
            };
            using var scene = new Scene(Box(outer));
            field.Focus();
            Settle();

            // The closest content scrolls, a page of 150.
            Press(scene, PhysicalKey.PageDown);
            clock.At(400);
            Assert.Equal(131, Scroll(inner).Offset.Y);
            Assert.Equal(0, Scroll(outer).Offset.Y);

            // At its limit, the one around: its extent is the field's height and 400.
            Press(scene, PhysicalKey.PageDown);
            clock.At(800);
            Press(scene, PhysicalKey.PageDown);
            clock.At(1200);
            Assert.Equal(Scroll(inner).Extent.Height - 150, Scroll(inner).Offset.Y);
            Assert.Equal(0, Scroll(outer).Offset.Y);
            Press(scene, PhysicalKey.PageDown);
            clock.At(1600);
            Assert.Equal(262, Scroll(outer).Offset.Y);
            Assert.True(field.IsFocused);
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    [AvaloniaFact]
    public void B50_A_Multi_Line_Field_Keeps_The_Page_Keys_Over_A_Base_Theme()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            using var clock = new TestClock();
            var field = new TextBox { Text = "a\nb\nc", AcceptsReturn = true };
            var content = Tall(3000, field);
            using var scene = new Scene(Box(content));
            field.Focus();
            Settle();

            Press(scene, PhysicalKey.PageDown);
            clock.At(400);

            Assert.Equal(0, Scroll(content).Offset.Y);
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    // ── 51 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B51_The_Focus_Ring_Is_A_Browsers_And_Shows_For_Tab_Only()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var before = new Button { Content = "before" };
                var content = Tall();
                using var scene = new Scene(new StackPanel { Children = { before, Box(content) } });
                before.Focus(NavigationMethod.Tab);
                Settle();

                var ring = Scroll(content).GetVisualDescendants().OfType<Border>().First(border => border.Name == "PART_FocusRing");
                Assert.False(ring.IsVisible);

                Press(scene, PhysicalKey.Tab);
                Assert.True(Scroll(content).IsFocused);
                Assert.True(ring.IsVisible);
                Assert.Equal(new Thickness(2), ring.BorderThickness);
                Assert.Equal(new Thickness(-1), ring.Margin);
                Assert.Equal(new CornerRadius(3.5), ring.CornerRadius);
                Assert.Equal(Color.Parse("#E59700"), Assert.IsAssignableFrom<ISolidColorBrush>(ring.BorderBrush).Color);
                // Under the content, as Chromium paints the outline, so it's first in the scroller's template.
                Assert.Equal(0, ((Panel)ring.GetVisualParent()!).Children.IndexOf(ring));
                // And the base theme's adorner isn't drawn over it.
                Assert.Empty(Adorners(content));

                // A press focuses it too, with no ring.
                before.Focus(NavigationMethod.Tab);
                Settle();
                scene.Window.MouseDown(new Point(100, 150), MouseButton.Left);
                scene.Window.MouseUp(new Point(100, 150), MouseButton.Left);
                Settle();
                Assert.True(Scroll(content).IsFocused);
                Assert.False(ring.IsVisible);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    [AvaloniaFact]
    public void B51_The_Ring_Reaches_A_Pixel_Outside_And_Content_With_A_Background_Covers_Its_Inner_Half() => InstalledTheme.Run(_ =>
    {
        // Chromium: 2 px straddling the edge over a transparent child, the outer 1 px only over rows with a background.
        var rows = new StackPanel { Children = { new Border { Height = 100, Background = Brushes.White }, new Border { Height = 3000 } } };
        var content = new AvonicContent { Content = rows, Margin = new Thickness(10) };
        using var scene = new Scene(new Panel { Height = 300, VerticalAlignment = VerticalAlignment.Top, Children = { content } });
        scene.Window.Background = Brushes.White;
        Press(scene, PhysicalKey.Tab);
        Assert.True(Scroll(content).IsFocused);

        var frame = scene.Window.CaptureRenderedFrame()!;
        var orange = Color.Parse("#E59700");
        // Beside the white row, at 50: only the pixel outside.
        Assert.Equal(orange, Frames.PixelAt(frame, 9, 50));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 10, 50));
        // Beside nothing, at 200: both.
        Assert.Equal(orange, Frames.PixelAt(frame, 9, 200));
        Assert.Equal(orange, Frames.PixelAt(frame, 10, 200));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 11, 200));
    });

    /// <summary>What the adorner layer draws over the scroller.</summary>
    private static IEnumerable<Control> Adorners(AvonicContent content)
    {
        var scroll = Scroll(content);
        return AdornerLayer.GetAdornerLayer(scroll)?.Children.Where(child => AdornerLayer.GetAdornedElement(child) == scroll && child.IsVisible)
            ?? [];
    }

    // ── 52 ───────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B52_The_Scroll_Events_Report_A_Keys_Scroll()
    {
        using var clock = new TestClock();
        var content = Tall();
        content.ScrollEvents = true;
        var events = new List<string>();
        content.IonScrollStart += (_, _) => events.Add("start");
        content.IonScroll += (_, e) => events.Add($"scroll {e.ScrollTop}");
        content.IonScrollEnd += (_, _) => events.Add("end");
        using var scene = Focused(content, out _);

        Press(scene, PhysicalKey.ArrowDown);
        clock.At(1000);

        Assert.Equal("start", events[0]);
        Assert.Equal("scroll 40", events[^2]);
        Assert.Equal("end", events[^1]);
    }
}
