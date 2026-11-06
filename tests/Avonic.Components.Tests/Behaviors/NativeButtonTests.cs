using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Behaviors;

/// <summary>
/// What a native button does, on each control that has one. The numbers are from Chromium, under "Shared press handling" in
/// docs/components/ion-item/PLAN.md.
/// </summary>
public class NativeButtonTests
{
    private static readonly Point Centre = new(100, 25);

    public static TheoryData<string> Controls() => ["button", "item", "card"];

    private sealed class Probe(Control control, Func<int> clicks)
    {
        public Control Control { get; } = control;

        public int Clicks => clicks();
    }

    /// <summary>A control with a child in it, 200 by 50.</summary>
    private static Probe Make(string kind, Control? child = null)
    {
        var clicks = 0;
        Control control;
        switch (kind)
        {
            case "button":
                var button = new AvonicButton { Content = child };
                button.Click += (_, _) => clicks++;
                control = button;
                break;
            case "item":
                var item = new AvonicItem { Button = true, Content = child };
                item.Click += (_, _) => clicks++;
                control = item;
                break;
            default:
                var card = new AvonicCard { Button = true, Content = child };
                card.Click += (_, _) => clicks++;
                control = card;
                break;
        }

        control.Width = 200;
        control.Height = 50;
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Top;
        return new Probe(control, () => clicks);
    }

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static void Tap(Scene scene, Point point)
    {
        scene.Window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Settle();
    }

    private static PointerPointProperties Pressed => new(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed);

    private static PointerPointProperties Released => new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased);

    private static void TouchDown(Control control, Pointer finger, Scene scene, Point at)
    {
        control.RaiseEvent(new PointerPressedEventArgs(control, finger, scene.Window, at, 0, Pressed, KeyModifiers.None));
    }

    private static void TouchMove(Control control, Pointer finger, Scene scene, Point at)
    {
        control.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, control, finger, scene.Window, at, 0, new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
    }

    private static void TouchUp(Control control, Pointer finger, Scene scene, Point at)
    {
        control.RaiseEvent(new PointerReleasedEventArgs(control, finger, scene.Window, at, 0, Released, KeyModifiers.None, MouseButton.Left));
    }

    private static Pointer Finger() => new(Pointer.GetNextFreeId(), PointerType.Touch, true);

    private sealed class Command : ICommand
    {
        private bool _can = true;

        public int Executed { get; private set; }

        public object? LastParameter { get; private set; }

        public bool Can
        {
            get => _can;
            set
            {
                _can = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _can;

        public void Execute(object? parameter)
        {
            Executed++;
            LastParameter = parameter;
        }
    }

    // ── S1: a finger ─────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData("button", 0, 1)]
    [InlineData("button", 14, 1)]
    [InlineData("button", 16, 0)]
    [InlineData("button", 40, 0)]
    [InlineData("item", 0, 1)]
    [InlineData("item", 14, 1)]
    [InlineData("item", 16, 0)]
    [InlineData("card", 14, 1)]
    [InlineData("card", 16, 0)]
    public void S1_A_Finger_That_Moves_Too_Far_Does_Not_Click(string kind, double moved, int clicks)
    {
        var probe = Make(kind);
        using var scene = new Scene(probe.Control);
        var finger = Finger();
        var end = new Point(Centre.X + moved, Centre.Y);

        TouchDown(probe.Control, finger, scene, Centre);
        TouchMove(probe.Control, finger, scene, end);
        TouchUp(probe.Control, finger, scene, end);

        Assert.Equal(clicks, probe.Clicks);
    }

    [AvaloniaFact]
    public void S1_A_Finger_That_Moved_Too_Far_And_Came_Back_Does_Not_Click()
    {
        var probe = Make("button");
        using var scene = new Scene(probe.Control);
        var finger = Finger();

        TouchDown(probe.Control, finger, scene, Centre);
        TouchMove(probe.Control, finger, scene, new Point(Centre.X + 40, Centre.Y));
        TouchMove(probe.Control, finger, scene, Centre);
        TouchUp(probe.Control, finger, scene, Centre);

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaFact]
    public void S1_The_Limit_Is_A_Distance_In_Any_Direction()
    {
        var probe = Make("button");
        using var scene = new Scene(probe.Control);
        var finger = Finger();
        var end = new Point(Centre.X + 12, Centre.Y + 12);

        TouchDown(probe.Control, finger, scene, Centre);
        TouchMove(probe.Control, finger, scene, end);
        TouchUp(probe.Control, finger, scene, end);

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaFact]
    public void S1_The_Next_Press_Is_Not_Held_To_The_Last_Ones_Movement()
    {
        var probe = Make("button");
        using var scene = new Scene(probe.Control);
        var finger = Finger();
        var far = new Point(Centre.X + 40, Centre.Y);

        TouchDown(probe.Control, finger, scene, Centre);
        TouchMove(probe.Control, finger, scene, far);
        TouchUp(probe.Control, finger, scene, far);
        TouchDown(probe.Control, finger, scene, far);
        TouchUp(probe.Control, finger, scene, far);

        Assert.Equal(1, probe.Clicks);
    }

    // ── S2, S3: a mouse ──────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void S2_A_Mouse_Has_No_Movement_Limit(string kind)
    {
        var probe = Make(kind);
        using var scene = new Scene(probe.Control);
        var end = new Point(Centre.X + 60, Centre.Y);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseMove(end, RawInputModifiers.None);
        scene.Window.MouseUp(end, MouseButton.Left, RawInputModifiers.None);
        Settle();

        Assert.Equal(1, probe.Clicks);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void S2_Released_Off_The_Control_Does_Not_Click(string kind)
    {
        var probe = Make(kind);
        using var scene = new Scene(probe.Control);
        var off = new Point(300, 300);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseMove(off, RawInputModifiers.None);
        scene.Window.MouseUp(off, MouseButton.Left, RawInputModifiers.None);
        Settle();

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void S2_Every_Quick_Press_Clicks(string kind)
    {
        var probe = Make(kind);
        using var scene = new Scene(probe.Control);

        Tap(scene, Centre);
        Tap(scene, Centre);
        Tap(scene, Centre);

        Assert.Equal(3, probe.Clicks);
    }

    [AvaloniaTheory]
    [InlineData("button", MouseButton.Right)]
    [InlineData("button", MouseButton.Middle)]
    [InlineData("item", MouseButton.Right)]
    [InlineData("item", MouseButton.Middle)]
    [InlineData("card", MouseButton.Right)]
    [InlineData("card", MouseButton.Middle)]
    public void S3_Only_The_Left_Button_Clicks(string kind, MouseButton button)
    {
        var probe = Make(kind);
        using var scene = new Scene(probe.Control);

        scene.Window.MouseDown(Centre, button, RawInputModifiers.None);
        scene.Window.MouseUp(Centre, button, RawInputModifiers.None);
        Settle();

        Assert.Equal(0, probe.Clicks);
    }

    // ── Disabled ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void Disabled_During_The_Press_Does_Not_Click(string kind)
    {
        var probe = Make(kind);
        using var scene = new Scene(probe.Control);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        Settle();
        probe.Control.IsEnabled = false;
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Settle();

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void Enabled_Under_The_Finger_Does_Not_Lose_The_Next_Tap(string kind)
    {
        var probe = Make(kind);
        probe.Control.IsEnabled = false;
        using var scene = new Scene(probe.Control);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        Settle();
        probe.Control.IsEnabled = true;
        Settle();
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Settle();
        Assert.Equal(0, probe.Clicks);

        Tap(scene, Centre);
        Assert.Equal(1, probe.Clicks);
    }

    [AvaloniaFact]
    public void Disabled_Through_A_Parent_During_The_Press_Does_Not_Click()
    {
        var probe = Make("item");
        var parent = new StackPanel { Children = { probe.Control } };
        using var scene = new Scene(parent);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        Settle();
        parent.IsEnabled = false;
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Settle();

        Assert.Equal(0, probe.Clicks);
    }

    // ── S4, and what stops a click ───────────────────────────────────────────────────────────

    private static Border Child() => new() { Width = 60, Height = 30, Background = Brushes.Gray };

    private static Button StockButton() => new()
    {
        Width = 60,
        Height = 30,
        Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Gray }),
    };

    private static Point CentreOf(Visual visual, Scene scene) => visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), scene.Window)!.Value;

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void S4_A_Stock_Button_Inside_Clicks_Both(string kind)
    {
        var inner = 0;
        var child = StockButton();
        child.Click += (_, _) => inner++;
        var probe = Make(kind, child);
        using var scene = new Scene(probe.Control);

        Tap(scene, CentreOf(child, scene));

        Assert.Equal(1, inner);
        Assert.Equal(1, probe.Clicks);
    }

    [AvaloniaTheory]
    [InlineData("item")]
    [InlineData("card")]
    public void S4_An_AvonicButton_Inside_Clicks_Both(string kind)
    {
        var inner = 0;
        var child = new AvonicButton { Content = "x", Width = 60, Height = 30 };
        child.Click += (_, _) => inner++;
        var probe = Make(kind, child);
        using var scene = new Scene(probe.Control);

        Tap(scene, CentreOf(child, scene));

        Assert.Equal(1, inner);
        Assert.Equal(1, probe.Clicks);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void A_Child_That_Handles_The_Press_Stops_The_Click_Every_Time(string kind)
    {
        var child = Child();
        child.PointerPressed += (_, e) => e.Handled = true;
        var probe = Make(kind, child);
        using var scene = new Scene(probe.Control);
        var at = CentreOf(child, scene);

        Tap(scene, at);
        Tap(scene, at);

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void A_Child_That_Handles_Tapped_Does_Not_Stop_The_Click(string kind)
    {
        var child = Child();
        child.Tapped += (_, e) => e.Handled = true;
        var probe = Make(kind, child);
        using var scene = new Scene(probe.Control);

        Tap(scene, CentreOf(child, scene));

        Assert.Equal(1, probe.Clicks);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void A_Stock_Buttons_Handled_Click_Stops_The_One_Around_It(string kind)
    {
        var child = StockButton();
        child.Click += (_, e) => e.Handled = true;
        var probe = Make(kind, child);
        using var scene = new Scene(probe.Control);

        Tap(scene, CentreOf(child, scene));

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaTheory]
    [InlineData("item")]
    [InlineData("card")]
    public void An_AvonicButtons_Handled_Click_Stops_The_One_Around_It(string kind)
    {
        var child = new AvonicButton { Content = "x", Width = 60, Height = 30 };
        child.Click += (_, e) => e.Handled = true;
        var probe = Make(kind, child);
        using var scene = new Scene(probe.Control);

        Tap(scene, CentreOf(child, scene));

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaFact]
    public void A_Nested_Items_Handled_Click_Stops_The_Card_Around_It()
    {
        var inner = new AvonicItem { Button = true, Content = "x", Height = 30 };
        inner.Click += (_, e) => e.Handled = true;
        var probe = Make("card", inner);
        using var scene = new Scene(probe.Control);

        Tap(scene, CentreOf(inner, scene));

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaFact]
    public void A_Nested_Cards_Handled_Click_Stops_The_Item_Around_It()
    {
        var inner = new AvonicCard { Button = true, Content = "x", Height = 30 };
        inner.Click += (_, e) => e.Handled = true;
        var probe = Make("item", inner);
        using var scene = new Scene(probe.Control);

        Tap(scene, CentreOf(inner, scene));

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaFact]
    public void A_Handled_Click_On_Press_Stops_The_Press_It_Was_In()
    {
        var handle = true;
        var child = StockButton();
        child.ClickMode = ClickMode.Press;
        child.Click += (_, e) => e.Handled = handle;
        var probe = Make("item", child);
        using var scene = new Scene(probe.Control);
        var at = CentreOf(child, scene);

        Tap(scene, at);
        Assert.Equal(0, probe.Clicks);

        handle = false;
        Tap(scene, at);
        Assert.Equal(1, probe.Clicks);
    }

    [AvaloniaFact]
    public void A_Forward_With_No_Tapped_After_It_Does_Not_Swallow_A_Later_Tapped()
    {
        // Enter forwards, and no Tapped follows. A child then takes a press, so nothing forwards, and Tapped is still raised.
        var toggle = new ForwardingStub { Width = 40, Height = 30 };
        var child = Child();
        child.PointerPressed += (_, e) => e.Handled = true;
        var item = new AvonicItem { Button = true, Content = child, EndContent = toggle, Width = 200, Height = 50, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var tapped = 0;
        var parent = new StackPanel { Children = { item } };
        parent.Tapped += (_, _) => tapped++;
        using var scene = new Scene(parent);
        item.Focus();
        Settle();

        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(1, toggle.ItemTaps);

        Tap(scene, CentreOf(child, scene));

        Assert.Equal(1, tapped);
    }

    private sealed class ForwardingStub : Border, IItemControl
    {
        public int ItemTaps { get; private set; }

        public ItemControlType ItemControlType => ItemControlType.Toggle;

        public void OnItemTap() => ItemTaps++;
    }

    [AvaloniaFact]
    public void A_Handled_Click_Only_Stops_That_Press()
    {
        var handle = true;
        var child = StockButton();
        child.Click += (_, e) => e.Handled = handle;
        var probe = Make("item", child);
        using var scene = new Scene(probe.Control);
        var at = CentreOf(child, scene);

        Tap(scene, at);
        handle = false;
        Tap(scene, at);

        Assert.Equal(1, probe.Clicks);
    }

    // ── Keys ─────────────────────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void Enter_Clicks_On_Press_And_Space_On_Release(string kind)
    {
        var probe = Make(kind);
        using var scene = new Scene(probe.Control);
        probe.Control.Focus();
        Settle();

        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(1, probe.Clicks);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(1, probe.Clicks);

        scene.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Equal(1, probe.Clicks);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.Equal(2, probe.Clicks);
    }

    [AvaloniaTheory]
    [InlineData("item")]
    [InlineData("card")]
    public void Space_Held_While_It_Stops_Being_A_Button_Does_Nothing(string kind)
    {
        var probe = Make(kind);
        using var scene = new Scene(probe.Control);
        probe.Control.Focus();
        Settle();

        scene.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        probe.Control.SetValue(kind == "item" ? AvonicItem.ButtonProperty : AvonicCard.ButtonProperty, false);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaFact]
    public void Space_Held_When_Focus_Leaves_Does_Nothing()
    {
        var probe = Make("button");
        var other = new Button { Content = "y" };
        using var scene = new Scene(new StackPanel { Children = { probe.Control, other } });
        probe.Control.Focus();
        Settle();

        scene.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        other.Focus();
        Settle();
        scene.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);

        Assert.Equal(0, probe.Clicks);
    }

    [AvaloniaFact]
    public void A_Plain_Item_Takes_No_Keys()
    {
        var clicks = 0;
        var item = new AvonicItem { Focusable = true };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);
        item.Focus();
        Settle();

        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Assert.Equal(0, clicks);
    }

    // ── Command ──────────────────────────────────────────────────────────────────────────────

    private static void SetCommand(Control control, ICommand command, object? parameter = null)
    {
        control.SetValue(AvonicButton.CommandProperty, command);
        control.SetValue(AvonicButton.CommandParameterProperty, parameter);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void A_Click_Runs_The_Command_With_Its_Parameter(string kind)
    {
        var probe = Make(kind);
        var command = new Command();
        SetCommand(probe.Control, command, "x");
        using var scene = new Scene(probe.Control);

        Tap(scene, Centre);

        Assert.Equal(1, command.Executed);
        Assert.Equal("x", command.LastParameter);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void A_Handled_Click_Stops_The_Command(string kind)
    {
        var probe = Make(kind);
        probe.Control.AddHandler(
            kind switch { "button" => AvonicButton.ClickEvent, "item" => AvonicItem.ClickEvent, _ => AvonicCard.ClickEvent },
            (object? _, RoutedEventArgs e) => e.Handled = true);
        var command = new Command();
        SetCommand(probe.Control, command);
        using var scene = new Scene(probe.Control);

        Tap(scene, Centre);

        Assert.Equal(0, command.Executed);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void A_Command_That_Cannot_Execute_Disables_It(string kind)
    {
        var probe = Make(kind);
        var command = new Command { Can = false };
        SetCommand(probe.Control, command);
        using var scene = new Scene(probe.Control);

        Assert.False(probe.Control.IsEffectivelyEnabled);

        command.Can = true;
        Assert.True(probe.Control.IsEffectivelyEnabled);
    }

    [AvaloniaTheory]
    [InlineData("item")]
    [InlineData("card")]
    public void A_Command_Does_Nothing_Without_Button(string kind)
    {
        var command = new Command { Can = false };
        Control control = kind == "item" ? new AvonicItem() : new AvonicCard();
        SetCommand(control, command);
        control.Width = 200;
        control.Height = 50;
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.VerticalAlignment = VerticalAlignment.Top;
        using var scene = new Scene(control);

        Assert.True(control.IsEffectivelyEnabled);

        command.Can = true;
        Tap(scene, new Point(100, 25));
        Assert.Equal(0, command.Executed);
    }

    [AvaloniaTheory]
    [InlineData("item")]
    [InlineData("card")]
    public void Becoming_A_Button_Respects_The_Command(string kind)
    {
        var command = new Command { Can = false };
        Control control = kind == "item" ? new AvonicItem() : new AvonicCard();
        SetCommand(control, command);
        using var scene = new Scene(control);
        Assert.True(control.IsEffectivelyEnabled);

        control.SetValue(kind == "item" ? AvonicItem.ButtonProperty : AvonicCard.ButtonProperty, true);

        Assert.False(control.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void The_Command_Is_Not_Listened_To_Off_The_Tree()
    {
        var command = new Command();
        var probe = Make("item");
        SetCommand(probe.Control, command);
        var root = new StackPanel { Children = { probe.Control } };
        using var scene = new Scene(root);

        root.Children.Clear();
        command.Can = false;
        Assert.True(probe.Control.IsEnabled);

        root.Children.Add(probe.Control);
        Settle();
        Assert.False(probe.Control.IsEffectivelyEnabled);
    }

    [AvaloniaTheory]
    [InlineData("item")]
    [InlineData("card")]
    public void Automation_Invoke_Runs_The_Command(string kind)
    {
        var probe = Make(kind);
        var command = new Command();
        SetCommand(probe.Control, command);
        using var scene = new Scene(probe.Control);

        var peer = ControlAutomationPeer.CreatePeerForElement(probe.Control);
        Assert.IsAssignableFrom<Avalonia.Automation.Provider.IInvokeProvider>(peer.GetProvider<Avalonia.Automation.Provider.IInvokeProvider>()).Invoke();

        Assert.Equal(1, command.Executed);
    }

    [AvaloniaTheory]
    [InlineData("item")]
    [InlineData("card")]
    public void Automation_Says_A_Command_That_Cannot_Execute_Is_Disabled_And_Does_Not_Invoke(string kind)
    {
        var clicks = 0;
        var probe = Make(kind);
        probe.Control.AddHandler(kind == "item" ? AvonicItem.ClickEvent : AvonicCard.ClickEvent, (object? _, RoutedEventArgs _) => clicks++);
        SetCommand(probe.Control, new Command { Can = false });
        using var scene = new Scene(probe.Control);

        var peer = ControlAutomationPeer.CreatePeerForElement(probe.Control);
        Assert.False(peer.IsEnabled());

        peer.GetProvider<Avalonia.Automation.Provider.IInvokeProvider>()!.Invoke();
        Assert.Equal(0, clicks);
    }

    [AvaloniaTheory]
    [InlineData("item")]
    [InlineData("card")]
    public void Automation_Says_It_Is_Disabled_Under_A_Disabled_Parent(string kind)
    {
        var probe = Make(kind);
        using var scene = new Scene(new StackPanel { IsEnabled = false, Children = { probe.Control } });

        Assert.False(ControlAutomationPeer.CreatePeerForElement(probe.Control).IsEnabled());
    }

    // ── S5, S6: automation ───────────────────────────────────────────────────────────────────

    private static string? NameOf(Control control) => ControlAutomationPeer.CreatePeerForElement(control).GetName();

    [AvaloniaFact]
    public void S5_Words_In_Inlines_Count()
    {
        var text = new TextBlock();
        text.Inlines!.Add(new Avalonia.Controls.Documents.Run("Hello inline"));
        var item = new AvonicItem { Button = true, Content = text };
        using var scene = new Scene(item);

        Assert.Equal("Hello inline", NameOf(item));
    }

    [AvaloniaFact]
    public void S5_A_Nested_Buttons_Text_Counts_In_The_Case_It_Is_Drawn() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true, Content = new TextBlock { Text = "Text" }, EndContent = new AvonicButton { Content = "Inner btn" } };
        using var scene = new Scene(item);

        Assert.Equal("Text INNER BTN", NameOf(item));
    });

    [AvaloniaFact]
    public void S5_Hidden_Text_Does_Not_Count()
    {
        var item = new AvonicItem
        {
            Button = true,
            Content = new StackPanel { Children = { new TextBlock { Text = "Seen" }, new TextBlock { Text = "Hidden", IsVisible = false } } },
            EndContent = new Border { IsVisible = false, Child = new TextBlock { Text = "Invisible" } },
        };
        using var scene = new Scene(item);

        Assert.Equal("Seen", NameOf(item));
    }

    [AvaloniaFact]
    public void S5_Each_Slot_Counts_In_Order()
    {
        var item = new AvonicItem { Button = true, StartContent = "One", Content = "Two", EndContent = "Three" };
        using var scene = new Scene(item);

        Assert.Equal("One Two Three", NameOf(item));
    }

    [AvaloniaFact]
    public void S5_A_Name_Beats_Them_All()
    {
        var item = new AvonicItem { Button = true, Content = "Text" };
        AutomationProperties.SetName(item, "Aria name");
        using var scene = new Scene(item);

        Assert.Equal("Aria name", NameOf(item));
    }

    [AvaloniaFact]
    public void S5_A_Button_Reads_Inlines_And_Leaves_Out_What_Is_Hidden()
    {
        var text = new TextBlock();
        text.Inlines!.Add(new Avalonia.Controls.Documents.Run("In a run"));
        var panel = new StackPanel { Children = { text, new TextBlock { Text = "Hidden", IsVisible = false } } };
        var button = new AvonicButton { Content = panel };
        using var scene = new Scene(button);

        Assert.Equal("In a run", NameOf(button));
    }

    [AvaloniaFact]
    public void S5_A_Plain_Item_Has_No_Name_Of_Its_Own()
    {
        var item = new AvonicItem { Content = "Text" };
        using var scene = new Scene(item);

        Assert.True(string.IsNullOrEmpty(NameOf(item)));
    }

    [AvaloniaFact]
    public void S6_An_Item_In_A_List_Is_A_List_Item_Without_A_Theme()
    {
        var item = new AvonicItem { Content = "x" };
        var list = new AvonicList { Items = { item } };
        using var scene = new Scene(list);

        Assert.Equal(AutomationControlType.ListItem, ControlAutomationPeer.CreatePeerForElement(item).GetAutomationControlType());
    }

    [AvaloniaFact]
    public void S6_A_Button_Item_In_A_List_Stays_A_Button()
    {
        var item = new AvonicItem { Button = true, Content = "x" };
        var list = new AvonicList { Items = { item } };
        using var scene = new Scene(list);

        Assert.Equal(AutomationControlType.Button, ControlAutomationPeer.CreatePeerForElement(item).GetAutomationControlType());
    }

    [AvaloniaFact]
    public void S6_An_Item_Outside_A_List_Is_A_Group()
    {
        var item = new AvonicItem { Content = "x" };
        using var scene = new Scene(item);

        Assert.Equal(AutomationControlType.Group, ControlAutomationPeer.CreatePeerForElement(item).GetAutomationControlType());
    }

    // ── The ripple ───────────────────────────────────────────────────────────────────────────

    private static AvonicRippleEffect Ripple(Control host) =>
        host.GetVisualDescendants().OfType<AvonicRippleEffect>().First(ripple => ripple.Name == "PART_Ripple");

    [AvaloniaFact]
    public void A_Button_Item_With_A_Plain_Card_In_It_Keeps_Its_Own_Ripple()
    {
        using var clock = new TestClock();
        var card = new AvonicCard { Content = "x", Height = 20 };
        var item = new AvonicItem { Button = true, Content = card, Width = 200, Height = 50, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = new Scene(item);

        scene.Window.MouseDown(new Point(100, 25), MouseButton.Left, RawInputModifiers.None);
        clock.At(100);

        Assert.Single(Ripple(item).Ripples);
        Assert.Empty(Ripple(card).Ripples);
        scene.Window.MouseUp(new Point(100, 25), MouseButton.Left, RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void A_Button_In_A_Button_Item_Is_Drawn_Over_The_Items_Ripple()
    {
        var button = new AvonicButton { Content = "x" };
        var item = new AvonicItem { Button = true, EndContent = button };
        using var scene = new Scene(item);

        var panel = Ripple(item).GetVisualParent()!;
        var order = panel.GetVisualChildren().ToList();
        Assert.True(order.IndexOf(Ripple(item)) < order.IndexOf(order.First(child => child is FlexRow)));
    }

    // ── S10 ──────────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void S10_An_Items_Background_Reaches_Under_Its_Line() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = "x", Lines = ItemLines.Full, Height = 60 };
        using var scene = new Scene(new StackPanel { Background = Brushes.Lime, Children = { item } }, 390, 200);
        var native = item.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_Native");
        native.Transitions = null;
        native.Background = Brushes.Red;
        native.BorderThickness = new Thickness(0, 0, 0, 10);
        native.BorderBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        Settle();

        var pixel = Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, 200, 55);

        // Measured: rgb(127, 0, 128).
        Assert.InRange(pixel.R, 120, 135);
        Assert.Equal(0, pixel.G, 5d);
        Assert.InRange(pixel.B, 120, 135);
    });
}
