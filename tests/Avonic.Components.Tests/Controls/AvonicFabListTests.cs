using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicFabTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-fab-list</c>. The numbers are from real Ionic in Chromium. See docs/components/ion-fab/PLAN.md, behaviours L1 to L4.
/// </summary>
public class AvonicFabListTests
{
    private static AvonicFabButton[] Buttons(int count) =>
        Enumerable.Range(0, count).Select(_ => new AvonicFabButton { Width = 40, Height = 40 }).ToArray();

    // ── L1: shown only while activated ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void L1_It_Is_Not_Shown_Until_Activated()
    {
        var list = List(FabListSide.Bottom, Buttons(2));
        using var scene = new Scene(new StackPanel { Children = { list } });

        Assert.False(list.IsVisible);
        Assert.Equal(0, list.Bounds.Height);

        list.Activated = true;
        Dispatcher.UIThread.RunJobs();
        Assert.True(list.IsVisible);
        Assert.Contains(":active", list.Classes);
        Assert.Equal(80, list.Bounds.Height);

        list.Activated = false;
        Dispatcher.UIThread.RunJobs();
        Assert.False(list.IsVisible);
    }

    [AvaloniaFact]
    public void L1_An_App_Can_Still_Show_It()
    {
        var list = List(FabListSide.Bottom, Buttons(1));
        list.IsVisible = true;
        using var scene = new Scene(list);

        Assert.True(list.IsVisible);
    }

    // ── L2: the buttons appear one by one ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void L2_Activated_Shows_Its_Buttons_30_Ms_Apart()
    {
        using var clock = new TestClock();
        var buttons = Buttons(3);
        var list = List(FabListSide.Top, buttons);
        using var scene = new Scene(list);

        list.Activated = true;
        Assert.All(buttons, button => Assert.False(button.Show));

        clock.At(0);
        Assert.Equal([true, false, false], buttons.Select(b => b.Show));
        clock.At(29);
        Assert.Equal([true, false, false], buttons.Select(b => b.Show));
        clock.At(30);
        Assert.Equal([true, true, false], buttons.Select(b => b.Show));
        clock.At(60);
        Assert.Equal([true, true, true], buttons.Select(b => b.Show));
    }

    [AvaloniaFact]
    public void L2_Deactivated_Hides_Them_All_After_0_Ms()
    {
        using var clock = new TestClock();
        var buttons = Buttons(3);
        var list = List(FabListSide.Top, buttons);
        using var scene = new Scene(list);
        list.Activated = true;
        clock.At(100);

        list.Activated = false;
        Assert.All(buttons, button => Assert.True(button.Show));

        clock.At(100);
        Assert.All(buttons, button => Assert.False(button.Show));
    }

    [AvaloniaFact]
    public void L2_A_New_Change_Cancels_What_Was_Pending()
    {
        using var clock = new TestClock();
        var buttons = Buttons(3);
        var list = List(FabListSide.Top, buttons);
        using var scene = new Scene(list);

        list.Activated = true;
        clock.At(10);
        list.Activated = false;
        clock.At(200);

        Assert.All(buttons, button => Assert.False(button.Show));
    }

    [AvaloniaFact]
    public void L2_Leaving_The_Tree_Cancels_What_Was_Pending()
    {
        using var clock = new TestClock();
        var buttons = Buttons(3);
        var list = List(FabListSide.Top, buttons);
        var holder = new StackPanel { Children = { list } };
        using var scene = new Scene(holder);

        list.Activated = true;
        clock.At(0);
        holder.Children.Remove(list);
        clock.At(200);

        Assert.Equal([true, false, false], buttons.Select(b => b.Show));
    }

    [AvaloniaFact]
    public void L2_It_Shows_Fab_Buttons_At_Any_Depth()
    {
        using var clock = new TestClock();
        var nested = new AvonicFabButton();
        var list = List(FabListSide.Bottom, new Border { Child = nested });
        using var scene = new Scene(list);

        list.Activated = true;
        clock.At(0);

        Assert.True(nested.Show);
    }

    // ── L3: Side ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void L3_Each_Side_Is_A_Row_Or_Column_Centred_Across() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        foreach (var (side, firstX, firstY, secondX, secondY, width, height) in new[]
                 {
                     // A column of 50s for the bottom, from the bottom up for the top: margins 5 along the list.
                     (FabListSide.Bottom, 8.0, 5.0, 8.0, 55.0, 56.0, 100.0),
                     (FabListSide.Top, 8.0, 55.0, 8.0, 5.0, 56.0, 100.0),

                     // A row from the end for the start, measured 269 then 219 in a list at 214.
                     (FabListSide.Start, 55.0, 8.0, 5.0, 8.0, 100.0, 56.0),
                     (FabListSide.End, 5.0, 8.0, 55.0, 8.0, 100.0, 56.0),
                 })
        {
            var buttons = new[] { new AvonicFabButton { Content = "a" }, new AvonicFabButton { Content = "b" } };
            var list = List(side, buttons);
            list.Activated = true;
            list.Margin = default;
            var holder = new Panel { Margin = new Thickness(100), Children = { list } };
            list.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            list.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            using var scene = new Scene(holder);
            clock.At(clock.Now.TotalMilliseconds + 100);
            Dispatcher.UIThread.RunJobs();

            AssertBox(0, 0, width, height, Box(list, holder));
            AssertBox(firstX, firstY, 40, 40, Box(buttons[0], list));
            AssertBox(secondX, secondY, 40, 40, Box(buttons[1], list));
        }
    });

    [AvaloniaFact]
    public void L3_A_Short_List_Is_At_Least_56_Square_With_Its_Button_Centred_Across() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        // Measured: a bottom list of one at 324, 600, 56 × 56, its button at 332, 605.
        var button = new AvonicFabButton { Content = "d" };
        var list = List(FabListSide.Bottom, button);
        list.Activated = true;
        list.Margin = default;
        list.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        list.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        using var scene = new Scene(list);
        clock.At(100);
        Dispatcher.UIThread.RunJobs();

        AssertBox(0, 0, 56, 56, Box(list, scene.Window));
        AssertBox(8, 5, 40, 40, Box(button, list));
    });

    [AvaloniaFact]
    public void L3_A_Button_Shadow_Is_Not_Cut_Off_At_The_List_Edge() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var list = List(FabListSide.Bottom, new AvonicFabButton { Content = "a" }, new AvonicFabButton { Content = "b" });
        list.Activated = true;
        list.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        list.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        var holder = new Panel { Margin = new Thickness(50), Children = { list } };
        using var scene = new Scene(holder);
        scene.Window.Background = Avalonia.Media.Brushes.White;
        clock.At(100);
        Dispatcher.UIThread.RunJobs();

        // The list ends 5 below the button; its shadow goes on past that, as CSS doesn't clip it.
        var under = Box(list, scene.Window);
        var frame = scene.Window.CaptureRenderedFrame()!;
        Assert.InRange(Frames.PixelAt(frame, (int)under.Center.X, (int)under.Bottom).R, 0, 250);
    });

    // ── L4: Tab follows the list's order ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void L4_Tab_Follows_The_Order_Of_The_Buttons_Not_Of_The_Screen()
    {
        using var clock = new TestClock();
        var main = new AvonicFabButton { Width = 56, Height = 56 };
        var tops = Buttons(3);
        var starts = Buttons(1);
        var fab = Fab(main, List(FabListSide.Top, tops), List(FabListSide.Start, starts));
        fab.Margin = new Thickness(200);
        using var scene = new Scene(new Panel { Children = { fab } });
        fab.Activated = true;
        clock.At(100);
        Dispatcher.UIThread.RunJobs();
        main.Focus(NavigationMethod.Tab);

        var order = new List<Control>();
        for (var i = 0; i < 4; i++)
        {
            PressKey(scene, PhysicalKey.Tab);
            order.Add((Control)scene.Window.FocusManager!.GetFocusedElement()!);
        }

        // Measured: a, b, c, then the start list, though the top list runs up the screen.
        Assert.Equal([tops[0], tops[1], tops[2], starts[0]], order);
    }

    [AvaloniaFact]
    public void L4_The_Arrow_Keys_Do_Not_Move_Focus()
    {
        using var clock = new TestClock();
        var buttons = Buttons(2);
        var list = List(FabListSide.Bottom, buttons);
        using var scene = new Scene(list);
        list.Activated = true;
        clock.At(100);
        Dispatcher.UIThread.RunJobs();
        buttons[0].Focus(NavigationMethod.Tab);

        PressKey(scene, PhysicalKey.ArrowDown);

        Assert.True(buttons[0].IsFocused);
    }
}
