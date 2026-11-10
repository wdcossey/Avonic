using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-fab</c>. The numbers are from real Ionic in Chromium, at 390 × 844. See docs/components/ion-fab/PLAN.md, behaviours F1
/// to F9. Tests with <c>InstalledTheme</c> check what the theme places; the others run on the functional stub.
/// </summary>
public class AvonicFabTests
{
    internal static AvonicFab Fab(params Control[] items)
    {
        var fab = new AvonicFab();
        foreach (var item in items)
        {
            fab.Items.Add(item);
        }

        return fab;
    }

    internal static AvonicFabList List(FabListSide side, params Control[] buttons)
    {
        var list = new AvonicFabList { Side = side };
        foreach (var button in buttons)
        {
            list.Items.Add(button);
        }

        return list;
    }

    internal static Rect Box(Visual visual, Visual root) =>
        new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree"));

    internal static void AssertBox(double x, double y, double width, double height, Rect actual)
    {
        Assert.InRange(actual.X, x - 0.5, x + 0.5);
        Assert.InRange(actual.Y, y - 0.5, y + 0.5);
        Assert.InRange(actual.Width, width - 0.5, width + 0.5);
        Assert.InRange(actual.Height, height - 0.5, height + 0.5);
    }

    internal static void Tap(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    internal static void PressKey(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The fab in an <see cref="AvonicContent"/>'s fixed slot, the content filling a 390 × 844 window.</summary>
    private static (AvonicContent Content, Scene Scene) InContent(Control fixedContent, FlowDirection flow = FlowDirection.LeftToRight)
    {
        var content = new AvonicContent { FixedContent = fixedContent, FlowDirection = flow };
        var scene = new Scene(content);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();
        return (content, scene);
    }

    private static AvonicFabButton MainButton() => new() { Content = "+" };

    // ── F1: Horizontal and Vertical place it ─────────────────────────────────────────────────

    [AvaloniaFact]
    public void F1_Horizontal_And_Vertical_Place_It_Ten_In_From_The_Edges_Or_Centred() => InstalledTheme.Run(_ =>
    {
        // Measured in a 390 × 732 content: start 10, centre 167, end 324; top 10, centre 338, bottom 666, from its top. This one
        // is 844 high, so the centre is 394 and the bottom 778.
        (FabHorizontal H, FabVertical V, double X, double Y)[] cases =
        [
            (FabHorizontal.Start, FabVertical.Top, 10, 10),
            (FabHorizontal.Center, FabVertical.Center, 167, 394),
            (FabHorizontal.End, FabVertical.Bottom, 324, 778),
            (FabHorizontal.Start, FabVertical.Bottom, 10, 778),
            (FabHorizontal.End, FabVertical.Top, 324, 10),
        ];
        foreach (var (h, v, x, y) in cases)
        {
            var fab = Fab(MainButton());
            fab.Horizontal = h;
            fab.Vertical = v;
            var (content, scene) = InContent(fab);
            using (scene)
            {
                AssertBox(x, y, 56, 56, Box(fab, content));
            }
        }
    });

    [AvaloniaFact]
    public void F1_Start_And_End_Add_The_Safe_Area_On_Their_Side_And_Swap_It_In_Rtl() => InstalledTheme.Run(_ =>
    {
        foreach (var (h, flow, x) in new[]
                 {
                     (FabHorizontal.Start, FlowDirection.LeftToRight, 10 + 20.0),
                     (FabHorizontal.End, FlowDirection.LeftToRight, 390 - 56 - 10 - 30.0),

                     // RTL: start is on the right, with ion-safe-area-right; end on the left, with -left.
                     (FabHorizontal.Start, FlowDirection.RightToLeft, 390 - 56 - 10 - 30.0),
                     (FabHorizontal.End, FlowDirection.RightToLeft, 10 + 20.0),
                 })
        {
            var fab = Fab(MainButton());
            fab.Horizontal = h;
            fab.Vertical = FabVertical.Top;
            var (content, scene) = InContent(fab, flow);
            content.Resources["Avonic.SafeAreaLeft"] = 20.0;
            content.Resources["Avonic.SafeAreaRight"] = 30.0;
            content.Resources["Avonic.SafeAreaTop"] = 40.0;
            using (scene)
            {
                Dispatcher.UIThread.RunJobs();

                // top: 10px, with no safe area.
                AssertBox(x, 10, 56, 56, Box(fab, scene.Window));
            }
        }
    });

    [AvaloniaFact]
    public void F1_An_App_Local_Alignment_Still_Wins() => InstalledTheme.Run(_ =>
    {
        var fab = Fab(MainButton());
        fab.Horizontal = FabHorizontal.End;
        fab.Vertical = FabVertical.Bottom;
        fab.HorizontalAlignment = HorizontalAlignment.Left;
        var (content, scene) = InContent(fab);
        using (scene)
        {
            AssertBox(0, 778, 56, 56, Box(fab, content));
        }
    });

    // ── F2: no inset ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void F2_With_Neither_It_Is_At_The_Top_Start_As_Big_As_Its_Button() => InstalledTheme.Run(_ =>
    {
        var fab = Fab(MainButton());
        var (content, scene) = InContent(fab);
        using (scene)
        {
            AssertBox(0, 0, 56, 56, Box(fab, content));
        }
    });

    [AvaloniaFact]
    public void F2_Outside_A_Content_It_Is_Not_Stretched()
    {
        var fab = Fab(new AvonicFabButton { Width = 56, Height = 56 });
        using var scene = new Scene(new Panel { Children = { fab } });

        AssertBox(0, 0, 56, 56, Box(fab, scene.Window));
    }

    // ── F3: edge ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void F3_Edge_At_The_Top_Puts_Half_The_Button_Above_The_Content() => InstalledTheme.Run(_ =>
    {
        var main = MainButton();
        var fab = Fab(main);
        fab.Horizontal = FabHorizontal.Start;
        fab.Vertical = FabVertical.Top;
        fab.Edge = true;
        var (content, scene) = InContent(fab);
        using (scene)
        {
            // Measured: the fab at the content's top, 28 high, and the button 28 above it.
            AssertBox(10, 0, 56, 28, Box(fab, content));
            AssertBox(10, -28, 56, 56, Box(main, content));
        }
    });

    [AvaloniaFact]
    public void F3_Edge_At_The_Bottom_Puts_Half_The_Button_Below_The_Content() => InstalledTheme.Run(_ =>
    {
        var main = MainButton();
        var fab = Fab(main);
        fab.Horizontal = FabHorizontal.End;
        fab.Vertical = FabVertical.Bottom;
        fab.Edge = true;
        var (content, scene) = InContent(fab);
        using (scene)
        {
            AssertBox(324, 844 - 28, 56, 28, Box(fab, content));
            AssertBox(324, 844 - 28, 56, 56, Box(main, content));
        }
    });

    [AvaloniaFact]
    public void F3_A_Small_Button_On_An_Edge_Also_Sits_Half_Out() => InstalledTheme.Run(_ =>
    {
        // Measured: a fab at 324, 760, 56 × 28 at the bottom of a content ending at 788, its small button at 332, 768.
        var main = new AvonicFabButton { Content = "+", Size = FabButtonSize.Small };
        var fab = Fab(main);
        fab.Horizontal = FabHorizontal.End;
        fab.Vertical = FabVertical.Bottom;
        fab.Edge = true;
        var (content, scene) = InContent(fab);
        using (scene)
        {
            AssertBox(324, 816, 56, 28, Box(fab, content));
            AssertBox(332, 824, 40, 40, Box(main, content));
        }

        var top = new AvonicFabButton { Content = "+", Size = FabButtonSize.Small };
        var topFab = Fab(top);
        topFab.Horizontal = FabHorizontal.Start;
        topFab.Vertical = FabVertical.Top;
        topFab.Edge = true;
        (content, scene) = InContent(topFab);
        using (scene)
        {
            // margin-top: calc((-100% + 16px) / 2), which is -20.
            AssertBox(10, 0, 56, 28, Box(topFab, content));
            AssertBox(18, -20, 40, 40, Box(top, content));
        }
    });

    [AvaloniaFact]
    public void F3_Edge_Without_A_Top_Or_Bottom_Changes_Nothing() => InstalledTheme.Run(_ =>
    {
        var main = MainButton();
        var fab = Fab(main);
        fab.Horizontal = FabHorizontal.End;
        fab.Vertical = FabVertical.Center;
        fab.Edge = true;
        var (content, scene) = InContent(fab);
        using (scene)
        {
            AssertBox(324, 394, 56, 56, Box(fab, content));
            AssertBox(324, 394, 56, 56, Box(main, content));
        }
    });

    [AvaloniaFact]
    public void F3_Edge_Can_Change_After_It_Is_Shown() => InstalledTheme.Run(_ =>
    {
        var main = MainButton();
        var fab = Fab(main);
        fab.Horizontal = FabHorizontal.Start;
        fab.Vertical = FabVertical.Top;
        var (content, scene) = InContent(fab);
        using (scene)
        {
            fab.Edge = true;
            Dispatcher.UIThread.RunJobs();
            AssertBox(10, -28, 56, 56, Box(main, content));
        }
    });

    [AvaloniaFact]
    public void F3_Vertical_Can_Change_On_An_Edge() => InstalledTheme.Run(_ =>
    {
        var main = MainButton();
        var fab = Fab(main);
        fab.Horizontal = FabHorizontal.Start;
        fab.Vertical = FabVertical.Top;
        fab.Edge = true;
        var (content, scene) = InContent(fab);
        using (scene)
        {
            // An edge has no inset at the top or the bottom, so only the panel's own layout moves the button.
            fab.Vertical = FabVertical.Bottom;
            Dispatcher.UIThread.RunJobs();
            AssertBox(10, 844 - 28, 56, 56, Box(main, content));
        }
    });

    // ── F4: Activated reaches the button and the lists ───────────────────────────────────────

    [AvaloniaFact]
    public void F4_Activated_Sets_The_First_Button_And_Every_List()
    {
        var main = new AvonicFabButton();
        var other = new AvonicFabButton();
        var top = List(FabListSide.Top, new AvonicFabButton());
        var end = List(FabListSide.End, new AvonicFabButton());
        var fab = Fab(main, top, end, other);
        using var scene = new Scene(fab);

        fab.Activated = true;
        Assert.True(main.Activated);
        Assert.False(other.Activated);
        Assert.True(top.Activated);
        Assert.True(end.Activated);

        fab.Activated = false;
        Assert.False(main.Activated);
        Assert.False(top.Activated);
        Assert.False(end.Activated);
    }

    [AvaloniaFact]
    public void F4_Activated_Given_Before_Its_Children_Reaches_Them_When_It_Loads()
    {
        // The order XAML sets things in: the property, then the children.
        var fab = new AvonicFab { Activated = true };
        var main = new AvonicFabButton();
        var list = List(FabListSide.Top, new AvonicFabButton());
        fab.Items.Add(main);
        fab.Items.Add(list);
        Assert.False(list.Activated);

        using var scene = new Scene(fab);

        Assert.True(main.Activated);
        Assert.True(list.Activated);
    }

    [AvaloniaFact]
    public void F4_Activated_Reaches_Children_From_A_Template_Once_They_Exist()
    {
        var fab = new AvonicFab
        {
            Activated = true,
            ItemsSource = new[] { "main", "list" },
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((name, _) =>
                name == "main" ? new AvonicFabButton() : List(FabListSide.Top, new AvonicFabButton())),
        };
        using var scene = new Scene(fab);
        Dispatcher.UIThread.RunJobs();

        Assert.True(fab.GetLogicalDescendants().OfType<AvonicFabButton>().First().Activated);
        Assert.True(fab.GetLogicalDescendants().OfType<AvonicFabList>().Single().Activated);
    }

    [AvaloniaFact]
    public void F4_Coming_Back_Into_The_Tree_Does_Not_Open_A_List_The_App_Closed()
    {
        var list = List(FabListSide.Top, new AvonicFabButton());
        var fab = new AvonicFab { Activated = true, Items = { new AvonicFabButton(), list } };
        var holder = new StackPanel { Children = { fab } };
        using var scene = new Scene(holder);
        Assert.True(list.Activated);

        list.Activated = false;
        holder.Children.Remove(fab);
        holder.Children.Add(fab);

        Assert.False(list.Activated);
    }

    [AvaloniaFact]
    public void F4_Activated_With_No_List_Still_Shows_The_Close_Icon()
    {
        var main = new AvonicFabButton();
        var fab = Fab(main);
        using var scene = new Scene(fab);

        fab.Activated = true;

        Assert.True(main.Activated);
        Assert.Contains(":close-active", main.Classes);
    }

    // ── F5: a fab button's click toggles its fab ─────────────────────────────────────────────

    private static (AvonicFab Fab, AvonicFabButton Main, AvonicFabButton InList, Scene Scene) Openable()
    {
        var main = new AvonicFabButton { Width = 56, Height = 56 };
        var inList = new AvonicFabButton { Width = 40, Height = 40 };
        var fab = Fab(main, List(FabListSide.Bottom, inList));
        fab.Margin = new Thickness(100);
        var scene = new Scene(new Panel { Children = { fab } });
        return (fab, main, inList, scene);
    }

    [AvaloniaFact]
    public void F5_A_Tap_On_The_Main_Button_Toggles_The_Fab()
    {
        var (fab, main, _, scene) = Openable();
        using (scene)
        {
            Tap(scene, Box(main, scene.Window).Center);
            Assert.True(fab.Activated);

            Tap(scene, Box(main, scene.Window).Center);
            Assert.False(fab.Activated);
        }
    }

    [AvaloniaFact]
    public void F5_A_Tap_On_A_Button_In_A_List_Closes_The_Fab()
    {
        using var clock = new TestClock();
        var (fab, _, inList, scene) = Openable();
        using (scene)
        {
            fab.Activated = true;
            clock.At(100);
            Dispatcher.UIThread.RunJobs();

            Tap(scene, Box(inList, scene.Window).Center);

            Assert.False(fab.Activated);
        }
    }

    [AvaloniaFact]
    public void F5_Enter_And_Space_Toggle_It_As_A_Native_Button_Does()
    {
        var (fab, main, _, scene) = Openable();
        using (scene)
        {
            main.Focus(NavigationMethod.Tab);

            PressKey(scene, PhysicalKey.Enter);
            Assert.True(fab.Activated);

            PressKey(scene, PhysicalKey.Space);
            Assert.False(fab.Activated);
        }
    }

    [AvaloniaFact]
    public void F5_A_Fab_With_No_List_Does_Not_Toggle()
    {
        var main = new AvonicFabButton { Width = 56, Height = 56 };
        var fab = Fab(main);
        using var scene = new Scene(new Panel { Children = { fab } });

        Tap(scene, Box(main, scene.Window).Center);

        Assert.False(fab.Activated);
        Assert.False(main.Activated);
    }

    [AvaloniaFact]
    public void F5_A_Disabled_Button_Does_Not_Toggle()
    {
        var (fab, main, _, scene) = Openable();
        using (scene)
        {
            main.IsEnabled = false;
            Tap(scene, Box(main, scene.Window).Center);

            Assert.False(fab.Activated);
        }
    }

    [AvaloniaFact]
    public void F5_A_Button_Outside_A_Fab_Toggles_Nothing_And_Still_Clicks()
    {
        var clicks = 0;
        var button = new AvonicFabButton { Width = 56, Height = 56 };
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(new Panel { Children = { button } });

        Tap(scene, Box(button, scene.Window).Center);

        Assert.Equal(1, clicks);
        Assert.False(button.Activated);
    }

    [AvaloniaFact]
    public void F5_Click_Is_Raised_After_The_Fab_Toggles()
    {
        var (fab, main, _, scene) = Openable();
        using (scene)
        {
            bool? seen = null;
            main.Click += (_, _) => seen = fab.Activated;

            Tap(scene, Box(main, scene.Window).Center);

            Assert.True(seen);
        }
    }

    [AvaloniaFact]
    public void F5_The_Toggle_Reaches_A_Two_Way_Binding()
    {
        var (fab, main, _, scene) = Openable();
        using (scene)
        {
            var source = new Avalonia.Controls.Border();
            fab.Bind(AvonicFab.ActivatedProperty, new Avalonia.Data.Binding(nameof(Avalonia.Controls.Border.Tag)) { Source = source });
            source.Tag = false;

            Tap(scene, Box(main, scene.Window).Center);

            Assert.Equal(true, source.Tag);
        }
    }

    [AvaloniaFact]
    public void F5_The_Arrow_Keys_Do_Not_Move_Focus_Between_Its_Buttons()
    {
        // Two buttons in the flow, one under the other, which an ItemsControl's arrow keys would move between.
        var first = new AvonicFabButton { Width = 56, Height = 56 };
        var second = new AvonicFabButton { Width = 56, Height = 56 };
        var fab = Fab(first, second);
        using var scene = new Scene(new Panel { Children = { fab } });
        first.Focus(NavigationMethod.Tab);

        PressKey(scene, PhysicalKey.ArrowDown);

        Assert.True(first.IsFocused);
    }

    [AvaloniaFact]
    public void F5_Enter_On_A_List_Button_That_Closed_The_Fab_Does_Not_Open_It_Again()
    {
        using var clock = new TestClock();
        var clicks = 0;
        var (fab, _, inList, scene) = Openable();
        inList.Click += (_, _) => clicks++;
        using (scene)
        {
            fab.Activated = true;
            clock.At(100);
            Dispatcher.UIThread.RunJobs();
            inList.Focus(NavigationMethod.Tab);

            PressKey(scene, PhysicalKey.Enter);
            Assert.False(fab.Activated);
            Assert.Equal(1, clicks);

            // In a browser, display: none blurs the button. Here the focus stays, so the button ignores it.
            clock.At(200);
            PressKey(scene, PhysicalKey.Enter);
            Assert.False(fab.Activated);
            Assert.Equal(1, clicks);
        }
    }

    // ── F6: Close ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void F6_Close_Closes_It()
    {
        var (fab, main, _, scene) = Openable();
        using (scene)
        {
            fab.Activated = true;

            fab.Close();

            Assert.False(fab.Activated);
            Assert.False(main.Activated);
        }
    }

    // ── F7: z-index ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void F7_It_Is_Above_Its_Siblings() => InstalledTheme.Run(_ =>
    {
        var fab = Fab(MainButton());
        var after = new Border { Background = Brushes.Red, Width = 100, Height = 100, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(new Panel { Children = { fab, after } });
        scene.Window.CaptureRenderedFrame();

        Assert.Equal(999, fab.ZIndex);
        Assert.Same(fab, scene.Window.InputHitTest(new Point(28, 28)) is Visual hit ? hit.FindAncestorOfType<AvonicFab>(includeSelf: true) : null);
    });

    // ── F8: the lists sit beside the button ──────────────────────────────────────────────────

    [AvaloniaFact]
    public void F8_Each_Side_Opens_Ten_Outside_The_Button_As_Measured() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        AvonicFabButton[] tops = [new() { Content = "a" }, new() { Content = "b" }, new() { Content = "c" }];
        AvonicFabButton[] starts = [new() { Content = "s" }, new() { Content = "t" }];
        var top = List(FabListSide.Top, tops);
        var start = List(FabListSide.Start, starts);
        var end = List(FabListSide.End, new AvonicFabButton { Content = "e" });
        var bottom = List(FabListSide.Bottom, new AvonicFabButton { Content = "d" });
        var fab = Fab(MainButton(), top, start, end, bottom);
        fab.Horizontal = FabHorizontal.End;
        fab.Vertical = FabVertical.Bottom;
        var (_, scene) = InContent(fab);
        using (scene)
        {
            fab.Activated = true;
            clock.At(200);
            Dispatcher.UIThread.RunJobs();

            // Measured with the fab at 324, 534: top 324, 374, 56 × 150; start 214, 534, 100 × 56; end 390, 534; bottom 324, 600.
            AssertBox(0, -160, 56, 150, Box(top, fab));
            AssertBox(-110, 0, 100, 56, Box(start, fab));
            AssertBox(66, 0, 56, 56, Box(end, fab));
            AssertBox(0, 66, 56, 56, Box(bottom, fab));

            // The buttons: top 332, 479 / 429 / 379, from the button up; start 269, 219, from the button back.
            AssertBox(8, -55, 40, 40, Box(tops[0], fab));
            AssertBox(8, -105, 40, 40, Box(tops[1], fab));
            AssertBox(8, -155, 40, 40, Box(tops[2], fab));
            AssertBox(-55, 8, 40, 40, Box(starts[0], fab));
            AssertBox(-105, 8, 40, 40, Box(starts[1], fab));
        }
    });

    [AvaloniaFact]
    public void F8_Beside_A_Top_Edge_The_Lists_Stay_Ten_From_The_Button() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var bottom = List(FabListSide.Bottom, new AvonicFabButton());
        var end = List(FabListSide.End, new AvonicFabButton());
        var top = List(FabListSide.Top, new AvonicFabButton());
        var fab = Fab(MainButton(), bottom, end, top);
        fab.Horizontal = FabHorizontal.Start;
        fab.Vertical = FabVertical.Top;
        fab.Edge = true;
        var (_, scene) = InContent(fab);
        using (scene)
        {
            fab.Activated = true;
            clock.At(100);
            Dispatcher.UIThread.RunJobs();

            // Measured: a fab at 10, 56, its button at 28, its bottom list at 94: 38 below the fab's top. The end list is level
            // with the button, and the top list 10 above it.
            AssertBox(0, 38, 56, 56, Box(bottom, fab));
            AssertBox(66, -28, 56, 56, Box(end, fab));
            AssertBox(0, -28 - 10 - 56, 56, 56, Box(top, fab));
        }
    });

    [AvaloniaFact]
    public void F8_Beside_A_Bottom_Edge_The_Lists_Stay_Ten_From_The_Button() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var top = List(FabListSide.Top, new AvonicFabButton());
        var start = List(FabListSide.Start, new AvonicFabButton());
        var bottom = List(FabListSide.Bottom, new AvonicFabButton());
        var fab = Fab(MainButton(), top, start, bottom);
        fab.Horizontal = FabHorizontal.End;
        fab.Vertical = FabVertical.Bottom;
        fab.Edge = true;
        var (_, scene) = InContent(fab);
        using (scene)
        {
            fab.Activated = true;
            clock.At(100);
            Dispatcher.UIThread.RunJobs();

            // The button is 0 to 56 in a fab 28 high.
            AssertBox(0, -10 - 56, 56, 56, Box(top, fab));
            AssertBox(-66, 0, 56, 56, Box(start, fab));
            AssertBox(0, 66, 56, 56, Box(bottom, fab));
        }
    });

    [AvaloniaFact]
    public void F8_A_List_Moves_When_Its_Side_Changes() => InstalledTheme.Run(_ =>
    {
        var list = List(FabListSide.Bottom, new AvonicFabButton());
        var fab = Fab(MainButton(), list);
        fab.Activated = true;
        fab.Horizontal = FabHorizontal.Center;
        fab.Vertical = FabVertical.Center;
        var (_, scene) = InContent(fab);
        using (scene)
        {
            list.Side = FabListSide.End;
            Dispatcher.UIThread.RunJobs();

            AssertBox(66, 0, 56, 56, Box(list, fab));
        }
    });

    [AvaloniaFact]
    public void F8_The_Lists_Do_Not_Count_In_The_Fab_Size() => InstalledTheme.Run(_ =>
    {
        var fab = Fab(MainButton(), List(FabListSide.Bottom, new AvonicFabButton(), new AvonicFabButton()));
        fab.Activated = true;
        var (content, scene) = InContent(fab);
        using (scene)
        {
            AssertBox(0, 0, 56, 56, Box(fab, content));
        }
    });

    [AvaloniaFact]
    public void F8_A_List_From_A_Template_Is_Placed_Beside_The_Button_Too() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var fab = new AvonicFab
        {
            ItemsSource = new object[] { "main", "list" },
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((name, _) =>
                name == "main" ? new AvonicFabButton { Content = "+" } : List(FabListSide.End, new AvonicFabButton { Content = "a" })),
        };
        fab.Horizontal = FabHorizontal.Start;
        fab.Vertical = FabVertical.Top;
        var (content, scene) = InContent(fab);
        using (scene)
        {
            fab.Activated = true;
            clock.At(100);
            Dispatcher.UIThread.RunJobs();

            var list = fab.GetVisualDescendants().OfType<AvonicFabList>().Single();
            AssertBox(10, 10, 56, 56, Box(fab, content));
            AssertBox(66, 0, 56, 56, Box(list, fab));
        }
    });

    [AvaloniaFact]
    public void F8_A_List_With_Its_Own_Margin_Moves_When_Its_Side_Changes() => InstalledTheme.Run(_ =>
    {
        var list = List(FabListSide.Bottom, new AvonicFabButton());
        list.Margin = new Thickness(4);
        var fab = Fab(MainButton(), list);
        fab.Activated = true;
        fab.Horizontal = FabHorizontal.Center;
        fab.Vertical = FabVertical.Center;
        var (_, scene) = InContent(fab);
        using (scene)
        {
            list.Side = FabListSide.Top;
            Dispatcher.UIThread.RunJobs();

            // Its margin box ends at the fab's top, as bottom: 0 with a margin of 100%, and its own margin is 4 all round.
            AssertBox(4, -60, 56, 56, Box(list, fab));
        }
    });

    [AvaloniaFact]
    public void F8_A_List_From_A_Template_Moves_When_Its_Side_Changes() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        AvonicFabList? list = null;
        var fab = new AvonicFab
        {
            ItemsSource = new object[] { "main", "list" },
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((name, _) =>
                name == "main" ? new AvonicFabButton { Content = "+" } : list = List(FabListSide.Bottom, new AvonicFabButton { Content = "a" })),
            Horizontal = FabHorizontal.Center,
            Vertical = FabVertical.Center,
        };
        var (_, scene) = InContent(fab);
        using (scene)
        {
            fab.Activated = true;
            clock.At(100);
            Dispatcher.UIThread.RunJobs();
            AssertBox(0, 66, 56, 56, Box(list!, fab));

            list!.Side = FabListSide.Top;
            Dispatcher.UIThread.RunJobs();
            AssertBox(0, -66, 56, 56, Box(list, fab));
        }
    });

    [AvaloniaFact]
    public void F8_Another_Child_In_The_Flow_Is_As_Wide_As_The_Fab()
    {
        var bar = new Border { Height = 4 };
        var fab = Fab(new AvonicFabButton { Width = 56, Height = 56 }, bar);
        using var scene = new Scene(new Panel { Children = { fab } });

        // display: block in a fit-content box: the button's 56.
        AssertBox(0, 56, 56, 4, Box(bar, fab));
    }

    // ── F9: nothing is cut off ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void F9_The_Edge_Button_And_The_Lists_Outside_The_Fab_Are_Drawn_And_Take_Input() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var main = MainButton();
        var inList = new AvonicFabButton { Content = "x" };
        var fab = Fab(main, List(FabListSide.End, inList));
        fab.Horizontal = FabHorizontal.Start;
        fab.Vertical = FabVertical.Top;
        fab.Edge = true;
        var holder = new Border { Padding = new Thickness(0, 100, 0, 0), Child = new AvonicContent { FixedContent = fab } };
        using var scene = new Scene(holder);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();
        fab.Activated = true;
        clock.At(100);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        // The top half of the button, above the fab and the content, is drawn primary, and a press there reaches it.
        var upper = new Point(38, 100 - 14);
        Assert.Equal(Color.Parse("#0054E9"), Frames.PixelAt(frame, (int)upper.X, (int)upper.Y));
        Assert.Same(main, (scene.Window.InputHitTest(upper) as Visual)?.FindAncestorOfType<AvonicFabButton>(includeSelf: true));

        var inListCentre = Box(inList, scene.Window).Center;
        Assert.Same(inList, (scene.Window.InputHitTest(inListCentre) as Visual)?.FindAncestorOfType<AvonicFabButton>(includeSelf: true));
    });
}
