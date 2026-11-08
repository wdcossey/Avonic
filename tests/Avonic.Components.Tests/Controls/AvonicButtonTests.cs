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
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-button in Chromium, at 390 px wide. See docs/components/ion-button/PLAN.md.
/// Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub.
/// </summary>
public class AvonicButtonTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Danger = Color.Parse("#C5000F");
    private static readonly Point Centre = new(50, 20);

    /// <summary>On the stub, which has no size of its own, a box to press.</summary>
    private static AvonicButton Boxed(object? content = null) => new()
    {
        Content = content ?? "x",
        Width = 100,
        Height = 40,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
    };

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static Border Native(AvonicButton button) => Part<Border>(button, "PART_Native");
    private static Border Overlay(AvonicButton button) => Part<Border>(button, "PART_Overlay");
    private static ButtonInner Inner(AvonicButton button) => Part<ButtonInner>(button, "PART_Inner");
    private static AvonicRippleEffect Ripple(AvonicButton button) => Part<AvonicRippleEffect>(button, "PART_Ripple");
    private static ContentPresenter Slot(AvonicButton button, string name) => Part<ContentPresenter>(button, name);
    private static TextBlock Text(AvonicButton button) => Assert.IsType<TextBlock>(Slot(button, "PART_Content").Child);

    private static bool Has(Control control, string pseudoClass) => control.Classes.Contains(pseudoClass);

    private static Rect Box(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void AssertBox(double x, double y, double width, double height, Rect actual)
    {
        // Layout rounds to whole pixels at this scale, and Chromium keeps fractions.
        Assert.InRange(actual.X, x - 0.5, x + 0.5);
        Assert.InRange(actual.Y, y - 0.5, y + 0.5);
        Assert.InRange(actual.Width, width - 0.5, width + 0.5);
        Assert.InRange(actual.Height, height - 0.5, height + 0.5);
    }

    private static void AssertThickness(double left, double top, double right, double bottom, Thickness actual)
    {
        Assert.Equal(left, actual.Left, 2);
        Assert.Equal(top, actual.Top, 2);
        Assert.Equal(right, actual.Right, 2);
        Assert.Equal(bottom, actual.Bottom, 2);
    }

    private static void Tap(Scene scene, Point point, MouseButton button = MouseButton.Left)
    {
        scene.Window.MouseDown(point, button, RawInputModifiers.None);
        scene.Window.MouseUp(point, button, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Press(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Release(Scene scene, Point point)
    {
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Hover(Scene scene, Point point)
    {
        scene.Window.MouseMove(point, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void PressKey(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyPressQwerty(key, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    private static void ReleaseKey(Scene scene, PhysicalKey key)
    {
        scene.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Transitions run on real time, so a test of a state's end value turns them off.</summary>
    private static T NoTransitions<T>(T control) where T : Control
    {
        control.Resources["Avonic.Button.Transition"] = new Transitions();
        return control;
    }

    private static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static void AssertTransparent(IBrush? brush) => Assert.True(brush is null || Solid(brush).A == 0, $"{brush} isn't transparent");

    /// <summary>What <c>.button-native</c>'s <c>color</c> resolves to, which is what slotted content inherits.</summary>
    private static Color TextColour(AvonicButton button) => Solid(Native(button).GetValue(TextElement.ForegroundProperty));

    private static Color Pixel(Scene scene, int x, int y)
    {
        Dispatcher.UIThread.RunJobs();
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, x, y);
    }

    private static void AssertClose(Color expected, Color actual, int tolerance = 2)
    {
        Assert.InRange(actual.R, expected.R - tolerance, expected.R + tolerance);
        Assert.InRange(actual.G, expected.G - tolerance, expected.G + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
    }

    /// <summary>A button at a known place in a themed window, with room around it for its shadow.</summary>
    private static (AvonicButton Button, Scene Scene, Point Centre) Themed(AvonicButton? button = null, bool transitions = false)
    {
        button ??= new AvonicButton { Content = "Text" };
        var holder = new StackPanel { Margin = new Thickness(20), Children = { button } };
        if (!transitions)
        {
            NoTransitions(holder);
        }

        var scene = new Scene(holder);
        scene.Window.Background = Brushes.White;
        Dispatcher.UIThread.RunJobs();
        return (button, scene, Box(button, scene.Window).Center);
    }

    private sealed class TestCommand : ICommand
    {
        public bool Allowed { get; set; } = true;
        public int Executed { get; private set; }
        public object? LastParameter { get; private set; }
        public object? LastQueried { get; private set; }
        public int Subscribers => CanExecuteChangedHandlers?.GetInvocationList().Length ?? 0;

        private EventHandler? CanExecuteChangedHandlers;

        public event EventHandler? CanExecuteChanged
        {
            add => CanExecuteChangedHandlers += value;
            remove => CanExecuteChangedHandlers -= value;
        }

        public bool CanExecute(object? parameter)
        {
            LastQueried = parameter;
            return Allowed;
        }

        public void Execute(object? parameter)
        {
            Executed++;
            LastParameter = parameter;
        }

        public void Raise() => CanExecuteChangedHandlers?.Invoke(this, EventArgs.Empty);
    }

    // ── Behaviour 1: a tap clicks ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_A_Tap_Clicks_On_Release()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Press(scene, Centre);
        Assert.Equal(0, clicks);

        Release(scene, Centre);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_Click_Bubbles()
    {
        var bubbled = 0;
        var button = Boxed();
        var parent = new StackPanel { Children = { button } };
        parent.AddHandler(AvonicButton.ClickEvent, (_, _) => bubbled++);
        using var scene = new Scene(parent);

        Tap(scene, Centre);

        Assert.Equal(1, bubbled);
    }

    [AvaloniaFact]
    public void B1_Three_Quick_Taps_Click_Three_Times()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre);
        Tap(scene, Centre);
        Tap(scene, Centre);

        Assert.Equal(3, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Second_Quick_Press_Clicks_On_Release_Not_On_Press()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre);
        Press(scene, Centre);
        Assert.Equal(1, clicks);

        Release(scene, Centre);
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Second_Quick_Press_Released_Off_The_Button_Does_Not_Click()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);
        var off = new Point(50, 400);

        Tap(scene, Centre);
        Press(scene, Centre);
        scene.Window.MouseMove(off, RawInputModifiers.None);
        Release(scene, off);

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Second_Press_Taken_By_A_Gesture_Does_Not_Click_Later()
    {
        var clicks = 0;
        IPointer? pointer = null;
        var button = Boxed();
        var other = new Border { Height = 48, Background = Brushes.Gray };
        button.Click += (_, _) => clicks++;
        button.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel);
        using var scene = new Scene(new StackPanel { Children = { button, other } });

        Tap(scene, Centre);
        Assert.Equal(1, clicks);

        // As a scroll does: the pointer goes elsewhere, so the release never reaches the button.
        Press(scene, Centre);
        pointer!.Capture(other);
        Release(scene, Centre);
        Assert.Equal(1, clicks);

        // Elsewhere on the button, so this is a new first tap, which clicks once.
        Tap(scene, new Point(20, 20));
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B1_Another_Pointer_Releasing_Does_Not_Complete_A_Second_Press()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre);
        Press(scene, Centre);

        button.RaiseEvent(new PointerReleasedEventArgs(
            button, new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true), scene.Window, Centre, 0,
            new PointerPointProperties(), KeyModifiers.None, MouseButton.Left));
        Assert.Equal(1, clicks);

        Release(scene, Centre);
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B1_Being_Disabled_During_A_Second_Press_Does_Not_Click()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre);
        Press(scene, Centre);
        button.IsEnabled = false;
        Release(scene, Centre);

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Quick_Tap_After_One_On_Something_Else_Still_Clicks()
    {
        // Avalonia raises neither Tapped nor DoubleTapped for this.
        var clicks = 0;
        var button = Boxed();
        button.IsEnabled = false;
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre);
        button.IsEnabled = true;
        Tap(scene, Centre);

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_Two_Quick_Right_Presses_Do_Not_Click()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre, MouseButton.Right);
        Tap(scene, Centre, MouseButton.Right);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_Dragging_Within_The_Button_Still_Clicks_Measured()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Press(scene, new Point(5, 20));
        Hover(scene, new Point(95, 20));
        Release(scene, new Point(95, 20));

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_Dragging_Off_And_Back_Still_Clicks_Measured()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Press(scene, Centre);
        Hover(scene, new Point(50, 400));
        Hover(scene, new Point(60, 20));
        Release(scene, new Point(60, 20));

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_Being_Disabled_During_A_First_Press_Does_Not_Click_Measured()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Press(scene, Centre);
        button.IsEnabled = false;
        Release(scene, Centre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_Being_Disabled_And_Enabled_During_A_Press_Does_Not_Click()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Press(scene, Centre);
        button.IsEnabled = false;
        button.IsEnabled = true;
        Release(scene, Centre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Command_That_Stops_Being_Able_To_Execute_During_A_Press_Does_Not_Click()
    {
        var clicks = 0;
        var command = new TestCommand();
        var button = Boxed();
        button.Command = command;
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Press(scene, Centre);
        command.Allowed = false;
        command.Raise();
        Release(scene, Centre);

        Assert.Equal(0, clicks);
        Assert.Equal(0, command.Executed);
    }

    [AvaloniaFact]
    public void B1_A_Child_That_Handles_The_Press_Stops_Click_Every_Time_Measured()
    {
        // Measured: a child's stopPropagation stops it, on one tap and on two quick ones.
        var clicks = 0;
        var child = new Border { Width = 60, Height = 30, Background = Brushes.Gray };
        child.PointerPressed += (_, e) => e.Handled = true;
        var button = Boxed(child);
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre);
        Tap(scene, Centre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Press_A_Gesture_Took_Does_Not_Click_On_The_Next_Handled_Press()
    {
        var clicks = 0;
        IPointer? pointer = null;
        var handle = false;
        var child = new Border { Width = 60, Height = 30, Background = Brushes.Gray };
        child.PointerPressed += (_, e) => e.Handled = handle;
        var button = Boxed(child);
        var other = new Border { Height = 48, Background = Brushes.Gray };
        button.Click += (_, _) => clicks++;
        button.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel);
        using var scene = new Scene(new StackPanel { Children = { button, other } });

        Press(scene, Centre);
        pointer!.Capture(other);
        Release(scene, Centre);

        handle = true;
        Tap(scene, Centre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Press_That_Loses_The_Pointer_Does_Not_Click()
    {
        // pointercancel, measured "touch cancelled". The release still lands on the button.
        var clicks = 0;
        IPointer? pointer = null;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        button.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel);
        using var scene = new Scene(button);

        Press(scene, Centre);
        pointer!.Capture(null);
        Release(scene, Centre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Child_Taking_The_Pointer_Is_Still_The_Same_Press()
    {
        var clicks = 0;
        IPointer? pointer = null;
        var child = new Border { Width = 60, Height = 30, Background = Brushes.Gray };
        var button = Boxed(child);
        button.Click += (_, _) => clicks++;
        button.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel);
        using var scene = new Scene(button);

        Press(scene, new Point(5, 5));
        pointer!.Capture(child);
        Release(scene, new Point(5, 5));
        Assert.Equal(1, clicks);

        Press(scene, new Point(5, 5));
        pointer!.Capture(button);
        Release(scene, new Point(5, 5));
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Child_That_Only_Handles_The_Release_Does_Not_Stop_The_Click()
    {
        var clicks = 0;
        var child = new Border { Width = 60, Height = 30, Background = Brushes.Gray };
        child.PointerReleased += (_, e) => e.Handled = true;
        var button = Boxed(child);
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre);

        Assert.Equal(1, clicks);
    }

    private static void RaisePress(AvonicButton button, Pointer pointer, Scene scene) =>
        button.RaiseEvent(new PointerPressedEventArgs(
            button, pointer, scene.Window, Centre, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));

    private static void RaiseRelease(AvonicButton button, Pointer pointer, Scene scene)
    {
        pointer.Capture(button);
        button.RaiseEvent(new PointerReleasedEventArgs(
            button, pointer, scene.Window, Centre, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void B1_A_Second_Finger_Means_No_Click_Measured(bool secondLiftsFirst)
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);
        var first = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        var second = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, false);

        RaisePress(button, first, scene);
        RaisePress(button, second, scene);
        RaiseRelease(button, secondLiftsFirst ? second : first, scene);
        RaiseRelease(button, secondLiftsFirst ? first : second, scene);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_One_Finger_Clicks()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);
        var finger = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);

        RaisePress(button, finger, scene);
        RaiseRelease(button, finger, scene);
        Assert.Equal(1, clicks);

        // One press, one click.
        RaiseRelease(button, finger, scene);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_Another_Button_Tapped_During_A_Press_Means_No_Click_Measured()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseDown(Centre, MouseButton.Right, RawInputModifiers.None);
        scene.Window.MouseUp(Centre, MouseButton.Right, RawInputModifiers.None);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_Another_Button_Still_Held_At_The_Release_Clicks_Measured()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseDown(Centre, MouseButton.Right, RawInputModifiers.None);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        scene.Window.MouseUp(Centre, MouseButton.Right, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_Releasing_Another_Button_Does_Not_Click_Measured()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        // The middle of this is what matters: the left one is still down.
        scene.Window.MouseDown(Centre, MouseButton.Right, RawInputModifiers.None);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(Centre, MouseButton.Right, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, clicks);

        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Left_Tap_While_The_Right_Is_Held_Clicks_Measured()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        scene.Window.MouseDown(Centre, MouseButton.Right, RawInputModifiers.None);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        scene.Window.MouseUp(Centre, MouseButton.Right, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_The_Right_Button_Does_Not_Click()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre, MouseButton.Right);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_A_Disabled_Button_Does_Not_Click()
    {
        var clicks = 0;
        var button = Boxed();
        button.IsEnabled = false;
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Tap(scene, Centre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_Released_Off_The_Button_Does_Not_Click()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);
        var off = new Point(50, 400);

        Press(scene, Centre);
        scene.Window.MouseMove(off, RawInputModifiers.None);
        Release(scene, off);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B1_The_Whole_Box_Takes_Input()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        // Well outside the text
        Tap(scene, new Point(3, 3));

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B1_The_Tap_Still_Reaches_What_Is_Around_It()
    {
        var tapped = 0;
        var button = Boxed();
        var parent = new StackPanel { Children = { button } };
        parent.AddHandler(InputElement.TappedEvent, (_, _) => tapped++);
        using var scene = new Scene(parent);

        Tap(scene, Centre);

        Assert.Equal(1, tapped);
    }

    // ── Behaviour 2: keys ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Enter_Clicks_On_Press()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);
        button.Focus();

        PressKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, clicks);

        ReleaseKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B2_Space_Clicks_On_Release()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);
        button.Focus();

        PressKey(scene, PhysicalKey.Space);
        Assert.Equal(0, clicks);

        ReleaseKey(scene, PhysicalKey.Space);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B2_Space_Released_After_Focus_Moved_Away_Does_Not_Click()
    {
        var clicks = 0;
        var button = Boxed();
        var other = new Button { Content = "y" };
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(new StackPanel { Children = { button, other } });
        button.Focus();

        PressKey(scene, PhysicalKey.Space);
        other.Focus();
        button.Focus();
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B2_Being_Disabled_During_A_Space_Press_Does_Not_Click()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);
        button.Focus();

        PressKey(scene, PhysicalKey.Space);
        button.IsEnabled = false;
        button.IsEnabled = true;
        button.Focus();
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B2_A_Second_Release_Does_Not_Click_Again()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);
        button.Focus();

        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B2_It_Keeps_The_Keys_It_Uses()
    {
        var down = new List<Key>();
        var up = new List<Key>();
        var button = Boxed();
        var parent = new StackPanel { Children = { button } };
        parent.KeyDown += (_, e) => down.Add(e.Key);
        parent.KeyUp += (_, e) => up.Add(e.Key);
        using var scene = new Scene(parent);
        button.Focus();

        PressKey(scene, PhysicalKey.Enter);
        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.DoesNotContain(Key.Enter, down);
        Assert.DoesNotContain(Key.Space, down);
        Assert.DoesNotContain(Key.Space, up);

        // And not one it doesn't.
        PressKey(scene, PhysicalKey.A);
        Assert.Contains(Key.A, down);
    }

    [AvaloniaFact]
    public void B2_Keys_On_A_Nested_Control_Are_Its_Own()
    {
        var clicks = 0;
        var child = new Border { Width = 40, Height = 20, Focusable = true };
        var button = new AvonicButton { Content = child };
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);
        child.Focus();

        PressKey(scene, PhysicalKey.Enter);
        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
    }

    // ── Behaviour 3: focus ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Tab_Stops_Unless_Disabled()
    {
        var first = Boxed();
        var disabled = Boxed();
        disabled.IsEnabled = false;
        var last = Boxed();
        using var scene = new Scene(new StackPanel { Children = { first, disabled, last } });

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(first.IsFocused);

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(last.IsFocused);
    }

    [AvaloniaFact]
    public void B3_A_Press_Focuses_It()
    {
        var button = Boxed();
        using var scene = new Scene(button);

        Tap(scene, Centre);

        Assert.True(button.IsFocused);
    }

    // ── Behaviour 4: ionFocus and ionBlur ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Focus_And_Blur_Fire_On_Any_Focus()
    {
        var log = new List<string>();
        var button = Boxed();
        var other = new Button { Content = "y" };
        button.IonFocus += (_, _) => log.Add("focus");
        button.IonBlur += (_, _) => log.Add("blur");
        button.Click += (_, _) => log.Add("click");
        using var scene = new Scene(new StackPanel { Children = { button, other } });

        // ionFocus, then click: measured.
        Tap(scene, Centre);
        Assert.Equal(["focus", "click"], log);

        log.Clear();
        other.Focus();
        Assert.Equal(["blur"], log);

        log.Clear();
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(button.IsFocused || other.IsFocused);
        button.Focus();
        Assert.Equal(["focus"], log);
    }

    [AvaloniaFact]
    public void B4_They_Bubble()
    {
        var log = new List<string>();
        var button = Boxed();
        var other = new Button { Content = "y" };
        var parent = new StackPanel { Children = { button, other } };
        parent.AddHandler(AvonicButton.IonFocusEvent, (_, _) => log.Add("focus"));
        parent.AddHandler(AvonicButton.IonBlurEvent, (_, _) => log.Add("blur"));
        using var scene = new Scene(parent);

        button.Focus();
        other.Focus();

        Assert.Equal(["focus", "blur"], log);
    }

    [AvaloniaFact]
    public void B4_Focus_On_A_Nested_Control_Is_Not_The_Buttons()
    {
        var log = new List<string>();
        var child = new Border { Width = 40, Height = 20, Focusable = true };
        var button = new AvonicButton { Content = child };
        var other = new Button { Content = "y" };
        button.IonFocus += (_, _) => log.Add("focus");
        button.IonBlur += (_, _) => log.Add("blur");
        using var scene = new Scene(new StackPanel { Children = { button, other } });

        child.Focus();
        other.Focus();

        Assert.Empty(log);
    }

    [AvaloniaFact]
    public void B4_Disabling_A_Focused_Button_Blurs_It()
    {
        var blurs = 0;
        var button = Boxed();
        button.IonBlur += (_, _) => blurs++;
        using var scene = new Scene(button);
        button.Focus();

        button.IsEnabled = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, blurs);
        Assert.False(button.IsFocused);
    }

    // ── Behaviour 5: activatable and the ripple ──────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_A_Press_Activates_And_Ripples_Bounded()
    {
        var button = Boxed();
        using var scene = new Scene(button);
        using var clock = new TestClock();

        Press(scene, Centre);
        Assert.False(Has(button, Activatable.ActivatedPseudoClass));

        clock.At(100);
        Assert.True(Has(button, Activatable.ActivatedPseudoClass));
        Assert.Single(Ripple(button).Ripples);
        Assert.Equal(RippleEffectType.Bounded, Ripple(button).Type);

        Release(scene, Centre);
    }

    [AvaloniaFact]
    public void B5_The_Ripple_Is_Under_The_Content() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed();
        using (scene)
        {
            var panel = Assert.IsType<Panel>(Ripple(button).GetVisualParent());
            Assert.True(panel.Children.IndexOf(Ripple(button)) < panel.Children.IndexOf(Inner(button)));
        }
    });

    [AvaloniaFact]
    public void B5_The_Ripple_Covers_The_Padding_And_Follows_The_Colour() => InstalledTheme.Run(_ =>
    {
        var solid = new AvonicButton { Content = "Text" };
        var outline = new AvonicButton { Content = "Text", Fill = ButtonFill.Outline };
        var danger = new AvonicButton { Content = "Text", Fill = ButtonFill.Clear, Color = "danger" };
        var token = new AvonicButton { Content = "Text" };
        token.Resources["Avonic.Button.RippleColor"] = Brushes.Red;
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { solid, outline, danger, token } }));

        var box = Box(solid, scene.Window);
        AssertBox(box.X, box.Y, box.Width, box.Height, Box(Ripple(solid), scene.Window));
        Assert.Equal(Colors.White, Solid(Ripple(solid).Foreground));
        Assert.Equal(Primary, Solid(Ripple(outline).Foreground));
        Assert.Equal(Danger, Solid(Ripple(danger).Foreground));
        Assert.Equal(Colors.Red, Solid(Ripple(token).Foreground));
    });

    [AvaloniaFact]
    public void B5_The_Innermost_Activatable_Wins_In_A_Button_Item()
    {
        var button = new AvonicButton { Content = "x", Width = 60, Height = 30 };
        var item = new AvonicItem { Button = true, EndContent = button };
        using var scene = new Scene(item);
        using var clock = new TestClock();
        var centre = Box(button, scene.Window).Center;

        Press(scene, centre);
        clock.At(100);

        Assert.True(Has(button, Activatable.ActivatedPseudoClass));
        Assert.False(Has(item, Activatable.ActivatedPseudoClass));
        Release(scene, centre);
    }

    // ── Behaviours 6-8: overlays ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Hover_Shows_The_Overlay_For_A_Mouse() => InstalledTheme.Run(_ =>
    {
        var (button, scene, centre) = Themed();
        using (scene)
        {
            Assert.Equal(0, Overlay(button).Opacity);

            Hover(scene, centre);
            Assert.Equal(0.08, Overlay(button).Opacity, 3);
            Assert.Equal(Colors.White, Solid(Overlay(button).Background));

            // 8% white on primary, across the padding too
            var box = Box(button, scene.Window);
            AssertClose(Color.FromRgb(20, 98, 235), Pixel(scene, (int)box.X + 3, (int)box.Center.Y));

            Hover(scene, new Point(380, 800));
            Assert.Equal(0, Overlay(button).Opacity);
        }
    });

    [AvaloniaFact]
    public void B6_A_Touch_Does_Not_Hover()
    {
        var button = Boxed();
        using var scene = new Scene(button);

        Enter(button, PointerType.Touch);
        Assert.False(Has(button, Activatable.HoverPseudoClass));

        Enter(button, PointerType.Pen);
        Assert.True(Has(button, Activatable.HoverPseudoClass));

        static void Enter(AvonicButton target, PointerType type) =>
            target.RaiseEvent(new PointerEventArgs(
                InputElement.PointerEnteredEvent, target, new Pointer(Pointer.GetNextFreeId(), type, true),
                TopLevel.GetTopLevel(target), default, 0, new PointerPointProperties(), KeyModifiers.None));
    }

    [AvaloniaFact]
    public void B6_Keyboard_Focus_Shows_The_Overlay() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed();
        using (scene)
        {
            PressKey(scene, PhysicalKey.Tab);

            Assert.True(Has(button, FocusVisible.FocusedPseudoClass));
            Assert.Equal(0.24, Overlay(button).Opacity, 3);
            Assert.Equal(Colors.White, Solid(Overlay(button).Background));
        }
    });

    [AvaloniaFact]
    public void B6_A_Press_Or_Another_Key_Clears_Focus_State()
    {
        var button = Boxed();
        var second = Boxed();
        var other = new Border { Height = 300, Background = Brushes.Gray };
        using var scene = new Scene(new StackPanel { Children = { button, second, other } });

        PressKey(scene, PhysicalKey.Tab);
        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);
        Assert.True(Has(button, FocusVisible.FocusedPseudoClass));

        // Measured "other key"
        PressKey(scene, PhysicalKey.A);
        Assert.False(Has(button, FocusVisible.FocusedPseudoClass));

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(Has(second, FocusVisible.FocusedPseudoClass));

        Tap(scene, new Point(50, 200));
        Assert.False(Has(second, FocusVisible.FocusedPseudoClass));
    }

    [AvaloniaFact]
    public void B6_Mouse_Focus_Does_Not_Show_It()
    {
        var button = Boxed();
        using var scene = new Scene(button);

        Tap(scene, Centre);

        Assert.True(button.IsFocused);
        Assert.False(Has(button, FocusVisible.FocusedPseudoClass));
    }

    // outline: none. The adorner comes from the app's base theme, so this adds it underneath. See AvonicItemTests.
    [AvaloniaFact]
    public void B6_There_Is_No_Focus_Adorner_Over_A_Base_Theme()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var button = new AvonicButton { Content = "x" };
                var other = new Button();
                using var scene = new Scene(new StackPanel { Children = { button, other } });

                var layer = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(button)!;

                PressKey(scene, PhysicalKey.Tab);
                Assert.True(button.IsFocused);
                Assert.Empty(layer.Children);

                // Proves the base theme really does give a focused control one.
                PressKey(scene, PhysicalKey.Tab);
                Assert.True(other.IsFocused);
                Assert.Single(layer.Children);
            });
        }
        finally
        {
            styles.Remove(baseTheme);
        }
    }

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid, 0.08, 0.24)]
    [InlineData(ButtonFill.Outline, 0.04, 0.12)]
    [InlineData(ButtonFill.Clear, 0.04, 0.12)]
    public void B7_Hover_Beats_Focus_Measured(ButtonFill fill, double hover, double focus) => InstalledTheme.Run(_ =>
    {
        var (button, scene, centre) = Themed(new AvonicButton { Content = "Text", Fill = fill });
        using (scene)
        {
            PressKey(scene, PhysicalKey.Tab);
            Assert.Equal(focus, Overlay(button).Opacity, 3);

            Hover(scene, centre);
            Assert.True(Has(button, FocusVisible.FocusedPseudoClass));
            Assert.Equal(hover, Overlay(button).Opacity, 3);
        }
    });

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid)]
    [InlineData(ButtonFill.Outline)]
    [InlineData(ButtonFill.Clear)]
    public void B7_A_Press_Beats_Hover_So_There_Is_No_Overlay(ButtonFill fill) => InstalledTheme.Run(_ =>
    {
        var (button, scene, centre) = Themed(new AvonicButton { Content = "Text", Fill = fill });
        using (scene)
        {
            using var clock = new TestClock();
            Hover(scene, centre);
            Assert.True(Overlay(button).Opacity > 0);

            Press(scene, centre);
            clock.At(100);

            Assert.Equal(0, Overlay(button).Opacity);
            Release(scene, centre);
        }
    });

    [AvaloniaFact]
    public void B7_State_Tokens_Apply_In_Order() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        button.Resources["Avonic.Button.BackgroundHover"] = new SolidColorBrush(Color.Parse("#FF0000"));
        button.Resources["Avonic.Button.BackgroundHoverOpacity"] = 0.5;
        button.Resources["Avonic.Button.BackgroundActivated"] = new SolidColorBrush(Color.Parse("#0000FF"));
        button.Resources["Avonic.Button.BackgroundActivatedOpacity"] = 0.7;
        button.Resources["Avonic.Button.ColorHover"] = new SolidColorBrush(Color.Parse("#111111"));
        button.Resources["Avonic.Button.ColorActivated"] = new SolidColorBrush(Color.Parse("#333333"));
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            using var clock = new TestClock();

            // Measured tok:hover
            Hover(scene, centre);
            Assert.Equal(Color.Parse("#FF0000"), Solid(Overlay(button).Background));
            Assert.Equal(0.5, Overlay(button).Opacity, 3);
            Assert.Equal(Color.Parse("#111111"), TextColour(button));

            // tok:press, with the mouse still over it
            Press(scene, centre);
            clock.At(100);
            Assert.Equal(Color.Parse("#0000FF"), Solid(Overlay(button).Background));
            Assert.Equal(0.7, Overlay(button).Opacity, 3);
            Assert.Equal(Color.Parse("#333333"), TextColour(button));
            Release(scene, centre);
        }
    });

    [AvaloniaFact]
    public void B7_Focus_Tokens_Apply_Measured_Tok() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        button.Resources["Avonic.Button.BackgroundFocused"] = new SolidColorBrush(Color.Parse("#00FF00"));
        button.Resources["Avonic.Button.BackgroundFocusedOpacity"] = 0.6;
        button.Resources["Avonic.Button.ColorFocused"] = new SolidColorBrush(Color.Parse("#222222"));
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            PressKey(scene, PhysicalKey.Tab);
            Assert.Equal(Color.Parse("#00FF00"), Solid(Overlay(button).Background));
            Assert.Equal(0.6, Overlay(button).Opacity, 3);
            Assert.Equal(Color.Parse("#222222"), TextColour(button));
        }
    });

    [AvaloniaFact]
    public void B8_The_Overlay_Has_No_Transition() => InstalledTheme.Run(_ =>
    {
        var (button, scene, centre) = Themed(transitions: true);
        using (scene)
        {
            Assert.Null(Overlay(button).Transitions);

            // At once, with no frames run: measured tr=0s.
            Hover(scene, centre);
            Assert.Equal(0.08, Overlay(button).Opacity, 3);
        }
    });

    // ── Behaviours 9-12: fills ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_Solid_Is_The_Default_Fill() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed();
        using (scene)
        {
            Assert.True(Has(button, ":fill-solid"));
            Assert.Equal(Primary, Solid(Native(button).Background));
            Assert.Equal(Colors.White, TextColour(button));
            Assert.Equal(Colors.White, Solid(Text(button).Foreground));
        }
    });

    [AvaloniaFact]
    public void B9_Solid_Has_A_Shadow_That_Rises_While_Pressed() => InstalledTheme.Run(_ =>
    {
        var low = BoxShadows.Parse("0 3 1 -2 #33000000, 0 2 2 0 #24000000, 0 1 5 0 #1F000000");
        var high = BoxShadows.Parse("0 5 5 -3 #33000000, 0 8 10 1 #24000000, 0 3 14 2 #1F000000");
        var (button, scene, centre) = Themed();
        using (scene)
        {
            using var clock = new TestClock();
            Assert.Equal(low, Native(button).BoxShadow);

            Press(scene, centre);
            clock.At(100);
            Assert.Equal(high, Native(button).BoxShadow);

            Release(scene, centre);
            clock.At(500);
            Assert.Equal(low, Native(button).BoxShadow);
        }
    });

    [AvaloniaFact]
    public void B9_The_Shadow_Is_Really_Drawn_Outside_The_Box() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed();
        using (scene)
        {
            var box = Box(button, scene.Window);
            var under = Pixel(scene, (int)box.Center.X, (int)box.Bottom + 1);
            Assert.True(under.R < 235, $"under the button was {under}");
        }
    });

    [AvaloniaFact]
    public void B10_Outline_Has_A_Border_And_No_Background() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Fill = ButtonFill.Outline });
        using (scene)
        {
            Assert.Null(Native(button).Background);
            Assert.Equal(new Thickness(2), Native(button).BorderThickness);
            Assert.Equal(Primary, Solid(Native(button).BorderBrush));
            Assert.Equal(Primary, TextColour(button));
            Assert.Equal(default, Native(button).BoxShadow);
        }
    });

    [AvaloniaFact]
    public void B11_Clear_Has_No_Background_Or_Border() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Fill = ButtonFill.Clear });
        using (scene)
        {
            Assert.Null(Native(button).Background);
            Assert.Equal(default, Native(button).BorderThickness);
            Assert.Equal(Primary, TextColour(button));
            Assert.Equal(default, Native(button).BoxShadow);
        }
    });

    [AvaloniaFact]
    public void B12_Fill_Default_Inherits_And_Ignores_Color() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicButton { Content = "Text", Fill = ButtonFill.Default };
        var coloured = new AvonicButton { Content = "Text", Fill = ButtonFill.Default, Color = "danger" };
        using var scene = new Scene(NoTransitions(new StackPanel
        {
            [TextElement.ForegroundProperty] = Brushes.Green,
            Children = { plain, coloured },
        }));

        foreach (var button in new[] { plain, coloured })
        {
            Assert.Equal(Colors.Green, Solid(Text(button).Foreground));
            Assert.Null(Native(button).Background);
            Assert.Equal(default, Native(button).BorderThickness);
            Assert.Equal(default, Native(button).BoxShadow);
        }
    });

    [AvaloniaFact]
    public void B12_Its_Overlay_Is_Opaque_If_Only_The_Colour_Is_Set() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Fill = ButtonFill.Default };
        button.Resources["Avonic.Button.BackgroundHover"] = Brushes.Red;
        button.Resources["Avonic.Button.BackgroundActivated"] = Brushes.Blue;
        button.Resources["Avonic.Button.ColorHover"] = Brushes.Lime;
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            using var clock = new TestClock();

            // Measured tokf:hover
            Hover(scene, centre);
            Assert.Equal(Colors.Red, Solid(Overlay(button).Background));
            Assert.Equal(1, Overlay(button).Opacity);
            Assert.Equal(Colors.Lime, TextColour(button));

            Press(scene, centre);
            clock.At(100);
            Assert.Equal(Colors.Blue, Solid(Overlay(button).Background));
            Assert.Equal(1, Overlay(button).Opacity);
            Release(scene, centre);
        }
    });

    [AvaloniaFact]
    public void B12_Focus_On_Fill_Default_Reads_Its_Tokens() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Fill = ButtonFill.Default };
        button.Resources["Avonic.Button.BackgroundFocused"] = Brushes.Red;
        button.Resources["Avonic.Button.ColorFocused"] = Brushes.Lime;
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            PressKey(scene, PhysicalKey.Tab);
            Assert.Equal(Colors.Red, Solid(Overlay(button).Background));
            Assert.Equal(1, Overlay(button).Opacity);
            Assert.Equal(Colors.Lime, TextColour(button));
        }
    });

    [AvaloniaFact]
    public void B12_Fill_Changes_Are_Followed() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        var holder = new StackPanel { [TextElement.ForegroundProperty] = Brushes.Green, Children = { button } };
        using var scene = new Scene(NoTransitions(holder));

        button.Fill = ButtonFill.Outline;
        Assert.Equal(new Thickness(2), Native(button).BorderThickness);
        Assert.Equal(Primary, TextColour(button));
        Assert.Null(Native(button).Background);

        button.Fill = ButtonFill.Clear;
        Assert.Equal(default, Native(button).BorderThickness);
        Assert.Equal(Primary, TextColour(button));

        button.Fill = ButtonFill.Default;
        Assert.Equal(Colors.Green, TextColour(button));

        button.Fill = ButtonFill.Solid;
        Assert.Equal(Primary, Solid(Native(button).Background));
        Assert.Equal(Colors.White, TextColour(button));

        button.Fill = ButtonFill.Clear;
        button.Fill = null;
        Assert.Equal(Primary, Solid(Native(button).Background));
    });

    // ── Behaviours 13-16: colour ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_Color_Paints_Each_Fill() => InstalledTheme.Run(_ =>
    {
        var solid = new AvonicButton { Content = "Text", Color = "danger" };
        var outline = new AvonicButton { Content = "Text", Color = "danger", Fill = ButtonFill.Outline };
        var clear = new AvonicButton { Content = "Text", Color = "danger", Fill = ButtonFill.Clear };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { solid, outline, clear } }));

        Assert.Equal(Danger, Solid(Native(solid).Background));
        Assert.Equal(Colors.White, TextColour(solid));
        Assert.Equal(Colors.White, Solid(Text(solid).Foreground));

        AssertTransparent(Native(outline).Background);
        Assert.Equal(Danger, TextColour(outline));
        Assert.Equal(Danger, Solid(Native(outline).BorderBrush));

        AssertTransparent(Native(clear).Background);
        Assert.Equal(Danger, TextColour(clear));
    });

    [AvaloniaFact]
    public void B13_A_Known_Color_Beats_The_Tokens_Measured_Varsc() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Color = "danger" };
        button.Resources["Avonic.Button.Background"] = Brushes.Green;
        button.Resources["Avonic.Button.Color"] = Brushes.Lime;
        var outline = new AvonicButton { Content = "Text", Color = "danger", Fill = ButtonFill.Outline };
        outline.Resources["Avonic.Button.BorderColor"] = Brushes.Green;
        outline.Resources["Avonic.Button.Background"] = Brushes.Green;
        var clear = new AvonicButton { Content = "Text", Color = "danger", Fill = ButtonFill.Clear };
        clear.Resources["Avonic.Button.Background"] = Brushes.Green;
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button, outline, clear } }));

        Assert.Equal(Danger, Solid(Native(button).Background));
        Assert.Equal(Colors.White, TextColour(button));
        Assert.Equal(Danger, Solid(Native(outline).BorderBrush));

        // Measured c and d: background: transparent on outline and clear beats the variable.
        AssertTransparent(Native(outline).Background);
        AssertTransparent(Native(clear).Background);
    });

    [AvaloniaFact]
    public void B14_An_Unknown_Color_On_Solid_Is_Transparent_With_Its_Text_And_Shadow() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Color = "badcolor" });
        using (scene)
        {
            Assert.Null(Native(button).Background);
            Assert.Equal(Colors.White, TextColour(button));
            Assert.NotEqual(default, Native(button).BoxShadow);
        }
    });

    [AvaloniaFact]
    public void B14_An_Unknown_Color_On_Outline_Uses_The_Current_Colour_For_Its_Border() => InstalledTheme.Run(_ =>
    {
        var outline = new AvonicButton { Content = "Text", Color = "badcolor", Fill = ButtonFill.Outline };
        outline.Resources["Avonic.Button.BorderColor"] = Brushes.Red;
        var clear = new AvonicButton { Content = "Text", Color = "badcolor", Fill = ButtonFill.Clear };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { outline, clear } }));

        Assert.Equal(Primary, TextColour(outline));
        Assert.Equal(Primary, Solid(Native(outline).BorderBrush));
        Assert.Equal(Primary, TextColour(clear));
    });

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid)]
    [InlineData(ButtonFill.Outline)]
    [InlineData(ButtonFill.Clear)]
    public void B14_An_Unknown_Color_Beats_The_Background_Token_Measured(ButtonFill fill) => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Color = "nope", Fill = fill };
        button.Resources["Avonic.Button.Background"] = Brushes.Green;
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            AssertTransparent(Native(button).Background);
        }
    });

    [AvaloniaFact]
    public void B14_An_Unknown_Color_Keeps_State_Colours_Measured_Tokbad() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Color = "badcolor" };
        button.Resources["Avonic.Button.ColorHover"] = Brushes.Lime;
        button.Resources["Avonic.Button.BackgroundHover"] = Brushes.Red;
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            Hover(scene, centre);

            Assert.Equal(Colors.Lime, TextColour(button));

            // .ion-color's var(--ion-color-contrast) is invalid, so the overlay is transparent and only its opacity shows.
            Assert.Null(Overlay(button).Background);
            Assert.Equal(0.08, Overlay(button).Opacity, 3);
        }
    });

    [AvaloniaFact]
    public void B14_Unknown_Color_On_Flat_Fills_Has_A_Transparent_Overlay() => InstalledTheme.Run(_ =>
    {
        var (button, scene, centre) = Themed(new AvonicButton { Content = "Text", Color = "badcolor", Fill = ButtonFill.Outline });
        using (scene)
        {
            Hover(scene, centre);
            Assert.Null(Overlay(button).Background);
            Assert.Equal(0.04, Overlay(button).Opacity, 3);
        }
    });

    [AvaloniaFact]
    public void B14_An_Unknown_Color_Still_Takes_Input() => InstalledTheme.Run(_ =>
    {
        var clicks = 0;
        var button = new AvonicButton { Content = "Text", Color = "badcolor" };
        button.Click += (_, _) => clicks++;
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            Tap(scene, centre);
            Assert.Equal(1, clicks);
        }
    });

    [AvaloniaFact]
    public void B15_Inside_A_Coloured_Ancestor() => InstalledTheme.Run(_ =>
    {
        var none = new AvonicButton { Content = "Text" };
        var unknown = new AvonicButton { Content = "Text", Color = "nope" };
        var item = new AvonicItem { Color = "danger", Content = new StackPanel { Children = { none, unknown } } };
        using var scene = new Scene(NoTransitions(item));

        // Measured initemcol: stays primary. initemcolbad: --ion-color-base is inherited.
        Assert.Equal(Primary, Solid(Native(none).Background));
        Assert.Equal(Danger, Solid(Native(unknown).Background));
    });

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid, 0.24, 0.08, "#FFFFFF")]
    [InlineData(ButtonFill.Outline, 0.12, 0.04, "#C5000F")]
    [InlineData(ButtonFill.Clear, 0.12, 0.04, "#C5000F")]
    public void B16_On_A_Color_The_Overlays_Use_Contrast_Or_Base(ButtonFill fill, double focus, double hover, string colour) => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Color = "danger", Fill = fill };
        button.Resources["Avonic.Button.BackgroundHover"] = Brushes.Lime;
        button.Resources["Avonic.Button.BackgroundFocused"] = Brushes.Lime;
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            PressKey(scene, PhysicalKey.Tab);
            Assert.Equal(Color.Parse(colour), Solid(Overlay(button).Background));
            Assert.Equal(focus, Overlay(button).Opacity, 3);

            Hover(scene, centre);
            Assert.Equal(Color.Parse(colour), Solid(Overlay(button).Background));
            Assert.Equal(hover, Overlay(button).Opacity, 3);
        }
    });

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid, "#FFFFFF")]
    [InlineData(ButtonFill.Outline, "#C5000F")]
    [InlineData(ButtonFill.Clear, "#C5000F")]
    public void B16_Hover_Alone_On_A_Color_Uses_Contrast_Or_Base(ButtonFill fill, string colour) => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Color = "danger", Fill = fill };
        button.Resources["Avonic.Button.BackgroundHover"] = Brushes.Lime;
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            Hover(scene, centre);
            Assert.Equal(Color.Parse(colour), Solid(Overlay(button).Background));
        }
    });

    [AvaloniaFact]
    public void B16_Opacity_Tokens_Still_Apply_And_Text_Stays_Measured_Tokc() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Color = "danger" };
        button.Resources["Avonic.Button.BackgroundHoverOpacity"] = 0.5;
        button.Resources["Avonic.Button.BackgroundActivatedOpacity"] = 0.7;
        button.Resources["Avonic.Button.BackgroundActivated"] = Brushes.Blue;
        button.Resources["Avonic.Button.ColorHover"] = Brushes.Lime;
        button.Resources["Avonic.Button.ColorActivated"] = Brushes.Lime;
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            using var clock = new TestClock();
            Hover(scene, centre);
            Assert.Equal(0.5, Overlay(button).Opacity, 3);
            Assert.Equal(Colors.White, TextColour(button));

            // Contrast from the hover rule, opacity from activated: measured "rgb(255, 255, 255) 0.7"
            Press(scene, centre);
            clock.At(100);
            Assert.Equal(0.7, Overlay(button).Opacity, 3);
            Assert.Equal(Colors.White, Solid(Overlay(button).Background));
            Assert.Equal(Colors.White, TextColour(button));
            Release(scene, centre);
        }
    });

    [AvaloniaFact]
    public void B16_A_Touch_Press_On_A_Color_Reads_The_Activated_Token() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Color = "danger" };
        button.Resources["Avonic.Button.BackgroundActivated"] = Brushes.Blue;
        button.Resources["Avonic.Button.BackgroundActivatedOpacity"] = 0.7;
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            // With no hover, nothing replaces the token.
            ((IPseudoClasses)button.Classes).Add(Activatable.ActivatedPseudoClass);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Colors.Blue, Solid(Overlay(button).Background));
            Assert.Equal(0.7, Overlay(button).Opacity, 3);
        }
    });

    // ── Behaviours 17-20: shape, size, expand ────────────────────────────────────────────────

    [AvaloniaFact]
    public void B17_Round() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Shape = ButtonShape.Round });
        using (scene)
        {
            Assert.Equal(new CornerRadius(999), Native(button).CornerRadius);
            AssertThickness(26, 0, 26, 0, Inner(button).Margin);
            Assert.Equal(36, button.Bounds.Height);
        }
    });

    [AvaloniaTheory]
    [InlineData(ButtonSize.Small, 11.7, 4)]
    [InlineData(ButtonSize.Large, 20, 14)]
    public void B17_Size_Beats_Rounds_Padding(ButtonSize size, double inline, double block) => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Shape = ButtonShape.Round, Size = size });
        using (scene)
        {
            Assert.Equal(new CornerRadius(999), Native(button).CornerRadius);
            AssertThickness(inline, block, inline, block, Inner(button).Margin);
        }
    });

    [AvaloniaFact]
    public void B17_Round_Is_Really_Round() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Shape = ButtonShape.Round, Width = 200 });
        using (scene)
        {
            var box = Box(button, scene.Window);
            AssertClose(Colors.White, Pixel(scene, (int)box.X + 1, (int)box.Y + 1), 30);
            AssertClose(Primary, Pixel(scene, (int)box.X + 3, (int)box.Center.Y), 30);
        }
    });

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData(ButtonSize.Default)]
    public void B18_The_Default_Box(ButtonSize? size) => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Size = size });
        using (scene)
        {
            Assert.Equal(14, button.FontSize);
            Assert.Equal(36, button.Bounds.Height);
            Assert.Equal(new Thickness(2, 4), button.Margin);
            Assert.Equal(new CornerRadius(4), Native(button).CornerRadius);
            AssertThickness(15.4, 8, 15.4, 8, Inner(button).Margin);
            Assert.Equal(HorizontalAlignment.Left, button.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Center, button.VerticalAlignment);
        }
    });

    [AvaloniaFact]
    public void B18_A_Taller_Button_Centres_Its_Content() => InstalledTheme.Run(_ =>
    {
        // Measured tall: stretched by a flex parent to 92, with the content in the middle.
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Height = 92 });
        using (scene)
        {
            Assert.Equal(92, button.Bounds.Height);
            var box = Box(button, scene.Window);
            var presenter = Box(Slot(button, "PART_Content"), scene.Window);
            Assert.InRange(presenter.Center.Y, box.Center.Y - 1, box.Center.Y + 1);
            Assert.Equal(14, presenter.Height);
        }
    });

    [AvaloniaFact]
    public void B18_A_Wider_Button_Centres_Its_Content_On_The_Stub()
    {
        var button = Boxed();
        using var scene = new Scene(button);

        // As wide as its content, in the middle: justify-content: center.
        var inner = Box(Inner(button), scene.Window);
        Assert.True(inner.Width < 90, $"{inner.Width}");
        Assert.InRange(inner.Center.X, 49, 51);
        var text = Box(Text(button), scene.Window);
        Assert.InRange(text.Center.Y, 19, 21);
        Assert.True(text.Height < 30, $"{text.Height}");
    }

    [AvaloniaFact]
    public void B18_Em_Values_Follow_The_Font_Size() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", FontSize = 20 });
        using (scene)
        {
            AssertThickness(22, 8, 22, 8, Inner(button).Margin);
            Assert.Equal(1.2, Text(button).LetterSpacing, 3);
        }
    });

    [AvaloniaFact]
    public void B19_Large() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Size = ButtonSize.Large });
        using (scene)
        {
            Assert.Equal(20, button.FontSize);
            Assert.Equal(56, button.Bounds.Height);
            AssertThickness(20, 14, 20, 14, Inner(button).Margin);
        }
    });

    [AvaloniaFact]
    public void B19_Small() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Size = ButtonSize.Small });
        using (scene)
        {
            Assert.Equal(13, button.FontSize);
            Assert.Equal(27.3, button.MinHeight, 2);
            AssertThickness(11.7, 4, 11.7, 4, Inner(button).Margin);
        }
    });

    [AvaloniaTheory]
    [InlineData(ButtonExpand.Block)]
    [InlineData(ButtonExpand.Full)]
    public void B20_Expand_Fills_The_Width_Keeping_The_Margin(ButtonExpand expand) => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Expand = expand };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        // Measured: 2 to 388
        AssertBox(2, 4, 386, 36, Box(button, scene.Window));
    });

    [AvaloniaFact]
    public void B20_Block_Keeps_The_Radius_And_Centres_The_Content() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Expand = ButtonExpand.Block, StartContent = new AvonicIcon { IconName = "heart" } };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.Equal(new CornerRadius(4), Native(button).CornerRadius);

        // Measured blockic: the icon stays beside the text, in the middle, rather than going to the edge.
        var box = Box(button, scene.Window);
        var inner = Box(Inner(button), scene.Window);
        Assert.InRange(inner.Center.X, box.Center.X - 1, box.Center.X + 1);
        Assert.True(inner.Width < 200);
    });

    [AvaloniaFact]
    public void B20_Full_Drops_The_Radius_Even_With_The_Token() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Expand = ButtonExpand.Full };
        button.Resources["Avonic.Button.BorderRadius"] = new CornerRadius(9);
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.Equal(new CornerRadius(0), Native(button).CornerRadius);
    });

    [AvaloniaFact]
    public void B20_Full_Outline_Has_No_Side_Borders() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Expand = ButtonExpand.Full, Fill = ButtonFill.Outline };
        var token = new AvonicButton { Content = "Text", Expand = ButtonExpand.Full, Fill = ButtonFill.Outline };
        token.Resources["Avonic.Button.BorderWidth"] = new Thickness(5);
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button, token } }));

        Assert.Equal(new Thickness(0, 2, 0, 2), Native(button).BorderThickness);
        Assert.Equal(new Thickness(0, 5, 0, 5), Native(token).BorderThickness);
    });

    [AvaloniaFact]
    public void B20_Full_And_Round_Stays_Round_With_Its_Sides() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Expand = ButtonExpand.Full, Shape = ButtonShape.Round, Fill = ButtonFill.Outline };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.Equal(new CornerRadius(999), Native(button).CornerRadius);
        Assert.Equal(new Thickness(2), Native(button).BorderThickness);
    });

    [AvaloniaFact]
    public void B20_Block_Is_Not_Full() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Expand = ButtonExpand.Block, Fill = ButtonFill.Outline };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.Equal(new Thickness(2), Native(button).BorderThickness);
    });

    // ── Behaviours 21, 22: text ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B21_Font_Weight_And_Spacing() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicButton { Content = "Text" };
        var strong = new AvonicButton { Content = "Text", Strong = true };
        var small = new AvonicButton { Content = "Text", Size = ButtonSize.Small };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { plain, strong, small } }));

        Assert.Equal(FontWeight.Medium, Text(plain).FontWeight);
        Assert.Equal(FontWeight.Bold, Text(strong).FontWeight);
        Assert.Equal(0.84, Text(plain).LetterSpacing, 3);
        Assert.Equal(0.78, Text(small).LetterSpacing, 3);
        Assert.Equal(14, Text(plain).LineHeight);
        Assert.Equal(13, Text(small).LineHeight);
        Assert.Equal(TextAlignment.Center, Text(plain).TextAlignment);
    });

    [AvaloniaFact]
    public void B21_There_Is_No_Kerning() => InstalledTheme.Run(_ =>
    {
        // Arial kerns AV tightly, so this text is narrower when kerning is on.
        var theme = new AvonicButton { Content = "AVAVAVAVAVAV", FontFamily = new FontFamily("Arial") };
        var kerned = new AvonicButton { Content = "AVAVAVAVAVAV", FontFamily = new FontFamily("Arial") };
        kerned.SetValue(TextElement.FontFeaturesProperty, new FontFeatureCollection());
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { theme, kerned } }));

        Assert.True(Text(theme).Bounds.Width > Text(kerned).Bounds.Width + 1, $"{Text(theme).Bounds.Width} against {Text(kerned).Bounds.Width}");
    });

    [AvaloniaFact]
    public void B21_Uppercase_Is_In_The_Display_Only() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", StartContent = "start", EndContent = "end" };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.Equal("TEXT", Text(button).Text);
        Assert.Equal("START", Assert.IsType<TextBlock>(Slot(button, "PART_Start").Child).Text);
        Assert.Equal("END", Assert.IsType<TextBlock>(Slot(button, "PART_End").Child).Text);
        Assert.Equal("Text", button.Content);
    });

    [AvaloniaFact]
    public void B21_Text_Is_Centred_In_The_Box() => InstalledTheme.Run(_ =>
    {
        // The middle of the ink, which a line kept at the top of its height puts about 3 px low.
        var (button, scene, _) = Themed(new AvonicButton { Content = "HHH", FontFamily = new FontFamily("Arial"), Fill = ButtonFill.Clear });
        using (scene)
        {
            var box = Box(button, scene.Window);
            var frame = scene.Window.CaptureRenderedFrame()!;
            int? first = null, last = null;
            for (var y = (int)box.Y; y < (int)box.Bottom; y++)
            {
                for (var x = (int)box.X; x < (int)box.Right; x++)
                {
                    if (Frames.PixelAt(frame, x, y).R < 128)
                    {
                        first ??= y;
                        last = y;
                        break;
                    }
                }
            }

            Assert.NotNull(first);
            Assert.InRange((first!.Value + last!.Value + 1) / 2.0, box.Center.Y - 1.5, box.Center.Y + 1.5);
        }
    });

    [AvaloniaFact]
    public void B21_Moving_The_Text_Does_Not_Change_Its_Box() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed();
        using (scene)
        {
            Assert.Equal(-Text(button).Margin.Top, Text(button).Margin.Bottom, 3);
            Assert.False(Text(button).ClipToBounds);
            Assert.Equal(36, button.Bounds.Height);
        }
    });

    [AvaloniaFact]
    public void B21_A_TextBlock_In_A_Panel_In_A_Slot_Gets_The_Same_Rules() => InstalledTheme.Run(_ =>
    {
        var block = new TextBlock { Text = "Text" };
        var button = new AvonicButton { Content = new StackPanel { Children = { block } } };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.False(block.ClipToBounds);
        Assert.NotEqual(0, block.Margin.Bottom);
    });

    [AvaloniaFact]
    public void B21_A_TextBlock_Deeper_Does_Not()
    {
        InstalledTheme.Run(_ =>
        {
            var block = new TextBlock { Text = "Text" };
            var button = new AvonicButton { Content = new Border { Child = new StackPanel { Children = { block } } } };
            using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

            Assert.Equal(default, block.Margin);
        });
    }

    [AvaloniaFact]
    public void B22_Long_Text_Wraps_Centred_And_Fills_The_Width() => InstalledTheme.Run(_ =>
    {
        var text = string.Join(' ', Enumerable.Repeat("A long line of text", 8));
        var button = new AvonicButton { Content = text };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        // Measured long: 2 to 388, as shrink-to-fit is once it doesn't fit, and 14 per line plus 16.
        var box = Box(button, scene.Window);
        AssertBox(2, 4, 386, box.Height, box);
        var lines = (box.Height - 16) / 14;
        Assert.True(lines >= 2);
        Assert.Equal(Math.Round(lines), lines, 3);
    });

    [AvaloniaFact]
    public void B22_Short_Text_Keeps_The_Button_As_Wide_As_It() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.InRange(button.Bounds.Width, 40, 120);
    });

    [AvaloniaFact]
    public void B22_A_Narrow_Parent_Wraps_It_To_Fit() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Narrow button text wraps" };
        using var scene = new Scene(NoTransitions(new StackPanel
        {
            Width = 100,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children = { button },
        }));

        // Measured narrow wraps. A control can't be wider than its parent here, where Chromium's grew to 99.6 of 96.
        Assert.Equal(96, button.Bounds.Width);
        Assert.True(button.Bounds.Height > 36);
    });

    // ── Behaviours 23-25: icons ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B23_Slotted_Icons_Size_Margins_And_Input() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var end = new AvonicIcon { IconName = "star" };
        var middle = new AvonicIcon { IconName = "heart" };
        var both = new AvonicButton { StartContent = start, Content = "Both", EndContent = end };
        var alone = new AvonicButton { Content = middle };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { both, alone } }));

        Assert.Equal(18.9, start.FontSize, 2);
        Assert.Equal(18.9, end.FontSize, 2);
        Assert.Equal(18.9, middle.FontSize, 2);
        AssertThickness(-5.67, 0, 5.67, 0, start.Margin);
        AssertThickness(5.67, 0, -3.78, 0, end.Margin);
        Assert.Equal(default, middle.Margin);
        Assert.False(start.IsHitTestVisible);
        Assert.False(middle.IsHitTestVisible);
        Assert.Equal(Colors.White, Solid(start.Foreground));
    });

    [AvaloniaFact]
    public void B23_Margins_Use_The_Icons_Own_Size() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var button = new AvonicButton { StartContent = start, Content = "x", Size = ButtonSize.Large };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        // 1.35 of 20 is 27, and 0.3 of that is 8.1
        Assert.Equal(27, start.FontSize, 2);
        AssertThickness(-8.1, 0, 8.1, 0, start.Margin);
    });

    [AvaloniaFact]
    public void B23_A_Press_On_An_Icon_Clicks_The_Button() => InstalledTheme.Run(_ =>
    {
        var clicks = 0;
        var button = new AvonicButton { Content = new AvonicIcon { IconName = "heart" } };
        button.Click += (_, _) => clicks++;
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            Tap(scene, centre);
            Assert.Equal(1, clicks);
        }
    });

    [AvaloniaFact]
    public void B23_Direct_Children_Only() => InstalledTheme.Run(_ =>
    {
        var nested = new AvonicIcon { IconName = "heart" };
        var button = new AvonicButton { StartContent = new StackPanel { Children = { nested } } };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.Equal(14, nested.FontSize, 2);
        Assert.Equal(default, nested.Margin);
    });

    [AvaloniaTheory]
    [InlineData(null, 40.04, 22.4)]
    [InlineData(ButtonSize.Small, 28.08, 16.006)]
    [InlineData(ButtonSize.Large, 50, 28)]
    public void B24_Icon_Only_Sizes(ButtonSize? size, double box, double icon) => InstalledTheme.Run(_ =>
    {
        var glyph = new AvonicIcon { IconName = "heart" };
        var (button, scene, _) = Themed(new AvonicButton { IconOnlyContent = glyph, Size = size });
        using (scene)
        {
            Assert.Equal(box, button.MinWidth, 2);
            Assert.Equal(box, button.MinHeight, 2);
            AssertThickness(0, 0, 0, 0, Inner(button).Margin);
            Assert.Equal(icon, glyph.FontSize, 2);
            Assert.False(glyph.IsHitTestVisible);
        }
    });

    [AvaloniaFact]
    public void B24_The_Clamps_Hold() => InstalledTheme.Run(_ =>
    {
        var tiny = new AvonicIcon { IconName = "heart" };
        var huge = new AvonicIcon { IconName = "heart" };
        var small = new AvonicButton { IconOnlyContent = tiny, FontSize = 2 };
        var big = new AvonicButton { IconOnlyContent = huge, FontSize = 200 };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { small, big } }));

        Assert.Equal(30, small.MinHeight, 2);
        Assert.Equal(60, big.MinHeight, 2);
        Assert.Equal(15.104, tiny.FontSize, 3);
        Assert.Equal(43.008, huge.FontSize, 3);
    });

    [AvaloniaFact]
    public void B24_A_Slot_Change_Updates_It() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));
        Assert.Equal(36, button.MinHeight);

        button.IconOnlyContent = new AvonicIcon { IconName = "heart" };
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(40.04, button.MinHeight, 2);

        button.IconOnlyContent = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(36, button.MinHeight);
    });

    [AvaloniaFact]
    public void B24_Padding_Top_Sets_All_Four_Measured_Onlyv() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { IconOnlyContent = new AvonicIcon { IconName = "heart" } };
        button.Resources["Avonic.Button.PaddingTop"] = 5d;
        var end = new AvonicButton { IconOnlyContent = new AvonicIcon { IconName = "heart" } };
        end.Resources["Avonic.Button.PaddingEnd"] = 7d;
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button, end } }));

        AssertThickness(5, 5, 5, 5, Inner(button).Margin);

        // --padding-start follows --padding-end, and the others stay 0
        AssertThickness(7, 0, 7, 0, Inner(end).Margin);
    });

    [AvaloniaFact]
    public void B24_Round_Icon_Only_Keeps_Its_Radius() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { IconOnlyContent = new AvonicIcon { IconName = "heart" }, Shape = ButtonShape.Round });
        using (scene)
        {
            Assert.Equal(new CornerRadius(999), Native(button).CornerRadius);
            AssertThickness(0, 0, 0, 0, Inner(button).Margin);
        }
    });

    [AvaloniaFact]
    public void B24_Its_Template_Is_Used() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton
        {
            IconOnlyContent = "x",
            IconOnlyContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((_, _) => new Border { Name = "fromTemplate" }),
            StartContent = "x",
            StartContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((_, _) => new Border { Name = "fromStart" }),
            EndContent = "x",
            EndContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((_, _) => new Border { Name = "fromEnd" }),
            Content = "x",
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((_, _) => new Border { Name = "fromContent" }),
        };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.Equal("fromTemplate", Slot(button, "PART_IconOnly").Child!.Name);
        Assert.Equal("fromStart", Slot(button, "PART_Start").Child!.Name);
        Assert.Equal("fromEnd", Slot(button, "PART_End").Child!.Name);
        Assert.Equal("fromContent", Slot(button, "PART_Content").Child!.Name);
    });

    [AvaloniaFact]
    public void B25_Start_And_End_Do_Not_Shrink_And_Long_Text_Does() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var end = new AvonicIcon { IconName = "star" };
        var text = string.Join(' ', Enumerable.Repeat("A long line of text", 8));
        var button = new AvonicButton { StartContent = start, Content = text, EndContent = end };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.Equal(18.9, start.Bounds.Width, 0);
        Assert.Equal(18.9, end.Bounds.Width, 0);
        var box = Box(button, scene.Window);
        Assert.True(box.Height > 36);

        // Measured longic: start at 11.73 and end at 357.48 of a button from 2 to 388
        Assert.InRange(Box(start, scene.Window).X, 10.5, 13);
        Assert.InRange(Box(end, scene.Window).X, 356, 359);

        // And in the middle of the height
        Assert.InRange(Box(start, scene.Window).Center.Y, box.Center.Y - 1, box.Center.Y + 1);
    });

    [AvaloniaFact]
    public void B25_Text_In_Start_Does_Not_Wrap() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { StartContent = "A long piece of text in the start slot" };
        using var scene = new Scene(NoTransitions(new StackPanel { Width = 150, HorizontalAlignment = HorizontalAlignment.Left, Children = { button } }));

        // [slot=start] { flex-shrink: 0 }, so it stays on one line and overflows.
        Assert.Equal(36, button.Bounds.Height);
    });

    [AvaloniaFact]
    public void B25_Slots_Count_When_Deciding_To_Wrap() => InstalledTheme.Run(_ =>
    {
        // Text that just fits with no icons has to wrap once there's one beside it.
        var text = "A";
        var fits = new AvonicButton { Content = text };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { fits } }));
        var wrapped = false;
        for (var length = 0; length < 200 && !wrapped; length++)
        {
            fits.Content = text + "A";
            Dispatcher.UIThread.RunJobs();
            wrapped = fits.Bounds.Height > 36;
            if (!wrapped)
            {
                text += "A";
            }
        }

        Assert.True(wrapped);
        fits.Content = text;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(36, fits.Bounds.Height);

        fits.StartContent = new AvonicIcon { IconName = "heart" };
        Dispatcher.UIThread.RunJobs();
        Assert.True(fits.Bounds.Height > 36);
    });

    [AvaloniaFact]
    public void B25_A_Tall_Slot_Grows_The_Button() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", StartContent = new Border { Height = 80, Width = 10 } };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        // 80, and 8 above and below
        Assert.Equal(96, button.Bounds.Height);
    });

    [AvaloniaFact]
    public void B25_Slots_Are_In_Order() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var end = new AvonicIcon { IconName = "star" };
        var button = new AvonicButton { StartContent = start, Content = "Both", EndContent = end };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        var text = Box(Text(button), scene.Window);
        Assert.True(Box(start, scene.Window).Right <= text.X + 6);
        Assert.True(Box(end, scene.Window).X >= text.Right);
    });

    // ── Behaviour 26: disabled ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B26_Disabled_Is_Faded() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", IsEnabled = false });
        using (scene)
        {
            Assert.Equal(0.5, button.Opacity);
            Assert.Equal(1, Native(button).Opacity);
        }
    });

    [AvaloniaFact]
    public void B26_In_A_Disabled_Item_Only_Its_Own_Disabled_Fades_It_Measured() => InstalledTheme.Run(_ =>
    {
        var inside = new AvonicButton { Content = "Text" };
        var both = new AvonicButton { Content = "Text", IsEnabled = false };
        var plain = new AvonicButton { Content = "Text", IsEnabled = false };
        using var scene = new Scene(NoTransitions(new StackPanel
        {
            Children =
            {
                new AvonicItem { IsEnabled = false, EndContent = inside },
                new AvonicItem { IsEnabled = false, EndContent = both },
                new AvonicItem { EndContent = plain },
            },
        }));

        // Measured: 1 and 0.5, inside an item at 0.3
        Assert.Equal(1, inside.Opacity);
        Assert.False(inside.IsEffectivelyEnabled);
        Assert.Equal(0.5, both.Opacity);
        Assert.Equal(0.5, plain.Opacity);
    });

    [AvaloniaFact]
    public void B26_A_Disabled_Panel_Fades_It_As_Avalonia_Does() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        using var scene = new Scene(NoTransitions(new StackPanel { IsEnabled = false, Children = { button } }));

        Assert.Equal(0.5, button.Opacity);
    });

    [AvaloniaFact]
    public void B26_A_Tap_On_A_Disabled_One_Goes_Through()
    {
        var behind = 0;
        var under = new Border { Background = Brushes.Gray };
        under.Tapped += (_, _) => behind++;
        var button = Boxed();
        button.IsEnabled = false;
        using var scene = new Scene(new Panel { Children = { under, button } });

        Tap(scene, Centre);

        Assert.Equal(1, behind);
    }

    // ── Behaviour 27: tokens ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B27_Every_Variable_Applies_Measured_Vars() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        button.Resources["Avonic.Button.Background"] = Brushes.Green;
        button.Resources["Avonic.Button.Color"] = Brushes.Lime;
        button.Resources["Avonic.Button.BorderRadius"] = new CornerRadius(9);
        button.Resources["Avonic.Button.PaddingStart"] = 30d;
        button.Resources["Avonic.Button.PaddingTop"] = 3d;
        button.Resources["Avonic.Button.Opacity"] = 0.7;
        button.Resources["Avonic.Button.BoxShadow"] = BoxShadows.Parse("none");
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            Assert.Equal(Colors.Green, Solid(Native(button).Background));
            Assert.Equal(Colors.Lime, TextColour(button));
            Assert.Equal(new CornerRadius(9), Native(button).CornerRadius);

            // Each side on its own: 30 3 15.4 8
            AssertThickness(30, 3, 15.4, 8, Inner(button).Margin);

            // On the native part, not the host
            Assert.Equal(0.7, Native(button).Opacity, 3);
            Assert.Equal(1, button.Opacity);
            Assert.Equal(default, Native(button).BoxShadow);
        }
    });

    private static void SetStateTokens(AvonicButton button)
    {
        button.Resources["Avonic.Button.BackgroundHover"] = new SolidColorBrush(Color.Parse("#FF0000"));
        button.Resources["Avonic.Button.BackgroundHoverOpacity"] = 0.5;
        button.Resources["Avonic.Button.BackgroundFocused"] = new SolidColorBrush(Color.Parse("#00FF00"));
        button.Resources["Avonic.Button.BackgroundFocusedOpacity"] = 0.6;
        button.Resources["Avonic.Button.BackgroundActivated"] = new SolidColorBrush(Color.Parse("#0000FF"));
        button.Resources["Avonic.Button.BackgroundActivatedOpacity"] = 0.7;
        button.Resources["Avonic.Button.ColorHover"] = new SolidColorBrush(Color.Parse("#111111"));
        button.Resources["Avonic.Button.ColorFocused"] = new SolidColorBrush(Color.Parse("#222222"));
        button.Resources["Avonic.Button.ColorActivated"] = new SolidColorBrush(Color.Parse("#333333"));
    }

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid)]
    [InlineData(ButtonFill.Outline)]
    [InlineData(ButtonFill.Clear)]
    [InlineData(ButtonFill.Default)]
    public void B27_State_Tokens_Apply_On_Every_Fill(ButtonFill fill) => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Fill = fill };
        SetStateTokens(button);
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            using var clock = new TestClock();

            Hover(scene, centre);
            Assert.Equal(Color.Parse("#FF0000"), Solid(Overlay(button).Background));
            Assert.Equal(0.5, Overlay(button).Opacity, 3);
            Assert.Equal(Color.Parse("#111111"), TextColour(button));

            Press(scene, centre);
            clock.At(100);
            Assert.Equal(Color.Parse("#0000FF"), Solid(Overlay(button).Background));
            Assert.Equal(0.7, Overlay(button).Opacity, 3);
            Assert.Equal(Color.Parse("#333333"), TextColour(button));
            Release(scene, centre);
        }

        var focused = new AvonicButton { Content = "Text", Fill = fill };
        SetStateTokens(focused);
        var (_, focusScene, _) = Themed(focused);
        using (focusScene)
        {
            PressKey(focusScene, PhysicalKey.Tab);
            Assert.Equal(Color.Parse("#00FF00"), Solid(Overlay(focused).Background));
            Assert.Equal(0.6, Overlay(focused).Opacity, 3);
            Assert.Equal(Color.Parse("#222222"), TextColour(focused));
        }
    });

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid)]
    [InlineData(ButtonFill.Outline)]
    [InlineData(ButtonFill.Clear)]
    [InlineData(ButtonFill.Default)]
    public void B27_State_Colours_Follow_The_Color_Token_Unless_Set(ButtonFill fill) => InstalledTheme.Run(_ =>
    {
        var lime = new SolidColorBrush(Colors.Lime);
        var button = new AvonicButton { Content = "Text", Fill = fill };
        button.Resources["Avonic.Button.Color"] = lime;
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            using var clock = new TestClock();
            Assert.Equal(Colors.Lime, TextColour(button));

            Hover(scene, centre);
            Assert.Equal(Colors.Lime, TextColour(button));

            Press(scene, centre);
            clock.At(100);
            Assert.Equal(Colors.Lime, TextColour(button));
            Release(scene, centre);
        }

        var focused = new AvonicButton { Content = "Text", Fill = fill };
        focused.Resources["Avonic.Button.Color"] = lime;
        var (_, focusScene, _) = Themed(focused);
        using (focusScene)
        {
            PressKey(focusScene, PhysicalKey.Tab);
            Assert.Equal(Colors.Lime, TextColour(focused));
        }
    });

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid, "#FFFFFF")]
    [InlineData(ButtonFill.Outline, "#0054E9")]
    [InlineData(ButtonFill.Clear, "#0054E9")]
    public void B27_With_No_Tokens_State_Colours_Are_The_Fills(ButtonFill fill, string colour) => InstalledTheme.Run(_ =>
    {
        var (button, scene, centre) = Themed(new AvonicButton { Content = "Text", Fill = fill });
        using (scene)
        {
            using var clock = new TestClock();
            Hover(scene, centre);
            Assert.Equal(Color.Parse(colour), TextColour(button));
            Assert.Equal(Color.Parse(colour), Solid(Overlay(button).Background));

            Press(scene, centre);
            clock.At(100);
            Assert.Equal(Color.Parse(colour), TextColour(button));
            Assert.Null(Overlay(button).Background);
            Release(scene, centre);
        }

        var (focused, focusScene, _) = Themed(new AvonicButton { Content = "Text", Fill = fill });
        using (focusScene)
        {
            PressKey(focusScene, PhysicalKey.Tab);
            Assert.Equal(Color.Parse(colour), TextColour(focused));
            Assert.Equal(Color.Parse(colour), Solid(Overlay(focused).Background));
        }
    });

    [AvaloniaFact]
    public void B27_Fill_Default_State_Colours_Inherit_Without_Tokens() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Fill = ButtonFill.Default };
        var holder = new StackPanel { Margin = new Thickness(20), [TextElement.ForegroundProperty] = Brushes.Green, Children = { button } };
        using var scene = new Scene(NoTransitions(holder));
        using var clock = new TestClock();
        var centre = Box(button, scene.Window).Center;

        Assert.Equal(Colors.Green, Solid(button.Foreground));

        Hover(scene, centre);
        Assert.Equal(Colors.Green, Solid(button.Foreground));
        Assert.Null(Overlay(button).Background);

        Press(scene, centre);
        clock.At(100);
        Assert.Equal(Colors.Green, Solid(button.Foreground));
        Release(scene, centre);
    });

    [AvaloniaFact]
    public void B27_Fill_Default_Focus_Inherits_Without_Tokens() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Fill = ButtonFill.Default };
        var holder = new StackPanel { [TextElement.ForegroundProperty] = Brushes.Green, Children = { button } };
        using var scene = new Scene(NoTransitions(holder));

        PressKey(scene, PhysicalKey.Tab);

        Assert.True(Has(button, FocusVisible.FocusedPseudoClass));
        Assert.Equal(Colors.Green, Solid(button.Foreground));
        Assert.Null(Overlay(button).Background);
    });

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid)]
    [InlineData(ButtonFill.Outline)]
    [InlineData(ButtonFill.Clear)]
    [InlineData(ButtonFill.Default)]
    public void B27_The_Background_And_Shadow_Tokens_Apply_On_Every_Fill(ButtonFill fill) => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Fill = fill };
        button.Resources["Avonic.Button.Background"] = Brushes.Green;
        button.Resources["Avonic.Button.BoxShadow"] = BoxShadows.Parse("0 1 1 0 #FF000000");
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            using var clock = new TestClock();
            Assert.Equal(Colors.Green, Solid(Native(button).Background));
            Assert.Equal(BoxShadows.Parse("0 1 1 0 #FF000000"), Native(button).BoxShadow);

            // It beats solid's raised shadow too, as a variable set from outside does.
            Press(scene, centre);
            clock.At(100);
            Assert.Equal(BoxShadows.Parse("0 1 1 0 #FF000000"), Native(button).BoxShadow);
            Release(scene, centre);
        }
    });

    [AvaloniaTheory]
    [InlineData(null, null, 15.4, 8)]
    [InlineData(ButtonShape.Round, null, 26, 0)]
    [InlineData(null, ButtonSize.Small, 11.7, 4)]
    [InlineData(null, ButtonSize.Large, 20, 14)]
    public void B27_Padding_Tokens_Apply_In_Every_Class(ButtonShape? shape, ButtonSize? size, double inline, double block) => InstalledTheme.Run(_ =>
    {
        void Check(string key, Func<Thickness> expected)
        {
            var button = new AvonicButton { Content = "Text", Shape = shape, Size = size };
            button.Resources[key] = 31d;
            var (_, scene, _) = Themed(button);
            using (scene)
            {
                var actual = Inner(button).Margin;
                var wanted = expected();
                AssertThickness(wanted.Left, wanted.Top, wanted.Right, wanted.Bottom, actual);
            }
        }

        Check("Avonic.Button.PaddingStart", () => new Thickness(31, block, inline, block));
        Check("Avonic.Button.PaddingTop", () => new Thickness(inline, 31, inline, block));
        Check("Avonic.Button.PaddingEnd", () => new Thickness(inline, block, 31, block));
        Check("Avonic.Button.PaddingBottom", () => new Thickness(inline, block, inline, 31));
    });

    [AvaloniaFact]
    public void B27_Padding_Tokens_Apply_On_Icon_Only() => InstalledTheme.Run(_ =>
    {
        void Check(string key, double left, double top, double right, double bottom)
        {
            var button = new AvonicButton { IconOnlyContent = new AvonicIcon { IconName = "heart" } };
            button.Resources[key] = 31d;
            var (_, scene, _) = Themed(button);
            using (scene)
            {
                AssertThickness(left, top, right, bottom, Inner(button).Margin);
            }
        }

        Check("Avonic.Button.PaddingStart", 31, 0, 0, 0);
        Check("Avonic.Button.PaddingBottom", 0, 0, 0, 31);
    });

    [AvaloniaFact]
    public void B27_Padding_Tokens_Apply_In_An_Item() => InstalledTheme.Run(_ =>
    {
        void Check(string key, double left, double top, double right, double bottom)
        {
            var button = new AvonicButton { Content = "Text" };
            button.Resources[key] = 31d;
            using var scene = new Scene(NoTransitions(new AvonicItem { EndContent = button }));
            AssertThickness(left, top, right, bottom, Inner(button).Margin);
        }

        Check("Avonic.Button.PaddingStart", 31, 2, 7.2, 2);
        Check("Avonic.Button.PaddingTop", 7.2, 31, 7.2, 2);
        Check("Avonic.Button.PaddingEnd", 7.2, 2, 31, 2);
        Check("Avonic.Button.PaddingBottom", 7.2, 2, 7.2, 31);
    });

    [AvaloniaFact]
    public void B27_Padding_End_And_Bottom_Apply() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        button.Resources["Avonic.Button.PaddingEnd"] = 31d;
        button.Resources["Avonic.Button.PaddingBottom"] = 3d;
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            AssertThickness(15.4, 8, 31, 3, Inner(button).Margin);
        }
    });

    [AvaloniaFact]
    public void B27_A_Token_Beats_Rounds_Defaults_Measured_Varsr() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Shape = ButtonShape.Round };
        button.Resources["Avonic.Button.PaddingStart"] = 30d;
        button.Resources["Avonic.Button.BorderRadius"] = new CornerRadius(3);
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            Assert.Equal(new CornerRadius(3), Native(button).CornerRadius);
            AssertThickness(30, 0, 26, 0, Inner(button).Margin);
        }
    });

    [AvaloniaFact]
    public void B27_Outline_Border_Tokens_Measured_Varso() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Fill = ButtonFill.Outline };
        button.Resources["Avonic.Button.BorderWidth"] = new Thickness(5);
        button.Resources["Avonic.Button.BorderColor"] = Brushes.Green;
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            Assert.Equal(new Thickness(5), Native(button).BorderThickness);
            Assert.Equal(Colors.Green, Solid(Native(button).BorderBrush));

            // 14 of text, 16 of padding and 10 of border, past the minimum of 36
            Assert.Equal(40, button.Bounds.Height);
        }
    });

    [AvaloniaTheory]
    [InlineData(ButtonFill.Solid)]
    [InlineData(ButtonFill.Clear)]
    public void B27_Border_Tokens_Draw_Nothing_Without_Outline_Measured_Varss(ButtonFill fill) => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Fill = fill };
        button.Resources["Avonic.Button.BorderWidth"] = new Thickness(5);
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            Assert.Equal(default, Native(button).BorderThickness);
        }
    });

    [AvaloniaFact]
    public void B27_The_Background_Token_Works_On_Flat_Fills() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Fill = ButtonFill.Clear };
        button.Resources["Avonic.Button.Background"] = Brushes.Green;
        button.Resources["Avonic.Button.BoxShadow"] = BoxShadows.Parse("0 1 1 0 #FF000000");
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            Assert.Equal(Colors.Green, Solid(Native(button).Background));
            Assert.NotEqual(default, Native(button).BoxShadow);
        }
    });

    [AvaloniaFact]
    public void B27_A_Radius_Clips_What_Is_Inside() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", Width = 200, Height = 80 };
        button.Resources["Avonic.Button.BorderRadius"] = new CornerRadius(30);
        button.Resources["Avonic.Button.Background"] = Brushes.White;
        button.Resources["Avonic.Button.BoxShadow"] = BoxShadows.Parse("none");
        button.Resources["Avonic.Button.BackgroundHover"] = Brushes.Black;
        button.Resources["Avonic.Button.BackgroundHoverOpacity"] = 1d;
        var (_, scene, centre) = Themed(button);
        using (scene)
        {
            Hover(scene, centre);
            var box = Box(button, scene.Window);

            Assert.Equal(Colors.Black, Pixel(scene, (int)box.Center.X, (int)box.Y + 2));
            Assert.Equal(Colors.White, Pixel(scene, (int)box.X + 1, (int)box.Y + 1));
        }
    });

    // ── Behaviours 28-32: in an item ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B28_In_An_Item_Null_Is_Small_At_Any_Depth() => InstalledTheme.Run(_ =>
    {
        var direct = new AvonicButton { Content = "Text" };
        var deep = new AvonicButton { Content = "Text" };
        var standard = new AvonicButton { Content = "Text", Size = ButtonSize.Default };
        var large = new AvonicButton { Content = "Text", Size = ButtonSize.Large };
        var alone = new AvonicButton { Content = "Text" };
        using var scene = new Scene(NoTransitions(new StackPanel
        {
            Children =
            {
                new AvonicItem { Content = direct },
                new AvonicItem { Content = new Border { Child = new Border { Child = deep } } },
                new AvonicItem { Content = standard },
                new AvonicItem { Content = large },
                alone,
            },
        }));

        // Measured: 12 directly in a slot, 13 deeper, 14 or 20 when it's set, and 14 outside an item.
        Assert.Equal(12, direct.FontSize);
        Assert.Equal(13, deep.FontSize);
        Assert.Equal(14, standard.FontSize);
        Assert.Equal(20, large.FontSize);
        Assert.Equal(14, alone.FontSize);
    });

    [AvaloniaFact]
    public void B28_Moving_To_An_Item_Follows_It() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        var holder = new Border { Child = button };
        var item = new AvonicItem();
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { holder, item } }));
        Assert.Equal(14, button.FontSize);

        holder.Child = null;
        item.Content = button;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(12, button.FontSize);

        item.Content = null;
        holder.Child = button;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(14, button.FontSize);
    });

    [AvaloniaFact]
    public void B28_Size_Changes_Are_Followed_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        using var scene = new Scene(NoTransitions(new AvonicItem { Content = button }));
        Assert.Equal(12, button.FontSize);

        button.Size = ButtonSize.Default;
        Assert.Equal(14, button.FontSize);

        button.Size = null;
        Assert.Equal(12, button.FontSize);
    });

    [AvaloniaFact]
    public void B29_Directly_In_A_Slot_A_Small_Button_Is_Smaller_Still() => InstalledTheme.Run(_ =>
    {
        var implicit_ = new AvonicButton { Content = "Text" };
        var explicit_ = new AvonicButton { Content = "Text", Size = ButtonSize.Small };
        var standard = new AvonicButton { Content = "Text", Size = ButtonSize.Default };
        using var scene = new Scene(NoTransitions(new StackPanel
        {
            Children =
            {
                new AvonicItem { Content = implicit_ },
                new AvonicItem { EndContent = explicit_ },
                new AvonicItem { StartContent = standard },
            },
        }));

        foreach (var button in new[] { implicit_, explicit_ })
        {
            Assert.Equal(12, button.FontSize);
            Assert.Equal(25, button.Bounds.Height);
            AssertThickness(7.2, 2, 7.2, 2, Inner(button).Margin);
        }

        Assert.Equal(14, standard.FontSize);
        Assert.Equal(36, standard.Bounds.Height);
        AssertThickness(15.4, 8, 15.4, 8, Inner(standard).Margin);
    });

    [AvaloniaFact]
    public void B29_Icon_Only_In_A_Slot_Measured_Initemo() => InstalledTheme.Run(_ =>
    {
        var glyph = new AvonicIcon { IconName = "heart" };
        var button = new AvonicButton { IconOnlyContent = glyph };
        using var scene = new Scene(NoTransitions(new AvonicItem { EndContent = button }));

        Assert.Equal(25, button.MinHeight, 2);
        Assert.Equal(25.92, button.MinWidth, 2);
        Assert.Equal(14.775, glyph.FontSize, 3);
        AssertThickness(7.2, 2, 7.2, 2, Inner(button).Margin);
        Assert.Equal(25, button.Bounds.Height);
        Assert.Equal(29.17, button.Bounds.Width, 0);
    });

    [AvaloniaFact]
    public void B29_Deeper_Gets_The_Plain_Small_Values_Measured_Initemdeep() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        using var scene = new Scene(NoTransitions(new AvonicItem { Content = new Border { Child = button } }));

        Assert.Equal(13, button.FontSize);
        Assert.Equal(27.3, button.MinHeight, 2);
        AssertThickness(11.7, 4, 11.7, 4, Inner(button).Margin);
        Assert.Equal(new Thickness(2, 4), button.Margin);
    });

    [AvaloniaFact]
    public void B29_A_Panel_Directly_In_A_Slot_Stands_For_It() => InstalledTheme.Run(_ =>
    {
        var first = new AvonicButton { Content = "One" };
        var second = new AvonicButton { Content = "Two" };
        using var scene = new Scene(NoTransitions(new AvonicItem
        {
            EndContent = new StackPanel { Orientation = Orientation.Horizontal, Children = { first, second } },
        }));

        Assert.Equal(12, first.FontSize);
        Assert.Equal(25, second.Bounds.Height);
        AssertThickness(7.2, 2, 7.2, 2, Inner(second).Margin);
    });

    [AvaloniaFact]
    public void B29_Tokens_Still_Beat_The_Items_Values() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        button.Resources["Avonic.Button.PaddingStart"] = 30d;
        using var scene = new Scene(NoTransitions(new AvonicItem { EndContent = button }));

        AssertThickness(30, 2, 7.2, 2, Inner(button).Margin);
    });

    [AvaloniaFact]
    public void B30_Margins_In_A_Slot() => InstalledTheme.Run(_ =>
    {
        var content = new AvonicButton { Content = "Text" };
        var start = new AvonicButton { Content = "Text" };
        var end = new AvonicButton { Content = "Text" };
        var nested = new AvonicButton { Content = "Text" };
        using var scene = new Scene(NoTransitions(new StackPanel
        {
            Children =
            {
                new AvonicItem { Content = content, StartContent = start, EndContent = end },
                new AvonicItem { EndContent = new Border { Child = nested } },
            },
        }));

        Assert.Equal(new Thickness(2, 4), content.Margin);
        Assert.Equal(new Thickness(2, 4, 16, 4), start.Margin);
        Assert.Equal(new Thickness(16, 4, 2, 4), end.Margin);
        Assert.Equal(new Thickness(2, 4), nested.Margin);

        // Measured initems: x = 18, and initeme ends at 372, against an item with 16 and 16 of padding.
        Assert.Equal(18, Box(start, scene.Window).X, 0);
        Assert.Equal(372, Box(end, scene.Window).Right, 0);
    });

    // closest('ion-item-divider') counts too. Measured divs, dive and divm: small, 27.3 high, at x = 18 and ending at 372,
    // as in an item, with 4px 16px 4px 2px at the start, 4px 2px 4px 16px at the end and 4px 2px in the default slot. A
    // divider has no ::slotted(.button-small) rule, so it stays 27.3 high where an item's is 25.
    [AvaloniaFact]
    public void B30_In_A_Divider_It_Is_Small_With_The_Same_Margins() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicButton { Content = "Text" };
        var end = new AvonicButton { Content = "Text" };
        var main = new AvonicButton { Content = "Text" };
        using var scene = new Scene(NoTransitions(new StackPanel
        {
            Children =
            {
                new AvonicItemDivider { StartContent = start, Content = "x", EndContent = end },
                new AvonicItemDivider { Content = main },
            },
        }));

        Assert.All(new[] { start, end, main }, button =>
        {
            Assert.Equal(13, button.FontSize);
            Assert.Equal(27.3, button.MinHeight, 2);
        });
        Assert.Equal(new Thickness(2, 4, 16, 4), start.Margin);
        Assert.Equal(new Thickness(16, 4, 2, 4), end.Margin);
        Assert.Equal(new Thickness(2, 4), main.Margin);
        Assert.Equal(18, Box(start, scene.Window).X, 0);
        Assert.Equal(372, Box(end, scene.Window).Right, 0);
    });

    // The slot rules are in Item.axaml and ItemDivider.axaml, after the rule for any child in a slot, with no activator. A plain
    // rule in Button.axaml would lose to that rule merged first and win merged last.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void B30_The_Slot_Rules_Hold_Whatever_Order_The_Files_Merge_In(bool first) =>
        AvonicAvatarTests.InMergeOrder("Button.axaml", first, () =>
        {
            var itemStart = new AvonicButton { Content = "Text" };
            var itemEnd = new AvonicButton { Content = "Text" };
            var dividerStart = new AvonicButton { Content = "Text" };
            var dividerEnd = new AvonicButton { Content = "Text" };
            var stacked = new AvonicButton { Content = "Text" };
            using var scene = new Scene(NoTransitions(new StackPanel
            {
                Children =
                {
                    new AvonicItem { StartContent = itemStart, Content = "x", EndContent = itemEnd },
                    new AvonicItemDivider { StartContent = dividerStart, Content = "x", EndContent = dividerEnd },
                    new AvonicItem { Content = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked }, EndContent = stacked },
                },
            }));

            Assert.Equal(new Thickness(2, 4, 16, 4), itemStart.Margin);
            Assert.Equal(new Thickness(16, 4, 2, 4), itemEnd.Margin);
            Assert.Equal(new Thickness(2, 4, 16, 4), dividerStart.Margin);
            Assert.Equal(new Thickness(16, 4, 2, 4), dividerEnd.Margin);
            Assert.Equal(new Thickness(16, 7, 2, 7), stacked.Margin);
        });

    // Measured itemapp and divapp: a page's .app { margin: 3px } is 3px in the start and end slots of both, as a page's rule
    // beats ::slotted. Beside a stacked label it wins too (stackedapp), and here it doesn't: that rule needs the item's state,
    // which is an activator, as for every control in that slot.
    [AvaloniaFact]
    public void B30_An_Apps_Plain_Margin_Style_Beats_The_Slots() => InstalledTheme.Run(_ =>
        AvonicAvatarTests.WithAppMargin<AvonicButton>(new Thickness(3), () =>
            AvonicAvatarTests.AssertMarginInEverySlot(() => new AvonicButton { Content = "Text" }, new Thickness(3))));

    [AvaloniaFact]
    public void B31_Two_Buttons_Are_Multiple_Inputs_To_The_Item()
    {
        var one = new AvonicButton();
        var two = new AvonicButton();
        var item = new AvonicItem { Content = one };
        using var scene = new Scene(item);
        Assert.False(Has(item, ":item-multiple-inputs"));

        item.EndContent = two;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Has(item, ":item-multiple-inputs"));
        Assert.Equal(ItemControlType.Button, ((IItemControl)one).ItemControlType);
    }

    [AvaloniaFact]
    public void B31_Its_Focus_Shows_On_The_Item_And_The_Button()
    {
        var button = new AvonicButton { Width = 60, Height = 30 };
        var item = new AvonicItem { EndContent = button };
        using var scene = new Scene(item);
        Dispatcher.UIThread.RunJobs();

        Assert.True(FocusVisible.GetIsEnabled(item));

        PressKey(scene, PhysicalKey.Tab);

        Assert.True(button.IsFocused);
        Assert.True(Has(button, FocusVisible.FocusedPseudoClass));
        Assert.True(Has(item, FocusVisible.FocusedPseudoClass));
    }

    [AvaloniaFact]
    public void B31_An_Item_Tap_Would_Click_It()
    {
        var clicks = 0;
        var button = Boxed();
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        ((IItemControl)button).OnItemTap();
        Assert.Equal(1, clicks);

        button.IsEnabled = false;
        ((IItemControl)button).OnItemTap();
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B32_A_Tap_In_A_Button_Item_Clicks_Both()
    {
        var log = new List<string>();
        var button = new AvonicButton { Content = "x", Width = 60, Height = 30 };
        button.Click += (_, _) => log.Add("button");
        var item = new AvonicItem { Button = true, EndContent = button };
        item.Click += (_, _) => log.Add("item");
        using var scene = new Scene(item);

        Tap(scene, Box(button, scene.Window).Center);

        Assert.Equal(["button", "item"], log);
    }

    [AvaloniaFact]
    public void B32_A_Tap_On_A_Disabled_One_Only_Clicks_The_Item()
    {
        var log = new List<string>();
        var button = new AvonicButton { Content = "x", Width = 60, Height = 30, IsEnabled = false };
        button.Click += (_, _) => log.Add("button");
        var item = new AvonicItem { Button = true, EndContent = button };
        item.Click += (_, _) => log.Add("item");
        using var scene = new Scene(item);

        Tap(scene, Box(button, scene.Window).Center);

        Assert.Equal(["item"], log);
    }

    // ── Behaviour 33: automation ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B33_It_Is_An_Invokable_Button_Named_By_Its_Text_In_Its_Original_Case() => InstalledTheme.Run(_ =>
    {
        var clicks = 0;
        var button = new AvonicButton { Content = "Open" };
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        var peer = ControlAutomationPeer.CreatePeerForElement(button);

        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("Open", peer.GetName());
        Assert.Equal(nameof(AvonicButton), peer.GetClassName());
        Assert.True(peer.IsEnabled());
        Assert.True(peer.IsContentElement());
        Assert.True(peer.IsControlElement());
        Assert.Equal("OPEN", Text(button).Text);

        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, clicks);
    });

    [AvaloniaFact]
    public void B33_The_Name_Is_From_The_Text_In_Each_Slot_And_An_Icon_Adds_Nothing()
    {
        var both = new AvonicButton { StartContent = "One", Content = "Two", EndContent = new TextBlock { Text = "Three" } };
        var icon = new AvonicButton { IconOnlyContent = new AvonicIcon { IconName = "heart" } };
        var named = new AvonicButton { IconOnlyContent = new AvonicIcon { IconName = "heart" } };
        Avalonia.Automation.AutomationProperties.SetName(named, "Favourite");
        var number = new AvonicButton { Content = 42 };
        var panel = new AvonicButton { Content = new StackPanel { Children = { new TextBlock { Text = "Open" }, new TextBlock { Text = "file" } } } };
        using var scene = new Scene(new StackPanel { Children = { both, icon, named, number, panel } });

        Assert.Equal("One Two Three", ControlAutomationPeer.CreatePeerForElement(both).GetName());
        Assert.Equal(string.Empty, ControlAutomationPeer.CreatePeerForElement(icon).GetName());
        Assert.Equal("Favourite", ControlAutomationPeer.CreatePeerForElement(named).GetName());
        Assert.Equal("42", ControlAutomationPeer.CreatePeerForElement(number).GetName());
        Assert.Equal("Open file", ControlAutomationPeer.CreatePeerForElement(panel).GetName());
    }

    [AvaloniaFact]
    public void B33_Disabled_Reports_It_And_Invoking_Does_Nothing()
    {
        var clicks = 0;
        var button = Boxed();
        button.IsEnabled = false;
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        var peer = ControlAutomationPeer.CreatePeerForElement(button);

        Assert.False(peer.IsEnabled());
        Assert.IsAssignableFrom<IInvokeProvider>(peer).Invoke();
        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B33_The_Ripple_Is_Hidden_From_Automation()
    {
        var button = Boxed();
        using var scene = new Scene(button);

        Assert.Equal(
            Avalonia.Automation.AccessibilityView.Raw,
            Avalonia.Automation.AutomationProperties.GetAccessibilityView(Ripple(button)));
    }

    // ── Behaviour 34: RTL ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B34_Rtl_Mirrors_The_Slots_And_Margins() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var end = new AvonicIcon { IconName = "star" };
        var button = new AvonicButton { StartContent = start, Content = "RTL", EndContent = end };
        using var scene = new Scene(NoTransitions(new StackPanel { FlowDirection = FlowDirection.RightToLeft, Children = { button } }));

        // Measured rtl: at the right, with a margin of 2, and the start icon on the right.
        var box = Box(button, scene.Window);
        Assert.Equal(388, box.Right, 0);
        Assert.True(Box(start, scene.Window).X > Box(end, scene.Window).X);

        // Start sticks out by 5.67 past the padding of 15.4: measured 359.38 + 18.89 = 378.27 of 388
        Assert.InRange(box.Right - Box(start, scene.Window).Right, 9, 11);
    });

    // ── Behaviour 35: transitions ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B35_The_Transition_Token_Reaches_The_Native_Part() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(transitions: true);
        using (scene)
        {
            var transitions = Assert.IsAssignableFrom<Transitions>(Native(button).Transitions);

            var shadow = transitions.OfType<BoxShadowsTransition>().Single();
            Assert.Equal(TimeSpan.FromMilliseconds(280), shadow.Duration);
            Assert.Same(Border.BoxShadowProperty, shadow.Property);

            var brushes = transitions.OfType<BrushTransition>().ToList();
            Assert.All(brushes, brush => Assert.Equal(TimeSpan.FromMilliseconds(15), brush.Duration));
            Assert.Contains(brushes, brush => brush.Property == Border.BackgroundProperty);
            Assert.Contains(brushes, brush => brush.Property == TextElement.ForegroundProperty);
        }
    });

    [AvaloniaFact]
    public void B35_The_Shadow_Eases() => InstalledTheme.Run(_ =>
    {
        var (button, scene, centre) = Themed(transitions: true);
        using (scene)
        {
            using var clock = new TestClock();
            var low = BoxShadows.Parse("0 3 1 -2 #33000000, 0 2 2 0 #24000000, 0 1 5 0 #1F000000");
            var high = BoxShadows.Parse("0 5 5 -3 #33000000, 0 8 10 1 #24000000, 0 3 14 2 #1F000000");
            Settle(() => Native(button).BoxShadow == low);

            Press(scene, centre);
            clock.At(100);
            Dispatcher.UIThread.RunJobs();

            // The transition's start: it hasn't jumped to the end.
            Assert.NotEqual(high, Native(button).BoxShadow);

            Settle(() => Native(button).BoxShadow == high);
            Assert.Equal(high, Native(button).BoxShadow);
            Release(scene, centre);
        }
    });

    private static void Settle(Func<bool> done)
    {
        var started = DateTime.UtcNow;
        while (!done() && DateTime.UtcNow - started < TimeSpan.FromSeconds(5))
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    // ── Behaviour 36: extensions ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B36_Command_Runs_After_Click_With_Its_Parameter()
    {
        var log = new List<string>();
        var command = new TestCommand();
        var button = Boxed();
        button.Command = command;
        button.CommandParameter = "p";
        button.Click += (_, _) => log.Add($"click:{command.Executed}");
        using var scene = new Scene(button);

        Tap(scene, Centre);

        Assert.Equal(["click:0"], log);
        Assert.Equal(1, command.Executed);
        Assert.Equal("p", command.LastParameter);
    }

    [AvaloniaFact]
    public void B36_Keys_And_Automation_Run_The_Command()
    {
        var command = new TestCommand();
        var button = Boxed();
        button.Command = command;
        using var scene = new Scene(button);
        button.Focus();

        PressKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, command.Executed);

        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);
        Assert.Equal(2, command.Executed);

        ControlAutomationPeer.CreatePeerForElement(button).GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(3, command.Executed);
    }

    [AvaloniaFact]
    public void B36_A_Handled_Click_Stops_The_Command()
    {
        var command = new TestCommand();
        var button = Boxed();
        button.Command = command;
        button.Click += (_, e) => e.Handled = true;
        using var scene = new Scene(button);

        Tap(scene, Centre);

        Assert.Equal(0, command.Executed);
    }

    [AvaloniaFact]
    public void B36_Cannot_Execute_Disables_It_And_IsEnabled_Is_Untouched()
    {
        var clicks = 0;
        var command = new TestCommand { Allowed = false };
        var button = Boxed();
        button.Command = command;
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        Assert.False(button.IsEffectivelyEnabled);
        Assert.True(button.IsEnabled);
        Assert.False(ControlAutomationPeer.CreatePeerForElement(button).IsEnabled());

        Tap(scene, Centre);
        Assert.Equal(0, clicks);
        Assert.Equal(0, command.Executed);

        command.Allowed = true;
        command.Raise();

        Assert.True(button.IsEffectivelyEnabled);
        Tap(scene, Centre);
        Assert.Equal(1, clicks);
        Assert.Equal(1, command.Executed);
    }

    [AvaloniaFact]
    public void B36_Cannot_Execute_Looks_Disabled() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed(new AvonicButton { Content = "Text", Command = new TestCommand { Allowed = false } });
        using (scene)
        {
            Assert.Equal(0.5, button.Opacity);
        }
    });

    [AvaloniaFact]
    public void B36_It_Asks_Again_Just_Before_Running()
    {
        // CanExecute can go false with no CanExecuteChanged.
        var clicks = 0;
        var command = new TestCommand();
        var button = Boxed();
        button.Command = command;
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(button);

        command.Allowed = false;
        Tap(scene, Centre);

        Assert.Equal(1, clicks);
        Assert.Equal(0, command.Executed);
    }

    [AvaloniaFact]
    public void B36_A_Parameter_Change_Asks_Again()
    {
        var command = new TestCommand();
        var button = Boxed();
        button.Command = command;
        using var scene = new Scene(button);

        command.Allowed = false;
        button.CommandParameter = "new";

        Assert.Equal("new", command.LastQueried);
        Assert.False(button.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void B36_A_Command_Change_Moves_The_Subscription()
    {
        var first = new TestCommand();
        var second = new TestCommand { Allowed = false };
        var button = Boxed();
        button.Command = first;
        using var scene = new Scene(button);
        Assert.Equal(1, first.Subscribers);

        button.Command = second;

        Assert.Equal(0, first.Subscribers);
        Assert.Equal(1, second.Subscribers);
        Assert.False(button.IsEffectivelyEnabled);

        button.Command = null;

        Assert.Equal(0, second.Subscribers);
        Assert.True(button.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void B36_Only_Subscribed_While_Attached_So_It_Does_Not_Leak()
    {
        var command = new TestCommand();
        var button = Boxed();
        button.Command = command;
        Assert.Equal(0, command.Subscribers);

        var holder = new Border { Child = button };
        using var scene = new Scene(holder);
        Assert.Equal(1, command.Subscribers);

        holder.Child = null;
        Assert.Equal(0, command.Subscribers);

        // Back again, it asks afresh, because it missed anything while it was away.
        command.Allowed = false;
        holder.Child = button;
        Assert.Equal(1, command.Subscribers);
        Assert.False(button.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void B36_It_Works_In_An_Items_Slot()
    {
        var command = new TestCommand();
        var button = new AvonicButton { Width = 60, Height = 30, Command = command };
        using var scene = new Scene(new AvonicItem { EndContent = button });
        Assert.Equal(1, command.Subscribers);

        Tap(scene, Box(button, scene.Window).Center);
        Assert.Equal(1, command.Executed);
    }

    [AvaloniaFact]
    public void B36_TextTransform_None_Is_Text_Transform_None() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text", TextTransform = ButtonTextTransform.None };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        Assert.Equal("Text", Text(button).Text);

        button.TextTransform = ButtonTextTransform.Uppercase;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("TEXT", Text(button).Text);
    });

    [AvaloniaFact]
    public void B36_TextTransform_Is_None_On_The_Stub_And_Only_Touches_Strings()
    {
        var plain = Boxed("Text");
        var control = new TextBlock { Text = "Text" };
        var upper = new AvonicButton { Content = control, TextTransform = ButtonTextTransform.Uppercase };
        var number = new AvonicButton { Content = 42, TextTransform = ButtonTextTransform.Uppercase };
        using var scene = new Scene(new StackPanel { Children = { plain, upper, number } });

        Assert.Equal(ButtonTextTransform.None, plain.TextTransform);
        Assert.Equal("Text", Text(plain).Text);
        Assert.Same(control, Slot(upper, "PART_Content").Child);
        Assert.Equal("Text", control.Text);
        Assert.Equal("42", Text(number).Text);
    }

    [AvaloniaFact]
    public void B36_Content_Changes_Are_Followed() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "one" };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        button.Content = "two";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("TWO", Text(button).Text);

        button.Content = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Null(Slot(button, "PART_Content").Child);
    });

    // ── Behaviour 37: in a list header ──────────────────────────────────────────────────────

    /// <summary>Transparent: a clear button has no background, and that's null here.</summary>
    private static void AssertClear(AvonicButton button)
    {
        var background = Native(button).Background;
        Assert.True(background is null || Solid(background).A == 0, $"background {background}");
        Assert.Equal(0, Native(button).BoxShadow.Count);
        Assert.Equal(default, Native(button).BorderThickness);
    }

    private static Scene InListHeaders(params Control[] contents)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(contents.Select(content => new AvonicListHeader { Content = content }));
        return new Scene(NoTransitions(panel));
    }

    // closest('ion-list-header'), at any depth: fill resolves to clear. Measured none and deep: transparent, primary text, no
    // shadow, 36 high with margin 4 2.
    [AvaloniaFact]
    public void B37_In_A_List_Header_No_Fill_Is_Clear_At_Any_Depth() => InstalledTheme.Run(_ =>
    {
        var direct = new AvonicButton { Content = "None" };
        var deeper = new AvonicButton { Content = "Deep" };
        using var scene = InListHeaders(direct, new Grid { Children = { deeper } });

        foreach (var button in new[] { direct, deeper })
        {
            AssertClear(button);
            Assert.Equal(Primary, TextColour(button));
            Assert.Equal(36, button.MinHeight);
            Assert.Equal(new Thickness(2, 4), button.Margin);
        }
    });

    // Measured solid, outline and outside: a fill it's given wins, and a button outside a header stays solid.
    [AvaloniaFact]
    public void B37_A_Fill_It_Is_Given_Wins_And_Outside_It_Stays_Solid() => InstalledTheme.Run(_ =>
    {
        var solid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        var outline = new AvonicButton { Content = "Outline", Fill = ButtonFill.Outline };
        var outside = new AvonicButton { Content = "Outside" };
        using var scene = new Scene(NoTransitions(new StackPanel
        {
            Children = { new AvonicListHeader { Content = solid }, new AvonicListHeader { Content = outline }, outside },
        }));

        Assert.Equal(Primary, Solid(Native(solid).Background));
        Assert.Equal(Colors.White, TextColour(solid));
        Assert.Equal(3, Native(solid).BoxShadow.Count);
        Assert.Equal(new Thickness(2), Native(outline).BorderThickness);
        Assert.Equal(Primary, Solid(Native(outline).BorderBrush));
        Assert.Equal(Primary, Solid(Native(outside).Background));
    });

    // Measured incolor: in a danger header a clear button's text stays primary, as the header's colour doesn't reach it.
    [AvaloniaFact]
    public void B37_In_A_Coloured_Header_It_Stays_Primary() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "InColor" };
        using var scene = new Scene(NoTransitions(new AvonicListHeader { Color = "danger", Content = button }));

        AssertClear(button);
        Assert.Equal(Primary, TextColour(button));
    });

    // A deliberate deviation, recorded in the plan: Ionic reads closest() once, on first load. Here it's read on every attach,
    // as inItem is, so a moved or recycled button follows where it is.
    [AvaloniaFact]
    public void B37_Moving_Into_And_Out_Of_A_Header_Follows() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "A" };
        var header = new AvonicListHeader();
        var outside = new StackPanel { Children = { button } };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { header, outside } }));
        Assert.Equal(Primary, Solid(Native(button).Background));

        outside.Children.Remove(button);
        header.Content = button;
        Dispatcher.UIThread.RunJobs();
        AssertClear(button);

        header.Content = null;
        outside.Children.Add(button);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Primary, Solid(Native(button).Background));
    });

    // ── The theme's host ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Shows_A_Hand_Cursor() => InstalledTheme.Run(_ =>
    {
        var (button, scene, _) = Themed();
        using (scene)
        {
            Assert.Equal(new Cursor(StandardCursorType.Hand).ToString(), button.Cursor?.ToString());
        }
    });

    [AvaloniaFact]
    public void Uses_The_Token_Font() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        button.Resources["Avonic.FontFamily"] = new FontFamily("Courier New");
        var (_, scene, _) = Themed(button);
        using (scene)
        {
            Assert.Equal("Courier New", button.FontFamily.Name);
        }
    });

    [AvaloniaFact]
    public void Strong_Size_Shape_And_Expand_Each_Follow_Changes() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "Text" };
        using var scene = new Scene(NoTransitions(new StackPanel { Children = { button } }));

        button.Strong = true;
        Assert.Equal(FontWeight.Bold, button.FontWeight);

        button.Strong = false;
        Assert.Equal(FontWeight.Medium, button.FontWeight);

        button.Shape = ButtonShape.Round;
        Assert.Equal(new CornerRadius(999), Native(button).CornerRadius);

        button.Shape = null;
        Assert.Equal(new CornerRadius(4), Native(button).CornerRadius);

        button.Expand = ButtonExpand.Block;
        Assert.Equal(HorizontalAlignment.Stretch, button.HorizontalAlignment);

        button.Expand = ButtonExpand.Full;
        Assert.Equal(HorizontalAlignment.Stretch, button.HorizontalAlignment);
        Assert.Equal(new CornerRadius(0), Native(button).CornerRadius);

        button.Expand = null;
        Assert.Equal(HorizontalAlignment.Left, button.HorizontalAlignment);

        button.Size = ButtonSize.Large;
        Assert.Equal(20, button.FontSize);

        button.Size = ButtonSize.Small;
        Assert.Equal(13, button.FontSize);

        button.Size = null;
        Assert.Equal(14, button.FontSize);
    });

    [AvaloniaFact]
    public void Works_On_The_Stub_With_No_Theme()
    {
        var clicks = 0;
        var start = new TextBlock { Text = "s" };
        var end = new TextBlock { Text = "e" };
        var icon = new TextBlock { Text = "i" };
        var button = new AvonicButton { Content = "x", StartContent = start, EndContent = end, IconOnlyContent = icon };
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(new StackPanel { Children = { button } });

        Assert.Same(start, Slot(button, "PART_Start").Child);
        Assert.Same(end, Slot(button, "PART_End").Child);
        Assert.Same(icon, Slot(button, "PART_IconOnly").Child);
        Assert.Equal("x", Text(button).Text);
        Assert.Single(Native(button).GetVisualDescendants().OfType<AvonicRippleEffect>());

        Tap(scene, Box(button, scene.Window).Center);
        Assert.Equal(1, clicks);
    }
}
