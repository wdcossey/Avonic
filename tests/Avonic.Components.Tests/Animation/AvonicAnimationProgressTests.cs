using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Animation;

namespace Avonic.Components.Tests.Animation;

/// <summary>
/// Ionic's <c>progressStart</c>, <c>progressStep</c> and <c>progressEnd</c>, and <c>keyframes</c> on an animation that has started
/// (<c>setKeyframes</c>), from <c>utils/animation/animation.js</c>, which the sheet modal's gesture drives. A step is held at
/// <c>delay + duration * step</c> (<c>setAnimationStep</c>), clamped to 0..0.9999; <c>progressEnd</c> plays from there at the next
/// frame, as <see cref="AvonicAnimationTests"/>' plays do. The eased values are <see cref="CubicBezierEasing"/>'s, which those
/// tests pin to Chromium's.
/// </summary>
public class AvonicAnimationProgressTests
{
    private static readonly CubicBezierEasing SheetEasing = new(0.32, 0.72, 0, 1);

    private static (Scene Scene, Border Element) Stage()
    {
        var element = new Border
        {
            Width = 200,
            Height = 300,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Red,
        };
        return (new Scene(new Panel { Children = { element } }), element);
    }

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static double Y(Visual element) => element.RenderTransform?.Value.M32 ?? 0;

    private static void Near(double expected, double actual, double tolerance = 1e-4) =>
        Assert.True(Math.Abs(expected - actual) <= tolerance, $"expected {expected}, was {actual}");

    /// <summary>The sheet's wrapper animation: 0 to 100 px down, 500 ms, its easing.</summary>
    private static AvonicAnimation Sheet(Visual element) => AvonicAnimation.Create()
        .AddElement(element)
        .FromTo("transform", "translateY(0px)", "translateY(100px)")
        .Easing("cubic-bezier(0.32,0.72,0,1)")
        .Duration(500);

    [AvaloniaFact]
    public void P1_ProgressStart_Holds_The_Step_Linearly_Without_Playing()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var animation = Sheet(element);

        animation.ProgressStart(forceLinearEasing: true, 0.25);
        Near(25, Y(element));
        Assert.Equal("linear", animation.GetEasing());
        Assert.False(animation.IsRunning());
        clock.At(400);
        Settle();
        Near(25, Y(element));

        // Without forcing linear, the step is a time, eased as the animation is.
        var other = Stage();
        using var __ = other.Scene;
        Sheet(other.Element).ProgressStart(forceLinearEasing: false, 0.25);
        Near(SheetEasing.Ease(0.25) * 100, Y(other.Element));
    }

    [AvaloniaFact]
    public void P2_ProgressStep_Moves_The_Hold_Clamped_To_0_And_0_9999()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var animation = Sheet(element).ProgressStart(true, 0);

        animation.ProgressStep(0.6);
        Near(60, Y(element));
        animation.ProgressStep(1.5);
        Near(99.99, Y(element));
        animation.ProgressStep(-1);
        Near(0, Y(element));
        clock.At(1000);
        Settle();
        Near(0, Y(element));
    }

    [AvaloniaFact]
    public void P3_ProgressEnd_1_Plays_From_The_Step_To_The_End_Over_The_Duration_Given_With_The_Easing_Back()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var steps = new List<int>();
        var animation = Sheet(element).Duration(1000).ProgressStart(true, 0);
        animation.ProgressStep(0.4);

        animation.OnFinish((step, a) => steps.Add(step), oneTimeCallback: true).ProgressEnd(1, 0.6, 500);

        // Held at 0.6 of the forced 500 ms, eased: the move starts at the next frame.
        Near(SheetEasing.Ease(0.6) * 100, Y(element));
        Assert.Equal(500, animation.GetDuration());
        Assert.Equal("cubic-bezier(0.32,0.72,0,1)", animation.GetEasing());
        Assert.True(animation.IsRunning());
        clock.At(16);
        Near(SheetEasing.Ease(0.6) * 100, Y(element));
        clock.At(16 + 96);
        Near(SheetEasing.Ease(396 / 500.0) * 100, Y(element));
        clock.At(16 + 192);
        Assert.Empty(steps);
        clock.At(16 + 208);
        Near(100, Y(element));
        Assert.Equal([1], steps);

        // The forced duration went with the finish.
        Assert.Equal(1000, animation.GetDuration());
    }

    [AvaloniaFact]
    public void P4_ProgressEnd_0_Plays_Back_To_The_Start_And_Finishes_At_Step_0()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var steps = new List<(int Step, AnimationDirection Direction, double Duration)>();
        var animation = Sheet(element).ProgressStart(true, 0.6);

        animation.OnFinish((step, a) => steps.Add((step, a.GetDirection(), a.GetDuration())), oneTimeCallback: true).ProgressEnd(0, 0.6, 200);

        Assert.Equal(AnimationDirection.Reverse, animation.GetDirection());
        Near(SheetEasing.Ease(0.6) * 100, Y(element));
        clock.At(16 + 32);
        Near(SheetEasing.Ease(1 - (80 + 32) / 200.0) * 100, Y(element));
        clock.At(16 + 128);
        Near(0, Y(element));

        // Step 0, and the coercions gone before the callback.
        Assert.Equal([(0, AnimationDirection.Normal, 500.0)], steps);
    }

    [AvaloniaFact]
    public void P5_Keyframes_On_A_Started_Animation_Replace_Them_Where_It_Is()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var animation = Sheet(element).ProgressStart(true, 0.5);
        Near(50, Y(element));

        animation.Keyframes([
            new AnimationKeyframe(0) { ["transform"] = "translateY(100px)" },
            new AnimationKeyframe(1) { ["transform"] = "translateY(300px)" },
        ]);
        Near(200, Y(element));

        // And while it plays.
        animation.ProgressEnd(1, 0.5, 500);
        clock.At(16 + 96);
        animation.Keyframes([
            new AnimationKeyframe(0) { ["transform"] = "translateY(0px)" },
            new AnimationKeyframe(1) { ["transform"] = "translateY(10px)" },
        ]);
        Near(SheetEasing.Ease(346 / 500.0) * 10, Y(element));
    }

    [AvaloniaFact]
    public void P6_A_Child_Follows_Its_Parents_Progress_And_Plays_With_It()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var child = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 1, 0);
        var finished = false;
        var parent = AvonicAnimation.Create().Duration(500).Easing("cubic-bezier(0.32,0.72,0,1)").AddAnimation(child);

        parent.ProgressStart(true, 0.25);
        Near(0.75, element.Opacity);
        Assert.Equal("linear", child.GetEasing());
        parent.ProgressStep(0.5);
        Near(0.5, element.Opacity);

        parent.OnFinish((_, _) => finished = true, oneTimeCallback: true).ProgressEnd(1, 0, 300);
        Assert.Equal(300, child.GetDuration());
        Near(1, element.Opacity);
        clock.At(16 + 144);
        Near(1 - SheetEasing.Ease(144 / 300.0), element.Opacity);
        clock.At(16 + 304);
        Assert.True(finished);
        Near(0, element.Opacity);
    }

    [AvaloniaFact]
    public void P7_Max_Height_Is_A_Percentage_Of_The_Parents_Height()
    {
        using var clock = new TestClock();
        var element = new Border { VerticalAlignment = VerticalAlignment.Top, Child = new Border { Height = 1000 } };
        var parent = new Panel { Height = 400, Children = { element } };
        using var scene = new Scene(new Panel { Children = { parent } });
        var animation = AvonicAnimation.Create().AddElement(element).Duration(500)
            .Keyframes([new AnimationKeyframe(0) { ["maxHeight"] = "100%" }, new AnimationKeyframe(1) { ["maxHeight"] = "0%" }]);

        animation.ProgressStart(true, 0.25);
        Assert.Equal(300, element.MaxHeight, 6);
        scene.Window.UpdateLayout();
        Assert.Equal(300, element.Bounds.Height, 6);

        parent.Height = 200;
        scene.Window.UpdateLayout();
        Assert.Equal(150, element.MaxHeight, 6);

        animation.Keyframes([new AnimationKeyframe(0) { ["maxHeight"] = "120px" }, new AnimationKeyframe(1) { ["maxHeight"] = 40 }]);
        Assert.Equal(100, element.MaxHeight, 6);

        animation.Destroy();
        Assert.Equal(double.PositiveInfinity, element.MaxHeight);
    }
}
