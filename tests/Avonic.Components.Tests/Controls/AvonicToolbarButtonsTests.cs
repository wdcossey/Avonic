using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicToolbarCaptureTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// <c>ion-buttons</c>, and what it and a toolbar do to an <c>ion-button</c>. The numbers are from real Ionic in Chromium at
/// 390 px wide; see docs/components/ion-toolbar/PLAN.md.
/// </summary>
public class AvonicToolbarButtonsTests
{
    private static Border Native(AvonicButton button) => ToolbarTest.Part<Border>(button, "PART_Native");

    private static Border Overlay(AvonicButton button) => ToolbarTest.Part<Border>(button, "PART_Overlay");

    private static Thickness Padding(AvonicButton button) => ToolbarTest.Part<ButtonInner>(button, "PART_Inner").Margin;

    /// <summary>Transparent: a clear button has no background, and that's null here.</summary>
    private static void AssertNoBackground(Border native) => Assert.True(native.Background is null || ToolbarTest.Solid(native.Background).A == 0);

    private static Color Text(AvonicButton button) => ToolbarTest.Solid(TextElement.GetForeground(Native(button)));

    private static Scene InToolbar(string? color, params Control[] buttons) =>
        ToolbarTest.Show(new AvonicToolbar { Color = color, StartContent = Buttons(buttons) });

    // ── B1: fill resolves to clear ──────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_No_Fill_Is_Clear_In_Buttons_At_Any_Depth() => InstalledTheme.Run(_ =>
    {
        var direct = new AvonicButton { Content = "A" };
        var deeper = new AvonicButton { Content = "B" };
        using var scene = InToolbar(null, direct, new StackPanel { Children = { deeper } });

        AssertNoBackground(Native(direct));
        Assert.Equal(ToolbarTest.Toolbar, Text(direct));
        AssertNoBackground(Native(deeper));
    });

    [AvaloniaFact]
    public void B1_A_Fill_It_Is_Given_Wins_And_A_Button_Outside_Buttons_Stays_Solid() => InstalledTheme.Run(_ =>
    {
        var solid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        var loose = new AvonicButton { Content = "Loose" };
        using var scene = ToolbarTest.Show(new AvonicToolbar { StartContent = Buttons(solid), Content = loose });

        Assert.Equal(ToolbarTest.Toolbar, ToolbarTest.Solid(Native(solid).Background));

        // Measured #loose: primary, 36 high, margin 4 2, with its shadow.
        Assert.Equal(ToolbarTest.Primary, ToolbarTest.Solid(Native(loose).Background));
        Assert.Equal(36, loose.Bounds.Height);
        Assert.Equal(new Thickness(2, 4), loose.Margin);
        Assert.Equal(3, Native(loose).BoxShadow.Count);
    });

    [AvaloniaFact]
    public void B1_Moving_Out_Of_Buttons_Makes_It_Solid_Again() => InstalledTheme.Run(_ =>
    {
        var button = new AvonicButton { Content = "A" };
        var row = Buttons(button);
        var holder = new StackPanel();
        using var scene = ToolbarTest.Show(new StackPanel { Children = { new AvonicToolbar { StartContent = row }, holder } });
        AssertNoBackground(Native(button));

        row.Items.Remove(button);
        holder.Children.Add(button);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ToolbarTest.Primary, ToolbarTest.Solid(Native(button).Background));
    });

    // ── B2: size ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_32_High_Padded_8_And_3_Two_Apart_Two_Round_No_Shadow() => InstalledTheme.Run(_ =>
    {
        var edit = new AvonicButton { Content = "Edit" };
        var small = new AvonicButton { Content = "Small", Size = ButtonSize.Small };
        var solid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        using var scene = InToolbar(null, edit, small, solid);

        // Measured #edit: 51.25 × 32 at y 12, margin 0 2, padding 3px 8px, radius 2.
        Assert.Equal(32, edit.Bounds.Height);
        ToolbarTest.Near(12, ToolbarTest.Box(edit, scene.Window).Y);
        // The text's width differs with the font: Arial in Chromium, Segoe UI here.
        ToolbarTest.Near(51.25, edit.Bounds.Width, 3);
        Assert.Equal(new Thickness(2, 0), edit.Margin);
        Assert.Equal(new Thickness(8, 3), Padding(edit));
        Assert.Equal(new CornerRadius(2), Native(edit).CornerRadius);

        // The toolbar's rules beat a size's, as outer rules beat :host ones.
        Assert.Equal(32, small.Bounds.Height);
        Assert.Equal(new Thickness(8, 3), Padding(small));

        Assert.Equal(0, Native(solid).BoxShadow.Count);
    });

    [AvaloniaFact]
    public void B2_A_Pressed_Solid_Button_Has_No_Shadow_Either() => InstalledTheme.Run(_ =>
    {
        var solid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        using var scene = InToolbar(null, solid);

        using var clock = new TestClock();
        scene.Window.MouseDown(ToolbarTest.Box(solid, scene.Window).Center, MouseButton.Left, RawInputModifiers.None);
        clock.At(100);
        Assert.Contains(":activated", solid.Classes);
        Assert.Equal(0, Native(solid).BoxShadow.Count);
    });

    // ── B3: icon-only ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_Icon_Only_Clear_Is_A_48_Circle_With_An_Unbounded_Ripple() => InstalledTheme.Run(_ =>
    {
        var button = IconButton("menu");
        using var scene = InToolbar(null, button);

        // Measured #menu: 48 × 48, padding 12, radius 50%, margin 0, icon 25.2, ripple unbounded.
        Assert.Equal(new Size(48, 48), button.Bounds.Size);
        Assert.Equal(new Thickness(0), button.Margin);
        Assert.Equal(new Thickness(12), Padding(button));
        Assert.Equal(new CornerRadius(24), Native(button).CornerRadius);
        Assert.Equal(25.2, ((AvonicIcon)button.IconOnlyContent!).FontSize, 3);
        Assert.Equal(RippleEffectType.Unbounded, ToolbarTest.Part<AvonicRippleEffect>(button, "PART_Ripple").Type);
    });

    [AvaloniaFact]
    public void B3_Icon_Only_Solid_Is_32_High_Padded_8_And_0_With_A_Bounded_Ripple() => InstalledTheme.Run(_ =>
    {
        var button = IconButton("star");
        button.Fill = ButtonFill.Solid;
        using var scene = InToolbar(null, button);

        // Measured #ioo: 41.19 × 32, padding 0 8, icon 25.2.
        Assert.Equal(32, button.Bounds.Height);
        ToolbarTest.Near(41.19, button.Bounds.Width, 1);
        Assert.Equal(new Thickness(8, 0), Padding(button));
        Assert.Equal(new CornerRadius(2), Native(button).CornerRadius);
        Assert.Equal(25.2, ((AvonicIcon)button.IconOnlyContent!).FontSize, 3);
        Assert.Equal(RippleEffectType.Bounded, ToolbarTest.Part<AvonicRippleEffect>(button, "PART_Ripple").Type);
    });

    // ── B4: icons beside text ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_Start_And_End_Icons_Are_1_4_Em_With_0_3_And_0_4_Em_Beside_The_Text() => InstalledTheme.Run(_ =>
    {
        var startIcon = new AvonicIcon { IconName = "star" };
        var endIcon = new AvonicIcon { IconName = "star" };
        using var scene = InToolbar(
            null,
            new AvonicButton { Content = "Start", StartContent = startIcon },
            new AvonicButton { Content = "End", EndContent = endIcon });

        // Measured #ic1: 19.6, margin-inline-end 5.88; #ic2: margin-inline-start 7.84.
        Assert.Equal(19.6, startIcon.FontSize, 3);
        Assert.Equal(19.6, endIcon.FontSize, 3);
        Assert.Equal(0, startIcon.Margin.Left, 3);
        Assert.Equal(5.88, startIcon.Margin.Right, 3);
        Assert.Equal(7.84, endIcon.Margin.Left, 3);
        Assert.Equal(0, endIcon.Margin.Right, 3);
    });

    // ── B5: an uncoloured toolbar ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Uncoloured_Toolbar_Colours() => InstalledTheme.Run(_ =>
    {
        var solid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        var outline = new AvonicButton { Content = "Outline", Fill = ButtonFill.Outline };
        var clear = new AvonicButton { Content = "Clear" };
        var success = new AvonicButton { Content = "Clear", Color = "success" };
        using var scene = InToolbar(null, solid, outline, clear, success);

        // Measured #t4.
        Assert.Equal(ToolbarTest.Toolbar, ToolbarTest.Solid(Native(solid).Background));
        Assert.Equal(Colors.White, Text(solid));
        Assert.Equal(ToolbarTest.Toolbar, Text(outline));
        Assert.Equal(ToolbarTest.Toolbar, ToolbarTest.Solid(Native(outline).BorderBrush));
        Assert.Equal(new Thickness(2), Native(outline).BorderThickness);
        Assert.Equal(ToolbarTest.Toolbar, Text(clear));
        Assert.Equal(Color.Parse("#2DD55B"), Text(success));
    });

    [AvaloniaFact]
    public void B5_Clear_Follows_The_Toolbar_Text() => InstalledTheme.Run(_ =>
    {
        var clear = new AvonicButton { Content = "Clear" };
        var toolbar = new AvonicToolbar { StartContent = Buttons(clear), Resources = { ["Avonic.Toolbar.Color"] = Brushes.Purple } };
        using var scene = ToolbarTest.Show(toolbar);

        Assert.Equal(Colors.Purple, Text(clear));
    });

    // ── B6: a coloured toolbar ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_Coloured_Toolbar_Colours() => InstalledTheme.Run(_ =>
    {
        var solid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        var outline = new AvonicButton { Content = "Outline", Fill = ButtonFill.Outline };
        var clear = new AvonicButton { Content = "Clear" };
        using var scene = InToolbar("danger", solid, outline, clear);

        // Measured #t3.
        Assert.Equal(Colors.White, ToolbarTest.Solid(Native(solid).Background));
        Assert.Equal(ToolbarTest.Danger, Text(solid));
        Assert.Equal(Colors.White, Text(outline));
        Assert.Equal(Colors.White, ToolbarTest.Solid(Native(outline).BorderBrush));
        Assert.Equal(Colors.White, Text(clear));
    });

    // ── B7: focus and hover ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_Focus_Overlay_Is_The_Text_Colour_And_Shade_For_Solid_In_Colour() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicButton { Content = "Clear" };
        var colouredClear = new AvonicButton { Content = "Clear" };
        var colouredSolid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        var page = new StackPanel
        {
            Children =
            {
                new AvonicToolbar { StartContent = Buttons(plain) },
                new AvonicToolbar { Color = "primary", StartContent = Buttons(colouredClear, colouredSolid) },
            },
        };
        using var scene = new Scene(page);

        // Measured with Tab on s3: rgb(66,66,66) at 0.12, white at 0.12, rgb(0,74,205) at 0.24.
        ToolbarTest.PressKey(scene, PhysicalKey.Tab);
        Assert.Equal(ToolbarTest.Toolbar, ToolbarTest.Solid(Overlay(plain).Background));
        Assert.Equal(0.12, Overlay(plain).Opacity, 3);

        ToolbarTest.PressKey(scene, PhysicalKey.Tab);
        Assert.Equal(Colors.White, ToolbarTest.Solid(Overlay(colouredClear).Background));
        Assert.Equal(0.12, Overlay(colouredClear).Opacity, 3);

        ToolbarTest.PressKey(scene, PhysicalKey.Tab);
        Assert.Equal(Color.Parse("#004ACD"), ToolbarTest.Solid(Overlay(colouredSolid).Background));
        Assert.Equal(0.24, Overlay(colouredSolid).Opacity, 3);
    });

    [AvaloniaFact]
    public void B7_Hover_Overlay_Is_The_Text_Colour_And_Base_For_Solid_In_Colour() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicButton { Content = "Clear" };
        var colouredSolid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        var page = new StackPanel
        {
            Children =
            {
                new AvonicToolbar { StartContent = Buttons(plain) },
                new AvonicToolbar { Color = "primary", StartContent = Buttons(colouredSolid) },
            },
        };
        using var scene = new Scene(page);

        scene.Window.MouseMove(ToolbarTest.Box(plain, scene.Window).Center, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ToolbarTest.Toolbar, ToolbarTest.Solid(Overlay(plain).Background));

        scene.Window.MouseMove(ToolbarTest.Box(colouredSolid, scene.Window).Center, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(ToolbarTest.Primary, ToolbarTest.Solid(Overlay(colouredSolid).Background));
    });

    // ── B8: round ───────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_Round_Keeps_Its_Rounding_And_The_Toolbar_Padding() => InstalledTheme.Run(_ =>
    {
        var round = new AvonicButton { Content = "Round", Fill = ButtonFill.Solid, Shape = ButtonShape.Round };
        using var scene = InToolbar(null, round);

        // Measured #rnd: 999, 3px 8px.
        Assert.Equal(new CornerRadius(999), Native(round).CornerRadius);
        Assert.Equal(new Thickness(8, 3), Padding(round));
    });

    // ── B9: Avonic.ToolbarColorBrush ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_The_Toolbar_Colour_Token_Reaches_Buttons_In_An_Uncoloured_Toolbar() => InstalledTheme.Run(_ =>
    {
        var clear = new AvonicButton { Content = "Clear" };
        var solid = new AvonicButton { Content = "Solid", Fill = ButtonFill.Solid };
        var loose = new AvonicButton { Content = "Loose" };
        var own = new AvonicButton { Content = "Own", Color = "success" };
        var inColour = new AvonicButton { Content = "In colour" };
        var page = new StackPanel
        {
            Children =
            {
                new AvonicToolbar { StartContent = Buttons(clear, solid, own), Content = loose },
                new AvonicToolbar { Color = "danger", StartContent = Buttons(inColour) },
            },
            Resources = { ["Avonic.ToolbarColorBrush"] = Brushes.Red },
        };
        using var scene = new Scene(page);

        Assert.Equal(Colors.Red, Text(clear));
        Assert.Equal(Colors.Red, ToolbarTest.Solid(Native(solid).Background));
        Assert.Equal(Colors.White, Text(solid));
        Assert.Equal(Colors.Red, ToolbarTest.Solid(Native(loose).Background));
        Assert.Equal(Colors.White, Text(loose));
        Assert.Equal(Color.Parse("#2DD55B"), Text(own));
        Assert.Equal(Colors.White, Text(inColour));
    });

    [AvaloniaFact]
    public void B9_An_Outline_Border_Follows_The_Toolbar_Colour_Token() => InstalledTheme.Run(_ =>
    {
        var loose = new AvonicButton { Content = "Loose", Fill = ButtonFill.Outline };
        var inColour = new AvonicButton { Content = "In colour", Fill = ButtonFill.Outline };
        var page = new StackPanel
        {
            Children = { new AvonicToolbar { Content = loose }, new AvonicToolbar { Color = "danger", Content = inColour } },
            Resources = { ["Avonic.ToolbarColorBrush"] = Brushes.Red },
        };
        using var scene = new Scene(page);

        Assert.Equal(Colors.Red, Text(loose));
        Assert.Equal(Colors.Red, ToolbarTest.Solid(Native(loose).BorderBrush));

        // in-toolbar-color: its own primary, outside ion-buttons.
        Assert.Equal(ToolbarTest.Primary, ToolbarTest.Solid(Native(inColour).BorderBrush));
    });

    // ── B10: outline pressed ────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_A_Pressed_Outline_Button_Has_The_Toolbar_Background() => InstalledTheme.Run(_ =>
    {
        var outline = new AvonicButton { Content = "Outline", Fill = ButtonFill.Outline };
        var page = new StackPanel
        {
            Children = { new AvonicToolbar { Content = outline } },
            Resources = { ["Avonic.ToolbarBackgroundBrush"] = Brushes.Yellow },
        };
        using var scene = new Scene(page);
        using var clock = new TestClock();
        AssertNoBackground(Native(outline));

        // The theme eases the background over 15 ms, on real time here.
        Native(outline).Transitions = [];

        // tap-click activates after its delay.
        scene.Window.MouseDown(ToolbarTest.Box(outline, scene.Window).Center, MouseButton.Left, RawInputModifiers.None);
        clock.At(100);
        Assert.Equal(Colors.Yellow, ToolbarTest.Solid(Native(outline).Background));
    });
}
