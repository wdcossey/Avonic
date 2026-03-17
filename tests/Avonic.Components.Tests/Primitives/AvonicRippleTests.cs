using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avonic.Components.Primitives;

namespace Avonic.Components.Tests.Primitives;

public class AvonicRippleTests
{
    // ── Defaults ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Default_RippleType_Is_Bounded()
    {
        var ripple = new AvonicRipple();
        Assert.Equal(RippleType.Bounded, ripple.RippleType);
    }

    [AvaloniaFact]
    public void Default_RippleOpacity_Is_0_16()
    {
        var ripple = new AvonicRipple();
        Assert.Equal(0.16, ripple.RippleOpacity);
    }

    [AvaloniaFact]
    public void Default_RippleFill_Is_White()
    {
        var ripple = new AvonicRipple();
        Assert.Equal(Brushes.White, ripple.RippleFill);
    }

    // ── Layout constraints ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void IsHitTestVisible_Is_False_By_Default()
    {
        var ripple = new AvonicRipple();
        Assert.False(ripple.IsHitTestVisible);
    }

    [AvaloniaFact]
    public void HorizontalAlignment_Is_Stretch_By_Default()
    {
        var ripple = new AvonicRipple();
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Stretch, ripple.HorizontalAlignment);
    }

    [AvaloniaFact]
    public void VerticalAlignment_Is_Stretch_By_Default()
    {
        var ripple = new AvonicRipple();
        Assert.Equal(Avalonia.Layout.VerticalAlignment.Stretch, ripple.VerticalAlignment);
    }

    // ── Parent wiring via AvonicPressable ─────────────────────────────────────

    [AvaloniaFact]
    public void Attaches_To_Visual_Tree_Without_Error_Inside_Pressable()
    {
        var ripple   = new AvonicRipple();
        var pressable = new AvonicPressable { Content = ripple, Width = 120, Height = 48 };
        var window   = new Window { Content = pressable, Width = 390, Height = 200 };

        // Should not throw
        var ex = Record.Exception(() => window.Show());
        Assert.Null(ex);
    }

    [AvaloniaFact]
    public void Detaches_From_Visual_Tree_Without_Error()
    {
        var ripple    = new AvonicRipple();
        var pressable = new AvonicPressable { Content = ripple, Width = 120, Height = 48 };
        var window    = new Window { Content = pressable, Width = 390, Height = 200 };
        window.Show();

        // Removing the content should cleanly unsubscribe
        var ex = Record.Exception(() => pressable.Content = null);
        Assert.Null(ex);
    }

    // ── Press interaction ─────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Press_On_Pressable_Does_Not_Throw()
    {
        var ripple    = new AvonicRipple();
        var pressable = new AvonicPressable { Content = ripple, Width = 120, Height = 48 };
        var window    = new Window { Content = pressable, Width = 390, Height = 200 };
        window.Show();

        var ex = Record.Exception(() =>
            window.MouseDown(new Avalonia.Point(195, 100), MouseButton.Left));
        Assert.Null(ex);
    }

    [AvaloniaFact]
    public void Press_And_Release_On_Pressable_Does_Not_Throw()
    {
        var ripple    = new AvonicRipple();
        var pressable = new AvonicPressable { Content = ripple, Width = 120, Height = 48 };
        var window    = new Window { Content = pressable, Width = 390, Height = 200 };
        window.Show();

        var ex = Record.Exception(() =>
        {
            window.MouseDown(new Avalonia.Point(195, 100), MouseButton.Left);
            window.MouseUp(new Avalonia.Point(195, 100), MouseButton.Left);
        });
        Assert.Null(ex);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void RippleType_Can_Be_Set_To_Unbounded()
    {
        var ripple = new AvonicRipple { RippleType = RippleType.Unbounded };
        Assert.Equal(RippleType.Unbounded, ripple.RippleType);
    }

    [AvaloniaFact]
    public void RippleFill_Can_Be_Set()
    {
        var ripple = new AvonicRipple { RippleFill = Brushes.Black };
        Assert.Equal(Brushes.Black, ripple.RippleFill);
    }

    [AvaloniaFact]
    public void RippleOpacity_Can_Be_Set()
    {
        var ripple = new AvonicRipple { RippleOpacity = 0.3 };
        Assert.Equal(0.3, ripple.RippleOpacity);
    }

    [AvaloniaFact]
    public void CornerRadius_Can_Be_Set()
    {
        var radius = new Avalonia.CornerRadius(8);
        var ripple = new AvonicRipple { CornerRadius = radius };
        Assert.Equal(radius, ripple.CornerRadius);
    }
}
