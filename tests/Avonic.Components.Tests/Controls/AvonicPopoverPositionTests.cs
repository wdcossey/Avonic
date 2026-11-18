using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avonic.Components.Controls;
using static Avonic.Components.Tests.Controls.AvonicPopoverTests;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// B4 to B8 in docs/components/ion-popover/PLAN.md: where <c>md.enter.js</c> puts the content, against Chromium's 108 cases and the
/// rest in <c>ref/measurements.txt</c> (M5), on a 390 by 844 window with 120 px of content, 250 wide. The places are a 100 by 40
/// trigger's: <c>tl</c> 10, 10; <c>tr</c> 280, 10; <c>c</c> 145, 400; <c>bl</c> 10, 794; <c>br</c> 280, 794; <c>mid</c> 145, 200.
/// The rows were generated from the measurements, not typed.
/// </summary>
public class AvonicPopoverPositionTests
{
    private static readonly Dictionary<string, Point> Places = new()
    {
        ["tl"] = new(10, 10), ["tr"] = new(280, 10), ["c"] = new(145, 400), ["bl"] = new(10, 794), ["br"] = new(280, 794), ["mid"] = new(145, 200),
    };

    /// <summary>
    /// Presents <paramref name="popover"/> by <paramref name="ev"/> (in the window, with none), and reads the content's box and its
    /// origin, left to right. Not animated, it still takes the wrapper's own 150 ms, as Ionic's <c>duration(0)</c> is the parent's.
    /// </summary>
    private static (Rect Box, double OriginX, double OriginY) Place(Stage stage, AvonicPopover popover, PopoverEvent? ev)
    {
        popover.Animated = false;
        _ = ev is null ? popover.Present(stage.Window) : popover.Present(ev);
        stage.After(Entered);
        var box = Box(popover, stage.Window);
        var origin = popover.ContentPart!.RenderTransformOrigin.Point;
        var mirrored = popover.Wrapper!.Place?.Mirrored == true;
        return (box, mirrored ? 1 - origin.X : origin.X, origin.Y);
    }

    /// <summary>The box, to the pixel: Avalonia rounds layout to whole pixels at a scale of 1, where Chromium at 2x keeps 759.59.</summary>
    private static void AssertBox(double x, double y, double width, double height, Rect box)
    {
        Assert.Equal(x, box.X, 0.5);
        Assert.Equal(y, box.Y, 0.5);
        Assert.Equal(width, box.Width, 0.5);
        Assert.Equal(height, box.Height, 0.5);
    }

    [AvaloniaTheory]
    [InlineData("tl", PopoverSide.Bottom, PopoverAlignment.Start, 12, 50, 120, 0, 0)]
    [InlineData("tl", PopoverSide.Bottom, PopoverAlignment.Center, 12, 50, 120, 0, 0)]
    [InlineData("tl", PopoverSide.Bottom, PopoverAlignment.End, 12, 50, 120, 0, 0)]
    [InlineData("tl", PopoverSide.Top, PopoverAlignment.Start, 12, -110, 120, 0, 1)]
    [InlineData("tl", PopoverSide.Top, PopoverAlignment.Center, 12, -110, 120, 0, 1)]
    [InlineData("tl", PopoverSide.Top, PopoverAlignment.End, 12, -110, 120, 0, 1)]
    [InlineData("tl", PopoverSide.Left, PopoverAlignment.Start, 12, 10, 120, 0, 0)]
    [InlineData("tl", PopoverSide.Left, PopoverAlignment.Center, 12, -30, 120, 0, 0.5)]
    [InlineData("tl", PopoverSide.Left, PopoverAlignment.End, 12, -70, 120, 0, 1)]
    [InlineData("tl", PopoverSide.Right, PopoverAlignment.Start, 110, 10, 120, 0, 0)]
    [InlineData("tl", PopoverSide.Right, PopoverAlignment.Center, 110, -30, 120, 0, 0.5)]
    [InlineData("tl", PopoverSide.Right, PopoverAlignment.End, 110, -70, 120, 0, 1)]
    [InlineData("tl", PopoverSide.Start, PopoverAlignment.Start, 12, 10, 120, 0, 0)]
    [InlineData("tl", PopoverSide.Start, PopoverAlignment.Center, 12, -30, 120, 0, 0.5)]
    [InlineData("tl", PopoverSide.Start, PopoverAlignment.End, 12, -70, 120, 0, 1)]
    [InlineData("tl", PopoverSide.End, PopoverAlignment.Start, 110, 10, 120, 0, 0)]
    [InlineData("tl", PopoverSide.End, PopoverAlignment.Center, 110, -30, 120, 0, 0.5)]
    [InlineData("tl", PopoverSide.End, PopoverAlignment.End, 110, -70, 120, 0, 1)]
    [InlineData("tr", PopoverSide.Bottom, PopoverAlignment.Start, 128, 50, 120, 1, 0)]
    [InlineData("tr", PopoverSide.Bottom, PopoverAlignment.Center, 128, 50, 120, 1, 0)]
    [InlineData("tr", PopoverSide.Bottom, PopoverAlignment.End, 128, 50, 120, 1, 0)]
    [InlineData("tr", PopoverSide.Top, PopoverAlignment.Start, 128, -110, 120, 1, 1)]
    [InlineData("tr", PopoverSide.Top, PopoverAlignment.Center, 128, -110, 120, 1, 1)]
    [InlineData("tr", PopoverSide.Top, PopoverAlignment.End, 128, -110, 120, 1, 1)]
    [InlineData("tr", PopoverSide.Left, PopoverAlignment.Start, 30, 10, 120, 1, 0)]
    [InlineData("tr", PopoverSide.Left, PopoverAlignment.Center, 30, -30, 120, 1, 0.5)]
    [InlineData("tr", PopoverSide.Left, PopoverAlignment.End, 30, -70, 120, 1, 1)]
    [InlineData("tr", PopoverSide.Right, PopoverAlignment.Start, 128, 10, 120, 1, 0)]
    [InlineData("tr", PopoverSide.Right, PopoverAlignment.Center, 128, -30, 120, 1, 0.5)]
    [InlineData("tr", PopoverSide.Right, PopoverAlignment.End, 128, -70, 120, 1, 1)]
    [InlineData("tr", PopoverSide.Start, PopoverAlignment.Start, 30, 10, 120, 1, 0)]
    [InlineData("tr", PopoverSide.Start, PopoverAlignment.Center, 30, -30, 120, 1, 0.5)]
    [InlineData("tr", PopoverSide.Start, PopoverAlignment.End, 30, -70, 120, 1, 1)]
    [InlineData("tr", PopoverSide.End, PopoverAlignment.Start, 128, 10, 120, 1, 0)]
    [InlineData("tr", PopoverSide.End, PopoverAlignment.Center, 128, -30, 120, 1, 0.5)]
    [InlineData("tr", PopoverSide.End, PopoverAlignment.End, 128, -70, 120, 1, 1)]
    [InlineData("c", PopoverSide.Bottom, PopoverAlignment.Start, 128, 440, 120, 1, 0)]
    [InlineData("c", PopoverSide.Bottom, PopoverAlignment.Center, 70, 440, 120, 0.5, 0)]
    [InlineData("c", PopoverSide.Bottom, PopoverAlignment.End, 12, 440, 120, 0, 0)]
    [InlineData("c", PopoverSide.Top, PopoverAlignment.Start, 128, 280, 120, 1, 1)]
    [InlineData("c", PopoverSide.Top, PopoverAlignment.Center, 70, 280, 120, 0.5, 1)]
    [InlineData("c", PopoverSide.Top, PopoverAlignment.End, 12, 280, 120, 0, 1)]
    [InlineData("c", PopoverSide.Left, PopoverAlignment.Start, 12, 400, 120, 0, 0)]
    [InlineData("c", PopoverSide.Left, PopoverAlignment.Center, 12, 360, 120, 0, 0.5)]
    [InlineData("c", PopoverSide.Left, PopoverAlignment.End, 12, 320, 120, 0, 1)]
    [InlineData("c", PopoverSide.Right, PopoverAlignment.Start, 128, 400, 120, 1, 0)]
    [InlineData("c", PopoverSide.Right, PopoverAlignment.Center, 128, 360, 120, 1, 0.5)]
    [InlineData("c", PopoverSide.Right, PopoverAlignment.End, 128, 320, 120, 1, 1)]
    [InlineData("c", PopoverSide.Start, PopoverAlignment.Start, 12, 400, 120, 0, 0)]
    [InlineData("c", PopoverSide.Start, PopoverAlignment.Center, 12, 360, 120, 0, 0.5)]
    [InlineData("c", PopoverSide.Start, PopoverAlignment.End, 12, 320, 120, 0, 1)]
    [InlineData("c", PopoverSide.End, PopoverAlignment.Start, 128, 400, 120, 1, 0)]
    [InlineData("c", PopoverSide.End, PopoverAlignment.Center, 128, 360, 120, 1, 0.5)]
    [InlineData("c", PopoverSide.End, PopoverAlignment.End, 128, 320, 120, 1, 1)]
    [InlineData("bl", PopoverSide.Bottom, PopoverAlignment.Start, 12, 675, 120, 0, 1)]
    [InlineData("bl", PopoverSide.Bottom, PopoverAlignment.Center, 12, 675, 120, 0, 1)]
    [InlineData("bl", PopoverSide.Bottom, PopoverAlignment.End, 12, 675, 120, 0, 1)]
    [InlineData("bl", PopoverSide.Top, PopoverAlignment.Start, 12, 675, 120, 0, 1)]
    [InlineData("bl", PopoverSide.Top, PopoverAlignment.Center, 12, 675, 120, 0, 1)]
    [InlineData("bl", PopoverSide.Top, PopoverAlignment.End, 12, 675, 120, 0, 1)]
    [InlineData("bl", PopoverSide.Left, PopoverAlignment.Start, 12, 794, 120, 0, 0)]
    [InlineData("bl", PopoverSide.Left, PopoverAlignment.Center, 12, 754, 120, 0, 0.5)]
    [InlineData("bl", PopoverSide.Left, PopoverAlignment.End, 12, 714, 120, 0, 1)]
    [InlineData("bl", PopoverSide.Right, PopoverAlignment.Start, 110, 794, 120, 0, 0)]
    [InlineData("bl", PopoverSide.Right, PopoverAlignment.Center, 110, 754, 120, 0, 0.5)]
    [InlineData("bl", PopoverSide.Right, PopoverAlignment.End, 110, 714, 120, 0, 1)]
    [InlineData("bl", PopoverSide.Start, PopoverAlignment.Start, 12, 794, 120, 0, 0)]
    [InlineData("bl", PopoverSide.Start, PopoverAlignment.Center, 12, 754, 120, 0, 0.5)]
    [InlineData("bl", PopoverSide.Start, PopoverAlignment.End, 12, 714, 120, 0, 1)]
    [InlineData("bl", PopoverSide.End, PopoverAlignment.Start, 110, 794, 120, 0, 0)]
    [InlineData("bl", PopoverSide.End, PopoverAlignment.Center, 110, 754, 120, 0, 0.5)]
    [InlineData("bl", PopoverSide.End, PopoverAlignment.End, 110, 714, 120, 0, 1)]
    [InlineData("br", PopoverSide.Bottom, PopoverAlignment.Start, 128, 675, 120, 1, 1)]
    [InlineData("br", PopoverSide.Bottom, PopoverAlignment.Center, 128, 675, 120, 1, 1)]
    [InlineData("br", PopoverSide.Bottom, PopoverAlignment.End, 128, 675, 120, 1, 1)]
    [InlineData("br", PopoverSide.Top, PopoverAlignment.Start, 128, 675, 120, 1, 1)]
    [InlineData("br", PopoverSide.Top, PopoverAlignment.Center, 128, 675, 120, 1, 1)]
    [InlineData("br", PopoverSide.Top, PopoverAlignment.End, 128, 675, 120, 1, 1)]
    [InlineData("br", PopoverSide.Left, PopoverAlignment.Start, 30, 794, 120, 1, 0)]
    [InlineData("br", PopoverSide.Left, PopoverAlignment.Center, 30, 754, 120, 1, 0.5)]
    [InlineData("br", PopoverSide.Left, PopoverAlignment.End, 30, 714, 120, 1, 1)]
    [InlineData("br", PopoverSide.Right, PopoverAlignment.Start, 128, 794, 120, 1, 0)]
    [InlineData("br", PopoverSide.Right, PopoverAlignment.Center, 128, 754, 120, 1, 0.5)]
    [InlineData("br", PopoverSide.Right, PopoverAlignment.End, 128, 714, 120, 1, 1)]
    [InlineData("br", PopoverSide.Start, PopoverAlignment.Start, 30, 794, 120, 1, 0)]
    [InlineData("br", PopoverSide.Start, PopoverAlignment.Center, 30, 754, 120, 1, 0.5)]
    [InlineData("br", PopoverSide.Start, PopoverAlignment.End, 30, 714, 120, 1, 1)]
    [InlineData("br", PopoverSide.End, PopoverAlignment.Start, 128, 794, 120, 1, 0)]
    [InlineData("br", PopoverSide.End, PopoverAlignment.Center, 128, 754, 120, 1, 0.5)]
    [InlineData("br", PopoverSide.End, PopoverAlignment.End, 128, 714, 120, 1, 1)]
    [InlineData("mid", PopoverSide.Bottom, PopoverAlignment.Start, 128, 240, 120, 1, 0)]
    [InlineData("mid", PopoverSide.Bottom, PopoverAlignment.Center, 70, 240, 120, 0.5, 0)]
    [InlineData("mid", PopoverSide.Bottom, PopoverAlignment.End, 12, 240, 120, 0, 0)]
    [InlineData("mid", PopoverSide.Top, PopoverAlignment.Start, 128, 80, 120, 1, 1)]
    [InlineData("mid", PopoverSide.Top, PopoverAlignment.Center, 70, 80, 120, 0.5, 1)]
    [InlineData("mid", PopoverSide.Top, PopoverAlignment.End, 12, 80, 120, 0, 1)]
    [InlineData("mid", PopoverSide.Left, PopoverAlignment.Start, 12, 200, 120, 0, 0)]
    [InlineData("mid", PopoverSide.Left, PopoverAlignment.Center, 12, 160, 120, 0, 0.5)]
    [InlineData("mid", PopoverSide.Left, PopoverAlignment.End, 12, 120, 120, 0, 1)]
    [InlineData("mid", PopoverSide.Right, PopoverAlignment.Start, 128, 200, 120, 1, 0)]
    [InlineData("mid", PopoverSide.Right, PopoverAlignment.Center, 128, 160, 120, 1, 0.5)]
    [InlineData("mid", PopoverSide.Right, PopoverAlignment.End, 128, 120, 120, 1, 1)]
    [InlineData("mid", PopoverSide.Start, PopoverAlignment.Start, 12, 200, 120, 0, 0)]
    [InlineData("mid", PopoverSide.Start, PopoverAlignment.Center, 12, 160, 120, 0, 0.5)]
    [InlineData("mid", PopoverSide.Start, PopoverAlignment.End, 12, 120, 120, 0, 1)]
    [InlineData("mid", PopoverSide.End, PopoverAlignment.Start, 128, 200, 120, 1, 0)]
    [InlineData("mid", PopoverSide.End, PopoverAlignment.Center, 128, 160, 120, 1, 0.5)]
    [InlineData("mid", PopoverSide.End, PopoverAlignment.End, 128, 120, 120, 1, 1)]
    public void B4_B5_Each_Side_And_Alignment_At_Each_Place_Is_Where_Chromium_Put_It(
        string place, PopoverSide side, PopoverAlignment alignment, double x, double y, double height, double originX, double originY)
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(Places[place].X, Places[place].Y);
        var popover = Popover();
        popover.Side = side;
        popover.Alignment = alignment;
        var (box, ox, oy) = Place(stage, popover, new PopoverEvent(trigger));
        AssertBox(x, y, 250, height, box);
        Assert.Equal(originX, ox, 6);
        Assert.Equal(originY, oy, 6);
    }

    [AvaloniaTheory]
    [InlineData("tl", 10, 50, 0, 0)]
    [InlineData("tr", 210, 50, 1, 0)]
    [InlineData("c", 145, 440, 0, 0)]
    [InlineData("bl", 10, 675, 0, 1)]
    [InlineData("br", 210, 675, 1, 1)]
    [InlineData("mid", 145, 240, 0, 0)]
    public void B7_Cover_Is_As_Wide_As_The_Trigger_With_No_Padding(string place, double x, double y, double originX, double originY)
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(Places[place].X, Places[place].Y, width: 180);
        var popover = Popover();
        popover.Size = PopoverSize.Cover;
        var (box, ox, oy) = Place(stage, popover, new PopoverEvent(trigger));
        AssertBox(x, y, 180, 120, box);
        Assert.Equal(originX, ox, 6);
        Assert.Equal(originY, oy, 6);
    }

    [AvaloniaFact]
    public void B7_Auto_Is_The_Width_Key()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 400, width: 180);
        var popover = Popover();
        popover.Resources["Avonic.Popover.Width"] = "200px";
        var (box, _, _) = Place(stage, popover, new PopoverEvent(trigger));
        AssertBox(145, 440, 200, 120, box);
    }

    [AvaloniaFact]
    public void B6_With_No_Event_It_Is_Centred_And_Grows_From_Its_Top_Left()
    {
        using var stage = new Stage();
        var popover = Popover();
        var (box, ox, oy) = Place(stage, popover, null);
        AssertBox(70, 362, 250, 120, box);
        Assert.Equal((0, 0), (ox, oy));
    }

    [AvaloniaTheory]
    [InlineData("c", 600, 128, 440, 392, 1, 0)]   // stretched to 12 from the bottom
    [InlineData("bl", 600, 12, 195, 600, 0, 1)]   // flipped above
    [InlineData("tl", 600, 12, 50, 600, 0, 0)]
    [InlineData("c", 900, 128, 440, 392, 1, 0)]
    [InlineData("bl", 900, 12, 35.41, 759.59, 0, 1)] // 90% high, flipped
    [InlineData("tl", 900, 12, 50, 759.59, 0, 0)]  // 90% high, stretched
    public void B5_Tall_Content_Flips_Or_Stretches_To_The_Bottom_Padding(
        string place, double contentHeight, double x, double y, double height, double originX, double originY)
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(Places[place].X, Places[place].Y);
        var popover = Popover(contentHeight);
        var (box, ox, oy) = Place(stage, popover, new PopoverEvent(trigger));
        AssertBox(x, y, 250, height, box);
        Assert.Equal((originX, originY), (ox, oy));
    }

    [AvaloniaFact]
    public void B5_A_Box_That_Would_Start_Past_The_Bottom_Padding_Is_Pulled_Up_To_The_Top()
    {
        // From calculateWindowAdjustment, not measured: a trigger 835 high leaves no room above to flip to, and its bottom is past
        // 844 - 12, so the box goes from 12 down to 12 from the bottom, within its maximum.
        using var stage = new Stage();
        var trigger = stage.Trigger(0, 0, 100, 835);
        var (box, _, _) = Place(stage, Popover(), new PopoverEvent(trigger));
        AssertBox(12, 12, 250, 759.6, box);
    }

    [AvaloniaTheory]
    [InlineData(50, 60, 50, 61, 0, 0)]
    [InlineData(380, 60, 128, 61, 1, 0)]
    [InlineData(200, 800, 128, 681, 1, 1)]
    [InlineData(5, 500, 12, 501, 0, 0)]
    public void B6_Reference_Event_Is_The_Events_Point(double pointX, double pointY, double x, double y, double originX, double originY)
    {
        using var stage = new Stage();
        var target = stage.Trigger(0, 0, 390, 844);
        var popover = Popover();
        popover.Reference = PopoverReference.Event;
        var (box, ox, oy) = Place(stage, popover, new PopoverEvent(target, new Point(pointX, pointY)));
        AssertBox(x, y, 250, 120, box);
        Assert.Equal((originX, originY), (ox, oy));
    }

    [AvaloniaFact]
    public void B6_Reference_Trigger_Uses_The_Trigger_Over_The_Events_Target()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 200);
        var other = stage.Trigger(10, 600, name: "Other");
        var popover = Popover();
        popover.Trigger = "Trigger";
        stage.Page.Children.Add(popover);
        Stage.Settle();

        // A click on the trigger presents it under the trigger, whatever the event's target.
        var (box, _, _) = Place(stage, popover, new PopoverEvent(other));
        AssertBox(128, 240, 250, 120, box);
    }

    [AvaloniaFact]
    public void B6_The_Events_Target_Is_The_Reference_With_No_Trigger()
    {
        using var stage = new Stage();
        var target = stage.Trigger(10, 600);
        var popover = Popover();
        var (box, _, _) = Place(stage, popover, new PopoverEvent(target));
        AssertBox(12, 640, 250, 120, box);
    }

    [AvaloniaFact]
    public void B8_The_Offset_Keys_Move_It()
    {
        using var stage = new Stage();
        var trigger = stage.Trigger(145, 400);
        var popover = Popover();
        popover.Resources["Avonic.Popover.OffsetX"] = 7d;
        popover.Resources["Avonic.Popover.OffsetY"] = -3d;
        var (box, _, _) = Place(stage, popover, new PopoverEvent(trigger));
        AssertBox(135, 437, 250, 120, box);
    }

    [AvaloniaTheory]
    [InlineData(PopoverSide.Start, 128, 400, 1)]
    [InlineData(PopoverSide.End, 12, 400, 0)]
    [InlineData(PopoverSide.Bottom, 128, 440, 1)]
    public void B16_Start_And_End_Follow_The_Pages_Direction(PopoverSide side, double x, double y, double originX)
    {
        using var stage = new Stage();
        stage.Page.FlowDirection = FlowDirection.RightToLeft;
        var trigger = stage.Trigger(145, 400);
        var popover = Popover();
        popover.Side = side;
        stage.Page.Children.Add(popover);
        Stage.Settle();
        var (box, ox, oy) = Place(stage, popover, new PopoverEvent(trigger));
        AssertBox(x, y, 250, 120, box);
        Assert.Equal((originX, 0d), (ox, oy));
        Assert.Equal(FlowDirection.RightToLeft, popover.FlowDirection);
    }

    [AvaloniaFact]
    public void B16_In_A_Right_To_Left_Window_It_Is_Placed_Left_To_Right_As_The_Viewport_Is()
    {
        // Canvas.Left 50 in a mirrored window is 240 to 340 on the screen: left 240 runs past the right, so 390 - 250 - 12, as
        // the measured rtl c bottom start; in the window's own mirrored coordinates it would fit at 50, and land at 90.
        using var stage = new Stage();
        stage.Window.FlowDirection = FlowDirection.RightToLeft;
        var trigger = stage.Trigger(50, 400);
        var (box, ox, oy) = Place(stage, Popover(), new PopoverEvent(trigger));
        Assert.Equal(240, new Rect(trigger.Bounds.Size).TransformToAABB(trigger.TransformToVisual(stage.Window)!.Value).X, 0.5);
        AssertBox(128, 440, 250, 120, box);
        Assert.Equal((1d, 0d), (ox, oy));
    }

    [AvaloniaFact]
    public void B5_The_Safe_Area_Keeps_It_From_The_Edges()
    {
        using var stage = new Stage();
        stage.Window.Resources["Avonic.SafeAreaLeft"] = 20d;
        stage.Window.Resources["Avonic.SafeAreaRight"] = 30d;
        stage.Window.Resources["Avonic.SafeAreaBottom"] = 40d;
        stage.Window.Resources["Avonic.SafeAreaTop"] = 50d;

        // Left of 12 + 20: at 12 + 20. Its content reads 0 inside.
        var left = stage.Trigger(25, 300);
        var popover = Popover();
        var (box, _, _) = Place(stage, popover, new PopoverEvent(left));
        AssertBox(32, 340, 250, 120, box);
        Assert.True(((Control)popover.Content!).TryFindResource("Avonic.SafeAreaLeft", out var inside));
        Assert.Equal(0d, inside);
        popover.Dismiss();
        stage.After(Left);

        // Past the right with its safe area: at 390 - 250 - 12 - 30.
        var right = stage.Trigger(200, 300, name: "Right");
        (box, _, _) = Place(stage, popover, new PopoverEvent(right));
        AssertBox(98, 340, 250, 120, box);
        popover.Dismiss();
        stage.After(Left);

        // Below the bottom's safe area: flipped; with no room above, held 12 + 40 from the bottom.
        var low = stage.Trigger(145, 700, name: "Low");
        (box, _, _) = Place(stage, popover, new PopoverEvent(low));
        Assert.Equal(700 - 120 + 1, box.Y, 1);
        popover.Dismiss();
        stage.After(Left);
        var tall = Popover(700);
        (box, _, _) = Place(stage, tall, new PopoverEvent(low));
        Assert.Equal(740, box.Y, 1);
        Assert.Equal(844 - 12 - 40, box.Bottom, 1);
    }

    [AvaloniaFact]
    public void B5_Cover_Takes_No_Safe_Area_Into_Its_Sums()
    {
        using var stage = new Stage();
        stage.Window.Resources["Avonic.SafeAreaLeft"] = 20d;
        var trigger = stage.Trigger(10, 300, width: 180);
        var popover = Popover();
        popover.Size = PopoverSize.Cover;
        var (box, _, _) = Place(stage, popover, new PopoverEvent(trigger));
        AssertBox(10, 340, 180, 120, box);
    }
}
