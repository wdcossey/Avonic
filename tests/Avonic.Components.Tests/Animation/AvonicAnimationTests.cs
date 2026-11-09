using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avonic.Components.Animation;

namespace Avonic.Components.Tests.Animation;

/// <summary>
/// Ionic's <c>createAnimation</c> on the test clock. The expected values are Chromium's, sampled by <c>el.animate()</c> at the
/// same local times on a 200 x 300 element: the table "Web Animations values" in docs/infra/overlays/PLAN.md. Frames come every
/// 16 ms; an animation played at 0 starts at the first, so its local time t is shown at 16 + t.
/// </summary>
public class AvonicAnimationTests
{
    private const double FirstFrame = 16;

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

    private static Matrix Transform(Visual element) => element.RenderTransform?.Value ?? Matrix.Identity;

    /// <summary>Advances to local time <paramref name="local"/> of an animation played at 0.</summary>
    private static void At(TestClock clock, double local)
    {
        clock.At(FirstFrame + local);
        Settle();
    }

    private static void Near(double expected, double actual, double tolerance = 1e-4) =>
        Assert.True(Math.Abs(expected - actual) <= tolerance, $"expected {expected}, was {actual}");

    // ── A1: easing and interpolation, as Chromium ────────────────────────────────────────────

    [AvaloniaFact]
    public void A1_The_Alert_Md_Enter_Animation_Has_Chromiums_Values()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create()
            .AddElement(element)
            .Keyframes([
                new AnimationKeyframe(0) { ["opacity"] = "0.01", ["transform"] = "scale(0.9)" },
                new AnimationKeyframe(1) { ["opacity"] = "1", ["transform"] = "scale(1)" },
            ])
            .Easing("ease-in-out")
            .Duration(150)
            .Play();

        foreach (var (t, opacity, scale) in new[] { (0.0, 0.01, 0.9), (32, 0.102319, 0.909325), (80, 0.561736, 0.955731), (112, 0.86861, 0.986728), (144, 0.996953, 0.999692), (160, 1, 1) })
        {
            At(clock, t);
            Near(opacity, element.Opacity);
            Near(scale, Transform(element).M11);
            Near(scale, Transform(element).M22);
        }
    }

    [AvaloniaFact]
    public void A1_The_Action_Sheet_Md_Enter_Animation_Translates_By_A_Percentage_Of_The_Elements_Height()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create()
            .AddElement(element)
            .FromTo("transform", "translateY(100%)", "translateY(0%)")
            .Easing("cubic-bezier(.36,.66,.04,1)")
            .Duration(400)
            .Play();

        foreach (var (t, y) in new[] { (0.0, 300.0), (48, 219.68), (96, 100.87), (208, 15.8117), (400, 0) })
        {
            At(clock, t);
            Near(y, Transform(element).M32, 1e-3);
        }
    }

    [AvaloniaFact]
    public void A1_The_Modal_Md_Leave_Animation_Has_Chromiums_Values()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create()
            .AddElement(element)
            .Keyframes([
                new AnimationKeyframe(0) { ["opacity"] = 0.99, ["transform"] = "translateY(0px)" },
                new AnimationKeyframe(1) { ["opacity"] = 0, ["transform"] = "translateY(40px)" },
            ])
            .Easing("cubic-bezier(0.47,0,0.745,0.715)")
            .Duration(200)
            .Play();

        foreach (var (t, opacity, y) in new[] { (48.0, 0.925564, 2.6035), (96, 0.724577, 10.7242), (144, 0.400144, 23.8326) })
        {
            At(clock, t);
            Near(opacity, element.Opacity);
            Near(y, Transform(element).M32, 1e-3);
        }
    }

    [AvaloniaTheory]
    [InlineData("ease-in", 0.04125, 0.294144)]
    [InlineData("ease-out", 0.250589, 0.662901)]
    [InlineData("linear", 0.16, 0.48)]
    public void A1_The_Css_Easing_Keywords_Have_Chromiums_Values(string easing, double at16, double at48)
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Easing(easing).Duration(100).Play();

        At(clock, 16);
        Near(at16, element.Opacity);
        At(clock, 48);
        Near(at48, element.Opacity);
    }

    [AvaloniaFact]
    public void A1_Two_Transform_Functions_Interpolate_One_By_One()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create().AddElement(element).FromTo("transform", "translateY(40px) scale(0.5)", "translateY(0px) scale(1)").Duration(100).Play();

        At(clock, 48);
        var matrix = Transform(element);
        Near(0.74, matrix.M11);
        // The translate isn't scaled: CSS applies the last function first.
        Near(20.8, matrix.M32, 1e-3);
    }

    // ── A2: the timing model ─────────────────────────────────────────────────────────────────

    [AvaloniaTheory]
    [InlineData(AnimationFill.None, 1.0, 1.0)]
    [InlineData(AnimationFill.Backwards, 0.0, 1.0)]
    [InlineData(AnimationFill.Forwards, 1.0, 0.0)]
    [InlineData(AnimationFill.Both, 0.0, 0.0)]
    public void A2_Delay_Iterations_Alternate_And_Each_Fill_Have_Chromiums_Values(AnimationFill fill, double before, double after)
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create()
            .AddElement(element)
            .FromTo("opacity", 0, 1)
            .Duration(100)
            .Delay(50)
            .Iterations(2)
            .Direction(AnimationDirection.Alternate)
            .Easing("ease")
            .Fill(fill)
            .Play();

        // 1 is the element's own opacity, which shows where the effect has no value.
        foreach (var (t, opacity) in new[] { (0.0, before), (16, before), (80, 0.513315), (128, 0.970068), (144, 0.998007), (160, 0.994316), (240, 0.094796), (256, after), (304, after) })
        {
            At(clock, t);
            Near(opacity, element.Opacity);
        }
    }

    [AvaloniaFact]
    public void A2_Reverse_Plays_From_The_End_As_The_Md_Menu_Does()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create()
            .AddElement(element)
            .FromTo("transform", "translateX(-288px)", "translateX(0px)")
            .Easing("cubic-bezier(0.0,0.0,0.2,1)")
            .Duration(300)
            .Direction(AnimationDirection.Reverse)
            .Play();

        foreach (var (t, x) in new[] { (0.0, 0.0), (96, -17.3969), (288, -246.275), (304, -288) })
        {
            At(clock, t);
            Near(x, Transform(element).M31, 1e-3);
        }
    }

    [AvaloniaFact]
    public void A2_Alternate_Reverse_Starts_Backwards()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(100).Iterations(2).Direction(AnimationDirection.AlternateReverse).Play();

        At(clock, 16);
        Near(0.84, element.Opacity);
        At(clock, 112);
        Near(0.12, element.Opacity);
    }

    [AvaloniaFact]
    public void A2_A_Fractional_Iteration_Count_Reversed_Ends_Part_Way()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(100).Iterations(1.5).Direction(AnimationDirection.Reverse).Fill(AnimationFill.Forwards).Play();

        foreach (var (t, opacity) in new[] { (48.0, 0.52), (112, 0.88), (144, 0.56), (160, 0.5) })
        {
            At(clock, t);
            Near(opacity, element.Opacity);
        }
    }

    [AvaloniaFact]
    public void A2_Keyframes_With_No_Offsets_Are_Spaced_Evenly()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create()
            .AddElement(element)
            .Keyframes([new AnimationKeyframe { ["opacity"] = 0 }, new AnimationKeyframe { ["opacity"] = 1 }, new AnimationKeyframe { ["opacity"] = 0.5 }])
            .Duration(100)
            .Play();

        foreach (var (t, opacity) in new[] { (16.0, 0.32), (48, 0.96), (80, 0.7) })
        {
            At(clock, t);
            Near(opacity, element.Opacity);
        }
    }

    [AvaloniaFact]
    public void A2_Infinite_Iterations_Never_Finish()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var played = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(100).Iterations(double.PositiveInfinity).Play();

        At(clock, 1056);
        Near(0.56, element.Opacity);
        Assert.False(played.IsCompleted);
    }

    // ── A3: parents and children ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A3_A_Child_Takes_Its_Parents_Timing_When_It_Has_None()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var child = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1);
        var parent = AvonicAnimation.Create().Duration(100).Easing("ease-in").Delay(10).Fill(AnimationFill.Forwards)
            .Direction(AnimationDirection.Reverse).Iterations(2).AddAnimation(child);

        Assert.Same(parent, child.ParentAnimation);
        Assert.Equal((100.0, "ease-in", 10.0, AnimationFill.Forwards, AnimationDirection.Reverse, 2.0),
            (child.GetDuration(), child.GetEasing(), child.GetDelay(), child.GetFill(), child.GetDirection(), child.GetIterations()));

        var own = AvonicAnimation.Create().Duration(50);
        parent.AddAnimation(own);
        Assert.Equal(50, own.GetDuration());

        parent.Play();
        At(clock, 64);
        // At 54 of 100, after the delay of 10, reversed, then eased by ease-in.
        Near(new CubicBezierEasing(0.42, 0, 1, 1).Ease(0.46), element.Opacity);
    }

    [AvaloniaFact]
    public void A3_The_Defaults_Are_Ionics()
    {
        var animation = AvonicAnimation.Create("id");
        Assert.Equal(("id", 0.0, "linear", 0.0, AnimationFill.Both, AnimationDirection.Normal, 1.0),
            (animation.Id, animation.GetDuration(), animation.GetEasing(), animation.GetDelay(), animation.GetFill(), animation.GetDirection(), animation.GetIterations()));
    }

    [AvaloniaFact]
    public void A3_The_Parent_Finishes_When_Every_Child_Has()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var other = new Border();
        var parentFinished = 0;
        var parent = AvonicAnimation.Create().Duration(100)
            .AddAnimation(
                AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1),
                AvonicAnimation.Create().AddElement(other).FromTo("opacity", 0, 1).Duration(200))
            .OnFinish((_, _) => parentFinished++);

        var played = parent.Play();
        At(clock, 112);
        Assert.Equal(0, parentFinished);
        Assert.True(parent.IsRunning());
        At(clock, 208);
        Settle();
        Assert.Equal(1, parentFinished);
        Assert.True(played.IsCompleted);
        Assert.False(parent.IsRunning());
    }

    [AvaloniaFact]
    public void A3_An_Animation_With_No_Elements_Or_No_Keyframes_Finishes_At_Once()
    {
        using var clock = new TestClock();
        var noElements = AvonicAnimation.Create().FromTo("opacity", 0, 1).Duration(100).Play();
        var noKeyframes = AvonicAnimation.Create().AddElement(new Border()).Duration(100).Play();
        Settle();

        Assert.True(noElements.IsCompleted);
        Assert.True(noKeyframes.IsCompleted);
    }

    // ── A4: playback ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A4_Play_Shows_The_First_Values_At_Once_And_Finishes_At_The_First_Frame_Past_The_End()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var played = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0.2, 0.6).Duration(100).Play();

        Assert.Equal(0.2, element.Opacity, 6);

        At(clock, 96);
        Settle();
        Assert.False(played.IsCompleted);
        At(clock, 112);
        Settle();
        Assert.True(played.IsCompleted);
        // fill: both holds the last value.
        Assert.Equal(0.6, element.Opacity, 6);
    }

    [AvaloniaFact]
    public void A4_A_Zero_Duration_Animation_Finishes_At_The_Next_Frame_With_Its_Last_Values()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var played = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0.2, 0.6).Duration(0).Play();

        Settle();
        Assert.False(played.IsCompleted);
        Assert.Equal(0.6, element.Opacity, 6);
        clock.At(FirstFrame);
        Settle();
        Assert.True(played.IsCompleted);
    }

    [AvaloniaFact]
    public void A4_A_Sync_Play_Has_No_Duration_And_The_Next_Play_Has_Its_Own()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0.2, 0.6).Duration(1000);

        var sync = animation.Play(sync: true);
        Assert.Equal(0, animation.GetDuration());
        clock.At(FirstFrame);
        Settle();
        Assert.True(sync.IsCompleted);
        Assert.Equal(1000, animation.GetDuration());

        var normal = animation.Play();
        clock.At(500);
        Settle();
        Assert.False(normal.IsCompleted);
        Assert.True(element.Opacity < 0.6);
    }

    [AvaloniaFact]
    public void A4_Playing_A_Finished_Animation_Again_Starts_From_The_Beginning()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(100);
        animation.Play();
        clock.At(200);
        Settle();

        animation.Play();
        Assert.Equal(0, element.Opacity, 6);
        clock.At(216);
        clock.At(264);
        Near(0.48, element.Opacity);
    }

    [AvaloniaFact]
    public void A4_A_Duration_Set_While_Running_Applies_At_Once()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(1000);
        animation.Play();
        At(clock, 48);

        animation.Duration(100);
        At(clock, 64);
        Near(0.64, element.Opacity);
    }

    // ── A5: pause, stop, destroy, callbacks ──────────────────────────────────────────────────

    [AvaloniaFact]
    public void A5_Pause_Holds_The_Value_And_Play_Resumes_From_It()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(100);
        var played = animation.Play();
        At(clock, 48);

        animation.Pause();
        Assert.False(animation.IsRunning());
        clock.At(400);
        Settle();
        Near(0.48, element.Opacity);
        Assert.False(played.IsCompleted);

        animation.Play();
        Assert.True(animation.IsRunning());
        clock.At(416);
        Near(0.48, element.Opacity);
        clock.At(432);
        Near(0.64, element.Opacity);
        clock.At(464);
        Settle();
        Assert.False(played.IsCompleted);
        clock.At(480);
        Settle();
        Assert.True(played.IsCompleted);
    }

    [AvaloniaFact]
    public void A5_Stop_Takes_The_Values_Away_And_Completes_Play()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var finished = 0;
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0.2, 0.6).Duration(100).OnFinish((_, _) => finished++);
        var played = animation.Play();
        At(clock, 48);

        animation.Stop();
        Settle();
        Assert.Equal(1, element.Opacity);
        Assert.True(played.IsCompleted);
        Assert.False(animation.IsRunning());
        Assert.Equal(0, finished);

        // The finish callbacks stay for the next play, and it starts again from its first values.
        animation.Play();
        Assert.Equal(0.2, element.Opacity, 6);
        clock.At(400);
        Assert.Equal(1, finished);
    }

    [AvaloniaFact]
    public void A5_Destroy_Takes_The_Held_Values_Away()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var child = AvonicAnimation.Create().AddElement(element).FromTo("transform", "scale(0.5)", "scale(0.8)");
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0.2, 0.6).Duration(100).AddAnimation(child);
        animation.Play();
        clock.At(200);
        Assert.Equal(0.6, element.Opacity, 6);
        Near(0.8, Transform(element).M11);

        animation.Destroy();
        Assert.Equal(1, element.Opacity);
        Assert.Equal(Matrix.Identity, Transform(element));
        Assert.Empty(animation.Elements);
        Assert.Empty(animation.ChildAnimations);
        Assert.Empty(animation.GetKeyframes());
    }

    [AvaloniaFact]
    public void A5_OnFinish_Runs_Every_Time_Or_Once_With_The_Step_And_The_Animation()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var every = new List<int>();
        var once = new List<int>();
        AvonicAnimation? seen = null;
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(50)
            .OnFinish((step, a) =>
            {
                every.Add(step);
                seen = a;
            })
            .OnFinish((step, _) => once.Add(step), oneTimeCallback: true);

        animation.Play();
        clock.At(100);
        animation.Play();
        clock.At(200);

        Assert.Equal([1, 1], every);
        Assert.Equal([1], once);
        Assert.Same(animation, seen);
    }

    [AvaloniaFact]
    public void A5_A_Later_Animation_Of_The_Same_Property_Wins_And_The_Earlier_Shows_Again_When_It_Goes()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0.01, 0.32).Duration(100).Play();
        clock.At(200);
        var leave = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0.32, 0).Duration(100);
        leave.Play();
        clock.At(216 + 48);
        Near(0.32 * 0.52, element.Opacity);

        leave.Destroy();
        Assert.Equal(0.32, element.Opacity, 6);
    }

    // ── A6: hooks ────────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void A6_Reads_Then_Writes_Run_At_The_First_Play_And_At_The_Finish()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var log = new List<string>();
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(50)
            .BeforeAddWrite(() => log.Add("before write"))
            .BeforeAddRead(() => log.Add("before read"))
            .AfterAddWrite(() => log.Add("after write"))
            .AfterAddRead(() => log.Add("after read"));

        animation.Play();
        Assert.Equal(["before read", "before write"], log);
        clock.At(100);
        Assert.Equal(["before read", "before write", "after read", "after write"], log);
    }

    [AvaloniaFact]
    public void A6_Classes_Are_Added_And_Removed_At_The_Start_And_The_Finish()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        element.Classes.Add("old");
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(50)
            .BeforeAddClass("x").BeforeRemoveClass("old").AfterRemoveClass("x").AfterAddClass("y");

        animation.Play();
        Assert.Equal(["x"], element.Classes);
        clock.At(100);
        Assert.Equal(["y"], element.Classes);
    }

    [AvaloniaFact]
    public void A6_Pointer_Events_None_During_The_Animation_Is_Cleared_After_It_As_The_Overlays_Backdrop_Has_It()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(50)
            .BeforeStyles(new() { ["pointer-events"] = "none" })
            .AfterClearStyles(["pointer-events"]);

        animation.Play();
        Assert.False(element.IsHitTestVisible);
        clock.At(100);
        Assert.True(element.IsHitTestVisible);
        Assert.False(element.IsSet(InputElement.IsHitTestVisibleProperty));
    }

    [AvaloniaFact]
    public void A6_Opacity_Styles_Are_Inline_And_The_Animation_Beats_Them_While_It_Runs()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        var animation = AvonicAnimation.Create().AddElement(element).FromTo("opacity", 0, 1).Duration(50).Fill(AnimationFill.None)
            .BeforeStyles(new() { ["opacity"] = "0.3" })
            .AfterStyles(new() { ["opacity"] = 0.7, ["pointer-events"] = "auto" })
            .BeforeClearStyles(["pointer-events"]);

        animation.Play();
        Assert.Equal(0, element.Opacity, 6);
        Assert.Equal(0.3, element.GetBaseValue(Visual.OpacityProperty).Value, 6);
        clock.At(100);
        Assert.Equal(0.7, element.Opacity, 6);
        Assert.True(element.IsSet(InputElement.IsHitTestVisibleProperty));
    }

    [AvaloniaFact]
    public void A6_A_Property_It_Cannot_Animate_Or_Style_Is_Refused_When_Given()
    {
        var animation = AvonicAnimation.Create();
        Assert.Throws<ArgumentException>(() => animation.FromTo("top", "0px", "10px"));
        Assert.Throws<ArgumentException>(() => animation.FromTo("transform", "skew(10deg)", "none"));
        Assert.Throws<ArgumentException>(() => animation.Keyframes([new AnimationKeyframe(0) { ["easing"] = "ease" }]));
        Assert.Throws<ArgumentException>(() => animation.BeforeStyles(new() { ["transform-origin"] = "top left" }));
        Assert.Throws<ArgumentException>(() => animation.AfterClearStyles(["left"]));
        Assert.Throws<ArgumentException>(() => animation.Easing("steps(4)"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationKeyframe(1.5));
    }

    // ── A7: percentages and the element's size ───────────────────────────────────────────────

    [AvaloniaFact]
    public void A7_A_Percentage_Follows_The_Elements_Size_From_Before_Its_First_Layout()
    {
        using var clock = new TestClock();
        using var scene = new Scene(new Panel());
        var element = new Border { Width = 200, Height = 300, VerticalAlignment = VerticalAlignment.Top };
        ((Panel)scene.Window.Content!).Children.Add(element);
        Assert.Equal(default, element.Bounds);

        AvonicAnimation.Create().AddElement(element).FromTo("transform", "translateY(100%)", "translateY(0%)").Duration(400).Play();
        // Laid out, and drawn, before any frame of the animation.
        Settle();
        Assert.Equal(300, Transform(element).M32, 6);

        element.Height = 100;
        Settle();
        Assert.Equal(100, Transform(element).M32, 6);
    }

    [AvaloniaFact]
    public void A7_None_Interpolates_With_The_Others_Identity()
    {
        using var clock = new TestClock();
        var (scene, element) = Stage();
        using var _ = scene;
        AvonicAnimation.Create().AddElement(element).FromTo("transform", "none", "translate(10px, 50%) scale(2) rotate(90deg)").Duration(100).Play();

        At(clock, 48);
        // CSS applies the last first: rotate, then scale, then translate, each 48% of the way from its identity.
        var expected = Matrix.CreateRotation(0.48 * Math.PI / 2) * Matrix.CreateScale(1.48, 1.48) * Matrix.CreateTranslation(4.8, 0.48 * 0.5 * 300);
        var actual = Transform(element);
        Near(expected.M11, actual.M11);
        Near(expected.M12, actual.M12);
        Near(expected.M31, actual.M31);
        Near(expected.M32, actual.M32);
    }

    // ── A8: cubic-bezier.js ──────────────────────────────────────────────────────────────────

    /// <summary>The values Ionic's own <c>getTimeGivenProgression</c> gave in Node, including 0.849 at 0.5, where the curve is symmetric.</summary>
    [AvaloniaTheory]
    [InlineData(0.01, 0.06868376293383441)]
    [InlineData(0.25, 0.34162218679968365)]
    [InlineData(0.5, 0.8490381056766578)]
    [InlineData(0.75, 0.6583778132003169)]
    [InlineData(0.9999, 0.9930783988885179)]
    public void A8_GetTimeGivenProgression_Is_Ionics_On_The_Menus_Curve(double progression, double expected)
    {
        var times = CubicBezier.GetTimeGivenProgression(new Point(0, 0), new Point(0.4, 0), new Point(0.6, 1), new Point(1, 1), progression);
        Near(expected, Assert.Single(times), 1e-12);
    }

    [AvaloniaFact]
    public void A8_GetTimeGivenProgression_Is_Ionics_On_Other_Curves()
    {
        Near(0.16015409375296139, Assert.Single(CubicBezier.GetTimeGivenProgression(new(0, 0), new(0.32, 0.72), new(0, 1), new(1, 1), 0.5)), 1e-12);
        Near(0.30000000000000027, Assert.Single(CubicBezier.GetTimeGivenProgression(new(0, 0), new(0, 0), new(1, 1), new(1, 1), 0.3)), 1e-12);
        Assert.Empty(CubicBezier.GetTimeGivenProgression(new(0, 0), new(0.4, 0), new(0.6, 1), new(1, 1), 1.5));
    }

    [AvaloniaFact]
    public void A8_CubicBezierEasing_Refuses_An_X_Outside_0_To_1_And_Clamps_Its_Input()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CubicBezierEasing(1.2, 0, 0.5, 1));
        var easing = new CubicBezierEasing(0.42, 0, 0.58, 1);
        Assert.Equal(0, easing.Ease(-1));
        Assert.Equal(1, easing.Ease(2));
        Assert.Null(CubicBezierEasing.ParseCss("linear"));
    }
}
