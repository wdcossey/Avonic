using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avonic.Components.Behaviors;
using Avonic.Components.Controls;
using Avonic.Themes;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-avatar in Chromium, at 390 px wide. See docs/components/ion-badge/PLAN.md.
/// </summary>
public class AvonicAvatarTests
{
    internal static readonly Color Blue = Color.FromRgb(0, 84, 233);
    internal static readonly Color Yellow = Color.FromRgb(255, 196, 9);
    internal static readonly Color Green = Color.FromRgb(45, 213, 91);
    internal static readonly Color Red = Color.FromRgb(200, 0, 0);

    /// <summary>The picture in the reference render: 120 × 80, blue, with a yellow circle at the left and a green bar at the right.</summary>
    internal static Image Picture()
    {
        var group = new DrawingGroup
        {
            Children =
            {
                new GeometryDrawing { Brush = new SolidColorBrush(Blue), Geometry = new RectangleGeometry(new Rect(0, 0, 120, 80)) },
                new GeometryDrawing { Brush = new SolidColorBrush(Yellow), Geometry = new EllipseGeometry(new Rect(6, 16, 48, 48)) },
                new GeometryDrawing { Brush = new SolidColorBrush(Green), Geometry = new RectangleGeometry(new Rect(70, 10, 40, 60)) },
            },
        };
        return new Image { Source = new DrawingImage(group) };
    }

    internal static Rect Box(Visual visual, Visual root) => new(visual.TranslatePoint(default, root)!.Value, visual.Bounds.Size);

    internal static Scene InScene(params Control[] children)
    {
        var panel = new StackPanel();
        panel.Children.AddRange(children);
        var scene = new Scene(panel);
        scene.Window.Background = Brushes.White;
        return scene;
    }

    /// <summary>A pixel at a point in a control, at 1x.</summary>
    internal static Color Pixel(Scene scene, Visual visual, double x, double y)
    {
        Dispatcher.UIThread.RunJobs();
        var origin = visual.TranslatePoint(new Point(x, y), scene.Window)!.Value;
        return Frames.PixelAt(scene.Window.CaptureRenderedFrame()!, (int)origin.X, (int)origin.Y);
    }

    internal static void AssertNear(Color expected, Color actual)
    {
        Assert.True(Math.Abs(expected.R - actual.R) + Math.Abs(expected.G - actual.G) + Math.Abs(expected.B - actual.B) < 30, $"Expected {expected}, got {actual}.");
    }

    /// <summary>Layout rounds to whole pixels, so an odd gap can't be split exactly.</summary>
    internal static void AssertCentred(Visual visual, AvonicItem item) =>
        Assert.InRange(Box(visual, item).Center.Y - item.Bounds.Height / 2, -1, 1);

    internal static StackPanel Tall() => new()
    {
        Children =
        {
            new TextBlock { Text = "One", Height = 30 },
            new TextBlock { Text = "Two", Height = 30 },
            new TextBlock { Text = "Three", Height = 30 },
        },
    };

    /// <summary>
    /// Runs a test with one of the theme's Styles files merged before every other one, or after. Files merge in ordinal order, so
    /// a rule that beats Item.axaml's only by coming after it would hold in one control's file and not in another's.
    /// </summary>
    internal static void InMergeOrder(string file, bool first, Action test)
    {
        var app = Application.Current!;
        var theme = new AvonicTheme();
        var source = new Uri($"avares://Avonic.Themes/Styles/{file}");
        var include = theme.OfType<StyleInclude>().Single(include => include.Source == source);
        theme.Remove(include);
        if (first)
        {
            theme.Insert(theme.IndexOf(theme.OfType<StyleInclude>().First()), include);
        }
        else
        {
            theme.Add(include);
        }

        app.Styles.Add(theme);
        try
        {
            Dispatcher.UIThread.RunJobs();
            test();
        }
        finally
        {
            app.Styles.Remove(theme);
        }
    }

    /// <summary>
    /// The margins of a start and an end in an item and in a divider, and of an end beside a stacked and a floating label, in an
    /// item. Measured in Chromium, the same for an avatar and a thumbnail.
    /// </summary>
    internal static void AssertSlotMargins(Func<Control> create)
    {
        var start = create();
        var end = create();
        var dividerStart = create();
        var dividerEnd = create();
        var stacked = create();
        var floating = create();
        using var scene = InScene(
            new AvonicItem { StartContent = start, Content = "x" },
            new AvonicItem { Content = "x", EndContent = end },
            new AvonicItemDivider { StartContent = dividerStart, Content = "x", EndContent = dividerEnd },
            new AvonicItem { Content = new AvonicLabel { Content = "x", Position = LabelPosition.Stacked }, EndContent = stacked },
            new AvonicItem { Content = new AvonicLabel { Content = "x", Position = LabelPosition.Floating }, EndContent = floating });

        Assert.Equal(new Thickness(0, 8, 16, 8), start.Margin);
        Assert.Equal(new Thickness(16, 8, 0, 8), end.Margin);
        Assert.Equal(new Thickness(0, 8, 16, 8), dividerStart.Margin);
        Assert.Equal(new Thickness(16, 8, 0, 8), dividerEnd.Margin);
        Assert.Equal(new Thickness(16, 7, 0, 7), stacked.Margin);
        Assert.Equal(new Thickness(16, 7, 0, 7), floating.Margin);
    }

    /// <summary>
    /// Runs a test with an app's own plain style for <c>Margin</c> on every <typeparamref name="T"/>, after the theme, as an
    /// app's <c>Styles</c> are. In CSS a page's <c>ion-avatar { margin: 3px }</c> beats every <c>::slotted</c> rule, measured.
    /// </summary>
    internal static void WithAppMargin<T>(Thickness margin, Action test) where T : Control
    {
        var style = new Style(selector => selector.OfType<T>()) { Setters = { new Setter(Layoutable.MarginProperty, margin) } };
        var styles = Application.Current!.Styles;
        styles.Add(style);
        try
        {
            Dispatcher.UIThread.RunJobs();
            test();
        }
        finally
        {
            styles.Remove(style);
        }
    }

    /// <summary>The margin of a control in the start, end and default slots of an item, and the start and end of a divider.</summary>
    internal static void AssertMarginInEverySlot(Func<Control> create, Thickness expected)
    {
        var controls = Enumerable.Range(0, 5).Select(_ => create()).ToArray();
        using var scene = InScene(
            new AvonicItem { StartContent = controls[0], Content = "x", EndContent = controls[1] },
            new AvonicItem { Content = controls[2] },
            new AvonicItemDivider { StartContent = controls[3], Content = "x", EndContent = controls[4] });

        Assert.All(controls, control => Assert.Equal(expected, control.Margin));
    }

    // ── 1. Alone ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A1_Alone_It_Is_64_Square_At_The_Left_With_No_Margin() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(avatar);

        Assert.Equal(new Rect(0, 0, 64, 64), Box(avatar, scene.Window));
        Assert.Equal(default, avatar.Margin);
    });

    [AvaloniaFact]
    public void A1_It_Keeps_Its_Size_Whatever_Is_Around_It() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar();
        var parent = new StackPanel { Children = { avatar } };
        TextElement.SetFontSize(parent, 30);
        using var scene = new Scene(parent);

        Assert.Equal(new Size(64, 64), avatar.Bounds.Size);
    });

    // ── 2. An image in it ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A2_An_Image_Fills_It_Cropped_From_The_Middle_And_Cut_To_A_Circle() => InstalledTheme.Run(_ =>
    {
        var picture = Picture();
        var avatar = new AvonicAvatar { Content = picture };
        using var scene = InScene(avatar);

        Assert.Equal(new Size(64, 64), picture.Bounds.Size);

        // Cover: 96 × 64, with 16 cut from each side. The bar is at 60, the circle at 8, and the top is the picture's.
        AssertNear(Green, Pixel(scene, avatar, 60, 32));
        AssertNear(Yellow, Pixel(scene, avatar, 8, 32));
        AssertNear(Blue, Pixel(scene, avatar, 32, 2));

        // Outside the circle.
        AssertNear(Colors.White, Pixel(scene, avatar, 3, 3));
        AssertNear(Colors.White, Pixel(scene, avatar, 61, 61));
    });

    // ── 3. The radius ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A3_The_Radius_In_One_Avatars_Resources_Rounds_Its_Image() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        var other = new AvonicAvatar { Content = Picture() };
        avatar.Resources["Avonic.Avatar.BorderRadius"] = new CornerRadius(8);
        using var scene = InScene(avatar, other);

        // Measured 8 px on the host and the image. (3, 3) is inside a corner of 8, and outside a circle.
        AssertNear(Blue, Pixel(scene, avatar, 3, 3));
        AssertNear(Colors.White, Pixel(scene, avatar, 1, 1));
        AssertNear(Colors.White, Pixel(scene, other, 3, 3));
        Assert.Equal(new CornerRadius(8), avatar.CornerRadius);
    });

    [AvaloniaTheory]
    [InlineData("8")]
    [InlineData("8px")]
    [InlineData("8PX")]
    [InlineData(8.0)]
    public void A3_The_Radius_Can_Be_A_Number_Or_A_Length(object radius) => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(avatar);

        scene.Window.Resources["Avonic.Avatar.BorderRadius"] = radius;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new CornerRadius(8), avatar.CornerRadius);
        AssertNear(Blue, Pixel(scene, avatar, 3, 3));
    });

    [AvaloniaFact]
    public void A3_A_Percentage_Is_Of_The_Size() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(avatar);

        scene.Window.Resources["Avonic.Avatar.BorderRadius"] = "25%";
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new CornerRadius(16), avatar.CornerRadius);
        AssertNear(Blue, Pixel(scene, avatar, 6, 6));
        AssertNear(Colors.White, Pixel(scene, avatar, 2, 2));
    });

    [AvaloniaFact]
    public void A3_An_Invalid_Radius_Is_None() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(avatar);

        scene.Window.Resources["Avonic.Avatar.BorderRadius"] = "round";
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(default, avatar.CornerRadius);
        AssertNear(Blue, Pixel(scene, avatar, 1, 1));
    });

    [AvaloniaTheory]
    [InlineData("8px 0", 8, 0, 8, 0)]
    [InlineData("8 4 2", 8, 4, 2, 4)]
    [InlineData("1px 2px 3px 4px", 1, 2, 3, 4)]
    [InlineData("50% 0", 32, 0, 32, 0)]
    [InlineData("8px / 4px", 0, 0, 0, 0)]
    [InlineData("1 2 3 4 5", 0, 0, 0, 0)]
    [InlineData("Infinity", 0, 0, 0, 0)]
    [InlineData("-4px", 0, 0, 0, 0)]
    public void A3_A_String_Is_Read_As_CSS_Reads_Border_Radius(string radius, double topLeft, double topRight, double bottomRight, double bottomLeft) => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar();
        avatar.Resources["Avonic.Avatar.BorderRadius"] = radius;
        using var scene = InScene(avatar);

        Assert.Equal(new CornerRadius(topLeft, topRight, bottomRight, bottomLeft), avatar.CornerRadius);
    });

    [AvaloniaFact]
    public void A3_A_Radius_That_Is_Not_Finite_Is_None() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar();
        avatar.Resources["Avonic.Avatar.BorderRadius"] = double.PositiveInfinity;
        using var scene = InScene(avatar);

        Assert.Equal(default, avatar.CornerRadius);
    });

    // ── 4. A background ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A4_A_Background_Follows_The_Radius() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Background = new SolidColorBrush(Blue) };
        using var scene = InScene(avatar);

        // Measured rgb(0, 84, 233) at 50%.
        Assert.Equal(new CornerRadius(32), avatar.CornerRadius);
        AssertNear(Blue, Pixel(scene, avatar, 32, 32));
        AssertNear(Colors.White, Pixel(scene, avatar, 3, 3));
    });

    [AvaloniaFact]
    public void A4_On_A_Wide_Avatar_The_Background_Takes_The_Smaller_Radius() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Width = 80, Height = 40, Background = new SolidColorBrush(Blue) };
        using var scene = InScene(avatar);

        // CSS draws an ellipse of 40 by 20 (measured av-wide). One radius per corner can only be the smaller, a stadium.
        Assert.Equal(new CornerRadius(20), avatar.CornerRadius);
    });

    // ── 5. Other content ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A5_Content_That_Is_Not_An_Image_Is_Not_Cut() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = new Border { Background = new SolidColorBrush(Red) } };
        using var scene = InScene(avatar);

        AssertNear(Red, Pixel(scene, avatar, 2, 2));
    });

    [AvaloniaFact]
    public void A5_Content_Larger_Than_It_Is_Not_Cut() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = new Border { Background = new SolidColorBrush(Red), Width = 100, Height = 20 } };
        using var scene = InScene(avatar);

        // overflow: visible on the host, so what's wider shows beyond it.
        AssertNear(Red, Pixel(scene, avatar, 75, 32));
    });

    // ── 6. In an item ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A6_In_An_Item_It_Is_40_With_8_Above_And_Below_And_16_Towards_The_Content() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicAvatar { Content = Picture() };
        var end = new AvonicAvatar { Content = Picture() };
        var main = new AvonicAvatar { Content = Picture() };
        var itemStart = new AvonicItem { StartContent = start, Content = "x" };
        var itemEnd = new AvonicItem { Content = "x", EndContent = end };
        using var scene = InScene(itemStart, itemEnd, new AvonicItem { Content = main });

        // Measured margin: 8px 16px 8px 0px, 8px 0px 8px 16px and 8px 0px, and rows of 56, and 57 with the inner border under the end slot.
        Assert.Equal(new Thickness(0, 8, 16, 8), start.Margin);
        Assert.Equal(new Thickness(16, 8, 0, 8), end.Margin);
        Assert.Equal(new Thickness(0, 8, 0, 8), main.Margin);
        foreach (var avatar in new[] { start, end, main })
        {
            Assert.Equal(new Size(40, 40), avatar.Bounds.Size);
        }

        Assert.Equal(56, itemStart.Bounds.Height);
        Assert.Equal(57, itemEnd.Bounds.Height);
        Assert.Equal(16, Box(start, itemStart).X);
        Assert.Equal(334, Box(end, itemEnd).X);
    });

    [AvaloniaFact]
    public void A6_In_A_Divider_It_Is_The_Same() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicAvatar { Content = Picture() };
        var end = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(new AvonicItemDivider { StartContent = start, Content = "x", EndContent = end });

        Assert.Equal(new Thickness(0, 8, 16, 8), start.Margin);
        Assert.Equal(new Thickness(16, 8, 0, 8), end.Margin);
        Assert.Equal(new Size(40, 40), start.Bounds.Size);
    });

    [AvaloniaFact]
    public void A6_In_A_Panel_Directly_In_A_Slot_It_Is_40_With_8_Above_And_Below() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        var panel = new StackPanel { Children = { avatar } };
        using var scene = InScene(new AvonicItem { StartContent = panel, Content = "x" });

        Assert.Equal(new Size(40, 40), avatar.Bounds.Size);
        Assert.Equal(new Thickness(0, 8, 0, 8), avatar.Margin);
        Assert.Equal(new Thickness(0, 0, 16, 0), panel.Margin);
    });

    [AvaloniaFact]
    public void A6_The_Image_Is_Cut_To_The_Smaller_Circle() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(new AvonicItem { StartContent = avatar, Content = "x" });

        AssertNear(Blue, Pixel(scene, avatar, 20, 2));
        AssertNear(Colors.White, Pixel(scene, avatar, 3, 3));
    });

    // ── 7. Nested deeper ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A7_Nested_Deeper_In_A_Slot_It_Is_64_With_No_Margin() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(new AvonicItem { StartContent = new Border { Child = avatar }, Content = "x" });

        Assert.Equal(new Size(64, 64), avatar.Bounds.Size);
        Assert.Equal(default, avatar.Margin);
    });

    // ── 8. Beside a stacked label ────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void A8_Beside_A_Stacked_Or_Floating_Label_It_Is_7_Above_And_Below(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = "x", Position = position }, EndContent = avatar });

        // Measured margin: 7px 0px 7px 16px.
        Assert.Equal(new Thickness(16, 7, 0, 7), avatar.Margin);
        Assert.Equal(new Size(40, 40), avatar.Bounds.Size);
    });

    // In CSS, the slot rules for an avatar beat the item's by specificity, not by where they come. Here they're in Item.axaml and
    // ItemDivider.axaml, after the rule for any child. A plain rule back in Avatar.axaml would lose to that rule merged first,
    // and win merged last; one with an activator would beat an app's style, which A13 checks.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void A6_A8_The_Slot_Rules_Hold_Whatever_Order_The_Files_Merge_In(bool first) =>
        InMergeOrder("Avatar.axaml", first, () => AssertSlotMargins(() => new AvonicAvatar { Content = Picture() }));

    // ── 13. An app's own style ───────────────────────────────────────────────────────────────

    // Measured a-app: a page's margin: 3px on an avatar in an item's end slot is 3px all round, as a page's rule beats every
    // ::slotted one, so it's the same in every slot.
    [AvaloniaFact]
    public void A13_An_Apps_Plain_Margin_Style_Beats_The_Slots() => InstalledTheme.Run(_ =>
        WithAppMargin<AvonicAvatar>(new Thickness(3), () => AssertMarginInEverySlot(() => new AvonicAvatar { Content = Picture() }, new Thickness(3))));

    // ── 9. A tall row ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A9_It_Is_Centred_In_A_Tall_Row() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        var item = new AvonicItem { StartContent = avatar, Content = Tall() };
        using var scene = InScene(item);

        // Measured y = 24.09 in 88.19.
        AssertCentred(avatar, item);
        Assert.Equal(40, avatar.Bounds.Height);
    });

    // ── 10. Its own size ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A10_Its_Own_Width_And_Height_Beat_The_Slots_And_The_Margins_Stay() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture(), Width = 30, Height = 30 };
        using var scene = InScene(new AvonicItem { StartContent = avatar, Content = "x" });

        // Measured 30 × 30, margin: 8px 16px 8px 0px.
        Assert.Equal(new Size(30, 30), avatar.Bounds.Size);
        Assert.Equal(new Thickness(0, 8, 16, 8), avatar.Margin);
    });

    // ── 11. Right to left ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A11_In_RTL_A_Start_Avatar_Is_At_The_Right() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(new AvonicItem { StartContent = avatar, Content = "RTL", FlowDirection = FlowDirection.RightToLeft });

        // Measured at x = 334, 40 wide. A mirrored element's origin is its right-hand edge.
        Assert.Equal(334 + 40, Box(avatar, scene.Window).X, 1);
    });

    // CSS's corners are physical: border-radius doesn't follow the direction. The image's clip and the background have to agree.
    [AvaloniaFact]
    public void A11_In_RTL_The_Corners_Stay_Where_They_Are() => InstalledTheme.Run(_ =>
        AssertCornersStayInRtl(() => new AvonicAvatar(), "Avonic.Avatar.BorderRadius"));

    internal static void AssertCornersStayInRtl(Func<ContentControl> create, string key)
    {
        var picture = create();
        picture.Content = Picture();
        var plain = create();
        plain.Background = new SolidColorBrush(Red);
        foreach (var control in new[] { picture, plain })
        {
            control.Resources[key] = "20px 0 0 0";
        }

        using var scene = InScene(new StackPanel { FlowDirection = FlowDirection.RightToLeft, Children = { picture, plain } });
        Dispatcher.UIThread.RunJobs();
        var frame = scene.Window.CaptureRenderedFrame()!;

        foreach (var (control, colour) in new[] { (picture, Blue), (plain, Red) })
        {
            // The physical top left and top right, in the window, wherever the mirror puts the element's origin.
            var a = control.TranslatePoint(default, scene.Window)!.Value;
            var b = control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), scene.Window)!.Value;
            var left = (int)Math.Min(a.X, b.X);
            var right = (int)Math.Max(a.X, b.X);
            var top = (int)Math.Min(a.Y, b.Y);
            AssertNear(Colors.White, Frames.PixelAt(frame, left + 2, top + 2));
            AssertNear(colour, Frames.PixelAt(frame, right - 3, top + 2));
        }
    }

    // ── 12. The item's size ──────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A12_Its_Size_Does_Not_Follow_The_Items_Font_Size() => InstalledTheme.Run(_ =>
    {
        var avatar = new AvonicAvatar { Content = Picture() };
        using var scene = InScene(new AvonicItem { FontSize = 30, StartContent = avatar, Content = "x" });

        Assert.Equal(new Size(40, 40), avatar.Bounds.Size);
    });

    // ── The radius, read ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void BorderRadius_Scales_Corners_That_Would_Overlap_As_CSS_Does() => InstalledTheme.Run(_ =>
    {
        var radius = BorderRadius.ToCornerRadius.Convert([new CornerRadius(40, 40, 0, 0), new Rect(0, 0, 64, 64)], typeof(CornerRadius), null, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(new CornerRadius(32, 32, 0, 0), radius);
    });

    [AvaloniaFact]
    public void BorderRadius_A_Percentage_Of_A_Wide_Box_Clips_An_Ellipse() => InstalledTheme.Run(_ =>
    {
        var clip = BorderRadius.ToClip.Convert(["50%", new Rect(0, 0, 80, 40)], typeof(Geometry), null, System.Globalization.CultureInfo.InvariantCulture);

        var geometry = Assert.IsType<RectangleGeometry>(clip);
        Assert.Equal(40, geometry.RadiusX);
        Assert.Equal(20, geometry.RadiusY);
    });

    [AvaloniaFact]
    public void BorderRadius_Different_Corners_Clip_Each_Its_Own() => InstalledTheme.Run(_ =>
    {
        var clip = (Geometry)BorderRadius.ToClip.Convert([new CornerRadius(20, 0, 0, 0), new Rect(0, 0, 40, 40)], typeof(Geometry), null, System.Globalization.CultureInfo.InvariantCulture)!;

        Assert.False(clip.FillContains(new Point(2, 2)));
        Assert.True(clip.FillContains(new Point(38, 2)));
        Assert.True(clip.FillContains(new Point(2, 38)));
    });
}
