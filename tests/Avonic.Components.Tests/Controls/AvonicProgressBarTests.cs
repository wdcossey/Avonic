using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avonic.Components.Controls;
using Avonic.Components.Controls.Enums;

namespace Avonic.Components.Tests.Controls;

public class AvonicProgressBarTests
{
    // ── Type pseudoclasses ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Type_Determinate_By_Default_Sets_Determinate_Pseudoclass()
    {
        var bar = new AvonicProgressBar();
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":determinate", bar.Classes);
        Assert.DoesNotContain(":indeterminate", bar.Classes);
    }

    [AvaloniaFact]
    public void Type_Indeterminate_Sets_Indeterminate_Pseudoclass()
    {
        var bar = new AvonicProgressBar { Type = ProgressBarType.Indeterminate };
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":indeterminate", bar.Classes);
        Assert.DoesNotContain(":determinate", bar.Classes);
    }

    [AvaloniaFact]
    public void Type_Changing_Updates_Pseudoclasses()
    {
        var bar = new AvonicProgressBar();
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        bar.Type = ProgressBarType.Indeterminate;

        Assert.Contains(":indeterminate", bar.Classes);
        Assert.DoesNotContain(":determinate", bar.Classes);
    }

    // ── Reversed pseudoclass ──────────────────────────────────────────────────

    [AvaloniaFact]
    public void Reversed_False_By_Default_Does_Not_Set_Pseudoclass()
    {
        var bar = new AvonicProgressBar();
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        Assert.DoesNotContain(":reversed", bar.Classes);
    }

    [AvaloniaFact]
    public void Reversed_True_Sets_Reversed_Pseudoclass()
    {
        var bar = new AvonicProgressBar { Reversed = true };
        var window = new Window { Content = bar, Width = 390, Height = 200 };
        window.Show();

        Assert.Contains(":reversed", bar.Classes);
    }

    // ── Value clamping ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Value_Clamped_To_Zero_When_Negative()
    {
        var bar = new AvonicProgressBar { Value = -0.5 };

        Assert.Equal(0.0, bar.Value);
    }

    [AvaloniaFact]
    public void Value_Clamped_To_One_When_Over()
    {
        var bar = new AvonicProgressBar { Value = 1.5 };

        Assert.Equal(1.0, bar.Value);
    }

    [AvaloniaFact]
    public void Value_Within_Range_Is_Preserved()
    {
        var bar = new AvonicProgressBar { Value = 0.75 };

        Assert.Equal(0.75, bar.Value);
    }

    // ── Buffer clamping ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void Buffer_Defaults_To_One()
    {
        var bar = new AvonicProgressBar();

        Assert.Equal(1.0, bar.Buffer);
    }

    [AvaloniaFact]
    public void Buffer_Clamped_To_Zero_When_Negative()
    {
        var bar = new AvonicProgressBar { Buffer = -1.0 };

        Assert.Equal(0.0, bar.Buffer);
    }

    [AvaloniaFact]
    public void Buffer_Clamped_To_One_When_Over()
    {
        var bar = new AvonicProgressBar { Buffer = 2.0 };

        Assert.Equal(1.0, bar.Buffer);
    }
}
