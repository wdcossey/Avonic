using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-badge in Chromium, at 390 px wide. See docs/components/ion-badge/PLAN.md.
/// </summary>
public class AvonicBadgeTests
{
    private static readonly Color Primary = Color.FromRgb(0, 84, 233);
    private static readonly Color Danger = Color.FromRgb(197, 0, 15);
    private static readonly Color White = Color.FromRgb(255, 255, 255);
    private static readonly Color Parent = Color.FromRgb(1, 2, 3);
    private static readonly Color TokenBackground = Color.FromRgb(10, 20, 30);
    private static readonly Color TokenText = Color.FromRgb(40, 50, 60);

    private static TextBlock Text(AvonicBadge badge) => badge.GetVisualDescendants().OfType<TextBlock>().First();

    private static Color TextColor(Control control) =>
        Assert.IsAssignableFrom<ISolidColorBrush>(TextElement.GetForeground(control)).Color;

    private static Color? BackgroundColor(AvonicBadge badge) => (badge.Background as ISolidColorBrush)?.Color;

    private static Rect Box(Visual visual, Visual root) => new(visual.TranslatePoint(default, root)!.Value, visual.Bounds.Size);

    private static Scene InScene(params Control[] children)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(children);
        return new Scene(panel);
    }

    /// <summary>Layout rounds to whole pixels, so an odd gap can't be split exactly.</summary>
    private static void AssertCentred(Visual visual, AvonicItem item) =>
        Assert.InRange(Box(visual, item).Center.Y - item.Bounds.Height / 2, -1, 1);

    /// <summary>Three lines of text, so the row is much taller than a badge: measured 88 in Ionic.</summary>
    private static StackPanel Tall() => new()
    {
        Children =
        {
            new TextBlock { Text = "One", Height = 30 },
            new TextBlock { Text = "Two", Height = 30 },
            new TextBlock { Text = "Three", Height = 30 },
        },
    };

    // ── 1. Alone ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B1_Alone_It_Is_13_Bold_Primary_On_White_Whatever_Its_Parent() => InstalledTheme.Run(theme =>
    {
        var badge = new AvonicBadge { Content = "22" };
        var parent = new StackPanel { Children = { badge } };
        TextElement.SetForeground(parent, new SolidColorBrush(Parent));
        TextElement.SetFontSize(parent, 30);
        TextElement.SetFontFamily(parent, new FontFamily("serif"));
        using var scene = new Scene(parent);

        Assert.Equal(13, Text(badge).FontSize);
        Assert.Equal(FontWeight.Bold, Text(badge).FontWeight);
        Assert.True(theme.TryGetResource("Avonic.FontFamily", null, out var font));
        Assert.Equal(font, Text(badge).FontFamily);
        Assert.Equal(Primary, BackgroundColor(badge));
        Assert.Equal(White, TextColor(Text(badge)));
        Assert.Equal(new CornerRadius(4), badge.CornerRadius);
    });

    [AvaloniaFact]
    public void B1_It_Is_20_High_With_Its_Padding() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "22" };
        using var scene = InScene(badge);

        // Measured: padding 3px 4px 4px, and line-height: 1 of 13, whatever the font.
        Assert.Equal(new Thickness(4, 3, 4, 4), badge.Padding);
        Assert.Equal(20, badge.Bounds.Height);
    });

    [AvaloniaFact]
    public void B1_The_Text_Is_Not_Cut_And_Sits_In_The_Middle() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "22" };
        using var scene = InScene(badge);
        scene.Window.Background = Brushes.White;
        scene.Window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        // The rows with white ink in them, at 2x, inside the blue. A line kept at the top of a 13 px line sits low and is cut.
        var box = Box(badge, scene.Window);
        var height = (int)(box.Height * 2);
        var rows = new List<int>();
        for (var y = 0; y < height; y++)
        {
            for (var x = (int)(box.X * 2) + 4; x < (int)(box.Right * 2) - 4; x++)
            {
                var pixel = Frames.PixelAt(frame, x, (int)(box.Y * 2) + y);
                if (pixel.R > 200 && pixel.G > 200)
                {
                    rows.Add(y);
                    break;
                }
            }
        }

        Assert.NotEmpty(rows);
        var above = rows.Min();
        var below = height - 1 - rows.Max();
        Assert.InRange(above - below, -3, 3);
    });

    [AvaloniaFact]
    public void B1_In_A_Row_With_Something_Taller_It_Keeps_Its_Height() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "22" };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { badge, new Border { Width = 10, Height = 60 } } };
        using var scene = new Scene(row);

        // An inline-block on a line never stretches to it.
        Assert.Equal(20, badge.Bounds.Height);
    });

    // ── 2. Color ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Color_Paints_The_Base_Behind_The_Contrast_Alone_And_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var alone = new AvonicBadge { Content = "99+", Color = "danger" };
        var slotted = new AvonicBadge { Content = "3", Color = "danger" };
        using var scene = InScene(alone, new AvonicItem { StartContent = slotted });

        foreach (var badge in new[] { alone, slotted })
        {
            Assert.Equal(Danger, BackgroundColor(badge));
            Assert.Equal(White, TextColor(Text(badge)));
        }
    });

    [AvaloniaFact]
    public void B2_A_Change_Of_Color_Follows() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "x", Color = "danger" };
        using var scene = InScene(badge);

        badge.Color = "light";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Color.FromRgb(244, 245, 248), BackgroundColor(badge));
        Assert.Equal(Color.FromRgb(0, 0, 0), TextColor(Text(badge)));

        badge.Color = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Primary, BackgroundColor(badge));
        Assert.Equal(White, TextColor(Text(badge)));
    });

    // ── 3. An unknown colour ─────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_An_Unknown_Color_Has_No_Background_And_Inherits_The_Text() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "Bad", Color = "nonexistent" };
        var parent = new StackPanel { Children = { badge } };
        TextElement.SetForeground(parent, new SolidColorBrush(Parent));
        using var scene = new Scene(parent);

        Assert.Null(badge.Background);
        Assert.Equal(Parent, TextColor(Text(badge)));
    });

    [AvaloniaFact]
    public void B3_An_Unknown_Color_In_A_Coloured_Item_Shows_The_Items() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "Bad", Color = "nonexistent" };
        using var scene = InScene(new AvonicItem { Color = "primary", Content = "x", EndContent = badge });

        Assert.Equal(Primary, BackgroundColor(badge));
        Assert.Equal(White, TextColor(Text(badge)));
    });

    // ── 4. The tokens ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B4_The_Tokens_Set_The_Background_The_Text_And_The_Padding() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "Vars" };
        using var scene = InScene(badge);

        scene.Window.Resources["Avonic.Badge.Background"] = new SolidColorBrush(TokenBackground);
        scene.Window.Resources["Avonic.Badge.Color"] = new SolidColorBrush(TokenText);
        scene.Window.Resources["Avonic.Badge.PaddingTop"] = 1.0;
        scene.Window.Resources["Avonic.Badge.PaddingEnd"] = 2.0;
        scene.Window.Resources["Avonic.Badge.PaddingBottom"] = 3.0;
        scene.Window.Resources["Avonic.Badge.PaddingStart"] = 4.0;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(TokenBackground, BackgroundColor(badge));
        Assert.Equal(TokenText, TextColor(Text(badge)));
        Assert.Equal(new Thickness(4, 1, 2, 3), badge.Padding);

        // Measured 17 high.
        Assert.Equal(17, badge.Bounds.Height);
    });

    [AvaloniaFact]
    public void B4_The_Primary_Colour_Sets_The_Default() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "x" };
        using var scene = InScene(badge);

        scene.Window.Resources["Avonic.Color.PrimaryBrush"] = new SolidColorBrush(TokenBackground);
        scene.Window.Resources["Avonic.Color.PrimaryContrastBrush"] = new SolidColorBrush(TokenText);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(TokenBackground, BackgroundColor(badge));
        Assert.Equal(TokenText, TextColor(Text(badge)));
    });

    [AvaloniaFact]
    public void B4_Color_Beats_The_Tokens() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "x", Color = "danger" };
        using var scene = InScene(badge);

        scene.Window.Resources["Avonic.Badge.Background"] = new SolidColorBrush(TokenBackground);
        scene.Window.Resources["Avonic.Badge.Color"] = new SolidColorBrush(TokenText);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Danger, BackgroundColor(badge));
        Assert.Equal(White, TextColor(Text(badge)));
    });

    // ── 5. Empty ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B5_Empty_It_Takes_No_Space() => InstalledTheme.Run(_ =>
    {
        var before = new AvonicBadge { Content = "1" };
        var empty = new AvonicBadge();
        var blank = new AvonicBadge { Content = "" };
        var after = new TextBlock { Text = "after" };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { before, empty, blank, after } };
        using var scene = new Scene(row);

        Assert.Equal(default, empty.Bounds.Size);
        Assert.Equal(default, blank.Bounds.Size);
        Assert.Equal(before.Bounds.Right, after.Bounds.Left);
    });

    [AvaloniaFact]
    public void B5_It_Shows_Again_With_Content_And_Hides_Without() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge();
        using var scene = InScene(badge);

        badge.Content = "3";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(20, badge.Bounds.Height);

        badge.Content = "";
        Dispatcher.UIThread.RunJobs();
        Assert.False(badge.IsEffectivelyVisible);
    });

    [AvaloniaFact]
    public void B5_Empty_In_A_Slot_It_Leaves_No_Margin() => InstalledTheme.Run(_ =>
    {
        var label = new AvonicLabel { Content = "x" };
        var withEmpty = new AvonicLabel { Content = "x" };
        using var scene = InScene(
            new AvonicItem { Content = label },
            new AvonicItem { Content = withEmpty, EndContent = new AvonicBadge() });

        Assert.Equal(label.Bounds.Width, withEmpty.Bounds.Width);
    });

    // ── 6. No wrapping ───────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B6_It_Never_Wraps() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "A fairly long badge text that would wrap if it could, but it never does, whatever happens" };
        using var scene = InScene(badge);

        Assert.Equal(20, badge.Bounds.Height);
    });

    // ── 7. Width ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B7_It_Is_Its_Text_Plus_8_And_At_Least_10() => InstalledTheme.Run(_ =>
    {
        var one = new AvonicBadge { Content = "1" };
        // Without padding, as nothing in Ionic is narrower than 10 with it: measured "i" at 11.63.
        var i = new AvonicBadge { Content = "i" };
        foreach (var side in new[] { "Top", "End", "Bottom", "Start" })
        {
            i.Resources["Avonic.Badge.Padding" + side] = 0.0;
        }

        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { one, i } };
        using var scene = new Scene(row);

        Assert.Equal(Text(one).Bounds.Width + 8, one.Bounds.Width, 1);
        Assert.True(Text(i).Bounds.Width < 10, "The test needs a letter narrower than 10.");
        Assert.Equal(10, i.Bounds.Width);
    });

    // ── 8. In an item ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B8_In_The_Slots_It_Has_The_Slots_Margin_And_None_In_The_Default() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicBadge { Content = "3" };
        var end = new AvonicBadge { Content = "22" };
        var main = new AvonicBadge { Content = "Default slot" };
        var itemMain = new AvonicItem { Content = main };
        using var scene = InScene(new AvonicItem { StartContent = start, Content = "x", EndContent = end }, itemMain);

        Assert.Equal(new Thickness(0, 0, 16, 0), start.Margin);
        Assert.Equal(new Thickness(16, 0, 0, 0), end.Margin);
        Assert.Equal(default, main.Margin);

        // Measured at x = 16, as wide as its text.
        Assert.Equal(16, Box(main, itemMain).X);
        Assert.Equal(Text(main).Bounds.Width + 8, main.Bounds.Width, 1);
    });

    [AvaloniaFact]
    public void B8_It_Is_Centred_In_A_Plain_Row_And_A_Tall_One() => InstalledTheme.Run(_ =>
    {
        var plain = new AvonicBadge { Content = "22" };
        var tall = new AvonicBadge { Content = "7" };
        var itemPlain = new AvonicItem { Content = "x", EndContent = plain };
        var itemTall = new AvonicItem { Content = Tall(), EndContent = tall };
        using var scene = InScene(itemPlain, itemTall);

        // Measured y = 13.5 in 48, and 33.59 in 88.19, at its own height.
        Assert.Equal(48, itemPlain.Bounds.Height);
        AssertCentred(plain, itemPlain);
        AssertCentred(tall, itemTall);
        Assert.Equal(20, tall.Bounds.Height);
    });

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void B8_Beside_A_Stacked_Or_Floating_Label_It_Is_7_Above_And_Below(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "7" };
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = "x", Position = position }, EndContent = badge });

        Assert.Equal(new Thickness(16, 7, 0, 7), badge.Margin);
    });

    [AvaloniaFact]
    public void B8_Its_Size_Does_Not_Follow_The_Items() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "7" };
        using var scene = InScene(new AvonicItem { FontSize = 30, Content = "x", EndContent = badge });

        Assert.Equal(13, Text(badge).FontSize);
        Assert.Equal(20, badge.Bounds.Height);
    });

    [AvaloniaFact]
    public void B8_In_A_Divider_It_Has_The_Slots_Margin() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "7" };
        using var scene = InScene(new AvonicItemDivider { Content = "x", EndContent = badge });

        Assert.Equal(new Thickness(16, 0, 0, 0), badge.Margin);
    });

    // ── 9. A coloured item ───────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B9_A_Coloured_Item_Does_Not_Change_A_Badge() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "Stays" };
        using var scene = InScene(new AvonicItem { Color = "danger", Content = "x", EndContent = badge });

        Assert.Equal(Primary, BackgroundColor(badge));
        Assert.Equal(White, TextColor(Text(badge)));
    });

    // ── 10. Right to left ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B10_In_RTL_An_End_Badge_Is_At_The_Left() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "7" };
        using var scene = InScene(new AvonicItem { Content = "RTL", EndContent = badge, FlowDirection = FlowDirection.RightToLeft });

        // Measured at x = 16. A mirrored element's origin is its right-hand edge.
        Assert.Equal(16, Box(badge, scene.Window).X - badge.Bounds.Width, 1);
    });

    // ── 11. Centred text ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B11_Its_Text_Is_Centred_In_A_Wider_Badge() => InstalledTheme.Run(_ =>
    {
        var badge = new AvonicBadge { Content = "1", Width = 100 };
        using var scene = InScene(badge);

        Assert.Equal(50, Box(Text(badge), badge).Center.X, 1);
    });
}
