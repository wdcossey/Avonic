using Avalonia;
using Avalonia.Automation;
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
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-card</c> itself. The numbers are from real Ionic in Chromium, at 390 px wide. See
/// docs/components/ion-card/PLAN.md, whose behaviour numbers these are named after. The other four controls are in
/// <see cref="AvonicCardFamilyTests"/>. Tests with <c>InstalledTheme</c> check the look. The others run on the stub.
/// </summary>
public class AvonicCardTests
{
    private static readonly Point Centre = new(195, 40);

    private static AvonicCard ButtonCard() => new()
    {
        Button = true,
        Height = 80,
        VerticalAlignment = VerticalAlignment.Top,
        Content = "x",
    };

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static AvonicRippleEffect Ripple(AvonicCard card) =>
        card.GetVisualDescendants().OfType<AvonicRippleEffect>().First(ripple => ripple.TemplatedParent == card);
    private static Border Native(AvonicCard card) => Part<Border>(card, "PART_Native");

    private static bool Has(Control control, string pseudoClass) => control.Classes.Contains(pseudoClass);

    private static Rect Box(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void Tap(Scene scene, Point point, MouseButton button = MouseButton.Left)
    {
        scene.Window.MouseDown(point, button, RawInputModifiers.None);
        scene.Window.MouseUp(point, button, RawInputModifiers.None);
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

    private static Color Pixel(Scene scene, int x, int y)
    {
        Dispatcher.UIThread.RunJobs();
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, x, y);
    }

    private static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static readonly Color Text450 = Color.Parse("#737373");

    // ── Behaviours 1, 2: what Button turns on ────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Button_Is_Activatable_With_A_Ripple()
    {
        var card = ButtonCard();
        using var scene = new Scene(card);

        Assert.True(Activatable.GetIsEnabled(card));
        Assert.True(Ripple(card).IsVisible);

        using var clock = new TestClock();
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        clock.At(100);
        Assert.True(Has(card, Activatable.ActivatedPseudoClass));
        Assert.Single(Ripple(card).Ripples);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void B1_Button_Shows_A_Hand_Cursor() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicCard { Button = true };
        var plain = new AvonicCard();
        using var scene = new Scene(new StackPanel { Children = { button, plain } });

        Assert.Equal(new Cursor(StandardCursorType.Hand).ToString(), button.Cursor?.ToString());
        Assert.Null(plain.Cursor);
    });

    [AvaloniaFact]
    public void B2_Plain_Card_Is_Not_Activatable()
    {
        var card = new AvonicCard { Content = "x", Height = 80 };
        using var scene = new Scene(card);

        Assert.False(Activatable.GetIsEnabled(card));
        Assert.False(Ripple(card).IsVisible);
        Assert.False(card.Focusable);
    }

    [AvaloniaFact]
    public void B2_Turning_Button_Off_Removes_All_Of_It()
    {
        var card = ButtonCard();
        using var scene = new Scene(card);
        Assert.True(card.Focusable);

        card.Button = false;

        Assert.False(Activatable.GetIsEnabled(card));
        Assert.False(card.Focusable);
        Assert.False(Ripple(card).IsVisible);
    }

    [AvaloniaFact]
    public void B2_An_Apps_Own_Focusable_Survives_Button_Going_On_And_Off()
    {
        var card = new AvonicCard { Focusable = false };
        using var scene = new Scene(card);

        card.Button = true;
        Assert.False(card.Focusable);

        card.Button = false;
        Assert.False(card.Focusable);
    }

    // An app's own style for it wins, whenever button turns on after it. A value given at style priority when it turns on would
    // come after the app's style, and win.
    [AvaloniaFact]
    public void B2_An_Apps_Style_Survives_Button_Turning_On_After_It()
    {
        var card = new AvonicCard();
        using var scene = Scene.Styled(card, Scene.AppStyle<AvonicCard>(InputElement.FocusableProperty, false));

        card.Button = true;

        Assert.False(card.Focusable);
    }

    // ── Behaviours 3, 4: Click ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Tap_On_Button_Card_Raises_Click()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre);

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B3_Secondary_Button_Does_Not_Raise_Click()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre, MouseButton.Right);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B3_Tap_On_Plain_Card_Does_Not_Raise_Click()
    {
        var clicks = 0;
        var card = new AvonicCard { Content = "x", Height = 80 };
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre);
        Tap(scene, Centre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B3_Tap_On_Disabled_Card_Does_Not_Raise_Click()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.IsEnabled = false;
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B3_Disabled_During_A_Press_Does_Not_Raise_Click()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        card.IsEnabled = false;
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, clicks);

        // And on a second quick press, whose click comes on release too.
        card.IsEnabled = true;
        Tap(scene, Centre);
        Assert.Equal(1, clicks);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        card.IsEnabled = false;
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B3_Click_Bubbles_From_The_Card()
    {
        var bubbled = 0;
        var card = ButtonCard();
        var parent = new StackPanel { Children = { card } };
        parent.AddHandler(AvonicCard.ClickEvent, (_, e) =>
        {
            bubbled++;
            Assert.Same(card, e.Source);
        });
        using var scene = new Scene(parent);

        Tap(scene, Box(card, scene.Window).Center);

        Assert.Equal(1, bubbled);
    }

    [AvaloniaFact]
    public void B3_Three_Quick_Taps_Raise_Click_Three_Times()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre);
        Tap(scene, Centre);
        Tap(scene, Centre);

        Assert.Equal(3, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Second_Quick_Press_Clicks_On_Release_Not_On_Press()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Second_Quick_Press_On_A_Plain_Card_Does_Not_Click()
    {
        var clicks = 0;
        var card = new AvonicCard { Content = "x", Height = 80, VerticalAlignment = VerticalAlignment.Top };
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre);
        Tap(scene, Centre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Second_Quick_Press_Released_Off_The_Card_Does_Not_Click()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseMove(new Point(100, 400), RawInputModifiers.None);
        scene.Window.MouseUp(new Point(100, 400), MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Second_Press_Taken_By_A_Gesture_Does_Not_Click_Later()
    {
        var clicks = 0;
        IPointer? pointer = null;
        var card = ButtonCard();
        var other = new Border { Height = 48, Background = Brushes.Gray };
        card.Click += (_, _) => clicks++;
        card.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel);
        using var scene = new Scene(new StackPanel { Children = { card, other } });

        Tap(scene, Centre);
        Assert.Equal(1, clicks);

        // As a scroll does: the pointer goes elsewhere, so the release never reaches the card.
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        pointer!.Capture(other);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        // Elsewhere on the card, so this is a new first tap, which clicks once.
        Tap(scene, new Point(60, 40));
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Second_Quick_Press_That_Slides_Still_Clicks_With_A_Mouse()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);
        var slid = new Point(Centre.X + 60, Centre.Y + 10);

        Tap(scene, Centre);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseMove(slid, RawInputModifiers.None);
        scene.Window.MouseUp(slid, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        // Measured: a mouse has no movement limit. A finger does, in NativeButtonTests.
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Second_Quick_Press_That_Barely_Moves_Clicks()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);
        var nudged = new Point(Centre.X + 1, Centre.Y);

        Tap(scene, Centre);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseMove(nudged, RawInputModifiers.None);
        scene.Window.MouseUp(nudged, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Press_After_One_Taken_By_A_Gesture_Clicks_Once()
    {
        var clicks = 0;
        IPointer? pointer = null;
        var card = ButtonCard();
        var other = new Border { Height = 48, Background = Brushes.Gray };
        card.Click += (_, _) => clicks++;
        card.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, RoutingStrategies.Tunnel);
        using var scene = new Scene(new StackPanel { Children = { card, other } });

        Tap(scene, Centre);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        pointer!.Capture(other);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        // In the same place, so the unfinished second press mustn't count as well.
        Tap(scene, Centre);
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B3_Another_Pointer_Releasing_Does_Not_Complete_A_Second_Press()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        card.RaiseEvent(new PointerReleasedEventArgs(
            card, new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true), scene.Window, Centre, 0,
            new PointerPointProperties(), KeyModifiers.None, MouseButton.Left));
        Assert.Equal(1, clicks);

        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Second_Finger_Ends_A_Second_Press_Measured()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Centre);
        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        // A second finger, somewhere else, which isn't on any of this press's events. Measured on ion-button.
        card.RaiseEvent(new PointerPressedEventArgs(
            card, new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, false), scene.Window, new Point(60, 20), 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None)
        {
            RoutedEvent = InputElement.PointerPressedEvent,
            Route = RoutingStrategies.Tunnel,
        });

        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B4_Tap_On_A_Child_Also_Raises_Click()
    {
        var cardClicks = 0;
        var childClicks = 0;
        var child = new Button
        {
            Width = 60,
            Height = 30,
            Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Gray }),
        };
        child.Click += (_, _) => childClicks++;
        var card = ButtonCard();
        card.Content = child;
        card.Click += (_, _) => cardClicks++;
        using var scene = new Scene(card);

        Tap(scene, Box(child, scene.Window).Center);

        Assert.Equal(1, childClicks);
        Assert.Equal(1, cardClicks);
    }

    [AvaloniaFact]
    public void B4_A_Child_That_Handles_Tapped_Does_Not_Stop_Click()
    {
        var clicks = 0;
        var child = new Border { Width = 60, Height = 30, Background = Brushes.Gray };
        child.Tapped += (_, e) => e.Handled = true;
        var card = ButtonCard();
        card.Content = child;
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, Box(child, scene.Window).Center);

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B4_A_Child_That_Takes_The_Press_Stops_Click_Every_Time()
    {
        var clicks = 0;
        var child = new Border { Width = 60, Height = 30, Background = Brushes.Gray };
        child.PointerPressed += (_, e) => e.Handled = true;
        var card = ButtonCard();
        card.Content = child;
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        var centre = Box(child, scene.Window).Center;
        Tap(scene, centre);
        Tap(scene, centre);

        Assert.Equal(0, clicks);
    }

    // Measured outer, inner: a tap on the inner card is one click, which reaches both.
    [AvaloniaFact]
    public void B4_A_Tap_On_A_Nested_Button_Card_Reaches_Each_Card_Once()
    {
        var inner = new AvonicCard { Button = true, Height = 40, Content = "inner" };
        var outer = ButtonCard();
        outer.Content = inner;
        var innerClicks = 0;
        var outerClicks = 0;
        var outerSources = new List<object?>();
        inner.Click += (_, _) => innerClicks++;
        outer.Click += (_, e) =>
        {
            outerClicks++;
            outerSources.Add(e.Source);
        };
        using var scene = new Scene(outer);

        var centre = Box(inner, scene.Window).Center;
        Tap(scene, centre);
        Assert.Equal(1, innerClicks);
        Assert.Equal(1, outerClicks);

        // The second quick press goes the other way.
        Tap(scene, centre);
        Assert.Equal(2, innerClicks);
        Assert.Equal(2, outerClicks);
        Assert.All(outerSources, source => Assert.Same(inner, source));
    }

    [AvaloniaFact]
    public void B4_A_Tap_On_A_Nested_Plain_Card_Clicks_The_Outer_One()
    {
        var inner = new AvonicCard { Height = 40, Content = "inner" };
        var outer = ButtonCard();
        outer.Content = inner;
        var clicks = 0;
        outer.Click += (_, _) => clicks++;
        using var scene = new Scene(outer);

        Tap(scene, Box(inner, scene.Window).Center);

        Assert.Equal(1, clicks);
    }

    // ── Behaviour 5: keys ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Enter_Activates_On_Press()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);
        card.Focus();

        PressKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, clicks);

        ReleaseKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B5_Space_Activates_On_Release()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);
        card.Focus();

        PressKey(scene, PhysicalKey.Space);
        Assert.Equal(0, clicks);

        ReleaseKey(scene, PhysicalKey.Space);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B5_Space_Released_After_Focus_Moved_Away_Does_Not_Activate()
    {
        var clicks = 0;
        var card = ButtonCard();
        var other = new Button { Content = "y" };
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(new StackPanel { Children = { card, other } });
        card.Focus();

        PressKey(scene, PhysicalKey.Space);
        other.Focus();
        card.Focus();
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B5_Turning_Button_Off_And_On_During_A_Space_Press_Does_Not_Click()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);
        card.Focus();

        PressKey(scene, PhysicalKey.Space);
        card.Button = false;
        card.Button = true;
        card.Focus();
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B5_Keys_Do_Nothing_On_A_Plain_Card()
    {
        var clicks = 0;
        var card = new AvonicCard { Content = "x", Focusable = true };
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);
        card.Focus();

        PressKey(scene, PhysicalKey.Enter);
        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B5_Keys_On_A_Plain_Card_Go_On_To_The_Parent()
    {
        var keys = 0;
        var card = new AvonicCard { Content = "x", Focusable = true };
        var parent = new Border { Child = card };
        parent.KeyDown += (_, _) => keys++;
        using var scene = new Scene(parent);
        card.Focus();

        PressKey(scene, PhysicalKey.Enter);
        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(2, keys);
    }

    [AvaloniaFact]
    public void B5_Keys_On_A_Nested_Control_Are_Its_Own()
    {
        var clicks = 0;
        var child = new Border { Width = 40, Height = 20, Focusable = true };
        var card = ButtonCard();
        card.Content = child;
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);
        child.Focus();

        PressKey(scene, PhysicalKey.Enter);
        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B5_A_Key_An_App_Handled_First_Does_Nothing()
    {
        var clicks = 0;
        var card = ButtonCard();
        card.Click += (_, _) => clicks++;
        card.AddHandler(InputElement.KeyDownEvent, (_, e) => e.Handled = true, RoutingStrategies.Tunnel);
        using var scene = new Scene(card);
        card.Focus();

        PressKey(scene, PhysicalKey.Enter);

        Assert.Equal(0, clicks);
    }

    // ── Behaviour 6: tab stops ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Tab_Stops_On_Button_Cards_Only()
    {
        var plain = new AvonicCard { Content = "plain" };
        var button = new AvonicCard { Button = true, Content = "button" };
        var disabled = new AvonicCard { Button = true, IsEnabled = false, Content = "disabled" };
        var last = new AvonicCard { Button = true, Content = "last" };
        using var scene = new Scene(new StackPanel { Children = { plain, button, disabled, last } });

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(button.IsFocused);

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(last.IsFocused);
    }

    // ── Behaviour 7: hover changes nothing, and keyboard focus shows, unlike Ionic ───────────

    private static void AssertBlend(Color under, Color over, double opacity, Color actual)
    {
        Assert.InRange(actual.R, under.R + (over.R - under.R) * opacity - 2, under.R + (over.R - under.R) * opacity + 2);
        Assert.InRange(actual.G, under.G + (over.G - under.G) * opacity - 2, under.G + (over.G - under.G) * opacity + 2);
        Assert.InRange(actual.B, under.B + (over.B - under.B) * opacity - 2, under.B + (over.B - under.B) * opacity + 2);
    }

    [AvaloniaFact]
    public void B7_Hover_Changes_Nothing() => InstalledTheme.Run(_ =>
    {
        var card = ButtonCard();
        using var scene = new Scene(card);
        scene.Window.Background = Brushes.White;
        var before = Pixel(scene, 195, 40);

        scene.Window.MouseMove(Centre, RawInputModifiers.None);

        Assert.Equal(before, Pixel(scene, 195, 40));
        Assert.Equal(Text450, Solid(card.Foreground));
    });

    [AvaloniaFact]
    public void B7_Keyboard_Focus_Shows_And_Goes() => InstalledTheme.Run(_ =>
    {
        var card = ButtonCard();
        var other = new Button();
        using var scene = new Scene(new StackPanel { Children = { card, other } });
        scene.Window.Background = Brushes.White;
        var before = Pixel(scene, 195, 40);

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(card.IsFocused);
        AssertBlend(before, Text450, 0.12, Pixel(scene, 195, 40));

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(other.IsFocused);
        Assert.Equal(before, Pixel(scene, 195, 40));
    });

    [AvaloniaFact]
    public void B7_Focus_Shows_In_The_Cards_Own_Colour() => InstalledTheme.Run(_ =>
    {
        var card = ButtonCard();
        card.Color = "danger";
        using var scene = new Scene(card);
        var before = Pixel(scene, 195, 40);

        PressKey(scene, PhysicalKey.Tab);

        Assert.Equal(Colors.White, Solid(card.Foreground));
        AssertBlend(before, Colors.White, 0.12, Pixel(scene, 195, 40));
    });

    [AvaloniaFact]
    public void B7_Focus_Shows_Over_The_Content() => InstalledTheme.Run(_ =>
    {
        var card = ButtonCard();
        card.Content = new Border { Background = Brushes.Red };
        using var scene = new Scene(card);
        Assert.Equal(Colors.Red, Pixel(scene, 195, 40));

        PressKey(scene, PhysicalKey.Tab);

        AssertBlend(Colors.Red, Text450, 0.12, Pixel(scene, 195, 40));
    });

    [AvaloniaFact]
    public void B7_The_Focus_Overlay_Lets_A_Press_Through_To_The_Content() => InstalledTheme.Run(_ =>
    {
        var child = new Border { Background = Brushes.Red };
        var card = ButtonCard();
        card.Content = child;
        using var scene = new Scene(card);
        object? source = null;
        card.AddHandler(InputElement.PointerPressedEvent, (_, e) => source = e.Source, handledEventsToo: true);

        // With the overlay showing, and rendered, as hit tests read the last frame.
        PressKey(scene, PhysicalKey.Tab);
        Pixel(scene, 195, 40);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);

        Assert.Same(child, source);
    });

    [AvaloniaFact]
    public void B7_Pointer_Focus_Does_Not_Show() => InstalledTheme.Run(_ =>
    {
        var card = ButtonCard();
        using var scene = new Scene(card);
        scene.Window.Background = Brushes.White;
        var before = Pixel(scene, 195, 40);

        card.Focus(NavigationMethod.Pointer);

        Assert.True(card.IsFocused);
        Assert.Equal(before, Pixel(scene, 195, 40));
    });

    [AvaloniaFact]
    public void B7_A_Press_On_The_Card_Clears_It() => InstalledTheme.Run(_ =>
    {
        var card = ButtonCard();
        using var scene = new Scene(card);
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(Part<Border>(card, "PART_Focus").IsVisible);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);

        Assert.True(card.IsFocused);
        Assert.False(Part<Border>(card, "PART_Focus").IsVisible);
    });

    [AvaloniaFact]
    public void B7_A_Press_Elsewhere_Clears_It() => InstalledTheme.Run(_ =>
    {
        var card = ButtonCard();
        var below = new Border { Height = 100, Background = Brushes.Transparent };
        using var scene = new Scene(new StackPanel { Children = { card, below } });
        scene.Window.Background = Brushes.White;
        var before = Pixel(scene, 195, 40);
        PressKey(scene, PhysicalKey.Tab);
        Assert.NotEqual(before, Pixel(scene, 195, 40));

        scene.Window.MouseDown(new Point(195, 150), MouseButton.Left, RawInputModifiers.None);
        scene.Window.MouseUp(new Point(195, 150), MouseButton.Left, RawInputModifiers.None);

        Assert.Equal(before, Pixel(scene, 195, 40));
    });

    // FOCUS_KEYS, as an item's: a letter ends keyboard mode, and Shift doesn't.
    [AvaloniaFact]
    public void B7_Only_A_Key_Outside_The_Focus_Keys_Clears_It() => InstalledTheme.Run(_ =>
    {
        var card = ButtonCard();
        using var scene = new Scene(card);
        scene.Window.Background = Brushes.White;
        var before = Pixel(scene, 195, 40);
        PressKey(scene, PhysicalKey.Tab);

        PressKey(scene, PhysicalKey.ShiftLeft);
        ReleaseKey(scene, PhysicalKey.ShiftLeft);
        Assert.NotEqual(before, Pixel(scene, 195, 40));

        PressKey(scene, PhysicalKey.A);
        ReleaseKey(scene, PhysicalKey.A);
        Assert.Equal(before, Pixel(scene, 195, 40));
    });

    [AvaloniaFact]
    public void B7_Button_Going_Off_Clears_It() => InstalledTheme.Run(_ =>
    {
        var card = ButtonCard();
        using var scene = new Scene(card);
        PressKey(scene, PhysicalKey.Tab);
        Assert.True(Part<Border>(card, "PART_Focus").IsVisible);

        card.Button = false;

        Assert.False(Part<Border>(card, "PART_Focus").IsVisible);
    });

    [AvaloniaFact]
    public void B7_Focus_On_A_Child_Does_Not_Show_On_The_Card() => InstalledTheme.Run(_ =>
    {
        var child = new Border { Width = 10, Height = 10, Focusable = true };
        var card = ButtonCard();
        card.Content = child;
        using var scene = new Scene(card);
        scene.Window.Background = Brushes.White;
        var before = Pixel(scene, 195, 40);

        child.Focus(NavigationMethod.Tab);

        Assert.True(child.IsFocused);
        Assert.Equal(before, Pixel(scene, 195, 40));
    });

    [AvaloniaFact]
    public void B7_Nested_Button_Cards_Do_Not_Stack_Their_Overlays() => InstalledTheme.Run(_ =>
    {
        var inner = new AvonicCard { Button = true };
        var outer = ButtonCard();
        outer.Content = inner;
        using var scene = new Scene(outer);
        scene.Window.Background = Brushes.White;
        var before = Pixel(scene, 195, 40);

        inner.Focus(NavigationMethod.Tab);

        Assert.True(inner.IsFocused);
        AssertBlend(before, Text450, 0.12, Pixel(scene, 195, 40));
    });

    [AvaloniaFact]
    public void B7_A_Plain_Card_Made_Focusable_Does_Not_Show_Focus() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 80, VerticalAlignment = VerticalAlignment.Top, Focusable = true };
        using var scene = new Scene(card);
        scene.Window.Background = Brushes.White;
        var before = Pixel(scene, 195, 40);

        PressKey(scene, PhysicalKey.Tab);

        Assert.True(card.IsFocused);
        Assert.Equal(before, Pixel(scene, 195, 40));
    });

    // Ionic's card isn't ion-focusable, so an item around one doesn't react. Here it does, at any depth, because an item
    // treats any FocusVisible descendant as ion-focusable. A button card in an item is rare, so this is recorded, not fixed.
    [AvaloniaFact]
    public void B7_Known_Gap_An_Item_Around_A_Button_Card_Shows_Focus_Too() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Button = true };
        var item = new AvonicItem { Content = card };
        using var scene = new Scene(item);

        PressKey(scene, PhysicalKey.Tab);

        Assert.True(card.IsFocused);
        Assert.True(Has(card, FocusVisible.FocusedPseudoClass));
        Assert.True(Has(item, FocusVisible.FocusedPseudoClass));
        Assert.False(item.Focusable);
    });

    // outline: none. The adorner comes from the app's base theme, which TestApp doesn't load, so this adds it
    // underneath, in the same order as the sample. FocusAdorner reads null whether or not it's set, because an
    // unset one falls back to the base theme's default, so only the layer shows what's really drawn.
    [AvaloniaFact]
    public void B7_There_Is_No_Focus_Adorner_Over_A_Base_Theme()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var card = new AvonicCard { Button = true };
                var other = new Button();
                using var scene = new Scene(new StackPanel { Children = { card, other } });

                var layer = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(card)!;

                PressKey(scene, PhysicalKey.Tab);
                Assert.True(card.IsFocused);
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

    // ── Behaviour 8: the ripple ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_The_Ripple_Fills_The_Card_And_Uses_The_Current_Colour() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicCard { Button = true, Height = 90 };
        var coloured = new AvonicCard { Button = true, Height = 90, Color = "danger" };
        var token = new AvonicCard { Button = true, Height = 90 };
        token.Resources["Avonic.Card.Color"] = Brushes.Lime;
        using var scene = new Scene(new StackPanel { Children = { plain, coloured, token } });

        var box = Box(Ripple(plain), scene.Window);
        Assert.Equal(new Rect(10, 10, 370, 90), box);

        Assert.Equal(Text450, Solid(Ripple(plain).Foreground));
        Assert.Equal(Colors.White, Solid(Ripple(coloured).Foreground));
        Assert.Equal(Colors.Lime, Solid(Ripple(token).Foreground));
    });

    // getRippleEffect looks in the host's shadow root first, and a plain child's ripple is hidden.
    [AvaloniaFact]
    public void B8_A_Plain_Control_Inside_Does_Not_Take_The_Ripple()
    {
        using var clock = new TestClock();
        var plainCard = new AvonicCard { Height = 30 };
        var plainItem = new AvonicItem { Content = "x" };
        var card = ButtonCard();
        card.Content = new StackPanel { Children = { plainCard, plainItem } };
        using var scene = new Scene(card);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        clock.At(100);

        Assert.Single(Ripple(card).Ripples);
        Assert.Empty(Ripple(plainCard).Ripples);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void B8_A_Plain_Control_Inside_Does_Not_Take_The_Ripple_In_The_Theme() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var plainCard = new AvonicCard { Height = 30 };
        var card = ButtonCard();
        card.Content = plainCard;
        using var scene = new Scene(card);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        clock.At(100);

        Assert.Single(Ripple(card).Ripples);
        Assert.Empty(Ripple(plainCard).Ripples);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
    });

    [AvaloniaFact]
    public void B8_The_Ripple_Draws_Over_The_Content() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var card = new AvonicCard
        {
            Button = true,
            Height = 90,
            VerticalAlignment = VerticalAlignment.Top,
            Content = new Border { Background = Brushes.Red },
        };
        using var scene = new Scene(card);
        Assert.Equal(Colors.Red, Pixel(scene, 195, 50));

        var centre = new Point(195, 55);
        scene.Window.MouseDown(centre, MouseButton.Left, RawInputModifiers.None);
        clock.At(320);
        Ripple(card).Tick();

        Assert.NotEqual(Colors.Red, Pixel(scene, 195, 50));
        scene.Window.MouseUp(centre, MouseButton.Left, RawInputModifiers.None);
    });

    [AvaloniaFact]
    public void B8_The_Ripple_Clips_To_The_Corners() => InstalledTheme.Run(_ =>
    {
        // A big radius, so the corner is clearly outside, and content that would otherwise fill it.
        var card = new AvonicCard
        {
            Button = true,
            Height = 90,
            VerticalAlignment = VerticalAlignment.Top,
            CornerRadius = new CornerRadius(20),
            Content = new Border { Background = Brushes.Red },
        };
        card.Resources["Avonic.Card.BoxShadow"] = default(BoxShadows);
        using var scene = new Scene(card);
        scene.Window.Background = Brushes.White;

        Assert.Equal(Colors.Red, Pixel(scene, 195, 50));
        Assert.Equal(Colors.White, Pixel(scene, 11, 11));
    });

    // ── Behaviour 9: the box ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_Margin_Is_10_And_The_Font_Is_The_Cards() => InstalledTheme.Run(theme =>
    {
        var text = new TextBlock { Text = "x" };
        var card = new AvonicCard { Content = text };
        using var scene = new Scene(card);

        Assert.Equal(new Thickness(10), card.Margin);
        Assert.Equal(new Rect(10, 10, 370, card.Bounds.Height), Box(card, scene.Window));
        Assert.Equal(14, text.FontSize);
        Assert.Equal(card.FontFamily, text.FontFamily);
        Assert.Equal(theme.Resources.TryGetResource("Avonic.FontFamily", null, out var family) ? family : null, card.FontFamily);
    });

    [AvaloniaFact]
    public void B9_The_Radius_Is_4_On_What_Is_Drawn_And_What_Clips() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90 };
        using var scene = new Scene(card);

        Assert.Equal(new CornerRadius(4), card.CornerRadius);
        Assert.Equal(new CornerRadius(4), Native(card).CornerRadius);
        Assert.Equal(new CornerRadius(4), Part<Border>(card, "PART_Host").CornerRadius);
    });

    [AvaloniaFact]
    public void B9_The_Shadow_Shows_Around_The_Card_But_Content_Is_Clipped() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard
        {
            Height = 90,
            VerticalAlignment = VerticalAlignment.Top,
            Content = new Border { Background = Brushes.Red, Margin = new Thickness(-50) },
        };
        using var scene = new Scene(card);
        scene.Window.Background = Brushes.White;

        // Measured in Chromium at scale 1: 170 straight below the card, and 247 straight above it. Avonic draws 164 and 247.
        var below = Pixel(scene, 195, 100);
        Assert.InRange(below.R, 155, 180);
        Assert.Equal(below.R, below.G);
        Assert.InRange(Pixel(scene, 195, 9).R, 240, 252);

        // The content overflows by 50, and stops at the card's edge.
        Assert.Equal(Colors.Red, Pixel(scene, 195, 50));
        Assert.NotEqual(Colors.Red, Pixel(scene, 195, 5));
        Assert.NotEqual(Colors.Red, Pixel(scene, 5, 50));
    });

    [AvaloniaFact]
    public void B9_The_Shadow_Token_Applies() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90, VerticalAlignment = VerticalAlignment.Top };
        card.Resources["Avonic.Card.BoxShadow"] = default(BoxShadows);
        using var scene = new Scene(card);
        scene.Window.Background = Brushes.White;

        Assert.Equal(Colors.White, Pixel(scene, 195, 100));
    });

    [AvaloniaFact]
    public void B9_Text_Wraps_And_Grows_The_Card() => InstalledTheme.Run(_ =>
    {
        var one = new AvonicCard { Content = "x" };
        var many = new AvonicCard { Content = new string('x', 10) + string.Concat(Enumerable.Repeat(" word", 100)) };
        using var scene = new Scene(new StackPanel { Children = { one, many } });

        Assert.True(many.Bounds.Height > one.Bounds.Height * 2);
    });

    [AvaloniaFact]
    public void B9_On_The_Stub_Text_Wraps_And_The_Card_Does_Not_Clip_Its_Own_Box()
    {
        var many = new AvonicCard { Content = string.Concat(Enumerable.Repeat(" word", 200)) };
        var one = new AvonicCard { Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { one, many } });

        Assert.True(many.Bounds.Height > one.Bounds.Height * 2);
        Assert.False(many.ClipToBounds);
        Assert.True(Native(many).ClipToBounds);
    }

    [AvaloniaFact]
    public void B9_The_Template_Follows_The_Cards_Own_Properties_On_The_Stub()
    {
        var card = new AvonicCard { Height = 90, Background = Brushes.Teal, CornerRadius = new CornerRadius(7) };
        using var scene = new Scene(card);

        AssertTemplateFollows(card);
    }

    [AvaloniaFact]
    public void B9_An_Apps_Own_Border_Shows() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard
        {
            Height = 90,
            VerticalAlignment = VerticalAlignment.Top,
            BorderBrush = Brushes.Lime,
            BorderThickness = new Thickness(3),
        };
        using var scene = new Scene(card);

        Assert.Equal(Colors.Lime, Pixel(scene, 195, 11));
    });

    [AvaloniaFact]
    public void B9_The_Template_Follows_The_Cards_Own_Properties_In_The_Theme() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90, Background = Brushes.Teal, CornerRadius = new CornerRadius(7) };
        using var scene = new Scene(card);

        AssertTemplateFollows(card);
    });

    private static void AssertTemplateFollows(AvonicCard card)
    {
        Assert.Equal(Colors.Teal, Solid(Native(card).Background));
        Assert.Equal(new CornerRadius(7), Native(card).CornerRadius);
        Assert.Equal(new CornerRadius(7), Part<Border>(card, "PART_Host").CornerRadius);

        card.BorderBrush = Brushes.Lime;
        card.BorderThickness = new Thickness(2);
        Assert.Equal(Colors.Lime, Solid(Native(card).BorderBrush));
        Assert.Equal(new Thickness(2), Native(card).BorderThickness);

        var presenter = Part<ContentPresenter>(card, "PART_ContentPresenter");
        card.Padding = new Thickness(3);
        card.HorizontalContentAlignment = HorizontalAlignment.Right;
        card.VerticalContentAlignment = VerticalAlignment.Bottom;
        Assert.Equal(new Thickness(3), presenter.Padding);
        Assert.Equal(HorizontalAlignment.Right, presenter.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Bottom, presenter.VerticalContentAlignment);
    }

    // ── Behaviours 10-12: colours ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_Defaults() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90 };
        using var scene = new Scene(card);

        Assert.Equal(Colors.White, Solid(Native(card).Background));
        Assert.Equal(Text450, Solid(card.Foreground));
    });

    [AvaloniaFact]
    public void B11_The_Cards_Own_Tokens_Apply() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90 };
        card.Resources["Avonic.Card.Background"] = Brushes.Red;
        card.Resources["Avonic.Card.Color"] = Brushes.Lime;
        card.Resources["Avonic.CardBackgroundBrush"] = Brushes.Blue;
        card.Resources["Avonic.CardColorBrush"] = Brushes.Blue;
        using var scene = new Scene(card);

        Assert.Equal(Colors.Red, Solid(Native(card).Background));
        Assert.Equal(Colors.Lime, Solid(card.Foreground));
    });

    [AvaloniaFact]
    public void B11_The_Global_Tokens_Apply() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90 };
        card.Resources["Avonic.CardBackgroundBrush"] = Brushes.Blue;
        card.Resources["Avonic.CardColorBrush"] = Brushes.Yellow;
        using var scene = new Scene(card);

        Assert.Equal(Colors.Blue, Solid(Native(card).Background));
        Assert.Equal(Colors.Yellow, Solid(card.Foreground));
    });

    [AvaloniaFact]
    public void B11_The_Text_Colour_Follows_The_Palette() => InstalledTheme.Run(_ =>
    {
        var app = Application.Current!;
        app.Resources["Avonic.TextColorStep450"] = Colors.Purple;
        try
        {
            var card = new AvonicCard { Height = 90 };
            using var scene = new Scene(card);

            Assert.Equal(Colors.Purple, Solid(card.Foreground));
        }
        finally
        {
            app.Resources.Remove("Avonic.TextColorStep450");
        }
    });

    [AvaloniaFact]
    public void B11_An_Apps_Own_Background_Wins() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90, Background = Brushes.Teal };
        using var scene = new Scene(card);

        Assert.Equal(Colors.Teal, Solid(Native(card).Background));
    });

    [AvaloniaFact]
    public void B12_Color_Paints_Base_And_Contrast() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90, Color = "danger" };
        card.Resources["Avonic.Card.Background"] = Brushes.Lime;
        card.Resources["Avonic.Card.Color"] = Brushes.Lime;
        using var scene = new Scene(card);

        Assert.Equal(Color.Parse("#C5000F"), Solid(Native(card).Background));
        Assert.Equal(Colors.White, Solid(card.Foreground));
    });

    [AvaloniaFact]
    public void B12_An_Unknown_Color_Is_Transparent_And_Inherits() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90, Color = "nonexistent" };
        var parent = new Border { Child = card };
        TextElement.SetForeground(parent, Brushes.Teal);
        using var scene = new Scene(parent);
        scene.Window.Background = Brushes.Orange;
        card.Resources["Avonic.Card.BoxShadow"] = default(BoxShadows);

        Assert.Null(Native(card).Background);
        Assert.Equal(Colors.Teal, Solid(card.Foreground));
        Assert.Equal(Colors.Orange, Pixel(scene, 195, 50));
    });

    [AvaloniaFact]
    public void B12_An_Unknown_Color_Still_Takes_A_Tap_On_Its_Empty_Part() => InstalledTheme.Run(_ =>
    {
        // Nothing in it is painted, so only the template's panel can take this.
        var clicks = 0;
        var card = new AvonicCard { Button = true, Color = "nonexistent", Height = 90, VerticalAlignment = VerticalAlignment.Top };
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        Tap(scene, new Point(195, 50));

        Assert.Equal(1, clicks);
    });

    [AvaloniaFact]
    public void B12_No_Color_Keeps_The_Defaults_In_A_Coloured_Ancestor() => InstalledTheme.Run(_ =>
    {
        var inner = new AvonicCard { Height = 90 };
        var outer = new AvonicCard { Color = "danger", Content = inner };
        using var scene = new Scene(outer);

        Assert.Equal(Colors.White, Solid(Native(inner).Background));
        Assert.Equal(Text450, Solid(inner.Foreground));
    });

    [AvaloniaFact]
    public void B12_Clearing_Color_Restores_The_Defaults() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Height = 90, Color = "danger" };
        using var scene = new Scene(card);

        card.Color = null;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Colors.White, Solid(Native(card).Background));
        Assert.Equal(Text450, Solid(card.Foreground));
    });

    // ── Behaviour 13: disabled ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B13_Disabled_Is_Faded_And_Takes_No_Input() => InstalledTheme.Run(_ =>
    {
        var card = new AvonicCard { Button = true, IsEnabled = false, Height = 90, VerticalAlignment = VerticalAlignment.Top };
        var plain = new AvonicCard { IsEnabled = false, Height = 90 };
        var enabled = new AvonicCard { Button = true, Height = 90 };
        using var scene = new Scene(new StackPanel { Children = { card, plain, enabled } });

        Assert.Equal(0.3, card.Opacity);
        Assert.Equal(0.3, plain.Opacity);
        Assert.Equal(1, enabled.Opacity);

        var presses = 0;
        card.PointerPressed += (_, _) => presses++;
        enabled.PointerPressed += (_, _) => presses += 10;
        Tap(scene, Box(card, scene.Window).Center);
        Assert.Equal(0, presses);
        Tap(scene, Box(enabled, scene.Window).Center);
        Assert.Equal(10, presses);
    });

    [AvaloniaFact]
    public void B13_A_Disabled_Card_Does_Not_Ripple()
    {
        using var clock = new TestClock();
        var card = ButtonCard();
        card.IsEnabled = false;
        using var scene = new Scene(card);

        scene.Window.MouseDown(Centre, MouseButton.Left, RawInputModifiers.None);
        clock.At(100);

        Assert.False(Has(card, Activatable.ActivatedPseudoClass));
        Assert.Empty(Ripple(card).Ripples);
        scene.Window.MouseUp(Centre, MouseButton.Left, RawInputModifiers.None);
    }

    // ── Behaviour 14: automation ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B14_Button_Card_Is_An_Invokable_Button()
    {
        var clicks = 0;
        var card = new AvonicCard { Button = true, Content = "Open" };
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        var peer = ControlAutomationPeer.CreatePeerForElement(card);

        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("Open", peer.GetName());
        Assert.True(peer.IsEnabled());
        Assert.True(peer.IsContentElement());
        Assert.True(peer.IsControlElement());
        Assert.NotNull(peer.GetProvider<IInvokeProvider>());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B14_Plain_Card_Is_A_Group_That_Cannot_Be_Invoked()
    {
        var clicks = 0;
        var card = new AvonicCard { Content = "Text" };
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        var peer = ControlAutomationPeer.CreatePeerForElement(card);

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Null(peer.GetProvider<IInvokeProvider>());
        Assert.True(string.IsNullOrEmpty(peer.GetName()));
        Assert.False(peer.IsContentElement());
        Assert.IsAssignableFrom<IInvokeProvider>(peer).Invoke();
        Assert.Equal(0, clicks);

        card.Button = true;
        Assert.NotNull(peer.GetProvider<IInvokeProvider>());
    }

    [AvaloniaFact]
    public void B14_Disabled_Reports_It_And_Invoking_Does_Nothing()
    {
        var clicks = 0;
        var card = new AvonicCard { Button = true, IsEnabled = false };
        card.Click += (_, _) => clicks++;
        using var scene = new Scene(card);

        var peer = ControlAutomationPeer.CreatePeerForElement(card);

        Assert.False(peer.IsEnabled());
        Assert.IsAssignableFrom<IInvokeProvider>(peer).Invoke();
        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B14_The_Name_Comes_From_All_The_Text_Inside()
    {
        var card = new AvonicCard
        {
            Button = true,
            Content = new StackPanel
            {
                Children =
                {
                    new AvonicCardHeader { Content = new AvonicCardTitle { Content = "Title" } },
                    new AvonicCardContent { Content = "Body" },
                },
            },
        };
        using var scene = new Scene(card);

        Assert.Equal("Title Body", ControlAutomationPeer.CreatePeerForElement(card).GetName());
    }

    [AvaloniaFact]
    public void B14_The_Name_Leaves_Out_Hidden_Text_And_Reads_Inlines()
    {
        var inlines = new TextBlock();
        inlines.Inlines!.Add(new Run("Runs"));
        var card = new AvonicCard
        {
            Button = true,
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Visible" },
                    new TextBlock { Text = "Hidden", IsVisible = false },
                    new Border { IsVisible = false, Child = new TextBlock { Text = "Hidden inside" } },
                    inlines,
                },
            },
        };
        using var scene = new Scene(card);

        Assert.Equal("Visible Runs", ControlAutomationPeer.CreatePeerForElement(card).GetName());
    }

    [AvaloniaFact]
    public void B14_An_Explicit_Name_Wins()
    {
        var card = new AvonicCard { Button = true, Content = "Has a label" };
        AutomationProperties.SetName(card, "Named");
        using var scene = new Scene(card);

        Assert.Equal("Named", ControlAutomationPeer.CreatePeerForElement(card).GetName());
    }
}
