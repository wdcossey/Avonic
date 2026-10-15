using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

/// <summary>
/// Behaviours B1–B10 from docs/components/ion-ripple-effect/PLAN.md, read from Ionic's
/// ripple-effect.js / ripple-effect.css. Numbers are the spec, not the implementation.
/// </summary>
public class AvonicRippleEffectTests
{
    private const double HostWidth  = 240;
    private const double HostHeight = 96;

    /// <summary>A 240×96 surface at the window's top-left containing a ripple.</summary>
    private sealed class Fixture : IDisposable
    {
        public readonly AvonicRippleEffect Ripple;
        public readonly Border Host;
        private readonly Scene _scene;

        public Fixture(RippleEffectType type = RippleEffectType.Bounded)
        {
            Ripple = new AvonicRippleEffect { Type = type };
            Host = new Border
            {
                Width = HostWidth, Height = HostHeight,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.LightGray,
                Child = new Panel { Children = { Ripple } },
            };
            _scene = new Scene(Host);
        }

        public void Dispose() => _scene.Dispose();
    }

    // ── B1: fills parent, not hit-testable, presentation-only ────────────────────────────────

    [AvaloniaFact]
    public void B1_Fills_Parent_Is_Not_HitTestable_And_Is_Presentation_Only()
    {
        using var f = new Fixture();

        Assert.Equal(new Size(HostWidth, HostHeight), f.Ripple.Bounds.Size);
        Assert.False(f.Ripple.IsHitTestVisible);
        Assert.False(f.Ripple.Focusable);
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(f.Ripple));
    }

    // ── B2: bounded clips, unbounded does not ────────────────────────────────────────────────

    [AvaloniaFact]
    public void B2_Bounded_Clips_To_Own_Bounds_Unbounded_Does_Not()
    {
        using var bounded   = new Fixture(RippleEffectType.Bounded);
        using var unbounded = new Fixture(RippleEffectType.Unbounded);

        Assert.True(bounded.Ripple.ClipToBounds);
        Assert.DoesNotContain(":type-unbounded", bounded.Ripple.Classes);   // Ionic host class `unbounded`

        Assert.False(unbounded.Ripple.ClipToBounds);
        Assert.Contains(":type-unbounded", unbounded.Ripple.Classes);

        unbounded.Ripple.Type = RippleEffectType.Bounded;
        Assert.True(unbounded.Ripple.ClipToBounds);
        Assert.DoesNotContain(":type-unbounded", unbounded.Ripple.Classes);
    }

    // ── B3 + B4 + B5: geometry ───────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void B3_B4_B5_Bounded_Geometry_Matches_addRipple()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        f.Ripple.AddRipple(30, 20);
        var r = Assert.Single(f.Ripple.Ripples);

        var initialSize = Math.Floor(Math.Max(HostWidth, HostHeight) * 0.5);           // 120
        var hypotenuse  = Math.Sqrt(HostWidth * HostWidth + HostHeight * HostHeight);
        Assert.Equal(initialSize, r.InitialSize);
        Assert.Equal(initialSize, r.Width);
        Assert.Equal(initialSize, r.Height);
        Assert.Equal((hypotenuse + 10) / initialSize, r.FinalScale, 10);                // maxRadius = hypot + PADDING
        Assert.Equal(30 - initialSize * 0.5, Canvas.GetLeft(r));                        // origin − initialSize/2
        Assert.Equal(20 - initialSize * 0.5, Canvas.GetTop(r));
        Assert.Equal(new Vector(HostWidth * 0.5 - 30, HostHeight * 0.5 - 20), r.TranslateEnd);
    }

    [AvaloniaFact]
    public void B3_B4_B5_Unbounded_Starts_At_Centre_Regardless_Of_Press_Point()
    {
        using var clock = new TestClock();
        using var f = new Fixture(RippleEffectType.Unbounded);

        f.Ripple.AddRipple(5, 5);
        var r = Assert.Single(f.Ripple.Ripples);

        var initialSize = Math.Floor(Math.Max(HostWidth, HostHeight) * 0.5);
        Assert.Equal(Math.Max(HostWidth, HostHeight) / initialSize, r.FinalScale, 10);   // maxRadius = maxDim
        Assert.Equal(HostWidth  * 0.5 - initialSize * 0.5, Canvas.GetLeft(r));
        Assert.Equal(HostHeight * 0.5 - initialSize * 0.5, Canvas.GetTop(r));
        Assert.Equal(new Vector(0, 0), r.TranslateEnd);
    }

    // ── B6: expand 225 ms cubic-bezier(.4,0,.2,1); fade-in 75 ms linear to 0.16 ─────────────

    [AvaloniaFact]
    public void B6_Expand_And_FadeIn_Timelines()
    {
        using var clock = new TestClock();
        using var f = new Fixture();
        f.Ripple.AddRipple(120, 48);
        var r = Assert.Single(f.Ripple.Ripples);

        Assert.Equal(0, r.Progress);
        Assert.Equal(0, r.Opacity);

        clock.At(40); f.Ripple.Tick();
        Assert.Equal(RippleVisual.ExpandEasing.Ease(40.0 / 225), r.Progress, 10);
        Assert.Equal(0.16 * 40 / 75, r.Opacity, 10);

        clock.At(160); f.Ripple.Tick();
        Assert.Equal(RippleVisual.ExpandEasing.Ease(160.0 / 225), r.Progress, 10);
        Assert.Equal(0.16, r.Opacity, 10);

        clock.At(300); f.Ripple.Tick();
        Assert.Equal(1, r.Progress, 10);
        Assert.Equal(0.16, r.Opacity, 10);
        Assert.Null(r.FadeOutStartedAt);
    }

    // ── B7: currentColor — fill inherits the parent's Foreground ────────────────────────────

    [AvaloniaFact]
    public void B7_Fill_Is_The_Inherited_Foreground()
    {
        using var clock = new TestClock();
        using var f = new Fixture();
        f.Host.SetValue(TextElement.ForegroundProperty, Brushes.Red);

        f.Ripple.AddRipple(10, 10);
        var r = Assert.Single(f.Ripple.Ripples);

        Assert.Same(Brushes.Red, r.Fill);

        f.Host.SetValue(TextElement.ForegroundProperty, Brushes.Blue);
        Assert.Same(Brushes.Blue, r.Fill);
    }

    // ── B8: release cannot take effect before 325 ms ─────────────────────────────────────────

    [AvaloniaFact]
    public void B8_Early_Release_Is_Deferred_To_325ms_Late_Release_Is_Immediate()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        var release = f.Ripple.AddRipple(120, 48);
        var r = Assert.Single(f.Ripple.Ripples);

        clock.At(100); release();
        clock.At(200); f.Ripple.Tick();
        Assert.Null(r.FadeOutStartedAt);
        Assert.Equal(0.16, r.Opacity, 10);

        clock.At(330); f.Ripple.Tick();
        Assert.Equal(TimeSpan.FromMilliseconds(325), r.FadeOutStartedAt);
        Assert.Equal(0.16 * (1 - 5.0 / 150), r.Opacity, 10);

        var late = f.Ripple.AddRipple(120, 48);
        var r2 = f.Ripple.Ripples[^1];
        clock.At(330 + 400); late();
        Assert.Equal(TimeSpan.FromMilliseconds(730), r2.FadeOutStartedAt);
    }

    // ── B9: fade-out 150 ms linear, removed 200 ms after it starts ───────────────────────────

    [AvaloniaFact]
    public void B9_FadeOut_Then_Removal()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        var release = f.Ripple.AddRipple(120, 48);
        var r = Assert.Single(f.Ripple.Ripples);

        clock.At(500); release();                      // fade-out starts at 500
        clock.At(575); f.Ripple.Tick();
        Assert.Equal(0.16 * 0.5, r.Opacity, 10);

        clock.At(650); f.Ripple.Tick();
        Assert.Equal(0, r.Opacity, 10);
        Assert.Single(f.Ripple.Ripples);               // still present until 200 ms

        clock.At(699); f.Ripple.Tick();
        Assert.Single(f.Ripple.Ripples);

        clock.At(700); f.Ripple.Tick();
        Assert.Empty(f.Ripple.Ripples);
    }

    // ── B10: ripples are independent; the component never removes one unasked ───────────────

    [AvaloniaFact]
    public void B10_Multiple_Ripples_Coexist_Until_Each_Is_Released()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        var first  = f.Ripple.AddRipple(10, 10);
        clock.At(50);
        var second = f.Ripple.AddRipple(200, 80);
        Assert.Equal(2, f.Ripple.Ripples.Count);

        clock.At(5000); f.Ripple.Tick();
        Assert.Equal(2, f.Ripple.Ripples.Count);

        first();
        clock.At(5200); f.Ripple.Tick();
        Assert.Single(f.Ripple.Ripples);

        second();
        clock.At(5400); f.Ripple.Tick();
        Assert.Empty(f.Ripple.Ripples);
    }

    // ── Idle: a held ripple stops animating at its end state; release restarts it ───────────

    [AvaloniaFact]
    public void Held_Ripple_Idles_At_End_State_And_Animates_Again_On_Release()
    {
        using var clock = new TestClock();
        using var f = new Fixture();

        var release = f.Ripple.AddRipple(120, 48);
        Assert.True(f.Ripple.IsFrameTimerRunning);

        clock.At(224); f.Ripple.Tick();
        Assert.True(f.Ripple.IsFrameTimerRunning);

        clock.At(225); f.Ripple.Tick();
        Assert.False(f.Ripple.IsFrameTimerRunning);            // expanded and faded in, nothing pending

        clock.At(5000); release();
        Assert.True(f.Ripple.IsFrameTimerRunning);

        clock.At(5200); f.Ripple.Tick();
        Assert.Empty(f.Ripple.Ripples);
        Assert.False(f.Ripple.IsFrameTimerRunning);
    }

    // ── Guard: no size yet → no-op, no exception ─────────────────────────────────────────────

    [AvaloniaFact]
    public void AddRipple_On_Unsized_Control_Is_A_NoOp()
    {
        var ripple  = new AvonicRippleEffect();
        var release = ripple.AddRipple(0, 0);

        Assert.Empty(ripple.Ripples);
        release();
    }
}
