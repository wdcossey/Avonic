using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;

namespace Avonic.Components.Tests.Controls;

public class AvonicChipTests
{
    // ── Outline pseudoclass ───────────────────────────────────────────────────

    [AvaloniaFact]
    public void Outline_False_By_Default_Does_Not_Set_Outline_Pseudoclass()
    {
        var chip = new AvonicChip { Content = "Tag" };
        var window = new Window { Content = chip, Width = 390, Height = 200 };
        window.Show();

        Assert.DoesNotContain(":outline", chip.Classes);
    }

    [AvaloniaFact]
    public void Outline_True_Sets_Outline_Pseudoclass()
    {
        var chip = new AvonicChip { Content = "Tag", Outline = true };
        var window = new Window { Content = chip, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":outline", chip.Classes);
    }

    [AvaloniaFact]
    public void Outline_Changing_To_True_Updates_Pseudoclass()
    {
        var chip = new AvonicChip { Content = "Tag" };
        var window = new Window { Content = chip, Width = 390, Height = 200 };
        window.Show();

        chip.Outline = true;

        Assert.Contains(":outline", chip.Classes);
    }

    [AvaloniaFact]
    public void Outline_Changing_To_False_Clears_Pseudoclass()
    {
        var chip = new AvonicChip { Content = "Tag", Outline = true };
        var window = new Window { Content = chip, Width = 390, Height = 200 };
        window.Show();

        chip.Outline = false;

        Assert.DoesNotContain(":outline", chip.Classes);
    }

    // ── Inherits AvonicPressable ──────────────────────────────────────────────

    [AvaloniaFact]
    public void Press_Raises_Pressed_Event()
    {
        var chip = new AvonicChip { Content = "Tag", Width = 80, Height = 48 };
        var window = new Window { Content = chip, Width = 390, Height = 200 };
        window.Show();

        var raised = false;
        chip.Pressed += (_, _) => raised = true;

        window.MouseDown(new Avalonia.Point(195, 100), Avalonia.Input.MouseButton.Left);

        Assert.True(raised);
    }

    [AvaloniaFact]
    public void Disabled_Chip_Does_Not_Respond_To_Press()
    {
        var chip = new AvonicChip { Content = "Tag", IsEnabled = false, Width = 80, Height = 48 };
        var window = new Window { Content = chip, Width = 390, Height = 200 };
        window.Show();

        var raised = false;
        chip.Pressed += (_, _) => raised = true;

        window.MouseDown(new Avalonia.Point(195, 100), Avalonia.Input.MouseButton.Left);

        Assert.False(raised);
    }

    [AvaloniaFact]
    public void MinimumTouchTarget_Enforced()
    {
        var chip = new AvonicChip
        {
            Content             = "x",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Top
        };
        var window = new Window { Content = chip, Width = 390, Height = 200 };
        window.Show();

        Assert.True(chip.Bounds.Height >= 48);
    }
}
