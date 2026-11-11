using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-item in Chromium, at 390 px wide. See docs/components/ion-item/PLAN.md.
/// Tests with <c>InstalledTheme</c> check the look. The others run on the functional stub.
/// </summary>
public class AvonicItemTests
{
    private static readonly Point RowCentre = new(200, 24);

    /// <summary>A control the item knows about, standing in for the form controls that come later.</summary>
    private sealed class Stub(ItemControlType type) : Border, IItemControl
    {
        public int ItemTaps { get; private set; }

        public ItemControlType ItemControlType => type;

        public void OnItemTap() => ItemTaps++;
    }

    private sealed class OtherStyler : Border;

    /// <summary>A real Button that can be hit. The stub theme has no template for one.</summary>
    private static Button HittableButton() => new()
    {
        Width = 60,
        Height = 30,
        Template = new Avalonia.Controls.Templates.FuncControlTemplate<Button>((_, _) => new Border { Background = Brushes.Gray }),
    };

    private static Stub StubControl(ItemControlType type) =>
        new(type) { Width = 40, Height = 20, Background = Brushes.Gray };

    private static T Part<T>(Control host, string name) where T : Control =>
        host.GetVisualDescendants().OfType<T>().First(control => control.Name == name);

    private static Border Native(AvonicItem item) => Part<Border>(item, "PART_Native");
    private static Border Inner(AvonicItem item) => Part<Border>(item, "PART_Inner");
    private static Border Overlay(AvonicItem item) => Part<Border>(item, "PART_Overlay");
    private static ContentPresenter Container(AvonicItem item) => Part<ContentPresenter>(item, "PART_Container");
    private static AvonicIcon DetailIcon(AvonicItem item) => Part<AvonicIcon>(item, "PART_DetailIcon");
    private static AvonicRippleEffect Ripple(AvonicItem item) => Part<AvonicRippleEffect>(item, "PART_Ripple");

    private static bool Has(AvonicItem item, string pseudoClass) => item.Classes.Contains(pseudoClass);

    /// <summary>A control's box in the window, through any mirroring.</summary>
    private static Rect Box(Visual visual, Visual root)
    {
        var transform = visual.TransformToVisual(root) ?? throw new InvalidOperationException("not in the tree");
        return new Rect(visual.Bounds.Size).TransformToAABB(transform);
    }

    private static void AssertBox(double x, double y, double width, double height, Rect actual)
    {
        // Layout rounds to whole pixels at this scale, and Chromium keeps half pixels.
        Assert.InRange(actual.X, x - 0.5, x + 0.5);
        Assert.InRange(actual.Y, y - 0.5, y + 0.5);
        Assert.InRange(actual.Width, width - 0.5, width + 0.5);
        Assert.InRange(actual.Height, height - 0.5, height + 0.5);
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

    /// <summary>Transitions run on real time, so a test of a state's end value turns them off.</summary>
    private static AvonicItem NoTransitions(AvonicItem item)
    {
        item.Resources["Avonic.Item.Transition"] = new Transitions();
        return item;
    }

    private static void SettleTransitions() => Dispatcher.UIThread.RunJobs();

    private static Color Pixel(Scene scene, int x, int y)
    {
        SettleTransitions();
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, x, y);
    }

    private static Color Solid(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    private static void AssertClose(Color expected, Color actual, int tolerance = 2)
    {
        Assert.InRange(actual.R, expected.R - tolerance, expected.R + tolerance);
        Assert.InRange(actual.G, expected.G - tolerance, expected.G + tolerance);
        Assert.InRange(actual.B, expected.B - tolerance, expected.B + tolerance);
    }

    // ── Behaviours 1, 2: what Button turns on ────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Button_Is_Activatable_With_A_Ripple()
    {
        var item = new AvonicItem { Button = true, Content = "x" };
        using var scene = new Scene(item);

        Assert.True(Activatable.GetIsEnabled(item));
        Assert.True(Has(item, ":ion-activatable"));
        Assert.True(Ripple(item).IsVisible);

        using var clock = new TestClock();
        scene.Window.MouseDown(RowCentre, MouseButton.Left, RawInputModifiers.None);
        clock.At(100);
        Assert.True(Has(item, Activatable.ActivatedPseudoClass));
        Assert.Single(Ripple(item).Ripples);
        scene.Window.MouseUp(RowCentre, MouseButton.Left, RawInputModifiers.None);
    }

    [AvaloniaFact]
    public void B1_The_Ripple_Fills_The_Row_And_Follows_Its_Colours() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicItem { Button = true };
        var token = new AvonicItem { Button = true };
        token.Resources["Avonic.Item.RippleColor"] = Brushes.Red;
        var coloured = new AvonicItem { Button = true, Color = "danger" };
        using var scene = new Scene(new StackPanel { Children = { plain, token, coloured } });

        AssertBox(0, 0, 390, 48, Box(Ripple(plain), scene.Window));
        Assert.Equal(Colors.Black, Solid(Ripple(plain).Foreground));
        Assert.Equal(Colors.Red, Solid(Ripple(token).Foreground));
        Assert.Equal(Colors.White, Solid(Ripple(coloured).Foreground));
    });

    [AvaloniaFact]
    public void B1_Button_Shows_A_Hand_Cursor() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicItem { Button = true };
        var plain = new AvonicItem();
        using var scene = new Scene(new StackPanel { Children = { button, plain } });

        Assert.Equal(StandardCursorType.Hand, CursorType(button));
        Assert.Null(plain.Cursor);
    });

    private static StandardCursorType CursorType(Control control) =>
        control.Cursor?.ToString() == new Cursor(StandardCursorType.Hand).ToString()
            ? StandardCursorType.Hand
            : StandardCursorType.Arrow;

    [AvaloniaFact]
    public void B2_Plain_Item_Is_Not_Activatable()
    {
        var item = new AvonicItem { Content = "x" };
        using var scene = new Scene(item);

        Assert.False(Activatable.GetIsEnabled(item));
        Assert.False(Has(item, ":ion-activatable"));
        Assert.False(FocusVisible.GetIsEnabled(item));
        Assert.False(Ripple(item).IsVisible);
    }

    [AvaloniaFact]
    public void B2_Turning_Button_Off_Removes_All_Of_It()
    {
        var item = new AvonicItem { Button = true, Content = "x" };
        using var scene = new Scene(item);

        item.Button = false;

        Assert.False(Activatable.GetIsEnabled(item));
        Assert.False(FocusVisible.GetIsEnabled(item));
        Assert.False(item.Focusable);
        Assert.False(Ripple(item).IsVisible);
    }

    // ── Behaviours 3-6: Click and tab stops ──────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Tap_On_Button_Item_Raises_Click()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "x" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B3_Secondary_Button_Does_Not_Raise_Click()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "x" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, RowCentre, MouseButton.Right);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B3_Tap_On_Plain_Item_Does_Not_Raise_Click()
    {
        var clicks = 0;
        var item = new AvonicItem { Content = "x" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B3_Tap_On_Disabled_Item_Does_Not_Raise_Click()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, IsEnabled = false, Content = "x" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B3_Click_Bubbles_From_The_Item()
    {
        var bubbled = 0;
        var item = new AvonicItem { Button = true, Content = "x" };
        var parent = new StackPanel { Children = { item } };
        parent.AddHandler(AvonicItem.ClickEvent, (_, _) => bubbled++);
        using var scene = new Scene(parent);

        Tap(scene, Box(item, scene.Window).Center);

        Assert.Equal(1, bubbled);
    }

    [AvaloniaFact]
    public void B3_Two_Quick_Taps_Raise_Click_Twice()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "x" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, RowCentre);
        Tap(scene, RowCentre);
        Tap(scene, RowCentre);

        Assert.Equal(3, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Second_Quick_Press_Clicks_On_Release_Not_On_Press()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "x" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, RowCentre);
        scene.Window.MouseDown(RowCentre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        scene.Window.MouseUp(RowCentre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B3_A_Second_Quick_Press_Released_Off_The_Item_Does_Not_Click()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "x", Width = 300, Height = 48, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);
        var inside = new Point(100, 24);

        Tap(scene, inside);
        scene.Window.MouseDown(inside, MouseButton.Left, RawInputModifiers.None);
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
        var item = new AvonicItem { Button = true, Content = "x", Height = 48, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        var other = new Border { Height = 48, Background = Brushes.Gray };
        item.Click += (_, _) => clicks++;
        item.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        using var scene = new Scene(new StackPanel { Children = { item, other } });

        Tap(scene, RowCentre);
        Assert.Equal(1, clicks);

        // As a scroll does: the pointer goes elsewhere, so the release never reaches the item.
        scene.Window.MouseDown(RowCentre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        pointer!.Capture(other);
        scene.Window.MouseUp(RowCentre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, clicks);

        // Elsewhere on the row, so this is a new first tap, which clicks once.
        Tap(scene, new Point(60, 24));
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B4_Tap_On_A_Child_Also_Raises_Click()
    {
        var itemClicks = 0;
        var childClicks = 0;
        var child = HittableButton();
        child.Click += (_, _) => childClicks++;
        var item = new AvonicItem { Button = true, EndContent = child };
        item.Click += (_, _) => itemClicks++;
        using var scene = new Scene(item);

        var centre = Box(child, scene.Window).Center;
        Tap(scene, centre);

        Assert.Equal(1, childClicks);
        Assert.Equal(1, itemClicks);
    }

    [AvaloniaFact]
    public void B4_A_Child_That_Handles_Tapped_Does_Not_Stop_Click()
    {
        var clicks = 0;
        var child = new Border { Width = 60, Height = 30, Background = Brushes.Gray };
        child.Tapped += (_, e) => e.Handled = true;
        var item = new AvonicItem { Button = true, EndContent = child };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, Box(child, scene.Window).Center);

        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B3_Another_Pointer_Releasing_Does_Not_Complete_A_Second_Press()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "x" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, RowCentre);
        scene.Window.MouseDown(RowCentre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        item.RaiseEvent(new PointerReleasedEventArgs(
            item, new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true), scene.Window, RowCentre, 0,
            new PointerPointProperties(), KeyModifiers.None, MouseButton.Left));
        Assert.Equal(1, clicks);

        scene.Window.MouseUp(RowCentre, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, clicks);
    }

    [AvaloniaFact]
    public void B5_Enter_Activates_On_Press()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "x" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);
        item.Focus();

        PressKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, clicks);

        ReleaseKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B5_Space_Activates_On_Release()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "x" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);
        item.Focus();

        PressKey(scene, PhysicalKey.Space);
        Assert.Equal(0, clicks);

        ReleaseKey(scene, PhysicalKey.Space);
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B5_Space_Released_After_Focus_Moved_Away_Does_Not_Activate()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "x" };
        var other = new Button { Content = "y" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(new StackPanel { Children = { item, other } });
        item.Focus();

        PressKey(scene, PhysicalKey.Space);
        other.Focus();
        item.Focus();
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B5_Keys_Also_Forward_To_The_First_Interactive()
    {
        var toggle = StubControl(ItemControlType.Toggle);
        var item = new AvonicItem { Button = true, EndContent = toggle };
        using var scene = new Scene(item);
        item.Focus();

        PressKey(scene, PhysicalKey.Enter);
        Assert.Equal(1, toggle.ItemTaps);

        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);
        Assert.Equal(2, toggle.ItemTaps);
    }

    [AvaloniaFact]
    public void B5_Turning_Button_Off_During_A_Space_Press_Does_Not_Click()
    {
        var clicks = 0;
        var toggle = StubControl(ItemControlType.Toggle);
        var item = new AvonicItem { Button = true, EndContent = toggle };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);
        item.Focus();

        PressKey(scene, PhysicalKey.Space);
        item.Button = false;
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
        Assert.Equal(0, toggle.ItemTaps);
    }

    [AvaloniaFact]
    public void B5_Keys_Do_Nothing_On_A_Plain_Item()
    {
        var clicks = 0;
        var toggle = StubControl(ItemControlType.Toggle);
        var item = new AvonicItem { EndContent = toggle };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);
        item.Focusable = true;
        item.Focus();

        PressKey(scene, PhysicalKey.Enter);

        Assert.Equal(0, clicks);
        Assert.Equal(0, toggle.ItemTaps);
    }

    [AvaloniaFact]
    public void B5_Keys_On_A_Nested_Control_Are_Its_Own()
    {
        var clicks = 0;
        var child = new Border { Width = 40, Height = 20, Focusable = true };
        var item = new AvonicItem { Button = true, EndContent = child };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);
        child.Focus();

        PressKey(scene, PhysicalKey.Enter);
        PressKey(scene, PhysicalKey.Space);
        ReleaseKey(scene, PhysicalKey.Space);

        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B6_Tab_Stops_On_Button_Items_Only()
    {
        var plain = new AvonicItem { Content = "plain" };
        var button = new AvonicItem { Button = true, Content = "button" };
        var disabled = new AvonicItem { Button = true, IsEnabled = false, Content = "disabled" };
        var last = new AvonicItem { Button = true, Content = "last" };
        using var scene = new Scene(new StackPanel { Children = { plain, button, disabled, last } });

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(button.IsFocused);

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(last.IsFocused);
    }

    // ── Behaviours 7-9: the detail icon ──────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(null, false, false)]
    [InlineData(null, true, false)]   // ios would show it here
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void B7_Detail_Icon_Shows_Only_When_True(bool? detail, bool button, bool visible)
    {
        var item = new AvonicItem { Detail = detail, Button = button };
        using var scene = new Scene(item);

        Assert.Equal(visible, DetailIcon(item).IsVisible);
    }

    [AvaloniaFact]
    public void B7_Detail_Follows_Changes()
    {
        var item = new AvonicItem();
        using var scene = new Scene(item);

        item.Detail = true;
        Assert.True(DetailIcon(item).IsVisible);

        item.Detail = null;
        Assert.False(DetailIcon(item).IsVisible);
    }

    [AvaloniaFact]
    public void B8_Detail_Icon_Defaults_To_The_Chevron_And_Changes()
    {
        var item = new AvonicItem { Detail = true };
        using var scene = new Scene(item);

        Assert.Equal("chevron-forward", DetailIcon(item).Icon);

        item.DetailIcon = "heart";
        Assert.Equal("heart", DetailIcon(item).Icon);
    }

    [AvaloniaFact]
    public void B8_Default_Detail_Icon_Flips_In_Rtl()
    {
        // On the centre row a chevron is only ink at its point, so which side that's on says which way it faces.
        static double PointSide(AvonicItem item, Scene scene)
        {
            var frame = scene.Window.CaptureRenderedFrame()!;
            var box = Box(DetailIcon(item), scene.Window);
            double weight = 0, sum = 0;
            for (var x = (int)box.X; x < (int)box.Right; x++)
            {
                var ink = 255 - Frames.PixelAt(frame, x, (int)box.Center.Y).R;
                weight += ink;
                sum += ink * (x - box.Center.X);
            }

            return sum / weight;
        }

        InstalledTheme.Run(_ =>
        {
            var chevron = new AvonicItem { Detail = true, Lines = ItemLines.None };
            using var scene = new Scene(new StackPanel { Children = { chevron } });
            Assert.True(PointSide(chevron, scene) > 0);

            chevron.FlowDirection = FlowDirection.RightToLeft;
            Dispatcher.UIThread.RunJobs();
            Assert.True(PointSide(chevron, scene) < 0);
        });
    }

    // Measured: arrow-forward and chevron-down flip by name, as the default does, and heart doesn't.
    [AvaloniaTheory]
    [InlineData("arrow-forward", true)]
    [InlineData("chevron-down", true)]
    [InlineData("heart", false)]
    public void B8_A_Custom_Detail_Icon_Flips_By_Its_Name(string icon, bool flips)
    {
        var item = new AvonicItem { Detail = true, DetailIcon = icon, FlowDirection = FlowDirection.RightToLeft };
        using var scene = new Scene(item);

        Assert.Equal(flips, DetailIcon(item).Classes.Contains(":flip-rtl"));
    }

    [AvaloniaFact]
    public void B9_Detail_Icon_Box_Opacity_And_Colour() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Detail = true, Foreground = Brushes.Lime };
        using var scene = new Scene(new StackPanel { Children = { item } });

        var icon = DetailIcon(item);
        AssertBox(360, 13.5, 20, 20, Box(icon, scene.Window));
        Assert.Equal(0.25, icon.Opacity);
        Assert.Equal(Colors.Lime, Solid(icon.Foreground));
        Assert.Equal(336, Container(item).Bounds.Width);
    });

    [AvaloniaFact]
    public void B9_Detail_Icon_Scales_With_The_Item() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Detail = true, FontSize = 20 };
        using var scene = new Scene(item);

        Assert.Equal(25, DetailIcon(item).Bounds.Width);
    });

    [AvaloniaFact]
    public void B9_Detail_Tokens_Override()
    {
        InstalledTheme.Run(_ =>
        {
            var item = new AvonicItem { Detail = true };
            item.Resources["Avonic.Item.DetailIconColor"] = Brushes.Red;
            item.Resources["Avonic.Item.DetailIconOpacity"] = 0.5;
            item.Resources["Avonic.Item.DetailIconFontSize"] = 2d;
            using var scene = new Scene(item);

            var icon = DetailIcon(item);
            Assert.Equal(Colors.Red, Solid(icon.Foreground));
            Assert.Equal(0.5, icon.Opacity);
            Assert.Equal(32, icon.Bounds.Width);
        });
    }

    // ── Behaviours 10, 11: lines ─────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(null, 0, 1, 48)]
    [InlineData(ItemLines.Inset, 0, 1, 48)]
    [InlineData(ItemLines.Full, 1, 0, 49)]
    [InlineData(ItemLines.None, 0, 0, 48)]
    public void B10_Lines_Choose_The_Border(ItemLines? lines, double outer, double inner, double height)
    {
        InstalledTheme.Run(_ =>
        {
            var item = new AvonicItem { Lines = lines, Content = "x" };
            using var scene = new Scene(new StackPanel { Children = { item } });

            Assert.Equal(new Thickness(0, 0, 0, outer), Native(item).BorderThickness);
            Assert.Equal(new Thickness(0, 0, 0, inner), Inner(item).BorderThickness);
            Assert.Equal(height, item.Bounds.Height);
        });
    }

    [AvaloniaTheory]
    [InlineData(null, ":lines-default")]
    [InlineData(ItemLines.Full, ":lines-full")]
    [InlineData(ItemLines.Inset, ":lines-inset")]
    [InlineData(ItemLines.None, ":lines-none")]
    public void B10_Lines_Pseudoclass_Is_Exclusive(ItemLines? lines, string expected)
    {
        var item = new AvonicItem { Lines = ItemLines.Full };
        item.Lines = lines;

        foreach (var pseudoClass in new[] { ":lines-default", ":lines-full", ":lines-inset", ":lines-none" })
        {
            Assert.Equal(pseudoClass == expected, Has(item, pseudoClass));
        }
    }

    [AvaloniaFact]
    public void B10_The_Border_Is_Drawn_Inset_By_The_Start_Padding() => InstalledTheme.Run(_ =>
    {
        var inset = new AvonicItem();
        var full = new AvonicItem { Lines = ItemLines.Full };
        using var scene = new Scene(new StackPanel { Children = { inset, full } });

        var frame = scene.Window.CaptureRenderedFrame()!;

        // rgba(0, 0, 0, 0.13) on white is 222.
        Assert.Equal(255, Frames.PixelAt(frame, 8, 47).R);
        Assert.InRange((int)Frames.PixelAt(frame, 24, 47).R, 220, 224);
        Assert.InRange((int)Frames.PixelAt(frame, 8, 96).R, 220, 224);
    });

    [AvaloniaFact]
    public void B10_An_Explicit_Value_Beats_The_Tokens() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Lines = ItemLines.None };
        item.Resources["Avonic.Item.InnerBorderWidth"] = new Thickness(0, 0, 0, 5);
        using var scene = new Scene(item);

        Assert.Equal(new Thickness(0), Inner(item).BorderThickness);
    });

    [AvaloniaFact]
    public void B10_Default_Lines_Read_The_Tokens() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem();
        item.Resources["Avonic.Item.InnerBorderWidth"] = new Thickness(0, 0, 0, 5);
        item.Resources["Avonic.Item.BorderWidth"] = new Thickness(0, 0, 0, 3);
        using var scene = new Scene(item);

        Assert.Equal(new Thickness(0, 0, 0, 5), Inner(item).BorderThickness);
        Assert.Equal(new Thickness(0, 0, 0, 3), Native(item).BorderThickness);
    });

    // ── Behaviours 12-14: colour ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B12_Color_Paints_Base_Contrast_And_Shade() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Color = "danger", Detail = true, Content = "x" };
        using var scene = new Scene(item);

        Assert.Equal(Color.Parse("#C5000F"), Solid(Native(item).Background));
        Assert.Equal(Colors.White, Solid(item.Foreground));
        Assert.Equal(Color.Parse("#AD000D"), Solid(Native(item).BorderBrush));
        Assert.Equal(Color.Parse("#AD000D"), Solid(Inner(item).BorderBrush));
        Assert.Equal(Colors.White, Solid(DetailIcon(item).Foreground));
    });

    [AvaloniaFact]
    public void B13_Unknown_Color_Is_Transparent_With_CurrentColor_Borders() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Color = "nonexistent", Content = "x" };
        using var scene = new Scene(item);

        Assert.Null(Native(item).Background);
        Assert.Equal(Colors.Black, Solid(Inner(item).BorderBrush));
    });

    [AvaloniaFact]
    public void B13_Unknown_Color_Text_Falls_Back_To_The_Item_Colour() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Color = "nonexistent", Content = "x" };
        item.Resources["Avonic.ItemColorBrush"] = Brushes.Blue;
        var overridden = new AvonicItem { Color = "nonexistent", Content = "x" };
        overridden.Resources["Avonic.Item.Color"] = Brushes.Lime;
        using var scene = new Scene(new StackPanel { Children = { item, overridden } });

        Assert.Equal(Colors.Blue, Solid(item.Foreground));
        Assert.Equal(Colors.Lime, Solid(overridden.Foreground));
    });

    [AvaloniaFact]
    public void B13_Unknown_Color_Still_Takes_Input() => InstalledTheme.Run(_ =>
    {
        var clicks = 0;
        var item = new AvonicItem { Color = "nonexistent", Button = true };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(1, clicks);
    });

    [AvaloniaFact]
    public void B14_No_Color_In_A_Coloured_Ancestor_Keeps_Every_Default() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Button = true, Detail = true, Content = "x" });
        var parent = new Border { Child = item };
        ColorPalette.SetColor(parent, "danger");
        using var scene = new Scene(parent);

        Assert.Equal(Colors.White, Solid(Native(item).Background));
        Assert.Equal(Colors.Black, Solid(item.Foreground));
        Assert.Equal(Color.Parse("#21000000"), Solid(Inner(item).BorderBrush));

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        SettleTransitions();
        Assert.Equal(Colors.Black, Solid(Overlay(item).Background));
    });

    // ── Behaviours 15-18: state overlay ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B15_Hover_Shows_The_Overlay() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Button = true });
        using var scene = new Scene(new StackPanel { Children = { item } });

        Assert.Equal(Colors.White, Pixel(scene, 200, 24));

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);

        // 4% black on white
        AssertClose(Color.FromRgb(245, 245, 245), Pixel(scene, 200, 24));
        Assert.Equal(0.04, Overlay(item).Opacity, 3);

        scene.Window.MouseMove(new Point(200, 400), RawInputModifiers.None);
        Assert.Equal(Colors.White, Pixel(scene, 200, 24));
    });

    [AvaloniaFact]
    public void B15_The_Overlay_Covers_The_Padding() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Button = true });
        using var scene = new Scene(new StackPanel { Children = { item } });

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);

        // Inside the 16 px of start padding.
        AssertClose(Color.FromRgb(245, 245, 245), Pixel(scene, 4, 24));
        AssertBox(0, 0, 390, 48, Box(Overlay(item), scene.Window));
    });

    [AvaloniaFact]
    public void B15_Hover_Token_Applies() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Button = true });
        item.Resources["Avonic.Item.BackgroundHover"] = Brushes.Red;
        using var scene = new Scene(item);

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        SettleTransitions();

        Assert.Equal(Colors.Red, Solid(Overlay(item).Background));
    });

    [AvaloniaFact]
    public void B15_Plain_Item_Has_No_Hover() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem();
        using var scene = new Scene(item);

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);

        Assert.Equal(Colors.White, Pixel(scene, 200, 24));
    });

    [AvaloniaFact]
    public void B15_A_Touch_Does_Not_Hover()
    {
        var item = new AvonicItem { Button = true };
        using var scene = new Scene(item);

        Enter(item, PointerType.Touch);
        Assert.False(Has(item, Activatable.HoverPseudoClass));

        Enter(item, PointerType.Pen);
        Assert.True(Has(item, Activatable.HoverPseudoClass));

        static void Enter(AvonicItem target, PointerType type) =>
            target.RaiseEvent(new PointerEventArgs(
                InputElement.PointerEnteredEvent, target, new Pointer(Pointer.GetNextFreeId(), type, true),
                TopLevel.GetTopLevel(target), default, 0, new PointerPointProperties(), KeyModifiers.None));
    }

    [AvaloniaFact]
    public void B15_Becoming_Activatable_Under_A_Still_Pointer_Hovers()
    {
        var item = new AvonicItem();
        using var scene = new Scene(item);
        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        item.Button = true;

        Assert.True(Has(item, Activatable.HoverPseudoClass));
    }

    [AvaloniaFact]
    public void B15_Hover_Ends_When_The_Item_Stops_Being_Activatable()
    {
        var item = new AvonicItem { Button = true };
        using var scene = new Scene(item);
        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        Assert.True(Has(item, Activatable.HoverPseudoClass));

        item.Button = false;

        Assert.False(Has(item, Activatable.HoverPseudoClass));
    }

    [AvaloniaFact]
    public void B16_Keyboard_Focus_Shows_The_Stronger_Overlay_Over_Hover() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Button = true });
        using var scene = new Scene(item);

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(Has(item, FocusVisible.FocusedPseudoClass));

        // 12% black on white
        AssertClose(Color.FromRgb(224, 224, 224), Pixel(scene, 200, 24));

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        AssertClose(Color.FromRgb(224, 224, 224), Pixel(scene, 200, 24));
    });

    // outline: none. The adorner comes from the app's base theme, which TestApp doesn't load, so this adds it
    // underneath, in the same order as the sample. FocusAdorner reads null whether or not it's set, because an
    // unset one falls back to the base theme's default, so only the layer shows what's really drawn.
    [AvaloniaFact]
    public void B16_There_Is_No_Focus_Adorner_Over_A_Base_Theme()
    {
        var styles = Application.Current!.Styles;
        var baseTheme = new Avalonia.Themes.Simple.SimpleTheme();
        styles.Insert(0, baseTheme);
        try
        {
            InstalledTheme.Run(_ =>
            {
                var item = new AvonicItem { Button = true };
                var other = new Button();
                using var scene = new Scene(new StackPanel { Children = { item, other } });

                var layer = Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(item)!;

                PressKey(scene, PhysicalKey.Tab);
                Assert.True(item.IsFocused);
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

    [AvaloniaFact]
    public void B16_Focus_Tokens_Apply() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Button = true });
        item.Resources["Avonic.Item.BackgroundFocused"] = Brushes.Red;
        item.Resources["Avonic.Item.ColorFocused"] = Brushes.Lime;
        using var scene = new Scene(item);

        PressKey(scene, PhysicalKey.Tab);
        SettleTransitions();

        Assert.Equal(Colors.Red, Solid(Overlay(item).Background));
        Assert.Equal(Colors.Lime, Solid(item.Foreground));
    });

    [AvaloniaFact]
    public void B16_A_Press_Clears_Focus_State() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true };
        using var scene = new Scene(new StackPanel { Children = { item } });

        PressKey(scene, PhysicalKey.Tab);
        Assert.True(Has(item, FocusVisible.FocusedPseudoClass));

        Tap(scene, new Point(200, 400));

        Assert.False(Has(item, FocusVisible.FocusedPseudoClass));
    });

    [AvaloniaFact]
    public void B16_Other_Keys_Clear_Focus_State_And_Focus_Keys_Do_Not()
    {
        var item = new AvonicItem { Button = true };
        using var scene = new Scene(item);

        PressKey(scene, PhysicalKey.Tab);
        PressKey(scene, PhysicalKey.ArrowDown);
        Assert.True(Has(item, FocusVisible.FocusedPseudoClass));

        PressKey(scene, PhysicalKey.A);
        Assert.False(Has(item, FocusVisible.FocusedPseudoClass));
    }

    [AvaloniaFact]
    public void B16_Mouse_Focus_Does_Not_Show_It()
    {
        var item = new AvonicItem { Button = true };
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.True(item.IsFocused);
        Assert.False(Has(item, FocusVisible.FocusedPseudoClass));
    }

    [AvaloniaFact]
    public void B16_Leaving_Clears_It()
    {
        var item = new AvonicItem { Button = true };
        var other = new AvonicItem { Button = true };
        using var scene = new Scene(new StackPanel { Children = { item, other } });

        PressKey(scene, PhysicalKey.Tab);
        PressKey(scene, PhysicalKey.Tab);

        Assert.False(Has(item, FocusVisible.FocusedPseudoClass));
        Assert.True(Has(other, FocusVisible.FocusedPseudoClass));
    }

    [AvaloniaFact]
    public void B17_Press_Keeps_The_Hover_Overlay() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var item = NoTransitions(new AvonicItem { Button = true });
        using var scene = new Scene(item);

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        scene.Window.MouseDown(RowCentre, MouseButton.Left, RawInputModifiers.None);
        clock.At(100);
        Assert.True(Has(item, Activatable.ActivatedPseudoClass));

        SettleTransitions();
        Assert.Equal(0.04, Overlay(item).Opacity, 3);
        Assert.Equal(Colors.Black, Solid(Overlay(item).Background));

        scene.Window.MouseUp(RowCentre, MouseButton.Left, RawInputModifiers.None);
    });

    [AvaloniaFact]
    public void B17_Activated_Without_Hover_Uses_Its_Tokens() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var item = NoTransitions(new AvonicItem { Button = true });
        item.Resources["Avonic.Item.BackgroundActivatedOpacity"] = 0.5;
        item.Resources["Avonic.Item.BackgroundActivated"] = Brushes.Red;
        item.Resources["Avonic.Item.ColorActivated"] = Brushes.Lime;
        using var scene = new Scene(item);

        // Mouse over is what's being avoided here, as on a touch device.
        scene.Window.MouseDown(RowCentre, MouseButton.Left, RawInputModifiers.None);
        ((IPseudoClasses)item.Classes).Remove(Activatable.HoverPseudoClass);
        clock.At(100);
        SettleTransitions();

        Assert.Equal(0.5, Overlay(item).Opacity, 3);
        Assert.Equal(Colors.Red, Solid(Overlay(item).Background));
        Assert.Equal(Colors.Lime, Solid(item.Foreground));

        scene.Window.MouseUp(RowCentre, MouseButton.Left, RawInputModifiers.None);
    });

    [AvaloniaFact]
    public void B18_On_Color_The_Overlay_Uses_Contrast() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Button = true, Color = "danger" });
        using var scene = new Scene(item);

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        SettleTransitions();
        Assert.Equal(Colors.White, Solid(Overlay(item).Background));

        scene.Window.MouseMove(new Point(200, 400), RawInputModifiers.None);
        PressKey(scene, PhysicalKey.Tab);
        SettleTransitions();
        Assert.Equal(Colors.White, Solid(Overlay(item).Background));
        Assert.Equal(0.12, Overlay(item).Opacity, 3);
    });

    // These pass by coincidence without a token set, because the current colour is already the contrast.
    [AvaloniaFact]
    public void B18_Contrast_Beats_The_Overlay_Tokens() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Button = true, Color = "danger" });
        item.Resources["Avonic.Item.BackgroundHover"] = Brushes.Red;
        item.Resources["Avonic.Item.BackgroundFocused"] = Brushes.Red;
        using var scene = new Scene(item);

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        SettleTransitions();
        Assert.Equal(Colors.White, Solid(Overlay(item).Background));

        scene.Window.MouseMove(new Point(200, 400), RawInputModifiers.None);
        PressKey(scene, PhysicalKey.Tab);
        SettleTransitions();
        Assert.Equal(Colors.White, Solid(Overlay(item).Background));
    });

    [AvaloniaFact]
    public void B18_On_Color_The_Activated_Overlay_Is_Transparent() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var item = NoTransitions(new AvonicItem { Button = true, Color = "danger" });
        item.Resources["Avonic.Item.BackgroundActivated"] = Brushes.Red;
        item.Resources["Avonic.Item.BackgroundActivatedOpacity"] = 0.5;
        using var scene = new Scene(item);

        scene.Window.MouseDown(RowCentre, MouseButton.Left, RawInputModifiers.None);
        ((IPseudoClasses)item.Classes).Remove(Activatable.HoverPseudoClass);
        clock.At(100);
        SettleTransitions();

        Assert.Equal(0.5, Overlay(item).Opacity, 3);
        Assert.Equal(Colors.Transparent, Solid(Overlay(item).Background));

        scene.Window.MouseUp(RowCentre, MouseButton.Left, RawInputModifiers.None);
    });

    [AvaloniaFact]
    public void B18_State_Colours_Do_Not_Beat_Contrast() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true, Color = "danger" };
        item.Resources["Avonic.Item.ColorHover"] = Brushes.Lime;
        using var scene = new Scene(item);

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        SettleTransitions();

        Assert.Equal(Colors.White, Solid(item.Foreground));
    });

    [AvaloniaFact]
    public void B18_State_Colours_Follow_The_Item_Colour_Unless_Set() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true };
        item.Resources["Avonic.Item.Color"] = Brushes.Blue;
        using var scene = new Scene(item);
        Assert.Equal(Colors.Blue, Solid(item.Foreground));

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        SettleTransitions();
        Assert.Equal(Colors.Blue, Solid(item.Foreground));

        item.Resources["Avonic.Item.ColorHover"] = Brushes.Lime;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Lime, Solid(item.Foreground));
    });

    // ── Behaviour 19: disabled ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B19_Disabled_Is_Faded_And_Takes_No_Input() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true, IsEnabled = false };
        using var scene = new Scene(item);

        Assert.Equal(0.3, item.Opacity);

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        Assert.False(Has(item, Activatable.HoverPseudoClass));

        using var clock = new TestClock();
        scene.Window.MouseDown(RowCentre, MouseButton.Left, RawInputModifiers.None);
        clock.At(200);
        Assert.False(Has(item, Activatable.ActivatedPseudoClass));
        scene.Window.MouseUp(RowCentre, MouseButton.Left, RawInputModifiers.None);
    });

    // ── Behaviour 20: focus inside ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B20_Focus_On_A_Child_Shows_On_A_Focusable_Item()
    {
        var child = new Button { Content = "x" };
        var item = new AvonicItem { Button = true, EndContent = child };
        using var scene = new Scene(item);

        PressKey(scene, PhysicalKey.Tab);
        PressKey(scene, PhysicalKey.Tab);

        Assert.True(child.IsFocused);
        Assert.True(Has(item, FocusVisible.FocusedPseudoClass));
    }

    [AvaloniaFact]
    public void B20_A_Plain_Nested_Button_Does_Not_Make_An_Item_Focusable()
    {
        var child = new Button { Content = "x" };
        var item = new AvonicItem { EndContent = child };
        using var scene = new Scene(item);

        PressKey(scene, PhysicalKey.Tab);

        Assert.True(child.IsFocused);
        Assert.False(FocusVisible.GetIsEnabled(item));
        Assert.False(Has(item, FocusVisible.FocusedPseudoClass));
    }

    [AvaloniaFact]
    public void B20_A_Focusable_Child_Makes_The_Item_Focusable()
    {
        var child = new Button { Content = "x" };
        FocusVisible.SetIsEnabled(child, true);
        var item = new AvonicItem { EndContent = child };
        using var scene = new Scene(item);

        Assert.True(FocusVisible.GetIsEnabled(item));

        PressKey(scene, PhysicalKey.Tab);

        // As Ionic's composedPath: both surfaces around the focused element.
        Assert.True(Has(item, FocusVisible.FocusedPseudoClass));
        Assert.Contains(FocusVisible.FocusedPseudoClass, child.Classes);
        Assert.False(item.Focusable);
    }

    // ── Behaviours 21-24: the controls inside ────────────────────────────────────────────────

    [AvaloniaTheory]
    // covers + inputs > 1
    [InlineData(new[] { ItemControlType.Checkbox, ItemControlType.Input }, true)]
    [InlineData(new[] { ItemControlType.Input, ItemControlType.Toggle }, true)]
    // covers + clickables > 1
    [InlineData(new[] { ItemControlType.Radio, ItemControlType.Button }, true)]
    [InlineData(new[] { ItemControlType.Select, ItemControlType.Checkbox }, true)]
    // Ionic's formula counts two clickables with no input at all.
    [InlineData(new[] { ItemControlType.Button, ItemControlType.Button }, true)]
    [InlineData(new[] { ItemControlType.Checkbox }, false)]
    [InlineData(new[] { ItemControlType.Input }, false)]
    [InlineData(new[] { ItemControlType.Button }, false)]
    [InlineData(new[] { ItemControlType.Input, ItemControlType.Button }, false)]
    public void B21_Multiple_Inputs(ItemControlType[] types, bool multiple)
    {
        var panel = new StackPanel();
        foreach (var type in types)
        {
            panel.Children.Add(StubControl(type));
        }

        var item = new AvonicItem { Content = panel };
        using var scene = new Scene(item);

        Assert.Equal(multiple, Has(item, ":item-multiple-inputs"));
    }

    [AvaloniaFact]
    public void B21_A_Cover_In_A_Button_Item_Is_Multiple_Inputs()
    {
        var item = new AvonicItem { Button = true, Content = StubControl(ItemControlType.Radio) };
        using var scene = new Scene(item);

        Assert.True(Has(item, ":item-multiple-inputs"));
    }

    [AvaloniaTheory]
    [InlineData(ItemControlType.Checkbox, true)]
    [InlineData(ItemControlType.Datetime, true)]
    [InlineData(ItemControlType.Select, true)]
    [InlineData(ItemControlType.Radio, true)]
    [InlineData(ItemControlType.Input, false)]
    [InlineData(ItemControlType.Toggle, false)]
    [InlineData(ItemControlType.Range, false)]
    [InlineData(ItemControlType.Searchbar, false)]
    [InlineData(ItemControlType.Segment, false)]
    [InlineData(ItemControlType.Textarea, false)]
    [InlineData(ItemControlType.Button, false)]
    public void B21_A_Cover_Makes_The_Item_Activatable(ItemControlType type, bool activatable)
    {
        var item = new AvonicItem { Content = StubControl(type) };
        using var scene = new Scene(item);

        Assert.Equal(activatable, Activatable.GetIsEnabled(item));
        Assert.Equal(activatable, FocusVisible.GetIsEnabled(item));
        Assert.Equal(activatable, Ripple(item).IsVisible);

        // The control takes focus, not the item.
        Assert.False(item.Focusable);
    }

    [AvaloniaFact]
    public void B21_Slot_Changes_Update_It()
    {
        var item = new AvonicItem();
        using var scene = new Scene(item);
        Assert.False(Activatable.GetIsEnabled(item));

        item.Content = StubControl(ItemControlType.Checkbox);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Activatable.GetIsEnabled(item));

        item.EndContent = StubControl(ItemControlType.Input);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Has(item, ":item-multiple-inputs"));
        Assert.False(Activatable.GetIsEnabled(item));

        item.EndContent = null;
        Dispatcher.UIThread.RunJobs();
        Assert.False(Has(item, ":item-multiple-inputs"));
        Assert.True(Activatable.GetIsEnabled(item));

        item.Content = null;
        Dispatcher.UIThread.RunJobs();
        Assert.False(Activatable.GetIsEnabled(item));
    }

    [AvaloniaFact]
    public void B21_Finds_Controls_In_Every_Slot_At_Any_Depth()
    {
        var start = new AvonicItem { StartContent = new Border { Child = new Panel { Children = { StubControl(ItemControlType.Radio) } } } };
        var end = new AvonicItem { EndContent = new Panel { Children = { StubControl(ItemControlType.Radio) } } };
        using var scene = new Scene(new StackPanel { Children = { start, end } });

        Assert.True(Activatable.GetIsEnabled(start));
        Assert.True(Activatable.GetIsEnabled(end));
    }

    [AvaloniaFact]
    public void B21_Start_And_End_Templates_Work()
    {
        static Avalonia.Controls.Templates.FuncDataTemplate<string> Template() =>
            new((_, _) => StubControl(ItemControlType.Checkbox));

        var start = new AvonicItem { StartContent = "model", StartContentTemplate = Template() };
        var end = new AvonicItem { EndContent = "model", EndContentTemplate = Template() };
        using var scene = new Scene(new StackPanel { Children = { start, end } });

        Assert.True(Activatable.GetIsEnabled(start));
        Assert.True(Activatable.GetIsEnabled(end));
    }

    [AvaloniaFact]
    public void B21_Finds_Controls_From_A_Data_Template()
    {
        var item = new AvonicItem
        {
            Content = "model",
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((_, _) => StubControl(ItemControlType.Checkbox)),
        };
        using var scene = new Scene(item);

        Assert.True(Activatable.GetIsEnabled(item));
    }

    [AvaloniaFact]
    public void B22_Tap_On_Padding_Forwards_To_The_First_Interactive()
    {
        var first = StubControl(ItemControlType.Toggle);
        var second = StubControl(ItemControlType.Button);
        var item = new AvonicItem { StartContent = first, EndContent = second };
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(1, first.ItemTaps);
    }

    [AvaloniaFact]
    public void B22_Disabled_Controls_Are_Skipped()
    {
        // One disabled input alone: two would be multiple inputs, even if one is disabled.
        var disabled = StubControl(ItemControlType.Toggle);
        disabled.IsEnabled = false;
        var item = new AvonicItem { EndContent = disabled };
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(0, disabled.ItemTaps);
    }

    [AvaloniaFact]
    public void B22_A_Control_Disabled_Through_An_Ancestor_Is_Skipped()
    {
        var control = StubControl(ItemControlType.Toggle);
        var item = new AvonicItem { EndContent = new StackPanel { IsEnabled = false, Children = { control } } };
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(0, control.ItemTaps);
        Assert.False(Has(item, ":item-control-needs-pointer-cursor"));
    }

    [AvaloniaTheory]
    [InlineData(ItemControlType.Datetime)]
    [InlineData(ItemControlType.Range)]
    [InlineData(ItemControlType.Searchbar)]
    [InlineData(ItemControlType.Segment)]
    public void B22_These_Types_Are_Not_First_Interactive(ItemControlType type)
    {
        var control = StubControl(type);
        var item = new AvonicItem { EndContent = control };
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(0, control.ItemTaps);
    }

    [AvaloniaFact]
    public void B22_Multiple_Inputs_Do_Not_Forward()
    {
        var first = StubControl(ItemControlType.Toggle);
        var second = StubControl(ItemControlType.Input);
        var item = new AvonicItem { StartContent = first, EndContent = second };
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(0, first.ItemTaps);
        Assert.Equal(0, second.ItemTaps);
    }

    [AvaloniaFact]
    public void B22_A_Tap_On_Text_Is_A_Tap_On_Padding()
    {
        var control = StubControl(ItemControlType.Toggle);
        var item = new AvonicItem { Content = "A label in text", EndContent = control };
        using var scene = new Scene(item);

        // A background makes the text itself the hit target, rather than whatever is behind it.
        var text = Assert.IsAssignableFrom<TextBlock>(Container(item).Child);
        text.Background = Brushes.Transparent;
        Dispatcher.UIThread.RunJobs();
        Tap(scene, Box(text, scene.Window).Center);

        Assert.Equal(1, control.ItemTaps);
    }

    [AvaloniaFact]
    public void B22_A_Forwarded_Tap_Is_Handled()
    {
        var bubbled = 0;
        var item = new AvonicItem { EndContent = StubControl(ItemControlType.Toggle) };
        var parent = new StackPanel { Children = { item } };
        parent.Tapped += (_, _) => bubbled++;
        using var scene = new Scene(parent);

        Tap(scene, Box(item, scene.Window).Center);

        Assert.Equal(0, bubbled);
    }

    [AvaloniaFact]
    public void B22_A_Button_Item_With_An_Input_Clicks_And_Forwards()
    {
        var clicks = 0;
        var control = StubControl(ItemControlType.Toggle);
        var item = new AvonicItem { Button = true, EndContent = control };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Tap(scene, RowCentre);

        Assert.Equal(1, clicks);
        Assert.Equal(1, control.ItemTaps);
    }

    [AvaloniaFact]
    public void B22_Text_From_A_Number_Is_Also_Padding()
    {
        var control = StubControl(ItemControlType.Toggle);
        var item = new AvonicItem { Content = 42, EndContent = control };
        using var scene = new Scene(item);

        var text = Assert.IsAssignableFrom<TextBlock>(Container(item).Child);
        text.Background = Brushes.Transparent;
        Dispatcher.UIThread.RunJobs();
        Tap(scene, Box(text, scene.Window).Center);

        Assert.Equal(1, control.ItemTaps);
    }

    [AvaloniaFact]
    public void B23_A_Tap_On_A_Templated_String_Does_Not_Forward()
    {
        var control = StubControl(ItemControlType.Toggle);
        var item = new AvonicItem
        {
            Content = "model",
            ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>(
                (_, _) => new Border { Width = 100, Height = 30, Background = Brushes.Gray }),
            EndContent = control,
        };
        using var scene = new Scene(item);

        Tap(scene, Box(Container(item).Child!, scene.Window).Center);

        Assert.Equal(0, control.ItemTaps);
    }

    [AvaloniaFact]
    public void B23_A_Tap_On_A_Slotted_Child_Does_Not_Forward()
    {
        var control = StubControl(ItemControlType.Toggle);
        var label = new Border { Width = 100, Height = 30, Background = Brushes.Gray };
        var item = new AvonicItem { Content = label, EndContent = control };
        using var scene = new Scene(item);

        Tap(scene, Box(label, scene.Window).Center);
        Assert.Equal(0, control.ItemTaps);

        Tap(scene, Box(control, scene.Window).Center);
        Assert.Equal(0, control.ItemTaps);
    }

    [AvaloniaFact]
    public void B23_A_Tap_Deep_Inside_A_Slotted_Child_Does_Not_Forward()
    {
        var control = StubControl(ItemControlType.Toggle);
        var inner = new Border { Width = 50, Height = 20, Background = Brushes.Gray };
        var item = new AvonicItem { Content = new Panel { Children = { inner } }, EndContent = control };
        using var scene = new Scene(item);

        Tap(scene, Box(inner, scene.Window).Center);

        Assert.Equal(0, control.ItemTaps);
    }

    [AvaloniaTheory]
    [InlineData(ItemControlType.Toggle, true)]
    [InlineData(ItemControlType.Checkbox, true)]
    [InlineData(ItemControlType.Radio, true)]
    [InlineData(ItemControlType.Select, true)]
    [InlineData(ItemControlType.Input, false)]
    [InlineData(ItemControlType.Textarea, false)]
    [InlineData(ItemControlType.Range, false)]
    public void B24_Pointer_Cursor_Pseudoclass(ItemControlType type, bool needsCursor)
    {
        var item = new AvonicItem { Content = StubControl(type) };
        using var scene = new Scene(item);

        Assert.Equal(needsCursor, Has(item, ":item-control-needs-pointer-cursor"));
    }

    [AvaloniaFact]
    public void B24_Pointer_Cursor_Follows_IonStyle()
    {
        var toggle = StubControl(ItemControlType.Toggle);
        var input = StubControl(ItemControlType.Input);
        var panel = new StackPanel { Children = { toggle } };
        var item = new AvonicItem { Content = panel };
        using var scene = new Scene(item);
        Assert.True(Has(item, ":item-control-needs-pointer-cursor"));

        // Ionic re-renders on ionStyle, which finds the first interactive control again.
        toggle.IsEnabled = false;
        Raise(toggle, ("interactive-disabled", true));

        Assert.False(Has(item, ":item-control-needs-pointer-cursor"));
    }

    [AvaloniaFact]
    public void B24_Pointer_Cursor_Follows_The_Item_Being_Enabled()
    {
        var item = new AvonicItem { IsEnabled = false, EndContent = StubControl(ItemControlType.Toggle) };
        using var scene = new Scene(item);
        Assert.False(Has(item, ":item-control-needs-pointer-cursor"));

        item.IsEnabled = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(Has(item, ":item-control-needs-pointer-cursor"));
    }

    [AvaloniaFact]
    public void B24_Pointer_Cursor_Applies_In_The_Theme() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = StubControl(ItemControlType.Toggle) };
        using var scene = new Scene(item);

        Assert.Equal(StandardCursorType.Hand, CursorType(item));
    });

    // ── Behaviours 25-27: ionStyle ───────────────────────────────────────────────────────────

    private static void Raise(Control source, params (string Key, bool Value)[] styles) =>
        source.RaiseEvent(new IonStyleEventArgs(styles.ToDictionary(style => style.Key, style => style.Value)) { Source = source });

    [AvaloniaFact]
    public void B25_True_Keys_Become_Pseudoclasses()
    {
        var child = new Border();
        var item = new AvonicItem { Content = child };
        using var scene = new Scene(item);

        Raise(child, ("label", true), ("label-stacked", false));
        Assert.True(Has(item, ":item-label"));
        Assert.False(Has(item, ":item-label-stacked"));

        Raise(child, ("label-stacked", true));
        Assert.False(Has(item, ":item-label"));
        Assert.True(Has(item, ":item-label-stacked"));
    }

    [AvaloniaFact]
    public void B25_Kept_Per_Type_Of_Child()
    {
        var first = new Border();
        var second = new OtherStyler();
        var item = new AvonicItem { Content = new StackPanel { Children = { first, second } } };
        using var scene = new Scene(item);

        Raise(first, ("label", true));
        Raise(second, ("input", true));
        Assert.True(Has(item, ":item-label"));
        Assert.True(Has(item, ":item-input"));

        Raise(first, ("label", false));
        Assert.False(Has(item, ":item-label"));
        Assert.True(Has(item, ":item-input"));
    }

    [AvaloniaFact]
    public void B25_Two_Types_Reporting_One_Key_Keep_It_Until_Both_Stop()
    {
        var first = new Border();
        var second = new OtherStyler();
        var item = new AvonicItem { Content = new StackPanel { Children = { first, second } } };
        using var scene = new Scene(item);

        Raise(first, ("label", true));
        Raise(second, ("label", true));
        Raise(first, ("label", false));
        Assert.True(Has(item, ":item-label"));

        Raise(second, ("label", false));
        Assert.False(Has(item, ":item-label"));
    }

    [AvaloniaFact]
    public void B25_The_Event_Stops_At_The_Item()
    {
        var outside = 0;
        var child = new Border();
        var item = new AvonicItem { Content = child };
        var parent = new StackPanel { Children = { item } };
        parent.AddHandler(IonStyle.IonStyleEvent, (_, _) => outside++);
        using var scene = new Scene(parent);

        Raise(child, ("label", true));

        Assert.Equal(0, outside);
    }

    [AvaloniaFact]
    public void B26_Interactive_Swaps_The_Borders_Unless_Lines_Are_Explicit() => InstalledTheme.Run(_ =>
    {
        var child = new Border();
        var item = new AvonicItem { Content = child };
        using var scene = new Scene(item);

        Raise(child, ("interactive", true));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(0, 0, 0, 1), Native(item).BorderThickness);
        Assert.Equal(new Thickness(0), Inner(item).BorderThickness);

        item.Lines = ItemLines.None;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(0), Native(item).BorderThickness);
    });

    [AvaloniaTheory]
    [InlineData("label-stacked")]
    [InlineData("label-floating")]
    public void B26_Stacked_And_Floating_Labels_Raise_The_Minimum_And_Align_Top(string key)
    {
        InstalledTheme.Run(_ =>
        {
            var child = new Border();
            var start = new Border { Width = 10, Height = 10 };
            var item = new AvonicItem { Content = child, StartContent = start };
            using var scene = new Scene(new StackPanel { Children = { item } });

            Raise(child, (key, true));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(55, item.Bounds.Height);
            Assert.Equal(0, Box(start, scene.Window).Y);
        });
    }

    [AvaloniaTheory]
    [InlineData("label-stacked")]
    [InlineData("label-floating")]
    public void B26_Stacked_And_Floating_Labels_Move_The_Detail_Icon_And_End_Margins(string key)
    {
        InstalledTheme.Run(_ =>
        {
            var child = new Border { Height = 80 };
            var endBox = new Border { Width = 10, Height = 10 };
            var endIcon = new AvonicIcon { IconName = "star" };
            var boxItem = new AvonicItem { Content = child, EndContent = endBox, Detail = true };
            var iconItem = new AvonicItem { Content = new Border(), EndContent = endIcon };
            using var scene = new Scene(new StackPanel { Children = { boxItem, iconItem } });

            Raise(child, (key, true));
            Raise((Control)iconItem.Content!, (key, true));
            Dispatcher.UIThread.RunJobs();

            // Measured: "7px 0px 7px 16px" on both, and the detail icon at the top.
            Assert.Equal(new Thickness(16, 7, 0, 7), endBox.Margin);
            Assert.Equal(new Thickness(16, 7, 0, 7), endIcon.Margin);
            Assert.Equal(0, Box(DetailIcon(boxItem), scene.Window).Y);
        });
    }

    [AvaloniaFact]
    public void B26_Textarea_Stretches_The_Slots() => InstalledTheme.Run(_ =>
    {
        var child = new Border { Height = 80 };
        var start = new Border { Width = 10 };
        var end = new Border { Width = 10 };
        var item = new AvonicItem { Content = child, StartContent = start, EndContent = end, Detail = true };
        using var scene = new Scene(new StackPanel { Children = { item } });

        Raise(child, ("textarea", true));
        Dispatcher.UIThread.RunJobs();

        // Measured: 81 and 80, the difference being the inner border.
        Assert.Equal(81, start.Bounds.Height);
        Assert.Equal(80, end.Bounds.Height);
        Assert.Equal(0, Box(DetailIcon(item), scene.Window).Y);
    });

    [AvaloniaFact]
    public void B26_Textarea_Stretches_Default_Slot_Content_Too() => InstalledTheme.Run(_ =>
    {
        var child = new Border();
        var item = new AvonicItem { Content = child, StartContent = new Border { Width = 10, Height = 100 } };
        using var scene = new Scene(new StackPanel { Children = { item } });

        Raise(child, ("textarea", true));
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(99, child.Bounds.Height);
    });

    [AvaloniaFact]
    public void B27_Interactive_Disabled_Takes_No_Input()
    {
        var clicks = 0;
        var child = new Border();
        var item = new AvonicItem { Button = true, Content = child };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        Raise(child, ("interactive-disabled", true));
        Tap(scene, RowCentre);
        Assert.Equal(0, clicks);

        // Somewhere else: in the same spot, this would be a double click whose first press hit nothing.
        Raise(child, ("interactive-disabled", false));
        Tap(scene, new Point(100, 24));
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B27_Not_With_Multiple_Inputs()
    {
        var child = StubControl(ItemControlType.Input);
        var item = new AvonicItem
        {
            Content = child,
            EndContent = StubControl(ItemControlType.Toggle),
        };
        using var scene = new Scene(item);

        Raise(child, ("interactive-disabled", true));

        Assert.True(item.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void B27_An_Apps_Own_Values_Survive()
    {
        var overlaid = new AvonicItem { IsHitTestVisible = false, Content = "x" };
        var unfocusable = new AvonicItem { Button = true, Focusable = false, Content = "x" };
        var focusable = new AvonicItem { Focusable = true, Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { overlaid, unfocusable, focusable } });

        overlaid.Content = "y";
        Dispatcher.UIThread.RunJobs();

        Assert.False(overlaid.IsHitTestVisible);
        Assert.False(unfocusable.Focusable);
        Assert.True(focusable.Focusable);
    }

    // CSS: an app's rule beats the host's own, whenever the host's class turns on. A value given at style priority when the
    // state turns on would come after the app's style, and win.
    [AvaloniaFact]
    public void B27_An_Apps_Style_Survives_A_State_That_Turns_On_After_It()
    {
        var child = new Border();
        var item = new AvonicItem { Content = child };
        var window = new Window
        {
            Content = item,
            Styles =
            {
                new Style(x => x.OfType<AvonicItem>())
                {
                    Setters =
                    {
                        new Setter(InputElement.FocusableProperty, false),
                        new Setter(InputElement.IsHitTestVisibleProperty, true),
                    },
                },
            },
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        item.Button = true;
        Raise(child, ("interactive-disabled", true));
        Dispatcher.UIThread.RunJobs();

        Assert.False(item.Focusable);
        Assert.True(item.IsHitTestVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void B27_Input_Rules_Do_Not_Need_The_Theme()
    {
        // No template at all, so nothing here can come from a theme.
        var item = new AvonicItem { Button = true, Theme = new Avalonia.Styling.ControlTheme(typeof(AvonicItem)) };
        using var scene = new Scene(item);

        Assert.True(item.Focusable);

        Raise(item, ("interactive-disabled", true));
        Assert.False(item.IsHitTestVisible);

        Raise(item, ("interactive-disabled", false));
        Assert.True(item.IsHitTestVisible);

        item.Button = false;
        Assert.False(item.Focusable);
    }

    // ── Behaviours 28-33: layout ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B28_Default_Box() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = "x" };
        using var scene = new Scene(new StackPanel { Children = { item } });

        AssertBox(0, 0, 390, 48, Box(item, scene.Window));
        AssertBox(0, 0, 390, 48, Box(Native(item), scene.Window));
        AssertBox(16, 0, 374, 48, Box(Inner(item), scene.Window));
        AssertBox(16, 0, 358, 47, Box(Container(item), scene.Window));
    });

    [AvaloniaFact]
    public void B28_Start_Sits_Outside_The_Inner_Border_And_End_Inside() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var end = new AvonicIcon { IconName = "star" };
        var item = new AvonicItem { StartContent = start, EndContent = end };
        using var scene = new Scene(new StackPanel { Children = { item } });

        Assert.Equal(49, item.Bounds.Height);
        AssertBox(16, 12.5, 24, 24, Box(start, scene.Window));
        AssertBox(350, 12, 24, 24, Box(end, scene.Window));
        AssertBox(72, 0, 318, 49, Box(Inner(item), scene.Window));
        AssertBox(72, 0, 262, 48, Box(Container(item), scene.Window));
    });

    [AvaloniaFact]
    public void B28_Slots_Are_Vertically_Centred() => InstalledTheme.Run(_ =>
    {
        var start = new Border { Width = 10, Height = 18 };
        var item = new AvonicItem { StartContent = start, Content = new Border { Height = 100 } };
        using var scene = new Scene(new StackPanel { Children = { item } });

        Assert.Equal(101, item.Bounds.Height);
        AssertBox(16, 41.5, 10, 18, Box(start, scene.Window));
    });

    [AvaloniaFact]
    public void B28_Padding_Tokens_Apply() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = "x" };
        item.Resources["Avonic.Item.PaddingStart"] = 1d;
        item.Resources["Avonic.Item.PaddingTop"] = 2d;
        item.Resources["Avonic.Item.PaddingEnd"] = 3d;
        item.Resources["Avonic.Item.PaddingBottom"] = 4d;
        item.Resources["Avonic.Item.InnerPaddingStart"] = 5d;
        item.Resources["Avonic.Item.InnerPaddingTop"] = 6d;
        item.Resources["Avonic.Item.InnerPaddingEnd"] = 7d;
        item.Resources["Avonic.Item.InnerPaddingBottom"] = 8d;
        item.Resources["Avonic.Item.MinHeight"] = 80d;
        using var scene = new Scene(new StackPanel { Children = { item } });

        Assert.Equal(new Thickness(1, 2, 3, 4), Part<FlexRow>(item, "PART_Layout").Margin);
        Assert.Equal(new Thickness(5, 6, 7, 8), Inner(item).Padding);
        Assert.Equal(80, Inner(item).MinHeight);
        Assert.Equal(80, Native(item).MinHeight);
    });

    [AvaloniaFact]
    public void B28_Inner_Shadow_Token_Applies() => InstalledTheme.Run(_ =>
    {
        var shadow = BoxShadows.Parse("0 0 5 0 #FF0000");
        var item = new AvonicItem();
        item.Resources["Avonic.Item.InnerBoxShadow"] = shadow;
        using var scene = new Scene(item);

        Assert.Equal(shadow, Inner(item).BoxShadow);
    });

    [AvaloniaFact]
    public void B28_Colour_Tokens_Apply_And_Follow_Their_Fallbacks() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Content = "x" });
        using var scene = new Scene(new StackPanel { Children = { item } });

        Assert.Equal(Colors.White, Solid(Native(item).Background));
        Assert.Equal(Colors.Black, Solid(item.Foreground));

        item.Resources["Avonic.ItemBackgroundBrush"] = Brushes.Gray;
        item.Resources["Avonic.ItemColorBrush"] = Brushes.Blue;
        item.Resources["Avonic.ItemBorderColorBrush"] = Brushes.Green;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Gray, Solid(Native(item).Background));
        Assert.Equal(Colors.Blue, Solid(item.Foreground));
        Assert.Equal(Colors.Green, Solid(Inner(item).BorderBrush));

        item.Resources["Avonic.Item.Background"] = Brushes.Red;
        item.Resources["Avonic.Item.Color"] = Brushes.Lime;
        item.Resources["Avonic.Item.BorderColor"] = Brushes.Yellow;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Colors.Red, Solid(Native(item).Background));
        Assert.Equal(Colors.Lime, Solid(item.Foreground));
        Assert.Equal(Colors.Yellow, Solid(Native(item).BorderBrush));
        Assert.Equal(Colors.Yellow, Solid(Inner(item).BorderBrush));
    });

    [AvaloniaFact]
    public void B29_Long_Text_Wraps_And_Grows_The_Item() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Content = string.Concat(Enumerable.Repeat("wrapping text ", 20)) };
        using var scene = new Scene(new StackPanel { Children = { item } });

        Assert.True(item.Bounds.Height > 48);
        Assert.Equal(390, item.Bounds.Width);
    });

    [AvaloniaFact]
    public void B30_Font_And_Slotted_Icon_Sizes() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var inDefault = new AvonicIcon { IconName = "star" };
        var item = new AvonicItem { StartContent = start, Content = inDefault };
        var big = new AvonicIcon { IconName = "heart" };
        var bigItem = new AvonicItem { StartContent = big, FontSize = 20 };
        using var scene = new Scene(new StackPanel { Children = { item, bigItem } });

        Assert.Equal(16, item.FontSize);
        Assert.Equal(new FontFamily("Roboto, Helvetica Neue, sans-serif"), item.FontFamily);
        Assert.Equal(24, start.Bounds.Width);
        Assert.Equal(24, inDefault.Bounds.Width);
        Assert.Equal(30, big.Bounds.Width);
    });

    [AvaloniaFact]
    public void B30_Slotted_Icon_Colour() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicIcon { IconName = "heart" };
        var coloured = new AvonicIcon { IconName = "heart" };
        var own = new AvonicIcon { IconName = "heart", Color = "success" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItem { StartContent = plain },
                new AvonicItem { StartContent = coloured, Color = "danger" },
                new AvonicItem { StartContent = own },
            },
        });

        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(plain.Foreground);
        Assert.Equal(Colors.Black, brush.Color);
        Assert.Equal(0.54, brush.Opacity, 3);
        Assert.Equal(Colors.White, Solid(coloured.Foreground));

        // ion-icon's own colour still wins: 45, 213, 91 is the success base, against a 54% black icon.
        var frame = scene.Window.CaptureRenderedFrame()!;
        var box = Box(own, scene.Window);
        var pixel = Frames.PixelAt(frame, (int)box.Center.X, (int)box.Center.Y);
        Assert.True(pixel.G > pixel.R);
    });

    [AvaloniaFact]
    public void B31_Slot_Margins() => InstalledTheme.Run(_ =>
    {
        var startText = new Border { Width = 10, Height = 10 };
        var endText = new Border { Width = 10, Height = 10 };
        var startIcon = new AvonicIcon { IconName = "heart" };
        var endIcon = new AvonicIcon { IconName = "star" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItem { StartContent = startText, EndContent = endText },
                new AvonicItem { StartContent = startIcon, EndContent = endIcon },
            },
        });

        Assert.Equal(new Thickness(0, 0, 16, 0), startText.Margin);
        Assert.Equal(new Thickness(16, 0, 0, 0), endText.Margin);
        Assert.Equal(new Thickness(0, 12, 32, 12), startIcon.Margin);
        Assert.Equal(new Thickness(16, 12, 0, 12), endIcon.Margin);
    });

    [AvaloniaFact]
    public void B32_Slot_Rules_Are_For_Direct_Children_Only() => InstalledTheme.Run(_ =>
    {
        var nested = new AvonicIcon { IconName = "heart" };
        var inDefault = new AvonicIcon { IconName = "star" };
        using var scene = new Scene(new StackPanel
        {
            Children =
            {
                new AvonicItem { StartContent = new Border { Child = nested } },
                new AvonicItem { Content = inDefault },
            },
        });

        // Nested deeper: none of it, including the size.
        Assert.Equal(new Thickness(0), nested.Margin);
        Assert.NotEqual(24, nested.Bounds.Width);

        // In the default slot: size and colour, but ::slotted(ion-icon[slot]) doesn't match.
        Assert.Equal(new Thickness(0), inDefault.Margin);
        Assert.Equal(24, inDefault.Bounds.Width);
    });

    [AvaloniaFact]
    public void B33_Rtl_Mirrors_Padding_And_Slots() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicIcon { IconName = "heart" };
        var end = new Border { Width = 11, Height = 18 };
        var item = new AvonicItem
        {
            StartContent = start,
            EndContent = end,
            Detail = true,
            FlowDirection = FlowDirection.RightToLeft,
        };
        using var scene = new Scene(new StackPanel { Children = { item } });

        AssertBox(350, 12, 24, 24, Box(start, scene.Window));
        AssertBox(0, 0, 318, 48, Box(Inner(item), scene.Window));
        AssertBox(10, 14, 20, 20, Box(DetailIcon(item), scene.Window));
        AssertBox(38, 14.5, 11, 18, Box(end, scene.Window));
    });

    // ── Behaviour 34: rounded corners ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B34_Overlay_And_Ripple_Clip_To_The_Radius() => InstalledTheme.Run(_ =>
    {
        var item = NoTransitions(new AvonicItem { Button = true });
        item.Resources["Avonic.Item.BorderRadius"] = new CornerRadius(20);
        item.Resources["Avonic.Item.BackgroundHover"] = Brushes.Black;
        item.Resources["Avonic.Item.BackgroundHoverOpacity"] = 1d;
        using var scene = new Scene(item);
        scene.Window.Background = Brushes.White;

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);

        // The very corner is outside the radius, and the middle of the top edge is inside.
        Assert.Equal(Colors.Black, Pixel(scene, 200, 2));
        Assert.Equal(Colors.White, Pixel(scene, 1, 1));
    });

    // ── Behaviour 35: automation ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B35_Button_Item_Is_An_Invokable_Button()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, Content = "Open" };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        var peer = ControlAutomationPeer.CreatePeerForElement(item);

        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("Open", peer.GetName());
        Assert.True(peer.IsEnabled());
        Assert.NotNull(peer.GetProvider<IInvokeProvider>());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void B35_Plain_Item_Is_A_Group_That_Cannot_Be_Invoked()
    {
        var item = new AvonicItem { Content = "Row" };
        using var scene = new Scene(item);

        var peer = ControlAutomationPeer.CreatePeerForElement(item);

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Null(peer.GetProvider<IInvokeProvider>());

        item.Button = true;
        Assert.NotNull(peer.GetProvider<IInvokeProvider>());
    }

    [AvaloniaFact]
    public void B35_Interactive_Disabled_Reports_It_And_Invoking_Does_Nothing()
    {
        var clicks = 0;
        var child = new Border();
        var item = new AvonicItem { Button = true, Content = child };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);
        var peer = ControlAutomationPeer.CreatePeerForElement(item);
        Assert.True(peer.IsEnabled());

        Raise(child, ("interactive-disabled", true));

        Assert.False(peer.IsEnabled());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B35_Disabled_Reports_It_And_Invoking_Does_Nothing()
    {
        var clicks = 0;
        var item = new AvonicItem { Button = true, IsEnabled = false };
        item.Click += (_, _) => clicks++;
        using var scene = new Scene(item);

        var peer = ControlAutomationPeer.CreatePeerForElement(item);

        Assert.False(peer.IsEnabled());
        Assert.IsAssignableFrom<IInvokeProvider>(peer).Invoke();
        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void B35_The_Name_Comes_From_What_Is_Inside()
    {
        var control = new AvonicItem { Button = true, Content = new StackPanel { Children = { new TextBlock { Text = "Open" }, new TextBlock { Text = "file" } } } };
        var number = new AvonicItem { Button = true, Content = 42 };
        var label = new AvonicItem { Button = true, Content = "ignored" };
        Avalonia.Automation.AutomationProperties.SetName(label, "Named");
        using var scene = new Scene(new StackPanel { Children = { control, number, label } });

        Assert.Equal("Open file", ControlAutomationPeer.CreatePeerForElement(control).GetName());
        Assert.Equal("42", ControlAutomationPeer.CreatePeerForElement(number).GetName());
        Assert.Equal("Named", ControlAutomationPeer.CreatePeerForElement(label).GetName());
    }

    [AvaloniaFact]
    public void B35_The_Detail_Icon_Is_Hidden_From_Automation()
    {
        var item = new AvonicItem { Detail = true };
        using var scene = new Scene(item);

        Assert.Equal(
            Avalonia.Automation.AccessibilityView.Raw,
            Avalonia.Automation.AutomationProperties.GetAccessibilityView(DetailIcon(item)));
    }

    // ── Transition ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Transition_Fades_To_The_State_Value() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true };
        using var scene = new Scene(item);

        scene.Window.MouseMove(RowCentre, RawInputModifiers.None);
        var started = DateTime.UtcNow;
        while (Overlay(item).Opacity < 0.04 && DateTime.UtcNow - started < TimeSpan.FromSeconds(5))
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        Assert.Equal(0.04, Overlay(item).Opacity, 3);
        Assert.Equal(Colors.Black, Solid(Overlay(item).Background));
    });

    [AvaloniaFact]
    public void Transition_Token_Reaches_The_Overlay() => InstalledTheme.Run(_ =>
    {
        var item = new AvonicItem { Button = true };
        using var scene = new Scene(item);

        var transitions = Assert.IsAssignableFrom<Transitions>(Overlay(item).Transitions);
        var opacity = transitions.OfType<DoubleTransition>().Single();
        Assert.Equal(TimeSpan.FromMilliseconds(15), opacity.Duration);
        Assert.Same(transitions, Native(item).Transitions);
    });
}
