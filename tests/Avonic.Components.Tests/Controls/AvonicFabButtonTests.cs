using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicFabTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-fab-button</c>. The numbers are from real Ionic in Chromium. See docs/components/ion-fab/PLAN.md, behaviours B1 to B15.
/// What <c>NativeButton</c> does for a press is tested on <c>AvonicButton</c>; these check that the fab button has it.
/// </summary>
public class AvonicFabButtonTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Light = Color.Parse("#F4F5F8");

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static Border Native(AvonicFabButton button) => Part<Border>(button, "PART_Native");
    private static Border Overlay(AvonicFabButton button) => Part<Border>(button, "PART_Overlay");
    private static AvonicIcon CloseIcon(AvonicFabButton button) => Part<AvonicIcon>(button, "PART_CloseIcon");
    private static ContentPresenter Inner(AvonicFabButton button) => Part<ContentPresenter>(button, "PART_ContentPresenter");

    private static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    /// <summary>A brush's colour with its opacity, as CSS's rgba gives it.</summary>
    private static Color Rgba(IBrush? brush)
    {
        var solid = Assert.IsAssignableFrom<ISolidColorBrush>(brush);
        return Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B);
    }

    private static Color TextColour(AvonicFabButton button) => Rgba(Native(button).GetValue(TextElement.ForegroundProperty));

    private static void AssertTransform(string expected, ITransform? actual)
    {
        var expectedMatrix = TransformOperations.Parse(expected).Value;
        var actualMatrix = Assert.IsAssignableFrom<ITransform>(actual).Value;
        Assert.Equal(expectedMatrix.M11, actualMatrix.M11, 3);
        Assert.Equal(expectedMatrix.M12, actualMatrix.M12, 3);
        Assert.Equal(expectedMatrix.M21, actualMatrix.M21, 3);
        Assert.Equal(expectedMatrix.M22, actualMatrix.M22, 3);
    }

    /// <summary>Transitions run on real time, so a test of a state's end value turns them off.</summary>
    private static void NoTransitions(Control holder)
    {
        holder.Resources["Avonic.FabButton.Transition"] = new Transitions();
        holder.Resources["Avonic.FabButton.Md.CloseTransition"] = new Transitions();
    }

    /// <summary>A button at a known place in a themed window, with room around it for its shadow.</summary>
    private static (AvonicFabButton Button, Scene Scene) Themed(AvonicFabButton? button = null, bool transitions = false)
    {
        button ??= new AvonicFabButton { Content = new AvonicIcon { IconName = "add" } };
        var holder = new StackPanel { Margin = new Thickness(20), Children = { button } };
        if (!transitions)
        {
            NoTransitions(holder);
        }

        var scene = new Scene(holder);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();
        return (button, scene);
    }

    /// <summary>A button in an open list, its buttons all shown.</summary>
    private static (AvonicFabButton Button, AvonicFabList List, Scene Scene) InList(AvonicFabButton? button = null, FabListSide side = FabListSide.Bottom)
    {
        button ??= new AvonicFabButton { Content = new AvonicIcon { IconName = "add" } };
        var list = List(side, button);
        var fab = Fab(new AvonicFabButton(), list);
        fab.Margin = new Thickness(100);
        NoTransitions(fab);
        var scene = new Scene(new Panel { Children = { fab } });
        scene.Window.Background = Brushes.White;
        fab.Activated = true;
        Dispatcher.UIThread.RunJobs();
        return (button, list, scene);
    }

    private sealed class TestCommand : ICommand
    {
        public int Executed { get; private set; }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => Executed++;
    }

    // ── B1: the standard button ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_A_Round_Primary_56_Square_With_A_Shadow_And_A_24_Icon() => InstalledTheme.Run(_ =>
    {
        var icon = new AvonicIcon { IconName = "add" };
        var (button, scene) = Themed(new AvonicFabButton { Content = icon });
        using (scene)
        {
            Assert.Equal(new Size(56, 56), button.Bounds.Size);
            Assert.Equal(new CornerRadius(28), Native(button).CornerRadius);
            Assert.Equal(Primary, Solid(Native(button).Background));
            Assert.Equal(Colors.White, TextColour(button));
            Assert.Equal(3, Native(button).BoxShadow.Count);
            Assert.Equal(6, Native(button).BoxShadow[1].OffsetY);
            Assert.Equal(14, button.FontSize);
            Assert.Equal(24, icon.FontSize);

            // Measured: the icon at 16, 16 in the button, and drawn in the content's colour.
            AssertBox(16, 16, 24, 24, Box(icon, button));
            Assert.Equal(Colors.White, Solid(icon.Foreground));

            // The shadow is drawn: Chromium reads 182 just under the button, at 1x here about 160 to 180.
            var frame = scene.Window.CaptureRenderedFrame()!;
            var under = Box(button, scene.Window);
            Assert.InRange(Frames.PixelAt(frame, (int)under.Center.X, (int)under.Bottom).R, 140, 200);
        }
    });

    [AvaloniaFact]
    public void B1_Text_Is_Centred_On_One_Line() => InstalledTheme.Run(_ =>
    {
        var (button, scene) = Themed(new AvonicFabButton { Content = "Fab" });
        using (scene)
        {
            var text = button.GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal(TextWrapping.NoWrap, text.TextWrapping);
            var box = Box(text, button);
            Assert.Equal(28, box.Center.X, 0);
            Assert.Equal(28, box.Center.Y, 0);
        }
    });

    [AvaloniaFact]
    public void B1_An_App_Variable_Wins() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicFabButton { Content = "x" };
        button.Resources["Avonic.FabButton.Background"] = new SolidColorBrush(Colors.Green);
        button.Resources["Avonic.FabButton.BorderRadius"] = "8";
        var (_, scene) = Themed(button);
        using (scene)
        {
            Assert.Equal(Colors.Green, Solid(Native(button).Background));
            Assert.Equal(new CornerRadius(8), Native(button).CornerRadius);
        }
    });

    // ── B2: small ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Small_Is_40_Square_With_8_Margins() => InstalledTheme.Run(_ =>
    {
        var (button, scene) = Themed(new AvonicFabButton { Content = "x", Size = FabButtonSize.Small });
        using (scene)
        {
            Assert.Equal(new Size(40, 40), button.Bounds.Size);
            Assert.Equal(new Thickness(8), button.Margin);
            Assert.Contains(":size-small", button.Classes);
        }
    });

    // ── B3: the close icon ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Activated_Turns_The_Close_Icon_In_And_The_Content_Out() => InstalledTheme.Run(_ =>
    {
        var (button, scene) = Themed();
        using (scene)
        {
            AssertTransform("scale(0.4) rotate(-45deg)", CloseIcon(button).RenderTransform);
            Assert.Equal(0, CloseIcon(button).Opacity);
            AssertTransform("scale(1)", Inner(button).RenderTransform);
            Assert.Equal(1, Inner(button).Opacity);

            button.Activated = true;
            Dispatcher.UIThread.RunJobs();

            AssertTransform("scale(1)", CloseIcon(button).RenderTransform);
            Assert.Equal(1, CloseIcon(button).Opacity);
            AssertTransform("scale(0.4) rotate(45deg)", Inner(button).RenderTransform);
            Assert.Equal(0, Inner(button).Opacity);

            // 24 and centred: measured at 16, 16 untransformed.
            Assert.Equal(24, CloseIcon(button).FontSize);
            AssertBox(16, 16, 24, 24, new Rect(CloseIcon(button).Bounds.Size).Translate(CloseIcon(button).Bounds.Position));
        }
    });

    [AvaloniaFact]
    public void B3_It_Eases_Over_300_Ms() => InstalledTheme.Run(_ =>
    {
        var (button, scene) = Themed(transitions: true);
        using (scene)
        {
            Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.Single(CloseIcon(button).Transitions!.OfType<DoubleTransition>()).Duration);
            Assert.Equal(TimeSpan.FromMilliseconds(300), Assert.Single(Inner(button).Transitions!.OfType<TransformOperationsTransition>()).Duration);

            button.Activated = true;
            Dispatcher.UIThread.RunJobs();

            // At the start of the transition, nothing has moved yet.
            Assert.True(CloseIcon(button).Opacity < 0.5, $"close icon at {CloseIcon(button).Opacity}");
            Assert.True(Inner(button).Opacity > 0.5, $"content at {Inner(button).Opacity}");
        }
    });

    [AvaloniaFact]
    public void B3_The_Close_Icon_Size_Is_A_Variable() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicFabButton();
        button.Resources["Avonic.FabButton.CloseIconFontSize"] = 30.0;
        var (_, scene) = Themed(button);
        using (scene)
        {
            Assert.Equal(30, CloseIcon(button).FontSize);
        }
    });

    // ── B4: CloseIcon ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_CloseIcon_Names_The_Close_Icon()
    {
        var button = new AvonicFabButton();
        using var scene = new Scene(button);
        Assert.Equal("close", CloseIcon(button).Icon);

        button.CloseIcon = "arrow-up";

        Assert.Equal("arrow-up", CloseIcon(button).Icon);
    }

    // ── B5: a native button ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_A_Tap_Enter_And_Space_Click_And_Run_The_Command()
    {
        var clicks = 0;
        var command = new TestCommand();
        var button = new AvonicFabButton { Width = 56, Height = 56, Command = command };
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(new Panel { Children = { button } });

        Tap(scene, Box(button, scene.Window).Center);
        button.Focus(NavigationMethod.Tab);
        PressKey(scene, PhysicalKey.Enter);
        PressKey(scene, PhysicalKey.Space);

        Assert.Equal(3, clicks);
        Assert.Equal(3, command.Executed);
    }

    [AvaloniaFact]
    public void B5_It_Is_A_Tab_Stop()
    {
        var before = new Button();
        var button = new AvonicFabButton { Width = 56, Height = 56 };
        using var scene = new Scene(new StackPanel { Children = { before, button } });
        before.Focus();

        PressKey(scene, PhysicalKey.Tab);

        Assert.True(button.IsFocused);
    }

    // ── B6: IonFocus and IonBlur ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_IonFocus_And_IonBlur()
    {
        var events = new List<string>();
        var button = new AvonicFabButton { Width = 56, Height = 56 };
        var other = new Button();
        button.IonFocus += (_, _) => events.Add("focus");
        button.IonBlur += (_, _) => events.Add("blur");
        using var scene = new Scene(new StackPanel { Children = { button, other } });

        button.Focus();
        other.Focus();
        button.Focus();

        Assert.Equal(["focus", "blur", "focus"], events);
    }

    // ── B7, B8, B9: focus, hover, press ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_A_Keyboard_Focus_Shows_The_Overlay_At_024_Of_The_Current_Colour() => InstalledTheme.Run(_ =>
    {
        var (button, scene) = Themed();
        using (scene)
        {
            button.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(":ion-focused", button.Classes);
            Assert.Equal(Colors.White, Solid(Overlay(button).Background));
            Assert.Equal(0.24, Overlay(button).Opacity, 3);
            Assert.Null(button.FocusAdorner);
        }
    });

    [AvaloniaFact]
    public void B8_A_Mouse_Over_It_Shows_The_Overlay_At_008() => InstalledTheme.Run(_ =>
    {
        var (button, scene) = Themed();
        using (scene)
        {
            scene.Window.MouseMove(Box(button, scene.Window).Center, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Colors.White, Solid(Overlay(button).Background));
            Assert.Equal(0.08, Overlay(button).Opacity, 3);
        }
    });

    [AvaloniaFact]
    public void B9_A_Press_Deepens_The_Shadow_And_Hides_The_Overlay() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (button, scene) = Themed();
        using (scene)
        {
            var centre = Box(button, scene.Window).Center;
            scene.Window.MouseDown(centre, MouseButton.Left, RawInputModifiers.None);
            clock.At(200);
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(":activated", button.Classes);
            Assert.Equal(12, Native(button).BoxShadow[1].OffsetY);
            Assert.Equal(0, Overlay(button).Opacity);
            Assert.Contains(Part<AvonicRippleEffect>(button, "PART_Ripple").GetVisualDescendants(), _ => true);

            scene.Window.MouseUp(centre, MouseButton.Left, RawInputModifiers.None);
        }
    });

    // ── B10: Color ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Color_Is_The_Base_Behind_And_The_Contrast_In_Front_In_Every_State() => InstalledTheme.Run(_ =>
    {
        var (button, scene) = Themed(new AvonicFabButton { Content = "x", Color = "warning" });
        using (scene)
        {
            Assert.Equal(Color.Parse("#FFC409"), Solid(Native(button).Background));
            Assert.Equal(Colors.Black, TextColour(button));

            button.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Colors.Black, TextColour(button));
            Assert.Equal(Colors.Black, Solid(Overlay(button).Background));
        }
    });

    [AvaloniaFact]
    public void B10_Color_In_A_List_Beats_The_List_Colours() => InstalledTheme.Run(_ =>
    {
        var (button, _, scene) = InList(new AvonicFabButton { Content = "x", Color = "danger" });
        using (scene)
        {
            Assert.Equal(Color.Parse("#C5000F"), Solid(Native(button).Background));
            Assert.Equal(Colors.White, TextColour(button));
        }
    });

    // ── B11: disabled ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Disabled_Is_Half_Opaque_And_Takes_No_Input() => InstalledTheme.Run(_ =>
    {
        var clicks = 0;
        var (button, scene) = Themed(new AvonicFabButton { Content = "x", IsEnabled = false });
        button.Click += (_, _) => clicks++;
        using (scene)
        {
            Assert.Equal(0.5, button.Opacity);

            Tap(scene, Box(button, scene.Window).Center);
            Assert.Equal(0, clicks);
        }
    });

    [AvaloniaFact]
    public void B11_Disabled_In_A_List_And_Shown_Is_Opaque_As_Measured() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (button, _, scene) = InList(new AvonicFabButton { Content = "x", IsEnabled = false });
        using (scene)
        {
            clock.At(100);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, button.Opacity);
        }
    });

    // ── B12, B13, B14: in a list ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_In_A_List_It_Is_40_Square_Light_With_Grey_Content_And_An_18_Icon() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var icon = new AvonicIcon { IconName = "heart" };
        var (button, _, scene) = InList(new AvonicFabButton { Content = icon });
        using (scene)
        {
            clock.At(100);
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(":in-list", button.Classes);
            Assert.Equal(new Size(40, 40), button.Bounds.Size);
            Assert.Equal(Light, Solid(Native(button).Background));
            Assert.Equal(Color.FromArgb(138, 0, 0, 0), TextColour(button));
            Assert.Equal(18, icon.FontSize);
        }
    });

    [AvaloniaFact]
    public void B12_In_A_List_Focus_Is_The_Light_Shade_And_Hover_The_Light_Tint() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var (button, _, scene) = InList();
        using (scene)
        {
            clock.At(100);
            Dispatcher.UIThread.RunJobs();

            scene.Window.MouseMove(Box(button, scene.Window).Center, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Color.Parse("#F5F6F9"), Solid(Overlay(button).Background));

            // Focus comes after hover in the file, so it wins.
            button.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Color.Parse("#D7D8DA"), Solid(Overlay(button).Background));
            Assert.Equal(Color.FromArgb(138, 0, 0, 0), TextColour(button));
        }
    });

    [AvaloniaFact]
    public void B12_In_A_List_It_Is_Hidden_Until_Shown_And_Then_Appears_At_Once() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var button = new AvonicFabButton { Content = "x" };
        var list = List(FabListSide.Bottom, button);
        list.Activated = true;
        var holder = new Panel { Margin = new Thickness(50), Children = { list } };
        using var scene = new Scene(holder);
        Dispatcher.UIThread.RunJobs();

        Assert.False(button.Show);
        Assert.Equal(0, button.Opacity);
        AssertTransform("scale(0)", button.RenderTransform);
        Assert.False(button.IsHitTestVisible);
        Assert.False(button.Focusable);
        Assert.Null(button.Transitions);

        button.Show = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, button.Opacity);
        AssertTransform("scale(1)", button.RenderTransform);
        Assert.True(button.IsHitTestVisible);
        Assert.True(button.Focusable);
    });

    [AvaloniaFact]
    public void B12_A_Hidden_Button_In_A_List_Takes_No_Tap_And_No_Tab()
    {
        using var clock = new TestClock();
        var clicks = 0;
        var before = new Button();
        var button = new AvonicFabButton { Width = 40, Height = 40 };
        button.Click += (_, _) => clicks++;
        var list = List(FabListSide.Bottom, button);
        list.Activated = true;
        using var scene = new Scene(new StackPanel { Children = { before, list } });
        Dispatcher.UIThread.RunJobs();
        before.Focus();

        Tap(scene, Box(button, scene.Window).Center);
        PressKey(scene, PhysicalKey.Tab);

        Assert.Equal(0, clicks);
        Assert.False(button.IsFocused);
    }

    [AvaloniaFact]
    public void B13_Show_Outside_A_List_Does_Nothing() => InstalledTheme.Run(_ =>
    {
        var (button, scene) = Themed(new AvonicFabButton { Content = "x", Show = true });
        using (scene)
        {
            Assert.DoesNotContain(":in-list", button.Classes);
            Assert.Equal(new Size(56, 56), button.Bounds.Size);
            Assert.Equal(1, button.Opacity);
            Assert.True(button.IsHitTestVisible);
        }
    });

    [AvaloniaFact]
    public void B14_In_A_List_Its_Margin_Is_5_Along_The_List() => InstalledTheme.Run(_ =>
    {
        var (button, list, scene) = InList(side: FabListSide.Top);
        using (scene)
        {
            Assert.Equal(new Thickness(0, 5), button.Margin);

            list.Side = FabListSide.Start;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new Thickness(5, 0), button.Margin);
        }
    });

    // ── B15: automation ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_It_Is_A_Button_Named_By_Its_Text_And_Invoked_As_A_Click()
    {
        var clicks = 0;
        var button = new AvonicFabButton { Content = "Share" };
        button.Click += (_, _) => clicks++;
        var icon = new AvonicFabButton { Content = new AvonicIcon { IconName = "add" } };
        using var scene = new Scene(new StackPanel { Children = { button, icon } });

        var peer = ControlAutomationPeer.CreatePeerForElement(button);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("Share", peer.GetName());
        Assert.Equal(string.Empty, ControlAutomationPeer.CreatePeerForElement(icon).GetName() ?? string.Empty);

        ((IInvokeProvider)peer).Invoke();
        Assert.Equal(1, clicks);

        button.IsEnabled = false;
        ((IInvokeProvider)peer).Invoke();
        Assert.Equal(1, clicks);
        Assert.False(peer.IsEnabled());
    }

    [AvaloniaFact]
    public void B15_The_Close_Icon_Is_Not_Read_Out()
    {
        var button = new AvonicFabButton { Content = "Share", Activated = true };
        using var scene = new Scene(button);

        Assert.Equal("Share", ControlAutomationPeer.CreatePeerForElement(button).GetName());
    }

    [AvaloniaFact]
    public void Click_Bubbles()
    {
        var bubbled = 0;
        var button = new AvonicFabButton { Width = 56, Height = 56 };
        var parent = new Panel { Children = { button } };
        parent.AddHandler(AvonicFabButton.ClickEvent, (_, _) => bubbled++);
        using var scene = new Scene(parent);

        Tap(scene, Box(button, scene.Window).Center);

        Assert.Equal(1, bubbled);
    }
}
