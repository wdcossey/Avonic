using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicButtonTests
{
    // ── Fill pseudoclasses ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Fill_Solid_By_Default_Sets_FillSolid_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Solid" };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":fill-solid", button.Classes);
        Assert.DoesNotContain(":fill-outline", button.Classes);
        Assert.DoesNotContain(":fill-clear",   button.Classes);
    }

    [AvaloniaFact]
    public void Fill_Outline_Sets_FillOutline_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Outline", Fill = ButtonFill.Outline };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":fill-outline", button.Classes);
        Assert.DoesNotContain(":fill-solid", button.Classes);
        Assert.DoesNotContain(":fill-clear", button.Classes);
    }

    [AvaloniaFact]
    public void Fill_Clear_Sets_FillClear_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Clear", Fill = ButtonFill.Clear };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":fill-clear", button.Classes);
        Assert.DoesNotContain(":fill-solid",   button.Classes);
        Assert.DoesNotContain(":fill-outline", button.Classes);
    }

    [AvaloniaFact]
    public void Fill_Changing_Value_Updates_Pseudoclasses()
    {
        var button = new AvonicButton { Content = "Button" };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        button.Fill = ButtonFill.Outline;

        Assert.Contains(":fill-outline", button.Classes);
        Assert.DoesNotContain(":fill-solid", button.Classes);
    }

    // ── Size pseudoclasses ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Size_Default_Sets_SizeDefault_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Button" };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":size-default", button.Classes);
        Assert.DoesNotContain(":size-small", button.Classes);
        Assert.DoesNotContain(":size-large", button.Classes);
    }

    [AvaloniaFact]
    public void Size_Small_Sets_SizeSmall_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Small", Size = ButtonSize.Small };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":size-small", button.Classes);
        Assert.DoesNotContain(":size-default", button.Classes);
        Assert.DoesNotContain(":size-large",   button.Classes);
    }

    [AvaloniaFact]
    public void Size_Large_Sets_SizeLarge_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Large", Size = ButtonSize.Large };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":size-large", button.Classes);
        Assert.DoesNotContain(":size-small",   button.Classes);
        Assert.DoesNotContain(":size-default", button.Classes);
    }

    // ── Expand pseudoclasses ──────────────────────────────────────────────────

    [AvaloniaFact]
    public void Expand_Null_By_Default_Sets_No_Expand_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Button" };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.DoesNotContain(":expand-block", button.Classes);
        Assert.DoesNotContain(":expand-full",  button.Classes);
    }

    [AvaloniaFact]
    public void Expand_Block_Sets_ExpandBlock_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Block", Expand = ButtonExpand.Block };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":expand-block", button.Classes);
        Assert.DoesNotContain(":expand-full", button.Classes);
    }

    [AvaloniaFact]
    public void Expand_Full_Sets_ExpandFull_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Full", Expand = ButtonExpand.Full };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":expand-full", button.Classes);
        Assert.DoesNotContain(":expand-block", button.Classes);
    }

    // ── Shape + Strong ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Shape_Round_Sets_ShapeRound_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Round", Shape = ButtonShape.Round };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":shape-round", button.Classes);
    }

    [AvaloniaFact]
    public void Shape_Default_Does_Not_Set_ShapeRound_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Button" };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.DoesNotContain(":shape-round", button.Classes);
    }

    [AvaloniaFact]
    public void Strong_True_Sets_Strong_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Strong", Strong = true };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":strong", button.Classes);
    }

    [AvaloniaFact]
    public void Strong_False_By_Default_Does_Not_Set_Pseudoclass()
    {
        var button = new AvonicButton { Content = "Button" };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.DoesNotContain(":strong", button.Classes);
    }

    // ── Inherits AvonicPressable touch behaviour ──────────────────────────────

    [AvaloniaFact]
    public void MinimumTouchTarget_Enforced()
    {
        var button = new AvonicButton
        {
            Content = "Hi",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Top,
        };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        Assert.True(button.Bounds.Width  >= 48);
        Assert.True(button.Bounds.Height >= 48);
    }

    [AvaloniaFact]
    public void Disabled_Button_Does_Not_Raise_Pressed_Event()
    {
        var button = new AvonicButton { Content = "Button", IsEnabled = false };
        var window = new Window { Content = button, Width = 390, Height = 200 };
        window.Show();

        var raised = false;
        button.Pressed += (_, _) => raised = true;

        window.MouseDown(new Avalonia.Point(195, 100), Avalonia.Input.MouseButton.Left);

        Assert.False(raised);
    }
}
