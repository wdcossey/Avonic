using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicAvatarTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// The numbers are from real ion-thumbnail in Chromium, at 390 px wide. See docs/components/ion-badge/PLAN.md.
/// </summary>
public class AvonicThumbnailTests
{
    // ── 1. Alone ─────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T1_Alone_It_Is_48_Square_At_The_Left_With_No_Margin() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = Picture() };
        using var scene = InScene(thumbnail);

        Assert.Equal(new Rect(0, 0, 48, 48), Box(thumbnail, scene.Window));
        Assert.Equal(default, thumbnail.Margin);
    });

    // ── 2. An image in it ────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T2_An_Image_Fills_It_Cropped_From_The_Middle_With_Square_Corners() => InstalledTheme.Run(_ =>
    {
        var picture = Picture();
        var thumbnail = new AvonicThumbnail { Content = picture };
        using var scene = InScene(thumbnail);

        Assert.Equal(new Size(48, 48), picture.Bounds.Size);

        // Cover: 72 × 48, with 12 cut from each side. The bar is at 45, the circle at 6, and the corners are the picture's.
        AssertNear(Green, Pixel(scene, thumbnail, 45, 24));
        AssertNear(Yellow, Pixel(scene, thumbnail, 6, 24));
        AssertNear(Blue, Pixel(scene, thumbnail, 1, 1));
        AssertNear(Blue, Pixel(scene, thumbnail, 46, 46));
    });

    [AvaloniaFact]
    public void T2_Content_Larger_Than_It_Is_Not_Cut() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = new Border { Background = new SolidColorBrush(Red), Width = 100, Height = 20 } };
        using var scene = InScene(thumbnail);

        // overflow: visible on the host.
        AssertNear(Red, Pixel(scene, thumbnail, 70, 24));
    });

    // ── 3. The tokens ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T3_The_Size_Sets_It_Alone_And_In_An_Item() => InstalledTheme.Run(_ =>
    {
        var alone = new AvonicThumbnail { Content = Picture() };
        var slotted = new AvonicThumbnail { Content = Picture() };
        using var scene = InScene(alone, new AvonicItem { StartContent = slotted, Content = "x" });

        scene.Window.Resources["Avonic.Thumbnail.Size"] = 64.0;
        Dispatcher.UIThread.RunJobs();

        // Measured 64 high alone, and a page rule beats the item's 56.
        Assert.Equal(new Size(64, 64), alone.Bounds.Size);
        Assert.Equal(new Size(64, 64), slotted.Bounds.Size);
    });

    [AvaloniaFact]
    public void T3_The_Size_In_One_Thumbnails_Resources_Beats_The_Slots() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = Picture() };
        thumbnail.Resources["Avonic.Thumbnail.Size"] = 30.0;
        using var scene = InScene(new AvonicItem { StartContent = thumbnail, Content = "x" });

        // Measured th-vars: 30 in an item.
        Assert.Equal(new Size(30, 30), thumbnail.Bounds.Size);
    });

    [AvaloniaFact]
    public void T3_The_Radius_Rounds_The_Image_And_50_Percent_Is_A_Circle() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = Picture() };
        thumbnail.Resources["Avonic.Thumbnail.BorderRadius"] = "50%";
        using var scene = InScene(thumbnail);

        // Measured border-radius: 50% on host and image.
        Assert.Equal(new CornerRadius(24), thumbnail.CornerRadius);
        AssertNear(Colors.White, Pixel(scene, thumbnail, 3, 3));
        AssertNear(Blue, Pixel(scene, thumbnail, 24, 2));
    });

    [AvaloniaFact]
    public void T3_A_Background_Follows_The_Radius() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Background = new SolidColorBrush(Blue) };
        thumbnail.Resources["Avonic.Thumbnail.BorderRadius"] = new CornerRadius(10);
        using var scene = InScene(thumbnail);

        AssertNear(Colors.White, Pixel(scene, thumbnail, 1, 1));
        AssertNear(Blue, Pixel(scene, thumbnail, 24, 24));
    });

    // ── 4. In an item ────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T4_In_An_Item_It_Is_56_With_8_Above_And_Below_And_16_Towards_The_Content() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicThumbnail { Content = Picture() };
        var end = new AvonicThumbnail { Content = Picture() };
        var main = new AvonicThumbnail { Content = Picture() };
        var itemStart = new AvonicItem { StartContent = start, Content = "x" };
        var itemEnd = new AvonicItem { Content = "x", EndContent = end };
        using var scene = InScene(itemStart, itemEnd, new AvonicItem { Content = main });

        // Measured: margin 8px 16px 8px 0px, 8px 0px 8px 16px and 8px 0px, and rows of 72, and 73 with the inner border under the end slot.
        Assert.Equal(new Thickness(0, 8, 16, 8), start.Margin);
        Assert.Equal(new Thickness(16, 8, 0, 8), end.Margin);
        Assert.Equal(new Thickness(0, 8, 0, 8), main.Margin);
        foreach (var thumbnail in new[] { start, end, main })
        {
            Assert.Equal(new Size(56, 56), thumbnail.Bounds.Size);
        }

        Assert.Equal(72, itemStart.Bounds.Height);
        Assert.Equal(73, itemEnd.Bounds.Height);
        Assert.Equal(318, Box(end, itemEnd).X);
    });

    [AvaloniaFact]
    public void T4_In_A_Divider_It_Is_The_Same() => InstalledTheme.Run(_ =>
    {
        var start = new AvonicThumbnail { Content = Picture() };
        var end = new AvonicThumbnail { Content = Picture() };
        using var scene = InScene(new AvonicItemDivider { StartContent = start, Content = "x", EndContent = end });

        Assert.Equal(new Thickness(0, 8, 16, 8), start.Margin);
        Assert.Equal(new Thickness(16, 8, 0, 8), end.Margin);
        Assert.Equal(new Size(56, 56), end.Bounds.Size);
    });

    [AvaloniaFact]
    public void T4_In_A_Panel_Directly_In_A_Slot_It_Is_56_With_8_Above_And_Below() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = Picture() };
        var panel = new StackPanel { Children = { thumbnail } };
        using var scene = InScene(new AvonicItem { Content = "x", EndContent = panel });

        Assert.Equal(new Size(56, 56), thumbnail.Bounds.Size);
        Assert.Equal(new Thickness(0, 8, 0, 8), thumbnail.Margin);
    });

    [AvaloniaFact]
    public void T4_Nested_Deeper_It_Is_48_With_No_Margin() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = Picture() };
        using var scene = InScene(new AvonicItem { StartContent = new Border { Child = thumbnail }, Content = "x" });

        Assert.Equal(new Size(48, 48), thumbnail.Bounds.Size);
        Assert.Equal(default, thumbnail.Margin);
    });

    [AvaloniaFact]
    public void T4_It_Is_Centred_In_A_Tall_Row() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = Picture() };
        var item = new AvonicItem { StartContent = thumbnail, Content = Tall() };
        using var scene = InScene(item);

        AssertCentred(thumbnail, item);
        Assert.Equal(56, thumbnail.Bounds.Height);
    });

    // ── 5. Beside a stacked label ────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(LabelPosition.Stacked)]
    [InlineData(LabelPosition.Floating)]
    public void T5_Beside_A_Stacked_Or_Floating_Label_It_Is_7_Above_And_Below(LabelPosition position) => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = Picture() };
        using var scene = InScene(new AvonicItem { Content = new AvonicLabel { Content = "x", Position = position }, EndContent = thumbnail });

        // Measured margin: 7px 0px 7px 16px.
        Assert.Equal(new Thickness(16, 7, 0, 7), thumbnail.Margin);
        Assert.Equal(new Size(56, 56), thumbnail.Bounds.Size);
    });

    // ── 6. Its own size ──────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T6_Its_Own_Width_And_Height_Beat_The_Slots() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = Picture(), Width = 30, Height = 30 };
        using var scene = InScene(new AvonicItem { StartContent = thumbnail, Content = "x" });

        Assert.Equal(new Size(30, 30), thumbnail.Bounds.Size);
        Assert.Equal(new Thickness(0, 8, 16, 8), thumbnail.Margin);
    });

    // ── 7. Right to left ─────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void T7_In_RTL_A_Start_Thumbnail_Is_At_The_Right() => InstalledTheme.Run(_ =>
    {
        var thumbnail = new AvonicThumbnail { Content = Picture() };
        using var scene = InScene(new AvonicItem { StartContent = thumbnail, Content = "RTL", FlowDirection = FlowDirection.RightToLeft });

        // 16 from the right. A mirrored element's origin is its right-hand edge.
        Assert.Equal(390 - 16, Box(thumbnail, scene.Window).X, 1);
    });
}
