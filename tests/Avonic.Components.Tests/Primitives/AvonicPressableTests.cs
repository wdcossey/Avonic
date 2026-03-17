using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avonic.Components.Primitives;

namespace Avonic.Components.Tests.Primitives;

public class AvonicPressableTests
{
    // Centre of the 200x200 test window — always within bounds of a full-stretch pressable
    private static readonly Point Centre = new(100, 100);

    [AvaloniaFact]
    public void Press_Sets_Pressed_Pseudoclass()
    {
        var pressable = new AvonicPressable();
        var window = new Window { Content = pressable, Width = 200, Height = 200 };
        window.Show();

        window.MouseDown(Centre, MouseButton.Left);

        Assert.Contains(AvonicPressable.PressedPseudoClass, pressable.Classes);
    }

    [AvaloniaFact]
    public void Release_Within_Bounds_Clears_Pressed_Pseudoclass()
    {
        var pressable = new AvonicPressable();
        var window = new Window { Content = pressable, Width = 200, Height = 200 };
        window.Show();

        window.MouseDown(Centre, MouseButton.Left);
        window.MouseUp(Centre, MouseButton.Left);

        Assert.DoesNotContain(AvonicPressable.PressedPseudoClass, pressable.Classes);
        Assert.DoesNotContain(AvonicPressable.CancelledPseudoClass, pressable.Classes);
    }

    [AvaloniaFact]
    public void Press_Raises_Pressed_Event()
    {
        var pressable = new AvonicPressable();
        var window = new Window { Content = pressable, Width = 200, Height = 200 };
        window.Show();

        var raised = false;
        pressable.Pressed += (_, _) => raised = true;

        window.MouseDown(Centre, MouseButton.Left);

        Assert.True(raised);
    }

    [AvaloniaFact]
    public void Release_Within_Bounds_Raises_Released_Event()
    {
        var pressable = new AvonicPressable();
        var window = new Window { Content = pressable, Width = 200, Height = 200 };
        window.Show();

        var raised = false;
        pressable.Released += (_, _) => raised = true;

        window.MouseDown(Centre, MouseButton.Left);
        window.MouseUp(Centre, MouseButton.Left);

        Assert.True(raised);
    }

    [AvaloniaFact]
    public void MinimumTouchTarget_Enforced_When_Content_Is_Smaller()
    {
        var pressable = new AvonicPressable
        {
            Content = new Border { Width = 10, Height = 10 },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var window = new Window { Content = pressable, Width = 200, Height = 200 };
        window.Show();

        Assert.True(pressable.Bounds.Width  >= 48);
        Assert.True(pressable.Bounds.Height >= 48);
    }

    [AvaloniaFact]
    public void MinimumTouchTarget_Does_Not_Shrink_Larger_Content()
    {
        var pressable = new AvonicPressable
        {
            Content = new Border { Width = 100, Height = 100 },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var window = new Window { Content = pressable, Width = 200, Height = 200 };
        window.Show();

        Assert.True(pressable.Bounds.Width  >= 100);
        Assert.True(pressable.Bounds.Height >= 100);
    }

    [AvaloniaFact]
    public void Disabled_Control_Does_Not_Respond_To_Press()
    {
        var pressable = new AvonicPressable { IsEnabled = false };
        var window = new Window { Content = pressable, Width = 200, Height = 200 };
        window.Show();

        var raised = false;
        pressable.Pressed += (_, _) => raised = true;

        window.MouseDown(Centre, MouseButton.Left);

        Assert.False(raised);
        Assert.DoesNotContain(AvonicPressable.PressedPseudoClass, pressable.Classes);
    }
}
