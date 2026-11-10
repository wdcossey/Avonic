using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Avonic.Components.Tests.Gestures;
using static Avonic.Components.Tests.Controls.AvonicTabBarTests;
using static Avonic.Components.Tests.Controls.AvonicTabsCaptureTests;
using static Avonic.Components.Tests.Controls.AvonicTabsTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// ion-tab-button's behaviours, from <c>tab-button.js</c> and <c>tab-button.md.css</c>, measured in Chromium: see
/// <c>docs/components/ion-tabs/PLAN.md</c>.
/// </summary>
public class AvonicTabButtonTests
{
    private static readonly Color Primary = Color.Parse("#0054E9");
    private static readonly Color Grey = Color.Parse("#595959");

    /// <summary>Layout rounds to half a pixel at a phone's scale, where Chromium keeps fractions.</summary>
    private static readonly RectComparer Half = new(0.25);

    private static T Part<T>(Visual control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static List<(string Tab, bool Selected)> Clicks(Control on)
    {
        var clicks = new List<(string, bool)>();
        on.AddHandler(AvonicTabButton.IonTabButtonClickEvent, (_, e) => clicks.Add((e.Tab, e.Selected)));
        return clicks;
    }

    private static T Child<T>(AvonicTabButton button) => button.Items.OfType<T>().Single();

    // ── The click ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void N1_A_Click_Raises_The_Internal_Click_With_Its_Tab_If_It_Has_One_And_Is_Enabled()
    {
        var a = Button("a", "A");
        a.Selected = true;
        var none = new AvonicTabButton { Items = { new AvonicLabel { Content = "No tab" } } };
        var off = Button("off", "Off");
        off.IsEnabled = false;
        var bar = Bar(null, a, none, off);
        var clicks = Clicks(bar);
        using var scene = new Scene(new StackPanel { Children = { bar } });

        Tap(scene, Centre(a, scene));
        Tap(scene, Centre(none, scene));
        Tap(scene, Centre(off, scene));

        Assert.Equal([("a", true)], clicks);
    }

    [AvaloniaFact]
    public void N2_It_Is_Not_Focusable_But_Enter_And_Space_Released_On_It_Click()
    {
        var a = Button("a", "A");
        var bar = Bar(null, a);
        var clicks = Clicks(bar);
        using var scene = new Scene(new StackPanel { Children = { bar } });

        // Measured: Tab never reached one.
        scene.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Settle();
        Assert.False(a.IsFocused);
        Assert.False(a.Focusable);

        a.Focusable = true;
        a.Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Empty(clicks);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        scene.Window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None);
        scene.Window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.A, RawInputModifiers.None);
        Settle();

        Assert.Equal([("a", false), ("a", false)], clicks);
    }

    // ── States ───────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void N3_N4_Disabled_Is_At_0_4_And_Selected_Has_The_Selected_Colour() => InstalledTheme.Run(_ =>
    {
        var a = Button("a", "A", "calendar");
        var b = Button("b", "B", "map");
        b.IsEnabled = false;
        var bar = Bar("a", a, b);
        using var scene = new Scene(new StackPanel { Children = { bar } });

        Assert.Equal(1, a.Opacity);
        Assert.Equal(0.4, b.Opacity);
        Assert.Equal(Primary, ColorOf(a.Foreground));
        Assert.Equal(Grey, ColorOf(b.Foreground));
        Assert.Equal(Primary, ColorOf(Child<AvonicIcon>(a).Foreground));

        bar.SelectedTab = "b";
        Assert.Equal(Grey, ColorOf(a.Foreground));
    });

    [AvaloniaFact]
    public void N5_N7_A_Label_Only_Wraps_An_Icon_Only_Is_24_And_Both_Are_One_Line_At_22() => InstalledTheme.Run(_ =>
    {
        var both = Button("a", "Both", "calendar");
        var labelOnly = Button("b", "Label only, long enough to wrap");
        var iconOnly = Button("c", null, "map");
        var deep = new AvonicTabButton { Tab = "d", Items = { new StackPanel { Children = { new AvonicIcon { IconName = "map" } } }, new AvonicLabel { Content = "Deep" } } };
        using var scene = new Scene(new StackPanel { Children = { Bar(null, both, labelOnly, iconOnly, deep) } });

        Assert.Equal(TextWrapping.NoWrap, Child<AvonicLabel>(both).GetValue(TextBlock.TextWrappingProperty));
        Assert.Equal(TextTrimming.CharacterEllipsis, Child<AvonicLabel>(both).GetValue(TextBlock.TextTrimmingProperty));
        Assert.Equal(22, Child<AvonicIcon>(both).Bounds.Height);
        Assert.Equal(12, Child<AvonicLabel>(both).FontSize);
        // letter-spacing: 0.03em, and the bar's text-align: center, which both inherit.
        Assert.Equal(0.36, Child<AvonicLabel>(both).GetValue(TextBlock.LetterSpacingProperty), 3);
        Assert.Equal(TextAlignment.Center, Child<AvonicLabel>(labelOnly).GetValue(TextBlock.TextAlignmentProperty));
        Assert.Equal(TextWrapping.Wrap, Child<AvonicLabel>(labelOnly).GetValue(TextBlock.TextWrappingProperty));
        Assert.True(Child<AvonicLabel>(labelOnly).Bounds.Height > 40);
        Assert.Equal(24, Child<AvonicIcon>(iconOnly).Bounds.Height);
        // hasIcon looks anywhere inside, so the label doesn't wrap.
        Assert.Equal(TextWrapping.NoWrap, Child<AvonicLabel>(deep).GetValue(TextBlock.TextWrappingProperty));
    });

    // Measured in Chromium, relative to each button, from the reference page's bars.
    [AvaloniaTheory]
    [InlineData(TabButtonLayout.IconTop, 9, 33, 71, 8)]
    [InlineData(TabButtonLayout.IconBottom, 24, 10, 86.2, 8)]
    [InlineData(TabButtonLayout.IconStart, 17, 21, 96.8, 16)]
    [InlineData(TabButtonLayout.IconEnd, 17, 21, 96.8, 16)]
    public void N6_N8_The_Layouts_Place_The_Icon_The_Label_And_The_Badge(TabButtonLayout layout, double iconY, double labelY, double badgeX, double badgeY) =>
        InstalledTheme.Run(_ =>
        {
            var button = Button("a", "Label", "calendar", "6", layout);
            var bar = Bar(null, button, Button("b", "B"), Button("c", "C"));
            using var scene = new Scene(new StackPanel { Children = { bar } });
            Phone(scene);

            var icon = Box(Child<AvonicIcon>(button), button);
            var label = Box(Child<AvonicLabel>(button), button);
            var badge = Box(Child<AvonicBadge>(button), button);
            Assert.Equal(iconY, icon.Y, 0.5);
            Assert.Equal(labelY, label.Y, 0.5);
            Assert.Equal(new Rect(badgeX, badgeY, 12, 13), badge, Half);
            switch (layout)
            {
                case TabButtonLayout.IconStart:
                    Assert.Equal(6, label.X - icon.Right, 1);
                    break;
                case TabButtonLayout.IconEnd:
                    Assert.Equal(6, icon.X - label.Right, 1);
                    break;
                default:
                    Assert.Equal(icon.Center.X, label.Center.X, 1);
                    break;
            }
        });

    [AvaloniaFact]
    public void N6_The_Icon_Goes_First_Whatever_Its_Place() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicTabButton { Tab = "a", Items = { new AvonicLabel { Content = "Label" }, new AvonicIcon { IconName = "map" } } };
        using var scene = new Scene(new StackPanel { Children = { Bar(null, button) } });

        // ::slotted(ion-icon) { order: -1 }
        Assert.True(Box(Child<AvonicIcon>(button), button).Bottom <= Box(Child<AvonicLabel>(button), button).Y);
    });

    [AvaloniaFact]
    public void N6_N8_Icon_Hide_And_Label_Hide_Hide_One_And_Move_The_Badge() => InstalledTheme.Run(_ =>
    {
        var iconHide = Button("a", "Icon hide", "calendar", "6", TabButtonLayout.IconHide);
        var labelHide = Button("b", "Label hide", "person-circle", "6", TabButtonLayout.LabelHide);
        var bar = Bar(null, iconHide, labelHide, Button("c", "C"), Button("d", "D"));
        using var scene = new Scene(new StackPanel { Children = { bar } });
        Phone(scene);

        // Measured, in buttons 97.5 wide: the label at y 21; the badge at x 63.45, or 70% of the inner box, and y 16.
        Assert.False(Child<AvonicIcon>(iconHide).IsVisible);
        Assert.Equal(21, Box(Child<AvonicLabel>(iconHide), iconHide).Y, 0.5);
        Assert.Equal(new Rect(63.45, 16, 12, 13), Box(Child<AvonicBadge>(iconHide), iconHide), Half);
        // An icon of 24 with no margins at y 16, and the badge at 50% + 6 and y 16.
        Assert.False(Child<AvonicLabel>(labelHide).IsVisible);
        Assert.Equal(new Rect(36.75, 16, 24, 24), Box(Child<AvonicIcon>(labelHide), labelHide), Half);
        Assert.Equal(new Rect(54.75, 16, 12, 13), Box(Child<AvonicBadge>(labelHide), labelHide), Half);
    });

    [AvaloniaFact]
    public void N8_An_Empty_Badge_Is_A_Dot_And_A_Badge_Mirrors_In_RTL() => InstalledTheme.Run(_ =>
    {
        var dot = Button("a", "Dot", "person-circle", "");
        var rtl = Button("a", "RTL", "calendar", "6");
        var rtlBar = Bar(null, rtl, Button("b", "Map", "map"));
        rtlBar.FlowDirection = FlowDirection.RightToLeft;
        using var scene = new Scene(new StackPanel { Children = { Bar(null, dot, Button("b", "B"), Button("c", "C")), rtlBar } });
        Phone(scene);

        // Measured: 8 by 8 at x 71, y 8 in a button 130 wide.
        var badge = Child<AvonicBadge>(dot);
        Assert.True(badge.IsVisible);
        Assert.Equal(new Rect(71, 8, 8, 8), Box(badge, dot), Half);
        // Measured: x 261 in the window, the first button being on the right, from 195.
        Assert.Equal(195, Box(rtl, scene.Window).X, 0.25);
        Assert.Equal(261, Box(Child<AvonicBadge>(rtl), scene.Window).X, 0.5);
    });

    [AvaloniaFact]
    public void N8_A_Badge_Is_8_Px_With_Its_Padding_And_Radius() => InstalledTheme.Run(_ =>
    {
        var button = Button("a", "A", "calendar", "6");
        using var scene = new Scene(new StackPanel { Children = { Bar(null, button) } });

        var badge = Child<AvonicBadge>(button);
        Assert.Equal(8, badge.FontSize);
        Assert.Equal(FontWeight.Normal, badge.FontWeight);
        Assert.Equal(new Thickness(2, 3, 2, 2), badge.Padding);
        Assert.Equal(new CornerRadius(8), badge.CornerRadius);
        Assert.Equal(12, badge.Bounds.Width);
    });

    // ── Interaction ──────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void N9_It_Is_Activatable_With_An_Unbounded_Ripple_In_The_Selected_Colour() => InstalledTheme.Run(_ =>
    {
        using var clock = new TestClock();
        var button = Button("a", "A", "calendar");
        using var scene = new Scene(new StackPanel { Children = { Bar(null, button) } });
        var ripple = Part<AvonicRippleEffect>(button, "PART_Ripple");

        Assert.Equal(RippleEffectType.Unbounded, ripple.Type);
        Assert.Equal(Primary, ColorOf(ripple.Foreground));
        Assert.True(Part<Border>(button, "PART_Native").ClipToBounds);

        var at = Centre(button, scene);
        GestureTests.Render();
        scene.Window.MouseMove(at, RawInputModifiers.None);
        scene.Window.MouseDown(at, MouseButton.Left, RawInputModifiers.None);
        clock.At(100);
        // tap-click's ion-activated, 100 ms into the press, as on any ion-activatable.
        Assert.Contains(Activatable.ActivatedPseudoClass, button.Classes);
        scene.Window.MouseUp(at, MouseButton.Left, RawInputModifiers.None);
    });

    [AvaloniaFact]
    public void N10_A_Mouse_Over_It_Gives_The_Selected_Colour() => InstalledTheme.Run(_ =>
    {
        var button = Button("a", "A", "calendar");
        using var scene = new Scene(new StackPanel { Children = { Bar(null, button), new Border { Height = 200 } } });
        Assert.Equal(Grey, ColorOf(button.Foreground));

        GestureTests.Render();
        scene.Window.MouseMove(Centre(button, scene), RawInputModifiers.None);
        Settle();
        Assert.Equal(Primary, ColorOf(button.Foreground));

        scene.Window.MouseMove(new Point(195, 150), RawInputModifiers.None);
        Settle();
        Assert.Equal(Grey, ColorOf(button.Foreground));
    });

    [AvaloniaFact]
    public void N11_Focused_It_Shows_The_Focused_Background_Over_It() => InstalledTheme.Run(_ =>
    {
        var before = new Button();
        var button = Button("a", "A", "calendar");
        button.Focusable = true;
        using var scene = new Scene(new StackPanel { Children = { before, Bar(null, button) } });
        var overlay = Part<Border>(button, "PART_Overlay");
        Assert.Equal(0, overlay.Opacity);

        before.Focus();
        scene.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        scene.Window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Settle();

        Assert.True(button.IsFocused);
        Assert.Equal(1, overlay.Opacity);
        Assert.Equal(Color.Parse("#E0E0E0"), ColorOf(overlay.Background));
        // color-focused is unset, so the colour is the host's.
        Assert.Equal(Grey, ColorOf(button.Foreground));
    });

    [AvaloniaFact]
    public void N12_It_Is_A_Tab_Item_Selected_When_Selected_And_Named_By_Its_Text()
    {
        var button = Button("a", "Radio", "radio");
        var bar = Bar(null, button);
        var clicks = Clicks(bar);
        using var scene = new Scene(new StackPanel { Children = { bar } });

        var peer = ControlAutomationPeer.CreatePeerForElement(button);
        Assert.Equal(AutomationControlType.TabItem, peer.GetAutomationControlType());
        Assert.Equal("Radio", peer.GetName());
        var item = Assert.IsAssignableFrom<ISelectionItemProvider>(peer);
        Assert.False(item.IsSelected);
        bar.SelectedTab = "a";
        Assert.True(item.IsSelected);

        item.Select();
        Assert.Equal([("a", true)], clicks);
    }

    private sealed class RectComparer(double tolerance) : IEqualityComparer<Rect>
    {
        public bool Equals(Rect a, Rect b) =>
            Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance &&
            Math.Abs(a.Width - b.Width) <= tolerance && Math.Abs(a.Height - b.Height) <= tolerance;

        public int GetHashCode(Rect rect) => 0;
    }
}
